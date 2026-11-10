using Hl7.Fhir.Model;

namespace NPhies_FHIR_Integration.Application.Services.Http;

/// <summary>
/// HTTP client interface for NPHIES API communication
/// </summary>
public interface INphiesHttpClient
{
    /// <summary>
    /// Submit a FHIR bundle to NPHIES and get response
    /// </summary>
    /// <param name="requestBundle">FHIR bundle to submit</param>
    /// <param name="endpoint">API endpoint (eligibility, claim, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response bundle from NPHIES</returns>
    Task<Bundle> SubmitBundleAsync(
        Bundle requestBundle,
        string endpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a bundle by URL (used for polling)
    /// </summary>
    /// <param name="url">Full URL or Task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bundle from NPHIES</returns>
Task<Bundle> GetBundleAsync(
        string url,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a Task resource by ID (for polling)
    /// </summary>
    /// <param name="taskId">Task resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task resource</returns>
 Task<Hl7.Fhir.Model.Task> GetTaskAsync(
        string taskId,
     CancellationToken cancellationToken = default);

  /// <summary>
    /// Health check - verify NPHIES API is accessible
  /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if API is healthy</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get capability statement from NPHIES
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CapabilityStatement resource</returns>
    Task<CapabilityStatement> GetCapabilityStatementAsync(
        CancellationToken cancellationToken = default);
}
