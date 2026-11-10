using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Application.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// API Controller for Claim Response management
/// Handles CRUD operations for claim responses
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClaimResponsesController : ControllerBase
{
    private readonly IClaimResponseService _responseService;
    private readonly ILogger<ClaimResponsesController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public ClaimResponsesController(IClaimResponseService responseService, ILogger<ClaimResponsesController> logger)
    {
        _responseService = responseService ?? throw new ArgumentNullException(nameof(responseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new claim response
    /// </summary>
    /// <param name="dto">Response creation data</param>
    /// <returns>Created response</returns>
    /// <response code="201">Response created successfully</response>
    /// <response code="400">Invalid response data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateResponse([FromBody] CreateClaimResponseDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid response data", errors = ModelState.Values.SelectMany(v => v.Errors) });

            if (string.IsNullOrWhiteSpace(dto.ClaimId))
                return BadRequest(new { message = "Claim ID is required" });

            _logger.LogInformation("Creating new response for claim: {0}", dto.ClaimId);
            var result = await _responseService.CreateClaimResponseAsync(dto);

            return CreatedAtAction(nameof(GetResponse), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Operation error while creating response");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating response");
            return StatusCode(StatusCodes.Status500InternalServerError,
              new { message = "An error occurred while creating the response" });
        }
    }

    /// <summary>
    /// Get claim response by ID
    /// </summary>
    /// <param name="id">Response ID</param>
    /// <returns>Response details</returns>
    /// <response code="200">Response found and returned</response>
    /// <response code="404">Response not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetResponse(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Response ID is required" });

            _logger.LogInformation("Retrieving response: {0}", id);
            var result = await _responseService.GetClaimResponseAsync(id);

            if (result == null)
            {
                _logger.LogWarning("Response not found: {0}", id);
                return NotFound(new { message = $"Response with ID {id} not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving response: {0}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                 new { message = "An error occurred while retrieving the response" });
        }
    }

    /// <summary>
    /// Get response for a specific claim
    /// </summary>
    /// <param name="claimId">Claim ID</param>
    /// <returns>Response for the claim</returns>
    /// <response code="200">Response found and returned</response>
    /// <response code="404">Response not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("claim/{claimId}")]
    [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetResponseByClaim(string claimId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(claimId))
                return BadRequest(new { message = "Claim ID is required" });

            _logger.LogInformation("Retrieving response for claim: {0}", claimId);
            var result = await _responseService.GetResponseByClaimIdAsync(claimId);

            if (result == null)
            {
                _logger.LogWarning("Response not found for claim: {0}", claimId);
                return NotFound(new { message = $"No response found for claim {claimId}" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving response for claim: {0}", claimId);
            return StatusCode(StatusCodes.Status500InternalServerError,
      new { message = "An error occurred while retrieving the response" });
        }
    }

    /// <summary>
    /// Get responses by status
    /// </summary>
    /// <param name="status">Response status (active, submitted, processed, denied)</param>
    /// <returns>List of responses with specified status</returns>
    /// <response code="200">Responses found and returned</response>
    /// <response code="400">Invalid status</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<ClaimResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetResponsesByStatus(string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new { message = "Status is required" });

            var validStatuses = new[] { "active", "submitted", "processed", "denied", "approved", "pending" };
            if (!validStatuses.Contains(status.ToLower()))
                return BadRequest(new { message = $"Invalid status. Valid values: {string.Join(", ", validStatuses)}" });

            _logger.LogInformation("Retrieving responses with status: {0}", status);
            var result = await _responseService.GetResponsesByStatusAsync(status);

            return Ok(new
            {
                status,
                count = result.Count(),
                responses = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving responses by status: {0}", status);
            return StatusCode(StatusCodes.Status500InternalServerError,
              new { message = "An error occurred while retrieving responses" });
        }
    }

    /// <summary>
    /// Get responses with pre-auth reference
    /// </summary>
    /// <param name="preAuthRef">Pre-authorization reference</param>
    /// <returns>List of responses with matching pre-auth</returns>
    /// <response code="200">Responses found and returned</response>
    /// <response code="400">Invalid reference</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("preauth/{preAuthRef}")]
    [ProducesResponseType(typeof(IEnumerable<ClaimResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetResponsesByPreAuthRef(string preAuthRef)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(preAuthRef))
                return BadRequest(new { message = "Pre-auth reference is required" });

            _logger.LogInformation("Retrieving responses with pre-auth: {0}", preAuthRef);
            var result = await _responseService.GetResponsesByPreAuthRefAsync(preAuthRef);

            return Ok(new
            {
                preAuthRef,
                count = result.Count(),
                responses = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving responses by pre-auth: {0}", preAuthRef);
            return StatusCode(StatusCodes.Status500InternalServerError,
              new { message = "An error occurred while retrieving responses" });
        }
    }

    /// <summary>
    /// Update claim response
    /// </summary>
    /// <param name="id">Response ID to update</param>
    /// <param name="dto">Response update data</param>
    /// <returns>Updated response</returns>
    /// <response code="200">Response updated successfully</response>
    /// <response code="400">Invalid data</response>
    /// <response code="404">Response not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateResponse(string id, [FromBody] UpdateClaimResponseDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Response ID is required" });

            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid response data", errors = ModelState.Values.SelectMany(v => v.Errors) });

            _logger.LogInformation("Updating response: {0}", id);
            var result = await _responseService.UpdateClaimResponseAsync(id, dto);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Response not found for update: {0}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Operation error while updating response: {0}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating response: {0}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
  new { message = "An error occurred while updating the response" });
        }
    }
}
