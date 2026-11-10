using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Application.Services.Masters;
using NPhies_FHIR_Integration.Infrastructure.Repositories;

namespace NPhies_FHIR_Integration.Application.Services.RCM;

/// <summary>
/// Appeal Service Implementation
/// Manages appeal workflow for denied claims
/// </summary>
public class AppealService : IAppealService
{
    private readonly ILogger<AppealService> _logger;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IAppealRepository _appealRepository;

    public AppealService(
        ILogger<AppealService> logger,
        IErrorCodeService errorCodeService,
        IAppealRepository appealRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorCodeService = errorCodeService ?? throw new ArgumentNullException(nameof(errorCodeService));
        _appealRepository = appealRepository ?? throw new ArgumentNullException(nameof(appealRepository));
    }

    // ========== APPEAL CREATION ==========

    /// <summary>
    /// Create a new appeal for a denied claim
    /// </summary>
    public async Task<CreateAppealResult> CreateAppealAsync(
            CreateAppealRequest request,
            CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating appeal for claim {ClaimId}, error code {ErrorCode}",
            request.ClaimId, request.ErrorCodeBeingAppealed);

        try
        {
            // Validate request
            if (string.IsNullOrEmpty(request.ClaimId))
            {
                return new CreateAppealResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Claim ID is required"
                };
            }

