using NPhies_FHIR_Integration.Domain.DTOs;

namespace NPhies_FHIR_Integration.Domain.DTOs;

/// <summary>
/// DTO for Encounter
/// </summary>
public class EncounterDto : BaseDto
{
public string EncounterId { get; set; } = string.Empty;
    public string? IdentifierSystem { get; set; }
    public string? IdentifierValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string? ServiceType { get; set; }
    public string? ServiceTypeSystem { get; set; }
    public string? PatientId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public string? AdmitSource { get; set; }
    public string? ServiceEventType { get; set; }
    public string? IntendedLengthOfStay { get; set; }
    public string? ServiceProviderId { get; set; }
}

/// <summary>
/// DTO for Claim
/// </summary>
public class ClaimDto : BaseDto
{
    public string ClaimNumber { get; set; } = string.Empty;
    public string? ClaimIdentifierSystem { get; set; }
    public string? ClaimIdentifierValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ClaimType { get; set; } = string.Empty;
    public string? ClaimSubType { get; set; }
  public string Use { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? ServicedPeriodStart { get; set; }
    public DateTime? ServicedPeriodEnd { get; set; }
    public string Priority { get; set; } = "normal";
    public string? PatientId { get; set; }
    public string? CoverageId { get; set; }
    public string? ProviderId { get; set; }
    public string? InsurerId { get; set; }
    public string? PractitionerId { get; set; }
 public string? LocationId { get; set; }
    public string? EncounterId { get; set; }
    public string? PayeeType { get; set; }
    public decimal Total { get; set; }
    public string TotalCurrency { get; set; } = "SAR";
    public List<ClaimItemDto> Items { get; set; } = new();
    public List<ClaimDiagnosisDto> Diagnoses { get; set; } = new();
    public List<ClaimCareTeamDto> CareTeam { get; set; } = new();
    public List<ClaimSupportingInfoDto> SupportingInfo { get; set; } = new();
    public List<ClaimRelatedDto> RelatedClaims { get; set; } = new();
}

/// <summary>
/// DTO for ClaimItem
/// </summary>
public class ClaimItemDto : BaseDto
{
    public string? ClaimId { get; set; }
    public int Sequence { get; set; }
    public int? CareTeamSequence { get; set; }
 public string ProductOrServiceCode { get; set; } = string.Empty;
    public string? ProductOrServiceSystem { get; set; }
    public string? ProductOrServiceDisplay { get; set; }
    public string? AltProductOrServiceCode { get; set; }
    public string? AltProductOrServiceSystem { get; set; }
    public DateTime? ServicedDate { get; set; }
    public DateTime? ServicedPeriodStart { get; set; }
    public DateTime? ServicedPeriodEnd { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Net { get; set; }
    public decimal? PatientShare { get; set; }
  public string? PatientShareCurrency { get; set; }
    public bool IsPackage { get; set; }
    public bool IsMaternity { get; set; }
    public string? Notes { get; set; }
    public List<ClaimItemDetailDto> Details { get; set; } = new();
}

/// <summary>
/// DTO for ClaimItemDetail
/// </summary>
public class ClaimItemDetailDto : BaseDto
{
    public string? ClaimItemId { get; set; }
    public int Sequence { get; set; }
    public string ProductOrServiceCode { get; set; } = string.Empty;
    public string? ProductOrServiceSystem { get; set; }
    public string? ProductOrServiceDisplay { get; set; }
    public string? AltProductOrServiceCode { get; set; }
    public string? AltProductOrServiceSystem { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Net { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for ClaimSupportingInfo
/// </summary>
public class ClaimSupportingInfoDto : BaseDto
{
    public string? ClaimId { get; set; }
    public int Sequence { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? CategorySystem { get; set; }
    public string? CategoryDisplay { get; set; }
    public string? CodeValue { get; set; }
    public string? CodeSystem { get; set; }
  public string? StringValue { get; set; }
    public decimal? QuantityValue { get; set; }
    public string? QuantityUnit { get; set; }
    public string? QuantitySystem { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for ClaimDiagnosis
/// </summary>
public class ClaimDiagnosisDto : BaseDto
{
    public string? ClaimId { get; set; }
    public int Sequence { get; set; }
  public string DiagnosisCode { get; set; } = string.Empty;
    public string? DiagnosisSystem { get; set; }
    public string? DiagnosisDisplay { get; set; }
    public string? DiagnosisType { get; set; }
    public string? DiagnosisTypeSystem { get; set; }
    public string? OnAdmissionCode { get; set; }
    public string? OnAdmissionSystem { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for ClaimCareTeam
/// </summary>
public class ClaimCareTeamDto : BaseDto
{
    public string? ClaimId { get; set; }
    public int Sequence { get; set; }
    public string? PractitionerId { get; set; }
    public string? Role { get; set; }
    public string? RoleSystem { get; set; }
    public string? RoleDisplay { get; set; }
    public string? Qualification { get; set; }
  public string? QualificationSystem { get; set; }
    public string? QualificationDisplay { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for ClaimRelated
/// </summary>
public class ClaimRelatedDto : BaseDto
{
    public string? ClaimId { get; set; }
    public string? RelatedClaimIdentifierSystem { get; set; }
    public string? RelatedClaimIdentifierValue { get; set; }
    public string? Relationship { get; set; }
    public string? RelationshipSystem { get; set; }
    public string? RelationshipDisplay { get; set; }
    public string? ReferencedClaimId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for ClaimResponse
/// </summary>
public class ClaimResponseDto : BaseDto
{
    public string? ClaimId { get; set; }
    public string? ResponseIdentifierSystem { get; set; }
    public string? ResponseIdentifierValue { get; set; }
    public string? ClaimResponseStatus { get; set; }
    public string? ClaimType { get; set; }
  public string? ClaimTypeSystem { get; set; }
  public string? ClaimSubType { get; set; }
    public string? ClaimSubTypeSystem { get; set; }
    public string? Use { get; set; }
    public string? PatientId { get; set; }
 public string? InsurerId { get; set; }
   public string? RequestorId { get; set; }
    public string? RequestIdentifierSystem { get; set; }
    public string? RequestIdentifierValue { get; set; }
    public string? PreAuthRef { get; set; }
        public DateTime? PreAuthPeriodStart { get; set; }
        public DateTime? PreAuthPeriodEnd { get; set; }
        public string? AdvancedAuthReason { get; set; }
        public string? AdvancedAuthReasonSystem { get; set; }
    public string? ServiceProviderId { get; set; }
   public string? AdjudicationOutcome { get; set; }
        public string? AdjudicationOutcomeSystem { get; set; }
        public string? ResponseStatus { get; set; }
  public string? Outcome { get; set; }
     public decimal? ApprovedAmount { get; set; }
        public string? ApprovedCurrency { get; set; }
        public DateTime? ResponseCreatedAt { get; set; }
        public DateTime? ResponseReceivedAt { get; set; }
        public string? Disposition { get; set; }
        public List<ClaimResponseInsuranceDto> Insurance { get; set; } = new();
        public List<ClaimResponseAddItemDto> AddItems { get; set; } = new();
        public List<ClaimResponseTotalDto> Totals { get; set; } = new();
        public List<ClaimResponseDiagnosisExtDto> DiagnosesExt { get; set; } = new();
        public List<ClaimResponseSupportingInfoExtDto> SupportingInfoExt { get; set; } = new();
}
/// <summary>
/// DTO for ClaimResponseInsurance
/// </summary>
public class ClaimResponseInsuranceDto : BaseDto
{
    public string? ClaimResponseId { get; set; }
    public int Sequence { get; set; }
    public bool Focal { get; set; }
    public string? CoverageId { get; set; }
}

/// <summary>
/// DTO for ClaimResponseAddItem
/// </summary>
public class ClaimResponseAddItemDto : BaseDto
{
    public string? ClaimResponseId { get; set; }
 public int Sequence { get; set; }
    public string? ProductOrServiceCode { get; set; }
    public string? ProductOrServiceSystem { get; set; }
    public string? ProductOrServiceDisplay { get; set; }
    public bool IsApproved { get; set; }
    public int? ApprovedQuantity { get; set; }
    public decimal? BenefitAmount { get; set; }
    public string? BenefitCurrency { get; set; }
    public decimal? SubmittedAmount { get; set; }
    public int? DiagnosisSequence { get; set; }
    public int? InformationSequence { get; set; }
    public bool IsMaternity { get; set; }
    public string? Notes { get; set; }
    public List<ClaimResponseAdjudicationDto> Adjudications { get; set; } = new();
}

/// <summary>
/// DTO for ClaimResponseAdjudication
/// </summary>
public class ClaimResponseAdjudicationDto : BaseDto
{
    public string? ClaimResponseAddItemId { get; set; }
    public string AdjudicationCategory { get; set; } = string.Empty;
    public string? AdjudicationSystem { get; set; }
    public string? AdjudicationDisplay { get; set; }
    public decimal? Amount { get; set; }
 public string? Currency { get; set; }
    public int? QuantityValue { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for ClaimResponseTotal
/// </summary>
public class ClaimResponseTotalDto : BaseDto
{
    public string? ClaimResponseId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? CategorySystem { get; set; }
    public string? CategoryDisplay { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public int? Sequence { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for ClaimResponseDiagnosisExt
/// </summary>
public class ClaimResponseDiagnosisExtDto : BaseDto
{
    public string? ClaimResponseId { get; set; }
    public int Sequence { get; set; }
    public string DiagnosisCode { get; set; } = string.Empty;
  public string? DiagnosisSystem { get; set; }
    public string? DiagnosisDisplay { get; set; }
    public string? DiagnosisType { get; set; }
    public string? DiagnosisTypeSystem { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for ClaimResponseSupportingInfoExt
/// </summary>
public class ClaimResponseSupportingInfoExtDto : BaseDto
{
    public string? ClaimResponseId { get; set; }
    public int Sequence { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? CategorySystem { get; set; }
    public string? CategoryDisplay { get; set; }
    public string? CodeValue { get; set; }
    public string? CodeSystem { get; set; }
    public string? StringValue { get; set; }
    public decimal? QuantityValue { get; set; }
    public string? QuantityUnit { get; set; }
    public string? QuantitySystem { get; set; }
    public string? Notes { get; set; }
}
