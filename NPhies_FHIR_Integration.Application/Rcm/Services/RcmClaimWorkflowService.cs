using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Nphies.Models;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

/// <summary>
/// Prior authorization and claim lifecycle against NPHIES: submit, update, extend, resubmit,
/// convert prior-auth → claim, batch, status-check and cancel/nullify.
/// </summary>
public interface IRcmClaimWorkflowService
{
    Task<SubmissionResultDto> SubmitAsync(string claimId, CancellationToken ct = default);

    /// <summary>Prior-auth update: clone with related=prior and submit (or leave as draft).</summary>
    Task<SubmissionResultDto> UpdatePreAuthAsync(string preAuthId, DerivedClaimRequestDto request, CancellationToken ct = default);

    /// <summary>Prior-auth extension: clone with related=extend and submit (or leave as draft).</summary>
    Task<SubmissionResultDto> ExtendPreAuthAsync(string preAuthId, DerivedClaimRequestDto request, CancellationToken ct = default);

    /// <summary>Claim resubmission (correction/appeal): clone with related=prior and submit (or leave as draft).</summary>
    Task<SubmissionResultDto> ResubmitClaimAsync(string claimId, DerivedClaimRequestDto request, CancellationToken ct = default);

    /// <summary>Creates a claim from an approved prior authorization, carrying its preAuthRef.</summary>
    Task<SubmissionResultDto> CreateClaimFromPreAuthAsync(string preAuthId, DerivedClaimRequestDto request, CancellationToken ct = default);

    Task<RcmClaimSummaryDto> LinkPreAuthAsync(string claimId, LinkPreAuthDto request, CancellationToken ct = default);

    Task<BatchSubmitResultDto> SubmitBatchAsync(BatchSubmitRequestDto request, CancellationToken ct = default);
    Task<BatchSubmitResultDto?> GetBatchAsync(string batchIdentifier, CancellationToken ct = default);
    Task<List<BatchSubmitResultDto>> ListBatchesAsync(RcmQuery query, CancellationToken ct = default);

    Task<TaskResultDto> StatusCheckAsync(string claimId, CancellationToken ct = default);
    Task<TaskResultDto> CancelAsync(string claimId, CancelClaimRequestDto request, CancellationToken ct = default);

    Task<RcmPagedResult<RcmClaimSummaryDto>> ListAsync(RcmClaimQuery query, CancellationToken ct = default);
    Task<ClaimTimelineDto?> GetTimelineAsync(string claimId, CancellationToken ct = default);
    Task<ClaimAdjudicationDto?> GetLatestAdjudicationAsync(string claimId, CancellationToken ct = default);

    /// <summary>Builds (and validates) the request bundle a claim / prior-auth would send, without dispatching or saving.</summary>
    Task<BundlePreviewDto> PreviewBundleAsync(string claimId, CancellationToken ct = default);

    /// <summary>Raw request/response JSON for one NPHIES transaction; <paramref name="providerId"/> restricts to that facility.</summary>
    /// <param name="includeAttachments">Restore attachment content that the audit stored as document references (evidence export).</param>
    Task<MessageJsonDto?> GetTransactionJsonAsync(string transactionId, string? providerId, CancellationToken ct = default, bool includeAttachments = false);
}

public sealed class RcmClaimWorkflowService : IRcmClaimWorkflowService
{
    private static readonly string[] Submittable = { RcmClaimStatus.Draft, RcmClaimStatus.Error, RcmClaimStatus.ValidationFailed };

    private readonly ApplicationDbContext _db;
    private readonly Documents.IClaimDocumentStorage _documents;
    private readonly IRcmInputMapper _mapper;
    private readonly INphiesBundleBuilder _builder;
    private readonly IRcmMessageDispatcher _dispatcher;
    private readonly IRcmResponseIngestor _ingestor;
    private readonly INphiesBundleValidator _validator;
    private readonly NphiesConfiguration _nphies;
    private readonly RcmOptions _options;
    private readonly ILogger<RcmClaimWorkflowService> _logger;

    public RcmClaimWorkflowService(
        ApplicationDbContext db,
        IRcmInputMapper mapper,
        INphiesBundleBuilder builder,
        IRcmMessageDispatcher dispatcher,
        IRcmResponseIngestor ingestor,
        INphiesBundleValidator validator,
        IOptions<NphiesConfiguration> nphies,
        IOptions<RcmOptions> options,
        ILogger<RcmClaimWorkflowService> logger,
        Documents.IClaimDocumentStorage documents)
    {
        _db = db;
        _documents = documents;
        _mapper = mapper;
        _builder = builder;
        _dispatcher = dispatcher;
        _ingestor = ingestor;
        _validator = validator;
        _nphies = nphies.Value;
        _options = options.Value;
        _logger = logger;
    }

    // ===================== Submit =====================

