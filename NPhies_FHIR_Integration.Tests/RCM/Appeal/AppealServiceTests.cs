using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using NPhies_FHIR_Integration.Application.Services.RCM;
using NPhies_FHIR_Integration.Application.Services.Masters;
using NPhies_FHIR_Integration.Infrastructure.Repositories;
using NPhies_FHIR_Integration.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Tests.RCM.Appeal;

/// <summary>
/// Unit tests for AppealService
/// Tests complete appeal lifecycle
/// </summary>
public class AppealServiceTests
{
    private readonly Mock<ILogger<AppealService>> _mockLogger;
    private readonly Mock<IErrorCodeService> _mockErrorCodeService;
    private readonly Mock<IAppealRepository> _mockAppealRepository;
  private readonly AppealService _appealService;

    public AppealServiceTests()
    {
    _mockLogger = new Mock<ILogger<AppealService>>();
  _mockErrorCodeService = new Mock<IErrorCodeService>();
        _mockAppealRepository = new Mock<IAppealRepository>();
        
   _appealService = new AppealService(
 _mockLogger.Object,
   _mockErrorCodeService.Object,
          _mockAppealRepository.Object);
    }

    #region CreateAppealAsync Tests

    [Fact]
 public async Task CreateAppealAsync_WithValidRequest_CreatesAppeal()
    {
        // Arrange
 var request = new CreateAppealRequest
        {
  ClaimId = "CLAIM-001",
        ClaimResponseId = "RESPONSE-001",
     PatientId = "PATIENT-001",
            InsurerId = "INSURER-001",
            ProviderId = "PROVIDER-001",
      ErrorCodeBeingAppealed = "AD-1-1",
            AppealReason = "Wrong calculation"
   };

   var errorCode = new ErrorCodeMaster
   {
            ErrorCode = "AD-1-1",
            ErrorDescription = "Administrative Error",
        AllowsAppeal = true,
     StandardAppealDays = 30
        };

        _mockErrorCodeService
       .Setup(x => x.GetErrorCodeAsync("AD-1-1"))
 .ReturnsAsync(errorCode);

     _mockAppealRepository
            .Setup(x => x.AddAsync(It.IsAny<AppealRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((AppealRequest a, CancellationToken ct) => 
            {
    a.Id = Guid.NewGuid().ToString();
     return a;
            });

     // Act
var result = await _appealService.CreateAppealAsync(request);

        // Assert
   Assert.True(result.IsSuccess);
    Assert.NotNull(result.AppealId);
 Assert.NotNull(result.AppealNumber);
        Assert.NotNull(result.DeadlineDate);
        _mockAppealRepository.Verify(x => x.AddAsync(It.IsAny<AppealRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAppealAsync_WithMissingClaimId_ReturnsFailed()
    {
        // Arrange
var request = new CreateAppealRequest
        {
 ClaimId = "", // Empty claim ID
            ErrorCodeBeingAppealed = "AD-1-1"
        };

     // Act
        var result = await _appealService.CreateAppealAsync(request);

        // Assert
    Assert.False(result.IsSuccess);
        Assert.Contains("Claim ID is required", result.ErrorMessage);
        _mockAppealRepository.Verify(x => x.AddAsync(It.IsAny<AppealRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAppealAsync_WithInvalidErrorCode_ReturnsFailed()
    {
    // Arrange
 var request = new CreateAppealRequest
      {
     ClaimId = "CLAIM-001",
      ErrorCodeBeingAppealed = "INVALID-CODE"
        };

        _mockErrorCodeService
            .Setup(x => x.GetErrorCodeAsync("INVALID-CODE"))
            .ReturnsAsync((ErrorCodeMaster)null);

        // Act
var result = await _appealService.CreateAppealAsync(request);

// Assert
 Assert.False(result.IsSuccess);
     Assert.Contains("not found", result.ErrorMessage);
    }

[Fact]
    public async Task CreateAppealAsync_WithNonAppealableErrorCode_ReturnsFailed()
    {
        // Arrange
        var request = new CreateAppealRequest
  {
            ClaimId = "CLAIM-001",
        ErrorCodeBeingAppealed = "NO-APPEAL-CODE"
        };

        var errorCode = new ErrorCodeMaster
        {
            ErrorCode = "NO-APPEAL-CODE",
        AllowsAppeal = false
     };

        _mockErrorCodeService
   .Setup(x => x.GetErrorCodeAsync("NO-APPEAL-CODE"))
         .ReturnsAsync(errorCode);

        // Act
   var result = await _appealService.CreateAppealAsync(request);

 // Assert
        Assert.False(result.IsSuccess);
    Assert.Contains("does not allow appeals", result.ErrorMessage);
    }

    #endregion

    #region SubmitAppealAsync Tests

    [Fact]
    public async Task SubmitAppealAsync_WithValidAppeal_SubmitsSuccessfully()
    {
  // Arrange
        var appeal = new AppealRequest
        {
          Id = "APPEAL-001",
     AppealStatus = "draft",
            AppealDeadlineDate = DateTime.UtcNow.AddDays(10),
      AppealSubmittedDate = null,
          InternalReferenceNumber = "REF-001"
      };

        _mockAppealRepository
       .Setup(x => x.GetByIdAsync("APPEAL-001", It.IsAny<CancellationToken>()))
         .ReturnsAsync(appeal);

        _mockAppealRepository
   .Setup(x => x.UpdateAsync(It.IsAny<AppealRequest>(), It.IsAny<CancellationToken>()))
  .ReturnsAsync(appeal);

        _mockAppealRepository
            .Setup(x => x.AddStatusHistoryAsync(It.IsAny<string>(), It.IsAny<AppealStatusHistory>(), It.IsAny<CancellationToken>()))
          .Returns(Task.CompletedTask);

        // Act
        var result = await _appealService.SubmitAppealAsync("APPEAL-001");

 // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("APPEAL-001", result.AppealId);
        Assert.NotNull(result.SubmittedDate);
    }

    [Fact]
    public async Task SubmitAppealAsync_WithPastDeadline_ReturnsFailed()
    {
    // Arrange
 var appeal = new AppealRequest
 {
            Id = "APPEAL-001",
            AppealDeadlineDate = DateTime.UtcNow.AddDays(-1) // Past deadline
   };

        _mockAppealRepository
            .Setup(x => x.GetByIdAsync("APPEAL-001", It.IsAny<CancellationToken>()))
  .ReturnsAsync(appeal);

  // Act
        var result = await _appealService.SubmitAppealAsync("APPEAL-001");

// Assert
      Assert.False(result.IsSuccess);
      Assert.Contains("deadline has passed", result.ErrorMessage);
    }

    [Fact]
    public async Task SubmitAppealAsync_WithNonexistentAppeal_ReturnsFailed()
    {
        // Arrange
   _mockAppealRepository
.Setup(x => x.GetByIdAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
       .ReturnsAsync((AppealRequest)null);

  // Act
        var result = await _appealService.SubmitAppealAsync("NONEXISTENT");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage);
    }

    #endregion

    #region WithdrawAppealAsync Tests

    [Fact]
    public async Task WithdrawAppealAsync_WithValidAppeal_WithdrawsSuccessfully()
    {
        // Arrange
var appealId = "APPEAL-001";
 var reason = "Provider error";

        _mockAppealRepository
      .Setup(x => x.MarkAsWithdrawnAsync(appealId, reason, It.IsAny<CancellationToken>()))
   .Returns(Task.CompletedTask);

     _mockAppealRepository
 .Setup(x => x.AddStatusHistoryAsync(It.IsAny<string>(), It.IsAny<AppealStatusHistory>(), It.IsAny<CancellationToken>()))
 .Returns(Task.CompletedTask);

   // Act
        var result = await _appealService.WithdrawAppealAsync(appealId, reason);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(appealId, result.AppealId);
        Assert.NotNull(result.WithdrawnDate);
    }

    #endregion

    #region EscalateAppealAsync Tests

    [Fact]
    public async Task EscalateAppealAsync_WithValidAppeal_EscalatesSuccessfully()
  {
        // Arrange
        var appeal = new AppealRequest
        {
     Id = "APPEAL-001",
   AppealLevel = 1,
            AppealNumber = "APPEAL-001",
            AllowsEscalation = true,
   ClaimId = "CLAIM-001",
            PatientId = "PATIENT-001",
            InsurerId = "INSURER-001",
            ProviderId = "PROVIDER-001",
            ErrorCodeBeingAppealed = "AD-1-1",
          DenialDate = DateTime.UtcNow
        };

      var escalatedAppeal = new AppealRequest
        {
   Id = "APPEAL-002",
       AppealNumber = "APPEAL-002",
            AppealLevel = 2
        };

        _mockAppealRepository
        .Setup(x => x.GetByIdAsync("APPEAL-001", It.IsAny<CancellationToken>()))
  .ReturnsAsync(appeal);

  _mockAppealRepository
        .Setup(x => x.AddAsync(It.IsAny<AppealRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(escalatedAppeal);

        _mockAppealRepository
      .Setup(x => x.UpdateAsync(It.IsAny<AppealRequest>(), It.IsAny<CancellationToken>()))
 .ReturnsAsync(appeal);

        _mockAppealRepository
            .Setup(x => x.AddStatusHistoryAsync(It.IsAny<string>(), It.IsAny<AppealStatusHistory>(), It.IsAny<CancellationToken>()))
     .Returns(Task.CompletedTask);

        // Act
var result = await _appealService.EscalateAppealAsync("APPEAL-001", "Provider request");

        // Assert
    Assert.True(result.IsSuccess);
        Assert.Equal("APPEAL-002", result.EscalatedAppealId);
        Assert.Equal(2, result.NewAppealLevel);
    }

[Fact]
    public async Task EscalateAppealAsync_WithMaxLevel_ReturnsFailed()
    {
        // Arrange
        var appeal = new AppealRequest
        {
            Id = "APPEAL-003",
   AppealLevel = 3 // Already at max level
        };

        _mockAppealRepository
            .Setup(x => x.GetByIdAsync("APPEAL-003", It.IsAny<CancellationToken>()))
   .ReturnsAsync(appeal);

        // Act
        var result = await _appealService.EscalateAppealAsync("APPEAL-003", "Request");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("maximum level", result.ErrorMessage);
    }

    #endregion

    #region GetAppealStatusAsync Tests

    [Fact]
    public async Task GetAppealStatusAsync_WithValidAppeal_ReturnsStatus()
    {
        // Arrange
        var appeal = new AppealRequest
        {
            Id = "APPEAL-001",
        AppealStatus = "submitted",
    DenialDate = DateTime.UtcNow.AddDays(-5),
       AppealDeadlineDate = DateTime.UtcNow.AddDays(25),
       AppealSubmittedDate = DateTime.UtcNow.AddDays(-1)
    };

        var history = new List<AppealStatusHistory>
        {
    new AppealStatusHistory { Status = "draft", StatusChangeDate = DateTime.UtcNow.AddDays(-5) },
 new AppealStatusHistory { Status = "submitted", StatusChangeDate = DateTime.UtcNow.AddDays(-1) }
 };

        _mockAppealRepository
    .Setup(x => x.GetByIdAsync("APPEAL-001", It.IsAny<CancellationToken>()))
      .ReturnsAsync(appeal);

        _mockAppealRepository
        .Setup(x => x.GetStatusHistoryAsync("APPEAL-001", It.IsAny<CancellationToken>()))
      .ReturnsAsync(history);

        // Act
        var result = await _appealService.GetAppealStatusAsync("APPEAL-001");

        // Assert
    Assert.Equal("APPEAL-001", result.AppealId);
        Assert.Equal("submitted", result.CurrentStatus);
   Assert.NotEmpty(result.StatusHistory);
    Assert.True(result.DaysRemainingToAppeal > 0);
    }

    #endregion

    #region GetAppealStatisticsAsync Tests

    [Fact]
    public async Task GetAppealStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
     _mockAppealRepository.Setup(x => x.GetTotalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockAppealRepository.Setup(x => x.GetActiveAppealsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AppealRequest> { new AppealRequest() });
     _mockAppealRepository.Setup(x => x.GetCountByStatusAsync("approved", It.IsAny<CancellationToken>())).ReturnsAsync(70);
        _mockAppealRepository.Setup(x => x.GetCountByStatusAsync("denied", It.IsAny<CancellationToken>())).ReturnsAsync(20);
  _mockAppealRepository.Setup(x => x.GetCountByStatusAsync("partial", It.IsAny<CancellationToken>())).ReturnsAsync(5);
    _mockAppealRepository.Setup(x => x.GetCountByStatusAsync("withdrawn", It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _mockAppealRepository.Setup(x => x.GetApprovalRateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(77.78m);
      _mockAppealRepository.Setup(x => x.GetTotalApprovedAmountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(500000m);
        _mockAppealRepository.Setup(x => x.GetAppealsNearingDeadlineAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<AppealRequest>());

        // Act
        var result = await _appealService.GetAppealStatisticsAsync();

        // Assert
    Assert.Equal(100, result.TotalAppeals);
    Assert.Equal(1, result.ActiveAppeals);
        Assert.Equal(70, result.ApprovedAppeals);
        Assert.Equal(77.78m, result.AverageApprovalRate);
     Assert.Equal(500000m, result.TotalAmountApproved);
    }

  #endregion

    #region Document Tests

    [Fact]
    public async Task AttachDocumentAsync_WithValidRequest_AttachesSuccessfully()
    {
// Arrange
        var request = new AttachDocumentRequest
        {
AppealId = "APPEAL-001",
      DocumentType = "Medical Record",
            DocumentTitle = "Test Document",
            FilePath = "/docs/test.pdf",
        FileSizeBytes = 1024,
            MimeType = "application/pdf"
        };

        _mockAppealRepository
            .Setup(x => x.AddDocumentAsync(It.IsAny<string>(), It.IsAny<AppealDocument>(), It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

        // Act
        var result = await _appealService.AttachDocumentAsync(request);

 // Assert
  Assert.True(result.IsSuccess);
        Assert.NotNull(result.DocumentId);
    }

    [Fact]
    public async Task GetAppealDocumentsAsync_WithValidAppealId_ReturnsDocuments()
    {
        // Arrange
        var documents = new List<AppealDocument>
{
   new AppealDocument
   {
   Id = "DOC-001",
          DocumentType = "Medical Record",
      DocumentTitle = "Test",
     MimeType = "application/pdf",
       FileSizeBytes = 1024
            }
    };

        _mockAppealRepository
            .Setup(x => x.GetDocumentsAsync("APPEAL-001", It.IsAny<CancellationToken>()))
      .ReturnsAsync(documents);

     // Act
     var result = await _appealService.GetAppealDocumentsAsync("APPEAL-001");

    // Assert
        Assert.Single(result);
        Assert.Equal("DOC-001", result[0].DocumentId);
    }

    #endregion
}
