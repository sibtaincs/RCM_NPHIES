using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Application.Exceptions;
using NPhies_FHIR_Integration.Application.Services.FHIR;
using NPhies_FHIR_Integration.Application.Services.Http;
using NPhies_FHIR_Integration.Application.Services.Polling;
using DomainPatient = NPhies_FHIR_Integration.Domain.Entities.Patient;
using DomainOrganization = NPhies_FHIR_Integration.Domain.Entities.Organization;
using DomainCoverage = NPhies_FHIR_Integration.Domain.Entities.Coverage;
using DomainClaim = NPhies_FHIR_Integration.Domain.Entities.Claim;
using DomainEncounter = NPhies_FHIR_Integration.Domain.Entities.Encounter;
using DomainCoverageEligibilityRequest = NPhies_FHIR_Integration.Domain.Entities.CoverageEligibilityRequest;
using DomainCoverageEligibilityResponse = NPhies_FHIR_Integration.Domain.Entities.CoverageEligibilityResponse;
using DomainClaimResponse = NPhies_FHIR_Integration.Domain.Entities.ClaimResponse;

namespace NPhies_FHIR_Integration.Application.Services.Orchestration;

/// <summary>
/// NPHIES orchestration service implementation
/// Coordinates end-to-end workflows: bundle creation ? submission ? polling ? response parsing
/// </summary>
public class NphiesOrchestrationService : INphiesOrchestrationService
{
    private readonly IFhirBundleService _bundleService;
    private readonly INphiesHttpClient _httpClient;
    private readonly INphiesPollingService _pollingService;
    private readonly ILogger<NphiesOrchestrationService> _logger;
    private readonly NphiesConfiguration _config;

