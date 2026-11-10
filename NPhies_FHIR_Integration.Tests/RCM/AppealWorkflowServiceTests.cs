using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Application.Services.RCM;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Tests.RCM;

/// <summary>
/// Unit tests for AppealWorkflowService
/// Tests appeal submission, status tracking, documentation, and metrics
/// </summary>
public class AppealWorkflowServiceTests
{
    private readonly Mock<ILogger<AppealWorkflowService>> _mockLogger;
    private readonly AppealWorkflowService _service;

    public AppealWorkflowServiceTests()
    {
        _mockLogger = new Mock<ILogger<AppealWorkflowService>>();
  _service = new AppealWorkflowService(_mockLogger.Object);
    }

    #region SubmitAppealAsync Tests

    [Fact]
    public async Task SubmitAppealAsync_WithValidClaim_ReturnsSuccess()
    {
      // Arrange
        var claimId = "CLM-001";
    var denialReason = "Not covered";
    var appealReason = "Medical necessity documented";

        // Act
        var result = await _service.SubmitAppealAsync(claimId, denialReason, appealReason);

        // Assert
  Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
     Assert.Equal(claimId, result.ClaimId);
        Assert.NotEmpty(result.AppealId);
        Assert.NotEmpty(result.ConfirmationNumber);
    Assert.NotEqual(default, result.AppealDeadline);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SubmitAppealAsync_WithEmptyClaimId_ReturnsFailed()
    {
        // Arrange
        var claimId = string.Empty;
        var denialReason = "Not covered";
        var appealReason = "Medical necessity";

        // Act
  var result = await _service.SubmitAppealAsync(claimId, denialReason, appealReason);

        // Assert
        Assert.NotNull(result);
      Assert.False(result.IsSuccessful);
    Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task SubmitAppealAsync_WithEmptyAppealReason_ReturnsFailed()
    {
        // Arrange
        var claimId = "CLM-001";
    var denialReason = "Not covered";
        var appealReason = string.Empty;

        // Act
     var result = await _service.SubmitAppealAsync(claimId, denialReason, appealReason);

    // Assert
    Assert.NotNull(result);
        Assert.False(result.IsSuccessful);
    Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task SubmitAppealAsync_DeadlineIsAfterNow()
  {
   // Arrange
    var claimId = "CLM-002";
        var denialReason = "Frequency limit";
        var appealReason = "Clinical justification provided";

        // Act
        var result = await _service.SubmitAppealAsync(claimId, denialReason, appealReason);

        // Assert
        Assert.True(result.IsSuccessful);
    Assert.True(result.AppealDeadline > DateTime.UtcNow);
        Assert.True((result.AppealDeadline - DateTime.UtcNow).Days >= 59); // At least 59 days
    }

    [Fact]
    public async Task SubmitAppealAsync_DeadlineAvoidsWeekends()
    {
        // Arrange
        var claimId = "CLM-003";
        var denialReason = "Authorization required";
        var appealReason = "Auth obtained retrospectively";

      // Act
        var result = await _service.SubmitAppealAsync(claimId, denialReason, appealReason);

    // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotEqual(DayOfWeek.Saturday, result.AppealDeadline.DayOfWeek);
        Assert.NotEqual(DayOfWeek.Sunday, result.AppealDeadline.DayOfWeek);
    }

    #endregion

    #region GetAppealStatusAsync Tests

    [Fact]
    public async Task GetAppealStatusAsync_WithValidAppealId_ReturnsStatus()
    {
        // Arrange
        var appealId = "APP-20240101120000-12345678";

 // Act
        var status = await _service.GetAppealStatusAsync(appealId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(appealId, status.AppealId);
        Assert.NotEmpty(status.ClaimId);
        Assert.NotEmpty(status.Status);
        Assert.True(status.DaysSinceSubmission >= 0);
    }

    [Fact]
    public async Task GetAppealStatusAsync_WithEmptyAppealId_ThrowsException()
    {
        // Arrange
        var appealId = string.Empty;

        // Act & Assert
  await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetAppealStatusAsync(appealId));
    }

    [Fact]
    public async Task GetAppealStatusAsync_ReturnsValidStatus()
    {
        // Arrange
  var appealId = "APP-20240101120000-12345678";

        // Act
        var status = await _service.GetAppealStatusAsync(appealId);

        // Assert
        Assert.True(status.Status == "submitted" || status.Status == "under review" || 
         status.Status == "approved" || status.Status == "denied" || 
          status.Status == "withdrawn");
    }

    #endregion

 #region AddSupportingDocumentationAsync Tests

    [Fact]
    public async Task AddSupportingDocumentationAsync_WithValidDocument_ReturnsSuccess()
    {
        // Arrange
        var appealId = "APP-001";
        var document = new byte[] { 1, 2, 3, 4, 5 };
  var documentType = "clinical-note";

        // Act
        var result = await _service.AddSupportingDocumentationAsync(appealId, document, documentType);

    // Assert
     Assert.True(result);
    }

    [Fact]
    public async Task AddSupportingDocumentationAsync_WithEmptyAppealId_ThrowsException()
    {
 // Arrange
        var appealId = string.Empty;
        var document = new byte[] { 1, 2, 3 };
        var documentType = "clinical-note";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
       _service.AddSupportingDocumentationAsync(appealId, document, documentType));
    }

    [Fact]
    public async Task AddSupportingDocumentationAsync_WithNullDocument_ThrowsException()
    {
        // Arrange
        var appealId = "APP-001";
      byte[] document = null;
        var documentType = "clinical-note";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
    _service.AddSupportingDocumentationAsync(appealId, document, documentType));
    }

    [Fact]
    public async Task AddSupportingDocumentationAsync_WithOversizedDocument_ThrowsException()
    {
      // Arrange
        var appealId = "APP-001";
        var document = new byte[11 * 1024 * 1024]; // 11 MB (over 10 MB limit)
        var documentType = "clinical-note";

        // Act & Assert
     await Assert.ThrowsAsync<ArgumentException>(() => 
     _service.AddSupportingDocumentationAsync(appealId, document, documentType));
    }

    [Fact]
public async Task AddSupportingDocumentationAsync_WithEmptyDocumentType_ThrowsException()
    {
        // Arrange
        var appealId = "APP-001";
  var document = new byte[] { 1, 2, 3 };
        var documentType = string.Empty;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
    _service.AddSupportingDocumentationAsync(appealId, document, documentType));
    }

