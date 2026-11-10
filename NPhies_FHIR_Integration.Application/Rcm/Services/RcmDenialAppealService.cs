using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

public static class RcmDenialStatus
{
    public const string Open = "Open";
    public const string Resubmitted = "Resubmitted";
    public const string Appealed = "Appealed";
    public const string Accepted = "Accepted";
    public const string WrittenOff = "WrittenOff";
    public const string Overturned = "Overturned";
    public const string Upheld = "Upheld";

    /// <summary>Statuses a user may set manually.</summary>
    public static readonly string[] Manual = { Open, Accepted, WrittenOff };
}

/// <summary>Denials derived from real adjudication data (ClaimResponse item reasons) and their work queue.</summary>
public interface IRcmDenialService
{
    Task<RcmPagedResult<DenialDto>> ListAsync(DenialQuery query, CancellationToken ct = default);
    Task<DenialDto?> GetAsync(string id, CancellationToken ct = default);
    Task<DenialSummaryDto> GetSummaryAsync(RcmQuery query, CancellationToken ct = default);
    Task<DenialDto> UpdateAsync(string id, UpdateDenialDto request, CancellationToken ct = default);
}

public sealed class RcmDenialService : IRcmDenialService
{
    private readonly ApplicationDbContext _db;

    public RcmDenialService(ApplicationDbContext db) => _db = db;

    private IQueryable<ClaimDenial> Filter(RcmQuery query)
    {
        var q = _db.ClaimDenials.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(d => d.ProviderId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) q = q.Where(d => d.InsurerId == query.InsurerId);
        if (!string.IsNullOrWhiteSpace(query.PatientId)) q = q.Where(d => d.Claim!.PatientId == query.PatientId);
        if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(d => d.Status == query.Status);
        if (query.From.HasValue) q = q.Where(d => d.DenialDate >= query.From);
        if (query.To.HasValue) q = q.Where(d => d.DenialDate <= query.To);
        if (query is DenialQuery dq)
        {
            if (!string.IsNullOrWhiteSpace(dq.Category)) q = q.Where(d => d.Category == dq.Category);
            if (!string.IsNullOrWhiteSpace(dq.ReasonCode)) q = q.Where(d => d.ReasonCode == dq.ReasonCode);
            if (!string.IsNullOrWhiteSpace(dq.ClaimId)) q = q.Where(d => d.ClaimId == dq.ClaimId);
        }
        return q;
    }

    public async Task<RcmPagedResult<DenialDto>> ListAsync(DenialQuery query, CancellationToken ct = default)
    {
        var q = Filter(query);
        var total = await q.CountAsync(ct);
        var rows = await q.Include(d => d.Claim).OrderBy(d => d.Status == RcmDenialStatus.Open ? 0 : 1).ThenBy(d => d.AppealDeadline)
            .Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        return new RcmPagedResult<DenialDto> { Total = total, Page = Math.Max(1, query.Page), PageSize = query.Take, Items = await ProjectAsync(rows, ct) };
    }

    public async Task<DenialDto?> GetAsync(string id, CancellationToken ct = default)
    {
        var d = await _db.ClaimDenials.AsNoTracking().Include(x => x.Claim).FirstOrDefaultAsync(x => x.Id == id, ct);
        return d is null ? null : (await ProjectAsync(new List<ClaimDenial> { d }, ct))[0];
    }

    private async Task<List<DenialDto>> ProjectAsync(List<ClaimDenial> rows, CancellationToken ct)
    {
        var claimIds = rows.Select(r => r.ClaimId).Distinct().ToList();
        var items = await _db.ClaimItems.AsNoTracking().Where(i => claimIds.Contains(i.ClaimId))
            .Select(i => new { i.ClaimId, i.Sequence, i.ProductOrServiceCode }).ToListAsync(ct);
        var insurerIds = rows.Where(r => r.InsurerId != null).Select(r => r.InsurerId!).Distinct().ToList();
        var insurers = await _db.Organizations.AsNoTracking().Where(o => insurerIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.OrganizationName, ct);
        return rows.Select(d => RcmProjections.ToDto(d,
            itemCode: items.FirstOrDefault(i => i.ClaimId == d.ClaimId && i.Sequence == d.ItemSequence)?.ProductOrServiceCode,
            insurerName: d.InsurerId != null && insurers.TryGetValue(d.InsurerId, out var n) ? n : null)).ToList();
    }

