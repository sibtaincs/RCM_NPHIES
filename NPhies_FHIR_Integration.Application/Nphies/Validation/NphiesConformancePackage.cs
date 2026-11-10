using System.Collections.Concurrent;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace NPhies_FHIR_Integration.Application.Nphies.Validation;

/// <summary>
/// The NPHIES FHIR IG package (<c>https://portal.nphies.sa/ig/package.tgz</c>): StructureDefinitions (with snapshots),
/// ValueSets and CodeSystems indexed by canonical URL. The package is embedded in the assembly; a newer package can be
/// supplied through <c>Nphies:ProfileValidation:PackagePath</c> without a rebuild.
/// </summary>
public sealed class NphiesConformancePackage
{
    public const string EmbeddedResourceName = "NPhies.IgPackage.tgz";

    private readonly Dictionary<string, StructureDefinition> _profiles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ValueSet> _valueSets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CodeSystem> _codeSystems = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ValueSetCodes?> _expansions = new(StringComparer.Ordinal);

    public string PackageName { get; private set; } = "unknown";
    public string PackageVersion { get; private set; } = "unknown";
    public string? PackageDate { get; private set; }
    public string Source { get; private set; } = "embedded";
    public IReadOnlyDictionary<string, StructureDefinition> Profiles => _profiles;
    public IReadOnlyDictionary<string, ValueSet> ValueSets => _valueSets;
    public IReadOnlyDictionary<string, CodeSystem> CodeSystems => _codeSystems;
    public List<string> LoadErrors { get; } = new();

    private static readonly Lazy<NphiesConformancePackage> EmbeddedInstance = new(() => Load(null));

    /// <summary>The package embedded in this assembly (loaded once per process).</summary>
    public static NphiesConformancePackage Embedded => EmbeddedInstance.Value;

    /// <summary>Loads a package from <paramref name="packagePath"/> (.tgz) or, when null/empty, from the embedded resource.</summary>
    public static NphiesConformancePackage Load(string? packagePath)
    {
        var package = new NphiesConformancePackage();
        Stream stream;
        if (!string.IsNullOrWhiteSpace(packagePath))
        {
            stream = File.OpenRead(packagePath);
            package.Source = packagePath;
        }
        else
        {
            stream = typeof(NphiesConformancePackage).Assembly.GetManifestResourceStream(EmbeddedResourceName)
                ?? throw new InvalidOperationException($"Embedded NPHIES IG package '{EmbeddedResourceName}' is missing.");
        }

        using (stream)
        using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
        using (var tar = new TarReader(gzip))
        {
            TarEntry? entry;
            while ((entry = tar.GetNextEntry()) is not null)
            {
                if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile) || entry.DataStream is null) continue;
                var name = entry.Name.Replace('\\', '/');
                if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                using var reader = new StreamReader(entry.DataStream, System.Text.Encoding.UTF8, true, 4096, leaveOpen: true);
                package.Add(name, reader.ReadToEnd());
            }
        }
        return package;
    }

#pragma warning disable CS0618 // ParserSettings is the supported way to configure the legacy FhirJsonParser in SDK 5.x
    private static readonly FhirJsonParser Parser = new(new ParserSettings
    {
        PermissiveParsing = true,
        AcceptUnknownMembers = true,
        AllowUnrecognizedEnums = true,
    });
