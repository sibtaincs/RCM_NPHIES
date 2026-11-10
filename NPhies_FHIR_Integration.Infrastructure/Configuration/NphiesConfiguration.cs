namespace NPhies_FHIR_Integration.Infrastructure.Configuration;

/// <summary>
/// NPHIES integration settings (configuration section "Nphies").
/// NPHIES exposes a single FHIR <c>$process-message</c> endpoint secured with mutual TLS.
/// </summary>
public class NphiesConfiguration
{
    public const string SectionName = "Nphies";

    /// <summary>Sandbox or Production.</summary>
    public string Environment { get; set; } = "Sandbox";

    /// <summary>Base URL used in the Sandbox environment.</summary>
    public string SandboxBaseUrl { get; set; } = "https://hsb.nphies.sa/";

    /// <summary>Base URL used in the Production environment.</summary>
    public string ProductionBaseUrl { get; set; } = "https://hsb.nphies.sa/";

    private string? _baseUrl;

    /// <summary>
    /// Explicit base URL override. When empty, resolves from <see cref="Environment"/>.
    /// </summary>
    public string BaseUrl
    {
        get => !string.IsNullOrWhiteSpace(_baseUrl) ? _baseUrl! : (IsProduction ? ProductionBaseUrl : SandboxBaseUrl);
        set => _baseUrl = value;
    }

    /// <summary>Relative path of the FHIR messaging operation.</summary>
    public string ProcessMessagePath { get; set; } = "$process-message";

    /// <summary>Absolute URL of the <c>$process-message</c> endpoint.</summary>
    public string ProcessMessageUrl => BaseUrl.TrimEnd('/') + "/" + ProcessMessagePath.TrimStart('/');

    /// <summary>Request timeout in seconds (NPHIES real-time session limit is 60 s).</summary>
    public int Timeout { get; set; } = 60;

    /// <summary>Retry attempts for idempotent messages only (poll, status-check).</summary>
    public int RetryCount { get; set; } = 3;

    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>Logs message metadata (never PHI payloads).</summary>
    public bool EnableDetailedLogging { get; set; } = true;

    /// <summary>Persist raw request/response bundles to the audit store.</summary>
    public bool EnableMessageAudit { get; set; } = true;

    /// <summary>
    /// In the audit, replace embedded attachment data that is held in the claim document store with a
    /// <c>nphies-doc:{id}:{sha256}</c> reference so PDFs are not stored twice.
    /// </summary>
    public bool AuditStripStoredAttachments { get; set; } = true;

    /// <summary>NPHIES maximum message size (10 MB).</summary>
    public long MaxMessageSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>NPHIES maximum number of claims in one batch.</summary>
    public int BatchMaxClaims { get; set; } = 200;

    /// <summary>NPHIES maximum number of messages returned by one poll.</summary>
    public int PollMaxCount { get; set; } = 100;

    /// <summary>Provider (sender) identity used to stamp outgoing MessageHeaders.</summary>
    public NphiesSenderConfiguration Sender { get; set; } = new();

    /// <summary>mTLS client certificate.</summary>
    public NphiesCertificateConfiguration Certificate { get; set; } = new();

    /// <summary>Legacy per-resource paths. NPHIES ignores these; everything goes to <see cref="ProcessMessagePath"/>.</summary>
    public EndpointsConfiguration Endpoints { get; set; } = new();

    /// <summary>Legacy OAuth settings (unused by NPHIES, which authenticates with mTLS).</summary>
    public AuthConfiguration Auth { get; set; } = new();

    public PollingConfiguration Polling { get; set; } = new();

    /// <summary>Local NPHIES IG profile validation of outgoing bundles.</summary>
    public NphiesProfileValidationConfiguration ProfileValidation { get; set; } = new();

    public bool IsSandbox => !IsProduction;

    public bool IsProduction => string.Equals(Environment, "Production", StringComparison.OrdinalIgnoreCase);
}

public class NphiesSenderConfiguration
{
    /// <summary>Provider license issued by NPHIES (sender identifier value).</summary>
    public string ProviderLicense { get; set; } = string.Empty;

    /// <summary>Provider base URL used for MessageHeader.source.endpoint and entry fullUrls.</summary>
    public string ProviderBaseUrl { get; set; } = "http://provider.com";

    /// <summary>
    /// Optional MessageHeader.source.endpoint override. When empty the builders use
    /// <c>http://nphies.sa/license/provider-license/{ProviderLicense}</c>.
    /// </summary>
    public string? SourceEndpoint { get; set; }

    /// <summary>Provider organization display name used when the caller does not supply one.</summary>
    public string? ProviderName { get; set; }
}

public enum NphiesCertificateSource
{
    None = 0,
    Store = 1,
    Pfx = 2
}

public class NphiesCertificateConfiguration
{
    public NphiesCertificateSource Source { get; set; } = NphiesCertificateSource.None;

    /// <summary>Thumbprint when <see cref="Source"/> is Store.</summary>
    public string? Thumbprint { get; set; }

    /// <summary>X509 StoreName value (default My).</summary>
    public string StoreName { get; set; } = "My";

    /// <summary>CurrentUser or LocalMachine.</summary>
    public string StoreLocation { get; set; } = "CurrentUser";

    /// <summary>PFX path when <see cref="Source"/> is Pfx.</summary>
    public string? PfxPath { get; set; }

    /// <summary>PFX password. Supply via user-secrets or environment (Nphies__Certificate__PfxPassword).</summary>
    public string? PfxPassword { get; set; }

    /// <summary>Optional base64 PFX content (e.g. from a key vault) instead of a file path.</summary>
    public string? PfxBase64 { get; set; }
}

public class EndpointsConfiguration
{
    public string Eligibility { get; set; } = "$process-message";
    public string Claim { get; set; } = "$process-message";
    public string PreAuth { get; set; } = "$process-message";
    public string Poll { get; set; } = "$process-message";
    public string HealthCheck { get; set; } = "metadata";
}

public class AuthConfiguration
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public int TokenCacheDurationMinutes { get; set; } = 55;
}

public enum NphiesProfileValidationMode
{
    /// <summary>Only the NPHIES business rules run.</summary>
    Off = 0,
    /// <summary>Profile findings are reported as warnings (preview, health, audit) but never block a send.</summary>
    Warn = 1,
    /// <summary>Profile errors block the send like business-rule errors.</summary>
    Enforce = 2,
}

public class NphiesProfileValidationConfiguration
{
    public NphiesProfileValidationMode Mode { get; set; } = NphiesProfileValidationMode.Warn;

    /// <summary>Optional path to a newer NPHIES IG package.tgz; when empty the package embedded in the build is used.</summary>
    public string? PackagePath { get; set; }
}

public class PollingConfiguration
{
    public int IntervalSeconds { get; set; } = 3;
    public int MaxDurationMinutes { get; set; } = 5;
    public bool ExponentialBackoff { get; set; } = true;
    public int MaxRetries { get; set; } = 100;
    public int InitialBackoffSeconds { get; set; } = 2;
    public int MaxBackoffSeconds { get; set; } = 30;
}
