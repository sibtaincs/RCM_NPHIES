namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Pre-Authorization Request entity
/// FHIR Resource: Claim with use=preauthorization
/// Represents NPHIES pre-authorization requests (prior authorization)
/// </summary>
public class PreAuthorizationRequest : BaseEntity
{
    /// <summary>
    /// Request identifier (unique per request)
    /// </summary>
    public string RequestIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Request identifier system (e.g., "http://provider.com/preauth-id")
    /// </summary>
    public string? RequestIdentifierSystem { get; set; }

    /// <summary>
/// Status: draft, active, cancelled, completed
/// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Type: oral, pharmacy, institutional, professional, vision
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Priority: stat, normal, deferred
    /// </summary>
    public string? Priority { get; set; }

    // Foreign Keys

  /// <summary>
    /// Patient ID (beneficiary)
    /// </summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>
    /// Provider organization ID (requester)
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

  /// <summary>
    /// Insurer organization ID (target)
    /// </summary>
    public string InsurerId { get; set; } = string.Empty;

    /// <summary>
    /// Coverage ID (insurance policy reference)
    /// </summary>
    public string? CoverageId { get; set; }

    // Service Information

    /// <summary>
    /// Service period start date
  /// </summary>
    public DateTime? ServicedPeriodStart { get; set; }

    /// <summary>
    /// Service period end date
    /// </summary>
    public DateTime? ServicedPeriodEnd { get; set; }

    /// <summary>
    /// Single service date (alternative to period)
    /// </summary>
    public DateTime? ServicedDate { get; set; }

    /// <summary>
    /// Date when the pre-auth request was entered/created
 /// </summary>
    public DateTime EnteredDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Purpose: preauthorization, predetermination
  /// </summary>
    public string? Purpose { get; set; }

 // FHIR Message Reference

    /// <summary>
    /// Message header ID (FHIR message envelope)
    /// </summary>
public string? MessageHeaderId { get; set; }

    /// <summary>
 /// Complete FHIR request bundle (JSON)
    /// </summary>
    public string? FhirRequestBundle { get; set; }
    public string? RequestId { get; set; }
    public string? RequestedDate { get; set; }


    // Navigation Properties

  /// <summary>
    /// Patient (beneficiary)
    /// </summary>
    public Patient? Patient { get; set; }

 /// <summary>
    /// Provider organization
    /// </summary>
    public Organization? Provider { get; set; }

    /// <summary>
    /// Insurer organization
    /// </summary>
    public Organization? Insurer { get; set; }

  /// <summary>
    /// Coverage (insurance policy)
    /// </summary>
    public Coverage? Coverage { get; set; }

    /// <summary>
    /// Message header
    /// </summary>
 public MessageHeader? MessageHeader { get; set; }

    /// <summary>
    /// Pre-authorization items (services/products requested)
    /// </summary>
    public ICollection<PreAuthorizationItem> Items { get; set; } = new List<PreAuthorizationItem>();

    /// <summary>
    /// Pre-authorization diagnoses
    /// </summary>
    public ICollection<PreAuthorizationDiagnosis> Diagnoses { get; set; } = new List<PreAuthorizationDiagnosis>();

    /// <summary>
    /// Supporting information
    /// </summary>
  public ICollection<PreAuthorizationSupportingInfo> SupportingInfo { get; set; } = new List<PreAuthorizationSupportingInfo>();

    /// <summary>
    /// Pre-authorization response (if received)
    /// </summary>
    public PreAuthorizationResponse? Response { get; set; }

    /// <summary>
    /// Check if pre-authorization has been completed
    /// </summary>
    public bool IsCompleted => Status == "completed";

    /// <summary>
    /// Get service date display
    /// </summary>
 public string GetServiceDateDisplay()
    {
      if (ServicedDate.HasValue)
    return ServicedDate.Value.ToString("yyyy-MM-dd");
        
        if (ServicedPeriodStart.HasValue && ServicedPeriodEnd.HasValue)
            return $"{ServicedPeriodStart.Value:yyyy-MM-dd} to {ServicedPeriodEnd.Value:yyyy-MM-dd}";
    
        return "Not specified";
 }
}

/// <summary>
/// Pre-Authorization Item entity
/// Represents individual service/product in pre-auth request
/// </summary>
public class PreAuthorizationItem : BaseEntity
{
    /// <summary>
    /// Pre-authorization request ID
    /// </summary>
    public string PreAuthorizationRequestId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number
    /// </summary>
    public int Sequence { get; set; }
    public int ServiceCode { get; set; }
    public int ServiceSystem { get; set; }

    /// <summary>
    /// Care team sequence reference
    /// </summary>
    public int? CareTeamSequence { get; set; }

    /// <summary>
    /// Diagnosis sequence reference (comma-separated if multiple)
    /// </summary>
 public string? DiagnosisSequence { get; set; }

