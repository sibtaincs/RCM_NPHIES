using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.ApiService.Controllers;
using NPhies_FHIR_Integration.Application.Services.RCM;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Tests.RCM;

/// <summary>
/// Unit tests for RCMController
/// Tests all 7 REST endpoints for RCM workflow
/// </summary>
public class RCMControllerTests
{
    private readonly Mock<IClaimResponseProcessingService> _mockClaimProcessor;
    private readonly Mock<IAdjudicationWorkflowService> _mockAdjudication;
  private readonly Mock<IAppealWorkflowService> _mockAppealWorkflow;
    private readonly Mock<IDenialManagementService> _mockDenialManagement;
    private readonly Mock<IPaymentReconciliationService> _mockPaymentReconciliation;
    private readonly Mock<ILogger<RCMController>> _mockLogger;
    private readonly RCMController _controller;

    public RCMControllerTests()
    {
        _mockClaimProcessor = new Mock<IClaimResponseProcessingService>();
        _mockAdjudication = new Mock<IAdjudicationWorkflowService>();
        _mockAppealWorkflow = new Mock<IAppealWorkflowService>();
        _mockDenialManagement = new Mock<IDenialManagementService>();
        _mockPaymentReconciliation = new Mock<IPaymentReconciliationService>();
    _mockLogger = new Mock<ILogger<RCMController>>();

        _controller = new RCMController(
         _mockClaimProcessor.Object,
   _mockAdjudication.Object,
     _mockAppealWorkflow.Object,
            _mockDenialManagement.Object,
 _mockPaymentReconciliation.Object,
          _mockLogger.Object);
    }

    #region ProcessClaimResponse Tests

