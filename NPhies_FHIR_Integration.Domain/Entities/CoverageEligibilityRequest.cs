namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// CoverageEligibilityRequest entity - represents a request to check insurance coverage
/// FHIR Resource: CoverageEligibilityRequest
/// </summary>
public class CoverageEligibilityRequest : BaseEntity
{
    /// <summary>
    /// Unique message identifier (UUID)
    /// Derived from FHIR Bundle ID
    /// </summary>
    public string MessageUUID { get; set; } = string.Empty;

    /// <summary>
    /// Request identifier
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Request identifier system (e.g., "http://pr-fhir.com.sa/CoverageEligibilityRequest")
    /// </summary>
    public string? RequestIdentifierSystem { get; set; }

    /// <summary>
    /// Request identifier value (typically same as RequestId)
    /// </summary>
  public string? RequestIdentifierValue { get; set; }

    // Message Tracking
    /// <summary>
    /// Reference to message header
    /// </summary>
    public string MessageHeaderId { get; set; } = string.Empty;

    /// <summary>
    /// The message header for this request
    /// </summary>
    public MessageHeader? MessageHeader { get; set; }

    // Request Details
    /// <summary>
    /// Request type: "validation" (check coverage) or "discovery" (find providers)
    /// </summary>
    public string RequestType { get; set; } = "validation";

    /// <summary>
 /// Priority coding system (e.g., "http://terminology.hl7.org/CodeSystem/processpriority")
  /// </summary>
    public string? PrioritySystem { get; set; }

    /// <summary>
  /// Purpose of request: "benefits" (coverage check), "discovery" (network check), or both
    /// Stored as JSON array string
    /// </summary>
    public string PurposeJson { get; set; } = "[\"benefits\"]";

    /// <summary>
    /// Request status: "active", "submitted", "acknowledged", "responded"
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Request priority: "normal", "urgent"
    /// </summary>
    public string Priority { get; set; } = "normal";

  // Service Details
    /// <summary>
    /// Date when request was created/submitted
    /// </summary>
    public DateTime? CreatedDate { get; set; }

    /// <summary>
    /// Date of service to check coverage for
    /// </summary>
    public DateTime ServiceDate { get; set; }

    /// <summary>
    /// Service period start date
    /// </summary>
    public DateTime ServicedPeriodStart { get; set; }

    /// <summary>
    /// Service period end date
    /// </summary>
    public DateTime ServicedPeriodEnd { get; set; }

    /// <summary>
    /// Service type (optional): "dental", "pharmacy", "medical", etc.
    /// </summary>
    public string ServiceType { get; set; } = string.Empty;

    // References
  /// <summary>
    /// Reference to patient ID
    /// </summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>
    /// The patient requesting eligibility check
    /// </summary>
    public Patient? Patient { get; set; }

    /// <summary>
    /// Reference to coverage ID
    /// </summary>
    public string CoverageId { get; set; } = string.Empty;

    /// <summary>
    /// The insurance coverage being checked
    /// </summary>
    public Coverage? Coverage { get; set; }

    /// <summary>
    /// Reference to provider organization ID
    /// </summary>
  public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// The provider requesting eligibility
    /// </summary>
    public Organization? Provider { get; set; }

    /// <summary>
    /// Reference to insurer organization ID
    /// </summary>
    public string InsurerId { get; set; } = string.Empty;

    /// <summary>
    /// The insurance company being queried
  /// </summary>
    public Organization? Insurer { get; set; }

    /// <summary>
    /// Reference to enterer (practitioner) ID
    /// </summary>
    public string EntererPractitionerId { get; set; } = string.Empty;

    /// <summary>
  /// The practitioner who entered the request
    /// </summary>
    public Practitioner? Enterer { get; set; }

    // Request Items
    /// <summary>
    /// Collection of service items being checked
    /// </summary>
  public ICollection<EligibilityItem> Items { get; set; } = new List<EligibilityItem>();

    // Request Tracking
    /// <summary>
    /// When the request was created
    /// </summary>
    public DateTime RequestCreatedAt { get; set; }

    /// <summary>
    /// When the request was submitted to NPhies
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// When response was received
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    // Response Information
    /// <summary>
    /// Reference to eligibility response
    /// </summary>
 public string ResponseId { get; set; } = string.Empty;

    /// <summary>
    /// Eligibility status from response: "active", "inactive", "pending"
    /// </summary>
    public string EligibilityStatus { get; set; } = string.Empty;

    /// <summary>
    /// Message processing status
    /// </summary>
    public string MessageStatus { get; set; } = "sent";

    // Raw FHIR Storage
    /// <summary>
    /// Complete FHIR request bundle JSON (for audit/compliance)
    /// </summary>
 public string FhirRequestBundle { get; set; } = string.Empty;

    /// <summary>
    /// Complete FHIR response bundle JSON (when received)
    /// </summary>
    public string FhirResponseBundle { get; set; } = string.Empty;

  // Navigation Properties
    /// <summary>
    /// The response to this request (1:1 relationship)
    /// </summary>
    public CoverageEligibilityResponse? Response { get; set; }

  // Methods
    /// <summary>
    /// Check if this is a benefits validation request
    /// </summary>
    public bool IsBenefitsValidation => PurposeJson.Contains("benefits", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if this is a provider discovery request
 /// </summary>
    public bool IsNetworkDiscovery => PurposeJson.Contains("discovery", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if request has been responded
    /// </summary>
  public bool HasResponse => !string.IsNullOrEmpty(ResponseId);

    /// <summary>
    /// Check if request is pending response
    /// </summary>
    public bool IsPending => Status == "submitted" || Status == "acknowledged";

    /// <summary>
    /// Mark request as submitted
    /// </summary>
    public void MarkAsSubmitted()
    {
    Status = "submitted";
SubmittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
  /// Mark request as responded
  /// </summary>
    public void MarkAsResponded()
    {
        Status = "responded";
        RespondedAt = DateTime.UtcNow;
      UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get purpose as array
    /// </summary>
    public List<string> GetPurposeArray()
    {
        try
     {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(PurposeJson) ?? new List<string>();
  }
     catch
        {
        return new List<string>();
   }
    }
}
