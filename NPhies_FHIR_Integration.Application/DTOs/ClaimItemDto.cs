namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// DTO for ClaimItem entity - for read operations
/// </summary>
public class ClaimItemDto
{
 public int Id { get; set; }

    /// <summary>
    /// Claim ID
    /// </summary>
 public string ClaimId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number
  /// </summary>
    public int Sequence { get; set; }

  /// <summary>
  /// Product or service code
    /// </summary>
    public string ProductOrServiceCode { get; set; } = string.Empty;

    /// <summary>
    /// Product or service system
    /// </summary>
    public string? ProductOrServiceSystem { get; set; }

    /// <summary>
    /// Alternate product or service code
 /// </summary>
    public string? AltProductOrServiceCode { get; set; }

    /// <summary>
    /// Alternate product or service system
    /// </summary>
    public string? AltProductOrServiceSystem { get; set; }

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Net amount
    /// </summary>
    public decimal? Net { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

 /// <summary>
    /// Patient invoice reference system
    /// </summary>
    public string? PatientInvoiceSystem { get; set; }

    /// <summary>
 /// Patient invoice reference value
    /// </summary>
    public string? PatientInvoiceValue { get; set; }
    
    // NEW: Body Site and Sub-Site
    public string? BodySiteCode { get; set; }
    public string? BodySiteSystem { get; set; }
    public string? SubSiteCode { get; set; }
    public string? SubSiteSystem { get; set; }
    
    // NEW: Pricing Factors
    public decimal? Factor { get; set; }
    public decimal? Tax { get; set; }
  public decimal? TaxRate { get; set; }
    
    // NEW: Linkage to Other Claim Elements
    public string? DiagnosisSequence { get; set; }
    public string? InformationSequence { get; set; }
public string? ProcedureSequence { get; set; }
    
    // NEW: Device and Location
  public string? UDI { get; set; }
    public string? LocationId { get; set; }
    
    // NEW: Program Code
    public string? ProgramCode { get; set; }
    public string? ProgramCodeSystem { get; set; }
}

/// <summary>
/// DTO for creating a ClaimItem
/// </summary>
public class CreateClaimItemDto
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
    /// Product or service code
    /// </summary>
    public string ProductOrServiceCode { get; set; } = string.Empty;

    /// <summary>
    /// Product or service system
    /// </summary>
    public string? ProductOrServiceSystem { get; set; }

    /// <summary>
    /// Alternate product or service code
    /// </summary>
 public string? AltProductOrServiceCode { get; set; }

    /// <summary>
    /// Alternate product or service system
    /// </summary>
    public string? AltProductOrServiceSystem { get; set; }

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Net amount
    /// </summary>
    public decimal? Net { get; set; }

    /// <summary>
  /// Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Patient invoice reference system
    /// </summary>
    public string? PatientInvoiceSystem { get; set; }

    /// <summary>
    /// Patient invoice reference value
    /// </summary>
    public string? PatientInvoiceValue { get; set; }
    
    // NEW: Body Site and Sub-Site
    public string? BodySiteCode { get; set; }
    public string? BodySiteSystem { get; set; }
    public string? SubSiteCode { get; set; }
    public string? SubSiteSystem { get; set; }
    
    // NEW: Pricing Factors
    public decimal? Factor { get; set; }
    public decimal? Tax { get; set; }
    public decimal? TaxRate { get; set; }
    
    // NEW: Linkage to Other Claim Elements
    public string? DiagnosisSequence { get; set; }
    public string? InformationSequence { get; set; }
    public string? ProcedureSequence { get; set; }
    
    // NEW: Device and Location
    public string? UDI { get; set; }
    public string? LocationId { get; set; }
    
    // NEW: Program Code
    public string? ProgramCode { get; set; }
    public string? ProgramCodeSystem { get; set; }
}

/// <summary>
/// DTO for updating a ClaimItem
/// </summary>
public class UpdateClaimItemDto
{
    /// <summary>
    /// Sequence number
    /// </summary>
  public int? Sequence { get; set; }

/// <summary>
    /// Product or service code
    /// </summary>
    public string? ProductOrServiceCode { get; set; }

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Net amount
    /// </summary>
    public decimal? Net { get; set; }

    /// <summary>
    /// Patient invoice reference value
    /// </summary>
    public string? PatientInvoiceValue { get; set; }
    
    // NEW: Body Site and Sub-Site
    public string? BodySiteCode { get; set; }
    public string? SubSiteCode { get; set; }
    
    // NEW: Pricing Factors
public decimal? Factor { get; set; }
    public decimal? Tax { get; set; }
    public decimal? TaxRate { get; set; }
    
    // NEW: Linkage to Other Claim Elements
    public string? DiagnosisSequence { get; set; }
    public string? InformationSequence { get; set; }
    public string? ProcedureSequence { get; set; }
    
    // NEW: Device and Location
    public string? UDI { get; set; }
    public string? LocationId { get; set; }
    
    // NEW: Program Code
    public string? ProgramCode { get; set; }
}
