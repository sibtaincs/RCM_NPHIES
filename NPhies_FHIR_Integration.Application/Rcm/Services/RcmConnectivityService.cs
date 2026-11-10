using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Sandbox;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Infrastructure.Nphies;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

/// <summary>
/// Pre-flight check for the live NPHIES connection: configuration, mTLS client certificate, DNS, TLS handshake with the
/// client certificate and (optionally) a real poll-request round trip whose results are ingested normally.
/// </summary>
public interface IRcmConnectivityService
{
    Task<ConnectivityReportDto> CheckAsync(bool includePoll, CancellationToken ct = default);
}

public sealed class RcmConnectivityService : IRcmConnectivityService
{
    private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ExpiryWarning = TimeSpan.FromDays(30);

    private readonly NphiesConfiguration _config;
    private readonly NphiesSandboxOptions _sandbox;
    private readonly INphiesCertificateProvider _certificates;
    private readonly IRcmPollService _poll;

    public RcmConnectivityService(IOptions<NphiesConfiguration> config, IOptions<NphiesSandboxOptions> sandbox,
        INphiesCertificateProvider certificates, IRcmPollService poll)
    {
        _config = config.Value;
        _sandbox = sandbox.Value;
        _certificates = certificates;
        _poll = poll;
    }

    public async Task<ConnectivityReportDto> CheckAsync(bool includePoll, CancellationToken ct = default)
    {
        var report = new ConnectivityReportDto
        {
            Environment = _config.Environment,
            Endpoint = _config.ProcessMessageUrl,
            SandboxSimulator = _sandbox.Enabled,
            ProfileValidationMode = _config.ProfileValidation.Mode.ToString(),
        };

        var uri = CheckConfiguration(report);
        var cert = CheckCertificate(report);
        IPAddress[]? addresses = uri is null ? null : await CheckDnsAsync(report, uri, ct);
        if (uri is not null && addresses is { Length: > 0 }) await CheckTlsAsync(report, uri, cert, ct);
        else report.Checks.Add(Skipped("tls-handshake", "No resolvable endpoint."));

        if (!includePoll) report.Checks.Add(Skipped("poll", "Not requested (add ?poll=true to send a real poll-request)."));
        else await CheckPollAsync(report, ct);

        report.Status = report.Checks.Any(c => c.Status == ConnectivityCheckDto.Fail) ? ConnectivityCheckDto.Fail
            : report.Checks.Any(c => c.Status == ConnectivityCheckDto.Warn) ? ConnectivityCheckDto.Warn
            : ConnectivityCheckDto.Pass;
        return report;
    }

    private Uri? CheckConfiguration(ConnectivityReportDto report)
    {
        var problems = new List<string>();
        var warnings = new List<string>();
        Uri.TryCreate(_config.ProcessMessageUrl, UriKind.Absolute, out var uri);
        if (uri is null) problems.Add($"Process-message URL '{_config.ProcessMessageUrl}' is not an absolute URL.");
        else if (uri.Scheme != Uri.UriSchemeHttps) problems.Add("Process-message URL must use https.");
        if (string.IsNullOrWhiteSpace(_config.Sender.ProviderLicense)) problems.Add("Nphies:Sender:ProviderLicense is not configured.");
        if (_sandbox.Enabled) warnings.Add("Nphies:Sandbox:Enabled is true - messages are answered by the in-process simulator, not NPHIES.");
        if (_config.ProfileValidation.Mode != NphiesProfileValidationMode.Enforce)
            warnings.Add($"Profile validation mode is {_config.ProfileValidation.Mode}; Enforce is recommended for certification runs.");
        if (!_config.EnableMessageAudit) warnings.Add("Message audit is disabled - test-case evidence cannot be exported.");

        report.Checks.Add(new ConnectivityCheckDto
        {
            Name = "configuration",
            Status = problems.Count > 0 ? ConnectivityCheckDto.Fail : warnings.Count > 0 ? ConnectivityCheckDto.Warn : ConnectivityCheckDto.Pass,
            Detail = string.Join(" ", problems.Concat(warnings).DefaultIfEmpty($"Endpoint {_config.ProcessMessageUrl}, provider license {_config.Sender.ProviderLicense}.")),
        });
        return problems.Count > 0 && uri is null ? null : uri;
    }

