using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;

namespace NPhies_FHIR_Integration.Infrastructure.Repositories;

/// <summary>
/// Repository interface for CoverageEligibilityRequest operations
/// </summary>
public interface ICoverageEligibilityRequestRepository : IRepository<CoverageEligibilityRequest>
{
    /// <summary>
    /// Get request with all related data (items, patient, coverage, etc.)
    /// </summary>
    Task<CoverageEligibilityRequest?> GetWithDetailsAsync(string requestId);

    /// <summary>
    /// Get requests by patient ID
    /// </summary>
    Task<IEnumerable<CoverageEligibilityRequest>> GetByPatientIdAsync(string patientId);

    /// <summary>
    /// Get requests by coverage ID
    /// </summary>
    Task<IEnumerable<CoverageEligibilityRequest>> GetByCoverageIdAsync(string coverageId);

  /// <summary>
    /// Get requests by provider ID
    /// </summary>
    Task<IEnumerable<CoverageEligibilityRequest>> GetByProviderIdAsync(string providerId);

 /// <summary>
    /// Get requests by status
    /// </summary>
    Task<IEnumerable<CoverageEligibilityRequest>> GetByStatusAsync(string status);

    /// <summary>
    /// Get requests pending response
    /// </summary>
    Task<IEnumerable<CoverageEligibilityRequest>> GetPendingRequestsAsync();

    /// <summary>
    /// Get request by message UUID
    /// </summary>
    Task<CoverageEligibilityRequest?> GetByMessageUUIDAsync(string messageUUID);

    /// <summary>
    /// Get request by request ID with response
    /// </summary>
    Task<CoverageEligibilityRequest?> GetWithResponseAsync(string requestId);
}

/// <summary>
/// Repository interface for CoverageEligibilityResponse operations
/// </summary>
public interface ICoverageEligibilityResponseRepository : IRepository<CoverageEligibilityResponse>
{
    /// <summary>
    /// Get response with all related data (benefits, errors, etc.)
    /// </summary>
    Task<CoverageEligibilityResponse?> GetWithDetailsAsync(string responseId);

    /// <summary>
    /// Get responses by request ID
    /// </summary>
    Task<CoverageEligibilityResponse?> GetByRequestIdAsync(string requestId);

    /// <summary>
    /// Get responses by insurer ID
    /// </summary>
    Task<IEnumerable<CoverageEligibilityResponse>> GetByInsurerIdAsync(string insurerId);

    /// <summary>
    /// Get responses by outcome
    /// </summary>
    Task<IEnumerable<CoverageEligibilityResponse>> GetByOutcomeAsync(string outcome);

    /// <summary>
    /// Get responses with errors
    /// </summary>
    Task<IEnumerable<CoverageEligibilityResponse>> GetWithErrorsAsync();

    /// <summary>
    /// Get response by UUID
 /// </summary>
    Task<CoverageEligibilityResponse?> GetByResponseUUIDAsync(string responseUUID);
}

/// <summary>
/// Repository interface for Patient operations
/// </summary>
public interface IPatientRepository : IRepository<Patient>
{
    /// <summary>
    /// Get patient by MRN
    /// </summary>
    Task<Patient?> GetByMRNAsync(string mrn);

    /// <summary>
    /// Get patient with all coverage and eligibility data
    /// </summary>
    Task<Patient?> GetWithCoverageAndEligibilityAsync(string patientId);

    /// <summary>
    /// Get patient by national ID
    /// </summary>
    Task<Patient?> GetByNationalIdAsync(string nationalId);

    /// <summary>
    /// Check if patient exists by MRN
    /// </summary>
    Task<bool> ExistsByMRNAsync(string mrn);
}

/// <summary>
/// Repository interface for Coverage operations
/// </summary>
public interface ICoverageRepository : IRepository<Coverage>
{
    /// <summary>
    /// Get coverage by policy number
    /// </summary>
    Task<Coverage?> GetByPolicyNumberAsync(string policyNumber);

    /// <summary>
    /// Get active coverage for patient
    /// </summary>
    Task<IEnumerable<Coverage>> GetActiveByPatientIdAsync(string patientId);

    /// <summary>
/// Get coverage with all related data
    /// </summary>
    Task<Coverage?> GetWithDetailsAsync(string coverageId);

    /// <summary>
    /// Check if coverage is active on specific date
    /// </summary>
    Task<bool> IsCoverageActiveAsync(string coverageId, DateTime date);
}

/// <summary>
/// Repository interface for Organization operations
 /// </summary>
public interface IOrganizationRepository : IRepository<Organization>
{
    /// <summary>
    /// Get organization by license number
    /// </summary>
 Task<Organization?> GetByLicenseNumberAsync(string licenseNumber);

    /// <summary>
  /// Get all providers
/// </summary>
    Task<IEnumerable<Organization>> GetProvidersAsync();

    /// <summary>
    /// Get all insurers
    /// </summary>
    Task<IEnumerable<Organization>> GetInsurersAsync();

    /// <summary>
    /// Get organization with all locations and practitioners
    /// </summary>
    Task<Organization?> GetWithDetailsAsync(string organizationId);
}

/// <summary>
/// Repository interface for BenefitBalance operations
/// </summary>
public interface IBenefitBalanceRepository : IRepository<BenefitBalance>
{
    /// <summary>
 /// Get benefit balances for a response with benefits
    /// </summary>
 Task<IEnumerable<BenefitBalance>> GetWithBenefitsAsync(string responseId);

    /// <summary>
    /// Get benefit balance by category
    /// </summary>
    Task<BenefitBalance?> GetByCategoryAsync(string responseId, string category);

    /// <summary>
 /// Get all benefit balances for a response
    /// </summary>
    Task<IEnumerable<BenefitBalance>> GetByResponseIdAsync(string responseId);
}

/// <summary>
/// Repository interface for EligibilityError operations
 /// </summary>
public interface IEligibilityErrorRepository : IRepository<EligibilityError>
{
    /// <summary>
    /// Get errors for a request
    /// </summary>
    Task<IEnumerable<EligibilityError>> GetRequestErrorsAsync(string requestId);

    /// <summary>
    /// Get errors for a response
    /// </summary>
    Task<IEnumerable<EligibilityError>> GetResponseErrorsAsync(string responseId);

    /// <summary>
    /// Get critical errors
    /// </summary>
    Task<IEnumerable<EligibilityError>> GetCriticalErrorsAsync();

    /// <summary>
    /// Get errors by code
    /// </summary>
    Task<IEnumerable<EligibilityError>> GetByErrorCodeAsync(string errorCode);
}
