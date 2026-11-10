using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Prior Authorization Controller
/// Handles NPHIES pre-authorization requests and responses
/// </summary>
[ApiController]
[Route("api/preauth")]
[Authorize]
public class PreAuthorizationController : BaseController
{
    private readonly IClaimService _claimService;
    private readonly IClaimResponseService _responseService;
 private readonly ILogger<PreAuthorizationController> _logger;

    public PreAuthorizationController(
  IClaimService claimService,
  IClaimResponseService responseService,
      ILogger<PreAuthorizationController> logger)
    {
        _claimService = claimService ?? throw new ArgumentNullException(nameof(claimService));
     _responseService = responseService ?? throw new ArgumentNullException(nameof(responseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submit a prior authorization request
    /// POST /api/preauth/request
    /// </summary>
    /// <param name="preAuthRequest">Pre-authorization claim request</param>
    /// <returns>Created pre-authorization with ID</returns>
    [HttpPost("request")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitPreAuthRequest([FromBody] CreateClaimDto preAuthRequest)
 {
    try
        {
 _logger.LogInformation("Submitting pre-authorization request for patient {PatientId}", 
  preAuthRequest.PatientId);

            // Ensure this is a pre-authorization claim
            preAuthRequest.Use = "preauthorization";

       // Validate required fields
          if (string.IsNullOrEmpty(preAuthRequest.PatientId))
       return BadRequest("PatientId is required");

 if (string.IsNullOrEmpty(preAuthRequest.ProviderId))
   return BadRequest("ProviderId is required");

   if (string.IsNullOrEmpty(preAuthRequest.InsurerId))
 return BadRequest("InsurerId is required");

            // Create the pre-authorization request
          var result = await _claimService.CreateClaimAsync(preAuthRequest);

 _logger.LogInformation("Pre-authorization request created with ID: {ClaimId}", result.Id);

            return CreatedAtAction(
 nameof(GetPreAuthStatus),
    new { id = result.Id },
       result);
      }
    catch (Exception ex)
        {
        _logger.LogError(ex, "Error submitting pre-authorization request");
            return StatusCode(500, new { error = "An error occurred while submitting the pre-authorization request" });
        }
    }

    /// <summary>
    /// Get prior authorization status
    /// GET /api/preauth/{id}
    /// </summary>
    /// <param name="id">Pre-authorization claim ID</param>
    /// <returns>Pre-authorization details with response if available</returns>
    [HttpGet("{id}")]
[ProducesResponseType(typeof(PreAuthStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreAuthStatus(string id)
    {
   try
        {
   _logger.LogInformation("Retrieving pre-authorization status for ID: {Id}", id);

var claim = await _claimService.GetClaimByIdAsync(id);
            
       if (claim == null)
            {
           _logger.LogWarning("Pre-authorization not found: {Id}", id);
     return NotFound(new { error = "Pre-authorization not found" });
      }

            if (claim.Use != "preauthorization")
    {
             _logger.LogWarning("Claim {Id} is not a pre-authorization", id);
      return BadRequest(new { error = "The specified claim is not a pre-authorization" });
     }

            // Get the response if available
       var response = await _responseService.GetResponseForClaimAsync(id);

          var statusResponse = new PreAuthStatusResponse
          {
   PreAuthId = claim.Id.ToString(),
    ClaimNumber = claim.ClaimNumber,
     Status = claim.Status,
 PatientId = claim.PatientId,
  ProviderId = claim.ProviderId,
     InsurerId = claim.InsurerId,
        RequestedDate = claim.CreatedAt,
    TotalAmount = claim.Total,
     ResponseReceived = response != null,
    ResponseStatus = response?.Outcome,
        AuthorizationNumber = response?.PreAuthRef,
     ResponseDate = response?.CreatedAt,
       Items = new List<PreAuthItemSummary>() // Items would need to be loaded separately
      };

      _logger.LogInformation("Retrieved pre-authorization status: {Status}", statusResponse.Status);

return Ok(statusResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pre-authorization status for ID: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the pre-authorization status" });
        }
    }

    /// <summary>
    /// Extend prior authorization
    /// POST /api/preauth/{id}/extend
    /// </summary>
    /// <param name="id">Original pre-authorization ID</param>
    /// <param name="request">Extension request details</param>
 /// <returns>New pre-authorization extension</returns>
    [HttpPost("{id}/extend")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status201Created)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
 public async Task<IActionResult> ExtendPreAuth(string id, [FromBody] PreAuthExtensionRequest request)
    {
        try
        {
_logger.LogInformation("Extending pre-authorization {Id}", id);

         var originalClaim = await _claimService.GetClaimByIdAsync(id);
        
    if (originalClaim == null)
          {
   _logger.LogWarning("Original pre-authorization not found: {Id}", id);
return NotFound(new { error = "Original pre-authorization not found" });
     }

     if (originalClaim.Use != "preauthorization")
          {
      return BadRequest(new { error = "The specified claim is not a pre-authorization" });
            }

      // Create extension claim with reference to original
        var extensionClaim = new CreateClaimDto
            {
          Use = "preauthorization",
ClaimType = originalClaim.ClaimType,
      ClaimTypeSystem = originalClaim.ClaimTypeSystem,
       PatientId = originalClaim.PatientId,
           ProviderId = originalClaim.ProviderId,
      InsurerId = originalClaim.InsurerId,
            CoverageId = originalClaim.CoverageId,
        Priority = originalClaim.Priority
         };

          var result = await _claimService.CreateClaimAsync(extensionClaim);

            _logger.LogInformation("Pre-authorization extension created: {NewId} for original {OriginalId}", 
 result.Id, id);

      return CreatedAtAction(
         nameof(GetPreAuthStatus),
    new { id = result.Id },
         result);
        }
      catch (Exception ex)
        {
          _logger.LogError(ex, "Error extending pre-authorization {Id}", id);
   return StatusCode(500, new { error = "An error occurred while extending the pre-authorization" });
        }
    }

    /// <summary>
    /// Cancel prior authorization
    /// POST /api/preauth/{id}/cancel
    /// </summary>
    /// <param name="id">Pre-authorization ID</param>
    /// <param name="request">Cancellation request with reason</param>
 /// <returns>Result of cancellation</returns>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPreAuth(string id, [FromBody] PreAuthCancellationRequest request)
    {
        try
        {
            _logger.LogInformation("Cancelling pre-authorization {Id}", id);

         var claim = await _claimService.GetClaimByIdAsync(id);
            
            if (claim == null)
        {
     return NotFound(new { error = "Pre-authorization not found" });
            }

        if (claim.Use != "preauthorization")
            {
    return BadRequest(new { error = "The specified claim is not a pre-authorization" });
 }

    // Update claim status to cancelled
  var updateDto = new UpdateClaimDto { Status = "cancelled" };
       await _claimService.UpdateClaimAsync(claim.Id.ToString(), updateDto);

      _logger.LogInformation("Pre-authorization {Id} cancelled: {Reason}", id, request.CancellationReason);

    return Ok(new 
         { 
         message = "Pre-authorization cancelled successfully",
         preAuthId = id,
        status = "cancelled",
                cancelledAt = DateTime.UtcNow,
    reason = request.CancellationReason
        });
        }
 catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling pre-authorization {Id}", id);
            return StatusCode(500, new { error = "An error occurred while cancelling the pre-authorization" });
        }
    }

    /// <summary>
    /// Get all pre-authorizations for a patient
    /// GET /api/preauth/patient/{patientId}
    /// </summary>
    /// <param name="patientId">Patient ID</param>
  /// <returns>List of pre-authorizations</returns>
  [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(List<PreAuthSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientPreAuths(string patientId)
    {
    try
        {
            _logger.LogInformation("Retrieving pre-authorizations for patient {PatientId}", patientId);

    // This would need to be implemented in ClaimService
// For now, return a basic implementation
        var claims = await _claimService.GetClaimsByPatientIdAsync(patientId);
            
    var preAuths = claims
        .Where(c => c.Use == "preauthorization")
  .Select(c => new PreAuthSummary
        {
      PreAuthId = c.Id.ToString(),
     ClaimNumber = c.ClaimNumber,
       Status = c.Status,
      RequestedDate = c.CreatedAt,
TotalAmount = c.Total,
      ProviderId = c.ProviderId,
     InsurerId = c.InsurerId
    })
      .OrderByDescending(p => p.RequestedDate)
      .ToList();

 return Ok(preAuths);
      }
        catch (Exception ex)
      {
  _logger.LogError(ex, "Error retrieving pre-authorizations for patient {PatientId}", patientId);
   return StatusCode(500, new { error = "An error occurred while retrieving pre-authorizations" });
        }
}
}

#region DTOs

/// <summary>
/// Pre-authorization status response
/// </summary>
public class PreAuthStatusResponse
{
    public string PreAuthId { get; set; }
    public string ClaimNumber { get; set; }
    public string Status { get; set; }
public string PatientId { get; set; }
    public string ProviderId { get; set; }
    public string InsurerId { get; set; }
    public DateTime RequestedDate { get; set; }
 public decimal? TotalAmount { get; set; }
    public bool ResponseReceived { get; set; }
    public string? ResponseStatus { get; set; }
    public string? AuthorizationNumber { get; set; }
    public DateTime? ResponseDate { get; set; }
    public List<PreAuthItemSummary> Items { get; set; } = new();
}

/// <summary>
/// Pre-authorization item summary
/// </summary>
public class PreAuthItemSummary
{
    public int Sequence { get; set; }
    public string ServiceCode { get; set; }
    public string ServiceDescription { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? NetAmount { get; set; }
}

/// <summary>
/// Pre-authorization extension request
/// </summary>
public class PreAuthExtensionRequest
{
    public DateTime? NewExpirationDate { get; set; }
    public string ExtensionReason { get; set; }
}

/// <summary>
/// Pre-authorization cancellation request
/// </summary>
public class PreAuthCancellationRequest
{
    public string CancellationReason { get; set; }
}

/// <summary>
/// Pre-authorization summary
/// </summary>
public class PreAuthSummary
{
    public string PreAuthId { get; set; }
    public string ClaimNumber { get; set; }
    public string Status { get; set; }
    public DateTime RequestedDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string ProviderId { get; set; }
    public string InsurerId { get; set; }
}

#endregion
