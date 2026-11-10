using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Cancellation Request API Controller
/// Manages cancellation requests from providers to insurers (e.g., claim cancellation requests)
/// FHIR Resource: Task (Cancel variant)
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CancellationRequestsController : ControllerBase
{
    private readonly IRepository<CancellationRequest> _cancellationRequestRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CancellationRequestsController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public CancellationRequestsController(
  IRepository<CancellationRequest> cancellationRequestRepository,
        IMapper mapper,
   ILogger<CancellationRequestsController> logger)
  {
      _cancellationRequestRepository = cancellationRequestRepository ?? throw new ArgumentNullException(nameof(cancellationRequestRepository));
     _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all cancellation requests
 /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paginated list of cancellation requests</returns>
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

       var allRequests = await _cancellationRequestRepository.GetAllAsync();
            var totalCount = allRequests.Count();
            var requests = allRequests
         .Skip((pageNumber - 1) * pageSize)
  .Take(pageSize)
     .ToList();

    var requestDtos = _mapper.Map<List<TaskRequestDto>>(requests);

 _logger.LogInformation($"Retrieved {requestDtos.Count} cancellation requests (Page {pageNumber})");
       return Ok(new
    {
     pageNumber = pageNumber,
    pageSize = pageSize,
    totalCount = totalCount,
          totalPages = (totalCount + pageSize - 1) / pageSize,
   items = requestDtos
         });
        }
        catch (Exception ex)
      {
            _logger.LogError(ex, "Error retrieving cancellation requests");
     return StatusCode(500, new { message = "An error occurred while retrieving cancellation requests" });
        }
    }

  /// <summary>
    /// Get cancellation request by ID
    /// </summary>
  /// <param name="id">Cancellation Request ID</param>
    /// <returns>Cancellation request details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
 try
     {
  if (string.IsNullOrWhiteSpace(id))
      {
        return BadRequest(new { message = "Cancellation Request ID is required" });
  }

       var request = await _cancellationRequestRepository.GetByIdAsync(id);
if (request == null)
            {
   _logger.LogWarning($"Cancellation request not found: {id}");
 return NotFound(new { message = "Cancellation request not found" });
   }

 var requestDto = _mapper.Map<TaskRequestDto>(request);
            _logger.LogInformation($"Retrieved cancellation request: {id}");
      return Ok(requestDto);
        }
        catch (Exception ex)
  {
 _logger.LogError(ex, $"Error retrieving cancellation request: {id}");
          return StatusCode(500, new { message = "An error occurred while retrieving the cancellation request" });
   }
    }

    /// <summary>
 /// Get cancellation requests by status
    /// </summary>
    /// <param name="status">Status filter (requested, in-progress, completed, failed, cancelled)</param>
    /// <returns>List of cancellation requests with specified status</returns>
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

 var validStatuses = new[] { "requested", "in-progress", "completed", "failed", "cancelled" };
if (!validStatuses.Contains(status.ToLower()))
         {
 return BadRequest(new { message = $"Invalid status. Valid values: {string.Join(", ", validStatuses)}" });
    }

       var requests = await _cancellationRequestRepository.FindAsync(t => t.Status == status && t.Code == "cancel");
            var requestDtos = _mapper.Map<List<TaskRequestDto>>(requests);

       _logger.LogInformation($"Retrieved {requestDtos.Count} cancellation requests with status: {status}");
 return Ok(new { status = status, count = requestDtos.Count, items = requestDtos });
        }
  catch (Exception ex)
        {
  _logger.LogError(ex, $"Error retrieving cancellation requests by status: {status}");
    return StatusCode(500, new { message = "An error occurred while retrieving cancellation requests" });
    }
    }

    /// <summary>
    /// Get cancellation requests by reason code
    /// </summary>
    /// <param name="reasonCode">Reason code filter (WI, PR, PC, UC)</param>
