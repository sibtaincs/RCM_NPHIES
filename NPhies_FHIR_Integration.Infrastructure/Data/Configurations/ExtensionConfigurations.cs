using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class ClaimAccidentConfiguration : IEntityTypeConfiguration<ClaimAccident>
{
    public void Configure(EntityTypeBuilder<ClaimAccident> entity)
    {
    entity.HasKey(c => c.Id);
        entity.Property(c => c.ClaimId).IsRequired().HasMaxLength(100);
   entity.Property(c => c.AccidentDate).IsRequired();
     entity.Property(c => c.AccidentType).IsRequired().HasMaxLength(50);
    entity.Property(c => c.AccidentTypeSystem).HasMaxLength(200);
    entity.Property(c => c.AccidentLocation).HasMaxLength(500);
 entity.Property(c => c.AccidentLocationCity).HasMaxLength(100);
        entity.Property(c => c.AccidentLocationState).HasMaxLength(100);
        entity.Property(c => c.AccidentLocationCountry).HasMaxLength(2);

    entity.HasIndex(c => c.ClaimId);
        entity.HasIndex(c => c.AccidentDate);
     entity.HasIndex(c => c.AccidentType);

   entity.HasOne(c => c.Claim).WithMany().HasForeignKey(c => c.ClaimId).OnDelete(DeleteBehavior.Cascade);
  }
}

