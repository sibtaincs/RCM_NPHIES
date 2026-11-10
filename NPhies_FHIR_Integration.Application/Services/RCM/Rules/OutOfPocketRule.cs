using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Out-of-Pocket Maximum Rule - Priority 40
/// Enforces annual out-of-pocket maximum
/// Once patient pays up to OOP max, insurance pays 100% of remaining
/// </summary>
public class OutOfPocketRule : IAdjudicationRule
{
    private readonly ILogger<OutOfPocketRule> _logger;

    public string RuleId => "OUT_OF_POCKET";
    public string RuleName => "Out-of-Pocket Maximum Enforcement";
    public int Priority => 40;

   public OutOfPocketRule(ILogger<OutOfPocketRule> logger)
  {
        _logger = logger;
    }

 /// <summary>
    /// Apply OOP maximum if OOP max is set and not fully met
    /// </summary>
public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
        // Apply OOP check if:
 // 1. OOP maximum is set
     // 2. OOP not yet fully met
        return await Task.FromResult(
     context.OutOfPocketMax > 0 &&
      context.RemainingOOP > 0 &&
 context.RemainingAmount > 0);
    }

    /// <summary>
    /// Calculate OOP impact
    /// </summary>
    public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
        // Patient can pay up to their remaining OOP maximum
        decimal patientOOPShare = Math.Min(context.RemainingAmount, context.RemainingOOP);

        // Insurance pays the rest (if OOP max is reached)
        decimal insuranceShare = context.RemainingAmount - patientOOPShare;

        _logger.LogInformation(
    "Out-of-Pocket check: Patient can pay ${PatientOOP:F2} (OOP Met: ${Met:F2}/{Max:F2}). " +
  "Insurance pays: ${Insurance:F2}",
 patientOOPShare, context.OutOfPocketMet, context.OutOfPocketMax, insuranceShare);

        string message = context.RemainingOOP <= 0
 ? $"Out-of-pocket maximum reached. Insurance pays 100%."
      : $"Patient out-of-pocket: ${patientOOPShare:F2}. " +
    $"Insurance: ${insuranceShare:F2}. " +
   $"Remaining OOP available: ${Math.Max(0, context.RemainingOOP - patientOOPShare):F2}";

  return await Task.FromResult(
    RuleResult.Applied(
     ruleId: RuleId,
   patientResponsibility: patientOOPShare,
      remaining: insuranceShare,
  message: message));
   }
}
