using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    /// <summary>
    /// NPHIES Authorization Workflow Service
    /// Manages pre-authorization requests and responses
    /// Implements NPHIES pre-auth workflow requirements
    /// </summary>
    public interface INphiesAuthorizationWorkflowService
    {
        Task<AuthorizationResponse> RequestAuthorizationAsync(AuthorizationRequestDto request);
        Task<AuthorizationStatus> GetAuthorizationStatusAsync(string authorizationId);
        Task<bool> IsAuthorizationValidAsync(string authorizationId, DateTime serviceDate);
        Task<bool> ValidateClaimAgainstAuthAsync(string claimId, string authorizationId);
        Task<List<ActiveAuthorization>> GetActiveAuthorizationsAsync(string subscriberId);
        Task<bool> ExpireAuthorizationAsync(string authorizationId);
        Task<AuthorizationStatistics> GetAuthorizationStatisticsAsync(string providerId, DateTime? fromDate = null);
    }

    /// <summary>
    /// Authorization request DTO
    /// </summary>
    public class AuthorizationRequestDto
    {
        public string SubscriberId { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string ProcedureCode { get; set; } = string.Empty;
        public DateTime ProposedServiceDate { get; set; }
        public decimal EstimatedAmount { get; set; }
        public int UnitsRequested { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Authorization response DTO
    /// </summary>
    public class AuthorizationResponse
    {
        public bool Success { get; set; }
        public string AuthorizationId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // approved, denied, pending, expired
        public DateTime DecisionDate { get; set; } = DateTime.UtcNow;
        public int ApprovedUnits { get; set; }
        public decimal ApprovedAmount { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string DenialReason { get; set; } = string.Empty;
        public List<string> Conditions { get; set; } = new();
        public string ReferenceNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// Authorization status DTO
    /// </summary>
    public class AuthorizationStatus
    {
        public string AuthorizationId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SubscriberId { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsExpired { get; set; }
        public bool IsUsed { get; set; }
        public string LinkedClaimId { get; set; } = string.Empty;
        public int ApprovedUnits { get; set; }
        public int UnitsUsed { get; set; }
        public decimal ApprovedAmount { get; set; }
        public decimal AmountUsed { get; set; }
    }

    /// <summary>
    /// Active authorization DTO
    /// </summary>
    public class ActiveAuthorization
    {
        public string AuthorizationId { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string ProcedureCode { get; set; } = string.Empty;
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int ApprovedUnits { get; set; }
        public decimal ApprovedAmount { get; set; }
        public int RemainingUnits { get; set; }
        public decimal RemainingAmount { get; set; }
    }

    /// <summary>
    /// Authorization statistics DTO
    /// </summary>
    public class AuthorizationStatistics
    {
        public string ProviderId { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public int ApprovedCount { get; set; }
        public int DeniedCount { get; set; }
        public int PendingCount { get; set; }
        public int ExpiredCount { get; set; }
        public decimal TotalRequestedAmount { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public decimal ApprovalRate { get; set; }
        public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// NPHIES Authorization Workflow Service Implementation
    /// </summary>
    public class NphiesAuthorizationWorkflowService : INphiesAuthorizationWorkflowService
    {
        private readonly ILogger<NphiesAuthorizationWorkflowService> _logger;

        // In-memory storage (would use database in production)
        private readonly Dictionary<string, AuthorizationStatus> _authorizationStore = new();
        private readonly Dictionary<string, AuthorizationResponse> _authorizationResponses = new();
        private readonly List<AuthorizationRequestDto> _authorizationRequests = new();

        // Authorization validity period (default: 90 days)
        private readonly int _authValidityDays = 90;

        public NphiesAuthorizationWorkflowService(ILogger<NphiesAuthorizationWorkflowService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Requests authorization for a service
        /// </summary>
        public async Task<AuthorizationResponse> RequestAuthorizationAsync(AuthorizationRequestDto request)
        {
            try
            {
                _logger.LogInformation($"Requesting authorization for subscriber: {request.SubscriberId}, Service: {request.ServiceType}");

                if (request == null)
                {
                    return new AuthorizationResponse { Success = false, Status = "error" };
                }

                // Generate Authorization ID
                var authorizationId = GenerateAuthorizationId();
                var referenceNumber = GenerateReferenceNumber();

                // Create authorization response
                var response = new AuthorizationResponse
                {
                    Success = true,
                    AuthorizationId = authorizationId,
                    Status = "approved",
                    DecisionDate = DateTime.UtcNow,
                    ApprovedUnits = request.UnitsRequested,
                    ApprovedAmount = request.EstimatedAmount,
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.UtcNow.AddDays(_authValidityDays),
                    ReferenceNumber = referenceNumber,
                    Conditions = GetDefaultConditions(request.ServiceType)
                };

                // Create authorization status
                var status = new AuthorizationStatus
                {
                    AuthorizationId = authorizationId,
                    Status = response.Status,
                    SubscriberId = request.SubscriberId,
                    ServiceType = request.ServiceType,
                    ValidFrom = response.ValidFrom,
                    ValidTo = response.ValidTo,
                    IsExpired = false,
                    IsUsed = false,
                    ApprovedUnits = request.UnitsRequested,
                    UnitsUsed = 0,
                    ApprovedAmount = request.EstimatedAmount,
                    AmountUsed = 0
                };

                // Store authorization
                _authorizationStore[authorizationId] = status;
                _authorizationResponses[authorizationId] = response;
                _authorizationRequests.Add(request);

                _logger.LogInformation($"Authorization approved: {authorizationId}");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting authorization");
                return new AuthorizationResponse { Success = false, Status = "error" };
            }
        }

        /// <summary>
        /// Gets authorization status
        /// </summary>
        public async Task<AuthorizationStatus> GetAuthorizationStatusAsync(string authorizationId)
        {
            try
            {
                _logger.LogInformation($"Getting authorization status: {authorizationId}");

                if (string.IsNullOrWhiteSpace(authorizationId))
                {
                    return null;
                }

                if (_authorizationStore.TryGetValue(authorizationId, out var status))
                {
                    // Check if expired
                    status.IsExpired = DateTime.UtcNow > status.ValidTo;
                    return status;
                }

                _logger.LogWarning($"Authorization not found: {authorizationId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authorization status");
                return null;
            }
        }

        /// <summary>
        /// Validates if authorization is valid on service date
        /// </summary>
        public async Task<bool> IsAuthorizationValidAsync(string authorizationId, DateTime serviceDate)
        {
            try
            {
                _logger.LogInformation($"Validating authorization: {authorizationId}, ServiceDate: {serviceDate:yyyy-MM-dd}");

                var status = await GetAuthorizationStatusAsync(authorizationId);
                if (status == null)
                {
                    _logger.LogWarning("Authorization not found");
                    return false;
                }

                // Check if expired
                if (status.IsExpired || DateTime.UtcNow > status.ValidTo)
                {
                    _logger.LogWarning("Authorization expired");
                    return false;
                }

                // Check if service date is within validity period
                if (serviceDate < status.ValidFrom || serviceDate > status.ValidTo)
                {
                    _logger.LogWarning("Service date outside authorization validity period");
                    return false;
                }

                // Check if already used
                if (status.IsUsed && status.UnitsUsed >= status.ApprovedUnits)
                {
                    _logger.LogWarning("Authorization units exhausted");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating authorization");
                return false;
            }
        }

        /// <summary>
        /// Validates claim against authorization
        /// </summary>
        public async Task<bool> ValidateClaimAgainstAuthAsync(string claimId, string authorizationId)
        {
            try
            {
                _logger.LogInformation($"Validating claim: {claimId} against authorization: {authorizationId}");

                var status = await GetAuthorizationStatusAsync(authorizationId);
                if (status == null)
                {
                    return false;
                }

                // Check authorization is valid
                if (status.IsExpired)
                {
                    _logger.LogWarning("Authorization expired");
                    return false;
                }

                // Check units available
                int remainingUnits = status.ApprovedUnits - status.UnitsUsed;
                if (remainingUnits <= 0)
                {
                    _logger.LogWarning("No units remaining");
                    return false;
                }

                // Update usage
                status.LinkedClaimId = claimId;
                status.IsUsed = true;
                status.UnitsUsed += 1;

                _logger.LogInformation($"Claim validated against authorization successfully");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating claim against authorization");
                return false;
            }
        }

        /// <summary>
        /// Gets all active authorizations for subscriber
        /// </summary>
        public async Task<List<ActiveAuthorization>> GetActiveAuthorizationsAsync(string subscriberId)
        {
            try
            {
                _logger.LogInformation($"Getting active authorizations for subscriber: {subscriberId}");

                var active = _authorizationStore
                        .Where(kvp => kvp.Value.SubscriberId == subscriberId && !kvp.Value.IsExpired)
                     .Select(kvp =>
                    {
                        var status = kvp.Value;
                        return new ActiveAuthorization
                        {
                            AuthorizationId = status.AuthorizationId,
                            ServiceType = status.ServiceType,
                            ValidFrom = status.ValidFrom,
                            ValidTo = status.ValidTo,
                            ApprovedUnits = status.ApprovedUnits,
                            ApprovedAmount = status.ApprovedAmount,
                            RemainingUnits = status.ApprovedUnits - status.UnitsUsed,
                            RemainingAmount = status.ApprovedAmount - status.AmountUsed
                        };
                    })
                            .ToList();

                return active;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active authorizations");
                return new List<ActiveAuthorization>();
            }
        }

        /// <summary>
        /// Expires an authorization
        /// </summary>
        public async Task<bool> ExpireAuthorizationAsync(string authorizationId)
        {
            try
            {
                _logger.LogInformation($"Expiring authorization: {authorizationId}");

                if (_authorizationStore.TryGetValue(authorizationId, out var status))
                {
                    status.IsExpired = true;
                    status.ValidTo = DateTime.UtcNow;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring authorization");
                return false;
            }
        }

        /// <summary>
        /// Gets authorization statistics
        /// </summary>
        public async Task<AuthorizationStatistics> GetAuthorizationStatisticsAsync(string providerId, DateTime? fromDate = null)
        {
            try
            {
                _logger.LogInformation($"Getting authorization statistics for provider: {providerId}");

                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);

                var relevantAuths = _authorizationRequests
                    .Where(r => r.ProviderId == providerId && r.ProposedServiceDate >= from)
                  .ToList();

                var stats = new AuthorizationStatistics
                {
                    ProviderId = providerId,
                    TotalRequests = relevantAuths.Count,
                    TotalRequestedAmount = relevantAuths.Sum(r => r.EstimatedAmount),
                    ReportDate = DateTime.UtcNow
                };

                // Count statuses
                foreach (var request in relevantAuths)
                {
                    var auth = _authorizationRequests.FirstOrDefault(r => r == request);
                    if (auth != null)
                    {
                        stats.ApprovedCount++;
                        stats.TotalApprovedAmount += auth.EstimatedAmount;
                    }
                }

                // Calculate approval rate
                if (stats.TotalRequests > 0)
                {
                    stats.ApprovalRate = (decimal)stats.ApprovedCount / stats.TotalRequests;
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authorization statistics");
                return new AuthorizationStatistics { ProviderId = providerId };
            }
        }

        // Helper methods

        private string GenerateAuthorizationId()
        {
            return $"AUTH{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        private string GenerateReferenceNumber()
        {
            return $"REF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
        }

        private List<string> GetDefaultConditions(string serviceType)
        {
            return serviceType switch
            {
                "surgery" => new List<string>
  {
          "Post-operative follow-up required",
         "Discharge planning required",
           "Home health referral if needed"
      },
                "inpatient" => new List<string>
    {
         "Concurrent review required",
         "Discharge planning required",
    "Utilization review applies"
      },
                "physical_therapy" => new List<string>
         {
      "Reevaluation required after 15 visits",
         "Functional progress required",
          "Prior auth required for additional visits"
    },
                _ => new List<string>
{
         "Standard conditions apply"
         }
            };
        }
    }
}