    /// <summary>
    /// Information sequence reference (comma-separated if multiple)
    /// </summary>
    public string? InformationSequence { get; set; }

    /// <summary>
    /// Product or service code
    /// </summary>
    public string ProductOrServiceCode { get; set; } = string.Empty;

    /// <summary>
    /// Product or service system (e.g., CPT, HCPCS)
    /// </summary>
    public string? ProductOrServiceSystem { get; set; }

    /// <summary>
    /// Product or service description/display
    /// </summary>
    public string? ProductOrServiceDescription { get; set; }

    /// <summary>
    /// Service date (single date)
    /// </summary>
    public DateTime? ServicedDate { get; set; }

    /// <summary>
    /// Service period start
/// </summary>
    public DateTime? ServicedPeriodStart { get; set; }

    /// <summary>
    /// Service period end
    /// </summary>
    public DateTime? ServicedPeriodEnd { get; set; }

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal? UnitPrice { get; set; }

/// <summary>
 /// Net amount (quantity * unit price)
    /// </summary>
    public decimal? NetAmount { get; set; }

    /// <summary>
    /// Body site code
    /// </summary>
    public string? BodySiteCode { get; set; }

  /// <summary>
    /// Body site system
  /// </summary>
    public string? BodySiteSystem { get; set; }

    /// <summary>
    /// Sub-site code
    /// </summary>
    public string? SubSiteCode { get; set; }

    /// <summary>
    /// Currency code (e.g., SAR)
    /// </summary>
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Location where service will be performed
  /// </summary>
    public string? LocationId { get; set; }

    /// <summary>
    /// Practitioner who will perform the service
    /// </summary>
    public string? PractitionerId { get; set; }

    /// <summary>
 /// Additional notes
/// </summary>
    public string? Notes { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent pre-authorization request
    /// </summary>
    public PreAuthorizationRequest? PreAuthorizationRequest { get; set; }

    /// <summary>
    /// Service location
    /// </summary>
    public Location? Location { get; set; }

    /// <summary>
    /// Practitioner
    /// </summary>
    public Practitioner? Practitioner { get; set; }
}

/// <summary>
/// Pre-Authorization Diagnosis entity
/// </summary>
public class PreAuthorizationDiagnosis : BaseEntity
{
    /// <summary>
    /// Pre-authorization request ID
    /// </summary>
    public string PreAuthorizationRequestId { get; set; } = string.Empty;

 /// <summary>
    /// Sequence number
    /// </summary>
    public int Sequence { get; set; }


    /// <summary>
    /// Diagnosis code (ICD-10)
    /// </summary>
    public string DiagnosisCode { get; set; } = string.Empty;

    /// <summary>
    /// Diagnosis system (e.g., ICD-10)
    /// </summary>
    public string? DiagnosisSystem { get; set; }
    public string? DiagnosisDisplay { get; set; }

    /// <summary>
    /// Diagnosis type (admitting, principal, etc.)
/// </summary>
    public string? DiagnosisType { get; set; }

  /// <summary>
    /// On admission indicator
    /// </summary>
    public string? OnAdmission { get; set; }

    /// <summary>
    /// Additional notes
  /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent pre-authorization request
/// </summary>
    public PreAuthorizationRequest? PreAuthorizationRequest { get; set; }
}

/// <summary>
/// Pre-Authorization Supporting Info entity
/// </summary>
public class PreAuthorizationSupportingInfo : BaseEntity
{
    /// <summary>
    /// Pre-authorization request ID
    /// </summary>
    public string PreAuthorizationRequestId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
  /// Category (e.g., info, onset, hospitalized)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Category system
    /// </summary>
  public string? CategorySystem { get; set; }
  public string? CodeSystem { get; set; }
  public string? QuantityUnit { get; set; }
  public string? QuantitySystem { get; set; }

    /// <summary>
    /// Code value
    /// </summary>
    public string? CodeValue { get; set; }

    /// <summary>
    /// String value
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
  /// Date value
    /// </summary>
    public DateTime? DateValue { get; set; }

    /// <summary>
    /// Quantity value
    /// </summary>
  public decimal? QuantityValue { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
 public string? Notes { get; set; }

 // Navigation Properties

    /// <summary>
    /// Parent pre-authorization request
    /// </summary>
    public PreAuthorizationRequest? PreAuthorizationRequest { get; set; }
}

/// <summary>
/// Pre-Authorization Response entity
/// FHIR Resource: ClaimResponse for pre-authorization
/// </summary>
public class PreAuthorizationResponse : BaseEntity
{
    /// <summary>
    /// Pre-authorization request ID
    /// </summary>
    public string PreAuthorizationRequestId { get; set; } = string.Empty;

    /// <summary>
    /// Response identifier
    /// </summary>
    public string? ResponseIdentifier { get; set; }
    public string? ResponseId { get; set; }

    /// <summary>
    /// Response identifier system
    /// </summary>
    public string? ResponseIdentifierSystem { get; set; }

