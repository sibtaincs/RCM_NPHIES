using Xunit;
using NPhies_FHIR_Integration.Application.Services;

namespace NPhies_FHIR_Integration.Tests.Application.Services
{
    /// <summary>
    /// Unit tests for PaymentCalculationEngine
    /// </summary>
    public class PaymentCalculationEngineTests
    {
        private readonly PaymentCalculationEngine _engine;

 public PaymentCalculationEngineTests()
{
      _engine = new PaymentCalculationEngine();
     }

        #region Deductible Tests

        [Fact]
   public void CalculateDeductible_WhenDeductibleNotMet_AppliesDeductible()
        {
          // Arrange
        decimal allowedAmount = 1000;
            decimal deductibleLimit = 500;
       decimal deductibleMet = 0;

        // Act
            var result = _engine.CalculateDeductible(allowedAmount, deductibleLimit, deductibleMet, true);

      // Assert
       Assert.True(result.IsSubjectToDeductible);
      Assert.Equal(500, result.DeductibleApplied);
      Assert.Equal(500, result.AmountAfterDeductible);
     Assert.Equal(0, result.DeductibleRemaining);
 }

   [Fact]
   public void CalculateDeductible_WhenPartialDeductibleRemaining_AppliesPartial()
        {
  // Arrange
   decimal allowedAmount = 1000;
        decimal deductibleLimit = 500;
         decimal deductibleMet = 300; // $300 already used

            // Act
     var result = _engine.CalculateDeductible(allowedAmount, deductibleLimit, deductibleMet, true);

        // Assert
            Assert.True(result.IsSubjectToDeductible);
            Assert.Equal(200, result.DeductibleApplied); // Only $200 remaining
            Assert.Equal(800, result.AmountAfterDeductible);
            Assert.Equal(0, result.DeductibleRemaining);
    }

        [Fact]
        public void CalculateDeductible_WhenDeductibleMet_NoDeductibleApplied()
        {
 // Arrange
       decimal allowedAmount = 1000;
   decimal deductibleLimit = 500;
            decimal deductibleMet = 500; // Fully used

      // Act
    var result = _engine.CalculateDeductible(allowedAmount, deductibleLimit, deductibleMet, true);

       // Assert
       Assert.False(result.IsSubjectToDeductible);
   Assert.Equal(0, result.DeductibleApplied);
         Assert.Equal(1000, result.AmountAfterDeductible);
            Assert.Equal(0, result.DeductibleRemaining);
        }

   [Fact]
        public void CalculateDeductible_WhenAllowedLessThanDeductible_AppliesOnlyAllowed()
        {
            // Arrange
            decimal allowedAmount = 300;
            decimal deductibleLimit = 500;
     decimal deductibleMet = 0;

        // Act
            var result = _engine.CalculateDeductible(allowedAmount, deductibleLimit, deductibleMet, true);

         // Assert
  Assert.True(result.IsSubjectToDeductible);
        Assert.Equal(300, result.DeductibleApplied);
            Assert.Equal(0, result.AmountAfterDeductible);
Assert.Equal(200, result.DeductibleRemaining);
        }

        #endregion

   #region Coinsurance Tests

        [Fact]
        public void CalculateCoinsurance_WhenAfterDeductible_AppliesCoinsurance()
        {
    // Arrange
            decimal allowedAmount = 1000;
        decimal coinsurancePercent = 20; // Patient pays 20%
       decimal deductibleRemaining = 0;

            // Act
        var result = _engine.CalculateCoinsurance(allowedAmount, coinsurancePercent, deductibleRemaining);

         // Assert
      Assert.True(result.IsSubjectToCoinsurance);
            Assert.Equal(200, result.CoinsuranceAmount); // 20% of $1000
         Assert.Equal(800, result.InsuranceResponsibility); // 80%
       Assert.Equal(200, result.PatientResponsibility); // 20%
        }

[Fact]
  public void CalculateCoinsurance_WhenStillInDeductible_NoCoinsurance()
     {
          // Arrange
      decimal allowedAmount = 1000;
 decimal coinsurancePercent = 20;
          decimal deductibleRemaining = 300; // Still in deductible

            // Act
var result = _engine.CalculateCoinsurance(allowedAmount, coinsurancePercent, deductibleRemaining);

        // Assert
      Assert.False(result.IsSubjectToCoinsurance);
         Assert.Equal(0, result.CoinsuranceAmount);
    Assert.Equal(0, result.InsuranceResponsibility);
            Assert.Equal(1000, result.PatientResponsibility); // Full amount to patient (deductible)
        }

        [Fact]
 public void CalculateCoinsurance_With100Percent_NoPatientResponsibility()
    {
      // Arrange
            decimal allowedAmount = 1000;
 decimal coinsurancePercent = 100;
            decimal deductibleRemaining = 0;

      // Act
        var result = _engine.CalculateCoinsurance(allowedAmount, coinsurancePercent, deductibleRemaining);

    // Assert
          Assert.False(result.IsSubjectToCoinsurance);
            Assert.Equal(1000, result.InsuranceResponsibility);
            Assert.Equal(0, result.PatientResponsibility);
        }

