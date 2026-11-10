using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>
/// Design-time factory for creating ApplicationDbContext instances
/// This is used by Entity Framework Core migrations during development
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Create a DbContext instance for design-time operations (migrations)
    /// </summary>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
  var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
     
      // Use SQL Server as the default database provider
        // Connection string can be overridden via arguments or environment variables
     string connectionString = "Server=localhost;Database=NPhiesDb;Trusted_Connection=true;TrustServerCertificate=true;";
        
        optionsBuilder.UseSqlServer(connectionString);
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
