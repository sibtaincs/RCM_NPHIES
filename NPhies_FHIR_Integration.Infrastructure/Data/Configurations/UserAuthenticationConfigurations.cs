using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;
using System.Text.Json;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
   entity.HasKey(u => u.Id);
    entity.Property(u => u.Id).HasMaxLength(100);
        entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
   entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
        entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
  entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
   entity.Property(u => u.PasswordSalt).IsRequired().HasMaxLength(500);
      
        // ? FIXED: Proper JSON serialization for List<string> Roles
      entity.Property(u => u.Roles)
    .HasConversion(
         v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
          .HasColumnType("nvarchar(max)")
   .HasColumnName("Roles");
        
 entity.Property(u => u.IsActive).IsRequired();
        entity.Property(u => u.IsEmailVerified).IsRequired();
     entity.Property(u => u.IsMfaEnabled).IsRequired();
        entity.Property(u => u.MfaSecret).HasMaxLength(255);
    entity.Property(u => u.IsLocked).IsRequired();
        entity.Property(u => u.FailedLoginAttempts).IsRequired();
    entity.Property(u => u.LastLoginAt);
        entity.Property(u => u.LockedUntilAt);
      entity.Property(u => u.CreatedAt).IsRequired();
        entity.Property(u => u.UpdatedAt);
        entity.Property(u => u.DeletedAt);
        entity.Property(u => u.CreatedBy).HasMaxLength(100);
        entity.Property(u => u.UpdatedBy).HasMaxLength(100);
        entity.Property(u => u.OrganizationId).HasMaxLength(100);
        entity.Property(u => u.DepartmentId).HasMaxLength(100);

        entity.HasIndex(u => u.Username).IsUnique();
        entity.HasIndex(u => u.Email).IsUnique();
        entity.HasIndex(u => u.IsActive);
        entity.HasIndex(u => u.IsLocked);
 entity.HasIndex(u => u.OrganizationId);
        entity.HasIndex(u => u.DepartmentId);
      entity.HasIndex(u => u.CreatedAt);

    entity.HasMany(u => u.AuditLogs).WithOne(a => a.User).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
     entity.HasMany(u => u.RefreshTokens).WithOne(r => r.User).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(u => u.LoginAttempts).WithOne(l => l.User).HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(u => u.RateLimitLogs).WithOne(r => r.User).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.HasKey(r => r.Id);
    entity.Property(r => r.Id).HasMaxLength(100);
     entity.Property(r => r.UserId).IsRequired().HasMaxLength(100);
      entity.Property(r => r.Token).IsRequired().HasMaxLength(500);
        entity.Property(r => r.ExpiresAt).IsRequired();
    entity.Property(r => r.CreatedAt).IsRequired();
 entity.Property(r => r.IpAddress).HasMaxLength(50);
     entity.Property(r => r.UserAgent).HasMaxLength(500);
        entity.Property(r => r.IsRevoked).IsRequired();
        entity.Property(r => r.RevokedAt);
        entity.Property(r => r.RevokedBy).HasMaxLength(100);
        entity.Property(r => r.ReplacedByToken).HasMaxLength(500);

        entity.HasIndex(r => r.Token).IsUnique();
        entity.HasIndex(r => r.UserId);
   entity.HasIndex(r => r.ExpiresAt);
        entity.HasIndex(r => r.IsRevoked);
   entity.HasIndex(r => r.CreatedAt);

   entity.HasOne(r => r.User).WithMany(u => u.RefreshTokens).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
public void Configure(EntityTypeBuilder<LoginAttempt> entity)
    {
        entity.HasKey(l => l.Id);
 entity.Property(l => l.Id).HasMaxLength(100);
   entity.Property(l => l.UserId).HasMaxLength(100);
    entity.Property(l => l.Username).IsRequired().HasMaxLength(100);
    entity.Property(l => l.IpAddress).IsRequired().HasMaxLength(50);
   entity.Property(l => l.UserAgent).HasMaxLength(500);
  entity.Property(l => l.IsSuccessful).IsRequired();
   entity.Property(l => l.FailureReason).HasMaxLength(500);
        entity.Property(l => l.AttemptAt).IsRequired();
  entity.Property(l => l.DurationMs);

   entity.HasIndex(l => l.UserId);
        entity.HasIndex(l => l.Username);
entity.HasIndex(l => l.IpAddress);
        entity.HasIndex(l => l.IsSuccessful);
   entity.HasIndex(l => l.AttemptAt);
  entity.HasIndex(l => new { l.Username, l.AttemptAt });

      entity.HasOne(l => l.User).WithMany(u => u.LoginAttempts).HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
    entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasMaxLength(100);
   entity.Property(a => a.UserId).HasMaxLength(100);
        entity.Property(a => a.Username).HasMaxLength(100);
   entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
     entity.Property(a => a.EntityType).HasMaxLength(100);
     entity.Property(a => a.EntityId).HasMaxLength(100);
 entity.Property(a => a.OldValues).HasColumnType("ntext");
        entity.Property(a => a.NewValues).HasColumnType("ntext");
 entity.Property(a => a.ChangeDetails).HasColumnType("ntext");
 entity.Property(a => a.IpAddress).IsRequired().HasMaxLength(50);
      entity.Property(a => a.UserAgent).HasMaxLength(500);
        entity.Property(a => a.CreatedAt).IsRequired();
   entity.Property(a => a.AuditLevel).IsRequired().HasMaxLength(50);
entity.Property(a => a.Endpoint).HasMaxLength(500);
        entity.Property(a => a.HttpStatusCode);
   entity.Property(a => a.DurationMs);

entity.HasIndex(a => a.UserId);
 entity.HasIndex(a => a.Username);
   entity.HasIndex(a => a.Action);
      entity.HasIndex(a => a.EntityType);
     entity.HasIndex(a => a.EntityId);
   entity.HasIndex(a => a.CreatedAt);
   entity.HasIndex(a => a.AuditLevel);
 entity.HasIndex(a => new { a.EntityType, a.EntityId });
   entity.HasIndex(a => new { a.UserId, a.CreatedAt });

     entity.HasOne(a => a.User).WithMany(u => u.AuditLogs).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
  }
}

public class ApiRateLimitLogConfiguration : IEntityTypeConfiguration<ApiRateLimitLog>
{
    public void Configure(EntityTypeBuilder<ApiRateLimitLog> entity)
    {
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasMaxLength(100);
   entity.Property(r => r.UserId).HasMaxLength(100);
     entity.Property(r => r.IpAddress).IsRequired().HasMaxLength(50);
        entity.Property(r => r.Endpoint).IsRequired().HasMaxLength(500);
        entity.Property(r => r.HttpMethod).IsRequired().HasMaxLength(10);
        entity.Property(r => r.RequestCount).IsRequired();
entity.Property(r => r.MaxRequests).IsRequired();
        entity.Property(r => r.WindowStart).IsRequired();
   entity.Property(r => r.WindowEnd).IsRequired();
entity.Property(r => r.IsRateLimited).IsRequired();
     entity.Property(r => r.CreatedAt).IsRequired();
        entity.Property(r => r.ResetAt);

  entity.HasIndex(r => r.UserId);
        entity.HasIndex(r => r.IpAddress);
   entity.HasIndex(r => r.Endpoint);
entity.HasIndex(r => r.IsRateLimited);
entity.HasIndex(r => r.CreatedAt);
entity.HasIndex(r => new { r.IpAddress, r.Endpoint, r.WindowStart });
   entity.HasIndex(r => new { r.UserId, r.CreatedAt });

        entity.HasOne(r => r.User).WithMany(u => u.RateLimitLogs).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