        [Fact]
        public void CalculateCoinsurance_With0Percent_FullCoverage()
     {
      // Arrange
      decimal allowedAmount = 1000;
 decimal coinsurancePercent = 0;
   decimal deductibleRemaining = 0;

            // Act
   var result = _engine.CalculateCoinsurance(allowedAmount, coinsurancePercent, deductibleRemaining);

  // Assert
 Assert.False(result.IsSubjectToCoinsurance);
Assert.Equal(1000, result.InsuranceResponsibility);
  Assert.Equal(0, result.PatientResponsibility);
        }

    #endregion

        #region Out-of-Pocket Tests

        [Fact]
        public void CalculateOutOfPocket_WhenNotMet_AppliesOOP()
    {
       // Arrange
     decimal patientResponsibility = 500;
   decimal outOfPocketMax = 2000;
            decimal outOfPocketMet = 0;

            // Act
     var result = _engine.CalculateOutOfPocket(patientResponsibility, outOfPocketMax, outOfPocketMet);

   // Assert
            Assert.True(result.IsSubjectToOutOfPocket);
            Assert.Equal(500, result.OutOfPocketApplied);
            Assert.Equal(1500, result.OutOfPocketRemaining);
      Assert.False(result.OutOfPocketMet);
     }

        [Fact]
        public void CalculateOutOfPocket_WhenMaxReached_CoversRemainder()
  {
            // Arrange
      decimal patientResponsibility = 500;
 decimal outOfPocketMax = 2000;
        decimal outOfPocketMet = 1800; // Only $200 remaining

   // Act
       var result = _engine.CalculateOutOfPocket(patientResponsibility, outOfPocketMax, outOfPocketMet);

  // Assert
            Assert.True(result.IsSubjectToOutOfPocket);
            Assert.Equal(200, result.OutOfPocketApplied); // Only $200 to meet max
            Assert.Equal(0, result.OutOfPocketRemaining);
    Assert.True(result.OutOfPocketMet); // Max is now met
        }

        [Fact]
        public void CalculateOutOfPocket_WhenAlreadyMet_NoCoverage()
        {
   // Arrange
   decimal patientResponsibility = 500;
        decimal outOfPocketMax = 2000;
            decimal outOfPocketMet = 2000; // Already met

            // Act
            var result = _engine.CalculateOutOfPocket(patientResponsibility, outOfPocketMax, outOfPocketMet);

            // Assert
      Assert.False(result.IsSubjectToOutOfPocket);
Assert.Equal(0, result.OutOfPocketApplied);
Assert.Equal(0, result.OutOfPocketRemaining);
     Assert.True(result.OutOfPocketMet);
  }

        #endregion

        #region Complete Claim Benefit Tests

  [Fact]
        public void CalculateClaimBenefit_SimpleScenario_CalculatesCorrectly()
        {
            // Arrange
            var items = new List<ClaimItemBenefitContext>
      {
         new()
      {
          ItemSequence = 1,
 BenefitCategory = "medical",
           SubmittedAmount = 1000,
            AllowedAmount = 800,
            IsNetworkProvider = true
                }
   };

            var benefitConfig = new BenefitConfiguration
            {
AnnualDeductible = 500,
     DeductibleMet = 0,
      CoinsurancePercent = 20,
        OutOfPocketMax = 5000,
    OutOfPocketMet = 0,
        IsInNetwork = true
   };

      // Act
            var result = _engine.CalculateClaimBenefit(items, benefitConfig);

   // Assert
     Assert.Single(result.ItemCalculations);
 Assert.Equal(1000, result.TotalSubmittedAmount);
         Assert.Equal(800, result.TotalAllowedAmount);
  Assert.Equal(500, result.TotalDeductibleApplied);
      Assert.Equal(60, result.TotalCoinsuranceApplied); // 20% of $300
       Assert.Equal(240, result.TotalInsuranceResponsibility); // $300 after deductible - $60 coinsurance
Assert.Equal(560, result.TotalPatientResponsibility); // $500 deductible + $60 coinsurance
        }

        [Fact]
      public void CalculateClaimBenefit_MultipleItems_CalculatesCorrectly()
        {
       // Arrange
       var items = new List<ClaimItemBenefitContext>
    {
             new()
   {
          ItemSequence = 1,
         BenefitCategory = "medical",
              SubmittedAmount = 1000,
  AllowedAmount = 800,
         IsNetworkProvider = true
    },
        new()
    {
          ItemSequence = 2,
          BenefitCategory = "medical",
SubmittedAmount = 500,
        AllowedAmount = 400,
     IsNetworkProvider = true
          }
     };

     var benefitConfig = new BenefitConfiguration
  {
         AnnualDeductible = 500,
                DeductibleMet = 300,
         CoinsurancePercent = 20,
         OutOfPocketMax = 5000,
OutOfPocketMet = 0,
        IsInNetwork = true
            };

            // Act
            var result = _engine.CalculateClaimBenefit(items, benefitConfig);

      // Assert
  Assert.Equal(2, result.ItemCalculations.Count);
     Assert.Equal(1500, result.TotalSubmittedAmount);
  Assert.Equal(1200, result.TotalAllowedAmount);
            Assert.NotEmpty(result.ItemCalculations);
        }

