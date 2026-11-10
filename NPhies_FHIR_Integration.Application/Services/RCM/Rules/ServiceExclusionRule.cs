using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Service Exclusion Rule - Priority 1 (execute first)
/// Checks if the service is explicitly excluded from coverage
/// If excluded, the entire claim is denied with no appeal
/// </summary>
public class ServiceExclusionRule : IAdjudicationRule
{
    private readonly ILogger<ServiceExclusionRule> _logger;

    public string RuleId => "SERVICE_EXCLUSION";
    public string RuleName => "Service Exclusion Check";
    public int Priority => 1;

    public ServiceExclusionRule(ILogger<ServiceExclusionRule> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Service exclusion always applies (check it first)
    /// </summary>
    public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
     return await Task.FromResult(true);
    }

    /// <summary>
    /// Check if service is excluded
/// </summary>
public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
   return await Task.FromResult(
            context.IsServiceExcluded
          ? RuleResult.Applied(
      ruleId: RuleId,
     patientResponsibility: context.AllowedAmount,
   remaining: 0m,
            message: $"Service code {context.ServiceCode} is excluded from coverage. Claim denied.")
 : RuleResult.Skip(RuleId)
      );
    }
}
