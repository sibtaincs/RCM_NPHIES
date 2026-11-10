namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// PollingRecord entity - Records all polling activity for audit and tracking
/// Stores comprehensive polling request/response history
/// </summary>
public class PollingRecord : BaseEntity
{
    /// <summary>
    /// Polling Record ID
    /// </summary>
    public string PollingRecordId { get; set; } = string.Empty;

  /// <summary>
    /// Provider Organization ID (who initiated the poll)
    /// FK ? Organization
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider Organization (navigation property)
    /// </summary>
    public Organization? Provider { get; set; }

    /// <summary>
    /// Polling Request Task ID
    /// Reference to the Task resource sent to NPHIES
  /// </summary>
    public string? RequestTaskId { get; set; }

    /// <summary>
    /// Cancellation Request Database ID (if stored)
    /// FK ? CancellationRequest
    /// </summary>
    public string? CancellationRequestId { get; set; }

    /// <summary>
    /// Cancellation Request (navigation property)
    /// </summary>
    public CancellationRequest? CancellationRequest { get; set; }

  /// <summary>
    /// Message types requested in poll
    /// Example: "claim-response,eligibility-response,communication"
    /// </summary>
    public string? RequestedMessageTypes { get; set; }

    /// <summary>
    /// When the poll request was sent to NPHIES
  /// </summary>
    public DateTime? RequestSentAt { get; set; }

    /// <summary>
    /// Polling Response Task ID (from NPHIES)
    /// Reference to the Task resource received from NPHIES
    /// </summary>
    public string? ResponseTaskId { get; set; }

    /// <summary>
 /// Cancellation Response Database ID (if stored)
    /// FK ? CancellationResponse
    /// </summary>
    public string? CancellationResponseId { get; set; }

    /// <summary>
    /// Cancellation Response (navigation property)
    /// </summary>
  public CancellationResponse? CancellationResponse { get; set; }

    /// <summary>
  /// Response status from NPHIES
    /// Values: "ok", "error", "partial-success", "timeout"
    /// </summary>
    public string? ResponseStatus { get; set; }

    /// <summary>
    /// When the poll response was received from NPHIES
    /// </summary>
    public DateTime? ResponseReceivedAt { get; set; }

    /// <summary>
    /// Number of messages in the response
    /// Total count of extracted resources
    /// </summary>
    public int MessagesReceived { get; set; }

    /// <summary>
    /// Message types received in response
    /// Example: "ClaimResponse,CoverageEligibilityResponse,Communication"
    /// </summary>
    public string? ReceivedMessageTypes { get; set; }

 /// <summary>
    /// Full request bundle JSON (for audit trail)
    /// Stores complete FHIR Bundle sent to NPHIES
    /// </summary>
    public string? RequestBundleJson { get; set; }

    /// <summary>
    /// Full response bundle JSON (for audit trail)
    /// Stores complete FHIR Bundle received from NPHIES
    /// </summary>
    public string? ResponseBundleJson { get; set; }

    /// <summary>
/// Processing status of this polling record
/// Values: "pending", "processing", "completed", "failed", "acknowledged"
/// </summary>
    public string ProcessingStatus { get; set; } = "pending";

/// <summary>
    /// HTTP status code from response (if applicable)
    /// Example: 200, 400, 500
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Error message (if polling failed)
    /// Stores error details for troubleshooting
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code (if polling failed)
    /// Machine-readable error identifier
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Total duration of the polling cycle (in milliseconds)
    /// Time from request sent to response received
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Polling cycle status
    /// Values: "in-progress", "completed", "timed-out", "failed"
    /// </summary>
    public string CycleStatus { get; set; } = "in-progress";

/// <summary>
    /// Whether the response has been acknowledged by provider
    /// </summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>
    /// When the response was acknowledged
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Retry count (how many times this polling was retried)
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
  /// Maximum retries allowed for this polling
    /// </summary>
    public int? MaxRetries { get; set; }

    /// <summary>
    /// Next retry scheduled for (if applicable)
    /// </summary>
  public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// IP address of the requesting provider
    /// For security auditing
    /// </summary>
    public string? SourceIpAddress { get; set; }

 /// <summary>
    /// Request source identifier (provider system ID)
    /// Identifies which provider system made the request
    /// </summary>
    public string? RequestSourceId { get; set; }

    /// <summary>
    /// Notes or additional comments about this polling
    /// Free-form field for additional context
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
 /// Polling Record Summary
    /// </summary>
    public string GetSummary()
    {
        var status = CycleStatus ?? "unknown";
     var messages = MessagesReceived > 0 ? $", {MessagesReceived} messages" : "";
   return $"Polling Record: {status}{messages} - Provider: {Provider?.OrganizationName ?? "Unknown"}";
    }

    /// <summary>
    /// Get processing status display
    /// </summary>
    public string GetProcessingStatusDisplay()
    {
        return ProcessingStatus switch
        {
            "pending" => "Pending",
            "processing" => "Processing",
            "completed" => "Completed",
        "failed" => "Failed",
    "acknowledged" => "Acknowledged",
          _ => ProcessingStatus ?? "Unknown"
     };
 }

    /// <summary>
    /// Get cycle status display
    /// </summary>
    public string GetCycleStatusDisplay()
    {
      return CycleStatus switch
     {
 "in-progress" => "In Progress",
    "completed" => "Completed",
      "timed-out" => "Timed Out",
         "failed" => "Failed",
    _ => CycleStatus ?? "Unknown"
        };
    }

    /// <summary>
    /// Check if polling is still in progress
    /// </summary>
    public bool IsInProgress()
  {
        return CycleStatus == "in-progress";
    }

    /// <summary>
    /// Check if polling succeeded
    /// </summary>
    public bool IsSuccessful()
    {
        return CycleStatus == "completed" && ResponseStatus == "ok";
    }

    /// <summary>
    /// Check if polling failed
    /// </summary>
    public bool IsFailed()
    {
   return CycleStatus == "failed" || !string.IsNullOrEmpty(ErrorCode);
    }

    /// <summary>
    /// Get formatted duration
    /// </summary>
    public string GetFormattedDuration()
    {
 if (DurationMs.HasValue)
        {
            if (DurationMs.Value < 1000)
   return $"{DurationMs}ms";
     return $"{Math.Round(DurationMs.Value / 1000.0, 2)}s";
        }
  return "N/A";
    }

    /// <summary>
    /// Get full polling details
    /// </summary>
    public string GetFullDetails()
    {
        return $"""
PollingRecord: {PollingRecordId}
Provider: {Provider?.OrganizationName ?? "Unknown"}
Request Status: {(RequestSentAt.HasValue ? "Sent" : "Pending")}
Response Status: {ResponseStatus ?? "No response"}
Processing: {GetProcessingStatusDisplay()}
Cycle: {GetCycleStatusDisplay()}
Messages Received: {MessagesReceived}
Duration: {GetFormattedDuration()}
Acknowledged: {(IsAcknowledged ? "Yes" : "No")}
Retries: {RetryCount}/{MaxRetries ?? 0}
Error: {ErrorMessage ?? "None"}
Created: {CreatedAt:yyyy-MM-dd HH:mm:ss}
Updated: {UpdatedAt:yyyy-MM-dd HH:mm:ss}
""";
    }
}
