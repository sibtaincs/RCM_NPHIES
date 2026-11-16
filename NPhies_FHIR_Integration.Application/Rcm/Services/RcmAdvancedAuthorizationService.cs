using System.Text.Json;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Nphies.Models;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

/// <summary>
/// Insurer-initiated (advanced) authorizations delivered by poll. The provider bills them by quoting the
/// authorization's preAuthRef on a claim (<see cref="UseInClaimAsync"/>).
/// </summary>
public interface IRcmAdvancedAuthorizationService
{
    Task<RcmPagedResult<AdvancedAuthorizationDto>> ListAsync(RcmQuery query, CancellationToken ct = default);
    Task<AdvancedAuthorizationDto?> GetAsync(string id, string? providerScope, CancellationToken ct = default);
    Task<AdvancedAuthorizationDto> UseInClaimAsync(string id, UseAdvancedAuthorizationDto request, string? providerScope, CancellationToken ct = default);

    /// <summary>Asks the payer to cancel the authorization (cancel-request with the authorization's identifier as the Task focus).</summary>
    Task<TaskResultDto> CancelAsync(string id, CancelClaimRequestDto request, string? providerScope, CancellationToken ct = default);

    /// <summary>
    /// Starts a prior authorization that changes this authorization: a draft with all of its items and diagnoses and the
    /// same preAuthRef. Sending it overrides the authorization (IG 7.9.1). The draft is completed in the claim editor.
    /// </summary>
    Task<SubmissionResultDto> AmendAsync(string id, string? providerScope, CancellationToken ct = default);
}

public sealed class RcmAdvancedAuthorizationService : IRcmAdvancedAuthorizationService
{
    private readonly ApplicationDbContext _db;
    private readonly IRcmInputMapper _mapper;
    private readonly INphiesBundleBuilder _builder;
    private readonly IRcmMessageDispatcher _dispatcher;

    public RcmAdvancedAuthorizationService(ApplicationDbContext db, IRcmInputMapper mapper, INphiesBundleBuilder builder, IRcmMessageDispatcher dispatcher)
    {
        _db = db;
        _mapper = mapper;
        _builder = builder;
        _dispatcher = dispatcher;
    }

