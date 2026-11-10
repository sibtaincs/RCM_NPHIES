using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class ClaimItemConfiguration : IEntityTypeConfiguration<ClaimItem>
{
    public void Configure(EntityTypeBuilder<ClaimItem> entity)
    {
        entity.HasKey(c => c.Id);
      entity.Property(c => c.ClaimId).IsRequired().HasMaxLength(100);
   entity.Property(c => c.Sequence).IsRequired();
        entity.Property(c => c.ProductOrServiceCode).IsRequired().HasMaxLength(100);
        entity.Property(c => c.ProductOrServiceSystem).HasMaxLength(500);
    entity.Property(c => c.AltProductOrServiceCode).HasMaxLength(100);
        entity.Property(c => c.AltProductOrServiceSystem).HasMaxLength(500);
        entity.Property(c => c.Quantity).HasPrecision(18, 2);
   entity.Property(c => c.UnitPrice).HasPrecision(18, 2);
     entity.Property(c => c.Net).HasPrecision(18, 2);
     entity.Property(c => c.Notes).HasMaxLength(1000);
   entity.Property(c => c.PatientInvoiceSystem).HasMaxLength(500);
    entity.Property(c => c.PatientInvoiceValue).HasMaxLength(100);
     entity.Property(c => c.BodySiteCode).HasMaxLength(50);
        entity.Property(c => c.BodySiteSystem).HasMaxLength(200);
   entity.Property(c => c.SubSiteCode).HasMaxLength(50);
   entity.Property(c => c.SubSiteSystem).HasMaxLength(200);
      entity.Property(c => c.Factor).HasPrecision(5, 2);
   entity.Property(c => c.Tax).HasPrecision(18, 2);
        entity.Property(c => c.TaxRate).HasPrecision(5, 2);
     entity.Property(c => c.DiagnosisSequence).HasMaxLength(100);
entity.Property(c => c.InformationSequence).HasMaxLength(100);
     entity.Property(c => c.ProcedureSequence).HasMaxLength(100);
        entity.Property(c => c.UDI).HasMaxLength(100);
entity.Property(c => c.LocationId).HasMaxLength(100);
     entity.Property(c => c.ProgramCode).HasMaxLength(50);
        entity.Property(c => c.ProgramCodeSystem).HasMaxLength(200);

        entity.HasIndex(c => c.ClaimId);
        entity.HasIndex(c => c.Sequence);
        entity.HasIndex(c => c.PatientInvoiceValue);
        entity.HasIndex(c => c.BodySiteCode);
        entity.HasIndex(c => c.LocationId);
 entity.HasIndex(c => c.UDI);
entity.HasIndex(c => c.ProgramCode);

        entity.HasOne(c => c.Location).WithMany().HasForeignKey(c => c.LocationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClaimItemDetailConfiguration : IEntityTypeConfiguration<ClaimItemDetail>
{
    public void Configure(EntityTypeBuilder<ClaimItemDetail> entity)
    {
        entity.HasKey(c => c.Id);
        entity.Property(c => c.ClaimItemId).IsRequired().HasMaxLength(100);
        entity.Property(c => c.Sequence).IsRequired();
        entity.Property(c => c.ProductOrServiceCode).IsRequired().HasMaxLength(100);
entity.Property(c => c.ProductOrServiceSystem).HasMaxLength(500);
        entity.Property(c => c.ProductOrServiceDisplay).HasMaxLength(255);
        entity.Property(c => c.AltProductOrServiceCode).HasMaxLength(100);
        entity.Property(c => c.AltProductOrServiceSystem).HasMaxLength(500);
   entity.Property(c => c.Quantity).HasPrecision(18, 2);
 entity.Property(c => c.UnitPrice).HasPrecision(18, 2);
   entity.Property(c => c.Net).HasPrecision(18, 2);
        entity.Property(c => c.Notes).HasMaxLength(1000);

      entity.HasIndex(c => c.ClaimItemId);
   entity.HasIndex(c => c.Sequence);

     entity.HasOne(c => c.ClaimItem).WithMany(i => i.Details).HasForeignKey(c => c.ClaimItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClaimDiagnosisConfiguration : IEntityTypeConfiguration<ClaimDiagnosis>
{
    public void Configure(EntityTypeBuilder<ClaimDiagnosis> entity)
    {
 entity.HasKey(c => c.Id);
   entity.Property(c => c.ClaimId).IsRequired().HasMaxLength(100);
        entity.Property(c => c.Sequence).IsRequired();
   entity.Property(c => c.DiagnosisCode).IsRequired().HasMaxLength(100);
   entity.Property(c => c.DiagnosisSystem).HasMaxLength(500);
   entity.Property(c => c.DiagnosisDisplay).HasMaxLength(255);
        entity.Property(c => c.DiagnosisType).HasMaxLength(50);
        entity.Property(c => c.DiagnosisTypeSystem).HasMaxLength(500);
 entity.Property(c => c.OnAdmissionCode).HasMaxLength(10);
        entity.Property(c => c.OnAdmissionSystem).HasMaxLength(500);
        entity.Property(c => c.Notes).HasMaxLength(1000);

        entity.HasIndex(c => c.ClaimId);
     entity.HasIndex(c => c.Sequence);
    entity.HasIndex(c => c.DiagnosisCode);

        entity.HasOne(c => c.Claim).WithMany(c => c.Diagnoses).HasForeignKey(c => c.ClaimId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClaimCareTeamConfiguration : IEntityTypeConfiguration<ClaimCareTeam>
{
public void Configure(EntityTypeBuilder<ClaimCareTeam> entity)
    {
        entity.HasKey(c => c.Id);
    entity.Property(c => c.ClaimId).IsRequired().HasMaxLength(100);
entity.Property(c => c.Sequence).IsRequired();
     entity.Property(c => c.PractitionerId).IsRequired().HasMaxLength(100);
    entity.Property(c => c.Role).HasMaxLength(100);
        entity.Property(c => c.RoleSystem).HasMaxLength(500);
        entity.Property(c => c.RoleDisplay).HasMaxLength(255);
        entity.Property(c => c.Qualification).HasMaxLength(100);
  entity.Property(c => c.QualificationSystem).HasMaxLength(500);
        entity.Property(c => c.QualificationDisplay).HasMaxLength(255);
        entity.Property(c => c.Notes).HasMaxLength(1000);

      entity.HasIndex(c => c.ClaimId);
entity.HasIndex(c => c.Sequence);
        entity.HasIndex(c => c.PractitionerId);

entity.HasOne(c => c.Claim).WithMany(c => c.CareTeam).HasForeignKey(c => c.ClaimId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(c => c.Practitioner).WithMany().HasForeignKey(c => c.PractitionerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClaimSupportingInfoConfiguration : IEntityTypeConfiguration<ClaimSupportingInfo>
{
    public void Configure(EntityTypeBuilder<ClaimSupportingInfo> entity)
    {
        entity.HasKey(c => c.Id);
entity.Property(c => c.ClaimId).IsRequired().HasMaxLength(100);
     entity.Property(c => c.Sequence).IsRequired();
        entity.Property(c => c.Category).IsRequired().HasMaxLength(100);
        entity.Property(c => c.CategorySystem).HasMaxLength(500);
      entity.Property(c => c.CategoryDisplay).HasMaxLength(255);
   entity.Property(c => c.CodeValue).HasMaxLength(100);
 entity.Property(c => c.CodeSystem).HasMaxLength(500);
  entity.Property(c => c.StringValue).HasMaxLength(1000);
        entity.Property(c => c.QuantityValue).HasPrecision(18, 2);
        entity.Property(c => c.QuantityUnit).HasMaxLength(50);
        entity.Property(c => c.QuantitySystem).HasMaxLength(500);
        entity.Property(c => c.Notes).HasMaxLength(1000);
        entity.Property(c => c.AttachmentTitle).HasMaxLength(255);
        entity.Property(c => c.AttachmentContentType).HasMaxLength(100);
        entity.Property(c => c.AttachmentUrl).HasMaxLength(1000);
        entity.Property(c => c.DocumentId).HasMaxLength(100);
        entity.HasIndex(c => c.DocumentId);
        entity.HasOne<ClaimDocument>().WithMany().HasForeignKey(c => c.DocumentId).OnDelete(DeleteBehavior.Restrict);

entity.HasIndex(c => c.ClaimId);
   entity.HasIndex(c => c.Sequence);
        entity.HasIndex(c => c.Category);

      entity.HasOne(c => c.Claim).WithMany(c => c.SupportingInfo).HasForeignKey(c => c.ClaimId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClaimRelatedConfiguration : IEntityTypeConfiguration<ClaimRelated>
{
    public void Configure(EntityTypeBuilder<ClaimRelated> entity)
    {
        entity.HasKey(c => c.Id);
entity.Property(c => c.ClaimId).IsRequired().HasMaxLength(100);
     entity.Property(c => c.RelatedClaimIdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.RelatedClaimIdentifierValue).HasMaxLength(100);
        entity.Property(c => c.Relationship).HasMaxLength(50);
entity.Property(c => c.RelationshipSystem).HasMaxLength(500);
        entity.Property(c => c.RelationshipDisplay).HasMaxLength(255);
        entity.Property(c => c.ReferencedClaimId).HasMaxLength(100);
        entity.Property(c => c.Notes).HasMaxLength(1000);

entity.HasIndex(c => c.ClaimId);
 entity.HasIndex(c => c.RelatedClaimIdentifierValue);

    entity.HasOne(c => c.Claim).WithMany(c => c.RelatedClaims).HasForeignKey(c => c.ClaimId).OnDelete(DeleteBehavior.Cascade);
}
}
