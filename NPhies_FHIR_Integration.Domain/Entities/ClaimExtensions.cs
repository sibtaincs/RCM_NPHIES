namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Claim Accident Details
/// Tracks accident information for claims (MVA, workplace accidents, etc.)
/// </summary>
public class ClaimAccident : BaseEntity
{
    /// <summary>
    /// Claim ID
    /// </summary>
 public string ClaimId { get; set; } = string.Empty;

    /// <summary>
    /// Accident date
    /// </summary>
    public DateTime AccidentDate { get; set; }

    /// <summary>
 /// Accident type: MVA (Motor Vehicle Accident), WORK (Workplace), etc.
    /// </summary>
    public string AccidentType { get; set; } = string.Empty;

    /// <summary>
    /// Accident type system
    /// </summary>
    public string? AccidentTypeSystem { get; set; }

  /// <summary>
    /// Accident location (full address)
    /// </summary>
    public string? AccidentLocation { get; set; }

    /// <summary>
    /// Accident location city
    /// </summary>
    public string? AccidentLocationCity { get; set; }

    /// <summary>
    /// Accident location state/province
    /// </summary>
    public string? AccidentLocationState { get; set; }

    /// <summary>
    /// Accident location country
    /// </summary>
    public string? AccidentLocationCountry { get; set; }

    // NPHIES Saudi-specific extensions

    /// <summary>
    /// Driver license number (for MVA)
    /// </summary>
    public string? DriverLicenseNumber { get; set; }

    /// <summary>
    /// Vehicle plate number (for MVA)
    /// </summary>
    public string? VehiclePlateNumber { get; set; }

    /// <summary>
    /// Patient transfer reason
    /// </summary>
    public string? PatientTransferReason { get; set; }

    /// <summary>
 /// Police report number
    /// </summary>
    public string? PoliceReportNumber { get; set; }

    /// <summary>
    /// Is emergency case
    /// </summary>
    public bool? IsEmergency { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties

  /// <summary>
    /// Parent claim
  /// </summary>
    public Claim? Claim { get; set; }
}

/// <summary>
/// Claim Item Modifiers
/// CPT/HCPCS modifiers for claim items
/// </summary>
public class ClaimItemModifier : BaseEntity
{
    /// <summary>
    /// Claim item ID
    /// </summary>
    public string ClaimItemId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number
 /// </summary>
    public int Sequence { get; set; }

    /// <summary>
  /// Modifier code (e.g., "25", "50", "RT", "LT")
    /// </summary>
    public string ModifierCode { get; set; } = string.Empty;

    /// <summary>
    /// Modifier system
    /// </summary>
    public string? ModifierSystem { get; set; }

    /// <summary>
    /// Modifier display/description
    /// </summary>
public string? ModifierDisplay { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent claim item
    /// </summary>
  public ClaimItem? ClaimItem { get; set; }
}

/// <summary>
/// Claim Procedures
/// Surgical or diagnostic procedures performed
/// </summary>
public class ClaimProcedure : BaseEntity
{
    /// <summary>
    /// Claim ID
/// </summary>
    public string ClaimId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Procedure code (ICD-10-PCS, CPT, etc.)
    /// </summary>
    public string ProcedureCode { get; set; } = string.Empty;

    /// <summary>
    /// Procedure code system
    /// </summary>
    public string? ProcedureSystem { get; set; }

    /// <summary>
    /// Procedure display/description
    /// </summary>
    public string? ProcedureDisplay { get; set; }

    /// <summary>
    /// Procedure date
    /// </summary>
    public DateTime? ProcedureDate { get; set; }

/// <summary>
  /// Procedure type: primary, secondary
    /// </summary>
    public string? ProcedureType { get; set; }

    /// <summary>
    /// Procedure type system
    /// </summary>
    public string? ProcedureTypeSystem { get; set; }

  /// <summary>
    /// Unique Device Identifier (for medical devices)
    /// </summary>
    public string? UDI { get; set; }

    /// <summary>
    /// Additional notes
  /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent claim
    /// </summary>
    public Claim? Claim { get; set; }
}

/// <summary>
/// Vision Prescription
/// Optical prescription details for vision claims
/// </summary>
public class VisionPrescription : BaseEntity
{
    /// <summary>
    /// Claim item ID
    /// </summary>
    public string ClaimItemId { get; set; } = string.Empty;

    /// <summary>
    /// Lens specification: right, left
    /// </summary>
 public string LensSpecification { get; set; } = string.Empty;

    /// <summary>
    /// Product: lens, contact
    /// </summary>
    public string? Product { get; set; }

    /// <summary>
    /// Eye: right, left
    /// </summary>
    public string? Eye { get; set; }