    public async Task<SubmissionResultDto> SubmitAsync(string claimId, CancellationToken ct = default)
    {
        var claim = await _mapper.LoadClaimGraphAsync(claimId, ct) ?? throw RcmWorkflowException.NotFound("Claim", claimId);
        EnsureSubmittable(claim);
        if (_options.RequireReviewApprovalForClaims && !RcmInputMapper.IsPreAuth(claim) && !claim.IsApprovedForSubmission)
            throw RcmWorkflowException.Conflict(
                $"Claim {claim.ClaimNumber} needs technical and medical review approval before submission " +
                $"(technical: {claim.TechnicalReviewStatus ?? "Pending"}, medical: {claim.MedicalReviewStatus ?? "Pending"}).");
        if (claim.Items.Count == 0)
            throw RcmWorkflowException.Invalid("A claim or prior authorization needs at least one item.");

        var preAuth = RcmInputMapper.IsPreAuth(claim);
        ClaimRequestInput input;
        try
        {
            input = await _mapper.MapClaimAsync(claim, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw RcmWorkflowException.Invalid(ex.Message);
        }

        Hl7.Fhir.Model.Bundle bundle;
        try
        {
            bundle = preAuth ? _builder.BuildPriorAuthRequest(input) : _builder.BuildClaimRequest(input);
        }
        catch (ArgumentException ex)
        {
            throw RcmWorkflowException.Invalid(ex.Message);
        }

        var originals = claim.RelatedClaims.Where(r => r.ReferencedClaimId != null && r.Relationship is "prior" or "extend")
            .Select(r => r.ReferencedClaimId!).ToList();
        // Link open denials of the replaced version first so a synchronous adjudication can resolve them.
        var linked = originals.Count == 0 ? new List<ClaimDenial>()
            : await _db.ClaimDenials.Where(d => originals.Contains(d.ClaimId) && (d.Status == "Open" || d.Status == "Appealed") && d.ResubmissionClaimId == null).ToListAsync(ct);
        foreach (var d in linked) d.ResubmissionClaimId = claim.Id;

        var dispatch = await _dispatcher.DispatchAsync(new RcmDispatchRequest
        {
            Message = bundle,
            Operation = preAuth ? RcmOperations.PriorAuth : RcmOperations.Claim,
            EntityType = RcmEntityTypes.Claim,
            EntityId = claim.Id,
            RequestIdentifierSystem = input.Identifier.System,
            RequestIdentifierValue = input.Identifier.Value,
            ProviderId = claim.ProviderId,
        }, ct);

        await ApplyDispatchAsync(claim, dispatch, ct);
        await SupersedeRelatedAsync(claim, originals, linked, ct);
        await _db.SaveChangesAsync(ct);

        return await BuildSubmissionResultAsync(claim, dispatch, ct);
    }

    private static void EnsureSubmittable(Claim claim)
    {
        var status = claim.NphiesStatus ?? RcmClaimStatus.Draft;
        if (Submittable.Contains(status)) return;
        if (status == RcmClaimStatus.Timeout)
            throw RcmWorkflowException.Conflict("The previous submission timed out and may have been delivered. Run a status-check or poll instead of resending.");
        if (RcmClaimStatus.AwaitingPayer.Contains(status))
            throw RcmWorkflowException.Conflict($"Claim is already with the payer ({status}). Use status-check or poll.");
        throw RcmWorkflowException.Conflict($"Claim is {status}. Use update/extend/resubmit to send a new version.");
    }

    /// <summary>Applies the transport outcome and ingests the synchronous reply (if any).</summary>
    private async Task ApplyDispatchAsync(Claim claim, RcmDispatchResult dispatch, CancellationToken ct)
    {
        claim.LastMessageId = dispatch.Transaction.MessageId;
        claim.UpdatedAt = DateTime.UtcNow;
        if (dispatch.Sent) claim.SubmittedAt ??= DateTime.UtcNow;

        claim.NphiesStatus = dispatch.Status switch
        {
            RcmTransactionStatus.ValidationFailed => RcmClaimStatus.ValidationFailed,
            RcmTransactionStatus.Timeout => RcmClaimStatus.Timeout,
            RcmTransactionStatus.Queued => RcmClaimStatus.Queued,
            RcmTransactionStatus.Error => RcmClaimStatus.Error,
            _ => RcmClaimStatus.Submitted,
        };

        if (dispatch.Response != null && dispatch.Status is RcmTransactionStatus.Completed or RcmTransactionStatus.Queued or RcmTransactionStatus.Error)
        {
            await _db.SaveChangesAsync(ct);
            await _ingestor.IngestAsync(dispatch.Response, dispatch.Transaction, ct);
            if (dispatch.Status == RcmTransactionStatus.Error) claim.NphiesStatus = RcmClaimStatus.Error;
        }
    }

    private async Task SupersedeRelatedAsync(Claim claim, List<string> originals, List<ClaimDenial> linked, CancellationToken ct)
    {
        if (claim.NphiesStatus is RcmClaimStatus.ValidationFailed or RcmClaimStatus.Error)
        {
            foreach (var d in linked) d.ResubmissionClaimId = null;
            return;
        }
        if (originals.Count == 0) return;
        foreach (var original in await _db.Claims.Where(c => originals.Contains(c.Id)).ToListAsync(ct))
        {
            if (original.NphiesStatus is RcmClaimStatus.Cancelled or RcmClaimStatus.Nullified) continue;
            original.NphiesStatus = RcmClaimStatus.Superseded;
            original.UpdatedAt = DateTime.UtcNow;
        }
        // Denials not already resolved by a synchronous adjudication are now worked through the new version.
        foreach (var d in linked.Where(d => d.Status == "Open"))
        {
            d.Status = "Resubmitted";
            d.UpdatedAt = DateTime.UtcNow;
        }
    }
    private async Task<SubmissionResultDto> BuildSubmissionResultAsync(Claim claim, RcmDispatchResult dispatch, CancellationToken ct)
    {
        var latest = await LatestResponseQuery(claim.Id).FirstOrDefaultAsync(ct);
        return new SubmissionResultDto
        {
            ClaimId = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            Use = claim.Use,
            Operation = dispatch.Transaction.Operation,
            TransactionId = dispatch.Transaction.Id,
            MessageId = dispatch.Transaction.MessageId,
            TransactionStatus = dispatch.Status,
            ClaimStatus = claim.NphiesStatus,
            Outcome = claim.NphiesOutcome,
            AdjudicationOutcome = claim.AdjudicationOutcome,
            Errors = dispatch.Errors,
            Adjudication = latest is null ? null : RcmProjections.ToDto(latest),
        };
    }

    private IQueryable<ClaimResponse> LatestResponseQuery(string claimId) =>
        _db.ClaimResponses.Include(r => r.Items).Where(r => r.ClaimId == claimId).OrderByDescending(r => r.ReceivedAt).ThenByDescending(r => r.CreatedAt);

    // ===================== Derived versions =====================

    public async Task<SubmissionResultDto> UpdatePreAuthAsync(string preAuthId, DerivedClaimRequestDto request, CancellationToken ct = default)
    {
        var source = await LoadForDerivationAsync(preAuthId, expectPreAuth: true, ct);
        if (source.NphiesStatus is RcmClaimStatus.Cancelled or RcmClaimStatus.Nullified or RcmClaimStatus.Superseded)
            throw RcmWorkflowException.Conflict($"Prior authorization is {source.NphiesStatus} and cannot be updated.");
        if (source.SubmittedAt is null)
            throw RcmWorkflowException.Conflict("Prior authorization has not been sent yet; edit and submit it directly.");
        var clone = await CloneAsync(source, "U", relationship: "prior", useOverride: null, ct);
        ApplyPeriod(clone, request.NewPeriodEnd);
        return await FinishDerivedAsync(clone, request, ct);
    }

    public async Task<SubmissionResultDto> ExtendPreAuthAsync(string preAuthId, DerivedClaimRequestDto request, CancellationToken ct = default)
    {
        var source = await LoadForDerivationAsync(preAuthId, expectPreAuth: true, ct);
        if (source.NphiesStatus is not (RcmClaimStatus.Approved or RcmClaimStatus.PartiallyApproved))
            throw RcmWorkflowException.Conflict("Only an approved prior authorization can be extended.");
        var clone = await CloneAsync(source, "E", relationship: "extend", useOverride: null, ct);
        ApplyPeriod(clone, request.NewPeriodEnd);
        return await FinishDerivedAsync(clone, request, ct);
    }

    public async Task<SubmissionResultDto> ResubmitClaimAsync(string claimId, DerivedClaimRequestDto request, CancellationToken ct = default)
    {
        var source = await LoadForDerivationAsync(claimId, expectPreAuth: false, ct);
        if (source.NphiesStatus is not (RcmClaimStatus.Rejected or RcmClaimStatus.PartiallyApproved or RcmClaimStatus.Approved
            or RcmClaimStatus.Paid or RcmClaimStatus.PartiallyPaid or RcmClaimStatus.Recouped or RcmClaimStatus.Cancelled))
            throw RcmWorkflowException.Conflict($"Claim is {source.NphiesStatus ?? RcmClaimStatus.Draft}; only adjudicated or cancelled claims can be resubmitted.");
        var clone = await CloneAsync(source, "R", relationship: "prior", useOverride: null, ct);
        return await FinishDerivedAsync(clone, request, ct);
    }

    public async Task<SubmissionResultDto> CreateClaimFromPreAuthAsync(string preAuthId, DerivedClaimRequestDto request, CancellationToken ct = default)
    {
        var source = await LoadForDerivationAsync(preAuthId, expectPreAuth: true, ct);
        if (source.NphiesStatus is not (RcmClaimStatus.Approved or RcmClaimStatus.PartiallyApproved))
            throw RcmWorkflowException.Conflict("Only an approved prior authorization can be converted to a claim.");
        var preAuthRef = await LatestResponseQuery(source.Id).Select(r => r.PreAuthRef).FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(preAuthRef))
            throw RcmWorkflowException.Conflict("The prior authorization response has no preAuthRef.");

        var clone = await CloneAsync(source, "C", relationship: null, useOverride: "claim", ct);
        clone.PreAuthorizationRef = preAuthRef;
        return await FinishDerivedAsync(clone, request, ct);
    }

