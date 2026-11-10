namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Practitioner entity - represents a healthcare professional (doctor, nurse, etc.)
/// FHIR Resource: Practitioner
/// </summary>
public class Practitioner : BaseEntity
{
    /// <summary>
    /// Practitioner's first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Practitioner's last name/family name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Professional license number
    /// </summary>
    public string LicenseNumber { get; set; } = string.Empty;

    /// <summary>
    /// License system identifier
    /// Typically: http://nphies.sa/license/practitioner-license
    /// </summary>
    public string LicenseSystem { get; set; } = string.Empty;

    // Professional Details
    /// <summary>
    /// Medical specialization (Cardiologist, Dentist, Pediatrician, etc.)
    /// </summary>
    public string Specialization { get; set; } = string.Empty;

    /// <summary>
    /// Professional qualification/credentials
    /// </summary>
    public string Qualification { get; set; } = string.Empty;

    /// <summary>
    /// Professional title (Dr., Prof., Nurse, etc.)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    // Contact Information
    /// <summary>
  /// Practitioner email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Practitioner phone number
  /// </summary>
    public string Phone { get; set; } = string.Empty;

    // Organization Information
    /// <summary>
    /// Reference to the organization where practitioner works
    /// </summary>
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>
    /// The organization where this practitioner is employed
    /// </summary>
    public Organization? Organization { get; set; }

    // Status
    /// <summary>
    /// Practitioner status: active, inactive, retired, on-leave
    /// </summary>
    public string Status { get; set; } = "active";

    // ========== SAUDI ARABIA SPECIFIC FIELDS ==========

    /// <summary>
    /// Practitioner License Number (may differ from generic LicenseNumber)
    /// Professional practice license issued by relevant authority
    /// </summary>
    public string? PractitionerLicenseNumber { get; set; }

    /// <summary>
    /// License Issuing Authority
    /// E.g., "Saudi Commission for Health Specialties (SCFHS)", "Ministry of Health", etc.
  /// </summary>
    public string? LicenseIssuingAuthority { get; set; }

    /// <summary>
    /// License Expiry Date
    /// Date when the practitioner's license expires and needs renewal
    /// </summary>
    public DateTime? LicenseExpiryDate { get; set; }

    /// <summary>
    /// National Identification Number (National ID / Iqama Number)
    /// Saudi National ID for citizens or Iqama number for residents
    /// </summary>
    public string? NationalIdentificationNumber { get; set; }

    /// <summary>
    /// Practitioner Role Code
/// E.g., "doctor", "nurse", "pharmacist", "physiotherapist", "technician"
    /// Maps to FHIR PractitionerRole.code
    /// </summary>
    public string? PractitionerRole { get; set; }

  /// <summary>
    /// Practitioner Role System URL
    /// System URL for the practitioner role coding
    /// E.g., "http://terminology.hl7.org/CodeSystem/practitioner-role"
    /// </summary>
public string? PractitionerRoleSystem { get; set; }

    // Navigation Properties
/// <summary>
    /// Collection of claims where this practitioner is the provider
    /// </summary>
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();

    public DoctorMaster? DoctorMaster { get; set; }

    /// <summary>
    /// Get full name of practitioner
    /// </summary>
    public string GetFullName() => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Get display name with title (e.g., "Dr. Mohammed Abdullah")
  /// </summary>
    public string GetDisplayName()
    {
        var nameWithTitle = string.IsNullOrEmpty(Title) 
    ? GetFullName() 
       : $"{Title} {GetFullName()}";
 
        return nameWithTitle.Trim();
    }

    /// <summary>
    /// Check if practitioner is active
    /// </summary>
    public bool IsActive => Status == "active";

    /// <summary>
    /// Check if practitioner license is valid (not expired)
    /// </summary>
    public bool IsLicenseValid => !LicenseExpiryDate.HasValue || LicenseExpiryDate.Value > DateTime.Now;

    /// <summary>
 /// Check if practitioner license is expiring soon (within 90 days)
    /// </summary>
    public bool IsLicenseExpiringSoon => LicenseExpiryDate.HasValue && 
              LicenseExpiryDate.Value <= DateTime.Now.AddDays(90) &&
               LicenseExpiryDate.Value > DateTime.Now;

    /// <summary>
    /// Check if practitioner has the required specialization
    /// </summary>
public bool HasSpecialization(string specialization)
    {
    return !string.IsNullOrEmpty(Specialization) && 
               Specialization.Equals(specialization, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get license status summary
  /// </summary>
    public string GetLicenseStatus()
    {
        if (!LicenseExpiryDate.HasValue) return "No Expiry Set";
   if (LicenseExpiryDate.Value < DateTime.Now) return "Expired";
        if (IsLicenseExpiringSoon) return "Expiring Soon";
        return "Valid";
 }
}
