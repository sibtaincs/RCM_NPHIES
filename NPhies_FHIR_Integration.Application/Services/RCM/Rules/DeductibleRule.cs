using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Deductible Rule - Priority 10
/// Applies the annual deductible FIRST (before copay or coinsurance)
/// Patient pays their remaining deductible before insurance pays anything
/// </summary>
public class DeductibleRule : IAdjudicationRule
{
    private readonly ILogger<DeductibleRule> _logger;

    public string RuleId => "DEDUCTIBLE";
    public string RuleName => "Annual Deductible Application";
    public int Priority => 10;

    public DeductibleRule(ILogger<DeductibleRule> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Apply deductible only if deductible not fully met
    /// </summary>
    public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
        // Apply deductible if there's remaining deductible
        return await Task.FromResult(context.RemainingDeductible > 0);
    }

    /// <summary>
    /// Calculate deductible to apply
    /// </summary>
    public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
  // Deductible is applied first, up to the remaining deductible
        decimal deductibleToApply = Math.Min(context.RemainingAmount, context.RemainingDeductible);

        // Remaining for insurance/copay/coinsurance after deductible
 decimal remainingAfterDeductible = context.RemainingAmount - deductibleToApply;

        _logger.LogInformation(
     "Deductible applied: ${Deductible:F2} (Met: ${Met:F2}/{Total:F2}). " +
       "Remaining: ${Remaining:F2}",
       deductibleToApply, context.DeductibleMet, context.AnnualDeductible,
            remainingAfterDeductible);

        return await Task.FromResult(
            RuleResult.Applied(
          ruleId: RuleId,
   patientResponsibility: deductibleToApply,
             remaining: remainingAfterDeductible,
     message: $"Applied ${deductibleToApply:F2} deductible. " +
    $"Remaining deductible: ${Math.Max(0, context.RemainingDeductible - deductibleToApply):F2}"));
    }
}
