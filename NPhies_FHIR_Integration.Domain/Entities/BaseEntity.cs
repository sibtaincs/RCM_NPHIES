namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Base entity for all domain entities - FHIR Resources
/// </summary>
public abstract class BaseEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