    /// <summary>
    /// Status: active, cancelled, draft, entered-in-error
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Outcome: queued, complete, error, partial
    /// </summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>
    /// Disposition (human-readable outcome)
    /// </summary>
    public string? Disposition { get; set; }

    /// <summary>
    /// Pre-authorization reference number (approval number)
    /// </summary>
    public string? PreAuthRef { get; set; }

    /// <summary>
  /// Validity period start (when authorization becomes valid)
    /// </summary>
    public DateTime? ValidityPeriodStart { get; set; }

    /// <summary>
 /// Validity period end (when authorization expires)
    /// </summary>
    public DateTime? ValidityPeriodEnd { get; set; }

    /// <summary>
    /// Patient ID (from response)
    /// </summary>
    public string? PatientId { get; set; }

    /// <summary>
    /// Insurer ID (from response)
    /// </summary>
    public string? InsurerId { get; set; }

    /// <summary>
    /// Requestor ID (provider who made request)
    /// </summary>
 public string? RequestorId { get; set; }

    /// <summary>
    /// Response date
    /// </summary>
    public DateTime ResponseDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Complete FHIR response bundle (JSON)
    /// </summary>
    public string? FhirResponseBundle { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent pre-authorization request
    /// </summary>
public PreAuthorizationRequest? PreAuthorizationRequest { get; set; }

  /// <summary>
    /// Patient
    /// </summary>
    public Patient? Patient { get; set; }

    /// <summary>
    /// Insurer organization
    /// </summary>
 public Organization? Insurer { get; set; }

    /// <summary>
    /// Requestor organization
    /// </summary>
    public Organization? Requestor { get; set; }

    /// <summary>
    /// Response items (approved/denied items)
    /// </summary>
    public ICollection<PreAuthorizationResponseItem> ResponseItems { get; set; } = new List<PreAuthorizationResponseItem>();

    /// <summary>
    /// Response errors
    /// </summary>
    public ICollection<PreAuthorizationResponseError> Errors { get; set; } = new List<PreAuthorizationResponseError>();

    /// <summary>
    /// Check if pre-authorization is approved
    /// </summary>
    public bool IsApproved => Outcome == "complete" && !string.IsNullOrEmpty(PreAuthRef);

    /// <summary>
    /// Check if pre-authorization is still valid
    /// </summary>
    public bool IsValid()
    {
        if (!ValidityPeriodStart.HasValue || !ValidityPeriodEnd.HasValue)
     return false;

        var now = DateTime.UtcNow;
        return now >= ValidityPeriodStart.Value && now <= ValidityPeriodEnd.Value;
    }
}

/// <summary>
/// Pre-Authorization Response Item entity
/// </summary>
public class PreAuthorizationResponseItem : BaseEntity
{
    /// <summary>
    /// Pre-authorization response ID
  /// </summary>
    public string PreAuthorizationResponseId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number (in response)
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Request sequence number (links to request item sequence)
/// </summary>
    public int RequestSequence { get; set; }

  /// <summary>
    /// Decision: approved, denied, modified
    /// </summary>
    public string? Decision { get; set; }
    public string? ServiceCode { get; set; }
    public string? ServiceSystem { get; set; }
    public string? ServiceType { get; set; }
    public double? ApprovedUnitPrice { get; set; }

    /// <summary>
    /// Adjudication result (if different from decision)
    /// </summary>
    public string? AdjudicationResult { get; set; }

    /// <summary>
    /// Approved quantity
    /// </summary>
 public decimal? ApprovedQuantity { get; set; }

    /// <summary>
    /// Approved amount
    /// </summary>
    public decimal? ApprovedAmount { get; set; }

    /// <summary>
    /// Benefit amount (covered amount)
    /// </summary>
    public decimal? BenefitAmount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Denial reason (if denied)
    /// </summary>
    public string? DenialReason { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent pre-authorization response
 /// </summary>
    public PreAuthorizationResponse? PreAuthorizationResponse { get; set; }
}

/// <summary>
/// Pre-Authorization Response Error entity
/// </summary>
public class PreAuthorizationResponseError : BaseEntity
{
    /// <summary>
    /// Pre-authorization response ID
    /// </summary>
    public string PreAuthorizationResponseId { get; set; } = string.Empty;

    /// <summary>
    /// Error code
/// </summary>
  public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Error code system
    /// </summary>
    public string? ErrorCodeSystem { get; set; }
    public string? ErrorDetails { get; set; }
    public string? ErrorField { get; set; }
    public string? AdditionalContext { get; set; }
  
    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
  /// Severity: error, warning, information
    /// </summary>
    public string Severity { get; set; } = "error";

    /// <summary>
    /// Error location (field path)
    /// </summary>
    public string? ErrorLocation { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent pre-authorization response
    /// </summary>
    public PreAuthorizationResponse? PreAuthorizationResponse { get; set; }
}
