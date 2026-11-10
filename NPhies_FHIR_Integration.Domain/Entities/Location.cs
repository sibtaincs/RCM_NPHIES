namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Location entity - represents a physical location/facility where services are provided
/// FHIR Resource: Location
/// </summary>
public class Location : BaseEntity
{
    /// <summary>
 /// Name of the location/facility
    /// </summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// License number specific to this location
    /// </summary>
    public string LocationLicense { get; set; } = string.Empty;

    /// <summary>
    /// License system identifier
    /// Typically: http://nphies.sa/license/location-license
    /// </summary>
    public string LicenseSystem { get; set; } = string.Empty;

    // Foreign Keys
    /// <summary>
/// Reference to parent organization ID
/// </summary>
    public string OrganizationId { get; set; } = string.Empty;

    // Navigation Properties
    /// <summary>
    /// The organization that manages this location
    /// </summary>
    public Organization? Organization { get; set; }

    // Location Details
    /// <summary>
    /// Facility type code
  /// Examples: GACH (General Acute Care Hospital), CLI (Clinic), PHARM (Pharmacy), LAB (Laboratory), ACSN (Ambulatory Surgery Center)
    /// </summary>
    public string FacilityType { get; set; } = string.Empty;

    /// <summary>
    /// Facility type description
    /// </summary>
    public string FacilityTypeDescription { get; set; } = string.Empty;

    // Address Information
    /// <summary>
    /// Address line 1 (street address)
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Address line 2 (suite, floor, etc.)
    /// </summary>
    public string AddressLine2 { get; set; } = string.Empty;

    /// <summary>
    /// City name
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State/Region name
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Postal code
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country code (e.g., "SA" for Saudi Arabia)
    /// </summary>
    public string Country { get; set; } = "SA";

    // Operating Information
    /// <summary>
    /// Location phone number
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Location email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Location operating status: active, inactive
    /// </summary>
    public string Status { get; set; } = "active";

    // Navigation Properties
    /// <summary>
    /// Collection of claims served from this location
    /// </summary>
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();

    /// <summary>
    /// Check if location is active
    /// </summary>
    public bool IsActive => Status == "active";

    /// <summary>
    /// Get facility type name (human-readable)
    /// </summary>
    public string GetFacilityTypeName() => FacilityType switch
 {
      "GACH" => "General Acute Care Hospital",
        "CLI" => "Clinic",
        "PHARM" => "Pharmacy",
        "LAB" => "Laboratory",
    "ACSN" => "Ambulatory Surgery Center",
     "MORL" => "Morale Center",
    "HOSP" => "Hospital",
        "OUTB" => "Outpatient Facility",
        _ => FacilityTypeDescription ?? FacilityType
  };

    /// <summary>
    /// Get full address as a single string
    /// </summary>
    public string GetFullAddress()
    {
  var parts = new List<string>();
        if (!string.IsNullOrEmpty(AddressLine1)) parts.Add(AddressLine1);
        if (!string.IsNullOrEmpty(AddressLine2)) parts.Add(AddressLine2);
  if (!string.IsNullOrEmpty(City)) parts.Add(City);
        if (!string.IsNullOrEmpty(State)) parts.Add(State);
        if (!string.IsNullOrEmpty(PostalCode)) parts.Add(PostalCode);

        return string.Join(", ", parts);
    }
}
