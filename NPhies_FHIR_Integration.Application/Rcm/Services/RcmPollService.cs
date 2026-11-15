using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Models;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

public interface IRcmPollService
{
    /// <summary>
    /// Polls NPHIES until the queue is empty (or <see cref="RcmOptions.MaxPollsPerCycle"/> is reached), ingests every
    /// deferred message and acknowledges payment reconciliations with a payment-notice.
    /// </summary>
    Task<PollRunResultDto> PollOnceAsync(string? providerId = null, CancellationToken ct = default);

    /// <summary>
    /// Same as <see cref="PollOnceAsync"/> with filters: count, include or exclude message types, a period, or the
    /// response to one claim (<see cref="PollRequestDto.FocusClaimId"/>; polls that provider once).
    /// </summary>
    Task<PollRunResultDto> PollAsync(PollRequestDto request, CancellationToken ct = default);
}

public sealed class RcmPollService : IRcmPollService
{
    private readonly ApplicationDbContext _db;
    private readonly IRcmInputMapper _mapper;
    private readonly INphiesBundleBuilder _builder;
    private readonly IRcmMessageDispatcher _dispatcher;
    private readonly IRcmResponseIngestor _ingestor;
    private readonly IRcmPaymentService _payments;
    private readonly RcmOptions _options;
    private readonly NphiesConfiguration _nphies;
    private readonly ILogger<RcmPollService> _logger;

    public RcmPollService(
        ApplicationDbContext db,
        IRcmInputMapper mapper,
        INphiesBundleBuilder builder,
        IRcmMessageDispatcher dispatcher,
        IRcmResponseIngestor ingestor,
        IRcmPaymentService payments,
        IOptions<RcmOptions> options,
        IOptions<NphiesConfiguration> nphies,
        ILogger<RcmPollService> logger)
    {
        _db = db;
        _mapper = mapper;
        _builder = builder;
        _dispatcher = dispatcher;
        _ingestor = ingestor;
        _payments = payments;
        _options = options.Value;
        _nphies = nphies.Value;
        _logger = logger;
    }

    public Task<PollRunResultDto> PollOnceAsync(string? providerId = null, CancellationToken ct = default) =>
        PollAsync(new PollRequestDto { ProviderId = providerId }, ct);

    public async Task<PollRunResultDto> PollAsync(PollRequestDto request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.IncludeMessageTypes.Count > 0 && request.ExcludeMessageTypes.Count > 0)
            throw RcmWorkflowException.Invalid("Use include message types or exclude message types, not both.");
        if (request.Count is < 1 or > NphiesLimits.PollMaxMessages)
            throw RcmWorkflowException.Invalid($"Count must be between 1 and {NphiesLimits.PollMaxMessages}.");
        if (request.PeriodStart > request.PeriodEnd)
            throw RcmWorkflowException.Invalid("The period start must not be after its end.");

