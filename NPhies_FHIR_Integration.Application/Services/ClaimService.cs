using AutoMapper;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Repositories;

namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// The claim review gate: technical and medical review decisions, the review queues, and
/// submission (through the RCM workflow) once both reviews are approved.
/// </summary>
public interface IClaimService
{
    /// <summary>
    /// Get details required by the claims review workflow.
    /// </summary>
    Task<ClaimWorkflowDetailsDto?> GetClaimWorkflowDetailsAsync(string claimId);

    /// <summary>
    /// Record a technical review decision (coding/billing correctness) on a claim.
    /// </summary>
    Task<ClaimReviewStatusDto> TechnicalReviewAsync(string claimId, ClaimReviewDto dto);

    /// <summary>
    /// Record a medical review decision (clinical correctness / medical necessity) on a claim.
    /// </summary>
    Task<ClaimReviewStatusDto> MedicalReviewAsync(string claimId, ClaimReviewDto dto);

    /// <summary>
    /// Get the current dual-approval review status of a claim.
    /// </summary>
    Task<ClaimReviewStatusDto?> GetReviewStatusAsync(string claimId);

    /// <summary>
    /// Get claims awaiting a given review stage. Stage: "technical" or "medical".
    /// </summary>
    Task<IEnumerable<ClaimDto>> GetClaimsAwaitingReviewAsync(string stage);

    /// <summary>
    /// Get active claims with both review stages approved and ready for submission.
    /// </summary>
    Task<IEnumerable<ClaimDto>> GetClaimsReadyForSubmissionAsync();

    /// <summary>
    /// Mark a claim as submitted to NPHIES. Enforces the dual-approval gate:
    /// throws InvalidOperationException if the claim is not approved for submission.
    /// </summary>
    Task<ClaimDto> SubmitClaimAsync(string claimId, string? submittedBy = null);
}

/// <summary>
/// Implementation of <see cref="IClaimService"/>.
/// </summary>
public class ClaimService : IClaimService
{
    private readonly IClaimRepository _claimRepository;
    private readonly IMapper _mapper;
    private readonly NPhies_FHIR_Integration.Application.Rcm.Services.IRcmClaimWorkflowService _rcmClaims;

