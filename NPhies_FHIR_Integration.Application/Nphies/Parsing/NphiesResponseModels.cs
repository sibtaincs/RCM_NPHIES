using Hl7.Fhir.Model;

namespace NPhies_FHIR_Integration.Application.Nphies.Parsing;

/// <summary>Where an error was reported.</summary>
public enum NphiesErrorSource { OperationOutcome, ClaimResponse, EligibilityResponse, TaskOutput, MessageHeader, Transport }

/// <summary>
/// An error returned by NPHIES or the payer, enriched with the catalog display text.
/// <see cref="ItemSequence"/> is set for item-level ClaimResponse errors.
/// </summary>
public sealed record NphiesError(
    string Code,
    string? Message,
    string? Expression,
    NphiesErrorSource Source,
    int? ItemSequence = null,
    string? Severity = null);

public sealed record NphiesAmount(decimal? Value, string? Currency);

public sealed record NphiesAdjudication(string Category, decimal? Amount, string? Currency, decimal? Value, string? Reason);

public sealed class NphiesItemAdjudication
{
    public int ItemSequence { get; init; }
    /// <summary>adjudication-outcome: approved, rejected, partial, not-required, pended.</summary>
    public string? Outcome { get; init; }
    public List<NphiesAdjudication> Adjudications { get; init; } = new();
    public List<NphiesItemAdjudication> Details { get; init; } = new();

    public decimal? AmountOf(string category) =>
        Adjudications.FirstOrDefault(a => string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase))?.Amount;
}

/// <summary>Result of a claim-response or priorauth-response.</summary>
public sealed class NphiesClaimAdjudicationResult
{
    public string? Id { get; init; }
    public string? Identifier { get; init; }
    /// <summary>Identifier value of the Claim this response answers (ClaimResponse.request.identifier).</summary>
    public string? RequestIdentifier { get; init; }
    public string? RequestIdentifierSystem { get; init; }
    public string? Use { get; init; }
    public string? ClaimType { get; init; }
    /// <summary>FHIR outcome: queued, complete, error, partial.</summary>
    public string? Outcome { get; init; }
    /// <summary>NPHIES adjudication-outcome extension: approved, rejected, partial, not-required, pended.</summary>
    public string? AdjudicationOutcome { get; init; }
    public string? ReissueReason { get; init; }
    public string? Disposition { get; init; }
    public string? PreAuthRef { get; init; }
    public string? PreAuthPeriodStart { get; init; }
    public string? PreAuthPeriodEnd { get; init; }
    public List<NphiesAdjudication> Totals { get; init; } = new();
    public List<NphiesItemAdjudication> Items { get; init; } = new();
    public NphiesAmount? Payment { get; init; }
    public string? PaymentDate { get; init; }
    public List<string> ProcessNotes { get; init; } = new();
    public List<string> CommunicationRequests { get; init; } = new();

    public decimal? TotalOf(string category) =>
        Totals.FirstOrDefault(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase))?.Amount;
}

public sealed class NphiesBenefit
{
    public string? Type { get; init; }
    public decimal? AllowedMoney { get; init; }
    public decimal? UsedMoney { get; init; }
    public string? Allowed { get; init; }
    public string? Used { get; init; }
}

public sealed class NphiesBenefitItem
{
    public string? Category { get; init; }
    public string? ProductOrService { get; init; }
    public bool? Excluded { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Network { get; init; }
    public string? Unit { get; init; }
    public string? Term { get; init; }
    public bool? AuthorizationRequired { get; init; }
    public List<NphiesBenefit> Benefits { get; init; } = new();
}

public sealed class NphiesEligibilityInsurance
{
    public string? CoverageReference { get; init; }
    public bool? InForce { get; init; }
    public string? NotInForceReason { get; init; }
    public string? BenefitPeriodStart { get; init; }
    public string? BenefitPeriodEnd { get; init; }
    public List<NphiesBenefitItem> Items { get; init; } = new();
}

/// <summary>Result of an eligibility-response.</summary>
public sealed class NphiesEligibilityResult
{
    public string? Id { get; init; }
    public string? Identifier { get; init; }
    public string? RequestIdentifier { get; init; }
    public List<string> Purposes { get; init; } = new();
    public string? Outcome { get; init; }
    public string? Disposition { get; init; }
    /// <summary>siteEligibility extension: eligible, not-eligible, out-network, ...</summary>
    public string? SiteEligibility { get; init; }
    public string? PreAuthRef { get; init; }
    public List<NphiesEligibilityInsurance> Insurance { get; init; } = new();
    /// <summary>Coverages discovered (discovery purpose): Coverage resources included in the response bundle.</summary>
    public List<Coverage> Coverages { get; init; } = new();

