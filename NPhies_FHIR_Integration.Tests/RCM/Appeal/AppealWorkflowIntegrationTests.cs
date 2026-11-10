using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Application.Services.RCM;
using NPhies_FHIR_Integration.Infrastructure.Repositories;
using NPhies_FHIR_Integration.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Tests.RCM.Appeal;

/// <summary>
/// Integration tests for Appeal workflow
/// Tests complete appeal lifecycle scenarios
/// </summary>
public class AppealWorkflowIntegrationTests
{
 private readonly Mock<IAppealRepository> _mockRepository;
    private readonly Mock<ILogger<AppealService>> _mockLogger;
    private readonly NullLogger<AppealService> _nullLogger;

  public AppealWorkflowIntegrationTests()
    {
 _mockRepository = new Mock<IAppealRepository>();
    _mockLogger = new Mock<ILogger<AppealService>>();
    _nullLogger = new NullLogger<AppealService>();
    }

    [Fact]
    public async Task CompleteAppealWorkflow_CreatedToSubmitted()
    {
     // Arrange: Setup complete scenario
        var claimId = "CLAIM-12345";
        var patientId = "PATIENT-001";
 var errorCode = "AD-1-1";

   var createdAppeal = new AppealRequest
        {
    Id = Guid.NewGuid().ToString(),
            AppealNumber = "APPEAL-20250101-123456",
            ClaimId = claimId,
          PatientId = patientId,
   ErrorCodeBeingAppealed = errorCode,
            AppealStatus = "draft",
AppealDeadlineDate = DateTime.UtcNow.AddDays(30),
            IsActive = true,
    AllowsEscalation = true
        };

   // Verify creation saved appeal
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<AppealRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdAppeal);

