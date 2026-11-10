using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Application.Services.RCM;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Tests.RCM;

/// <summary>
/// Unit tests for ClaimResponseProcessingService
/// Tests all major functionality for processing claim responses
/// </summary>
public class ClaimResponseProcessingServiceTests
{
    private readonly Mock<ILogger<ClaimResponseProcessingService>> _mockLogger;
    private readonly ClaimResponseProcessingService _service;

    public ClaimResponseProcessingServiceTests()
  {
        _mockLogger = new Mock<ILogger<ClaimResponseProcessingService>>();
  _service = new ClaimResponseProcessingService(_mockLogger.Object);
    }

    #region ProcessClaimResponseAsync Tests

    /// <summary>
    /// Test: Process valid claim response with approved items
    /// </summary>
    [Fact]
    public async Task ProcessClaimResponseAsync_WithApprovedItems_ReturnsSuccessResult()
    {
        // Arrange
var response = CreateMockClaimResponse("CLM-001", approvedCount: 2, deniedCount: 0);
        var claim = CreateMockClaim("CLM-001");

      // Act
        var result = await _service.ProcessClaimResponseAsync(response, claim);

     // Assert
        Assert.NotNull(result);
      Assert.True(result.IsSuccessful);
   Assert.Equal("CLM-001", result.ClaimId);
 Assert.Equal(2, result.ApprovedItemCount);
        Assert.Equal(0, result.DeniedItemCount);
        Assert.Contains("successfully", result.StatusMessage.ToLower());
    }

    /// <summary>
    /// Test: Process claim response with denied items
    /// </summary>
    [Fact]
    public async Task ProcessClaimResponseAsync_WithDeniedItems_IdentifiesDenials()
    {
        // Arrange
        var response = CreateMockClaimResponse("CLM-002", approvedCount: 1, deniedCount: 2);
        var claim = CreateMockClaim("CLM-002");

        // Act
        var result = await _service.ProcessClaimResponseAsync(response, claim);

        // Assert
        Assert.NotNull(result);
     Assert.True(result.IsSuccessful);
        Assert.Equal(1, result.ApprovedItemCount);
        Assert.Equal(2, result.DeniedItemCount);
Assert.True(result.TotalDeniedAmount > 0);
    }

    /// <summary>
    /// Test: Process claim response calculates totals correctly
    /// </summary>
    [Fact]
    public async Task ProcessClaimResponseAsync_CalculatesTotalsCorrectly()
    {
        // Arrange
        var response = CreateMockClaimResponse("CLM-003", approvedCount: 3, deniedCount: 1);
    var claim = CreateMockClaim("CLM-003");

        // Act
        var result = await _service.ProcessClaimResponseAsync(response, claim);

        // Assert
      Assert.NotNull(result);
 Assert.True(result.IsSuccessful);
        Assert.True(result.TotalApprovedAmount > 0, "Approved amount should be greater than 0");
     Assert.True(result.TotalDeniedAmount > 0, "Denied amount should be greater than 0");
        Assert.True(result.TotalPatientResponsibility >= 0, "Patient responsibility should be non-negative");
    }

    /// <summary>
    /// Test: Process claim response with null coverage
    /// </summary>
 [Fact]
    public async Task ProcessClaimResponseAsync_WithNullCoverage_StillProcesses()
    {
        // Arrange
        var response = CreateMockClaimResponse("CLM-004", approvedCount: 1, deniedCount: 0);
      var claim = CreateMockClaim("CLM-004");
      claim.Coverage = null; // No coverage

        // Act
        var result = await _service.ProcessClaimResponseAsync(response, claim);

        // Assert
        Assert.NotNull(result);
      Assert.True(result.IsSuccessful);
        Assert.True(result.TotalPatientResponsibility >= 0);
    }

    /// <summary>
    /// Test: Process claim response with empty response
    /// </summary>
    [Fact]
    public async Task ProcessClaimResponseAsync_WithEmptyResponse_ReturnsValidResult()
{
        // Arrange
      var response = new ClaimResponse
        {
 Id = "RESP-001",
       ClaimId = "CLM-005",
       AddItems = new List<ClaimResponseAddItem>(),
            Totals = new List<ClaimResponseTotal>()
        };
        var claim = CreateMockClaim("CLM-005");

        // Act
 var result = await _service.ProcessClaimResponseAsync(response, claim);

// Assert
   Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.Equal(0, result.ApprovedItemCount);
        Assert.Equal(0, result.DeniedItemCount);
  Assert.Equal(0, result.TotalApprovedAmount);
    }

    #endregion

    #region ExtractAdjudicationDetailsAsync Tests

    /// <summary>
    /// Test: Extract adjudication details from response
    /// </summary>
    [Fact]
    public async Task ExtractAdjudicationDetailsAsync_WithValidResponse_ReturnsDetails()
    {
        // Arrange
        var response = CreateMockClaimResponse("CLM-006", approvedCount: 2, deniedCount: 1);

  // Act
  var details = await _service.ExtractAdjudicationDetailsAsync(response);

     // Assert
   Assert.NotNull(details);
    Assert.NotEmpty(details);
        Assert.True(details.Count > 0);
     Assert.Contains(details, d => d.Status == "approved");
  }

