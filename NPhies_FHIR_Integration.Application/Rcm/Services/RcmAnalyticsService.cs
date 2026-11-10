using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

/// <summary>RCM KPIs computed from real claims, adjudications, denials and payments.</summary>
public interface IRcmAnalyticsService
{
    Task<RcmDashboardDto> GetDashboardAsync(RcmQuery query, CancellationToken ct = default);
    Task<List<AgingBucketDto>> GetAgingAsync(RcmQuery query, CancellationToken ct = default);
    Task<List<DenialRateDto>> GetDenialRateAsync(RcmQuery query, string groupBy = "month", CancellationToken ct = default);
    Task<List<PayerBreakdownDto>> GetPayerBreakdownAsync(RcmQuery query, CancellationToken ct = default);
}

public sealed class RcmAnalyticsService : IRcmAnalyticsService
{
    private readonly ApplicationDbContext _db;

    public RcmAnalyticsService(ApplicationDbContext db) => _db = db;

    private sealed record ClaimRow(string Id, string Use, string InsurerId, string NphiesStatus, decimal Total, decimal? Approved, decimal? PatientShare,
        decimal? Paid, DateTime Created, DateTime? Submitted, DateTime? Adjudicated, DateTime? PaidAt);

    private async Task<List<ClaimRow>> LoadClaimsAsync(RcmQuery query, CancellationToken ct)
    {
        var q = _db.Claims.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(c => c.ProviderId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) q = q.Where(c => c.InsurerId == query.InsurerId);
        if (!string.IsNullOrWhiteSpace(query.PatientId)) q = q.Where(c => c.PatientId == query.PatientId);
        if (query.From.HasValue) q = q.Where(c => (c.SubmittedAt ?? c.CreatedDate) >= query.From);
        if (query.To.HasValue) q = q.Where(c => (c.SubmittedAt ?? c.CreatedDate) <= query.To);
        var rows = await q.Select(c => new { c.Id, c.Use, c.InsurerId, c.NphiesStatus, c.Total, c.ApprovedAmount, c.PatientShareAmount, c.PaidAmount, c.CreatedDate, c.SubmittedAt, c.AdjudicatedAt, c.PaidAt })
            .ToListAsync(ct);
        return rows.Select(c => new ClaimRow(c.Id, c.Use, c.InsurerId, c.NphiesStatus ?? RcmClaimStatus.Draft, c.Total, c.ApprovedAmount, c.PatientShareAmount, c.PaidAmount,
            c.CreatedDate, c.SubmittedAt, c.AdjudicatedAt, c.PaidAt)).ToList();
    }

    private static bool IsPreAuth(ClaimRow c) => c.Use.Contains("preauth", StringComparison.OrdinalIgnoreCase);
    private static bool IsAdjudicated(ClaimRow c) => c.Adjudicated.HasValue && c.NphiesStatus is not (RcmClaimStatus.Cancelled or RcmClaimStatus.Nullified);
    private static bool IsLive(ClaimRow c) => c.Submitted.HasValue && c.NphiesStatus is not (RcmClaimStatus.Cancelled or RcmClaimStatus.Nullified or RcmClaimStatus.Superseded);
    // Approved = insurer benefit total, so the receivable from the payer is benefit minus what was paid.
    private static decimal Outstanding(ClaimRow c) => Math.Max(0m, (c.Approved ?? 0m) - (c.Paid ?? 0m));
    private static decimal Rate(decimal part, decimal whole) => whole == 0 ? 0 : Math.Round(part / whole * 100m, 2);

