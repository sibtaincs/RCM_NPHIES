namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Encounter entity - represents a healthcare encounter/admission
/// FHIR Resource: Encounter
/// </summary>
public class Encounter : BaseEntity
{
    /// <summary>
    /// Encounter identifier value
    /// </summary>
    public string EncounterId { get; set; } = string.Empty;

    /// <summary>
    /// Encounter identifier system (e.g., "http://hmg.com/Takhassusi/encounter")
    /// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Encounter identifier value
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Encounter status: "planned", "arrived", "triaged", "in-progress", "onleave", "finished", "cancelled"
    /// </summary>
    public string Status { get; set; } = "arrived";

    /// <summary>
    /// Encounter class: "IMP" (inpatient), "AMB" (ambulatory), "OBSENC" (observation), "EMER" (emergency), etc.
    /// </summary>
    public string Class { get; set; } = string.Empty;

    /// <summary>
    /// Service type code
    /// </summary>
    public string? ServiceType { get; set; }

    /// <summary>
    /// Service type system URL
    /// </summary>
    public string? ServiceTypeSystem { get; set; }

    /// <summary>
    /// Patient ID for this encounter
    /// </summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>
    /// Patient for this encounter
    /// </summary>
    public Patient? Patient { get; set; }

    /// <summary>
    /// Encounter period start date
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Encounter period end date
    /// </summary>
    public DateTime? PeriodEnd { get; set; }

    /// <summary>
    /// Admission source code (e.g., "EER" for Emergency)
    /// </summary>
    public string? AdmitSource { get; set; }

    /// <summary>
    /// Admission source system URL
    /// </summary>
    public string? AdmitSourceSystem { get; set; }

    /// <summary>
    /// Service event type (e.g., "ICSE" for ICU service)
    /// </summary>
    public string? ServiceEventType { get; set; }

    /// <summary>
    /// Service event type system URL
    /// </summary>
    public string? ServiceEventTypeSystem { get; set; }

    /// <summary>
    /// Intended length of stay (e.g., "IO" for inpatient overnight)
    /// </summary>
    public string? IntendedLengthOfStay { get; set; }

    /// <summary>
    /// Intended length of stay system URL
    /// </summary>
    public string? IntendedLengthOfStaySystem { get; set; }

    /// <summary>
    /// Service provider organization ID
    /// </summary>
  public string? ServiceProviderId { get; set; }

    /// <summary>
    /// Service provider organization
    /// </summary>
 public Organization? ServiceProvider { get; set; }

    /// <summary>
    /// FHIR Encounter JSON for storage
    /// </summary>
    public string? FhirEncounterJson { get; set; } = string.Empty;
}
