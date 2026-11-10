namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// CommunicationRequest entity - represents communication/instruction requests from insurer to provider
/// FHIR Resource: CommunicationRequest
/// Used for sending instructions, requesting information, providing guidance, etc.
/// </summary>
public class CommunicationRequest : BaseEntity
{
    /// <summary>
    /// Communication Request ID (FHIR resource ID)
 /// Example: "856278"
    /// </summary>
    public string CommunicationRequestId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier system (e.g., "http://sni.com.sa/communicationrequest")
    /// System URL for the request identifier
    /// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value (e.g., "CommReq_306278")
    /// Unique identifier within the system
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Status: "active", "completed", "cancelled", "draft"
    /// FHIR: RequestStatus
    /// Current state of the communication request
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Category: "instruction", "information-request", "reminder", "follow-up"
    /// System: http://terminology.hl7.org/CodeSystem/communication-category
    /// Type of communication/request
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Category system URL
    /// Example: "http://terminology.hl7.org/CodeSystem/communication-category"
    /// </summary>
  public string? CategorySystem { get; set; }

    /// <summary>
    /// Priority: "routine", "urgent", "asap", "stat"
    /// System: http://terminology.hl7.org/CodeSystem/request-priority
    /// Urgency level of the request
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Subject patient ID (who the communication is about)
    /// FK ? Patient
    /// Example: "123456777"
    /// </summary>
    public string? SubjectPatientId { get; set; }

    /// <summary>
    /// Subject patient (navigation property)
    /// The patient this communication relates to
    /// </summary>
    public Patient? SubjectPatient { get; set; }

    /// <summary>
    /// About resource type (what the communication is about)
    /// Example: "Claim", "ClaimResponse", "Encounter"
    /// For tracking which resource this communication relates to
    /// </summary>
    public string? AboutResourceType { get; set; }

  /// <summary>
/// About identifier system (link to the resource)
 /// Example: "http://saudicentralpharmacy.sa.com/claim"
    /// System URL for the resource being referenced
    /// </summary>
    public string? AboutIdentifierSystem { get; set; }

    /// <summary>
    /// About identifier value (the actual ID)
    /// Example: "req_00112489089"
    /// The ID of the resource being discussed (e.g., claim ID)
    /// </summary>
    public string? AboutIdentifierValue { get; set; }

    /// <summary>
  /// Payload content (the actual request message)
    /// Example: "Please provide an updated lab report including HbAIC, LFT and Lipid profiles."
    /// The actual communication content/instructions
    /// </summary>
    public string? PayloadContent { get; set; }

    /// <summary>
    /// Recipient organization ID (who receives this request)
    /// FK ? Organization (typically Provider)
    /// Example: "b1b3432921324f97af3be9fd0b1a34fa"
    /// </summary>
    public string? RecipientId { get; set; }

    /// <summary>
    /// Recipient organization (navigation property)
    /// The provider/organization receiving this communication
    /// </summary>
    public Organization? Recipient { get; set; }

 /// <summary>
    /// Sender organization ID (who sends this request)
    /// FK ? Organization (typically Insurer)
    /// Example: "bff3aa1fbd3648619ac082357bf135db"
    /// </summary>
    public string? SenderId { get; set; }

    /// <summary>
    /// Sender organization (navigation property)
    /// The insurer sending this communication
    /// </summary>
    public Organization? Sender { get; set; }

    /// <summary>
    /// FHIR CommunicationRequest JSON for storage (backup/reference)
    /// Stores complete FHIR CommunicationRequest resource as JSON
 /// </summary>
    public string? FhirCommunicationRequestJson { get; set; }

    // ===== NPHIES RCM workflow =====
    public string? ClaimId { get; set; }
    /// <summary>MessageHeader id of the inbound message (idempotency key).</summary>
    public string? MessageId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Reason { get; set; }
    /// <summary>JSON array of requested attachments / payload texts.</summary>
    public string? PayloadsJson { get; set; }
    public string? RespondedCommunicationId { get; set; }
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Get request summary
    /// </summary>
    public string GetSummary()
    {
    var content = PayloadContent?.Substring(0, Math.Min(50, PayloadContent.Length)) ?? "N/A";
        return $"Communication ({GetCategoryDisplay()}): {GetStatusDisplay()} - {content}...";
  }

    /// <summary>
    /// Get status display text
    /// </summary>
    public string GetStatusDisplay()
    {
  return Status switch
        {
     "active" => "Active",
      "completed" => "Completed",
"cancelled" => "Cancelled",
            "draft" => "Draft",
            _ => Status ?? "Unknown"
        };
    }

    /// <summary>
    /// Get category display text
    /// </summary>
    public string GetCategoryDisplay()
    {
        return Category switch
        {
       "instruction" => "Instruction",
         "information-request" => "Information Request",
        "reminder" => "Reminder",
   "follow-up" => "Follow-up",
          _ => Category ?? "Unknown"
    };
    }

    /// <summary>
    /// Get priority display text
    /// </summary>
    public string GetPriorityDisplay()
    {
        return Priority switch
        {
         "routine" => "Routine",
     "urgent" => "Urgent",
         "asap" => "ASAP",
            "stat" => "Stat",
            _ => Priority ?? "Unknown"
        };
    }

    /// <summary>
  /// Check if request is about a specific claim
    /// </summary>
    public bool IsAboutClaim()
    {
        return AboutResourceType == "Claim" && !string.IsNullOrEmpty(AboutIdentifierValue);
    }

    /// <summary>
    /// Get about resource reference for display
    /// </summary>
    public string GetAboutReference()
    {
        if (string.IsNullOrEmpty(AboutResourceType) || string.IsNullOrEmpty(AboutIdentifierValue))
 return "Unknown";
        return $"{AboutResourceType}: {AboutIdentifierValue}";
    }

    /// <summary>
    /// Get full communication request details
    /// </summary>
    public string GetFullDetails()
 {
        return $"""
Communication Request: {CommunicationRequestId}
Status: {GetStatusDisplay()}
Category: {GetCategoryDisplay()}
Priority: {GetPriorityDisplay()}
Patient: {SubjectPatient?.FirstName ?? "Unknown"} {SubjectPatient?.LastName ?? ""}
About: {GetAboutReference()}
Recipient: {Recipient?.OrganizationName ?? "Unknown"}
Sender: {Sender?.OrganizationName ?? "Unknown"}
Message: {PayloadContent ?? "N/A"}
""";
    }

    /// <summary>
    /// Check if request is active/pending
    /// </summary>
    public bool IsActive()
    {
  return Status == "active" || Status == "draft";
    }

    /// <summary>
    /// Check if request is completed
/// </summary>
    public bool IsCompleted()
{
        return Status == "completed";
    }

    /// <summary>
    /// Check if request is cancelled
    /// </summary>
    public bool IsCancelled()
    {
     return Status == "cancelled";
    }
}
