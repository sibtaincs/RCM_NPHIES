using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class CancellationRequestConfiguration : IEntityTypeConfiguration<CancellationRequest>
{
    public void Configure(EntityTypeBuilder<CancellationRequest> entity)
    {
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasMaxLength(100);
        entity.Property(c => c.TaskId).IsRequired().HasMaxLength(100);
        entity.Property(c => c.IdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.IdentifierValue).HasMaxLength(100);
        entity.Property(c => c.Status).IsRequired().HasMaxLength(50);
        entity.Property(c => c.Intent).IsRequired().HasMaxLength(50);
        entity.Property(c => c.Priority).HasMaxLength(50);
        entity.Property(c => c.Code).HasMaxLength(50);
        entity.Property(c => c.CodeSystem).HasMaxLength(500);
        entity.Property(c => c.FocusResourceType).HasMaxLength(100);
        entity.Property(c => c.FocusIdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.FocusIdentifierValue).HasMaxLength(100);
        entity.Property(c => c.ReasonCode).HasMaxLength(50);
        entity.Property(c => c.ReasonCodeSystem).HasMaxLength(500);
        entity.Property(c => c.ReasonText).HasMaxLength(1000);
        entity.Property(c => c.Description).HasMaxLength(2000);
        entity.Property(c => c.FhirTaskJson).HasColumnType("ntext");
        entity.Property(c => c.MessageHeaderId).HasMaxLength(50);
        entity.Property(c => c.ProcessingStatus).IsRequired().HasMaxLength(50);

        entity.HasIndex(c => c.TaskId).IsUnique();
        entity.HasIndex(c => c.Status);
        entity.HasIndex(c => c.ProcessingStatus);
        entity.HasIndex(c => c.FocusIdentifierValue);

        entity.HasOne(c => c.Requester).WithMany().HasForeignKey(c => c.RequesterId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Owner).WithMany().HasForeignKey(c => c.OwnerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CancellationResponseConfiguration : IEntityTypeConfiguration<CancellationResponse>
{
    public void Configure(EntityTypeBuilder<CancellationResponse> entity)
    {
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasMaxLength(100);
        entity.Property(c => c.TaskId).IsRequired().HasMaxLength(100);
        entity.Property(c => c.IdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.IdentifierValue).HasMaxLength(100);
        entity.Property(c => c.ReferencedRequestId).HasMaxLength(100);
        entity.Property(c => c.Status).IsRequired().HasMaxLength(50);
        entity.Property(c => c.Intent).IsRequired().HasMaxLength(50);
        entity.Property(c => c.Priority).HasMaxLength(50);
        entity.Property(c => c.Code).HasMaxLength(50);
        entity.Property(c => c.CodeSystem).HasMaxLength(500);
        entity.Property(c => c.FocusResourceType).HasMaxLength(100);
        entity.Property(c => c.FocusIdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.FocusIdentifierValue).HasMaxLength(100);
        entity.Property(c => c.ResponseCode).HasMaxLength(50);
        entity.Property(c => c.ResponseMessage).HasMaxLength(2000);
        entity.Property(c => c.Description).HasMaxLength(2000);
        entity.Property(c => c.ResultText).HasMaxLength(2000);
        entity.Property(c => c.FhirTaskJson).HasColumnType("ntext");
        entity.Property(c => c.MessageHeaderId).HasMaxLength(50);
        entity.Property(c => c.ProcessingStatus).IsRequired().HasMaxLength(50);

        entity.HasIndex(c => c.TaskId).IsUnique();
        entity.HasIndex(c => c.Status);
        entity.HasIndex(c => c.ProcessingStatus);
        entity.HasIndex(c => c.CancellationRequestId);

        entity.HasOne(c => c.CancellationRequest).WithMany().HasForeignKey(c => c.CancellationRequestId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Requester).WithMany().HasForeignKey(c => c.RequesterId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Owner).WithMany().HasForeignKey(c => c.OwnerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommunicationConfiguration : IEntityTypeConfiguration<Communication>
{
    public void Configure(EntityTypeBuilder<Communication> entity)
    {
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasMaxLength(100);
        entity.Property(c => c.CommunicationId).IsRequired().HasMaxLength(100);
        entity.Property(c => c.IdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.IdentifierValue).HasMaxLength(100);
        entity.Property(c => c.BasedOnResourceType).HasMaxLength(100);
        entity.Property(c => c.BasedOnIdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.BasedOnIdentifierValue).HasMaxLength(100);
        entity.Property(c => c.Status).IsRequired().HasMaxLength(50);
        entity.Property(c => c.Category).HasMaxLength(100);
        entity.Property(c => c.CategorySystem).HasMaxLength(500);
        entity.Property(c => c.Priority).HasMaxLength(50);
        entity.Property(c => c.AboutResourceType).HasMaxLength(100);
        entity.Property(c => c.AboutIdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.AboutIdentifierValue).HasMaxLength(100);
        entity.Property(c => c.PayloadContent).HasColumnType("ntext");
        entity.Property(c => c.PayloadAttachmentContentType).HasMaxLength(255);
        entity.Property(c => c.PayloadAttachmentTitle).HasMaxLength(255);
        entity.Property(c => c.FhirCommunicationJson).HasColumnType("ntext");
        entity.Property(c => c.MessageHeaderId).HasMaxLength(50);
        entity.Property(c => c.ProcessingStatus).IsRequired().HasMaxLength(50);

        entity.HasIndex(c => c.CommunicationId).IsUnique();
        entity.HasIndex(c => c.Status);
        entity.HasIndex(c => c.ProcessingStatus);
        entity.HasIndex(c => c.AboutIdentifierValue);

        entity.HasOne(c => c.SubjectPatient).WithMany().HasForeignKey(c => c.SubjectPatientId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Recipient).WithMany().HasForeignKey(c => c.RecipientId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Sender).WithMany().HasForeignKey(c => c.SenderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommunicationRequestConfiguration : IEntityTypeConfiguration<CommunicationRequest>
{
    public void Configure(EntityTypeBuilder<CommunicationRequest> entity)
    {
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasMaxLength(100);
        entity.Property(c => c.CommunicationRequestId).IsRequired().HasMaxLength(100);
        entity.Property(c => c.IdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.IdentifierValue).HasMaxLength(100);
        entity.Property(c => c.Status).IsRequired().HasMaxLength(50);
        entity.Property(c => c.Category).HasMaxLength(100);
        entity.Property(c => c.CategorySystem).HasMaxLength(500);
        entity.Property(c => c.Priority).HasMaxLength(50);
        entity.Property(c => c.AboutResourceType).HasMaxLength(100);
        entity.Property(c => c.AboutIdentifierSystem).HasMaxLength(500);
        entity.Property(c => c.AboutIdentifierValue).HasMaxLength(100);
        entity.Property(c => c.PayloadContent).HasColumnType("ntext");
        entity.Property(c => c.FhirCommunicationRequestJson).HasColumnType("ntext");

        entity.HasIndex(c => c.CommunicationRequestId).IsUnique();
        entity.HasIndex(c => c.Status);
        entity.HasIndex(c => c.AboutIdentifierValue);

        entity.HasOne(c => c.SubjectPatient).WithMany().HasForeignKey(c => c.SubjectPatientId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Recipient).WithMany().HasForeignKey(c => c.RecipientId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Sender).WithMany().HasForeignKey(c => c.SenderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PollingRecordConfiguration : IEntityTypeConfiguration<PollingRecord>
{
    public void Configure(EntityTypeBuilder<PollingRecord> entity)
    {
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasMaxLength(100);
        entity.Property(p => p.PollingRecordId).IsRequired().HasMaxLength(100);
        entity.Property(p => p.ProviderId).IsRequired().HasMaxLength(100);
        entity.Property(p => p.RequestTaskId).HasMaxLength(100);
        entity.Property(p => p.RequestedMessageTypes).HasMaxLength(500);
        entity.Property(p => p.ResponseTaskId).HasMaxLength(100);
        entity.Property(p => p.ResponseStatus).HasMaxLength(50);
        entity.Property(p => p.ReceivedMessageTypes).HasMaxLength(500);
        entity.Property(p => p.RequestBundleJson).HasColumnType("ntext");
        entity.Property(p => p.ResponseBundleJson).HasColumnType("ntext");
        entity.Property(p => p.ProcessingStatus).IsRequired().HasMaxLength(50);
        entity.Property(p => p.ErrorMessage).HasMaxLength(2000);
        entity.Property(p => p.ErrorCode).HasMaxLength(100);
        entity.Property(p => p.CycleStatus).IsRequired().HasMaxLength(50);
        entity.Property(p => p.SourceIpAddress).HasMaxLength(50);
        entity.Property(p => p.RequestSourceId).HasMaxLength(100);
        entity.Property(p => p.Notes).HasMaxLength(2000);

        entity.HasIndex(p => p.PollingRecordId).IsUnique();
        entity.HasIndex(p => p.ProviderId);
        entity.HasIndex(p => p.ProcessingStatus);
        entity.HasIndex(p => p.CycleStatus);
        entity.HasIndex(p => p.RequestSentAt);

        entity.HasOne(p => p.Provider).WithMany().HasForeignKey(p => p.ProviderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(p => p.CancellationRequest).WithMany().HasForeignKey(p => p.CancellationRequestId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(p => p.CancellationResponse).WithMany().HasForeignKey(p => p.CancellationResponseId).OnDelete(DeleteBehavior.Restrict);
    }
}