    public async Task<RcmClaimSummaryDto> LinkPreAuthAsync(string claimId, LinkPreAuthDto request, CancellationToken ct = default)
    {
        var claim = await _db.Claims.Include(c => c.Patient).Include(c => c.Insurer).FirstOrDefaultAsync(c => c.Id == claimId, ct)
            ?? throw RcmWorkflowException.NotFound("Claim", claimId);
        if (RcmInputMapper.IsPreAuth(claim))
            throw RcmWorkflowException.Invalid("Prior authorizations cannot reference other prior authorizations.");
        if (!Submittable.Contains(claim.NphiesStatus ?? RcmClaimStatus.Draft))
            throw RcmWorkflowException.Conflict("Only unsent claims can be linked; resubmit to change an adjudicated claim.");

        string? reference = request.PreAuthRef;
        if (!string.IsNullOrWhiteSpace(request.PreAuthClaimId))
        {
            var preAuth = await _db.Claims.FirstOrDefaultAsync(c => c.Id == request.PreAuthClaimId, ct)
                ?? throw RcmWorkflowException.NotFound("Prior authorization", request.PreAuthClaimId!);
            if (!RcmInputMapper.IsPreAuth(preAuth))
                throw RcmWorkflowException.Invalid("The referenced record is not a prior authorization.");
            if (preAuth.PatientId != claim.PatientId || preAuth.InsurerId != claim.InsurerId)
                throw RcmWorkflowException.Invalid("Prior authorization belongs to a different patient or insurer.");
            if (preAuth.NphiesStatus is not (RcmClaimStatus.Approved or RcmClaimStatus.PartiallyApproved))
                throw RcmWorkflowException.Conflict("Prior authorization is not approved.");
            if (preAuth.PreAuthPeriodEnd.HasValue && preAuth.PreAuthPeriodEnd.Value.Date < DateTime.UtcNow.Date)
                throw RcmWorkflowException.Conflict($"Prior authorization expired on {preAuth.PreAuthPeriodEnd:yyyy-MM-dd}.");
            reference = await LatestResponseQuery(preAuth.Id).Select(r => r.PreAuthRef).FirstOrDefaultAsync(ct);
        }
        if (string.IsNullOrWhiteSpace(reference))
            throw RcmWorkflowException.Invalid("Provide preAuthClaimId of an approved prior authorization or a preAuthRef.");

        var refs = (claim.PreAuthorizationRef ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (!refs.Contains(reference!)) refs.Add(reference!);
        claim.PreAuthorizationRef = string.Join(",", refs);
        claim.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return RcmProjections.ToSummary(claim);
    }

    private async Task<Claim> LoadForDerivationAsync(string id, bool expectPreAuth, CancellationToken ct)
    {
        var claim = await _mapper.LoadClaimGraphAsync(id, ct) ?? throw RcmWorkflowException.NotFound(expectPreAuth ? "Prior authorization" : "Claim", id);
        if (RcmInputMapper.IsPreAuth(claim) != expectPreAuth)
            throw RcmWorkflowException.Invalid(expectPreAuth ? "Record is not a prior authorization." : "Record is not a claim.");
        return claim;
    }

    private static void ApplyPeriod(Claim clone, DateTime? newEnd)
    {
        if (!newEnd.HasValue) return;
        clone.BillablePeriodEnd = newEnd;
        clone.ServicedPeriodEnd = newEnd;
    }

    private async Task<SubmissionResultDto> FinishDerivedAsync(Claim clone, DerivedClaimRequestDto request, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);
        if (request.Submit) return await SubmitAsync(clone.Id, ct);
        return new SubmissionResultDto
        {
            ClaimId = clone.Id,
            ClaimNumber = clone.ClaimNumber,
            Use = clone.Use,
            Operation = "draft",
            TransactionStatus = "NotSent",
            ClaimStatus = clone.NphiesStatus,
        };
    }

