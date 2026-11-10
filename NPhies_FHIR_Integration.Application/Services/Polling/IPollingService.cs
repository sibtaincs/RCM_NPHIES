using Hl7.Fhir.Model;

namespace NPhies_FHIR_Integration.Application.Services.Polling;

/// <summary>
/// NPHIES async polling service interface for handling Task-based async responses
/// </summary>
public interface INphiesPollingService
{
    /// <summary>
    /// Poll for response using Task ID until complete or timeout
    /// </summary>
    /// <param name="taskId">NPHIES Task resource ID</param>
    /// <param name="timeout">Maximum time to poll</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response bundle when ready, null if timeout or cancelled</returns>
    Task<Bundle?> PollForResponseAsync(
        string taskId,
        TimeSpan timeout,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Check the status of a Task resource
    /// </summary>
    /// <param name="taskId">NPHIES Task resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task status</returns>
    Task<TaskStatus> GetTaskStatusAsync(
        string taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a pending task
    /// </summary>
    /// <param name="taskId">NPHIES Task resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancellation was successful</returns>
    Task<bool> CancelTaskAsync(
    string taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Poll for response with custom polling configuration
    /// </summary>
/// <param name="taskId">NPHIES Task resource ID</param>
    /// <param name="intervalSeconds">Polling interval in seconds</param>
    /// <param name="maxRetries">Maximum number of polling attempts</param>
  /// <param name="useExponentialBackoff">Use exponential backoff</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response bundle when ready, null if timeout or cancelled</returns>
    Task<Bundle?> PollForResponseAsync(
        string taskId,
        int intervalSeconds,
        int maxRetries,
        bool useExponentialBackoff,
  CancellationToken cancellationToken = default);
}

/// <summary>
/// FHIR Task resource status enumeration
/// </summary>
public enum TaskStatus
{
/// <summary>
    /// Task has just been created
    /// </summary>
    Draft,

    /// <summary>
  /// Task is ready to be acted upon
    /// </summary>
    Requested,

    /// <summary>
    /// Task has been received by fulfiller
  /// </summary>
    Received,

    /// <summary>
    /// Task has been accepted by fulfiller
    /// </summary>
    Accepted,

    /// <summary>
    /// Task has been rejected by fulfiller
    /// </summary>
  Rejected,

    /// <summary>
/// Task is ready for retrieval
  /// </summary>
    Ready,

    /// <summary>
    /// Task has been cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    /// Task is currently being performed
    /// </summary>
    InProgress,

    /// <summary>
    /// Task has been placed on hold
    /// </summary>
    OnHold,

    /// <summary>
    /// Task failed to complete
    /// </summary>
    Failed,

    /// <summary>
    /// Task has been completed
  /// </summary>
    Completed,

    /// <summary>
    /// Task was entered in error
    /// </summary>
    EnteredInError
}
