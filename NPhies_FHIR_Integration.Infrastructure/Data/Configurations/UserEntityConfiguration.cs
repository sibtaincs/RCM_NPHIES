using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>
/// User Authentication Configuration Extensions
/// </summary>
public static class UserEntityConfiguration
{
    /// <summary>
    /// Configure User, RefreshToken, LoginAttempt, AuditLog, and ApiRateLimitLog entities
    /// </summary>
    public static void ConfigureUserEntities(this ModelBuilder modelBuilder)
    {
        ConfigureUserEntity(modelBuilder);
        ConfigureRefreshTokenEntity(modelBuilder);
        ConfigureLoginAttemptEntity(modelBuilder);
        ConfigureAuditLogEntity(modelBuilder);
        ConfigureApiRateLimitLogEntity(modelBuilder);
    }

    /// <summary>
    /// Configure User entity
    /// </summary>
    private static void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();

        // Primary Key
        entity.HasKey(u => u.Id);

        // Properties
        entity.Property(u => u.Id).HasMaxLength(100);
        entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
        entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
        entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
        entity.Property(u => u.PasswordSalt).IsRequired().HasMaxLength(500);
        entity.Property(u => u.Roles).HasConversion(
          v => string.Join(",", v),
            v => v.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList()
        );
        entity.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        entity.Property(u => u.IsEmailVerified).IsRequired().HasDefaultValue(false);
        entity.Property(u => u.IsMfaEnabled).IsRequired().HasDefaultValue(false);
        entity.Property(u => u.MfaSecret).HasMaxLength(255);
        entity.Property(u => u.IsLocked).IsRequired().HasDefaultValue(false);
        entity.Property(u => u.FailedLoginAttempts).IsRequired().HasDefaultValue(0);
        entity.Property(u => u.LastLoginAt);
        entity.Property(u => u.LockedUntilAt);
        entity.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        entity.Property(u => u.UpdatedAt);
        entity.Property(u => u.DeletedAt);
        entity.Property(u => u.CreatedBy).HasMaxLength(100);
        entity.Property(u => u.UpdatedBy).HasMaxLength(100);
        entity.Property(u => u.OrganizationId).HasMaxLength(100);
        entity.Property(u => u.DepartmentId).HasMaxLength(100);

        // Indexes
        entity.HasIndex(u => u.Username).IsUnique();
        entity.HasIndex(u => u.Email).IsUnique();
        entity.HasIndex(u => u.IsActive);
        entity.HasIndex(u => u.IsLocked);
        entity.HasIndex(u => u.CreatedAt);
        entity.HasIndex(u => u.LastLoginAt);

