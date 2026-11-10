using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Patient entity
/// </summary>
public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> entity)
    {
        // Primary Key
      entity.HasKey(p => p.Id);

        // Properties
        entity.Property(p => p.MRN).IsRequired().HasMaxLength(50);
        entity.Property(p => p.IdentifierSystem).HasMaxLength(500);
        entity.Property(p => p.NationalId).HasMaxLength(50);
        entity.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        entity.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        entity.Property(p => p.Gender).HasMaxLength(1);
        entity.Property(p => p.Email).HasMaxLength(255);
        entity.Property(p => p.Phone).HasMaxLength(20);
        entity.Property(p => p.AddressLine1).HasMaxLength(255);
        entity.Property(p => p.AddressLine2).HasMaxLength(255);
        entity.Property(p => p.City).HasMaxLength(100);
      entity.Property(p => p.State).HasMaxLength(100);
   entity.Property(p => p.PostalCode).HasMaxLength(20);
   entity.Property(p => p.Country).HasMaxLength(2);
        entity.Property(p => p.Status).IsRequired().HasMaxLength(50);
        entity.Property(p => p.CreatedAt).IsRequired();
        entity.Property(p => p.UpdatedAt);
        entity.Property(p => p.IsActive).IsRequired();

        // Indexes
entity.HasIndex(p => p.MRN).IsUnique();
        entity.HasIndex(p => p.Status);
        entity.HasIndex(p => p.IsActive);

        // Relationships
      entity.HasMany(p => p.Coverages)
       .WithOne(c => c.Patient)
     .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(p => p.EligibilityRequests)
            .WithOne(e => e.Patient)
  .HasForeignKey(e => e.PatientId)
.OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(p => p.Claims)
   .WithOne(c => c.Patient)
        .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
