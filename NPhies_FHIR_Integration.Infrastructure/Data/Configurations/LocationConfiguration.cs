using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Location entity
/// </summary>
public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> entity)
    {
 // Primary Key
        entity.HasKey(l => l.Id);

        // Properties
  entity.Property(l => l.LocationName).IsRequired().HasMaxLength(255);
        entity.Property(l => l.LocationLicense).IsRequired().HasMaxLength(100);
        entity.Property(l => l.LicenseSystem).HasMaxLength(500);
        entity.Property(l => l.OrganizationId).IsRequired();
        entity.Property(l => l.FacilityType).HasMaxLength(50);
        entity.Property(l => l.FacilityTypeDescription).HasMaxLength(255);
        entity.Property(l => l.AddressLine1).HasMaxLength(255);
        entity.Property(l => l.AddressLine2).HasMaxLength(255);
        entity.Property(l => l.City).HasMaxLength(100);
        entity.Property(l => l.State).HasMaxLength(100);
        entity.Property(l => l.PostalCode).HasMaxLength(20);
        entity.Property(l => l.Country).HasMaxLength(2);
        entity.Property(l => l.Phone).HasMaxLength(20);
        entity.Property(l => l.Email).HasMaxLength(255);
    entity.Property(l => l.Status).IsRequired().HasMaxLength(50);

        // Indexes
        entity.HasIndex(l => l.LocationLicense).IsUnique();
    entity.HasIndex(l => l.OrganizationId);
      entity.HasIndex(l => l.Status);

    // Relationships
  entity.HasOne(l => l.Organization)
      .WithMany(o => o.Locations)
          .HasForeignKey(l => l.OrganizationId)
.OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(l => l.Claims)
            .WithOne(c => c.ServiceLocation)
  .HasForeignKey(c => c.LocationId)
       .OnDelete(DeleteBehavior.Restrict);
    }
}
