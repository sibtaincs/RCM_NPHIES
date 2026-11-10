using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class NphiesTransactionConfiguration : IEntityTypeConfiguration<NphiesTransaction>
{
    public void Configure(EntityTypeBuilder<NphiesTransaction> b)
    {
        b.ToTable("NphiesTransactions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Direction).HasMaxLength(20).IsRequired();
        b.Property(x => x.Operation).HasMaxLength(50).IsRequired();
        b.Property(x => x.EventCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.MessageId).HasMaxLength(100).IsRequired();
        b.Property(x => x.BundleId).HasMaxLength(100);
        b.Property(x => x.CorrelationId).HasMaxLength(200);
        b.Property(x => x.EntityType).HasMaxLength(50);
        b.Property(x => x.EntityId).HasMaxLength(100);
        b.Property(x => x.RequestIdentifierSystem).HasMaxLength(500);
        b.Property(x => x.RequestIdentifierValue).HasMaxLength(200);
        b.Property(x => x.ProviderId).HasMaxLength(100);
        b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        b.Property(x => x.ResponseCode).HasMaxLength(50);
        b.Property(x => x.Outcome).HasMaxLength(50);
        b.Property(x => x.ResponseMessageId).HasMaxLength(100);
        b.Property(x => x.ResponseEventCode).HasMaxLength(50);
        b.Property(x => x.ErrorSummary).HasMaxLength(4000);

        b.HasIndex(x => new { x.Direction, x.MessageId }).IsUnique();
        b.HasIndex(x => new { x.EntityType, x.EntityId });
        b.HasIndex(x => x.RequestIdentifierValue);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.SentAt);
    }
}

public class ClaimResponseItemConfiguration : IEntityTypeConfiguration<ClaimResponseItem>
{
    public void Configure(EntityTypeBuilder<ClaimResponseItem> b)
    {
        b.ToTable("ClaimResponseItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.ClaimResponseId).HasMaxLength(100).IsRequired();
        b.Property(x => x.ClaimId).HasMaxLength(100).IsRequired();
        b.Property(x => x.Outcome).HasMaxLength(30);
        b.Property(x => x.ReasonCode).HasMaxLength(100);
        b.Property(x => x.ReasonDisplay).HasMaxLength(500);
        foreach (var p in new[] { "Submitted", "Eligible", "Benefit", "Copay", "Deductible", "Tax", "PatientShare", "ApprovedQuantity" })
            b.Property<decimal?>(p).HasPrecision(18, 2);

        b.HasOne(x => x.ClaimResponse).WithMany(r => r.Items).HasForeignKey(x => x.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.ClaimId);
    }
}

public class ClaimDenialConfiguration : IEntityTypeConfiguration<ClaimDenial>
{
    public void Configure(EntityTypeBuilder<ClaimDenial> b)
    {
        b.ToTable("ClaimDenials");
        b.HasKey(x => x.Id);
        b.Property(x => x.ClaimId).HasMaxLength(100).IsRequired();
        b.Property(x => x.ClaimResponseId).HasMaxLength(100);
        b.Property(x => x.ReasonCode).HasMaxLength(100).IsRequired();
        b.Property(x => x.ReasonDisplay).HasMaxLength(1000);
        b.Property(x => x.Category).HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        b.Property(x => x.SubmittedAmount).HasPrecision(18, 2);
        b.Property(x => x.DeniedAmount).HasPrecision(18, 2);
        b.Property(x => x.AppealRequestId).HasMaxLength(100);
        b.Property(x => x.ResubmissionClaimId).HasMaxLength(100);
        b.Property(x => x.ProviderId).HasMaxLength(100);
        b.Property(x => x.InsurerId).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(2000);

        b.HasOne(x => x.Claim).WithMany().HasForeignKey(x => x.ClaimId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ClaimId, x.ClaimResponseId, x.ItemSequence, x.ReasonCode });
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ReasonCode);
    }
}

