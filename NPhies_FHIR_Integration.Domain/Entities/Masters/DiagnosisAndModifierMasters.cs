using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// DiagnosisCodeMaster - ICD diagnosis codes with NPHIES mappings
/// </summary>
[Table("DiagnosisCodeMaster")]
public class DiagnosisCodeMaster : BaseEntity
{
    [Required]
    [StringLength(20)]
    public string DiagnosisCode { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string DiagnosisName { get; set; } = string.Empty;

    [StringLength(100)]
  public string? DiagnosisCategory { get; set; }

    [StringLength(50)]
    public string? DiagnosisType { get; set; }

    public bool IsOnAdmission { get; set; }

    [StringLength(20)]
    public string? NphiesDiagnosisCode { get; set; }

    public bool IsNphiesMapped { get; set; }

[StringLength(50)]
    public string? Severity { get; set; }

    public bool RequiresDocumentation { get; set; }

  public bool IsActive { get; set; } = true;

    [StringLength(100)]
  public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// ModifierCodeMaster - Procedure modifiers
/// </summary>
[Table("ModifierCodeMaster")]
public class ModifierCodeMaster : BaseEntity
{
    [Required]
    [StringLength(10)]
    public string ModifierCode { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
public string ModifierName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ModifierDescription { get; set; }

    [StringLength(50)]
    public string? ModifierType { get; set; }

 [StringLength(100)]
    public string? ImpactOnCharges { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? ChargePercentage { get; set; }

    [StringLength(10)]
    public string? NphiesModifierCode { get; set; }

    public bool IsActive { get; set; } = true;

[StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// BenefitCodeMaster - Benefit category codes
/// </summary>
[Table("BenefitCodeMaster")]
public class BenefitCodeMaster : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string BenefitCode { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string BenefitName { get; set; } = string.Empty;

 [StringLength(100)]
public string? BenefitCategory { get; set; }

    public bool IsActive { get; set; } = true;

[StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
}
