using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using NPhies_FHIR_Integration.Infrastructure.Configuration;

namespace NPhies_FHIR_Integration.Application.Nphies.Validation;

/// <summary>Validates every resource in an NPHIES bundle against the IG profile(s) named in its <c>meta.profile</c>.</summary>
public interface INphiesProfileValidator
{
    NphiesConformancePackage Package { get; }

    NphiesValidationResult Validate(Bundle bundle);
}

/// <summary>
/// Local NPHIES IG conformance checks driven by the StructureDefinition snapshots of the NPHIES package:
/// cardinality, choice types, fixed/pattern values, slicing (value/pattern/type discriminators, closed slicing,
/// slice cardinality), required bindings to enumerable NPHIES value sets, NPHIES extension definitions and the
/// NPHIES business invariants (BV-xxxxx FHIRPath constraints). Constraints that cannot be evaluated locally
/// (filtered/external value sets, exists/profile discriminators, unsupported FHIRPath) are skipped, never guessed.
/// </summary>
public sealed class NphiesProfileValidator : INphiesProfileValidator
{
    public const string RuleProfile = "PRF-PROFILE";
    public const string RuleCardinality = "PRF-CARD";
    public const string RuleType = "PRF-TYPE";
    public const string RuleFixed = "PRF-FIXED";
    public const string RuleCode = "PRF-CODE";
    public const string RuleSlice = "PRF-SLICE";
    public const string RuleExtension = "PRF-EXT";
    public const string RuleInternal = "PRF-INTERNAL";

    private const int MaxDepth = 48;
    private const int MaxIssues = 300;
    private static readonly Regex CoreInvariantKey = new("^[a-z]+-\\d+$", RegexOptions.Compiled);

    /// <summary>
    /// IG invariants whose published FHIRPath contradicts their own human text and the official examples; evaluated
    /// with the intended expression instead. BV-00036 ("No Claim item detail provided when Claim item package = true")
    /// is published as <c>a implies b implies detail.exists().not()</c>, which (left-associative) rejects packages
    /// that do carry details, e.g. docs example "19 - Shadow billing".
    /// </summary>
    private static readonly Dictionary<string, string> CorrectedInvariants = new(StringComparer.Ordinal)
    {
        ["BV-00036"] = "extension('" + Constants.NphiesExtensions.Package + "').where(value = true).exists() implies detail.exists()",
    };

    private readonly ConcurrentDictionary<string, ProfileIndex> _indexes = new(StringComparer.Ordinal);
    private readonly ConditionalWeakTable<Base, ITypedElement> _typedPatterns = new();

    public NphiesProfileValidator(NphiesConformancePackage package) => Package = package;

    public NphiesConformancePackage Package { get; }

    public NphiesValidationResult Validate(Bundle bundle)
    {
        var ctx = new Run();
        try
        {
            var root = new ScopedNode(bundle.ToTypedElement(ModelInfo.ModelInspector));
            ValidateResource(ctx, root);
        }
        catch (Exception ex)
        {
            ctx.Add(RuleInternal, NphiesValidationSeverity.Warning, "Profile validation aborted: " + ex.Message, null);
        }
        return ctx.Result;
    }

    // ---------------------------------------------------------------- resources

    private void ValidateResource(Run ctx, ScopedNode resource)
    {
        var profiles = resource.Children("meta").SelectMany(m => m.Children("profile"))
            .Select(p => p.Value as string).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        foreach (var url in profiles)
        {
            var index = IndexOf(url!);
            if (index is null)
            {
                ctx.Add(RuleProfile, NphiesValidationSeverity.Warning,
                    $"Profile '{url}' is not defined in NPHIES IG package {Package.PackageName}#{Package.PackageVersion}.", resource.Location);
                continue;
            }
            var previous = ctx.Resource;
            ctx.Resource = resource;
            ValidateElement(ctx, resource, index, index.RootId, 0);
            ctx.Resource = previous;
        }

        if (resource.InstanceType == "Bundle")
            foreach (var nested in resource.Children("entry").SelectMany(e => e.Children("resource")).OfType<ScopedNode>())
                ValidateResource(ctx, nested);
    }

    // ---------------------------------------------------------------- elements

