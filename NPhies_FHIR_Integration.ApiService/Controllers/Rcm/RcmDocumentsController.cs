using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.Application.Rcm.Documents;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;

namespace NPhies_FHIR_Integration.ApiService.Controllers.Rcm;

/// <summary>
/// Claim document store (PDF / images). Content is kept outside the claim tables and embedded as supportingInfo
/// valueAttachment only when the bundle is built. Filter by patient, claim, encounter number/type, invoice and service date.
/// </summary>
[Route("api/rcm/documents")]
public sealed class RcmDocumentsController : RcmControllerBase
{
    private const long MaxRequestBytes = 40L * 1024 * 1024;
    private readonly IRcmDocumentService _service;
    public RcmDocumentsController(IRcmDocumentService service) => _service = service;

    [HttpGet]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<RcmPagedResult<ClaimDocumentDto>>> List([FromQuery] DocumentQuery query, CancellationToken ct)
        => Ok(await _service.ListAsync(Scope(query), ct));

    [HttpGet("types")]
    [HasPermission(Permissions.ClaimsRead)]
    public ActionResult<string[]> Types() => Ok(RcmDocumentService.DocumentTypes);

    [HttpGet("{id}")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
        => await _service.GetAsync(id, ProviderScope(), ct) is { } dto ? Ok(dto) : NotFoundResult("Document", id);

    /// <summary>Downloads the content sent to NPHIES (or the uncompressed upload with <c>original=true</c>).</summary>
    [HttpGet("{id}/content")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<IActionResult> Content(string id, [FromQuery] bool original, [FromQuery] bool inline, CancellationToken ct)
    {
        var content = await _service.DownloadAsync(id, original, ProviderScope(), ct);
        if (content is null) return NotFoundResult("Document", id);
        return inline ? File(content.Data, content.ContentType) : File(content.Data, content.ContentType, content.FileName);
    }

    /// <summary>Multipart upload: <c>file</c> plus metadata fields (patientId, encounterNumber, invoiceDate, claimId ...).</summary>
    [HttpPost]
    [HasPermission(Permissions.ClaimsCreate)]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<ActionResult<ClaimDocumentDto>> Upload([FromForm] IFormFile file, [FromForm] DocumentMetadataDto metadata, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "A non-empty file is required." });
        using var buffer = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
        await file.CopyToAsync(buffer, ct);
        return Ok(await _service.UploadAsync(metadata, file.FileName, file.ContentType, buffer.ToArray(), Actor(), ProviderScope(), ct));
    }

    /// <summary>HIS push with base64 content; idempotent on <c>externalId</c>.</summary>
    [HttpPost("ingest")]
    [HasPermission(Permissions.ClaimsCreate)]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<ActionResult<ClaimDocumentDto>> Ingest([FromBody] DocumentIngestDto request, CancellationToken ct)
        => Ok(await _service.IngestAsync(request, Actor(), ProviderScope(), ct));

    [HttpDelete("{id}")]
    [HasPermission(Permissions.ClaimsDelete)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ProviderScope(), ct);
        return NoContent();
    }

    /// <summary>Admin: moves legacy inline base64 attachments (ClaimSupportingInfo.AttachmentData) into the document store.</summary>
    [HttpPost("migrate-inline")]
    [Authorize(Roles = SecurityConstants.ADMIN_ROLE)]
    public async Task<ActionResult<DocumentMigrationResultDto>> MigrateInline([FromQuery] int max = 200, CancellationToken ct = default)
        => Ok(await _service.MigrateInlineAttachmentsAsync(max, ProviderScope(), ct));
}

/// <summary>Documents attached to a claim / prior authorization and its NPHIES 10 MB size budget.</summary>
[Route("api/rcm/claims/{claimId}")]
public sealed class RcmClaimDocumentsController : RcmControllerBase
{
    private readonly IRcmDocumentService _service;
    public RcmClaimDocumentsController(IRcmDocumentService service) => _service = service;

    [HttpGet("documents")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<RcmPagedResult<ClaimDocumentDto>>> List(string claimId, CancellationToken ct)
        => Ok(await _service.ListAsync(Scope(new DocumentQuery { ClaimId = claimId, PageSize = 500 }), ct));

    /// <summary>Attaches stored documents as supportingInfo (category <c>attachment</c> by default). Claim must be unsent.</summary>
    [HttpPost("documents")]
    [HasPermission(Permissions.ClaimsUpdate)]
    public async Task<ActionResult<ClaimSizeBudgetDto>> Attach(string claimId, [FromBody] AttachDocumentsDto request, CancellationToken ct)
        => Ok(await _service.AttachToClaimAsync(claimId, request, ProviderScope(), ct));

    [HttpDelete("documents/{documentId}")]
    [HasPermission(Permissions.ClaimsUpdate)]
    public async Task<ActionResult<ClaimSizeBudgetDto>> Detach(string claimId, string documentId, CancellationToken ct)
        => Ok(await _service.DetachFromClaimAsync(claimId, documentId, ProviderScope(), ct));

    /// <summary>Patient documents from the claim's encounter or billable period (inpatient interim claims).</summary>
    [HttpGet("document-candidates")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<List<ClaimDocumentDto>>> Candidates(string claimId, CancellationToken ct)
        => Ok(await _service.GetCandidatesAsync(claimId, ProviderScope(), ct));

    [HttpGet("size-budget")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<ClaimSizeBudgetDto>> SizeBudget(string claimId, CancellationToken ct)
        => Ok(await _service.GetSizeBudgetAsync(claimId, ProviderScope(), ct));
}
