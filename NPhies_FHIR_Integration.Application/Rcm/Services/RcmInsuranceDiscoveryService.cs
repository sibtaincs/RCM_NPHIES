using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Nphies.Discovery;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

/// <summary>
/// Insurance discovery for the front office: looks a patient's policies up at NPHIES by identity number
/// (Check Insurance API), matches each one to a payer and a coverage already on file, and can add the coverage.
/// </summary>
public interface IRcmInsuranceDiscoveryService
{
    Task<InsuranceDiscoveryDto> DiscoverAsync(InsuranceDiscoveryRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Looks the policy up again and saves it as a coverage of the patient. The policy data comes from NPHIES, not from
    /// the caller.
    /// </summary>
    Task<DiscoveredCoverageDto> AddCoverageAsync(AddDiscoveredCoverageDto request, CancellationToken ct = default);
}

public sealed class RcmInsuranceDiscoveryService : IRcmInsuranceDiscoveryService
{
    /// <summary>subscriber-relationship codes a dependent can have (everything except <c>self</c>).</summary>
    public static readonly string[] DependentRelationships = { "child", "spouse", "parent", "common", "injured", "other" };

    private readonly ApplicationDbContext _db;
    private readonly INphiesInsuranceDiscoveryClient _client;

    public RcmInsuranceDiscoveryService(ApplicationDbContext db, INphiesInsuranceDiscoveryClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task<InsuranceDiscoveryDto> DiscoverAsync(InsuranceDiscoveryRequestDto request, CancellationToken ct = default)
    {
        var (patient, key, systemType) = await ResolveAsync(request.PatientId, request.PatientKey, request.SystemType, ct);
        var result = await _client.CheckAsync(key, systemType, ct);

        var dto = new InsuranceDiscoveryDto
        {
            PatientId = patient?.Id,
            PatientKey = key,
            SystemType = (int)systemType,
            Found = result.Policies.Count > 0,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            Simulated = result.Simulated,
        };
        if (result.Policies.Count == 0) return dto;

        var insurers = await _db.Organizations.AsNoTracking().Where(o => o.OrganizationType == "ins")
            .Select(o => new InsurerRow(o.Id, o.OrganizationName, o.LicenseNumber, o.NphiesPayerId)).ToListAsync(ct);
        var coverages = patient is null
            ? new List<Coverage>()
            : await _db.Coverages.AsNoTracking().Where(c => c.PatientId == patient.Id).ToListAsync(ct);

        foreach (var policy in result.Policies)
        {
            var insurer = MatchInsurer(insurers, policy);
            var existing = coverages.FirstOrDefault(c => !string.IsNullOrWhiteSpace(policy.PolicyNumber)
                && string.Equals(c.PolicyNumber, policy.PolicyNumber, StringComparison.OrdinalIgnoreCase)
                && (insurer is null || c.InsurerId == insurer.Id));
            dto.Policies.Add(new DiscoveredPolicyDto
            {
                PolicyNumber = policy.PolicyNumber,
                InsuranceCompanyEn = policy.InsuranceCompanyEn,
                InsuranceCompanyAr = policy.InsuranceCompanyAr,
                InsuranceCompanyId = policy.InsuranceCompanyId,
                IssueDate = policy.IssueDate,
                ExpiryDate = policy.ExpiryDate,
                Expired = policy.ExpiryDate.HasValue && policy.ExpiryDate.Value.Date < DateTime.UtcNow.Date,
                ClassName = policy.ClassName,
                NetworkId = policy.NetworkId,
                DeductibleRate = policy.DeductibleRate,
                MaxLimit = policy.MaxLimit,
                BeneficiaryType = policy.BeneficiaryType switch { "1" => "main", "2" => "dependent", var other => other },
                BeneficiaryNumber = policy.BeneficiaryNumber,
                SponsorNumber = policy.SponsorNumber,
                PolicyHolder = policy.PolicyHolder,
                InsuranceStatus = policy.InsuranceStatus,
                InsuranceType = policy.InsuranceType,
                Name = policy.Name,
                Gender = policy.Gender,
                Nationality = policy.Nationality,
                DateOfBirth = policy.DateOfBirth,
                MatchedInsurerId = insurer?.Id,
                MatchedInsurerName = insurer?.Name,
                ExistingCoverageId = existing?.Id,
            });
        }
        return dto;
    }

    public async Task<DiscoveredCoverageDto> AddCoverageAsync(AddDiscoveredCoverageDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatientId)) throw RcmWorkflowException.Invalid("patientId is required.");
        if (string.IsNullOrWhiteSpace(request.PolicyNumber)) throw RcmWorkflowException.Invalid("policyNumber is required.");
        var (patient, key, systemType) = await ResolveAsync(request.PatientId, request.PatientKey, request.SystemType, ct);

