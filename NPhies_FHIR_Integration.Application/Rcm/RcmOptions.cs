namespace NPhies_FHIR_Integration.Application.Rcm;

/// <summary>Configuration for the RCM workflow layer (section <c>Rcm</c>).</summary>
public sealed class RcmOptions
{
    public const string SectionName = "Rcm";

    /// <summary>Run the background poll worker that ingests deferred NPHIES messages.</summary>
    public bool PollWorkerEnabled { get; set; } = false;

    /// <summary>Delay between poll cycles.</summary>
    public int PollIntervalSeconds { get; set; } = 300;

    /// <summary>Maximum poll requests per cycle (the worker keeps polling while messages keep arriving).</summary>
    public int MaxPollsPerCycle { get; set; } = 10;

    /// <summary>Send a payment-notice automatically after a payment-reconciliation is ingested.</summary>
    public bool AutoAcknowledgePayments { get; set; } = true;

    /// <summary>Days after a denial during which an appeal/resubmission is allowed.</summary>
    public int AppealWindowDays { get; set; } = 30;

    /// <summary>Tolerance (SAR) when matching reconciliation amounts to adjudicated amounts.</summary>
    public decimal PaymentMatchTolerance { get; set; } = 0.01m;

    /// <summary>
    /// Base used for provider-assigned identifier systems (claim, preauth, task, communication...).
    /// Defaults to <c>Nphies:Sender:ProviderBaseUrl</c>.
    /// </summary>
    public string? IdentifierBaseUrl { get; set; }

    /// <summary>
    /// Require both technical and medical review approval before a claim (not a prior authorization) is sent
    /// to NPHIES. Batch submission applies the same gate.
    /// </summary>
    public bool RequireReviewApprovalForClaims { get; set; } = true;
}

/// <summary>Values for <c>Claim.NphiesStatus</c>.</summary>
public static class RcmClaimStatus
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string Queued = "Queued";
    public const string Pended = "Pended";
    public const string Approved = "Approved";
    public const string PartiallyApproved = "PartiallyApproved";
    public const string Rejected = "Rejected";
    public const string Error = "Error";
    public const string ValidationFailed = "ValidationFailed";
    public const string Timeout = "Timeout";
    public const string CancelRequested = "CancelRequested";
    public const string Cancelled = "Cancelled";
    public const string Nullified = "Nullified";
    public const string Paid = "Paid";
    public const string PartiallyPaid = "PartiallyPaid";
    /// <summary>The payer took back (recouped) everything it had paid on this claim.</summary>
    public const string Recouped = "Recouped";
    public const string Superseded = "Superseded";

    public static readonly string[] Adjudicated = { Approved, PartiallyApproved, Rejected, Paid, PartiallyPaid, Recouped };
    public static readonly string[] AwaitingPayer = { Submitted, Queued, Pended, Timeout, CancelRequested };
}

/// <summary>Values for <c>NphiesTransaction.Status</c>.</summary>
public static class RcmTransactionStatus
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Queued = "Queued";
    public const string Error = "Error";
    public const string ValidationFailed = "ValidationFailed";
    public const string Timeout = "Timeout";
    public const string Duplicate = "Duplicate";
    public const string Unmatched = "Unmatched";
}

public static class RcmOperations
{
    public const string Eligibility = "eligibility";
    public const string PriorAuth = "priorauth";
    public const string Claim = "claim";
    public const string Batch = "batch";
    public const string StatusCheck = "status-check";
    public const string Cancel = "cancel";
    public const string Nullify = "nullify";
    public const string Poll = "poll";
    public const string Communication = "communication";
    public const string PaymentNotice = "payment-notice";
    public const string Inbound = "inbound";
}

public static class RcmEntityTypes
{
    public const string Claim = "Claim";
    public const string Batch = "Batch";
    public const string EligibilityCheck = "EligibilityCheck";
    public const string Communication = "Communication";
    public const string CommunicationRequest = "CommunicationRequest";
    public const string PaymentReconciliation = "PaymentReconciliation";
    public const string AdvancedAuthorization = "AdvancedAuthorization";
    public const string Poll = "Poll";
}
