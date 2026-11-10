namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Patient (Member) entity - represents a healthcare member/patient
/// FHIR Resource: Patient
/// </summary>
public class Patient : BaseEntity
{
    /// <summary>
    /// Member Registration Number (MRN) - unique identifier for the member
    /// </summary>
    public string MRN { get; set; } = string.Empty;

    /// <summary>
    /// Identifier system - typically http://nphies.sa/identifier/member-id
    /// </summary>
    public string IdentifierSystem { get; set; } = string.Empty;

    /// <summary>
    /// National ID or other government ID
    /// </summary>
    public string NationalId { get; set; } = string.Empty;

    /// <summary>
    /// National ID identifier system (e.g., "http://nphies.sa/identifier/nationalid")
    /// </summary>
    public string? NationalIdSystem { get; set; }

    /// <summary>
    /// National ID type (e.g., "NI" for National ID)
    /// </summary>
    public string? NationalIdType { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-3 issuing country of the identifier (e.g. "IND"). Required by NPHIES for passports
    /// (Patient.identifier:passport.extension:country, BV-00481).
    /// </summary>
    public string? NationalIdCountry { get; set; }

    // Demographics
    /// <summary>
    /// Patient's first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Patient's last name/family name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Patient's complete full name as provided in FHIR
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Patient's date of birth in YYYY-MM-DD format
    /// </summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Patient gender: M (Male), F (Female), O (Other), U (Unknown)
    /// </summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Patient's marital status code (M=Married, S=Single, etc.)
    /// </summary>
    public string? MaritalStatus { get; set; }

    /// <summary>
    /// Patient's occupation code
    /// </summary>
    public string? Occupation { get; set; }

    /// <summary>
    /// Occupation coding system (e.g., "http://nphies.sa/terminology/CodeSystem/occupation")
    /// </summary>
    public string? OccupationSystem { get; set; }

    /// <summary>
    /// Is patient deceased
    /// </summary>
    public bool Deceased { get; set; } = false;

    /// <summary>
    /// Is patient record active
    /// </summary>
    public bool Active { get; set; } = true;

    // Contact Information
    /// <summary>
    /// Patient's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Patient's phone number
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Phone system type (e.g., "phone", "fax", "email")
    /// </summary>
    public string? PhoneSystem { get; set; }

    /// <summary>
    /// Phone usage context (e.g., "mobile", "home", "work")
    /// </summary>
    public string? PhoneUse { get; set; }

    // Address Information
    /// <summary>
    /// Address line 1 (street address)
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Address line 2 (apartment, suite, etc.)
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
    public string Country { get; set; } = string.Empty;

    // Status
    /// <summary>
    /// Patient status: active, inactive, deceased
    /// </summary>
    public string Status { get; set; } = "active";

    // Navigation Properties
    /// <summary>
    /// Collection of coverages for this patient
    /// </summary>
    public ICollection<Coverage> Coverages { get; set; } = new List<Coverage>();

    /// <summary>
    /// Collection of eligibility requests for this patient
    /// </summary>
    public ICollection<CoverageEligibilityRequest> EligibilityRequests { get; set; } = new List<CoverageEligibilityRequest>();

    /// <summary>
    /// Collection of claims for this patient
    /// </summary>
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();

    /// <summary>Patient name in Arabic (shown on KSA documents and sent as name text when no Latin name exists).</summary>
    public string? FirstNameArabic { get; set; }
    public string? LastNameArabic { get; set; }
    public string? FullNameArabic { get; set; }

    /// <summary>
    /// Get full name of patient
    /// </summary>
    public string GetFullName() => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Calculate patient age based on date of birth
    /// </summary>
    public int GetAge()
    {
        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > today.AddYears(-age))
            age--;
        return age;
    }
}
