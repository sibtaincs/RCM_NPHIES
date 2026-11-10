using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Common.Models;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Controller for handling NPHIES polling requests and responses
/// Enables providers to poll for queued messages from NPHIES
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PollingController : BaseController
{
    private readonly IPollingService _pollingService;
    private readonly ILogger<PollingController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public PollingController(IPollingService pollingService, ILogger<PollingController> logger)
    {
        _pollingService = pollingService ?? throw new ArgumentNullException(nameof(pollingService));
 _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

 /// <summary>
   /// Create a new poll request to NPHIES
    /// </summary>
    /// <param name="request">Poll request details</param>
  /// <returns>Created poll request</returns>
    /// <response code="201">Poll request created successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
  [HttpPost("request")]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<PollRequestDto>), StatusCodes.Status201Created)]
 [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
   public async Task<IActionResult> CreatePollRequest([FromBody] CreatePollRequestDto request)
    {
        try
        {
  _logger.LogInformation($"Creating poll request for provider: {request.ProviderId}");

  if (string.IsNullOrEmpty(request.ProviderId))
        {
    return BadRequest("Provider ID is required");
       }

 if (request.MessageTypes == null || request.MessageTypes.Count == 0)
  {
  return BadRequest("At least one message type is required");
     }

     var pollRequest = await _pollingService.CreatePollRequestAsync(
  request.ProviderId,
    request.MessageTypes
    );

       // Record polling activity
      var pollingRecord = new Domain.Entities.PollingRecord
   {
  Id = Guid.NewGuid().ToString(),
    PollingRecordId = $"poll-{Guid.NewGuid().ToString().Substring(0, 8)}",
       ProviderId = request.ProviderId,
   RequestTaskId = pollRequest.TaskId,
     RequestedMessageTypes = string.Join(",", request.MessageTypes),
    RequestSentAt = DateTime.UtcNow,
      ProcessingStatus = "pending",
 CycleStatus = "in-progress",
RetryCount = 0,
   CreatedAt = DateTime.UtcNow
      };

    await _pollingService.RecordPollingActivityAsync(pollingRecord);

   var dto = new PollRequestDto
  {
   Id = pollRequest.Id,
 TaskId = pollRequest.TaskId,
        Status = pollRequest.Status,
Code = pollRequest.Code,
   MessageTypes = request.MessageTypes,
       ProviderId = request.ProviderId,
         CreatedAt = pollRequest.CreatedAt
      };

   return Created($"/api/v1/polling/request/{pollRequest.Id}", 
     Ok(dto, "Poll request created successfully"));
        }
  catch (Exception ex)
    {
 _logger.LogError(ex, "Error creating poll request");
            return InternalServerError("Failed to create poll request");
        }
    }

    /// <summary>
 /// Get pending poll requests for a provider
    /// </summary>
 /// <param name="providerId">Provider organization ID</param>
    /// <returns>List of pending poll requests</returns>
    /// <response code="200">Poll requests retrieved successfully</response>
   /// <response code="404">Provider not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("requests/{providerId}")]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<List<PollRequestDto>>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetPendingPollRequests(string providerId)
    {
        try
        {
       _logger.LogInformation($"Getting pending poll requests for provider: {providerId}");

            var requests = await _pollingService.GetPendingPollRequestsAsync(providerId);

    var dtos = requests.Select(r => new PollRequestDto
            {
Id = r.Id,
           TaskId = r.TaskId,
Status = r.Status,
  Code = r.Code,
  MessageTypes = ExtractMessageTypes(r.ReasonText),
            ProviderId = providerId,
         CreatedAt = r.CreatedAt
       }).ToList();

            return Ok(dtos, $"Found {dtos.Count} pending poll requests");
        }
        catch (Exception ex)
      {
       _logger.LogError(ex, "Error getting pending poll requests");
  return InternalServerError("Failed to retrieve poll requests");
        }
    }

   /// <summary>
    /// Process a poll response from NPHIES
    /// </summary>
    /// <param name="response">Poll response with queued messages</param>
    /// <returns>Processing result</returns>
    /// <response code="200">Response processed successfully</response>
    /// <response code="400">Invalid response</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("response")]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<PollResponseDto>), StatusCodes.Status200OK)]
 [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessPollResponse([FromBody] ProcessPollResponseDto response)
    {
        try
    {
   _logger.LogInformation($"Processing poll response: {response.TaskId}");

        if (string.IsNullOrEmpty(response.TaskId))
    {
      return BadRequest("Task ID is required");
   }

    if (string.IsNullOrEmpty(response.ResponseBundle))
   {
         return BadRequest("Response bundle is required");
       }

  var startTime = DateTime.UtcNow;

     // Process the response
   var processedIds = await _pollingService.ProcessPollResponseAsync(
   null,
  response.ResponseBundle
 );

  var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;

   // Record polling response activity
    var pollingRecord = new Domain.Entities.PollingRecord
     {
 Id = Guid.NewGuid().ToString(),
 PollingRecordId = $"resp-{Guid.NewGuid().ToString().Substring(0, 8)}",
      ResponseTaskId = response.TaskId,
   ResponseStatus = response.ResponseCode ?? "ok",
 ResponseReceivedAt = DateTime.UtcNow,
       MessagesReceived = processedIds.Count,
   ProcessingStatus = "completed",
     CycleStatus = "completed",
   DurationMs = (long)duration,
 HttpStatusCode = 200,
   CreatedAt = DateTime.UtcNow
     };

    await _pollingService.RecordPollingActivityAsync(pollingRecord);

       var dto = new PollResponseDto
 {
TaskId = response.TaskId,
        Status = "processed",
     ProcessedMessageCount = processedIds.Count,
          ProcessedMessageIds = processedIds,
    ProcessedAt = DateTime.UtcNow
   };

   return Ok(dto, $"Poll response processed successfully ({processedIds.Count} messages)");
}
      catch (Exception ex)
        {
   _logger.LogError(ex, "Error processing poll response");
    return InternalServerError("Failed to process poll response");
        }
    }

   /// <summary>
    /// Get completed poll responses for a provider
    /// </summary>
    /// <param name="providerId">Provider organization ID</param>
    /// <returns>List of completed poll responses</returns>
    /// <response code="200">Poll responses retrieved successfully</response>
    /// <response code="404">Provider not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("responses/{providerId}")]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<List<PollResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCompletedPollResponses(string providerId)
    {
try
        {
    _logger.LogInformation($"Getting completed poll responses for provider: {providerId}");

         var responses = await _pollingService.GetCompletedPollResponsesAsync(providerId);

     var dtos = responses.Select(r => new PollResponseDto
      {
         TaskId = r.TaskId,
        Status = r.Status,
      ResponseCode = r.ResponseCode,
            ProcessedMessageCount = ExtractProcessedCount(r.ResultText),
 ProcessedAt = r.UpdatedAt ?? r.CreatedAt
       }).ToList();

            return Ok(dtos, $"Found {dtos.Count} completed poll responses");
        }
  catch (Exception ex)
        {
  _logger.LogError(ex, "Error getting completed poll responses");
     return InternalServerError("Failed to retrieve poll responses");
        }
    }

    /// <summary>
    /// Acknowledge a poll response (mark as processed)
  /// </summary>
    /// <param name="taskResponseId">Task response ID</param>
    /// <returns>Acknowledged response</returns>
    /// <response code="200">Response acknowledged successfully</response>
    /// <response code="404">Response not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("responses/{taskResponseId}/acknowledge")]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<PollResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AcknowledgePollResponse(string taskResponseId)
  {
        try
      {
    _logger.LogInformation($"Acknowledging poll response: {taskResponseId}");

          var response = await _pollingService.AcknowledgePollResponseAsync(taskResponseId);

            var dto = new PollResponseDto
           {
      TaskId = response.TaskId,
    Status = response.Status,
          ResponseCode = response.ResponseCode,
            ProcessedAt = response.UpdatedAt ?? response.CreatedAt
        };

    return Ok(dto, "Poll response acknowledged successfully");
        }
        catch (Exception ex)
        {
     _logger.LogError(ex, "Error acknowledging poll response");
            return InternalServerError("Failed to acknowledge poll response");
        }
    }

    /// <summary>
    /// Get queued messages for a provider
    /// </summary>
    /// <param name="providerId">Provider organization ID</param>
/// <returns>List of queued messages</returns>
    /// <response code="200">Queued messages retrieved successfully</response>
    /// <response code="404">Provider not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("messages/{providerId}")]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<List<QueuedMessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQueuedMessages(string providerId)
    {
  try
   {
 _logger.LogInformation($"Getting queued messages for provider: {providerId}");

    var messages = await _pollingService.GetQueuedMessagesAsync(providerId);

   var dtos = messages.Select(m => new QueuedMessageDto
      {
BundleId = m.BundleId,
          MessageType = m.MessageType,
     Status = m.Status,
    QueuedAt = m.QueuedAt
       }).ToList();

     return Ok(dtos, $"Found {dtos.Count} queued messages");
  }
      catch (Exception ex)
      {
       _logger.LogError(ex, "Error getting queued messages");
     return InternalServerError("Failed to retrieve queued messages");
}
    }

/// <summary>
    /// Get polling history for a provider
    /// </summary>
    /// <param name="providerId">Provider organization ID</param>
    /// <returns>Polling history records</returns>
    /// <response code="200">History retrieved successfully</response>
    /// <response code="404">Provider not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("history/{providerId}")]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<List<PollingHistoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPollingHistory(string providerId)
    {
   try
        {
     _logger.LogInformation($"Getting polling history for provider: {providerId}");

 var records = await _pollingService.GetPollingHistoryAsync(providerId);

        var dtos = records.Select(r => new PollingHistoryDto
  {
          PollingRecordId = r.PollingRecordId,
   RequestTaskId = r.RequestTaskId,
       ResponseTaskId = r.ResponseTaskId,
  RequestedMessageTypes = r.RequestedMessageTypes,
       ReceivedMessageTypes = r.ReceivedMessageTypes,
     RequestSentAt = r.RequestSentAt,
 ResponseReceivedAt = r.ResponseReceivedAt,
   MessagesReceived = r.MessagesReceived,
    ProcessingStatus = r.GetProcessingStatusDisplay(),
    CycleStatus = r.GetCycleStatusDisplay(),
   ResponseStatus = r.ResponseStatus ?? "pending",
DurationMs = r.DurationMs,
 IsAcknowledged = r.IsAcknowledged,
 ErrorCode = r.ErrorCode,
       ErrorMessage = r.ErrorMessage,
   CreatedAt = r.CreatedAt
            }).ToList();

       return Ok(dtos, $"Found {dtos.Count} polling records");
        }
     catch (Exception ex)
        {
     _logger.LogError(ex, "Error getting polling history");
     return InternalServerError("Failed to retrieve polling history");
}
    }

    /// <summary>
    /// Get specific polling record
    /// </summary>
    /// <param name="recordId">Polling record ID</param>
    /// <returns>Polling record details</returns>
    /// <response code="200">Record retrieved successfully</response>
    /// <response code="404">Record not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("record/{recordId}")]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<PollingHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Common.Models.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPollingRecord(string recordId)
    {
        try
        {
        _logger.LogInformation($"Getting polling record: {recordId}");

 var record = await _pollingService.GetPollingRecordAsync(recordId);
       
   if (record == null)
 {
         return NotFound("Polling record not found");
            }

      var dto = new PollingHistoryDto
 {
    PollingRecordId = record.PollingRecordId,
     RequestTaskId = record.RequestTaskId,
        ResponseTaskId = record.ResponseTaskId,
   RequestedMessageTypes = record.RequestedMessageTypes,
      ReceivedMessageTypes = record.ReceivedMessageTypes,
   RequestSentAt = record.RequestSentAt,
   ResponseReceivedAt = record.ResponseReceivedAt,
   MessagesReceived = record.MessagesReceived,
  ProcessingStatus = record.GetProcessingStatusDisplay(),
      CycleStatus = record.GetCycleStatusDisplay(),
      ResponseStatus = record.ResponseStatus ?? "pending",
  DurationMs = record.DurationMs,
  IsAcknowledged = record.IsAcknowledged,
    ErrorCode = record.ErrorCode,
           ErrorMessage = record.ErrorMessage,
  CreatedAt = record.CreatedAt
           };

            return Ok(dto, "Polling record retrieved successfully");
    }
        catch (Exception ex)
        {
    _logger.LogError(ex, "Error getting polling record");
        return InternalServerError("Failed to retrieve polling record");
        }
    }

    /// <summary>
    /// Extract message types from reason text
    /// </summary>
    private static List<string> ExtractMessageTypes(string? reasonText)
    {
        if (string.IsNullOrEmpty(reasonText))
 return new List<string>();

        // Parse from format: "Polling for messages: claim-response, eligibility-response"
        var parts = reasonText.Split(":");
    if (parts.Length > 1)
        {
      return parts[1]
    .Split(",")
       .Select(s => s.Trim())
   .Where(s => !string.IsNullOrEmpty(s))
.ToList();
        }

        return new List<string>();
 }

    /// <summary>
    /// Extract processed count from result text
  /// </summary>
    private static int ExtractProcessedCount(string? resultText)
    {
 if (string.IsNullOrEmpty(resultText))
     return 0;

    // Parse from format: "Processed 5 messages"
 var parts = resultText.Split();
      if (parts.Length > 1 && int.TryParse(parts[1], out var count))
        {
          return count;
        }

    return 0;
    }
}

/// <summary>
/// DTO for creating a poll request
/// </summary>
public class CreatePollRequestDto
{
    /// <summary>
    /// Provider organization ID
  /// </summary>
   public string ProviderId { get; set; } = string.Empty;

    /// <summary>
/// Message types to poll for
    /// </summary>
    public List<string> MessageTypes { get; set; } = new();
}

/// <summary>
/// DTO for poll request
/// </summary>
public class PollRequestDto
{
    public string Id { get; set; } = string.Empty;
 public string TaskId { get; set; } = string.Empty;
   public string Status { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public List<string> MessageTypes { get; set; } = new();
    public string ProviderId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for processing poll response
/// </summary>
public class ProcessPollResponseDto
{
    public string TaskId { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public string ResponseBundle { get; set; } = string.Empty;
}

/// <summary>
/// DTO for poll response
/// </summary>
public class PollResponseDto
{
 public string TaskId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
   public string ResponseCode { get; set; } = string.Empty;
    public int ProcessedMessageCount { get; set; }
 public List<string> ProcessedMessageIds { get; set; } = new();
   public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// DTO for queued message
/// </summary>
public class QueuedMessageDto
{
    public string BundleId { get; set; } = string.Empty;
   public string MessageType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
}

/// <summary>
/// DTO for polling history
/// </summary>
public class PollingHistoryDto
{
    public string PollingRecordId { get; set; } = string.Empty;
    public string? RequestTaskId { get; set; }
    public string? ResponseTaskId { get; set; }
    public string? RequestedMessageTypes { get; set; }
    public string? ReceivedMessageTypes { get; set; }
    public DateTime? RequestSentAt { get; set; }
    public DateTime? ResponseReceivedAt { get; set; }
    public int MessagesReceived { get; set; }
    public string ProcessingStatus { get; set; } = string.Empty;
    public string CycleStatus { get; set; } = string.Empty;
    public string ResponseStatus { get; set; } = string.Empty;
    public long? DurationMs { get; set; }
    public bool IsAcknowledged { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
