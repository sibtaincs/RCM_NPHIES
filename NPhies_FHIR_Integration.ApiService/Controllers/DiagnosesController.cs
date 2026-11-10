using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Application.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// API Controller for Claim Diagnosis management
/// Handles CRUD operations for diagnoses within claims
/// </summary>
[ApiController]
[Route("api/claims/{claimId}/[controller]")]
[Produces("application/json")]
public class DiagnosesController : ControllerBase
{
  private readonly IClaimDiagnosisService _diagnosisService;
  private readonly ILogger<DiagnosesController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public DiagnosesController(IClaimDiagnosisService diagnosisService, ILogger<DiagnosesController> logger)
 {
        _diagnosisService = diagnosisService ?? throw new ArgumentNullException(nameof(diagnosisService));
  _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new claim diagnosis
    /// </summary>
    /// <param name="claimId">Parent claim ID</param>
    /// <param name="dto">Diagnosis creation data</param>
    /// <returns>Created diagnosis</returns>
    /// <response code="201">Diagnosis created successfully</response>
    /// <response code="400">Invalid diagnosis data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClaimDiagnosisDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateDiagnosis(string claimId, [FromBody] CreateClaimDiagnosisDto dto)
    {
        try
    {
  if (string.IsNullOrWhiteSpace(claimId))
    return BadRequest(new { message = "Claim ID is required" });

      if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid diagnosis data", errors = ModelState.Values.SelectMany(v => v.Errors) });

 // Ensure claimId matches the DTO
   dto.ClaimId = claimId;

           if (string.IsNullOrWhiteSpace(dto.DiagnosisCode))
     return BadRequest(new { message = "Diagnosis code (ICD-10) is required" });

_logger.LogInformation("Creating new diagnosis for claim: {0}", claimId);
          var result = await _diagnosisService.CreateClaimDiagnosisAsync(dto);

    return CreatedAtAction(nameof(GetDiagnosis), new { claimId, id = result.Id }, result);
    }
        catch (InvalidOperationException ex)
        {
      _logger.LogError(ex, "Operation error while creating diagnosis for claim: {0}", claimId);
return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
 {
    _logger.LogError(ex, "Error while creating diagnosis for claim: {0}", claimId);
            return StatusCode(StatusCodes.Status500InternalServerError,
      new { message = "An error occurred while creating the diagnosis" });
      }
    }

    /// <summary>
    /// Get claim diagnosis by ID
    /// </summary>
    /// <param name="claimId">Parent claim ID</param>
    /// <param name="id">Diagnosis ID</param>
    /// <returns>Diagnosis details</returns>
 /// <response code="200">Diagnosis found and returned</response>
    /// <response code="404">Diagnosis not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClaimDiagnosisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDiagnosis(string claimId, string id)
    {
        try
    {
      if (string.IsNullOrWhiteSpace(claimId) || string.IsNullOrWhiteSpace(id))
        return BadRequest(new { message = "Claim ID and Diagnosis ID are required" });

   _logger.LogInformation("Retrieving diagnosis: {0} from claim: {1}", id, claimId);
 var result = await _diagnosisService.GetClaimDiagnosisAsync(id);

           if (result == null || result.ClaimId != claimId)
           {
 _logger.LogWarning("Diagnosis not found: {0}", id);
     return NotFound(new { message = $"Diagnosis with ID {id} not found" });
    }

  return Ok(result);
 }
  catch (Exception ex)
        {
 _logger.LogError(ex, "Error while retrieving diagnosis: {0}", id);
return StatusCode(StatusCodes.Status500InternalServerError,
 new { message = "An error occurred while retrieving the diagnosis" });
}
    }

    /// <summary>
    /// Get all diagnoses for a claim
    /// </summary>
    /// <param name="claimId">Claim ID</param>
    /// <returns>List of claim diagnoses</returns>
    /// <response code="200">Diagnoses found and returned</response>
    /// <response code="400">Invalid claim ID</response>
    /// <response code="500">Internal server error</response>
[HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClaimDiagnosisDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClaimDiagnoses(string claimId)
    {
        try
        {
       if (string.IsNullOrWhiteSpace(claimId))
    return BadRequest(new { message = "Claim ID is required" });

      _logger.LogInformation("Retrieving diagnoses for claim: {0}", claimId);
    var result = await _diagnosisService.GetClaimDiagnosesAsync(claimId);

     return Ok(new
     {
   claimId,
 count = result.Count(),
    diagnoses = result
        });
        }
       catch (Exception ex)
        {
     _logger.LogError(ex, "Error while retrieving diagnoses for claim: {0}", claimId);
      return StatusCode(StatusCodes.Status500InternalServerError,
    new { message = "An error occurred while retrieving claim diagnoses" });
        }
    }

    /// <summary>
    /// Get diagnoses by code
    /// </summary>
    /// <param name="diagnosisCode">ICD-10 code (e.g., J39.3)</param>
    /// <returns>List of diagnoses with matching code</returns>
    /// <response code="200">Diagnoses found and returned</response>
    /// <response code="400">Invalid code</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("code/{diagnosisCode}")]
    [ProducesResponseType(typeof(IEnumerable<ClaimDiagnosisDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDiagnosesByCode(string diagnosisCode)
    {
 try
        {
   if (string.IsNullOrWhiteSpace(diagnosisCode))
     return BadRequest(new { message = "Diagnosis code is required" });

 _logger.LogInformation("Retrieving diagnoses with code: {0}", diagnosisCode);
   var result = await _diagnosisService.GetDiagnosesByCodeAsync(diagnosisCode);

    return Ok(new
    {
   diagnosisCode,
     count = result.Count(),
 diagnoses = result
  });
        }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Error while retrieving diagnoses by code: {0}", diagnosisCode);
         return StatusCode(StatusCodes.Status500InternalServerError,
        new { message = "An error occurred while retrieving diagnoses" });
        }
    }

    /// <summary>
    /// Update claim diagnosis
    /// </summary>
    /// <param name="claimId">Parent claim ID</param>
  /// <param name="id">Diagnosis ID to update</param>
    /// <param name="dto">Diagnosis update data</param>
    /// <returns>Updated diagnosis</returns>
 /// <response code="200">Diagnosis updated successfully</response>
    /// <response code="400">Invalid data</response>
    /// <response code="404">Diagnosis not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ClaimDiagnosisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
 public async Task<IActionResult> UpdateDiagnosis(string claimId, string id, [FromBody] UpdateClaimDiagnosisDto dto)
  {
        try
        {
    if (string.IsNullOrWhiteSpace(claimId) || string.IsNullOrWhiteSpace(id))
   return BadRequest(new { message = "Claim ID and Diagnosis ID are required" });

       if (!ModelState.IsValid)
         return BadRequest(new { message = "Invalid diagnosis data", errors = ModelState.Values.SelectMany(v => v.Errors) });

   _logger.LogInformation("Updating diagnosis: {0} in claim: {1}", id, claimId);
            var result = await _diagnosisService.UpdateClaimDiagnosisAsync(id, dto);

       return Ok(result);
        }
        catch (KeyNotFoundException ex)
     {
           _logger.LogWarning("Diagnosis not found for update: {0}", id);
     return NotFound(new { message = ex.Message });
}
        catch (InvalidOperationException ex)
        {
      _logger.LogError(ex, "Operation error while updating diagnosis: {0}", id);
       return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
 {
     _logger.LogError(ex, "Error while updating diagnosis: {0}", id);
       return StatusCode(StatusCodes.Status500InternalServerError,
 new { message = "An error occurred while updating the diagnosis" });
       }
    }

    /// <summary>
    /// Delete claim diagnosis
    /// </summary>
    /// <param name="claimId">Parent claim ID</param>
    /// <param name="id">Diagnosis ID to delete</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Diagnosis deleted successfully</response>
    /// <response code="404">Diagnosis not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDiagnosis(string claimId, string id)
    {
        try
        {
        if (string.IsNullOrWhiteSpace(claimId) || string.IsNullOrWhiteSpace(id))
     return BadRequest(new { message = "Claim ID and Diagnosis ID are required" });

        _logger.LogInformation("Deleting diagnosis: {0} from claim: {1}", id, claimId);
 var success = await _diagnosisService.DeleteClaimDiagnosisAsync(id);

         if (!success)
        {
        _logger.LogWarning("Diagnosis not found for deletion: {0}", id);
             return NotFound(new { message = $"Diagnosis with ID {id} not found" });
      }

            return NoContent();
        }
  catch (Exception ex)
   {
   _logger.LogError(ex, "Error while deleting diagnosis: {0}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
        new { message = "An error occurred while deleting the diagnosis" });
        }
    }
}