    public async Task<RcmPagedResult<AdvancedAuthorizationDto>> ListAsync(RcmQuery query, CancellationToken ct = default)
    {
        var q = _db.AdvancedAuthorizations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(a => a.ServiceProviderId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) q = q.Where(a => a.InsurerId == query.InsurerId);
        if (!string.IsNullOrWhiteSpace(query.PatientId)) q = q.Where(a => a.PatientId == query.PatientId);
        if (query.From.HasValue) q = q.Where(a => a.ReceivedAt >= query.From);
        if (query.To.HasValue) q = q.Where(a => a.ReceivedAt <= query.To);
        var today = DateTime.UtcNow.Date;
        if (string.Equals(query.Status, "Expired", StringComparison.OrdinalIgnoreCase))
            q = q.Where(a => a.Status == "Active" && a.PreAuthPeriodEnd != null && a.PreAuthPeriodEnd < today);
        else if (string.Equals(query.Status, "Active", StringComparison.OrdinalIgnoreCase))
            q = q.Where(a => a.Status == "Active" && (a.PreAuthPeriodEnd == null || a.PreAuthPeriodEnd >= today));
        else if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(a => a.Status == query.Status);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.ReceivedAt).Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        return new RcmPagedResult<AdvancedAuthorizationDto> { Total = total, Page = Math.Max(1, query.Page), PageSize = query.Take, Items = items.Select(ToDto).ToList() };
    }

    public async Task<AdvancedAuthorizationDto?> GetAsync(string id, string? providerScope, CancellationToken ct = default)
    {
        var a = await _db.AdvancedAuthorizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return a is null || !InScope(a, providerScope) ? null : ToDto(a);
    }

    public async Task<AdvancedAuthorizationDto> UseInClaimAsync(string id, UseAdvancedAuthorizationDto request, string? providerScope, CancellationToken ct = default)
    {
        var auth = await _db.AdvancedAuthorizations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (auth is null || !InScope(auth, providerScope)) throw RcmWorkflowException.NotFound("Advanced authorization", id);
        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.Id == request.ClaimId, ct) ?? throw RcmWorkflowException.NotFound("Claim", request.ClaimId);
        if (providerScope != null && claim.ProviderId != providerScope) throw RcmWorkflowException.NotFound("Claim", request.ClaimId);

        if (auth.Status == "Cancelled") throw RcmWorkflowException.Conflict("The insurer cancelled this authorization.");
        if (auth.Status == "Used" && auth.UsedByClaimId != claim.Id) throw RcmWorkflowException.Conflict("This authorization was already used by another claim.");
        if (auth.PreAuthPeriodEnd.HasValue && auth.PreAuthPeriodEnd.Value.Date < DateTime.UtcNow.Date)
            throw RcmWorkflowException.Conflict($"Authorization expired on {auth.PreAuthPeriodEnd:yyyy-MM-dd}.");
        if (string.IsNullOrWhiteSpace(auth.PreAuthRef)) throw RcmWorkflowException.Invalid("Authorization has no preAuthRef to quote.");
        if (RcmInputMapper.IsPreAuth(claim)) throw RcmWorkflowException.Invalid("Advanced authorizations are billed on claims, not prior authorizations.");
        if (!RcmClaimEditorService.IsEditable(claim)) throw RcmWorkflowException.Conflict("Only unsent claims can quote an authorization.");
        if (auth.PatientId != null && auth.PatientId != claim.PatientId) throw RcmWorkflowException.Invalid("Authorization belongs to a different patient.");
        if (auth.InsurerId != null && auth.InsurerId != claim.InsurerId) throw RcmWorkflowException.Invalid("Authorization was issued by a different insurer.");

        var refs = (claim.PreAuthorizationRef ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (!refs.Contains(auth.PreAuthRef)) refs.Add(auth.PreAuthRef);
        claim.PreAuthorizationRef = string.Join(",", refs);
        claim.UpdatedAt = DateTime.UtcNow;
        auth.Status = "Used";
        auth.UsedByClaimId = claim.Id;
        auth.UsedAt ??= DateTime.UtcNow;
        auth.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToDto(auth);
    }

    public async Task<TaskResultDto> CancelAsync(string id, CancelClaimRequestDto request, string? providerScope, CancellationToken ct = default)
    {
        var auth = await _db.AdvancedAuthorizations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (auth is null || !InScope(auth, providerScope)) throw RcmWorkflowException.NotFound("Advanced authorization", id);
        if (string.IsNullOrWhiteSpace(request.Reason)) throw RcmWorkflowException.Invalid("A cancellation reason is required.");
        if (auth.Status is "Cancelled" or "Superseded") throw RcmWorkflowException.Conflict($"The authorization is already {auth.Status.ToLowerInvariant()}.");
        if (auth.Status == "Used") throw RcmWorkflowException.Conflict("A claim already quotes this authorization, so it cannot be cancelled.");
        // An authorization whose period has passed is already void; NPHIES says not to cancel it.
        if (auth.PreAuthPeriodEnd.HasValue && auth.PreAuthPeriodEnd.Value.Date < DateTime.UtcNow.Date)
            throw RcmWorkflowException.Conflict($"The authorization expired on {auth.PreAuthPeriodEnd:yyyy-MM-dd}, so it is no longer in force and must not be cancelled.");
        if (string.IsNullOrWhiteSpace(auth.IdentifierSystem) || string.IsNullOrWhiteSpace(auth.IdentifierValue))
            throw RcmWorkflowException.Conflict("The authorization has no identifier to cancel by.");

        var provider = auth.ServiceProviderId is null ? null : await _db.Organizations.FirstOrDefaultAsync(o => o.Id == auth.ServiceProviderId, ct);
        var insurer = auth.InsurerId is null ? null : await _db.Organizations.FirstOrDefaultAsync(o => o.Id == auth.InsurerId, ct);
        if (provider is null || insurer is null)
            throw RcmWorkflowException.Conflict("The authorization's provider or insurer is not on file, so the cancel request cannot be addressed.");

        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        var input = new ClaimTaskInput
        {
            TaskId = Guid.NewGuid().ToString("N"),
            Identifier = new NphiesIdentifier(_mapper.IdentifierSystem("task"), $"Cancel_{stamp}"),
            ClaimIdentifier = new NphiesIdentifier(auth.IdentifierSystem!, auth.IdentifierValue!),
            Provider = _mapper.MapOrganization(provider, NphiesOrganizationKind.Provider),
            Insurer = _mapper.MapOrganization(insurer, NphiesOrganizationKind.Insurer),
            Reason = request.Reason,
            CancelCode = "cancel",
        };
        var dispatch = await _dispatcher.DispatchAsync(new RcmDispatchRequest
        {
            Message = _builder.BuildCancelRequest(input),
            Operation = RcmOperations.Cancel,
            EntityType = RcmEntityTypes.AdvancedAuthorization,
            EntityId = auth.Id,
            RequestIdentifierSystem = input.ClaimIdentifier.System,
            RequestIdentifierValue = input.ClaimIdentifier.Value,
            ProviderId = provider.Id,
        }, ct);

        var task = dispatch.Response?.Task;
        if (dispatch.Status == RcmTransactionStatus.Completed && string.Equals(task?.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            auth.Status = "Cancelled";
            auth.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);

        return new TaskResultDto
        {
            ClaimId = auth.Id,
            Operation = RcmOperations.Cancel,
            TransactionId = dispatch.Transaction.Id,
            MessageId = dispatch.Transaction.MessageId,
            TransactionStatus = dispatch.Status,
            TaskStatus = task?.Status,
            ProcessingStatus = task?.ProcessingStatus,
            ClaimStatus = auth.Status,
            Errors = dispatch.Errors,
        };
    }

    public async Task<SubmissionResultDto> AmendAsync(string id, string? providerScope, CancellationToken ct = default)
    {
        var auth = await _db.AdvancedAuthorizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (auth is null || !InScope(auth, providerScope)) throw RcmWorkflowException.NotFound("Advanced authorization", id);
        if (auth.Status is "Cancelled" or "Superseded") throw RcmWorkflowException.Conflict($"The authorization is {auth.Status.ToLowerInvariant()} and cannot be changed.");
        if (string.IsNullOrWhiteSpace(auth.PreAuthRef)) throw RcmWorkflowException.Conflict("The authorization has no preAuthRef to keep.");
        if (auth.PatientId is null || auth.InsurerId is null || auth.ServiceProviderId is null)
            throw RcmWorkflowException.Conflict("The authorization's patient, insurer or provider is not on file. Add them to the master data first.");

        var coverage = await _db.Coverages.AsNoTracking()
            .Where(c => c.PatientId == auth.PatientId && c.InsurerId == auth.InsurerId && c.Status == "active")
            .OrderByDescending(c => c.CoverageEndDate).FirstOrDefaultAsync(ct)
            ?? throw RcmWorkflowException.Conflict("The patient has no active coverage with this insurer. Add the coverage first (Find insurance).");

        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var claim = new Claim
        {
            ClaimNumber = $"PA-APA-{stamp}",
            ClaimType = string.IsNullOrWhiteSpace(auth.ClaimType) ? "professional" : auth.ClaimType!,
            ClaimSubType = string.IsNullOrWhiteSpace(auth.SubType) ? "op" : auth.SubType!,
            Use = "preauthorization",
            CreatedDate = DateTime.UtcNow,
            ServicedPeriodStart = DateTime.UtcNow.Date,
            PatientId = auth.PatientId,
            CoverageId = coverage.Id,
            ProviderId = auth.ServiceProviderId,
            InsurerId = auth.InsurerId,
            PayeeType = "provider",
            // Keeping the authorization's preAuthRef is what makes the payer treat this request as a change to it.
            PreAuthorizationRef = auth.PreAuthRef,
            NphiesStatus = RcmClaimStatus.Draft,
        };

        // Every item of the authorization goes on the request: the new prior authorization overrides it as a whole.
        foreach (var item in ParseArray(auth.ItemsJson))
        {
            var code = Text(item, "Code");
            if (string.IsNullOrWhiteSpace(code)) continue;
            var quantity = Number(item, "Quantity") ?? 1m;
            var amount = AdjudicatedAmount(item, "submitted") ?? AdjudicatedAmount(item, "eligible") ?? AdjudicatedAmount(item, "benefit") ?? 0m;
            claim.Items.Add(new ClaimItem
            {
                ClaimId = claim.Id,
                Sequence = claim.Items.Count + 1,
                ProductOrServiceCode = code!,
                ProductOrServiceSystem = Text(item, "System"),
                ProductOrServiceDisplay = Text(item, "Display"),
                Quantity = quantity,
                UnitPrice = quantity == 0 ? amount : Math.Round(amount / quantity, 2),
                Net = amount,
                PatientShare = 0,
                DiagnosisSequence = "1",
                ServicedDate = DateTime.UtcNow.Date,
            });
        }
        if (claim.Items.Count == 0) throw RcmWorkflowException.Conflict("The authorization lists no items to carry over.");
        claim.Total = claim.Items.Sum(i => i.Net ?? 0m);

        foreach (var diagnosis in ParseArray(auth.DiagnosesJson))
        {
            var code = Text(diagnosis, "Code");
            if (string.IsNullOrWhiteSpace(code)) continue;
            claim.Diagnoses.Add(new ClaimDiagnosis
            {
                ClaimId = claim.Id,
                Sequence = claim.Diagnoses.Count + 1,
                DiagnosisCode = code!,
                DiagnosisSystem = Text(diagnosis, "System"),
                DiagnosisDisplay = Text(diagnosis, "Display"),
                DiagnosisType = claim.Diagnoses.Count == 0 ? "principal" : "secondary",
            });
        }

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync(ct);
        return new SubmissionResultDto
        {
            ClaimId = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            Use = claim.Use,
            Operation = "draft",
            TransactionStatus = "NotSent",
            ClaimStatus = claim.NphiesStatus,
        };
    }

    private static IEnumerable<JsonElement> ParseArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Enumerable.Empty<JsonElement>();
        var root = JsonDocument.Parse(json).RootElement.Clone();
        return root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToList() : Enumerable.Empty<JsonElement>();
    }

    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static decimal? Number(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;

    private static decimal? AdjudicatedAmount(JsonElement item, string category)
    {
        if (!item.TryGetProperty("Adjudication", out var list) || list.ValueKind != JsonValueKind.Array) return null;
        foreach (var a in list.EnumerateArray())
            if (Text(a, "Category") == category && Number(a, "Amount") is { } amount) return amount;
        return null;
    }

    private static bool InScope(AdvancedAuthorization a, string? providerScope) =>
        providerScope is null || a.ServiceProviderId == providerScope;

    public static AdvancedAuthorizationDto ToDto(AdvancedAuthorization a) => new()
    {
        Id = a.Id,
        Identifier = a.IdentifierValue,
        PreAuthRef = a.PreAuthRef,
        PeriodStart = a.PreAuthPeriodStart,
        PeriodEnd = a.PreAuthPeriodEnd,
        Reason = a.Reason,
        ClaimType = a.ClaimType,
        SubType = a.SubType,
        Outcome = a.Outcome,
        AdjudicationOutcome = a.AdjudicationOutcome,
        Disposition = a.Disposition,
        PatientId = a.PatientId,
        PatientIdentifier = a.PatientIdentifier,
        PatientName = a.PatientName,
        InsurerId = a.InsurerId,
        InsurerLicense = a.InsurerLicense,
        ServiceProviderId = a.ServiceProviderId,
        ServiceProviderLicense = a.ServiceProviderLicense,
        ReferringProviderLicense = a.ReferringProviderLicense,
        TotalEligible = a.TotalEligible,
        TotalBenefit = a.TotalBenefit,
        Currency = a.Currency,
        ReissueReason = a.ReissueReason,
        Status = a.Status == "Active" && a.PreAuthPeriodEnd.HasValue && a.PreAuthPeriodEnd.Value.Date < DateTime.UtcNow.Date ? "Expired" : a.Status,
        UsedByClaimId = a.UsedByClaimId,
        UsedAt = a.UsedAt,
        ReceivedAt = a.ReceivedAt,
        Items = Parse(a.ItemsJson),
        Diagnoses = Parse(a.DiagnosesJson),
    };

    private static JsonElement? Parse(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonDocument.Parse(json).RootElement.Clone();
}
