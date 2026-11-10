using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Application.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// API Controller for Claim management operations
/// Handles CRUD operations for insurance claims
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClaimsController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly ILogger<ClaimsController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public ClaimsController(IClaimService claimService, ILogger<ClaimsController> logger)
    {
        _claimService = claimService ?? throw new ArgumentNullException(nameof(claimService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new claim
    /// </summary>
    /// <param name="dto">Claim creation data transfer object</param>
    /// <returns>Created claim with generated ID</returns>
    /// <response code="201">Claim created successfully</response>
    /// <response code="400">Invalid claim data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateClaim([FromBody] CreateClaimDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateClaim: {0}", string.Join(",", ModelState.Values.SelectMany(v => v.Errors)));
                return BadRequest(new { message = "Invalid claim data", errors = ModelState.Values.SelectMany(v => v.Errors) });
            }

            if (string.IsNullOrWhiteSpace(dto.ClaimNumber))
                return BadRequest(new { message = "Claim number is required" });

            if (string.IsNullOrWhiteSpace(dto.PatientId))
                return BadRequest(new { message = "Patient ID is required" });

            _logger.LogInformation("Creating new claim: {0}", dto.ClaimNumber);
            var result = await _claimService.CreateClaimAsync(dto);

            return CreatedAtAction(nameof(GetClaim), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Operation error while creating claim");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating claim");
            return StatusCode(StatusCodes.Status500InternalServerError,
              new { message = "An error occurred while creating the claim" });
        }
    }

    /// <summary>
    /// Get claim by ID
    /// </summary>
    /// <param name="id">Claim ID</param>
    /// <returns>Claim details</returns>
    /// <response code="200">Claim found and returned</response>
    /// <response code="404">Claim not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClaim(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Claim ID is required" });

            _logger.LogInformation("Retrieving claim: {0}", id);
            var result = await _claimService.GetClaimAsync(id);

            if (result == null)
            {
                _logger.LogWarning("Claim not found: {0}", id);
                return NotFound(new { message = $"Claim with ID {id} not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving claim: {0}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
               new { message = "An error occurred while retrieving the claim" });
        }
    }

    /// <summary>
    /// Get claim with all related details
    /// </summary>
    /// <param name="id">Claim ID</param>
    /// <returns>Claim with items, diagnoses, and responses</returns>
    /// <response code="200">Claim with details found</response>
    /// <response code="404">Claim not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}/details")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClaimWithDetails(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Claim ID is required" });

            _logger.LogInformation("Retrieving claim with details: {0}", id);
            var result = await _claimService.GetClaimWithDetailsAsync(id);

            if (result == null)
            {
                _logger.LogWarning("Claim not found: {0}", id);
                return NotFound(new { message = $"Claim with ID {id} not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving claim details: {0}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
              new { message = "An error occurred while retrieving the claim details" });
        }
    }

    [HttpGet("{id}/workflow-details")]
    [HasPermission(Permissions.ClaimsRead)]
    [ProducesResponseType(typeof(ClaimWorkflowDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClaimWorkflowDetails(string id)
    {
        var result = await _claimService.GetClaimWorkflowDetailsAsync(id);
        return result is null
            ? NotFound(new { message = $"Claim with ID {id} not found" })
            : Ok(result);
    }

    /// <summary>
    /// Get all claims for a patient
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <returns>List of patient's claims</returns>
    /// <response code="200">Claims found and returned</response>
    /// <response code="400">Invalid patient ID</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(IEnumerable<ClaimDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPatientClaims(string patientId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(patientId))
                return BadRequest(new { message = "Patient ID is required" });

            _logger.LogInformation("Retrieving claims for patient: {0}", patientId);
            var result = await _claimService.GetPatientClaimsAsync(patientId);

            return Ok(new
            {
                patientId = patientId,
                count = result.Count(),
                claims = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving patient claims: {0}", patientId);
            return StatusCode(StatusCodes.Status500InternalServerError,
         new { message = "An error occurred while retrieving patient claims" });
        }
    }

    /// <summary>
    /// Get claims by status
    /// </summary>
    /// <param name="status">Claim status (active, submitted, processed, denied, cancelled)</param>
    /// <returns>List of claims with specified status</returns>
    /// <response code="200">Claims found and returned</response>
    /// <response code="400">Invalid status</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<ClaimDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClaimsByStatus(string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new { message = "Status is required" });

            var validStatuses = new[] { "active", "submitted", "processed", "denied", "cancelled" };
            if (!validStatuses.Contains(status.ToLower()))
                return BadRequest(new { message = $"Invalid status. Valid values: {string.Join(", ", validStatuses)}" });

            _logger.LogInformation("Retrieving claims with status: {0}", status);
            var result = await _claimService.GetClaimsByStatusAsync(status);

            return Ok(new
            {
                status = status,
                count = result.Count(),
                claims = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving claims by status: {0}", status);
            return StatusCode(StatusCodes.Status500InternalServerError,
           new { message = "An error occurred while retrieving claims by status" });
        }
    }

    /// <summary>
    /// Update claim
    /// </summary>
    /// <param name="id">Claim ID to update</param>
    /// <param name="dto">Claim update data</param>
    /// <returns>Updated claim</returns>
    /// <response code="200">Claim updated successfully</response>
    /// <response code="400">Invalid data</response>
    /// <response code="404">Claim not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateClaim(string id, [FromBody] UpdateClaimDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Claim ID is required" });

            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid claim data", errors = ModelState.Values.SelectMany(v => v.Errors) });

            _logger.LogInformation("Updating claim: {0}", id);
            var result = await _claimService.UpdateClaimAsync(id, dto);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Claim not found for update: {0}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (NPhies_FHIR_Integration.Application.Rcm.RcmWorkflowException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Operation error while updating claim: {0}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating claim: {0}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                   new { message = "An error occurred while updating the claim" });
        }
    }

    // ===================== DUAL-APPROVAL REVIEW WORKFLOW =====================

    /// <summary>
    /// Get the dual-approval review status of a claim.
    /// </summary>
    /// <param name="id">Claim ID</param>
    [HttpGet("{id}/review-status")]
    [Authorize]
    [ProducesResponseType(typeof(ClaimReviewStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReviewStatus(string id)
    {
        try
        {
 var status = await _claimService.GetReviewStatusAsync(id);
  if (status == null)
       return NotFound(new { message = $"Claim with ID {id} not found" });

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving review status for claim {0}", id);
    return StatusCode(StatusCodes.Status500InternalServerError,
   new { message = "An error occurred while retrieving the review status" });
        }
    }

    /// <summary>
    /// Get claims awaiting review for a stage ("technical" or "medical").
 /// </summary>
    [HttpGet("awaiting-review/{stage}")]
    [HasPermission(Permissions.ClaimsRead)]
    [ProducesResponseType(typeof(IEnumerable<ClaimDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetClaimsAwaitingReview(string stage)
    {
        try
        {
            var claims = await _claimService.GetClaimsAwaitingReviewAsync(stage);
            return Ok(new { stage, count = claims.Count(), claims });
    }
   catch (ArgumentException ex)
        {
    return BadRequest(new { message = ex.Message });
 }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving claims awaiting {0} review", stage);
  return StatusCode(StatusCodes.Status500InternalServerError,
     new { message = "An error occurred while retrieving claims awaiting review" });
     }
    }

    /// <summary>
    /// Get active claims approved by both reviewers and ready to submit.
    /// </summary>
    [HttpGet("ready-for-submission")]
    [HasPermission(Permissions.ClaimsRead)]
    [ProducesResponseType(typeof(IEnumerable<ClaimDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClaimsReadyForSubmission()
    {
        var claims = (await _claimService.GetClaimsReadyForSubmissionAsync()).ToList();
        return Ok(new { count = claims.Count, claims });
    }

    /// <summary>
    /// Submit a technical review decision (coding/billing correctness).
 /// Restricted to Admin and TechnicalReviewer roles.
 /// </summary>
    /// <param name="id">Claim ID</param>
    /// <param name="dto">Review decision (Approved/Rejected) with optional notes</param>
    [HttpPost("{id}/technical-review")]
    [HasPermission(Permissions.ClaimsTechnicalReview)]
    [ProducesResponseType(typeof(ClaimReviewStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TechnicalReview(string id, [FromBody] ClaimReviewDto dto)
    {
        try
        {
            dto.ReviewerId ??= User.FindFirst("sub")?.Value;
    _logger.LogInformation("Technical review '{0}' for claim {1}", dto.Decision, id);
  var status = await _claimService.TechnicalReviewAsync(id, dto);
        return Ok(status);
        }
        catch (KeyNotFoundException ex)
   {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
      {
        return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
      {
        _logger.LogError(ex, "Error during technical review of claim {0}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
       new { message = "An error occurred while recording the technical review" });
    }
    }

    /// <summary>
    /// Submit a medical review decision (clinical correctness / medical necessity).
    /// Restricted to Admin and MedicalReviewer roles.
    /// </summary>
    /// <param name="id">Claim ID</param>
    /// <param name="dto">Review decision (Approved/Rejected) with optional notes</param>
    [HttpPost("{id}/medical-review")]
    [HasPermission(Permissions.ClaimsMedicalReview)]
    [ProducesResponseType(typeof(ClaimReviewStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MedicalReview(string id, [FromBody] ClaimReviewDto dto)
    {
        try
        {
      dto.ReviewerId ??= User.FindFirst("sub")?.Value;
  _logger.LogInformation("Medical review '{0}' for claim {1}", dto.Decision, id);
 var status = await _claimService.MedicalReviewAsync(id, dto);
            return Ok(status);
        }
        catch (KeyNotFoundException ex)
  {
    return NotFound(new { message = ex.Message });
        }
     catch (ArgumentException ex)
        {
       return BadRequest(new { message = ex.Message });
   }
        catch (Exception ex)
 {
 _logger.LogError(ex, "Error during medical review of claim {0}", id);
  return StatusCode(StatusCodes.Status500InternalServerError,
       new { message = "An error occurred while recording the medical review" });
        }
    }

 /// <summary>
/// Submit a claim to NPHIES. Enforces the dual-approval gate:
    /// the claim must have both technical and medical reviews Approved.
    /// Restricted to Admin and ClaimsProcessor roles.
    /// </summary>
    /// <param name="id">Claim ID</param>
    [HttpPost("{id}/submit")]
    [HasPermission(Permissions.ClaimsSubmit)]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitClaim(string id)
    {
    try
 {
       var submittedBy = User.FindFirst("sub")?.Value;
            _logger.LogInformation("Submitting claim {0} to NPHIES by {1}", id, submittedBy);
   var result = await _claimService.SubmitClaimAsync(id, submittedBy);
  return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
        return NotFound(new { message = ex.Message });
   }
     catch (InvalidOperationException ex)
        {
      // Dual-approval gate not satisfied
    _logger.LogWarning("Claim {0} submission blocked: {1}", id, ex.Message);
    return BadRequest(new { message = ex.Message });
        }
      catch (Exception ex)
{
        _logger.LogError(ex, "Error submitting claim {0}", id);
    return StatusCode(StatusCodes.Status500InternalServerError,
        new { message = "An error occurred while submitting the claim" });
        }
    }
}
