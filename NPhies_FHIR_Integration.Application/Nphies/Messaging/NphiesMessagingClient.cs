using System.Diagnostics;
using System.Net;
using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Exceptions;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NphiesMessageLog = NPhies_FHIR_Integration.Domain.Entities.NphiesMessageLog;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Infrastructure.Nphies;

namespace NPhies_FHIR_Integration.Application.Nphies.Messaging;

/// <summary>Per-call options for <see cref="INphiesMessagingClient.SendAsync"/>.</summary>
public sealed class NphiesSendOptions
{
    /// <summary>Business correlation id (claim number, eligibility id...) stored with the audit record.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Skip local validation (not recommended; NPHIES will still reject invalid messages).</summary>
    public bool SkipValidation { get; init; }

    /// <summary>Send even when validation reports errors (issues are still returned).</summary>
    public bool SendWhenInvalid { get; init; }
}

/// <summary>Result of a <c>$process-message</c> exchange.</summary>
public sealed class NphiesMessageResult
{
    public required string MessageId { get; init; }
    public required string BundleId { get; init; }
    public required string EventCode { get; init; }
    public required NphiesResponse Response { get; init; }
    public required string RawRequest { get; init; }
    public string? RawResponse { get; init; }
    public HttpStatusCode? HttpStatus { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<NphiesValidationIssue> ValidationIssues { get; init; } = Array.Empty<NphiesValidationIssue>();

    /// <summary>True when NPHIES accepted the message and returned no errors.</summary>
    public bool IsSuccess => Response.IsSuccess;
}

/// <summary>
/// Sends NPHIES FHIR message bundles to <c>$process-message</c> over mutual TLS: validates, serializes,
/// posts, audits the raw request/response and parses the response.
/// </summary>
public interface INphiesMessagingClient
{
    /// <exception cref="NphiesValidationException">The bundle failed local validation.</exception>
    /// <exception cref="NphiesTimeoutException">NPHIES did not answer within the session timeout.</exception>
    /// <exception cref="NphiesException">Transport failure (connection, TLS, size limit, non-FHIR response).</exception>
    Task<NphiesMessageResult> SendAsync(Bundle message, NphiesSendOptions? options = null, CancellationToken cancellationToken = default);
}

public sealed class NphiesMessagingClient : INphiesMessagingClient
{
    private static readonly FhirJsonSerializer Serializer = new(new SerializerSettings { Pretty = false });

    private readonly INphiesTransport _transport;
    private readonly INphiesBundleValidator _validator;
    private readonly INphiesResponseParser _parser;
    private readonly INphiesMessageAuditStore _audit;
    private readonly NphiesConfiguration _config;
    private readonly ILogger<NphiesMessagingClient> _logger;

    public NphiesMessagingClient(
        INphiesTransport transport,
        INphiesBundleValidator validator,
        INphiesResponseParser parser,
        INphiesMessageAuditStore audit,
        IOptions<NphiesConfiguration> options,
        ILogger<NphiesMessagingClient> logger)
    {
        _transport = transport;
        _validator = validator;
        _parser = parser;
        _audit = audit;
        _config = options.Value;
        _logger = logger;
    }

