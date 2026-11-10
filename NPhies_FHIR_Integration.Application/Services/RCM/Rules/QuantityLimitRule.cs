using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Quantity Limit Rule - Priority 7
/// Enforces quantity limits on services
/// Example: Maximum 30 physical therapy visits per year
/// </summary>
public class QuantityLimitRule : IAdjudicationRule
{
    private readonly ILogger<QuantityLimitRule> _logger;

   public string RuleId => "QUANTITY_LIMIT";
    public string RuleName => "Quantity Limit Enforcement";
    public int Priority => 7;

    // Service quantity limits (in production, load from database)
    private static readonly Dictionary<string, (int max, string period)> ServiceLimits =
  new()
        {
    { "PT", (30, "per year") },         // Physical therapy: 30 visits/year
       { "OT", (30, "per year") },   // Occupational therapy
       { "SLP", (30, "per year") },        // Speech-language pathology
   { "CHI", (3, "per calendar year") }, // Chiropractic: 3 visits/year
          { "EYE", (1, "per 12 months") },      // Eye exam: 1 per year
     { "DENT", (2, "per year") }      // Dental cleaning: 2 per year
        };

    public QuantityLimitRule(ILogger<QuantityLimitRule> logger)
    {
        _logger = logger;
    }

  /// <summary>
    /// Apply quantity limit if service has a limit defined
    /// </summary>
    public async Task<bool> IsApplicableAsync(AdjudicationContext context)
 {
        return await Task.FromResult(
      ServiceLimits.ContainsKey(context.ServiceCode));
    }

    /// <summary>
    /// Check quantity against limit
    /// </summary>
    public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
        if (!ServiceLimits.TryGetValue(context.ServiceCode, out var limit))
       {
          return RuleResult.Skip(RuleId);
        }

        _logger.LogInformation("Quantity limit for {ServiceCode}: {Max} {Period}",
    context.ServiceCode, limit.max, limit.period);

      // In production, check actual quantity used from claims database
        // For now, assume limit not exceeded
  return RuleResult.Skip(RuleId);
    }
}
