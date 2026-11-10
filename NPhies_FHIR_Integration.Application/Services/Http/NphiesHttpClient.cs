using Hl7.Fhir.Model;
using NPhies_FHIR_Integration.Application.Exceptions;
using NPhies_FHIR_Integration.Application.Nphies.Messaging;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
using NPhies_FHIR_Integration.Infrastructure.Nphies;

namespace NPhies_FHIR_Integration.Application.Services.Http;

/// <summary>
/// Backwards-compatible adapter over <see cref="INphiesMessagingClient"/>. NPHIES only exposes
/// <c>$process-message</c>, so the <paramref name="endpoint"/> argument of <see cref="SubmitBundleAsync"/> is ignored
/// and resource reads are not supported (use poll-request / status-check messages instead).
/// New code should use <see cref="INphiesMessagingClient"/> directly.
/// </summary>
public sealed class NphiesHttpClient : INphiesHttpClient
{
    private readonly INphiesMessagingClient _messaging;
    private readonly INphiesCertificateProvider _certificates;

    public NphiesHttpClient(INphiesMessagingClient messaging, INphiesCertificateProvider certificates)
    {
        _messaging = messaging;
        _certificates = certificates;
    }

    public async Task<Bundle> SubmitBundleAsync(Bundle requestBundle, string endpoint, CancellationToken cancellationToken = default)
    {
        var result = await _messaging.SendAsync(requestBundle, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result.Response.Bundle is { } bundle)
            return bundle;

        var error = result.Response.Errors.FirstOrDefault();
        throw new NphiesException(
            error?.Message ?? "NPHIES did not return a message bundle.",
            error?.Code ?? "NPHIES_NO_BUNDLE");
    }

    public Task<Bundle> GetBundleAsync(string url, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("NPHIES does not support RESTful reads. Send a poll-request message via INphiesMessagingClient.");

    public Task<Hl7.Fhir.Model.Task> GetTaskAsync(string taskId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("NPHIES does not support RESTful reads. Send a status-check or poll-request message via INphiesMessagingClient.");

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default) =>
        System.Threading.Tasks.Task.FromResult(_certificates.GetClientCertificate() is { } cert && cert.NotAfter > DateTime.Now);

    public Task<CapabilityStatement> GetCapabilityStatementAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("NPHIES does not publish a CapabilityStatement on the messaging endpoint.");
}
