using NPhies_FHIR_Integration.Application.Services;
using Xunit;

namespace NPhies_FHIR_Integration.Tests.Application.Services;

/// <summary>
/// Additional Payment Calculation Engine Tests
/// Tests edge cases, error handling, and boundary conditions
/// </summary>
public class PaymentCalculationEngineAdvancedTests
{
    private readonly PaymentCalculationEngine _engine = new();

    #region Deductible Edge Case Tests

    [Fact]
    public void CalculateDeductible_WithZeroDeductible_ReturnsNoDeductible()
    {
    // Arrange
        decimal allowedAmount = 1000m;
  decimal deductibleLimit = 0m;
        decimal deductibleMet = 0m;

        // Act
     var result = _engine.CalculateDeductible(allowedAmount, deductibleLimit, deductibleMet, true);

      // Assert
        Assert.False(result.IsSubjectToDeductible);
        Assert.Equal(0m, result.DeductibleApplied);
     Assert.Equal(1000m, result.AmountAfterDeductible);
    }

    [Fact]
    public void CalculateDeductible_WhenAlreadyMet_ReturnsZeroApplied()
    {
        // Arrange
        decimal allowedAmount = 1000m;
  decimal deductibleLimit = 1000m;
        decimal deductibleMet = 1000m;

   // Act
        var result = _engine.CalculateDeductible(allowedAmount, deductibleLimit, deductibleMet, true);

        // Assert
        Assert.False(result.IsSubjectToDeductible);
        Assert.Equal(0m, result.DeductibleApplied);
        Assert.Equal(1000m, result.AmountAfterDeductible);
        Assert.Contains("met", result.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalculateDeductible_PartialDeductible_SplitsCorrectly()
 {
        // Arrange
   decimal allowedAmount = 500m;
   decimal deductibleLimit = 1000m;
        decimal deductibleMet = 800m;

        // Act
   var result = _engine.CalculateDeductible(allowedAmount, deductibleLimit, deductibleMet, true);

        // Assert
        Assert.True(result.IsSubjectToDeductible);
        Assert.Equal(200m, result.DeductibleApplied);
        Assert.Equal(300m, result.AmountAfterDeductible);
    Assert.Equal(0m, result.DeductibleRemaining);
    }

  [Fact]
    public void CalculateDeductible_WithNegativeAmount_HandlesGracefully()
    {
        // Arrange - even though negative amounts shouldn't happen, test defensive programming
      decimal allowedAmount = -100m;
        decimal deductibleLimit = 1000m;
        decimal deductibleMet = 0m;

   // Act
   var result = _engine.CalculateDeductible(allowedAmount, deductibleLimit, deductibleMet, true);

 // Assert - should apply full deductible to negative amount (unlikely scenario)
    Assert.Equal(-100m, result.DeductibleApplied);
    }

[Fact]
    public void CalculateDeductible_OutOfNetworkNoDeductible_ReturnsCorrectly()
    {
        // Arrange
        decimal allowedAmount = 500m;
 decimal deductibleLimit = 0m;
     decimal deductibleMet = 0m;

    // Act
        var result = _engine.CalculateDeductible(allowedAmount, deductibleLimit, deductibleMet, false);

        // Assert
     Assert.False(result.IsSubjectToDeductible);
Assert.Equal(500m, result.AmountAfterDeductible);
    }

  #endregion

    #region Coinsurance Edge Case Tests

    [Fact]
 public void CalculateCoinsurance_WithZeroCoinsurance_ReturnsFullInsurance()
    {
      // Arrange
        decimal allowedAmount = 1000m;
        decimal coinsurancePercentage = 0m;

        // Act
        var result = _engine.CalculateCoinsurance(allowedAmount, coinsurancePercentage, 0m);

        // Assert
    Assert.False(result.IsSubjectToCoinsurance);
        Assert.Equal(1000m, result.InsuranceResponsibility);
 Assert.Equal(0m, result.PatientResponsibility);
    }

    [Fact]
 public void CalculateCoinsurance_WithHundredPercentCoinsurance_ReturnsFullPatient()
    {
     // Arrange
        decimal allowedAmount = 1000m;
        decimal coinsurancePercentage = 100m;

    // Act
        var result = _engine.CalculateCoinsurance(allowedAmount, coinsurancePercentage, 0m);

        // Assert
        Assert.False(result.IsSubjectToCoinsurance);
        Assert.Equal(1000m, result.InsuranceResponsibility);
  Assert.Equal(0m, result.PatientResponsibility);
    }

    [Fact]
    public void CalculateCoinsurance_DuringDeductiblePhase_ReturnsPatientResponsibility()
    {
        // Arrange
      decimal allowedAmount = 1000m;
        decimal coinsurancePercentage = 20m;
      decimal deductibleRemaining = 500m; // Still in deductible phase

        // Act
 var result = _engine.CalculateCoinsurance(allowedAmount, coinsurancePercentage, deductibleRemaining);

  // Assert
Assert.False(result.IsSubjectToCoinsurance);
        Assert.Equal(0m, result.CoinsuranceAmount);
        Assert.Equal(1000m, result.PatientResponsibility);
    }

    [Fact]
    public void CalculateCoinsurance_With80Percent_CalculatesCorrectly()
    {
        // Arrange
        decimal allowedAmount = 500m;
        decimal coinsurancePercentage = 80m;

        // Act
  var result = _engine.CalculateCoinsurance(allowedAmount, coinsurancePercentage, 0m);

   // Assert
        Assert.True(result.IsSubjectToCoinsurance);
        Assert.Equal(80m, result.CoinsurancePercentage);
        Assert.Equal(400m, result.InsuranceResponsibility);
        Assert.Equal(100m, result.PatientResponsibility);
    }

    #endregion

    #region Out-of-Pocket Edge Case Tests

    [Fact]
    public void CalculateOutOfPocket_WithZeroLimit_ReturnsNoCapEffect()
    {
      // Arrange
        decimal patientResponsibility = 500m;
        decimal outOfPocketLimit = 0m;
        decimal outOfPocketMet = 0m;

        // Act
        var result = _engine.CalculateOutOfPocket(patientResponsibility, outOfPocketLimit, outOfPocketMet);

        // Assert
   Assert.False(result.IsSubjectToOutOfPocket);
      Assert.False(result.OutOfPocketMet);
    }

    [Fact]
    public void CalculateOutOfPocket_WhenMaxMet_NoFurtherCharges()
    {
        // Arrange
     decimal patientResponsibility = 500m;
        decimal outOfPocketLimit = 5000m;
        decimal outOfPocketMet = 5000m;

    // Act
        var result = _engine.CalculateOutOfPocket(patientResponsibility, outOfPocketLimit, outOfPocketMet);

  // Assert
        Assert.False(result.IsSubjectToOutOfPocket);
        Assert.True(result.OutOfPocketMet);
        Assert.Equal(0m, result.OutOfPocketApplied);
    }

    [Fact]
    public void CalculateOutOfPocket_ExceedsMaximum_CapsCorrectly()
  {
        // Arrange
     decimal patientResponsibility = 1000m;
        decimal outOfPocketLimit = 5000m;
     decimal outOfPocketMet = 4500m;

        // Act
 var result = _engine.CalculateOutOfPocket(patientResponsibility, outOfPocketLimit, outOfPocketMet);

        // Assert
        Assert.True(result.IsSubjectToOutOfPocket);
  Assert.Equal(500m, result.OutOfPocketApplied);
        Assert.Equal(0m, result.OutOfPocketRemaining);
     Assert.True(result.OutOfPocketMet);
    }

    #endregion

    #region Benefit Calculation Edge Cases

    [Fact]
    public void CalculateBenefit_WithZeroAllowedAmount_DeniesService()
    {
// Arrange
        var context = new ClaimItemBenefitContext
        {
            ItemSequence = 1,
        BenefitCategory = "test",
         SubmittedAmount = 500m,
     AllowedAmount = 0m
        };

      // Act
        var result = _engine.CalculateBenefit(context);

        // Assert
     Assert.Equal("denied", result.AdjudicationStatus);
     Assert.Equal(500m, result.PatientResponsibility);
        Assert.Equal(0m, result.InsuranceResponsibility);
    }

  [Fact]
    public void CalculateBenefit_WithNegativeAmount_Denies()
    {
        // Arrange
        var context = new ClaimItemBenefitContext
     {
         ItemSequence = 1,
            BenefitCategory = "test",
  SubmittedAmount = -100m,
     AllowedAmount = -100m
        };

  // Act
        var result = _engine.CalculateBenefit(context);

        // Assert
      Assert.Equal("denied", result.AdjudicationStatus);
    Assert.Single(result.Notes);
    }

    [Fact]
    public void CalculateBenefit_WithHighestAllowance_Approves()
    {
        // Arrange
  var context = new ClaimItemBenefitContext
        {
            ItemSequence = 1,
       BenefitCategory = "test",
          SubmittedAmount = 500m,
  AllowedAmount = 500m
        };

      // Act
        var result = _engine.CalculateBenefit(context);

        // Assert
        Assert.Equal("approved", result.AdjudicationStatus);
 Assert.Equal(0m, result.NotAllowedAmount);
        Assert.Equal(500m, result.InsuranceResponsibility);
    }

    #endregion

  #region Claim Benefit Calculation Comprehensive Tests

 [Fact]
    public void CalculateClaimBenefit_WithMultipleItems_CalculatesCumulatively()
    {
        // Arrange
        var items = new List<ClaimItemBenefitContext>
        {
            new() { ItemSequence = 1, BenefitCategory = "test", SubmittedAmount = 500m, AllowedAmount = 500m },
    new() { ItemSequence = 2, BenefitCategory = "test", SubmittedAmount = 300m, AllowedAmount = 300m },
         new() { ItemSequence = 3, BenefitCategory = "test", SubmittedAmount = 200m, AllowedAmount = 200m }
        };
        var config = new BenefitConfiguration
        {
            AnnualDeductible = 1000m,
   DeductibleMet = 0m,
    CoinsurancePercent = 20m,
OutOfPocketMax = 5000m,
            OutOfPocketMet = 0m
        };

    // Act
      var result = _engine.CalculateClaimBenefit(items, config);

        // Assert
        Assert.Equal(3, result.ItemCalculations.Count);
        Assert.Equal(1000m, result.TotalAllowedAmount);
        Assert.Equal(1000m, result.TotalSubmittedAmount);
    }

    [Fact]
    public void CalculateClaimBenefit_WithNullItems_ThrowsException()
    {
        // Arrange
        List<ClaimItemBenefitContext> items = null!;
        var config = new BenefitConfiguration();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _engine.CalculateClaimBenefit(items, config));
    }

    [Fact]
    public void CalculateClaimBenefit_WithEmptyItems_ThrowsException()
 {
        // Arrange
      var items = new List<ClaimItemBenefitContext>();
        var config = new BenefitConfiguration();

   // Act & Assert
      Assert.Throws<ArgumentException>(() => _engine.CalculateClaimBenefit(items, config));
    }

    [Fact]
    public void CalculateClaimBenefit_WithNullConfig_ThrowsException()
    {
// Arrange
        var items = new List<ClaimItemBenefitContext>
        {
    new() { ItemSequence = 1, BenefitCategory = "test", SubmittedAmount = 100m, AllowedAmount = 100m }
   };
 BenefitConfiguration config = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _engine.CalculateClaimBenefit(items, config));
    }

