using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Application.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Generic base controller for master data CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class MasterDataControllerBase<TDto> : BaseController where TDto : class
{
    protected readonly IMasterDataService<object, TDto> _service;
    protected readonly ILogger<MasterDataControllerBase<TDto>> _logger;

    protected MasterDataControllerBase(
        IMasterDataService<object, TDto> service,
        ILogger<MasterDataControllerBase<TDto>> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all records with pagination
    /// </summary>
    [HttpGet]
   [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? searchTerm = null)
    {
        try
        {
            var (items, total) = await _service.GetAllAsync(skip, take, searchTerm);
          return Ok(new { items, total, skip, take });
        }
        catch (Exception ex)
        {
       _logger.LogError(ex, "Error retrieving records");
            return StatusCode(500, new { message = "An error occurred while retrieving records" });
        }
    }

    /// <summary>
    /// Get a single record by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetById(string id)
    {
    try
      {
            if (string.IsNullOrEmpty(id))
            return BadRequest(new { message = "ID is required" });

    var item = await _service.GetByIdAsync(id);
            if (item == null)
                return NotFound(new { message = $"Record with ID {id} not found" });

      return Ok(item);
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, "Error retrieving record");
        return StatusCode(500, new { message = "An error occurred while retrieving the record" });
        }
    }

 /// <summary>
  /// Create a new record
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] TDto dto)
    {
        try
        {
  if (dto == null)
       return BadRequest(new { message = "Request body is required" });

    var createdBy = User?.FindFirst("sub")?.Value ?? "API";
 var result = await _service.CreateAsync(dto, createdBy);
            return CreatedAtAction(nameof(GetById), new { id = GetIdFromDto(result) }, result);
        }
    catch (Exception ex)
     {
            _logger.LogError(ex, "Error creating record");
       return StatusCode(500, new { message = "An error occurred while creating the record" });
    }
    }

    /// <summary>
/// Update an existing record
    /// </summary>
   [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(string id, [FromBody] TDto dto)
    {
    try
        {
            if (string.IsNullOrEmpty(id))
      return BadRequest(new { message = "ID is required" });

       if (dto == null)
  return BadRequest(new { message = "Request body is required" });

       var modifiedBy = User?.FindFirst("sub")?.Value ?? "API";
            var result = await _service.UpdateAsync(id, dto, modifiedBy);
   return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Record not found");
return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Error updating record");
          return StatusCode(500, new { message = "An error occurred while updating the record" });
        }
    }

    /// <summary>
    /// Delete a record
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(string id)
  {
        try
        {
    if (string.IsNullOrEmpty(id))
         return BadRequest(new { message = "ID is required" });

            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
    _logger.LogWarning(ex, "Record not found");
          return NotFound(new { message = ex.Message });
        }
      catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting record");
  return StatusCode(500, new { message = "An error occurred while deleting the record" });
        }
    }

    /// <summary>
    /// Search records
    /// </summary>
    [HttpGet("search/{criteria}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Search(string criteria)
    {
        try
        {
            if (string.IsNullOrEmpty(criteria))
      return BadRequest(new { message = "Search criteria is required" });

            var results = await _service.SearchAsync(criteria);
            return Ok(results);
        }
     catch (Exception ex)
 {
     _logger.LogError(ex, "Error searching records");
   return StatusCode(500, new { message = "An error occurred while searching records" });
        }
    }

    /// <summary>
    /// Toggle active status
    /// </summary>
    [HttpPatch("{id}/toggle-active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ToggleActive(string id, [FromQuery] bool isActive)
    {
        try
        {
         if (string.IsNullOrEmpty(id))
     return BadRequest(new { message = "ID is required" });

        var result = await _service.ToggleActiveAsync(id, isActive);
     return Ok(result);
        }
   catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Record not found");
    return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
           _logger.LogError(ex, "Error toggling active status");
      return StatusCode(500, new { message = "An error occurred while toggling active status" });
        }
    }

    /// <summary>
    /// Helper method to extract ID from DTO - override in derived classes if needed
    /// </summary>
    protected virtual string? GetIdFromDto(TDto dto)
    {
        var idProperty = dto?.GetType().GetProperty("Id");
        return idProperty?.GetValue(dto)?.ToString();
    }
}
