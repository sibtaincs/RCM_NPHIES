using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class ClaimResponseConfiguration : IEntityTypeConfiguration<ClaimResponse>
{
    public void Configure(EntityTypeBuilder<ClaimResponse> entity)
    {
entity.HasKey(c => c.Id);
  entity.Property(c => c.ClaimId).IsRequired().HasMaxLength(100);
entity.Property(c => c.ResponseIdentifierSystem).HasMaxLength(500);
  entity.Property(c => c.ResponseIdentifierValue).HasMaxLength(100);
  entity.Property(c => c.ClaimResponseStatus).HasMaxLength(50);
 entity.Property(c => c.ClaimType).HasMaxLength(50);
    entity.Property(c => c.ClaimTypeSystem).HasMaxLength(500);
        entity.Property(c => c.ClaimSubType).HasMaxLength(50);
  entity.Property(c => c.ClaimSubTypeSystem).HasMaxLength(500);
entity.Property(c => c.Use).HasMaxLength(50);
    entity.Property(c => c.PatientId).HasMaxLength(100);
  entity.Property(c => c.InsurerId).HasMaxLength(100);
 entity.Property(c => c.RequestorId).HasMaxLength(100);
entity.Property(c => c.RequestIdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.RequestIdentifierValue).HasMaxLength(100);
    entity.Property(c => c.PreAuthRef).HasMaxLength(100);
        entity.Property(c => c.AdvancedAuthReason).HasMaxLength(100);
  entity.Property(c => c.AdvancedAuthReasonSystem).HasMaxLength(500);
        entity.Property(c => c.ServiceProviderId).HasMaxLength(100);
entity.Property(c => c.FhirClaimResponseJson).HasColumnType("ntext");

   entity.HasIndex(c => c.ClaimId);
 entity.HasIndex(c => c.ResponseIdentifierValue);
   entity.HasIndex(c => c.ClaimResponseStatus);
     entity.HasIndex(c => c.PreAuthRef);

entity.HasOne(c => c.Claim).WithMany(c => c.ClaimResponses).HasForeignKey(c => c.ClaimId).OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(c => c.Patient).WithMany().HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Restrict);
      entity.HasOne(c => c.Insurer).WithMany().HasForeignKey(c => c.InsurerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Requestor).WithMany().HasForeignKey(c => c.RequestorId).OnDelete(DeleteBehavior.Restrict);
entity.HasOne(c => c.ServiceProvider).WithMany().HasForeignKey(c => c.ServiceProviderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(c => c.Insurance).WithOne(i => i.ClaimResponse).HasForeignKey(i => i.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
     entity.HasMany(c => c.AddItems).WithOne(a => a.ClaimResponse).HasForeignKey(a => a.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
   entity.HasMany(c => c.Totals).WithOne(t => t.ClaimResponse).HasForeignKey(t => t.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
     entity.HasMany(c => c.DiagnosesExt).WithOne(d => d.ClaimResponse).HasForeignKey(d => d.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
entity.HasMany(c => c.SupportingInfoExt).WithOne(s => s.ClaimResponse).HasForeignKey(s => s.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClaimResponseInsuranceConfiguration : IEntityTypeConfiguration<ClaimResponseInsurance>
{
    public void Configure(EntityTypeBuilder<ClaimResponseInsurance> entity)
{
        entity.HasKey(c => c.Id);
entity.Property(c => c.ClaimResponseId).IsRequired().HasMaxLength(100);
   entity.Property(c => c.Sequence).IsRequired();
     entity.Property(c => c.Focal).IsRequired();
  entity.Property(c => c.CoverageId).HasMaxLength(100);
        entity.Property(c => c.PreAuthReferences).HasMaxLength(1000);

entity.HasIndex(c => c.ClaimResponseId);
entity.HasIndex(c => c.CoverageId);

     entity.HasOne(c => c.ClaimResponse).WithMany(cr => cr.Insurance).HasForeignKey(c => c.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
     entity.HasOne(c => c.Coverage).WithMany().HasForeignKey(c => c.CoverageId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClaimResponseAddItemConfiguration : IEntityTypeConfiguration<ClaimResponseAddItem>
{
    public void Configure(EntityTypeBuilder<ClaimResponseAddItem> entity)
    {
        entity.HasKey(c => c.Id);
   entity.Property(c => c.ClaimResponseId).IsRequired().HasMaxLength(100);
entity.Property(c => c.Sequence).IsRequired();
   entity.Property(c => c.ProductOrServiceCode).HasMaxLength(100);
   entity.Property(c => c.ProductOrServiceSystem).HasMaxLength(500);
  entity.Property(c => c.ProductOrServiceDisplay).HasMaxLength(255);
        entity.Property(c => c.BenefitAmount).HasPrecision(18, 2);
 entity.Property(c => c.BenefitCurrency).HasMaxLength(3);
entity.Property(c => c.SubmittedAmount).HasPrecision(18, 2);
     entity.Property(c => c.Notes).HasMaxLength(1000);

entity.HasIndex(c => c.ClaimResponseId);
entity.HasIndex(c => c.Sequence);

        entity.HasOne(c => c.ClaimResponse).WithMany(cr => cr.AddItems).HasForeignKey(c => c.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
entity.HasMany(c => c.Adjudications).WithOne(a => a.ClaimResponseAddItem).HasForeignKey(a => a.ClaimResponseAddItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClaimResponseAdjudicationConfiguration : IEntityTypeConfiguration<ClaimResponseAdjudication>
{
    public void Configure(EntityTypeBuilder<ClaimResponseAdjudication> entity)
    {
        entity.HasKey(c => c.Id);
        entity.Property(c => c.ClaimResponseAddItemId).IsRequired().HasMaxLength(100);
   entity.Property(c => c.AdjudicationCategory).IsRequired().HasMaxLength(100);
   entity.Property(c => c.AdjudicationSystem).HasMaxLength(500);
        entity.Property(c => c.AdjudicationDisplay).HasMaxLength(255);
     entity.Property(c => c.Amount).HasPrecision(18, 2);
        entity.Property(c => c.Currency).HasMaxLength(3);
 entity.Property(c => c.Notes).HasMaxLength(1000);

entity.HasIndex(c => c.ClaimResponseAddItemId);
        entity.HasIndex(c => c.AdjudicationCategory);

        entity.HasOne(c => c.ClaimResponseAddItem).WithMany(a => a.Adjudications).HasForeignKey(c => c.ClaimResponseAddItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClaimResponseTotalConfiguration : IEntityTypeConfiguration<ClaimResponseTotal>
{
    public void Configure(EntityTypeBuilder<ClaimResponseTotal> entity)
  {
   entity.HasKey(t => t.Id);
entity.Property(t => t.ClaimResponseId).IsRequired().HasMaxLength(100);
    entity.Property(t => t.Category).IsRequired().HasMaxLength(100);
   entity.Property(t => t.CategorySystem).HasMaxLength(500);
        entity.Property(t => t.CategoryDisplay).HasMaxLength(255);
   entity.Property(t => t.Amount).HasPrecision(18, 2);
entity.Property(t => t.Currency).HasMaxLength(3);
   entity.Property(t => t.Notes).HasMaxLength(1000);

     entity.HasIndex(t => t.ClaimResponseId);
   entity.HasIndex(t => t.Category);

     entity.HasOne(t => t.ClaimResponse).WithMany(cr => cr.Totals).HasForeignKey(t => t.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClaimResponseDiagnosisExtConfiguration : IEntityTypeConfiguration<ClaimResponseDiagnosisExt>
{
    public void Configure(EntityTypeBuilder<ClaimResponseDiagnosisExt> entity)
    {
 entity.HasKey(d => d.Id);
   entity.Property(d => d.ClaimResponseId).IsRequired().HasMaxLength(100);
        entity.Property(d => d.Sequence).IsRequired();
   entity.Property(d => d.DiagnosisCode).IsRequired().HasMaxLength(100);
entity.Property(d => d.DiagnosisSystem).HasMaxLength(500);
     entity.Property(d => d.DiagnosisDisplay).HasMaxLength(255);
   entity.Property(d => d.DiagnosisType).HasMaxLength(50);
    entity.Property(d => d.DiagnosisTypeSystem).HasMaxLength(500);
     entity.Property(d => d.OnAdmissionCode).HasMaxLength(10);
     entity.Property(d => d.OnAdmissionSystem).HasMaxLength(500);
 entity.Property(d => d.Notes).HasMaxLength(1000);

        entity.HasIndex(d => d.ClaimResponseId);
    entity.HasIndex(d => d.Sequence);
entity.HasIndex(d => d.DiagnosisCode);

entity.HasOne(d => d.ClaimResponse).WithMany(cr => cr.DiagnosesExt).HasForeignKey(d => d.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClaimResponseSupportingInfoExtConfiguration : IEntityTypeConfiguration<ClaimResponseSupportingInfoExt>
{
    public void Configure(EntityTypeBuilder<ClaimResponseSupportingInfoExt> entity)
    {
      entity.HasKey(e => e.Id);
entity.Property(e => e.ClaimResponseId).IsRequired().HasMaxLength(100);
entity.Property(e => e.Sequence).IsRequired();
     entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
        entity.Property(e => e.CategorySystem).HasMaxLength(500);
 entity.Property(e => e.CategoryDisplay).HasMaxLength(255);
   entity.Property(e => e.CodeValue).HasMaxLength(100);
   entity.Property(e => e.CodeSystem).HasMaxLength(500);
        entity.Property(e => e.StringValue).HasMaxLength(1000);
        entity.Property(e => e.QuantityValue).HasPrecision(18, 2);
entity.Property(e => e.QuantityUnit).HasMaxLength(50);
   entity.Property(e => e.QuantitySystem).HasMaxLength(500);
   entity.Property(e => e.Notes).HasMaxLength(1000);

entity.HasIndex(e => e.ClaimResponseId);
        entity.HasIndex(e => e.Sequence);
        entity.HasIndex(e => e.Category);

        entity.HasOne(e => e.ClaimResponse).WithMany(c => c.SupportingInfoExt).HasForeignKey(e => e.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
    }
}
