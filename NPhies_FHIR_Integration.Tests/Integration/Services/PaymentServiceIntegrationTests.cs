using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Domain.Entities;
using Moq;
using Xunit;

namespace NPhies_FHIR_Integration.Tests.Integration.Services;

/// <summary>
/// Payment Service Integration Tests
/// Tests the complete payment calculation workflow
/// </summary>
public class PaymentServiceIntegrationTests
{
    private readonly Mock<IPaymentCalculationEngine> _mockEngine;
    private readonly Mock<IClaimRepository> _mockClaimRepository;
    private readonly Mock<ICoverageRepository> _mockCoverageRepository;
  private readonly Mock<ILogger<PaymentService>> _mockLogger;
    private readonly PaymentService _service;

    public PaymentServiceIntegrationTests()
    {
        _mockEngine = new Mock<IPaymentCalculationEngine>();
     _mockClaimRepository = new Mock<IClaimRepository>();
      _mockCoverageRepository = new Mock<ICoverageRepository>();
        _mockLogger = new Mock<ILogger<PaymentService>>();

        _service = new PaymentService(
        _mockEngine.Object,
            _mockClaimRepository.Object,
       _mockCoverageRepository.Object,
  _mockLogger.Object);
    }

    #region Payment Calculation Tests

    [Fact]
  public async Task CalculatePaymentAsync_WithValidClaimAndCoverage_ReturnsSuccessfulResult()
    {
        // Arrange
    var claimId = 123;
  var request = new PaymentCalculationRequest { ClaimId = claimId };

        var claim = new ClaimEntity
        {
            Id = claimId,
     ClaimType = "institutional",
      CoverageId = 1,
      IsInNetwork = true,
      Items = new List<ClaimItemEntity>
  {
                new() { Id = 1, ClaimId = claimId, Sequence = 1, SubmittedAmount = 1000m, AllowedAmount = 1000m, ServiceCode = "99213" }
      }
       };

        var coverage = new CoverageEntity
        {
    Id = 1,
      Subscriber = "Test Patient"
};

        var benefitResult = new ClaimBenefitCalculationResult
  {
            ClaimId = claimId,
       TotalSubmittedAmount = 1000m,
   TotalAllowedAmount = 1000m,
   TotalInsuranceResponsibility = 800m,
     TotalPatientResponsibility = 200m,
        ValidationErrors = new List<string>()
     };

        _mockClaimRepository
           .Setup(r => r.GetByIdAsync(claimId, default))
  .ReturnsAsync(claim);

 _mockCoverageRepository
  .Setup(r => r.GetByIdAsync(1, default))
           .ReturnsAsync(coverage);

        _mockEngine
  .Setup(e => e.CalculateClaimBenefit(It.IsAny<List<ClaimItemBenefitContext>>(), It.IsAny<BenefitConfiguration>()))
        .Returns(benefitResult);

    // Act
        var result = await _service.CalculatePaymentAsync(request);

        // Assert
        Assert.NotNull(result);
       Assert.True(result.IsSuccessful);
        Assert.Equal(claimId, result.ClaimId);
        Assert.Equal(1000m, result.TotalSubmittedAmount);
     Assert.Equal(1000m, result.TotalAllowedAmount);
        Assert.Equal(800m, result.TotalInsuranceResponsibility);
     Assert.Equal(200m, result.TotalPatientResponsibility);

        _mockClaimRepository.Verify(r => r.GetByIdAsync(claimId, default), Times.Once);
     _mockCoverageRepository.Verify(r => r.GetByIdAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task CalculatePaymentAsync_WithInvalidClaimId_ReturnsFailed()
    {
        // Arrange
        var request = new PaymentCalculationRequest { ClaimId = -1 };

  // Act
   var result = await _service.CalculatePaymentAsync(request);

 // Assert
  Assert.NotNull(result);
     Assert.False(result.IsSuccessful);
        Assert.NotNull(result.ErrorMessage);
   Assert.Single(result.ValidationErrors);
    }

  [Fact]
    public async Task CalculatePaymentAsync_WithMissingClaim_ReturnsFailed()
    {
  // Arrange
        var claimId = 999;
        var request = new PaymentCalculationRequest { ClaimId = claimId };

       _mockClaimRepository
        .Setup(r => r.GetByIdAsync(claimId, default))
       .ReturnsAsync((ClaimEntity)null!);

    // Act
  var result = await _service.CalculatePaymentAsync(request);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculatePaymentAsync_WithMissingCoverage_ReturnsFailed()
    {
   // Arrange
    var claimId = 123;
      var request = new PaymentCalculationRequest { ClaimId = claimId };

      var claim = new ClaimEntity
        {
   Id = claimId,
           CoverageId = 999,
     Items = new List<ClaimItemEntity>()
        };

       _mockClaimRepository
           .Setup(r => r.GetByIdAsync(claimId, default))
      .ReturnsAsync(claim);

        _mockCoverageRepository
  .Setup(r => r.GetByIdAsync(999, default))
     .ReturnsAsync((CoverageEntity)null!);

        // Act
        var result = await _service.CalculatePaymentAsync(request);

 // Assert
   Assert.False(result.IsSuccessful);
        Assert.Contains("Coverage", result.ErrorMessage);
    }

  [Fact]
    public async Task CalculatePaymentAsync_WithNoClaimItems_ReturnsFailed()
    {
     // Arrange
       var claimId = 123;
        var request = new PaymentCalculationRequest { ClaimId = claimId };

       var claim = new ClaimEntity
        {
   Id = claimId,
 CoverageId = 1,
            Items = new List<ClaimItemEntity>() // Empty
  };

    var coverage = new CoverageEntity { Id = 1 };

        _mockClaimRepository
    .Setup(r => r.GetByIdAsync(claimId, default))
       .ReturnsAsync(claim);

        _mockCoverageRepository
    .Setup(r => r.GetByIdAsync(1, default))
            .ReturnsAsync(coverage);

        // Act
   var result = await _service.CalculatePaymentAsync(request);

   // Assert
Assert.False(result.IsSuccessful);
   Assert.Single(result.ValidationErrors);
    }

    [Fact]
    public async Task CalculatePaymentAsync_WithCalculationErrors_ReturnsErrorsInResponse()
    {
       // Arrange
   var claimId = 123;
   var request = new PaymentCalculationRequest { ClaimId = claimId };

  var claim = new ClaimEntity
        {
 Id = claimId,
            CoverageId = 1,
   Items = new List<ClaimItemEntity>
        {
  new() { Id = 1, ClaimId = claimId, Sequence = 1, SubmittedAmount = 1000m, AllowedAmount = 500m }
      }
  };

        var coverage = new CoverageEntity { Id = 1 };

var benefitResult = new ClaimBenefitCalculationResult
        {
            ClaimId = claimId,
  TotalSubmittedAmount = 1000m,
           TotalAllowedAmount = 500m,
           ValidationErrors = new List<string> { "Test validation error" }
        };

    _mockClaimRepository
  .Setup(r => r.GetByIdAsync(claimId, default))
     .ReturnsAsync(claim);

        _mockCoverageRepository
 .Setup(r => r.GetByIdAsync(1, default))
      .ReturnsAsync(coverage);

  _mockEngine
  .Setup(e => e.CalculateClaimBenefit(It.IsAny<List<ClaimItemBenefitContext>>(), It.IsAny<BenefitConfiguration>()))
        .Returns(benefitResult);

    // Act
      var result = await _service.CalculatePaymentAsync(request);

        // Assert
 Assert.False(result.IsSuccessful);
        Assert.Single(result.ValidationErrors);
        Assert.Contains("validation errors", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

  #endregion

    #region Payment Summary Tests

    [Fact]
    public async Task GetPaymentSummaryAsync_WithValidClaim_ReturnsSummary()
    {
  // Arrange
     var claimId = 123;

        var claim = new ClaimEntity
    {
  Id = claimId,
 CoverageId = 1,
            Items = new List<ClaimItemEntity>
     {
    new() { Id = 1, ClaimId = claimId, Sequence = 1, SubmittedAmount = 1000m, AllowedAmount = 1000m }
  }
        };

        var coverage = new CoverageEntity { Id = 1 };

     var benefitResult = new ClaimBenefitCalculationResult
        {
       ClaimId = claimId,
            TotalSubmittedAmount = 1000m,
        TotalAllowedAmount = 1000m,
        TotalInsuranceResponsibility = 800m,
            TotalPatientResponsibility = 200m,
           ValidationErrors = new List<string>()
     };

   _mockClaimRepository
   .Setup(r => r.GetByIdAsync(claimId, default))
            .ReturnsAsync(claim);

        _mockCoverageRepository
    .Setup(r => r.GetByIdAsync(1, default))
     .ReturnsAsync(coverage);

       _mockEngine
.Setup(e => e.CalculateClaimBenefit(It.IsAny<List<ClaimItemBenefitContext>>(), It.IsAny<BenefitConfiguration>()))
          .Returns(benefitResult);

        // Act
     var summary = await _service.GetPaymentSummaryAsync(claimId);

        // Assert
     Assert.NotNull(summary);
      Assert.Equal(claimId, summary.ClaimId);
        Assert.Equal(1000m, summary.TotalSubmittedAmount);
        Assert.Equal(1000m, summary.TotalAllowedAmount);
      Assert.Equal(800m, summary.TotalInsurancePays);
        Assert.Equal(200m, summary.TotalPatientPays);
 Assert.Equal(1, summary.ItemCount);
   }

    [Fact]
   public async Task GetPaymentSummaryAsync_WithMissingClaim_ReturnsNull()
   {
        // Arrange
        var claimId = 999;

  _mockClaimRepository
           .Setup(r => r.GetByIdAsync(claimId, default))
 .ReturnsAsync((ClaimEntity)null!);

      // Act
 var summary = await _service.GetPaymentSummaryAsync(claimId);

       // Assert
       Assert.Null(summary);
    }

#endregion

   #region Payment Details Tests

 [Fact]
    public async Task GetPaymentDetailsAsync_WithValidClaim_ReturnsDetails()
    {
        // Arrange
     var claimId = 123;

        var claim = new ClaimEntity
       {
            Id = claimId,
      CoverageId = 1,
  Items = new List<ClaimItemEntity>
           {
   new() { Id = 1, ClaimId = claimId, Sequence = 1, SubmittedAmount = 500m, AllowedAmount = 500m, ServiceCode = "99213" },
       new() { Id = 2, ClaimId = claimId, Sequence = 2, SubmittedAmount = 300m, AllowedAmount = 300m, ServiceCode = "99214" }
     }
        };

      var coverage = new CoverageEntity { Id = 1 };

        var benefitResult = new ClaimBenefitCalculationResult
        {
   ClaimId = claimId,
        TotalSubmittedAmount = 800m,
       TotalAllowedAmount = 800m,
TotalInsuranceResponsibility = 640m,
      TotalPatientResponsibility = 160m,
    ValidationErrors = new List<string>()
        };

    _mockClaimRepository
            .Setup(r => r.GetByIdAsync(claimId, default))
  .ReturnsAsync(claim);

 _mockCoverageRepository
        .Setup(r => r.GetByIdAsync(1, default))
           .ReturnsAsync(coverage);

   _mockEngine
          .Setup(e => e.CalculateClaimBenefit(It.IsAny<List<ClaimItemBenefitContext>>(), It.IsAny<BenefitConfiguration>()))
    .Returns(benefitResult);

        // Act
 var details = await _service.GetPaymentDetailsAsync(claimId);

       // Assert
        Assert.NotNull(details);
        Assert.Equal(claimId, details.ClaimId);
      Assert.NotNull(details.Summary);
 Assert.Equal(2, details.ItemDetails.Count);
        Assert.Equal("99213", details.ItemDetails[0].ServiceDescription);
    Assert.Equal("99214", details.ItemDetails[1].ServiceDescription);
    }

  [Fact]
    public async Task GetPaymentDetailsAsync_WithMissingClaim_ReturnsNull()
    {
  // Arrange
       var claimId = 999;

  _mockClaimRepository
       .Setup(r => r.GetByIdAsync(claimId, default))
      .ReturnsAsync((ClaimEntity)null!);

      // Act
     var details = await _service.GetPaymentDetailsAsync(claimId);

    // Assert
        Assert.Null(details);
    }

    #endregion

    #region SaveCalculation Tests

    [Fact]
    public async Task SaveCalculationAsync_WithValidCalculation_SavesSuccessfully()
    {
      // Arrange
 var calculation = new ClaimPaymentCalculation
      {
   ClaimId = 123,
     SubmittedAmount = 1000m,
          AllowedAmount = 1000m,
        InsuranceResponsibility = 800m,
      PatientResponsibility = 200m,
     IsValid = true
 };

       var claim = new ClaimEntity { Id = 123 };

     _mockClaimRepository
    .Setup(r => r.GetByIdAsync(123, default))
  .ReturnsAsync(claim);

      // Act & Assert
 await _service.SaveCalculationAsync(calculation);

      _mockClaimRepository.Verify(r => r.GetByIdAsync(123, default), Times.Once);
    }

    [Fact]
   public async Task SaveCalculationAsync_WithNullCalculation_ThrowsException()
  {
     // Arrange
  ClaimPaymentCalculation calculation = null!;

        // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SaveCalculationAsync(calculation));
     }

    [Fact]
    public async Task SaveCalculationAsync_WithInvalidClaimId_ThrowsException()
    {
    // Arrange
     var calculation = new ClaimPaymentCalculation { ClaimId = -1 };

    // Act & Assert
 await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveCalculationAsync(calculation));
   }

    #endregion
}
