namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// CancellationRequest entity - represents FHIR Task resource for cancellation request operations
/// Used for requesting claim cancellations from provider to insurer
/// FHIR Resource: Task (Request variant for cancellations)
/// </summary>
public class CancellationRequest : BaseEntity
{
    /// <summary>
    /// Task ID (FHIR resource ID)
/// Example: "392930"
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
  /// Identifier system (e.g., "http://saudidentalclinic.com.sa/task")
  /// System URL for the task identifier
/// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value (e.g., "Cancel_682930")
    /// Unique identifier within the system
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Task Status: "requested", "in-progress", "completed", "failed", "cancelled"
    /// FHIR: TaskStatus
 /// Current state of the task request
    /// </summary>
    public string Status { get; set; } = "requested";

    /// <summary>
    /// Task Intent: "order", "plan", "option", "proposal"
    /// FHIR: TaskIntent
    /// The intent of the task
    /// </summary>
    public string Intent { get; set; } = "order";

    /// <summary>
    /// Priority: "routine", "urgent", "asap", "stat"
  /// System: http://terminology.hl7.org/CodeSystem/request-priority
    /// Urgency level of the task
 /// </summary>
    public string? Priority { get; set; } = "routine";

    /// <summary>
    /// Task Code: "cancel", "review", "approve", "reject"
  /// System: http://nphies.sa/terminology/CodeSystem/task-code
    /// The type of task/action being requested
 /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Task Code System URL
    /// Example: "http://nphies.sa/terminology/CodeSystem/task-code"
    /// </summary>
  public string? CodeSystem { get; set; }

  /// <summary>
    /// Focus Resource Type (what the task is about)
    /// Example: "Claim"
/// The resource being acted upon
  /// </summary>
    public string? FocusResourceType { get; set; }

  /// <summary>
    /// Focus Identifier System (link to the resource)
    /// Example: "http://saudicentralpharmacy.sa.com/claim"
    /// System URL for the resource being referenced
    /// </summary>
    public string? FocusIdentifierSystem { get; set; }

  /// <summary>
    /// Focus Identifier Value (the actual ID)
    /// Example: "req_00112482930"
    /// The ID of the resource being acted upon (e.g., claim ID)
    /// </summary>
    public string? FocusIdentifierValue { get; set; }

    /// <summary>
  /// Reason Code: "WI" (Wrong Item), "PR" (Prior Request), etc.
/// System: http://nphies.sa/terminology/CodeSystem/task-reason-code
    /// The reason for the task
    /// </summary>
    public string? ReasonCode { get; set; }

    /// <summary>
    /// Reason Code System URL
    /// Example: "http://nphies.sa/terminology/CodeSystem/task-reason-code"
    /// </summary>
    public string? ReasonCodeSystem { get; set; }

    /// <summary>
    /// Reason Text (human-readable reason)
    /// Additional explanation for the task
    /// </summary>
  public string? ReasonText { get; set; }

 /// <summary>
    /// Authored On (when the task was created)
    /// Date: "2021-12-02"
    /// When the task request was created
    /// </summary>
    public DateTime? AuthoredOn { get; set; }

    /// <summary>
    /// Last Modified (when the task was last updated)
    /// Date: "2021-12-02"
/// When the task request was last modified
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Requester Organization ID (who is requesting the action)
    /// FK ? Organization (typically Provider)
    /// Example: "b1b3432921324f97af3be9fd0b1a14ae"
    /// </summary>
    public string? RequesterId { get; set; }

    /// <summary>
    /// Requester Organization (navigation property)
/// The provider/organization requesting the action
    /// </summary>
  public Organization? Requester { get; set; }

    /// <summary>
    /// Owner Organization ID (who should handle the action)
    /// FK ? Organization (typically Insurer)
    /// Example: "bff3aa1fbd3648619ac082357bf135db"
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Owner Organization (navigation property)
    /// The insurer/payer who owns this task
/// </summary>
    public Organization? Owner { get; set; }

