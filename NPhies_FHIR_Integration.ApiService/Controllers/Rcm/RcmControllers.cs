using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers.Rcm;

/// <summary>NPHIES eligibility (CoverageEligibilityRequest: benefits / validation / discovery).</summary>
[Route("api/rcm/eligibility")]
public sealed class RcmEligibilityController : RcmControllerBase
{
    private readonly IRcmEligibilityService _service;
    public RcmEligibilityController(IRcmEligibilityService service) => _service = service;

    [HttpPost]
    [HasPermission(Permissions.EligibilityCheck)]
    public async Task<ActionResult<EligibilityCheckDto>> Check([FromBody] EligibilityCheckRequestDto request, CancellationToken ct)
        => Ok(await _service.CheckAsync(request, ct));

    [HttpGet]
    [HasPermission(Permissions.EligibilityRead)]
    public async Task<ActionResult<RcmPagedResult<EligibilityCheckDto>>> List([FromQuery] RcmQuery query, CancellationToken ct)
        => Ok(await _service.ListAsync(Scope(query), ct));

    [HttpGet("{id}")]
    [HasPermission(Permissions.EligibilityRead)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
        => await _service.GetAsync(id, ct) is { } dto ? Ok(dto) : NotFoundResult("Eligibility check", id);

    [HttpGet("patients/{patientId}/latest")]
    [HasPermission(Permissions.EligibilityRead)]
    public async Task<IActionResult> Latest(string patientId, [FromQuery] string? insurerId, CancellationToken ct)
        => await _service.GetLatestForPatientAsync(patientId, insurerId, ct) is { } dto ? Ok(dto) : NotFoundResult("Eligibility check for patient", patientId);
}

/// <summary>
/// Claim and prior-authorization lifecycle: submit, update/extend, convert to claim, resubmit,
/// status-check, cancel/nullify, timeline and adjudication.
/// </summary>
[Route("api/rcm/claims")]
public sealed class RcmClaimsController : RcmControllerBase
{
    private readonly IRcmClaimWorkflowService _service;
    public RcmClaimsController(IRcmClaimWorkflowService service) => _service = service;

    [HttpGet]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<RcmPagedResult<RcmClaimSummaryDto>>> List([FromQuery] RcmClaimQuery query, CancellationToken ct)
        => Ok(await _service.ListAsync(Scope(query), ct));

    [HttpPost("{id}/submit")]
    [HasPermission(Permissions.ClaimsSubmit)]
    public async Task<ActionResult<SubmissionResultDto>> Submit(string id, CancellationToken ct)
        => Ok(await _service.SubmitAsync(id, ct));

    [HttpPost("{id}/resubmit")]
    [HasPermission(Permissions.ClaimsSubmit)]
    public async Task<ActionResult<SubmissionResultDto>> Resubmit(string id, [FromBody] DerivedClaimRequestDto? request, CancellationToken ct)
        => Ok(await _service.ResubmitClaimAsync(id, request ?? new DerivedClaimRequestDto(), ct));

    [HttpPost("{id}/link-preauth")]
    [HasPermission(Permissions.ClaimsUpdate)]
    public async Task<ActionResult<RcmClaimSummaryDto>> LinkPreAuth(string id, [FromBody] LinkPreAuthDto request, CancellationToken ct)
        => Ok(await _service.LinkPreAuthAsync(id, request, ct));

