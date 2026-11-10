using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// MedicationCodeMaster - Medical medications with NPHIES mappings
/// </summary>
[Table("MedicationCodeMaster")]
public class MedicationCodeMaster : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string MedicationCode { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string MedicationName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? ActiveIngredient { get; set; }

    [StringLength(100)]
    public string? Strength { get; set; }

    [StringLength(50)]
    public string? Unit { get; set; }

    [StringLength(50)]
    public string? Form { get; set; }

    [StringLength(255)]
    public string? Manufacturer { get; set; }

    [StringLength(50)]
    public string? NphiesMedicationCode { get; set; }

    public bool IsNphiesMapped { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [StringLength(3)]
    public string CurrencyCode { get; set; } = "SAR";

    public bool IsControlledSubstance { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// MedicalDeviceCodeMaster - Medical devices with NPHIES mappings
/// </summary>
[Table("MedicalDeviceCodeMaster")]
public class MedicalDeviceCodeMaster : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string DeviceCode { get; set; } = string.Empty;

  [Required]
    [StringLength(255)]
    public string DeviceName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? DeviceDescription { get; set; }

    [StringLength(100)]
    public string? DeviceType { get; set; }

    [StringLength(255)]
    public string? Manufacturer { get; set; }

    [StringLength(50)]
    public string? NphiesDeviceCode { get; set; }

    public bool IsNphiesMapped { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [StringLength(3)]
    public string CurrencyCode { get; set; } = "SAR";

    public bool IsImplantable { get; set; }

    public bool IsReusable { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
}
