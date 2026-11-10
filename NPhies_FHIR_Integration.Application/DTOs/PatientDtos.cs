namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// Base DTO for read operations
/// </summary>
public class BaseDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Patient DTO for API responses
/// </summary>
public class PatientDto : BaseDto
{
    public string MRN { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    /// <summary>Raw identifier type code as stored (NI, PRC, PPN, BN, DP, MR).</summary>
    public string? NationalIdType { get; set; }
    public string? NationalIdSystem { get; set; }
    /// <summary>ISO 3166-1 alpha-3 issuing country of the identifier; required for passports.</summary>
    public string? NationalIdCountry { get; set; }
    /// <summary>Resolved identifier kind: NationalId | Iqama | Passport | BorderNumber | DisplacedPerson | MedicalRecord.</summary>
    public string IdentifierType { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? FirstNameArabic { get; set; }
    public string? LastNameArabic { get; set; }
    public string? FullNameArabic { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? MaritalStatus { get; set; }
    public string? Occupation { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
 public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>
    /// Get full name
    /// </summary>
    public string GetFullName() => $"{FirstName} {LastName}";
}

/// <summary>
/// Create Patient DTO for API requests
/// </summary>
public class CreatePatientDto
{
    public string MRN { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    /// <summary>NationalId | Iqama | Passport | BorderNumber | DisplacedPerson (or the NPHIES code NI/PRC/PPN/BN/DP).</summary>
    public string? IdentifierType { get; set; }
    /// <summary>ISO 3166-1 alpha-3 issuing country (e.g. "IND"); required for passports.</summary>
    public string? NationalIdCountry { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FirstNameArabic { get; set; }
    public string? LastNameArabic { get; set; }
    public string? FullNameArabic { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? MaritalStatus { get; set; }
    public string? Occupation { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Update Patient DTO for API requests
/// </summary>
public class UpdatePatientDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FirstNameArabic { get; set; }
    public string? LastNameArabic { get; set; }
    public string? FullNameArabic { get; set; }
    public string? NationalId { get; set; }
    public string? IdentifierType { get; set; }
    public string? NationalIdCountry { get; set; }
    public string? MaritalStatus { get; set; }
 public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
  public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Patient search result DTO
/// </summary>
public class PatientSearchResultDto
{
    public IEnumerable<PatientDto> Items { get; set; } = new List<PatientDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}
