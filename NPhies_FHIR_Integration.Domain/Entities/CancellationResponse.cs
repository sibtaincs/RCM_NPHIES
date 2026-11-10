namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// CancellationResponse entity - represents FHIR Task resource for cancellation response operations
/// Used for responding to cancellation requests from insurer to provider
/// FHIR Resource: Task (Response variant for cancellations)
/// </summary>
public class CancellationResponse : BaseEntity
{
    /// <summary>
    /// Task ID (FHIR resource ID)
    /// Example: "392930" (same as request)
 /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
 /// Identifier system (e.g., "http://pseudo-payer.com.sa/task")
    /// System URL for the task identifier
    /// </summary>
  public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value (e.g., "resp_49243")
    /// Unique identifier within the system
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Referenced Request ID (original cancellation request ID)
    /// Example: "af4bf225-05df-4435-a6ba-73008c0d2930"
    /// Links this response to the original request
/// </summary>
    public string? ReferencedRequestId { get; set; }

    /// <summary>
    /// Cancellation Request ID (foreign key to CancellationRequest)
    /// FK ? CancellationRequest
    /// </summary>
    public string? CancellationRequestId { get; set; }

    /// <summary>
    /// Cancellation Request (navigation property)
    /// Reference to the original cancellation request
    /// </summary>
    public CancellationRequest? CancellationRequest { get; set; }

    /// <summary>
  /// Task Status: "completed", "failed", "in-progress", "rejected"
    /// FHIR: TaskStatus
    /// Current state of the cancellation response
 /// </summary>
    public string Status { get; set; } = "completed";

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
    /// The type of task/action being responded to
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
    /// Response Code: "ok", "error", "partial-success", "timeout"
    /// Status of the response
    /// </summary>
    public string? ResponseCode { get; set; } = "ok";

  /// <summary>
    /// Response Message (human-readable response)
    /// Example: "Claim cancellation approved"
    /// Additional information about the response
    /// </summary>
  public string? ResponseMessage { get; set; }

    /// <summary>
    /// Response Status Code (HTTP-like status)
    /// 200: OK, 400: Bad Request, 404: Not Found, 500: Error
    /// </summary>
    public int? ResponseStatusCode { get; set; } = 200;

    /// <summary>
    /// Authored On (when the response was created)
    /// Date: "2022-08-03"
/// When the cancellation response was created
    /// </summary>
    public DateTime? AuthoredOn { get; set; }

    /// <summary>
    /// Last Modified (when the response was last updated)
    /// Date: "2022-08-03"
    /// When the cancellation response was last modified
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Requester Organization ID (who requested the action)
    /// FK ? Organization (typically Provider)
    /// Example: "b1b3432921324f97af3be9fd0b1a14ae"
    /// </summary>
    public string? RequesterId { get; set; }

    /// <summary>
    /// Requester Organization (navigation property)
    /// The provider/organization who requested the cancellation
    /// </summary>
    public Organization? Requester { get; set; }

    /// <summary>
    /// Owner Organization ID (who handled/responded)
    /// FK ? Organization (typically Insurer)
    /// Example: "bff3aa1fbd3648619ac082357bf135db"
/// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Owner Organization (navigation property)
    /// The insurer/payer who handled this cancellation request
  /// </summary>
    public Organization? Owner { get; set; }

    /// <summary>
/// Description (response description)
    /// Additional details about the response
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Result Text (outcome of the task)
 /// What was accomplished
    /// </summary>
    public string? ResultText { get; set; }

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
    public string ProcessingStatus { get; set; } = "received";

  /// <summary>
    /// Get cancellation response summary
 /// </summary>
    public string GetSummary()
    {
    var response = ResponseCode ?? "unknown";
 return $"Cancellation Response ({GetCodeDisplay()}): {GetStatusDisplay()} - Response: {response}";
    }

    /// <summary>
    /// Get status display text
    /// </summary>
    public string GetStatusDisplay()
    {
  return Status switch
        {
            "completed" => "Completed",
"failed" => "Failed",
   "in-progress" => "In Progress",
         "rejected" => "Rejected",
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
    /// Get response code display text
    /// </summary>
    public string GetResponseCodeDisplay()
    {
        return ResponseCode switch
      {
  "ok" => "OK",
            "error" => "Error",
       "partial-success" => "Partial Success",
    "timeout" => "Timeout",
     _ => ResponseCode ?? "Unknown"
     };
    }

    /// <summary>
    /// Check if response was successful
 /// </summary>
    public bool IsSuccessful()
    {
        return ResponseCode == "ok" && (ResponseStatusCode == 200 || ResponseStatusCode == 201);
}

    /// <summary>
    /// Check if response indicates an error
    /// </summary>
    public bool IsError()
    {
    return ResponseCode == "error" || ResponseStatusCode >= 400;
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
    /// Get full cancellation response details
    /// </summary>
    public string GetFullDetails()
    {
    return $"""
Cancellation Response: {TaskId}
Status: {GetStatusDisplay()}
Code: {GetCodeDisplay()}
Priority: {GetPriorityDisplay()}
Focus: {GetFocusReference()}
Response Code: {GetResponseCodeDisplay()}
Response Status: {ResponseStatusCode}
Response Message: {ResponseMessage ?? "N/A"}
Result: {ResultText ?? "N/A"}
Requester: {Requester?.OrganizationName ?? "Unknown"}
Owner: {Owner?.OrganizationName ?? "Unknown"}
Authored On: {AuthoredOn:yyyy-MM-dd}
Last Modified: {LastModified:yyyy-MM-dd}
Description: {Description ?? "N/A"}
""";
    }

    /// <summary>
    /// Check if response is for a cancel request
    /// </summary>
    public bool IsCancelResponse()
    {
        return Code == "cancel";
    }

    /// <summary>
    /// Check if cancellation is completed successfully
    /// </summary>
  public bool IsCompletedSuccessfully()
    {
        return IsCompleted() && IsSuccessful();
    }

    /// <summary>
    /// Check if cancellation is completed
    /// </summary>
  public bool IsCompleted()
    {
        return Status == "completed";
    }

    /// <summary>
    /// Check if cancellation failed
  /// </summary>
  public bool IsFailed()
    {
        return Status == "failed" || IsError();
 }
}