        // Relationships
        entity.HasMany(u => u.RefreshTokens)
    .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
   .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.LoginAttempts)
     .WithOne(la => la.User)
        .HasForeignKey(la => la.UserId)
 .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.AuditLogs)
        .WithOne(al => al.User)
  .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasMany(u => u.RateLimitLogs)
                 .WithOne(rll => rll.User)
             .HasForeignKey(rll => rll.UserId)
                 .OnDelete(DeleteBehavior.SetNull);
    }

    /// <summary>
    /// Configure RefreshToken entity
    /// </summary>
    private static void ConfigureRefreshTokenEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RefreshToken>();

        // Primary Key
        entity.HasKey(rt => rt.Id);

        // Properties
        entity.Property(rt => rt.Id).HasMaxLength(100);
        entity.Property(rt => rt.UserId).IsRequired().HasMaxLength(100);
        entity.Property(rt => rt.Token).IsRequired().HasMaxLength(500);
        entity.Property(rt => rt.ExpiresAt).IsRequired();
        entity.Property(rt => rt.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        entity.Property(rt => rt.IpAddress).HasMaxLength(50);
        entity.Property(rt => rt.UserAgent).HasMaxLength(500);
        entity.Property(rt => rt.IsRevoked).IsRequired().HasDefaultValue(false);
        entity.Property(rt => rt.RevokedAt);
        entity.Property(rt => rt.RevokedBy).HasMaxLength(100);
        entity.Property(rt => rt.ReplacedByToken).HasMaxLength(500);

        // Indexes
        entity.HasIndex(rt => rt.UserId);
        entity.HasIndex(rt => rt.Token).IsUnique();
        entity.HasIndex(rt => rt.ExpiresAt);
        entity.HasIndex(rt => rt.IsRevoked);

        // Relationships
        entity.HasOne(rt => rt.User)
  .WithMany(u => u.RefreshTokens)
    .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Configure LoginAttempt entity
    /// </summary>
    private static void ConfigureLoginAttemptEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LoginAttempt>();

        // Primary Key
        entity.HasKey(la => la.Id);

        // Properties
        entity.Property(la => la.Id).HasMaxLength(100);
        entity.Property(la => la.UserId).HasMaxLength(100);
        entity.Property(la => la.Username).IsRequired().HasMaxLength(100);
        entity.Property(la => la.IpAddress).IsRequired().HasMaxLength(50);
        entity.Property(la => la.UserAgent).HasMaxLength(500);
        entity.Property(la => la.IsSuccessful).IsRequired().HasDefaultValue(false);
        entity.Property(la => la.FailureReason).HasMaxLength(500);
        entity.Property(la => la.AttemptAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        entity.Property(la => la.DurationMs);

        // Indexes
        entity.HasIndex(la => la.UserId);
        entity.HasIndex(la => la.Username);
        entity.HasIndex(la => la.IpAddress);
        entity.HasIndex(la => la.AttemptAt);
        entity.HasIndex(la => la.IsSuccessful);

        // Relationships
        entity.HasOne(la => la.User)
                 .WithMany(u => u.LoginAttempts)
          .HasForeignKey(la => la.UserId)
           .OnDelete(DeleteBehavior.SetNull);
    }

    /// <summary>
    /// Configure AuditLog entity
    /// </summary>
    private static void ConfigureAuditLogEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditLog>();

        // Primary Key
        entity.HasKey(al => al.Id);

        // Properties
        entity.Property(al => al.Id).HasMaxLength(100);
        entity.Property(al => al.UserId).HasMaxLength(100);
        entity.Property(al => al.Username).HasMaxLength(100);
        entity.Property(al => al.Action).IsRequired().HasMaxLength(255);
        entity.Property(al => al.EntityType).HasMaxLength(100);
        entity.Property(al => al.EntityId).HasMaxLength(100);
        entity.Property(al => al.OldValues).HasColumnType("nvarchar(max)");
        entity.Property(al => al.NewValues).HasColumnType("nvarchar(max)");
        entity.Property(al => al.ChangeDetails).HasColumnType("nvarchar(max)");
        entity.Property(al => al.IpAddress).IsRequired().HasMaxLength(50);
        entity.Property(al => al.UserAgent).HasMaxLength(500);
        entity.Property(al => al.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        entity.Property(al => al.AuditLevel).IsRequired().HasMaxLength(50).HasDefaultValue("Info");
        entity.Property(al => al.Endpoint).HasMaxLength(500);
        entity.Property(al => al.HttpStatusCode);
        entity.Property(al => al.DurationMs);

        // Indexes
        entity.HasIndex(al => al.UserId);
        entity.HasIndex(al => al.Username);
        entity.HasIndex(al => al.Action);
        entity.HasIndex(al => al.EntityType);
        entity.HasIndex(al => al.EntityId);
        entity.HasIndex(al => al.CreatedAt);
        entity.HasIndex(al => al.AuditLevel);
        entity.HasIndex(al => new { al.EntityType, al.EntityId });

        // Relationships
        entity.HasOne(al => al.User)
      .WithMany(u => u.AuditLogs)
 .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    /// <summary>
    /// Configure ApiRateLimitLog entity
    /// </summary>
    private static void ConfigureApiRateLimitLogEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ApiRateLimitLog>();

        // Primary Key
        entity.HasKey(rll => rll.Id);

        // Properties
        entity.Property(rll => rll.Id).HasMaxLength(100);
        entity.Property(rll => rll.UserId).HasMaxLength(100);
        entity.Property(rll => rll.IpAddress).IsRequired().HasMaxLength(50);
        entity.Property(rll => rll.Endpoint).IsRequired().HasMaxLength(500);
        entity.Property(rll => rll.HttpMethod).IsRequired().HasMaxLength(10);
        entity.Property(rll => rll.RequestCount).IsRequired().HasDefaultValue(0);
        entity.Property(rll => rll.MaxRequests).IsRequired().HasDefaultValue(0);
        entity.Property(rll => rll.WindowStart).IsRequired();
        entity.Property(rll => rll.WindowEnd).IsRequired();
        entity.Property(rll => rll.IsRateLimited).IsRequired().HasDefaultValue(false);
        entity.Property(rll => rll.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        entity.Property(rll => rll.ResetAt);

        // Indexes
        entity.HasIndex(rll => rll.UserId);
        entity.HasIndex(rll => rll.IpAddress);
        entity.HasIndex(rll => rll.Endpoint);
        entity.HasIndex(rll => rll.IsRateLimited);
        entity.HasIndex(rll => rll.CreatedAt);
        entity.HasIndex(rll => new { rll.IpAddress, rll.Endpoint });

        // Relationships
        entity.HasOne(rll => rll.User)
 .WithMany(u => u.RateLimitLogs)
            .HasForeignKey(rll => rll.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
