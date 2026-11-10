namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Benefit entity - represents individual benefits (copay, deductible, annual limit, etc.)
/// FHIR Resource: CoverageEligibilityResponse.insurance.benefitBalance.benefit
/// </summary>
public class Benefit : BaseEntity
{
/// <summary>
    /// Reference to the parent benefit balance
    /// </summary>
    public string BenefitBalanceId { get; set; } = string.Empty;

    /// <summary>
    /// The parent benefit balance
    /// </summary>
  public BenefitBalance? BenefitBalance { get; set; }

 /// <summary>
    /// Sequence number of this benefit
    /// </summary>
    public int SequenceNumber { get; set; }

    // Benefit Classification
    /// <summary>
    /// Benefit type code
 /// Examples: "benefit" (annual limit), "copay", "deductible", "coinsurance", "out-of-pocket"
    /// System: http://nphies.sa/terminology/CodeSystem/benefit-type
    /// </summary>
    public string BenefitType { get; set; } = string.Empty;

    /// <summary>
    /// Benefit type system
    /// </summary>
    public string BenefitTypeSystem { get; set; } = "http://nphies.sa/terminology/CodeSystem/benefit-type";

    /// <summary>
    /// Human-readable benefit type description
    /// </summary>
    public string BenefitTypeDescription { get; set; } = string.Empty;

    // Allowed Amount
  /// <summary>
    /// Allowed/Maximum amount for this benefit
    /// </summary>
    public decimal AllowedAmount { get; set; }

    /// <summary>
    /// Currency code (usually "SAR" for Saudi Riyal)
    /// </summary>
    public string AllowedCurrency { get; set; } = "SAR";

 /// <summary>
    /// Unit for allowed amount (e.g., "per visit", "annual", "per procedure")
    /// </summary>
    public string AllowedUnit { get; set; } = string.Empty;

    // Used Amount
    /// <summary>
    /// Amount already used/claimed under this benefit
    /// </summary>
    public decimal UsedAmount { get; set; }

 // Percentage-based Benefits
    /// <summary>
    /// Percentage for coinsurance/copay percentage benefits
    /// </summary>
    public decimal? PercentageAmount { get; set; }

    // Additional Information
    /// <summary>
    /// Description or notes about this benefit
 /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Start date for this benefit (if applicable)
    /// </summary>
    public DateTime? BenefitStartDate { get; set; }

/// <summary>
    /// End date for this benefit (if applicable)
    /// </summary>
    public DateTime? BenefitEndDate { get; set; }

    // Methods
    /// <summary>
    /// Get benefit type name (human-readable)
    /// </summary>
    public string GetBenefitTypeName() => BenefitType switch
    {
   "benefit" => "Annual Benefit Limit",
  "copay" => "Co-payment",
        "deductible" => "Deductible",
     "coinsurance" => "Coinsurance",
        "out-of-pocket" => "Out-of-Pocket Maximum",
        _ => BenefitTypeDescription ?? BenefitType
    };

    /// <summary>
    /// Calculate remaining benefit amount
    /// </summary>
    public decimal GetRemainingAmount()
    {
        if (AllowedAmount == 0) return 0;
        return Math.Max(0, AllowedAmount - UsedAmount);
    }

    /// <summary>
    /// Calculate percentage of benefit used
    /// </summary>
    public decimal GetPercentageUsed()
    {
        if (AllowedAmount == 0) return 0;
  return (UsedAmount / AllowedAmount) * 100;
    }

    /// <summary>
    /// Check if benefit has been exhausted
    /// </summary>
    public bool IsExhausted()
    {
        return AllowedAmount > 0 && UsedAmount >= AllowedAmount;
    }

/// <summary>
  /// Check if benefit is percentage-based (coinsurance)
    /// </summary>
    public bool IsPercentageBased => PercentageAmount.HasValue;

    /// <summary>
    /// Get formatted display string
    /// </summary>
    public string GetDisplayText()
    {
   var benefitName = GetBenefitTypeName();
     
        if (IsPercentageBased && PercentageAmount.HasValue)
        {
      return $"{benefitName}: {PercentageAmount}%";
        }

     return $"{benefitName}: {AllowedAmount} {AllowedCurrency}";
    }

    /// <summary>
  /// Get formatted status showing used/allowed
    /// </summary>
    public string GetStatusText()
    {
        if (AllowedAmount == 0)
        {
    return "Unlimited";
        }

        var remaining = GetRemainingAmount();
        var percentage = GetPercentageUsed();

        return $"Used: {UsedAmount} / {AllowedAmount} {AllowedCurrency} ({percentage:F1}%) - Remaining: {remaining} {AllowedCurrency}";
    }
}
