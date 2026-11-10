namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// EligibilityItemModifier entity - represents modifiers for eligibility request items
/// FHIR Resource: CoverageEligibilityRequest.item.modifier
/// </summary>
public class EligibilityItemModifier : BaseEntity
{
    /// <summary>
    /// Reference to the parent eligibility item
    /// </summary>
    public string EligibilityItemId { get; set; } = string.Empty;

    /// <summary>
    /// The parent eligibility item
    /// </summary>
  public EligibilityItem? EligibilityItem { get; set; }

    /// <summary>
    /// Modifier code
    /// System: http://terminology.hl7.org/CodeSystem/modifier
    /// </summary>
    public string ModifierCode { get; set; } = string.Empty;

    /// <summary>
    /// Modifier system
 /// </summary>
    public string ModifierSystem { get; set; } = "http://terminology.hl7.org/CodeSystem/modifier";

/// <summary>
    /// Description of the modifier
    /// </summary>
    public string ModifierDescription { get; set; } = string.Empty;

    /// <summary>
    /// Get modifier name (human-readable)
/// </summary>
    public string GetModifierName() => ModifierCode switch
    {
        "BILATERAL" => "Bilateral Procedure",
 "STAGED" => "Staged Procedure",
        "GLOBAL" => "Global Procedure",
        _ => ModifierDescription ?? ModifierCode
    };
}
