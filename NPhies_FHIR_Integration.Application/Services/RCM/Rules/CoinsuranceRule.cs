using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Coinsurance Rule - Priority 30
/// Applies coinsurance percentage (patient's share after insurance percentage)
/// Coinsurance is applied AFTER deductible and copay
/// Example: 20% coinsurance = patient pays 20%, insurance pays 80%
/// </summary>
public class CoinsuranceRule : IAdjudicationRule
{
    private readonly ILogger<CoinsuranceRule> _logger;

   public string RuleId => "COINSURANCE";
    public string RuleName => "Coinsurance Application";
    public int Priority => 30;

    public CoinsuranceRule(ILogger<CoinsuranceRule> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Apply coinsurance if remaining amount > 0 and coinsurance not yet applied
    /// </summary>
  public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
        // Apply coinsurance if:
        // 1. Coinsurance not already applied
 // 2. There's remaining amount
 // 3. Coinsurance percentage > 0
  return await Task.FromResult(
       !context.CoinsuranceApplied &&
  context.RemainingAmount > 0 &&
      context.CoinsurancePercentage > 0);
    }

    /// <summary>
    /// Calculate coinsurance to apply
    /// </summary>
    public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
   // Patient's coinsurance share
        decimal patientCoinsurance = context.RemainingAmount * (context.CoinsurancePercentage / 100m);

   // Insurance's share
        decimal insuranceCoinsurance = context.RemainingAmount - patientCoinsurance;

        _logger.LogInformation(
  "Coinsurance applied: {Percentage}%. Patient: ${Patient:F2}, Insurance: ${Insurance:F2}",
         context.CoinsurancePercentage, patientCoinsurance, insuranceCoinsurance);

  // Mark coinsurance as applied in context
        context.CoinsuranceApplied = true;

      return await Task.FromResult(
    RuleResult.Applied(
  ruleId: RuleId,
                patientResponsibility: patientCoinsurance,
    remaining: insuranceCoinsurance,
message: $"Applied {context.CoinsurancePercentage}% coinsurance. " +
 $"Patient: ${patientCoinsurance:F2}, Insurance: ${insuranceCoinsurance:F2}"));
    }
}