public class ClaimItemModifierConfiguration : IEntityTypeConfiguration<ClaimItemModifier>
{
    public void Configure(EntityTypeBuilder<ClaimItemModifier> entity)
    {
     entity.HasKey(c => c.Id);
 entity.Property(c => c.ClaimItemId).IsRequired().HasMaxLength(100);
        entity.Property(c => c.ModifierCode).IsRequired().HasMaxLength(50);
entity.Property(c => c.ModifierSystem).HasMaxLength(200);
      entity.Property(c => c.ModifierDisplay).HasMaxLength(255);

   entity.HasIndex(c => c.ClaimItemId);
     entity.HasIndex(c => c.ModifierCode);

        entity.HasOne(c => c.ClaimItem).WithMany().HasForeignKey(c => c.ClaimItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClaimProcedureConfiguration : IEntityTypeConfiguration<ClaimProcedure>
{
  public void Configure(EntityTypeBuilder<ClaimProcedure> entity)
    {
        entity.HasKey(c => c.Id);
        entity.Property(c => c.ClaimId).IsRequired().HasMaxLength(100);
 entity.Property(c => c.ProcedureCode).IsRequired().HasMaxLength(100);
  entity.Property(c => c.ProcedureSystem).HasMaxLength(500);
        entity.Property(c => c.ProcedureDisplay).HasMaxLength(255);
        entity.Property(c => c.ProcedureDate);
        entity.Property(c => c.ProcedureType).HasMaxLength(50);
        entity.Property(c => c.ProcedureTypeSystem).HasMaxLength(500);

        entity.HasIndex(c => c.ClaimId);
   entity.HasIndex(c => c.ProcedureCode);
   entity.HasIndex(c => c.ProcedureDate);

     entity.HasOne(c => c.Claim).WithMany().HasForeignKey(c => c.ClaimId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PaymentReconciliationConfiguration : IEntityTypeConfiguration<PaymentReconciliation>
{
public void Configure(EntityTypeBuilder<PaymentReconciliation> entity)
    {
      entity.HasKey(p => p.Id);
   entity.Property(p => p.PaymentReconciliationId).IsRequired().HasMaxLength(100);
        entity.Property(p => p.PaymentAmount).IsRequired().HasPrecision(18, 2);
     entity.Property(p => p.FhirPaymentReconciliationJson).HasColumnType("ntext");

entity.HasIndex(p => p.PaymentReconciliationId).IsUnique();

      entity.HasOne(p => p.PaymentIssuer).WithMany().HasForeignKey("PaymentIssuerId").OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(p => p.Details).WithOne(d => d.PaymentReconciliation).HasForeignKey(d => d.PaymentReconciliationId).OnDelete(DeleteBehavior.Cascade);

        entity.Property(p => p.MessageId).HasMaxLength(100);
        entity.Property(p => p.MatchStatus).IsRequired().HasMaxLength(30).HasDefaultValue("Unmatched");
        entity.Property(p => p.ExpectedAmount).HasPrecision(18, 2);
        entity.Property(p => p.VarianceAmount).HasPrecision(18, 2);
        entity.Property(p => p.PaymentNoticeMessageId).HasMaxLength(100);
        entity.Property(p => p.AcknowledgementStatus).HasMaxLength(30);
        entity.Property(p => p.AcknowledgementError).HasMaxLength(2000);
        entity.HasIndex(p => p.MessageId);
        entity.HasIndex(p => p.MatchStatus);
    }
}

public class PaymentReconciliationDetailConfiguration : IEntityTypeConfiguration<PaymentReconciliationDetail>
{
    public void Configure(EntityTypeBuilder<PaymentReconciliationDetail> entity)
 {
        entity.HasKey(p => p.Id);
 entity.Property(p => p.PaymentReconciliationId).IsRequired().HasMaxLength(100);
     entity.Property(p => p.ComponentPayment).HasPrecision(18, 2);

     entity.HasIndex(p => p.PaymentReconciliationId);

        entity.HasOne(p => p.PaymentReconciliation).WithMany(r => r.Details).HasForeignKey(p => p.PaymentReconciliationId).OnDelete(DeleteBehavior.Cascade);

        entity.Property(p => p.MatchedClaimId).HasMaxLength(100);
        entity.Property(p => p.ExpectedAmount).HasPrecision(18, 2);
        entity.Property(p => p.VarianceAmount).HasPrecision(18, 2);
        entity.Property(p => p.MatchStatus).IsRequired().HasMaxLength(30).HasDefaultValue("Unmatched");
        entity.HasIndex(p => p.MatchedClaimId);
        entity.HasIndex(p => p.RequestIdentifierValue);
        entity.Property(p => p.DetailIdentifier).HasMaxLength(200);
        entity.Property(p => p.PredecessorIdentifier).HasMaxLength(200);
        entity.HasIndex(p => p.DetailIdentifier);
    }
}

public class AdvancedAuthorizationConfiguration : IEntityTypeConfiguration<AdvancedAuthorization>
{
    public void Configure(EntityTypeBuilder<AdvancedAuthorization> entity)
    {
        entity.HasKey(a => a.Id);
        entity.Property(a => a.ClaimResponseId).IsRequired().HasMaxLength(100);
        entity.Property(a => a.IdentifierSystem).HasMaxLength(500);
        entity.Property(a => a.IdentifierValue).HasMaxLength(200);
        entity.Property(a => a.PreAuthRef).HasMaxLength(200);
        entity.Property(a => a.Reason).HasMaxLength(50);
        entity.Property(a => a.ClaimType).HasMaxLength(50);
        entity.Property(a => a.SubType).HasMaxLength(50);
        entity.Property(a => a.Outcome).HasMaxLength(30);
        entity.Property(a => a.AdjudicationOutcome).HasMaxLength(50);
        entity.Property(a => a.Disposition).HasMaxLength(1000);
        entity.Property(a => a.PatientId).HasMaxLength(100);
        entity.Property(a => a.PatientIdentifier).HasMaxLength(100);
        entity.Property(a => a.PatientName).HasMaxLength(300);
        entity.Property(a => a.InsurerId).HasMaxLength(100);
        entity.Property(a => a.InsurerLicense).HasMaxLength(100);
        entity.Property(a => a.ServiceProviderId).HasMaxLength(100);
        entity.Property(a => a.ServiceProviderLicense).HasMaxLength(100);
        entity.Property(a => a.ReferringProviderLicense).HasMaxLength(100);
        entity.Property(a => a.TotalEligible).HasPrecision(18, 2);
        entity.Property(a => a.TotalBenefit).HasPrecision(18, 2);
        entity.Property(a => a.Currency).HasMaxLength(3);
        entity.Property(a => a.Status).IsRequired().HasMaxLength(30);
        entity.Property(a => a.UsedByClaimId).HasMaxLength(100);
        entity.Property(a => a.MessageId).HasMaxLength(200);
        entity.HasIndex(a => a.IdentifierValue);
        entity.HasIndex(a => a.PreAuthRef);
        entity.HasIndex(a => a.PatientId);
        entity.HasIndex(a => a.ServiceProviderId);
    }
}

public class ClaimDocumentConfiguration : IEntityTypeConfiguration<ClaimDocument>
{
    public void Configure(EntityTypeBuilder<ClaimDocument> entity)
    {
        entity.HasKey(d => d.Id);
        entity.Property(d => d.ProviderId).IsRequired().HasMaxLength(100);
        entity.Property(d => d.PatientId).IsRequired().HasMaxLength(100);
        entity.Property(d => d.EncounterId).HasMaxLength(100);
        entity.Property(d => d.EncounterNumber).HasMaxLength(100);
        entity.Property(d => d.EncounterType).HasMaxLength(20);
        entity.Property(d => d.InvoiceNumber).HasMaxLength(100);
        entity.Property(d => d.DocumentType).IsRequired().HasMaxLength(50);
        entity.Property(d => d.Title).HasMaxLength(255);
        entity.Property(d => d.FileName).IsRequired().HasMaxLength(255);
        entity.Property(d => d.ContentType).IsRequired().HasMaxLength(100);
        entity.Property(d => d.Sha256).IsRequired().HasMaxLength(64);
        entity.Property(d => d.StorageProvider).IsRequired().HasMaxLength(20);
        entity.Property(d => d.StorageKey).IsRequired().HasMaxLength(400);
        entity.Property(d => d.OriginalStorageKey).HasMaxLength(400);
        entity.Property(d => d.Source).IsRequired().HasMaxLength(20);
        entity.Property(d => d.ExternalId).HasMaxLength(200);
        entity.Property(d => d.Notes).HasMaxLength(1000);
        entity.Property(d => d.CreatedBy).HasMaxLength(200);
        entity.HasOne(d => d.Patient).WithMany().HasForeignKey(d => d.PatientId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Encounter>().WithMany().HasForeignKey(d => d.EncounterId).OnDelete(DeleteBehavior.SetNull);
        entity.HasIndex(d => new { d.PatientId, d.EncounterNumber, d.ServiceDate });
        entity.HasIndex(d => new { d.ProviderId, d.InvoiceDate });
        entity.HasIndex(d => new { d.ProviderId, d.EncounterType, d.ServiceDate });
        entity.HasIndex(d => d.InvoiceNumber);
        entity.HasIndex(d => d.EncounterId);
        entity.HasIndex(d => d.Sha256);
        entity.HasIndex(d => new { d.ProviderId, d.ExternalId }).IsUnique().HasFilter("[ExternalId] IS NOT NULL");
    }
}

public class ClaimDocumentContentConfiguration : IEntityTypeConfiguration<ClaimDocumentContent>
{
    public void Configure(EntityTypeBuilder<ClaimDocumentContent> entity)
    {
        entity.ToTable("ClaimDocumentContents");
        entity.HasKey(d => d.Key);
        entity.Property(d => d.Key).HasMaxLength(400);
        entity.Property(d => d.Data).IsRequired();
    }
}

public class VisionPrescriptionConfiguration : IEntityTypeConfiguration<VisionPrescription>
{
    public void Configure(EntityTypeBuilder<VisionPrescription> entity)
    {
        entity.HasKey(v => v.Id);
        entity.Property(v => v.ClaimItemId).IsRequired().HasMaxLength(100);

        entity.HasIndex(v => v.ClaimItemId);

     entity.HasOne(v => v.ClaimItem).WithMany().HasForeignKey(v => v.ClaimItemId).OnDelete(DeleteBehavior.Cascade);
  }
}

public class OralDetailConfiguration : IEntityTypeConfiguration<OralDetail>
{
    public void Configure(EntityTypeBuilder<OralDetail> entity)
    {
entity.HasKey(o => o.Id);
        entity.Property(o => o.ClaimItemId).IsRequired().HasMaxLength(100);

        entity.HasIndex(o => o.ClaimItemId);

        entity.HasOne(o => o.ClaimItem).WithMany().HasForeignKey(o => o.ClaimItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClaimErrorConfiguration : IEntityTypeConfiguration<ClaimError>
{
    public void Configure(EntityTypeBuilder<ClaimError> entity)
    {
        entity.HasKey(c => c.Id);
      entity.Property(c => c.ClaimId).HasMaxLength(100);
entity.Property(c => c.ClaimResponseId).HasMaxLength(100);
   entity.Property(c => c.ErrorCode).IsRequired().HasMaxLength(100);
   entity.Property(c => c.ErrorCodeSystem).HasMaxLength(500);
        entity.Property(c => c.ErrorSeverity).IsRequired().HasMaxLength(50);
        entity.Property(c => c.ErrorDescription).HasMaxLength(2000);
        entity.Property(c => c.ErrorDetails).HasMaxLength(2000);
        entity.Property(c => c.ErrorPath).HasMaxLength(500);
   entity.Property(c => c.ErrorExpression).HasMaxLength(500);

        entity.HasIndex(c => c.ClaimId);
    entity.HasIndex(c => c.ClaimResponseId);
   entity.HasIndex(c => c.ErrorCode);
        entity.HasIndex(c => c.ErrorSeverity);

entity.HasOne(c => c.Claim).WithMany().HasForeignKey(c => c.ClaimId).OnDelete(DeleteBehavior.Cascade);
entity.HasOne(c => c.ClaimResponse).WithMany().HasForeignKey(c => c.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PaymentNoticeConfiguration : IEntityTypeConfiguration<PaymentNotice>
{
    public void Configure(EntityTypeBuilder<PaymentNotice> entity)
    {
        entity.HasKey(p => p.Id);
  entity.Property(p => p.Id).HasMaxLength(100);
        entity.Property(p => p.PaymentNoticeId).IsRequired().HasMaxLength(100);
     entity.Property(p => p.IdentifierSystem).HasMaxLength(500);
        entity.Property(p => p.IdentifierValue).HasMaxLength(100);
   entity.Property(p => p.Status).IsRequired().HasMaxLength(50);
   entity.Property(p => p.CreatedDate).IsRequired();
entity.Property(p => p.PaymentDate);
        entity.Property(p => p.PaymentIdentifierSystem).HasMaxLength(500);
entity.Property(p => p.PaymentIdentifierValue).HasMaxLength(100);
   entity.Property(p => p.Amount).IsRequired().HasPrecision(18, 2);
 entity.Property(p => p.Currency).IsRequired().HasMaxLength(3);
  entity.Property(p => p.PaymentStatus).HasMaxLength(50);
        entity.Property(p => p.PaymentStatusSystem).HasMaxLength(500);
  entity.Property(p => p.ProviderId).HasMaxLength(100);
      entity.Property(p => p.PayeeId).HasMaxLength(100);
        entity.Property(p => p.RecipientSystem).HasMaxLength(500);
  entity.Property(p => p.RecipientValue).HasMaxLength(100);
        entity.Property(p => p.FhirPaymentNoticeJson).HasColumnType("ntext");

        entity.HasIndex(p => p.PaymentNoticeId).IsUnique();
   entity.HasIndex(p => p.IdentifierValue);
        entity.HasIndex(p => p.Status);
        entity.HasIndex(p => p.PaymentStatus);
  entity.HasIndex(p => p.PaymentDate);
      entity.HasIndex(p => p.CreatedDate);
entity.HasIndex(p => p.PaymentIdentifierValue);
        entity.HasIndex(p => p.ProviderId);
      entity.HasIndex(p => p.PayeeId);
        entity.HasIndex(p => new { p.Status, p.PaymentStatus });
entity.HasIndex(p => new { p.ProviderId, p.CreatedDate });

        entity.HasOne(p => p.Provider).WithMany().HasForeignKey(p => p.ProviderId).OnDelete(DeleteBehavior.Restrict);
     entity.HasOne(p => p.Payee).WithMany().HasForeignKey(p => p.PayeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
