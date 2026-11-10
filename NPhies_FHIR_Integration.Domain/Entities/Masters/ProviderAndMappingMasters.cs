using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// NphiesCodeMapping - Centralized NPHIES code mappings
/// </summary>
[Table("NphiesCodeMapping")]
public class NphiesCodeMapping : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string LocalCode { get; set; } = string.Empty;

    [StringLength(500)]
    public string? LocalCodeSystem { get; set; }

    [StringLength(255)]
    public string? LocalDescription { get; set; }

    [Required]
    [StringLength(50)]
    public string NphiesCode { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string NphiesCodeSystem { get; set; } = string.Empty;

    [StringLength(255)]
    public string? NphiesDescription { get; set; }

    [Required]
    [StringLength(50)]
    public string CodeType { get; set; } = string.Empty;

    public bool IsMappingValid { get; set; } = true;

    public DateTime? MappingValidationDate { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// ClinicMaster - Extended clinic/facility information
/// </summary>
[Table("ClinicMaster")]
public class ClinicMaster : BaseEntity
{
    [Required]
    [StringLength(100)] // Fixed: Changed from 450 to 100 to match Organization.Id
    [ForeignKey("Organization")]
    public string OrganizationId { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string ClinicName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ClinicCode { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ClinicType { get; set; }

    [StringLength(500)]
    public string? SpecializedServices { get; set; }

    public int? NumberOfBeds { get; set; }

    public int? NumberOfDoctors { get; set; }

    public int? NumberOfNurses { get; set; }

    [StringLength(255)]
    public string? IsCertifiedBy { get; set; }

    [StringLength(100)]
    public string? AccreditationLevel { get; set; }

    public TimeOnly? WorkingHoursFrom { get; set; }

    public TimeOnly? WorkingHoursTo { get; set; }

    public bool IsEmergencyAvailable { get; set; }

    public bool PharmacyAvailable { get; set; }

    public bool LabAvailable { get; set; }

    public bool ImagingAvailable { get; set; }

    public bool AcceptsCashPayment { get; set; }

    public bool AcceptsInsurance { get; set; }

    public bool AcceptsCardPayment { get; set; }

    public int? AvailableBeds { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    // Navigation
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    public virtual ICollection<DoctorMaster> Doctors { get; set; } = new List<DoctorMaster>();
}

/// <summary>
/// DoctorMaster - Extended doctor/practitioner information
/// </summary>
[Table("DoctorMaster")]
public class DoctorMaster : BaseEntity
{
    [Required]
    [StringLength(100)]
    [ForeignKey("Practitioner")]
    public string PractitionerId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string DoctorCode { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string DoctorName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Specialization { get; set; }

    [StringLength(100)]
    public string? SubSpecialization { get; set; }

    [StringLength(50)]
    public string? QualificationDegree { get; set; }

    [StringLength(255)]
    public string? UniversityName { get; set; }

    public int? YearsOfExperience { get; set; }

    public bool IsConsultant { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ConsultationFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FollowupFee { get; set; }

    [StringLength(3)]
    public string CurrencyCode { get; set; } = "SAR";

    [ForeignKey("Clinic")]
    public string? ClinicMasterId { get; set; }

    public bool IsAvailableForAppointments { get; set; }

    public int? AvailableSlotsPerDay { get; set; }

    [StringLength(100)]
    public string? Board { get; set; }

    [StringLength(100)]
    public string? BoardLicenseNumber { get; set; }

    public DateTime? BoardLicenseExpiry { get; set; }

    public int? ResearchPapers { get; set; }

    public bool IsTeachingMember { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    // Navigation
    [ForeignKey("PractitionerId")]
    public virtual Practitioner? Practitioner { get; set; }

    [ForeignKey("ClinicMasterId")]
    public virtual ClinicMaster? Clinic { get; set; }

    public virtual ICollection<DoctorQualification> Qualifications { get; set; } = new List<DoctorQualification>();
}

/// <summary>
/// DoctorQualification - Multiple qualifications per doctor
/// </summary>
[Table("DoctorQualification")]
public class DoctorQualification : BaseEntity
{
    [Required]
    [ForeignKey("DoctorMaster")]
    public string DoctorMasterId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string QualificationType { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string QualificationName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string UniversityName { get; set; } = string.Empty;

    [Required]
    public DateTime IssuedDate { get; set; }

    public bool IsExpiring { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [StringLength(100)]
    public string? CertificateNumber { get; set; }

    [StringLength(50)]
    public string? VerificationStatus { get; set; } = "Pending";

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation
    [ForeignKey("DoctorMasterId")]
    public virtual DoctorMaster? Doctor { get; set; }
}
