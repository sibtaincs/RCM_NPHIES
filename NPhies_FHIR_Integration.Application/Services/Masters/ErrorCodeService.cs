using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.Masters;

/// <summary>
/// Error Code Service Implementation
/// Provides comprehensive access to NPHIES error codes
/// </summary>
public class ErrorCodeService : IErrorCodeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ErrorCodeService> _logger;

    public ErrorCodeService(
        ApplicationDbContext context,
        ILogger<ErrorCodeService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get error code by error code identifier (e.g., "AD-1-1")
    /// </summary>
    public async Task<ErrorCodeMaster?> GetErrorCodeAsync(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            _logger.LogWarning("GetErrorCodeAsync called with null or empty error code");
            return null;
        }

        try
        {
            var result = await _context.ErrorCodeMasters
       .AsNoTracking()
        .FirstOrDefaultAsync(e => e.ErrorCode == errorCode && e.IsActive);

            if (result == null)
            {
                _logger.LogWarning("Error code '{ErrorCode}' not found or inactive", errorCode);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving error code '{ErrorCode}'", errorCode);
            throw;
        }
    }

    /// <summary>
    /// Get all error codes in a specific category (e.g., "adjudication")
    /// </summary>
    public async Task<List<ErrorCodeMaster>> GetErrorCodesByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            _logger.LogWarning("GetErrorCodesByCategoryAsync called with null or empty category");
            return new List<ErrorCodeMaster>();
        }

        try
        {
            var result = await _context.ErrorCodeMasters
      .AsNoTracking()
      .Where(e => e.ErrorCategory == category && e.IsActive)
       .OrderBy(e => e.ErrorCode)
      .ToListAsync();

            _logger.LogInformation("Retrieved {Count} error codes for category '{Category}'",
     result.Count, category);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving error codes for category '{Category}'", category);
            throw;
        }
    }

    /// <summary>
    /// Search error codes by description or error code
    /// </summary>
    public async Task<List<ErrorCodeMaster>> SearchErrorCodesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            _logger.LogWarning("SearchErrorCodesAsync called with null or empty search term");
            return new List<ErrorCodeMaster>();
        }

        try
        {
            var searchTermLower = searchTerm.ToLower();

            var result = await _context.ErrorCodeMasters
                .AsNoTracking()
           .Where(e => e.IsActive && (
             e.ErrorCode.ToLower().Contains(searchTermLower) ||
         e.ErrorDescription.ToLower().Contains(searchTermLower)
            ))
        .OrderBy(e => e.ErrorCode)
                     .ToListAsync();

            _logger.LogInformation("Search for '{SearchTerm}' returned {Count} results",
             searchTerm, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching error codes with term '{SearchTerm}'", searchTerm);
            throw;
        }
    }

    /// <summary>
    /// Get all active error codes
    /// </summary>
    public async Task<List<ErrorCodeMaster>> GetAllErrorCodesAsync()
    {
        try
        {
            var result = await _context.ErrorCodeMasters
                    .AsNoTracking()
            .Where(e => e.IsActive)
                    .OrderBy(e => e.ErrorCode)
          .ToListAsync();

            _logger.LogInformation("Retrieved {Count} active error codes", result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all error codes");
            throw;
        }
    }

    /// <summary>
    /// Check if a specific error code allows appeals
    /// </summary>
    public async Task<bool> AllowsAppealAsync(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            _logger.LogWarning("AllowsAppealAsync called with null or empty error code");
            return false;
        }

        try
        {
            var code = await GetErrorCodeAsync(errorCode);
            return code?.AllowsAppeal ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking appeal status for error code '{ErrorCode}'", errorCode);
            throw;
        }
    }

    /// <summary>
    /// Get appeal deadline days for a specific error code
    /// </summary>
    public async Task<int> GetAppealDeadlineDaysAsync(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            _logger.LogWarning("GetAppealDeadlineDaysAsync called with null or empty error code, returning default 60 days");
            return 60; // Default to 60 days
        }

        try
        {
            var code = await GetErrorCodeAsync(errorCode);
            return code?.StandardAppealDays ?? 60;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appeal deadline for error code '{ErrorCode}'", errorCode);
            throw;
        }
    }

    /// <summary>
    /// Check if error code is recoverable (can be fixed by resubmission)
    /// </summary>
    public async Task<bool> IsRecoverableAsync(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            _logger.LogWarning("IsRecoverableAsync called with null or empty error code");
            return false;
        }

        try
        {
            var code = await GetErrorCodeAsync(errorCode);
            return code?.IsRecoverable ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recovery status for error code '{ErrorCode}'", errorCode);
            throw;
        }
    }

    /// <summary>
    /// Get the recommended action for an error code
    /// </summary>
    public async Task<string?> GetRecommendedActionAsync(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            _logger.LogWarning("GetRecommendedActionAsync called with null or empty error code");
            return null;
        }

        try
        {
            var code = await GetErrorCodeAsync(errorCode);
            return code?.RecommendedAction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommended action for error code '{ErrorCode}'", errorCode);
            throw;
        }
    }

    /// <summary>
    /// Get error codes by severity level
    /// </summary>
    public async Task<List<ErrorCodeMaster>> GetErrorCodesBySeverityAsync(string severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
        {
            _logger.LogWarning("GetErrorCodesBySeverityAsync called with null or empty severity");
            return new List<ErrorCodeMaster>();
        }

        try
        {
            var result = await _context.ErrorCodeMasters
    .AsNoTracking()
       .Where(e => e.Severity == severity && e.IsActive)
      .OrderBy(e => e.ErrorCode)
        .ToListAsync();

            _logger.LogInformation("Retrieved {Count} error codes with severity '{Severity}'",
    result.Count, severity);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving error codes with severity '{Severity}'", severity);
            throw;
        }
    }

    /// <summary>
    /// Bulk create or update error codes
    /// </summary>
    public async Task<int> BulkImportErrorCodesAsync(List<ErrorCodeMaster> errorCodes)
    {
        if (errorCodes == null || errorCodes.Count == 0)
        {
            _logger.LogWarning("BulkImportErrorCodesAsync called with null or empty list");
            return 0;
        }

        try
        {
            int created = 0;
            int updated = 0;

            foreach (var errorCode in errorCodes)
            {
                var existing = await _context.ErrorCodeMasters
              .FirstOrDefaultAsync(e => e.ErrorCode == errorCode.ErrorCode);

                if (existing == null)
                {
                    _context.ErrorCodeMasters.Add(errorCode);
                    created++;
                }
                else
                {
                    // Update existing
                    existing.ErrorDescription = errorCode.ErrorDescription;
                    existing.ErrorCategory = errorCode.ErrorCategory;
                    existing.Severity = errorCode.Severity;
                    existing.IsRecoverable = errorCode.IsRecoverable;
                    existing.AllowsAppeal = errorCode.AllowsAppeal;
                    existing.StandardAppealDays = errorCode.StandardAppealDays;
                    existing.RecommendedAction = errorCode.RecommendedAction;
                    existing.AdjudicationImpact = errorCode.AdjudicationImpact;
                    existing.Notes = errorCode.Notes;
                    existing.IsActive = errorCode.IsActive;
                    existing.LastModifiedDate = DateTime.UtcNow;

                    _context.ErrorCodeMasters.Update(existing);
                    updated++;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk import completed: {Created} created, {Updated} updated",
      created, updated);

            return created + updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk import of error codes");
            throw;
        }
    }
}
