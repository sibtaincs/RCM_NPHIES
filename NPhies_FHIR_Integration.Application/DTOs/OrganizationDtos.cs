namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// Organization DTO for API responses
/// </summary>
public class OrganizationDto : BaseDto
{
    public string OrganizationName { get; set; } = string.Empty;
public string LicenseNumber { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = string.Empty; // "prov" or "ins"
    public string SpecializationType { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
  public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Get organization type display name
    /// </summary>
    public string GetTypeDisplayName() => OrganizationType switch
    {
      "prov" => "Healthcare Provider",
        "ins" => "Insurance Company",
    _ => "Unknown"
    };
}

/// <summary>
/// Create Organization DTO for API requests
/// </summary>
public class CreateOrganizationDto
{
    public string OrganizationName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = string.Empty;
    public string SpecializationType { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

/// <summary>
/// Update Organization DTO for API requests
/// </summary>
public class UpdateOrganizationDto
{
    public string Website { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Organization search result DTO
/// </summary>
public class OrganizationSearchResultDto
{
    public IEnumerable<OrganizationDto> Items { get; set; } = new List<OrganizationDto>();
    public int TotalCount { get; set; }
 public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

/// <summary>
/// Provider list DTO (simplified)
/// </summary>
public class ProviderDto
{
    public string Id { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string SpecializationType { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// Insurer list DTO (simplified)
/// </summary>
public class InsurerDto
{
    public string Id { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
