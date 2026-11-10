using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.Services.Diagnostics;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// NPHIES Diagnostics Controller
/// Provides endpoints to test and diagnose NPHIES integration health
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NphiesDiagnosticsController : ControllerBase
{
    private readonly INphiesDiagnosticService _diagnosticService;
    private readonly ILogger<NphiesDiagnosticsController> _logger;

    public NphiesDiagnosticsController(
        INphiesDiagnosticService diagnosticService,
        ILogger<NphiesDiagnosticsController> logger)
    {
        _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
/// Run comprehensive NPHIES integration diagnostics
  /// </summary>
    /// <returns>Diagnostic results including Transport Security, Authentication, and more</returns>
  /// <response code="200">Diagnostics completed successfully</response>
    /// <response code="500">Diagnostics failed with error</response>
    [HttpGet("full")]
    [ProducesResponseType(typeof(NphiesDiagnosticResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NphiesDiagnosticResult>> RunFullDiagnostics(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? Running full NPHIES diagnostics...");

      try
        {
   var result = await _diagnosticService.RunDiagnosticsAsync(cancellationToken);
          
      if (!result.OverallStatus)
 {
           _logger.LogWarning("?? Diagnostics completed with failures");
  return Ok(result); // Still return 200 but with failure details
            }

_logger.LogInformation("? All diagnostics passed");
            return Ok(result);
        }
        catch (Exception ex)
 {
            _logger.LogError(ex, "? Diagnostics failed with exception");
      return StatusCode(500, new
 {
        error = "Diagnostics failed",
   message = ex.Message,
             timestamp = DateTime.UtcNow
            });
      }
    }

    /// <summary>
    /// Test Transport Security (TLS/HTTPS)
    /// </summary>
    /// <returns>Transport security test results</returns>
    /// <response code="200">Test completed successfully</response>
    [HttpGet("transport-security")]
    [ProducesResponseType(typeof(TransportSecurityResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransportSecurityResult>> TestTransportSecurity(CancellationToken cancellationToken)
    {
      _logger.LogInformation("?? Testing transport security...");
        var result = await _diagnosticService.TestTransportSecurityAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Test OAuth2 Authentication
    /// </summary>
    /// <returns>Authentication test results</returns>
    /// <response code="200">Test completed successfully</response>
    [HttpGet("authentication")]
[ProducesResponseType(typeof(AuthenticationTestResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationTestResult>> TestAuthentication(CancellationToken cancellationToken)
    {
   _logger.LogInformation("?? Testing authentication...");
  var result = await _diagnosticService.TestAuthenticationAsync(cancellationToken);
return Ok(result);
    }

  /// <summary>
/// Test Certificate Authentication (mTLS)
    /// </summary>
    /// <returns>Certificate authentication test results</returns>
  /// <response code="200">Test completed successfully</response>
    [HttpGet("certificate")]
    [ProducesResponseType(typeof(CertificateTestResult), StatusCodes.Status200OK)]
 public async Task<ActionResult<CertificateTestResult>> TestCertificate(CancellationToken cancellationToken)
  {
        _logger.LogInformation("?? Testing certificate authentication...");
    var result = await _diagnosticService.TestCertificateAuthenticationAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
  /// Test NPHIES API Endpoints
    /// </summary>
    /// <returns>Endpoint availability test results</returns>
    /// <response code="200">Test completed successfully</response>
    [HttpGet("endpoints")]
    [ProducesResponseType(typeof(EndpointTestResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<EndpointTestResult>> TestEndpoints(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? Testing endpoints...");
        var result = await _diagnosticService.TestEndpointsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Test FHIR Bundle Creation
    /// </summary>
    /// <returns>FHIR bundle test results</returns>
    /// <response code="200">Test completed successfully</response>
    [HttpGet("fhir-bundle")]
    [ProducesResponseType(typeof(FhirTestResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<FhirTestResult>> TestFhirBundle(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? Testing FHIR bundle creation...");
        var result = await _diagnosticService.TestFhirBundleCreationAsync(cancellationToken);
 return Ok(result);
    }

    /// <summary>
    /// Get quick health status
    /// </summary>
    /// <returns>Simple health status</returns>
    /// <response code="200">Service is healthy</response>
    /// <response code="503">Service is unhealthy</response>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealthStatus(CancellationToken cancellationToken)
    {
        try
        {
   var result = await _diagnosticService.RunDiagnosticsAsync(cancellationToken);

    if (result.OverallStatus)
            {
     return Ok(new
     {
           status = "healthy",
    message = "NPHIES integration is operational",
      timestamp = DateTime.UtcNow,
          details = new
        {
     transportSecurity = result.TransportSecurity.IsSecure,
   authentication = result.Authentication.Success,
          configuration = result.Configuration.IsValid
      }
    });
 }
            else
            {
              return StatusCode(503, new
           {
      status = "unhealthy",
 message = "NPHIES integration has issues",
           timestamp = DateTime.UtcNow,
          details = new
          {
        transportSecurity = result.TransportSecurity.IsSecure,
          authentication = result.Authentication.Success,
     configuration = result.Configuration.IsValid
},
    issues = result.TransportSecurity.Issues
        .Concat(result.Configuration.MissingConfigurations)
         .Concat(result.Certificate.Issues)
       .ToList()
             });
        }
    }
        catch (Exception ex)
        {
         _logger.LogError(ex, "Health check failed");
      return StatusCode(503, new
            {
   status = "error",
           message = ex.Message,
       timestamp = DateTime.UtcNow
            });
   }
    }
}