    [HttpPost("{id}/status-check")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<TaskResultDto>> StatusCheck(string id, CancellationToken ct)
        => Ok(await _service.StatusCheckAsync(id, ct));

    /// <summary>Cancel (before adjudication) or nullify (after) a claim or prior authorization.</summary>
    [HttpPost("{id}/cancel")]
    [HasPermission(Permissions.ClaimsSubmit)]
    public async Task<ActionResult<TaskResultDto>> Cancel(string id, [FromBody] CancelClaimRequestDto request, CancellationToken ct)
        => Ok(await _service.CancelAsync(id, request, ct));

    [HttpGet("{id}/timeline")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<IActionResult> Timeline(string id, CancellationToken ct)
        => await _service.GetTimelineAsync(id, ct) is { } dto ? Ok(dto) : NotFoundResult("Claim", id);

    [HttpGet("{id}/adjudication")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<IActionResult> Adjudication(string id, CancellationToken ct)
        => await _service.GetLatestAdjudicationAsync(id, ct) is { } dto ? Ok(dto) : NotFoundResult("Adjudication for claim", id);

    /// <summary>Builds the NPHIES request bundle for the claim / prior-auth without sending it (plus local validation findings).</summary>
    [HttpGet("{id}/preview-bundle")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<BundlePreviewDto>> PreviewBundle(string id, CancellationToken ct)
        => Ok(await _service.PreviewBundleAsync(id, ct));

    /// <summary>NPHIES test-case evidence for one claim / prior-auth: zip of every exchanged bundle plus timeline and summary.</summary>
    [HttpGet("{id}/evidence")]
    [HasPermission(Permissions.ClaimsRead)]
    [Produces("application/zip")]
    public async Task<IActionResult> Evidence(string id, [FromServices] IRcmEvidenceService evidence, CancellationToken ct)
    {
        var file = await evidence.BuildAsync(new[] { id }, ProviderScope(), ct);
        return File(file.Content, "application/zip", file.FileName);
    }

    /// <summary>Raw request/response FHIR JSON of one NPHIES transaction (id from the timeline's transactions).</summary>
    [HttpGet("transactions/{transactionId}/json")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<IActionResult> TransactionJson(string transactionId, CancellationToken ct)
        => await _service.GetTransactionJsonAsync(transactionId, Scope(new RcmQuery()).ProviderId, ct) is { } dto ? Ok(dto) : NotFoundResult("Transaction", transactionId);
}

/// <summary>Prior-authorization specific transitions (the submit/status/cancel routes are shared with claims).</summary>
[Route("api/rcm/preauths")]
public sealed class RcmPreAuthController : RcmControllerBase
{
    private readonly IRcmClaimWorkflowService _service;
    public RcmPreAuthController(IRcmClaimWorkflowService service) => _service = service;

    [HttpGet]
    [HasPermission(Permissions.PreAuthRead)]
    public async Task<ActionResult<RcmPagedResult<RcmClaimSummaryDto>>> List([FromQuery] RcmClaimQuery query, CancellationToken ct)
    {
        query.Use = "preauthorization";
        return Ok(await _service.ListAsync(Scope(query), ct));
    }

    [HttpPost("{id}/submit")]
    [HasPermission(Permissions.PreAuthSubmit)]
    public async Task<ActionResult<SubmissionResultDto>> Submit(string id, CancellationToken ct)
        => Ok(await _service.SubmitAsync(id, ct));

    [HttpPost("{id}/update")]
    [HasPermission(Permissions.PreAuthSubmit)]
    public async Task<ActionResult<SubmissionResultDto>> Update(string id, [FromBody] DerivedClaimRequestDto? request, CancellationToken ct)
        => Ok(await _service.UpdatePreAuthAsync(id, request ?? new DerivedClaimRequestDto(), ct));

    [HttpPost("{id}/extend")]
    [HasPermission(Permissions.PreAuthSubmit)]
    public async Task<ActionResult<SubmissionResultDto>> Extend(string id, [FromBody] DerivedClaimRequestDto? request, CancellationToken ct)
        => Ok(await _service.ExtendPreAuthAsync(id, request ?? new DerivedClaimRequestDto(), ct));

    /// <summary>Creates a claim from an approved prior authorization (carries the preAuthRef).</summary>
    [HttpPost("{id}/claim")]
    [HasPermission(Permissions.ClaimsCreate)]
    public async Task<ActionResult<SubmissionResultDto>> ToClaim(string id, [FromBody] DerivedClaimRequestDto? request, CancellationToken ct)
        => Ok(await _service.CreateClaimFromPreAuthAsync(id, request ?? new DerivedClaimRequestDto(), ct));

    [HttpPost("{id}/status-check")]
    [HasPermission(Permissions.PreAuthRead)]
    public async Task<ActionResult<TaskResultDto>> StatusCheck(string id, CancellationToken ct)
        => Ok(await _service.StatusCheckAsync(id, ct));

    [HttpPost("{id}/cancel")]
    [HasPermission(Permissions.PreAuthSubmit)]
    public async Task<ActionResult<TaskResultDto>> Cancel(string id, [FromBody] CancelClaimRequestDto request, CancellationToken ct)
        => Ok(await _service.CancelAsync(id, request, ct));
}

[Route("api/rcm/batches")]
public sealed class RcmBatchesController : RcmControllerBase
{
    private readonly IRcmClaimWorkflowService _service;
    public RcmBatchesController(IRcmClaimWorkflowService service) => _service = service;

    [HttpPost]
    [HasPermission(Permissions.ClaimsSubmit)]
    public async Task<ActionResult<BatchSubmitResultDto>> Submit([FromBody] BatchSubmitRequestDto request, CancellationToken ct)
        => Ok(await _service.SubmitBatchAsync(request, ct));

    [HttpGet]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<List<BatchSubmitResultDto>>> List([FromQuery] RcmQuery query, CancellationToken ct)
        => Ok(await _service.ListBatchesAsync(Scope(query), ct));

    [HttpGet("{batchId}")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<IActionResult> Get(string batchId, CancellationToken ct)
        => await _service.GetBatchAsync(batchId, ct) is { } dto ? Ok(dto) : NotFoundResult("Batch", batchId);
}

/// <summary>Manual triggers for deferred-response ingestion (the poll worker does this on a schedule).</summary>
[Route("api/rcm/messages")]
public sealed class RcmMessagesController : RcmControllerBase
{
    private readonly IRcmPollService _poll;
    private readonly IRcmInboundService _inbound;

    public RcmMessagesController(IRcmPollService poll, IRcmInboundService inbound)
    {
        _poll = poll;
        _inbound = inbound;
    }

    [HttpPost("poll")]
    [HasPermission(Permissions.ClaimsSubmit)]
    public async Task<ActionResult<PollRunResultDto>> Poll([FromQuery] string? providerId, CancellationToken ct)
        => Ok(await _poll.PollOnceAsync(providerId, ct));

    /// <summary>Ingests a raw NPHIES response message bundle (replay / out-of-band delivery). Idempotent.</summary>
    [HttpPost("inbound")]
    [HasPermission(Permissions.ClaimsSubmit)]
    [Consumes("application/json", "application/fhir+json", "text/plain")]
    public async Task<ActionResult<RcmInboundResultDto>> Inbound(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(ct);
        return Ok(await _inbound.IngestRawAsync(json, ct));
    }
}

[Route("api/rcm")]
public sealed class RcmCommunicationsController : RcmControllerBase
{
    private readonly IRcmCommunicationService _service;
    public RcmCommunicationsController(IRcmCommunicationService service) => _service = service;

    [HttpGet("communication-requests")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<RcmPagedResult<CommunicationRequestDto>>> ListRequests([FromQuery] RcmQuery query, [FromQuery] bool openOnly = false, CancellationToken ct = default)
        => Ok(await _service.ListRequestsAsync(Scope(query), openOnly, ct));

    [HttpGet("communication-requests/{id}")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<IActionResult> GetRequest(string id, CancellationToken ct)
        => await _service.GetRequestAsync(id, ct) is { } dto ? Ok(dto) : NotFoundResult("Communication request", id);

    /// <summary>Solicited communication answering an insurer CommunicationRequest.</summary>
    [HttpPost("communication-requests/{id}/respond")]
    [HasPermission(Permissions.ClaimsUpdate)]
    public async Task<ActionResult<CommunicationDto>> Respond(string id, [FromBody] SendCommunicationDto request, CancellationToken ct)
        => Ok(await _service.RespondAsync(id, request, ct));

    [HttpGet("communications")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<RcmPagedResult<CommunicationDto>>> List([FromQuery] RcmQuery query, [FromQuery] string? claimId, CancellationToken ct)
        => Ok(await _service.ListAsync(Scope(query), claimId, ct));

    /// <summary>Unsolicited communication (e.g. additional attachments for a claim).</summary>
    [HttpPost("communications")]
    [HasPermission(Permissions.ClaimsUpdate)]
    public async Task<ActionResult<CommunicationDto>> Send([FromBody] SendCommunicationDto request, CancellationToken ct)
        => Ok(await _service.SendAsync(request, ct));
}

[Route("api/rcm/payments")]
public sealed class RcmPaymentsController : RcmControllerBase
{
    private readonly IRcmPaymentService _service;
    public RcmPaymentsController(IRcmPaymentService service) => _service = service;

    [HttpGet]
    [HasPermission(Permissions.PaymentsRead)]
    public async Task<ActionResult<RcmPagedResult<PaymentReconciliationDto>>> List([FromQuery] RcmQuery query, CancellationToken ct)
        => Ok(await _service.ListAsync(Scope(query), ct));

    [HttpGet("discrepancies")]
    [HasPermission(Permissions.PaymentsRead)]
    public async Task<ActionResult<List<PaymentDetailDto>>> Discrepancies([FromQuery] RcmQuery query, CancellationToken ct)
        => Ok(await _service.GetDiscrepanciesAsync(Scope(query), ct));

    [HttpGet("{id}")]
    [HasPermission(Permissions.PaymentsRead)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
        => await _service.GetAsync(id, ct) is { } dto ? Ok(dto) : NotFoundResult("Payment reconciliation", id);

    /// <summary>Sends the PaymentNotice acknowledging a payment reconciliation.</summary>
    [HttpPost("{id}/acknowledge")]
    [HasPermission(Permissions.PaymentsManage)]
    public async Task<ActionResult<PaymentReconciliationDto>> Acknowledge(string id, CancellationToken ct)
        => Ok(await _service.AcknowledgeAsync(id, ct));

    [HttpPost("details/{detailId}/match")]
    [HasPermission(Permissions.PaymentsManage)]
    public async Task<ActionResult<PaymentReconciliationDto>> Match(string detailId, [FromBody] ManualMatchDto request, CancellationToken ct)
        => Ok(await _service.ManualMatchAsync(detailId, request, ct));
}

/// <summary>Insurer-initiated (advanced) authorizations received via poll, and quoting them on a claim.</summary>
[Route("api/rcm/advanced-authorizations")]
public sealed class RcmAdvancedAuthorizationsController : RcmControllerBase
{
    private readonly IRcmAdvancedAuthorizationService _service;
    public RcmAdvancedAuthorizationsController(IRcmAdvancedAuthorizationService service) => _service = service;

    [HttpGet]
    [HasPermission(Permissions.PreAuthRead)]
    public async Task<ActionResult<RcmPagedResult<AdvancedAuthorizationDto>>> List([FromQuery] RcmQuery query, CancellationToken ct)
        => Ok(await _service.ListAsync(Scope(query), ct));

    [HttpGet("{id}")]
    [HasPermission(Permissions.PreAuthRead)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
        => await _service.GetAsync(id, ProviderScope(), ct) is { } dto ? Ok(dto) : NotFoundResult("Advanced authorization", id);

    /// <summary>Quotes the authorization's preAuthRef on an unsent claim and marks the authorization used.</summary>
    [HttpPost("{id}/use")]
    [HasPermission(Permissions.ClaimsUpdate)]
    public async Task<ActionResult<AdvancedAuthorizationDto>> Use(string id, [FromBody] UseAdvancedAuthorizationDto request, CancellationToken ct)
        => Ok(await _service.UseInClaimAsync(id, request, ProviderScope(), ct));
}

[Route("api/rcm/denials")]
public sealed class RcmDenialsController : RcmControllerBase
{
    private readonly IRcmDenialService _service;
    public RcmDenialsController(IRcmDenialService service) => _service = service;

    [HttpGet]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<RcmPagedResult<DenialDto>>> List([FromQuery] DenialQuery query, CancellationToken ct)
        => Ok(await _service.ListAsync(Scope(query), ct));

    [HttpGet("summary")]
    [HasPermission(Permissions.ReportsRead)]
    public async Task<ActionResult<DenialSummaryDto>> Summary([FromQuery] RcmQuery query, CancellationToken ct)
        => Ok(await _service.GetSummaryAsync(Scope(query), ct));

    [HttpGet("{id}")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
        => await _service.GetAsync(id, ct) is { } dto ? Ok(dto) : NotFoundResult("Denial", id);

    [HttpPatch("{id}")]
    [HasPermission(Permissions.ClaimsUpdate)]
    public async Task<ActionResult<DenialDto>> Update(string id, [FromBody] UpdateDenialDto request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));
}

public sealed class WithdrawAppealRequest
{
    public string? Reason { get; set; }
}

[Route("api/rcm/appeals")]
public sealed class RcmAppealsController : RcmControllerBase
{
    private readonly IRcmAppealService _service;
    public RcmAppealsController(IRcmAppealService service) => _service = service;

    [HttpPost]
    [HasPermission(Permissions.ClaimsSubmit)]
    public async Task<ActionResult<AppealDto>> Create([FromBody] CreateAppealDto request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));

    [HttpGet]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<RcmPagedResult<AppealDto>>> List([FromQuery] RcmQuery query, CancellationToken ct)
        => Ok(await _service.ListAsync(Scope(query), ct));

    [HttpGet("{id}")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
        => await _service.GetAsync(id, ct) is { } dto ? Ok(dto) : NotFoundResult("Appeal", id);

    [HttpPost("{id}/withdraw")]
    [HasPermission(Permissions.ClaimsUpdate)]
    public async Task<ActionResult<AppealDto>> Withdraw(string id, [FromBody] WithdrawAppealRequest? request, CancellationToken ct)
        => Ok(await _service.WithdrawAsync(id, request?.Reason, ct));
}

[Route("api/rcm/reports")]
public sealed class RcmReportsController : RcmControllerBase
{
    private readonly IRcmAnalyticsService _service;
    public RcmReportsController(IRcmAnalyticsService service) => _service = service;

    [HttpGet("dashboard")]
    [HasPermission(Permissions.ReportsRead)]
    public async Task<ActionResult<RcmDashboardDto>> Dashboard([FromQuery] RcmQuery query, CancellationToken ct)
        => Ok(await _service.GetDashboardAsync(Scope(query), ct));

    [HttpGet("aging")]
    [HasPermission(Permissions.ReportsRead)]
    public async Task<ActionResult<List<AgingBucketDto>>> Aging([FromQuery] RcmQuery query, CancellationToken ct)
        => Ok(await _service.GetAgingAsync(Scope(query), ct));

    /// <summary>Denial rate grouped by day, week, month or payer.</summary>
    [HttpGet("denial-rate")]
    [HasPermission(Permissions.ReportsRead)]
    public async Task<ActionResult<List<DenialRateDto>>> DenialRate([FromQuery] RcmQuery query, [FromQuery] string groupBy = "month", CancellationToken ct = default)
        => Ok(await _service.GetDenialRateAsync(Scope(query), groupBy, ct));

    [HttpGet("payers")]
    [HasPermission(Permissions.ReportsRead)]
    public async Task<ActionResult<List<PayerBreakdownDto>>> Payers([FromQuery] RcmQuery query, CancellationToken ct)
        => Ok(await _service.GetPayerBreakdownAsync(Scope(query), ct));
}
