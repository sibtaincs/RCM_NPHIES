using NPhies_FHIR_Integration.Domain.Entities;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// Service for handling NPHIES polling requests and responses
/// Manages the polling mechanism for retrieving queued messages from NPHIES
/// </summary>
public interface IPollingService
{
    /// <summary>
    /// Create a poll request task
    /// </summary>
  /// <param name="providerId">Provider organization ID</param>
    /// <param name="messageTypes">Message types to poll for (e.g., "claim-response")</param>
    /// <returns>Created CancellationRequest</returns>
  Task<CancellationRequest> CreatePollRequestAsync(string providerId, List<string> messageTypes);

    /// <summary>
 /// Record polling activity
 /// </summary>
 /// <param name="pollingRecord">Polling record to store</param>
 /// <returns>Stored polling record</returns>
    Task<PollingRecord> RecordPollingActivityAsync(PollingRecord pollingRecord);

    /// <summary>
    /// Get polling history for a provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <returns>List of polling records</returns>
    Task<List<PollingRecord>> GetPollingHistoryAsync(string providerId);

    /// <summary>
    /// Get polling record by ID
    /// </summary>
    /// <param name="recordId">Polling record ID</param>
    /// <returns>Polling record</returns>
Task<PollingRecord?> GetPollingRecordAsync(string recordId);

    /// <summary>
    /// Process poll response with queued messages
    /// </summary>
    /// <param name="taskResponse">Poll response Task</param>
    /// <param name="responseBundle">Bundle containing queued messages</param>
 /// <returns>List of processed message IDs</returns>
    Task<List<string>> ProcessPollResponseAsync(CancellationResponse taskResponse, string responseBundle);

    /// <summary>
    /// Get pending poll requests for a provider
    /// </summary>
    /// <param name="providerId">Provider organization ID</param>
    /// <returns>List of pending poll requests</returns>
 Task<List<CancellationRequest>> GetPendingPollRequestsAsync(string providerId);

    /// <summary>
    /// Get completed poll responses
  /// </summary>
    /// <param name="providerId">Provider organization ID</param>
    /// <returns>List of completed poll responses</returns>
    Task<List<CancellationResponse>> GetCompletedPollResponsesAsync(string providerId);

    /// <summary>
    /// Acknowledge poll response (mark as processed)
    /// </summary>
    /// <param name="taskResponseId">Task response ID</param>
    /// <returns>Updated CancellationResponse</returns>
    Task<CancellationResponse> AcknowledgePollResponseAsync(string taskResponseId);

    /// <summary>
 /// Get queued messages for a provider
 /// </summary>
 /// <param name="providerId">Provider organization ID</param>
 /// <returns>List of queued message bundles</returns>
    Task<List<MessageBundle>> GetQueuedMessagesAsync(string providerId);
}

/// <summary>
/// Represents a queued message bundle
/// </summary>
public class MessageBundle
{
    /// <summary>
    /// Unique bundle ID
    /// </summary>
    public string BundleId { get; set; } = string.Empty;

    /// <summary>
    /// Message type (claim-response, eligibility-response, etc.)
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Bundle content as JSON
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the message was queued
    /// </summary>
    public DateTime QueuedAt { get; set; }

    /// <summary>
    /// Processing status
    /// </summary>
    public string Status { get; set; } = "pending"; // pending, processed, failed
}