    public async Task<DenialSummaryDto> GetSummaryAsync(RcmQuery query, CancellationToken ct = default)
    {
        var rows = await Filter(query).Select(d => new { d.Status, d.Category, d.ReasonCode, d.ReasonDisplay, d.InsurerId, d.DeniedAmount, d.AppealDeadline }).ToListAsync(ct);
        var soon = DateTime.UtcNow.Date.AddDays(7);
        var insurerIds = rows.Where(r => r.InsurerId != null).Select(r => r.InsurerId!).Distinct().ToList();
        var insurers = await _db.Organizations.AsNoTracking().Where(o => insurerIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.OrganizationName, ct);

        static List<BreakdownDto> Group<T>(IEnumerable<T> src, Func<T, string> key, Func<T, decimal> amount, Func<string, string?>? label = null) =>
            src.GroupBy(key).Select(g => new BreakdownDto { Key = g.Key, Label = label?.Invoke(g.Key), Count = g.Count(), Amount = g.Sum(amount) })
               .OrderByDescending(b => b.Amount).ToList();

        var open = rows.Where(r => r.Status == RcmDenialStatus.Open).ToList();
        return new DenialSummaryDto
        {
            TotalDenials = rows.Count,
            OpenDenials = open.Count,
            TotalDeniedAmount = rows.Sum(r => r.DeniedAmount),
            OpenDeniedAmount = open.Sum(r => r.DeniedAmount),
            RecoveredAmount = rows.Where(r => r.Status == RcmDenialStatus.Overturned).Sum(r => r.DeniedAmount),
            ExpiringIn7Days = open.Count(r => r.AppealDeadline <= soon),
            ByCategory = Group(rows, r => r.Category, r => r.DeniedAmount),
            ByReason = Group(rows, r => r.ReasonCode, r => r.DeniedAmount, k => rows.FirstOrDefault(r => r.ReasonCode == k && r.ReasonDisplay != null)?.ReasonDisplay).Take(25).ToList(),
            ByPayer = Group(rows, r => r.InsurerId ?? "unknown", r => r.DeniedAmount, k => insurers.TryGetValue(k, out var n) ? n : null),
            ByStatus = Group(rows, r => r.Status, r => r.DeniedAmount),
        };
    }

    public async Task<DenialDto> UpdateAsync(string id, UpdateDenialDto request, CancellationToken ct = default)
    {
        var d = await _db.ClaimDenials.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw RcmWorkflowException.NotFound("Denial", id);
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != d.Status)
        {
            if (!RcmDenialStatus.Manual.Contains(request.Status))
                throw RcmWorkflowException.Invalid($"Status can only be set to {string.Join(", ", RcmDenialStatus.Manual)}; appeals and resubmissions set the others.");
            if (d.Status is not (RcmDenialStatus.Open or RcmDenialStatus.Accepted or RcmDenialStatus.WrittenOff))
                throw RcmWorkflowException.Conflict($"Denial is {d.Status} and is managed by its appeal/resubmission.");
            d.Status = request.Status;
        }
        if (request.Notes != null) d.Notes = request.Notes;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct))!;
    }
}

/// <summary>
/// Appeals. NPHIES has no dedicated appeal message: an appeal is tracked as an <see cref="AppealRequest"/>, sent as a
/// resubmission (related = prior) of the denied claim, with justification/documents as a Communication about it.
/// The outcome is synced when the resubmission is adjudicated (see RcmResponseIngestor).
/// </summary>
public interface IRcmAppealService
{
    Task<AppealDto> CreateAsync(CreateAppealDto request, CancellationToken ct = default);
    Task<AppealDto?> GetAsync(string id, CancellationToken ct = default);
    Task<RcmPagedResult<AppealDto>> ListAsync(RcmQuery query, CancellationToken ct = default);
    Task<AppealDto> WithdrawAsync(string id, string? reason, CancellationToken ct = default);
}

public sealed class RcmAppealService : IRcmAppealService
{
    private readonly ApplicationDbContext _db;
    private readonly IRcmClaimWorkflowService _claims;
    private readonly IRcmCommunicationService _communications;
    private readonly IRcmInputMapper _mapper;
    private readonly RcmOptions _options;

    public RcmAppealService(ApplicationDbContext db, IRcmClaimWorkflowService claims, IRcmCommunicationService communications, IRcmInputMapper mapper, IOptions<RcmOptions> options)
    {
        _db = db;
        _claims = claims;
        _communications = communications;
        _mapper = mapper;
        _options = options.Value;
    }

    public async Task<AppealDto> CreateAsync(CreateAppealDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw RcmWorkflowException.Invalid("An appeal reason is required.");
        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.Id == request.ClaimId, ct) ?? throw RcmWorkflowException.NotFound("Claim", request.ClaimId);
        if (RcmInputMapper.IsPreAuth(claim)) throw RcmWorkflowException.Invalid("Appeal prior-authorization denials by sending an update.");