    /// <summary>Deep-copies a claim (items, details, diagnoses, care team, supporting info, vision/oral rows) as a new draft version.</summary>
    private async Task<Claim> CloneAsync(Claim source, string suffix, string? relationship, string? useOverride, CancellationToken ct)
    {
        var prefix = $"{source.ClaimNumber}-{suffix}";
        var existing = await _db.Claims.CountAsync(c => c.ClaimNumber.StartsWith(prefix), ct);
        // Pin the source's episode and invoice numbers first so the clone (resubmission, update, extension,
        // claim from a prior authorization) stays in the same episode of care instead of getting new defaults.
        _mapper.EnsureClaimDefaults(source);
        var clone = (Claim)_db.Entry(source).CurrentValues.ToObject();
        clone.Id = Guid.NewGuid().ToString();
        clone.ClaimNumber = $"{prefix}{existing + 1}";
        clone.CreatedDate = DateTime.UtcNow;
        clone.CreatedAt = DateTime.UtcNow;
        clone.UpdatedAt = DateTime.UtcNow;
        clone.Status = "active";
        clone.ClaimIdentifierValue = null;
        if (useOverride != null)
        {
            clone.Use = useOverride;
            clone.ClaimIdentifierSystem = null;
        }
        clone.MessageHeaderId = null;
        clone.FhirClaimBundle = null;
        clone.NphiesStatus = RcmClaimStatus.Draft;
        clone.NphiesOutcome = null;
        clone.AdjudicationOutcome = null;
        clone.SubmittedAt = null;
        clone.AdjudicatedAt = null;
        clone.ApprovedAmount = null;
        clone.PatientShareAmount = null;
        clone.PaidAmount = null;
        clone.PaidAt = null;
        clone.LastMessageId = null;
        clone.BatchIdentifier = null;
        clone.BatchNumber = null;
        if (useOverride == "claim") clone.PreAuthPeriodEnd = null;
        _db.Claims.Add(clone);

        var itemIdMap = new Dictionary<string, string>();
        foreach (var item in source.Items)
        {
            var ni = (ClaimItem)_db.Entry(item).CurrentValues.ToObject();
            ni.Id = Guid.NewGuid().ToString();
            ni.ClaimId = clone.Id;
            ni.CreatedAt = DateTime.UtcNow;
            itemIdMap[item.Id] = ni.Id;
            _db.ClaimItems.Add(ni);
            foreach (var detail in item.Details)
            {
                var nd = (ClaimItemDetail)_db.Entry(detail).CurrentValues.ToObject();
                nd.Id = Guid.NewGuid().ToString();
                nd.ClaimItemId = ni.Id;
                _db.Add(nd);
            }
        }

        foreach (var d in source.Diagnoses) { var x = (ClaimDiagnosis)_db.Entry(d).CurrentValues.ToObject(); x.Id = Guid.NewGuid().ToString(); x.ClaimId = clone.Id; _db.Add(x); }
        foreach (var m in source.CareTeam) { var x = (ClaimCareTeam)_db.Entry(m).CurrentValues.ToObject(); x.Id = Guid.NewGuid().ToString(); x.ClaimId = clone.Id; _db.Add(x); }
        foreach (var s in source.SupportingInfo) { var x = (ClaimSupportingInfo)_db.Entry(s).CurrentValues.ToObject(); x.Id = Guid.NewGuid().ToString(); x.ClaimId = clone.Id; _db.Add(x); }

        var sourceItemIds = itemIdMap.Keys.ToList();
        if (sourceItemIds.Count > 0)
        {
            foreach (var v in await _db.VisionPrescriptions.Where(v => sourceItemIds.Contains(v.ClaimItemId)).ToListAsync(ct))
            { var x = (VisionPrescription)_db.Entry(v).CurrentValues.ToObject(); x.Id = Guid.NewGuid().ToString(); x.ClaimItemId = itemIdMap[v.ClaimItemId]; _db.Add(x); }
            foreach (var o in await _db.OralDetails.Where(o => sourceItemIds.Contains(o.ClaimItemId)).ToListAsync(ct))
            { var x = (OralDetail)_db.Entry(o).CurrentValues.ToObject(); x.Id = Guid.NewGuid().ToString(); x.ClaimItemId = itemIdMap[o.ClaimItemId]; _db.Add(x); }
        }

        if (relationship != null)
        {
            var sourceIdentifier = _mapper.EnsureClaimIdentifier(source);
            _db.Add(new ClaimRelated
            {
                ClaimId = clone.Id,
                RelatedClaimIdentifierSystem = sourceIdentifier.System,
                RelatedClaimIdentifierValue = sourceIdentifier.Value,
                Relationship = relationship,
                RelationshipSystem = Nphies.Constants.NphiesCodeSystems.RelatedClaimRelationship,
                ReferencedClaimId = source.Id,
            });
        }
        return clone;
    }

    // ===================== Batch =====================

