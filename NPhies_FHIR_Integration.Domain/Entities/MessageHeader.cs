namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// MessageHeader entity - represents FHIR message envelope and routing information
/// FHIR Resource: MessageHeader
/// This is essential for every NPhies message (request/response)
/// </summary>
public class MessageHeader : BaseEntity
{
    /// <summary>
    /// Unique message identifier (UUID format)
    /// </summary>
    public string MessageUUID { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for linking request to response
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Event code identifying message type
    /// Examples: "eligibility-check", "eligibility-response", "preauth-request", "claim", etc.
    /// System: http://nphies.sa/terminology/CodeSystem/message-events
 /// </summary>
    public string EventCode { get; set; } = string.Empty;

    /// <summary>
 /// Event code system
 /// </summary>
    public string EventSystem { get; set; } = "http://nphies.sa/terminology/CodeSystem/message-events";

    // Sender/Receiver Information
    /// <summary>
    /// Reference to sending organization ID
    /// </summary>
    public string SenderOrganizationId { get; set; } = string.Empty;

    /// <summary>
    /// The organization sending the message
    /// </summary>
    public Organization? SenderOrganization { get; set; }

    /// <summary>
    /// Sender organization license system (e.g., "http://nphies.sa/license/provider-license")
    /// </summary>
    public string? SenderLicenseSystem { get; set; }

    /// <summary>
    /// Sender organization license value (provider license number)
    /// </summary>
    public string? SenderOrganizationLicense { get; set; }

    /// <summary>
    /// Name of the destination (typically "NPhies Platform")
    /// </summary>
    public string DestinationName { get; set; } = "NPhies Platform";

    /// <summary>
    /// Destination endpoint URL
    /// </summary>
    public string DestinationEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Destination organization ID (payer/insurer ID)
    /// </summary>
    public string? DestinationOrganizationId { get; set; }

  /// <summary>
    /// Destination organization license system (e.g., "http://nphies.sa/license/payer-license")
    /// </summary>
    public string? DestinationLicenseSystem { get; set; }

    // Focus Resource Information
    /// <summary>
    /// Primary resource type in this message (CoverageEligibilityRequest, Claim, etc.)
    /// </summary>
    public string FocusResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Primary resource ID being referenced
    /// </summary>
    public string FocusResourceId { get; set; } = string.Empty;

    // Source System Information
    /// <summary>
    /// Name of the source system
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Source system endpoint/URL
    /// </summary>
    public string SourceEndpoint { get; set; } = string.Empty;

    // Timestamps
    /// <summary>
/// When the message was created
/// </summary>
    public DateTime MessageTimestamp { get; set; }

    /// <summary>
    /// When the response was received (if applicable)
    /// </summary>
    public DateTime? ResponseTimestamp { get; set; }

 // Status Tracking
    /// <summary>
    /// Message status: "sent", "received", "acknowledged", "processed", "error", "failed"
 /// </summary>
    public string Status { get; set; } = "sent";

    /// <summary>
    /// Response status: "ok", "error", "fatal", "warning"
    /// </summary>
    public string ResponseStatus { get; set; } = string.Empty;

    /// <summary>
    /// Response identifier (from MessageHeader response.identifier)
    /// </summary>
    public string? ResponseIdentifier { get; set; }

    /// <summary>
    /// Response code (from MessageHeader response.code) - "ok", "error", etc.
    /// </summary>
    public string? ResponseCode { get; set; }

    /// <summary>
    /// Error code if message processing failed
  /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Error message if message processing failed
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

// Raw Content Storage
    /// <summary>
    /// Complete FHIR Bundle JSON content (stored for audit/compliance)
    /// </summary>
  public string BundleContent { get; set; } = string.Empty;

    /// <summary>
    /// Response bundle content (for messages that have responses)
    /// </summary>
    public string ResponseBundleContent { get; set; } = string.Empty;

    // Navigation Properties
    /// <summary>
    /// Collection of related eligibility requests
    /// </summary>
    public ICollection<CoverageEligibilityRequest> EligibilityRequests { get; set; } = new List<CoverageEligibilityRequest>();

    /// <summary>
    /// Collection of related eligibility responses
    /// </summary>
    public ICollection<CoverageEligibilityResponse> EligibilityResponses { get; set; } = new List<CoverageEligibilityResponse>();

    /// <summary>
    /// Collection of related claims
    /// </summary>
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();

    // Methods
    /// <summary>
    /// Check if this is a request message
    /// </summary>
  public bool IsRequest => EventCode switch
    {
        "eligibility-check" => true,
        "eligibility-discovery" => true,
        "preauth-request" => true,
        "claim" => true,
        "communication" => true,
        _ => false
  };

    /// <summary>
    /// Check if this is a response message
    /// </summary>
    public bool IsResponse => EventCode switch
    {
     "eligibility-response" => true,
        "preauth-response" => true,
        "claim-response" => true,
        _ => false
    };

    /// <summary>
    /// Check if message processing was successful
    /// </summary>
    public bool IsSuccessful => Status == "processed" || Status == "acknowledged";

    /// <summary>
    /// Check if message had an error
    /// </summary>
    public bool HasError => Status == "error" || Status == "failed" || !string.IsNullOrEmpty(ErrorCode);

    /// <summary>
    /// Mark message as successfully processed
    /// </summary>
    public void MarkAsProcessed()
    {
        Status = "processed";
    UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark message as failed with error details
    /// </summary>
  public void MarkAsFailed(string errorCode, string errorMessage)
    {
     Status = "error";
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }
}
