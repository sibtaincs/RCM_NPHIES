using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities.Access;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>EF configuration for <see cref="AppRole"/>.</summary>
public class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> entity)
    {
  entity.ToTable("AppRoles");
        entity.HasKey(r => r.Id);
     entity.Property(r => r.Id).HasMaxLength(100);
        entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
    entity.Property(r => r.DisplayName).IsRequired().HasMaxLength(150);
        entity.Property(r => r.Description).HasMaxLength(500);

        entity.HasIndex(r => r.Name).IsUnique();
    }
}

/// <summary>EF configuration for <see cref="AppPermission"/>.</summary>
public class AppPermissionConfiguration : IEntityTypeConfiguration<AppPermission>
{
    public void Configure(EntityTypeBuilder<AppPermission> entity)
    {
        entity.ToTable("AppPermissions");
  entity.HasKey(p => p.Id);
      entity.Property(p => p.Id).HasMaxLength(100);
      entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
        entity.Property(p => p.Category).IsRequired().HasMaxLength(100);
   entity.Property(p => p.Action).IsRequired().HasMaxLength(50);
  entity.Property(p => p.Description).HasMaxLength(500);

        entity.HasIndex(p => p.Name).IsUnique();
        entity.HasIndex(p => p.Category);
    }
}

/// <summary>EF configuration for the role-permission join table.</summary>
public class AppRolePermissionConfiguration : IEntityTypeConfiguration<AppRolePermission>
{
    public void Configure(EntityTypeBuilder<AppRolePermission> entity)
    {
        entity.ToTable("AppRolePermissions");
        entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

    entity.Property(rp => rp.RoleId).HasMaxLength(100);
entity.Property(rp => rp.PermissionId).HasMaxLength(100);
        entity.Property(rp => rp.GrantedBy).HasMaxLength(100);

        entity.HasOne(rp => rp.Role)
      .WithMany(r => r.RolePermissions)
     .HasForeignKey(rp => rp.RoleId)
    .OnDelete(DeleteBehavior.Cascade);

      entity.HasOne(rp => rp.Permission)
        .WithMany(p => p.RolePermissions)
     .HasForeignKey(rp => rp.PermissionId)
      .OnDelete(DeleteBehavior.Cascade);
    }
}
