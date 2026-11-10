using Microsoft.Extensions.Logging;
using Xunit;
using NPhies_FHIR_Integration.Application.Services.RCM.Rules;

namespace NPhies_FHIR_Integration.Tests.RCM.Rules;

/// <summary>
/// Unit tests for CopayRule
/// Tests copay application logic
/// </summary>
public class CopayRuleTests
{
    private readonly ILogger<CopayRule> _logger;
    private readonly CopayRule _rule;

    public CopayRuleTests()
    {
        _logger = new NullLogger<CopayRule>();
        _rule = new CopayRule(_logger);
    }

  [Fact]
    public async Task RuleId_ReturnsCorrectIdentifier()
    {
        // Assert
        Assert.Equal("COPAY", _rule.RuleId);
    }

    [Fact]
    public async Task Priority_Returns20()
    {
        // Assert
    Assert.Equal(20, _rule.Priority);
    }

    [Fact]
    public async Task IsApplicableAsync_ReturnsTrueWhenCopayNotApplied()
    {
        // Arrange
        var context = new AdjudicationContext
      {
            CopayAmount = 25m,
      CopayApplied = false,
          RemainingAmount = 100m
        };

        // Act
        var result = await _rule.IsApplicableAsync(context);

        // Assert
    Assert.True(result);
    }

    [Fact]
    public async Task IsApplicableAsync_ReturnsFalseWhenCopayAlreadyApplied()
    {
        // Arrange
  var context = new AdjudicationContext
        {
            CopayAmount = 25m,
   CopayApplied = true,
            RemainingAmount = 100m
        };

        // Act
    var result = await _rule.IsApplicableAsync(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsApplicableAsync_ReturnsFalseWhenNoCopayAmount()
{
     // Arrange
        var context = new AdjudicationContext
        {
            CopayAmount = 0m,
     CopayApplied = false,
            RemainingAmount = 100m
        };

        // Act
  var result = await _rule.IsApplicableAsync(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_AppliesCopayCorrectly()
    {
  // Arrange
  var context = new AdjudicationContext
        {
   ItemSequence = 1,
    CopayAmount = 25m,
      CopayApplied = false,
     RemainingAmount = 100m
   };

   // Act
        var result = await _rule.EvaluateAsync(context);

   // Assert
      Assert.True(result.IsApplied);
        Assert.Equal(25m, result.PatientResponsibilityApplied);
      Assert.Equal(75m, result.RemainingAmount);
        Assert.True(context.CopayApplied); // Should mark copay as applied
    }

[Fact]
    public async Task EvaluateAsync_LimitsCopayToRemainingAmount()
    {
        // Arrange
var context = new AdjudicationContext
      {
            ItemSequence = 1,
       CopayAmount = 50m,
         CopayApplied = false,
            RemainingAmount = 30m // Less than copay
        };

        // Act
        var result = await _rule.EvaluateAsync(context);

        // Assert
        Assert.True(result.IsApplied);
 Assert.Equal(30m, result.PatientResponsibilityApplied); // Can't exceed remaining
        Assert.Equal(0m, result.RemainingAmount);
    }

    [Theory]
    [InlineData(25, 100, 25, 75)]      // Standard $25 copay
    [InlineData(50, 100, 50, 50)]      // Higher copay
    [InlineData(25, 50, 25, 25)]       // Remaining > copay
    [InlineData(25, 20, 20, 0)]        // Remaining < copay
  [InlineData(0, 100, 0, 100)]       // No copay
    public async Task EvaluateAsync_VariousCopayScenarios(
        decimal copay,
        decimal remaining,
        decimal expectedPatient,
  decimal expectedInsurance)
    {
        // Arrange
        var context = new AdjudicationContext
        {
            CopayAmount = copay,
       CopayApplied = false,
   RemainingAmount = remaining
    };

        // Act
  var result = await _rule.EvaluateAsync(context);

        // Assert
        if (copay > 0)
        {
   Assert.True(result.IsApplied);
     Assert.Equal(expectedPatient, result.PatientResponsibilityApplied);
   Assert.Equal(expectedInsurance, result.RemainingAmount);
 }
    }
}