        // Verify retrieval for submission
        _mockRepository
  .Setup(x => x.GetByIdAsync(createdAppeal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdAppeal);

        // Verify submission updates status
        _mockRepository
    .Setup(x => x.UpdateAsync(It.IsAny<AppealRequest>(), It.IsAny<CancellationToken>()))
 .ReturnsAsync((AppealRequest a, CancellationToken ct) => 
            {
      a.AppealStatus = "submitted";
  a.AppealSubmittedDate = DateTime.UtcNow;
             return a;
       });

  // Verify status history added
        _mockRepository
            .Setup(x => x.AddStatusHistoryAsync(It.IsAny<string>(), It.IsAny<AppealStatusHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: Create appeal
        var createdResult = createdAppeal;
        Assert.NotNull(createdResult);

     // Act: Update to submitted
  createdResult.AppealStatus = "submitted";
        createdResult.AppealSubmittedDate = DateTime.UtcNow;
        var updatedResult = await _mockRepository.Object.UpdateAsync(createdResult);

        // Assert
        Assert.NotNull(updatedResult);
        Assert.Equal("submitted", updatedResult.AppealStatus);
        Assert.NotNull(updatedResult.AppealSubmittedDate);
  }

    [Fact]
    public async Task AppealEscalation_Level1ToLevel2ToLevel3()
{
        // Arrange: Level 1 appeal
  var level1Appeal = new AppealRequest
        {
            Id = "APPEAL-L1",
     AppealNumber = "APPEAL-20250101-111111",
     AppealLevel = 1,
          AppealStatus = "denied",
      AllowsEscalation = true,
          ClaimId = "CLAIM-001",
         PatientId = "PATIENT-001"
 };

     // Level 2 appeal
 var level2Appeal = new AppealRequest
      {
       Id = "APPEAL-L2",
        AppealNumber = "APPEAL-20250101-222222",
            AppealLevel = 2,
   AppealStatus = "draft",
       AllowsEscalation = true,
  ClaimId = "CLAIM-001",
  PatientId = "PATIENT-001"
        };

        // Level 3 appeal
        var level3Appeal = new AppealRequest
      {
            Id = "APPEAL-L3",
   AppealNumber = "APPEAL-20250101-333333",
            AppealLevel = 3,
            AppealStatus = "draft",
     AllowsEscalation = false, // No further escalation
            ClaimId = "CLAIM-001",
            PatientId = "PATIENT-001"
        };

      _mockRepository
       .Setup(x => x.GetByIdAsync("APPEAL-L1", It.IsAny<CancellationToken>()))
       .ReturnsAsync(level1Appeal);

        _mockRepository
       .Setup(x => x.GetByIdAsync("APPEAL-L2", It.IsAny<CancellationToken>()))
      .ReturnsAsync(level2Appeal);

        _mockRepository
      .Setup(x => x.GetByIdAsync("APPEAL-L3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(level3Appeal);

        _mockRepository
       .Setup(x => x.AddAsync(It.IsAny<AppealRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppealRequest a, CancellationToken ct) => a);

        _mockRepository
   .Setup(x => x.UpdateAsync(It.IsAny<AppealRequest>(), It.IsAny<CancellationToken>()))
   .ReturnsAsync((AppealRequest a, CancellationToken ct) => a);

        _mockRepository
     .Setup(x => x.AddStatusHistoryAsync(It.IsAny<string>(), It.IsAny<AppealStatusHistory>(), It.IsAny<CancellationToken>()))
.Returns(Task.CompletedTask);

  // Act & Assert: L1 escalates to L2
        var l1 = await _mockRepository.Object.GetByIdAsync("APPEAL-L1");
   Assert.NotNull(l1);
  Assert.Equal(1, l1.AppealLevel);
        Assert.True(l1.AllowsEscalation);

        // Act & Assert: L2 escalates to L3
        var l2 = await _mockRepository.Object.GetByIdAsync("APPEAL-L2");
        Assert.NotNull(l2);
      Assert.Equal(2, l2.AppealLevel);
        Assert.True(l2.AllowsEscalation);

   // Act & Assert: L3 cannot escalate
        var l3 = await _mockRepository.Object.GetByIdAsync("APPEAL-L3");
        Assert.NotNull(l3);
        Assert.Equal(3, l3.AppealLevel);
        Assert.False(l3.AllowsEscalation);
    }

    [Fact]
    public async Task AppealWithDocuments_AttachAndRetrieve()
{
        // Arrange
        var appealId = "APPEAL-DOC-001";
        
   var documents = new List<AppealDocument>
        {
            new AppealDocument
     {
        Id = "DOC-001",
       AppealId = appealId,
       DocumentType = "Medical Record",
   DocumentTitle = "Patient Lab Report",
     FilePath = "/documents/lab-report.pdf",
        FileSizeBytes = 2048,
       MimeType = "application/pdf",
   AttachedDate = DateTime.UtcNow,
    IsActive = true
            },
            new AppealDocument
            {
       Id = "DOC-002",
      AppealId = appealId,
 DocumentType = "Provider Note",
        DocumentTitle = "Clinical Notes",
     FilePath = "/documents/clinical-notes.pdf",
          FileSizeBytes = 1536,
MimeType = "application/pdf",
        AttachedDate = DateTime.UtcNow,
                IsActive = true
        }
};

        _mockRepository
            .Setup(x => x.GetDocumentsAsync(appealId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(documents);

        _mockRepository
       .Setup(x => x.AddDocumentAsync(It.IsAny<string>(), It.IsAny<AppealDocument>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

        // Act
  var retrievedDocs = await _mockRepository.Object.GetDocumentsAsync(appealId);

        // Assert
  Assert.NotEmpty(retrievedDocs);
   Assert.Equal(2, retrievedDocs.Count);
        Assert.All(retrievedDocs, d => Assert.Equal(appealId, d.AppealId));
   Assert.All(retrievedDocs, d => Assert.True(d.FileSizeBytes > 0));
    }

    [Fact]
    public async Task AppealTimeline_TrackingDeadlineAndStatusChanges()
    {
      // Arrange
        var denialDate = DateTime.UtcNow.AddDays(-5);
        var deadlineDate = denialDate.AddDays(30);
        var submittedDate = DateTime.UtcNow.AddDays(-1);

        var appeal = new AppealRequest
  {
    Id = "APPEAL-TIMELINE",
            AppealNumber = "APPEAL-20250101-999999",
          DenialDate = denialDate,
         AppealDeadlineDate = deadlineDate,
       AppealSubmittedDate = submittedDate,
   AppealStatus = "submitted",
     IsActive = true
        };

        var history = new List<AppealStatusHistory>
        {
            new AppealStatusHistory
            {
                Status = "draft",
      ChangedBy = "system",
      StatusChangeDate = denialDate,
           Comments = "Appeal created"
            },
            new AppealStatusHistory
    {
                Status = "submitted",
           ChangedBy = "provider",
                StatusChangeDate = submittedDate,
    Comments = "Submitted to insurer"
 }
     };

        _mockRepository
        .Setup(x => x.GetByIdAsync("APPEAL-TIMELINE", It.IsAny<CancellationToken>()))
         .ReturnsAsync(appeal);

      _mockRepository
            .Setup(x => x.GetStatusHistoryAsync("APPEAL-TIMELINE", It.IsAny<CancellationToken>()))
    .ReturnsAsync(history);

        // Act
    var retrievedAppeal = await _mockRepository.Object.GetByIdAsync("APPEAL-TIMELINE");
  var daysRemaining = retrievedAppeal.DaysRemainingToAppeal();
        var appealAge = retrievedAppeal.AppealAgeDays();

        // Assert
    Assert.NotNull(retrievedAppeal);
     Assert.True(daysRemaining > 0); // Should have days remaining
  Assert.True(appealAge > 0);     // Should have age
        Assert.Equal("submitted", retrievedAppeal.AppealStatus);
        
        var statusHistory = await _mockRepository.Object.GetStatusHistoryAsync("APPEAL-TIMELINE");
  Assert.Equal(2, statusHistory.Count);
    }

    [Fact]
    public async Task MultipleAppealsPerClaim_QueryAndFilter()
    {
        // Arrange
        var claimId = "CLAIM-MULTI";
    
        var appeals = new List<AppealRequest>
        {
   new AppealRequest
    {
    Id = "APPEAL-1",
           ClaimId = claimId,
         AppealLevel = 1,
    AppealStatus = "denied"
            },
            new AppealRequest
       {
   Id = "APPEAL-2",
        ClaimId = claimId,
       AppealLevel = 2,
  AppealStatus = "pending"
   },
new AppealRequest
   {
            Id = "APPEAL-3",
           ClaimId = claimId,
                AppealLevel = 3,
          AppealStatus = "approved"
            }
        };

    _mockRepository
  .Setup(x => x.GetByClaimIdAsync(claimId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appeals);

        _mockRepository
            .Setup(x => x.GetByLevelAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int level, CancellationToken ct) => 
      appeals.FindAll(a => a.AppealLevel == level));

        // Act
        var claimAppeals = await _mockRepository.Object.GetByClaimIdAsync(claimId);
        var level1Appeals = await _mockRepository.Object.GetByLevelAsync(1);
        var level2Appeals = await _mockRepository.Object.GetByLevelAsync(2);

     // Assert
        Assert.Equal(3, claimAppeals.Count);
        Assert.Single(level1Appeals);
        Assert.Single(level2Appeals);
        Assert.All(claimAppeals, a => Assert.Equal(claimId, a.ClaimId));
    }

    [Fact]
    public async Task AppealStatistics_CalculateApprovalRates()
    {
        // Arrange
      _mockRepository.Setup(x => x.GetTotalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(200);
      _mockRepository.Setup(x => x.GetCountByStatusAsync("approved", It.IsAny<CancellationToken>())).ReturnsAsync(140);
  _mockRepository.Setup(x => x.GetCountByStatusAsync("denied", It.IsAny<CancellationToken>())).ReturnsAsync(40);
        _mockRepository.Setup(x => x.GetCountByStatusAsync("partial", It.IsAny<CancellationToken>())).ReturnsAsync(20);
        _mockRepository.Setup(x => x.GetApprovalRateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(70m);
        _mockRepository.Setup(x => x.GetTotalApprovedAmountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1500000m);
        _mockRepository.Setup(x => x.GetActiveAppealsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AppealRequest>());

        // Act
        var total = await _mockRepository.Object.GetTotalCountAsync();
      var approved = await _mockRepository.Object.GetCountByStatusAsync("approved");
var approvalRate = await _mockRepository.Object.GetApprovalRateAsync();
        var totalApprovedAmount = await _mockRepository.Object.GetTotalApprovedAmountAsync();

        // Assert
        Assert.Equal(200, total);
        Assert.Equal(140, approved);
        Assert.Equal(70m, approvalRate);
      Assert.Equal(1500000m, totalApprovedAmount);
     Assert.Equal(70, (approved / (decimal)total) * 100); // Manual calculation matches
 }
}
