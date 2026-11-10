namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Base interface for all adjudication rules
/// Each rule applies specific business logic to claim adjudication
/// Rules are executed in priority order by the AdjudicationRuleEngine
/// </summary>
public interface IAdjudicationRule
{
    /// <summary>
    /// Unique rule identifier (e.g., "DEDUCTIBLE", "COPAY", "COINSURANCE")
    /// Used for logging and tracking
 /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Human-readable rule name
    /// Used for reporting and UI display
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Execution priority (lower = higher priority)
    /// Example: 
    ///   1  = ServiceExclusionRule (check first)
    ///   2  = PriorAuthRule
    ///   3  = WaitingPeriodRule
    ///   10 = DeductibleRule
    ///   20 = CopayRule
    ///   30 = CoinsuranceRule
    ///   40 = OutOfPocketRule
    ///   50 = BenefitLimitRule
    /// Rules with lower priority execute first
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Check if this rule applies to the given context
  /// Allows rules to skip themselves based on conditions
    /// </summary>
    /// <param name="context">The adjudication context with claim and coverage data</param>
    /// <returns>True if rule should be executed, false to skip</returns>
    Task<bool> IsApplicableAsync(AdjudicationContext context);

    /// <summary>
    /// Evaluate and apply the rule
    /// Updates the context for the next rule in the chain
    /// </summary>
    /// <param name="context">The adjudication context (will be modified)</param>
    /// <returns>Result indicating if rule was applied and how much</returns>
    Task<RuleResult> EvaluateAsync(AdjudicationContext context);
}

/// <summary>
/// Context passed to all adjudication rules
/// Contains all claim and coverage information needed for adjudication
/// This context is updated by each rule as it executes
/// </summary>
public class AdjudicationContext
{
    // ========== CLAIM ITEM INFORMATION ==========
    /// <summary>
    /// Sequence number of this line item in the claim
    /// </summary>
    public int ItemSequence { get; set; }

    /// <summary>
    /// Service/procedure code (e.g., CPT code)
    /// </summary>
    public string ServiceCode { get; set; } = string.Empty;

    /// <summary>
    /// Amount submitted by provider
    /// </summary>
    public decimal SubmittedAmount { get; set; }

    /// <summary>
    /// Amount allowed/covered by insurance
    /// </summary>
    public decimal AllowedAmount { get; set; }

    /// <summary>
    /// Remaining amount to be divided between insurance and patient
    /// Updated after each rule applies
    /// </summary>
    public decimal RemainingAmount { get; set; }

    // ========== COVERAGE INFORMATION ==========
    /// <summary>
    /// Network status: "in-network" or "out-of-network"
    /// Affects coverage percentages
    /// </summary>
    public string NetworkStatus { get; set; } = "in-network";

 /// <summary>
    /// Insurance coverage percentage (e.g., 80 for 80% coverage)
    /// </summary>
    public decimal CoveragePercentage { get; set; } = 80m;

  /// <summary>
    /// Type of coverage (e.g., "medical", "dental", "vision")
    /// </summary>
    public string CoverageType { get; set; } = string.Empty;

    // ========== DEDUCTIBLE INFORMATION ==========
    /// <summary>
    /// Annual deductible amount
    /// </summary>
    public decimal AnnualDeductible { get; set; }

/// <summary>
    /// Amount of deductible already met this year
    /// </summary>
    public decimal DeductibleMet { get; set; }

    /// <summary>
  /// Calculated remaining deductible
    /// </summary>
    public decimal RemainingDeductible => Math.Max(0, AnnualDeductible - DeductibleMet);

    // ========== COPAY INFORMATION ==========
    /// <summary>
    /// Fixed copay amount per visit (e.g., $25)
    /// </summary>
    public decimal CopayAmount { get; set; }

    /// <summary>
  /// Has copay already been applied for this claim?
    /// </summary>
    public bool CopayApplied { get; set; }

