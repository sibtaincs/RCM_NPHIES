using Hl7.Fhir.Model;
using System.Threading.Tasks;

// Type aliases to avoid ambiguous references
using DomainPatient = NPhies_FHIR_Integration.Domain.Entities.Patient;
using DomainOrganization = NPhies_FHIR_Integration.Domain.Entities.Organization;
using DomainCoverage = NPhies_FHIR_Integration.Domain.Entities.Coverage;
using DomainClaim = NPhies_FHIR_Integration.Domain.Entities.Claim;
using DomainClaimResponse = NPhies_FHIR_Integration.Domain.Entities.ClaimResponse;
using DomainEncounter = NPhies_FHIR_Integration.Domain.Entities.Encounter;
using DomainCoverageEligibilityRequest = NPhies_FHIR_Integration.Domain.Entities.CoverageEligibilityRequest;
using DomainCoverageEligibilityResponse = NPhies_FHIR_Integration.Domain.Entities.CoverageEligibilityResponse;

namespace NPhies_FHIR_Integration.Application.Services.FHIR;

/// <summary>
/// FHIR Bundle Service Interface
/// Handles creation and parsing of FHIR R4 bundles for NPHIES communication
/// </summary>
public interface IFhirBundleService
{
    // ==========================================
    // Eligibility Bundles
    // ==========================================
    
    /// <summary>
    /// Create FHIR Bundle for eligibility request
    /// </summary>
    /// <param name="request">Coverage eligibility request entity</param>
    /// <param name="patient">Patient entity</param>
    /// <param name="coverage">Coverage entity</param>
    /// <param name="provider">Provider organization</param>
    /// <param name="insurer">Insurer organization</param>
    /// <returns>FHIR Bundle ready for submission</returns>
    Task<Bundle> CreateEligibilityRequestBundleAsync(
        DomainCoverageEligibilityRequest request,
        DomainPatient patient,
        DomainCoverage coverage,
        DomainOrganization provider,
        DomainOrganization insurer);

    /// <summary>
    /// Parse eligibility response bundle
    /// </summary>
    /// <param name="responseBundle">Response bundle from NPHIES</param>
    /// <returns>Coverage eligibility response entity</returns>
    Task<DomainCoverageEligibilityResponse> ParseEligibilityResponseAsync(Bundle responseBundle);

    // ==========================================
    // Claim Bundles
    // ==========================================
    
    /// <summary>
    /// Create FHIR Bundle for claim request
    /// </summary>
    /// <param name="claim">Claim entity</param>
    /// <param name="patient">Patient entity</param>
    /// <param name="provider">Provider organization</param>
    /// <param name="insurer">Insurer organization</param>
    /// <param name="coverage">Coverage entity</param>
    /// <param name="encounter">Encounter entity (optional for professional claims)</param>
    /// <returns>FHIR Bundle ready for submission</returns>
    Task<Bundle> CreateClaimRequestBundleAsync(
     DomainClaim claim,
   DomainPatient patient,
   DomainOrganization provider,
DomainOrganization insurer,
      DomainCoverage coverage,
        DomainEncounter? encounter = null);

    /// <summary>
    /// Parse claim response bundle
    /// </summary>
    /// <param name="responseBundle">Response bundle from NPHIES</param>
    /// <returns>Claim response entity</returns>
    Task<DomainClaimResponse> ParseClaimResponseAsync(Bundle responseBundle);

    // ==========================================
    // Pre-Authorization Bundles
    // ==========================================
    
    /// <summary>
    /// Create FHIR Bundle for pre-authorization request
    /// </summary>
    /// <param name="preAuth">Pre-auth claim entity (use=preauthorization)</param>
    /// <param name="patient">Patient entity</param>
    /// <param name="provider">Provider organization</param>
    /// <param name="insurer">Insurer organization</param>
    /// <param name="coverage">Coverage entity</param>
    /// <param name="encounter">Encounter entity (optional)</param>
    /// <returns>FHIR Bundle ready for submission</returns>
    Task<Bundle> CreatePreAuthRequestBundleAsync(
        DomainClaim preAuth,
    DomainPatient patient,
      DomainOrganization provider,
  DomainOrganization insurer,
        DomainCoverage coverage,
     DomainEncounter? encounter = null);

    /// <summary>
    /// Parse pre-authorization response bundle
    /// </summary>
    /// <param name="responseBundle">Response bundle from NPHIES</param>
    /// <returns>Claim response entity</returns>
    Task<DomainClaimResponse> ParsePreAuthResponseAsync(Bundle responseBundle);

  // ==========================================
    // Polling Bundles
    // ==========================================
    
    /// <summary>
    /// Create FHIR Bundle for polling request
    /// </summary>
    /// <param name="taskId">Task ID to poll</param>
    /// <param name="provider">Provider organization</param>
    /// <returns>FHIR Bundle ready for submission</returns>
    Task<Bundle> CreatePollRequestBundleAsync(string taskId, DomainOrganization provider);

    // ==========================================
    // Serialization
    // ==========================================
    
    /// <summary>
    /// Serialize FHIR Bundle to JSON string
    /// </summary>
    /// <param name="bundle">FHIR Bundle</param>
    /// <returns>JSON string</returns>
    Task<string> SerializeToJsonAsync(Bundle bundle);

    /// <summary>
 /// Deserialize JSON string to FHIR Bundle
    /// </summary>
    /// <param name="json">JSON string</param>
    /// <returns>FHIR Bundle</returns>
    Task<Bundle> DeserializeFromJsonAsync(string json);

    /// <summary>
    /// Parse specific resource from bundle
    /// </summary>
    /// <typeparam name="T">FHIR Resource type</typeparam>
    /// <param name="bundle">FHIR Bundle</param>
    /// <returns>Resource of type T</returns>
    Task<T?> ParseResourceFromBundleAsync<T>(Bundle bundle) where T : Resource;

    /// <summary>
    /// Extract all resources of a specific type from bundle
    /// </summary>
    /// <typeparam name="T">FHIR Resource type</typeparam>
    /// <param name="bundle">FHIR Bundle</param>
    /// <returns>List of resources of type T</returns>
    Task<List<T>> ExtractResourcesAsync<T>(Bundle bundle) where T : Resource;

    // ==========================================
    // Validation
    // ==========================================
    
    /// <summary>
    /// Validate FHIR Bundle structure
    /// </summary>
    /// <param name="bundle">FHIR Bundle</param>
    /// <returns>Validation result with errors/warnings</returns>
    Task<BundleValidationResult> ValidateBundleAsync(Bundle bundle);
}

/// <summary>
/// Bundle validation result
/// </summary>
public class BundleValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}
