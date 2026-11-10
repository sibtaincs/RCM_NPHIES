namespace NPhies_FHIR_Integration.Application.DTOs;

using NPhies_FHIR_Integration.Domain.DTOs;

/// <summary>
/// DTO for Communication entity
/// Used for API communication and data transfer
/// </summary>
public class CommunicationDto : BaseDto
{
    /// <summary>
    /// Communication ID
  /// </summary>
  public string CommunicationId { get; set; } = string.Empty;

    /// <summary>
 /// Identifier system
    /// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Based on resource type
    /// </summary>
    public string? BasedOnResourceType { get; set; }

    /// <summary>
    /// Based on identifier system
    /// </summary>
    public string? BasedOnIdentifierSystem { get; set; }

    /// <summary>
    /// Based on identifier value
    /// </summary>
 public string? BasedOnIdentifierValue { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = "completed";

/// <summary>
    /// Category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Category system
    /// </summary>
    public string? CategorySystem { get; set; }

    /// <summary>
    /// Priority
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Subject patient ID
    /// </summary>
    public string? SubjectPatientId { get; set; }

    /// <summary>
    /// About resource type
    /// </summary>
  public string? AboutResourceType { get; set; }

 /// <summary>
    /// About identifier system
 /// </summary>
    public string? AboutIdentifierSystem { get; set; }

    /// <summary>
    /// About identifier value
    /// </summary>
    public string? AboutIdentifierValue { get; set; }

    /// <summary>
    /// Payload content
    /// </summary>
    public string? PayloadContent { get; set; }

    /// <summary>
    /// Recipient ID
    /// </summary>
    public string? RecipientId { get; set; }

    /// <summary>
    /// Sender ID
    /// </summary>
  public string? SenderId { get; set; }

    /// <summary>
    /// Payload attachment content type
    /// </summary>
    public string? PayloadAttachmentContentType { get; set; }

    /// <summary>
    /// Payload attachment title
    /// </summary>
    public string? PayloadAttachmentTitle { get; set; }

    /// <summary>
    /// Payload attachment creation date
    /// </summary>
    public DateTime? PayloadAttachmentCreation { get; set; }

/// <summary>
    /// Payload attachment size in KB
    /// </summary>
public decimal PayloadAttachmentSizeKB { get; set; }

 /// <summary>
    /// Has attachment flag
    /// </summary>
    public bool HasAttachment { get; set; }

    /// <summary>
    /// Processing status
    /// </summary>
    public string ProcessingStatus { get; set; } = "received";

    /// <summary>
    /// Processed at date
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// DTO for creating a Communication
/// </summary>
public class CreateCommunicationDto
{
    /// <summary>
    /// Communication ID
    /// </summary>
    public string CommunicationId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier system
    /// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value
    /// </summary>
 public string? IdentifierValue { get; set; }

    /// <summary>
 /// Based on resource type
    /// </summary>
    public string? BasedOnResourceType { get; set; }

    /// <summary>
 /// Based on identifier system
    /// </summary>
    public string? BasedOnIdentifierSystem { get; set; }

  /// <summary>
    /// Based on identifier value
 /// </summary>
    public string? BasedOnIdentifierValue { get; set; }

    /// <summary>
    /// Status
  /// </summary>
    public string Status { get; set; } = "completed";

    /// <summary>
  /// Category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
 /// Category system
    /// </summary>
    public string? CategorySystem { get; set; }

    /// <summary>
    /// Priority
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Subject patient ID
    /// </summary>
public string? SubjectPatientId { get; set; }

    /// <summary>
    /// About resource type
    /// </summary>
    public string? AboutResourceType { get; set; }

    /// <summary>
    /// About identifier system
    /// </summary>
    public string? AboutIdentifierSystem { get; set; }

    /// <summary>
    /// About identifier value
    /// </summary>
    public string? AboutIdentifierValue { get; set; }

    /// <summary>
    /// Payload content
    /// </summary>
    public string? PayloadContent { get; set; }

    /// <summary>
    /// Recipient ID
    /// </summary>
    public string? RecipientId { get; set; }

 /// <summary>
    /// Sender ID
    /// </summary>
    public string? SenderId { get; set; }

    /// <summary>
    /// Payload attachment content type
    /// </summary>
    public string? PayloadAttachmentContentType { get; set; }

  /// <summary>
    /// Payload attachment data (base64 string)
  /// </summary>
    public string? PayloadAttachmentDataBase64 { get; set; }

    /// <summary>
    /// Payload attachment title
    /// </summary>
    public string? PayloadAttachmentTitle { get; set; }

    /// <summary>
    /// Payload attachment creation date
    /// </summary>
    public DateTime? PayloadAttachmentCreation { get; set; }
}

/// <summary>
/// DTO for updating a Communication
/// </summary>
public class UpdateCommunicationDto
{
    /// <summary>
    /// Status
  /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Payload content
    /// </summary>
    public string? PayloadContent { get; set; }

    /// <summary>
    /// Processing status
    /// </summary>
    public string? ProcessingStatus { get; set; }

    /// <summary>
    /// Processed at date
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}
