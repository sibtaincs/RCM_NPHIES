using System.Collections.Concurrent;
using System.Text.Json;

namespace NPhies_FHIR_Integration.Application.Nphies.Parsing;

/// <summary>An NPHIES validation rule from the "ValidationErrorCodes" workbook.</summary>
public sealed record NphiesRule(string Id, string Type, string? Element, string? Description, string? Display, bool Deactivated);

/// <summary>
/// Lookup of NPHIES error codes: numeric adjudication-error codes (CodeSystem adjudication-error)
/// and rule ids (BV-, DT-, IC-, IB-, RE-, GE-, FR-xxxxx) from the validation-error workbook (IG v2.6).
/// </summary>
public interface INphiesErrorCatalog
{
    /// <summary>Display text of an adjudication-error code (e.g. "1290") or rule id (e.g. "BV-00002").</summary>
    string? Describe(string? code);

    bool TryGetRule(string ruleId, out NphiesRule? rule);

    IReadOnlyDictionary<string, string> AdjudicationErrors { get; }

    IReadOnlyDictionary<string, NphiesRule> Rules { get; }
}

/// <summary>Error catalog backed by the embedded <c>nphies-error-catalog.json</c> resource.</summary>
public sealed class NphiesErrorCatalog : INphiesErrorCatalog
{
    private const string ResourceName = "NPhies.ErrorCatalog.json";
    private static readonly Lazy<NphiesErrorCatalog> Shared = new(Load);

    private NphiesErrorCatalog(IReadOnlyDictionary<string, string> adjudicationErrors, IReadOnlyDictionary<string, NphiesRule> rules)
    {
        AdjudicationErrors = adjudicationErrors;
        Rules = rules;
    }

    public static NphiesErrorCatalog Instance => Shared.Value;

    public IReadOnlyDictionary<string, string> AdjudicationErrors { get; }

    public IReadOnlyDictionary<string, NphiesRule> Rules { get; }

    public string? Describe(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        code = code.Trim();
        if (AdjudicationErrors.TryGetValue(code, out var display)) return display;
        if (Rules.TryGetValue(code.ToUpperInvariant(), out var rule)) return rule.Display ?? rule.Description;
        return null;
    }

    public bool TryGetRule(string ruleId, out NphiesRule? rule)
    {
        rule = null;
        return !string.IsNullOrWhiteSpace(ruleId) && Rules.TryGetValue(ruleId.Trim().ToUpperInvariant(), out rule);
    }

    private static NphiesErrorCatalog Load()
    {
        var assembly = typeof(NphiesErrorCatalog).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in root.GetProperty("adjudicationErrors").EnumerateObject())
            errors[property.Name] = property.Value.GetString() ?? string.Empty;

        var rules = new ConcurrentDictionary<string, NphiesRule>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in root.GetProperty("rules").EnumerateObject())
        {
            var value = property.Value;
            rules[property.Name] = new NphiesRule(
                property.Name,
                GetString(value, "type") ?? string.Empty,
                GetString(value, "element"),
                GetString(value, "description"),
                GetString(value, "display"),
                value.TryGetProperty("deactivated", out var deactivated) && deactivated.ValueKind == JsonValueKind.True);
        }

        return new NphiesErrorCatalog(errors, rules);
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
