namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// EligibilityItem entity - represents service items in an eligibility request
/// FHIR Resource: CoverageEligibilityRequest.item
/// </summary>
public class EligibilityItem : BaseEntity
{
  /// <summary>
 /// Reference to the parent eligibility request
    /// </summary>
    public string EligibilityRequestId { get; set; } = string.Empty;

    /// <summary>
    /// The parent eligibility request
/// </summary>
    public CoverageEligibilityRequest? EligibilityRequest { get; set; }

    /// <summary>
    /// Sequence number of this item in the request
    /// </summary>
    public int SequenceNumber { get; set; }

    // Service Classification
    /// <summary>
    /// Service category code
    /// Examples: "medical", "dental", "pharmacy", "vision", "institutional", "professional"
    /// System: http://terminology.hl7.org/CodeSystem/ex-benefitcategory
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Service category system
    /// </summary>
    public string CategorySystem { get; set; } = "http://terminology.hl7.org/CodeSystem/ex-benefitcategory";

    /// <summary>
    /// Human-readable category description
    /// </summary>
    public string CategoryDescription { get; set; } = string.Empty;

 // Service Code (Optional)
    /// <summary>
    /// Specific procedure/service code (optional)
    /// System: http://nphies.sa/terminology/CodeSystem/procedure-code
    /// </summary>
    public string ProductOrServiceCode { get; set; } = string.Empty;

    /// <summary>
    /// Product or service code system
    /// </summary>
    public string ProductOrServiceSystem { get; set; } = "http://nphies.sa/terminology/CodeSystem/procedure-code";

    /// <summary>
    /// Description of the procedure/service
    /// </summary>
    public string ProductOrServiceDescription { get; set; } = string.Empty;

    // Modifiers (Optional)
    /// <summary>
    /// Collection of service modifiers
    /// </summary>
  public ICollection<EligibilityItemModifier> Modifiers { get; set; } = new List<EligibilityItemModifier>();

    // Additional Information
    /// <summary>
    /// Diagnosis codes related to this service
  /// </summary>
    public string DiagnosisCodes { get; set; } = string.Empty;

    /// <summary>
    /// Any notes or comments about this item
    /// </summary>
    public string Notes { get; set; } = string.Empty;

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
}
