using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Nphies.Models;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

public interface IRcmEligibilityService
{
    Task<EligibilityCheckDto> CheckAsync(EligibilityCheckRequestDto request, CancellationToken ct = default);
    /// <summary>
    /// Records eligibility confirmed on a payer portal or by phone. Nothing is sent to NPHIES; the reference and date
    /// go on later claims and prior authorizations (<see cref="ApplyOfflineToClaimAsync"/>).
    /// </summary>
    Task<EligibilityCheckDto> RecordOfflineAsync(OfflineEligibilityRequestDto request, CancellationToken ct = default);

    /// <summary>Copies an offline record's reference and date onto a claim or prior authorization that is not sent yet.</summary>
    Task<EligibilityCheckDto> ApplyOfflineToClaimAsync(string checkId, string claimId, string? providerScope, CancellationToken ct = default);

    Task<EligibilityCheckDto?> GetAsync(string id, CancellationToken ct = default);
    Task<RcmPagedResult<EligibilityCheckDto>> ListAsync(RcmQuery query, CancellationToken ct = default);
    Task<EligibilityCheckDto?> GetLatestForPatientAsync(string patientId, string? insurerId, CancellationToken ct = default);
}

/// <summary>CoverageEligibilityRequest (benefits / validation / discovery) against NPHIES with persisted results.</summary>
public sealed class RcmEligibilityService : IRcmEligibilityService
{
    private static readonly HashSet<string> AllowedPurposes = new(StringComparer.Ordinal) { "benefits", "validation", "discovery" };

    /// <summary>A newborn may be checked against the mother's coverage for this many days after birth (IG 6.5).</summary>
    public const int NewbornWindowDays = 90;

    private readonly ApplicationDbContext _db;
    private readonly IRcmInputMapper _mapper;
    private readonly INphiesBundleBuilder _builder;
    private readonly IRcmMessageDispatcher _dispatcher;
    private readonly IRcmResponseIngestor _ingestor;

    public RcmEligibilityService(ApplicationDbContext db, IRcmInputMapper mapper, INphiesBundleBuilder builder, IRcmMessageDispatcher dispatcher, IRcmResponseIngestor ingestor)
    {
        _db = db;
        _mapper = mapper;
        _builder = builder;
        _dispatcher = dispatcher;
        _ingestor = ingestor;
    }

