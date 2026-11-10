using System.Threading;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.Diagnostics;

/// <summary>
/// Interface for NPHIES diagnostic service
/// Tests all aspects of NPHIES integration including Transport Security
/// </summary>
public interface INphiesDiagnosticService
{
    /// <summary>
    /// Run comprehensive diagnostic tests on NPHIES integration
    /// </summary>
    Task<NphiesDiagnosticResult> RunDiagnosticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Test HTTPS/TLS connection to NPHIES
 /// </summary>
    Task<TransportSecurityResult> TestTransportSecurityAsync(CancellationToken cancellationToken = default);

  /// <summary>
    /// Test OAuth2 authentication
    /// </summary>
    Task<AuthenticationTestResult> TestAuthenticationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Test certificate-based authentication (mTLS) if configured
    /// </summary>
    Task<CertificateTestResult> TestCertificateAuthenticationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Test NPHIES API endpoints availability
    /// </summary>
    Task<EndpointTestResult> TestEndpointsAsync(CancellationToken cancellationToken = default);

  /// <summary>
    /// Test FHIR bundle creation and serialization
    /// </summary>
    Task<FhirTestResult> TestFhirBundleCreationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Comprehensive diagnostic result
/// </summary>
public class NphiesDiagnosticResult
{
    public bool OverallStatus { get; set; }
    public string Summary { get; set; } = string.Empty;
    public TransportSecurityResult TransportSecurity { get; set; } = new();
    public AuthenticationTestResult Authentication { get; set; } = new();
    public CertificateTestResult Certificate { get; set; } = new();
    public EndpointTestResult Endpoints { get; set; } = new();
    public FhirTestResult FhirBundle { get; set; } = new();
    public ConfigurationTestResult Configuration { get; set; } = new();
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Transport Security (TLS/HTTPS) test result
/// </summary>
public class TransportSecurityResult
{
    public bool IsSecure { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public string CipherSuite { get; set; } = string.Empty;
    public bool CertificateValid { get; set; }
    public string CertificateIssuer { get; set; } = string.Empty;
    public DateTime? CertificateExpiry { get; set; }
    public List<string> Issues { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Authentication test result
/// </summary>
public class AuthenticationTestResult
{
    public bool Success { get; set; }
    public bool TokenObtained { get; set; }
    public bool TokenValid { get; set; }
    public int? TokenExpiresInSeconds { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
}

/// <summary>
/// Certificate authentication (mTLS) test result
/// </summary>
public class CertificateTestResult
{
    public bool Enabled { get; set; }
    public bool CertificateFound { get; set; }
    public bool CertificateValid { get; set; }
    public string CertificateSubject { get; set; } = string.Empty;
    public string CertificateIssuer { get; set; } = string.Empty;
    public DateTime? CertificateExpiry { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public bool HasPrivateKey { get; set; }
    public List<string> Issues { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint availability test result
/// </summary>
public class EndpointTestResult
{
    public bool MetadataEndpointAvailable { get; set; }
    public bool EligibilityEndpointAvailable { get; set; }
    public bool ClaimEndpointAvailable { get; set; }
    public Dictionary<string, EndpointStatus> Endpoints { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class EndpointStatus
{
    public bool Available { get; set; }
    public int StatusCode { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// FHIR bundle test result
/// </summary>
public class FhirTestResult
{
    public bool Success { get; set; }
    public bool BundleCreated { get; set; }
    public bool BundleValid { get; set; }
    public int ResourceCount { get; set; }
    public string BundleType { get; set; } = string.Empty;
    public List<string> ValidationErrors { get; set; } = new();
public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Configuration test result
/// </summary>
public class ConfigurationTestResult
{
    public bool IsValid { get; set; }
    public bool BaseUrlConfigured { get; set; }
    public bool AuthConfigured { get; set; }
    public bool TimeoutConfigured { get; set; }
    public bool CertificateConfigured { get; set; }
    public List<string> MissingConfigurations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string Environment { get; set; } = string.Empty;
}
