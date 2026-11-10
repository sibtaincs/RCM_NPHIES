using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Infrastructure.Configuration;

namespace NPhies_FHIR_Integration.Infrastructure.Nphies;

/// <summary>A serialized NPHIES message ready to be posted to <c>$process-message</c>.</summary>
public sealed record NphiesTransportRequest(
    string Json,
    string MessageId,
    string BundleId,
    string EventCode,
    bool IsIdempotent);

/// <summary>Raw transport result.</summary>
public sealed record NphiesTransportResponse(
    HttpStatusCode StatusCode,
    string Body,
    string Endpoint,
    TimeSpan Duration);

/// <summary>Low-level HTTP transport for NPHIES (mTLS handled by the primary HTTP handler).</summary>
public interface INphiesTransport
{
    Task<NphiesTransportResponse> SendAsync(NphiesTransportRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Transport failure. <see cref="RequestMayHaveBeenDelivered"/> tells callers whether a resend could duplicate it.</summary>
public sealed class NphiesTransportException : Exception
{
    public NphiesTransportException(string message, bool isTimeout, bool requestMayHaveBeenDelivered, Exception? inner = null)
        : base(message, inner)
    {
        IsTimeout = isTimeout;
        RequestMayHaveBeenDelivered = requestMayHaveBeenDelivered;
    }

    public bool IsTimeout { get; }
    public bool RequestMayHaveBeenDelivered { get; }
}

/// <summary>
/// Posts FHIR message bundles to the NPHIES <c>$process-message</c> endpoint.
/// Enforces the 10 MB message limit and the 60 s NPHIES session timeout. Never logs payloads.
/// </summary>
public sealed class NphiesTransport : INphiesTransport
{
    public const string HttpClientName = "NphiesTransport";
    public static readonly HttpRequestOptionsKey<bool> IdempotentOption = new("nphies.idempotent");
    private const string FhirJson = "application/fhir+json";

    private readonly HttpClient _httpClient;
    private readonly NphiesConfiguration _config;
    private readonly ILogger<NphiesTransport> _logger;

    public NphiesTransport(HttpClient httpClient, IOptions<NphiesConfiguration> options, ILogger<NphiesTransport> logger)
    {
        _httpClient = httpClient;
        _config = options.Value;
        _logger = logger;
    }

    public async Task<NphiesTransportResponse> SendAsync(NphiesTransportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bytes = Encoding.UTF8.GetBytes(request.Json);
        if (bytes.LongLength > _config.MaxMessageSizeBytes)
            throw new NphiesTransportException(
                $"NPHIES message {request.MessageId} is {bytes.LongLength} bytes, above the {_config.MaxMessageSizeBytes} byte limit.",
                isTimeout: false, requestMayHaveBeenDelivered: false);

        var endpoint = _config.ProcessMessageUrl;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Content = new ByteArrayContent(bytes);
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(FhirJson) { CharSet = "utf-8" };
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(FhirJson));
        httpRequest.Headers.TryAddWithoutValidation("X-Correlation-Id", request.MessageId);
        httpRequest.Options.Set(IdempotentOption, request.IsIdempotent);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _config.Timeout)));

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("NPHIES send {EventCode} message {MessageId} bundle {BundleId} ({Size} bytes)",
            request.EventCode, request.MessageId, request.BundleId, bytes.LongLength);
        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, timeoutCts.Token)
                .ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            sw.Stop();

            _logger.LogInformation("NPHIES {EventCode} message {MessageId} returned HTTP {Status} in {Elapsed} ms ({Size} bytes)",
                request.EventCode, request.MessageId, (int)response.StatusCode, sw.ElapsedMilliseconds, body.Length);

            return new NphiesTransportResponse(response.StatusCode, body, endpoint, sw.Elapsed);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("NPHIES {EventCode} message {MessageId} timed out after {Elapsed} ms",
                request.EventCode, request.MessageId, sw.ElapsedMilliseconds);
            throw new NphiesTransportException(
                $"NPHIES did not respond within {_config.Timeout} seconds.", isTimeout: true, requestMayHaveBeenDelivered: true, ex);
        }
        catch (HttpRequestException ex)
        {
            var notDelivered = ex.HttpRequestError is HttpRequestError.ConnectionError or HttpRequestError.NameResolutionError
                or HttpRequestError.SecureConnectionError;
            _logger.LogWarning("NPHIES {EventCode} message {MessageId} transport error {Error}",
                request.EventCode, request.MessageId, ex.HttpRequestError);
            throw new NphiesTransportException(
                $"NPHIES transport error ({ex.HttpRequestError}): {ex.Message}", isTimeout: false,
                requestMayHaveBeenDelivered: !notDelivered, ex);
        }
    }
}