    public async Task<NphiesMessageResult> SendAsync(Bundle message, NphiesSendOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        options ??= new NphiesSendOptions();

        var header = message.Entry.FirstOrDefault()?.Resource as MessageHeader
                     ?? throw new NphiesValidationException("NPHIES message bundle must start with a MessageHeader.", new List<string> { "BV-00126" });
        var eventCode = (header.Event as Coding)?.Code ?? string.Empty;
        var messageId = header.Id ?? string.Empty;
        var bundleId = message.Id ?? string.Empty;

        var json = Serializer.SerializeToString(message);
        IReadOnlyList<NphiesValidationIssue> issues = Array.Empty<NphiesValidationIssue>();
        if (!options.SkipValidation)
        {
            var validation = _validator.Validate(message, Encoding.UTF8.GetByteCount(json));
            issues = validation.Issues;
            if (!validation.IsValid && !options.SendWhenInvalid)
            {
                _logger.LogWarning("NPHIES {EventCode} message {MessageId} failed local validation: {Rules}",
                    eventCode, messageId, string.Join(",", validation.Errors.Select(e => e.RuleId).Distinct()));
                throw new NphiesValidationException(
                    $"NPHIES {eventCode} message failed validation.",
                    "NPHIES_VALIDATION",
                    validation.Errors.Select(e => $"{e.RuleId} {e.Expression}: {e.Message}").ToList());
            }
        }

        var log = new NphiesMessageLog
        {
            MessageId = messageId,
            BundleId = bundleId,
            CorrelationId = options.CorrelationId,
            EventCode = eventCode,
            Endpoint = _config.ProcessMessageUrl,
            RequestJson = json,
        };

        var request = new NphiesTransportRequest(json, messageId, bundleId, eventCode, NphiesMessageEvents.IsIdempotent(eventCode));
        var stopwatch = Stopwatch.StartNew();
        NphiesTransportResponse response;
        try
        {
            response = await _transport.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (NphiesTransportException ex)
        {
            log.Error = ex.Message;
            log.DurationMs = stopwatch.ElapsedMilliseconds;
            await AuditAsync(log).ConfigureAwait(false);
            if (ex.IsTimeout)
                throw new NphiesTimeoutException($"NPHIES did not respond to {eventCode} message {messageId} in time. Use status-check/poll before resending.", ex);
            throw new NphiesException($"NPHIES transport failure for {eventCode} message {messageId}: {ex.Message}", ex);
        }

        log.HttpStatus = (int)response.StatusCode;
        log.ResponseJson = response.Body;
        log.DurationMs = (long)response.Duration.TotalMilliseconds;

        var parsed = string.IsNullOrWhiteSpace(response.Body)
            ? new NphiesResponse
            {
                Errors = { new NphiesError(((int)response.StatusCode).ToString(), $"Empty NPHIES response (HTTP {(int)response.StatusCode}).", null, NphiesErrorSource.Transport) },
            }
            : _parser.Parse(response.Body);

        if (parsed.RequestMessageId is { } requestId && requestId != messageId)
        {
            _logger.LogWarning("NPHIES response {ResponseMessageId} correlates to {RequestId}, expected {MessageId}",
                parsed.MessageId, requestId, messageId);
        }

        log.ResponseEventCode = parsed.EventCode;
        log.ResponseCode = parsed.ResponseCode;
        log.ResponseMessageId = parsed.MessageId;
        if (parsed.Errors.Count > 0)
            log.Error = string.Join("; ", parsed.Errors.Select(e => e.Code).Distinct());
        await AuditAsync(log).ConfigureAwait(false);

        _logger.LogInformation(
            "NPHIES {EventCode} message {MessageId} -> {ResponseEvent} {ResponseCode} HTTP {Status} in {Duration} ms with {ErrorCount} errors",
            eventCode, messageId, parsed.EventCode, parsed.ResponseCode, (int)response.StatusCode,
            (long)response.Duration.TotalMilliseconds, parsed.Errors.Count);

        return new NphiesMessageResult
        {
            MessageId = messageId,
            BundleId = bundleId,
            EventCode = eventCode,
            Response = parsed,
            RawRequest = json,
            RawResponse = response.Body,
            HttpStatus = response.StatusCode,
            Duration = response.Duration,
            ValidationIssues = issues,
        };
    }

    private async System.Threading.Tasks.Task AuditAsync(NphiesMessageLog log)
    {
        if (!_config.EnableMessageAudit) return;
        try
        {
            // Audit must survive caller cancellation so we always keep the exchange on record.
            await _audit.SaveAsync(log, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist NPHIES audit record for message {MessageId}", log.MessageId);
        }
    }
}