    public bool IsEligible =>
        string.Equals(SiteEligibility, "eligible", StringComparison.OrdinalIgnoreCase)
        || (SiteEligibility == null && Insurance.Count > 0 && Insurance.All(i => i.InForce == true));
}

/// <summary>Result of status-response / cancel-response / poll-response Tasks.</summary>
public sealed class NphiesTaskResult
{
    public string? Id { get; init; }
    public string? Identifier { get; init; }
    /// <summary>task-code: status, cancel, nullify, poll, ...</summary>
    public string? Code { get; init; }
    /// <summary>Task.status: requested, completed, rejected, failed, ...</summary>
    public string? Status { get; init; }
    public string? StatusReason { get; init; }
    public string? FocusIdentifier { get; init; }
    public string? FocusIdentifierSystem { get; init; }
    /// <summary>For status-response: the status output value (e.g. queued, complete, pended, cancelled).</summary>
    public string? ProcessingStatus { get; init; }
    /// <summary>For poll-response: references of the response bundles in Task.output.</summary>
    public List<string> ResponseReferences { get; init; } = new();
}

public sealed class NphiesReconciliationDetail
{
    public string? Type { get; init; }
    /// <summary>Identifier of this payment detail; a later recoupment/adjustment names it as <see cref="Predecessor"/>.</summary>
    public string? Identifier { get; init; }
    public string? Predecessor { get; init; }
    public string? RequestIdentifier { get; init; }
    public string? ResponseIdentifier { get; init; }
    public string? Date { get; init; }
    public decimal? Amount { get; init; }
    public decimal? ComponentPayment { get; init; }
    public decimal? ComponentEarlyFee { get; init; }
    public decimal? ComponentNphiesFee { get; init; }
}

/// <summary>Result of a payment-reconciliation message.</summary>
public sealed class NphiesPaymentReconciliationResult
{
    public string? Id { get; init; }
    public string? Identifier { get; init; }
    public string? IdentifierSystem { get; init; }
    public string? Outcome { get; init; }
    public string? Disposition { get; init; }
    public string? PeriodStart { get; init; }
    public string? PeriodEnd { get; init; }
    public string? PaymentDate { get; init; }
    public NphiesAmount? PaymentAmount { get; init; }
    public string? PaymentIdentifier { get; init; }
    public string? PaymentMethod { get; init; }
    public List<NphiesReconciliationDetail> Details { get; init; } = new();
}

/// <summary>Result of a communication-request message.</summary>
public sealed class NphiesCommunicationRequestResult
{
    public string? Id { get; init; }
    public string? Identifier { get; init; }
    public string? IdentifierSystem { get; init; }
    public string? Status { get; init; }
    public string? Category { get; init; }
    public string? Priority { get; init; }
    public string? AboutClaimIdentifier { get; init; }
    public string? AboutClaimIdentifierSystem { get; init; }
    public List<string> PayloadText { get; init; } = new();
    public List<Attachment> PayloadAttachments { get; init; } = new();
    public string? Reason { get; init; }
}

/// <summary>Parsed NPHIES response message.</summary>
public sealed class NphiesResponse
{
    /// <summary>The parsed resource (normally a message Bundle, or an OperationOutcome).</summary>
    public Resource? Resource { get; init; }
    public Bundle? Bundle => Resource as Bundle;
    public string? BundleId { get; init; }
    public string? MessageId { get; init; }
    /// <summary>MessageHeader.eventCoding.code (e.g. claim-response).</summary>
    public string? EventCode { get; init; }
    /// <summary>MessageHeader.response.identifier: the id of the request MessageHeader this answers.</summary>
    public string? RequestMessageId { get; init; }
    /// <summary>MessageHeader.response.code: ok, transient-error, fatal-error.</summary>
    public string? ResponseCode { get; init; }
    /// <summary>Sender license (payer or NPHIES).</summary>
    public string? SenderLicense { get; init; }

    public List<NphiesError> Errors { get; init; } = new();

    public NphiesEligibilityResult? Eligibility { get; init; }
    public NphiesClaimAdjudicationResult? Adjudication { get; init; }
    public NphiesTaskResult? Task { get; init; }
    public NphiesPaymentReconciliationResult? PaymentReconciliation { get; init; }
    public NphiesCommunicationRequestResult? CommunicationRequest { get; init; }
    /// <summary>Messages carried inside a poll-response or batch-response, parsed recursively.</summary>
    public List<NphiesResponse> InnerMessages { get; init; } = new();

    public bool IsTransientError => string.Equals(ResponseCode, "transient-error", StringComparison.OrdinalIgnoreCase);
    public bool IsFatalError => string.Equals(ResponseCode, "fatal-error", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when NPHIES accepted the message (no fatal/transient error and no reported errors).</summary>
    public bool IsSuccess => !IsTransientError && !IsFatalError && Errors.Count == 0 && Resource is not OperationOutcome;
}
