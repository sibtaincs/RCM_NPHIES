using AutoMapper;
using NPhies_FHIR_Integration.Application.Mapping;
using NPhies_FHIR_Integration.Domain.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;
using NPhies_FHIR_Integration.Infrastructure.FHIR;
using NPhies_FHIR_Integration.Infrastructure.Repositories;

namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// Service interface for eligibility operations
/// </summary>
public interface IEligibilityService
{
    /// <summary>
    /// Submit a coverage eligibility request
    /// </summary>
    Task<CoverageEligibilityRequestDto> SubmitEligibilityRequestAsync(CoverageEligibilityRequestDto requestDto);

    /// <summary>
    /// Check eligibility (alias for SubmitEligibilityRequestAsync)
    /// </summary>
    Task<CoverageEligibilityRequestDto> CheckEligibilityAsync(CoverageEligibilityRequestDto requestDto);

    /// <summary>
    /// Get eligibility request with response
    /// </summary>
    Task<CoverageEligibilityRequestDto?> GetEligibilityRequestAsync(string requestId);

 /// <summary>
    /// Get all pending eligibility requests
    /// </summary>
  Task<IEnumerable<CoverageEligibilityRequestDto>> GetPendingRequestsAsync();

    /// <summary>
    /// Process eligibility response from FHIR JSON
    /// </summary>
    Task<CoverageEligibilityResponseDto> ProcessEligibilityResponseAsync(string fhirResponseJson);

    /// <summary>
    /// Get eligibility response
    /// </summary>
    Task<CoverageEligibilityResponseDto?> GetEligibilityResponseAsync(string responseId);

    /// <summary>
    /// Check coverage eligibility for a patient
    /// </summary>
    Task<CoverageEligibilityResponseDto?> CheckCoverageEligibilityAsync(string patientId, string coverageId, string serviceType);
}

/// <summary>
/// Implementation of eligibility service
/// </summary>
public class EligibilityService : IEligibilityService
{
    private readonly ICoverageEligibilityRequestRepository _requestRepository;
    private readonly ICoverageEligibilityResponseRepository _responseRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICoverageRepository _coverageRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMapper _mapper;
    private readonly IFhirToEntityMapper _fhirMapper;

    /// <summary>
    /// Constructor with dependencies
    /// </summary>
    public EligibilityService(
     ICoverageEligibilityRequestRepository requestRepository,
        ICoverageEligibilityResponseRepository responseRepository,
        IPatientRepository patientRepository,
        ICoverageRepository coverageRepository,
    IOrganizationRepository organizationRepository,
        IMapper mapper,
        IFhirToEntityMapper fhirMapper)
    {
  _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _responseRepository = responseRepository ?? throw new ArgumentNullException(nameof(responseRepository));
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        _coverageRepository = coverageRepository ?? throw new ArgumentNullException(nameof(coverageRepository));
   _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
      _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _fhirMapper = fhirMapper ?? throw new ArgumentNullException(nameof(fhirMapper));
    }

    /// <summary>
    /// Submit a coverage eligibility request
    /// </summary>
    public async Task<CoverageEligibilityRequestDto> SubmitEligibilityRequestAsync(CoverageEligibilityRequestDto requestDto)
    {
        try
        {
     // Validate request
    if (string.IsNullOrEmpty(requestDto.PatientId))
   throw new ArgumentException("Patient ID is required");

 if (string.IsNullOrEmpty(requestDto.CoverageId))
           throw new ArgumentException("Coverage ID is required");

     // Map DTO to entity
            var request = _mapper.Map<CoverageEligibilityRequest>(requestDto);

            // Set system-generated fields
      request.MessageUUID = Guid.NewGuid().ToString();
     request.RequestId = GenerateRequestId();
          request.Status = "active";
            request.MessageStatus = "sent";
    request.RequestCreatedAt = DateTime.UtcNow;
  request.SubmittedAt = DateTime.UtcNow;

  // Add to repository
   await _requestRepository.AddAsync(request);
            await _requestRepository.SaveChangesAsync();

 // Return DTO
      return _mapper.Map<CoverageEligibilityRequestDto>(request);
   }
 catch (Exception ex)
      {
         throw new InvalidOperationException("Failed to submit eligibility request", ex);
        }
    }

    /// <summary>
  /// Check eligibility (alias for SubmitEligibilityRequestAsync)
    /// </summary>
    public async Task<CoverageEligibilityRequestDto> CheckEligibilityAsync(CoverageEligibilityRequestDto requestDto)
    {
            return await SubmitEligibilityRequestAsync(requestDto);
    }

