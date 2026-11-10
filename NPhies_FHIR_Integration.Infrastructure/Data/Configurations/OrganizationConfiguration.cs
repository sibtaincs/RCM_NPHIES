using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Organization entity
/// </summary>
public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> entity)
    {
      // Primary Key
        entity.HasKey(o => o.Id);
        entity.Property(o => o.Id).HasMaxLength(100);

        // Properties
        entity.Property(o => o.OrganizationName).IsRequired().HasMaxLength(255);
   entity.Property(o => o.LicenseNumber).IsRequired().HasMaxLength(100);
   entity.Property(o => o.LicenseSystem).HasMaxLength(500);
   entity.Property(o => o.OrganizationType).IsRequired().HasMaxLength(50);
        entity.Property(o => o.SpecializationType).HasMaxLength(100);
        entity.Property(o => o.Website).HasMaxLength(500);
        entity.Property(o => o.Email).HasMaxLength(255);
        entity.Property(o => o.PhoneNumber).HasMaxLength(20);
        entity.Property(o => o.AddressLine1).HasMaxLength(255);
        entity.Property(o => o.AddressLine2).HasMaxLength(255);
        entity.Property(o => o.City).HasMaxLength(100);
      entity.Property(o => o.State).HasMaxLength(100);
     entity.Property(o => o.PostalCode).HasMaxLength(20);
        entity.Property(o => o.Status).IsRequired().HasMaxLength(50);

        // Saudi Arabia Specific Fields
        entity.Property(o => o.MOHLicenseNumber).HasMaxLength(50);
      entity.Property(o => o.CHINumber).HasMaxLength(50);
  entity.Property(o => o.NphiesOrganizationId).HasMaxLength(100);
 entity.Property(o => o.NphiesProviderId).HasMaxLength(100);
        entity.Property(o => o.NphiesPayerId).HasMaxLength(100);
        entity.Property(o => o.TaxRegistrationNumber).HasMaxLength(50);

 // Indexes
   entity.HasIndex(o => o.LicenseNumber).IsUnique();
    entity.HasIndex(o => o.OrganizationType);
        entity.HasIndex(o => o.Status);
        entity.HasIndex(o => o.MOHLicenseNumber).IsUnique().HasFilter("[MOHLicenseNumber] IS NOT NULL");
        entity.HasIndex(o => o.CHINumber).IsUnique().HasFilter("[CHINumber] IS NOT NULL");
        entity.HasIndex(o => o.NphiesOrganizationId).HasFilter("[NphiesOrganizationId] IS NOT NULL");
      entity.HasIndex(o => o.NphiesProviderId).HasFilter("[NphiesProviderId] IS NOT NULL");
  entity.HasIndex(o => o.NphiesPayerId).HasFilter("[NphiesPayerId] IS NOT NULL");
  entity.HasIndex(o => o.TaxRegistrationNumber).HasFilter("[TaxRegistrationNumber] IS NOT NULL");

        // Relationships
        entity.HasMany(o => o.Locations)
       .WithOne(l => l.Organization)
       .HasForeignKey(l => l.OrganizationId)
   .OnDelete(DeleteBehavior.Cascade);

   entity.HasMany(o => o.Practitioners)
            .WithOne(p => p.Organization)
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(o => o.SubmittedClaims)
      .WithOne(c => c.Provider)
    .HasForeignKey(c => c.ProviderId)
    .OnDelete(DeleteBehavior.Restrict);

   entity.HasMany(o => o.ProcessedClaims)
            .WithOne(c => c.Insurer)
       .HasForeignKey(c => c.InsurerId)
         .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(o => o.EligibilityRequests)
.WithOne(e => e.Provider)
            .HasForeignKey(e => e.ProviderId)
      .OnDelete(DeleteBehavior.Restrict);

    entity.HasMany(o => o.EligibilityResponses)
   .WithOne(e => e.Insurer)
     .HasForeignKey(e => e.InsurerId)
   .OnDelete(DeleteBehavior.Restrict);
    }
}
