using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Communication API Controller
/// Manages communication responses from providers to insurers
/// FHIR Resource: Communication
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CommunicationsController : ControllerBase
{
    private readonly IRepository<Communication> _communicationRepository;
private readonly IMapper _mapper;
    private readonly ILogger<CommunicationsController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public CommunicationsController(
        IRepository<Communication> communicationRepository,
        IMapper mapper,
        ILogger<CommunicationsController> logger)
 {
        _communicationRepository = communicationRepository ?? throw new ArgumentNullException(nameof(communicationRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
     _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all communications
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
  /// <returns>Paginated list of communications</returns>
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

            var allCommunications = await _communicationRepository.GetAllAsync();
  var totalCount = allCommunications.Count();
            var communications = allCommunications
        .Skip((pageNumber - 1) * pageSize)
     .Take(pageSize)
          .ToList();

 var communicationDtos = _mapper.Map<List<CommunicationDto>>(communications);

            _logger.LogInformation($"Retrieved {communicationDtos.Count} communications (Page {pageNumber})");
     return Ok(new
            {
    pageNumber = pageNumber,
          pageSize = pageSize,
        totalCount = totalCount,
     totalPages = (totalCount + pageSize - 1) / pageSize,
        items = communicationDtos
            });
        }
     catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving communications");
            return StatusCode(500, new { message = "An error occurred while retrieving communications" });
        }
 }

    /// <summary>
  /// Get communication by ID
    /// </summary>
    /// <param name="id">Communication ID</param>
    /// <returns>Communication details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
     if (string.IsNullOrWhiteSpace(id))
        {
  return BadRequest(new { message = "Communication ID is required" });
            }

 var communication = await _communicationRepository.GetByIdAsync(id);
            if (communication == null)
       {
           _logger.LogWarning($"Communication not found: {id}");
     return NotFound(new { message = "Communication not found" });
            }

     var communicationDto = _mapper.Map<CommunicationDto>(communication);
            _logger.LogInformation($"Retrieved communication: {id}");
      return Ok(communicationDto);
      }
      catch (Exception ex)
  {
     _logger.LogError(ex, $"Error retrieving communication: {id}");
       return StatusCode(500, new { message = "An error occurred while retrieving the communication" });
        }
  }

    /// <summary>
    /// Get communications by patient ID
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <returns>List of communications for the patient</returns>
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

            var communications = await _communicationRepository.FindAsync(c => c.SubjectPatientId == patientId);
            var communicationDtos = _mapper.Map<List<CommunicationDto>>(communications);

      _logger.LogInformation($"Retrieved {communicationDtos.Count} communications for patient: {patientId}");
            return Ok(new { patientId = patientId, count = communicationDtos.Count, items = communicationDtos });
        }
        catch (Exception ex)
   {
          _logger.LogError(ex, $"Error retrieving communications for patient: {patientId}");
  return StatusCode(500, new { message = "An error occurred while retrieving communications" });
        }
    }

    /// <summary>
    /// Get communications by status
    /// </summary>
    /// <param name="status">Status filter (in-progress, completed, entered-in-error, not-done)</param>
    /// <returns>List of communications with specified status</returns>
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

   var validStatuses = new[] { "in-progress", "completed", "entered-in-error", "not-done" };
    if (!validStatuses.Contains(status.ToLower()))
      {
       return BadRequest(new { message = $"Invalid status. Valid values: {string.Join(", ", validStatuses)}" });
    }

     var communications = await _communicationRepository.FindAsync(c => c.Status == status);
      var communicationDtos = _mapper.Map<List<CommunicationDto>>(communications);

    _logger.LogInformation($"Retrieved {communicationDtos.Count} communications with status: {status}");
 return Ok(new { status = status, count = communicationDtos.Count, items = communicationDtos });
        }
 catch (Exception ex)
      {
            _logger.LogError(ex, $"Error retrieving communications by status: {status}");
        return StatusCode(500, new { message = "An error occurred while retrieving communications" });
        }
    }

    /// <summary>
 /// Get communications by sender organization
    /// </summary>
    /// <param name="senderId">Sender Organization ID</param>
    /// <returns>List of communications from the sender</returns>
    [HttpGet("sender/{senderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBySender(string senderId)
    {
        try
  {
  if (string.IsNullOrWhiteSpace(senderId))
     {
          return BadRequest(new { message = "Sender ID is required" });
    }

var communications = await _communicationRepository.FindAsync(c => c.SenderId == senderId);
    var communicationDtos = _mapper.Map<List<CommunicationDto>>(communications);

            _logger.LogInformation($"Retrieved {communicationDtos.Count} communications from sender: {senderId}");
      return Ok(new { senderId = senderId, count = communicationDtos.Count, items = communicationDtos });
        }
  catch (Exception ex)
        {
     _logger.LogError(ex, $"Error retrieving communications by sender: {senderId}");
        return StatusCode(500, new { message = "An error occurred while retrieving communications" });
        }
    }

    /// <summary>
    /// Get communications by recipient organization
    /// </summary>
    /// <param name="recipientId">Recipient Organization ID</param>
    /// <returns>List of communications to the recipient</returns>
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

          var communications = await _communicationRepository.FindAsync(c => c.RecipientId == recipientId);
 var communicationDtos = _mapper.Map<List<CommunicationDto>>(communications);

    _logger.LogInformation($"Retrieved {communicationDtos.Count} communications for recipient: {recipientId}");
         return Ok(new { recipientId = recipientId, count = communicationDtos.Count, items = communicationDtos });
        }
        catch (Exception ex)
      {
    _logger.LogError(ex, $"Error retrieving communications by recipient: {recipientId}");
      return StatusCode(500, new { message = "An error occurred while retrieving communications" });
        }
    }

    /// <summary>
 /// Create a new communication
    /// </summary>
    /// <param name="createDto">Communication creation data</param>
    /// <returns>Created communication</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateCommunicationDto createDto)
    {
        try
        {
       if (createDto == null)
            {
         return BadRequest(new { message = "Communication data is required" });
     }

   if (string.IsNullOrWhiteSpace(createDto.CommunicationId))
       {
 return BadRequest(new { message = "Communication ID is required" });
       }

  var communication = _mapper.Map<Communication>(createDto);
      await _communicationRepository.AddAsync(communication);
     await _communicationRepository.SaveChangesAsync();

         var communicationDto = _mapper.Map<CommunicationDto>(communication);
    _logger.LogInformation($"Created new communication: {communication.Id}");

        return CreatedAtAction(nameof(GetById), new { id = communication.Id }, communicationDto);
     }
        catch (Exception ex)
        {
       _logger.LogError(ex, "Error creating communication");
            return StatusCode(500, new { message = "An error occurred while creating the communication" });
   }
  }

    /// <summary>
    /// Update an existing communication
    /// </summary>
    /// <param name="id">Communication ID</param>
  /// <param name="updateDto">Updated communication data</param>
    /// <returns>Updated communication</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCommunicationDto updateDto)
    {
      try
 {
            if (string.IsNullOrWhiteSpace(id))
    {
         return BadRequest(new { message = "Communication ID is required" });
   }

            var communication = await _communicationRepository.GetByIdAsync(id);
            if (communication == null)
            {
       _logger.LogWarning($"Communication not found for update: {id}");
      return NotFound(new { message = "Communication not found" });
          }

            _mapper.Map(updateDto, communication);
       _communicationRepository.Update(communication);
            await _communicationRepository.SaveChangesAsync();

     var communicationDto = _mapper.Map<CommunicationDto>(communication);
      _logger.LogInformation($"Updated communication: {id}");

    return Ok(communicationDto);
        }
        catch (Exception ex)
        {
     _logger.LogError(ex, $"Error updating communication: {id}");
   return StatusCode(500, new { message = "An error occurred while updating the communication" });
  }
    }

    /// <summary>
    /// Delete a communication
    /// </summary>
    /// <param name="id">Communication ID</param>
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
           return BadRequest(new { message = "Communication ID is required" });
            }

            var communication = await _communicationRepository.GetByIdAsync(id);
            if (communication == null)
        {
                _logger.LogWarning($"Communication not found for deletion: {id}");
         return NotFound(new { message = "Communication not found" });
            }

      _communicationRepository.Delete(communication);
   await _communicationRepository.SaveChangesAsync();

        _logger.LogInformation($"Deleted communication: {id}");
            return Ok(new { message = "Communication deleted successfully" });
        }
 catch (Exception ex)
        {
 _logger.LogError(ex, $"Error deleting communication: {id}");
            return StatusCode(500, new { message = "An error occurred while deleting the communication" });
        }
    }

    /// <summary>
    /// Download communication attachment
    /// </summary>
    /// <param name="id">Communication ID</param>
    /// <returns>File attachment</returns>
    [HttpGet("{id}/attachment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> DownloadAttachment(string id)
    {
    try
        {
          if (string.IsNullOrWhiteSpace(id))
   {
         return BadRequest(new { message = "Communication ID is required" });
            }

    var communication = await _communicationRepository.GetByIdAsync(id);
     if (communication == null)
            {
            _logger.LogWarning($"Communication not found: {id}");
       return NotFound(new { message = "Communication not found" });
            }

   if (!communication.HasAttachment())
  {
                _logger.LogWarning($"No attachment found in communication: {id}");
        return NotFound(new { message = "No attachment found in this communication" });
         }

  var fileName = communication.PayloadAttachmentTitle ?? $"attachment_{id}";
            var contentType = communication.PayloadAttachmentContentType ?? "application/octet-stream";

 _logger.LogInformation($"Downloading attachment from communication: {id}");
          return File(communication.PayloadAttachmentData, contentType, fileName);
  }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading attachment from communication: {id}");
  return StatusCode(500, new { message = "An error occurred while downloading the attachment" });
    }
    }
}
