using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Infrastructure.Nphies;

/// <summary>
/// Keeps PDFs out of the message audit: attachment <c>data</c> whose SHA-256 matches a stored claim document is
/// replaced by <c>nphies-doc:{documentId}:{sha256}</c>; <see cref="RehydrateAsync"/> restores the exact bytes sent.
/// </summary>
public static class NphiesAuditAttachments
{
    public const string MarkerPrefix = "nphies-doc:";

    /// <summary>Only base64 strings at least this long are considered (small inline values are left alone).</summary>
    private const int MinDataLength = 1024;

    private static readonly Regex DataPattern = new("\"data\"\\s*:\\s*\"([A-Za-z0-9+/]{" + MinDataLength + ",}={0,2})\"", RegexOptions.Compiled);
    private static readonly Regex MarkerPattern = new("\"" + MarkerPrefix + "([^:\"]+):([0-9a-f]{64})\"", RegexOptions.Compiled);

    public static async Task<string> StripAsync(string json, ApplicationDbContext db, CancellationToken ct = default)
    {
        var matches = DataPattern.Matches(json);
        if (matches.Count == 0) return json;

        var hashes = new Dictionary<string, string>();
        foreach (Match m in matches)
        {
            var value = m.Groups[1].Value;
            if (hashes.ContainsKey(value)) continue;
            try { hashes[value] = Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(value))).ToLowerInvariant(); }
            catch (FormatException) { /* not base64 after all */ }
        }
        if (hashes.Count == 0) return json;

        var shaList = hashes.Values.Distinct().ToList();
        var docs = await db.ClaimDocuments.AsNoTracking().Where(d => shaList.Contains(d.Sha256))
            .Select(d => new { d.Id, d.Sha256 }).ToListAsync(ct).ConfigureAwait(false);
        if (docs.Count == 0) return json;
        var bySha = docs.GroupBy(d => d.Sha256).ToDictionary(g => g.Key, g => g.First().Id);

        return DataPattern.Replace(json, m =>
            hashes.TryGetValue(m.Groups[1].Value, out var sha) && bySha.TryGetValue(sha, out var id)
                ? $"\"data\":\"{MarkerPrefix}{id}:{sha}\""
                : m.Value);
    }

    public static bool HasMarkers(string? json) => json != null && json.Contains(MarkerPrefix, StringComparison.Ordinal);

    /// <summary>Replaces markers with the stored content (base64). Unresolvable markers are left as they are.</summary>
    public static async Task<string?> RehydrateAsync(string? json, Func<string, CancellationToken, Task<byte[]?>> loadContent, CancellationToken ct = default)
    {
        if (!HasMarkers(json)) return json;
        var values = new Dictionary<string, string>();
        foreach (Match m in MarkerPattern.Matches(json!))
        {
            if (values.ContainsKey(m.Value)) continue;
            var data = await loadContent(m.Groups[1].Value, ct).ConfigureAwait(false);
            if (data is null) continue;
            var sha = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
            if (sha == m.Groups[2].Value) values[m.Value] = "\"" + Convert.ToBase64String(data) + "\"";
        }
        return values.Count == 0 ? json : MarkerPattern.Replace(json!, m => values.TryGetValue(m.Value, out var v) ? v : m.Value);
    }
}
