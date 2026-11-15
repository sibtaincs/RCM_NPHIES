using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

/// <summary>
/// Finds claims and prior authorizations that were sent but have had no final response for too long. They are
/// flagged after <c>Rcm:StuckAfterDays</c> and escalated after <c>Rcm:StuckEscalateAfterDays</c>: the Claim Submission
/// Guideline says to contact NPHIES support with the bundle ids when no final response arrives within 30 days.
/// </summary>
public interface IRcmStuckRequestService
{
    /// <summary>Flags, escalates and resolves stuck requests. Safe to run as often as wanted.</summary>
    Task<StuckScanResultDto> ScanAsync(CancellationToken ct = default);

    /// <summary>Lists stuck requests; open ones unless <c>query.Status</c> names a level (Flagged, Escalated, Resolved, All).</summary>
    Task<RcmPagedResult<StuckRequestDto>> ListAsync(RcmQuery query, CancellationToken ct = default);
}

public static class RcmStuckLevel
{
    public const string Flagged = "Flagged";
    public const string Escalated = "Escalated";
    public const string Resolved = "Resolved";
}

public sealed class RcmStuckRequestService : IRcmStuckRequestService
{
    private static readonly string[] RequestOperations = { RcmOperations.Claim, RcmOperations.PriorAuth };

    private readonly ApplicationDbContext _db;
    private readonly RcmOptions _options;
    private readonly ILogger<RcmStuckRequestService> _logger;

