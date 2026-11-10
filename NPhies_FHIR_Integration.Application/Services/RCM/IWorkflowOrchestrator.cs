using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM;

/// <summary>
/// Workflow Orchestrator Interface
/// Manages automated RCM workflows, scheduling, and orchestration
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Initialize automated workflows
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Initialization result</returns>
    Task<WorkflowInitializationResult> InitializeWorkflowsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule automatic appeal submission
    /// </summary>
    /// <param name="claimId">Claim ID to appeal</param>
    /// <param name="scheduledTime">When to submit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scheduled task ID</returns>
    Task<string> ScheduleAutomaticAppealAsync(
        string claimId,
        DateTime scheduledTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule automatic claim resubmission
    /// </summary>
    /// <param name="claimIds">Claim IDs to resubmit</param>
    /// <param name="resubmissionDelay">Days to wait before resubmission</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task ID</returns>
Task<string> ScheduleAutomaticResubmissionAsync(
        List<string> claimIds,
        int resubmissionDelay,
      CancellationToken cancellationToken = default);

 /// <summary>
/// Execute immediate workflow action
    /// </summary>
    /// <param name="workflowAction">Action to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result</returns>
    Task<WorkflowExecutionResult> ExecuteWorkflowActionAsync(
    WorkflowAction workflowAction,
 CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflow status
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Workflow status</returns>
    Task<WorkflowStatus> GetWorkflowStatusAsync(
        string taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel scheduled workflow
    /// </summary>
  /// <param name="taskId">Task ID to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether cancellation was successful</returns>
    Task<bool> CancelWorkflowAsync(
        string taskId,
      CancellationToken cancellationToken = default);
}

/// <summary>
/// Workflow Action
/// </summary>
public class WorkflowAction
{
    /// <summary>
    /// Action type
    /// </summary>
  public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Claim ID
    /// </summary>
    public string ClaimId { get; set; } = string.Empty;

    /// <summary>
    /// Action details/parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Priority (High, Medium, Low)
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Action description
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Workflow Initialization Result
/// </summary>
public class WorkflowInitializationResult
{
    /// <summary>
    /// Whether initialization was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Number of workflows initialized
    /// </summary>
    public int WorkflowsInitialized { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Initialization timestamp
    /// </summary>
    public DateTime InitializedAt { get; set; } = DateTime.UtcNow;

 /// <summary>
    /// Active workflows
    /// </summary>
 public List<string> ActiveWorkflows { get; set; } = new();
}

/// <summary>
/// Workflow Execution Result
/// </summary>
public class WorkflowExecutionResult
{
    /// <summary>
    /// Task ID
    /// </summary>
  public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Execution status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Execution message
    /// </summary>
  public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Result data
  /// </summary>
    public Dictionary<string, object> ResultData { get; set; } = new();

    /// <summary>
    /// Execution time (milliseconds)
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Execution timestamp
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Workflow Status
/// </summary>
public class WorkflowStatus
{
    /// <summary>
    /// Task ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Current status (Scheduled, Running, Completed, Failed, Cancelled)
    /// </summary>
    public string CurrentStatus { get; set; } = string.Empty;

  /// <summary>
    /// Progress percentage
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Started at
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Completed at
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Scheduled for
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
  /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error details if failed
    /// </summary>
    public string ErrorDetails { get; set; } = string.Empty;

    /// <summary>
    /// Retry count
  /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Last updated
 /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
