using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Application.Exceptions;
using NPhies_FHIR_Integration.Application.Services.Http;
using System.Diagnostics;

namespace NPhies_FHIR_Integration.Application.Services.Polling;

/// <summary>
/// NPHIES polling service implementation
/// Handles async response polling via Task resources
/// </summary>
public class NphiesPollingService : INphiesPollingService
{
    private readonly INphiesHttpClient _httpClient;
    private readonly ILogger<NphiesPollingService> _logger;
    private readonly NphiesConfiguration _config;

    public NphiesPollingService(
    INphiesHttpClient httpClient,
 ILogger<NphiesPollingService> logger,
        IOptions<NphiesConfiguration> config)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
_config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Poll for response until complete or timeout
    /// </summary>
    public async Task<Bundle?> PollForResponseAsync(
        string taskId,
        TimeSpan timeout,
      CancellationToken cancellationToken = default)
    {
  return await PollForResponseAsync(
        taskId,
   _config.Polling.IntervalSeconds,
     _config.Polling.MaxRetries,
        _config.Polling.ExponentialBackoff,
         cancellationToken);
    }

    /// <summary>
    /// Poll for response with custom configuration
    /// </summary>
    public async Task<Bundle?> PollForResponseAsync(
        string taskId,
        int intervalSeconds,
        int maxRetries,
        bool useExponentialBackoff,
    CancellationToken cancellationToken = default)
    {
        try
        {
       _logger.LogInformation("Starting polling for Task {TaskId} (interval: {Interval}s, max retries: {MaxRetries})",
           taskId, intervalSeconds, maxRetries);

            var stopwatch = Stopwatch.StartNew();
            var attempt = 0;
          var currentDelay = intervalSeconds;

   while (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
            {
attempt++;

                try
                {
  _logger.LogDebug("Polling attempt {Attempt}/{MaxRetries} for Task {TaskId}",
           attempt, maxRetries, taskId);

      // Get Task resource
var task = await _httpClient.GetTaskAsync(taskId, cancellationToken);

      // Check Task status
       var status = MapTaskStatus(task.Status);
     _logger.LogDebug("Task {TaskId} status: {Status}", taskId, status);

         // Handle different statuses
    switch (status)
            {
                  case TaskStatus.Completed:
            case TaskStatus.Ready:
              _logger.LogInformation("Task {TaskId} is ready. Retrieving response bundle.", taskId);
 return await GetResponseBundleFromTask(task, cancellationToken);

           case TaskStatus.Failed:
     _logger.LogError("Task {TaskId} failed", taskId);
    throw new NphiesException($"Task {taskId} failed", "TASK_FAILED");

 case TaskStatus.Rejected:
        _logger.LogError("Task {TaskId} was rejected", taskId);
     throw new NphiesException($"Task {taskId} was rejected", "TASK_REJECTED");

             case TaskStatus.Cancelled:
         _logger.LogWarning("Task {TaskId} was cancelled", taskId);
         return null;

           case TaskStatus.EnteredInError:
    _logger.LogError("Task {TaskId} was entered in error", taskId);
      throw new NphiesException($"Task {taskId} was entered in error", "TASK_ERROR");

        case TaskStatus.InProgress:
case TaskStatus.Accepted:
        case TaskStatus.Received:
      case TaskStatus.Requested:
       // Still processing, continue polling
   _logger.LogDebug("Task {TaskId} still processing (status: {Status}). Waiting {Delay}s before next poll.",
   taskId, status, currentDelay);
          break;

           default:
        _logger.LogWarning("Task {TaskId} has unexpected status: {Status}", taskId, status);
        break;
       }

           // If we haven't returned yet, wait before next poll
    if (attempt < maxRetries)
     {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(currentDelay), cancellationToken);

             // Apply exponential backoff if enabled
         if (useExponentialBackoff)
        {
      currentDelay = CalculateExponentialBackoff(
attempt,
 _config.Polling.InitialBackoffSeconds,
     _config.Polling.MaxBackoffSeconds);

        _logger.LogDebug("Next poll delay: {Delay}s (exponential backoff)", currentDelay);
    }
  }
        }
                catch (OperationCanceledException)
    {
           _logger.LogWarning("Polling for Task {TaskId} was cancelled after {Attempts} attempts", taskId, attempt);
  throw;
          }
       catch (NphiesTimeoutException ex)
    {
             _logger.LogWarning(ex, "Timeout during polling attempt {Attempt} for Task {TaskId}. Will retry.", attempt, taskId);

 // Continue polling if we have retries left
   if (attempt >= maxRetries)
   throw;
  }
     catch (NphiesException)
           {
        // Re-throw our custom exceptions
     throw;
                }
       catch (Exception ex)
  {
   _logger.LogError(ex, "Error during polling attempt {Attempt} for Task {TaskId}", attempt, taskId);

       // Continue polling if we have retries left
        if (attempt >= maxRetries)
                throw new NphiesException($"Polling failed for Task {taskId}: {ex.Message}", ex);
     }
       }

  // Max retries exceeded or cancelled
            stopwatch.Stop();
   _logger.LogWarning("Polling timeout for Task {TaskId} after {Attempts} attempts ({Elapsed}s)",
 taskId, attempt, stopwatch.Elapsed.TotalSeconds);

  throw new NphiesTimeoutException(
   $"Polling timeout for Task {taskId} after {attempt} attempts",
      (int)stopwatch.Elapsed.TotalSeconds);
        }
 catch (Exception ex) when (ex is not NphiesException && ex is not OperationCanceledException)
   {
            _logger.LogError(ex, "Unexpected error while polling for Task {TaskId}", taskId);
         throw new NphiesException($"Polling error for Task {taskId}: {ex.Message}", ex);
  }
 }

