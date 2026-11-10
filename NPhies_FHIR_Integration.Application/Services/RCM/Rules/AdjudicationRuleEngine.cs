using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Adjudication Rule Engine
/// Manages execution of adjudication rules in priority order
/// Rules are executed sequentially, each updating context for the next
/// </summary>
public class AdjudicationRuleEngine
{
    private readonly ILogger<AdjudicationRuleEngine> _logger;
    private readonly List<IAdjudicationRule> _rules = new();

    public AdjudicationRuleEngine(ILogger<AdjudicationRuleEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register rules to be executed
    /// Rules are automatically sorted by priority (lower number = earlier execution)
    /// </summary>
    /// <param name="rules">Rules to register</param>
    public void RegisterRules(params IAdjudicationRule[] rules)
    {
        if (rules == null || rules.Length == 0)
        {
            _logger.LogWarning("No rules provided to RegisterRules");
            return;
        }

        _rules.AddRange(rules);

        // Sort by priority (lower = higher priority = execute first)
        _rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        _logger.LogInformation("Registered {Count} rules in execution order: {RuleOrder}",
        _rules.Count,
      string.Join(" ? ", _rules.Select(r => $"{r.RuleId}({r.Priority})")));
    }

    /// <summary>
    /// Execute all applicable rules in priority order
    /// Each rule updates the context for the next rule
    /// </summary>
    /// <param name="context">Claim and coverage context for adjudication</param>
    /// <returns>Adjudication execution result</returns>
    public async Task<AdjudicationExecutionResult> ExecuteAsync(AdjudicationContext context)
    {
        if (context == null)
        {
            _logger.LogError("AdjudicationContext is null");
            throw new ArgumentNullException(nameof(context));
        }

        _logger.LogInformation("Starting adjudication for item sequence {ItemSequence}. " +
          "Submitted: ${Submitted:F2}, Allowed: ${Allowed:F2}",
      context.ItemSequence, context.SubmittedAmount, context.AllowedAmount);

        // Initialize execution context
        context.RemainingAmount = context.AllowedAmount;

        var result = new AdjudicationExecutionResult
        {
            ItemSequence = context.ItemSequence,
            SubmittedAmount = context.SubmittedAmount,
            AllowedAmount = context.AllowedAmount,
            ExecutedRules = new List<ExecutedRuleDetail>(),
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Execute each rule in priority order
            foreach (var rule in _rules)
            {
                try
                {
                    // Check if rule is applicable
                    var applicable = await rule.IsApplicableAsync(context);

                    if (!applicable)
                    {
                        _logger.LogDebug("Rule {RuleId} not applicable for item {ItemSequence}",
                         rule.RuleId, context.ItemSequence);

                        result.ExecutedRules.Add(new ExecutedRuleDetail
                        {
                            RuleId = rule.RuleId,
                            RuleName = rule.RuleName,
                            Priority = rule.Priority,
                            IsApplied = false,
                            Message = "Not applicable"
                        });

                        continue;
                    }

                    // Execute rule
                    _logger.LogInformation("Applying rule {RuleId} ({RuleName}) - Priority {Priority}",
            rule.RuleId, rule.RuleName, rule.Priority);

                    var ruleResult = await rule.EvaluateAsync(context);

                    // Record execution
                    result.ExecutedRules.Add(new ExecutedRuleDetail
                    {
                        RuleId = rule.RuleId,
                        RuleName = rule.RuleName,
                        Priority = rule.Priority,
                        IsApplied = ruleResult.IsApplied,
                        PatientResponsibilityApplied = ruleResult.PatientResponsibilityApplied,
                        RemainingAmount = ruleResult.RemainingAmount,
                        Message = ruleResult.Message,
                        Error = ruleResult.Error
                    });

                    if (ruleResult.IsApplied)
                    {
                        // Update context for next rule
                        context.RemainingAmount = ruleResult.RemainingAmount ?? context.RemainingAmount;

                        _logger.LogInformation("Rule {RuleId} applied: {Message}. Remaining: ${Remaining:F2}",
                              rule.RuleId, ruleResult.Message, context.RemainingAmount);
                    }
                    else if (!string.IsNullOrEmpty(ruleResult.Error))
                    {
                        _logger.LogWarning("Rule {RuleId} error: {Error}",
                    rule.RuleId, ruleResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in rule {RuleId} for item {ItemSequence}",
                        rule.RuleId, context.ItemSequence);

                    result.ExecutedRules.Add(new ExecutedRuleDetail
                    {
                        RuleId = rule.RuleId,
                        RuleName = rule.RuleName,
                        Priority = rule.Priority,
                        IsApplied = false,
                        Error = ex.Message
                    });

                    result.HasErrors = true;
                }
            }

            // Calculate final result
            result.InsuranceResponsibility = Math.Max(0, context.RemainingAmount);
            result.PatientResponsibility = Math.Max(0, context.AllowedAmount - result.InsuranceResponsibility);
            result.IsSuccessful = !result.HasErrors;
            result.EndTime = DateTime.UtcNow;
            result.DurationMs = (result.EndTime - result.StartTime).TotalMilliseconds;

            _logger.LogInformation(
   "Adjudication completed for item {ItemSequence}: " +
            "Insurance=${Insurance:F2}, Patient=${Patient:F2}, Duration={DurationMs}ms",
         context.ItemSequence,
   result.InsuranceResponsibility,
   result.PatientResponsibility,
      result.DurationMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error executing adjudication for item {ItemSequence}",
        context.ItemSequence);

            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            result.HasErrors = true;
            result.EndTime = DateTime.UtcNow;
            result.DurationMs = (result.EndTime - result.StartTime).TotalMilliseconds;

            return result;
        }
    }
}

/// <summary>
/// Result of adjudication execution for a single claim item
/// </summary>
public class AdjudicationExecutionResult
{
    /// <summary>
    /// Line item sequence number
    /// </summary>
    public int ItemSequence { get; set; }

    /// <summary>
    /// Amount provider submitted
    /// </summary>
    public decimal SubmittedAmount { get; set; }

    /// <summary>
    /// Amount allowed by insurance
    /// </summary>
    public decimal AllowedAmount { get; set; }

    /// <summary>
    /// Amount insurance will pay
    /// </summary>
    public decimal InsuranceResponsibility { get; set; }

    /// <summary>
    /// Amount patient is responsible for
    /// </summary>
    public decimal PatientResponsibility { get; set; }

    /// <summary>
    /// Was adjudication successful?
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Were there any errors during processing?
    /// </summary>
    public bool HasErrors { get; set; }

    /// <summary>
    /// Error message if adjudication failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// List of rules that were executed
    /// </summary>
    public List<ExecutedRuleDetail> ExecutedRules { get; set; } = new();

    /// <summary>
    /// When adjudication started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When adjudication ended
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// How long adjudication took in milliseconds
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Get summary of adjudication result
    /// </summary>
    public string GetSummary() =>
        $"Item {ItemSequence}: Submitted ${SubmittedAmount:F2} ? Allowed ${AllowedAmount:F2} " +
        $"? Insurance ${InsuranceResponsibility:F2}, Patient ${PatientResponsibility:F2}";
}

/// <summary>
/// Details of a rule that was executed
/// </summary>
public class ExecutedRuleDetail
{
    /// <summary>
    /// Rule identifier
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Rule name
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Execution priority
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Was the rule applied?
    /// </summary>
    public bool IsApplied { get; set; }

    /// <summary>
    /// Amount applied to patient responsibility
    /// </summary>
    public decimal? PatientResponsibilityApplied { get; set; }

    /// <summary>
    /// Remaining amount after rule
    /// </summary>
    public decimal? RemainingAmount { get; set; }

    /// <summary>
    /// Descriptive message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error message if rule failed
    /// </summary>
    public string? Error { get; set; }
}
