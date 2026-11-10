namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Insurer-initiated authorization (NPHIES profile <c>advanced-authorization</c>, event <c>advanced-authorization</c>).
/// The payer pushes a ClaimResponse via poll that authorizes services for a patient at a service provider
/// (refill, referral, second opinion, home healthcare, ...). The provider later bills it by quoting <see cref="PreAuthRef"/>.
/// </summary>
public class AdvancedAuthorization : BaseEntity
{
    /// <summary>FHIR ClaimResponse.id</summary>
    public string ClaimResponseId { get; set; } = string.Empty;
    public string? IdentifierSystem { get; set; }
    public string? IdentifierValue { get; set; }

    /// <summary>Authorization number to quote on the claim (Claim.insurance.preAuthRef).</summary>
    public string? PreAuthRef { get; set; }
    public DateTime? PreAuthPeriodStart { get; set; }
    public DateTime? PreAuthPeriodEnd { get; set; }

    /// <summary>advancedAuth-reason code: refill | second-opinion | referral | authorization | non-network | medication-dispense | home-healthcare</summary>
    public string? Reason { get; set; }
    public string? ClaimType { get; set; }
    public string? SubType { get; set; }
    public string? Outcome { get; set; }
    public string? AdjudicationOutcome { get; set; }
    public string? Disposition { get; set; }

    public string? PatientId { get; set; }
    public string? PatientIdentifier { get; set; }
    public string? PatientName { get; set; }
    public string? InsurerId { get; set; }
    public string? InsurerLicense { get; set; }
    /// <summary>Provider organization the services are authorized at (our facility when matched).</summary>
    public string? ServiceProviderId { get; set; }
    public string? ServiceProviderLicense { get; set; }
    public string? ReferringProviderLicense { get; set; }

    public decimal? TotalEligible { get; set; }
    public decimal? TotalBenefit { get; set; }
    public string Currency { get; set; } = "SAR";

    /// <summary>JSON array of authorized addItem lines (sequence, code, quantity, adjudication).</summary>
    public string? ItemsJson { get; set; }
    /// <summary>JSON array of diagnosis codings.</summary>
    public string? DiagnosesJson { get; set; }
    public string? FhirClaimResponseJson { get; set; }

    /// <summary>Active | Used | Cancelled (expiry is derived from <see cref="PreAuthPeriodEnd"/>).</summary>
    public string Status { get; set; } = "Active";
    public string? UsedByClaimId { get; set; }
    public DateTime? UsedAt { get; set; }

    public string? MessageId { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
