using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Domain.DTOs;
using NPhies_FHIR_Integration.Common.Models;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Controller for managing coverage eligibility requests and responses
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class EligibilityController : BaseController
{
    private readonly IEligibilityService _eligibilityService;
    private readonly ILogger<EligibilityController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public EligibilityController(IEligibilityService eligibilityService, ILogger<EligibilityController> logger)
    {
        _eligibilityService = eligibilityService ?? throw new ArgumentNullException(nameof(eligibilityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submit a new coverage eligibility request
    /// </summary>
    /// <param name="request">Eligibility request details</param>
    /// <returns>Created eligibility request with ID</returns>
    /// <response code="201">Eligibility request created successfully</response>
  /// <response code="400">Invalid request data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("requests")]
    [ProducesResponseType(typeof(ApiResponse<CoverageEligibilityRequestDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitEligibilityRequest([FromBody] CoverageEligibilityRequestDto request)
    {
 try
        {
   if (request == null)
      return BadRequest("Request body cannot be empty");

     if (string.IsNullOrEmpty(request.PatientId))
    return BadRequest("Patient ID is required");

  if (string.IsNullOrEmpty(request.CoverageId))
 return BadRequest("Coverage ID is required");

            _logger.LogInformation("Submitting eligibility request for patient {PatientId} and coverage {CoverageId}", 
   request.PatientId, request.CoverageId);

            var result = await _eligibilityService.SubmitEligibilityRequestAsync(request);

            _logger.LogInformation("Eligibility request submitted successfully with ID {RequestId}", result.Id);

            return Created($"/api/v1/eligibility/requests/{result.Id}", result);
        }
        catch (ArgumentException ex)
        {
      _logger.LogWarning("Validation error in eligibility request: {Message}", ex.Message);
            return BadRequest(ex.Message);
     }
        catch (Exception ex)
     {
            _logger.LogError(ex, "Error submitting eligibility request");
    return InternalServerError("Failed to submit eligibility request");
}
    }

    /// <summary>
    /// Get an eligibility request by ID
    /// </summary>
    /// <param name="id">Request ID</param>
    /// <returns>Eligibility request details</returns>
    /// <response code="200">Request found and returned</response>
    /// <response code="404">Request not found</response>
/// <response code="500">Internal server error</response>
    [HttpGet("requests/{id}")]
    [ProducesResponseType(typeof(ApiResponse<CoverageEligibilityRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEligibilityRequest(string id)
    {
        try
        {
    if (string.IsNullOrEmpty(id))
    return BadRequest("Request ID is required");

  _logger.LogInformation("Retrieving eligibility request {RequestId}", id);

       var request = await _eligibilityService.GetEligibilityRequestAsync(id);

            if (request == null)
  return NotFound($"Eligibility request with ID {id} not found");

            return Ok(request, "Eligibility request retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving eligibility request {RequestId}", id);
        return InternalServerError("Failed to retrieve eligibility request");
        }
    }

    /// <summary>
    /// Get all pending eligibility requests
    /// </summary>
    /// <returns>List of pending eligibility requests</returns>
    /// <response code="200">Pending requests returned</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("requests/pending")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CoverageEligibilityRequestDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPendingRequests()
    {
  try
        {
_logger.LogInformation("Retrieving pending eligibility requests");

       var requests = await _eligibilityService.GetPendingRequestsAsync();

  return Ok(requests, "Pending eligibility requests retrieved successfully");
      }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Error retrieving pending eligibility requests");
            return InternalServerError("Failed to retrieve pending eligibility requests");
        }
    }

    /// <summary>
    /// Check coverage eligibility for a patient
    /// </summary>
    /// <param name="request">Coverage eligibility check request</param>
    /// <returns>Eligibility response with coverage details</returns>
    /// <response code="200">Eligibility checked successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Patient or coverage not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("check")]
    [ProducesResponseType(typeof(ApiResponse<CoverageEligibilityResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckCoverageEligibility([FromBody] CheckCoverageEligibilityRequest request)
    {
        try
     {
      if (request == null)
        return BadRequest("Request body cannot be empty");

            if (string.IsNullOrEmpty(request.PatientId))
 return BadRequest("Patient ID is required");

        if (string.IsNullOrEmpty(request.CoverageId))
    return BadRequest("Coverage ID is required");

         _logger.LogInformation("Checking coverage eligibility for patient {PatientId} and coverage {CoverageId}", 
    request.PatientId, request.CoverageId);

            var result = await _eligibilityService.CheckCoverageEligibilityAsync(
        request.PatientId, 
        request.CoverageId, 
   request.ServiceType ?? "medical");

      if (result == null)
       return NotFound("Coverage not found or eligibility check failed");

            return Ok(result, "Coverage eligibility checked successfully");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Entity not found: {Message}", ex.Message);
        return NotFound(ex.Message);
 }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking coverage eligibility");
            return InternalServerError("Failed to check coverage eligibility");
        }
    }

    /// <summary>
    /// Get an eligibility response by ID
    /// </summary>
    /// <param name="id">Response ID</param>
    /// <returns>Eligibility response with benefits</returns>
    /// <response code="200">Response found and returned</response>
    /// <response code="404">Response not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("responses/{id}")]
    [ProducesResponseType(typeof(ApiResponse<CoverageEligibilityResponseDto>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEligibilityResponse(string id)
    {
      try
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Response ID is required");

         _logger.LogInformation("Retrieving eligibility response {ResponseId}", id);

            var response = await _eligibilityService.GetEligibilityResponseAsync(id);

            if (response == null)
  return NotFound($"Eligibility response with ID {id} not found");

            return Ok(response, "Eligibility response retrieved successfully");
        }
 catch (Exception ex)
        {
        _logger.LogError(ex, "Error retrieving eligibility response {ResponseId}", id);
    return InternalServerError("Failed to retrieve eligibility response");
        }
    }

    /// <summary>
    /// Process an eligibility response from FHIR JSON
    /// </summary>
    /// <param name="request">FHIR response processing request</param>
    /// <returns>Processed eligibility response</returns>
    /// <response code="200">Response processed successfully</response>
/// <response code="400">Invalid FHIR JSON</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("responses/process")]
    [ProducesResponseType(typeof(ApiResponse<CoverageEligibilityResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessEligibilityResponse([FromBody] ProcessFhirResponseRequest request)
    {
        try
        {
  if (request == null || string.IsNullOrEmpty(request.FhirJson))
       return BadRequest("FHIR JSON content is required");

         _logger.LogInformation("Processing eligibility response from FHIR JSON");

          var result = await _eligibilityService.ProcessEligibilityResponseAsync(request.FhirJson);

            return Ok(result, "Eligibility response processed successfully");
        }
  catch (InvalidOperationException ex)
        {
         _logger.LogWarning("Invalid FHIR JSON: {Message}", ex.Message);
            return BadRequest($"Invalid FHIR JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
        _logger.LogError(ex, "Error processing eligibility response");
   return InternalServerError("Failed to process eligibility response");
        }
    }

    /// <summary>
    /// Get eligibility request with response
    /// </summary>
    /// <param name="requestId">Request ID</param>
    /// <returns>Eligibility request with associated response</returns>
    /// <response code="200">Request with response returned</response>
    /// <response code="404">Request not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("requests/{requestId}/response")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRequestWithResponse(string requestId)
    {
        try
     {
   if (string.IsNullOrEmpty(requestId))
 return BadRequest("Request ID is required");

            _logger.LogInformation("Retrieving eligibility request {RequestId} with response", requestId);

      var request = await _eligibilityService.GetEligibilityRequestAsync(requestId);

     if (request == null)
   return NotFound($"Eligibility request with ID {requestId} not found");

   var response = new
       {
 request.Id,
         request.RequestId,
 request.Status,
       request.PatientId,
      request.CoverageId,
                request.ServiceDate,
     request.ServiceType,
              request.RequestCreatedAt,
              request.SubmittedAt,
 request.RespondedAt,
    request.EligibilityStatus,
           Response = request.ResponseId != null ? new { message = "Response available" } : null
            };

         return Ok(response, "Eligibility request with response retrieved successfully");
      }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Error retrieving eligibility request {RequestId} with response", requestId);
        return InternalServerError("Failed to retrieve eligibility request with response");
        }
 }
}

/// <summary>
/// Request model for checking coverage eligibility
/// </summary>
public class CheckCoverageEligibilityRequest
{
    /// <summary>
    /// Patient ID
    /// </summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>
    /// Coverage ID
    /// </summary>
    public string CoverageId { get; set; } = string.Empty;

    /// <summary>
    /// Service type (optional, defaults to "medical")
    /// </summary>
 public string? ServiceType { get; set; }
}

/// <summary>
/// Request model for processing FHIR response
/// </summary>
public class ProcessFhirResponseRequest
{
    /// <summary>
    /// FHIR Bundle JSON content
    /// </summary>
public string FhirJson { get; set; } = string.Empty;
}
