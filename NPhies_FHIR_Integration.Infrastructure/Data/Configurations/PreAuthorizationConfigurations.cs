using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class PreAuthorizationRequestConfiguration : IEntityTypeConfiguration<PreAuthorizationRequest>
{
    public void Configure(EntityTypeBuilder<PreAuthorizationRequest> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.RequestId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.PatientId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.ProviderId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.RequestedDate).IsRequired();
        entity.Property(e => e.FhirRequestBundle).HasColumnType("ntext");

        entity.HasIndex(e => e.RequestId).IsUnique();
        entity.HasIndex(e => e.PatientId);
        entity.HasIndex(e => e.ProviderId);
        entity.HasIndex(e => e.Status);

        // Fix: Explicitly configure relationships to avoid cascade paths
        entity.HasOne<Patient>()
          .WithMany()
  .HasForeignKey(e => e.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

     entity.HasOne<Organization>()
      .WithMany()
            .HasForeignKey(e => e.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(e => e.Items).WithOne(i => i.PreAuthorizationRequest).HasForeignKey(i => i.PreAuthorizationRequestId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(e => e.Diagnoses).WithOne(d => d.PreAuthorizationRequest).HasForeignKey(d => d.PreAuthorizationRequestId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(e => e.SupportingInfo).WithOne(s => s.PreAuthorizationRequest).HasForeignKey(s => s.PreAuthorizationRequestId).OnDelete(DeleteBehavior.Cascade);
      entity.HasOne(e => e.Response).WithOne(r => r.PreAuthorizationRequest).HasForeignKey<PreAuthorizationResponse>(r => r.PreAuthorizationRequestId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PreAuthorizationItemConfiguration : IEntityTypeConfiguration<PreAuthorizationItem>
{
    public void Configure(EntityTypeBuilder<PreAuthorizationItem> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
 entity.Property(e => e.PreAuthorizationRequestId).IsRequired().HasMaxLength(100);
  entity.Property(e => e.Sequence).IsRequired();
 entity.Property(e => e.ServiceCode).IsRequired().HasMaxLength(100);
     entity.Property(e => e.ServiceSystem).HasMaxLength(500);
  entity.Property(e => e.Quantity).HasPrecision(18, 2);
        entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
        entity.Property(e => e.Notes).HasMaxLength(1000);

    entity.HasIndex(e => e.PreAuthorizationRequestId);
        entity.HasIndex(e => e.ServiceCode);

   entity.HasOne(e => e.PreAuthorizationRequest).WithMany(r => r.Items).HasForeignKey(e => e.PreAuthorizationRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PreAuthorizationDiagnosisConfiguration : IEntityTypeConfiguration<PreAuthorizationDiagnosis>
{
 public void Configure(EntityTypeBuilder<PreAuthorizationDiagnosis> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
  entity.Property(e => e.PreAuthorizationRequestId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Sequence).IsRequired();
        entity.Property(e => e.DiagnosisCode).IsRequired().HasMaxLength(100);
        entity.Property(e => e.DiagnosisSystem).HasMaxLength(500);
    entity.Property(e => e.DiagnosisDisplay).HasMaxLength(255);
        entity.Property(e => e.Notes).HasMaxLength(1000);

        entity.HasIndex(e => e.PreAuthorizationRequestId);
        entity.HasIndex(e => e.DiagnosisCode);

        entity.HasOne(e => e.PreAuthorizationRequest).WithMany(r => r.Diagnoses).HasForeignKey(e => e.PreAuthorizationRequestId).OnDelete(DeleteBehavior.Cascade);
  }
}

public class PreAuthorizationSupportingInfoConfiguration : IEntityTypeConfiguration<PreAuthorizationSupportingInfo>
{
    public void Configure(EntityTypeBuilder<PreAuthorizationSupportingInfo> entity)
    {
      entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.PreAuthorizationRequestId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Sequence).IsRequired();
        entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
        entity.Property(e => e.CategorySystem).HasMaxLength(500);
        entity.Property(e => e.CodeValue).HasMaxLength(100);
        entity.Property(e => e.CodeSystem).HasMaxLength(500);
        entity.Property(e => e.StringValue).HasMaxLength(1000);
  entity.Property(e => e.QuantityValue).HasPrecision(18, 2);
      entity.Property(e => e.QuantityUnit).HasMaxLength(50);
        entity.Property(e => e.QuantitySystem).HasMaxLength(500);
        entity.Property(e => e.Notes).HasMaxLength(1000);

        entity.HasIndex(e => e.PreAuthorizationRequestId);
   entity.HasIndex(e => e.Category);

 entity.HasOne(e => e.PreAuthorizationRequest).WithMany(r => r.SupportingInfo).HasForeignKey(e => e.PreAuthorizationRequestId).OnDelete(DeleteBehavior.Cascade);
 }
}

public class PreAuthorizationResponseConfiguration : IEntityTypeConfiguration<PreAuthorizationResponse>
{
    public void Configure(EntityTypeBuilder<PreAuthorizationResponse> entity)
    {
        entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.PreAuthorizationRequestId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.ResponseId).IsRequired().HasMaxLength(100);
    entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
      entity.Property(e => e.Outcome).IsRequired().HasMaxLength(50);
        entity.Property(e => e.FhirResponseBundle).HasColumnType("ntext");

   entity.HasIndex(e => e.PreAuthorizationRequestId).IsUnique();
    entity.HasIndex(e => e.ResponseId);
        entity.HasIndex(e => e.Status);

        entity.HasOne(e => e.PreAuthorizationRequest).WithOne(r => r.Response).HasForeignKey<PreAuthorizationResponse>(e => e.PreAuthorizationRequestId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(e => e.ResponseItems).WithOne(i => i.PreAuthorizationResponse).HasForeignKey(i => i.PreAuthorizationResponseId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(e => e.Errors).WithOne(er => er.PreAuthorizationResponse).HasForeignKey(er => er.PreAuthorizationResponseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PreAuthorizationResponseItemConfiguration : IEntityTypeConfiguration<PreAuthorizationResponseItem>
{
    public void Configure(EntityTypeBuilder<PreAuthorizationResponseItem> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.PreAuthorizationResponseId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Sequence).IsRequired();
        entity.Property(e => e.ServiceCode).IsRequired().HasMaxLength(100);
        entity.Property(e => e.ServiceSystem).HasMaxLength(500);
        entity.Property(e => e.ApprovedQuantity).HasPrecision(18, 2);
        entity.Property(e => e.ApprovedUnitPrice).HasPrecision(18, 2);
      entity.Property(e => e.Notes).HasMaxLength(1000);

        entity.HasIndex(e => e.PreAuthorizationResponseId);
        entity.HasIndex(e => e.ServiceCode);

        entity.HasOne(e => e.PreAuthorizationResponse).WithMany(r => r.ResponseItems).HasForeignKey(e => e.PreAuthorizationResponseId).OnDelete(DeleteBehavior.Cascade);
}
}

public class PreAuthorizationResponseErrorConfiguration : IEntityTypeConfiguration<PreAuthorizationResponseError>
{
    public void Configure(EntityTypeBuilder<PreAuthorizationResponseError> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.PreAuthorizationResponseId).IsRequired().HasMaxLength(100);
entity.Property(e => e.ErrorCode).IsRequired().HasMaxLength(100);
   entity.Property(e => e.ErrorMessage).IsRequired().HasMaxLength(1000);
        entity.Property(e => e.ErrorDetails).HasMaxLength(2000);
        entity.Property(e => e.Severity).IsRequired().HasMaxLength(50);
        entity.Property(e => e.ErrorLocation).HasMaxLength(255);
        entity.Property(e => e.ErrorField).HasMaxLength(255);
        entity.Property(e => e.AdditionalContext).HasMaxLength(1000);

    entity.HasIndex(e => e.PreAuthorizationResponseId);
        entity.Property(e => e.ErrorCode);
  entity.HasIndex(e => e.Severity);

        entity.HasOne(e => e.PreAuthorizationResponse).WithMany(r => r.Errors).HasForeignKey(e => e.PreAuthorizationResponseId).OnDelete(DeleteBehavior.Cascade);
    }
}
