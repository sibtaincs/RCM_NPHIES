using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Practitioner entity
/// </summary>
public class PractitionerConfiguration : IEntityTypeConfiguration<Practitioner>
{
    public void Configure(EntityTypeBuilder<Practitioner> entity)
    {
        // Primary Key
        entity.HasKey(p => p.Id);

        // Properties
   entity.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
entity.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        entity.Property(p => p.LicenseNumber).IsRequired().HasMaxLength(100);
entity.Property(p => p.LicenseSystem).HasMaxLength(500);
        entity.Property(p => p.Specialization).HasMaxLength(100);
        entity.Property(p => p.Qualification).HasMaxLength(255);
        entity.Property(p => p.Title).HasMaxLength(50);
 entity.Property(p => p.Email).HasMaxLength(255);
        entity.Property(p => p.Phone).HasMaxLength(20);
        entity.Property(p => p.Status).IsRequired().HasMaxLength(50);

  // Saudi Arabia Specific Fields
        entity.Property(p => p.PractitionerLicenseNumber).HasMaxLength(50);
        entity.Property(p => p.LicenseIssuingAuthority).HasMaxLength(100);
   entity.Property(p => p.LicenseExpiryDate);
        entity.Property(p => p.NationalIdentificationNumber).HasMaxLength(20);
        entity.Property(p => p.PractitionerRole).HasMaxLength(100);
        entity.Property(p => p.PractitionerRoleSystem).HasMaxLength(200);

        // Indexes
        entity.HasIndex(p => p.LicenseNumber).IsUnique();
   entity.HasIndex(p => p.Status);
        entity.HasIndex(p => p.PractitionerLicenseNumber).IsUnique().HasFilter("[PractitionerLicenseNumber] IS NOT NULL");
        entity.HasIndex(p => p.NationalIdentificationNumber).IsUnique().HasFilter("[NationalIdentificationNumber] IS NOT NULL");
 entity.HasIndex(p => p.LicenseExpiryDate).HasFilter("[LicenseExpiryDate] IS NOT NULL");
        entity.HasIndex(p => p.PractitionerRole).HasFilter("[PractitionerRole] IS NOT NULL");

        // Relationships
        entity.HasOne(p => p.Organization)
            .WithMany(o => o.Practitioners)
   .HasForeignKey(p => p.OrganizationId)
 .OnDelete(DeleteBehavior.Restrict);

   entity.HasMany(p => p.Claims)
            .WithOne(c => c.Practitioner)
            .HasForeignKey(c => c.PractitionerId)
    .OnDelete(DeleteBehavior.Restrict);
    }
}