     [Fact]
        public void CalculateClaimBenefit_WithOutOfPocketMax_CalculatesCorrectly()
        {
 // Arrange
            var items = new List<ClaimItemBenefitContext>
            {
    new()
                {
    ItemSequence = 1,
         BenefitCategory = "medical",
        SubmittedAmount = 10000,
          AllowedAmount = 8000,
                    IsNetworkProvider = true
    }
      };

            var benefitConfig = new BenefitConfiguration
            {
AnnualDeductible = 1000,
              DeductibleMet = 0,
        CoinsurancePercent = 20,
        OutOfPocketMax = 2000,
       OutOfPocketMet = 0,
         IsInNetwork = true
            };

    // Act
     var result = _engine.CalculateClaimBenefit(items, benefitConfig);

      // Assert
            Assert.Single(result.ItemCalculations);
       Assert.Equal(1000, result.TotalDeductibleApplied);
Assert.True(result.ItemCalculations[0].OutOfPocketRemaining >= 0);
        }

        [Fact]
        public void CalculateClaimBenefit_NullItems_ThrowsException()
{
          // Arrange
       var benefitConfig = new BenefitConfiguration();

    // Act & Assert
            Assert.Throws<ArgumentException>(() => _engine.CalculateClaimBenefit(null, benefitConfig));
     }

        [Fact]
        public void CalculateClaimBenefit_EmptyItems_ThrowsException()
    {
       // Arrange
   var items = new List<ClaimItemBenefitContext>();
      var benefitConfig = new BenefitConfiguration();

     // Act & Assert
            Assert.Throws<ArgumentException>(() => _engine.CalculateClaimBenefit(items, benefitConfig));
        }

      [Fact]
        public void CalculateClaimBenefit_NullBenefitConfig_ThrowsException()
        {
        // Arrange
            var items = new List<ClaimItemBenefitContext>
            {
     new() { ItemSequence = 1, AllowedAmount = 100 }
            };

      // Act & Assert
       Assert.Throws<ArgumentNullException>(() => _engine.CalculateClaimBenefit(items, null));
        }

        #endregion

        #region Edge Cases

      [Fact]
 public void CalculateClaimBenefit_ZeroAmounts_HandlesGracefully()
     {
    // Arrange
 var items = new List<ClaimItemBenefitContext>
   {
     new()
            {
               ItemSequence = 1,
      BenefitCategory = "medical",
            SubmittedAmount = 0,
       AllowedAmount = 0
     }
            };

            var benefitConfig = new BenefitConfiguration
    {
AnnualDeductible = 500,
     DeductibleMet = 0,
                CoinsurancePercent = 20,
             OutOfPocketMax = 5000,
   OutOfPocketMet = 0
   };

            // Act
    var result = _engine.CalculateClaimBenefit(items, benefitConfig);

            // Assert
            Assert.Single(result.ItemCalculations);
         Assert.Equal(0, result.TotalAllowedAmount);
        }

        [Fact]
   public void CalculateClaimBenefit_NegativeAmounts_DeniesItem()
        {
            // Arrange
            var items = new List<ClaimItemBenefitContext>
   {
                new()
  {
   ItemSequence = 1,
    SubmittedAmount = -100,
      AllowedAmount = -100
         }
    };

        var benefitConfig = new BenefitConfiguration();

     // Act
     var result = _engine.CalculateClaimBenefit(items, benefitConfig);

            // Assert
     Assert.Equal("denied", result.ItemCalculations[0].AdjudicationStatus);
        }

        [Fact]
    public void CalculateClaimBenefit_HighCoinsurance_CalculatesCorrectly()
        {
     // Arrange
     var items = new List<ClaimItemBenefitContext>
  {
  new()
     {
     ItemSequence = 1,
           SubmittedAmount = 1000,
              AllowedAmount = 1000,
     IsNetworkProvider = false // Out of network
 }
            };

       var benefitConfig = new BenefitConfiguration
  {
          AnnualDeductible = 0,
       DeductibleMet = 0,
  CoinsurancePercent = 50, // High coinsurance
                OutOfPocketMax = 10000,
 OutOfPocketMet = 0
            };

 // Act
          var result = _engine.CalculateClaimBenefit(items, benefitConfig);

            // Assert
      Assert.Equal(500, result.TotalCoinsuranceApplied);
            Assert.Equal(500, result.TotalInsuranceResponsibility);
      Assert.Equal(500, result.TotalPatientResponsibility);
        }

        #endregion
 }
}
