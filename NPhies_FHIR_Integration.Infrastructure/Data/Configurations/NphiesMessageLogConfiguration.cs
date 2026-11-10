using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class NphiesMessageLogConfiguration : IEntityTypeConfiguration<NphiesMessageLog>
{
    public void Configure(EntityTypeBuilder<NphiesMessageLog> builder)
    {
        builder.ToTable("NphiesMessageLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(100);
        builder.Property(x => x.MessageId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BundleId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(200);
        builder.Property(x => x.EventCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ResponseEventCode).HasMaxLength(50);
        builder.Property(x => x.Direction).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Endpoint).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ResponseCode).HasMaxLength(50);
        builder.Property(x => x.ResponseMessageId).HasMaxLength(100);
        builder.Property(x => x.RequestJson).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);

        builder.HasIndex(x => x.MessageId);
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
