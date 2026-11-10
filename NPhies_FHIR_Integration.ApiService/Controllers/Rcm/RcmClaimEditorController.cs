using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers.Rcm;

/// <summary>
/// Claim / prior-authorization editor: drafts are fully editable; sent versions are locked and corrected through the
/// lifecycle actions listed by <c>corrections</c>. Every edit is recalculated server-side and audited.
/// </summary>
[Route("api/rcm/claims")]
public sealed class RcmClaimEditorController : RcmControllerBase
{
    private readonly IRcmClaimEditorService _editor;
    private readonly IRcmClaimWorkflowService _workflow;
    public RcmClaimEditorController(IRcmClaimEditorService editor, IRcmClaimWorkflowService workflow)
    {
        _editor = editor;
        _workflow = workflow;
    }

    /// <summary>Full editable view (header, items, diagnoses, care team, supporting info) plus lock state.</summary>
    [HttpGet("{id}/edit")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<ClaimEditDto>> Get(string id, CancellationToken ct)
        => Ok(await _editor.GetAsync(id, ProviderScope(), ct));

    /// <summary>Creates a new draft claim or prior authorization (use = claim | preauthorization).</summary>
    [HttpPost("draft")]
    [HasPermission(Permissions.ClaimsCreate)]
    public async Task<ActionResult<ClaimEditDto>> Create([FromBody] ClaimEditDto request, CancellationToken ct)
        => Ok(await _editor.CreateAsync(request, Actor(), ProviderScope(), ct));

    /// <summary>Saves a draft / failed version. Returns 409 when the NPHIES state locks the record.</summary>
    [HttpPut("{id}")]
    [HasPermission(Permissions.ClaimsUpdate)]
    public async Task<ActionResult<ClaimEditDto>> Update(string id, [FromBody] ClaimEditDto request, CancellationToken ct)
        => Ok(await _editor.UpdateAsync(id, request, Actor(), ProviderScope(), ct));

    /// <summary>
    /// Claim health check: data-quality and NPHIES readiness rules with field locations. With conformance=true (default)
    /// the request bundle is also built and validated locally against the NPHIES IG profiles (issues under "bundle:" fields).
    /// </summary>
    [HttpGet("{id}/health")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<ClaimHealthDto>> Health(string id, [FromQuery] bool conformance = true, CancellationToken ct = default)
    {
        var health = await _editor.CheckHealthAsync(id, ProviderScope(), ct);
        if (!conformance) return Ok(health);
        try
        {
            var preview = await _workflow.PreviewBundleAsync(id, ct);
            health.Issues.AddRange(preview.Issues.Select(i => new ClaimHealthIssueDto
            {
                Severity = i.Severity == "Information" ? "Info" : i.Severity,
                Code = i.RuleId,
                Field = i.Expression is null ? "bundle" : "bundle:" + i.Expression,
                Message = i.Message,
            }));
        }
        catch (RcmWorkflowException ex) when (ex.Kind == RcmErrorKind.BadRequest)
        {
            health.Issues.Add(new ClaimHealthIssueDto { Severity = "Error", Code = "BUNDLE-BUILD", Field = "bundle", Message = "The NPHIES bundle cannot be built: " + ex.Message });
        }
        return Ok(health);
    }

    /// <summary>Health report across claims (same filters as the claim list); problemsOnly hides clean claims.</summary>
    [HttpGet("health")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<List<ClaimHealthDto>>> HealthReport([FromQuery] RcmClaimQuery query, [FromQuery] bool problemsOnly = true, CancellationToken ct = default)
        => Ok(await _editor.CheckHealthBulkAsync(Scope(query), problemsOnly, ct));

    /// <summary>Lifecycle-valid ways to correct the record in its current NPHIES state.</summary>
    [HttpGet("{id}/corrections")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<ClaimCorrectionOptionsDto>> Corrections(string id, CancellationToken ct)
        => Ok(await _editor.GetCorrectionOptionsAsync(id, ProviderScope(), ct));

    [HttpGet("{id}/audit")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<List<ClaimAuditEntryDto>>> Audit(string id, CancellationToken ct)
        => Ok(await _editor.GetAuditAsync(id, ProviderScope(), ct));

    /// <summary>Field-level differences from the version this one replaces (related.claim).</summary>
    [HttpGet("{id}/compare")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<ClaimVersionCompareDto>> Compare(string id, CancellationToken ct)
        => Ok(await _editor.CompareWithPreviousAsync(id, ProviderScope(), ct));
}
