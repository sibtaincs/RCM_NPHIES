using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Application.Mapping;

// Type aliases to avoid ambiguous references between domain entities and FHIR models
using Task = System.Threading.Tasks.Task;
using DomainPatient = NPhies_FHIR_Integration.Domain.Entities.Patient;
using DomainOrganization = NPhies_FHIR_Integration.Domain.Entities.Organization;
using DomainCoverage = NPhies_FHIR_Integration.Domain.Entities.Coverage;
using DomainClaim = NPhies_FHIR_Integration.Domain.Entities.Claim;
using DomainClaimResponse = NPhies_FHIR_Integration.Domain.Entities.ClaimResponse;
using DomainEncounter = NPhies_FHIR_Integration.Domain.Entities.Encounter;
using DomainCoverageEligibilityRequest = NPhies_FHIR_Integration.Domain.Entities.CoverageEligibilityRequest;
using DomainCoverageEligibilityResponse = NPhies_FHIR_Integration.Domain.Entities.CoverageEligibilityResponse;
using DomainBenefitBalance = NPhies_FHIR_Integration.Domain.Entities.BenefitBalance;
using DomainBenefit = NPhies_FHIR_Integration.Domain.Entities.Benefit;
using DomainEligibilityError = NPhies_FHIR_Integration.Domain.Entities.EligibilityError;

using FhirPatient = Hl7.Fhir.Model.Patient;
using FhirOrganization = Hl7.Fhir.Model.Organization;
using FhirCoverage = Hl7.Fhir.Model.Coverage;
using FhirClaim = Hl7.Fhir.Model.Claim;
using FhirClaimResponse = Hl7.Fhir.Model.ClaimResponse;
using FhirEncounter = Hl7.Fhir.Model.Encounter;
using FhirMessageHeader = Hl7.Fhir.Model.MessageHeader;
using FhirCoverageEligibilityRequest = Hl7.Fhir.Model.CoverageEligibilityRequest;
using FhirCoverageEligibilityResponse = Hl7.Fhir.Model.CoverageEligibilityResponse;
using FhirTask = Hl7.Fhir.Model.Task;

namespace NPhies_FHIR_Integration.Application.Services.FHIR;

/// <summary>
/// FHIR Bundle Service Implementation
/// Handles creation and parsing of NPHIES-compliant FHIR R4 bundles
/// </summary>
public class FhirBundleService : IFhirBundleService
{
    private readonly ILogger<FhirBundleService> _logger;
    private readonly IEntityToFhirMapper _mapper;
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonParser _parser;

    public FhirBundleService(
    ILogger<FhirBundleService> logger,
  IEntityToFhirMapper mapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        // Initialize FHIR serializer and parser with settings
        var serializerSettings = new SerializerSettings
   {
            Pretty = true,
            AppendNewLine = false
        };

        _serializer = new FhirJsonSerializer(serializerSettings);
   _parser = new FhirJsonParser();
    }

    public Task<Bundle> CreateEligibilityRequestBundleAsync(
        DomainCoverageEligibilityRequest request,
        DomainPatient patient,
        DomainCoverage coverage,
  DomainOrganization provider,
        DomainOrganization insurer)
    {
   _logger.LogInformation("Creating eligibility request bundle for patient {PatientId}", patient.Id);

        // Create FHIR resources
    var fhirPatient = _mapper.MapToFhirPatient(patient);
        var fhirCoverage = _mapper.MapToFhirCoverage(coverage);
        var fhirProvider = _mapper.MapToFhirOrganization(provider);
        var fhirInsurer = _mapper.MapToFhirOrganization(insurer);

        // Create eligibility request
        var fhirRequest = _mapper.MapToFhirEligibilityRequest(
       request,
    $"Patient/{fhirPatient.Id}",
       $"Coverage/{fhirCoverage.Id}",
      $"Organization/{fhirProvider.Id}",
    $"Organization/{fhirInsurer.Id}"
        );

        // Create MessageHeader
        var messageHeader = _mapper.CreateMessageHeader("eligibility-request", provider);
        messageHeader.Focus = new List<ResourceReference>
      {
   new ResourceReference($"CoverageEligibilityRequest/{fhirRequest.Id}")
        };

      // Create bundle
        var bundle = new Bundle
        {
        Type = Bundle.BundleType.Message,
     Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
          Entry = new List<Bundle.EntryComponent>
    {
        new Bundle.EntryComponent
         {
        FullUrl = $"urn:uuid:{messageHeader.Id}",
          Resource = messageHeader
    },
    new Bundle.EntryComponent
     {
         FullUrl = $"urn:uuid:{fhirRequest.Id}",
               Resource = fhirRequest
            },
       new Bundle.EntryComponent
                {
   FullUrl = $"urn:uuid:{fhirPatient.Id}",
         Resource = fhirPatient
                },
        new Bundle.EntryComponent
        {
        FullUrl = $"urn:uuid:{fhirCoverage.Id}",
     Resource = fhirCoverage
             },
       new Bundle.EntryComponent
  {
         FullUrl = $"urn:uuid:{fhirProvider.Id}",
         Resource = fhirProvider
    },
       new Bundle.EntryComponent
    {
     FullUrl = $"urn:uuid:{fhirInsurer.Id}",
Resource = fhirInsurer
      }
            }
        };

        _logger.LogInformation("Created eligibility bundle with {Count} entries", bundle.Entry.Count);
        return Task.FromResult(bundle);
    }

