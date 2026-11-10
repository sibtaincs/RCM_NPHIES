using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Copay Rule - Priority 20
/// Applies fixed copay amount per visit (e.g., $25 copay)
/// Copay is applied AFTER deductible but BEFORE coinsurance
/// </summary>
public class CopayRule : IAdjudicationRule
{
    private readonly ILogger<CopayRule> _logger;

    public string RuleId => "COPAY";
    public string RuleName => "Copay Application";
    public int Priority => 20;

    public CopayRule(ILogger<CopayRule> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Apply copay if copay amount is set and not yet applied
    /// </summary>
    public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
     // Apply copay if:
        // 1. Copay amount > 0
     // 2. Copay not already applied
 // 3. There's remaining amount to apply copay to
      return await Task.FromResult(
 context.CopayAmount > 0 &&
     !context.CopayApplied &&
    context.RemainingAmount > 0);
    }

    /// <summary>
    /// Calculate copay to apply
  /// </summary>
    public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
      // Copay cannot exceed remaining amount
      decimal copayToApply = Math.Min(context.CopayAmount, context.RemainingAmount);

   // Remaining for coinsurance after copay
        decimal remainingAfterCopay = context.RemainingAmount - copayToApply;

_logger.LogInformation(
     "Copay applied: ${Copay:F2}. Remaining: ${Remaining:F2}",
      copayToApply, remainingAfterCopay);

   // Mark copay as applied in context
      context.CopayApplied = true;

     return await Task.FromResult(
            RuleResult.Applied(
    ruleId: RuleId,
                patientResponsibility: copayToApply,
        remaining: remainingAfterCopay,
       message: $"Applied ${copayToApply:F2} copay"));
    }
}
