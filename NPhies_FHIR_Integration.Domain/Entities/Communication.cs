namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Communication entity - represents FHIR Communication resource
/// Response to a CommunicationRequest from provider to insurer/payer
/// Contains both text and attachment payloads
/// </summary>
public class Communication : BaseEntity
{
    /// <summary>
    /// Communication ID (FHIR resource ID)
    /// Example: "892284"
    /// </summary>
    public string CommunicationId { get; set; } = string.Empty;

  /// <summary>
    /// Identifier system (e.g., "http://saudicentralpharmacy.sa.com/communication")
    /// System URL for the communication identifier
/// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
 /// Identifier value (e.g., "Communication_20211202982284")
    /// Unique identifier within the system
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// BasedOn resource type (CommunicationRequest)
    /// What request this communication is responding to
    /// </summary>
    public string? BasedOnResourceType { get; set; }

  /// <summary>
    /// BasedOn identifier system
    /// System URL for the request being referenced
  /// </summary>
    public string? BasedOnIdentifierSystem { get; set; }

    /// <summary>
    /// BasedOn identifier value (CommunicationRequest ID)
    /// Example: "CommReq_302568"
    /// The request being responded to
    /// </summary>
    public string? BasedOnIdentifierValue { get; set; }

    /// <summary>
    /// Status: "in-progress", "completed", "entered-in-error", "not-done"
    /// Current state of the communication
    /// </summary>
    public string Status { get; set; } = "completed";

 /// <summary>
    /// Category: "instruction", "information-request", "reminder", "follow-up"
 /// System: http://terminology.hl7.org/CodeSystem/communication-category
    /// Type of communication
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
    /// Urgency level
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
    /// Example: "req_00112482284"
    /// The ID of the resource being discussed (e.g., claim ID)
    /// </summary>
    public string? AboutIdentifierValue { get; set; }

    /// <summary>
    /// Primary payload content (the actual response message)
    /// Example: "As per your request, please find the enclosed updated lab report..."
    /// The main communication content/response
    /// </summary>
    public string? PayloadContent { get; set; }

    /// <summary>
    /// Recipient organization ID (who receives this communication)
    /// FK ? Organization (typically Insurer/Payer)
    /// Example: "bff3aa1fbd3648619ac082357bf135db"
    /// </summary>
    public string? RecipientId { get; set; }

    /// <summary>
 /// Recipient organization (navigation property)
    /// The insurer/payer receiving this communication
    /// </summary>
    public Organization? Recipient { get; set; }

/// <summary>
    /// Sender organization ID (who sends this communication)
    /// FK ? Organization (typically Provider)
  /// Example: "b1b3432921324f97af3be9fd0b1a34fa"
    /// </summary>
    public string? SenderId { get; set; }

    /// <summary>
    /// Sender organization (navigation property)
    /// The provider sending this communication
    /// </summary>
    public Organization? Sender { get; set; }

    /// <summary>
    /// Payload attachment content type
    /// Example: "application/pdf"
    /// MIME type of attached file
    /// </summary>
    public string? PayloadAttachmentContentType { get; set; }

    /// <summary>
    /// Payload attachment data (binary file content)
    /// Base64 encoded or raw bytes
    /// The attached file (e.g., PDF report)
    /// </summary>
    public byte[]? PayloadAttachmentData { get; set; }

    /// <summary>
    /// Payload attachment title
    /// Example: "Lab Report"
    /// User-friendly name for attachment
    /// </summary>
    public string? PayloadAttachmentTitle { get; set; }

    /// <summary>
    /// Payload attachment creation date
    /// When the attachment was created
    /// </summary>
    public DateTime? PayloadAttachmentCreation { get; set; }

    /// <summary>
  /// FHIR Communication JSON for storage (backup/reference)
    /// Stores complete FHIR Communication resource as JSON
    /// </summary>
  public string? FhirCommunicationJson { get; set; }

    /// <summary>
    /// Message Header ID reference
    /// Reference to the message header in the bundle
/// </summary>
    public string? MessageHeaderId { get; set; }

    /// <summary>
    /// Processing status
    /// Values: pending, sent, received, acknowledged, failed, archived
    /// </summary>
    public string ProcessingStatus { get; set; } = "received";

    /// <summary>
    /// Date/time when communication was received/processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    // ===== NPHIES RCM workflow =====
    /// <summary>outbound (provider → payer) | inbound</summary>
    public string Direction { get; set; } = "outbound";
    public string? ClaimId { get; set; }
    /// <summary>Local CommunicationRequest.Id this communication answers (solicited).</summary>
    public string? CommunicationRequestId { get; set; }
    /// <summary>JSON array of payloads ({contentString} or {contentAttachment}).</summary>
    public string? PayloadsJson { get; set; }
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Get communication summary
    /// </summary>
    public string GetSummary()
    {
  var content = PayloadContent?.Substring(0, Math.Min(50, PayloadContent.Length)) ?? "N/A";
   return $"Communication ({GetStatusDisplay()}): {content}...";
    }

    /// <summary>
 /// Get status display text
    /// </summary>
    public string GetStatusDisplay()
    {
        return Status switch
        {
 "in-progress" => "In Progress",
         "completed" => "Completed",
            "entered-in-error" => "Entered in Error",
      "not-done" => "Not Done",
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
    /// Check if communication has attachment
    /// </summary>
    public bool HasAttachment()
    {
        return PayloadAttachmentData != null && PayloadAttachmentData.Length > 0;
    }

    /// <summary>
    /// Get attachment size in KB
    /// </summary>
    public decimal GetAttachmentSizeKB()
    {
        if (!HasAttachment()) return 0m;
  return Math.Round((decimal)PayloadAttachmentData!.Length / 1024, 2);
    }

    /// <summary>
    /// Check if communication is about a specific claim
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
    /// Get based-on request reference for display
    /// </summary>
    public string GetBasedOnReference()
    {
        if (string.IsNullOrEmpty(BasedOnResourceType) || string.IsNullOrEmpty(BasedOnIdentifierValue))
          return "Unknown";
     return $"{BasedOnResourceType}: {BasedOnIdentifierValue}";
    }

    /// <summary>
    /// Get full communication details
    /// </summary>
    public string GetFullDetails()
    {
var attachmentInfo = HasAttachment() ? $" ({PayloadAttachmentTitle}, {GetAttachmentSizeKB()} KB)" : "";
        return $"""
Communication: {CommunicationId}
Status: {GetStatusDisplay()}
Category: {GetCategoryDisplay()}
Priority: {GetPriorityDisplay()}
Patient: {SubjectPatient?.FirstName ?? "Unknown"} {SubjectPatient?.LastName ?? ""}
About: {GetAboutReference()}
Based On: {GetBasedOnReference()}
Recipient: {Recipient?.OrganizationName ?? "Unknown"}
Sender: {Sender?.OrganizationName ?? "Unknown"}
Message: {PayloadContent ?? "N/A"}
Attachment{attachmentInfo}
""";
    }

    /// <summary>
    /// Check if communication is completed
    /// </summary>
    public bool IsCompleted()
    {
  return Status == "completed";
    }

    /// <summary>
/// Check if communication is in progress
    /// </summary>
 public bool IsInProgress()
    {
        return Status == "in-progress";
 }

    /// <summary>
  /// Check if communication has error
    /// </summary>
    public bool HasError()
    {
        return Status == "entered-in-error" || ProcessingStatus == "failed";
    }
}