    /// <summary>
    /// Test: Extract handles missing add items gracefully
    /// </summary>
    [Fact]
    public async Task ExtractAdjudicationDetailsAsync_WithNoAddItems_ReturnsEmptyList()
    {
        // Arrange
        var response = new ClaimResponse
        {
         Id = "RESP-002",
            ClaimId = "CLM-007",
            AddItems = null
 };

        // Act
        var details = await _service.ExtractAdjudicationDetailsAsync(response);

        // Assert
        Assert.NotNull(details);
    Assert.Empty(details);
    }

    /// <summary>
    /// Test: Extract correctly maps amounts
    /// </summary>
 [Fact]
    public async Task ExtractAdjudicationDetailsAsync_MapsAmountsCorrectly()
    {
        // Arrange
        const decimal submittedAmount = 1000m;
        const decimal benefitAmount = 800m;

        var addItem = new ClaimResponseAddItem
   {
      Id = "ITEM-001",
            Sequence = 1,
      SubmittedAmount = submittedAmount,
            BenefitAmount = benefitAmount,
  ProductOrServiceDisplay = "Service A",
            Adjudications = new List<ClaimResponseAdjudication>()
        };

        var response = new ClaimResponse
        {
            Id = "RESP-003",
  ClaimId = "CLM-008",
       AddItems = new List<ClaimResponseAddItem> { addItem }
        };

  // Act
     var details = await _service.ExtractAdjudicationDetailsAsync(response);

        // Assert
   Assert.Single(details);
        Assert.Equal(submittedAmount, details.First().SubmittedAmount);
        Assert.Equal(benefitAmount, details.First().AllowedAmount);
    }

  #endregion

    #region CalculatePatientResponsibilityAsync Tests

    /// <summary>
    /// Test: Calculate patient responsibility with deductible
    /// </summary>
    [Fact]
    public async Task CalculatePatientResponsibilityAsync_WithDeductible_CalculatesCorrectly()
    {
     // Arrange
        var response = CreateMockClaimResponse("CLM-009", approvedCount: 1, deniedCount: 0, 
          deductible: 100m, coinsurance: 50m);
        var coverage = new Coverage
     {
    Id = "COV-001",
   AnnualDeductible = 500m,
            DeductibleMet = 0m,
     OutOfPocketMax = 2000m
  };

        // Act
        var result = await _service.CalculatePatientResponsibilityAsync(response, coverage);

   // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalResponsibility > 0);
        Assert.Equal(100m, result.DeductibleAmount);
    }

    /// <summary>
    /// Test: Calculate patient responsibility components sum correctly
    /// </summary>
    [Fact]
    public async Task CalculatePatientResponsibilityAsync_ComponentsSumCorrectly()
    {
        // Arrange
        const decimal deductible = 100m;
        const decimal coinsurance = 50m;
        const decimal oop = 25m;

 var response = CreateMockClaimResponse("CLM-010", approvedCount: 1, deniedCount: 0, 
          deductible: deductible, coinsurance: coinsurance, outOfPocket: oop);
        var coverage = CreateMockCoverage();

        // Act
    var result = await _service.CalculatePatientResponsibilityAsync(response, coverage);

// Assert
        Assert.NotNull(result);
        Assert.Equal(deductible + coinsurance + oop, result.TotalResponsibility);
    }

    #endregion

    #region IdentifyDeniedItemsAsync Tests

    /// <summary>
    /// Test: Identify denied items correctly
    /// </summary>
    [Fact]
    public async Task IdentifyDeniedItemsAsync_WithDeniedItems_ReturnsCorrectList()
    {
        // Arrange
        var response = CreateMockClaimResponse("CLM-011", approvedCount: 1, deniedCount: 2);

        // Act
        var deniedItems = await _service.IdentifyDeniedItemsAsync(response);

    // Assert
        Assert.NotNull(deniedItems);
        Assert.NotEmpty(deniedItems);
        Assert.All(deniedItems, item => Assert.Equal("denied", item.DenialReasonCode == "UNKNOWN" ? "unknown" : item.DenialReasonCode.ToLower()));
    }

    /// <summary>
    /// Test: Identify denied items sets appeal deadline
    /// </summary>
    [Fact]
    public async Task IdentifyDeniedItemsAsync_SetAppealDeadline()
    {
        // Arrange
        var response = CreateMockClaimResponse("CLM-012", approvedCount: 0, deniedCount: 1);

        // Act
        var deniedItems = await _service.IdentifyDeniedItemsAsync(response);

        // Assert
        Assert.NotNull(deniedItems);
        Assert.Single(deniedItems);
      var deniedItem = deniedItems.First();
        Assert.True(deniedItem.AppealDeadline > DateTime.UtcNow);
        Assert.True(deniedItem.CanAppeal);
    }