            if (string.IsNullOrEmpty(request.ErrorCodeBeingAppealed))
            {
                return new CreateAppealResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Error code is required"
                };
            }

            // Get error code details
            var errorCode = await _errorCodeService.GetErrorCodeAsync(
                     request.ErrorCodeBeingAppealed);

            if (errorCode == null)
            {
                return new CreateAppealResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Error code {request.ErrorCodeBeingAppealed} not found"
                };
            }

            // Check if error code allows appeal
            if (!errorCode.AllowsAppeal)
            {
                return new CreateAppealResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Error code {request.ErrorCodeBeingAppealed} does not allow appeals"
                };
            }

            // Create appeal entity
            var appeal = new AppealRequest
            {
                AppealNumber = GenerateAppealNumber(),
                AppealIdentifierSystem = "http://nphies.sa/identifier/appeal-id",
                AppealIdentifierValue = Guid.NewGuid().ToString(),
                ClaimId = request.ClaimId,
                ClaimResponseId = request.ClaimResponseId,
                PatientId = request.PatientId,
                InsurerId = request.InsurerId,
                ProviderId = request.ProviderId,
                ErrorCodeBeingAppealed = request.ErrorCodeBeingAppealed,
                ErrorDescription = errorCode.ErrorDescription,
                AppealReason = request.AppealReason,
                SupportingDocumentation = request.SupportingDocumentation,
                DenialDate = DateTime.UtcNow,
                AppealDeadlineDate = DateTime.UtcNow.AddDays(errorCode.StandardAppealDays),
                AppealStatus = "draft",
                AppealLevel = 1,
                IsActive = true,
                AllowsEscalation = true,
                InternalReferenceNumber = Guid.NewGuid().ToString("N").Substring(0, 20).ToUpper()
            };

            // Add initial status history
            appeal.StatusHistory.Add(new AppealStatusHistory
            {
                Status = "draft",
                ChangedBy = "system",
                ChangeReason = "Appeal created",
                StatusChangeDate = DateTime.UtcNow,
                Comments = "Initial appeal created"
            });

            // Save appeal
            var savedAppeal = await _appealRepository.AddAsync(appeal, cancellationToken);

            _logger.LogInformation("Appeal {AppealNumber} created with deadline {Deadline}",
         savedAppeal.AppealNumber, savedAppeal.AppealDeadlineDate);

            return new CreateAppealResult
            {
                IsSuccess = true,
                AppealId = savedAppeal.Id,
                AppealNumber = savedAppeal.AppealNumber,
                DeadlineDate = savedAppeal.AppealDeadlineDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appeal for claim {ClaimId}", request.ClaimId);
            return new CreateAppealResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Get appeal by ID
    /// </summary>
    public async Task<AppealDto?> GetAppealAsync(
        string appealId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving appeal {AppealId}", appealId);

        try
        {
            var appeal = await _appealRepository.GetByIdAsync(appealId, cancellationToken);
            if (appeal == null)
            {
                _logger.LogWarning("Appeal {AppealId} not found", appealId);
                return null;
            }

            return MapToDto(appeal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appeal {AppealId}", appealId);
            return null;
        }
    }

    /// <summary>
    /// Get all appeals for a claim
    /// </summary>
    public async Task<List<AppealDto>> GetClaimAppealsAsync(
        string claimId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving appeals for claim {ClaimId}", claimId);

        try
        {
            var appeals = await _appealRepository.GetByClaimIdAsync(claimId, cancellationToken);
            return appeals.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appeals for claim {ClaimId}", claimId);
            return new List<AppealDto>();
        }
    }

    /// <summary>
    /// Get all appeals for a patient
    /// </summary>
    public async Task<List<AppealDto>> GetPatientAppealsAsync(
        string patientId,
   CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving appeals for patient {PatientId}", patientId);

        try
        {
            var appeals = await _appealRepository.GetByPatientIdAsync(patientId, cancellationToken);
            return appeals.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appeals for patient {PatientId}", patientId);
            return new List<AppealDto>();
        }
    }

    // ========== APPEAL SUBMISSION ==========

    /// <summary>
    /// Submit an appeal (finalize before deadline)
    /// </summary>
    public async Task<SubmitAppealResult> SubmitAppealAsync(
        string appealId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting appeal {AppealId}", appealId);

        try
        {
            var appeal = await _appealRepository.GetByIdAsync(appealId, cancellationToken);
            if (appeal == null)
            {
                return new SubmitAppealResult
                {
                    IsSuccess = false,
                    Message = $"Appeal {appealId} not found"
                };
            }

            // Check if within deadline
            if (!appeal.IsWithinDeadline())
            {
                return new SubmitAppealResult
                {
                    IsSuccess = false,
                    Message = "Appeal deadline has passed"
                };
            }

            // Update status
            appeal.AppealStatus = "submitted";
            appeal.AppealSubmittedDate = DateTime.UtcNow;
            appeal.LastStatusUpdateDate = DateTime.UtcNow;

            // Add status history
            await _appealRepository.AddStatusHistoryAsync(
        appealId,
     new AppealStatusHistory
     {
         Status = "submitted",
         ChangedBy = "provider",
         ChangeReason = "Appeal submitted to insurer",
         StatusChangeDate = DateTime.UtcNow,
         Comments = $"Appeal submitted on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
     },
      cancellationToken);

            // Save changes
            await _appealRepository.UpdateAsync(appeal, cancellationToken);

            _logger.LogInformation("Appeal {AppealId} submitted successfully", appealId);

            return new SubmitAppealResult
            {
                IsSuccess = true,
                AppealId = appealId,
                SubmittedDate = DateTime.UtcNow,
                ExpectedDecisionDate = DateTime.UtcNow.AddDays(30),
                ConfirmationNumber = appeal.InternalReferenceNumber,
                Message = "Appeal submitted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting appeal {AppealId}", appealId);
            return new SubmitAppealResult
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Update appeal with additional information
    /// </summary>
    public async Task<UpdateAppealResult> UpdateAppealAsync(
        UpdateAppealRequest request,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating appeal {AppealId}", request.AppealId);

        try
        {
            var appeal = await _appealRepository.GetByIdAsync(request.AppealId, cancellationToken);
            if (appeal == null)
            {
                return new UpdateAppealResult
                {
                    IsSuccess = false,
                    Message = $"Appeal {request.AppealId} not found"
                };
            }

            // Update fields
            if (!string.IsNullOrEmpty(request.AppealReason))
                appeal.AppealReason = request.AppealReason;

            if (!string.IsNullOrEmpty(request.SupportingDocumentation))
                appeal.SupportingDocumentation = request.SupportingDocumentation;

            if (!string.IsNullOrEmpty(request.Notes))
                appeal.Notes = request.Notes;

            appeal.LastStatusUpdateDate = DateTime.UtcNow;
            appeal.UpdatedAt = DateTime.UtcNow;

            await _appealRepository.UpdateAsync(appeal, cancellationToken);

            _logger.LogInformation("Appeal {AppealId} updated successfully", request.AppealId);

            return new UpdateAppealResult
            {
                IsSuccess = true,
                AppealId = request.AppealId,
                Message = "Appeal updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appeal {AppealId}", request.AppealId);
            return new UpdateAppealResult
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Withdraw an appeal
    /// </summary>
    public async Task<WithdrawAppealResult> WithdrawAppealAsync(
        string appealId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Withdrawing appeal {AppealId}, reason: {Reason}", appealId, reason);

        try
        {
            await _appealRepository.MarkAsWithdrawnAsync(appealId, reason, cancellationToken);

            // Add status history
            await _appealRepository.AddStatusHistoryAsync(
       appealId,
      new AppealStatusHistory
      {
          Status = "withdrawn",
          ChangedBy = "provider",
          ChangeReason = reason,
          StatusChangeDate = DateTime.UtcNow
      },
  cancellationToken);

            _logger.LogInformation("Appeal {AppealId} withdrawn successfully", appealId);

            return new WithdrawAppealResult
            {
                IsSuccess = true,
                AppealId = appealId,
                WithdrawnDate = DateTime.UtcNow,
                Message = "Appeal withdrawn successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error withdrawing appeal {AppealId}", appealId);
            return new WithdrawAppealResult
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    // ========== APPEAL ESCALATION ==========

    /// <summary>
    /// Escalate appeal to next level
    /// </summary>
    public async Task<EscalateAppealResult> EscalateAppealAsync(
        string appealId,
        string escalationReason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Escalating appeal {AppealId}: {Reason}", appealId, escalationReason);

        try
        {
            var appeal = await _appealRepository.GetByIdAsync(appealId, cancellationToken);
            if (appeal == null)
            {
                return new EscalateAppealResult
                {
                    IsSuccess = false,
                    Message = $"Appeal {appealId} not found"
                };
            }

            if (!appeal.AllowsEscalation)
            {
                return new EscalateAppealResult
                {
                    IsSuccess = false,
                    Message = "This appeal cannot be escalated further"
                };
            }

            if (appeal.AppealLevel >= 3)
            {
                return new EscalateAppealResult
                {
                    IsSuccess = false,
                    Message = "Appeal already at maximum level"
                };
            }

            // Create new appeal at next level
            var escalatedAppeal = new AppealRequest
            {
                AppealNumber = GenerateAppealNumber(),
                AppealIdentifierSystem = appeal.AppealIdentifierSystem,
                AppealIdentifierValue = Guid.NewGuid().ToString(),
                ClaimId = appeal.ClaimId,
                ClaimResponseId = appeal.ClaimResponseId,
                PatientId = appeal.PatientId,
                InsurerId = appeal.InsurerId,
                ProviderId = appeal.ProviderId,
                ErrorCodeBeingAppealed = appeal.ErrorCodeBeingAppealed,
                ErrorDescription = appeal.ErrorDescription,
                AppealReason = $"Escalated from Level {appeal.AppealLevel}: {escalationReason}",
                DenialDate = appeal.DenialDate,
                AppealDeadlineDate = DateTime.UtcNow.AddDays(60), // Extended for escalation
                AppealStatus = "draft",
                AppealLevel = appeal.AppealLevel + 1,
                IsActive = true,
                AllowsEscalation = appeal.AppealLevel < 2 // Allow further escalation only up to Level 2
            };

            var savedEscalatedAppeal = await _appealRepository.AddAsync(escalatedAppeal, cancellationToken);

            // Link escalated appeal to original
            appeal.EscalatedAppealId = savedEscalatedAppeal.Id;
            await _appealRepository.UpdateAsync(appeal, cancellationToken);

            // Add history
            await _appealRepository.AddStatusHistoryAsync(
            appealId,
                     new AppealStatusHistory
                     {
                         Status = "escalated",
                         ChangedBy = "provider",
                         ChangeReason = escalationReason,
                         StatusChangeDate = DateTime.UtcNow,
                         Comments = $"Escalated to Level {escalatedAppeal.AppealLevel} - {savedEscalatedAppeal.AppealNumber}"
                     },
              cancellationToken);

            _logger.LogInformation("Appeal {AppealId} escalated to {NewAppealId}", appealId, savedEscalatedAppeal.Id);

            return new EscalateAppealResult
            {
                IsSuccess = true,
                EscalatedAppealId = savedEscalatedAppeal.Id,
                NewAppealLevel = escalatedAppeal.AppealLevel,
                NewDeadlineDate = escalatedAppeal.AppealDeadlineDate,
                Message = $"Appeal escalated to Level {escalatedAppeal.AppealLevel}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating appeal {AppealId}", appealId);
            return new EscalateAppealResult
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    // ========== APPEAL STATUS & TIMELINE ==========

    /// <summary>
    /// Get appeal status and timeline
    /// </summary>
    public async Task<AppealStatusDto> GetAppealStatusAsync(
     string appealId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting status for appeal {AppealId}", appealId);

        try
        {
            var appeal = await _appealRepository.GetByIdAsync(appealId, cancellationToken);
            if (appeal == null)
            {
                return new AppealStatusDto { AppealId = appealId };
            }

            var history = await _appealRepository.GetStatusHistoryAsync(appealId, cancellationToken);

            return new AppealStatusDto
            {
                AppealId = appealId,
                CurrentStatus = appeal.AppealStatus,
                DenialDate = appeal.DenialDate,
                DeadlineDate = appeal.AppealDeadlineDate,
                SubmittedDate = appeal.AppealSubmittedDate,
                ReceivedDate = appeal.ReceivedDate,
                ReviewCompletedDate = appeal.ReviewCompletedDate,
                ExpectedDecisionDate = appeal.ExpectedDecisionDate,
                StatusHistory = history.Select(h => new StatusChangeDto
                {
                    Status = h.Status,
                    ChangedDate = h.StatusChangeDate,
                    ChangedBy = h.ChangedBy,
                    Reason = h.ChangeReason
                }).ToList(),
                DaysRemainingToAppeal = appeal.DaysRemainingToAppeal(),
                DaysSinceSubmission = appeal.AppealSubmittedDate.HasValue ?
                    (int)(DateTime.UtcNow - appeal.AppealSubmittedDate.Value).TotalDays : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appeal status for {AppealId}", appealId);
            return new AppealStatusDto { AppealId = appealId };
        }
    }

    /// <summary>
    /// Get appeals nearing deadline
    /// </summary>
    public async Task<List<AppealDto>> GetAppealsNearingDeadlineAsync(
        int daysThreshold = 5,
      CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving appeals nearing deadline (threshold: {Days} days)", daysThreshold);

        try
        {
            var appeals = await _appealRepository.GetAppealsNearingDeadlineAsync(daysThreshold, cancellationToken);
            return appeals.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appeals nearing deadline");
            return new List<AppealDto>();
        }
    }

    /// <summary>
    /// Get appeal statistics
    /// </summary>
    public async Task<AppealStatisticsDto> GetAppealStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving appeal statistics");

        try
        {
            var totalAppeals = await _appealRepository.GetTotalCountAsync(cancellationToken);
            var activeAppeals = (await _appealRepository.GetActiveAppealsAsync(cancellationToken)).Count;
            var approvedAppeals = await _appealRepository.GetCountByStatusAsync("approved", cancellationToken);
            var deniedAppeals = await _appealRepository.GetCountByStatusAsync("denied", cancellationToken);
            var partialAppeals = await _appealRepository.GetCountByStatusAsync("partial", cancellationToken);
            var withdrawnAppeals = await _appealRepository.GetCountByStatusAsync("withdrawn", cancellationToken);
            var approvalRate = await _appealRepository.GetApprovalRateAsync(cancellationToken);
            var totalAmountApproved = await _appealRepository.GetTotalApprovedAmountAsync(cancellationToken);
            var apprealNearDeadline = (await _appealRepository.GetAppealsNearingDeadlineAsync(5, cancellationToken)).Count;

            return new AppealStatisticsDto
            {
                TotalAppeals = totalAppeals,
                ActiveAppeals = activeAppeals,
                ApprovedAppeals = approvedAppeals,
                DeniedAppeals = deniedAppeals,
                PartialAppeals = partialAppeals,
                WithdrawnAppeals = withdrawnAppeals,
                AverageApprovalRate = approvalRate,
                TotalAmountApproved = totalAmountApproved,
                AppealsNearingDeadline = apprealNearDeadline
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appeal statistics");
            return new AppealStatisticsDto();
        }
    }

    // ========== DOCUMENTS ==========

    /// <summary>
    /// Attach document to appeal
    /// </summary>
    public async Task<AttachDocumentResult> AttachDocumentAsync(
        AttachDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attaching document to appeal {AppealId}", request.AppealId);

        try
        {
            var document = new AppealDocument
            {
                DocumentType = request.DocumentType,
                DocumentTitle = request.DocumentTitle,
                DocumentDescription = request.DocumentDescription,
                FilePath = request.FilePath,
                FileSizeBytes = request.FileSizeBytes,
                MimeType = request.MimeType,
                AttachedDate = DateTime.UtcNow,
                IsVerified = false,
                Notes = request.Notes,
                IsActive = true
            };

            await _appealRepository.AddDocumentAsync(request.AppealId, document, cancellationToken);

            _logger.LogInformation("Document {DocumentTitle} attached to appeal {AppealId}",
        request.DocumentTitle, request.AppealId);

            return new AttachDocumentResult
            {
                IsSuccess = true,
                DocumentId = document.Id.ToString(),
                Message = "Document attached successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching document to appeal {AppealId}", request.AppealId);
            return new AttachDocumentResult
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Remove document from appeal
    /// </summary>
    public async Task<bool> RemoveDocumentAsync(
     string appealId,
          string documentId,
          CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing document {DocumentId} from appeal {AppealId}", documentId, appealId);

        try
        {
            await _appealRepository.RemoveDocumentAsync(appealId, documentId, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing document {DocumentId}", documentId);
            return false;
        }
    }

    /// <summary>
    /// Get appeal documents
    /// </summary>
    public async Task<List<AppealDocumentDto>> GetAppealDocumentsAsync(
        string appealId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving documents for appeal {AppealId}", appealId);

        try
        {
            var documents = await _appealRepository.GetDocumentsAsync(appealId, cancellationToken);
            return documents.Select(d => new AppealDocumentDto
            {
                DocumentId = d.Id.ToString(),
                DocumentType = d.DocumentType,
                DocumentTitle = d.DocumentTitle,
                DocumentDescription = d.DocumentDescription,
                FileSizeBytes = d.FileSizeBytes,
                MimeType = d.MimeType,
                AttachedDate = d.AttachedDate,
                IsVerified = d.IsVerified
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for appeal {AppealId}", appealId);
            return new List<AppealDocumentDto>();
        }
    }

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Generate unique appeal number
    /// Format: APPEAL-YYYYMMDD-XXXXXX
    /// </summary>
    private string GenerateAppealNumber()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random();
        var number = random.Next(100000, 999999);
        return $"APPEAL-{date}-{number}";
    }

    /// <summary>
    /// Map appeal entity to DTO
    /// </summary>
    private AppealDto MapToDto(AppealRequest appeal)
    {
        return new AppealDto
        {
            AppealId = appeal.Id,
            AppealNumber = appeal.AppealNumber,
            ClaimId = appeal.ClaimId,
            PatientId = appeal.PatientId,
            InsurerId = appeal.InsurerId,
            ErrorCodeBeingAppealed = appeal.ErrorCodeBeingAppealed,
            ErrorDescription = appeal.ErrorDescription ?? string.Empty,
            AppealStatus = appeal.AppealStatus,
            AppealLevel = appeal.AppealLevel,
            AppealReason = appeal.AppealReason,
            DenialDate = appeal.DenialDate,
            AppealDeadlineDate = appeal.AppealDeadlineDate,
            AppealSubmittedDate = appeal.AppealSubmittedDate,
            ReviewCompletedDate = appeal.ReviewCompletedDate,
            ExpectedDecisionDate = appeal.ExpectedDecisionDate,
            AppealOutcome = appeal.AppealOutcome,
            ApprovedAmount = appeal.ApprovedAmount,
            IsActive = appeal.IsActive,
            AllowsEscalation = appeal.AllowsEscalation,
            DaysRemainingToAppeal = appeal.DaysRemainingToAppeal()
        };
    }
}