#pragma warning restore CS0618

    private void Add(string fileName, string json)
    {
        var file = fileName[(fileName.LastIndexOf('/') + 1)..];
        try
        {
            if (file.Equals("package.json", StringComparison.OrdinalIgnoreCase))
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("name", out var n)) PackageName = n.GetString() ?? PackageName;
                if (doc.RootElement.TryGetProperty("version", out var v)) PackageVersion = v.GetString() ?? PackageVersion;
                if (doc.RootElement.TryGetProperty("date", out var d)) PackageDate = d.GetString();
                return;
            }
            if (!(file.StartsWith("StructureDefinition-") || file.StartsWith("ValueSet-") || file.StartsWith("CodeSystem-"))) return;

            switch (Parser.Parse<Resource>(json))
            {
                case StructureDefinition sd when sd.Url is not null && sd.Snapshot?.Element.Count > 0:
                    _profiles[Canonical(sd.Url)] = sd;
                    break;
                case ValueSet vs when vs.Url is not null:
                    _valueSets[Canonical(vs.Url)] = vs;
                    break;
                case CodeSystem cs when cs.Url is not null:
                    _codeSystems[Canonical(cs.Url)] = cs;
                    break;
            }
        }
        catch (Exception ex)
        {
            LoadErrors.Add($"{file}: {ex.Message}");
        }
    }

    /// <summary>Strips a <c>|version</c> suffix from a canonical.</summary>
    public static string Canonical(string url)
    {
        var bar = url.IndexOf('|');
        return bar < 0 ? url : url[..bar];
    }

    public StructureDefinition? FindProfile(string? url)
        => url is not null && _profiles.TryGetValue(Canonical(url), out var sd) ? sd : null;

    /// <summary>
    /// Codes of a value set when it can be enumerated from the package alone (explicit concept lists or complete,
    /// packaged code systems). Returns null when the value set uses filters, external code systems or is unknown.
    /// </summary>
    public ValueSetCodes? Expand(string valueSetUrl)
        => _expansions.GetOrAdd(Canonical(valueSetUrl), url => ExpandCore(url, new HashSet<string>(StringComparer.Ordinal)));

    private ValueSetCodes? ExpandCore(string url, HashSet<string> visiting)
    {
        if (!_valueSets.TryGetValue(url, out var vs) || !visiting.Add(url)) return null;

        var codes = new ValueSetCodes();
        if (vs.Expansion?.Contains.Count > 0)
        {
            AddContains(codes, vs.Expansion.Contains);
            return codes;
        }
        if (vs.Compose is null) return null;

        foreach (var include in vs.Compose.Include)
            if (!AddComponent(codes, include, visiting)) return null;

        foreach (var exclude in vs.Compose.Exclude)
        {
            var excluded = new ValueSetCodes();
            if (!AddComponent(excluded, exclude, visiting)) return null;
            codes.RemoveAll(excluded);
        }
        return codes;
    }

    private static void AddContains(ValueSetCodes codes, IEnumerable<ValueSet.ContainsComponent> contains)
    {
        foreach (var c in contains)
        {
            if (c.Code is not null) codes.Add(c.System, c.Code);
            AddContains(codes, c.Contains);
        }
    }

    private bool AddComponent(ValueSetCodes codes, ValueSet.ConceptSetComponent component, HashSet<string> visiting)
    {
        if (component.Filter.Count > 0) return false;

        ValueSetCodes? fromValueSets = null;
        foreach (var nested in component.ValueSet)
        {
            var expanded = ExpandCore(Canonical(nested), new HashSet<string>(visiting, StringComparer.Ordinal));
            if (expanded is null) return false;
            fromValueSets = fromValueSets is null ? expanded.Clone() : fromValueSets.Intersect(expanded);
        }

        var fromSystem = new ValueSetCodes();
        if (component.Concept.Count > 0)
        {
            foreach (var concept in component.Concept) fromSystem.Add(component.System, concept.Code);
        }
        else if (component.System is not null)
        {
            if (!_codeSystems.TryGetValue(Canonical(component.System), out var cs) || cs.Content != CodeSystemContentMode.Complete) return false;
            AddConcepts(fromSystem, cs.Url, cs.Concept);
        }
        else if (fromValueSets is null)
        {
            return false;
        }

        if (component.System is null) codes.AddAll(fromValueSets!);
        else codes.AddAll(fromValueSets is null ? fromSystem : fromSystem.Intersect(fromValueSets));
        return true;
    }

    private static void AddConcepts(ValueSetCodes codes, string system, IEnumerable<CodeSystem.ConceptDefinitionComponent> concepts)
    {
        foreach (var c in concepts)
        {
            if (c.Code is not null) codes.Add(system, c.Code);
            AddConcepts(codes, system, c.Concept);
        }
    }
}

/// <summary>An enumerated value set: (system, code) pairs plus a code-only index for <c>code</c>-typed elements.</summary>
public sealed class ValueSetCodes
{
    private readonly HashSet<(string System, string Code)> _pairs = new();
    private readonly HashSet<string> _codes = new(StringComparer.Ordinal);

    public int Count => _pairs.Count;
    public IEnumerable<string> Systems => _pairs.Select(p => p.System).Distinct();

    public void Add(string? system, string code)
    {
        _pairs.Add((system ?? string.Empty, code));
        _codes.Add(code);
    }

    public void AddAll(ValueSetCodes other)
    {
        foreach (var p in other._pairs) Add(p.System, p.Code);
    }

    public void RemoveAll(ValueSetCodes other)
    {
        _pairs.ExceptWith(other._pairs);
        _codes.Clear();
        foreach (var p in _pairs) _codes.Add(p.Code);
    }

    public ValueSetCodes Clone()
    {
        var copy = new ValueSetCodes();
        copy.AddAll(this);
        return copy;
    }

    public ValueSetCodes Intersect(ValueSetCodes other)
    {
        var result = new ValueSetCodes();
        foreach (var p in _pairs.Where(other._pairs.Contains)) result.Add(p.System, p.Code);
        return result;
    }

    public bool Contains(string? system, string code)
        => system is null ? _codes.Contains(code) : _pairs.Contains((system, code));

    public bool ContainsCode(string code) => _codes.Contains(code);
}
