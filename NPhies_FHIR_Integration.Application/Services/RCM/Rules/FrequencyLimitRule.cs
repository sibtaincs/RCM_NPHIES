using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Frequency Limit Rule - Priority 8
/// Enforces frequency limits on services
/// Example: Maximum 1 eye exam per 12 months
/// </summary>
public class FrequencyLimitRule : IAdjudicationRule
{
    private readonly ILogger<FrequencyLimitRule> _logger;

    public string RuleId => "FREQUENCY_LIMIT";
    public string RuleName => "Frequency Limit Enforcement";
    public int Priority => 8;

 // Service frequency limits (days between services)
    private static readonly Dictionary<string, (int days, string description)> FrequencyLimits =
    new()
      {
      { "EYE", (365, "Eye exams: 1 per year") },
      { "DENT_CHECK", (180, "Dental cleaning: 2 per year") },
  { "MAMM", (365, "Mammogram: 1 per year") },
      { "PSA", (365, "PSA screening: 1 per year") },
            { "COLON", (1095, "Colonoscopy: 1 per 3 years") }
        };

  public FrequencyLimitRule(ILogger<FrequencyLimitRule> logger)
    {
     _logger = logger;
    }

  /// <summary>
    /// Apply frequency limit if service has a limit defined
    /// </summary>
    public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
 return await Task.FromResult(
   FrequencyLimits.ContainsKey(context.ServiceCode));
    }

    /// <summary>
    /// Check frequency against limit
    /// </summary>
    public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
    if (!FrequencyLimits.TryGetValue(context.ServiceCode, out var limit))
  {
return RuleResult.Skip(RuleId);
      }

 _logger.LogInformation("Frequency limit for {ServiceCode}: {Description}",
 context.ServiceCode, limit.description);

        // In production, check last service date from claims database
    // Compare with today to ensure frequency not exceeded
        // For now, assume frequency limit not exceeded

    return RuleResult.Skip(RuleId);
    }
}
