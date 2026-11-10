namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Organization entity - represents healthcare providers and insurance companies
/// FHIR Resource: Organization
/// </summary>
public class Organization : BaseEntity
{
    /// <summary>
    /// Official name of the organization
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// License number (provider, insurer, or location license)
    /// </summary>
    public string LicenseNumber { get; set; } = string.Empty;

    /// <summary>
    /// License system identifier
    /// Examples:
    /// - http://nphies.sa/license/provider-license
    /// - http://nphies.sa/license/payer-license
    /// </summary>
    public string LicenseSystem { get; set; } = string.Empty;

    /// <summary>
    /// License type system URL for categorization
    /// </summary>
    public string? LicenseTypeSystem { get; set; }

    /// <summary>
    /// License use classification (e.g., "official")
    /// </summary>
    public string? LicenseUse { get; set; }

    /// <summary>
    /// Original FHIR resource ID for reference
    /// </summary>
    public string? FhirId { get; set; }

    // Organization Classification
    /// <summary>
    /// Organization type: "prov" (Provider), "ins" (Insurer), "auth" (Authority)
    /// </summary>
    public string OrganizationType { get; set; } = string.Empty;

    /// <summary>
    /// Provider type code (e.g., "1" for certain provider types)
    /// System: http://nphies.sa/terminology/CodeSystem/provider-type
    /// </summary>
    public string? ProviderTypeCode { get; set; }

    /// <summary>
    /// Provider type coding system
    /// </summary>
    public string? ProviderTypeSystem { get; set; }

    /// <summary>
    /// Specialization/Type: Hospital, Clinic, Pharmacy, Laboratory, etc.
    /// </summary>
    public string SpecializationType { get; set; } = string.Empty;

    /// <summary>
    /// Full address text from FHIR
    /// </summary>
    public string? AddressText { get; set; }

    /// <summary>
    /// Address country
    /// </summary>
    public string? AddressCountry { get; set; }

    // Contact Information
    /// <summary>
    /// Organization website URL
    /// </summary>
    public string Website { get; set; } = string.Empty;

    /// <summary>
    /// Organization email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Organization phone number
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Contact phone usage type (e.g., "mobile", "work")
    /// </summary>
    public string? ContactPhoneUse { get; set; }

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

    // Status
    /// <summary>
    /// Organization status: active, inactive
    /// </summary>
    public string Status { get; set; } = "active";

    // ========== SAUDI ARABIA SPECIFIC IDENTIFIERS ==========

    /// <summary>
    /// Ministry of Health (MOH) License Number
    /// Required for healthcare providers in Saudi Arabia
    /// </summary>
    public string? MOHLicenseNumber { get; set; }

    /// <summary>
    /// Council of Health Insurance (CHI) Number
    /// Required for insurance companies operating in Saudi Arabia
    /// </summary>
    public string? CHINumber { get; set; }

    /// <summary>
    /// NPHIES Organization Identifier
    /// Unique identifier assigned by NPHIES platform
    /// </summary>
    public string? NphiesOrganizationId { get; set; }

    /// <summary>
    /// NPHIES Provider ID
    /// Specific identifier for providers in NPHIES system
    /// </summary>
    public string? NphiesProviderId { get; set; }

    /// <summary>
    /// NPHIES Payer ID
    /// Specific identifier for insurance payers in NPHIES system
    /// </summary>
    public string? NphiesPayerId { get; set; }

    /// <summary>
    /// Tax Registration Number / VAT Number
    /// Required for billing and tax purposes in Saudi Arabia
    /// </summary>
    public string? TaxRegistrationNumber { get; set; }

    // Navigation Properties
    /// <summary>
    /// Collection of locations managed by this organization
    /// </summary>
    public ICollection<Location> Locations { get; set; } = new List<Location>();

    /// <summary>
    /// Collection of practitioners working for this organization
    /// </summary>
    public ICollection<Practitioner> Practitioners { get; set; } = new List<Practitioner>();

    /// <summary>
    /// Collection of claims submitted by this organization (if provider)
    /// </summary>
    public ICollection<Claim> SubmittedClaims { get; set; } = new List<Claim>();

    /// <summary>
    /// Collection of claims processed by this organization (if insurer)
    /// </summary>
    public ICollection<Claim> ProcessedClaims { get; set; } = new List<Claim>();

    /// <summary>
    /// Collection of eligibility requests from this organization (if provider)
    /// </summary>
    public ICollection<CoverageEligibilityRequest> EligibilityRequests { get; set; } = new List<CoverageEligibilityRequest>();

    /// <summary>
    /// Collection of eligibility responses from this organization (if insurer)
    /// </summary>
    public ICollection<CoverageEligibilityResponse> EligibilityResponses { get; set; } = new List<CoverageEligibilityResponse>();

    /// <summary>
    /// Check if organization is a provider
    /// </summary>
    public bool IsProvider => OrganizationType == "prov";

    /// <summary>
    /// Check if organization is an insurer
    /// </summary>
    public bool IsInsurer => OrganizationType == "ins";

    /// <summary>
    /// Check if organization is active
    /// </summary>
    public bool IsActiveOrganization => Status == "active";

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