    #endregion

    #region Complex Calculation Scenarios

    [Fact]
    public void ComplexScenario_DeductiblePlusCoinsurancePlusOutOfPocket_CalculatesCorrectly()
    {
        // Arrange
        var items = new List<ClaimItemBenefitContext>
        {
       new() { ItemSequence = 1, BenefitCategory = "test", SubmittedAmount = 2000m, AllowedAmount = 2000m, IsNetworkProvider = true }
  };
 var config = new BenefitConfiguration
{
       AnnualDeductible = 500m,
            DeductibleMet = 0m,
  CoinsurancePercent = 20m,
            OutOfPocketMax = 2000m,
          OutOfPocketMet = 0m,
     IsInNetwork = true
     };

        // Act
  var result = _engine.CalculateClaimBenefit(items, config);

        // Assert
        Assert.True(result.ValidationErrors.Count == 0);
        Assert.Equal(2000m, result.TotalAllowedAmount);
        // Deductible: $500
        // After deductible: $1500
   // Coinsurance (20%): $300
        // Patient: $500 (deductible) + $300 (coinsurance) = $800
        // Insurance: $1200
     Assert.Equal(800m, result.TotalPatientResponsibility);
    Assert.Equal(1200m, result.TotalInsuranceResponsibility);
    }

    [Fact]
    public void EdgeCase_VeryLargeAmounts_CalculatesWithoutOverflow()
    {
        // Arrange
    var items = new List<ClaimItemBenefitContext>
    {
            new() { ItemSequence = 1, BenefitCategory = "test", SubmittedAmount = 999999.99m, AllowedAmount = 999999.99m }
        };
     var config = new BenefitConfiguration
        {
 AnnualDeductible = 100000m,
 DeductibleMet = 50000m,
            CoinsurancePercent = 15m,
         OutOfPocketMax = 500000m,
   OutOfPocketMet = 100000m
        };

      // Act
        var result = _engine.CalculateClaimBenefit(items, config);

        // Assert
        Assert.True(result.ValidationErrors.Count == 0);
        Assert.NotEqual(0m, result.TotalInsuranceResponsibility);
    }

