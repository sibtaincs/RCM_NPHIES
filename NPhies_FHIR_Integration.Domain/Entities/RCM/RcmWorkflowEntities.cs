namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// One NPHIES business message exchanged by the RCM workflow layer (outbound request or inbound
/// deferred message delivered by poll). Correlates the MessageHeader id with the local entity it
/// concerns and is the idempotency key for inbound ingestion.
/// </summary>
public class NphiesTransaction : BaseEntity
{
    /// <summary>outbound | inbound</summary>
    public string Direction { get; set; } = "outbound";

    /// <summary>eligibility | priorauth | claim | batch | status-check | cancel | poll | communication | payment-notice | inbound</summary>
    public string Operation { get; set; } = string.Empty;

    public string EventCode { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string? BundleId { get; set; }
    public string? CorrelationId { get; set; }

    /// <summary>Claim | EligibilityCheck | Communication | PaymentReconciliation | CommunicationRequest | Batch</summary>
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }

    public string? RequestIdentifierSystem { get; set; }
    public string? RequestIdentifierValue { get; set; }

    /// <summary>Provider organization (facility) that owns the message – used for tenant scoping.</summary>
    public string? ProviderId { get; set; }

    /// <summary>Pending | Completed | Queued | Error | Rejected | Timeout</summary>
    public string Status { get; set; } = "Pending";
    public string? ResponseCode { get; set; }
    public string? Outcome { get; set; }
    public string? ResponseMessageId { get; set; }
    public string? ResponseEventCode { get; set; }
    public string? ErrorSummary { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Per-item adjudication returned in a ClaimResponse (claim or prior-authorization).
/// </summary>
public class ClaimResponseItem : BaseEntity
{
    public string ClaimResponseId { get; set; } = string.Empty;
    public ClaimResponse? ClaimResponse { get; set; }
    public string ClaimId { get; set; } = string.Empty;
    public int ItemSequence { get; set; }
    /// <summary>approved | rejected | partial | pended</summary>
    public string? Outcome { get; set; }
    public decimal? Submitted { get; set; }
    public decimal? Eligible { get; set; }
    public decimal? Benefit { get; set; }
    public decimal? Copay { get; set; }
    public decimal? Deductible { get; set; }
    public decimal? Tax { get; set; }
    public decimal? PatientShare { get; set; }
    public decimal? ApprovedQuantity { get; set; }
    public string? ReasonCode { get; set; }
    public string? ReasonDisplay { get; set; }
    /// <summary>Raw adjudication list as JSON (category, amount, value, reason).</summary>
    public string? AdjudicationsJson { get; set; }
}

/// <summary>
/// A denial (rejected or partially paid line, or a whole-claim rejection) derived from real
/// adjudication data. Drives the denial work-queue and appeals.
/// </summary>
public class ClaimDenial : BaseEntity
{
    public string ClaimId { get; set; } = string.Empty;
    public Claim? Claim { get; set; }
    public string? ClaimResponseId { get; set; }
    /// <summary>Null for header-level (whole claim) denials.</summary>
    public int? ItemSequence { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? ReasonDisplay { get; set; }
    /// <summary>Category bucket (e.g. Medical necessity, Coverage, Coding, Administrative, Technical).</summary>
    public string Category { get; set; } = "Other";
    public decimal SubmittedAmount { get; set; }
    public decimal DeniedAmount { get; set; }
    /// <summary>Open | Appealed | Resubmitted | Overturned | Upheld | WrittenOff</summary>
    public string Status { get; set; } = "Open";
    public DateTime DenialDate { get; set; } = DateTime.UtcNow;
    public DateTime AppealDeadline { get; set; }
    public string? AppealRequestId { get; set; }
    public string? ResubmissionClaimId { get; set; }
    public string? ProviderId { get; set; }
    public string? InsurerId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// A CoverageEligibilityRequest/Response exchange performed by the RCM workflow.
/// </summary>
public class EligibilityCheck : BaseEntity
{
    public string PatientId { get; set; } = string.Empty;
    public Patient? Patient { get; set; }
    public string? CoverageId { get; set; }
    public Coverage? Coverage { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string InsurerId { get; set; } = string.Empty;
    /// <summary>Comma separated purposes: benefits, validation, discovery, auth-requirements.</summary>
    public string Purposes { get; set; } = "validation";
    public DateTime ServicedDate { get; set; }
    public string RequestIdentifierSystem { get; set; } = string.Empty;
    public string RequestIdentifierValue { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    /// <summary>Pending | Completed | Error | Timeout | Rejected</summary>
    public string Status { get; set; } = "Pending";
    public string? Outcome { get; set; }
    public bool? IsEligible { get; set; }
    public bool? InForce { get; set; }
    public string? SiteEligibility { get; set; }
    public string? Disposition { get; set; }
    public string? NotInForceReason { get; set; }
    public DateTime? BenefitPeriodStart { get; set; }
    public DateTime? BenefitPeriodEnd { get; set; }
    public string? ResponseIdentifier { get; set; }
    /// <summary>Benefit items as JSON (category, network, authorizationRequired, allowed/used).</summary>
    public string? BenefitsJson { get; set; }
    /// <summary>Coverages discovered by a discovery request, as JSON.</summary>
    public string? DiscoveredCoveragesJson { get; set; }
    public string? ErrorsJson { get; set; }
    public DateTime? RespondedAt { get; set; }
}