    public async Task<DomainCoverageEligibilityResponse> ParseEligibilityResponseAsync(Bundle responseBundle)
    {
        _logger.LogInformation("Parsing eligibility response bundle");

        try
        {
            // Extract resources
   var eligibilityResponse = await ParseResourceFromBundleAsync<FhirCoverageEligibilityResponse>(responseBundle);
     if (eligibilityResponse == null)
 {
    throw new InvalidOperationException("No CoverageEligibilityResponse found in bundle");
            }

      var outcome = await ParseResourceFromBundleAsync<OperationOutcome>(responseBundle);

            // Map to domain entity
       var response = new DomainCoverageEligibilityResponse
       {
                Id = eligibilityResponse.Id,
       ResponseUUID = Guid.NewGuid().ToString(),
     Status = eligibilityResponse.Status.ToString().ToLower(),
     Outcome = eligibilityResponse.Outcome.ToString().ToLower(),
          ResponseCreatedAt = DateTime.Parse(eligibilityResponse.Created),
     ResponseReceivedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
   IsActive = true
 };

         // Parse insurance details
      if (eligibilityResponse.Insurance?.Any() == true)
            {
     var insurance = eligibilityResponse.Insurance.First();
response.IsInForce = insurance.Inforce ?? false;
       response.EligibilityStatus = insurance.Inforce == true ? "active" : "inactive";

  // Parse benefit balances
       if (insurance.Item?.Any() == true)
    {
     response.BenefitBalances = new List<DomainBenefitBalance>();
 foreach (var item in insurance.Item)
 {
       var benefitBalance = new DomainBenefitBalance
        {
      Id = Guid.NewGuid().ToString(),
         Category = item.Category?.Coding?.FirstOrDefault()?.Code,
 CreatedAt = DateTime.UtcNow,
     IsActive = true,
       Benefits = new List<DomainBenefit>()
   };

         if (item.Benefit?.Any() == true)
   {
   foreach (var benefit in item.Benefit)
    {
       var domainBenefit = new DomainBenefit
    {
  Id = Guid.NewGuid().ToString(),
    CreatedAt = DateTime.UtcNow,
IsActive = true
  };
 benefitBalance.Benefits.Add(domainBenefit);
        }
 }

  response.BenefitBalances.Add(benefitBalance);
   }
    }
    }

    // Parse errors if any
     if (outcome != null && outcome.Issue?.Any() == true)
 {
     response.Errors = new List<DomainEligibilityError>();
      foreach (var issue in outcome.Issue)
         {
           response.Errors.Add(new DomainEligibilityError
          {
       Id = Guid.NewGuid().ToString(),
ErrorCode = issue.Code?.ToString(),
     ErrorMessage = issue.Diagnostics,
      CreatedAt = DateTime.UtcNow,
        IsActive = true
   });
  }
 }
  _logger.LogInformation("Successfully parsed eligibility response. Status: {Status}, Outcome: {Outcome}",
    response.Status, response.Outcome);

       return response;
    }
     catch (Exception ex)
        {
      _logger.LogError(ex, "Failed to parse eligibility response bundle");
         throw;
        }
    }