    private void ValidateElement(Run ctx, ITypedElement node, ProfileIndex p, string elementId, int depth)
    {
        if (depth > MaxDepth || ctx.Full) return;
        var ed = p.ById[elementId];
        CheckInvariants(ctx, node, ed);

        if (!p.Groups.TryGetValue(elementId, out var groups))
        {
            FollowTypeProfile(ctx, node, ed, depth);
            return;
        }
        foreach (var group in groups) ValidateGroup(ctx, node, p, group, depth);
    }

    private void ValidateGroup(Run ctx, ITypedElement parent, ProfileIndex p, ChildGroup group, int depth)
    {
        var baseEd = group.Base;
        var instances = parent.Children(group.Name).ToList();
        var location = parent.Location + "." + group.Name;

        var label = baseEd.ElementId;
        var sliceMinReported = false;

        if (group.IsChoice && baseEd.Type.Count > 0)
            foreach (var inst in instances.Where(i => !baseEd.Type.Any(t => TypeMatches(t.Code, i.InstanceType))))
                ctx.Add(RuleType, NphiesValidationSeverity.Error,
                    $"{label}: type '{inst.InstanceType}' is not allowed (allowed: {string.Join(", ", baseEd.Type.Select(t => t.Code))}).", inst.Location);

        var assigned = new ElementDefinition?[instances.Count];
        if (group.Slices.Count > 0 && baseEd.Slicing?.Discriminator.Count > 0)
        {
            var evaluable = true;
            for (var i = 0; i < instances.Count && evaluable; i++)
            {
                foreach (var slice in group.Slices)
                {
                    var match = SliceMatches(instances[i], slice, baseEd.Slicing!, p);
                    if (match is null) { evaluable = false; break; }
                    if (match.Value) { assigned[i] = slice; break; }
                }
            }

            if (evaluable)
            {
                if (baseEd.Slicing!.Rules == ElementDefinition.SlicingRules.Closed)
                    for (var i = 0; i < instances.Count; i++)
                        if (assigned[i] is null)
                            ctx.Add(RuleSlice, NphiesValidationSeverity.Error,
                                $"{label}: element does not match any of the allowed slices ({string.Join(", ", group.Slices.Select(s => s.SliceName))}).", instances[i].Location);

                foreach (var slice in group.Slices)
                    sliceMinReported |= CheckCardinality(ctx, RuleSlice, slice, assigned.Count(a => ReferenceEquals(a, slice)), location, slice.ElementId, checkMin: true);
            }
            else
            {
                Array.Clear(assigned);
            }
        }

        // When a required slice is missing, the slice error is the actionable one; the derived base minimum is noise.
        CheckCardinality(ctx, RuleCardinality, baseEd, instances.Count, location, label, checkMin: !sliceMinReported);

        for (var i = 0; i < instances.Count; i++)
            ValidateInstance(ctx, instances[i], p, assigned[i] ?? baseEd, depth + 1);
    }

    private void ValidateInstance(Run ctx, ITypedElement inst, ProfileIndex p, ElementDefinition ed, int depth)
    {
        if (ctx.Full) return;
        // Bundled and contained resources are validated against their own meta.profile.
        if (inst.InstanceType is not null && ModelInfo.IsKnownResource(inst.InstanceType)) return;

        var expected = ed.Fixed ?? ed.Pattern;
        if (expected is not null && !PatternMatches(inst, Typed(expected)))
            ctx.Add(RuleFixed, NphiesValidationSeverity.Error,
                $"{ed.ElementId}: value does not match the required {(ed.Fixed is not null ? "fixed" : "pattern")} value {Describe(expected)}.", inst.Location);

        if (ed.Binding is { Strength: BindingStrength.Required, ValueSet: not null })
            CheckBinding(ctx, inst, ed);

        if (inst.InstanceType == "Extension" && ValidateExtension(ctx, inst, ed, depth)) return;

        ValidateElement(ctx, inst, p, ed.ElementId, depth);
    }

    /// <summary>Validates an extension against its NPHIES definition. Returns false when the definition is not packaged.</summary>
    private bool ValidateExtension(Run ctx, ITypedElement inst, ElementDefinition ed, int depth)
    {
        var url = inst.Children("url").FirstOrDefault()?.Value as string;
        if (string.IsNullOrWhiteSpace(url)) return false;
        var index = IndexOf(url);
        if (index is null)
        {
            if (url.StartsWith("http://nphies.sa/", StringComparison.OrdinalIgnoreCase))
                ctx.Add(RuleExtension, NphiesValidationSeverity.Warning, $"Extension '{url}' is not defined in the NPHIES IG package.", inst.Location);
            return false;
        }
        CheckInvariants(ctx, inst, ed);
        ValidateElement(ctx, inst, index, index.RootId, depth + 1);
        return true;
    }

