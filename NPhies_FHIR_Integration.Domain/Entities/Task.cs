namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// PollTask entity - represents work items, poll requests, or async operations
/// FHIR Resource: Task
/// Used for polling, scheduling, and other async operations in NPhies
/// </summary>
public class PollTask : BaseEntity
{
    /// <summary>
    /// Task ID (FHIR resource ID)
    /// Example: "2342905"
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Task identifier system (e.g., "http://sgh.com.sa/task")
    /// System URL for the task identifier
    /// </summary>
 public string? TaskIdentifierSystem { get; set; }

/// <summary>
    /// Task identifier value (e.g., "PlReq_202112022342905")
  /// Unique identifier value within the system
    /// </summary>
 public string? TaskIdentifierValue { get; set; }

    /// <summary>
    /// Task status: "draft", "requested", "received", "accepted", "rejected", 
    /// "ready", "cancelled", "in-progress", "on-hold", "failed", "completed", "entered-in-error"
    /// FHIR: TaskStatus code
    /// </summary>
    public string Status { get; set; } = "requested";

    /// <summary>
    /// Task intent: "proposal", "plan", "order", "original-order", "reflex-order", 
    /// "filler-order", "instance-order", "option"
  /// FHIR: TaskIntent code
    /// </summary>
  public string Intent { get; set; } = "order";

    /// <summary>
    /// Task priority: "stat", "asap", "urgent", "normal", "low"
    /// FHIR: RequestPriority code
    /// </summary>
    public string Priority { get; set; } = "normal";

    /// <summary>
    /// Task code (e.g., "poll", "notify", "execute")
    /// System: http://nphies.sa/terminology/CodeSystem/task-code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Task code system URL
    /// Example: "http://nphies.sa/terminology/CodeSystem/task-code"
    /// </summary>
    public string? CodeSystem { get; set; }

    /// <summary>
    /// Task code display text (human-readable)
    /// Example: "Poll Request"
    /// </summary>
    public string? CodeDisplay { get; set; }

 /// <summary>
    /// When the task was authored/created (ISO 8601)
    /// FHIR: Task.authoredOn
    /// </summary>
    public DateTime AuthoredOn { get; set; }

    /// <summary>
    /// When the task was last modified (ISO 8601)
    /// FHIR: Task.lastModified
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
  /// Requester organization ID (who is requesting the task)
    /// FHIR: Task.requester.reference
    /// </summary>
 public string RequesterId { get; set; } = string.Empty;

    /// <summary>
    /// Requester organization (navigation property)
  /// </summary>
    public Organization? Requester { get; set; }

    /// <summary>
    /// Owner/Responsible organization ID (who handles/processes the task)
    /// FHIR: Task.owner.identifier
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

 /// <summary>
    /// Owner/Responsible organization (navigation property)
    /// </summary>
    public Organization? Owner { get; set; }

    /// <summary>
    /// Poll input type (e.g., "include-message-type")
    /// For poll tasks: specifies the type of input filter
    /// System: http://nphies.sa/terminology/CodeSystem/task-input-type
    /// </summary>
    public string? PollInputType { get; set; }

    /// <summary>
    /// Poll input value (e.g., "claim-response")
    /// For poll tasks: the actual value/message types to retrieve
    /// Example values: "claim-response", "eligibility-response", "all"
    /// </summary>
    public string? PollInputValue { get; set; }

    /// <summary>
    /// Focus resource type (what this task is about)
    /// For cancel tasks: "Claim", "ClaimResponse"
    /// For poll tasks: null
    /// Example: "Claim"
    /// </summary>
    public string? FocusResourceType { get; set; }

    /// <summary>
    /// Focus identifier system (link to the resource being referenced)
    /// For cancel tasks: "http://saudicentralpharmacy.sa.com/claim"
    /// System URL for the identifier
    /// </summary>
    public string? FocusIdentifierSystem { get; set; }

    /// <summary>
    /// Focus identifier value (the actual ID of the resource)
    /// For cancel tasks: "req_00112482930" (claim ID)
    /// Example: claim ID, response ID, etc.
    /// </summary>
    public string? FocusIdentifierValue { get; set; }

    /// <summary>
    /// Reason code (why the task exists)
    /// For cancel tasks: "WI" (Wrong Item), "DU" (Duplicate), "RQ" (Request), etc.
    /// For poll tasks: null
    /// System: http://nphies.sa/terminology/CodeSystem/task-reason-code
    /// </summary>
  public string? ReasonCode { get; set; }

    /// <summary>
    /// Reason code system URL
    /// Example: "http://nphies.sa/terminology/CodeSystem/task-reason-code"
    /// </summary>
    public string? ReasonCodeSystem { get; set; }

    /// <summary>
    /// Output type for poll response (e.g., "response")
    /// For completed polls: indicates type of output provided
    /// System: http://nphies.sa/terminology/ValueSet/task-output-type
    /// </summary>
    public string? OutputType { get; set; }

    /// <summary>
    /// Output type system URL
    /// Example: "http://nphies.sa/terminology/ValueSet/task-output-type"
    /// </summary>
    public string? OutputTypeSystem { get; set; }

    /// <summary>
  /// Output bundle ID (FHIR Bundle resource ID)
    /// Reference to the bundle containing queued messages
    /// Example: "b0e34661-3b06-11ec-b009-39eda7432945"
  /// </summary>
    public string? OutputBundleId { get; set; }

