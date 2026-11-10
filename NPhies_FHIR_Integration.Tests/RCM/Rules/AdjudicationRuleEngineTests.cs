using Microsoft.Extensions.Logging;
using Xunit;
using NPhies_FHIR_Integration.Application.Services.RCM.Rules;

namespace NPhies_FHIR_Integration.Tests.RCM.Rules;

/// <summary>
/// Integration tests for AdjudicationRuleEngine
/// Tests complete rule execution pipeline
/// </summary>
public class AdjudicationRuleEngineTests
{
    private readonly ILogger<AdjudicationRuleEngine> _logger;
    private readonly ILogger<DeductibleRule> _deductibleLogger;
  private readonly ILogger<CopayRule> _copayLogger;
    private readonly ILogger<CoinsuranceRule> _coinsuranceLogger;
    private readonly ILogger<OutOfPocketRule> _oopLogger;

    public AdjudicationRuleEngineTests()
    {
      _logger = new NullLogger<AdjudicationRuleEngine>();
        _deductibleLogger = new NullLogger<DeductibleRule>();
        _copayLogger = new NullLogger<CopayRule>();
        _coinsuranceLogger = new NullLogger<CoinsuranceRule>();
        _oopLogger = new NullLogger<OutOfPocketRule>();
    }

    [Fact]
    public async Task ExecuteAsync_WithDeductibleOnly()
    {
        // Arrange
  var engine = new AdjudicationRuleEngine(_logger);
    engine.RegisterRules(new DeductibleRule(_deductibleLogger));

        var context = new AdjudicationContext
        {
            ItemSequence = 1,
            ServiceCode = "99213",
SubmittedAmount = 150m,
        AllowedAmount = 100m,
AnnualDeductible = 1000m,
      DeductibleMet = 800m,
    CopayAmount = 0m,
            CoinsurancePercentage = 0m
      };

     // Act
        var result = await engine.ExecuteAsync(context);

        // Assert
        Assert.True(result.IsSuccessful);
      Assert.Equal(1, result.ExecutedRules.Count);
  Assert.Equal(100m, result.PatientResponsibility); // Entire amount applies to deductible
 Assert.Equal(0m, result.InsuranceResponsibility);
    }

    [Fact]
 public async Task ExecuteAsync_WithDeductibleAndCopay()
    {
        // Arrange
        var engine = new AdjudicationRuleEngine(_logger);
        engine.RegisterRules(
    new DeductibleRule(_deductibleLogger),
            new CopayRule(_copayLogger)
   );

        var context = new AdjudicationContext
        {
       ItemSequence = 1,
     ServiceCode = "99213",
   SubmittedAmount = 150m,
            AllowedAmount = 100m,
      AnnualDeductible = 1000m,
    DeductibleMet = 950m, // Only $50 deductible remaining
 CopayAmount = 25m,
    CoinsurancePercentage = 0m
        };

        // Act
   var result = await engine.ExecuteAsync(context);

        // Assert
        Assert.True(result.IsSuccessful);
 Assert.Equal(2, result.ExecutedRules.Count);
        // Deductible: $50, Copay: $25
   Assert.Equal(75m, result.PatientResponsibility);
  Assert.Equal(25m, result.InsuranceResponsibility);
    }

    [Fact]
    public async Task ExecuteAsync_CompleteScenario_DeductibleCopayCoinsurance()
    {
        // Arrange
  var engine = new AdjudicationRuleEngine(_logger);
        engine.RegisterRules(
  new DeductibleRule(_deductibleLogger),
  new CopayRule(_copayLogger),
        new CoinsuranceRule(_coinsuranceLogger)
  );

        var context = new AdjudicationContext
        {
         ItemSequence = 1,
   ServiceCode = "99213",
   SubmittedAmount = 200m,
      AllowedAmount = 100m,
         AnnualDeductible = 1000m,
   DeductibleMet = 1000m, // Deductible fully met
     CopayAmount = 25m,
   CoinsurancePercentage = 20m // Patient pays 20%, insurance 80%
     };

     // Act
        var result = await engine.ExecuteAsync(context);

  // Assert
        Assert.True(result.IsSuccessful);
    Assert.Equal(3, result.ExecutedRules.Count);

        // Copay: $25
        // Remaining: $75
        // Coinsurance (20%): $75 * 20% = $15
    Assert.Equal(40m, result.PatientResponsibility); // $25 copay + $15 coinsurance
        Assert.Equal(60m, result.InsuranceResponsibility); // $75 * 80%
    }