    /// <summary>
 /// Get Task status
    /// </summary>
    public async Task<TaskStatus> GetTaskStatusAsync(
      string taskId,
    CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting status for Task {TaskId}", taskId);

  var task = await _httpClient.GetTaskAsync(taskId, cancellationToken);
        var status = MapTaskStatus(task.Status);

            _logger.LogDebug("Task {TaskId} status: {Status}", taskId, status);

        return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for Task {TaskId}", taskId);
            throw new NphiesException($"Failed to get Task status: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Cancel a pending task
    /// </summary>
    public async Task<bool> CancelTaskAsync(
        string taskId,
      CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to cancel Task {TaskId}", taskId);

        // Get current task
      var task = await _httpClient.GetTaskAsync(taskId, cancellationToken);

        // Check if task can be cancelled
        var status = MapTaskStatus(task.Status);
            if (status == TaskStatus.Completed || status == TaskStatus.Failed || status == TaskStatus.Cancelled)
 {
     _logger.LogWarning("Cannot cancel Task {TaskId} in status {Status}", taskId, status);
      return false;
    }

  // Update task status to cancelled
            task.Status = Hl7.Fhir.Model.Task.TaskStatus.Cancelled;

            // Note: In a real implementation, you would PUT the updated task back to NPHIES
            // For now, we'll log that cancellation was attempted
            _logger.LogInformation("Task {TaskId} cancellation requested", taskId);

    // TODO: Implement task update via HTTP PUT
            // await _httpClient.UpdateTaskAsync(task, cancellationToken);

     return true;
        }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Error cancelling Task {TaskId}", taskId);
      throw new NphiesException($"Failed to cancel Task: {ex.Message}", ex);
      }
    }

  /// <summary>
    /// Map FHIR Task status to our enum
    /// </summary>
    private TaskStatus MapTaskStatus(Hl7.Fhir.Model.Task.TaskStatus? fhirStatus)
    {
     if (!fhirStatus.HasValue)
            return TaskStatus.Draft;

     return fhirStatus.Value switch
        {
        Hl7.Fhir.Model.Task.TaskStatus.Draft => TaskStatus.Draft,
            Hl7.Fhir.Model.Task.TaskStatus.Requested => TaskStatus.Requested,
       Hl7.Fhir.Model.Task.TaskStatus.Received => TaskStatus.Received,
            Hl7.Fhir.Model.Task.TaskStatus.Accepted => TaskStatus.Accepted,
            Hl7.Fhir.Model.Task.TaskStatus.Rejected => TaskStatus.Rejected,
         Hl7.Fhir.Model.Task.TaskStatus.Ready => TaskStatus.Ready,
      Hl7.Fhir.Model.Task.TaskStatus.Cancelled => TaskStatus.Cancelled,
     Hl7.Fhir.Model.Task.TaskStatus.InProgress => TaskStatus.InProgress,
       Hl7.Fhir.Model.Task.TaskStatus.OnHold => TaskStatus.OnHold,
       Hl7.Fhir.Model.Task.TaskStatus.Failed => TaskStatus.Failed,
          Hl7.Fhir.Model.Task.TaskStatus.Completed => TaskStatus.Completed,
            Hl7.Fhir.Model.Task.TaskStatus.EnteredInError => TaskStatus.EnteredInError,
            _ => TaskStatus.Draft
  };
    }

 /// <summary>
  /// Get response bundle from completed Task
    /// </summary>
    private async Task<Bundle?> GetResponseBundleFromTask(
        Hl7.Fhir.Model.Task task,
    CancellationToken cancellationToken)
{
    try
        {
       // Check if task has output with response reference
            if (task.Output == null || !task.Output.Any())
  {
 _logger.LogWarning("Task {TaskId} is complete but has no output", task.Id);
    return null;
   }

 // Find the response bundle reference in task output
 var responseOutput = task.Output.FirstOrDefault(o =>
     o.Type?.Coding?.Any(c => c.Code == "bundle" || c.Code == "response") == true);

       if (responseOutput?.Value == null)
       {
    _logger.LogWarning("Task {TaskId} output does not contain response bundle reference", task.Id);
      return null;
   }

 // Extract bundle reference URL
            string? bundleUrl = null;

         if (responseOutput.Value is ResourceReference reference)
            {
         bundleUrl = reference.Reference;
      }
            else if (responseOutput.Value is FhirUrl url)
            {
     bundleUrl = url.Value;
            }
            else if (responseOutput.Value is FhirString str)
   {
bundleUrl = str.Value;
            }

            if (string.IsNullOrEmpty(bundleUrl))
  {
     _logger.LogWarning("Task {TaskId} output contains empty bundle reference", task.Id);
 return null;
 }

         _logger.LogInformation("Retrieving response bundle from: {BundleUrl}", bundleUrl);

   // Get the response bundle
            var responseBundle = await _httpClient.GetBundleAsync(bundleUrl, cancellationToken);

            _logger.LogInformation("Successfully retrieved response bundle with {Count} entries",
             responseBundle.Entry?.Count ?? 0);

            return responseBundle;
        }
    catch (Exception ex)
        {
     _logger.LogError(ex, "Error retrieving response bundle from Task {TaskId}", task.Id);
            throw new NphiesException($"Failed to retrieve response bundle: {ex.Message}", ex);
      }
    }

    /// <summary>
    /// Calculate exponential backoff delay
    /// </summary>
    private int CalculateExponentialBackoff(int attempt, int initialDelay, int maxDelay)
    {
        // Calculate exponential backoff: initialDelay * 2^(attempt-1)
        var delay = initialDelay * Math.Pow(2, attempt - 1);

  // Cap at maximum delay
   return Math.Min((int)delay, maxDelay);
    }
}