        var result = await _client.CheckAsync(key, systemType, ct);
        if (result.IsError)
            throw RcmWorkflowException.Conflict($"NPHIES insurance discovery failed ({result.ErrorCode}): {result.ErrorMessage}");
        var policy = result.Policies.FirstOrDefault(p => string.Equals(p.PolicyNumber, request.PolicyNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                     ?? throw RcmWorkflowException.NotFound("Policy", request.PolicyNumber);

        var insurers = await _db.Organizations.AsNoTracking().Where(o => o.OrganizationType == "ins")
            .Select(o => new InsurerRow(o.Id, o.OrganizationName, o.LicenseNumber, o.NphiesPayerId)).ToListAsync(ct);
        var insurer = !string.IsNullOrWhiteSpace(request.InsurerId)
            ? insurers.FirstOrDefault(i => i.Id == request.InsurerId) ?? throw RcmWorkflowException.NotFound("Insurer", request.InsurerId!)
            : MatchInsurer(insurers, policy)
              ?? throw RcmWorkflowException.Conflict(
                  $"No payer on file matches insurance company {policy.InsuranceCompanyId} ({policy.InsuranceCompanyEn}). Add the payer to the master data, or pass insurerId.");

        var existing = await _db.Coverages.FirstOrDefaultAsync(c => c.PatientId == patient!.Id && c.InsurerId == insurer.Id && c.PolicyNumber == policy.PolicyNumber, ct);
        if (existing != null)
            return new DiscoveredCoverageDto { CoverageId = existing.Id, InsurerId = insurer.Id, PolicyNumber = existing.PolicyNumber, Created = false };

        var start = policy.IssueDate?.Date ?? DateTime.UtcNow.Date;
        var end = policy.ExpiryDate?.Date ?? start.AddYears(1);
        var dependent = policy.BeneficiaryType == "2";

        // The API says only "main" or "dependent". For a dependent the front desk says how the patient is related to
        // the policy holder (a child on the mother's or father's policy is "child") and, when known, who that is.
        var relationship = "self";
        string? subscriberPatientId = null;
        if (dependent)
        {
            relationship = string.IsNullOrWhiteSpace(request.Relationship) ? "other" : request.Relationship.Trim().ToLowerInvariant();
            if (!DependentRelationships.Contains(relationship))
                throw RcmWorkflowException.Invalid($"relationship must be one of: {string.Join(", ", DependentRelationships)}. This policy lists the patient as a dependent.");
            if (!string.IsNullOrWhiteSpace(request.SubscriberPatientId))
            {
                if (request.SubscriberPatientId == patient!.Id)
                    throw RcmWorkflowException.Invalid("The policy holder of a dependent's policy cannot be the patient themselves.");
                if (!await _db.Patients.AnyAsync(p => p.Id == request.SubscriberPatientId, ct))
                    throw RcmWorkflowException.NotFound("Policy holder (patient)", request.SubscriberPatientId!);
                subscriberPatientId = request.SubscriberPatientId;
            }
        }

        var coverage = new Coverage
        {
            PatientId = patient!.Id,
            InsurerId = insurer.Id,
            PolicyNumber = policy.PolicyNumber!,
            MemberID = policy.BeneficiaryNumber ?? policy.IdentityNumber ?? key,
            CoverageType = "EHCPOL",
            SubscriberRelationship = relationship,
            RelationToSubscriber = relationship,
            SubscriberPatientId = subscriberPatientId,
            SubscriberMRN = patient.MRN,
            Status = end < DateTime.UtcNow.Date ? "cancelled" : "active",
            CoverageStartDate = start,
            CoverageEndDate = end,
            CoverageClassType = string.IsNullOrWhiteSpace(policy.ClassName) ? null : "class",
            CoverageClassValue = policy.ClassName,
            CoverageClass = policy.ClassName,
            PlanCode = policy.NetworkId,
            CoinsurancePercentValue = policy.DeductibleRate,
            MaxCopay = policy.MaxLimit,
            MaxCopayCurrency = policy.MaxLimit.HasValue ? "SAR" : null,
        };
        _db.Coverages.Add(coverage);
        await _db.SaveChangesAsync(ct);
        return new DiscoveredCoverageDto { CoverageId = coverage.Id, InsurerId = insurer.Id, PolicyNumber = coverage.PolicyNumber, Created = true };
    }

    private async Task<(Patient? Patient, string Key, InsuranceSystemType SystemType)> ResolveAsync(string? patientId, string? patientKey, int systemType, CancellationToken ct)
    {
        Patient? patient = null;
        if (!string.IsNullOrWhiteSpace(patientId))
            patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId, ct) ?? throw RcmWorkflowException.NotFound("Patient", patientId);

        var key = !string.IsNullOrWhiteSpace(patientKey) ? patientKey.Trim() : patient?.NationalId?.Trim();
        var type = (InsuranceSystemType)(systemType == 0 ? 1 : systemType);
        if (NphiesInsuranceDiscoveryClient.Validate(key, type) is { } invalid)
            throw RcmWorkflowException.Invalid(invalid.ErrorCode switch
            {
                NphiesInsuranceDiscoveryErrors.KeyRequired => "A patient key is required: the patient has no identity number on file, so pass patientKey.",
                NphiesInsuranceDiscoveryErrors.UndefinedSystemType => "systemType must be 1 (nationals and residents), 2 (visitors), 3 (tourists), 4 (premium residency) or 5 (Hajj and Umrah).",
                _ => "For nationals and residents the patient key is the 10-digit national id or Iqama number. For a passport, visa or border number choose another system type.",
            });
        return (patient, key!, type);
    }

    private sealed record InsurerRow(string Id, string Name, string? License, string? NphiesPayerId);

    /// <summary>By the payer's id first, then by its English name.</summary>
    private static InsurerRow? MatchInsurer(List<InsurerRow> insurers, DiscoveredPolicy policy)
    {
        if (!string.IsNullOrWhiteSpace(policy.InsuranceCompanyId))
        {
            var byId = insurers.FirstOrDefault(i => string.Equals(i.NphiesPayerId, policy.InsuranceCompanyId, StringComparison.OrdinalIgnoreCase)
                                                    || string.Equals(i.License, policy.InsuranceCompanyId, StringComparison.OrdinalIgnoreCase));
            if (byId != null) return byId;
        }
        return string.IsNullOrWhiteSpace(policy.InsuranceCompanyEn) ? null
            : insurers.FirstOrDefault(i => string.Equals(i.Name?.Trim(), policy.InsuranceCompanyEn.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