    public RcmStuckRequestService(ApplicationDbContext db, IOptions<RcmOptions> options, ILogger<RcmStuckRequestService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<StuckScanResultDto> ScanAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var flagAfter = Math.Max(1, _options.StuckAfterDays);
        var escalateAfter = Math.Max(flagAfter, _options.StuckEscalateAfterDays);
        var flagBefore = now.AddDays(-flagAfter);
        var result = new StuckScanResultDto();

        var waiting = await _db.Claims.AsNoTracking()
            .Where(c => c.SubmittedAt != null && c.SubmittedAt < flagBefore
                        && c.NphiesStatus != null && RcmClaimStatus.AwaitingFinalResponse.Contains(c.NphiesStatus))
            .Select(c => new { c.Id, c.ProviderId, c.InsurerId, c.Use, c.NphiesStatus, c.SubmittedAt, c.ClaimIdentifierValue, c.LastMessageId })
            .ToListAsync(ct);
        var waitingById = waiting.ToDictionary(c => c.Id);

        var open = await _db.RcmStuckRequests.Where(s => s.Level != RcmStuckLevel.Resolved).ToListAsync(ct);
        foreach (var row in open.Where(s => !waitingById.ContainsKey(s.ClaimId)))
        {
            // The final response arrived, or the request was cancelled or replaced.
            row.Level = RcmStuckLevel.Resolved;
            row.ResolvedAt = now;
            row.UpdatedAt = now;
            result.Resolved++;
        }

        var openByClaim = open.Where(s => s.Level != RcmStuckLevel.Resolved)
            .GroupBy(s => s.ClaimId).ToDictionary(g => g.Key, g => g.First());
        var newIds = waiting.Select(c => c.Id).Where(id => !openByClaim.ContainsKey(id)).ToList();
        var transactions = newIds.Count == 0
            ? new Dictionary<string, NphiesTransaction>()
            : (await _db.NphiesTransactions.AsNoTracking()
                .Where(t => t.Direction == "outbound" && t.EntityType == RcmEntityTypes.Claim && t.EntityId != null
                            && newIds.Contains(t.EntityId) && RequestOperations.Contains(t.Operation))
                .ToListAsync(ct))
              .GroupBy(t => t.EntityId!)
              .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.SentAt).First());

        foreach (var claim in waiting)
        {
            if (!openByClaim.TryGetValue(claim.Id, out var row))
            {
                transactions.TryGetValue(claim.Id, out var tx);
                row = new RcmStuckRequest
                {
                    ClaimId = claim.Id,
                    ProviderId = claim.ProviderId,
                    InsurerId = claim.InsurerId,
                    Use = claim.Use,
                    WaitingSince = claim.SubmittedAt!.Value,
                    TransactionId = tx?.Id,
                    BundleId = tx?.BundleId,
                    MessageId = tx?.MessageId ?? claim.LastMessageId,
                    RequestIdentifier = claim.ClaimIdentifierValue,
                    HeldByNphies = tx?.NphiesGenerated ?? false,
                    Level = RcmStuckLevel.Flagged,
                    FlaggedAt = now,
                };
                _db.RcmStuckRequests.Add(row);
                result.Flagged++;
            }
            row.ClaimStatus = claim.NphiesStatus!;

            if (row.Level != RcmStuckLevel.Escalated && (now - row.WaitingSince).TotalDays >= escalateAfter)
            {
                row.Level = RcmStuckLevel.Escalated;
                row.EscalatedAt = now;
                row.UpdatedAt = now;
                result.Escalated++;
                _logger.LogWarning(
                    "NPHIES request for claim {ClaimId} ({Identifier}) has had no final response for {Days} days. Contact NPHIES support with Bundle.id {BundleId} and MessageHeader.id {MessageId}.",
                    row.ClaimId, row.RequestIdentifier, (int)(now - row.WaitingSince).TotalDays, row.BundleId, row.MessageId);
            }
        }

        await _db.SaveChangesAsync(ct);
        result.Open = waiting.Count;
        return result;
    }

    public async Task<RcmPagedResult<StuckRequestDto>> ListAsync(RcmQuery query, CancellationToken ct = default)
    {
        var rows = _db.RcmStuckRequests.AsNoTracking().AsQueryable();
        if (string.IsNullOrWhiteSpace(query.Status))
            rows = rows.Where(s => s.Level != RcmStuckLevel.Resolved);
        else if (!string.Equals(query.Status, "All", StringComparison.OrdinalIgnoreCase))
            rows = rows.Where(s => s.Level == query.Status);
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) rows = rows.Where(s => s.ProviderId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) rows = rows.Where(s => s.InsurerId == query.InsurerId);
        if (query.From.HasValue) rows = rows.Where(s => s.WaitingSince >= query.From);
        if (query.To.HasValue) rows = rows.Where(s => s.WaitingSince <= query.To);

        var total = await rows.CountAsync(ct);
        var page = await rows.OrderBy(s => s.WaitingSince).Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        var claimIds = page.Select(s => s.ClaimId).ToList();
        var numbers = await _db.Claims.AsNoTracking().Where(c => claimIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.ClaimNumber, ct);

        var now = DateTime.UtcNow;
        return new RcmPagedResult<StuckRequestDto>
        {
            Total = total,
            Page = Math.Max(1, query.Page),
            PageSize = query.Take,
            Items = page.Select(s => new StuckRequestDto
            {
                Id = s.Id,
                ClaimId = s.ClaimId,
                ClaimNumber = numbers.GetValueOrDefault(s.ClaimId),
                ProviderId = s.ProviderId,
                InsurerId = s.InsurerId,
                Use = s.Use,
                ClaimStatus = s.ClaimStatus,
                WaitingSince = s.WaitingSince,
                DaysWaiting = (int)((s.ResolvedAt ?? now) - s.WaitingSince).TotalDays,
                TransactionId = s.TransactionId,
                BundleId = s.BundleId,
                MessageId = s.MessageId,
                RequestIdentifier = s.RequestIdentifier,
                HeldByNphies = s.HeldByNphies,
                Level = s.Level,
                FlaggedAt = s.FlaggedAt,
                EscalatedAt = s.EscalatedAt,
                ResolvedAt = s.ResolvedAt,
                Guidance = Guidance(s),
            }).ToList(),
        };
    }

    private static string? Guidance(RcmStuckRequest s) => s.Level switch
    {
        RcmStuckLevel.Escalated => "No final response within the NPHIES limit. Contact NPHIES support and quote the bundle id and message id.",
        RcmStuckLevel.Flagged when s.ClaimStatus == RcmClaimStatus.Timeout =>
            "The request timed out. Resend the identical message, or run a status-check. Never send it again with new ids.",
        RcmStuckLevel.Flagged => s.HeldByNphies
            ? "NPHIES is holding this request for the payer. Poll for the response."
            : "The payer has not answered yet. Run a status-check or poll for this claim.",
        _ => null,
    };
}
