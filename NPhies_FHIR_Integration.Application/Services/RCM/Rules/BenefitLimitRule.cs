using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Benefit Limit Rule - Priority 50
/// Enforces annual benefit limits for specific services
/// Example: $5000 annual limit for physical therapy
/// </summary>
public class BenefitLimitRule : IAdjudicationRule
{
    private readonly ILogger<BenefitLimitRule> _logger;

    public string RuleId => "BENEFIT_LIMIT";
    public string RuleName => "Annual Benefit Limit Enforcement";
    public int Priority => 50;

    public BenefitLimitRule(ILogger<BenefitLimitRule> logger)
    {
      _logger = logger;
    }

/// <summary>
    /// Apply benefit limit if limit is set and not exceeded
    /// </summary>
    public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
   // Apply benefit limit if:
     // 1. Annual benefit limit > 0
   // 2. Benefit not yet fully used
       return await Task.FromResult(
       context.AnnualBenefitLimit > 0 &&
       context.RemainingBenefit > 0 &&
     context.RemainingAmount > 0);
    }

    /// <summary>
    /// Calculate benefit impact
  /// </summary>
    public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
        // Insurance can cover up to remaining benefit limit
   decimal insuranceCanCover = Math.Min(context.RemainingAmount, context.RemainingBenefit);

        // Patient pays remainder (if benefit limit will be exceeded)
   decimal patientCoverageGap = context.RemainingAmount - insuranceCanCover;

     _logger.LogInformation(
         "Benefit limit check: Insurance covers ${Insurance:F2}, " +
    "Benefit used: ${Used:F2}/{Total:F2}. " +
       "Patient coverage gap: ${Gap:F2}",
    insuranceCanCover, context.BenefitUsed, context.AnnualBenefitLimit,
          patientCoverageGap);

    string message = context.RemainingBenefit <= insuranceCanCover
      ? $"Insurance covers ${insuranceCanCover:F2}. Remaining benefit: ${Math.Max(0, context.RemainingBenefit - insuranceCanCover):F2}"
   : $"Benefit limit will be exceeded. Insurance: ${insuranceCanCover:F2}, " +
 $"Patient responsible for gap: ${patientCoverageGap:F2}";

     return await Task.FromResult(
      RuleResult.Applied(
        ruleId: RuleId,
       patientResponsibility: patientCoverageGap,
     remaining: insuranceCanCover,
       message: message));
    }
}
