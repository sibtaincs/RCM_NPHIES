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

    /// <summary>
    /// NphiesValidation | PayerBusinessError | Rejection (Error received by HCP guideline); null when the exchange succeeded.
    /// </summary>
    public string? ErrorKind { get; set; }
    /// <summary>Header-level <c>adjudication-outcome</c>: approved | partial | rejected | pended | not-required.</summary>
    public string? AdjudicationOutcome { get; set; }
    /// <summary>Comma-separated <c>meta-tags</c> codes on the response (queued-messages, nphies-generated, large-claim).</summary>
    public string? MetaTags { get; set; }
    /// <summary>NPHIES, not the payer, answered (payer offline or slow, batch, large claim): the queue is NPHIES's.</summary>
    public bool NphiesGenerated { get; set; }

    /// <summary>How many times the identical message (same Bundle.id and MessageHeader.id) was sent again after a timeout or transport failure.</summary>
    public int ResendCount { get; set; }
    public DateTime? LastResentAt { get; set; }
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
    /// <summary>Category bucket (e.g. Administrative, Coverage, MedicalNecessity, SupportingEvidence, BillingError, Technical).</summary>
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
    /// <summary>extension-newborn: the patient is a newborn checked against the mother's coverage (IG 6.5).</summary>
    public bool IsNewborn { get; set; }
    /// <summary>extension-transfer: check by the receiving provider of a referral (IG 7.3).</summary>
    public bool IsTransfer { get; set; }
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
    public string? ResponseIdentifierSystem { get; set; }
    /// <summary>Benefit items as JSON (category, network, authorizationRequired, allowed/used).</summary>
    public string? BenefitsJson { get; set; }
    /// <summary>Coverages discovered by a discovery request, as JSON.</summary>
    public string? DiscoveredCoveragesJson { get; set; }
    public string? ErrorsJson { get; set; }
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Eligibility confirmed outside NPHIES (payer portal or phone). Nothing was sent; later claims and prior
    /// authorizations carry <see cref="OfflineReference"/> and <see cref="OfflineDate"/> as
    /// <c>eligibility-offline-reference</c> and <c>eligibility-offline-date</c>.
    /// </summary>
    public bool IsOffline { get; set; }
    public string? OfflineReference { get; set; }
    public DateTime? OfflineDate { get; set; }
}

/// <summary>
/// A database lease that lets exactly one API instance run a background job (the NPHIES poll worker) at a time.
/// The holder renews it every cycle; another instance takes over only after it expires.
/// </summary>
public class RcmWorkerLease
{
    /// <summary>Job name, for example <c>nphies-poll</c>.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Instance that holds the lease (machine, process id and a random suffix).</summary>
    public string Owner { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// A claim or prior authorization that has had no final response for too long. Flagged after
/// <c>Rcm:StuckAfterDays</c> and escalated after <c>Rcm:StuckEscalateAfterDays</c> (30 by default), when NPHIES
/// support must be contacted with the bundle ids (Claim Submission Guideline).
/// </summary>
public class RcmStuckRequest : BaseEntity
{
    public string ClaimId { get; set; } = string.Empty;
    public string? ProviderId { get; set; }
    public string? InsurerId { get; set; }
    /// <summary>claim | preauthorization</summary>
    public string? Use { get; set; }
    /// <summary>The claim's NPHIES status when it was last scanned (Submitted | Queued | Pended | Timeout).</summary>
    public string ClaimStatus { get; set; } = string.Empty;
    /// <summary>When the request was sent; the waiting time counts from here.</summary>
    public DateTime WaitingSince { get; set; }
    /// <summary>The outbound transaction that carried the request.</summary>
    public string? TransactionId { get; set; }
    /// <summary>Bundle.id of the request: NPHIES support asks for it.</summary>
    public string? BundleId { get; set; }
    /// <summary>MessageHeader.id of the request.</summary>
    public string? MessageId { get; set; }
    public string? RequestIdentifier { get; set; }
    /// <summary>NPHIES, not the payer, is holding the request (the answer carried <c>nphies-generated</c>).</summary>
    public bool HeldByNphies { get; set; }
    /// <summary>Flagged | Escalated | Resolved</summary>
    public string Level { get; set; } = "Flagged";
    public DateTime FlaggedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EscalatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
