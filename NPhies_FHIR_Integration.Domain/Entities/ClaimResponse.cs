namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimResponse entity - represents a response to a claim submission
/// FHIR Resource: ClaimResponse
/// </summary>
public class ClaimResponse : BaseEntity
{
    /// <summary>
 /// Claim ID this response is for
    /// </summary>
    public string ClaimId { get; set; } = string.Empty;

    /// <summary>
    /// Claim
    /// </summary>
    public Claim? Claim { get; set; }

    /// <summary>
    /// Response identifier system (e.g., "https://bupa.com.sa/ClaimResponse")
    /// </summary>
    public string? ResponseIdentifierSystem { get; set; }

    /// <summary>
    /// Response identifier value (e.g., "208198360136997701")
    /// </summary>
 public string? ResponseIdentifierValue { get; set; }

    /// <summary>
    /// ClaimResponse status: "active", "cancelled", "draft", "entered-in-error"
    /// </summary>
    public string? ClaimResponseStatus { get; set; }

    /// <summary>
    /// Claim type: "institutional", "professional", "pharmacy", etc.
    /// </summary>
    public string? ClaimType { get; set; }

    /// <summary>
    /// Claim type system URL
    /// </summary>
    public string? ClaimTypeSystem { get; set; }

    /// <summary>
    /// Claim subtype: "ip", "op", "er"
    /// </summary>
    public string? ClaimSubType { get; set; }

    /// <summary>
    /// Claim subtype system URL
  /// </summary>
    public string? ClaimSubTypeSystem { get; set; }

    /// <summary>
    /// Use: "claim", "preauthorization", "predetermination"
    /// </summary>
    public string? Use { get; set; }

    /// <summary>
    /// Patient ID
    /// </summary>
    public string? PatientId { get; set; }

 /// <summary>
    /// Patient
    /// </summary>
    public Patient? Patient { get; set; }

    /// <summary>
    /// Insurer organization ID
    /// </summary>
    public string? InsurerId { get; set; }

    /// <summary>
    /// Insurer organization
    /// </summary>
    public Organization? Insurer { get; set; }

    /// <summary>
    /// Requestor (Provider) organization ID
    /// </summary>
 public string? RequestorId { get; set; }

    /// <summary>
    /// Requestor (Provider) organization
 /// </summary>
    public Organization? Requestor { get; set; }

    /// <summary>
    /// Original request identifier system
    /// </summary>
    public string? RequestIdentifierSystem { get; set; }

    /// <summary>
    /// Original request identifier value
    /// </summary>
    public string? RequestIdentifierValue { get; set; }

    /// <summary>
    /// Pre-authorization reference number (e.g., "136997701")
    /// </summary>
    public string? PreAuthRef { get; set; }

    /// <summary>
    /// Pre-authorization period start date
    /// </summary>
    public DateTime? PreAuthPeriodStart { get; set; }

    /// <summary>
    /// Pre-authorization period end date (validity period)
    /// </summary>
    public DateTime? PreAuthPeriodEnd { get; set; }

    /// <summary>
    /// Advanced authorization reason (e.g., "referral")
    /// </summary>
    public string? AdvancedAuthReason { get; set; }

    /// <summary>
    /// Advanced auth reason system URL
    /// </summary>
    public string? AdvancedAuthReasonSystem { get; set; }

    /// <summary>
    /// Service provider ID (from extension)
    /// </summary>
    public string? ServiceProviderId { get; set; }

    /// <summary>
    /// Service provider organization
    /// </summary>
    public Organization? ServiceProvider { get; set; }

    // Navigation Properties
    /// <summary>
    /// Collection of insurance information in response
    /// </summary>
    public ICollection<ClaimResponseInsurance> Insurance { get; set; } = new List<ClaimResponseInsurance>();

    /// <summary>
    /// Collection of items ADDED by insurer (advanced authorization)
    /// </summary>
    public ICollection<ClaimResponseAddItem> AddItems { get; set; } = new List<ClaimResponseAddItem>();

    /// <summary>
/// Collection of total adjustments in response
    /// </summary>
    public ICollection<ClaimResponseTotal> Totals { get; set; } = new List<ClaimResponseTotal>();

    /// <summary>
    /// Collection of diagnoses in response (from extension)
    /// </summary>
    public ICollection<ClaimResponseDiagnosisExt> DiagnosesExt { get; set; } = new List<ClaimResponseDiagnosisExt>();

 /// <summary>
    /// Collection of supporting information in response (from extension)
    /// </summary>
  public ICollection<ClaimResponseSupportingInfoExt> SupportingInfoExt { get; set; } = new List<ClaimResponseSupportingInfoExt>();

    /// <summary>
    /// FHIR ClaimResponse JSON for storage
    /// </summary>
    public string? FhirClaimResponseJson { get; set; } = string.Empty;

    // ===== NPHIES RCM adjudication summary =====
    public string? Outcome { get; set; }
    public string? AdjudicationOutcome { get; set; }
    public string? Disposition { get; set; }
    /// <summary>MessageHeader id of the response message (idempotency key for ingestion).</summary>
    public string? ResponseMessageId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public decimal? TotalSubmitted { get; set; }
    public decimal? TotalEligible { get; set; }
    public decimal? TotalBenefit { get; set; }
    public decimal? TotalCopay { get; set; }
    public decimal? TotalTax { get; set; }
    public decimal? TotalPatientShare { get; set; }
    public decimal? PaymentAmount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public ICollection<ClaimResponseItem> Items { get; set; } = new List<ClaimResponseItem>();
}
