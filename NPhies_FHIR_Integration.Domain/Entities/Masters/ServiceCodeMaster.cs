using NPhies_FHIR_Integration.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ServiceCodeMaster - Medical services and procedures
/// Stores all medical services offered with NPHIES mappings
/// </summary>
[Table("ServiceCodeMaster")]
public class ServiceCodeMaster : BaseEntity
{
    /// <summary>
    /// Service Code (Primary identifier)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ServiceCode { get; set; } = string.Empty;

    /// <summary>
    /// Service Name
 /// </summary>
    [Required]
    [StringLength(255)]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Service Description
    /// </summary>
    [StringLength(1000)]
    public string? ServiceDescription { get; set; }

    /// <summary>
    /// Service Category (Diagnostic, Surgical, Consultation, etc.)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ServiceCategory { get; set; } = string.Empty;

 /// <summary>
  /// NPHIES Service Code (Mapped code)
    /// </summary>
    [StringLength(50)]
    public string? NphiesServiceCode { get; set; }

    /// <summary>
    /// NPHIES Service Name
    /// </summary>
    [StringLength(255)]
public string? NphiesServiceName { get; set; }

    /// <summary>
  /// NPHIES Category Code
    /// </summary>
    [StringLength(50)]
    public string? NphiesCategoryCode { get; set; }

    /// <summary>
    /// Is this service mapped to NPHIES?
    /// </summary>
    public bool IsNphiesMapped { get; set; }

    /// <summary>
    /// Mapping Validation Status (Valid, Invalid, Pending)
    /// </summary>
    [StringLength(50)]
    public string? MappingValidationStatus { get; set; } = "Pending";

    /// <summary>
  /// Default Price
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultPrice { get; set; }

    /// <summary>
  /// Currency Code (e.g., SAR)
    /// </summary>
    [StringLength(3)]
    public string CurrencyCode { get; set; } = "SAR";

    /// <summary>
    /// Requires Authorization?
    /// </summary>
    public bool IsRequiresAuthorization { get; set; }

    /// <summary>
    /// Default Authorization Validity (Days)
    /// </summary>
    public int? DefaultAuthorizationDays { get; set; }

    /// <summary>
  /// Is Active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Created By
 /// </summary>
 [StringLength(100)]
    public string? CreatedBy { get; set; }

  /// <summary>
    /// Last Modified By
    /// </summary>
    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Get display name
    /// </summary>
    public string GetDisplayName() => $"{ServiceCode} - {ServiceName}";
}
