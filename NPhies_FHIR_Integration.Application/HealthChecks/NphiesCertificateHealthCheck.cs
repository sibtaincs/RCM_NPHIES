using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Infrastructure.Nphies;

namespace NPhies_FHIR_Integration.Application.HealthChecks;

/// <summary>
/// Checks the mutual-TLS client certificate used for NPHIES: present, has a private key and is not about to expire.
/// </summary>
public sealed class NphiesCertificateHealthCheck : IHealthCheck
{
    private static readonly TimeSpan ExpiryWarning = TimeSpan.FromDays(30);

    private readonly INphiesCertificateProvider _certificates;
    private readonly NphiesConfiguration _config;

    public NphiesCertificateHealthCheck(INphiesCertificateProvider certificates, IOptions<NphiesConfiguration> options)
    {
        _certificates = certificates;
        _config = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["Environment"] = _config.Environment,
            ["Source"] = _config.Certificate.Source.ToString(),
        };

        HealthCheckResult result;
        try
        {
            var cert = _certificates.GetClientCertificate();
            if (cert is null)
            {
                result = _config.IsProduction
                    ? HealthCheckResult.Unhealthy("No NPHIES client certificate configured.", data: data)
                    : HealthCheckResult.Degraded("No NPHIES client certificate configured (required for mutual TLS).", data: data);
            }
            else
            {
                data["Subject"] = cert.Subject;
                data["Thumbprint"] = cert.Thumbprint;
                data["NotAfter"] = cert.NotAfter;
                if (!cert.HasPrivateKey)
                    result = HealthCheckResult.Unhealthy("NPHIES client certificate has no private key.", data: data);
                else if (cert.NotAfter <= DateTime.Now)
                    result = HealthCheckResult.Unhealthy("NPHIES client certificate has expired.", data: data);
                else if (cert.NotAfter - DateTime.Now < ExpiryWarning)
                    result = HealthCheckResult.Degraded("NPHIES client certificate expires within 30 days.", data: data);
                else
                    result = HealthCheckResult.Healthy("NPHIES client certificate is valid.", data);
            }
        }
        catch (Exception ex)
        {
            result = HealthCheckResult.Unhealthy("Failed to load the NPHIES client certificate.", ex, data);
        }

        return Task.FromResult(result);
    }
}