/// <returns>List of cancellation requests with specified reason</returns>
    [HttpGet("reason/{reasonCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByReasonCode(string reasonCode)
    {
        try
    {
       if (string.IsNullOrWhiteSpace(reasonCode))
      {
     return BadRequest(new { message = "Reason code is required" });
  }

  var requests = await _cancellationRequestRepository.FindAsync(t => t.ReasonCode == reasonCode && t.Code == "cancel");
 var requestDtos = _mapper.Map<List<TaskRequestDto>>(requests);

    _logger.LogInformation($"Retrieved {requestDtos.Count} cancellation requests with reason: {reasonCode}");
      return Ok(new { reasonCode = reasonCode, count = requestDtos.Count, items = requestDtos });
        }
   catch (Exception ex)
{
   _logger.LogError(ex, $"Error retrieving cancellation requests by reason: {reasonCode}");
       return StatusCode(500, new { message = "An error occurred while retrieving cancellation requests" });
        }
    }

    /// <summary>
    /// Get cancellation requests by owner (insurer)
    /// </summary>
    /// <param name="ownerId">Owner Organization ID</param>
    /// <returns>List of cancellation requests assigned to the owner</returns>
    [HttpGet("owner/{ownerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByOwner(string ownerId)
    {
   try
        {
 if (string.IsNullOrWhiteSpace(ownerId))
  {
            return BadRequest(new { message = "Owner ID is required" });
}

var requests = await _cancellationRequestRepository.FindAsync(t => t.OwnerId == ownerId && t.Code == "cancel");
var requestDtos = _mapper.Map<List<TaskRequestDto>>(requests);

       _logger.LogInformation($"Retrieved {requestDtos.Count} cancellation requests for owner: {ownerId}");
      return Ok(new { ownerId = ownerId, count = requestDtos.Count, items = requestDtos });
     }
     catch (Exception ex)
        {
          _logger.LogError(ex, $"Error retrieving cancellation requests by owner: {ownerId}");
            return StatusCode(500, new { message = "An error occurred while retrieving cancellation requests" });
        }
    }

    /// <summary>
    /// Create a new cancellation request
    /// </summary>
    /// <param name="createDto">Cancellation request creation data</param>
    /// <returns>Created cancellation request</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequestDto createDto)
    {
     try
 {
   if (createDto == null)
     {
   return BadRequest(new { message = "Cancellation request data is required" });
  }

   if (string.IsNullOrWhiteSpace(createDto.TaskId))
   {
    return BadRequest(new { message = "Task ID is required" });
          }

 // Ensure code is set to "cancel"
         createDto.Code = "cancel";

       var request = _mapper.Map<CancellationRequest>(createDto);
   await _cancellationRequestRepository.AddAsync(request);
     await _cancellationRequestRepository.SaveChangesAsync();

var requestDto = _mapper.Map<TaskRequestDto>(request);
            _logger.LogInformation($"Created new cancellation request: {request.Id}");

     return CreatedAtAction(nameof(GetById), new { id = request.Id }, requestDto);
      }
        catch (Exception ex)
        {
     _logger.LogError(ex, "Error creating cancellation request");
     return StatusCode(500, new { message = "An error occurred while creating the cancellation request" });
        }
    }

  /// <summary>
    /// Update an existing cancellation request
    /// </summary>
  /// <param name="id">Cancellation Request ID</param>
    /// <param name="updateDto">Updated cancellation request data</param>
    /// <returns>Updated cancellation request</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTaskRequestDto updateDto)
    {
    try
        {
 if (string.IsNullOrWhiteSpace(id))
      {
       return BadRequest(new { message = "Cancellation Request ID is required" });
 }

       var request = await _cancellationRequestRepository.GetByIdAsync(id);
      if (request == null)
  {
        _logger.LogWarning($"Cancellation request not found for update: {id}");
     return NotFound(new { message = "Cancellation request not found" });
   }

 _mapper.Map(updateDto, request);
   _cancellationRequestRepository.Update(request);
            await _cancellationRequestRepository.SaveChangesAsync();

   var requestDto = _mapper.Map<TaskRequestDto>(request);
     _logger.LogInformation($"Updated cancellation request: {id}");

 return Ok(requestDto);
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, $"Error updating cancellation request: {id}");
  return StatusCode(500, new { message = "An error occurred while updating the cancellation request" });
   }
}

    /// <summary>
    /// Delete a cancellation request
    /// </summary>
    /// <param name="id">Cancellation Request ID</param>
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
return BadRequest(new { message = "Cancellation Request ID is required" });
       }

   var request = await _cancellationRequestRepository.GetByIdAsync(id);
            if (request == null)
   {
     _logger.LogWarning($"Cancellation request not found for deletion: {id}");
     return NotFound(new { message = "Cancellation request not found" });
  }

  _cancellationRequestRepository.Delete(request);
        await _cancellationRequestRepository.SaveChangesAsync();

     _logger.LogInformation($"Deleted cancellation request: {id}");
          return Ok(new { message = "Cancellation request deleted successfully" });
 }
        catch (Exception ex)
      {
   _logger.LogError(ex, $"Error deleting cancellation request: {id}");
return StatusCode(500, new { message = "An error occurred while deleting the cancellation request" });
        }
    }
}