        var providerId = request.ProviderId;
        NphiesIdentifier? focus = null;
        if (!string.IsNullOrWhiteSpace(request.FocusClaimId))
        {
            var claim = await _db.Claims.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.FocusClaimId, ct)
                        ?? throw RcmWorkflowException.NotFound("Claim", request.FocusClaimId!);
            if (claim.SubmittedAt is null || string.IsNullOrWhiteSpace(claim.ClaimIdentifierValue))
                throw RcmWorkflowException.Conflict("The claim has not been sent to NPHIES, so there is no response to poll for.");
            if (!string.IsNullOrWhiteSpace(providerId) && claim.ProviderId != providerId)
                throw RcmWorkflowException.NotFound("Claim", request.FocusClaimId!);
            focus = new NphiesIdentifier(claim.ClaimIdentifierSystem ?? _mapper.IdentifierSystem("claim"), claim.ClaimIdentifierValue!);
            providerId = claim.ProviderId;
        }

        var count = request.Count ?? (_nphies.PollMaxCount > 0 ? _nphies.PollMaxCount : (int?)null);
        var period = request.PeriodStart is null && request.PeriodEnd is null ? null
            : new NphiesPeriod(request.PeriodStart?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                               request.PeriodEnd?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        // A focused poll asks for one answer; the other filters page through the queue.
        var maxPolls = focus is null ? Math.Max(1, _options.MaxPollsPerCycle) : 1;

        var result = new PollRunResultDto();
        var providers = await ResolveProvidersAsync(providerId, ct);
        if (providers.Count == 0)
        {
            result.Notes.Add("No provider organization to poll for (configure Nphies:Sender:ProviderLicense or pass providerId).");
            return result;
        }

        foreach (var (orgId, provider) in providers)
        {
            for (var i = 0; i < maxPolls; i++)
            {
                ct.ThrowIfCancellationRequested();
                var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                var input = new PollRequestInput
                {
                    TaskId = Guid.NewGuid().ToString("N"),
                    Identifier = new NphiesIdentifier(_mapper.IdentifierSystem("task"), $"Poll_{stamp}"),
                    Provider = provider,
                    Count = count,
                    IncludeMessageTypes = request.IncludeMessageTypes.ToList(),
                    ExcludeMessageTypes = request.ExcludeMessageTypes.ToList(),
                    Period = period,
                    Focus = focus,
                };
                var dispatch = await _dispatcher.DispatchAsync(new RcmDispatchRequest
                {
                    Message = _builder.BuildPollRequest(input),
                    Operation = RcmOperations.Poll,
                    EntityType = RcmEntityTypes.Poll,
                    EntityId = input.Identifier.Value,
                    RequestIdentifierSystem = input.Identifier.System,
                    RequestIdentifierValue = input.Identifier.Value,
                    ProviderId = orgId,
                }, ct);
                result.PollRequests++;

                if (dispatch.Response is null || dispatch.Status is RcmTransactionStatus.Error or RcmTransactionStatus.ValidationFailed or RcmTransactionStatus.Timeout)
                {
                    result.Errors.AddRange(dispatch.Errors.Count > 0 ? dispatch.Errors : new List<string> { $"Poll {dispatch.Status}" });
                    break;
                }

                var received = dispatch.Response.InnerMessages.Count;
                result.MessagesReceived += received;
                if (received == 0) break;

                var ingest = await _ingestor.IngestAsync(dispatch.Response, dispatch.Transaction, ct);
                await _db.SaveChangesAsync(ct);
                Accumulate(result, ingest);

                // Keep polling while the page was full or NPHIES says more messages are queued.
                if (received < (input.Count ?? int.MaxValue) && input.Count.HasValue && !dispatch.Response.HasQueuedMessages) break;
            }
        }

        if (_options.AutoAcknowledgePayments)
        {
            foreach (var id in result.PaymentReconciliationIds.ToList())
            {
                try
                {
                    var ack = await _payments.AcknowledgeAsync(id, ct);
                    if (ack.AcknowledgementStatus == "Acknowledged") result.PaymentNoticesSent++;
                    else if (!string.IsNullOrWhiteSpace(ack.AcknowledgementError)) result.Errors.Add($"Payment {ack.PaymentReconciliationId}: {ack.AcknowledgementError}");
                }
                catch (RcmWorkflowException ex)
                {
                    result.Notes.Add($"Payment {id}: {ex.Message}");
                }
            }
        }

        _logger.LogInformation("NPHIES poll: {Polls} request(s), {Messages} message(s), {Processed} processed, {Duplicates} duplicate(s), {Unmatched} unmatched, {Acks} payment notice(s).",
            result.PollRequests, result.MessagesReceived, result.Processed, result.Duplicates, result.Unmatched, result.PaymentNoticesSent);
        return result;
    }

    private static void Accumulate(PollRunResultDto result, RcmIngestResult ingest)
    {
        result.Processed += ingest.Processed;
        result.Duplicates += ingest.Duplicates;
        result.Unmatched += ingest.Unmatched;
        foreach (var x in ingest.ClaimIds) if (!result.ClaimIds.Contains(x)) result.ClaimIds.Add(x);
        foreach (var x in ingest.PaymentReconciliationIds) if (!result.PaymentReconciliationIds.Contains(x)) result.PaymentReconciliationIds.Add(x);
        foreach (var x in ingest.CommunicationRequestIds) if (!result.CommunicationRequestIds.Contains(x)) result.CommunicationRequestIds.Add(x);
        foreach (var x in ingest.AdvancedAuthorizationIds) if (!result.AdvancedAuthorizationIds.Contains(x)) result.AdvancedAuthorizationIds.Add(x);
        result.Notes.AddRange(ingest.Notes);
    }

    /// <summary>Providers to poll for: the given one, else every provider that has sent messages, else the configured sender.</summary>
    private async Task<List<(string? OrgId, OrganizationInput Input)>> ResolveProvidersAsync(string? providerId, CancellationToken ct)
    {
        var list = new List<(string?, OrganizationInput)>();
        List<Organization> orgs;
        if (!string.IsNullOrWhiteSpace(providerId))
        {
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == providerId, ct) ?? throw RcmWorkflowException.NotFound("Provider", providerId!);
            orgs = new List<Organization> { org };
        }
        else
        {
            var ids = await _db.NphiesTransactions.Where(t => t.Direction == "outbound" && t.ProviderId != null && t.Operation != RcmOperations.Poll)
                .Select(t => t.ProviderId!).Distinct().ToListAsync(ct);
            orgs = await _db.Organizations.Where(o => ids.Contains(o.Id)).ToListAsync(ct);
        }

        foreach (var org in orgs)
        {
            var input = _mapper.MapOrganization(org, NphiesOrganizationKind.Provider);
            if (!string.IsNullOrWhiteSpace(input.License) && list.All(p => p.Item2.License != input.License)) list.Add((org.Id, input));
        }

        if (list.Count == 0 && string.IsNullOrWhiteSpace(providerId) && !string.IsNullOrWhiteSpace(_nphies.Sender.ProviderLicense))
        {
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.NphiesProviderId == _nphies.Sender.ProviderLicense || o.LicenseNumber == _nphies.Sender.ProviderLicense, ct);
            list.Add((org?.Id, org != null
                ? _mapper.MapOrganization(org, NphiesOrganizationKind.Provider)
                : new OrganizationInput { Id = "provider", License = _nphies.Sender.ProviderLicense, Name = _nphies.Sender.ProviderName, Kind = NphiesOrganizationKind.Provider }));
        }
        return list;
    }
}
