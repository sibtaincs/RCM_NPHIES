using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Application.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// API Controller for Claim Item management
/// Handles CRUD operations for items within claims
/// </summary>
[ApiController]
[Route("api/claims/{claimId}/[controller]")]
[Produces("application/json")]
public class ItemsController : ControllerBase
{
    private readonly IClaimItemService _itemService;
    private readonly ILogger<ItemsController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public ItemsController(IClaimItemService itemService, ILogger<ItemsController> logger)
    {
   _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
  /// Create a new claim item
    /// </summary>
    /// <param name="claimId">Parent claim ID</param>
/// <param name="dto">Item creation data</param>
    /// <returns>Created item</returns>
    /// <response code="201">Item created successfully</response>
    /// <response code="400">Invalid item data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClaimItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateItem(string claimId, [FromBody] CreateClaimItemDto dto)
    {
        try
     {
            if (string.IsNullOrWhiteSpace(claimId))
     return BadRequest(new { message = "Claim ID is required" });

      if (!ModelState.IsValid)
          return BadRequest(new { message = "Invalid item data", errors = ModelState.Values.SelectMany(v => v.Errors) });

   // Ensure claimId matches the DTO
  dto.ClaimId = claimId;

    if (string.IsNullOrWhiteSpace(dto.ProductOrServiceCode))
        return BadRequest(new { message = "Product or service code is required" });

         _logger.LogInformation("Creating new item for claim: {0}", claimId);
        var result = await _itemService.CreateClaimItemAsync(dto);

            return CreatedAtAction(nameof(GetItem), new { claimId, id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
         _logger.LogError(ex, "Operation error while creating item for claim: {0}", claimId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
     _logger.LogError(ex, "Error while creating item for claim: {0}", claimId);
return StatusCode(StatusCodes.Status500InternalServerError,
          new { message = "An error occurred while creating the item" });
  }
    }

    /// <summary>
    /// Get claim item by ID
    /// </summary>
    /// <param name="claimId">Parent claim ID</param>
    /// <param name="id">Item ID</param>
  /// <returns>Item details</returns>
    /// <response code="200">Item found and returned</response>
    /// <response code="404">Item not found</response>
  /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClaimItemDto), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
 [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetItem(string claimId, string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(claimId) || string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Claim ID and Item ID are required" });

   _logger.LogInformation("Retrieving item: {0} from claim: {1}", id, claimId);
        var result = await _itemService.GetClaimItemAsync(id);

            if (result == null || result.ClaimId != claimId)
          {
          _logger.LogWarning("Item not found: {0}", id);
     return NotFound(new { message = $"Item with ID {id} not found" });
      }

   return Ok(result);
        }
        catch (Exception ex)
  {
            _logger.LogError(ex, "Error while retrieving item: {0}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
            new { message = "An error occurred while retrieving the item" });
        }
    }

    /// <summary>
    /// Get all items for a claim
    /// </summary>
    /// <param name="claimId">Claim ID</param>
    /// <returns>List of claim items</returns>
    /// <response code="200">Items found and returned</response>
    /// <response code="400">Invalid claim ID</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClaimItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClaimItems(string claimId)
    {
        try
        {
    if (string.IsNullOrWhiteSpace(claimId))
  return BadRequest(new { message = "Claim ID is required" });

       _logger.LogInformation("Retrieving items for claim: {0}", claimId);
            var result = await _itemService.GetClaimItemsAsync(claimId);

            return Ok(new
     {
        claimId,
            count = result.Count(),
            items = result
  });
        }
        catch (Exception ex)
   {
       _logger.LogError(ex, "Error while retrieving items for claim: {0}", claimId);
            return StatusCode(StatusCodes.Status500InternalServerError,
         new { message = "An error occurred while retrieving claim items" });
  }
    }

    /// <summary>
    /// Update claim item
    /// </summary>
    /// <param name="claimId">Parent claim ID</param>
    /// <param name="id">Item ID to update</param>
    /// <param name="dto">Item update data</param>
    /// <returns>Updated item</returns>
    /// <response code="200">Item updated successfully</response>
    /// <response code="400">Invalid data</response>
    /// <response code="404">Item not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ClaimItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateItem(string claimId, string id, [FromBody] UpdateClaimItemDto dto)
    {
        try
        {
      if (string.IsNullOrWhiteSpace(claimId) || string.IsNullOrWhiteSpace(id))
       return BadRequest(new { message = "Claim ID and Item ID are required" });

     if (!ModelState.IsValid)
     return BadRequest(new { message = "Invalid item data", errors = ModelState.Values.SelectMany(v => v.Errors) });

       _logger.LogInformation("Updating item: {0} in claim: {1}", id, claimId);
  var result = await _itemService.UpdateClaimItemAsync(id, dto);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
   {
      _logger.LogWarning("Item not found for update: {0}", id);
   return NotFound(new { message = ex.Message });
        }
    catch (InvalidOperationException ex)
        {
    _logger.LogError(ex, "Operation error while updating item: {0}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, "Error while updating item: {0}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
     new { message = "An error occurred while updating the item" });
        }
    }

    /// <summary>
    /// Delete claim item
    /// </summary>
    /// <param name="claimId">Parent claim ID</param>
    /// <param name="id">Item ID to delete</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Item deleted successfully</response>
    /// <response code="404">Item not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteItem(string claimId, string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(claimId) || string.IsNullOrWhiteSpace(id))
         return BadRequest(new { message = "Claim ID and Item ID are required" });

  _logger.LogInformation("Deleting item: {0} from claim: {1}", id, claimId);
     var success = await _itemService.DeleteClaimItemAsync(id);

       if (!success)
   {
      _logger.LogWarning("Item not found for deletion: {0}", id);
            return NotFound(new { message = $"Item with ID {id} not found" });
    }

            return NoContent();
        }
        catch (Exception ex)
 {
    _logger.LogError(ex, "Error while deleting item: {0}", id);
      return StatusCode(StatusCodes.Status500InternalServerError,
    new { message = "An error occurred while deleting the item" });
        }
    }
}