    /// <summary>For snapshot leaves typed with an NPHIES data-type profile, continue into that profile.</summary>
    private void FollowTypeProfile(Run ctx, ITypedElement node, ElementDefinition ed, int depth)
    {
        if (ed.Type.Count != 1 || node.InstanceType == "Extension") return;
        foreach (var profile in ed.Type[0].Profile)
        {
            var index = IndexOf(profile);
            if (index is not null && index.Sd.Kind != StructureDefinition.StructureDefinitionKind.Resource)
                ValidateElement(ctx, node, index, index.RootId, depth + 1);
        }
    }

    // ---------------------------------------------------------------- rules

    /// <returns>True when a minimum-cardinality error was reported.</returns>
    private static bool CheckCardinality(Run ctx, string rule, ElementDefinition ed, int count, string location, string label, bool checkMin)
    {
        var min = ed.Min ?? 0;
        var minFailed = checkMin && count < min;
        if (minFailed)
            ctx.Add(rule, NphiesValidationSeverity.Error,
                count == 0 ? $"{label}: required element is missing." : $"{label}: minimum cardinality {min} not met (found {count}).", location);
        if (ed.Max is not null && ed.Max != "*" && int.TryParse(ed.Max, out var max) && count > max)
            ctx.Add(rule, NphiesValidationSeverity.Error,
                max == 0 ? $"{label}: element is not allowed by the NPHIES profile." : $"{label}: maximum cardinality {max} exceeded (found {count}).", location);
                        return minFailed;
                    }

    private void CheckBinding(Run ctx, ITypedElement inst, ElementDefinition ed)
    {
        var valueSet = ed.Binding.ValueSet;
        var codes = Package.Expand(valueSet);
        if (codes is null || codes.Count == 0) return;

        switch (inst.InstanceType)
        {
            case "code":
                if (inst.Value is string code && !codes.ContainsCode(code))
                    ctx.Add(RuleCode, NphiesValidationSeverity.Error, $"{ed.Path}: code '{code}' is not in required value set {valueSet}.", inst.Location);
                break;
            case "Coding":
                var (system, value) = CodingOf(inst);
                if (value is not null && !codes.Contains(system, value))
                    ctx.Add(RuleCode, NphiesValidationSeverity.Error, $"{ed.Path}: code '{system}|{value}' is not in required value set {valueSet}.", inst.Location);
                break;
            case "CodeableConcept":
                var codings = inst.Children("coding").Select(CodingOf).Where(c => c.Code is not null).ToList();
                if (codings.Count > 0 && !codings.Any(c => codes.Contains(c.System, c.Code!)))
                    ctx.Add(RuleCode, NphiesValidationSeverity.Error,
                        $"{ed.Path}: none of the codings ({string.Join(", ", codings.Select(c => $"{c.System}|{c.Code}"))}) is in required value set {valueSet}.", inst.Location);
                break;
        }
    }

    private static (string? System, string? Code) CodingOf(ITypedElement coding)
        => (coding.Children("system").FirstOrDefault()?.Value as string, coding.Children("code").FirstOrDefault()?.Value as string);

    private static void CheckInvariants(Run ctx, ITypedElement node, ElementDefinition ed)
    {
        foreach (var constraint in ed.Constraint)
        {
            if (string.IsNullOrWhiteSpace(constraint.Expression) || constraint.Key is null || CoreInvariantKey.IsMatch(constraint.Key)) continue;
            var expression = CorrectedInvariants.TryGetValue(constraint.Key, out var corrected) ? corrected : constraint.Expression;
            bool ok;
            try
            {
                ok = IValueProviderFPExtensions.Predicate(node, expression, ctx.EvaluationContext());
            }
            catch
            {
                ctx.SkippedInvariants++;
                continue;
            }
            if (!ok)
                ctx.Add(constraint.Key,
                    constraint.Severity == ConstraintSeverity.Error ? NphiesValidationSeverity.Error : NphiesValidationSeverity.Warning,
                    constraint.Human ?? constraint.Expression, node.Location);
        }
    }