    [Fact]
    public async Task ProcessClaimResponse_WithValidRequest_ReturnsOkResult()
    {
  // Arrange
 var claimId = "CLM-001";
        var response = new ClaimResponse { ClaimId = claimId };
  var processingResult = new ClaimResponseProcessingResult
        {
  ClaimId = claimId,
            IsSuccessful = true,
   ApprovedItemCount = 2,
     DeniedItemCount = 1
  };

    _mockClaimProcessor.Setup(s => s.ProcessClaimResponseAsync(
  It.IsAny<ClaimResponse>(),
       It.IsAny<Claim>()))
        .ReturnsAsync(processingResult);

        // Act
        var result = await _controller.ProcessClaimResponse(claimId, response);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ProcessClaimResponse_WithEmptyClaimId_ReturnsBadRequest()
    {
     // Arrange
        var claimId = string.Empty;
   var response = new ClaimResponse { ClaimId = "CLM-001" };

        // Act
        var result = await _controller.ProcessClaimResponse(claimId, response);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ProcessClaimResponse_WithNullResponse_ReturnsBadRequest()
    {
        // Arrange
        var claimId = "CLM-001";

        // Act
        var result = await _controller.ProcessClaimResponse(claimId, null);

        // Assert
 Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region ProcessAdjudication Tests

    [Fact]
    public async Task ProcessAdjudication_WithValidClaimId_ReturnsOkResult()
    {
        // Arrange
        var claimId = "CLM-001";
        var adjudicationResult = new AdjudicationWorkflowResult
        {
            ClaimId = claimId,
            IsSuccessful = true,
            OverallStatus = "approved"
        };

    _mockAdjudication.Setup(s => s.ProcessAdjudicationAsync(
   It.IsAny<Claim>(),
      It.IsAny<Coverage>(),
          It.IsAny<ClaimResponse>()))
            .ReturnsAsync(adjudicationResult);

        // Act
        var result = await _controller.ProcessAdjudication(claimId);

 // Assert
        Assert.IsType<OkObjectResult>(result);
  }

    [Fact]
    public async Task ProcessAdjudication_WithEmptyClaimId_ReturnsBadRequest()
    {
        // Arrange
        var claimId = string.Empty;

        // Act
     var result = await _controller.ProcessAdjudication(claimId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region SubmitAppeal Tests

    [Fact]
    public async Task SubmitAppeal_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var claimId = "CLM-001";
  var request = new AppealRequest
        {
   DenialReason = "Not covered",
  AppealReason = "Medical necessity"
        };
        var appealResult = new AppealSubmissionResult
        {
            AppealId = "APP-001",
            ClaimId = claimId,
          IsSuccessful = true,
         ConfirmationNumber = "CONF-12345678"
        };

      _mockAppealWorkflow.Setup(s => s.SubmitAppealAsync(
            It.IsAny<string>(),
       It.IsAny<string>(),
   It.IsAny<string>()))
 .ReturnsAsync(appealResult);

        // Act
  var result = await _controller.SubmitAppeal(claimId, request);

     // Assert
    Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task SubmitAppeal_WithEmptyClaimId_ReturnsBadRequest()
    {
        // Arrange
      var claimId = string.Empty;
        var request = new AppealRequest { AppealReason = "Medical necessity" };

        // Act
        var result = await _controller.SubmitAppeal(claimId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

 [Fact]
    public async Task SubmitAppeal_WithNullRequest_ReturnsBadRequest()
 {
        // Arrange
   var claimId = "CLM-001";

        // Act
        var result = await _controller.SubmitAppeal(claimId, null);

    // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SubmitAppeal_WithEmptyAppealReason_ReturnsBadRequest()
    {
        // Arrange
        var claimId = "CLM-001";
        var request = new AppealRequest { AppealReason = string.Empty };

        // Act
        var result = await _controller.SubmitAppeal(claimId, request);

      // Assert
   Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetAppealStatus Tests

    [Fact]
  public async Task GetAppealStatus_WithValidAppealId_ReturnsOkResult()
    {
        // Arrange
        var appealId = "APP-001";
        var appealStatus = new AppealStatus
    {
      AppealId = appealId,
        ClaimId = "CLM-001",
     Status = "under review"
        };

     _mockAppealWorkflow.Setup(s => s.GetAppealStatusAsync(
            It.IsAny<string>()))
     .ReturnsAsync(appealStatus);

      // Act
 var result = await _controller.GetAppealStatus(appealId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAppealStatus_WithEmptyAppealId_ReturnsBadRequest()
    {
  // Arrange
      var appealId = string.Empty;

        // Act
        var result = await _controller.GetAppealStatus(appealId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetDenials Tests

    [Fact]
    public async Task GetDenials_WithoutFilters_ReturnsOkResult()
    {
        // Act
        var result = await _controller.GetDenials(null, null, null);

  // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDenials_WithProviderFilter_ReturnsOkResult()
    {
     // Arrange
        var providerId = "PROV-001";

        // Act
    var result = await _controller.GetDenials(providerId, null, null);

      // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDenials_WithDateRangeFilter_ReturnsOkResult()
    {
        // Arrange
     var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetDenials(null, fromDate, toDate);

    // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region GetReconciliation Tests

    [Fact]
    public async Task GetReconciliation_WithValidDateRange_ReturnsOkResult()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetReconciliation(fromDate, toDate);

        // Assert
    Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetReconciliation_WithInvalidDateRange_ReturnsBadRequest()
{
        // Arrange
        var fromDate = DateTime.UtcNow;
  var toDate = DateTime.UtcNow.AddDays(-1); // toDate before fromDate

   // Act
    var result = await _controller.GetReconciliation(fromDate, toDate);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetClaimSummary Tests

    [Fact]
    public async Task GetClaimSummary_WithValidClaimId_ReturnsOkResult()
 {
    // Arrange
      var claimId = "CLM-001";
        var summary = new RCMSummary
        {
ClaimId = claimId,
 ApprovedItemCount = 2,
      DeniedItemCount = 1
        };

        _mockClaimProcessor.Setup(s => s.GenerateRCMSummaryAsync(
    It.IsAny<ClaimResponse>(),
 It.IsAny<Claim>()))
  .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetClaimSummary(claimId);

        // Assert
Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimSummary_WithEmptyClaimId_ReturnsBadRequest()
  {
        // Arrange
        var claimId = string.Empty;

        // Act
        var result = await _controller.GetClaimSummary(claimId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

 #endregion

    #region Integration Tests

    [Fact]
    public async Task AllEndpoints_ServiceDependenciesAreInjected()
    {
        // Assert - If we got here without exception, DI is working
        Assert.NotNull(_controller);
    }

    [Fact]
 public async Task ProcessClaimResponse_ErrorHandling_ReturnsServerError()
    {
     // Arrange
    var claimId = "CLM-001";
        var response = new ClaimResponse { ClaimId = claimId };

 _mockClaimProcessor.Setup(s => s.ProcessClaimResponseAsync(
      It.IsAny<ClaimResponse>(),
    It.IsAny<Claim>()))
        .ThrowsAsync(new Exception("Database error"));

      // Act
        var result = await _controller.ProcessClaimResponse(claimId, response);

        // Assert
     Assert.IsType<ObjectResult>(result);
        var objectResult = result as ObjectResult;
        Assert.Equal(500, objectResult.StatusCode);
    }

    #endregion
}