/// <summary>
/// Implementation of polling service
/// </summary>
public class PollingService : IPollingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PollingService> _logger;

  /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public PollingService(ApplicationDbContext context, ILogger<PollingService> logger)
    {
 _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a poll request task
    /// </summary>
    public async Task<CancellationRequest> CreatePollRequestAsync(string providerId, List<string> messageTypes)
    {
 try
    {
       _logger.LogInformation($"Creating poll request for provider: {providerId}, message types: {string.Join(", ", messageTypes)}");

  var pollRequest = new CancellationRequest
   {
       Id = Guid.NewGuid().ToString(),
      TaskId = $"poll-{Guid.NewGuid().ToString().Substring(0, 8)}",
   IdentifierSystem = "http://nphies.sa/identifier/task",
IdentifierValue = Guid.NewGuid().ToString(),
           Status = "requested",
  Intent = "order",
           Priority = "stat",
  Code = "poll",
              CodeSystem = "http://nphies.sa/terminology/CodeSystem/task-code",
      FocusResourceType = "Bundle",
        ReasonCode = "poll-request",
     ReasonCodeSystem = "http://nphies.sa/terminology/CodeSystem/task-reason",
     ReasonText = $"Polling for messages: {string.Join(", ", messageTypes)}",
       RequesterId = providerId,
 OwnerId = "NPHIES", // System owner is NPHIES
       ProcessingStatus = "pending",
   AuthoredOn = DateTime.UtcNow,
       LastModified = DateTime.UtcNow,
    CreatedAt = DateTime.UtcNow,
 Description = $"Poll request for message types: {string.Join(", ", messageTypes)}"
  };

  await _context.CancellationRequests.AddAsync(pollRequest);
      await _context.SaveChangesAsync();

            _logger.LogInformation($"Poll request created successfully: {pollRequest.TaskId}");

  return pollRequest;
        }
        catch (Exception ex)
  {
            _logger.LogError(ex, "Error creating poll request");
   throw;
        }
    }

    /// <summary>
 /// Record polling activity
 /// </summary>
 /// <param name="pollingRecord">Polling record to store</param>
 /// <returns>Stored polling record</returns>
    public async Task<PollingRecord> RecordPollingActivityAsync(PollingRecord pollingRecord)
    {
        try
      {
         _logger.LogInformation($"Recording polling activity for record: {pollingRecord.PollingRecordId}");

    if (string.IsNullOrEmpty(pollingRecord.Id))
    {
       pollingRecord.Id = Guid.NewGuid().ToString();
     }
         pollingRecord.CreatedAt = DateTime.UtcNow;

   await _context.PollingRecords.AddAsync(pollingRecord);
      await _context.SaveChangesAsync();

         _logger.LogInformation($"Polling activity recorded: {pollingRecord.Id}");

     return pollingRecord;
        }
        catch (Exception ex)
    {
      _logger.LogError(ex, "Error recording polling activity");
         throw;
        }
    }

    /// <summary>
    /// Process poll response with queued messages
    /// </summary>
    public async Task<List<string>> ProcessPollResponseAsync(CancellationResponse taskResponse, string responseBundle)
    {
        try
   {
      _logger.LogInformation($"Processing poll response: {taskResponse.TaskId}");

 var processedIds = new List<string>();

// Parse the response bundle
          using (JsonDocument doc = JsonDocument.Parse(responseBundle))
    {
       var root = doc.RootElement;

         if (root.TryGetProperty("entry", out JsonElement entries))
            {
      foreach (var entry in entries.EnumerateArray())
        {
   if (entry.TryGetProperty("resource", out JsonElement resource))
      {
    if (resource.TryGetProperty("resourceType", out JsonElement resourceType))
{
  var type = resourceType.GetString();
   _logger.LogInformation($"Processing resource type: {type}");

 // Store the resource based on type
     var resourceId = await ProcessResourceAsync(type, resource.GetRawText());
   if (resourceId != null)
       {
   processedIds.Add(resourceId);
      }
       }
  }
        }
      }
          }

  // Update task response status
            taskResponse.Status = "completed";
         taskResponse.ProcessingStatus = "processed";
      taskResponse.ResponseCode = "ok";
     taskResponse.ResultText = $"Processed {processedIds.Count} messages";
     taskResponse.LastModified = DateTime.UtcNow;
   taskResponse.UpdatedAt = DateTime.UtcNow;

    _context.CancellationResponses.Update(taskResponse);
    await _context.SaveChangesAsync();

     _logger.LogInformation($"Poll response processed successfully: {processedIds.Count} messages");

  return processedIds;
 }
  catch (Exception ex)
      {
       _logger.LogError(ex, "Error processing poll response");
    throw;
  }
    }

    /// <summary>
    /// Process individual resource from bundle
    /// </summary>
 private async Task<string?> ProcessResourceAsync(string? resourceType, string resourceJson)
    {
        try
        {
    return resourceType switch
            {
   "MessageHeader" => await ProcessMessageHeaderAsync(resourceJson),
       "ClaimResponse" => await ProcessClaimResponseAsync(resourceJson),
                "CoverageEligibilityResponse" => await ProcessEligibilityResponseAsync(resourceJson),
     "Communication" => await ProcessCommunicationAsync(resourceJson),
        "Bundle" => await ProcessNestedBundleAsync(resourceJson),
      _ => null
         };
        }
        catch (Exception ex)
     {
  _logger.LogError(ex, $"Error processing resource type: {resourceType}");
      return null;
        }
    }

    /// <summary>
    /// Process MessageHeader from bundle
    /// </summary>
    private async Task<string?> ProcessMessageHeaderAsync(string headerJson)
    {
        try
   {
     using (JsonDocument doc = JsonDocument.Parse(headerJson))
     {
       var root = doc.RootElement;
if (root.TryGetProperty("id", out JsonElement id))
      {
         _logger.LogInformation($"Processing MessageHeader: {id.GetString()}");
     return id.GetString();
        }
      }
            return null;
  }
        catch (Exception ex)
        {
 _logger.LogError(ex, "Error processing MessageHeader");
            return null;
     }
    }

    /// <summary>
  /// Process ClaimResponse from bundle
    /// </summary>
    private async Task<string?> ProcessClaimResponseAsync(string responseJson)
    {
        try
        {
       using (JsonDocument doc = JsonDocument.Parse(responseJson))
        {
        var root = doc.RootElement;
        if (root.TryGetProperty("id", out JsonElement id))
      {
    _logger.LogInformation($"Processing ClaimResponse: {id.GetString()}");
           // TODO: Store ClaimResponse in database
         return id.GetString();
                }
 }
       return null;
        }
        catch (Exception ex)
        {
    _logger.LogError(ex, "Error processing ClaimResponse");
            return null;
   }
    }

    /// <summary>
    /// Process CoverageEligibilityResponse from bundle
 /// </summary>
    private async Task<string?> ProcessEligibilityResponseAsync(string responseJson)
    {
        try
        {
          using (JsonDocument doc = JsonDocument.Parse(responseJson))
            {
     var root = doc.RootElement;
    if (root.TryGetProperty("id", out JsonElement id))
           {
       _logger.LogInformation($"Processing CoverageEligibilityResponse: {id.GetString()}");
      // TODO: Store EligibilityResponse in database
    return id.GetString();
       }
         }
    return null;
        }
 catch (Exception ex)
      {
         _logger.LogError(ex, "Error processing CoverageEligibilityResponse");
    return null;
        }
    }

    /// <summary>
    /// Process Communication from bundle
    /// </summary>
    private async Task<string?> ProcessCommunicationAsync(string communicationJson)
    {
      try
        {
     using (JsonDocument doc = JsonDocument.Parse(communicationJson))
          {
   var root = doc.RootElement;
    if (root.TryGetProperty("id", out JsonElement id))
       {
        _logger.LogInformation($"Processing Communication: {id.GetString()}");
         // TODO: Store Communication in database
        return id.GetString();
      }
        }
            return null;
        }
        catch (Exception ex)
  {
       _logger.LogError(ex, "Error processing Communication");
            return null;
     }
    }

    /// <summary>
 /// Process nested Bundle (e.g., claim-response bundle within poll response)
    /// </summary>
    private async Task<string?> ProcessNestedBundleAsync(string bundleJson)
    {
   try
        {
            using (JsonDocument doc = JsonDocument.Parse(bundleJson))
            {
      var root = doc.RootElement;
     if (root.TryGetProperty("id", out JsonElement id))
          {
             _logger.LogInformation($"Processing nested Bundle: {id.GetString()}");
                // TODO: Store Bundle metadata
             return id.GetString();
        }
            }
            return null;
        }
        catch (Exception ex)
{
          _logger.LogError(ex, "Error processing nested Bundle");
            return null;
        }
    }

  /// <summary>
    /// Get pending poll requests for a provider
    /// </summary>
    public async Task<List<CancellationRequest>> GetPendingPollRequestsAsync(string providerId)
    {
        try
 {
  _logger.LogInformation($"Getting pending poll requests for provider: {providerId}");

  return await Task.FromResult(_context.CancellationRequests
      .Where(r => r.RequesterId == providerId &&
   r.Code == "poll" &&
  r.Status == "requested")
         .ToList());
}
        catch (Exception ex)
    {
   _logger.LogError(ex, "Error getting pending poll requests");
throw;
 }
    }

 /// <summary>
    /// Get completed poll responses
    /// </summary>
    public async Task<List<CancellationResponse>> GetCompletedPollResponsesAsync(string providerId)
    {
        try
     {
       _logger.LogInformation($"Getting completed poll responses for provider: {providerId}");

    return await Task.FromResult(_context.CancellationResponses
     .Where(r => r.RequesterId == providerId &&
         r.Status == "completed" &&
    r.ProcessingStatus == "processed")
         .ToList());
        }
      catch (Exception ex)
   {
        _logger.LogError(ex, "Error getting completed poll responses");
        throw;
        }
    }

  /// <summary>
    /// Acknowledge poll response (mark as processed)
    /// </summary>
    public async Task<CancellationResponse> AcknowledgePollResponseAsync(string taskResponseId)
    {
try
        {
          _logger.LogInformation($"Acknowledging poll response: {taskResponseId}");

       var response = _context.CancellationResponses.FirstOrDefault(r => r.Id == taskResponseId);
      if (response == null)
{
       throw new InvalidOperationException($"Poll response not found: {taskResponseId}");
         }

      response.ProcessingStatus = "acknowledged";
         response.LastModified = DateTime.UtcNow;
       response.UpdatedAt = DateTime.UtcNow;

   _context.CancellationResponses.Update(response);
     await _context.SaveChangesAsync();

         _logger.LogInformation($"Poll response acknowledged: {taskResponseId}");

  return response;
   }
        catch (Exception ex)
    {
   _logger.LogError(ex, "Error acknowledging poll response");
         throw;
   }
    }
    
    /// <summary>
    /// Get queued messages for a provider
    /// </summary>
 public async Task<List<MessageBundle>> GetQueuedMessagesAsync(string providerId)
    {
 try
        {
       _logger.LogInformation($"Getting queued messages for provider: {providerId}");

        // Get all completed poll responses for this provider
   var responses = await GetCompletedPollResponsesAsync(providerId);

  var queuedMessages = new List<MessageBundle>();

      foreach (var response in responses)
     {
 if (!string.IsNullOrEmpty(response.FhirTaskJson))
       {
   try
        {
   using (JsonDocument doc = JsonDocument.Parse(response.FhirTaskJson))
{
       var root = doc.RootElement;

      // Extract output bundles from Task
if (root.TryGetProperty("output", out JsonElement outputs))
  {
 foreach (var output in outputs.EnumerateArray())
 {
   if (output.TryGetProperty("valueReference", out JsonElement reference) &&
         reference.TryGetProperty("reference", out JsonElement refUrl))
     {
    var bundleRef = refUrl.GetString();
          _logger.LogInformation($"Found message bundle reference: {bundleRef}");

   // TODO: Retrieve actual bundle content from reference
 queuedMessages.Add(new MessageBundle
     {
       BundleId = bundleRef ?? string.Empty,
     MessageType = "unknown", // Should parse from bundle
  Content = response.FhirTaskJson,
QueuedAt = response.CreatedAt,
     Status = "pending"
 });
 }
   }
     }
    }
       }
          catch (Exception ex)
      {
     _logger.LogError(ex, $"Error parsing FHIR Task JSON for response: {response.Id}");
            }
   }
      }

    return queuedMessages;
        }
catch (Exception ex)
        {
          _logger.LogError(ex, "Error getting queued messages");
        throw;
        }
 }

    /// <summary>
    /// Get polling history for a provider
    /// </summary>
    public async Task<List<PollingRecord>> GetPollingHistoryAsync(string providerId)
    {
     try
  {
     _logger.LogInformation($"Getting polling history for provider: {providerId}");

      return await Task.FromResult(_context.PollingRecords
      .Where(r => r.ProviderId == providerId)
  .OrderByDescending(r => r.CreatedAt)
    .ToList());
        }
        catch (Exception ex)
     {
          _logger.LogError(ex, "Error getting polling history");
  throw;
    }
    }

    /// <summary>
    /// Get polling record by ID
  /// </summary>
    public async Task<PollingRecord?> GetPollingRecordAsync(string recordId)
    {
      try
    {
       _logger.LogInformation($"Getting polling record: {recordId}");

       return await Task.FromResult(_context.PollingRecords
   .FirstOrDefault(r => r.Id == recordId || r.PollingRecordId == recordId));
    }
  catch (Exception ex)
        {
      _logger.LogError(ex, "Error getting polling record");
      throw;
  }
    }
}
