using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NPhies_FHIR_Integration.Application.Services.RCM;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Appeal Management API Controller
/// Handles all appeal workflow operations
/// NPHIES Compliance: Appeal Request/Response management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AppealController : ControllerBase
{
    private readonly IAppealService _appealService;
    private readonly ILogger<AppealController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
  public AppealController(
   IAppealService appealService,
        ILogger<AppealController> logger)
    {
_appealService = appealService ?? throw new ArgumentNullException(nameof(appealService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ========== APPEAL CREATION ==========

    /// <summary>
    /// Create a new appeal for a denied claim
  /// POST /api/appeal/create
    /// </summary>
    /// <param name="request">Appeal creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Appeal creation result with appeal ID and deadline</returns>
    /// <response code="201">Appeal created successfully</response>
    /// <response code="400">Invalid request or validation failed</response>
    /// <response code="404">Error code not found or not appealable</response>
    /// <response code="500">Server error</response>
    [HttpPost("create")]
    [ProducesResponseType(typeof(CreateAppealResult), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<CreateAppealResult>> CreateAppeal(
        [FromBody] CreateAppealRequest request,
        CancellationToken cancellationToken = default)
    {
_logger.LogInformation("Creating appeal for claim {ClaimId}", request.ClaimId);

    if (!ModelState.IsValid)
{
            return BadRequest(new ErrorResponse
    {
    Message = "Invalid request",
        Errors = ModelState
      });
      }

        try
        {
   var result = await _appealService.CreateAppealAsync(request, cancellationToken);

 if (!result.IsSuccess)
     {
            return BadRequest(new ErrorResponse
     {
               Message = result.ErrorMessage
      });
        }

    return CreatedAtAction(nameof(GetAppeal), new { appealId = result.AppealId }, result);
        }
        catch (Exception ex)
      {
      _logger.LogError(ex, "Error creating appeal");
       return StatusCode(500, new ErrorResponse
         {
         Message = "An error occurred while creating the appeal",
      Details = ex.Message
      });
        }
    }

    // ========== APPEAL RETRIEVAL ==========

 /// <summary>
    /// Get appeal by ID
    /// GET /api/appeal/{appealId}
    /// </summary>
    [HttpGet("{appealId}")]
    [ProducesResponseType(typeof(AppealDto), 200)]
  [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<AppealDto>> GetAppeal(
      string appealId,
        CancellationToken cancellationToken = default)
{
        _logger.LogInformation("Retrieving appeal {AppealId}", appealId);

        try
        {
            var appeal = await _appealService.GetAppealAsync(appealId, cancellationToken);

    if (appeal == null)
            {
  return NotFound(new ErrorResponse
         {
           Message = $"Appeal {appealId} not found"
     });
          }

    return Ok(appeal);
        }
     catch (Exception ex)
   {
            _logger.LogError(ex, "Error retrieving appeal {AppealId}", appealId);
     return StatusCode(500, new ErrorResponse
     {
   Message = "An error occurred while retrieving the appeal",
  Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get all appeals for a claim
    /// GET /api/appeal/claim/{claimId}
    /// </summary>
    [HttpGet("claim/{claimId}")]
    [ProducesResponseType(typeof(List<AppealDto>), 200)]
    public async Task<ActionResult<List<AppealDto>>> GetClaimAppeals(
    string claimId,
 CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving appeals for claim {ClaimId}", claimId);

     try
    {
            var appeals = await _appealService.GetClaimAppealsAsync(claimId, cancellationToken);
            return Ok(appeals);
        }
  catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appeals for claim {ClaimId}", claimId);
            return StatusCode(500, new ErrorResponse
      {
        Message = "An error occurred while retrieving appeals",
       Details = ex.Message
 });
        }
    }

    /// <summary>
    /// Get all appeals for a patient
    /// GET /api/appeal/patient/{patientId}
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(List<AppealDto>), 200)]
    public async Task<ActionResult<List<AppealDto>>> GetPatientAppeals(
   string patientId,
     CancellationToken cancellationToken = default)
    {
   _logger.LogInformation("Retrieving appeals for patient {PatientId}", patientId);

  try
   {
            var appeals = await _appealService.GetPatientAppealsAsync(patientId, cancellationToken);
            return Ok(appeals);
        }
        catch (Exception ex)
     {
     _logger.LogError(ex, "Error retrieving appeals for patient {PatientId}", patientId);
   return StatusCode(500, new ErrorResponse
      {
 Message = "An error occurred while retrieving appeals",
                Details = ex.Message
     });
        }
    }

    // ========== APPEAL SUBMISSION ==========

    /// <summary>
    /// Submit an appeal (finalize before deadline)
    /// POST /api/appeal/{appealId}/submit
    /// </summary>
  [HttpPost("{appealId}/submit")]
    [ProducesResponseType(typeof(SubmitAppealResult), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<SubmitAppealResult>> SubmitAppeal(
        string appealId,
        CancellationToken cancellationToken = default)
    {
      _logger.LogInformation("Submitting appeal {AppealId}", appealId);

    try
 {
     var result = await _appealService.SubmitAppealAsync(appealId, cancellationToken);

            if (!result.IsSuccess)
            {
  return BadRequest(new ErrorResponse
    {
             Message = result.Message
        });
            }

            return Ok(result);
     }
        catch (Exception ex)
        {
     _logger.LogError(ex, "Error submitting appeal {AppealId}", appealId);
 return StatusCode(500, new ErrorResponse
         {
    Message = "An error occurred while submitting the appeal",
    Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Update appeal with additional information
    /// PATCH /api/appeal/{appealId}
    /// </summary>
    [HttpPatch("{appealId}")]
    [ProducesResponseType(typeof(UpdateAppealResult), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<UpdateAppealResult>> UpdateAppeal(
        string appealId,
        [FromBody] UpdateAppealRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating appeal {AppealId}", appealId);

     try
      {
            request.AppealId = appealId;
            var result = await _appealService.UpdateAppealAsync(request, cancellationToken);

            if (!result.IsSuccess)
            {
    return BadRequest(new ErrorResponse
           {
       Message = result.Message
          });
            }

    return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appeal {AppealId}", appealId);
        return StatusCode(500, new ErrorResponse
    {
 Message = "An error occurred while updating the appeal",
     Details = ex.Message
 });
        }
 }

    // ========== APPEAL WITHDRAWAL ==========

    /// <summary>
    /// Withdraw an appeal
    /// POST /api/appeal/{appealId}/withdraw
    /// </summary>
    [HttpPost("{appealId}/withdraw")]
    [ProducesResponseType(typeof(WithdrawAppealResult), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<WithdrawAppealResult>> WithdrawAppeal(
   string appealId,
        [FromBody] WithdrawRequest request,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Withdrawing appeal {AppealId}", appealId);

        try
        {
         var result = await _appealService.WithdrawAppealAsync(
       appealId,
   request.Reason,
       cancellationToken);

    if (!result.IsSuccess)
      {
     return BadRequest(new ErrorResponse
        {
           Message = result.Message
     });
     }

      return Ok(result);
        }
        catch (Exception ex)
        {
        _logger.LogError(ex, "Error withdrawing appeal {AppealId}", appealId);
            return StatusCode(500, new ErrorResponse
    {
           Message = "An error occurred while withdrawing the appeal",
         Details = ex.Message
      });
     }
    }

    // ========== APPEAL ESCALATION ==========

    /// <summary>
    /// Escalate appeal to next level
    /// POST /api/appeal/{appealId}/escalate
    /// </summary>
    [HttpPost("{appealId}/escalate")]
    [ProducesResponseType(typeof(EscalateAppealResult), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<EscalateAppealResult>> EscalateAppeal(
        string appealId,
        [FromBody] EscalateRequest request,
   CancellationToken cancellationToken = default)
{
      _logger.LogInformation("Escalating appeal {AppealId}", appealId);

        try
    {
            var result = await _appealService.EscalateAppealAsync(
    appealId,
    request.Reason,
      cancellationToken);

 if (!result.IsSuccess)
            {
         return BadRequest(new ErrorResponse
              {
   Message = result.Message
          });
     }

      return Ok(result);
        }
      catch (Exception ex)
        {
     _logger.LogError(ex, "Error escalating appeal {AppealId}", appealId);
  return StatusCode(500, new ErrorResponse
      {
        Message = "An error occurred while escalating the appeal",
  Details = ex.Message
            });
        }
    }

    // ========== APPEAL STATUS ==========

    /// <summary>
    /// Get appeal status and timeline
    /// GET /api/appeal/{appealId}/status
    /// </summary>
    [HttpGet("{appealId}/status")]
    [ProducesResponseType(typeof(AppealStatusDto), 200)]
    public async Task<ActionResult<AppealStatusDto>> GetAppealStatus(
        string appealId,
   CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting status for appeal {AppealId}", appealId);

        try
        {
            var status = await _appealService.GetAppealStatusAsync(appealId, cancellationToken);
            return Ok(status);
        }
        catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting appeal status");
            return StatusCode(500, new ErrorResponse
            {
      Message = "An error occurred while retrieving appeal status",
              Details = ex.Message
    });
        }
    }

 /// <summary>
    /// Get appeals nearing deadline
    /// GET /api/appeal/nearing-deadline
    /// </summary>
    [HttpGet("nearing-deadline")]
    [ProducesResponseType(typeof(List<AppealDto>), 200)]
    public async Task<ActionResult<List<AppealDto>>> GetAppealsNearingDeadline(
        [FromQuery] int daysThreshold = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving appeals nearing deadline");

        try
        {
     var appeals = await _appealService.GetAppealsNearingDeadlineAsync(daysThreshold, cancellationToken);
            return Ok(appeals);
        }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Error retrieving appeals nearing deadline");
        return StatusCode(500, new ErrorResponse
 {
        Message = "An error occurred while retrieving appeals",
       Details = ex.Message
   });
  }
    }

 /// <summary>
/// Get appeal statistics
    /// GET /api/appeal/statistics
    /// </summary>
    [HttpGet("statistics")]
[ProducesResponseType(typeof(AppealStatisticsDto), 200)]
 public async Task<ActionResult<AppealStatisticsDto>> GetAppealStatistics(
CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving appeal statistics");

      try
        {
      var stats = await _appealService.GetAppealStatisticsAsync(cancellationToken);
            return Ok(stats);
}
        catch (Exception ex)
  {
      _logger.LogError(ex, "Error retrieving appeal statistics");
            return StatusCode(500, new ErrorResponse
            {
            Message = "An error occurred while retrieving statistics",
    Details = ex.Message
   });
        }
    }

    // ========== DOCUMENTS ==========

    /// <summary>
    /// Attach document to appeal
    /// POST /api/appeal/{appealId}/documents
    /// </summary>
    [HttpPost("{appealId}/documents")]
    [ProducesResponseType(typeof(AttachDocumentResult), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<AttachDocumentResult>> AttachDocument(
        string appealId,
    [FromBody] AttachDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attaching document to appeal {AppealId}", appealId);

        try
        {
        request.AppealId = appealId;
            var result = await _appealService.AttachDocumentAsync(request, cancellationToken);

         if (!result.IsSuccess)
       {
          return BadRequest(new ErrorResponse
                {
        Message = result.Message
   });
  }

            return CreatedAtAction(nameof(GetAppealDocuments), new { appealId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching document");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while attaching the document",
     Details = ex.Message
   });
        }
    }

    /// <summary>
    /// Get appeal documents
    /// GET /api/appeal/{appealId}/documents
    /// </summary>
    [HttpGet("{appealId}/documents")]
    [ProducesResponseType(typeof(List<AppealDocumentDto>), 200)]
    public async Task<ActionResult<List<AppealDocumentDto>>> GetAppealDocuments(
        string appealId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving documents for appeal {AppealId}", appealId);

        try
        {
  var documents = await _appealService.GetAppealDocumentsAsync(appealId, cancellationToken);
      return Ok(documents);
        }
   catch (Exception ex)
        {
        _logger.LogError(ex, "Error retrieving documents");
  return StatusCode(500, new ErrorResponse
     {
            Message = "An error occurred while retrieving documents",
         Details = ex.Message
       });
        }
    }

    /// <summary>
    /// Remove document from appeal
    /// DELETE /api/appeal/{appealId}/documents/{documentId}
    /// </summary>
    [HttpDelete("{appealId}/documents/{documentId}")]
  [ProducesResponseType(204)]
  [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> RemoveDocument(
    string appealId,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing document {DocumentId} from appeal {AppealId}", documentId, appealId);

        try
      {
 var result = await _appealService.RemoveDocumentAsync(appealId, documentId, cancellationToken);

if (!result)
            {
return BadRequest(new ErrorResponse
                {
         Message = "Failed to remove document"
           });
            }

    return NoContent();
  }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Error removing document");
            return StatusCode(500, new ErrorResponse
          {
              Message = "An error occurred while removing the document",
  Details = ex.Message
});
        }
    }
}

/// <summary>
/// Withdraw request model
/// </summary>
public class WithdrawRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Escalate request model
/// </summary>
public class EscalateRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Error response model
/// </summary>
public class ErrorResponse
{
 public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public object? Errors { get; set; }
}