    private X509Certificate2? CheckCertificate(ConnectivityReportDto report)
    {
        var check = new ConnectivityCheckDto { Name = "client-certificate" };
        report.Checks.Add(check);
        X509Certificate2? cert;
        try
        {
            cert = _certificates.GetClientCertificate();
        }
        catch (Exception ex)
        {
            check.Status = ConnectivityCheckDto.Fail;
            check.Detail = $"Failed to load ({_config.Certificate.Source}): {ex.Message}";
            return null;
        }

        if (cert is null)
        {
            check.Status = ConnectivityCheckDto.Fail;
            check.Detail = "No client certificate configured (Nphies:Certificate). NPHIES requires mutual TLS.";
            return null;
        }

        report.Certificate = new ConnectivityCertificateDto
        {
            Subject = cert.Subject,
            Issuer = cert.Issuer,
            Thumbprint = cert.Thumbprint,
            NotBefore = cert.NotBefore.ToUniversalTime(),
            NotAfter = cert.NotAfter.ToUniversalTime(),
            HasPrivateKey = cert.HasPrivateKey,
            DaysToExpiry = (int)Math.Floor((cert.NotAfter - DateTime.Now).TotalDays),
        };
        if (!cert.HasPrivateKey) { check.Status = ConnectivityCheckDto.Fail; check.Detail = "Certificate has no private key."; }
        else if (cert.NotAfter <= DateTime.Now) { check.Status = ConnectivityCheckDto.Fail; check.Detail = $"Certificate expired on {cert.NotAfter:yyyy-MM-dd}."; }
        else if (cert.NotBefore > DateTime.Now) { check.Status = ConnectivityCheckDto.Fail; check.Detail = $"Certificate is not valid before {cert.NotBefore:yyyy-MM-dd}."; }
        else if (cert.NotAfter - DateTime.Now < ExpiryWarning) { check.Status = ConnectivityCheckDto.Warn; check.Detail = $"Certificate expires on {cert.NotAfter:yyyy-MM-dd}."; }
        else { check.Status = ConnectivityCheckDto.Pass; check.Detail = $"{cert.Subject}, valid until {cert.NotAfter:yyyy-MM-dd}."; }
        return cert;
    }

    private static async Task<IPAddress[]?> CheckDnsAsync(ConnectivityReportDto report, Uri uri, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(NetworkTimeout);
            var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cts.Token);
            report.Checks.Add(new ConnectivityCheckDto
            {
                Name = "dns",
                Status = addresses.Length > 0 ? ConnectivityCheckDto.Pass : ConnectivityCheckDto.Fail,
                Detail = $"{uri.DnsSafeHost} -> {string.Join(", ", addresses.Select(a => a.ToString()))}",
                DurationMs = sw.ElapsedMilliseconds,
            });
            return addresses;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            report.Checks.Add(new ConnectivityCheckDto { Name = "dns", Status = ConnectivityCheckDto.Fail, Detail = $"{uri.DnsSafeHost}: {ex.Message}", DurationMs = sw.ElapsedMilliseconds });
            return null;
        }
    }

    private static async Task CheckTlsAsync(ConnectivityReportDto report, Uri uri, X509Certificate2? cert, CancellationToken ct)
    {
        var check = new ConnectivityCheckDto { Name = "tls-handshake" };
        report.Checks.Add(check);
        var sw = Stopwatch.StartNew();
        string? chainErrors = null;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(NetworkTimeout);
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(uri.DnsSafeHost, uri.Port, cts.Token);
            await using var ssl = new SslStream(tcp.GetStream(), leaveInnerStreamOpen: false);
            var options = new SslClientAuthenticationOptions
            {
                TargetHost = uri.DnsSafeHost,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ClientCertificates = cert is null ? null : new X509CertificateCollection { cert },
                RemoteCertificateValidationCallback = (_, _, _, errors) =>
                {
                    if (errors != SslPolicyErrors.None) chainErrors = errors.ToString();
                    return errors == SslPolicyErrors.None;
                },
            };
            await ssl.AuthenticateAsClientAsync(options, cts.Token);

            var server = ssl.RemoteCertificate is null ? null : new X509Certificate2(ssl.RemoteCertificate);
            check.Status = cert is null ? ConnectivityCheckDto.Warn : ConnectivityCheckDto.Pass;
            check.Detail = $"{ssl.SslProtocol} with {uri.DnsSafeHost}:{uri.Port}; server certificate {server?.Subject} valid until {server?.NotAfter:yyyy-MM-dd}."
                + (cert is null ? " No client certificate was presented." : " Client certificate offered" + (ssl.IsMutuallyAuthenticated ? " and accepted." : " (server did not request it during the handshake)."));
        }
        catch (Exception ex) when (ex is SocketException or IOException or AuthenticationException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            check.Status = ConnectivityCheckDto.Fail;
            check.Detail = (ex is OperationCanceledException ? $"Timed out after {NetworkTimeout.TotalSeconds:0} s" : ex.Message)
                + (chainErrors is null ? "" : $" (server certificate: {chainErrors})");
        }
        check.DurationMs = sw.ElapsedMilliseconds;
    }

    private async Task CheckPollAsync(ConnectivityReportDto report, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var check = new ConnectivityCheckDto { Name = "poll" };
        report.Checks.Add(check);
        try
        {
            var result = await _poll.PollOnceAsync(null, ct);
            report.Poll = result;
            check.Status = result.Errors.Count > 0 ? ConnectivityCheckDto.Fail : result.PollRequests == 0 ? ConnectivityCheckDto.Warn : ConnectivityCheckDto.Pass;
            check.Detail = result.Errors.Count > 0
                ? string.Join(" ", result.Errors)
                : result.PollRequests == 0 ? "No provider was polled: " + string.Join(" ", result.Notes)
                : $"{result.PollRequests} poll-request(s) answered; {result.MessagesReceived} message(s) received and ingested.";
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            check.Status = ConnectivityCheckDto.Fail;
            check.Detail = $"{ex.GetType().Name}: {ex.Message}";
        }
        check.DurationMs = sw.ElapsedMilliseconds;
    }

    private static ConnectivityCheckDto Skipped(string name, string detail) =>
        new() { Name = name, Status = ConnectivityCheckDto.Skipped, Detail = detail };
}
