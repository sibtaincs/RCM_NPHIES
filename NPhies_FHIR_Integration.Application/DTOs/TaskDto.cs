namespace NPhies_FHIR_Integration.Application.DTOs;

using NPhies_FHIR_Integration.Domain.DTOs;

/// <summary>
/// DTO for TaskRequest entity
/// Used for API communication and data transfer
/// </summary>
public class TaskRequestDto : BaseDto
{
    /// <summary>
    /// Task ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

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
    public string Status { get; set; } = "requested";

    /// <summary>
    /// Intent
    /// </summary>
    public string Intent { get; set; } = "order";

    /// <summary>
    /// Priority
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Code system
    /// </summary>
    public string? CodeSystem { get; set; }

    /// <summary>
    /// Focus resource type
    /// </summary>
    public string? FocusResourceType { get; set; }

    /// <summary>
/// Focus identifier system
  /// </summary>
    public string? FocusIdentifierSystem { get; set; }

    /// <summary>
    /// Focus identifier value
    /// </summary>
    public string? FocusIdentifierValue { get; set; }

    /// <summary>
    /// Reason code
    /// </summary>
    public string? ReasonCode { get; set; }

    /// <summary>
    /// Reason code system
    /// </summary>
    public string? ReasonCodeSystem { get; set; }

    /// <summary>
    /// Reason text
    /// </summary>
    public string? ReasonText { get; set; }

    /// <summary>
    /// Authored on
    /// </summary>
    public DateTime? AuthoredOn { get; set; }

    /// <summary>
    /// Last modified
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Requester ID
    /// </summary>
    public string? RequesterId { get; set; }

    /// <summary>
    /// Owner ID
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Processing status
    /// </summary>
    public string ProcessingStatus { get; set; } = "pending";
}

/// <summary>
/// DTO for creating a TaskRequest
/// </summary>
public class CreateTaskRequestDto
{
    /// <summary>
    /// Task ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

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
  public string Status { get; set; } = "requested";

    /// <summary>
  /// Intent
    /// </summary>
    public string Intent { get; set; } = "order";

    /// <summary>
    /// Priority
    /// </summary>
  public string? Priority { get; set; }

    /// <summary>
    /// Code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Code system
    /// </summary>
    public string? CodeSystem { get; set; }

    /// <summary>
    /// Focus resource type
    /// </summary>
    public string? FocusResourceType { get; set; }

    /// <summary>
    /// Focus identifier system
    /// </summary>
    public string? FocusIdentifierSystem { get; set; }

    /// <summary>
    /// Focus identifier value
    /// </summary>
 public string? FocusIdentifierValue { get; set; }

    /// <summary>
    /// Reason code
    /// </summary>
  public string? ReasonCode { get; set; }

    /// <summary>
    /// Reason code system
    /// </summary>
    public string? ReasonCodeSystem { get; set; }

    /// <summary>
    /// Reason text
    /// </summary>
    public string? ReasonText { get; set; }

    /// <summary>
    /// Authored on
    /// </summary>
    public DateTime? AuthoredOn { get; set; }

    /// <summary>
    /// Last modified
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Requester ID
    /// </summary>
    public string? RequesterId { get; set; }

    /// <summary>
    /// Owner ID
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating a TaskRequest
/// </summary>
public class UpdateTaskRequestDto
{
    /// <summary>
    /// Status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Last modified
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Reason text
/// </summary>
    public string? ReasonText { get; set; }

 /// <summary>
    /// Processing status
    /// </summary>
    public string? ProcessingStatus { get; set; }
}

/// <summary>
/// DTO for TaskResponse entity
/// </summary>
public class TaskResponseDto : BaseDto
{
    /// <summary>
    /// Task ID
 /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier system
  /// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Referenced request ID
    /// </summary>
    public string? ReferencedRequestId { get; set; }

    /// <summary>
    /// Task request ID
    /// </summary>
    public string? TaskRequestId { get; set; }

/// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = "completed";

/// <summary>
    /// Intent
    /// </summary>
    public string Intent { get; set; } = "order";

    /// <summary>
    /// Priority
    /// </summary>
    public string? Priority { get; set; }

  /// <summary>
    /// Code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Code system
    /// </summary>
    public string? CodeSystem { get; set; }

 /// <summary>
/// Focus resource type
    /// </summary>
    public string? FocusResourceType { get; set; }

    /// <summary>
    /// Focus identifier system
    /// </summary>
    public string? FocusIdentifierSystem { get; set; }

    /// <summary>
    /// Focus identifier value
    /// </summary>
    public string? FocusIdentifierValue { get; set; }

    /// <summary>
    /// Response code
    /// </summary>
    public string? ResponseCode { get; set; } = "ok";

    /// <summary>
    /// Response message
    /// </summary>
    public string? ResponseMessage { get; set; }

    /// <summary>
    /// Response status code
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Authored on
    /// </summary>
    public DateTime? AuthoredOn { get; set; }

    /// <summary>
    /// Last modified
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Requester ID
    /// </summary>
    public string? RequesterId { get; set; }

    /// <summary>
    /// Owner ID
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

/// <summary>
  /// Result text
    /// </summary>
    public string? ResultText { get; set; }

    /// <summary>
    /// Processing status
    /// </summary>
    public string ProcessingStatus { get; set; } = "received";

    /// <summary>
    /// Is successful flag
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Is error flag
    /// </summary>
    public bool IsError { get; set; }
}

/// <summary>
/// DTO for creating a TaskResponse
/// </summary>
public class CreateTaskResponseDto
{
    /// <summary>
    /// Task ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier system
    /// </summary>
 public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Referenced request ID
    /// </summary>
    public string? ReferencedRequestId { get; set; }

    /// <summary>
    /// Task request ID
    /// </summary>
    public string? TaskRequestId { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = "completed";

    /// <summary>
    /// Intent
    /// </summary>
    public string Intent { get; set; } = "order";

    /// <summary>
    /// Priority
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Code
 /// </summary>
    public string? Code { get; set; }

    /// <summary>
  /// Code system
    /// </summary>
    public string? CodeSystem { get; set; }

  /// <summary>
    /// Focus resource type
    /// </summary>
    public string? FocusResourceType { get; set; }

    /// <summary>
    /// Focus identifier system
    /// </summary>
    public string? FocusIdentifierSystem { get; set; }

    /// <summary>
    /// Focus identifier value
    /// </summary>
    public string? FocusIdentifierValue { get; set; }

  /// <summary>
    /// Response code
    /// </summary>
    public string? ResponseCode { get; set; } = "ok";

    /// <summary>
    /// Response message
  /// </summary>
    public string? ResponseMessage { get; set; }

    /// <summary>
    /// Response status code
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Authored on
    /// </summary>
    public DateTime? AuthoredOn { get; set; }

    /// <summary>
    /// Last modified
 /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Requester ID
/// </summary>
    public string? RequesterId { get; set; }

    /// <summary>
    /// Owner ID
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

  /// <summary>
    /// Result text
    /// </summary>
    public string? ResultText { get; set; }
}

/// <summary>
/// DTO for updating a TaskResponse
/// </summary>
public class UpdateTaskResponseDto
{
    /// <summary>
    /// Status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Response code
    /// </summary>
    public string? ResponseCode { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string? ResponseMessage { get; set; }

/// <summary>
    /// Last modified
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Result text
 /// </summary>
    public string? ResultText { get; set; }

    /// <summary>
    /// Processing status
    /// </summary>
    public string? ProcessingStatus { get; set; }
}