    public async Task<RcmDashboardDto> GetDashboardAsync(RcmQuery query, CancellationToken ct = default)
    {
        var all = await LoadClaimsAsync(query, ct);
        var claims = all.Where(c => !IsPreAuth(c) && IsLive(c)).ToList();
        var preauths = all.Where(c => IsPreAuth(c) && c.Submitted.HasValue).ToList();
        var adjudicated = claims.Where(IsAdjudicated).ToList();
        var rejected = adjudicated.Count(c => c.NphiesStatus == RcmClaimStatus.Rejected);
        var partial = adjudicated.Count(c => c.NphiesStatus == RcmClaimStatus.PartiallyApproved || (c.NphiesStatus == RcmClaimStatus.PartiallyPaid && (c.Approved ?? 0) < c.Total));
        var claimIds = claims.Select(c => c.Id).ToList();

        var denialsQ = _db.ClaimDenials.AsNoTracking().Where(d => claimIds.Contains(d.ClaimId));
        var deniedAmount = claimIds.Count == 0 ? 0m : await denialsQ.SumAsync(d => (decimal?)d.DeniedAmount, ct) ?? 0m;
        var openDenials = claimIds.Count == 0 ? 0 : await denialsQ.CountAsync(d => d.Status == "Open", ct);
        var claimsWithDenials = claimIds.Count == 0 ? new List<string>() : await denialsQ.Select(d => d.ClaimId).Distinct().ToListAsync(ct);

        var paymentsQ = _db.PaymentReconciliations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) paymentsQ = paymentsQ.Where(p => p.RequestorId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) paymentsQ = paymentsQ.Where(p => p.PaymentIssuerId == query.InsurerId);
        var commQ = _db.CommunicationRequests.AsNoTracking().Where(r => r.RespondedCommunicationId == null && r.Status == "active");
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) commQ = commQ.Where(r => r.RecipientId == query.ProviderId);
        var eligQ = _db.EligibilityChecks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) eligQ = eligQ.Where(e => e.ProviderId == query.ProviderId);
        if (query.From.HasValue) eligQ = eligQ.Where(e => e.CreatedAt >= query.From);
        if (query.To.HasValue) eligQ = eligQ.Where(e => e.CreatedAt <= query.To);
        var eligibility = await eligQ.Where(e => e.Status == RcmTransactionStatus.Completed).Select(e => e.IsEligible).ToListAsync(ct);

        var billed = claims.Sum(c => c.Total);
        var approved = adjudicated.Sum(c => c.Approved ?? 0m);
        var paid = claims.Sum(c => c.Paid ?? 0m);
        var toAdj = adjudicated.Where(c => c.Submitted.HasValue).Select(c => (c.Adjudicated!.Value - c.Submitted!.Value).TotalDays).ToList();
        var toPay = claims.Where(c => c.PaidAt.HasValue && c.Submitted.HasValue).Select(c => (c.PaidAt!.Value - c.Submitted!.Value).TotalDays).ToList();

        return new RcmDashboardDto
        {
            From = query.From ?? all.Select(c => (DateTime?)(c.Submitted ?? c.Created)).Min() ?? DateTime.UtcNow.Date,
            To = query.To ?? DateTime.UtcNow,
            ClaimsSubmitted = claims.Count,
            ClaimsAdjudicated = adjudicated.Count,
            ClaimsPending = claims.Count(c => RcmClaimStatus.AwaitingPayer.Contains(c.NphiesStatus)),
            ClaimsApproved = adjudicated.Count(c => c.NphiesStatus is RcmClaimStatus.Approved or RcmClaimStatus.Paid),
            ClaimsPartiallyApproved = partial,
            ClaimsRejected = rejected,
            ClaimsErrored = claims.Count(c => c.NphiesStatus is RcmClaimStatus.Error or RcmClaimStatus.ValidationFailed),
            PreAuthsSubmitted = preauths.Count,
            PreAuthsApproved = preauths.Count(c => c.NphiesStatus is RcmClaimStatus.Approved or RcmClaimStatus.PartiallyApproved),
            BilledAmount = billed,
            ApprovedAmount = approved,
            PatientShareAmount = adjudicated.Sum(c => c.PatientShare ?? 0m),
            PaidAmount = paid,
            OutstandingAmount = adjudicated.Sum(Outstanding),
            DeniedAmount = deniedAmount,
            DenialRate = Rate(rejected + partial, adjudicated.Count),
            CleanClaimRate = Rate(adjudicated.Count(c => !claimsWithDenials.Contains(c.Id)), adjudicated.Count),
            CollectionRate = Rate(paid, approved),
            AverageDaysToAdjudication = toAdj.Count == 0 ? null : Math.Round(toAdj.Average(), 1),
            AverageDaysToPayment = toPay.Count == 0 ? null : Math.Round(toPay.Average(), 1),
            OpenDenials = openDenials,
            OpenCommunicationRequests = await commQ.CountAsync(ct),
            UnacknowledgedPayments = await paymentsQ.CountAsync(p => p.AcknowledgementStatus != null && p.AcknowledgementStatus != "Acknowledged", ct),
            PaymentDiscrepancies = await paymentsQ.CountAsync(p => p.MatchStatus != "Matched", ct),
            EligibilityChecks = eligibility.Count,
            EligibilityRate = Rate(eligibility.Count(e => e == true), eligibility.Count),
        };
    }

    public async Task<List<AgingBucketDto>> GetAgingAsync(RcmQuery query, CancellationToken ct = default)
    {
        var claims = (await LoadClaimsAsync(query, ct)).Where(c => !IsPreAuth(c) && IsLive(c)).ToList();
        var today = DateTime.UtcNow.Date;
        var buckets = new List<AgingBucketDto>
        {
            new() { Bucket = "0-30", MinDays = 0, MaxDays = 30 },
            new() { Bucket = "31-60", MinDays = 31, MaxDays = 60 },
            new() { Bucket = "61-90", MinDays = 61, MaxDays = 90 },
            new() { Bucket = "91-120", MinDays = 91, MaxDays = 120 },
            new() { Bucket = "120+", MinDays = 121, MaxDays = null },
        };
        foreach (var c in claims)
        {
            // Open A/R: awaiting the payer, or adjudicated with a balance still unpaid.
            decimal open;
            if (RcmClaimStatus.AwaitingPayer.Contains(c.NphiesStatus) || c.NphiesStatus == RcmClaimStatus.Timeout) open = c.Total - (c.Paid ?? 0m);
            else if (IsAdjudicated(c)) open = Outstanding(c);
            else continue;
            if (open <= 0) continue;
            var days = (int)(today - c.Submitted!.Value.Date).TotalDays;
            var bucket = buckets.First(b => days >= b.MinDays && (b.MaxDays is null || days <= b.MaxDays));
            bucket.Count++;
            bucket.Amount += open;
        }
        return buckets;
    }

    public async Task<List<DenialRateDto>> GetDenialRateAsync(RcmQuery query, string groupBy = "month", CancellationToken ct = default)
    {
        var claims = (await LoadClaimsAsync(query, ct)).Where(c => !IsPreAuth(c) && IsAdjudicated(c) && c.NphiesStatus != RcmClaimStatus.Superseded).ToList();
        var ids = claims.Select(c => c.Id).ToList();
        var denied = ids.Count == 0 ? new Dictionary<string, decimal>()
            : await _db.ClaimDenials.AsNoTracking().Where(d => ids.Contains(d.ClaimId)).GroupBy(d => d.ClaimId)
                .Select(g => new { g.Key, Amount = g.Sum(d => d.DeniedAmount) }).ToDictionaryAsync(x => x.Key, x => x.Amount, ct);

        string Key(ClaimRow c)
        {
            var d = c.Adjudicated!.Value;
            return groupBy.ToLowerInvariant() switch
            {
                "day" => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "week" => $"{ISOWeek.GetYear(d)}-W{ISOWeek.GetWeekOfYear(d):00}",
                "payer" => c.InsurerId,
                _ => d.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            };
        }

        return claims.GroupBy(Key).OrderBy(g => g.Key).Select(g =>
        {
            var billed = g.Sum(c => c.Total);
            var deniedAmt = g.Sum(c => denied.TryGetValue(c.Id, out var a) ? a : 0m);
            var rejected = g.Count(c => c.NphiesStatus == RcmClaimStatus.Rejected);
            var partial = g.Count(c => c.NphiesStatus != RcmClaimStatus.Rejected && denied.ContainsKey(c.Id));
            return new DenialRateDto
            {
                Period = g.Key,
                Adjudicated = g.Count(),
                Denied = rejected,
                PartiallyDenied = partial,
                BilledAmount = billed,
                DeniedAmount = deniedAmt,
                DenialRate = Rate(rejected + partial, g.Count()),
                AmountDenialRate = Rate(deniedAmt, billed),
            };
        }).ToList();
    }

    public async Task<List<PayerBreakdownDto>> GetPayerBreakdownAsync(RcmQuery query, CancellationToken ct = default)
    {
        var claims = (await LoadClaimsAsync(query, ct)).Where(c => !IsPreAuth(c) && IsLive(c)).ToList();
        var insurerIds = claims.Select(c => c.InsurerId).Distinct().ToList();
        var names = await _db.Organizations.AsNoTracking().Where(o => insurerIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.OrganizationName, ct);
        return claims.GroupBy(c => c.InsurerId).Select(g =>
        {
            var adjudicated = g.Where(IsAdjudicated).ToList();
            var rejected = adjudicated.Count(c => c.NphiesStatus == RcmClaimStatus.Rejected);
            var partial = adjudicated.Count(c => c.NphiesStatus == RcmClaimStatus.PartiallyApproved);
            var toPay = g.Where(c => c.PaidAt.HasValue && c.Submitted.HasValue).Select(c => (c.PaidAt!.Value - c.Submitted!.Value).TotalDays).ToList();
            return new PayerBreakdownDto
            {
                InsurerId = g.Key,
                InsurerName = names.TryGetValue(g.Key, out var n) ? n : null,
                Claims = g.Count(),
                Approved = adjudicated.Count(c => c.NphiesStatus is RcmClaimStatus.Approved or RcmClaimStatus.Paid or RcmClaimStatus.PartiallyPaid),
                Rejected = rejected,
                Pending = g.Count(c => RcmClaimStatus.AwaitingPayer.Contains(c.NphiesStatus)),
                BilledAmount = g.Sum(c => c.Total),
                ApprovedAmount = adjudicated.Sum(c => c.Approved ?? 0m),
                PaidAmount = g.Sum(c => c.Paid ?? 0m),
                OutstandingAmount = adjudicated.Sum(Outstanding),
                DenialRate = Rate(rejected + partial, adjudicated.Count),
                AverageDaysToPayment = toPay.Count == 0 ? null : Math.Round(toPay.Average(), 1),
            };
        }).OrderByDescending(p => p.BilledAmount).ToList();
    }
}