    public async Task<BatchSubmitResultDto> SubmitBatchAsync(BatchSubmitRequestDto request, CancellationToken ct = default)
    {
        var ids = request.ClaimIds.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
        if (ids.Count == 0) throw RcmWorkflowException.Invalid("claimIds is required.");
        var max = _nphies.BatchMaxClaims > 0 ? _nphies.BatchMaxClaims : Nphies.Constants.NphiesLimits.BatchMaxClaims;
        if (ids.Count > max) throw RcmWorkflowException.Invalid($"A batch may contain at most {max} claims (configured Nphies:BatchMaxClaims).");

        var claims = new List<Claim>();
        foreach (var id in ids)
            claims.Add(await _mapper.LoadClaimGraphAsync(id, ct) ?? throw RcmWorkflowException.NotFound("Claim", id));

        var errors = new List<string>();
        foreach (var c in claims)
        {
            if (RcmInputMapper.IsPreAuth(c)) errors.Add($"{c.ClaimNumber}: prior authorizations cannot be batched.");
            if (!Submittable.Contains(c.NphiesStatus ?? RcmClaimStatus.Draft)) errors.Add($"{c.ClaimNumber}: status {c.NphiesStatus} is not submittable.");
            if (c.Items.Count == 0) errors.Add($"{c.ClaimNumber}: no items.");
            if (_options.RequireReviewApprovalForClaims && !c.IsApprovedForSubmission)
                errors.Add($"{c.ClaimNumber}: technical and medical review approval is required.");
        }
        if (claims.Select(c => c.InsurerId).Distinct().Count() > 1) errors.Add("All claims in a batch must be for the same insurer.");
        if (claims.Select(c => c.ProviderId).Distinct().Count() > 1) errors.Add("All claims in a batch must be from the same provider.");
        if (errors.Count > 0) throw RcmWorkflowException.Invalid("Batch rejected.", errors);

        var batchId = string.IsNullOrWhiteSpace(request.BatchIdentifier)
            ? $"batch_{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)}"
            : request.BatchIdentifier!.Trim();
        if (await _db.Claims.AnyAsync(c => c.BatchIdentifier == batchId, ct))
            throw RcmWorkflowException.Conflict($"Batch identifier '{batchId}' is already used.");

        var start = claims.Min(c => c.BillablePeriodStart ?? c.ServicedPeriodStart ?? c.CreatedDate);
        var end = claims.Max(c => c.BillablePeriodEnd ?? c.ServicedPeriodEnd ?? c.BillablePeriodStart ?? c.ServicedPeriodStart ?? c.CreatedDate);
        var inputs = new List<ClaimRequestInput>();
        for (var i = 0; i < claims.Count; i++)
        {
            claims[i].BatchIdentifier = batchId;
            claims[i].BatchNumber = i + 1;
            try { inputs.Add(await _mapper.MapClaimAsync(claims[i], ct)); }
            catch (InvalidOperationException ex) { errors.Add($"{claims[i].ClaimNumber}: {ex.Message}"); }
        }
        if (errors.Count > 0)
        {
            foreach (var c in claims) { c.BatchIdentifier = null; c.BatchNumber = null; }
            throw RcmWorkflowException.Invalid("Batch rejected.", errors);
        }

        var batchInput = new BatchRequestInput
        {
            BatchIdentifier = new NphiesIdentifier(_mapper.IdentifierSystem("batch"), batchId),
            BatchPeriod = new NphiesPeriod(RcmInputMapper.Date(start), RcmInputMapper.Date(end)),
            Claims = inputs,
            AssignBatchExtensions = true,
        };

        Hl7.Fhir.Model.Bundle bundle;
        try { bundle = _builder.BuildBatchRequest(batchInput); }
        catch (ArgumentException ex)
        {
            foreach (var c in claims) { c.BatchIdentifier = null; c.BatchNumber = null; }
            throw RcmWorkflowException.Invalid(ex.Message);
        }

        var dispatch = await _dispatcher.DispatchAsync(new RcmDispatchRequest
        {
            Message = bundle,
            Operation = RcmOperations.Batch,
            EntityType = RcmEntityTypes.Batch,
            EntityId = batchId,
            RequestIdentifierSystem = batchInput.BatchIdentifier.System,
            RequestIdentifierValue = batchId,
            ProviderId = claims[0].ProviderId,
        }, ct);

        foreach (var c in claims)
        {
            c.LastMessageId = dispatch.Transaction.MessageId;
            c.UpdatedAt = DateTime.UtcNow;
            if (dispatch.Sent) c.SubmittedAt ??= DateTime.UtcNow;
            c.NphiesStatus = dispatch.Status switch
            {
                RcmTransactionStatus.ValidationFailed => RcmClaimStatus.ValidationFailed,
                RcmTransactionStatus.Timeout => RcmClaimStatus.Timeout,
                RcmTransactionStatus.Error => RcmClaimStatus.Error,
                RcmTransactionStatus.Completed => RcmClaimStatus.Submitted,
                _ => RcmClaimStatus.Queued,
            };
            if (dispatch.Status == RcmTransactionStatus.ValidationFailed) { c.BatchIdentifier = null; c.BatchNumber = null; }
        }
        await _db.SaveChangesAsync(ct);

        // A batch-response may carry the claim-responses inline.
        if (dispatch.Response != null && dispatch.Status is RcmTransactionStatus.Completed or RcmTransactionStatus.Queued)
        {
            await _ingestor.IngestAsync(dispatch.Response, null, ct);
            await _db.SaveChangesAsync(ct);
        }

        var result = await GetBatchAsync(batchId, ct) ?? new BatchSubmitResultDto { BatchIdentifier = batchId };
        result.TransactionId = dispatch.Transaction.Id;
        result.MessageId = dispatch.Transaction.MessageId;
        result.TransactionStatus = dispatch.Status;
        result.Errors = dispatch.Errors;
        if (dispatch.Status == RcmTransactionStatus.ValidationFailed)
        {
            result.ClaimCount = claims.Count;
            result.Claims = claims.Select(c => new BatchClaimDto { ClaimId = c.Id, ClaimNumber = c.ClaimNumber, ClaimStatus = c.NphiesStatus, Total = c.Total }).ToList();
        }
        return result;
    }

    public async Task<BatchSubmitResultDto?> GetBatchAsync(string batchIdentifier, CancellationToken ct = default)
    {
        var claims = await _db.Claims.AsNoTracking().Where(c => c.BatchIdentifier == batchIdentifier).OrderBy(c => c.BatchNumber).ToListAsync(ct);
        var tx = await _db.NphiesTransactions.AsNoTracking()
            .Where(t => t.Direction == "outbound" && t.EntityType == RcmEntityTypes.Batch && t.EntityId == batchIdentifier)
            .OrderByDescending(t => t.SentAt).FirstOrDefaultAsync(ct);
        if (claims.Count == 0 && tx is null) return null;
        return new BatchSubmitResultDto
        {
            BatchIdentifier = batchIdentifier,
            TransactionId = tx?.Id,
            MessageId = tx?.MessageId,
            TransactionStatus = tx?.Status ?? "Unknown",
            ClaimCount = claims.Count,
            SubmittedAt = tx?.SentAt,
            CompletedAt = tx?.CompletedAt,
            Errors = string.IsNullOrWhiteSpace(tx?.ErrorSummary) ? new List<string>() : tx!.ErrorSummary!.Split(" | ").ToList(),
            Claims = claims.Select(c => new BatchClaimDto
            {
                ClaimId = c.Id,
                ClaimNumber = c.ClaimNumber,
                BatchNumber = c.BatchNumber,
                ClaimStatus = c.NphiesStatus,
                Outcome = c.AdjudicationOutcome ?? c.NphiesOutcome,
                Total = c.Total,
                ApprovedAmount = c.ApprovedAmount,
            }).ToList(),
        };
    }

