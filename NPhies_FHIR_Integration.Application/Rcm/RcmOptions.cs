namespace NPhies_FHIR_Integration.Application.Rcm;

/// <summary>Configuration for the RCM workflow layer (section <c>Rcm</c>).</summary>
public sealed class RcmOptions
{
    public const string SectionName = "Rcm";

    /// <summary>
    /// Run the background poll worker that ingests deferred NPHIES messages. When not set it runs everywhere except
    /// the Development environment: the IG expects providers to poll automatically, at least daily.
    /// </summary>
    public bool? PollWorkerEnabled { get; set; }

    /// <summary>Delay between poll cycles during working hours.</summary>
    public int PollIntervalSeconds { get; set; } = 300;

    /// <summary>Delay between poll cycles outside working hours. Never more than a day.</summary>
    public int OffHoursPollIntervalSeconds { get; set; } = 3600;

    /// <summary>Start of the working day, hour 0-23 in Saudi time (UTC+3).</summary>
    public int WorkingHoursStart { get; set; } = 7;

    /// <summary>End of the working day (exclusive), hour 1-24 in Saudi time (UTC+3).</summary>
    public int WorkingHoursEnd { get; set; } = 20;

    /// <summary>
    /// How long an instance keeps the poll lease without renewing it. Other instances take over after this, so it
    /// must be longer than one poll cycle takes. The worker never uses less than twice the current poll interval.
    /// </summary>
    public int PollLeaseSeconds { get; set; } = 900;

    /// <summary>
    /// A claim or prior authorization with no eligibility check picked is linked to the patient's latest eligible
    /// check with the same insurer, if it is no older than this. 0 turns the automatic link off.
    /// </summary>
    public int EligibilityLinkMaxAgeDays { get; set; } = 90;

    /// <summary>Flag a claim or prior authorization that has had no final response for this many days.</summary>
    public int StuckAfterDays { get; set; } = 3;

    /// <summary>Escalate it after this many days: NPHIES support must be contacted with the bundle ids.</summary>
    public int StuckEscalateAfterDays { get; set; } = 30;

    /// <summary>How often the poll worker scans for stuck requests.</summary>
    public int StuckScanIntervalMinutes { get; set; } = 60;

    /// <summary>The poll interval that applies at <paramref name="utcNow"/>, capped at one day.</summary>
    public TimeSpan PollIntervalAt(DateTime utcNow)
    {
        var hour = utcNow.AddHours(3).Hour; // Saudi Arabia is UTC+3 all year.
        var working = WorkingHoursStart <= WorkingHoursEnd
            ? hour >= WorkingHoursStart && hour < WorkingHoursEnd
            : hour >= WorkingHoursStart || hour < WorkingHoursEnd;
        var seconds = working ? PollIntervalSeconds : Math.Max(PollIntervalSeconds, OffHoursPollIntervalSeconds);
        return TimeSpan.FromSeconds(Math.Clamp(seconds, 10, 86400));
    }

    /// <summary>Maximum poll requests per cycle (the worker keeps polling while messages keep arriving).</summary>
    public int MaxPollsPerCycle { get; set; } = 10;

    /// <summary>Send a payment-notice automatically after a payment-reconciliation is ingested.</summary>
    public bool AutoAcknowledgePayments { get; set; } = true;

    /// <summary>Days after a denial during which an appeal/resubmission is allowed.</summary>
    public int AppealWindowDays { get; set; } = 30;

    /// <summary>
    /// Hours the hospital gives itself to answer a payer's information request, by request priority, when the payer
    /// sets no date (CommunicationRequest.occurrenceDateTime). NPHIES sets no deadline; these are local targets.
    /// A priority not listed uses <c>routine</c>.
    /// </summary>
    public Dictionary<string, int> InformationRequestResponseHours { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["routine"] = 48,
        ["urgent"] = 24,
        ["asap"] = 8,
        ["stat"] = 4,
    };

    /// <summary>An open information request due within this many hours is shown as due soon.</summary>
    public int InformationRequestDueSoonHours { get; set; } = 8;

    /// <summary>When an information request received at <paramref name="receivedAt"/> is due; null for a notification.</summary>
    public DateTime? InformationRequestDueAt(DateTime receivedAt, string? priority, string? category, DateTime? payerDate)
    {
        if (string.Equals(category, "notification", StringComparison.OrdinalIgnoreCase)) return null;
        if (payerDate > receivedAt) return payerDate;
        var hours = InformationRequestResponseHours.TryGetValue(priority ?? "routine", out var h) ? h
            : InformationRequestResponseHours.TryGetValue("routine", out var r) ? r : 48;
        return receivedAt.AddHours(Math.Max(1, hours));
    }

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
    /// <summary>Prior authorizations only: the payer says no authorization is needed (adjudication-outcome <c>not-required</c>).</summary>
    public const string NotRequired = "NotRequired";
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

    public static readonly string[] Adjudicated = { Approved, PartiallyApproved, Rejected, NotRequired, Paid, PartiallyPaid, Recouped };
    public static readonly string[] AwaitingPayer = { Submitted, Queued, Pended, Timeout, CancelRequested };
    /// <summary>Sent, with no final response yet: what the stuck-request scan watches.</summary>
    public static readonly string[] AwaitingFinalResponse = { Submitted, Queued, Pended, Timeout };
}

/// <summary>Values for <c>ClaimDenial.Status</c>.</summary>
public static class RcmDenialStatus
{
    public const string Open = "Open";
    public const string Resubmitted = "Resubmitted";
    public const string Appealed = "Appealed";
    public const string Accepted = "Accepted";
    public const string WrittenOff = "WrittenOff";
    public const string Overturned = "Overturned";
    /// <summary>
    /// The payer kept the denial after an appeal. When the appeal was a re-adjudication in the same claim, the row is
    /// still the live denial of that service: it can be appealed again, accepted or written off.
    /// </summary>
    public const string Upheld = "Upheld";

    /// <summary>Statuses a user may set manually.</summary>
    public static readonly string[] Manual = { Open, Accepted, WrittenOff };
}

/// <summary>Values for <c>AppealRequest.AppealMethod</c>.</summary>
public static class RcmAppealMethod
{
    /// <summary>A re-adjudication communication about the same claim; the payer reissues its ClaimResponse.</summary>
    public const string Communication = "Communication";
    /// <summary>A new claim with <c>related = prior</c> and all the services.</summary>
    public const string Resubmission = "Resubmission";
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
