namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Claim entity - represents insurance claim, pre-authorization, or predetermination request
/// FHIR Resource: Claim
/// </summary>
public class Claim : BaseEntity
{
    /// <summary>
    /// Claim number/identifier
    /// </summary>
    public string ClaimNumber { get; set; } = string.Empty;

    /// <summary>
    /// Claim identifier system (e.g., "http://hmg.com/Takhassusi/Authorization")
    /// </summary>
    public string? ClaimIdentifierSystem { get; set; }

    /// <summary>
    /// Claim identifier value
    /// </summary>
    public string? ClaimIdentifierValue { get; set; }

    /// <summary>
    /// Claim status: "active", "cancelled", "draft", "entered-in-error"
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Claim type: "institutional", "professional", "pharmacy", "oral", "vision", "transport", "other"
    /// </summary>
    public string ClaimType { get; set; } = string.Empty;

    /// <summary>
    /// Claim type system URL
    /// </summary>
    public string? ClaimTypeSystem { get; set; }

    /// <summary>
    /// Claim subtype: "ip" (inpatient), "op" (outpatient), "er" (emergency), "ambulatory"
    /// </summary>
    public string? ClaimSubType { get; set; }

    /// <summary>
    /// Claim subtype system URL
    /// </summary>
    public string? ClaimSubTypeSystem { get; set; }

    /// <summary>
    /// Claim use: "claim", "preauthorization", "predetermination"
    /// </summary>
    public string Use { get; set; } = "claim";

    /// <summary>
    /// When the claim was created (alias for CreatedDate)
    /// </summary>
    public DateTime Created
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }

    /// <summary>
    /// When the claim was created
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Service period start date
    /// </summary>
    public DateTime? ServicedPeriodStart { get; set; }

    /// <summary>
    /// Service period end date
    /// </summary>
    public DateTime? ServicedPeriodEnd { get; set; }

    /// <summary>
    /// Claim priority: "stat", "urgent", "normal", "deferred"
    /// </summary>
    public string Priority { get; set; } = "normal";

    /// <summary>
    /// Priority system URL
    /// </summary>
    public string? PrioritySystem { get; set; }

    /// <summary>
    /// Patient ID
    /// </summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>
 /// Patient
    /// </summary>
    public Patient? Patient { get; set; }

    /// <summary>
    /// Coverage ID
    /// </summary>
    public string CoverageId { get; set; } = string.Empty;

  /// <summary>
    /// Coverage
    /// </summary>
    public Coverage? Coverage { get; set; }

  /// <summary>
  /// Provider (submitting facility) ID
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider (submitting facility)
    /// </summary>
    public Organization? Provider { get; set; }