    public async Task<List<BatchSubmitResultDto>> ListBatchesAsync(RcmQuery query, CancellationToken ct = default)
    {
        var q = _db.NphiesTransactions.AsNoTracking().Where(t => t.Direction == "outbound" && t.Operation == RcmOperations.Batch);
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(t => t.ProviderId == query.ProviderId);
        if (query.From.HasValue) q = q.Where(t => t.SentAt >= query.From);
        if (query.To.HasValue) q = q.Where(t => t.SentAt <= query.To);
        var ids = await q.OrderByDescending(t => t.SentAt).Select(t => t.EntityId!).Distinct().Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        var list = new List<BatchSubmitResultDto>();
        foreach (var id in ids)
            if (await GetBatchAsync(id, ct) is { } b) list.Add(b);
        return list;
    }

    // ===================== Status-check / cancel =====================

    public Task<TaskResultDto> StatusCheckAsync(string claimId, CancellationToken ct = default) =>
        SendTaskAsync(claimId, RcmOperations.StatusCheck, null, ct);

    public Task<TaskResultDto> CancelAsync(string claimId, CancelClaimRequestDto request, CancellationToken ct = default) =>
        SendTaskAsync(claimId, request.Nullify ? RcmOperations.Nullify : RcmOperations.Cancel, request, ct);

    private async Task<TaskResultDto> SendTaskAsync(string claimId, string operation, CancelClaimRequestDto? cancel, CancellationToken ct)
    {
        var claim = await _db.Claims.Include(c => c.Provider).Include(c => c.Insurer).FirstOrDefaultAsync(c => c.Id == claimId, ct)
            ?? throw RcmWorkflowException.NotFound("Claim", claimId);
        if (claim.SubmittedAt is null || string.IsNullOrWhiteSpace(claim.ClaimIdentifierValue))
            throw RcmWorkflowException.Conflict("Claim has not been sent to NPHIES.");
        if (claim.NphiesStatus is RcmClaimStatus.Cancelled or RcmClaimStatus.Nullified)
            throw RcmWorkflowException.Conflict($"Claim is already {claim.NphiesStatus}.");
        if (cancel != null && string.IsNullOrWhiteSpace(cancel.Reason))
            throw RcmWorkflowException.Invalid("A cancellation reason is required.");
        if (claim.Provider is null || claim.Insurer is null)
            throw RcmWorkflowException.Invalid("Claim provider or insurer is missing.");

        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        var input = new ClaimTaskInput
        {
            TaskId = Guid.NewGuid().ToString("N"),
            Identifier = new NphiesIdentifier(_mapper.IdentifierSystem("task"), $"{(cancel is null ? "StatusCheck" : "Cancel")}_{stamp}"),
            ClaimIdentifier = new NphiesIdentifier(claim.ClaimIdentifierSystem ?? _mapper.IdentifierSystem("claim"), claim.ClaimIdentifierValue!),
            Provider = _mapper.MapOrganization(claim.Provider, NphiesOrganizationKind.Provider),
            Insurer = _mapper.MapOrganization(claim.Insurer, NphiesOrganizationKind.Insurer),
            Reason = cancel?.Reason,
            CancelCode = operation == RcmOperations.Nullify ? "nullify" : "cancel",
        };
        var bundle = cancel is null ? _builder.BuildStatusCheck(input) : _builder.BuildCancelRequest(input);
        var dispatch = await _dispatcher.DispatchAsync(new RcmDispatchRequest
        {
            Message = bundle,
            Operation = operation,
            EntityType = RcmEntityTypes.Claim,
            EntityId = claim.Id,
            RequestIdentifierSystem = input.ClaimIdentifier.System,
            RequestIdentifierValue = input.ClaimIdentifier.Value,
            ProviderId = claim.ProviderId,
        }, ct);

        if (dispatch.Response != null && dispatch.Status == RcmTransactionStatus.Completed)
            await _ingestor.IngestAsync(dispatch.Response, dispatch.Transaction, ct);
        await _db.SaveChangesAsync(ct);

        return new TaskResultDto
        {
            ClaimId = claim.Id,
            Operation = operation,
            TransactionId = dispatch.Transaction.Id,
            MessageId = dispatch.Transaction.MessageId,
            TransactionStatus = dispatch.Status,
            TaskStatus = dispatch.Response?.Task?.Status,
            ProcessingStatus = dispatch.Response?.Task?.ProcessingStatus,
            ClaimStatus = claim.NphiesStatus,
            Errors = dispatch.Errors,
        };
    }

    // ===================== Queries =====================

