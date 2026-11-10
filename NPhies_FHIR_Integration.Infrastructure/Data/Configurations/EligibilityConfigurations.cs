using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class CoverageEligibilityRequestConfiguration : IEntityTypeConfiguration<CoverageEligibilityRequest>
{
    public void Configure(EntityTypeBuilder<CoverageEligibilityRequest> entity)
    {
      entity.HasKey(c => c.Id);
        entity.Property(c => c.MessageUUID).IsRequired().HasMaxLength(50);
        entity.Property(c => c.RequestId).IsRequired().HasMaxLength(100);
        entity.Property(c => c.MessageHeaderId).HasMaxLength(50);
      entity.Property(c => c.RequestType).IsRequired().HasMaxLength(50);
        entity.Property(c => c.PurposeJson).HasColumnType("nvarchar(max)");
        entity.Property(c => c.Status).IsRequired().HasMaxLength(50);
        entity.Property(c => c.Priority).HasMaxLength(50);
        entity.Property(c => c.ServiceType).HasMaxLength(100);
        entity.Property(c => c.EligibilityStatus).HasMaxLength(50);
        entity.Property(c => c.MessageStatus).HasMaxLength(50);
      entity.Property(c => c.FhirRequestBundle).HasColumnType("ntext");
        entity.Property(c => c.FhirResponseBundle).HasColumnType("ntext");

        entity.HasIndex(c => c.RequestId).IsUnique();
        entity.HasIndex(c => c.Status);
     entity.HasIndex(c => c.PatientId);
        entity.HasIndex(c => c.CoverageId);

    entity.HasOne(c => c.MessageHeader).WithMany(m => m.EligibilityRequests).HasForeignKey(c => c.MessageHeaderId).OnDelete(DeleteBehavior.Restrict);
      entity.HasOne(c => c.Patient).WithMany(p => p.EligibilityRequests).HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Restrict);
   entity.HasOne(c => c.Coverage).WithMany(c => c.EligibilityRequests).HasForeignKey(c => c.CoverageId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Provider).WithMany(o => o.EligibilityRequests).HasForeignKey(c => c.ProviderId).OnDelete(DeleteBehavior.Restrict);
     entity.HasOne(c => c.Insurer).WithMany().HasForeignKey(c => c.InsurerId).OnDelete(DeleteBehavior.Restrict);
entity.HasOne(c => c.Enterer).WithMany().HasForeignKey(c => c.EntererPractitionerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(c => c.Items).WithOne(i => i.EligibilityRequest).HasForeignKey(i => i.EligibilityRequestId).OnDelete(DeleteBehavior.Cascade);
   entity.HasOne(c => c.Response).WithOne(r => r.EligibilityRequest).HasForeignKey<CoverageEligibilityResponse>(r => r.EligibilityRequestId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EligibilityItemConfiguration : IEntityTypeConfiguration<EligibilityItem>
{
    public void Configure(EntityTypeBuilder<EligibilityItem> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.EligibilityRequestId).IsRequired();
        entity.Property(e => e.SequenceNumber);
        entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
        entity.Property(e => e.CategorySystem).HasMaxLength(500);
  entity.Property(e => e.CategoryDescription).HasMaxLength(255);
     entity.Property(e => e.ProductOrServiceCode).HasMaxLength(100);
        entity.Property(e => e.ProductOrServiceSystem).HasMaxLength(500);
     entity.Property(e => e.ProductOrServiceDescription).HasMaxLength(255);
        entity.Property(e => e.DiagnosisCodes).HasColumnType("nvarchar(max)");
        entity.Property(e => e.Notes).HasMaxLength(1000);

        entity.HasIndex(e => e.EligibilityRequestId);
      entity.HasIndex(e => e.Category);

   entity.HasOne(e => e.EligibilityRequest).WithMany(r => r.Items).HasForeignKey(e => e.EligibilityRequestId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(e => e.Modifiers).WithOne(m => m.EligibilityItem).HasForeignKey(m => m.EligibilityItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EligibilityItemModifierConfiguration : IEntityTypeConfiguration<EligibilityItemModifier>
{
    public void Configure(EntityTypeBuilder<EligibilityItemModifier> entity)
    {
 entity.HasKey(m => m.Id);
     entity.Property(m => m.EligibilityItemId).IsRequired();
        entity.Property(m => m.ModifierCode).IsRequired().HasMaxLength(100);
        entity.Property(m => m.ModifierSystem).HasMaxLength(500);
      entity.Property(m => m.ModifierDescription).HasMaxLength(255);

   entity.HasOne(m => m.EligibilityItem).WithMany(e => e.Modifiers).HasForeignKey(m => m.EligibilityItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CoverageEligibilityResponseConfiguration : IEntityTypeConfiguration<CoverageEligibilityResponse>
{
  public void Configure(EntityTypeBuilder<CoverageEligibilityResponse> entity)
    {
        entity.HasKey(c => c.Id);
        entity.Property(c => c.ResponseUUID).IsRequired().HasMaxLength(50);
        entity.Property(c => c.RequestId).IsRequired().HasMaxLength(100);
        entity.Property(c => c.EligibilityRequestId).IsRequired();
entity.Property(c => c.MessageHeaderId).HasMaxLength(50);
        entity.Property(c => c.Status).IsRequired().HasMaxLength(50);
    entity.Property(c => c.Outcome).IsRequired().HasMaxLength(50);
   entity.Property(c => c.ProcessingStatus).HasMaxLength(255);
        entity.Property(c => c.EligibilityStatus).HasMaxLength(50);
        entity.Property(c => c.NetworkStatus).HasMaxLength(50);
        entity.Property(c => c.NetworkName).HasMaxLength(255);
        entity.Property(c => c.CoveredServicesJson).HasColumnType("nvarchar(max)");
        entity.Property(c => c.ExcludedServicesJson).HasColumnType("nvarchar(max)");
    entity.Property(c => c.LimitationsJson).HasColumnType("nvarchar(max)");
        entity.Property(c => c.ExplanationOfBenefits).HasMaxLength(1000);
        entity.Property(c => c.FhirResponseContent).HasColumnType("ntext");

        entity.HasIndex(c => c.ResponseUUID).IsUnique();
        entity.HasIndex(c => c.RequestId);
        entity.HasIndex(c => c.Outcome);
        entity.HasIndex(c => c.EligibilityRequestId).IsUnique();

   entity.HasOne(c => c.EligibilityRequest).WithOne(r => r.Response).HasForeignKey<CoverageEligibilityResponse>(c => c.EligibilityRequestId).OnDelete(DeleteBehavior.Restrict);
entity.HasOne(c => c.MessageHeader).WithMany(m => m.EligibilityResponses).HasForeignKey(c => c.MessageHeaderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Insurer).WithMany(o => o.EligibilityResponses).HasForeignKey(c => c.InsurerId).OnDelete(DeleteBehavior.Restrict);
 entity.HasOne(c => c.Patient).WithMany().HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Coverage).WithMany().HasForeignKey(c => c.CoverageId).OnDelete(DeleteBehavior.Restrict);
    entity.HasMany(c => c.BenefitBalances).WithOne(b => b.EligibilityResponse).HasForeignKey(b => b.EligibilityResponseId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(c => c.Errors).WithOne(e => e.EligibilityResponse).HasForeignKey(e => e.EligibilityResponseId).OnDelete(DeleteBehavior.Cascade);
 }
}

public class BenefitBalanceConfiguration : IEntityTypeConfiguration<BenefitBalance>
{
    public void Configure(EntityTypeBuilder<BenefitBalance> entity)
    {
        entity.HasKey(b => b.Id);
  entity.Property(b => b.EligibilityResponseId).IsRequired();
        entity.Property(b => b.SequenceNumber);
        entity.Property(b => b.Category).IsRequired().HasMaxLength(50);
        entity.Property(b => b.CategorySystem).HasMaxLength(500);
        entity.Property(b => b.CategoryDescription).HasMaxLength(255);

        entity.HasIndex(b => b.EligibilityResponseId);
   entity.HasIndex(b => b.Category);

        entity.HasOne(b => b.EligibilityResponse).WithMany(r => r.BenefitBalances).HasForeignKey(b => b.EligibilityResponseId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(b => b.Benefits).WithOne(b => b.BenefitBalance).HasForeignKey(b => b.BenefitBalanceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BenefitConfiguration : IEntityTypeConfiguration<Benefit>
{
  public void Configure(EntityTypeBuilder<Benefit> entity)
    {
      entity.HasKey(b => b.Id);
entity.Property(b => b.BenefitBalanceId).IsRequired();
        entity.Property(b => b.SequenceNumber);
        entity.Property(b => b.BenefitType).IsRequired().HasMaxLength(50);
        entity.Property(b => b.BenefitTypeSystem).HasMaxLength(500);
      entity.Property(b => b.BenefitTypeDescription).HasMaxLength(255);
        entity.Property(b => b.AllowedAmount).HasPrecision(18, 2);
        entity.Property(b => b.AllowedCurrency).HasMaxLength(3);
        entity.Property(b => b.AllowedUnit).HasMaxLength(50);
        entity.Property(b => b.UsedAmount).HasPrecision(18, 2);
        entity.Property(b => b.PercentageAmount).HasPrecision(5, 2);
        entity.Property(b => b.Description).HasMaxLength(1000);

   entity.HasIndex(b => b.BenefitBalanceId);
    entity.HasIndex(b => b.BenefitType);

   entity.HasOne(b => b.BenefitBalance).WithMany(b => b.Benefits).HasForeignKey(b => b.BenefitBalanceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EligibilityErrorConfiguration : IEntityTypeConfiguration<EligibilityError>
{
    public void Configure(EntityTypeBuilder<EligibilityError> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.EligibilityRequestId).HasMaxLength(100);
        entity.Property(e => e.EligibilityResponseId).HasMaxLength(100);
        entity.Property(e => e.ErrorCode).IsRequired().HasMaxLength(100);
      entity.Property(e => e.ErrorCodeSystem).HasMaxLength(500);
      entity.Property(e => e.ErrorMessage).IsRequired().HasMaxLength(1000);
      entity.Property(e => e.ErrorDetails).HasMaxLength(2000);
        entity.Property(e => e.Severity).IsRequired().HasMaxLength(50);
        entity.Property(e => e.ErrorLocation).HasMaxLength(255);
 entity.Property(e => e.ErrorField).HasMaxLength(255);
        entity.Property(e => e.AdditionalContext).HasMaxLength(1000);

     entity.HasIndex(e => e.Severity);
        entity.HasIndex(e => e.ErrorCode);

        entity.HasOne(e => e.EligibilityRequest).WithMany().HasForeignKey(e => e.EligibilityRequestId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.EligibilityResponse).WithMany(r => r.Errors).HasForeignKey(e => e.EligibilityResponseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EncounterConfiguration : IEntityTypeConfiguration<Encounter>
{
    public void Configure(EntityTypeBuilder<Encounter> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.EncounterId).IsRequired().HasMaxLength(100);
      entity.Property(e => e.IdentifierSystem).HasMaxLength(500);
 entity.Property(e => e.IdentifierValue).HasMaxLength(100);
 entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Class).IsRequired().HasMaxLength(50);
    entity.Property(e => e.ServiceType).HasMaxLength(100);
        entity.Property(e => e.ServiceTypeSystem).HasMaxLength(500);
    entity.Property(e => e.AdmitSource).HasMaxLength(50);
        entity.Property(e => e.AdmitSourceSystem).HasMaxLength(500);
        entity.Property(e => e.ServiceEventType).HasMaxLength(100);
        entity.Property(e => e.IntendedLengthOfStay).HasMaxLength(100);
        entity.Property(e => e.FhirEncounterJson).HasColumnType("ntext");

        entity.HasIndex(e => e.EncounterId).IsUnique();
    entity.HasIndex(e => e.Status);
      entity.HasIndex(e => e.Class);
        entity.HasIndex(e => e.PatientId);

      entity.HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
   entity.HasOne(e => e.ServiceProvider).WithMany().HasForeignKey(e => e.ServiceProviderId).OnDelete(DeleteBehavior.Restrict);
    }
}