    /// <summary>
    /// Sphere (diopters)
    /// </summary>
    public decimal? Sphere { get; set; }

    /// <summary>
    /// Cylinder (diopters)
    /// </summary>
    public decimal? Cylinder { get; set; }

    /// <summary>
    /// Axis (degrees, 0-180)
    /// </summary>
    public int? Axis { get; set; }

/// <summary>
    /// Prism (diopters)
    /// </summary>
    public decimal? Prism { get; set; }

    /// <summary>
    /// Prism base: up, down, in, out
    /// </summary>
    public string? PrismBase { get; set; }

    /// <summary>
    /// Add power (for bifocals/progressives)
    /// </summary>
    public decimal? Add { get; set; }

    /// <summary>
    /// Power (for contact lenses)
    /// </summary>
    public decimal? Power { get; set; }

    /// <summary>
    /// Back curve (for contact lenses)
    /// </summary>
    public decimal? BackCurve { get; set; }

    /// <summary>
  /// Diameter (for contact lenses)
    /// </summary>
    public decimal? Diameter { get; set; }

    /// <summary>
    /// Duration value
    /// </summary>
    public decimal? Duration { get; set; }

    /// <summary>
    /// Duration unit (days, months)
  /// </summary>
    public string? DurationUnit { get; set; }

    /// <summary>
    /// Color (for contact lenses)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Brand name
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent claim item
    /// </summary>
    public ClaimItem? ClaimItem { get; set; }
}

/// <summary>
/// Oral/Dental Details
/// Dental-specific information for oral claims
/// </summary>
public class OralDetail : BaseEntity
{
    /// <summary>
    /// Claim item ID
    /// </summary>
    public string ClaimItemId { get; set; } = string.Empty;

    /// <summary>
    /// Tooth code (FDI notation: 11-48)
    /// </summary>
    public string ToothCode { get; set; } = string.Empty;

    /// <summary>
    /// Tooth code system
    /// </summary>
    public string? ToothCodeSystem { get; set; }

    /// <summary>
    /// Tooth surface: M (Mesial), O (Occlusal), D (Distal), B (Buccal), L (Lingual)
    /// </summary>
    public string? ToothSurface { get; set; }

    /// <summary>
    /// Tooth surface system
    /// </summary>
    public string? ToothSurfaceSystem { get; set; }

    /// <summary>
    /// Is orthodontic treatment
    /// </summary>
    public bool? IsOrthodontic { get; set; }

    /// <summary>
    /// Orthodontic treatment phase
    /// </summary>
    public string? OrthodonticTreatmentPhase { get; set; }

    /// <summary>
    /// Is extraction
    /// </summary>
    public bool? IsExtraction { get; set; }

    /// <summary>
    /// Is implant
    /// </summary>
    public bool? IsImplant { get; set; }

    /// <summary>
    /// Is prosthetic
    /// </summary>
    public bool? IsProsthetic { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent claim item
    /// </summary>
    public ClaimItem? ClaimItem { get; set; }
}

/// <summary>
/// Claim Error
/// Errors and warnings from NPHIES responses
/// </summary>
public class ClaimError : BaseEntity
{
    /// <summary>
    /// Claim ID (optional)
    /// </summary>
    public string? ClaimId { get; set; }

    /// <summary>
    /// Claim response ID (optional)
    /// </summary>
    public string? ClaimResponseId { get; set; }

    /// <summary>
    /// Pre-authorization request ID (optional)
    /// </summary>
    public string? PreAuthRequestId { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Error code system
    /// </summary>
    public string? ErrorCodeSystem { get; set; }

    /// <summary>
    /// Error severity: error, warning, information, fatal
    /// </summary>
    public string ErrorSeverity { get; set; } = "error";

    /// <summary>
    /// Error description
    /// </summary>
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// Error details (additional information)
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Error path (FHIRPath location)
    /// </summary>
    public string? ErrorPath { get; set; }

    /// <summary>
    /// Error expression
/// </summary>
    public string? ErrorExpression { get; set; }

    /// <summary>
    /// Is error resolved
    /// </summary>
    public bool IsResolved { get; set; } = false;

 /// <summary>
    /// Resolved at (date/time)
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Resolved by (user ID)
    /// </summary>
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Resolution notes
    /// </summary>
    public string? ResolutionNotes { get; set; }

    // Navigation Properties

    /// <summary>
    /// Related claim
    /// </summary>
    public Claim? Claim { get; set; }

    /// <summary>
    /// Related claim response
    /// </summary>
    public ClaimResponse? ClaimResponse { get; set; }

  /// <summary>
    /// Related pre-authorization request
/// </summary>
    public PreAuthorizationRequest? PreAuthRequest { get; set; }
}
