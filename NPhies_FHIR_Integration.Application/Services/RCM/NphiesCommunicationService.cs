using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    /// <summary>
    /// NPHIES Communication Service
    /// Handles incoming and outgoing FHIR Communication resources
    /// Implements NPHIES communication workflow requirements
    /// </summary>
    public interface INphiesCommunicationService
    {
        Task<CommunicationResponse> SendCommunicationAsync(CommunicationRequestDto request);
        Task<CommunicationMessage> GetCommunicationAsync(string communicationId);
        Task<List<CommunicationMessage>> GetCommunicationsForClaimAsync(string claimId);
        Task<bool> UpdateCommunicationStatusAsync(string communicationId, string status);
        Task<CommunicationStatistics> GetCommunicationStatisticsAsync(string providerId, DateTime? fromDate = null);
        Task<List<CommunicationMessage>> GetPendingCommunicationsAsync(string recipientId);
        Task<bool> MarkCommunicationAsReadAsync(string communicationId);
    }

    /// <summary>
    /// Communication request DTO
    /// </summary>
    public class CommunicationRequestDto
    {
        public string SenderId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string MessageContent { get; set; } = string.Empty;
        public string CommunicationType { get; set; } = string.Empty; // pre-auth-decision, claim-decision, payment-notification, status-update, document-request
        public string RelatedClaimId { get; set; } = string.Empty;
        public string RelatedAuthorizationId { get; set; } = string.Empty;
        public DateTime? SendDate { get; set; }
    }

    /// <summary>
    /// Communication response DTO
    /// </summary>
    public class CommunicationResponse
    {
        public bool Success { get; set; }
        public string CommunicationId { get; set; } = string.Empty;
        public DateTime SentDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "sent";
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Communication message DTO
    /// </summary>
    public class CommunicationMessage
    {
        public string CommunicationId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string MessageContent { get; set; } = string.Empty;
        public string CommunicationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // sent, delivered, read, failed
        public DateTime SentDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? ReadDate { get; set; }
        public string RelatedClaimId { get; set; } = string.Empty;
        public string RelatedAuthorizationId { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }

    /// <summary>
    /// Communication statistics DTO
    /// </summary>
    public class CommunicationStatistics
    {
        public string ProviderId { get; set; } = string.Empty;
        public int TotalSent { get; set; }
        public int TotalReceived { get; set; }
        public int Delivered { get; set; }
        public int Read { get; set; }
        public int Failed { get; set; }
        public int Pending { get; set; }
        public double AverageDeliveryTimeMinutes { get; set; }
        public double AverageReadTimeMinutes { get; set; }
        public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// NPHIES Communication Service Implementation
    /// </summary>
    public class NphiesCommunicationService : INphiesCommunicationService
    {
        private readonly ILogger<NphiesCommunicationService> _logger;

        // In-memory storage (would use database in production)
        private readonly Dictionary<string, CommunicationMessage> _communications = new();
        private readonly List<CommunicationMessage> _allMessages = new();

        public NphiesCommunicationService(ILogger<NphiesCommunicationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Sends a communication
        /// </summary>
        public async Task<CommunicationResponse> SendCommunicationAsync(CommunicationRequestDto request)
        {
            try
            {
                _logger.LogInformation($"Sending communication from {request.SenderId} to {request.RecipientId}");

                if (request == null)
                {
                    return new CommunicationResponse { Success = false, Message = "Request is required" };
                }

                // Generate Communication ID
                var communicationId = GenerateCommunicationId();
                var sendDate = request.SendDate ?? DateTime.UtcNow;

                // Create communication message
                var message = new CommunicationMessage
                {
                    CommunicationId = communicationId,
                    SenderId = request.SenderId,
                    RecipientId = request.RecipientId,
                    Subject = request.Subject,
                    MessageContent = request.MessageContent,
                    CommunicationType = request.CommunicationType,
                    Status = "sent",
                    SentDate = sendDate,
                    DeliveredDate = sendDate.AddMinutes(5), // Simulating delivery
                    RelatedClaimId = request.RelatedClaimId,
                    RelatedAuthorizationId = request.RelatedAuthorizationId,
                    IsRead = false
                };

                // Store communication
                _communications[communicationId] = message;
                _allMessages.Add(message);

                _logger.LogInformation($"Communication sent successfully: {communicationId}");

                return new CommunicationResponse
                {
                    Success = true,
                    CommunicationId = communicationId,
                    SentDate = sendDate,
                    Status = "sent",
                    Message = "Communication sent successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending communication");
                return new CommunicationResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        /// <summary>
        /// Gets a communication by ID
        /// </summary>
        public async Task<CommunicationMessage> GetCommunicationAsync(string communicationId)
        {
            try
            {
                _logger.LogInformation($"Getting communication: {communicationId}");

                if (string.IsNullOrWhiteSpace(communicationId))
                {
                    return null;
                }

                if (_communications.TryGetValue(communicationId, out var message))
                {
                    return message;
                }

                _logger.LogWarning($"Communication not found: {communicationId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting communication");
                return null;
            }
        }

        /// <summary>
        /// Gets all communications for a claim
        /// </summary>
        public async Task<List<CommunicationMessage>> GetCommunicationsForClaimAsync(string claimId)
        {
            try
            {
                _logger.LogInformation($"Getting communications for claim: {claimId}");

                var communications = _allMessages
                 .Where(m => m.RelatedClaimId == claimId)
            .OrderByDescending(m => m.SentDate)
                .ToList();

                return communications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting communications for claim");
                return new List<CommunicationMessage>();
            }
        }

        /// <summary>
        /// Updates communication status
        /// </summary>
        public async Task<bool> UpdateCommunicationStatusAsync(string communicationId, string status)
        {
            try
            {
                _logger.LogInformation($"Updating communication status: {communicationId}, Status: {status}");

                if (_communications.TryGetValue(communicationId, out var message))
                {
                    message.Status = status;

                    if (status == "delivered")
                    {
                        message.DeliveredDate = DateTime.UtcNow;
                    }
                    else if (status == "read")
                    {
                        message.ReadDate = DateTime.UtcNow;
                        message.IsRead = true;
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating communication status");
                return false;
            }
        }

        /// <summary>
        /// Gets communication statistics for provider
        /// </summary>
        public async Task<CommunicationStatistics> GetCommunicationStatisticsAsync(string providerId, DateTime? fromDate = null)
        {
            try
            {
                _logger.LogInformation($"Getting communication statistics for provider: {providerId}");

                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);

                var relevantComms = _allMessages
          .Where(m => (m.SenderId == providerId || m.RecipientId == providerId) && m.SentDate >= from)
         .ToList();

                var stats = new CommunicationStatistics
                {
                    ProviderId = providerId,
                    TotalSent = relevantComms.Count(m => m.SenderId == providerId),
                    TotalReceived = relevantComms.Count(m => m.RecipientId == providerId),
                    Delivered = relevantComms.Count(m => m.Status == "delivered"),
                    Read = relevantComms.Count(m => m.IsRead),
                    Failed = relevantComms.Count(m => m.Status == "failed"),
                    Pending = relevantComms.Count(m => m.Status == "sent"),
                    ReportDate = DateTime.UtcNow
                };

                // Calculate average delivery time
                var deliveredComms = relevantComms.Where(m => m.DeliveredDate.HasValue).ToList();
                if (deliveredComms.Count > 0)
                {
                    var totalDeliveryTime = deliveredComms.Sum(m => (m.DeliveredDate.Value - m.SentDate).TotalMinutes);
                    stats.AverageDeliveryTimeMinutes = totalDeliveryTime / deliveredComms.Count;
                }

                // Calculate average read time
                var readComms = relevantComms.Where(m => m.ReadDate.HasValue).ToList();
                if (readComms.Count > 0)
                {
                    var totalReadTime = readComms.Sum(m => (m.ReadDate.Value - m.SentDate).TotalMinutes);
                    stats.AverageReadTimeMinutes = totalReadTime / readComms.Count;
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting communication statistics");
                return new CommunicationStatistics { ProviderId = providerId };
            }
        }

        /// <summary>
        /// Gets pending communications for recipient
        /// </summary>
        public async Task<List<CommunicationMessage>> GetPendingCommunicationsAsync(string recipientId)
        {
            try
            {
                _logger.LogInformation($"Getting pending communications for recipient: {recipientId}");

                var pending = _allMessages
          .Where(m => m.RecipientId == recipientId && !m.IsRead)
   .OrderByDescending(m => m.SentDate)
             .ToList();

                return pending;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending communications");
                return new List<CommunicationMessage>();
            }
        }

        /// <summary>
        /// Marks communication as read
        /// </summary>
        public async Task<bool> MarkCommunicationAsReadAsync(string communicationId)
        {
            try
            {
                _logger.LogInformation($"Marking communication as read: {communicationId}");

                if (_communications.TryGetValue(communicationId, out var message))
                {
                    message.IsRead = true;
                    message.Status = "read";
                    message.ReadDate = DateTime.UtcNow;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking communication as read");
                return false;
            }
        }

        // Helper methods

        private string GenerateCommunicationId()
        {
            return $"COMM{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }
}
