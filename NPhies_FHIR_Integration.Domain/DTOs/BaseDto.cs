namespace NPhies_FHIR_Integration.Domain.DTOs;

/// <summary>
/// Base DTO for all data transfer objects
/// </summary>
public abstract class BaseDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
