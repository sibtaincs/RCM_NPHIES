using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Application.Services.Orchestration;
using Microsoft.Extensions.Logging;

namespace NPhies_FHIR_Integration.Application.HealthChecks;

/// <summary>
/// Health check for NPHIES API connectivity
/// </summary>
public class NphiesHealthCheck : IHealthCheck
{
    private readonly INphiesOrchestrationService _orchestrationService;
    private readonly NphiesConfiguration _config;
    private readonly ILogger<NphiesHealthCheck> _logger;

    public NphiesHealthCheck(
      INphiesOrchestrationService orchestrationService,
  IOptions<NphiesConfiguration> config,
        ILogger<NphiesHealthCheck> logger)
    {
        _orchestrationService = orchestrationService ?? throw new ArgumentNullException(nameof(orchestrationService));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing NPHIES health check");

            // Try to perform health check
            var isHealthy = await _orchestrationService.HealthCheckAsync(cancellationToken);

            if (isHealthy)
            {
                var data = new Dictionary<string, object>
          {
              { "BaseUrl", _config.BaseUrl },
                 { "Status", "Connected" },
   { "CheckedAt", DateTime.UtcNow }
   };

                return HealthCheckResult.Healthy("NPHIES API is accessible", data);
            }
            else
            {
                var data = new Dictionary<string, object>
                {
 { "BaseUrl", _config.BaseUrl },
         { "Status", "Unreachable" },
         { "CheckedAt", DateTime.UtcNow }
      };

                return HealthCheckResult.Unhealthy("NPHIES API is not accessible", null, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NPHIES health check failed");

            var data = new Dictionary<string, object>
         {
                { "BaseUrl", _config.BaseUrl },
                { "Status", "Error" },
     { "Error", ex.Message },
     { "CheckedAt", DateTime.UtcNow }
            };

            return HealthCheckResult.Unhealthy(
           "NPHIES health check failed with exception",
              ex,
               data);
        }
    }
}