    /// <summary>
    /// Constructor with dependencies. SubmitClaimAsync sends through the RCM workflow layer.
    /// </summary>
    public ClaimService(
        IClaimRepository claimRepository,
        IMapper mapper,
        NPhies_FHIR_Integration.Application.Rcm.Services.IRcmClaimWorkflowService rcmClaims)
    {
        _claimRepository = claimRepository ?? throw new ArgumentNullException(nameof(claimRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _rcmClaims = rcmClaims ?? throw new ArgumentNullException(nameof(rcmClaims));
    }

    public async Task<ClaimWorkflowDetailsDto?> GetClaimWorkflowDetailsAsync(string claimId)
    {
        var claim = await _claimRepository.GetWithDetailsAsync(claimId);
        if (claim is null) return null;

        string? SupportingValue(string category) => claim.SupportingInfo
            .FirstOrDefault(info => string.Equals(info.Category, category, StringComparison.OrdinalIgnoreCase))
            ?.StringValue;

        var response = claim.ClaimResponses.OrderByDescending(item => item.CreatedAt).FirstOrDefault();
        var responseItems = response?.AddItems ?? [];

        DateTime? triageDate = DateTime.TryParse(SupportingValue("triage-date"), out var parsedTriageDate)
            ? parsedTriageDate
            : null;
        int? patientAge = claim.Patient is null
            ? null
            : CalculateAge(claim.Patient.DateOfBirth, claim.Encounter?.PeriodStart ?? DateTime.UtcNow);

        return new ClaimWorkflowDetailsDto
        {
            Id = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            Status = claim.Status,
            ClaimType = claim.ClaimType,
            ClaimSubType = claim.ClaimSubType,
            Priority = claim.Priority,
            Total = claim.Total,
            TotalCurrency = claim.TotalCurrency,
            PatientId = claim.PatientId,
            PatientName = claim.Patient?.GetFullName(),
            PatientAgeYears = patientAge,
            PatientMrn = claim.Patient?.MRN,
            PatientNationalId = claim.Patient?.NationalId,
            PatientNationalIdType = claim.Patient?.NationalIdType,
            IdentificationNumber = claim.Patient?.NationalId,
            NationalityId = claim.Patient?.NationalIdType,
            PatientDateOfBirth = claim.Patient?.DateOfBirth,
            PatientGender = claim.Patient?.Gender,
            CreatedAt = claim.CreatedAt,
            ClinicName = claim.Practitioner?.DoctorMaster?.Clinic?.ClinicName ?? claim.ServiceLocation?.LocationName,
            DoctorReference = claim.Practitioner?.DoctorMaster?.DoctorCode,
            DoctorName = claim.Practitioner?.DoctorMaster?.DoctorName ?? claim.Practitioner?.GetDisplayName(),
            DoctorLicenseNumber = claim.Practitioner?.DoctorMaster?.BoardLicenseNumber ?? claim.Practitioner?.LicenseNumber,
            CoveragePolicyNumber = claim.Coverage?.PolicyNumber,
            CoverageMemberId = claim.Coverage?.MemberID,
            CoverageStatus = claim.Coverage?.Status,
            PayerName = claim.Coverage?.Insurer?.OrganizationName ?? claim.Insurer?.OrganizationName,
            PolicyHolderNumber = claim.Coverage?.SubscriberId ?? claim.Coverage?.SubscriberMRN,
            PolicyHolderName = claim.Coverage?.SubscriberPatient?.GetFullName(),
            CoveragePlanCode = claim.Coverage?.PlanCode,
            CoverageType = claim.Coverage?.CoverageType,
            CoverageStartDate = claim.Coverage?.CoverageStartDate,
            CoverageEndDate = claim.Coverage?.CoverageEndDate,
            PreAuthorizationReference = claim.PreAuthorizationRef ?? response?.PreAuthRef,
            OfflineApprovalDate = claim.AuthorizationOfflineDate,
            EligibilityOfflineReference = claim.EligibilityOfflineReference,
            NphiesApprovalIdentifier = response?.ResponseIdentifierValue,
            EncounterId = claim.Encounter?.EncounterId,
            EncounterIdentifierValue = claim.Encounter?.IdentifierValue,
            EncounterClass = claim.Encounter?.Class,
            EncounterStatus = claim.Encounter?.Status,
            EncounterAdmitSource = claim.Encounter?.AdmitSource,
            EncounterPeriodStart = claim.Encounter?.PeriodStart,
            EncounterPeriodEnd = claim.Encounter?.PeriodEnd,
            EmergencyArrivalCode = SupportingValue("emergency-arrival-code"),
            TriageCategory = SupportingValue("triage-category"),
            DischargeDisposition = SupportingValue("discharge-disposition"),
            EmergencyDepartmentDisposition = SupportingValue("emergency-department-disposition"),
            TriageDate = triageDate,
            DischargeSpecialty = SupportingValue("discharge-specialty"),
            ChiefComplaint = SupportingValue("chief-complaint"),
            SignificantSigns = SupportingValue("significant-signs"),
            InvestigationResult = SupportingValue("investigation-result"),
            TreatmentPlan = SupportingValue("treatment-plan"),
            PatientHistory = SupportingValue("patient-history"),
            PhysicalExamination = SupportingValue("physical-examination"),
            HistoryOfPresentIllness = SupportingValue("history-of-present-illness"),
            VitalSigns = claim.SupportingInfo
                .Where(info => info.Category.StartsWith("vital-", StringComparison.OrdinalIgnoreCase))
                .OrderBy(info => info.Sequence)
                .Select(info => new ClaimWorkflowVitalDto
                {
                    Name = info.CategoryDisplay ?? info.Category[6..],
                    Value = info.QuantityValue?.ToString() ?? info.StringValue ?? info.CodeValue ?? string.Empty,
                    Unit = info.QuantityUnit
                }).ToList(),
            Services = claim.Items.OrderBy(item => item.Sequence).Select(item => new ClaimWorkflowServiceDto
            {
                Sequence = item.Sequence,
                ServiceCode = item.ProductOrServiceCode,
                ServiceName = item.ProductOrServiceDisplay,
                Quantity = item.Quantity,
                ServiceDate = item.ServicedDate,
                UnitPrice = item.UnitPrice,
                Net = item.Net,
                ReferenceInvoice = item.PatientInvoiceValue,
                ApprovalNumber = response?.ResponseIdentifierValue ?? response?.PreAuthRef,
                ApprovedAmount = responseItems.FirstOrDefault(approved => approved.Sequence == item.Sequence)?.BenefitAmount
                    ?? responseItems.FirstOrDefault(approved => approved.Sequence == item.Sequence)?.Adjudications
                        .FirstOrDefault(adjudication => adjudication.AdjudicationCategory == "benefit")?.Amount,
                ApprovedQuantity = responseItems.FirstOrDefault(approved => approved.Sequence == item.Sequence)?.ApprovedQuantity,
                IsApproved = responseItems.FirstOrDefault(approved => approved.Sequence == item.Sequence)?.IsApproved,
                Details = item.Details.OrderBy(detail => detail.Sequence).Select(detail => new ClaimWorkflowServiceDetailDto
                {
                    Sequence = detail.Sequence,
                    ServiceCode = detail.ProductOrServiceCode,
                    ServiceName = detail.ProductOrServiceDisplay,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    Net = detail.Net
                }).ToList()
            }).ToList(),
            Diagnoses = claim.Diagnoses.OrderBy(diagnosis => diagnosis.Sequence).Select(diagnosis => new ClaimWorkflowDiagnosisDto
            {
                Sequence = diagnosis.Sequence,
                DiagnosisCode = diagnosis.DiagnosisCode,
                DiagnosisDisplay = diagnosis.DiagnosisDisplay,
                DiagnosisType = diagnosis.DiagnosisType
            }).ToList()
        };
    }

    private static int CalculateAge(DateTime dateOfBirth, DateTime asOfDate)
    {
        var age = asOfDate.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > asOfDate.Date.AddYears(-age)) age--;
        return age;
    }

    /// <summary>
    /// Record a technical review decision on a claim.
    /// </summary>
 public async Task<ClaimReviewStatusDto> TechnicalReviewAsync(string claimId, ClaimReviewDto dto)
    {
var decision = NormalizeDecision(dto.Decision);
        var claim = await _claimRepository.GetByIdAsync(claimId)
       ?? throw new KeyNotFoundException($"Claim with ID {claimId} not found");

     claim.TechnicalReviewStatus = decision;
        claim.TechnicalReviewerId = dto.ReviewerId;
        claim.TechnicalReviewNotes = dto.Notes;
        claim.TechnicalReviewedAt = DateTime.UtcNow;
    claim.UpdatedAt = DateTime.UtcNow;

        _claimRepository.Update(claim);
        await _claimRepository.SaveChangesAsync();

   return MapReviewStatus(claim);
    }

    /// <summary>
  /// Record a medical review decision on a claim.
    /// </summary>
    public async Task<ClaimReviewStatusDto> MedicalReviewAsync(string claimId, ClaimReviewDto dto)
    {
        var decision = NormalizeDecision(dto.Decision);
        var claim = await _claimRepository.GetByIdAsync(claimId)
            ?? throw new KeyNotFoundException($"Claim with ID {claimId} not found");

        claim.MedicalReviewStatus = decision;
        claim.MedicalReviewerId = dto.ReviewerId;
        claim.MedicalReviewNotes = dto.Notes;
        claim.MedicalReviewedAt = DateTime.UtcNow;
        claim.UpdatedAt = DateTime.UtcNow;

        _claimRepository.Update(claim);
        await _claimRepository.SaveChangesAsync();

        return MapReviewStatus(claim);
    }

    /// <summary>
    /// Get the current dual-approval review status of a claim.
    /// </summary>
    public async Task<ClaimReviewStatusDto?> GetReviewStatusAsync(string claimId)
    {
        var claim = await _claimRepository.GetByIdAsync(claimId);
        return claim != null ? MapReviewStatus(claim) : null;
    }

 /// <summary>
    /// Get claims awaiting a given review stage ("technical" or "medical").
    /// </summary>
public async Task<IEnumerable<ClaimDto>> GetClaimsAwaitingReviewAsync(string stage)
    {
        var claims = await _claimRepository.GetAllAsync();

        var pending = stage?.Trim().ToLowerInvariant() switch
      {
     "technical" => claims.Where(c =>
      string.Equals(c.TechnicalReviewStatus, "Pending", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(c.TechnicalReviewStatus, "InReview", StringComparison.OrdinalIgnoreCase)),
            "medical" => claims.Where(c =>
 string.Equals(c.MedicalReviewStatus, "Pending", StringComparison.OrdinalIgnoreCase) ||
  string.Equals(c.MedicalReviewStatus, "InReview", StringComparison.OrdinalIgnoreCase)),
       _ => throw new ArgumentException("Stage must be 'technical' or 'medical'.", nameof(stage))
        };

        return _mapper.Map<IEnumerable<ClaimDto>>(pending);
    }

    /// <summary>
    /// Get active claims with both review stages approved and ready for submission.
    /// </summary>
    public async Task<IEnumerable<ClaimDto>> GetClaimsReadyForSubmissionAsync()
    {
        var claims = await _claimRepository.GetAllAsync();
        var ready = claims.Where(c =>
            c.IsApprovedForSubmission &&
            string.Equals(c.Status, "active", StringComparison.OrdinalIgnoreCase));

        return _mapper.Map<IEnumerable<ClaimDto>>(ready);
    }

    /// <summary>
    /// Validate and normalize a review decision value.
    /// </summary>
    private static string NormalizeDecision(string decision)
    {
      if (string.IsNullOrWhiteSpace(decision))
    throw new ArgumentException("Decision is required.", nameof(decision));

        return decision.Trim().ToLowerInvariant() switch
        {
        "approved" or "approve" => "Approved",
            "rejected" or "reject" => "Rejected",
         _ => throw new ArgumentException("Decision must be 'Approved' or 'Rejected'.", nameof(decision))
        };
    }

    /// <summary>
    /// Map a claim's review fields into a review status DTO.
    /// </summary>
 private static ClaimReviewStatusDto MapReviewStatus(Claim claim) => new()
    {
    ClaimId = claim.Id,
ClaimNumber = claim.ClaimNumber,
        TechnicalReviewStatus = claim.TechnicalReviewStatus,
   TechnicalReviewerId = claim.TechnicalReviewerId,
        TechnicalReviewedAt = claim.TechnicalReviewedAt,
        TechnicalReviewNotes = claim.TechnicalReviewNotes,
 MedicalReviewStatus = claim.MedicalReviewStatus,
        MedicalReviewerId = claim.MedicalReviewerId,
   MedicalReviewedAt = claim.MedicalReviewedAt,
        MedicalReviewNotes = claim.MedicalReviewNotes,
    IsApprovedForSubmission = claim.IsApprovedForSubmission
    };

    /// <summary>
    /// Mark a claim as submitted to NPHIES, enforcing the dual-approval gate.
    /// </summary>
    public async Task<ClaimDto> SubmitClaimAsync(string claimId, string? submittedBy = null)
    {
    // Load claim with related entities needed to build the FHIR bundle.
        var claim = await _claimRepository.GetWithDetailsAsync(claimId)
    ?? throw new KeyNotFoundException($"Claim with ID {claimId} not found");

        // ---- Dual-approval gate ----
      if (!claim.IsApprovedForSubmission)
        {
     throw new InvalidOperationException(
        $"Claim {claim.ClaimNumber} cannot be submitted: both technical and medical reviews " +
   $"must be Approved. Current status � Technical: {claim.TechnicalReviewStatus}, " +
       $"Medical: {claim.MedicalReviewStatus}.");
     }

     // ---- Transmit to NPHIES via the RCM workflow layer ----
        try
        {
            await _rcmClaims.SubmitAsync(claimId);
        }
        catch (NPhies_FHIR_Integration.Application.Rcm.RcmWorkflowException ex)
        {
            if (ex.Kind == NPhies_FHIR_Integration.Application.Rcm.RcmErrorKind.NotFound)
                throw new KeyNotFoundException(ex.Message, ex);
            throw new InvalidOperationException(ex.Message, ex);
        }
        var updated = await _claimRepository.GetWithDetailsAsync(claimId) ?? claim;
        if (!string.Equals(updated.Status, "cancelled", StringComparison.OrdinalIgnoreCase) &&
            updated.NphiesStatus is not ("Error" or "ValidationFailed"))
            updated.Status = "submitted";
        updated.UpdatedAt = DateTime.UtcNow;
        _claimRepository.Update(updated);
        await _claimRepository.SaveChangesAsync();
        return _mapper.Map<ClaimDto>(updated);
    }
}
