using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for MessageHeader entity
/// </summary>
public class MessageHeaderConfiguration : IEntityTypeConfiguration<MessageHeader>
{
    public void Configure(EntityTypeBuilder<MessageHeader> entity)
    {
    // Primary Key
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Id).HasMaxLength(50);

      // Properties
  entity.Property(m => m.MessageUUID).IsRequired().HasMaxLength(50);
        entity.Property(m => m.CorrelationId).HasMaxLength(50);
   entity.Property(m => m.EventCode).IsRequired().HasMaxLength(100);
        entity.Property(m => m.EventSystem).HasMaxLength(500);
  entity.Property(m => m.DestinationName).HasMaxLength(255);
  entity.Property(m => m.DestinationEndpoint).HasMaxLength(500);
    entity.Property(m => m.FocusResourceType).HasMaxLength(100);
   entity.Property(m => m.FocusResourceId).HasMaxLength(100);
        entity.Property(m => m.SourceName).HasMaxLength(255);
        entity.Property(m => m.SourceEndpoint).HasMaxLength(500);
  entity.Property(m => m.Status).IsRequired().HasMaxLength(50);
        entity.Property(m => m.ResponseStatus).HasMaxLength(50);
        entity.Property(m => m.ErrorCode).HasMaxLength(100);
      entity.Property(m => m.ErrorMessage).HasMaxLength(1000);
entity.Property(m => m.BundleContent).HasColumnType("ntext");
 entity.Property(m => m.ResponseBundleContent).HasColumnType("ntext");

        // Indexes
     entity.HasIndex(m => m.MessageUUID).IsUnique();
        entity.HasIndex(m => m.Status);
      entity.HasIndex(m => m.EventCode);

   // Relationships
        entity.HasMany(m => m.EligibilityRequests)
      .WithOne(e => e.MessageHeader)
       .HasForeignKey(e => e.MessageHeaderId)
    .OnDelete(DeleteBehavior.Restrict);

 entity.HasMany(m => m.EligibilityResponses)
.WithOne(e => e.MessageHeader)
      .HasForeignKey(e => e.MessageHeaderId)
        .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(m => m.Claims)
       .WithOne(c => c.MessageHeader)
   .HasForeignKey(c => c.MessageHeaderId)
      .OnDelete(DeleteBehavior.Restrict);
    }
}