    #endregion

    #region GenerateAppealLetterAsync Tests

    [Fact]
    public async Task GenerateAppealLetterAsync_WithValidAppeal_ReturnsPDFBytes()
    {
        // Arrange
      var appeal = new Appeal
        {
            Id = "APP-001",
ClaimId = "CLM-001",
   AppealReason = "Medical necessity",
 DenialReason = "Not covered"
        };

        // Act
        var pdfBytes = await _service.GenerateAppealLetterAsync(appeal);

    // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
    }

    [Fact]
 public async Task GenerateAppealLetterAsync_WithNullAppeal_ThrowsException()
    {
   // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
     _service.GenerateAppealLetterAsync(null));
    }

    [Fact]
    public async Task GenerateAppealLetterAsync_ContainsAppealInformation()
    {
        // Arrange
        var appeal = new Appeal
        {
            Id = "APP-001",
            ClaimId = "CLM-001",
            AppealReason = "Medical necessity",
  DenialReason = "Not covered"
        };

        // Act
   var pdfBytes = await _service.GenerateAppealLetterAsync(appeal);
        var pdfText = System.Text.Encoding.UTF8.GetString(pdfBytes);

        // Assert
  Assert.Contains("APPEAL LETTER", pdfText);
        Assert.Contains(appeal.ClaimId, pdfText);
        Assert.Contains(appeal.AppealReason, pdfText);
    }

    #endregion

    #region GetAppealDeadlineAsync Tests

    [Fact]
    public async Task GetAppealDeadlineAsync_WithValidClaimId_ReturnsDeadline()
    {
   // Arrange
        var claimId = "CLM-001";

        // Act
var deadline = await _service.GetAppealDeadlineAsync(claimId);

    // Assert
        Assert.NotEqual(default, deadline);
        Assert.True(deadline > DateTime.UtcNow);
    }

    [Fact]
    public async Task GetAppealDeadlineAsync_WithEmptyClaimId_ThrowsException()
    {
 // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
  _service.GetAppealDeadlineAsync(string.Empty));
    }

    [Fact]
    public async Task GetAppealDeadlineAsync_DeadlineIsApproximately60Days()
  {
        // Arrange
     var claimId = "CLM-002";

        // Act
        var deadline = await _service.GetAppealDeadlineAsync(claimId);
        var daysUntilDeadline = (deadline - DateTime.UtcNow).Days;

      // Assert
        Assert.True(daysUntilDeadline >= 59 && daysUntilDeadline <= 61);
    }

    #endregion

    #region GetAppealMetricsAsync Tests

    [Fact]
    public async Task GetAppealMetricsAsync_WithValidDateRange_ReturnsMetrics()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddMonths(-1);
var toDate = DateTime.UtcNow;

        // Act
        var metrics = await _service.GetAppealMetricsAsync(fromDate, toDate);

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(fromDate, metrics.FromDate);
     Assert.Equal(toDate, metrics.ToDate);
        Assert.True(metrics.TotalAppeals > 0);
        Assert.True(metrics.ApprovedAppeals >= 0);
        Assert.True(metrics.DeniedAppeals >= 0);
    }

    [Fact]
    public async Task GetAppealMetricsAsync_WithInvalidDateRange_ThrowsException()
    {
        // Arrange
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-1); // toDate before fromDate

     // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetAppealMetricsAsync(fromDate, toDate));
    }

    [Fact]
    public async Task GetAppealMetricsAsync_MetricsAreConsistent()
    {
 // Arrange
        var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

   // Act
        var metrics = await _service.GetAppealMetricsAsync(fromDate, toDate);

        // Assert
        Assert.True(metrics.TotalAppeals >= metrics.ApprovedAppeals + metrics.DeniedAppeals + 
          metrics.PendingAppeals + metrics.WithdrawnAppeals);
     Assert.True(metrics.ApprovalRate >= 0m && metrics.ApprovalRate <= 1m);
  Assert.True(metrics.TotalRecoveredAmount >= 0m);
    }

 #endregion
}