    /// <summary>
    /// Output bundle reference URL
    /// Full URL reference to the response bundle
    /// Example: "http://nphies.sa/Bundle/b0e34661-3b06-11ec-b009-39eda7432945"
    /// </summary>
    public string? OutputBundleReference { get; set; }

    /// <summary>
    /// Response code from poll completion (HTTP-style)
    /// "ok", "transient-error", "fatal-error", etc.
    /// FHIR: ResponseTypeCode
    /// </summary>
    public string? ResponseCode { get; set; }

    /// <summary>
    /// Response identifier (acknowledgment ID)
    /// Unique identifier for the poll response
    /// Example: "cb681f6e-a1a8-42bc-8d34-fc7bd7c694e0"
    /// </summary>
    public string? ResponseIdentifier { get; set; }

    /// <summary>
    /// Meta tag from response (e.g., "queued-messages")
  /// Indicates type or category of data in response queue
    /// Example: "queued-messages", "queued-responses", etc.
    /// </summary>
    public string? MetaTag { get; set; }

    /// <summary>
    /// Task description/notes
    /// </summary>
    public string? Notes { get; set; }

 /// <summary>
    /// FHIR Task JSON for storage (backup/reference)
    /// Stores complete FHIR Task resource as JSON
    /// </summary>
    public string? FhirTaskJson { get; set; } = string.Empty;

    /// <summary>
    /// Related MessageHeader ID (for tracking poll requests)
    /// Links this task to the message that carried it
    /// </summary>
    public string? MessageHeaderId { get; set; }

 /// <summary>
    /// Related MessageHeader (navigation property)
  /// </summary>
 public MessageHeader? MessageHeader { get; set; }

    /// <summary>
    /// Get task type name (human-readable)
    /// </summary>
    public string GetTaskTypeName()
    {
        return Code switch
      {
        "poll" => "Poll Request",
        "notify" => "Notification",
     "execute" => "Execute",
   "acknowledge" => "Acknowledgement",
            _ => Code
};
    }

    /// <summary>
    /// Get status name (human-readable)
    /// </summary>
public string GetStatusName()
    {
 return Status switch
  {
"requested" => "Requested",
   "in-progress" => "In Progress",
     "completed" => "Completed",
   "failed" => "Failed",
     "cancelled" => "Cancelled",
       _ => Status
  };
  }

    /// <summary>
    /// Check if this is a poll task
  /// </summary>
    public bool IsPollTask()
    {
        return Code == "poll";
    }

 /// <summary>
    /// Check if task is still active (not completed/failed/cancelled)
    /// </summary>
public bool IsActive()
    {
      return Status != "completed" && Status != "failed" && Status != "cancelled" && Status != "entered-in-error";
    }

 /// <summary>
 /// Get poll message types (comma-separated)
    /// </summary>
    public string GetPollMessageTypes()
    {
   if (PollInputType == "include-message-type" && !string.IsNullOrEmpty(PollInputValue))
      {
    return PollInputValue;
    }
     return "all";
    }

    /// <summary>
    /// Check if task is waiting for a specific message type
  /// </summary>
    public bool IncludesMessageType(string messageType)
    {
   if (!IsPollTask())
     return false;

      if (PollInputValue == "all" || string.IsNullOrEmpty(PollInputValue))
     return true;

      var types = PollInputValue.Split(',');
    return types.Any(t => t.Trim().Equals(messageType, StringComparison.OrdinalIgnoreCase));
  }

    /// <summary>
    /// Check if poll response is available
    /// </summary>
  public bool HasResponse()
    {
      return Status == "completed" && !string.IsNullOrEmpty(OutputBundleId);
    }

  /// <summary>
  /// Check if response contains queued messages
    /// </summary>
    public bool HasQueuedMessages()
    {
        return MetaTag == "queued-messages" || !string.IsNullOrEmpty(OutputBundleReference);
    }

    /// <summary>
    /// Get response status (human-readable)
    /// </summary>
    public string GetResponseStatus()
    {
  return ResponseCode switch
        {
            "ok" => "Success",
    "transient-error" => "Temporary Error",
   "fatal-error" => "Fatal Error",
 _ => ResponseCode ?? "Unknown"
    };
    }

    /// <summary>
    /// Check if this is a cancellation task
    /// </summary>
    public bool IsCancellationTask()
    {
      return Code == "cancel";
    }

    /// <summary>
    /// Get cancellation reason display text
    /// </summary>
    public string GetReasonCodeDisplay()
    {
        return ReasonCode switch
     {
      "WI" => "Wrong Item",
   "DU" => "Duplicate",
   "RQ" => "Request",
        "DQ" => "Denied Quality",
    "IN" => "Information",
  "UC" => "Unconfirmed",
 _ => ReasonCode ?? "Unknown"
        };
    }

    /// <summary>
    /// Get full cancellation information
    /// </summary>
    public string GetCancellationInfo()
    {
 if (!IsCancellationTask())
     return string.Empty;

        return $"""
Cancellation Task: {TaskId}
Reason: {GetReasonCodeDisplay()} ({ReasonCode})
Resource Type: {FocusResourceType}
Resource ID: {FocusIdentifierValue}
Status: {GetStatusName()}
Priority: {Priority}
""";
    }
}