    public async Task<RcmPagedResult<RcmClaimSummaryDto>> ListAsync(RcmClaimQuery query, CancellationToken ct = default)
    {
        var q = _db.Claims.AsNoTracking().Include(c => c.Patient).Include(c => c.Insurer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(c => c.ProviderId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) q = q.Where(c => c.InsurerId == query.InsurerId);
        if (!string.IsNullOrWhiteSpace(query.PatientId)) q = q.Where(c => c.PatientId == query.PatientId);
        if (!string.IsNullOrWhiteSpace(query.ClaimType)) q = q.Where(c => c.ClaimType == query.ClaimType);
        if (!string.IsNullOrWhiteSpace(query.Use))
            q = query.Use!.StartsWith("pre", StringComparison.OrdinalIgnoreCase)
                ? q.Where(c => c.Use.Contains("preauth"))
                : q.Where(c => c.Use == query.Use);
        var status = query.NphiesStatus ?? query.Status;
        if (!string.IsNullOrWhiteSpace(status))
            q = status == RcmClaimStatus.Draft ? q.Where(c => c.NphiesStatus == null || c.NphiesStatus == status) : q.Where(c => c.NphiesStatus == status);
        if (query.From.HasValue) q = q.Where(c => c.CreatedDate >= query.From);
        if (query.To.HasValue) q = q.Where(c => c.CreatedDate <= query.To);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(c => c.ClaimNumber.Contains(s) || (c.ClaimIdentifierValue != null && c.ClaimIdentifierValue.Contains(s))
                || (c.Patient != null && (c.Patient.FullName.Contains(s) || c.Patient.NationalId.Contains(s))));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(c => c.CreatedDate).Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        var ids = items.Select(c => c.Id).ToList();
        var refs = await _db.ClaimResponses.AsNoTracking().Where(r => ids.Contains(r.ClaimId) && r.PreAuthRef != null)
            .GroupBy(r => r.ClaimId).Select(g => new { g.Key, Ref = g.OrderByDescending(r => r.ReceivedAt).Select(r => r.PreAuthRef).First() })
            .ToDictionaryAsync(x => x.Key, x => x.Ref, ct);
        var taxes = await _db.ClaimResponses.AsNoTracking().Where(r => ids.Contains(r.ClaimId) && r.TotalTax != null)
            .GroupBy(r => r.ClaimId).Select(g => new { g.Key, Tax = g.OrderByDescending(r => r.ReceivedAt).Select(r => r.TotalTax).First() })
            .ToDictionaryAsync(x => x.Key, x => x.Tax, ct);
        var docCounts = await _db.ClaimSupportingInfos.AsNoTracking()
            .Where(s => ids.Contains(s.ClaimId) && (s.DocumentId != null || s.AttachmentData != null))
            .GroupBy(s => s.ClaimId).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return new RcmPagedResult<RcmClaimSummaryDto>
        {
            Total = total,
            Page = Math.Max(1, query.Page),
            PageSize = query.Take,
            Items = items.Select(c =>
            {
                var dto = RcmProjections.ToSummary(c,
                    RcmInputMapper.IsPreAuth(c) && refs.TryGetValue(c.Id, out var r) ? r : null,
                    taxes.TryGetValue(c.Id, out var t) ? t : null);
                dto.DocumentCount = docCounts.TryGetValue(c.Id, out var n) ? n : 0;
                return dto;
            }).ToList(),
        };
    }

    public async Task<ClaimAdjudicationDto?> GetLatestAdjudicationAsync(string claimId, CancellationToken ct = default)
    {
        var latest = await LatestResponseQuery(claimId).AsNoTracking().FirstOrDefaultAsync(ct);
        return latest is null ? null : RcmProjections.ToDto(latest);
    }

    public async Task<ClaimTimelineDto?> GetTimelineAsync(string claimId, CancellationToken ct = default)
    {
        var claim = await _db.Claims.AsNoTracking().Include(c => c.Patient).Include(c => c.Insurer).Include(c => c.RelatedClaims)
            .FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim is null) return null;

        var outbound = await _db.NphiesTransactions.AsNoTracking()
            .Where(t => (t.EntityType == RcmEntityTypes.Claim && t.EntityId == claimId)
                        || (claim.BatchIdentifier != null && t.EntityType == RcmEntityTypes.Batch && t.EntityId == claim.BatchIdentifier))
            .ToListAsync(ct);
        var outboundIds = outbound.Select(t => t.Id).ToList();
        var inbound = await _db.NphiesTransactions.AsNoTracking()
            .Where(t => t.Direction == "inbound" && ((t.EntityType == RcmEntityTypes.Claim && t.EntityId == claimId) || (t.CorrelationId != null && outboundIds.Contains(t.CorrelationId))))
            .ToListAsync(ct);

        var responses = await _db.ClaimResponses.AsNoTracking().Include(r => r.Items).Where(r => r.ClaimId == claimId).OrderByDescending(r => r.ReceivedAt).ToListAsync(ct);
        var denials = await _db.ClaimDenials.AsNoTracking().Where(d => d.ClaimId == claimId).OrderBy(d => d.ItemSequence).ToListAsync(ct);
        var commRequests = await _db.CommunicationRequests.AsNoTracking().Where(r => r.ClaimId == claimId).ToListAsync(ct);
        var comms = await _db.Communications.AsNoTracking().Where(c => c.ClaimId == claimId).ToListAsync(ct);
        var payments = await _db.PaymentReconciliationDetails.AsNoTracking().Where(d => d.MatchedClaimId == claimId).ToListAsync(ct);
        var derived = await _db.Set<ClaimRelated>().AsNoTracking().Where(r => r.ReferencedClaimId == claimId).Select(r => new { r.ClaimId, r.Relationship }).ToListAsync(ct);

        var timeline = new ClaimTimelineDto
        {
            Claim = RcmProjections.ToSummary(claim, RcmInputMapper.IsPreAuth(claim) ? responses.FirstOrDefault()?.PreAuthRef : null, responses.FirstOrDefault()?.TotalTax),
            Transactions = outbound.Concat(inbound).DistinctBy(t => t.Id).OrderBy(t => t.SentAt).Select(RcmProjections.ToDto).ToList(),
            Adjudications = responses.Select(RcmProjections.ToDto).ToList(),
            Denials = denials.Select(d => RcmProjections.ToDto(d, claim.ClaimNumber)).ToList(),
            CommunicationRequests = commRequests.Select(RcmProjections.ToDto).ToList(),
            Communications = comms.Select(RcmProjections.ToDto).ToList(),
            Payments = payments.Select(p => RcmProjections.ToDto(p, claim.ClaimNumber)).ToList(),
        };
        timeline.Related.AddRange(claim.RelatedClaims.Select(r => new RelatedClaimDto { ClaimId = r.ReferencedClaimId, Identifier = r.RelatedClaimIdentifierValue, Relationship = r.Relationship }));
        timeline.Related.AddRange(derived.Select(d => new RelatedClaimDto { ClaimId = d.ClaimId, Relationship = $"{d.Relationship} (newer version)" }));
        return timeline;
    }

    // ===================== JSON preview / audit =====================

    private static readonly Hl7.Fhir.Serialization.FhirJsonSerializer PrettySerializer = new(new Hl7.Fhir.Serialization.SerializerSettings { Pretty = true });

