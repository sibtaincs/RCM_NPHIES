namespace NPhies_FHIR_Integration.Application.DTOs;

using NPhies_FHIR_Integration.Domain.DTOs;

/// <summary>
/// DTO for CommunicationRequest entity
/// Used for API communication and data transfer
/// </summary>
public class CommunicationRequestDto : BaseDto
{
    /// <summary>
    /// Communication Request ID
    /// </summary>
    public string CommunicationRequestId { get; set; } = string.Empty;

    /// <summary>
  /// Identifier system
    /// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = "active";

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
}

/// <summary>
/// DTO for creating a CommunicationRequest
/// </summary>
public class CreateCommunicationRequestDto
{
    /// <summary>
    /// Communication Request ID
    /// </summary>
    public string CommunicationRequestId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier system
    /// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = "active";

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
}

/// <summary>
/// DTO for updating a CommunicationRequest
/// </summary>
public class UpdateCommunicationRequestDto
{
    /// <summary>
    /// Status
    /// </summary>
    public string? Status { get; set; }

  /// <summary>
    /// Payload content
    /// </summary>
    public string? PayloadContent { get; set; }
}