    // ---------------------------------------------------------------- slicing

    private bool? SliceMatches(ITypedElement inst, ElementDefinition slice, ElementDefinition.SlicingComponent slicing, ProfileIndex p)
    {
        foreach (var d in slicing.Discriminator)
        {
            bool? match = d.Type switch
            {
                ElementDefinition.DiscriminatorType.Value or ElementDefinition.DiscriminatorType.Pattern => ValueDiscriminator(inst, slice, d.Path, p),
                ElementDefinition.DiscriminatorType.Type when d.Path == "$this" => slice.Type.Count == 0 ? null : slice.Type.Any(t => TypeMatches(t.Code, inst.InstanceType)),
                _ => null,
            };
            if (match is null) return null;
            if (!match.Value) return false;
        }
        return true;
    }

    private bool? ValueDiscriminator(ITypedElement inst, ElementDefinition slice, string path, ProfileIndex p)
    {
        Base? expected;
        IEnumerable<ITypedElement> targets;
        if (path == "$this")
        {
            expected = slice.Fixed ?? slice.Pattern;
            targets = new[] { inst };
        }
        else
        {
            expected = p.ById.TryGetValue(slice.ElementId + "." + path, out var child) ? child.Fixed ?? child.Pattern : null;
            if (expected is null && path == "url" && slice.Type.FirstOrDefault()?.Profile.FirstOrDefault() is { } profile)
                expected = new FhirUri(NphiesConformancePackage.Canonical(profile));
            targets = Navigate(inst, path);
        }
        if (expected is null) return null;
        var typed = Typed(expected);
        return targets.Any(t => PatternMatches(t, typed));
    }

    private static IEnumerable<ITypedElement> Navigate(ITypedElement node, string path)
    {
        IEnumerable<ITypedElement> current = new[] { node };
        foreach (var step in path.Split('.'))
        {
            var name = step.EndsWith("[x]", StringComparison.Ordinal) ? step[..^3] : step;
            current = current.SelectMany(c => c.Children(name)).ToList();
        }
        return current;
    }

    // ---------------------------------------------------------------- values

    private ITypedElement Typed(Base value) => _typedPatterns.GetValue(value, v => v.ToTypedElement(ModelInfo.ModelInspector));

    /// <summary>Pattern semantics: every value present in <paramref name="pattern"/> must be present in the instance.</summary>
    private static bool PatternMatches(ITypedElement instance, ITypedElement pattern)
    {
        if (pattern.Value is not null && !ValueEquals(instance.Value, pattern.Value)) return false;
        foreach (var group in pattern.Children().GroupBy(c => c.Name))
        {
            var candidates = instance.Children(group.Key).ToList();
            foreach (var expectedChild in group)
                if (!candidates.Any(c => PatternMatches(c, expectedChild))) return false;
        }
        return true;
    }

