using NPhies_FHIR_Integration.Application.DTOs;

namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// Pre-Authorization Service Interface
/// Handles business logic for pre-authorization requests
/// </summary>
public interface IPreAuthorizationService
{
    /// <summary>
    /// Create a new pre-authorization request
    /// </summary>
    Task<PreAuthorizationRequestDto> CreatePreAuthorizationAsync(CreatePreAuthorizationRequestDto createDto);

    /// <summary>
   /// Get pre-authorization request by ID
    /// </summary>
    Task<PreAuthorizationRequestDto?> GetPreAuthorizationByIdAsync(string id);

    /// <summary>
    /// Get pre-authorization request by request identifier
    /// </summary>
    Task<PreAuthorizationRequestDto?> GetPreAuthorizationByRequestIdentifierAsync(string requestIdentifier);

    /// <summary>
    /// Get pre-authorization requests by patient ID
    /// </summary>
    Task<IEnumerable<PreAuthorizationSummaryDto>> GetPreAuthorizationsByPatientIdAsync(string patientId);

    /// <summary>
    /// Get pre-authorization requests by provider ID
    /// </summary>
    Task<IEnumerable<PreAuthorizationSummaryDto>> GetPreAuthorizationsByProviderIdAsync(string providerId);

    /// <summary>
    /// Get active pre-authorizations for a patient
    /// </summary>
  Task<IEnumerable<PreAuthorizationSummaryDto>> GetActivePreAuthorizationsByPatientIdAsync(string patientId);

    /// <summary>
    /// Get pending pre-authorization requests (no response yet)
 /// </summary>
    Task<IEnumerable<PreAuthorizationSummaryDto>> GetPendingPreAuthorizationsAsync();

  /// <summary>
  /// Update pre-authorization request
    /// </summary>
    Task<PreAuthorizationRequestDto> UpdatePreAuthorizationAsync(string id, UpdatePreAuthorizationRequestDto updateDto);

    /// <summary>
    /// Cancel pre-authorization request
    /// </summary>
  Task<bool> CancelPreAuthorizationAsync(string id, string cancellationReason);

    /// <summary>
    /// Check if request identifier exists
    /// </summary>
    Task<bool> RequestIdentifierExistsAsync(string requestIdentifier);

    /// <summary>
    /// Get pre-authorization statistics for a provider
    /// </summary>
Task<PreAuthStatisticsDto> GetStatisticsAsync(string providerId, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Submit pre-authorization to NPHIES (placeholder for FHIR integration)
    /// </summary>
    Task<PreAuthorizationRequestDto> SubmitToNphiesAsync(string id);

    /// <summary>
/// Process pre-authorization response from NPHIES
    /// </summary>
    Task<PreAuthorizationResponseDto> ProcessNphiesResponseAsync(string requestId, string responseFhirJson);
}
