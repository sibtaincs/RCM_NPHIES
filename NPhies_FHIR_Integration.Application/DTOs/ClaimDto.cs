namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// DTO for Claim entity - for read operations
/// </summary>
public class ClaimDto
{
    public string Id { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string? ClaimIdentifierSystem { get; set; }
    public string? ClaimIdentifierValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ClaimType { get; set; } = string.Empty;
    public string? ClaimTypeSystem { get; set; }
    public string? ClaimSubType { get; set; }
    public string Use { get; set; } = string.Empty;
    public string? Priority { get; set; }
    public string? PayeeType { get; set; }
    public decimal? Total { get; set; }
    public string? TotalCurrency { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string? ProviderId { get; set; }
    public string? InsurerId { get; set; }
    public string? CoverageId { get; set; }
    public string? MessageHeaderId { get; set; }
    public string? EpisodeIdentifierSystem { get; set; }
    public string? EpisodeIdentifierValue { get; set; }
    public string? EligibilityOfflineReference { get; set; }
    public DateTime? EligibilityOfflineDate { get; set; }
    public DateTime? AuthorizationOfflineDate { get; set; }
    
    // NEW: Accident Information
    public DateTime? AccidentDate { get; set; }
    public string? AccidentType { get; set; }
    public string? AccidentTypeSystem { get; set; }
    
    // NEW: Funds Reserve
    public string? FundsReserveCode { get; set; }
    public string? FundsReserveSystem { get; set; }
    
    // NEW: Referral and Prescription References
    public string? ReferralIdentifier { get; set; }
    public string? PrescriptionIdentifier { get; set; }
    public string? OriginalPrescriptionIdentifier { get; set; }
    public string? PreAuthorizationRef { get; set; }
    
    // NEW: Billable Period
    public DateTime? BillablePeriodStart { get; set; }
    public DateTime? BillablePeriodEnd { get; set; }
  
    public string? ClaimSubTypeSystem { get; set; }
    public string? PractitionerId { get; set; }
    public string? LocationId { get; set; }
    public string? EncounterId { get; set; }
    public DateTime? ServicedPeriodStart { get; set; }
    public DateTime? ServicedPeriodEnd { get; set; }
    public bool IsShadowBilling { get; set; }
    public bool? IsTransfer { get; set; }

    // Review gate
    public string? TechnicalReviewStatus { get; set; }
    public string? MedicalReviewStatus { get; set; }
    public bool IsApprovedForSubmission { get; set; }

    // NPHIES workflow tracking
    public string? NphiesStatus { get; set; }
    public string? NphiesOutcome { get; set; }
    public string? AdjudicationOutcome { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? AdjudicatedAt { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? PatientShareAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? PreAuthPeriodEnd { get; set; }
    public string? BatchIdentifier { get; set; }

    /// <summary>Populated when the claim was loaded with its details.</summary>
    public List<ClaimItemDto> Items { get; set; } = new();
    public List<ClaimDiagnosisDto> Diagnoses { get; set; } = new();
    public List<ClaimCareTeamDto> CareTeam { get; set; } = new();
    public List<ClaimSupportingInfoDto> SupportingInfo { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Alias for CreatedAt for compatibility
    /// </summary>
    public DateTime Created => CreatedAt;
}

/// <summary>
/// DTO for creating a Claim
/// </summary>
public class CreateClaimDto
{
    public string ClaimNumber { get; set; } = string.Empty;
    public string? ClaimIdentifierSystem { get; set; }
    public string? ClaimIdentifierValue { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string? ClaimTypeSystem { get; set; }
    public string? ClaimSubType { get; set; }
    public string Use { get; set; } = "claim";
    public string? Priority { get; set; }
    public string? PrioritySystem { get; set; }
    public string? PayeeType { get; set; }
    public string? PayeeTypeSystem { get; set; }
    public decimal? Total { get; set; }
    public string? TotalCurrency { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string? ProviderId { get; set; }
    public string? InsurerId { get; set; }
    public string? CoverageId { get; set; }
    public string? MessageHeaderId { get; set; }
    public string? EpisodeIdentifierSystem { get; set; }
    public string? EpisodeIdentifierValue { get; set; }
    public string? EligibilityOfflineReference { get; set; }
    public DateTime? EligibilityOfflineDate { get; set; }
    public DateTime? AuthorizationOfflineDate { get; set; }
    
    // NEW: Accident Information
    public DateTime? AccidentDate { get; set; }
    public string? AccidentType { get; set; }
    public string? AccidentTypeSystem { get; set; }
    
    // NEW: Funds Reserve
    public string? FundsReserveCode { get; set; }
    public string? FundsReserveSystem { get; set; }
    
    // NEW: Referral and Prescription References
    public string? ReferralIdentifier { get; set; }
    public string? PrescriptionIdentifier { get; set; }
    public string? OriginalPrescriptionIdentifier { get; set; }
    public string? PreAuthorizationRef { get; set; }
    
    // NEW: Billable Period
    public DateTime? BillablePeriodStart { get; set; }
    public DateTime? BillablePeriodEnd { get; set; }

    public string? ClaimSubTypeSystem { get; set; }
    public string? PractitionerId { get; set; }
    public string? LocationId { get; set; }
    public string? EncounterId { get; set; }
    public DateTime? ServicedPeriodStart { get; set; }
    public DateTime? ServicedPeriodEnd { get; set; }
    /// <summary>Shadow billing (claim for a service already covered, sent for statistics only).</summary>
    public bool IsShadowBilling { get; set; }
    public bool? IsTransfer { get; set; }

    /// <summary>Claim lines. Sequence is assigned in order when omitted; ClaimId is ignored.</summary>
    public List<CreateClaimItemDto> Items { get; set; } = new();
    /// <summary>Diagnoses (ICD-10-AM). Sequence is assigned in order when omitted; ClaimId is ignored.</summary>
    public List<CreateClaimDiagnosisDto> Diagnoses { get; set; } = new();
    public List<ClaimCareTeamDto> CareTeam { get; set; } = new();
    public List<ClaimSupportingInfoDto> SupportingInfo { get; set; } = new();
}

/// <summary>Claim care-team member.</summary>
public class ClaimCareTeamDto
{
    public string? Id { get; set; }
    public int Sequence { get; set; }
    public string PractitionerId { get; set; } = string.Empty;
    public string? PractitionerName { get; set; }
    /// <summary>primary | assist | supervisor | other (http://terminology.hl7.org/CodeSystem/claimcareteamrole)</summary>
    public string? Role { get; set; }
    public string? RoleSystem { get; set; }
    public string? RoleDisplay { get; set; }
    /// <summary>Practice speciality code (http://nphies.sa/terminology/CodeSystem/practice-codes)</summary>
    public string? Qualification { get; set; }
    public string? QualificationSystem { get; set; }
    public string? QualificationDisplay { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Claim supporting information (vital signs, chief complaint, attachments, days-supply, ...).</summary>
public class ClaimSupportingInfoDto
{
    public string? Id { get; set; }
    public int Sequence { get; set; }
    /// <summary>NPHIES claim-information-category code (e.g. vital-sign-systolic, chief-complaint, attachment).</summary>
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
/// DTO for updating a Claim
/// </summary>
public class UpdateClaimDto
{
 public string? Status { get; set; }
    public decimal? Total { get; set; }
    public string? EpisodeIdentifierValue { get; set; }
    public DateTime? AuthorizationOfflineDate { get; set; }
    
    // NEW: Accident Information
    public DateTime? AccidentDate { get; set; }
    public string? AccidentType { get; set; }
    
    // NEW: Funds Reserve
    public string? FundsReserveCode { get; set; }
    
    // NEW: Referral and Prescription References
    public string? ReferralIdentifier { get; set; }
    public string? PrescriptionIdentifier { get; set; }
    public string? PreAuthorizationRef { get; set; }
    
    // NEW: Billable Period
    public DateTime? BillablePeriodStart { get; set; }
    public DateTime? BillablePeriodEnd { get; set; }
}