public class EligibilityCheckConfiguration : IEntityTypeConfiguration<EligibilityCheck>
{
    public void Configure(EntityTypeBuilder<EligibilityCheck> b)
    {
        b.ToTable("EligibilityChecks");
        b.HasKey(x => x.Id);
        b.Property(x => x.PatientId).HasMaxLength(100).IsRequired();
        b.Property(x => x.CoverageId).HasMaxLength(100);
        b.Property(x => x.ProviderId).HasMaxLength(100).IsRequired();
        b.Property(x => x.InsurerId).HasMaxLength(100).IsRequired();
        b.Property(x => x.Purposes).HasMaxLength(200).IsRequired();
        b.Property(x => x.RequestIdentifierSystem).HasMaxLength(500).IsRequired();
        b.Property(x => x.RequestIdentifierValue).HasMaxLength(200).IsRequired();
        b.Property(x => x.MessageId).HasMaxLength(100);
        b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        b.Property(x => x.Outcome).HasMaxLength(30);
        b.Property(x => x.SiteEligibility).HasMaxLength(100);
        b.Property(x => x.Disposition).HasMaxLength(1000);
        b.Property(x => x.NotInForceReason).HasMaxLength(500);
        b.Property(x => x.ResponseIdentifier).HasMaxLength(200);

        b.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Coverage).WithMany().HasForeignKey(x => x.CoverageId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.RequestIdentifierValue);
        b.HasIndex(x => new { x.PatientId, x.CreatedAt });
    }
}

/// <summary>RCM workflow columns added to existing entities.</summary>
public class RcmColumnsConfiguration :
    IEntityTypeConfiguration<Claim>,
    IEntityTypeConfiguration<ClaimResponse>,
    IEntityTypeConfiguration<Patient>,
    IEntityTypeConfiguration<Communication>,
    IEntityTypeConfiguration<CommunicationRequest>
{
    public void Configure(EntityTypeBuilder<Claim> b)
    {
        b.Property(x => x.NphiesStatus).HasMaxLength(50);
        b.Property(x => x.NphiesOutcome).HasMaxLength(30);
        b.Property(x => x.AdjudicationOutcome).HasMaxLength(30);
        b.Property(x => x.ApprovedAmount).HasPrecision(18, 2);
        b.Property(x => x.PatientShareAmount).HasPrecision(18, 2);
        b.Property(x => x.PaidAmount).HasPrecision(18, 2);
        b.Property(x => x.LastMessageId).HasMaxLength(100);
        b.Property(x => x.BatchIdentifier).HasMaxLength(200);
        b.HasIndex(x => x.NphiesStatus);
        b.HasIndex(x => x.ClaimIdentifierValue);
        b.HasIndex(x => x.BatchIdentifier);
    }

    public void Configure(EntityTypeBuilder<ClaimResponse> b)
    {
        b.Property(x => x.Outcome).HasMaxLength(30);
        b.Property(x => x.AdjudicationOutcome).HasMaxLength(30);
        b.Property(x => x.Disposition).HasMaxLength(2000);
        b.Property(x => x.ResponseMessageId).HasMaxLength(100);
        foreach (var p in new[] { "TotalSubmitted", "TotalEligible", "TotalBenefit", "TotalCopay", "TotalTax", "TotalPatientShare", "PaymentAmount" })
            b.Property<decimal?>(p).HasPrecision(18, 2);
        b.HasIndex(x => x.ResponseMessageId);
    }

    public void Configure(EntityTypeBuilder<Patient> b)
    {
        b.Property(x => x.FirstNameArabic).HasMaxLength(200);
        b.Property(x => x.LastNameArabic).HasMaxLength(200);
        b.Property(x => x.FullNameArabic).HasMaxLength(400);
    }

    public void Configure(EntityTypeBuilder<Communication> b)
    {
        b.Property(x => x.Direction).HasMaxLength(20).IsRequired().HasDefaultValue("outbound");
        b.Property(x => x.ClaimId).HasMaxLength(100);
        b.Property(x => x.CommunicationRequestId).HasMaxLength(100);
        b.HasIndex(x => x.ClaimId);
    }

    public void Configure(EntityTypeBuilder<CommunicationRequest> b)
    {
        b.Property(x => x.ClaimId).HasMaxLength(100);
        b.Property(x => x.MessageId).HasMaxLength(100);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.RespondedCommunicationId).HasMaxLength(100);
        b.HasIndex(x => x.ClaimId);
        b.HasIndex(x => x.MessageId);
    }
}