    /// <summary>
    /// Description (task description)
  /// Additional details about the task
    /// </summary>
 public string? Description { get; set; }

    /// <summary>
    /// FHIR Task JSON for storage (backup/reference)
    /// Stores complete FHIR Task resource as JSON
 /// </summary>
    public string? FhirTaskJson { get; set; }

  /// <summary>
    /// Message Header ID reference
    /// Reference to the message header in the bundle
    /// </summary>
    public string? MessageHeaderId { get; set; }

    /// <summary>
 /// Processing Status
    /// Values: pending, sent, received, acknowledged, failed
    /// </summary>
    public string ProcessingStatus { get; set; } = "pending";

    /// <summary>
  /// Get task summary
    /// </summary>
    public string GetSummary()
    {
        var reason = ReasonCode ?? "N/A";
  return $"Cancellation Request ({GetCodeDisplay()}): {GetStatusDisplay()} - Reason: {reason}";
    }

    /// <summary>
    /// Get status display text
    /// </summary>
    public string GetStatusDisplay()
  {
     return Status switch
{
  "requested" => "Requested",
       "in-progress" => "In Progress",
  "completed" => "Completed",
   "failed" => "Failed",
      "cancelled" => "Cancelled",
  _ => Status ?? "Unknown"
        };
 }

    /// <summary>
    /// Get code display text
    /// </summary>
    public string GetCodeDisplay()
    {
   return Code switch
     {
     "cancel" => "Cancel",
    "review" => "Review",
"approve" => "Approve",
      "reject" => "Reject",
      _ => Code ?? "Unknown"
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
    /// Get reason code display text
    /// </summary>
    public string GetReasonCodeDisplay()
    {
     return ReasonCode switch
      {
    "WI" => "Wrong Item",
   "PR" => "Prior Request",
    "PC" => "Patient Change",
      "UC" => "Unable to Complete",
    _ => ReasonCode ?? "Unknown"
        };
    }

  /// <summary>
/// Check if task is about a specific claim
    /// </summary>
    public bool IsAboutClaim()
    {
        return FocusResourceType == "Claim" && !string.IsNullOrEmpty(FocusIdentifierValue);
    }

    /// <summary>
    /// Get focus resource reference for display
    /// </summary>
  public string GetFocusReference()
  {
        if (string.IsNullOrEmpty(FocusResourceType) || string.IsNullOrEmpty(FocusIdentifierValue))
    return "Unknown";
     return $"{FocusResourceType}: {FocusIdentifierValue}";
    }

    /// <summary>
    /// Get full task details
    /// </summary>
    public string GetFullDetails()
    {
        return $"""
Cancellation Request: {TaskId}
Status: {GetStatusDisplay()}
Code: {GetCodeDisplay()}
Priority: {GetPriorityDisplay()}
Focus: {GetFocusReference()}
Reason: {GetReasonCodeDisplay()} ({ReasonText ?? "No additional reason"})
Requester: {Requester?.OrganizationName ?? "Unknown"}
Owner: {Owner?.OrganizationName ?? "Unknown"}
Authored On: {AuthoredOn:yyyy-MM-dd}
Last Modified: {LastModified:yyyy-MM-dd}
Description: {Description ?? "N/A"}
""";
    }

    /// <summary>
    /// Check if task is active
    /// </summary>
    public bool IsActive()
    {
      return Status == "requested" || Status == "in-progress";
    }

    /// <summary>
    /// Check if task is completed
    /// </summary>
    public bool IsCompleted()
{
        return Status == "completed";
    }

    /// <summary>
    /// Check if task failed
    /// </summary>
  public bool IsFailed()
    {
     return Status == "failed";
    }

    /// <summary>
    /// Check if task is a cancel request
    /// </summary>
    public bool IsCancelRequest()
    {
        return Code == "cancel";
    }
}
