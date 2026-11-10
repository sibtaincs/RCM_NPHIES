using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.ApiService.Controllers.Rcm;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Legacy RCM routes (<c>api/RCM/*</c>) kept for existing clients. They delegate to the NPHIES RCM workflow
/// services; new clients should use the <c>api/rcm/{area}</c> controllers directly.
/// </summary>
/// <remarks>
/// The former <c>POST adjudicate</c> endpoint was removed: adjudication is performed by the insurer and
/// arrives through NPHIES (synchronous reply or poll), see <c>GET api/rcm/claims/{id}/adjudication</c>.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "RCMViewer")]
[RcmExceptionFilter]
public class RCMController : BaseController
{
    private readonly IRcmInboundService _inbound;
    private readonly IRcmClaimWorkflowService _claims;
    private readonly IRcmPaymentService _payments;

    public RCMController(
        IRcmInboundService inbound,
        IRcmClaimWorkflowService claims,
        IRcmPaymentService payments)
    {
        _inbound = inbound;
        _claims = claims;
        _payments = payments;
    }

    /// <summary>Ingests an NPHIES response message bundle (claim-response, payment-reconciliation, ...).</summary>
    [HttpPost("process-response")]
    [Authorize(Policy = "RCMProcessor")]
    [Consumes("application/json", "application/fhir+json", "text/plain")]
    public async Task<IActionResult> ProcessClaimResponse(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var result = await _inbound.IngestRawAsync(await reader.ReadToEndAsync(ct), ct);
        return result.Parsed ? Ok(result) : BadRequest("The message could not be parsed.", result.Errors);
    }

    // Denial listing and appeal create/get are served by RcmDenialsController / RcmAppealsController on the same
    // routes (api/rcm/denials, api/rcm/appeals); duplicates here made routing ambiguous.

    [HttpGet("reconciliation")]
    public async Task<IActionResult> GetReconciliation(
        [FromQuery] string? providerId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var query = new RcmQuery { ProviderId = providerId, From = fromDate, To = toDate };
        var payments = await _payments.ListAsync(query, ct);
        var discrepancies = await _payments.GetDiscrepanciesAsync(query, ct);
        return Ok(new { payments, discrepancies });
    }

    [HttpGet("summary/{claimId}")]
    public async Task<IActionResult> GetClaimSummary([FromRoute] string claimId, CancellationToken ct)
    {
        var timeline = await _claims.GetTimelineAsync(claimId, ct);
        if (timeline is null) return NotFound($"Claim '{claimId}' was not found.");
        var adjudication = await _claims.GetLatestAdjudicationAsync(claimId, ct);
        return Ok(new { timeline, adjudication });
    }
}