    public Task<Bundle> CreateClaimRequestBundleAsync(
      DomainClaim claim,
        DomainPatient patient,
        DomainOrganization provider,
        DomainOrganization insurer,
    DomainCoverage coverage,
        DomainEncounter? encounter = null)
 {
        _logger.LogInformation("Creating claim request bundle for claim {ClaimNumber}", claim.ClaimNumber);

        // Create FHIR resources
        var fhirPatient = _mapper.MapToFhirPatient(patient);
        var fhirProvider = _mapper.MapToFhirOrganization(provider);
     var fhirInsurer = _mapper.MapToFhirOrganization(insurer);
        var fhirCoverage = _mapper.MapToFhirCoverage(coverage);

    // Create claim
        var fhirClaim = _mapper.MapToFhirClaim(
            claim,
$"Patient/{fhirPatient.Id}",
            $"Organization/{fhirProvider.Id}",
            $"Organization/{fhirInsurer.Id}",
        $"Coverage/{fhirCoverage.Id}",
    encounter != null ? $"Encounter/{encounter.Id}" : null
        );

        // Create MessageHeader
        var messageHeader = _mapper.CreateMessageHeader("claim-request", provider);
      messageHeader.Focus = new List<ResourceReference>
        {
new ResourceReference($"Claim/{fhirClaim.Id}")
        };

        // Build bundle entries
        var entries = new List<Bundle.EntryComponent>
        {
  new Bundle.EntryComponent { FullUrl = $"urn:uuid:{messageHeader.Id}", Resource = messageHeader },
    new Bundle.EntryComponent { FullUrl = $"urn:uuid:{fhirClaim.Id}", Resource = fhirClaim },
            new Bundle.EntryComponent { FullUrl = $"urn:uuid:{fhirPatient.Id}", Resource = fhirPatient },
        new Bundle.EntryComponent { FullUrl = $"urn:uuid:{fhirProvider.Id}", Resource = fhirProvider },
   new Bundle.EntryComponent { FullUrl = $"urn:uuid:{fhirInsurer.Id}", Resource = fhirInsurer },
          new Bundle.EntryComponent { FullUrl = $"urn:uuid:{fhirCoverage.Id}", Resource = fhirCoverage }
        };

    // Add encounter if provided
        if (encounter != null)
  {
   var fhirEncounter = _mapper.MapToFhirEncounter(encounter);
   entries.Add(new Bundle.EntryComponent
            {
              FullUrl = $"urn:uuid:{fhirEncounter.Id}",
           Resource = fhirEncounter
       });
        }

        if (claim.Practitioner != null)
        {
          var fhirPractitioner = _mapper.MapToFhirPractitioner(claim.Practitioner);
          entries.Add(new Bundle.EntryComponent
          {
            FullUrl = $"urn:uuid:{fhirPractitioner.Id}",
            Resource = fhirPractitioner
          });
        }

        if (claim.ServiceLocation != null)
        {
          var fhirLocation = _mapper.MapToFhirLocation(claim.ServiceLocation);
          entries.Add(new Bundle.EntryComponent
          {
            FullUrl = $"urn:uuid:{fhirLocation.Id}",
            Resource = fhirLocation
          });
        }

        // Create bundle
    var bundle = new Bundle
        {
   Type = Bundle.BundleType.Message,
      Id = Guid.NewGuid().ToString(),
    Timestamp = DateTimeOffset.UtcNow,
            Entry = entries
        };

   _logger.LogInformation("Created claim bundle with {Count} entries", bundle.Entry.Count);
        return Task.FromResult(bundle);
    }

    public Task<DomainClaimResponse> ParseClaimResponseAsync(Bundle responseBundle)
    {
  _logger.LogInformation("Parsing claim response bundle");
        
        // TODO: Implement claim response parsing similar to eligibility response
        // This is a simplified stub - you can expand it based on your domain model
  
        var response = new DomainClaimResponse
        {
Id = Guid.NewGuid().ToString(),
     ClaimResponseStatus = "active",
    CreatedAt = DateTime.UtcNow,
 IsActive = true
      };
        
return Task.FromResult(response);
    }

