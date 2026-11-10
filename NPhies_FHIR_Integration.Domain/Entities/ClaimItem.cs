namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimItem entity - represents line items in a claim
/// FHIR Resource: Claim.item
/// </summary>
public class ClaimItem : BaseEntity
{
    /// <summary>
    /// Claim ID this item belongs to
    /// </summary>
    public string ClaimId { get; set; } = string.Empty;

    /// <summary>
    /// Claim
    /// </summary>
    public Claim? Claim { get; set; }

    /// <summary>
    /// Item sequence number
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Care team sequence reference
    /// </summary>
    public int? CareTeamSequence { get; set; }

    /// <summary>
    /// Product or service code
    /// </summary>
    public string ProductOrServiceCode { get; set; } = string.Empty;

    /// <summary>
    /// Product or service system (e.g., "http://nphies.sa/terminology/CodeSystem/services")
    /// </summary>
    public string? ProductOrServiceSystem { get; set; }

    /// <summary>
    /// Product or service display text
    /// </summary>
    public string? ProductOrServiceDisplay { get; set; }

    /// <summary>
    /// Alternative coding system (e.g., provider's own system)
    /// </summary>
    public string? AltProductOrServiceCode { get; set; }

    /// <summary>
    /// Alternative coding system URL
    /// </summary>
    public string? AltProductOrServiceSystem { get; set; }

    /// <summary>
    /// Service date
    /// </summary>
    public DateTime? ServicedDate { get; set; }

    /// <summary>
    /// Service period start date
    /// </summary>
    public DateTime? ServicedPeriodStart { get; set; }

    /// <summary>
    /// Service period end date
    /// </summary>
    public DateTime? ServicedPeriodEnd { get; set; }

    /// <summary>
    /// Quantity of service
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Total net amount for this item
    /// </summary>
    public decimal? Net { get; set; }

    /// <summary>
    /// Amount patient responsible for paying (extension)
    /// </summary>
    public decimal? PatientShare { get; set; }

    /// <summary>
    /// Patient share currency
    /// </summary>
    public string? PatientShareCurrency { get; set; }

    /// <summary>
    /// Is this item part of a package (extension)
    /// </summary>
    public bool IsPackage { get; set; } = false;

    /// <summary>
    /// Is this a maternity service (extension)
    /// </summary>
    public bool IsMaternity { get; set; } = false;

    /// <summary>
    /// Item notes/remarks
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Patient invoice reference system (e.g., "http://sgh.com/patientInvoice")
    /// System URL for the patient invoice identifier
    /// </summary>
    public string? PatientInvoiceSystem { get; set; }

    /// <summary>
    /// Patient invoice reference value (e.g., "Invc-20220120/IP-1110987")
    /// Links to provider's patient invoice
    /// </summary>
    public string? PatientInvoiceValue { get; set; }

    // ========== BODY SITE AND SUB-SITE ==========
    
    /// <summary>
    /// Body site code (anatomical location where service was performed)
    /// E.g., "right eye", "left knee", "upper arm"
    /// Maps to FHIR Claim.item.bodySite
    /// </summary>
  public string? BodySiteCode { get; set; }

    /// <summary>
    /// Body site system URL
    /// E.g., "http://snomed.info/sct" for SNOMED CT codes
    /// </summary>
    public string? BodySiteSystem { get; set; }

    /// <summary>
    /// Sub-site code (more specific anatomical location)
    /// E.g., "upper outer quadrant" for breast procedures
    /// Maps to FHIR Claim.item.subSite
    /// </summary>
    public string? SubSiteCode { get; set; }

    /// <summary>
    /// Sub-site system URL
    /// </summary>
    public string? SubSiteSystem { get; set; }

    // ========== PRICING FACTORS ==========
    
    /// <summary>
    /// Price multiplier/factor (e.g., 1.5 for emergency services, 2.0 for after-hours)
/// Applied to the unit price to calculate final price
    /// Maps to FHIR Claim.item.factor
    /// </summary>
    public decimal? Factor { get; set; }

    /// <summary>
    /// Tax amount applied to this item
    /// Maps to FHIR Claim.item.tax (Money)
    /// </summary>
  public decimal? Tax { get; set; }

    /// <summary>
  /// Tax rate as percentage (e.g., 15.00 for 15% VAT)
    /// Used to calculate tax amount
    /// </summary>
    public decimal? TaxRate { get; set; }

  // ========== LINKAGE TO OTHER CLAIM ELEMENTS ==========
    
    /// <summary>
    /// Comma-separated sequence numbers of related diagnoses
    /// E.g., "1,3" links to diagnosis sequences 1 and 3
    /// Maps to FHIR Claim.item.diagnosisSequence
    /// </summary>
    public string? DiagnosisSequence { get; set; }

    /// <summary>
    /// Comma-separated sequence numbers of related supporting information
    /// E.g., "2,4" links to supporting info sequences 2 and 4
    /// Maps to FHIR Claim.item.informationSequence
    /// </summary>
    public string? InformationSequence { get; set; }

    /// <summary>
    /// Comma-separated sequence numbers of related procedures
    /// E.g., "1" links to procedure sequence 1
    /// Maps to FHIR Claim.item.procedureSequence
    /// </summary>
    public string? ProcedureSequence { get; set; }

    // ========== DEVICE AND LOCATION ==========
    
    /// <summary>
  /// Unique Device Identifier (UDI) for medical devices/implants
    /// E.g., barcode or RFID of implanted device
    /// Maps to FHIR Claim.item.udi (Reference to Device)
    /// </summary>
    public string? UDI { get; set; }

    /// <summary>
    /// Location ID where service was performed
    /// Reference to Location entity (service location)
    /// Maps to FHIR Claim.item.locationReference
    /// </summary>
    public string? LocationId { get; set; }

    /// <summary>
    /// Service location
    /// Navigation property to Location entity
    /// </summary>
    public Location? Location { get; set; }

    // ========== PROGRAM CODE ==========
    
    /// <summary>
    /// Program code (e.g., specific insurance programs, government schemes)
    /// E.g., "maternal-health", "chronic-disease-management"
    /// Maps to FHIR Claim.item.programCode
    /// </summary>
    public string? ProgramCode { get; set; }

    /// <summary>
    /// Program code system URL
 /// E.g., "http://nphies.sa/terminology/CodeSystem/program"
    /// </summary>
  public string? ProgramCodeSystem { get; set; }

    // Navigation Properties
    /// <summary>
    /// Collection of claim item details (sub-items)
    /// </summary>
    public ICollection<ClaimItemDetail> Details { get; set; } = new List<ClaimItemDetail>();
}
