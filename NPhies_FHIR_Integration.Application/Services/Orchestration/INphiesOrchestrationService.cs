using Hl7.Fhir.Model;
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
/// Orchestration service interface for coordinating end-to-end NPHIES workflows
/// </summary>
public interface INphiesOrchestrationService
{
    /// <summary>
    /// Submit eligibility request and get response (end-to-end)
    /// </summary>
    /// <param name="request">Eligibility request</param>
    /// <param name="patient">Patient</param>
    /// <param name="coverage">Coverage</param>
 /// <param name="provider">Provider organization</param>
    /// <param name="insurer">Insurer organization</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Eligibility response from NPHIES</returns>
    Task<DomainCoverageEligibilityResponse> SubmitEligibilityRequestAsync(
        DomainCoverageEligibilityRequest request,
        DomainPatient patient,
        DomainCoverage coverage,
        DomainOrganization provider,
     DomainOrganization insurer,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit claim and get response (end-to-end)
    /// </summary>
  /// <param name="claim">Claim</param>
    /// <param name="patient">Patient</param>
    /// <param name="provider">Provider organization</param>
    /// <param name="insurer">Insurer organization</param>
    /// <param name="coverage">Coverage</param>
    /// <param name="encounter">Encounter (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Claim response from NPHIES</returns>
    Task<DomainClaimResponse> SubmitClaimAsync(
        DomainClaim claim,
        DomainPatient patient,
        DomainOrganization provider,
        DomainOrganization insurer,
      DomainCoverage coverage,
        DomainEncounter? encounter = null,
   CancellationToken cancellationToken = default);

  /// <summary>
    /// Submit pre-authorization request and get response (end-to-end)
    /// </summary>
    /// <param name="preAuth">Pre-authorization claim</param>
    /// <param name="patient">Patient</param>
/// <param name="provider">Provider organization</param>
    /// <param name="insurer">Insurer organization</param>
    /// <param name="coverage">Coverage</param>
    /// <param name="encounter">Encounter (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pre-authorization response from NPHIES</returns>
    Task<DomainClaimResponse> SubmitPreAuthorizationAsync(
        DomainClaim preAuth,
        DomainPatient patient,
        DomainOrganization provider,
        DomainOrganization insurer,
    DomainCoverage coverage,
        DomainEncounter? encounter = null,
   CancellationToken cancellationToken = default);

    /// <summary>
    /// Check health of NPHIES API
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if NPHIES is accessible</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get NPHIES capability statement
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Capability statement</returns>
    Task<CapabilityStatement> GetCapabilityStatementAsync(CancellationToken cancellationToken = default);
}