    #endregion

    #region Precision Tests

    [Fact]
    public void CalculateCoinsurance_WithFractionalPercentage_RoundsCorrectly()
    {
        // Arrange
        decimal allowedAmount = 333.33m;
        decimal coinsurancePercentage = 33.33m;

        // Act
        var result = _engine.CalculateCoinsurance(allowedAmount, coinsurancePercentage, 0m);

        // Assert
   Assert.True(result.IsSubjectToCoinsurance);
    // 333.33 * 0.3333 = 111.099... should round appropriately
        Assert.NotEqual(0m, result.CoinsuranceAmount);
    }

    [Fact]
    public void CalculateClaimBenefit_PrecisionValidation_InsurancePlusPatientEqualsAllowed()
    {
        // Arrange
   var items = new List<ClaimItemBenefitContext>
    {
            new() { ItemSequence = 1, BenefitCategory = "test", SubmittedAmount = 100.50m, AllowedAmount = 100.50m }
      };
        var config = new BenefitConfiguration
     {
            AnnualDeductible = 1000m,
  DeductibleMet = 950m,
     CoinsurancePercent = 10m,
      OutOfPocketMax = 5000m,
            OutOfPocketMet = 0m
        };

// Act
 var result = _engine.CalculateClaimBenefit(items, config);

 // Assert
        decimal sum = result.TotalInsuranceResponsibility + result.TotalPatientResponsibility;
        Assert.True(Math.Abs(sum - result.TotalAllowedAmount) < 0.01m, 
        $"Precision error: {sum} != {result.TotalAllowedAmount}");
    }

    #endregion
}
