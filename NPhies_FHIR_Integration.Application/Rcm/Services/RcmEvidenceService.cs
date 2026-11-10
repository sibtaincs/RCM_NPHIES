using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

/// <summary>
/// Builds NPHIES test-case evidence packs: per claim / prior-auth, a zip with every request and response bundle exchanged
/// with NPHIES (from the message audit), the claim timeline and a human-readable summary for the certification team.
/// </summary>
public interface IRcmEvidenceService
{
    Task<RcmEvidenceFile> BuildAsync(IReadOnlyCollection<string> claimIds, string? providerScope, CancellationToken ct = default);
}

public sealed record RcmEvidenceFile(string FileName, byte[] Content);

public sealed class RcmEvidenceService : IRcmEvidenceService
{
    public const int MaxClaims = 50;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly ApplicationDbContext _db;
    private readonly IRcmClaimWorkflowService _claims;

    public RcmEvidenceService(ApplicationDbContext db, IRcmClaimWorkflowService claims)
    {
        _db = db;
        _claims = claims;
    }

    public async Task<RcmEvidenceFile> BuildAsync(IReadOnlyCollection<string> claimIds, string? providerScope, CancellationToken ct = default)
    {
        var ids = claimIds.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
        if (ids.Count == 0) throw RcmWorkflowException.Invalid("At least one claim id is required.");
        if (ids.Count > MaxClaims) throw RcmWorkflowException.Invalid($"At most {MaxClaims} claims can be exported at once.");

        var claims = await _db.Claims.AsNoTracking().Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.ClaimNumber, c.ProviderId }).ToListAsync(ct);
        foreach (var id in ids)
        {
            var c = claims.FirstOrDefault(x => x.Id == id);
            if (c is null || (providerScope != null && c.ProviderId != providerScope)) throw RcmWorkflowException.NotFound("Claim", id);
        }

        using var buffer = new MemoryStream();
        var index = new StringBuilder();
        index.AppendLine("# NPHIES test-case evidence");
        index.AppendLine($"Generated (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        index.AppendLine();

        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var id in ids)
            {
                var timeline = (await _claims.GetTimelineAsync(id, ct))!;
                var folder = Safe(timeline.Claim.ClaimNumber is { Length: > 0 } n ? n : id);
                Write(zip, $"{folder}/timeline.json", JsonSerializer.Serialize(timeline, Json));

                var summary = new StringBuilder();
                var c = timeline.Claim;
                summary.AppendLine($"## {c.ClaimNumber} ({c.Use} / {c.ClaimType}{(c.ClaimSubType is null ? "" : "/" + c.ClaimSubType)})");
                summary.AppendLine($"- Identifier: {c.Identifier}");
                summary.AppendLine($"- Patient: {c.PatientName}  Insurer: {c.InsurerName}");
                summary.AppendLine($"- Status: {c.Status} / NPHIES: {c.NphiesStatus}  Outcome: {c.Outcome} {c.AdjudicationOutcome}");
                summary.AppendLine($"- Total: {c.Total}  Approved: {c.ApprovedAmount}");
                summary.AppendLine();
                summary.AppendLine("| # | Time (UTC) | Direction | Event | Response | Outcome | Files |");
                summary.AppendLine("|---|---|---|---|---|---|---|");

                var seq = 0;
                foreach (var tx in timeline.Transactions)
                {
                    seq++;
                    var json = await _claims.GetTransactionJsonAsync(tx.Id, providerScope, ct, includeAttachments: true);
                    var prefix = $"{folder}/{seq:00}-{Safe(tx.Direction)}-{Safe(tx.EventCode)}";
                    var files = new List<string>();
                    if (!string.IsNullOrEmpty(json?.RequestJson)) { Write(zip, $"{prefix}-request.json", json.RequestJson); files.Add("request"); }
                    if (!string.IsNullOrEmpty(json?.ResponseJson)) { Write(zip, $"{prefix}-response.json", json.ResponseJson); files.Add("response"); }
                    summary.AppendLine($"| {seq} | {tx.SentAt:yyyy-MM-dd HH:mm:ss} | {tx.Direction} | {tx.EventCode} | {tx.ResponseCode} | {tx.Outcome}{(tx.ErrorSummary is null ? "" : " - " + tx.ErrorSummary.Replace("|", "/"))} | {(files.Count == 0 ? "not audited" : string.Join(", ", files))} |");
                }

                if (timeline.Denials.Count > 0)
                {
                    summary.AppendLine();
                    summary.AppendLine("Denials: " + string.Join("; ", timeline.Denials.Select(d => $"item {d.ItemSequence}: {d.ReasonCode} {d.ReasonDisplay}")));
                }
                if (timeline.Payments.Count > 0)
                {
                    summary.AppendLine();
                    summary.AppendLine("Payments: " + string.Join("; ", timeline.Payments.Select(p => $"{p.Date:yyyy-MM-dd} {p.Amount} ({p.MatchStatus})")));
                }

                Write(zip, $"{folder}/summary.md", summary.ToString());
                index.Append(summary).AppendLine();
            }

            Write(zip, "README.md", index.ToString());
        }

        var name = ids.Count == 1 ? $"nphies-evidence-{Safe(claims[0].ClaimNumber)}.zip" : $"nphies-evidence-{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
        return new RcmEvidenceFile(name, buffer.ToArray());
    }

    private static void Write(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var w = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        w.Write(content);
    }

    private static string Safe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "x";
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) || ch == '/' ? '_' : ch).ToArray());
    }
}
