using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm;

public sealed class RcmInboundResultDto
{
    public bool Parsed { get; set; }
    public string? MessageId { get; set; }
    public string? EventCode { get; set; }
    public int Processed { get; set; }
    public int Duplicates { get; set; }
    public int Unmatched { get; set; }
    public List<string> ClaimIds { get; set; } = new();
    public List<string> PaymentReconciliationIds { get; set; } = new();
    public List<string> CommunicationRequestIds { get; set; } = new();
    public List<string> AdvancedAuthorizationIds { get; set; } = new();
    public List<string> EligibilityCheckIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

/// <summary>
/// Ingests an NPHIES response message received out of band (e.g. pasted from the portal or replayed from the audit log).
/// Uses the same idempotent ingestor as the synchronous and poll paths.
/// </summary>
public interface IRcmInboundService
{
    Task<RcmInboundResultDto> IngestRawAsync(string json, CancellationToken ct = default);
}

public sealed class RcmInboundService : IRcmInboundService
{
    private readonly INphiesResponseParser _parser;
    private readonly IRcmResponseIngestor _ingestor;
    private readonly ApplicationDbContext _db;

    public RcmInboundService(INphiesResponseParser parser, IRcmResponseIngestor ingestor, ApplicationDbContext db)
    {
        _parser = parser;
        _ingestor = ingestor;
        _db = db;
    }

    public async Task<RcmInboundResultDto> IngestRawAsync(string json, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(json)) throw RcmWorkflowException.Invalid("A FHIR message bundle is required.");
        var response = _parser.Parse(json);
        var result = new RcmInboundResultDto { MessageId = response.MessageId, EventCode = response.EventCode };
        if (response.Bundle is null && response.Resource is null)
        {
            result.Errors.AddRange(response.Errors.Select(e => $"{e.Code}: {e.Message}"));
            return result;
        }
        result.Parsed = true;

        Domain.Entities.NphiesTransaction? requestTx = null;
        if (!string.IsNullOrWhiteSpace(response.RequestMessageId))
            requestTx = await _db.NphiesTransactions.FirstOrDefaultAsync(t => t.Direction == "outbound" && t.MessageId == response.RequestMessageId, ct);

        var ingest = await _ingestor.IngestAsync(response, requestTx, ct);
        await _db.SaveChangesAsync(ct);

        result.Processed = ingest.Processed;
        result.Duplicates = ingest.Duplicates;
        result.Unmatched = ingest.Unmatched;
        result.ClaimIds = ingest.ClaimIds.ToList();
        result.PaymentReconciliationIds = ingest.PaymentReconciliationIds.ToList();
        result.CommunicationRequestIds = ingest.CommunicationRequestIds.ToList();
        result.AdvancedAuthorizationIds = ingest.AdvancedAuthorizationIds.ToList();
        result.EligibilityCheckIds = ingest.EligibilityCheckIds.ToList();
        result.Notes = ingest.Notes.ToList();
        result.Errors = response.Errors.Select(e => $"{e.Code}: {e.Message}").ToList();
        return result;
    }
}

public static class RcmServiceCollectionExtensions
{
    /// <summary>Registers the RCM workflow layer (requires the NPHIES messaging core and ApplicationDbContext).</summary>
    public static IServiceCollection AddRcmWorkflows(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RcmOptions>(configuration.GetSection(RcmOptions.SectionName));
        services.AddScoped<IRcmInputMapper, RcmInputMapper>();
        services.AddScoped<IRcmMessageDispatcher, RcmMessageDispatcher>();
        services.AddScoped<IRcmResponseIngestor, RcmResponseIngestor>();
        services.AddScoped<IRcmInboundService, RcmInboundService>();
        services.AddScoped<IRcmEligibilityService, RcmEligibilityService>();
        services.AddScoped<IRcmClaimWorkflowService, RcmClaimWorkflowService>();
        services.AddScoped<IRcmClaimEditorService, RcmClaimEditorService>();
        services.AddScoped<IRcmCommunicationService, RcmCommunicationService>();
        services.AddScoped<IRcmPaymentService, RcmPaymentService>();
        services.AddScoped<IRcmAdvancedAuthorizationService, RcmAdvancedAuthorizationService>();
        services.AddScoped<IRcmPollService, RcmPollService>();
        services.AddScoped<IRcmEvidenceService, RcmEvidenceService>();
        services.AddScoped<IRcmConnectivityService, RcmConnectivityService>();
        services.AddScoped<IRcmDenialService, RcmDenialService>();
        services.AddScoped<IRcmAppealService, RcmAppealService>();
        services.AddScoped<IRcmAnalyticsService, RcmAnalyticsService>();
        services.Configure<Documents.RcmDocumentOptions>(configuration.GetSection(Documents.RcmDocumentOptions.SectionName));
        services.AddScoped<Documents.IClaimDocumentStorage, Documents.ClaimDocumentStorage>();
        services.AddSingleton<Documents.IPdfCompressor, Documents.GhostscriptPdfCompressor>();
        services.AddScoped<Documents.IRcmDocumentService, Documents.RcmDocumentService>();
        services.AddScoped<NPhies_FHIR_Integration.Application.Services.Validation.IEnhancedEligibilityVerificationService, NPhies_FHIR_Integration.Application.Services.Validation.EnhancedEligibilityVerificationService>();
        return services;
    }
}
