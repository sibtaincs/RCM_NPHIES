using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;

namespace NPhies_FHIR_Integration.Application.Services
{
    /// <summary>
    /// Service for creating NPHIES-compliant message envelopes
    /// </summary>
    public interface INphiesMessageService
    {
     Task<NphiesBundleEntity> CreateBundleAsync(List<object> resources, string eventCode, int? messageHeaderId = null);
        Task<NphiesMessageHeaderEntity> CreateMessageHeaderAsync(string eventCode, string primaryResourceType, string primaryResourceId, int providerOrgId, string reason = null);
        Task<NphiesMessageHeaderEntity> GetMessageAsync(int id);
      Task UpdateMessageStatusAsync(int messageId, string status, string result = null);
    }

    public class NphiesMessageService : INphiesMessageService
    {
        private readonly ILogger<NphiesMessageService> _logger;

      public NphiesMessageService(ILogger<NphiesMessageService> logger)
        {
        _logger = logger;
      }

        public async Task<NphiesBundleEntity> CreateBundleAsync(List<object> resources, string eventCode, int? messageHeaderId = null)
        {
       try
            {
     var bundle = new NphiesBundleEntity
                {
 BundleId = Guid.NewGuid().ToString().Substring(0, 8),
       BundleType = "message",
           Timestamp = DateTime.UtcNow,
  MessageHeaderId = messageHeaderId,
        TotalEntries = resources.Count,
ProcessingStatus = "received"
          };

    int sequence = 0;
     foreach (var resource in resources)
  {
           var entry = new BundleEntryEntity
       {
 FullUrl = $"urn:uuid:{Guid.NewGuid()}",
               ResourceType = resource.GetType().Name.Replace("Entity", ""),
        ResourceId = ((dynamic)resource).Id?.ToString() ?? Guid.NewGuid().ToString(),
       ResourceJson = JsonSerializer.Serialize(resource, new JsonSerializerOptions { WriteIndented = true }),
       SequenceNumber = sequence++,
            CreatedAt = DateTime.UtcNow
        };

           bundle.Entries.Add(entry);
       }

         _logger.LogInformation($"? Bundle created: {bundle.BundleId} with {bundle.Entries.Count} entries");

    return bundle;
 }
            catch (Exception ex)
    {
              _logger.LogError(ex, "? Error creating bundle");
       throw;
       }
        }

        public async Task<NphiesMessageHeaderEntity> CreateMessageHeaderAsync(
            string eventCode,
     string primaryResourceType,
   string primaryResourceId,
            int providerOrgId,
      string reason = null)
  {
          try
       {
        var messageId = GenerateMessageId();

    var messageHeader = new NphiesMessageHeaderEntity
            {
              MessageId = messageId,
       EventCode = eventCode,
      TimeSent = DateTime.UtcNow,
         MessageVersion = "1.0.0",
           ProviderOrganizationId = providerOrgId,
        PayerIdentifier = "NPHIES",
   PrimaryResourceType = primaryResourceType,
 PrimaryResourceId = primaryResourceId,
 Reason = reason,
   ProcessingStatus = "received",
             CreatedAt = DateTime.UtcNow,
        CreatedBy = "System"
      };

      _logger.LogInformation($"? MessageHeader created: {messageId}");

         return messageHeader;
     }
        catch (Exception ex)
      {
                _logger.LogError(ex, "? Error creating message header");
      throw;
 }
        }

        public async Task<NphiesMessageHeaderEntity> GetMessageAsync(int id)
    {
            return await Task.FromResult<NphiesMessageHeaderEntity>(null);
        }

      public async Task UpdateMessageStatusAsync(int messageId, string status, string result = null)
        {
       try
  {
      _logger.LogInformation($"? Message {messageId} status updated to: {status}");
       }
            catch (Exception ex)
        {
      _logger.LogError(ex, $"? Error updating message status for ID: {messageId}");
                throw;
            }
        }

  private string GenerateMessageId()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
         return $"NPHIES-{timestamp}-{random}";
        }
    }
}