    public Task<Bundle> CreatePreAuthRequestBundleAsync(
   DomainClaim preAuth,
        DomainPatient patient,
        DomainOrganization provider,
      DomainOrganization insurer,
        DomainCoverage coverage,
        DomainEncounter? encounter = null)
    {
  _logger.LogInformation("Creating pre-auth request bundle for claim {ClaimNumber}", preAuth.ClaimNumber);
        
     // Pre-auth is same as claim, just with use=preauthorization
        return CreateClaimRequestBundleAsync(preAuth, patient, provider, insurer, coverage, encounter);
    }

 public Task<DomainClaimResponse> ParsePreAuthResponseAsync(Bundle responseBundle)
 {
        _logger.LogInformation("Parsing pre-auth response bundle");
        
        var response = new DomainClaimResponse
        {
         Id = Guid.NewGuid().ToString(),
 ClaimResponseStatus = "active",
            Use = "preauthorization",
CreatedAt = DateTime.UtcNow,
IsActive = true
 };
        
        return Task.FromResult(response);
    }

    public Task<Bundle> CreatePollRequestBundleAsync(string taskId, DomainOrganization provider)
    {
        _logger.LogInformation("Creating poll request bundle for task {TaskId}", taskId);
        // Implementation stub
        var bundle = new Bundle
        {
     Type = Bundle.BundleType.Message,
   Timestamp = DateTimeOffset.UtcNow,
            Id = Guid.NewGuid().ToString()
  };
        return Task.FromResult(bundle);
    }

    public Task<string> SerializeToJsonAsync(Bundle bundle)
    {
        _logger.LogDebug("Serializing FHIR bundle to JSON");
var json = _serializer.SerializeToString(bundle);
        _logger.LogDebug("Bundle serialized successfully. Size: {Size} bytes", Encoding.UTF8.GetByteCount(json));
        return Task.FromResult(json);
    }

 public Task<Bundle> DeserializeFromJsonAsync(string json)
    {
        _logger.LogDebug("Deserializing JSON to FHIR bundle");
        var bundle = _parser.Parse<Bundle>(json);
        _logger.LogDebug("Bundle deserialized successfully. Entries: {Count}", bundle.Entry?.Count ?? 0);
    return Task.FromResult(bundle);
    }

    public Task<T?> ParseResourceFromBundleAsync<T>(Bundle bundle) where T : Resource
    {
        var resource = bundle.Entry?
   .Select(e => e.Resource)
            .OfType<T>()
     .FirstOrDefault();

    if (resource == null)
        {
        _logger.LogWarning("Resource of type {ResourceType} not found in bundle", typeof(T).Name);
        }

        return Task.FromResult(resource);
    }

    public Task<List<T>> ExtractResourcesAsync<T>(Bundle bundle) where T : Resource
    {
        var resources = bundle.Entry?
    .Select(e => e.Resource)
       .OfType<T>()
            .ToList() ?? new List<T>();

        _logger.LogDebug("Extracted {Count} resources of type {ResourceType}", resources.Count, typeof(T).Name);
    return Task.FromResult(resources);
    }

    public Task<BundleValidationResult> ValidateBundleAsync(Bundle bundle)
    {
     var result = new BundleValidationResult { IsValid = true };

     if (bundle == null)
   {
            result.Errors.Add("Bundle is null");
      result.IsValid = false;
   return Task.FromResult(result);
        }

  if (bundle.Type != Bundle.BundleType.Message)
        {
            result.Errors.Add($"Bundle type must be 'message', got '{bundle.Type}'");
         result.IsValid = false;
        }

        if (bundle.Entry == null || !bundle.Entry.Any())
        {
          result.Errors.Add("Bundle must contain at least one entry");
     result.IsValid = false;
     }

   // Check for MessageHeader (required by NPHIES)
    var hasMessageHeader = bundle.Entry?.Any(e => e.Resource is FhirMessageHeader) ?? false;
        if (!hasMessageHeader)
        {
    result.Errors.Add("Bundle must contain a MessageHeader resource");
result.IsValid = false;
     }

      // Warnings
        if (bundle.Timestamp == null)
        {
            result.Warnings.Add("Bundle timestamp is recommended");
        }

        return Task.FromResult(result);
    }
}
