using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.CodeableConcept.Models;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class CodeSystemConfiguration : IEntityTypeConfiguration<CodeSystemEntity>
{
    public void Configure(EntityTypeBuilder<CodeSystemEntity> entity)
  {
  entity.HasKey(e => e.CodeSystemId);
        entity.Property(e => e.CodeSystemId).ValueGeneratedOnAdd();

     entity.HasIndex(e => e.Url).IsUnique();
    entity.HasIndex(e => e.Name);
  entity.HasIndex(e => e.Version);
    }
}

public class ConceptConfiguration : IEntityTypeConfiguration<ConceptEntity>
{
    public void Configure(EntityTypeBuilder<ConceptEntity> entity)
    {
        entity.HasKey(e => e.ConceptId);
     entity.Property(e => e.ConceptId).ValueGeneratedOnAdd();
        entity.Property(e => e.Code).IsRequired().HasMaxLength(100);

        entity.HasIndex(e => new { e.CodeSystemId, e.Code }).IsUnique();
entity.HasIndex(e => e.Code);

        entity.HasOne(e => e.CodeSystem).WithMany().HasForeignKey(e => e.CodeSystemId).OnDelete(DeleteBehavior.Cascade);
 }
}

public class ValueSetConfiguration : IEntityTypeConfiguration<ValueSetEntity>
{
    public void Configure(EntityTypeBuilder<ValueSetEntity> entity)
    {
entity.HasKey(e => e.ValueSetId);
        entity.Property(e => e.ValueSetId).ValueGeneratedOnAdd();

entity.HasIndex(e => e.Url).IsUnique();
entity.HasIndex(e => e.Name);
        entity.HasIndex(e => e.Version);
    }
}

public class ValueSetCodeSystemMapConfiguration : IEntityTypeConfiguration<ValueSetCodeSystemMapEntity>
{
    public void Configure(EntityTypeBuilder<ValueSetCodeSystemMapEntity> entity)
 {
      entity.HasKey(e => e.ValueSetCodeSystemMapId);
      entity.Property(e => e.ValueSetCodeSystemMapId).ValueGeneratedOnAdd();

        entity.HasIndex(e => new { e.ValueSetId, e.CodeSystemId }).IsUnique();

        entity.HasOne(e => e.ValueSet).WithMany().HasForeignKey(e => e.ValueSetId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.CodeSystem).WithMany().HasForeignKey(e => e.CodeSystemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProfileElementConfiguration : IEntityTypeConfiguration<ProfileElementEntity>
{
 public void Configure(EntityTypeBuilder<ProfileElementEntity> entity)
    {
        entity.HasKey(e => e.ProfileElementId);
     entity.Property(e => e.ProfileElementId).ValueGeneratedOnAdd();
        entity.Property(e => e.ProfileName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.MessageType).HasMaxLength(100);

        entity.HasIndex(e => new { e.ProfileName, e.ElementPath }).IsUnique();
        entity.HasIndex(e => e.ValueSetId);
      entity.HasIndex(e => e.MessageType);

     entity.HasOne(e => e.ValueSet).WithMany().HasForeignKey(e => e.ValueSetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ConceptCodeFilterConfiguration : IEntityTypeConfiguration<ConceptCodeFilterEntity>
{
    public void Configure(EntityTypeBuilder<ConceptCodeFilterEntity> entity)
    {
        entity.HasKey(e => e.ConceptCodeFilterId);
        entity.Property(e => e.ConceptCodeFilterId).ValueGeneratedOnAdd();
    entity.Property(e => e.Code).IsRequired().HasMaxLength(100);

        entity.HasIndex(e => new { e.ValueSetId, e.Code }).IsUnique();

     entity.HasOne(e => e.ValueSet).WithMany().HasForeignKey(e => e.ValueSetId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ValidationRuleConfiguration : IEntityTypeConfiguration<ValidationRuleEntity>
{
    public void Configure(EntityTypeBuilder<ValidationRuleEntity> entity)
    {
        entity.HasKey(e => e.ValidationRuleId);
        entity.Property(e => e.ValidationRuleId).ValueGeneratedOnAdd();
        entity.Property(e => e.ErrorCode).IsRequired().HasMaxLength(50);
        entity.Property(e => e.RuleType).HasMaxLength(100);

        entity.HasIndex(e => e.ErrorCode);
      entity.HasIndex(e => e.RuleType);
        entity.HasIndex(e => e.ValueSetId);

        entity.HasOne(e => e.ValueSet).WithMany().HasForeignKey(e => e.ValueSetId).OnDelete(DeleteBehavior.Restrict);
  }
}

public class NphiesMessageTypeConfiguration : IEntityTypeConfiguration<NphiesMessageTypeEntity>
{
    public void Configure(EntityTypeBuilder<NphiesMessageTypeEntity> entity)
    {
        entity.HasKey(e => e.NphiesMessageTypeId);
        entity.Property(e => e.NphiesMessageTypeId).ValueGeneratedOnAdd();
        entity.Property(e => e.MessageType).IsRequired().HasMaxLength(100);
   entity.Property(e => e.MessageTypeArabic).HasMaxLength(100);
        entity.Property(e => e.FhirResourceType).IsRequired().HasMaxLength(100);

        entity.HasIndex(e => e.MessageType).IsUnique();
        entity.HasIndex(e => e.FhirResourceType);
    }
}

public class NphiesMessageRequiredElementConfiguration : IEntityTypeConfiguration<NphiesMessageRequiredElementEntity>
{
    public void Configure(EntityTypeBuilder<NphiesMessageRequiredElementEntity> entity)
    {
        entity.HasKey(e => e.NphiesMessageRequiredElementId);
     entity.Property(e => e.NphiesMessageRequiredElementId).ValueGeneratedOnAdd();

        entity.HasIndex(e => new { e.NphiesMessageTypeId, e.ElementPath }).IsUnique();

        entity.HasOne(e => e.NphiesMessageType).WithMany().HasForeignKey(e => e.NphiesMessageTypeId).OnDelete(DeleteBehavior.Cascade);
    }
}
