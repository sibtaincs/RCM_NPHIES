using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class AppealRequestConfiguration : IEntityTypeConfiguration<AppealRequest>
{
    public void Configure(EntityTypeBuilder<AppealRequest> entity)
    {
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasMaxLength(100);
        entity.Property(a => a.AppealNumber).IsRequired().HasMaxLength(100);
        entity.Property(a => a.AppealIdentifierSystem).IsRequired().HasMaxLength(500);
        entity.Property(a => a.AppealIdentifierValue).IsRequired().HasMaxLength(100);
        entity.Property(a => a.ClaimId).IsRequired().HasMaxLength(100);
        entity.Property(a => a.ClaimResponseId).IsRequired().HasMaxLength(100);
        entity.Property(a => a.PatientId).IsRequired().HasMaxLength(100);
        entity.Property(a => a.InsurerId).IsRequired().HasMaxLength(100);
        entity.Property(a => a.ProviderId).IsRequired().HasMaxLength(100);
        entity.Property(a => a.AppealStatus).IsRequired().HasMaxLength(50);
        entity.Property(a => a.AppealLevel).IsRequired();
        entity.Property(a => a.ErrorCodeBeingAppealed).IsRequired().HasMaxLength(100);
        entity.Property(a => a.ErrorDescription).HasMaxLength(500);
        entity.Property(a => a.AppealReason).IsRequired().HasMaxLength(2000);
        entity.Property(a => a.SupportingDocumentation).HasMaxLength(4000);
        entity.Property(a => a.DenialDate).IsRequired();
        entity.Property(a => a.AppealDeadlineDate).IsRequired();
        entity.Property(a => a.AppealSubmittedDate);
        entity.Property(a => a.ReceivedDate);
        entity.Property(a => a.ReviewCompletedDate);
        entity.Property(a => a.ExpectedDecisionDate);
        entity.Property(a => a.AppealOutcome).HasMaxLength(50);
        entity.Property(a => a.ApprovedAmount).HasPrecision(18, 2);
        entity.Property(a => a.DecisionExplanation).HasMaxLength(2000);
        entity.Property(a => a.AllowsEscalation).IsRequired();
        entity.Property(a => a.EscalatedAppealId).HasMaxLength(100);
        entity.Property(a => a.IsActive).IsRequired();
        entity.Property(a => a.IsWithdrawn).IsRequired();
        entity.Property(a => a.WithdrawnDate);
        entity.Property(a => a.WithdrawalReason).HasMaxLength(1000);
        entity.Property(a => a.InternalReferenceNumber).HasMaxLength(100);
        entity.Property(a => a.Notes).HasMaxLength(2000);
        entity.Property(a => a.LastStatusUpdateDate).IsRequired();

        entity.HasIndex(a => a.AppealNumber).IsUnique();
        entity.HasIndex(a => a.AppealIdentifierValue).IsUnique();
        entity.HasIndex(a => a.ClaimId);
        entity.HasIndex(a => a.ClaimResponseId);
        entity.HasIndex(a => a.PatientId);
        entity.HasIndex(a => a.InsurerId);
        entity.HasIndex(a => a.ProviderId);
        entity.HasIndex(a => a.AppealStatus);
        entity.HasIndex(a => a.AppealLevel);
        entity.HasIndex(a => a.ErrorCodeBeingAppealed);
        entity.HasIndex(a => a.AppealDeadlineDate);
        entity.HasIndex(a => a.AppealSubmittedDate);
        entity.HasIndex(a => a.IsActive);
        entity.HasIndex(a => a.IsWithdrawn);
        entity.HasIndex(a => new { a.AppealStatus, a.AppealDeadlineDate });
        entity.HasIndex(a => new { a.ClaimId, a.AppealLevel });

        entity.HasOne(a => a.Claim).WithMany().HasForeignKey(a => a.ClaimId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.ClaimResponse).WithMany().HasForeignKey(a => a.ClaimResponseId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Insurer).WithMany().HasForeignKey(a => a.InsurerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Provider).WithMany().HasForeignKey(a => a.ProviderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(a => a.StatusHistory).WithOne(h => h.Appeal).HasForeignKey(h => h.AppealId).OnDelete(DeleteBehavior.Cascade);
        // Note: AttachedDocuments collection is not configured yet - needs separate AppealRequestDocument entity
        entity.Ignore(a => a.AttachedDocuments);
    }
}

public class AppealStatusHistoryConfiguration : IEntityTypeConfiguration<AppealStatusHistory>
{
    public void Configure(EntityTypeBuilder<AppealStatusHistory> entity)
    {
        entity.HasKey(h => h.Id);
        entity.Property(h => h.Id).HasMaxLength(100);
        entity.Property(h => h.AppealId).IsRequired().HasMaxLength(100);
        entity.Property(h => h.Status).IsRequired().HasMaxLength(50);
        entity.Property(h => h.ChangedBy).IsRequired().HasMaxLength(100);
        entity.Property(h => h.ChangeReason).HasMaxLength(500);
        entity.Property(h => h.StatusChangeDate).IsRequired();
        entity.Property(h => h.Comments).HasMaxLength(1000);

        entity.HasIndex(h => h.AppealId);
        entity.HasIndex(h => h.Status);
        entity.HasIndex(h => h.StatusChangeDate);
        entity.HasIndex(h => new { h.AppealId, h.StatusChangeDate });

        entity.HasOne(h => h.Appeal).WithMany(a => a.StatusHistory).HasForeignKey(h => h.AppealId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AppealDocumentConfiguration : IEntityTypeConfiguration<AppealDocument>
{
    public void Configure(EntityTypeBuilder<AppealDocument> entity)
    {
        entity.HasKey(d => d.Id);
        entity.Property(d => d.AppealId).IsRequired();
        entity.Property(d => d.DocumentType).IsRequired().HasMaxLength(100);
        entity.Property(d => d.DocumentName).IsRequired().HasMaxLength(255);
        entity.Property(d => d.DocumentUrl).IsRequired().HasMaxLength(500);
        entity.Property(d => d.DocumentSize).IsRequired();
        entity.Property(d => d.UploadedDate).IsRequired();
        entity.Property(d => d.UploadedBy).HasMaxLength(200);

        entity.HasIndex(d => d.AppealId);
        entity.HasIndex(d => d.DocumentType);
        entity.HasIndex(d => d.UploadedDate);

        entity.HasOne(d => d.Appeal).WithMany(a => a.Documents).HasForeignKey(d => d.AppealId).OnDelete(DeleteBehavior.Cascade);
    }
}
