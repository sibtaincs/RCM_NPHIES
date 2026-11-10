using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Network Status Rule - Priority 5
/// Adjusts coverage percentages based on in-network vs out-of-network provider
/// Out-of-network typically has lower coverage percentage (higher patient responsibility)
/// </summary>
public class NetworkStatusRule : IAdjudicationRule
{
  private readonly ILogger<NetworkStatusRule> _logger;

    public string RuleId => "NETWORK_STATUS";
    public string RuleName => "Network Status Adjustment";
    public int Priority => 5;

    public NetworkStatusRule(ILogger<NetworkStatusRule> logger)
    {
        _logger = logger;
   }

    /// <summary>
    /// Network status check always applies
    /// </summary>
    public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
        return await Task.FromResult(!string.IsNullOrEmpty(context.NetworkStatus));
    }

    /// <summary>
    /// Adjust coverage based on network status
    /// </summary>
    public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
        // Determine network adjustment
        decimal adjustedCoveragePercentage = context.NetworkStatus.ToLower() == "in-network"
  ? context.CoveragePercentage
   : Math.Max(50m, context.CoveragePercentage - 10m); // Out-of-network: 10% reduction

 decimal insuranceAmount = context.RemainingAmount * (adjustedCoveragePercentage / 100m);
      decimal patientAmount = context.RemainingAmount - insuranceAmount;

   string message = context.NetworkStatus.ToLower() == "out-of-network"
         ? $"Out-of-network adjustment applied. Coverage reduced from {context.CoveragePercentage}% " +
    $"to {adjustedCoveragePercentage}%. Insurance: ${insuranceAmount:F2}, Patient: ${patientAmount:F2}"
  : $"In-network provider confirmed. Coverage: {adjustedCoveragePercentage}%. " +
  $"Insurance: ${insuranceAmount:F2}, Patient: ${patientAmount:F2}";

       _logger.LogInformation("Network status rule applied: {Message}", message);

        return RuleResult.Applied(
ruleId: RuleId,
    patientResponsibility: patientAmount,
   remaining: insuranceAmount,
  message: message);
}
}
