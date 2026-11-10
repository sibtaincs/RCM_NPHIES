using Microsoft.Extensions.Logging;
using Xunit;
using NPhies_FHIR_Integration.Application.Services.RCM.Rules;

namespace NPhies_FHIR_Integration.Tests.RCM.Rules;

/// <summary>
/// Unit tests for DeductibleRule
/// Tests deductible application logic
/// </summary>
public class DeductibleRuleTests
{
    private readonly ILogger<DeductibleRule> _logger;
    private readonly DeductibleRule _rule;

    public DeductibleRuleTests()
    {
        // Create a null logger for testing (can be replaced with mock)
        _logger = new NullLogger<DeductibleRule>();
        _rule = new DeductibleRule(_logger);
    }

    [Fact]
    public async Task RuleId_ReturnsCorrectIdentifier()
    {
        // Assert
        Assert.Equal("DEDUCTIBLE", _rule.RuleId);
    }

    [Fact]
    public async Task Priority_Returns10()
    {
        // Assert
        Assert.Equal(10, _rule.Priority);
    }

    [Fact]
    public async Task IsApplicableAsync_ReturnsTrueWhenRemainingDeductible()
    {
        // Arrange
        var context = new AdjudicationContext
        {
            AnnualDeductible = 1000m,
            DeductibleMet = 500m,
            RemainingAmount = 100m
        };

        // Act
        var result = await _rule.IsApplicableAsync(context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsApplicableAsync_ReturnsFalseWhenDeductibleMet()
    {
        // Arrange
        var context = new AdjudicationContext
        {
            AnnualDeductible = 1000m,
            DeductibleMet = 1000m,
            RemainingAmount = 100m
        };

        // Act
        var result = await _rule.IsApplicableAsync(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_AppliesCorrectDeductibleAmount()
    {
        // Arrange
        var context = new AdjudicationContext
        {
            ItemSequence = 1,
            AnnualDeductible = 1000m,
            DeductibleMet = 800m,
            RemainingAmount = 150m
        };

        // Act
        var result = await _rule.EvaluateAsync(context);

        // Assert
        Assert.True(result.IsApplied);
        Assert.Equal(150m, result.PatientResponsibilityApplied); // Remaining deductible is $200, but only $150 available
        Assert.Equal(0m, result.RemainingAmount);
    }

    [Fact]
    public async Task EvaluateAsync_LimitsDeductibleToRemainingDeductible()
    {
        // Arrange
        var context = new AdjudicationContext
        {
            ItemSequence = 1,
            AnnualDeductible = 1000m,
            DeductibleMet = 900m,
            RemainingAmount = 200m
        };

        // Act
        var result = await _rule.EvaluateAsync(context);

        // Assert
        Assert.True(result.IsApplied);
        Assert.Equal(100m, result.PatientResponsibilityApplied); // Only $100 deductible remaining
        Assert.Equal(100m, result.RemainingAmount); // $200 - $100 = $100
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsSkipWhenDeductibleMet()
    {
        // Arrange
        var context = new AdjudicationContext
        {
            ItemSequence = 1,
            AnnualDeductible = 1000m,
            DeductibleMet = 1000m,
            RemainingAmount = 200m
        };

        // Act
        var result = await _rule.EvaluateAsync(context);

        // Assert
        Assert.False(result.IsApplied);
    }

    [Theory]
    [InlineData(1000, 0, 500, 500, 0)]    // No deductible met, apply entire amount
    [InlineData(1000, 500, 300, 300, 0)]   // $500 deductible met, apply remaining $300
    [InlineData(1000, 800, 150, 150, 0)]   // $800 met, only $150 available
    [InlineData(1000, 900, 200, 100, 100)] // Only $100 deductible remaining
    public async Task EvaluateAsync_VariousDeductibleScenarios(
        decimal annual,
        decimal met,
        decimal remaining,
  decimal expectedPatient,
        decimal expectedInsurance)
    {
        // Arrange
        var context = new AdjudicationContext
        {
            AnnualDeductible = annual,
            DeductibleMet = met,
            RemainingAmount = remaining
        };

        // Act
        var result = await _rule.EvaluateAsync(context);

        // Assert
        Assert.True(result.IsApplied);
        Assert.Equal(expectedPatient, result.PatientResponsibilityApplied);
        Assert.Equal(expectedInsurance, result.RemainingAmount);
    }
}

/// <summary>
/// Null logger implementation for testing
/// </summary>
public class NullLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
