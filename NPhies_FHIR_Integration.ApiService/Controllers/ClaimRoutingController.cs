using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.Services.RBAC;
using NPhies_FHIR_Integration.Domain.DTOs;

namespace NPhies_FHIR_Integration.ApiService.Controllers
{
    /// <summary>
    /// API controller for claim routing and assignment
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class ClaimRoutingController : ControllerBase
    {
        private readonly IClaimRoutingService _claimRoutingService;
        private readonly ILogger<ClaimRoutingController> _logger;

        public ClaimRoutingController(IClaimRoutingService claimRoutingService, ILogger<ClaimRoutingController> logger)
        {
            _claimRoutingService = claimRoutingService;
            _logger = logger;
        }

        /// <summary>
        /// Assign claim for review
        /// </summary>
        [HttpPost("assign")]
        [Authorize(Roles = "TECHNICAL_REVIEW_SUPERVISOR,TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_SUPERVISOR,MEDICAL_REVIEW_MANAGER")]
        [ProducesResponseType(typeof(ClaimAssignmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClaimAssignmentDto>> AssignClaim([FromBody] AssignClaimDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _logger.LogInformation("Assigning claim {ClaimId} for {ReviewType} review with complexity {Complexity}",
            dto.ClaimId, dto.ReviewType, dto.ComplexityScore);
                var assignment = await _claimRoutingService.AssignClaimAsync(dto);
                return CreatedAtAction(nameof(GetClaimAssignment), new { id = assignment.Id }, assignment);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to assign claim");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning claim");
                return StatusCode(StatusCodes.Status500InternalServerError,
            new { message = "Error assigning claim", error = ex.Message });
            }
        }

        /// <summary>
        /// Get claim assignment
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClaimAssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClaimAssignmentDto>> GetClaimAssignment(int id)
        {
            try
            {
                _logger.LogInformation("Getting claim assignment {AssignmentId}", id);
                var assignment = await _claimRoutingService.GetClaimAssignmentAsync(id);
                if (assignment == null)
                    return NotFound(new { message = $"Claim assignment {id} not found" });
                return Ok(assignment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting claim assignment");
                return StatusCode(StatusCodes.Status500InternalServerError,
              new { message = "Error retrieving assignment", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user's queue for review
        /// </summary>
        [HttpGet("queue/user/{userId}")]
        [ProducesResponseType(typeof(List<ClaimAssignmentDto>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<ActionResult<List<ClaimAssignmentDto>>> GetUserQueue(int userId, [FromQuery] string reviewType = null)
        {
            try
            {
                _logger.LogInformation("Getting queue for user {UserId}", userId);
                var queue = await _claimRoutingService.GetUserQueueAsync(userId, reviewType);
                return Ok(queue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user queue");
                return StatusCode(StatusCodes.Status500InternalServerError,
                      new { message = "Error retrieving queue", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all pending claims
        /// </summary>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(List<ClaimAssignmentDto>), StatusCodes.Status200OK)]
        [Authorize(Roles = "TECHNICAL_REVIEW_SUPERVISOR,TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_SUPERVISOR,MEDICAL_REVIEW_MANAGER")]
        public async Task<ActionResult<List<ClaimAssignmentDto>>> GetPendingClaims([FromQuery] string reviewType = null)
        {
            try
            {
                _logger.LogInformation("Getting pending claims");
                var pending = await _claimRoutingService.GetPendingClaimsAsync(reviewType);
                return Ok(pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending claims");
                return StatusCode(StatusCodes.Status500InternalServerError,
                   new { message = "Error retrieving pending claims", error = ex.Message });
            }
        }

        /// <summary>
        /// Submit review result
        /// </summary>
        [HttpPost("submit-review")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewDto dto, [FromHeader(Name = "X-User-Id")] int reviewerId)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _logger.LogInformation("Submitting review for claim assignment {AssignmentId}", dto.ClaimAssignmentId);
                var result = await _claimRoutingService.SubmitReviewAsync(dto.ClaimAssignmentId, dto, reviewerId);
                if (!result)
                    return BadRequest(new { message = "Failed to submit review" });
                return Ok(new { message = "Review submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting review");
                return StatusCode(StatusCodes.Status500InternalServerError,
                   new { message = "Error submitting review", error = ex.Message });
            }
        }

        /// <summary>
        /// Escalate claim
        /// </summary>
        [HttpPost("escalate")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EscalateClaim([FromBody] dynamic escalateDto)
        {
            try
            {
                // Parse escalation details
                int claimAssignmentId = escalateDto.claimAssignmentId;
                int escalatedToUserId = escalateDto.escalatedToUserId;
                string reason = escalateDto.reason;

                _logger.LogInformation("Escalating claim assignment {AssignmentId} to user {UserId}", claimAssignmentId, escalatedToUserId);
                var result = await _claimRoutingService.EscalateClaimAsync(claimAssignmentId, escalatedToUserId, reason);
                if (!result)
                    return BadRequest(new { message = "Failed to escalate claim" });
                return Ok(new { message = "Claim escalated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error escalating claim");
                return StatusCode(StatusCodes.Status500InternalServerError,
                  new { message = "Error escalating claim", error = ex.Message });
            }
        }

        /// <summary>
        /// Get QA queue for supervisor
        /// </summary>
        [HttpGet("qa-queue/{supervisorId}")]
        [ProducesResponseType(typeof(List<ClaimAssignmentDto>), StatusCodes.Status200OK)]
        [Authorize(Roles = "TECHNICAL_REVIEW_SUPERVISOR,TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_SUPERVISOR,MEDICAL_REVIEW_MANAGER")]
        public async Task<ActionResult<List<ClaimAssignmentDto>>> GetQAQueue(int supervisorId)
        {
            try
            {
                _logger.LogInformation("Getting QA queue for supervisor {SupervisorId}", supervisorId);
                var qaQueue = await _claimRoutingService.GetQAQueueAsync(supervisorId);
                return Ok(qaQueue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting QA queue");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving QA queue", error = ex.Message });
            }
        }

        /// <summary>
        /// Submit QA review
        /// </summary>
        [HttpPost("submit-qa-review")]
        [Authorize(Roles = "TECHNICAL_REVIEW_SUPERVISOR,TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_SUPERVISOR,MEDICAL_REVIEW_MANAGER")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitQAReview([FromBody] SubmitQAReviewDto dto, [FromHeader(Name = "X-User-Id")] int reviewerId)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _logger.LogInformation("Submitting QA review for claim assignment {AssignmentId}", dto.ClaimAssignmentId);
                var result = await _claimRoutingService.SubmitQAReviewAsync(dto.ClaimAssignmentId, dto, reviewerId);
                if (!result)
                    return BadRequest(new { message = "Failed to submit QA review" });
                return Ok(new { message = "QA review submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting QA review");
                return StatusCode(StatusCodes.Status500InternalServerError,
                   new { message = "Error submitting QA review", error = ex.Message });
            }
        }

        /// <summary>
        /// Get reviewer metrics
        /// </summary>
        [HttpGet("metrics/reviewer/{reviewerId}")]
        [ProducesResponseType(typeof(List<ClaimAssignmentDto>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<ActionResult<List<ClaimAssignmentDto>>> GetReviewerMetrics(int reviewerId, [FromQuery] int days = 7)
        {
            try
            {
                _logger.LogInformation("Getting metrics for reviewer {ReviewerId} for last {Days} days", reviewerId, days);
                var metrics = await _claimRoutingService.GetReviewerMetricsAsync(reviewerId, days);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviewer metrics");
                return StatusCode(StatusCodes.Status500InternalServerError,
            new { message = "Error retrieving metrics", error = ex.Message });
            }
        }
    }
}
