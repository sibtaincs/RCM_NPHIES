using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Models;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

/// <summary>Payment reconciliation storage, claim matching, discrepancy reporting and payment-notice acknowledgement.</summary>
public interface IRcmPaymentService
{
    Task<RcmPagedResult<PaymentReconciliationDto>> ListAsync(RcmQuery query, CancellationToken ct = default);
    Task<PaymentReconciliationDto?> GetAsync(string id, CancellationToken ct = default);
    Task<List<PaymentDetailDto>> GetDiscrepanciesAsync(RcmQuery query, CancellationToken ct = default);
    Task<PaymentReconciliationDto> AcknowledgeAsync(string id, CancellationToken ct = default);
    Task<PaymentReconciliationDto> ManualMatchAsync(string detailId, ManualMatchDto request, CancellationToken ct = default);
}

public sealed class RcmPaymentService : IRcmPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly IRcmInputMapper _mapper;
    private readonly INphiesBundleBuilder _builder;
    private readonly IRcmMessageDispatcher _dispatcher;
    private readonly RcmOptions _options;
    private readonly NphiesConfiguration _nphies;

    public RcmPaymentService(ApplicationDbContext db, IRcmInputMapper mapper, INphiesBundleBuilder builder, IRcmMessageDispatcher dispatcher,
        IOptions<RcmOptions> options, IOptions<NphiesConfiguration> nphies)
    {
        _db = db;
        _mapper = mapper;
        _builder = builder;
        _dispatcher = dispatcher;
        _options = options.Value;
        _nphies = nphies.Value;
    }

    public async Task<RcmPagedResult<PaymentReconciliationDto>> ListAsync(RcmQuery query, CancellationToken ct = default)
    {
        var q = _db.PaymentReconciliations.AsNoTracking().Include(p => p.PaymentIssuer).Include(p => p.Details).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(p => p.RequestorId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) q = q.Where(p => p.PaymentIssuerId == query.InsurerId);
        if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(p => p.MatchStatus == query.Status || p.AcknowledgementStatus == query.Status);
        if (query.From.HasValue) q = q.Where(p => (p.PaymentDate ?? p.CreatedDate) >= query.From);
        if (query.To.HasValue) q = q.Where(p => (p.PaymentDate ?? p.CreatedDate) <= query.To);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.ReceivedAt ?? p.CreatedAt).Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        var numbers = await ClaimNumbersAsync(items.SelectMany(p => p.Details), ct);
        return new RcmPagedResult<PaymentReconciliationDto> { Total = total, Page = Math.Max(1, query.Page), PageSize = query.Take, Items = items.Select(p => RcmProjections.ToDto(p, numbers)).ToList() };
    }

    public async Task<PaymentReconciliationDto?> GetAsync(string id, CancellationToken ct = default)
    {
        var p = await _db.PaymentReconciliations.AsNoTracking().Include(x => x.PaymentIssuer).Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        return RcmProjections.ToDto(p, await ClaimNumbersAsync(p.Details, ct));
    }

    public async Task<List<PaymentDetailDto>> GetDiscrepanciesAsync(RcmQuery query, CancellationToken ct = default)
    {
        var q = _db.PaymentReconciliationDetails.AsNoTracking().Include(d => d.PaymentReconciliation)
            .Where(d => d.MatchStatus != "Matched");
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(d => d.PaymentReconciliation!.RequestorId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) q = q.Where(d => d.PaymentReconciliation!.PaymentIssuerId == query.InsurerId);
        if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(d => d.MatchStatus == query.Status);
        if (query.From.HasValue) q = q.Where(d => d.PaymentReconciliation!.CreatedDate >= query.From);
        if (query.To.HasValue) q = q.Where(d => d.PaymentReconciliation!.CreatedDate <= query.To);
        var details = await q.OrderByDescending(d => d.CreatedAt).Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        var numbers = await ClaimNumbersAsync(details, ct);
        return details.Select(d => RcmProjections.ToDto(d, d.MatchedClaimId != null && numbers.TryGetValue(d.MatchedClaimId, out var n) ? n : null)).ToList();
    }

    public async Task<PaymentReconciliationDto> ManualMatchAsync(string detailId, ManualMatchDto request, CancellationToken ct = default)
    {
        var detail = await _db.PaymentReconciliationDetails.FirstOrDefaultAsync(d => d.Id == detailId, ct) ?? throw RcmWorkflowException.NotFound("Payment detail", detailId);
        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.Id == request.ClaimId, ct) ?? throw RcmWorkflowException.NotFound("Claim", request.ClaimId);
        if (RcmInputMapper.IsPreAuth(claim)) throw RcmWorkflowException.Invalid("Payments can only be matched to claims.");
        var recon = await _db.PaymentReconciliations.Include(p => p.Details).FirstAsync(p => p.Id == detail.PaymentReconciliationId, ct);
        if (recon.PaymentIssuerId != null && recon.PaymentIssuerId != claim.InsurerId)
            throw RcmWorkflowException.Invalid("Claim belongs to a different payer than this payment.");

        var previousClaimId = detail.MatchedClaimId;
        var expected = await _db.ClaimResponses.Where(r => r.ClaimId == claim.Id).OrderByDescending(r => r.ReceivedAt)
            .Select(r => r.TotalBenefit).FirstOrDefaultAsync(ct) ?? claim.ApprovedAmount;
        var paid = detail.ComponentPayment ?? detail.Amount;
        RcmPaymentMatching.Classify(detail, claim.Id, expected, _options.PaymentMatchTolerance);
        detail.UpdatedAt = DateTime.UtcNow;
        recon.PaymentIssuerId ??= claim.InsurerId;
        recon.RequestorId ??= claim.ProviderId;
        RcmPaymentMatching.RecomputeHeader(recon);
        await _db.SaveChangesAsync(ct);

        await RecomputeClaimPaidAsync(claim.Id, ct);
        if (previousClaimId != null && previousClaimId != claim.Id) await RecomputeClaimPaidAsync(previousClaimId, ct);
        await _db.SaveChangesAsync(ct);
        return (await GetAsync(recon.Id, ct))!;
    }

    private async Task RecomputeClaimPaidAsync(string claimId, CancellationToken ct)
    {
        var claim = await _db.Claims.FirstAsync(c => c.Id == claimId, ct);
        var rows = await _db.PaymentReconciliationDetails.Include(d => d.PaymentReconciliation).Where(d => d.MatchedClaimId == claimId).ToListAsync(ct);
        if (rows.Count == 0)
        {
            claim.PaidAmount = null;
            claim.PaidAt = null;
            if (claim.NphiesStatus is RcmClaimStatus.Paid or RcmClaimStatus.PartiallyPaid or RcmClaimStatus.Recouped)
                claim.NphiesStatus = RcmResponseIngestor.MapAdjudicationStatus(claim.NphiesOutcome, claim.AdjudicationOutcome);
            return;
        }
        claim.PaidAmount = rows.Sum(RcmPaymentMatching.Paid);
        claim.PaidAt = rows.Max(d => d.PaymentReconciliation?.PaymentDate ?? d.DetailDate);
        claim.NphiesStatus = RcmPaymentMatching.ClaimStatus(claim.PaidAmount.Value, claim.ApprovedAmount ?? 0m,
            rows.Any(d => d.MatchStatus == RcmPaymentMatching.Recoupment), _options.PaymentMatchTolerance);
        claim.UpdatedAt = DateTime.UtcNow;
    }

    public async Task<PaymentReconciliationDto> AcknowledgeAsync(string id, CancellationToken ct = default)
    {
        var recon = await _db.PaymentReconciliations.Include(p => p.Requestor).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw RcmWorkflowException.NotFound("PaymentReconciliation", id);
        if (recon.AcknowledgementStatus == "Acknowledged")
            return (await GetAsync(id, ct))!;

        OrganizationInput provider;
        if (recon.Requestor != null) provider = _mapper.MapOrganization(recon.Requestor, NphiesOrganizationKind.Provider);
        else if (!string.IsNullOrWhiteSpace(_nphies.Sender.ProviderLicense))
            provider = new OrganizationInput { Id = "provider", License = _nphies.Sender.ProviderLicense, Name = _nphies.Sender.ProviderName, Kind = NphiesOrganizationKind.Provider };
        else throw RcmWorkflowException.Invalid("Cannot determine the provider to acknowledge this payment (no matched claim and no Nphies:Sender:ProviderLicense).");

        var paymentIdentifier = !string.IsNullOrWhiteSpace(recon.PaymentIdentifierValue)
            ? new NphiesIdentifier(recon.PaymentIdentifierSystem ?? string.Empty, recon.PaymentIdentifierValue!)
            : new NphiesIdentifier(recon.IdentifierSystem ?? string.Empty, recon.IdentifierValue ?? recon.PaymentReconciliationId);
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        var input = new PaymentNoticeInput
        {
            PaymentNoticeId = Guid.NewGuid().ToString("N"),
            Identifier = new NphiesIdentifier(_mapper.IdentifierSystem("paymentnotice"), $"PN_{stamp}"),
            PaymentIdentifier = paymentIdentifier,
            ReconciliationMessageId = recon.MessageId,
            PaymentDate = RcmInputMapper.Date(recon.PaymentDate ?? recon.CreatedDate),
            Amount = new NphiesMoney(recon.PaymentAmount, recon.PaymentCurrency),
            PaymentStatus = "cleared",
            Provider = provider,
        };

        Hl7.Fhir.Model.Bundle bundle;
        try { bundle = _builder.BuildPaymentNotice(input); }
        catch (ArgumentException ex)
        {
            recon.AcknowledgementStatus = "Failed";
            recon.AcknowledgementError = ex.Message;
            await _db.SaveChangesAsync(ct);
            throw RcmWorkflowException.Invalid(ex.Message);
        }

        var dispatch = await _dispatcher.DispatchAsync(new RcmDispatchRequest
        {
            Message = bundle,
            Operation = RcmOperations.PaymentNotice,
            EntityType = RcmEntityTypes.PaymentReconciliation,
            EntityId = recon.Id,
            RequestIdentifierSystem = input.Identifier.System,
            RequestIdentifierValue = input.Identifier.Value,
            ProviderId = recon.RequestorId,
        }, ct);

        if (dispatch.Sent)
        {
            _db.PaymentNotices.Add(new PaymentNotice
            {
                PaymentNoticeId = input.PaymentNoticeId,
                IdentifierSystem = input.Identifier.System,
                IdentifierValue = input.Identifier.Value,
                CreatedDate = DateTime.UtcNow,
                PaymentDate = recon.PaymentDate,
                PaymentIdentifierSystem = paymentIdentifier.System,
                PaymentIdentifierValue = paymentIdentifier.Value,
                Amount = recon.PaymentAmount,
                Currency = recon.PaymentCurrency,
                PaymentStatus = "cleared",
                PaymentStatusSystem = NphiesCodeSystems.PaymentStatus,
                ProviderId = recon.RequestorId,
                PayeeId = recon.RequestorId,
            });
        }

        recon.PaymentNoticeMessageId = dispatch.Transaction.MessageId;
        recon.AcknowledgementStatus = dispatch.Status switch
        {
            RcmTransactionStatus.Completed or RcmTransactionStatus.Queued => "Acknowledged",
            RcmTransactionStatus.Timeout => "Timeout",
            _ => "Failed",
        };
        recon.AcknowledgementError = dispatch.Errors.Count == 0 ? null : string.Join(" | ", dispatch.Errors);
        if (recon.AcknowledgementStatus == "Acknowledged") recon.AcknowledgedAt = DateTime.UtcNow;
        recon.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct))!;
    }

    private async Task<Dictionary<string, string>> ClaimNumbersAsync(IEnumerable<PaymentReconciliationDetail> details, CancellationToken ct)
    {
        var ids = details.Where(d => d.MatchedClaimId != null).Select(d => d.MatchedClaimId!).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, string>();
        return await _db.Claims.AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.ClaimNumber, ct);
    }
}
