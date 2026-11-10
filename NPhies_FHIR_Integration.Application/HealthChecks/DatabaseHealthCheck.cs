using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.HealthChecks;

/// <summary>
/// Health check for database connectivity and availability
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
 private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        ApplicationDbContext context,
 ILogger<DatabaseHealthCheck> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
         _logger.LogDebug("Performing database health check");

     var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Try to connect to database
var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

  stopwatch.Stop();

  if (!canConnect)
            {
   var data = new Dictionary<string, object>
     {
       { "Status", "Disconnected" },
  { "CheckedAt", DateTime.UtcNow },
   { "ResponseTime", $"{stopwatch.ElapsedMilliseconds}ms" }
   };

         return HealthCheckResult.Unhealthy(
         "Cannot connect to database",
              null,
       data);
   }

      // Check for pending migrations
     var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            var hasPendingMigrations = pendingMigrations.Any();

        // Get database name
            var connectionString = _context.Database.GetConnectionString();
            var databaseName = ExtractDatabaseName(connectionString);

            var healthData = new Dictionary<string, object>
       {
           { "Status", "Connected" },
                { "DatabaseName", databaseName ?? "Unknown" },
  { "ResponseTime", $"{stopwatch.ElapsedMilliseconds}ms" },
 { "HasPendingMigrations", hasPendingMigrations },
                { "PendingMigrationsCount", pendingMigrations.Count() },
           { "CheckedAt", DateTime.UtcNow }
            };

       if (hasPendingMigrations)
      {
              healthData.Add("PendingMigrations", pendingMigrations.ToList());
      
     return HealthCheckResult.Degraded(
   $"Database connected but has {pendingMigrations.Count()} pending migration(s)",
        null,
      healthData);
      }

    return HealthCheckResult.Healthy(
      "Database is connected and up to date",
         healthData);
        }
    catch (Exception ex)
  {
            _logger.LogError(ex, "Database health check failed");

            var data = new Dictionary<string, object>
     {
           { "Status", "Error" },
           { "Error", ex.Message },
     { "ErrorType", ex.GetType().Name },
                { "CheckedAt", DateTime.UtcNow }
    };

            return HealthCheckResult.Unhealthy(
     "Database health check failed with exception",
   ex,
           data);
}
    }

    private static string? ExtractDatabaseName(string? connectionString)
    {
   if (string.IsNullOrWhiteSpace(connectionString))
   return null;

        // Try to extract database name from connection string
        var parts = connectionString.Split(';');
        var dbPart = parts.FirstOrDefault(p => 
   p.Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase) ||
            p.Trim().StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase));

        if (dbPart != null)
        {
       var nameValue = dbPart.Split('=');
  if (nameValue.Length > 1)
   {
     return nameValue[1].Trim();
            }
 }

        return null;
    }
}