/// <summary>
    /// Insurer ID
    /// </summary>
    public string InsurerId { get; set; } = string.Empty;

    /// <summary>
    /// Insurer
    /// </summary>
    public Organization? Insurer { get; set; }

    /// <summary>
    /// Practitioner ID (enterer/responsible)
    /// </summary>
    public string? PractitionerId { get; set; }

    /// <summary>
    /// Practitioner (enterer/responsible)
    /// </summary>
    public Practitioner? Practitioner { get; set; }

    /// <summary>
    /// Location ID (service location)
    /// </summary>
    public string? LocationId { get; set; }

    /// <summary>
    /// Location (service location)
    /// </summary>
    public Location? ServiceLocation { get; set; }

    /// <summary>
    /// Message header ID
    /// </summary>
    public string? MessageHeaderId { get; set; }

    /// <summary>
    /// Message header
    /// </summary>
  public MessageHeader? MessageHeader { get; set; }

    /// <summary>
    /// Encounter ID
    /// </summary>
    public string? EncounterId { get; set; }

  /// <summary>
    /// Encounter
  /// </summary>
    public Encounter? Encounter { get; set; }

    /// <summary>
    /// Payee type: "subscriber", "provider", "organization", "patient", "other"
    /// </summary>
    public string? PayeeType { get; set; }

    /// <summary>
    /// Payee type system URL
    /// </summary>
    public string? PayeeTypeSystem { get; set; }

    /// <summary>
    /// Total claim amount
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Total currency (e.g., "SAR")
 /// </summary>
    public string TotalCurrency { get; set; } = "SAR";

    // Navigation Properties
    /// <summary>
    /// Collection of claim items
    /// </summary>
    public ICollection<ClaimItem> Items { get; set; } = new List<ClaimItem>();

    /// <summary>
    /// Collection of diagnoses
    /// </summary>
    public ICollection<ClaimDiagnosis> Diagnoses { get; set; } = new List<ClaimDiagnosis>();

    /// <summary>
  /// Collection of care team members
    /// </summary>
    public ICollection<ClaimCareTeam> CareTeam { get; set; } = new List<ClaimCareTeam>();

    /// <summary>
    /// Collection of supporting information
 /// </summary>
    public ICollection<ClaimSupportingInfo> SupportingInfo { get; set; } = new List<ClaimSupportingInfo>();

    /// <summary>
    /// Collection of related claims
    /// </summary>
    public ICollection<ClaimRelated> RelatedClaims { get; set; } = new List<ClaimRelated>();

   public ICollection<ClaimResponse> ClaimResponses { get; set; } = new List<ClaimResponse>();

    /// <summary>
    /// FHIR Claim bundle JSON for storage
    /// </summary>
    public string? FhirClaimBundle { get; set; } = string.Empty;

    /// <summary>
    /// Episode identifier system (treatment episode reference)
    /// Example: "http://saudidentalclinic.com.sa/episode"
    /// </summary>
    public string? EpisodeIdentifierSystem { get; set; }

    /// <summary>
    /// Episode identifier value (e.g., "SDC_EpisodeID_48903211")
    /// Links claim to a treatment episode
    /// </summary>
    public string? EpisodeIdentifierValue { get; set; }

    /// <summary>
    /// Eligibility response offline reference (e.g., "EligResp-31212432")
    /// For offline eligibility checks
    /// </summary>
    public string? EligibilityOfflineReference { get; set; }

    /// <summary>
    /// Eligibility offline date (when eligibility was checked offline)
    /// Example: "2021-08-30"
    /// </summary>
    public DateTime? EligibilityOfflineDate { get; set; }

    /// <summary>
    /// Authorization offline date (when authorization was obtained offline)
    /// Example: "2021-06-30T11:05:48+03:00"
    /// </summary>
    public DateTime? AuthorizationOfflineDate { get; set; }

    // ========== ACCIDENT INFORMATION (inline) ==========
    // Note: For detailed accident info, use ClaimAccident entity
    
    /// <summary>
    /// Accident date (simplified - for quick reference)
    /// For detailed accident info, use ClaimAccident entity
    /// </summary>
public DateTime? AccidentDate { get; set; }

    /// <summary>
    /// Accident type: MVA (Motor Vehicle Accident), WORK (Workplace), etc.
    /// </summary>
    public string? AccidentType { get; set; }

    /// <summary>
    /// Accident type system URL
    /// </summary>
    public string? AccidentTypeSystem { get; set; }

 // ========== FUNDS RESERVE ==========

    /// <summary>
    /// Funds reserve requested: patient, provider, none
    /// Indicates who should hold funds in reserve
    /// </summary>
    public string? FundsReserveCode { get; set; }

    /// <summary>
    /// Funds reserve system URL
    /// </summary>
    public string? FundsReserveSystem { get; set; }

    // ========== REFERRAL AND PRESCRIPTION REFERENCES ==========
  
    /// <summary>
    /// Referral identifier (ServiceRequest reference)
    /// Reference to a referral document or ServiceRequest
 /// </summary>
    public string? ReferralIdentifier { get; set; }

/// <summary>
    /// Prescription identifier (MedicationRequest reference)
    /// Reference to the prescription/MedicationRequest
    /// </summary>
 public string? PrescriptionIdentifier { get; set; }

    /// <summary>
    /// Original prescription identifier (for prescription refills)
    /// Reference to the original prescription being refilled
    /// </summary>
    public string? OriginalPrescriptionIdentifier { get; set; }

 /// <summary>
