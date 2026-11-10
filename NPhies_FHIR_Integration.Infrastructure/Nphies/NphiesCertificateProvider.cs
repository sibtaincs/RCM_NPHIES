using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Infrastructure.Configuration;

namespace NPhies_FHIR_Integration.Infrastructure.Nphies;

/// <summary>Supplies the x.509 client certificate used for NPHIES mutual TLS.</summary>
public interface INphiesCertificateProvider
{
    /// <summary>Returns the client certificate, or null when no certificate is configured (Source = None).</summary>
    X509Certificate2? GetClientCertificate();
}

/// <summary>
/// Loads the client certificate from the Windows/OS certificate store (by thumbprint),
/// a PFX file path, or a base64 PFX (e.g. from a secret store). The certificate is loaded once and cached.
/// </summary>
public sealed class NphiesCertificateProvider : INphiesCertificateProvider
{
    private readonly NphiesCertificateConfiguration _config;
    private readonly ILogger<NphiesCertificateProvider> _logger;
    private readonly Lazy<X509Certificate2?> _certificate;

    public NphiesCertificateProvider(IOptions<NphiesConfiguration> options, ILogger<NphiesCertificateProvider> logger)
    {
        _config = options.Value.Certificate ?? new NphiesCertificateConfiguration();
        _logger = logger;
        _certificate = new Lazy<X509Certificate2?>(Load, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public X509Certificate2? GetClientCertificate() => _certificate.Value;

    private X509Certificate2? Load()
    {
        X509Certificate2? cert = _config.Source switch
        {
            NphiesCertificateSource.None => null,
            NphiesCertificateSource.Store => LoadFromStore(),
            NphiesCertificateSource.Pfx => LoadFromPfx(),
            _ => throw new InvalidOperationException($"Unsupported NPHIES certificate source '{_config.Source}'.")
        };

        if (cert is null)
        {
            _logger.LogWarning("NPHIES client certificate is not configured (Source=None); mutual TLS will fail against NPHIES");
            return null;
        }

        if (!cert.HasPrivateKey)
            throw new InvalidOperationException("The NPHIES client certificate has no private key; mutual TLS requires one.");

        var now = DateTime.Now;
        if (now < cert.NotBefore || now > cert.NotAfter)
            _logger.LogWarning("NPHIES client certificate {Thumbprint} is outside its validity period ({NotBefore:o} - {NotAfter:o})",
                cert.Thumbprint, cert.NotBefore, cert.NotAfter);
        else
            _logger.LogInformation("Loaded NPHIES client certificate {Thumbprint}, expires {NotAfter:o}", cert.Thumbprint, cert.NotAfter);

        return cert;
    }

    private X509Certificate2 LoadFromStore()
    {
        if (string.IsNullOrWhiteSpace(_config.Thumbprint))
            throw new InvalidOperationException("Nphies:Certificate:Thumbprint is required when Source = Store.");

        var storeName = Enum.Parse<StoreName>(_config.StoreName, ignoreCase: true);
        var storeLocation = Enum.Parse<StoreLocation>(_config.StoreLocation, ignoreCase: true);
        var thumbprint = _config.Thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();

        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
        var found = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
        if (found.Count == 0)
            throw new InvalidOperationException($"NPHIES client certificate with thumbprint {thumbprint} was not found in {storeLocation}/{storeName}.");
        return found[0];
    }

    private X509Certificate2 LoadFromPfx()
    {
        const X509KeyStorageFlags flags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;

        if (!string.IsNullOrWhiteSpace(_config.PfxBase64))
            return X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(_config.PfxBase64), _config.PfxPassword, flags);

        if (string.IsNullOrWhiteSpace(_config.PfxPath))
            throw new InvalidOperationException("Nphies:Certificate:PfxPath or PfxBase64 is required when Source = Pfx.");
        if (!File.Exists(_config.PfxPath))
            throw new FileNotFoundException("NPHIES client certificate PFX file not found.", _config.PfxPath);

        return X509CertificateLoader.LoadPkcs12FromFile(_config.PfxPath, _config.PfxPassword, flags);
    }
}
