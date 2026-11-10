using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Infrastructure.Configuration;

namespace NPhies_FHIR_Integration.ApiService.Controllers.Rcm;

/// <summary>Local NPHIES conformance: validates request bundles against the business rules and the embedded NPHIES IG profiles.</summary>
[Route("api/rcm/conformance")]
public sealed class RcmConformanceController : RcmControllerBase
{
    private static readonly FhirJsonParser Parser = new(new ParserSettings { PermissiveParsing = true });

    private readonly INphiesBundleValidator _validator;
    private readonly INphiesProfileValidator _profiles;
    private readonly NphiesProfileValidationMode _mode;
    private readonly IRcmEvidenceService _evidence;
    private readonly IRcmConnectivityService _connectivity;

    public RcmConformanceController(INphiesBundleValidator validator, INphiesProfileValidator profiles, IOptions<NphiesConfiguration> options,
        IRcmEvidenceService evidence, IRcmConnectivityService connectivity)
    {
        _evidence = evidence;
        _connectivity = connectivity;
        _validator = validator;
        _profiles = profiles;
        _mode = options.Value.ProfileValidation.Mode;
    }

    /// <summary>The NPHIES IG package used for local profile validation and the configured enforcement mode.</summary>
    [HttpGet("package")]
    [HasPermission(Permissions.ClaimsRead)]
    public ActionResult<ConformancePackageInfoDto> Package()
    {
        var p = _profiles.Package;
        return Ok(new ConformancePackageInfoDto
        {
            PackageName = p.PackageName,
            PackageVersion = p.PackageVersion,
            PackageDate = p.PackageDate,
            Profiles = p.Profiles.Count,
            ValueSets = p.ValueSets.Count,
            CodeSystems = p.CodeSystems.Count,
            ProfileMode = _mode.ToString(),
            LoadErrors = p.LoadErrors.ToList(),
        });
    }

    /// <summary>
    /// Validates a raw NPHIES request message bundle (FHIR JSON body). Profile findings are always included; with
    /// ProfileValidation:Mode = Warn they are reported as warnings, exactly as they would be on send.
    /// </summary>
    [HttpPost("validate")]
    [HasPermission(Permissions.ClaimsRead)]
    [Consumes("application/json", "application/fhir+json", "text/plain")]
    public async Task<ActionResult<BundleValidationResultDto>> Validate(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(ct);
        Bundle bundle;
        try
        {
            bundle = Parser.Parse<Bundle>(json);
        }
        catch (Exception ex) when (ex is FormatException or DeserializationFailedException or InvalidCastException)
        {
            return BadRequest(new ProblemDetails { Title = "Body is not a FHIR Bundle", Detail = ex.Message, Status = 400 });
        }

        var issues = _validator.Validate(bundle, System.Text.Encoding.UTF8.GetByteCount(json)).Issues.ToList();
        if (_mode == NphiesProfileValidationMode.Off)
            issues.AddRange(_profiles.Validate(bundle).Issues
                .Select(i => i with { Severity = i.Severity == NphiesValidationSeverity.Error ? NphiesValidationSeverity.Warning : i.Severity }));

        var result = new BundleValidationResultDto { ProfileMode = _mode.ToString() };
        result.Issues.AddRange(issues.Select(i => new BundleValidationIssueDto
        {
            Severity = i.Severity.ToString(),
            RuleId = i.RuleId,
            Expression = i.Expression,
            Message = i.Message,
        }));
        return Ok(result);
    }
    /// <summary>Live-test pre-flight: configuration, mTLS certificate, DNS and TLS handshake with the NPHIES endpoint.</summary>
    [HttpGet("connectivity")]
    [HasPermission(Permissions.ClaimsRead)]
    public async Task<ActionResult<ConnectivityReportDto>> Connectivity(CancellationToken ct)
        => Ok(await _connectivity.CheckAsync(includePoll: false, ct));

    /// <summary>The connectivity checks plus a real poll-request round trip (any messages returned are ingested as usual).</summary>
    [HttpPost("connectivity/poll")]
    [HasPermission(Permissions.ClaimsSubmit)]
    public async Task<ActionResult<ConnectivityReportDto>> ConnectivityWithPoll(CancellationToken ct)
        => Ok(await _connectivity.CheckAsync(includePoll: true, ct));

    /// <summary>Test-case evidence pack (zip of request/response bundles, timelines and summaries) for up to 50 claims / prior-auths.</summary>
    [HttpPost("evidence")]
    [HasPermission(Permissions.ClaimsRead)]
    [Produces("application/zip")]
    public async Task<IActionResult> Evidence([FromBody] EvidenceExportRequestDto request, CancellationToken ct)
    {
        var file = await _evidence.BuildAsync(request.ClaimIds, ProviderScope(), ct);
        return File(file.Content, "application/zip", file.FileName);
    }
}
