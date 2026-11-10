using System.Collections.Generic;
using System.Threading.Tasks;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Application.Services.Masters;

/// <summary>
/// Error Code Service Interface
/// Handles all NPHIES error code operations
/// </summary>
public interface IErrorCodeService
{
    /// <summary>
    /// Get error code by error code identifier (e.g., "AD-1-1")
    /// </summary>
    /// <param name="errorCode">Error code (e.g., "AD-1-1")</param>
/// <returns>ErrorCodeMaster entity or null if not found</returns>
    Task<ErrorCodeMaster?> GetErrorCodeAsync(string errorCode);

    /// <summary>
    /// Get all error codes in a specific category (e.g., "adjudication")
 /// </summary>
    /// <param name="category">Error category to filter by</param>
    /// <returns>List of error codes in that category</returns>
    Task<List<ErrorCodeMaster>> GetErrorCodesByCategoryAsync(string category);

    /// <summary>
    /// Search error codes by description or error code
    /// </summary>
    /// <param name="searchTerm">Term to search (matches in ErrorCode or ErrorDescription)</param>
    /// <returns>List of matching error codes</returns>
    Task<List<ErrorCodeMaster>> SearchErrorCodesAsync(string searchTerm);

    /// <summary>
    /// Get all active error codes
    /// </summary>
    /// <returns>List of all active error codes</returns>
    Task<List<ErrorCodeMaster>> GetAllErrorCodesAsync();

    /// <summary>
    /// Check if a specific error code allows appeals
    /// </summary>
    /// <param name="errorCode">Error code to check</param>
    /// <returns>True if appeals are allowed, false otherwise</returns>
    Task<bool> AllowsAppealAsync(string errorCode);

    /// <summary>
    /// Get appeal deadline days for a specific error code
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <returns>Number of days to appeal (default 60)</returns>
    Task<int> GetAppealDeadlineDaysAsync(string errorCode);

    /// <summary>
    /// Check if error code is recoverable (can be fixed by resubmission)
    /// </summary>
    /// <param name="errorCode">Error code to check</param>
    /// <returns>True if recoverable, false otherwise</returns>
    Task<bool> IsRecoverableAsync(string errorCode);

    /// <summary>
    /// Get the recommended action for an error code
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <returns>Recommended action text</returns>
    Task<string?> GetRecommendedActionAsync(string errorCode);

    /// <summary>
  /// Get error codes by severity level
    /// </summary>
    /// <param name="severity">Severity level (Error, Warning, Info)</param>
    /// <returns>List of error codes with that severity</returns>
    Task<List<ErrorCodeMaster>> GetErrorCodesBySeverityAsync(string severity);

    /// <summary>
    /// Bulk create or update error codes
    /// </summary>
    /// <param name="errorCodes">List of error codes to create/update</param>
    /// <returns>Number of codes created/updated</returns>
    Task<int> BulkImportErrorCodesAsync(List<ErrorCodeMaster> errorCodes);
}