    // ========== COINSURANCE INFORMATION ==========
    /// <summary>
    /// Patient's coinsurance percentage (e.g., 20 for 20%)
    /// This is patient's portion after insurance pays their percentage
    /// </summary>
    public decimal CoinsurancePercentage { get; set; } = 20m;

    /// <summary>
    /// Has coinsurance been calculated?
    /// </summary>
    public bool CoinsuranceApplied { get; set; }

    // ========== OUT-OF-POCKET INFORMATION ==========
    /// <summary>
    /// Annual out-of-pocket maximum
    /// Once patient pays this much, insurance pays 100%
    /// </summary>
    public decimal OutOfPocketMax { get; set; }

 /// <summary>
    /// Amount patient has paid toward OOP maximum this year
    /// </summary>
    public decimal OutOfPocketMet { get; set; }

    /// <summary>
    /// Remaining OOP before insurance pays 100%
    /// </summary>
    public decimal RemainingOOP => Math.Max(0, OutOfPocketMax - OutOfPocketMet);

    // ========== BENEFIT LIMITS ==========
    /// <summary>
    /// Annual benefit limit for this service/category
    /// </summary>
    public decimal AnnualBenefitLimit { get; set; }

    /// <summary>
    /// Amount of benefit already used this year
    /// </summary>
    public decimal BenefitUsed { get; set; }

    /// <summary>
    /// Remaining benefit available
    /// </summary>
  public decimal RemainingBenefit => Math.Max(0, AnnualBenefitLimit - BenefitUsed);

    // ========== SERVICE INFORMATION ==========
    /// <summary>
    /// Type of service (e.g., "inpatient", "outpatient", "emergency")
    /// Affects some coverage rules
    /// </summary>
    public string ServiceType { get; set; } = string.Empty;

    /// <summary>
    /// Primary diagnosis code
    /// Used for validation and coverage determination
    /// </summary>
    public string DiagnosisCode { get; set; } = string.Empty;

    /// <summary>
    /// Is this service excluded from coverage?
    /// </summary>
    public bool IsServiceExcluded { get; set; }

    /// <summary>
    /// Is prior authorization required?
    /// </summary>
    public bool RequiresPriorAuth { get; set; }

    /// <summary>
    /// Is valid prior auth present?
    /// </summary>
    public bool HasValidPriorAuth { get; set; }

    /// <summary>
    /// Is service within waiting period?
    /// </summary>
    public bool WithinWaitingPeriod { get; set; }

    /// <summary>
    /// Is patient age-qualified for this service?
    /// </summary>
    public bool IsAgeQualified { get; set; } = true;
}

/// <summary>
/// Result of a rule evaluation
/// Indicates whether the rule was applied and what amount was calculated
/// </summary>
public class RuleResult
{
    /// <summary>
    /// Was the rule applied successfully?
    /// False means the rule was not applicable or had an error
    /// </summary>
    public bool IsApplied { get; set; }

    /// <summary>
    /// Rule ID that produced this result
 /// Used for tracking and logging
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Amount of patient responsibility applied by this rule
    /// </summary>
    public decimal? PatientResponsibilityApplied { get; set; }

    /// <summary>
    /// Amount remaining for insurance to cover
    /// </summary>
    public decimal? RemainingAmount { get; set; }

    /// <summary>
    /// Descriptive message explaining the result
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error message if rule failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Create a skip result (rule not applicable)
    /// </summary>
    public static RuleResult Skip(string ruleId) =>
        new() { IsApplied = false, RuleId = ruleId };

    /// <summary>
    /// Create an applied result
    /// </summary>
  public static RuleResult Applied(
        string ruleId,
        decimal patientResponsibility,
        decimal? remaining = null,
     string? message = null) =>
      new()
        {
    IsApplied = true,
       RuleId = ruleId,
       PatientResponsibilityApplied = patientResponsibility,
          RemainingAmount = remaining,
      Message = message
        };

    /// <summary>
    /// Create a failure result
    /// </summary>
    public static RuleResult Failed(string ruleId, string error) =>
        new() { IsApplied = false, RuleId = ruleId, Error = error };
}
