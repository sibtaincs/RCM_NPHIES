using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Coverage entity
/// </summary>
public class CoverageConfiguration : IEntityTypeConfiguration<Coverage>
{
    public void Configure(EntityTypeBuilder<Coverage> entity)
    {
        // Primary Key
      entity.HasKey(c => c.Id);

        // Properties
    entity.Property(c => c.PolicyNumber).IsRequired().HasMaxLength(100);
        entity.Property(c => c.MemberID).IsRequired().HasMaxLength(50);
        entity.Property(c => c.CoverageIdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.CoverageIdentifierValue).HasMaxLength(100);
        entity.Property(c => c.CoverageType).IsRequired().HasMaxLength(50);
    entity.Property(c => c.CoverageTypeSystem).HasMaxLength(500);
        entity.Property(c => c.PlanCode).HasMaxLength(100);
        entity.Property(c => c.PlanCodeSystem).HasMaxLength(500);
   entity.Property(c => c.CoverageClass).HasMaxLength(100);
        entity.Property(c => c.Status).IsRequired().HasMaxLength(50);
      entity.Property(c => c.SubscriberRelationship).IsRequired().HasMaxLength(50);
        entity.Property(c => c.SubscriberId).HasMaxLength(100);
        entity.Property(c => c.SubscriberPatientId).HasMaxLength(100);
  entity.Property(c => c.Dependent).HasMaxLength(10);
        entity.Property(c => c.RelationToSubscriber).HasMaxLength(50);
        entity.Property(c => c.PolicyHolderOrganizationId).HasMaxLength(100);
        entity.Property(c => c.SubscriberMRN).HasMaxLength(50);
    entity.Property(c => c.Subrogation).IsRequired();
        entity.Property(c => c.MaxCopay).HasPrecision(18, 2);
 entity.Property(c => c.MaxCopayCurrency).HasMaxLength(3);
 entity.Property(c => c.CoinsurancePercentValue).HasPrecision(5, 2);
     entity.Property(c => c.CoverageClassType).HasMaxLength(50);
        entity.Property(c => c.CoverageClassValue).HasMaxLength(200);
        entity.Property(c => c.AnnualDeductible).HasPrecision(18, 2);
        entity.Property(c => c.DeductibleMet).HasPrecision(18, 2);
        entity.Property(c => c.Copay).HasPrecision(18, 2);
    entity.Property(c => c.CoinsurancePercent).HasPrecision(5, 2);
        entity.Property(c => c.OutOfPocketMax).HasPrecision(18, 2);

        // Indexes
        entity.HasIndex(c => c.PolicyNumber).IsUnique();
        entity.HasIndex(c => c.MemberID);
        entity.HasIndex(c => c.Status);
        entity.HasIndex(c => c.PatientId);
        entity.HasIndex(c => c.InsurerId);
        entity.HasIndex(c => c.SubscriberPatientId);
        entity.HasIndex(c => c.CoverageStartDate);
        entity.HasIndex(c => c.CoverageEndDate);
        entity.HasIndex(c => new { c.PatientId, c.Status });
 entity.HasIndex(c => new { c.InsurerId, c.Status });

        // Relationships

     // Patient (Beneficiary) - The person receiving healthcare services
        entity.HasOne(c => c.Patient)
       .WithMany(p => p.Coverages)
   .HasForeignKey(c => c.PatientId)
   .OnDelete(DeleteBehavior.Restrict);

   // SubscriberPatient - The person who holds the policy (may be same as beneficiary or different)
        // Per NPHIES FHIR IG: Coverage.subscriber references the Patient who is the policy holder
        // When SubscriberRelationship = "self", SubscriberPatientId = PatientId
        // When SubscriberRelationship = "spouse", "child", etc., SubscriberPatientId points to the policy holder
 entity.HasOne(c => c.SubscriberPatient)
            .WithMany()
    .HasForeignKey(c => c.SubscriberPatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Insurer Organization
    entity.HasOne(c => c.Insurer)
            .WithMany()
    .HasForeignKey(c => c.InsurerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Related Requests
        entity.HasMany(c => c.EligibilityRequests)
            .WithOne(e => e.Coverage)
     .HasForeignKey(e => e.CoverageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(c => c.Claims)
            .WithOne(c => c.Coverage)
            .HasForeignKey(c => c.CoverageId)
    .OnDelete(DeleteBehavior.Restrict);
    }
}