        var denialsQuery = _db.ClaimDenials.Where(d => d.ClaimId == claim.Id && d.Status == RcmDenialStatus.Open);
        if (request.DenialIds.Count > 0) denialsQuery = denialsQuery.Where(d => request.DenialIds.Contains(d.Id));
        var denials = await denialsQuery.ToListAsync(ct);
        if (denials.Count == 0) throw RcmWorkflowException.Conflict("There are no open denials on this claim to appeal.");
        if (request.DenialIds.Count > 0 && denials.Count != request.DenialIds.Distinct().Count())
            throw RcmWorkflowException.Conflict("Some denials were not found or are no longer open.");
        if (denials.All(d => d.AppealDeadline.Date < DateTime.UtcNow.Date))
            throw RcmWorkflowException.Conflict($"The appeal window ({_options.AppealWindowDays} days) has passed for the selected denials.");

        var response = await _db.ClaimResponses.Where(r => r.ClaimId == claim.Id).OrderByDescending(r => r.ReceivedAt).FirstOrDefaultAsync(ct)
            ?? throw RcmWorkflowException.Conflict("Claim has no adjudication to appeal.");
        var level = await _db.AppealRequests.CountAsync(a => a.ClaimId == claim.Id && !a.IsWithdrawn, ct) + 1;
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        var appeal = new AppealRequest
        {
            AppealNumber = $"APL-{claim.ClaimNumber}-{level}",
            AppealIdentifierSystem = _mapper.IdentifierSystem("appeal"),
            AppealIdentifierValue = $"appeal_{stamp}",
            ClaimId = claim.Id,
            ClaimResponseId = response.Id,
            PatientId = claim.PatientId,
            InsurerId = claim.InsurerId,
            ProviderId = claim.ProviderId,
            AppealStatus = "Draft",
            AppealLevel = level,
            ErrorCodeBeingAppealed = string.Join(",", denials.Select(d => d.ReasonCode).Distinct()),
            ErrorDescription = string.Join("; ", denials.Where(d => d.ReasonDisplay != null).Select(d => d.ReasonDisplay).Distinct()),
            AppealReason = request.Reason,
            SupportingDocumentation = request.Attachments.Count == 0 ? null : string.Join(", ", request.Attachments.Select(a => a.Title ?? a.ContentType)),
            DenialDate = denials.Min(d => d.DenialDate),
            AppealDeadlineDate = denials.Min(d => d.AppealDeadline),
            AllowsEscalation = true,
            Notes = request.Notes,
            LastStatusUpdateDate = DateTime.UtcNow,
        };
        _db.AppealRequests.Add(appeal);
        AddHistory(appeal, "Draft", request.CreatedBy ?? "system", "Created");
        foreach (var d in denials) d.AppealRequestId = appeal.Id;
        await _db.SaveChangesAsync(ct);

        var errors = new List<string>();
        string? resubmissionId = null;
        if (request.Resubmit)
        {
            var result = await _claims.ResubmitClaimAsync(claim.Id, new DerivedClaimRequestDto { Submit = true, Reason = request.Reason }, ct);
            resubmissionId = result.ClaimId;
            errors.AddRange(result.Errors);
            var sent = result.TransactionStatus is RcmTransactionStatus.Completed or RcmTransactionStatus.Queued or RcmTransactionStatus.Timeout;
            if (sent) appeal.AppealSubmittedDate = DateTime.UtcNow;
            appeal.InternalReferenceNumber = result.ClaimId;
            AddHistory(appeal, sent ? "Submitted" : "Failed", request.CreatedBy ?? "system", $"Resubmission {result.ClaimNumber}: {result.TransactionStatus}");
            // A synchronous adjudication of the resubmission may already have decided the appeal.
            if (appeal.AppealStatus != "Decided") appeal.AppealStatus = sent ? "Submitted" : "Failed";
            if (!sent) resubmissionId = null;
        }
        else
        {
            appeal.AppealStatus = "Submitted";
            appeal.AppealSubmittedDate = DateTime.UtcNow;
        }

        foreach (var d in denials.Where(d => d.Status is RcmDenialStatus.Open or RcmDenialStatus.Resubmitted))
        {
            d.Status = RcmDenialStatus.Appealed;
            if (resubmissionId != null) d.ResubmissionClaimId = resubmissionId;
            d.UpdatedAt = DateTime.UtcNow;
        }
        appeal.LastStatusUpdateDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        string? communicationId = null;
        if (request.Texts.Count > 0 || request.Attachments.Count > 0)
        {
            var aboutClaimId = resubmissionId != null && appeal.AppealStatus == "Submitted" ? resubmissionId : claim.Id;
            try
            {
                var texts = request.Texts.Count > 0 ? request.Texts : new List<string> { request.Reason };
                var comm = await _communications.SendAsync(new SendCommunicationDto { ClaimId = aboutClaimId, Texts = texts, Attachments = request.Attachments, Category = "instruction" }, ct);
                communicationId = comm.Id;
                errors.AddRange(comm.Errors);
            }
            catch (RcmWorkflowException ex)
            {
                errors.Add($"Communication not sent: {ex.Message}");
                errors.AddRange(ex.Errors);
            }
        }

