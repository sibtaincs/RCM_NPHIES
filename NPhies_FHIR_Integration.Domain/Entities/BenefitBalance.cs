namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// BenefitBalance entity - represents benefit categories in eligibility response
/// FHIR Resource: CoverageEligibilityResponse.insurance.benefitBalance
/// </summary>
public class BenefitBalance : BaseEntity
{
    /// <summary>
    /// Reference to the parent eligibility response
    /// </summary>
    public string EligibilityResponseId { get; set; } = string.Empty;

    /// <summary>
    /// The parent eligibility response
    /// </summary>
    public CoverageEligibilityResponse? EligibilityResponse { get; set; }

    /// <summary>
    /// Sequence number of this benefit balance
/// </summary>
    public int SequenceNumber { get; set; }

    // Benefit Category
 /// <summary>
 /// Benefit category code
    /// Examples: "medical", "dental", "pharmacy", "vision", "institutional", "professional"
    /// System: http://terminology.hl7.org/CodeSystem/ex-benefitcategory
    /// </summary>
  public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Category system
    /// </summary>
    public string CategorySystem { get; set; } = "http://terminology.hl7.org/CodeSystem/ex-benefitcategory";

    /// <summary>
    /// Human-readable category description
    /// </summary>
    public string CategoryDescription { get; set; } = string.Empty;

    // Benefit Information
    /// <summary>
    /// Collection of individual benefits in this category
    /// </summary>
    public ICollection<Benefit> Benefits { get; set; } = new List<Benefit>();

    // Methods
    /// <summary>
    /// Get category name (human-readable)
    /// </summary>
  public string GetCategoryName() => Category switch
    {
        "medical" => "Medical Services",
 "dental" => "Dental Services",
    "pharmacy" => "Pharmacy Services",
        "vision" => "Vision/Eye Care Services",
        "institutional" => "Institutional Services",
     "professional" => "Professional Services",
    _ => CategoryDescription ?? Category
    };

    /// <summary>
    /// Get total allowed amount across all benefits in this category
    /// </summary>
    public decimal GetTotalAllowedAmount()
    {
  return Benefits.Sum(b => b.AllowedAmount);
    }

    /// <summary>
    /// Get total used amount across all benefits in this category
    /// </summary>
  public decimal GetTotalUsedAmount()
    {
      return Benefits.Sum(b => b.UsedAmount);
    }

    /// <summary>
    /// Get remaining benefit amount
    /// </summary>
    public decimal GetRemainingAmount()
    {
        return GetTotalAllowedAmount() - GetTotalUsedAmount();
    }

 /// <summary>
    /// Get percentage of benefit used
    /// </summary>
    public decimal GetPercentageUsed()
    {
        var total = GetTotalAllowedAmount();
 if (total == 0) return 0;
        return (GetTotalUsedAmount() / total) * 100;
    }

    /// <summary>
    /// Check if this benefit category is exhausted
    /// </summary>
    public bool IsExhausted()
    {
        return GetRemainingAmount() <= 0;
    }
}