    private static bool ValueEquals(object? actual, object expected)
    {
        if (actual is null) return false;
        if (IsNumber(actual) && IsNumber(expected))
            return Convert.ToDecimal(actual, CultureInfo.InvariantCulture) == Convert.ToDecimal(expected, CultureInfo.InvariantCulture);
        return string.Equals(Convert.ToString(actual, CultureInfo.InvariantCulture), Convert.ToString(expected, CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private static bool IsNumber(object o) => o is decimal or int or long or double or float;

    private static bool TypeMatches(string? code, string? instanceType)
    {
        if (code is null || instanceType is null) return true;
        if (code.StartsWith("http://hl7.org/fhirpath/", StringComparison.Ordinal)) return true;
        return string.Equals(code, instanceType, StringComparison.OrdinalIgnoreCase);
    }

    private string Describe(Base value)
    {
        var typed = Typed(value);
        var parts = new List<string>();
        void Walk(ITypedElement n, string prefix)
        {
            if (n.Value is not null) parts.Add($"{prefix}{(prefix.Length > 0 ? "=" : "")}{n.Value}");
            foreach (var c in n.Children()) Walk(c, prefix.Length == 0 ? c.Name : prefix + "." + c.Name);
        }
        Walk(typed, "");
        return parts.Count == 0 ? value.TypeName : "[" + string.Join(", ", parts) + "]";
    }

    // ---------------------------------------------------------------- profile indexes

    private ProfileIndex? IndexOf(string url)
    {
        var sd = Package.FindProfile(url);
        return sd is null ? null : _indexes.GetOrAdd(NphiesConformancePackage.Canonical(sd.Url), _ => new ProfileIndex(sd));
    }

    private sealed class ChildGroup
    {
        public required string Name { get; init; }
        public required ElementDefinition Base { get; init; }
        public bool IsChoice { get; init; }
        public List<ElementDefinition> Slices { get; } = new();
    }

    private sealed class ProfileIndex
    {
        public StructureDefinition Sd { get; }
        public string RootId { get; }
        public Dictionary<string, ElementDefinition> ById { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, List<ChildGroup>> Groups { get; } = new(StringComparer.Ordinal);

        public ProfileIndex(StructureDefinition sd)
        {
            Sd = sd;
            var elements = sd.Snapshot.Element.Where(e => !string.IsNullOrEmpty(e.ElementId)).ToList();
            RootId = elements[0].ElementId;
            foreach (var e in elements) ById[e.ElementId] = e;

            foreach (var e in elements.Skip(1))
            {
                var id = e.ElementId;
                var dot = id.LastIndexOf('.');
                if (dot < 0) continue;
                var parentId = id[..dot];
                var segment = id[(dot + 1)..];
                if (segment.Contains('/')) continue; // re-slicing is not evaluated
                var colon = segment.IndexOf(':');
                var elementName = colon < 0 ? segment : segment[..colon];

                if (!Groups.TryGetValue(parentId, out var groups)) Groups[parentId] = groups = new List<ChildGroup>();
                var group = groups.FirstOrDefault(g => g.Base.ElementId == parentId + "." + elementName);
                if (colon < 0)
                {
                    if (group is null)
                        groups.Add(new ChildGroup
                        {
                            Name = elementName.EndsWith("[x]", StringComparison.Ordinal) ? elementName[..^3] : elementName,
                            Base = e,
                            IsChoice = elementName.EndsWith("[x]", StringComparison.Ordinal),
                        });
                }
                else
                {
                    group?.Slices.Add(e);
                }
            }
        }
    }

    private sealed class Run
    {
        public NphiesValidationResult Result { get; } = new();
        public ScopedNode? Resource { get; set; }
        public int SkippedInvariants { get; set; }
        public bool Full => Result.Issues.Count >= MaxIssues;
        private readonly HashSet<(string, string?, string)> _seen = new();

        public FhirEvaluationContext EvaluationContext() => Resource is null ? new FhirEvaluationContext() : new FhirEvaluationContext(Resource);

        public void Add(string rule, NphiesValidationSeverity severity, string message, string? expression)
        {
            if (Full || !_seen.Add((rule, expression, message))) return;
            Result.Issues.Add(new NphiesValidationIssue(rule, severity, message, expression));
        }
    }
}

/// <summary>
/// Runs the NPHIES business-rule validator and, depending on <see cref="NphiesProfileValidationMode"/>, the IG profile
/// validator. In <c>Warn</c> mode profile findings are reported as warnings so they never block a send.
/// </summary>
public sealed class CompositeNphiesBundleValidator : INphiesBundleValidator
{
    private readonly INphiesBundleValidator _rules;
    private readonly INphiesProfileValidator? _profiles;
    private readonly NphiesProfileValidationMode _mode;

    public CompositeNphiesBundleValidator(INphiesBundleValidator rules, INphiesProfileValidator? profiles, NphiesProfileValidationMode mode)
    {
        _rules = rules;
        _profiles = profiles;
        _mode = profiles is null ? NphiesProfileValidationMode.Off : mode;
    }

    public NphiesProfileValidationMode Mode => _mode;

    public NphiesValidationResult Validate(Bundle bundle, long? serializedSizeBytes = null)
    {
        var result = _rules.Validate(bundle, serializedSizeBytes);
        if (_mode == NphiesProfileValidationMode.Off) return result;

        foreach (var issue in _profiles!.Validate(bundle).Issues)
            result.Issues.Add(_mode == NphiesProfileValidationMode.Enforce ? issue : issue with { Severity = NphiesValidationSeverity.Warning });
        return result;
    }
}
