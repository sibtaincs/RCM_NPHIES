using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// CommunicationRequest API Controller
/// Manages communication/instruction requests between insurers and providers
/// FHIR Resource: CommunicationRequest
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CommunicationRequestsController : ControllerBase
{
    private readonly IRepository<CommunicationRequest> _communicationRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CommunicationRequestsController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public CommunicationRequestsController(
   IRepository<CommunicationRequest> communicationRepository,
    IMapper mapper,
    ILogger<CommunicationRequestsController> logger)
    {
        _communicationRepository = communicationRepository ?? throw new ArgumentNullException(nameof(communicationRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

  /// <summary>
    /// Get all communication requests
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paginated list of communication requests</returns>
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

     var allRequests = await _communicationRepository.GetAllAsync();
            var totalCount = allRequests.Count();
      var requests = allRequests
 .Skip((pageNumber - 1) * pageSize)
     .Take(pageSize)
    .ToList();

      var requestDtos = _mapper.Map<List<CommunicationRequestDto>>(requests);

 _logger.LogInformation($"Retrieved {requestDtos.Count} communication requests (Page {pageNumber})");
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
            _logger.LogError(ex, "Error retrieving communication requests");
     return StatusCode(500, new { message = "An error occurred while retrieving communication requests" });
     }
    }

    /// <summary>
    /// Get communication request by ID
    /// </summary>
    /// <param name="id">Communication Request ID</param>
    /// <returns>Communication request details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
     if (string.IsNullOrWhiteSpace(id))
    {
       return BadRequest(new { message = "Communication Request ID is required" });
  }

  var request = await _communicationRepository.GetByIdAsync(id);
         if (request == null)
 {
         _logger.LogWarning($"Communication request not found: {id}");
    return NotFound(new { message = "Communication request not found" });
         }

          var requestDto = _mapper.Map<CommunicationRequestDto>(request);
  _logger.LogInformation($"Retrieved communication request: {id}");
  return Ok(requestDto);
      }
        catch (Exception ex)
   {
            _logger.LogError(ex, $"Error retrieving communication request: {id}");
            return StatusCode(500, new { message = "An error occurred while retrieving the communication request" });
        }
  }

    /// <summary>
    /// Get communication requests by patient ID
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <returns>List of communication requests for the patient</returns>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByPatientId(string patientId)
    {
        try
   {
 if (string.IsNullOrWhiteSpace(patientId))
     {
                return BadRequest(new { message = "Patient ID is required" });
         }

            var requests = await _communicationRepository.FindAsync(r => r.SubjectPatientId == patientId);
        var requestDtos = _mapper.Map<List<CommunicationRequestDto>>(requests);

_logger.LogInformation($"Retrieved {requestDtos.Count} communication requests for patient: {patientId}");
            return Ok(new { patientId = patientId, count = requestDtos.Count, items = requestDtos });
     }
 catch (Exception ex)
        {
     _logger.LogError(ex, $"Error retrieving communication requests for patient: {patientId}");
      return StatusCode(500, new { message = "An error occurred while retrieving communication requests" });
        }
    }

    /// <summary>
  /// Get communication requests by status
    /// </summary>
    /// <param name="status">Status filter (active, completed, cancelled, draft)</param>
    /// <returns>List of communication requests with specified status</returns>
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

         var validStatuses = new[] { "active", "completed", "cancelled", "draft" };
  if (!validStatuses.Contains(status.ToLower()))
  {
          return BadRequest(new { message = $"Invalid status. Valid values: {string.Join(", ", validStatuses)}" });
         }

    var requests = await _communicationRepository.FindAsync(r => r.Status == status);
   var requestDtos = _mapper.Map<List<CommunicationRequestDto>>(requests);

      _logger.LogInformation($"Retrieved {requestDtos.Count} communication requests with status: {status}");
   return Ok(new { status = status, count = requestDtos.Count, items = requestDtos });
        }
        catch (Exception ex)
        {
   _logger.LogError(ex, $"Error retrieving communication requests by status: {status}");
         return StatusCode(500, new { message = "An error occurred while retrieving communication requests" });
        }
    }

    /// <summary>
    /// Get communication requests by recipient organization
    /// </summary>
    /// <param name="recipientId">Recipient Organization ID</param>
    /// <returns>List of communication requests for the recipient</returns>
    [HttpGet("recipient/{recipientId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByRecipient(string recipientId)
    {
        try
     {
            if (string.IsNullOrWhiteSpace(recipientId))
            {
           return BadRequest(new { message = "Recipient ID is required" });
    }

            var requests = await _communicationRepository.FindAsync(r => r.RecipientId == recipientId);
        var requestDtos = _mapper.Map<List<CommunicationRequestDto>>(requests);

            _logger.LogInformation($"Retrieved {requestDtos.Count} communication requests for recipient: {recipientId}");
          return Ok(new { recipientId = recipientId, count = requestDtos.Count, items = requestDtos });
        }
        catch (Exception ex)
    {
        _logger.LogError(ex, $"Error retrieving communication requests by recipient: {recipientId}");
       return StatusCode(500, new { message = "An error occurred while retrieving communication requests" });
        }
    }

    /// <summary>
    /// Create a new communication request
    /// </summary>
    /// <param name="createDto">Communication request creation data</param>
    /// <returns>Created communication request</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateCommunicationRequestDto createDto)
    {
        try
        {
            if (createDto == null)
            {
                return BadRequest(new { message = "Communication request data is required" });
      }

     if (string.IsNullOrWhiteSpace(createDto.CommunicationRequestId))
  {
                return BadRequest(new { message = "Communication Request ID is required" });
 }

          var request = _mapper.Map<CommunicationRequest>(createDto);
            await _communicationRepository.AddAsync(request);
        await _communicationRepository.SaveChangesAsync();

            var requestDto = _mapper.Map<CommunicationRequestDto>(request);
            _logger.LogInformation($"Created new communication request: {request.Id}");

 return CreatedAtAction(nameof(GetById), new { id = request.Id }, requestDto);
        }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Error creating communication request");
    return StatusCode(500, new { message = "An error occurred while creating the communication request" });
        }
    }

    /// <summary>
    /// Update an existing communication request
    /// </summary>
    /// <param name="id">Communication Request ID</param>
    /// <param name="updateDto">Updated communication request data</param>
    /// <returns>Updated communication request</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCommunicationRequestDto updateDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
   {
     return BadRequest(new { message = "Communication Request ID is required" });
            }

   var request = await _communicationRepository.GetByIdAsync(id);
  if (request == null)
   {
           _logger.LogWarning($"Communication request not found for update: {id}");
    return NotFound(new { message = "Communication request not found" });
    }

   _mapper.Map(updateDto, request);
  _communicationRepository.Update(request);
            await _communicationRepository.SaveChangesAsync();

       var requestDto = _mapper.Map<CommunicationRequestDto>(request);
 _logger.LogInformation($"Updated communication request: {id}");

            return Ok(requestDto);
        }
   catch (Exception ex)
        {
    _logger.LogError(ex, $"Error updating communication request: {id}");
   return StatusCode(500, new { message = "An error occurred while updating the communication request" });
   }
    }

    /// <summary>
    /// Delete a communication request
    /// </summary>
    /// <param name="id">Communication Request ID</param>
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
return BadRequest(new { message = "Communication Request ID is required" });
  }

            var request = await _communicationRepository.GetByIdAsync(id);
            if (request == null)
            {
      _logger.LogWarning($"Communication request not found for deletion: {id}");
 return NotFound(new { message = "Communication request not found" });
            }

     _communicationRepository.Delete(request);
         await _communicationRepository.SaveChangesAsync();

            _logger.LogInformation($"Deleted communication request: {id}");
    return Ok(new { message = "Communication request deleted successfully" });
        }
        catch (Exception ex)
     {
          _logger.LogError(ex, $"Error deleting communication request: {id}");
            return StatusCode(500, new { message = "An error occurred while deleting the communication request" });
        }
 }
}