/// Pre-authorization reference number
    /// Link to a pre-authorization approval (e.g., "136997701")
    /// </summary>
    public string? PreAuthorizationRef { get; set; }

    // ========== BILLABLE PERIOD ==========
    
  /// <summary>
    /// Billable period start date
    /// The period for which charges are being submitted (may differ from service period)
    /// </summary>
    public DateTime? BillablePeriodStart { get; set; }

    /// <summary>
    /// Billable period end date
 /// The end of the billing period
    /// </summary>
    public DateTime? BillablePeriodEnd { get; set; }

    // ========== DUAL-APPROVAL REVIEW WORKFLOW ==========
    // Two-stage review gate before a claim may be submitted to NPHIES:
    //   1. Technical review  (coding, completeness, submission-rule compliance)
    //   2. Medical review    (diagnoses, procedures, medical necessity)
    // A claim is submit-eligible only when both statuses are "Approved".

    /// <summary>
    /// Technical review status: "Pending", "InReview", "Approved", "Rejected"
    /// Performed by a TechnicalReviewer (coding/billing correctness).
    /// </summary>
    public string TechnicalReviewStatus { get; set; } = "Pending";

    /// <summary>
    /// User ID of the technical reviewer who actioned the review.
    /// </summary>
    public string? TechnicalReviewerId { get; set; }

    /// <summary>
    /// Timestamp when the technical review was completed.
    /// </summary>
    public DateTime? TechnicalReviewedAt { get; set; }

    /// <summary>
    /// Optional technical reviewer notes / rejection reason.
    /// </summary>
    public string? TechnicalReviewNotes { get; set; }

    /// <summary>
    /// Medical review status: "Pending", "InReview", "Approved", "Rejected"
    /// Performed by a MedicalReviewer (clinical correctness / medical necessity).
    /// </summary>
    public string MedicalReviewStatus { get; set; } = "Pending";

    /// <summary>
    /// User ID of the medical reviewer who actioned the review.
    /// </summary>
    public string? MedicalReviewerId { get; set; }

    /// <summary>
    /// Timestamp when the medical review was completed.
    /// </summary>
    public DateTime? MedicalReviewedAt { get; set; }

    /// <summary>
    /// Optional medical reviewer notes / rejection reason.
    /// </summary>
    public string? MedicalReviewNotes { get; set; }

    /// <summary>
    /// Convenience flag: true only when both technical and medical reviews are "Approved",
    /// meaning the claim is eligible to be submitted to NPHIES.
    /// </summary>
    public bool IsApprovedForSubmission =>
        string.Equals(TechnicalReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(MedicalReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase);

    // ===== NPHIES RCM workflow tracking =====

    /// <summary>RCM lifecycle status (see Rcm.RcmClaimStatus): Draft, Submitted, Queued, Approved, PartiallyApproved, Rejected, Pended, Cancelled, Error, Paid...</summary>
    public string? NphiesStatus { get; set; }

    /// <summary>Last ClaimResponse.outcome (queued | complete | partial | error).</summary>
    public string? NphiesOutcome { get; set; }

    /// <summary>Last NPHIES adjudication-outcome extension (approved | rejected | partial | pended).</summary>
    public string? AdjudicationOutcome { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? AdjudicatedAt { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? PatientShareAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? PreAuthPeriodEnd { get; set; }

    /// <summary>MessageHeader id of the last outbound NPHIES message for this claim.</summary>
    public string? LastMessageId { get; set; }

    public string? BatchIdentifier { get; set; }
    public int? BatchNumber { get; set; }

    /// <summary>Shadow billing: items carry the provider's internal code alongside the NPHIES standard code.</summary>
    public bool IsShadowBilling { get; set; }

    /// <summary>NPHIES extension-transfer (patient transferred to another facility).</summary>
    public bool? IsTransfer { get; set; }
}
