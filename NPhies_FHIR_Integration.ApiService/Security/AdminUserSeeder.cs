using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Services;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.Security;

/// <summary>
/// Seeds the bootstrap administrator so the system can be accessed after a fresh deploy.
/// Every other account is then created through the admin UI / API.
/// </summary>
public static class AdminUserSeeder
{
    private const string DefaultAdminUsername = "admin";
    private const string DefaultAdminEmail = "admin@nphies.local";

    /// <summary>
    /// Creates the bootstrap admin if no active administrator exists.
    /// The password is read from configuration key <c>Seed:AdminPassword</c>
    /// and falls back to a development default.
    /// </summary>
    public static async System.Threading.Tasks.Task SeedAsync(
    ApplicationDbContext dbContext,
        IPasswordHashingService passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        try
     {
          // If any active admin already exists, do nothing.
            var existingUsers = await dbContext.Users
       .Where(u => u.DeletedAt == null && u.IsActive)
        .Select(u => new { u.Id, u.Roles })
    .ToListAsync();

            if (existingUsers.Any(u => u.Roles.Contains(SecurityConstants.ADMIN_ROLE)))
            {
     logger.LogInformation("?? Admin user already present - skipping admin seed.");
 return;
          }

       // Don't collide with an existing (non-admin or inactive) 'admin' username.
   var usernameTaken = await dbContext.Users
     .AnyAsync(u => u.Username == DefaultAdminUsername);

  if (usernameTaken)
    {
       logger.LogWarning("?? Username '{Username}' exists but is not an active admin. Skipping seed.",
          DefaultAdminUsername);
   return;
     }

    var password = configuration["Seed:AdminPassword"] ?? "Admin@12345!";
      var (hash, salt) = passwordHasher.HashPassword(password);

     dbContext.Users.Add(new User
       {
   Username = DefaultAdminUsername,
 Email = DefaultAdminEmail,
      FirstName = "System",
   LastName = "Administrator",
  PasswordHash = hash,
    PasswordSalt = salt,
   Roles = new List<string> { SecurityConstants.ADMIN_ROLE },
      IsActive = true,
                IsEmailVerified = true,
     CreatedBy = "seeder"
        });

      await dbContext.SaveChangesAsync();

     logger.LogInformation("? Bootstrap admin created. Username: '{Username}'.", DefaultAdminUsername);
    logger.LogWarning("?? Change the seeded admin password immediately after first login.");
        }
        catch (Exception ex)
        {
       logger.LogError(ex, "? Failed to seed the bootstrap admin user.");
        }
    }
}
