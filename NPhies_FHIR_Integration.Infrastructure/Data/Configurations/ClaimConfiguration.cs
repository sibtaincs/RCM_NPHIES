using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Claim entity
/// </summary>
public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> entity)
    {
        // Primary Key
        entity.HasKey(c => c.Id);

        // Properties
        entity.Property(c => c.ClaimNumber).IsRequired().HasMaxLength(100);
        entity.Property(c => c.ClaimIdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.ClaimIdentifierValue).HasMaxLength(100);
        entity.Property(c => c.Status).IsRequired().HasMaxLength(50);
        entity.Property(c => c.ClaimType).IsRequired().HasMaxLength(50);
        entity.Property(c => c.ClaimTypeSystem).HasMaxLength(500);
        entity.Property(c => c.ClaimSubType).HasMaxLength(50);
        entity.Property(c => c.Use).IsRequired().HasMaxLength(50);
        entity.Property(c => c.Priority).HasMaxLength(50);
        entity.Property(c => c.PrioritySystem).HasMaxLength(500);
        entity.Property(c => c.PayeeType).HasMaxLength(50);
        entity.Property(c => c.PayeeTypeSystem).HasMaxLength(500);
        entity.Property(c => c.Total).HasPrecision(18, 2);
        entity.Property(c => c.TotalCurrency).HasMaxLength(3);
        entity.Property(c => c.FhirClaimBundle).HasColumnType("ntext");

        // Episode and Offline Fields
        entity.Property(c => c.EpisodeIdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.EpisodeIdentifierValue).HasMaxLength(100);
        entity.Property(c => c.EligibilityOfflineReference).HasMaxLength(100);
        entity.Property(c => c.AuthorizationOfflineDate);

        // Accident Information
        entity.Property(c => c.AccidentDate);
        entity.Property(c => c.AccidentType).HasMaxLength(50);
        entity.Property(c => c.AccidentTypeSystem).HasMaxLength(200);

        // Funds Reserve
        entity.Property(c => c.FundsReserveCode).HasMaxLength(50);
        entity.Property(c => c.FundsReserveSystem).HasMaxLength(200);

        // Referral and Prescription References
        entity.Property(c => c.ReferralIdentifier).HasMaxLength(100);
        entity.Property(c => c.PrescriptionIdentifier).HasMaxLength(100);
        entity.Property(c => c.OriginalPrescriptionIdentifier).HasMaxLength(100);
        entity.Property(c => c.PreAuthorizationRef).HasMaxLength(100);
        entity.Property(c => c.EligibilityCheckId).HasMaxLength(100);
        entity.Property(c => c.PriorAuthResponseIdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.PriorAuthResponseIdentifierValue).HasMaxLength(200);

        // Billable Period
        entity.Property(c => c.BillablePeriodStart);
        entity.Property(c => c.BillablePeriodEnd);

        // Dual-Approval Review Workflow
        entity.Property(c => c.TechnicalReviewStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
        entity.Property(c => c.TechnicalReviewerId).HasMaxLength(100);
        entity.Property(c => c.TechnicalReviewedAt);
        entity.Property(c => c.TechnicalReviewNotes).HasMaxLength(1000);
        entity.Property(c => c.MedicalReviewStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
        entity.Property(c => c.MedicalReviewerId).HasMaxLength(100);
        entity.Property(c => c.MedicalReviewedAt);
        entity.Property(c => c.MedicalReviewNotes).HasMaxLength(1000);
        // Computed convenience flag — not persisted
        entity.Ignore(c => c.IsApprovedForSubmission);

        // Indexes
        entity.HasIndex(c => c.ClaimNumber).IsUnique();
        entity.HasIndex(c => c.Status);
        entity.HasIndex(c => c.Use);
        entity.HasIndex(c => c.PatientId);
        entity.HasIndex(c => c.EpisodeIdentifierValue);
        entity.HasIndex(c => c.EligibilityOfflineReference);
        entity.HasIndex(c => c.PreAuthorizationRef);
        entity.HasIndex(c => c.ReferralIdentifier);
        entity.HasIndex(c => c.PrescriptionIdentifier);
        entity.HasIndex(c => c.TechnicalReviewStatus);
        entity.HasIndex(c => c.MedicalReviewStatus);

        // Relationships
        entity.HasOne(c => c.Patient)
 .WithMany(p => p.Claims)
          .HasForeignKey(c => c.PatientId)
       .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(c => c.Coverage)
            .WithMany(c => c.Claims)
            .HasForeignKey(c => c.CoverageId)
  .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(c => c.Provider)
            .WithMany(o => o.SubmittedClaims)
       .HasForeignKey(c => c.ProviderId)
          .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(c => c.Insurer)
            .WithMany(o => o.ProcessedClaims)
            .HasForeignKey(c => c.InsurerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(c => c.Practitioner)
       .WithMany(p => p.Claims)
                 .HasForeignKey(c => c.PractitionerId)
       .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(c => c.ServiceLocation)
              .WithMany(l => l.Claims)
              .HasForeignKey(c => c.LocationId)
         .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(c => c.MessageHeader)
     .WithMany(m => m.Claims)
     .HasForeignKey(c => c.MessageHeaderId)
   .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(c => c.Items)
     .WithOne()
 .HasForeignKey(i => i.ClaimId)
         .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(c => c.Diagnoses)
            .WithOne()
       .HasForeignKey(d => d.ClaimId)
        .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(c => c.CareTeam)
            .WithOne(ct => ct.Claim)
     .HasForeignKey(ct => ct.ClaimId)
               .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(c => c.SupportingInfo)
            .WithOne(si => si.Claim)
        .HasForeignKey(si => si.ClaimId)
     .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(c => c.RelatedClaims)
       .WithOne(rc => rc.Claim)
       .HasForeignKey(rc => rc.ClaimId)
        .OnDelete(DeleteBehavior.Cascade);
    }
}