    public NphiesOrchestrationService(
        IFhirBundleService bundleService,
        INphiesHttpClient httpClient,
        INphiesPollingService pollingService,
        ILogger<NphiesOrchestrationService> logger,
        IOptions<NphiesConfiguration> config)
    {
        _bundleService = bundleService ?? throw new ArgumentNullException(nameof(bundleService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _pollingService = pollingService ?? throw new ArgumentNullException(nameof(pollingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Submit eligibility request and get response (end-to-end)
    /// </summary>
    public async Task<DomainCoverageEligibilityResponse> SubmitEligibilityRequestAsync(
        DomainCoverageEligibilityRequest request,
        DomainPatient patient,
        DomainCoverage coverage,
        DomainOrganization provider,
        DomainOrganization insurer,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting eligibility request submission for request {RequestId}", request.RequestId);

            // Step 1: Create FHIR bundle
            _logger.LogDebug("Step 1: Creating eligibility request bundle");
            var requestBundle = await _bundleService.CreateEligibilityRequestBundleAsync(
                request, patient, coverage, provider, insurer);

            _logger.LogInformation("Created eligibility bundle with {Count} entries", requestBundle.Entry?.Count ?? 0);

            // Step 2: Submit bundle to NPHIES
            _logger.LogDebug("Step 2: Submitting bundle to NPHIES");
            var responseBundle = await _httpClient.SubmitBundleAsync(
                requestBundle,
                _config.Endpoints.Eligibility,
                cancellationToken);

            _logger.LogInformation("Received response bundle with {Count} entries", responseBundle.Entry?.Count ?? 0);

            // Step 3: Check if response is async (contains Task resource)
            var taskResource = await _bundleService.ParseResourceFromBundleAsync<Hl7.Fhir.Model.Task>(responseBundle);

            if (taskResource != null)
            {
                _logger.LogInformation("Response is async. Task ID: {TaskId}. Starting polling.", taskResource.Id);

                // Step 4: Poll for async response
                var timeout = TimeSpan.FromMinutes(_config.Polling.MaxDurationMinutes);
                responseBundle = await _pollingService.PollForResponseAsync(
                    taskResource.Id,
                    timeout,
                    cancellationToken);

                if (responseBundle == null)
                {
                    throw new NphiesTimeoutException(
                        $"Polling timeout for eligibility request {request.RequestId}",
                        _config.Polling.MaxDurationMinutes * 60);
                }

                _logger.LogInformation("Received async response bundle with {Count} entries",
                    responseBundle.Entry?.Count ?? 0);
            }

            // Step 5: Parse response bundle to domain entity
            _logger.LogDebug("Step 5: Parsing response bundle");
            var eligibilityResponse = await _bundleService.ParseEligibilityResponseAsync(responseBundle);

            _logger.LogInformation("Successfully completed eligibility request {RequestId}. Response status: {Status}",
                request.RequestId, eligibilityResponse.Status);

            return eligibilityResponse;
        }
        catch (NphiesException)
        {
            // Re-throw NPHIES exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting eligibility request {RequestId}", request.RequestId);
            throw new NphiesException(
                $"Failed to submit eligibility request: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Submit claim and get response (end-to-end)
    /// </summary>
    public async Task<DomainClaimResponse> SubmitClaimAsync(
        DomainClaim claim,
        DomainPatient patient,
        DomainOrganization provider,
        DomainOrganization insurer,
        DomainCoverage coverage,
        DomainEncounter? encounter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting claim submission for claim {ClaimNumber}", claim.ClaimNumber);

            // Step 1: Create FHIR bundle
            _logger.LogDebug("Step 1: Creating claim request bundle");
            var requestBundle = await _bundleService.CreateClaimRequestBundleAsync(
                claim, patient, provider, insurer, coverage, encounter);

            _logger.LogInformation("Created claim bundle with {Count} entries", requestBundle.Entry?.Count ?? 0);

            // Step 2: Submit bundle to NPHIES
            _logger.LogDebug("Step 2: Submitting bundle to NPHIES");
            var responseBundle = await _httpClient.SubmitBundleAsync(
                requestBundle,
                _config.Endpoints.Claim,
                cancellationToken);

            _logger.LogInformation("Received response bundle with {Count} entries", responseBundle.Entry?.Count ?? 0);

            // Step 3: Check if response is async (contains Task resource)
            var taskResource = await _bundleService.ParseResourceFromBundleAsync<Hl7.Fhir.Model.Task>(responseBundle);

            if (taskResource != null)
            {
                _logger.LogInformation("Response is async. Task ID: {TaskId}. Starting polling.", taskResource.Id);

                // Step 4: Poll for async response
                var timeout = TimeSpan.FromMinutes(_config.Polling.MaxDurationMinutes);
                responseBundle = await _pollingService.PollForResponseAsync(
                    taskResource.Id,
                    timeout,
                    cancellationToken);

                if (responseBundle == null)
                {
                    throw new NphiesTimeoutException(
                        $"Polling timeout for claim {claim.ClaimNumber}",
                        _config.Polling.MaxDurationMinutes * 60);
                }

                _logger.LogInformation("Received async response bundle with {Count} entries",
                    responseBundle.Entry?.Count ?? 0);
            }

            // Step 5: Parse response bundle to domain entity
            _logger.LogDebug("Step 5: Parsing response bundle");
            var claimResponse = await _bundleService.ParseClaimResponseAsync(responseBundle);

            _logger.LogInformation("Successfully completed claim {ClaimNumber}. Response status: {Status}",
                claim.ClaimNumber, claimResponse.ClaimResponseStatus);

            return claimResponse;
        }
        catch (NphiesException)
        {
            // Re-throw NPHIES exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting claim {ClaimNumber}", claim.ClaimNumber);
            throw new NphiesException(
                $"Failed to submit claim: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Submit pre-authorization and get response (end-to-end)
    /// </summary>
    public async Task<DomainClaimResponse> SubmitPreAuthorizationAsync(
        DomainClaim preAuth,
        DomainPatient patient,
        DomainOrganization provider,
        DomainOrganization insurer,
        DomainCoverage coverage,
        DomainEncounter? encounter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting pre-authorization submission for claim {ClaimNumber}", preAuth.ClaimNumber);

            // Step 1: Create FHIR bundle
            _logger.LogDebug("Step 1: Creating pre-authorization request bundle");
            var requestBundle = await _bundleService.CreatePreAuthRequestBundleAsync(
                preAuth, patient, provider, insurer, coverage, encounter);

            _logger.LogInformation("Created pre-auth bundle with {Count} entries", requestBundle.Entry?.Count ?? 0);

            // Step 2: Submit bundle to NPHIES
            _logger.LogDebug("Step 2: Submitting bundle to NPHIES");
            var responseBundle = await _httpClient.SubmitBundleAsync(
                requestBundle,
                _config.Endpoints.PreAuth,
                cancellationToken);

            _logger.LogInformation("Received response bundle with {Count} entries", responseBundle.Entry?.Count ?? 0);

            // Step 3: Check if response is async (contains Task resource)
            var taskResource = await _bundleService.ParseResourceFromBundleAsync<Hl7.Fhir.Model.Task>(responseBundle);

            if (taskResource != null)
            {
                _logger.LogInformation("Response is async. Task ID: {TaskId}. Starting polling.", taskResource.Id);

                // Step 4: Poll for async response
                var timeout = TimeSpan.FromMinutes(_config.Polling.MaxDurationMinutes);
                responseBundle = await _pollingService.PollForResponseAsync(
                    taskResource.Id,
                    timeout,
                    cancellationToken);

                if (responseBundle == null)
                {
                    throw new NphiesTimeoutException(
                        $"Polling timeout for pre-authorization {preAuth.ClaimNumber}",
                        _config.Polling.MaxDurationMinutes * 60);
                }

                _logger.LogInformation("Received async response bundle with {Count} entries",
                    responseBundle.Entry?.Count ?? 0);
            }

            // Step 5: Parse response bundle to domain entity
            _logger.LogDebug("Step 5: Parsing response bundle");
            var preAuthResponse = await _bundleService.ParsePreAuthResponseAsync(responseBundle);

            _logger.LogInformation("Successfully completed pre-authorization {ClaimNumber}. Response status: {Status}",
                preAuth.ClaimNumber, preAuthResponse.ClaimResponseStatus);

            return preAuthResponse;
        }
        catch (NphiesException)
        {
            // Re-throw NPHIES exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting pre-authorization {ClaimNumber}", preAuth.ClaimNumber);
            throw new NphiesException(
                $"Failed to submit pre-authorization: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Check NPHIES API health
    /// </summary>
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing NPHIES health check");
            var isHealthy = await _httpClient.HealthCheckAsync(cancellationToken);
            _logger.LogInformation("NPHIES health check result: {IsHealthy}", isHealthy);
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NPHIES health check failed");
            return false;
        }
    }

    /// <summary>
    /// Get NPHIES capability statement
    /// </summary>
    public async Task<CapabilityStatement> GetCapabilityStatementAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting NPHIES capability statement");
            var capability = await _httpClient.GetCapabilityStatementAsync(cancellationToken);
            _logger.LogInformation("Retrieved NPHIES capability statement: {Name} v{Version}",
                capability.Name, capability.Version);
            return capability;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get NPHIES capability statement");
            throw new NphiesException("Failed to get capability statement", ex);
        }
    }
}
