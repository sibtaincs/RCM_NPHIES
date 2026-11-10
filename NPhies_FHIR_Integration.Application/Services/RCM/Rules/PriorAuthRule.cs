using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Prior Authorization Rule - Priority 2
/// Checks if prior authorization is required and whether valid auth is present
/// </summary>
public class PriorAuthRule : IAdjudicationRule
{
    private readonly ILogger<PriorAuthRule> _logger;

  public string RuleId => "PRIOR_AUTH";
    public string RuleName => "Prior Authorization Check";
    public int Priority => 2;

    public PriorAuthRule(ILogger<PriorAuthRule> logger)
    {
        _logger = logger;
  }

    /// <summary>
    /// Only apply if prior auth is required
    /// </summary>
    public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
        return await Task.FromResult(context.RequiresPriorAuth);
    }

    /// <summary>
    /// Check if valid prior auth is present
    /// </summary>
    public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
      if (context.HasValidPriorAuth)
        {
            _logger.LogInformation("Valid prior authorization found for service {ServiceCode}",
       context.ServiceCode);
    return RuleResult.Skip(RuleId);
   }

  _logger.LogWarning("Prior authorization required but not found for service {ServiceCode}",
            context.ServiceCode);

        return RuleResult.Applied(
            ruleId: RuleId,
     patientResponsibility: context.AllowedAmount,
     remaining: 0m,
   message: "Prior authorization required but not provided. Claim denied.");
  }
}