    /// <summary>
  /// Get eligibility request with response
    /// </summary>
    public async Task<CoverageEligibilityRequestDto?> GetEligibilityRequestAsync(string requestId)
    {
        try
        {
    var request = await _requestRepository.GetWithDetailsAsync(requestId);
            return request != null ? _mapper.Map<CoverageEligibilityRequestDto>(request) : null;
        }
        catch (Exception ex)
      {
            throw new InvalidOperationException("Failed to retrieve eligibility request", ex);
        }
    }

    /// <summary>
    /// Get all pending eligibility requests
    /// </summary>
    public async Task<IEnumerable<CoverageEligibilityRequestDto>> GetPendingRequestsAsync()
    {
     try
  {
            var requests = await _requestRepository.GetPendingRequestsAsync();
         return _mapper.Map<IEnumerable<CoverageEligibilityRequestDto>>(requests);
        }
      catch (Exception ex)
        {
      throw new InvalidOperationException("Failed to retrieve pending requests", ex);
        }
 }

    /// <summary>
    /// Process eligibility response from FHIR JSON
    /// </summary>
    public async Task<CoverageEligibilityResponseDto> ProcessEligibilityResponseAsync(string fhirResponseJson)
    {
        try
   {
    // Parse FHIR response
            var (request, resources) = await _fhirMapper.MapFhirBundleAsync(fhirResponseJson);

            if (request == null)
   throw new InvalidOperationException("No CoverageEligibilityResponse found in FHIR bundle");

        // Create response entity
            var response = new CoverageEligibilityResponse
    {
                ResponseUUID = Guid.NewGuid().ToString(),
          Status = "active",
     Outcome = "complete",
       IsInForce = true,
         ResponseCreatedAt = DateTime.UtcNow,
  ResponseReceivedAt = DateTime.UtcNow,
             FhirResponseContent = fhirResponseJson
};

 // Add to repository
  await _responseRepository.AddAsync(response);
await _responseRepository.SaveChangesAsync();

    return _mapper.Map<CoverageEligibilityResponseDto>(response);
  }
        catch (Exception ex)
    {
            throw new InvalidOperationException("Failed to process eligibility response", ex);
        }
    }

    /// <summary>
    /// Get eligibility response
    /// </summary>
    public async Task<CoverageEligibilityResponseDto?> GetEligibilityResponseAsync(string responseId)
    {
        try
        {
            var response = await _responseRepository.GetWithDetailsAsync(responseId);
       return response != null ? _mapper.Map<CoverageEligibilityResponseDto>(response) : null;
        }
        catch (Exception ex)
        {
     throw new InvalidOperationException("Failed to retrieve eligibility response", ex);
        }
    }

    /// <summary>
    /// Check coverage eligibility for a patient
    /// </summary>
    public async Task<CoverageEligibilityResponseDto?> CheckCoverageEligibilityAsync(string patientId, string coverageId, string serviceType)
    {
try
        {
            // Verify patient exists
     var patient = await _patientRepository.GetByIdAsync(patientId);
 if (patient == null)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found");

            // Verify coverage exists and is active
       var coverage = await _coverageRepository.GetByIdAsync(coverageId);
            if (coverage == null)
throw new KeyNotFoundException($"Coverage with ID {coverageId} not found");

        if (!coverage.IsActiveOn(DateTime.UtcNow))
     throw new InvalidOperationException("Coverage is not active for current date");

       // Create eligibility request
    var requestDto = new CoverageEligibilityRequestDto
  {
   PatientId = patientId,
    CoverageId = coverageId,
    ServiceType = serviceType,
       ServiceDate = DateTime.UtcNow,
     Status = "active",
              Priority = "normal"
      };

 // Submit request
            var submittedRequest = await SubmitEligibilityRequestAsync(requestDto);

      // Get response (in real scenario, this would come from NPhies)
            if (!string.IsNullOrEmpty(submittedRequest.ResponseId))
        {
                return await GetEligibilityResponseAsync(submittedRequest.ResponseId);
            }

            return null;
}
        catch (Exception ex)
        {
 throw new InvalidOperationException("Failed to check coverage eligibility", ex);
     }
    }

    /// <summary>
    /// Generate unique request ID
    /// </summary>
    private static string GenerateRequestId()
    {
        return $"REQ-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";
    }
}
