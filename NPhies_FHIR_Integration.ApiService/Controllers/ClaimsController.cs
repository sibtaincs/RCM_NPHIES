using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Application.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Claim review gate: the technical and medical review decisions a claim needs before it is sent
/// (when Rcm:RequireReviewApprovalForClaims is set), and submission through the RCM workflow.
/// Creating and editing claims goes through api/rcm/claims (RcmClaimEditorController).
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

    // ===================== DUAL-APPROVAL REVIEW WORKFLOW =====================

    /// <summary>
    /// Get the dual-approval review status of a claim.
    /// </summary>
    /// <param name="id">Claim ID</param>
    [HttpGet("{id}/review-status")]
    [HasPermission(Permissions.ClaimsRead)]
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
