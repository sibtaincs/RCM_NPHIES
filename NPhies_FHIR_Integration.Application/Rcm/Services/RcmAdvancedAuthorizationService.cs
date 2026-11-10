using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
}

public sealed class RcmAdvancedAuthorizationService : IRcmAdvancedAuthorizationService
{
    private readonly ApplicationDbContext _db;

    public RcmAdvancedAuthorizationService(ApplicationDbContext db) => _db = db;

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
