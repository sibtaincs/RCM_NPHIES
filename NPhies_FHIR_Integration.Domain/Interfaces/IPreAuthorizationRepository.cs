using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Domain.Interfaces;

/// <summary>
/// Pre-Authorization repository interface
/// Extends generic repository with pre-authorization specific operations
/// </summary>
public interface IPreAuthorizationRepository : IRepository<PreAuthorizationRequest>
{
    /// <summary>
    /// Get pre-authorization request with all related data (items, diagnoses, supporting info, response)
    /// </summary>
    Task<PreAuthorizationRequest?> GetByIdWithDetailsAsync(string id);

  /// <summary>
    /// Get pre-authorization requests by patient ID
    /// </summary>
    Task<IEnumerable<PreAuthorizationRequest>> GetByPatientIdAsync(string patientId);

    /// <summary>
    /// Get pre-authorization requests by provider ID
    /// </summary>
    Task<IEnumerable<PreAuthorizationRequest>> GetByProviderIdAsync(string providerId);

/// <summary>
 /// Get pre-authorization requests by insurer ID
    /// </summary>
    Task<IEnumerable<PreAuthorizationRequest>> GetByInsurerIdAsync(string insurerId);

/// <summary>
    /// Get pre-authorization request by request identifier
    /// </summary>
    Task<PreAuthorizationRequest?> GetByRequestIdentifierAsync(string requestIdentifier);

    /// <summary>
    /// Get pre-authorization requests by status
    /// </summary>
    Task<IEnumerable<PreAuthorizationRequest>> GetByStatusAsync(string status);

  /// <summary>
    /// Get pre-authorization requests by type
    /// </summary>
    Task<IEnumerable<PreAuthorizationRequest>> GetByTypeAsync(string type);

    /// <summary>
    /// Get active pre-authorization requests for a patient
    /// </summary>
    Task<IEnumerable<PreAuthorizationRequest>> GetActiveByPatientIdAsync(string patientId);

  /// <summary>
 /// Get pending pre-authorization requests (no response received yet)
/// </summary>
    Task<IEnumerable<PreAuthorizationRequest>> GetPendingRequestsAsync();

    /// <summary>
    /// Get pre-authorization requests submitted in date range
    /// </summary>
    Task<IEnumerable<PreAuthorizationRequest>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Check if a pre-authorization request identifier already exists
    /// </summary>
    Task<bool> RequestIdentifierExistsAsync(string requestIdentifier);

    /// <summary>
    /// Get pre-authorization statistics for a provider
    /// </summary>
    Task<PreAuthStatistics> GetStatisticsAsync(string providerId, DateTime? fromDate = null, DateTime? toDate = null);
}

/// <summary>
/// Pre-authorization statistics DTO
/// </summary>
public class PreAuthStatistics
{
    public int TotalRequests { get; set; }
public int ActiveRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int CancelledRequests { get; set; }
    public int PendingRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int DeniedRequests { get; set; }
    public decimal AverageProcessingTimeHours { get; set; }
}
