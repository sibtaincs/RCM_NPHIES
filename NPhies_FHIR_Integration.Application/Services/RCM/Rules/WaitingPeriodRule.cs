using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Waiting Period Rule - Priority 3
/// Checks if service is within the waiting period (excluded for new members)
/// </summary>
public class WaitingPeriodRule : IAdjudicationRule
{
    private readonly ILogger<WaitingPeriodRule> _logger;

    public string RuleId => "WAITING_PERIOD";
    public string RuleName => "Waiting Period Check";
    public int Priority => 3;

    public WaitingPeriodRule(ILogger<WaitingPeriodRule> logger)
    {
      _logger = logger;
    }

    /// <summary>
    /// Apply waiting period check
  /// </summary>
    public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
        // Check if service is subject to waiting period
    // For now, return true for maternity and other typically waiting-period services
        return await Task.FromResult(!string.IsNullOrEmpty(context.ServiceType));
    }

    /// <summary>
    /// Check if within waiting period
    /// </summary>
  public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
  if (context.WithinWaitingPeriod)
        {
           _logger.LogWarning("Service {ServiceCode} is within waiting period",
    context.ServiceCode);

       return RuleResult.Applied(
                ruleId: RuleId,
    patientResponsibility: context.AllowedAmount,
         remaining: 0m,
  message: "Service is within waiting period. Claim denied.");
      }

    return RuleResult.Skip(RuleId);
    }
}
