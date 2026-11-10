using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    /// <summary>
    /// NPHIES Request Acknowledgment Service
    /// Handles async claim submission with Task-based acknowledgment
    /// Implements NPHIES async workflow requirements
    /// </summary>
    public interface INphiesRequestAcknowledgmentService
    {
        Task<AcknowledgmentResponse> AcknowledgeClaimSubmissionAsync(ClaimSubmissionDto claim);
        Task<SubmissionTask> GetSubmissionTaskAsync(string transactionId);
        Task<SubmissionStatus> GetSubmissionStatusAsync(string transactionId);
        Task<bool> UpdateSubmissionStatusAsync(string transactionId, string status, string details = "");
        Task<List<SubmissionTracking>> GetProviderSubmissionsAsync(string providerId, int pageSize = 50);
        Task<SubmissionStatistics> GetSubmissionStatisticsAsync(string providerId, DateTime? fromDate = null, DateTime? toDate = null);
    }

    /// <summary>
    /// Claim submission DTO
    /// </summary>
    public class ClaimSubmissionDto
    {
        public string ClaimId { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string InsurerId { get; set; } = string.Empty;
        public string ClaimType { get; set; } = string.Empty;
        public DateTime ServiceDate { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Acknowledgment response DTO
    /// </summary>
    public class AcknowledgmentResponse
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public DateTime SubmissionTime { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "accepted"; // accepted, queued, processing
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Submission task DTO
    /// </summary>
    public class SubmissionTask
    {
        public string TaskId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ClaimId { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<TaskInput> Inputs { get; set; } = new();
    }

    /// <summary>
    /// Task input DTO
    /// </summary>
    public class TaskInput
    {
        public string ParameterName { get; set; } = string.Empty;
        public string ParameterValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Submission status DTO
    /// </summary>
    public class SubmissionStatus
    {
        public string TransactionId { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ClaimId { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string StatusDetails { get; set; } = string.Empty;
        public int ProcessingTimeMinutes { get; set; }
    }

    /// <summary>
    /// Submission tracking DTO
    /// </summary>
    public class SubmissionTracking
    {
        public string TransactionId { get; set; } = string.Empty;
        public string ClaimId { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public decimal ClaimAmount { get; set; }
    }

    /// <summary>
    /// Submission statistics DTO
    /// </summary>
    public class SubmissionStatistics
    {
        public string ProviderId { get; set; } = string.Empty;
        public int TotalSubmissions { get; set; }
        public int AcceptedSubmissions { get; set; }
        public int ProcessingSubmissions { get; set; }
        public int CompletedSubmissions { get; set; }
        public int FailedSubmissions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public double AverageProcessingTimeMinutes { get; set; }
        public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// NPHIES Request Acknowledgment Service Implementation
    /// </summary>
    public class NphiesRequestAcknowledgmentService : INphiesRequestAcknowledgmentService
    {
        private readonly ILogger<NphiesRequestAcknowledgmentService> _logger;

        // In-memory storage (would use database in production)
        private readonly Dictionary<string, SubmissionStatus> _submissionTracker = new();
        private readonly Dictionary<string, SubmissionTask> _taskTracker = new();
        private readonly List<SubmissionTracking> _allSubmissions = new();

        public NphiesRequestAcknowledgmentService(ILogger<NphiesRequestAcknowledgmentService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates acknowledgment for claim submission with Task resource
        /// </summary>
        public async Task<AcknowledgmentResponse> AcknowledgeClaimSubmissionAsync(ClaimSubmissionDto claim)
        {
            try
            {
                _logger.LogInformation($"Creating acknowledgment for claim submission: {claim.ClaimId}");

                if (claim == null)
                {
                    return new AcknowledgmentResponse
                    {
                        Success = false,
                        Message = "Claim is required"
                    };
                }

                // Generate Transaction ID and Task ID
                var transactionId = GenerateTransactionId();
                var taskId = GenerateTaskId();

                // Create Task for async processing
                var task = CreateSubmissionTask(claim, taskId, transactionId);

                // Create submission status tracking
                var submissionStatus = new SubmissionStatus
                {
                    TransactionId = transactionId,
                    TaskId = taskId,
                    Status = "accepted",
                    ClaimId = claim.ClaimId,
                    ProviderId = claim.ProviderId,
                    SubmittedDate = DateTime.UtcNow,
                    StatusDetails = "Claim submitted successfully and accepted for processing"
                };

                // Store tracking information
                _submissionTracker[transactionId] = submissionStatus;
                _taskTracker[taskId] = task;

                _allSubmissions.Add(new SubmissionTracking
                {
                    TransactionId = transactionId,
                    ClaimId = claim.ClaimId,
                    ProviderId = claim.ProviderId,
                    PatientId = claim.PatientId,
                    SubmittedDate = DateTime.UtcNow,
                    Status = "accepted",
                    TaskId = taskId,
                    ClaimAmount = claim.Amount
                });

                _logger.LogInformation($"Acknowledgment created. Transaction ID: {transactionId}, Task ID: {taskId}");

                return new AcknowledgmentResponse
                {
                    Success = true,
                    TransactionId = transactionId,
                    TaskId = taskId,
                    SubmissionTime = DateTime.UtcNow,
                    Status = "accepted",
                    Message = "Claim received and queued for processing",
                    Metadata = new Dictionary<string, object>
   {
      { "claimId", claim.ClaimId },
       { "submissionTime", DateTime.UtcNow },
   { "expectedProcessingTime", "2-5 business days" },
      { "statusCheckUrl", $"/api/submissions/{transactionId}/status" },
       { "webhookUrl", $"/api/webhooks/submission/{transactionId}" }
      }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating acknowledgment");
                return new AcknowledgmentResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets submission task by transaction ID
        /// </summary>
        public async Task<SubmissionTask> GetSubmissionTaskAsync(string transactionId)
        {
            try
            {
                _logger.LogInformation($"Retrieving submission task for transaction: {transactionId}");

                if (string.IsNullOrWhiteSpace(transactionId))
                {
                    return null;
                }

                if (_submissionTracker.TryGetValue(transactionId, out var submission))
                {
                    if (_taskTracker.TryGetValue(submission.TaskId, out var task))
                    {
                        return task;
                    }
                }

                _logger.LogWarning($"Task not found for transaction: {transactionId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submission task");
                return null;
            }
        }

        /// <summary>
        /// Gets current submission status
        /// </summary>
        public async Task<SubmissionStatus> GetSubmissionStatusAsync(string transactionId)
        {
            try
            {
                _logger.LogInformation($"Getting submission status for transaction: {transactionId}");

                if (string.IsNullOrWhiteSpace(transactionId))
                {
                    return null;
                }

                if (_submissionTracker.TryGetValue(transactionId, out var status))
                {
                    // Calculate processing time
                    if (status.ProcessedDate.HasValue)
                    {
                        status.ProcessingTimeMinutes = (int)(status.ProcessedDate.Value - status.SubmittedDate).TotalMinutes;
                    }
                    return status;
                }

                _logger.LogWarning($"Submission not found: {transactionId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission status");
                return null;
            }
        }

        /// <summary>
        /// Updates submission status (called by claim processing workflow)
        /// </summary>
        public async Task<bool> UpdateSubmissionStatusAsync(string transactionId, string status, string details = "")
        {
            try
            {
                _logger.LogInformation($"Updating submission status - Transaction: {transactionId}, Status: {status}");

                if (!_submissionTracker.TryGetValue(transactionId, out var submission))
                {
                    _logger.LogWarning($"Submission not found: {transactionId}");
                    return false;
                }

                submission.Status = status;
                submission.StatusDetails = details;

                if (status == "completed" || status == "processed" || status == "denied" || status == "failed")
                {
                    submission.ProcessedDate = DateTime.UtcNow;
                }

                // Update tracking
                var tracking = _allSubmissions.FirstOrDefault(s => s.TransactionId == transactionId);
                if (tracking != null)
                {
                    tracking.Status = status;
                }

                _logger.LogInformation($"Submission status updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating submission status");
                return false;
            }
        }

        /// <summary>
        /// Gets all submissions for a provider
        /// </summary>
        public async Task<List<SubmissionTracking>> GetProviderSubmissionsAsync(string providerId, int pageSize = 50)
        {
            try
            {
                _logger.LogInformation($"Retrieving submissions for provider: {providerId}");

                var submissions = _allSubmissions
                               .Where(s => s.ProviderId == providerId)
                         .OrderByDescending(s => s.SubmittedDate)
             .Take(pageSize)
                        .ToList();

                return submissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submissions");
                return new List<SubmissionTracking>();
            }
        }

        /// <summary>
        /// Gets submission statistics for provider
        /// </summary>
        public async Task<SubmissionStatistics> GetSubmissionStatisticsAsync(string providerId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                _logger.LogInformation($"Calculating submission statistics for provider: {providerId}");

                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                var submissions = _allSubmissions
             .Where(s => s.ProviderId == providerId
 && s.SubmittedDate >= from
   && s.SubmittedDate <= to)
 .ToList();

                var stats = new SubmissionStatistics
                {
                    ProviderId = providerId,
                    TotalSubmissions = submissions.Count,
                    AcceptedSubmissions = submissions.Count(s => s.Status == "accepted"),
                    ProcessingSubmissions = submissions.Count(s => s.Status == "processing"),
                    CompletedSubmissions = submissions.Count(s => s.Status == "completed" || s.Status == "processed"),
                    FailedSubmissions = submissions.Count(s => s.Status == "denied" || s.Status == "failed"),
                    TotalAmount = submissions.Sum(s => s.ClaimAmount),
                    ApprovedAmount = submissions
               .Where(s => s.Status == "completed" || s.Status == "processed")
                    .Sum(s => s.ClaimAmount),
                    ReportDate = DateTime.UtcNow
                };

                // Calculate average processing time
                var completedSubmissions = _allSubmissions
        .Where(s => s.ProviderId == providerId && _submissionTracker.ContainsKey(s.TransactionId))
    .Where(s => _submissionTracker[s.TransactionId].ProcessedDate.HasValue)
               .ToList();

                if (completedSubmissions.Count > 0)
                {
                    var totalMinutes = completedSubmissions.Sum(s =>
                            {
                                if (_submissionTracker.TryGetValue(s.TransactionId, out var status))
                                {
                                    return (status.ProcessedDate.Value - status.SubmittedDate).TotalMinutes;
                                }
                                return 0;
                            });
                    stats.AverageProcessingTimeMinutes = totalMinutes / completedSubmissions.Count;
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating submission statistics");
                return new SubmissionStatistics { ProviderId = providerId };
            }
        }

        // Helper methods

        private string GenerateTransactionId()
        {
            return $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        private string GenerateTaskId()
        {
            return $"TASK-{Guid.NewGuid()}";
        }

        private SubmissionTask CreateSubmissionTask(ClaimSubmissionDto claim, string taskId, string transactionId)
        {
            return new SubmissionTask
            {
                TaskId = taskId,
                TransactionId = transactionId,
                Status = "requested",
                ClaimId = claim.ClaimId,
                CreatedDate = DateTime.UtcNow,
                Description = $"Processing claim {claim.ClaimId} from transaction {transactionId}",
                Inputs = new List<TaskInput>
         {
           new TaskInput { ParameterName = "ClaimId", ParameterValue = claim.ClaimId },
                 new TaskInput { ParameterName = "TransactionId", ParameterValue = transactionId },
  new TaskInput { ParameterName = "ProviderId", ParameterValue = claim.ProviderId },
       new TaskInput { ParameterName = "PatientId", ParameterValue = claim.PatientId },
           new TaskInput { ParameterName = "InsurerId", ParameterValue = claim.InsurerId },
        new TaskInput { ParameterName = "ClaimAmount", ParameterValue = claim.Amount.ToString("F2") }
      }
            };
        }
    }
}
