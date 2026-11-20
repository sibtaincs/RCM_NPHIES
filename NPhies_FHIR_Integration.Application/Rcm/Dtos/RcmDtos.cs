namespace NPhies_FHIR_Integration.Application.Rcm.Dtos;

public sealed class RcmPagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class RcmQuery
{
    public string? ProviderId { get; set; }
    public string? InsurerId { get; set; }
    public string? PatientId { get; set; }
    public string? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    public int Skip => (Math.Max(1, Page) - 1) * Take;
    public int Take => Math.Clamp(PageSize, 1, 500);
}

// ===================== Eligibility =====================

public sealed class EligibilityCheckRequestDto
{
    public string PatientId { get; set; } = string.Empty;
    /// <summary>Required for benefits/validation; omitted for discovery.</summary>
    public string? CoverageId { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    /// <summary>Defaults to the coverage insurer.</summary>
    public string? InsurerId { get; set; }
    public string? LocationId { get; set; }
    /// <summary>benefits, validation, discovery.</summary>
    public List<string> Purposes { get; set; } = new() { "validation" };
    public DateTime? ServicedDate { get; set; }
    public DateTime? ServicedPeriodEnd { get; set; }
    /// <summary>The patient is a newborn using the mother's coverage (coverageId may then be the mother's).</summary>
    public bool IsNewborn { get; set; }
    /// <summary>Check by the receiving provider of a referral prior authorization (transfer of care).</summary>
    public bool IsTransfer { get; set; }
}

public sealed class EligibilityCheckDto
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? CoverageId { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string InsurerId { get; set; } = string.Empty;
    public List<string> Purposes { get; set; } = new();
    public bool IsNewborn { get; set; }
    public bool IsTransfer { get; set; }
    public DateTime ServicedDate { get; set; }
    public string RequestIdentifier { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Outcome { get; set; }
    public bool? IsEligible { get; set; }
    public bool? InForce { get; set; }
    public string? SiteEligibility { get; set; }
    public string? Disposition { get; set; }
    public string? NotInForceReason { get; set; }
    public DateTime? BenefitPeriodStart { get; set; }
    public DateTime? BenefitPeriodEnd { get; set; }
    public string? ResponseIdentifier { get; set; }
    /// <summary>JSON array: insurance[] with items[] and benefits[].</summary>
    public object? Benefits { get; set; }
    /// <summary>JSON array of coverages returned by a discovery request.</summary>
    public object? DiscoveredCoverages { get; set; }
    public object? Errors { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    /// <summary>What <see cref="SiteEligibility"/> means for the visit, in plain language.</summary>
    public string? SiteEligibilityReason { get; set; }
    /// <summary>The errors with advice on what to do, for the payer codes that have any (1658 to 1682).</summary>
    public List<EligibilityErrorDto> ErrorExplanations { get; set; } = new();
    /// <summary>Confirmed on a payer portal or by phone; nothing was sent to NPHIES.</summary>
    public bool IsOffline { get; set; }
    public string? OfflineReference { get; set; }
    public DateTime? OfflineDate { get; set; }
}

public sealed class EligibilityErrorDto
{
    public string? Code { get; set; }
    public string? Message { get; set; }
    /// <summary>What to do about it; null when the code has no specific advice.</summary>
    public string? Explanation { get; set; }
}

/// <summary>An eligibility confirmation obtained outside NPHIES (payer portal or phone).</summary>
public sealed class OfflineEligibilityRequestDto
{
    public string PatientId { get; set; } = string.Empty;
    /// <summary>Defaults to the patient's active coverage.</summary>
    public string? CoverageId { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    /// <summary>Defaults to the coverage insurer.</summary>
    public string? InsurerId { get; set; }
    /// <summary>The reference the payer gave (portal reference or call reference).</summary>
    public string Reference { get; set; } = string.Empty;
    /// <summary>When the payer confirmed eligibility.</summary>
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}

// ===================== Insurance discovery =====================

public sealed class InsuranceDiscoveryRequestDto
{
    /// <summary>The patient on file; the patient key defaults to their identity number.</summary>
    public string? PatientId { get; set; }
    /// <summary>National id, Iqama, passport, visa or border number. Required when there is no patientId.</summary>
    public string? PatientKey { get; set; }
    /// <summary>1 nationals and residents (default), 2 visitors, 3 tourists, 4 premium residency, 5 Hajj and Umrah.</summary>
    public int SystemType { get; set; } = 1;
}

public sealed class InsuranceDiscoveryDto
{
    public string? PatientId { get; set; }
    public string PatientKey { get; set; } = string.Empty;
    public int SystemType { get; set; }
    public bool Found { get; set; }
    /// <summary>Check Insurance error code (105 = no record found), or timeout / transport / http.</summary>
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>The sandbox answered, not NPHIES.</summary>
    public bool Simulated { get; set; }
    public List<DiscoveredPolicyDto> Policies { get; set; } = new();
}

public sealed class DiscoveredPolicyDto
{
    public string? PolicyNumber { get; set; }
    public string? InsuranceCompanyEn { get; set; }
    public string? InsuranceCompanyAr { get; set; }
    public string? InsuranceCompanyId { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool Expired { get; set; }
    public string? ClassName { get; set; }
    public string? NetworkId { get; set; }
    public decimal? DeductibleRate { get; set; }
    public decimal? MaxLimit { get; set; }
    /// <summary>main | dependent</summary>
    public string? BeneficiaryType { get; set; }
    public string? BeneficiaryNumber { get; set; }
    public string? SponsorNumber { get; set; }
    public string? PolicyHolder { get; set; }
    public string? InsuranceStatus { get; set; }
    public string? InsuranceType { get; set; }
    public string? Name { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public DateTime? DateOfBirth { get; set; }
    /// <summary>The payer on file that matches the insurance company, when there is one.</summary>
    public string? MatchedInsurerId { get; set; }
    public string? MatchedInsurerName { get; set; }
    /// <summary>The patient's coverage already on file for this policy, when there is one.</summary>
    public string? ExistingCoverageId { get; set; }
}

public sealed class AddDiscoveredCoverageDto
{
    public string PatientId { get; set; } = string.Empty;
    public string? PatientKey { get; set; }
    public int SystemType { get; set; } = 1;
    public string PolicyNumber { get; set; } = string.Empty;
    /// <summary>
    /// For a dependent: how the patient is related to the policy holder (child, spouse, parent, common, injured, other).
    /// A child on the mother's or father's policy is <c>child</c>. Ignored for a main beneficiary, who is <c>self</c>.
    /// </summary>
    public string? Relationship { get; set; }
    /// <summary>For a dependent: the policy holder (for example the mother or father) when they are a patient on file.</summary>
    public string? SubscriberPatientId { get; set; }
    /// <summary>The payer to use when none on file matches the insurance company automatically.</summary>
    public string? InsurerId { get; set; }
}

public sealed class DiscoveredCoverageDto
{
    public string CoverageId { get; set; } = string.Empty;
    public string InsurerId { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    /// <summary>False when the coverage was already on file.</summary>
    public bool Created { get; set; }
}

// ===================== Claims / prior authorizations =====================

public sealed class SubmissionResultDto
{
    public string ClaimId { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string Use { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? MessageId { get; set; }
    /// <summary>Transaction status: Completed, Queued, Error, ValidationFailed, Timeout.</summary>
    public string TransactionStatus { get; set; } = string.Empty;
    public string? ClaimStatus { get; set; }
    public string? Outcome { get; set; }
    public string? AdjudicationOutcome { get; set; }
    public List<string> Errors { get; set; } = new();
    public ClaimAdjudicationDto? Adjudication { get; set; }
}

public sealed class ClaimAdjudicationDto
{
    public string ClaimResponseId { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public string? ResponseIdentifier { get; set; }
    public string? Outcome { get; set; }
    public string? AdjudicationOutcome { get; set; }
    public string? Disposition { get; set; }
    /// <summary>Why the payer reissued this response (reissue-reason), e.g. <c>adjudication</c> after a re-adjudication appeal.</summary>
    public string? ReissueReason { get; set; }
    public string? PreAuthRef { get; set; }
    public DateTime? PreAuthPeriodStart { get; set; }
    public DateTime? PreAuthPeriodEnd { get; set; }
    public decimal? TotalSubmitted { get; set; }
    public decimal? TotalEligible { get; set; }
    public decimal? TotalBenefit { get; set; }
    public decimal? TotalCopay { get; set; }
    public decimal? TotalTax { get; set; }
    public decimal? TotalPatientShare { get; set; }
    public decimal? PaymentAmount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public List<ItemAdjudicationDto> Items { get; set; } = new();
}

public sealed class ItemAdjudicationDto
{
    public int Sequence { get; set; }
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
}

public sealed class DerivedClaimRequestDto
{
    /// <summary>Submit the derived claim immediately (default) or leave it as a draft for editing.</summary>
    public bool Submit { get; set; } = true;
    public string? Reason { get; set; }
    /// <summary>New billable / authorization period end (extensions).</summary>
    public DateTime? NewPeriodEnd { get; set; }
    /// <summary>
    /// Prior authorization update or extension: item sequences to leave out of the new version. Allowed only once the
    /// payer has adjudicated; a pending request is cancelled and sent again instead.
    /// </summary>
    public List<int> RemoveItemSequences { get; set; } = new();
    /// <summary>
    /// Prior authorization update or extension: items that were only partly done. Each keeps the quantity done and a new
    /// item is added for the rest.
    /// </summary>
    public List<SplitItemDto> SplitItems { get; set; } = new();
}

public sealed class SplitItemDto
{
    public int Sequence { get; set; }
    /// <summary>The quantity already done: more than 0 and less than the item's quantity.</summary>
    public decimal DoneQuantity { get; set; }
    /// <summary>When the rest is planned for, if known.</summary>
    public DateTime? RemainderServicedDate { get; set; }
}

public sealed class LinkPreAuthDto
{
    /// <summary>Id of an approved prior-authorization claim in this system.</summary>
    public string? PreAuthClaimId { get; set; }
    /// <summary>Or a raw preAuthRef issued by the payer (offline authorization).</summary>
    public string? PreAuthRef { get; set; }
}

public sealed class BatchSubmitRequestDto
{
    public List<string> ClaimIds { get; set; } = new();
    public string? BatchIdentifier { get; set; }
}

public sealed class BatchSubmitResultDto
{
    public string BatchIdentifier { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? MessageId { get; set; }
    public string TransactionStatus { get; set; } = string.Empty;
    public int ClaimCount { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<BatchClaimDto> Claims { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public sealed class BatchClaimDto
{
    public string ClaimId { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public int? BatchNumber { get; set; }
    public string? ClaimStatus { get; set; }
    public string? Outcome { get; set; }
    public decimal Total { get; set; }
    public decimal? ApprovedAmount { get; set; }
}

public sealed class CancelClaimRequestDto
{
    /// <summary>NPHIES task-reason code (e.g. WI = wrong information, NP = service not performed, TAS = transaction already submitted).</summary>
    public string Reason { get; set; } = "WI";
    /// <summary>Nullify (entered-in-error) instead of cancel.</summary>
    public bool Nullify { get; set; }
}

public sealed class TaskResultDto
{
    public string ClaimId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? MessageId { get; set; }
    public string TransactionStatus { get; set; } = string.Empty;
    public string? TaskStatus { get; set; }
    public string? ProcessingStatus { get; set; }
    public string? ClaimStatus { get; set; }
    public List<string> Errors { get; set; } = new();
}

public sealed class RcmClaimQuery : RcmQuery
{
    /// <summary>claim | preauthorization</summary>
    public string? Use { get; set; }
    public string? ClaimType { get; set; }
    public string? NphiesStatus { get; set; }
    public string? Search { get; set; }
}

public sealed class RcmClaimSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string? Identifier { get; set; }
    public string Use { get; set; } = string.Empty;
    public string ClaimType { get; set; } = string.Empty;
    public string? ClaimSubType { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? PatientNameArabic { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string InsurerId { get; set; } = string.Empty;
    public string? InsurerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? NphiesStatus { get; set; }
    public string? Outcome { get; set; }
    public string? AdjudicationOutcome { get; set; }
    public decimal Total { get; set; }
    public decimal? ApprovedAmount { get; set; }
    /// <summary>VAT approved by the payer (ClaimResponse.total[tax]); ApprovedAmount excludes it.</summary>
    public decimal? ApprovedTax { get; set; }
    /// <summary>What the payer should remit: approved benefit + approved VAT.</summary>
    public decimal? ExpectedPayment { get; set; }
    public decimal? PatientShareAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    /// <summary>ExpectedPayment − PaidAmount once paid (positive = underpaid).</summary>
    public decimal? PaymentVariance { get; set; }
    public string? PreAuthRef { get; set; }
    public DateTime? PreAuthPeriodEnd { get; set; }
    public bool IsShadowBilling { get; set; }
    public string? BatchIdentifier { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? AdjudicatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    /// <summary>Attachments (stored documents + legacy inline) on the claim; filled by the list endpoint.</summary>
    public int? DocumentCount { get; set; }
}

public sealed class ClaimTimelineDto
{
    public RcmClaimSummaryDto Claim { get; set; } = new();
    public List<TransactionDto> Transactions { get; set; } = new();
    public List<ClaimAdjudicationDto> Adjudications { get; set; } = new();
    public List<DenialDto> Denials { get; set; } = new();
    public List<CommunicationRequestDto> CommunicationRequests { get; set; } = new();
    public List<CommunicationDto> Communications { get; set; } = new();
    public List<PaymentDetailDto> Payments { get; set; } = new();
    public List<RelatedClaimDto> Related { get; set; } = new();
}

public sealed class RelatedClaimDto
{
    public string? ClaimId { get; set; }
    public string? Identifier { get; set; }
    public string? Relationship { get; set; }
}

public sealed class TransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string EventCode { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ResponseCode { get; set; }
    public string? Outcome { get; set; }
    public string? ErrorSummary { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    /// <summary>NphiesValidation | PayerBusinessError | Rejection; null when the exchange succeeded.</summary>
    public string? ErrorKind { get; set; }
    public string? AdjudicationOutcome { get; set; }
    public List<string> MetaTags { get; set; } = new();
    public bool NphiesGenerated { get; set; }
    public int ResendCount { get; set; }
    public DateTime? LastResentAt { get; set; }
}

/// <summary>Result of sending the identical message of a transaction again.</summary>
public sealed class ResendResultDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string? BundleId { get; set; }
    public int ResendCount { get; set; }
    public string TransactionStatus { get; set; } = string.Empty;
    public string? ErrorKind { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    /// <summary>The claim or prior authorization status after the resend, when the transaction belongs to one.</summary>
    public string? ClaimStatus { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>Raw FHIR JSON exchanged with NPHIES for one transaction (from the NphiesMessageLogs audit).</summary>
public sealed class MessageJsonDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string EventCode { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string? ResponseEventCode { get; set; }
    public int? HttpStatus { get; set; }
    public string? RequestJson { get; set; }
    public string? ResponseJson { get; set; }
    /// <summary>Where the JSON came from: request | sync-response | poll-response.</summary>
    public string Source { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTime? LoggedAt { get; set; }
}

/// <summary>The request bundle a draft claim / prior-auth would send, built without dispatching.</summary>
public sealed class BundlePreviewDto
{
    public string ClaimId { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
    /// <summary>Structured local validation findings (business rules + NPHIES IG profile conformance).</summary>
    public List<BundleValidationIssueDto> Issues { get; set; } = new();
}

/// <summary>One local validation finding. RuleId is a BV-/PRF- rule (PRF-* come from the NPHIES IG profiles).</summary>
public sealed class BundleValidationIssueDto
{
    /// <summary>Error | Warning | Information.</summary>
    public string Severity { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    /// <summary>FHIR location in the bundle, e.g. Bundle.entry[1].resource[0].insurance.</summary>
    public string? Expression { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Result of validating an arbitrary NPHIES request bundle.</summary>
public sealed class BundleValidationResultDto
{
    public bool Valid => !Issues.Any(i => i.Severity == "Error");
    public string ProfileMode { get; set; } = string.Empty;
    public List<BundleValidationIssueDto> Issues { get; set; } = new();
}

/// <summary>The NPHIES IG package used for local profile validation.</summary>
public sealed class ConformancePackageInfoDto
{
    public string PackageName { get; set; } = string.Empty;
    public string PackageVersion { get; set; } = string.Empty;
    public string? PackageDate { get; set; }
    public int Profiles { get; set; }
    public int ValueSets { get; set; }
    public int CodeSystems { get; set; }
    public string ProfileMode { get; set; } = string.Empty;
    public List<string> LoadErrors { get; set; } = new();
}

// ===================== Poll =====================

/// <summary>
/// Filters for a poll request (IG, polling use case). With no filter NPHIES returns any pended messages up to the
/// configured count. Include and exclude message types are mutually exclusive.
/// </summary>
public sealed class PollRequestDto
{
    /// <summary>Provider organization to poll for; default: every provider that has sent messages.</summary>
    public string? ProviderId { get; set; }
    /// <summary>Maximum messages per poll request, 1 to 100. Default: <c>Nphies:PollMaxCount</c>.</summary>
    public int? Count { get; set; }
    /// <summary>Message event codes to include, for example <c>claim-response</c> or <c>communication-request</c>.</summary>
    public List<string> IncludeMessageTypes { get; set; } = new();
    /// <summary>Message event codes to exclude, for example <c>payment-reconciliation</c>.</summary>
    public List<string> ExcludeMessageTypes { get; set; } = new();
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    /// <summary>Fetch the deferred response to this claim or prior authorization only (sent as <c>Task.focus.identifier</c>).</summary>
    public string? FocusClaimId { get; set; }
}

public sealed class PollRunResultDto
{
    public int PollRequests { get; set; }
    public int MessagesReceived { get; set; }
    public int Processed { get; set; }
    public int Duplicates { get; set; }
    public int Unmatched { get; set; }
    public int PaymentNoticesSent { get; set; }
    public List<string> ClaimIds { get; set; } = new();
    public List<string> PaymentReconciliationIds { get; set; } = new();
    public List<string> CommunicationRequestIds { get; set; } = new();
    public List<string> AdvancedAuthorizationIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

// ===================== Stuck requests =====================

/// <summary>A claim or prior authorization with no final response for longer than <c>Rcm:StuckAfterDays</c>.</summary>
public sealed class StuckRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public string? ClaimNumber { get; set; }
    public string? ProviderId { get; set; }
    public string? InsurerId { get; set; }
    public string? Use { get; set; }
    public string ClaimStatus { get; set; } = string.Empty;
    public DateTime WaitingSince { get; set; }
    public int DaysWaiting { get; set; }
    public string? TransactionId { get; set; }
    /// <summary>Bundle.id of the request, to quote to NPHIES support.</summary>
    public string? BundleId { get; set; }
    public string? MessageId { get; set; }
    public string? RequestIdentifier { get; set; }
    public bool HeldByNphies { get; set; }
    /// <summary>Flagged | Escalated | Resolved</summary>
    public string Level { get; set; } = string.Empty;
    public DateTime FlaggedAt { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    /// <summary>What to do next.</summary>
    public string? Guidance { get; set; }
}

public sealed class StuckScanResultDto
{
    /// <summary>Newly flagged in this scan.</summary>
    public int Flagged { get; set; }
    /// <summary>Newly escalated in this scan.</summary>
    public int Escalated { get; set; }
    /// <summary>Closed in this scan because a final response arrived.</summary>
    public int Resolved { get; set; }
    /// <summary>Requests still waiting after the scan.</summary>
    public int Open { get; set; }
}

// ===================== Communications =====================

public sealed class AttachmentDto
{
    public string ContentType { get; set; } = "application/pdf";
    public string? Title { get; set; }
    /// <summary>Base64 content.</summary>
    public string? Data { get; set; }
    public string? Url { get; set; }
    public DateTime? Creation { get; set; }
    /// <summary>The claim item (Claim.item.sequence) the document supports; sent as <c>extension-ClaimItemSequence</c>.</summary>
    public int? ItemSequence { get; set; }
}

/// <summary>A text payload about one claim item (Communication.payload with <c>extension-ClaimItemSequence</c>).</summary>
public sealed class CommunicationItemNoteDto
{
    public int Sequence { get; set; }
    public string Text { get; set; } = string.Empty;
}

public sealed class SendCommunicationDto
{
    /// <summary>
    /// An advanced authorization to write about instead of a claim, for example to ask the payer to change or extend it.
    /// </summary>
    public string? AdvancedAuthorizationId { get; set; }
    /// <summary>Claim (or prior auth) the communication is about. Required for unsolicited communications.</summary>
    public string? ClaimId { get; set; }
    /// <summary>Texts about the whole claim.</summary>
    public List<string> Texts { get; set; } = new();
    /// <summary>Texts about single claim items; each must be a sequence of the claim's items.</summary>
    public List<CommunicationItemNoteDto> Items { get; set; } = new();
    public List<AttachmentDto> Attachments { get; set; } = new();
    public string? Category { get; set; }
    public string Priority { get; set; } = "routine";
    /// <summary>
    /// communication-reason code: <c>info-correct</c>, <c>missing-attach</c>, <c>missing-info</c> or
    /// <c>re-adjudication</c> (ask the payer to adjudicate the named items of a claim again; needs at least one item).
    /// </summary>
    public string? ReasonCode { get; set; }
}

public sealed class CommunicationRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string? Identifier { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string? ClaimId { get; set; }
    public string? ClaimIdentifier { get; set; }
    public string? PayloadText { get; set; }
    public object? Payloads { get; set; }
    public string? Reason { get; set; }
    /// <summary>Claim items the payer's request refers to (<c>extension-ClaimItemSequence</c>).</summary>
    public List<int> ItemSequences { get; set; } = new();
    public DateTime? ReceivedAt { get; set; }
    public string? RespondedCommunicationId { get; set; }
    public DateTime? RespondedAt { get; set; }
    /// <summary>
    /// A <c>notification</c> only informs the provider, for example why a re-adjudication appeal was not accepted;
    /// it needs no reply.
    /// </summary>
    public bool RequiresResponse => !string.Equals(Category, "notification", StringComparison.OrdinalIgnoreCase);
    public bool IsOpen => RespondedAt is null && RequiresResponse && string.Equals(Status, "active", StringComparison.OrdinalIgnoreCase);
}

public sealed class CommunicationDto
{
    public string Id { get; set; } = string.Empty;
    public string? Identifier { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string? ClaimId { get; set; }
    public string? CommunicationRequestId { get; set; }
    public string? PayloadText { get; set; }
    public object? Payloads { get; set; }
    public string? ReasonCode { get; set; }
    /// <summary>Claim items the payloads refer to (<c>extension-ClaimItemSequence</c>).</summary>
    public List<int> ItemSequences { get; set; } = new();
    public string ProcessingStatus { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TransactionStatus { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ===================== Payments =====================

public sealed class PaymentReconciliationDto
{
    public string Id { get; set; } = string.Empty;
    public string PaymentReconciliationId { get; set; } = string.Empty;
    public string? Identifier { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Outcome { get; set; }
    public string? Disposition { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? PaymentDate { get; set; }
    public decimal PaymentAmount { get; set; }
    public string PaymentCurrency { get; set; } = "SAR";
    public string? PaymentIdentifier { get; set; }
    public string? PayerId { get; set; }
    public string? PayerName { get; set; }
    public string MatchStatus { get; set; } = string.Empty;
    public decimal? ExpectedAmount { get; set; }
    public decimal? VarianceAmount { get; set; }
    /// <summary>Sum of negative adjustment lines (money the payer took back from earlier payments).</summary>
    public decimal TotalRecouped { get; set; }
    public decimal TotalNphiesFee { get; set; }
    public decimal TotalEarlyFee { get; set; }
    public string? AcknowledgementStatus { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgementError { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public List<PaymentDetailDto> Details { get; set; } = new();
}

public sealed class PaymentDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string PaymentReconciliationId { get; set; } = string.Empty;
    public string DetailType { get; set; } = string.Empty;
    public string? DetailIdentifier { get; set; }
    public string? PredecessorIdentifier { get; set; }
    public string? RequestIdentifier { get; set; }
    public string? ResponseIdentifier { get; set; }
    public DateTime? Date { get; set; }
    public decimal Amount { get; set; }
    public decimal? ComponentPayment { get; set; }
    public decimal? EarlyFee { get; set; }
    public decimal? NphiesFee { get; set; }
    public string? MatchedClaimId { get; set; }
    public string? ClaimNumber { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? VarianceAmount { get; set; }
    public string MatchStatus { get; set; } = string.Empty;
}

public sealed class ManualMatchDto
{
    public string ClaimId { get; set; } = string.Empty;
}

// ===================== Denials / appeals =====================

public sealed class DenialQuery : RcmQuery
{
    public string? Category { get; set; }
    public string? ReasonCode { get; set; }
    public string? ClaimId { get; set; }
}

public sealed class DenialDto
{
    public string Id { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public string? ClaimNumber { get; set; }
    public string? ClaimResponseId { get; set; }
    public int? ItemSequence { get; set; }
    public string? ItemCode { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? ReasonDisplay { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal SubmittedAmount { get; set; }
    public decimal DeniedAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DenialDate { get; set; }
    public DateTime AppealDeadline { get; set; }
    public int DaysToDeadline { get; set; }
    public string? AppealRequestId { get; set; }
    public string? ResubmissionClaimId { get; set; }
    public string? ProviderId { get; set; }
    public string? InsurerId { get; set; }
    public string? InsurerName { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateDenialDto
{
    /// <summary>Open, WrittenOff, Resubmitted, Overturned, Upheld.</summary>
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class DenialSummaryDto
{
    public int TotalDenials { get; set; }
    public int OpenDenials { get; set; }
    public decimal TotalDeniedAmount { get; set; }
    public decimal OpenDeniedAmount { get; set; }
    public decimal RecoveredAmount { get; set; }
    public int ExpiringIn7Days { get; set; }
    public List<BreakdownDto> ByCategory { get; set; } = new();
    public List<BreakdownDto> ByReason { get; set; } = new();
    public List<BreakdownDto> ByPayer { get; set; } = new();
    public List<BreakdownDto> ByStatus { get; set; } = new();
}

public sealed class BreakdownDto
{
    public string Key { get; set; } = string.Empty;
    public string? Label { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public sealed class CreateAppealDto
{
    public string ClaimId { get; set; } = string.Empty;
    /// <summary>Denials being appealed; defaults to every open denial on the claim.</summary>
    public List<string> DenialIds { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    /// <summary>Justification text and documents sent to the payer as a communication.</summary>
    public List<string> Texts { get; set; } = new();
    /// <summary>
    /// Re-adjudication only: a justification per appealed service. Services without one get the first of
    /// <see cref="Texts"/>, else <see cref="Reason"/>.
    /// </summary>
    public List<CommunicationItemNoteDto> Items { get; set; } = new();
    public List<AttachmentDto> Attachments { get; set; } = new();
    /// <summary>
    /// true (default): resubmit the claim as a new claim (related = prior) with all its services.
    /// false: appeal inside the same claim with a re-adjudication communication naming the denied services; the payer
    /// answers with a reissued ClaimResponse or a notification.
    /// </summary>
    public bool Resubmit { get; set; } = true;
    public string? CreatedBy { get; set; }
}

public sealed class AppealDto
{
    public string Id { get; set; } = string.Empty;
    public string AppealNumber { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public string? ClaimNumber { get; set; }
    public string? ResubmissionClaimId { get; set; }
    public string? CommunicationId { get; set; }
    /// <summary>Communication (re-adjudication in the same claim) or Resubmission (new claim, related = prior).</summary>
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Outcome { get; set; }
    public int Level { get; set; }
    public string ReasonCodes { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal AppealedAmount { get; set; }
    /// <summary>The part of the appealed amount the payer no longer denies.</summary>
    public decimal RecoveredAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public DateTime DenialDate { get; set; }
    public DateTime DeadlineDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime LastStatusUpdate { get; set; }
    public string? DecisionExplanation { get; set; }
    public List<string> DenialIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

// ===================== Dashboard / reports =====================

public sealed class RcmDashboardDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int ClaimsSubmitted { get; set; }
    public int ClaimsAdjudicated { get; set; }
    public int ClaimsPending { get; set; }
    public int ClaimsApproved { get; set; }
    public int ClaimsPartiallyApproved { get; set; }
    public int ClaimsRejected { get; set; }
    public int ClaimsErrored { get; set; }
    public int PreAuthsSubmitted { get; set; }
    public int PreAuthsApproved { get; set; }
    public decimal BilledAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal PatientShareAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal DeniedAmount { get; set; }
    public decimal DenialRate { get; set; }
    public decimal CleanClaimRate { get; set; }
    public decimal CollectionRate { get; set; }
    public double? AverageDaysToAdjudication { get; set; }
    public double? AverageDaysToPayment { get; set; }
    public int OpenDenials { get; set; }
    public int OpenCommunicationRequests { get; set; }
    public int UnacknowledgedPayments { get; set; }
    public int PaymentDiscrepancies { get; set; }
    public int EligibilityChecks { get; set; }
    public decimal EligibilityRate { get; set; }
}

public sealed class AgingBucketDto
{
    public string Bucket { get; set; } = string.Empty;
    public int MinDays { get; set; }
    public int? MaxDays { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public sealed class DenialRateDto
{
    public string Period { get; set; } = string.Empty;
    public int Adjudicated { get; set; }
    public int Denied { get; set; }
    public int PartiallyDenied { get; set; }
    public decimal BilledAmount { get; set; }
    public decimal DeniedAmount { get; set; }
    public decimal DenialRate { get; set; }
    public decimal AmountDenialRate { get; set; }
}

public sealed class PayerBreakdownDto
{
    public string InsurerId { get; set; } = string.Empty;
    public string? InsurerName { get; set; }
    public int Claims { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Pending { get; set; }
    public decimal BilledAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal DenialRate { get; set; }
    public double? AverageDaysToPayment { get; set; }
}

// ===================== Advanced (insurer-initiated) authorizations =====================

public sealed class AdvancedAuthorizationDto
{
    public string Id { get; set; } = string.Empty;
    public string? Identifier { get; set; }
    public string? PreAuthRef { get; set; }
    /// <summary>Why the payer sent it again (cancel, correction, adjudication...); null for a first issue.</summary>
    public string? ReissueReason { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
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
    public string? ServiceProviderId { get; set; }
    public string? ServiceProviderLicense { get; set; }
    public string? ReferringProviderLicense { get; set; }
    public decimal? TotalEligible { get; set; }
    public decimal? TotalBenefit { get; set; }
    public string Currency { get; set; } = "SAR";
    /// <summary>Active | Used | Expired | Cancelled</summary>
    public string Status { get; set; } = string.Empty;
    public string? UsedByClaimId { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime ReceivedAt { get; set; }
    public System.Text.Json.JsonElement? Items { get; set; }
    public System.Text.Json.JsonElement? Diagnoses { get; set; }
}

public sealed class UseAdvancedAuthorizationDto
{
    /// <summary>Unsent claim that will quote this authorization's preAuthRef.</summary>
    public string ClaimId { get; set; } = string.Empty;
}

// ===================== Live-test readiness =====================

public sealed class ConnectivityReportDto
{
    /// <summary>Pass | Warn | Fail (worst of the individual checks).</summary>
    public string Status { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public bool SandboxSimulator { get; set; }
    public string ProfileValidationMode { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public ConnectivityCertificateDto? Certificate { get; set; }
    public List<ConnectivityCheckDto> Checks { get; set; } = new();
    /// <summary>Result of the optional real poll-request (messages are ingested as usual).</summary>
    public PollRunResultDto? Poll { get; set; }
}

public sealed class ConnectivityCheckDto
{
    public const string Pass = "Pass";
    public const string Warn = "Warn";
    public const string Fail = "Fail";
    public const string Skipped = "Skipped";

    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public long? DurationMs { get; set; }
}

public sealed class ConnectivityCertificateDto
{
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Thumbprint { get; set; } = string.Empty;
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
    public int DaysToExpiry { get; set; }
    public bool HasPrivateKey { get; set; }
}

public sealed class EvidenceExportRequestDto
{
    public List<string> ClaimIds { get; set; } = new();
}