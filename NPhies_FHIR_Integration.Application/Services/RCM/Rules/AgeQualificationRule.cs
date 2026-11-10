using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Age Qualification Rule - Priority 4
/// Checks if patient age qualifies for the requested service
/// Some services have age restrictions (e.g., pediatric, geriatric)
/// </summary>
public class AgeQualificationRule : IAdjudicationRule
{
    private readonly ILogger<AgeQualificationRule> _logger;

    public string RuleId => "AGE_QUALIFICATION";
    public string RuleName => "Age Qualification Check";
    public int Priority => 4;

    public AgeQualificationRule(ILogger<AgeQualificationRule> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Apply age qualification check
    /// </summary>
    public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
        return await Task.FromResult(!string.IsNullOrEmpty(context.ServiceType));
    }

    /// <summary>
    /// Check if patient age qualifies for service
    /// </summary>
    public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
        if (!context.IsAgeQualified)
        {
            _logger.LogWarning("Patient age does not qualify for service {ServiceCode}",
         context.ServiceCode);

            return RuleResult.Applied(
               ruleId: RuleId,
                 patientResponsibility: context.AllowedAmount,
               remaining: 0m,
                 message: "Patient age does not qualify for this service. Claim denied.");
        }

        return RuleResult.Skip(RuleId);
    }
}