    public async Task<EligibilityCheckDto> CheckAsync(EligibilityCheckRequestDto request, CancellationToken ct = default)
    {
        var purposes = request.Purposes.Select(p => p.Trim().ToLowerInvariant()).Where(p => p.Length > 0).Distinct().ToList();
        if (purposes.Count == 0) purposes.Add("validation");
        var bad = purposes.Where(p => !AllowedPurposes.Contains(p)).ToList();
        if (bad.Count > 0) throw RcmWorkflowException.Invalid($"Unsupported purpose(s): {string.Join(", ", bad)}. Use benefits, validation or discovery.");

        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, ct) ?? throw RcmWorkflowException.NotFound("Patient", request.PatientId);
        var provider = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == request.ProviderId, ct) ?? throw RcmWorkflowException.NotFound("Provider", request.ProviderId);

        Coverage? coverage = null;
        if (!string.IsNullOrWhiteSpace(request.CoverageId))
        {
            coverage = await _db.Coverages.FirstOrDefaultAsync(c => c.Id == request.CoverageId, ct) ?? throw RcmWorkflowException.NotFound("Coverage", request.CoverageId!);
            // A newborn is checked against the mother's coverage (IG 6.5); otherwise the coverage must be the patient's.
            if (coverage.PatientId != patient.Id && !request.IsNewborn)
                throw RcmWorkflowException.Invalid("Coverage does not belong to the patient. For a newborn on the mother's coverage, set isNewborn.");
        }
        else if (!purposes.Contains("discovery"))
        {
            coverage = await _db.Coverages.Where(c => c.PatientId == patient.Id && c.Status == "active" && (request.InsurerId == null || c.InsurerId == request.InsurerId))
                .OrderByDescending(c => c.CoverageEndDate).FirstOrDefaultAsync(ct);
            if (coverage is null) throw RcmWorkflowException.Invalid("Patient has no active coverage; pass coverageId or use purpose 'discovery'.");
        }

        var insurerId = coverage?.InsurerId ?? request.InsurerId;
        if (string.IsNullOrWhiteSpace(insurerId)) throw RcmWorkflowException.Invalid("insurerId is required for discovery without coverage.");
        var insurer = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == insurerId, ct) ?? throw RcmWorkflowException.NotFound("Insurer", insurerId!);

        LocationInput? facility = null;
        if (!string.IsNullOrWhiteSpace(request.LocationId))
        {
            var loc = await _db.Locations.FirstOrDefaultAsync(l => l.Id == request.LocationId, ct) ?? throw RcmWorkflowException.NotFound("Location", request.LocationId!);
            facility = new LocationInput { Id = RcmInputMapper.SafeId(loc.Id), License = loc.LocationLicense ?? string.Empty, Name = loc.LocationName };
        }

        var serviced = (request.ServicedDate ?? DateTime.UtcNow).Date;
        if (request.IsNewborn)
        {
            // A newborn may use the mother's coverage for 90 days from birth; the payer rejects anything later.
            var lastDay = (request.ServicedPeriodEnd ?? serviced).Date;
            var limit = patient.DateOfBirth.Date.AddDays(NewbornWindowDays);
            if (patient.DateOfBirth != default && lastDay > limit)
                throw RcmWorkflowException.Invalid(
                    $"The service date {lastDay:yyyy-MM-dd} is more than {NewbornWindowDays} days after the date of birth {patient.DateOfBirth:yyyy-MM-dd}. " +
                    "The newborn can no longer use the mother's coverage; check eligibility with the baby's own policy.");
            if (patient.DateOfBirth != default && lastDay < patient.DateOfBirth.Date)
                throw RcmWorkflowException.Invalid("The service date is before the newborn's date of birth.");
        }
        var stamp =DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        var check = new EligibilityCheck
        {
            PatientId = patient.Id,
            CoverageId = coverage?.Id,
            ProviderId = provider.Id,
            InsurerId = insurer.Id,
            Purposes = string.Join(",", purposes),
            IsNewborn = request.IsNewborn,
            IsTransfer = request.IsTransfer,
            ServicedDate = serviced,
            RequestIdentifierSystem = _mapper.IdentifierSystem("coverageeligibilityrequest"),
            RequestIdentifierValue = $"req_elig_{stamp}",
        };
        _db.EligibilityChecks.Add(check);
        await _db.SaveChangesAsync(ct);

        var input = new EligibilityRequestInput
        {
            RequestId = RcmInputMapper.SafeId(check.Id),
            Identifier = new NphiesIdentifier(check.RequestIdentifierSystem, check.RequestIdentifierValue),
            Purposes = purposes,
            Patient = _mapper.MapPatient(patient),
            Provider = _mapper.MapOrganization(provider, NphiesOrganizationKind.Provider),
            Insurer = _mapper.MapOrganization(insurer, NphiesOrganizationKind.Insurer),
            Coverage = coverage is null ? null : _mapper.MapCoverage(coverage, patient),
            Facility = facility,
            Newborn = request.IsNewborn ? true : null,
            Transfer = request.IsTransfer ? true : null,
        };
        if (coverage != null)
            (input.Subscriber, input.Policyholder) = await _mapper.LoadCoveragePartiesAsync(coverage, patient, input.Coverage!, ct);
        if (request.ServicedPeriodEnd.HasValue)
            input.ServicedPeriod = new NphiesPeriod(RcmInputMapper.Date(serviced), RcmInputMapper.Date(request.ServicedPeriodEnd));
        else
            input.ServicedDate = RcmInputMapper.Date(serviced);

        Hl7.Fhir.Model.Bundle bundle;
        try { bundle = _builder.BuildEligibilityRequest(input); }
        catch (ArgumentException ex)
        {
            check.Status = RcmTransactionStatus.ValidationFailed;
            check.ErrorsJson = System.Text.Json.JsonSerializer.Serialize(new[] { ex.Message });
            await _db.SaveChangesAsync(ct);
            throw RcmWorkflowException.Invalid(ex.Message);
        }

        var dispatch = await _dispatcher.DispatchAsync(new RcmDispatchRequest
        {
            Message = bundle,
            Operation = RcmOperations.Eligibility,
            EntityType = RcmEntityTypes.EligibilityCheck,
            EntityId = check.Id,
            RequestIdentifierSystem = check.RequestIdentifierSystem,
            RequestIdentifierValue = check.RequestIdentifierValue,
            ProviderId = provider.Id,
        }, ct);

        check.MessageId = dispatch.Transaction.MessageId;
        check.Status = dispatch.Status;
        if (dispatch.Response?.Eligibility != null)
        {
            await _ingestor.IngestAsync(dispatch.Response, dispatch.Transaction, ct);
            check.Status = dispatch.Status == RcmTransactionStatus.Error ? RcmTransactionStatus.Error : RcmTransactionStatus.Completed;
        }
        if (dispatch.Errors.Count > 0 && check.ErrorsJson is null)
            check.ErrorsJson = System.Text.Json.JsonSerializer.Serialize(dispatch.Errors);
        check.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return RcmProjections.ToDto(check);
    }

    public async Task<EligibilityCheckDto> RecordOfflineAsync(OfflineEligibilityRequestDto request, CancellationToken ct = default)
    {
        var reference = request.Reference?.Trim();
        if (string.IsNullOrEmpty(reference)) throw RcmWorkflowException.Invalid("The reference the payer gave is required.");
        if (reference.Length > 200) throw RcmWorkflowException.Invalid("The reference is too long (200 characters at most).");
        if (request.Date == default) throw RcmWorkflowException.Invalid("The date the payer confirmed eligibility is required.");
        if (request.Date > DateTime.UtcNow.AddDays(1)) throw RcmWorkflowException.Invalid("The offline eligibility date cannot be in the future.");

        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, ct) ?? throw RcmWorkflowException.NotFound("Patient", request.PatientId);
        var provider = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == request.ProviderId, ct) ?? throw RcmWorkflowException.NotFound("Provider", request.ProviderId);

        Coverage? coverage;
        if (!string.IsNullOrWhiteSpace(request.CoverageId))
            coverage = await _db.Coverages.FirstOrDefaultAsync(c => c.Id == request.CoverageId, ct) ?? throw RcmWorkflowException.NotFound("Coverage", request.CoverageId!);
        else
            coverage = await _db.Coverages.Where(c => c.PatientId == patient.Id && c.Status == "active" && (request.InsurerId == null || c.InsurerId == request.InsurerId))
                .OrderByDescending(c => c.CoverageEndDate).FirstOrDefaultAsync(ct);

        var insurerId = coverage?.InsurerId ?? request.InsurerId;
        if (string.IsNullOrWhiteSpace(insurerId)) throw RcmWorkflowException.Invalid("Pass coverageId or insurerId: the patient has no active coverage to take the insurer from.");
        var insurer = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == insurerId, ct) ?? throw RcmWorkflowException.NotFound("Insurer", insurerId!);

        var check = new EligibilityCheck
        {
            PatientId = patient.Id,
            CoverageId = coverage?.Id,
            ProviderId = provider.Id,
            InsurerId = insurer.Id,
            Purposes = "validation",
            ServicedDate = request.Date.Date,
            RequestIdentifierSystem = _mapper.IdentifierSystem("coverageeligibilityrequest"),
            RequestIdentifierValue = $"offline_{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)}",
            Status = RcmTransactionStatus.Completed,
            Outcome = "offline",
            IsEligible = true,
            Disposition = string.IsNullOrWhiteSpace(request.Notes) ? "Eligibility confirmed outside NPHIES." : request.Notes.Trim()[..Math.Min(request.Notes.Trim().Length, 1000)],
            IsOffline = true,
            OfflineReference = reference,
            OfflineDate = request.Date,
            RespondedAt = request.Date,
        };
        _db.EligibilityChecks.Add(check);
        await _db.SaveChangesAsync(ct);
        return RcmProjections.ToDto(check);
    }

    public async Task<EligibilityCheckDto> ApplyOfflineToClaimAsync(string checkId, string claimId, string? providerScope, CancellationToken ct = default)
    {
        var check = await _db.EligibilityChecks.AsNoTracking().FirstOrDefaultAsync(c => c.Id == checkId, ct);
        if (check is null || (!string.IsNullOrWhiteSpace(providerScope) && check.ProviderId != providerScope))
            throw RcmWorkflowException.NotFound("Eligibility check", checkId);
        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim is null || (!string.IsNullOrWhiteSpace(providerScope) && claim.ProviderId != providerScope))
            throw RcmWorkflowException.NotFound("Claim", claimId);

        if (!check.IsOffline || string.IsNullOrWhiteSpace(check.OfflineReference))
            throw RcmWorkflowException.Conflict("Only an offline eligibility record has a reference to put on a claim. A check made through NPHIES needs none.");
        if (check.PatientId != claim.PatientId)
            throw RcmWorkflowException.Conflict("The eligibility record is for a different patient.");
        if (check.InsurerId != claim.InsurerId)
            throw RcmWorkflowException.Conflict("The eligibility record is with a different insurer than the claim.");
        if ((claim.NphiesStatus ?? RcmClaimStatus.Draft) is not (RcmClaimStatus.Draft or RcmClaimStatus.ValidationFailed))
            throw RcmWorkflowException.Conflict($"The claim is {claim.NphiesStatus}: a sent claim cannot be changed.");

        claim.EligibilityOfflineReference = check.OfflineReference;
        claim.EligibilityOfflineDate = check.OfflineDate;
        claim.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return RcmProjections.ToDto(check);
    }

    public async Task<EligibilityCheckDto?> GetAsync(string id, CancellationToken ct = default)
    {
        var e = await _db.EligibilityChecks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return e is null ? null : RcmProjections.ToDto(e);
    }

    public async Task<EligibilityCheckDto?> GetLatestForPatientAsync(string patientId, string? insurerId, CancellationToken ct = default)
    {
        var e = await _db.EligibilityChecks.AsNoTracking()
            .Where(x => x.PatientId == patientId && (insurerId == null || x.InsurerId == insurerId) && x.Status == RcmTransactionStatus.Completed)
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        return e is null ? null : RcmProjections.ToDto(e);
    }

    public async Task<RcmPagedResult<EligibilityCheckDto>> ListAsync(RcmQuery query, CancellationToken ct = default)
    {
        var q = _db.EligibilityChecks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(x => x.ProviderId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) q = q.Where(x => x.InsurerId == query.InsurerId);
        if (!string.IsNullOrWhiteSpace(query.PatientId)) q = q.Where(x => x.PatientId == query.PatientId);
        if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(x => x.Status == query.Status);
        if (query.From.HasValue) q = q.Where(x => x.CreatedAt >= query.From);
        if (query.To.HasValue) q = q.Where(x => x.CreatedAt <= query.To);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreatedAt).Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        return new RcmPagedResult<EligibilityCheckDto> { Total = total, Page = Math.Max(1, query.Page), PageSize = query.Take, Items = items.Select(RcmProjections.ToDto).ToList() };
    }
}