    public async Task<BundlePreviewDto> PreviewBundleAsync(string claimId, CancellationToken ct = default)
    {
        var claim = await _mapper.LoadClaimGraphAsync(claimId, ct) ?? throw RcmWorkflowException.NotFound("Claim", claimId);
        var preAuth = RcmInputMapper.IsPreAuth(claim);
        Hl7.Fhir.Model.Bundle bundle;
        try
        {
            var input = await _mapper.MapClaimAsync(claim, ct);
            bundle = preAuth ? _builder.BuildPriorAuthRequest(input) : _builder.BuildClaimRequest(input);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            throw RcmWorkflowException.Invalid(ex.Message);
        }
        finally
        {
            // The mapper may allocate identifiers on tracked entities; a preview must never persist them.
            _db.ChangeTracker.Clear();
        }

        var json = PrettySerializer.SerializeToString(bundle);
        var dto = new BundlePreviewDto
        {
            ClaimId = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            Operation = preAuth ? RcmOperations.PriorAuth : RcmOperations.Claim,
            Json = json,
        };
        if (claim.Items.Count == 0) dto.Warnings.Add("[Error] The claim has no items; NPHIES requires at least one.");
        var nphiesStatus = claim.NphiesStatus ?? RcmClaimStatus.Draft;
        if (!Submittable.Contains(nphiesStatus))
            dto.Warnings.Add($"[Info] Claim is '{nphiesStatus}'; this is the bundle as it would be built now, not necessarily what was sent (see NPHIES messages).");
        var issues = _validator.Validate(bundle, System.Text.Encoding.UTF8.GetByteCount(json)).Issues;
        dto.Warnings.AddRange(issues
            .Select(i => $"[{i.Severity}] {i.RuleId}{(i.Expression is null ? "" : " " + i.Expression)}: {i.Message}"));
        dto.Issues.AddRange(issues.Select(ToIssueDto));
        return dto;
    }

    internal static BundleValidationIssueDto ToIssueDto(NphiesValidationIssue i) => new()
    {
        Severity = i.Severity.ToString(),
        RuleId = i.RuleId,
        Expression = i.Expression,
        Message = i.Message,
    };

    private async Task<byte[]?> LoadDocumentContentAsync(string documentId, CancellationToken ct)
    {
        var doc = await _db.ClaimDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId, ct);
        return doc is null ? null : await _documents.ReadAsync(doc.StorageProvider, doc.StorageKey, ct);
    }

    public async Task<MessageJsonDto?> GetTransactionJsonAsync(string transactionId, string? providerId, CancellationToken ct = default, bool includeAttachments = false)
    {
        var tx = await _db.NphiesTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == transactionId, ct);
        if (tx is null) return null;
        if (!string.IsNullOrWhiteSpace(providerId) && tx.ProviderId != null && tx.ProviderId != providerId) return null;

        var dto = new MessageJsonDto
        {
            TransactionId = tx.Id,
            Direction = tx.Direction,
            Operation = tx.Operation,
            EventCode = tx.EventCode,
            MessageId = tx.MessageId,
            ResponseEventCode = tx.ResponseEventCode,
            Error = tx.ErrorSummary,
        };

        var logs = _db.NphiesMessageLogs.AsNoTracking();
        var log = await logs.Where(l => l.MessageId == tx.MessageId).OrderByDescending(l => l.CreatedAt).FirstOrDefaultAsync(ct);
        if (log is not null)
        {
            dto.Source = "request";
            dto.RequestJson = log.RequestJson;
            dto.ResponseJson = log.ResponseJson;
        }
        else if ((log = await logs.Where(l => l.ResponseMessageId == tx.MessageId).OrderByDescending(l => l.CreatedAt).FirstOrDefaultAsync(ct)) is not null)
        {
            // Synchronous reply: the inbound message is this log's response; its request is ours.
            dto.Source = "sync-response";
            dto.RequestJson = log.RequestJson;
            dto.ResponseJson = log.ResponseJson;
        }
        else if (!string.IsNullOrEmpty(tx.MessageId)
                 && (log = await logs.Where(l => l.EventCode == "poll-request" && l.ResponseJson != null && l.ResponseJson.Contains(tx.MessageId))
                        .OrderByDescending(l => l.CreatedAt).FirstOrDefaultAsync(ct)) is not null)
        {
            // Deferred reply delivered inside a poll-response: show the poll request and the inner message only.
            dto.Source = "poll-response";
            dto.RequestJson = log.RequestJson;
            dto.ResponseJson = ExtractInnerMessage(log.ResponseJson!, tx.MessageId) ?? log.ResponseJson;
        }
        else
        {
            return dto;
        }

        if (includeAttachments)
            dto.RequestJson = await NPhies_FHIR_Integration.Infrastructure.Nphies.NphiesAuditAttachments.RehydrateAsync(dto.RequestJson, LoadDocumentContentAsync, ct);
        dto.RequestJson = Pretty(dto.RequestJson);
        dto.ResponseJson = Pretty(dto.ResponseJson);
        dto.HttpStatus = log.HttpStatus;
        dto.LoggedAt = log.CreatedAt;
        dto.Error ??= log.Error;
        return dto;
    }

    private static string? ExtractInnerMessage(string pollResponseJson, string messageId)
    {
        try
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(pollResponseJson);
            foreach (var bundle in Descendants(root).OfType<System.Text.Json.Nodes.JsonObject>()
                         .Where(o => (string?)o["resourceType"] == "Bundle" && o != root))
            {
                var header = bundle["entry"]?.AsArray().FirstOrDefault()?["resource"];
                if ((string?)header?["id"] == messageId || (string?)bundle["id"] == messageId)
                    return bundle.ToJsonString();
            }
        }
        catch (System.Text.Json.JsonException)
        {
        }
        return null;
    }

    private static IEnumerable<System.Text.Json.Nodes.JsonNode> Descendants(System.Text.Json.Nodes.JsonNode? node)
    {
        switch (node)
        {
            case System.Text.Json.Nodes.JsonObject o:
                yield return o;
                foreach (var child in o.Select(p => p.Value)) foreach (var d in Descendants(child)) yield return d;
                break;
            case System.Text.Json.Nodes.JsonArray a:
                foreach (var child in a) foreach (var d in Descendants(child)) yield return d;
                break;
        }
    }

    private static readonly System.Text.Json.JsonSerializerOptions PrettyJson = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static string? Pretty(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        try { return System.Text.Json.Nodes.JsonNode.Parse(json)?.ToJsonString(PrettyJson); }
        catch (System.Text.Json.JsonException) { return json; }
    }
}