        var dto = await GetAsync(appeal.Id, ct) ?? throw new InvalidOperationException("Appeal vanished.");
        dto.CommunicationId = communicationId;
        dto.Errors = errors;
        return dto;
    }

    private void AddHistory(AppealRequest appeal, string status, string by, string? reason) =>
        _db.AppealStatusHistories.Add(new AppealStatusHistory { AppealId = appeal.Id, Status = status, ChangedBy = by, ChangeReason = reason });

    public async Task<AppealDto> WithdrawAsync(string id, string? reason, CancellationToken ct = default)
    {
        var appeal = await _db.AppealRequests.FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw RcmWorkflowException.NotFound("Appeal", id);
        if (appeal.IsWithdrawn || appeal.AppealStatus == "Decided") throw RcmWorkflowException.Conflict($"Appeal is {appeal.AppealStatus}.");
        appeal.IsWithdrawn = true;
        appeal.WithdrawnDate = DateTime.UtcNow;
        appeal.WithdrawalReason = reason;
        appeal.AppealStatus = "Withdrawn";
        appeal.LastStatusUpdateDate = DateTime.UtcNow;
        AddHistory(appeal, "Withdrawn", "user", reason);
        await _db.ClaimDenials.Where(d => d.AppealRequestId == id && d.Status == RcmDenialStatus.Appealed)
            .ForEachAsync(d => { d.Status = RcmDenialStatus.Open; d.AppealRequestId = null; }, ct);
        await _db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task<AppealDto?> GetAsync(string id, CancellationToken ct = default)
    {
        var a = await _db.AppealRequests.AsNoTracking().Include(x => x.Claim).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return null;
        return (await ProjectAsync(new List<AppealRequest> { a }, ct))[0];
    }

    public async Task<RcmPagedResult<AppealDto>> ListAsync(RcmQuery query, CancellationToken ct = default)
    {
        var q = _db.AppealRequests.AsNoTracking().Include(a => a.Claim).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(a => a.ProviderId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) q = q.Where(a => a.InsurerId == query.InsurerId);
        if (!string.IsNullOrWhiteSpace(query.PatientId)) q = q.Where(a => a.PatientId == query.PatientId);
        if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(a => a.AppealStatus == query.Status);
        if (query.From.HasValue) q = q.Where(a => a.CreatedAt >= query.From);
        if (query.To.HasValue) q = q.Where(a => a.CreatedAt <= query.To);
        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(a => a.CreatedAt).Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        return new RcmPagedResult<AppealDto> { Total = total, Page = Math.Max(1, query.Page), PageSize = query.Take, Items = await ProjectAsync(rows, ct) };
    }

    private async Task<List<AppealDto>> ProjectAsync(List<AppealRequest> rows, CancellationToken ct)
    {
        var ids = rows.Select(a => a.Id).ToList();
        var denials = await _db.ClaimDenials.AsNoTracking().Where(d => d.AppealRequestId != null && ids.Contains(d.AppealRequestId))
            .Select(d => new { d.Id, d.AppealRequestId, d.DeniedAmount, d.ResubmissionClaimId }).ToListAsync(ct);
        return rows.Select(a =>
        {
            var mine = denials.Where(d => d.AppealRequestId == a.Id).ToList();
            return new AppealDto
            {
                Id = a.Id,
                AppealNumber = a.AppealNumber,
                ClaimId = a.ClaimId,
                ClaimNumber = a.Claim?.ClaimNumber,
                ResubmissionClaimId = mine.Select(d => d.ResubmissionClaimId).FirstOrDefault(x => x != null) ?? a.InternalReferenceNumber,
                Status = a.AppealStatus,
                Outcome = a.AppealOutcome,
                Level = a.AppealLevel,
                ReasonCodes = a.ErrorCodeBeingAppealed,
                Reason = a.AppealReason,
                AppealedAmount = mine.Sum(d => d.DeniedAmount),
                ApprovedAmount = a.ApprovedAmount,
                DenialDate = a.DenialDate,
                DeadlineDate = a.AppealDeadlineDate,
                SubmittedDate = a.AppealSubmittedDate,
                LastStatusUpdate = a.LastStatusUpdateDate,
                DecisionExplanation = a.DecisionExplanation,
                DenialIds = mine.Select(d => d.Id).ToList(),
            };
        }).ToList();
    }
}
