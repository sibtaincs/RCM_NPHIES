using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Cancellation Response API Controller
/// Manages cancellation responses from insurers to providers (e.g., responses to claim cancellation requests)
/// FHIR Resource: Task (Cancel response variant)
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CancellationResponsesController : ControllerBase
{
    private readonly IRepository<CancellationResponse> _cancellationResponseRepository;
    private readonly IRepository<CancellationRequest> _cancellationRequestRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CancellationResponsesController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public CancellationResponsesController(
  IRepository<CancellationResponse> cancellationResponseRepository,
   IRepository<CancellationRequest> cancellationRequestRepository,
        IMapper mapper,
    ILogger<CancellationResponsesController> logger)
    {
      _cancellationResponseRepository = cancellationResponseRepository ?? throw new ArgumentNullException(nameof(cancellationResponseRepository));
  _cancellationRequestRepository = cancellationRequestRepository ?? throw new ArgumentNullException(nameof(cancellationRequestRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all cancellation responses
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
 /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paginated list of cancellation responses</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
 public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
  try
        {
    if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            {
         _logger.LogWarning("Invalid pagination parameters");
     return BadRequest(new { message = "Invalid pagination parameters" });
    }

      var allResponses = await _cancellationResponseRepository.GetAllAsync();
         var totalCount = allResponses.Count();
  var responses = allResponses
     .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
 .ToList();

 var responseDtos = _mapper.Map<List<TaskResponseDto>>(responses);

  _logger.LogInformation($"Retrieved {responseDtos.Count} cancellation responses (Page {pageNumber})");
        return Ok(new
    {
          pageNumber = pageNumber,
  pageSize = pageSize,
    totalCount = totalCount,
       totalPages = (totalCount + pageSize - 1) / pageSize,
       items = responseDtos
            });
        }
   catch (Exception ex)
        {
_logger.LogError(ex, "Error retrieving cancellation responses");
    return StatusCode(500, new { message = "An error occurred while retrieving cancellation responses" });
        }
}

    /// <summary>
    /// Get cancellation response by ID
    /// </summary>
    /// <param name="id">Cancellation Response ID</param>
    /// <returns>Cancellation response details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        try
   {
       if (string.IsNullOrWhiteSpace(id))
       {
     return BadRequest(new { message = "Cancellation Response ID is required" });
    }

  var response = await _cancellationResponseRepository.GetByIdAsync(id);
         if (response == null)
   {
      _logger.LogWarning($"Cancellation response not found: {id}");
           return NotFound(new { message = "Cancellation response not found" });
  }

  var responseDto = _mapper.Map<TaskResponseDto>(response);
 _logger.LogInformation($"Retrieved cancellation response: {id}");
  return Ok(responseDto);
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, $"Error retrieving cancellation response: {id}");
          return StatusCode(500, new { message = "An error occurred while retrieving the cancellation response" });
        }
    }

    /// <summary>
    /// Get cancellation responses by status
    /// </summary>
    /// <param name="status">Status filter (completed, failed, in-progress, rejected, cancelled)</param>
    /// <returns>List of cancellation responses with specified status</returns>
    [HttpGet("status/{status}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByStatus(string status)
    {
        try
   {
     if (string.IsNullOrWhiteSpace(status))
       {
          return BadRequest(new { message = "Status is required" });
}

      var validStatuses = new[] { "completed", "failed", "in-progress", "rejected", "cancelled" };
       if (!validStatuses.Contains(status.ToLower()))
            {
         return BadRequest(new { message = $"Invalid status. Valid values: {string.Join(", ", validStatuses)}" });
            }

  var responses = await _cancellationResponseRepository.FindAsync(t => t.Status == status && t.Code == "cancel");
      var responseDtos = _mapper.Map<List<TaskResponseDto>>(responses);

     _logger.LogInformation($"Retrieved {responseDtos.Count} cancellation responses with status: {status}");
      return Ok(new { status = status, count = responseDtos.Count, items = responseDtos });
        }
      catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving cancellation responses by status: {status}");
        return StatusCode(500, new { message = "An error occurred while retrieving cancellation responses" });
        }
 }

    /// <summary>
    /// Get cancellation responses by response code
    /// </summary>
    /// <param name="responseCode">Response code filter (ok, error, partial-success, timeout)</param>
    /// <returns>List of cancellation responses with specified response code</returns>
    [HttpGet("response-code/{responseCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
 public async Task<IActionResult> GetByResponseCode(string responseCode)
    {
try
        {
            if (string.IsNullOrWhiteSpace(responseCode))
{
         return BadRequest(new { message = "Response code is required" });
       }

            var responses = await _cancellationResponseRepository.FindAsync(t => t.ResponseCode == responseCode && t.Code == "cancel");
    var responseDtos = _mapper.Map<List<TaskResponseDto>>(responses);

     _logger.LogInformation($"Retrieved {responseDtos.Count} cancellation responses with response code: {responseCode}");
        return Ok(new { responseCode = responseCode, count = responseDtos.Count, items = responseDtos });
     }
  catch (Exception ex)
        {
  _logger.LogError(ex, $"Error retrieving cancellation responses by response code: {responseCode}");
   return StatusCode(500, new { message = "An error occurred while retrieving cancellation responses" });
        }
    }

    /// <summary>
    /// Get successful cancellation responses
  /// </summary>
    /// <returns>List of successful cancellation responses</returns>
    [HttpGet("filter/successful")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuccessful()
    {
     try
   {
  var responses = await _cancellationResponseRepository.FindAsync(t => t.ResponseCode == "ok" && t.ResponseStatusCode == 200 && t.Code == "cancel");
        var responseDtos = _mapper.Map<List<TaskResponseDto>>(responses);

       _logger.LogInformation($"Retrieved {responseDtos.Count} successful cancellation responses");
    return Ok(new { count = responseDtos.Count, items = responseDtos });
        }
   catch (Exception ex)
      {
   _logger.LogError(ex, "Error retrieving successful cancellation responses");
   return StatusCode(500, new { message = "An error occurred while retrieving cancellation responses" });
        }
    }

    /// <summary>
    /// Get failed cancellation responses
    /// </summary>
    /// <returns>List of failed cancellation responses</returns>
    [HttpGet("filter/failed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailed()
{
        try
 {
      var responses = await _cancellationResponseRepository.FindAsync(t => (t.ResponseCode == "error" || t.ResponseStatusCode >= 400) && t.Code == "cancel");
  var responseDtos = _mapper.Map<List<TaskResponseDto>>(responses);

            _logger.LogInformation($"Retrieved {responseDtos.Count} failed cancellation responses");
return Ok(new { count = responseDtos.Count, items = responseDtos });
     }
      catch (Exception ex)
        {
   _logger.LogError(ex, "Error retrieving failed cancellation responses");
        return StatusCode(500, new { message = "An error occurred while retrieving cancellation responses" });
        }
    }

    /// <summary>
    /// Create a new cancellation response
    /// </summary>
    /// <param name="createDto">Cancellation response creation data</param>
    /// <returns>Created cancellation response</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateTaskResponseDto createDto)
    {
        try
        {
 if (createDto == null)
  {
       return BadRequest(new { message = "Cancellation response data is required" });
    }

     if (string.IsNullOrWhiteSpace(createDto.TaskId))
    {
        return BadRequest(new { message = "Task ID is required" });
            }

     // Ensure code is set to "cancel"
     createDto.Code = "cancel";

            // If referenced cancellation request ID provided, verify it exists
    if (!string.IsNullOrEmpty(createDto.TaskRequestId))
 {
      var request = await _cancellationRequestRepository.GetByIdAsync(createDto.TaskRequestId);
      if (request == null)
    {
  _logger.LogWarning($"Referenced cancellation request not found: {createDto.TaskRequestId}");
    return BadRequest(new { message = "Referenced cancellation request not found" });
  }
   }

 var response = _mapper.Map<CancellationResponse>(createDto);
  await _cancellationResponseRepository.AddAsync(response);
  await _cancellationResponseRepository.SaveChangesAsync();

  var responseDto = _mapper.Map<TaskResponseDto>(response);
    _logger.LogInformation($"Created new cancellation response: {response.Id}");

    return CreatedAtAction(nameof(GetById), new { id = response.Id }, responseDto);
   }
     catch (Exception ex)
 {
            _logger.LogError(ex, "Error creating cancellation response");
  return StatusCode(500, new { message = "An error occurred while creating the cancellation response" });
        }
    }

    /// <summary>
    /// Update an existing cancellation response
    /// </summary>
    /// <param name="id">Cancellation Response ID</param>
    /// <param name="updateDto">Updated cancellation response data</param>
    /// <returns>Updated cancellation response</returns>
 [HttpPut("{id}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTaskResponseDto updateDto)
    {
      try
      {
  if (string.IsNullOrWhiteSpace(id))
       {
 return BadRequest(new { message = "Cancellation Response ID is required" });
   }

  var response = await _cancellationResponseRepository.GetByIdAsync(id);
  if (response == null)
            {
   _logger.LogWarning($"Cancellation response not found for update: {id}");
 return NotFound(new { message = "Cancellation response not found" });
        }

_mapper.Map(updateDto, response);
        _cancellationResponseRepository.Update(response);
      await _cancellationResponseRepository.SaveChangesAsync();

    var responseDto = _mapper.Map<TaskResponseDto>(response);
     _logger.LogInformation($"Updated cancellation response: {id}");

   return Ok(responseDto);
   }
        catch (Exception ex)
     {
         _logger.LogError(ex, $"Error updating cancellation response: {id}");
            return StatusCode(500, new { message = "An error occurred while updating the cancellation response" });
    }
    }

    /// <summary>
    /// Delete a cancellation response
    /// </summary>
    /// <param name="id">Cancellation Response ID</param>
    /// <returns>Deletion status</returns>
    [HttpDelete("{id}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
try
    {
            if (string.IsNullOrWhiteSpace(id))
     {
       return BadRequest(new { message = "Cancellation Response ID is required" });
     }

    var response = await _cancellationResponseRepository.GetByIdAsync(id);
     if (response == null)
 {
     _logger.LogWarning($"Cancellation response not found for deletion: {id}");
  return NotFound(new { message = "Cancellation response not found" });
   }

     _cancellationResponseRepository.Delete(response);
  await _cancellationResponseRepository.SaveChangesAsync();

 _logger.LogInformation($"Deleted cancellation response: {id}");
return Ok(new { message = "Cancellation response deleted successfully" });
  }
        catch (Exception ex)
   {
_logger.LogError(ex, $"Error deleting cancellation response: {id}");
  return StatusCode(500, new { message = "An error occurred while deleting the cancellation response" });
     }
    }
}