    [Fact]
    public async Task ExecuteAsync_WithOutOfPocketMaximum()
    {
        // Arrange
  var engine = new AdjudicationRuleEngine(_logger);
        engine.RegisterRules(
           new CoinsuranceRule(_coinsuranceLogger),
    new OutOfPocketRule(_oopLogger)
        );

        var context = new AdjudicationContext
        {
       ItemSequence = 1,
        ServiceCode = "99213",
 SubmittedAmount = 200m,
           AllowedAmount = 100m,
    CoinsurancePercentage = 20m,
          OutOfPocketMax = 5000m,
      OutOfPocketMet = 4950m // Patient almost hit OOP max
        };

    // Act
   var result = await engine.ExecuteAsync(context);

        // Assert
        Assert.True(result.IsSuccessful);
 Assert.Equal(2, result.ExecutedRules.Count);

  // Coinsurance would be $20, but OOP max is $50 away
  // So patient pays only $50 (to hit OOP max)
  Assert.Equal(50m, result.PatientResponsibility);
  Assert.Equal(50m, result.InsuranceResponsibility); // Insurance pays remainder
    }

    [Fact]
    public async Task ExecuteAsync_RulesExecuteInPriorityOrder()
    {
     // Arrange
        var engine = new AdjudicationRuleEngine(_logger);
 
   // Register in reverse order - engine should sort by priority
        engine.RegisterRules(
    new OutOfPocketRule(_oopLogger),       // Priority 40
      new CopayRule(_copayLogger),         // Priority 20
    new DeductibleRule(_deductibleLogger) // Priority 10
        );

        var context = new AdjudicationContext
        {
           ItemSequence = 1,
        AllowedAmount = 100m,
            AnnualDeductible = 1000m,
        DeductibleMet = 900m,
 CopayAmount = 25m,
     OutOfPocketMax = 5000m,
           OutOfPocketMet = 4900m
    };

 // Act
   var result = await engine.ExecuteAsync(context);

        // Assert
  Assert.True(result.IsSuccessful);
      // Verify rules executed in correct order (priority)
  Assert.Equal("DEDUCTIBLE", result.ExecutedRules[0].RuleId);     // Priority 10
        Assert.Equal("COPAY", result.ExecutedRules[1].RuleId);         // Priority 20
Assert.Equal("OUT_OF_POCKET", result.ExecutedRules[2].RuleId);  // Priority 40
   }

  [Fact]
    public async Task ExecuteAsync_TracksDuration()
    {
        // Arrange
        var engine = new AdjudicationRuleEngine(_logger);
        engine.RegisterRules(new DeductibleRule(_deductibleLogger));

     var context = new AdjudicationContext
        {
    ItemSequence = 1,
       AllowedAmount = 100m,
          AnnualDeductible = 1000m,
  DeductibleMet = 800m
        };

        // Act
var result = await engine.ExecuteAsync(context);

 // Assert
        Assert.True(result.IsSuccessful);
    Assert.True(result.DurationMs >= 0);
        Assert.InRange(result.DurationMs, 0, 100); // Should complete quickly
    }

 [Fact]
    public async Task ExecuteAsync_HandlesMissingContext()
    {
        // Arrange
 var engine = new AdjudicationRuleEngine(_logger);
        engine.RegisterRules(new DeductibleRule(_deductibleLogger));

   // Act & Assert
   await Assert.ThrowsAsync<ArgumentNullException>(
 async () => await engine.ExecuteAsync(null!)
        );
    }
}