    /// <summary>
    /// Test: Identify no denied items when all approved
    /// </summary>
    [Fact]
    public async Task IdentifyDeniedItemsAsync_WithAllApproved_ReturnsEmpty()
    {
        // Arrange
     var response = CreateMockClaimResponse("CLM-013", approvedCount: 3, deniedCount: 0);

        // Act
        var deniedItems = await _service.IdentifyDeniedItemsAsync(response);

        // Assert
        Assert.NotNull(deniedItems);
    Assert.Empty(deniedItems);
    }

    #endregion

    #region GenerateRCMSummaryAsync Tests

    /// <summary>
/// Test: Generate RCM summary with complete data
    /// </summary>
    [Fact]
    public async Task GenerateRCMSummaryAsync_WithCompleteData_GeneratesSummary()
    {
        // Arrange
        var response = CreateMockClaimResponse("CLM-014", approvedCount: 2, deniedCount: 1);
        var claim = CreateMockClaim("CLM-014");

        // Act
        var summary = await _service.GenerateRCMSummaryAsync(response, claim);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal("CLM-014", summary.ClaimId);
        Assert.Equal(2, summary.ApprovedItemCount);
        Assert.Equal(1, summary.DeniedItemCount);
        Assert.Equal("generated", summary.ProcessingStatus);
    }

    /// <summary>
    /// Test: RCM summary calculates totals correctly
    /// </summary>
    [Fact]
    public async Task GenerateRCMSummaryAsync_CalculatesTotalsCorrectly()
    {
        // Arrange
        var response = CreateMockClaimResponse("CLM-015", approvedCount: 1, deniedCount: 0);
        var claim = CreateMockClaim("CLM-015");

        // Act
        var summary = await _service.GenerateRCMSummaryAsync(response, claim);

        // Assert
        Assert.NotNull(summary);
        Assert.True(summary.TotalSubmittedAmount > 0);
  Assert.True(summary.TotalAllowedAmount > 0);
        Assert.True(summary.TotalApprovedAmount > 0);
        Assert.True(summary.TotalInsuranceResponsibility > 0);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create a mock claim response for testing
    /// </summary>
    private ClaimResponse CreateMockClaimResponse(string claimId, int approvedCount = 0, int deniedCount = 0,
        decimal deductible = 100m, decimal coinsurance = 50m, decimal outOfPocket = 25m)
    {
  var addItems = new List<ClaimResponseAddItem>();

        // Create approved items
        for (int i = 0; i < approvedCount; i++)
        {
    var adjudications = new List<ClaimResponseAdjudication>
     {
   new ClaimResponseAdjudication
        {
         Id = $"ADJ-APP-{i}",
           AdjudicationCategory = "benefit",
          Amount = 1000m,
    Currency = "SAR"
         }
  };

            addItems.Add(new ClaimResponseAddItem
   {
     Id = $"ITEM-APP-{i}",
   ClaimResponseId = "RESP-MOCK",
  Sequence = i + 1,
   ProductOrServiceCode = "99660000000001",
                ProductOrServiceDisplay = $"Service {i + 1}",
    SubmittedAmount = 1000m,
       BenefitAmount = 1000m,
          IsApproved = true,
    Adjudications = adjudications
            });
    }

 // Create denied items
        for (int i = 0; i < deniedCount; i++)
        {
  var adjudications = new List<ClaimResponseAdjudication>
         {
          new ClaimResponseAdjudication
      {
      Id = $"ADJ-DEN-{i}",
    AdjudicationCategory = "denied",
        Notes = "Service not covered",
       Currency = "SAR"
     }
};

   addItems.Add(new ClaimResponseAddItem
  {
        Id = $"ITEM-DEN-{i}",
    ClaimResponseId = "RESP-MOCK",
              Sequence = approvedCount + i + 1,
      ProductOrServiceCode = "99660000000002",
  ProductOrServiceDisplay = $"Service {approvedCount + i + 1}",
       SubmittedAmount = 500m,
      BenefitAmount = 0m,
    IsApproved = false,
         Adjudications = adjudications
            });
        }

        return new ClaimResponse
        {
       Id = "RESP-MOCK",
       ClaimId = claimId,
        ClaimResponseStatus = "active",
       AddItems = addItems,
            Totals = new List<ClaimResponseTotal>
            {
                new ClaimResponseTotal { Id = "TOT-1", Category = "benefit", Amount = 1000m * approvedCount },
                new ClaimResponseTotal { Id = "TOT-2", Category = "submitted", Amount = (1000m * approvedCount) + (500m * deniedCount) }
      }
 };
    }

    /// <summary>
    /// Create a mock claim for testing
    /// </summary>
    private Claim CreateMockClaim(string claimId)
    {
        return new Claim
        {
            Id = claimId,
      ClaimType = "institutional",
            PatientId = "PAT-001",
            ProviderId = "PROV-001",
            InsurerId = "INS-001",
            Coverage = CreateMockCoverage()
      };
    }

    /// <summary>
    /// Create a mock coverage for testing
    /// </summary>
    private Coverage CreateMockCoverage()
    {
        return new Coverage
        {
            Id = "COV-001",
            PatientId = "PAT-001",
InsurerId = "INS-001",
     CoverageStatus = "active",
            AnnualDeductible = 500m,
          DeductibleMet = 0m,
            OutOfPocketMax = 2000m
    };
    }

    #endregion
}
