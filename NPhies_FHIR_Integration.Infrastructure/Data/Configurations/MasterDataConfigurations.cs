using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

public class ServiceCodeMasterConfiguration : IEntityTypeConfiguration<ServiceCodeMaster>
{
    public void Configure(EntityTypeBuilder<ServiceCodeMaster> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.ServiceCode).IsRequired().HasMaxLength(50);
        entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.ServiceDescription).HasMaxLength(1000);
        entity.Property(e => e.ServiceCategory).IsRequired().HasMaxLength(100);
        entity.Property(e => e.NphiesServiceCode).HasMaxLength(50);
        entity.Property(e => e.NphiesServiceName).HasMaxLength(255);
        entity.Property(e => e.NphiesCategoryCode).HasMaxLength(50);
        entity.Property(e => e.IsNphiesMapped).IsRequired();
        entity.Property(e => e.MappingValidationStatus).HasMaxLength(50);
        entity.Property(e => e.DefaultPrice).HasPrecision(18, 2);
        entity.Property(e => e.CurrencyCode).HasMaxLength(3);
        entity.Property(e => e.IsRequiresAuthorization).IsRequired();
        entity.Property(e => e.DefaultAuthorizationDays);
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.ServiceCode).IsUnique();
        entity.HasIndex(e => e.ServiceCategory);
        entity.HasIndex(e => e.NphiesServiceCode);
        entity.HasIndex(e => e.IsActive);
        entity.HasIndex(e => e.IsNphiesMapped);
        entity.HasIndex(e => e.MappingValidationStatus);
    }
}

public class MedicationCodeMasterConfiguration : IEntityTypeConfiguration<MedicationCodeMaster>
{
    public void Configure(EntityTypeBuilder<MedicationCodeMaster> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.MedicationCode).IsRequired().HasMaxLength(50);
        entity.Property(e => e.MedicationName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.ActiveIngredient).HasMaxLength(255);
        entity.Property(e => e.Strength).HasMaxLength(100);
        entity.Property(e => e.Unit).HasMaxLength(50);
        entity.Property(e => e.Form).HasMaxLength(50);
        entity.Property(e => e.Manufacturer).HasMaxLength(255);
        entity.Property(e => e.NphiesMedicationCode).HasMaxLength(50);
        entity.Property(e => e.IsNphiesMapped).IsRequired();
        entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
        entity.Property(e => e.CurrencyCode).HasMaxLength(3);
        entity.Property(e => e.IsControlledSubstance).IsRequired();
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.MedicationCode).IsUnique();
        entity.HasIndex(e => e.NphiesMedicationCode);
        entity.HasIndex(e => e.IsActive);
        entity.HasIndex(e => e.IsNphiesMapped);
        entity.HasIndex(e => e.IsControlledSubstance);
        entity.HasIndex(e => e.ActiveIngredient);
    }
}

public class MedicalDeviceCodeMasterConfiguration : IEntityTypeConfiguration<MedicalDeviceCodeMaster>
{
    public void Configure(EntityTypeBuilder<MedicalDeviceCodeMaster> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.DeviceCode).IsRequired().HasMaxLength(50);
        entity.Property(e => e.DeviceName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.DeviceDescription).HasMaxLength(1000);
        entity.Property(e => e.DeviceType).HasMaxLength(100);
        entity.Property(e => e.Manufacturer).HasMaxLength(255);
        entity.Property(e => e.NphiesDeviceCode).HasMaxLength(50);
        entity.Property(e => e.IsNphiesMapped).IsRequired();
        entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
        entity.Property(e => e.CurrencyCode).HasMaxLength(3);
        entity.Property(e => e.IsImplantable).IsRequired();
        entity.Property(e => e.IsReusable).IsRequired();
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.DeviceCode).IsUnique();
        entity.HasIndex(e => e.DeviceType);
        entity.HasIndex(e => e.NphiesDeviceCode);
        entity.HasIndex(e => e.IsActive);
        entity.HasIndex(e => e.IsNphiesMapped);
        entity.HasIndex(e => e.IsImplantable);
        entity.HasIndex(e => e.IsReusable);
    }
}

public class DiagnosisCodeMasterConfiguration : IEntityTypeConfiguration<DiagnosisCodeMaster>
{
    public void Configure(EntityTypeBuilder<DiagnosisCodeMaster> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.DiagnosisCode).IsRequired().HasMaxLength(20);
        entity.Property(e => e.DiagnosisName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.DiagnosisCategory).HasMaxLength(100);
        entity.Property(e => e.DiagnosisType).HasMaxLength(50);
        entity.Property(e => e.IsOnAdmission).IsRequired();
        entity.Property(e => e.NphiesDiagnosisCode).HasMaxLength(20);
        entity.Property(e => e.IsNphiesMapped).IsRequired();
        entity.Property(e => e.Severity).HasMaxLength(50);
        entity.Property(e => e.RequiresDocumentation).IsRequired();
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.DiagnosisCode).IsUnique();
        entity.HasIndex(e => e.NphiesDiagnosisCode);
        entity.HasIndex(e => e.DiagnosisCategory);
        entity.HasIndex(e => e.IsActive);
        entity.HasIndex(e => e.IsNphiesMapped);
        entity.HasIndex(e => e.Severity);
    }
}

public class ModifierCodeMasterConfiguration : IEntityTypeConfiguration<ModifierCodeMaster>
{
    public void Configure(EntityTypeBuilder<ModifierCodeMaster> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.ModifierCode).IsRequired().HasMaxLength(10);
        entity.Property(e => e.ModifierName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.ModifierDescription).HasMaxLength(500);
        entity.Property(e => e.ModifierType).HasMaxLength(50);
        entity.Property(e => e.ImpactOnCharges).HasMaxLength(100);
        entity.Property(e => e.ChargePercentage).HasPrecision(5, 2);
        entity.Property(e => e.NphiesModifierCode).HasMaxLength(10);
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.ModifierCode).IsUnique();
        entity.HasIndex(e => e.NphiesModifierCode);
        entity.HasIndex(e => e.ModifierType);
        entity.HasIndex(e => e.IsActive);
    }
}

public class BenefitCodeMasterConfiguration : IEntityTypeConfiguration<BenefitCodeMaster>
{
    public void Configure(EntityTypeBuilder<BenefitCodeMaster> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.BenefitCode).IsRequired().HasMaxLength(50);
        entity.Property(e => e.BenefitName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.BenefitCategory).HasMaxLength(100);
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.BenefitCode).IsUnique();
        entity.HasIndex(e => e.BenefitCategory);
        entity.HasIndex(e => e.IsActive);
    }
}

public class PayerMasterConfiguration : IEntityTypeConfiguration<PayerMaster>
{
    public void Configure(EntityTypeBuilder<PayerMaster> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.PayerId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.PayerName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.PayerNameArabic).HasMaxLength(255);
        entity.Property(e => e.PayerType).HasMaxLength(50);
        entity.Property(e => e.LicenseNumber).HasMaxLength(100);
        entity.Property(e => e.NphiesPayerId).HasMaxLength(100);
        entity.Property(e => e.NphiesConnectionStatus).HasMaxLength(50);
        entity.Property(e => e.NphiesApiEndpoint).HasMaxLength(500);
        entity.Property(e => e.IsNphiesMember).IsRequired();
        entity.Property(e => e.ContactEmail).HasMaxLength(200);
        entity.Property(e => e.ContactPhone).HasMaxLength(50);
        entity.Property(e => e.Website).HasMaxLength(200);
        entity.Property(e => e.AddressLine1).HasMaxLength(500);
        entity.Property(e => e.AddressLine2).HasMaxLength(500);
        entity.Property(e => e.City).HasMaxLength(100);
        entity.Property(e => e.State).HasMaxLength(100);
        entity.Property(e => e.Country).HasMaxLength(100);
        entity.Property(e => e.PostalCode).HasMaxLength(20);
        entity.Property(e => e.ContractStartDate);
        entity.Property(e => e.ContractEndDate);
        entity.Property(e => e.IsNphiesIntegrated).IsRequired();
        entity.Property(e => e.SupportedClaimTypes).HasMaxLength(500);
        entity.Property(e => e.SupportedEligibilityTypes).HasMaxLength(500);
        entity.Property(e => e.PaymentCycle).HasMaxLength(50);
        entity.Property(e => e.AverageTurnaroundDays);
        entity.Property(e => e.MaxClaimsPerDay).IsRequired();
        entity.Property(e => e.MaxClaimAmount).HasPrecision(18, 2);
        entity.Property(e => e.CurrencyCode).HasMaxLength(3);
        entity.Property(e => e.Notes).HasMaxLength(1000);
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.PayerId).IsUnique();
        entity.HasIndex(e => e.NphiesPayerId);
        entity.HasIndex(e => e.PayerName);
        entity.HasIndex(e => e.IsActive);
        entity.HasIndex(e => e.IsNphiesMember);
        entity.HasIndex(e => e.IsNphiesIntegrated);
        entity.HasIndex(e => e.PayerType);
        entity.HasIndex(e => e.NphiesConnectionStatus);

        entity.HasMany(e => e.Policies).WithOne(p => p.Payer).HasForeignKey(p => p.PayerMasterId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayerPolicyMasterConfiguration : IEntityTypeConfiguration<PayerPolicyMaster>
{
    public void Configure(EntityTypeBuilder<PayerPolicyMaster> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.PayerMasterId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.PolicyCode).IsRequired().HasMaxLength(100);
        entity.Property(e => e.PolicyName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.PolicyNameArabic).HasMaxLength(255);
        entity.Property(e => e.PolicyDescription).HasMaxLength(1000);
        entity.Property(e => e.PolicyType).HasMaxLength(100);
        entity.Property(e => e.CoverageType).HasMaxLength(100);
        entity.Property(e => e.CoverageLevel).HasMaxLength(100);
        entity.Property(e => e.NetworkType).HasMaxLength(100);
        entity.Property(e => e.AnnualPremium).HasPrecision(18, 2);
        entity.Property(e => e.PremiumAmount).HasPrecision(18, 2);
        entity.Property(e => e.PremiumFrequency).HasMaxLength(50);
        entity.Property(e => e.CurrencyCode).HasMaxLength(3);
        entity.Property(e => e.AnnualDeductible).HasPrecision(18, 2);
        entity.Property(e => e.MaxOutOfPocket).HasPrecision(18, 2);
        entity.Property(e => e.OutOfPocketMax).HasPrecision(18, 2);
        entity.Property(e => e.Copay).HasPrecision(18, 2);
        entity.Property(e => e.CopaymentAmount).HasPrecision(18, 2);
        entity.Property(e => e.CoinsurancePercentage).HasPrecision(5, 2);
        entity.Property(e => e.CoverageLimitPerVisit).HasPrecision(18, 2);
        entity.Property(e => e.CoverageLimitPerYear).HasPrecision(18, 2);
        entity.Property(e => e.PreAuthRequiredForAmount).HasPrecision(18, 2);
        entity.Property(e => e.RequiresPriorAuth).IsRequired();
        entity.Property(e => e.EffectiveFromDate).IsRequired();
        entity.Property(e => e.EffectiveToDate);
        entity.Property(e => e.PolicyStartDate);
        entity.Property(e => e.PolicyEndDate);
        entity.Property(e => e.IsPolicyActive).IsRequired();
        entity.Property(e => e.Notes).HasMaxLength(1000);
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.PayerMasterId);
        entity.HasIndex(e => e.PolicyCode);
        entity.HasIndex(e => e.PolicyType);
        entity.HasIndex(e => e.IsPolicyActive);
        entity.HasIndex(e => e.CoverageType);
        entity.HasIndex(e => e.EffectiveFromDate);
        entity.HasIndex(e => e.EffectiveToDate);
        entity.HasIndex(e => new { e.PayerMasterId, e.PolicyCode });

        entity.HasOne(e => e.Payer).WithMany(p => p.Policies).HasForeignKey(e => e.PayerMasterId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(e => e.BenefitCoverages).WithOne(b => b.Policy).HasForeignKey(b => b.PolicyMasterId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PolicyBenefitCoverageConfiguration : IEntityTypeConfiguration<PolicyBenefitCoverage>
{
    public void Configure(EntityTypeBuilder<PolicyBenefitCoverage> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.PolicyMasterId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.ServiceCodeMasterId).HasMaxLength(100);
        entity.Property(e => e.ServiceCategory).HasMaxLength(100);
        entity.Property(e => e.BenefitType).HasMaxLength(100);
        entity.Property(e => e.CoveragePercentage).HasPrecision(5, 2);
        entity.Property(e => e.MaxCoverageAmount).HasPrecision(18, 2);
        entity.Property(e => e.RequiresPreAuth).IsRequired();
        entity.Property(e => e.RequiresReferral).IsRequired();
        entity.Property(e => e.PreAuthValidityDays);
        entity.Property(e => e.CoverageLimitPerYear);
        entity.Property(e => e.CoverageLimitPerLifetime);
        entity.Property(e => e.IsExcluded).IsRequired();
        entity.Property(e => e.ExclusionReason).HasMaxLength(500);
        entity.Property(e => e.IsWaitingPeriodApplicable).IsRequired();
        entity.Property(e => e.WaitingPeriodDays);
        entity.Property(e => e.Notes).HasMaxLength(1000);
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.PolicyMasterId);
        entity.HasIndex(e => e.ServiceCodeMasterId);
        entity.HasIndex(e => e.ServiceCategory);
        entity.HasIndex(e => e.BenefitType);
        entity.HasIndex(e => e.RequiresPreAuth);
        entity.HasIndex(e => e.IsExcluded);
        entity.HasIndex(e => new { e.PolicyMasterId, e.ServiceCodeMasterId });

        entity.HasOne(e => e.Policy).WithMany(p => p.BenefitCoverages).HasForeignKey(e => e.PolicyMasterId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.ServiceCode).WithMany().HasForeignKey(e => e.ServiceCodeMasterId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClaimSubmissionRulesConfiguration : IEntityTypeConfiguration<ClaimSubmissionRules>
{
    public void Configure(EntityTypeBuilder<ClaimSubmissionRules> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.PayerMasterId).HasMaxLength(100);
        entity.Property(e => e.PolicyMasterId).HasMaxLength(100);
        entity.Property(e => e.RuleName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.RuleType).HasMaxLength(100);
        entity.Property(e => e.RuleCondition).HasMaxLength(1000);
        entity.Property(e => e.MaxClaimAmount).HasPrecision(18, 2);
        entity.Property(e => e.MaxItemsPerClaim);
        entity.Property(e => e.RequiresInvoice).IsRequired();
        entity.Property(e => e.RequiresMedicalReport).IsRequired();
        entity.Property(e => e.RequiresPhotos).IsRequired();
        entity.Property(e => e.MaxDaysForSubmission);
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.Priority).IsRequired();
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.RuleType);
        entity.HasIndex(e => e.PayerMasterId);
        entity.HasIndex(e => e.PolicyMasterId);
        entity.HasIndex(e => e.IsActive);
        entity.HasIndex(e => e.Priority);
        entity.HasIndex(e => new { e.PayerMasterId, e.RuleType });

        entity.HasOne(e => e.Payer).WithMany().HasForeignKey(e => e.PayerMasterId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Policy).WithMany().HasForeignKey(e => e.PolicyMasterId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class NphiesCodeMappingConfiguration : IEntityTypeConfiguration<NphiesCodeMapping>
{
    public void Configure(EntityTypeBuilder<NphiesCodeMapping> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.LocalCode).IsRequired().HasMaxLength(50);
        entity.Property(e => e.LocalCodeSystem).HasMaxLength(500);
        entity.Property(e => e.LocalDescription).HasMaxLength(255);
        entity.Property(e => e.NphiesCode).IsRequired().HasMaxLength(50);
        entity.Property(e => e.NphiesCodeSystem).IsRequired().HasMaxLength(500);
        entity.Property(e => e.NphiesDescription).HasMaxLength(255);
        entity.Property(e => e.CodeType).IsRequired().HasMaxLength(50);
        entity.Property(e => e.IsMappingValid).IsRequired();
        entity.Property(e => e.MappingValidationDate);
        entity.Property(e => e.Notes).HasMaxLength(1000);
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => new { e.NphiesCode, e.NphiesCodeSystem }).IsUnique();
        entity.HasIndex(e => e.CodeType);
        entity.HasIndex(e => e.LocalCode);
        entity.HasIndex(e => e.IsMappingValid);
        entity.HasIndex(e => new { e.LocalCode, e.CodeType });
    }
}

public class ClinicMasterConfiguration : IEntityTypeConfiguration<ClinicMaster>
{
    public void Configure(EntityTypeBuilder<ClinicMaster> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.OrganizationId).IsRequired().HasMaxLength(100); // Fixed: Changed from 450 to 100
        entity.Property(e => e.ClinicName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.ClinicCode).IsRequired().HasMaxLength(50);
        entity.Property(e => e.ClinicType).HasMaxLength(100);
        entity.Property(e => e.SpecializedServices).HasMaxLength(500);
        entity.Property(e => e.NumberOfBeds);
        entity.Property(e => e.NumberOfDoctors);
        entity.Property(e => e.NumberOfNurses);
        entity.Property(e => e.IsCertifiedBy).HasMaxLength(255);
        entity.Property(e => e.AccreditationLevel).HasMaxLength(100);
        entity.Property(e => e.WorkingHoursFrom);
        entity.Property(e => e.WorkingHoursTo);
        entity.Property(e => e.IsEmergencyAvailable).IsRequired();
        entity.Property(e => e.PharmacyAvailable).IsRequired();
        entity.Property(e => e.LabAvailable).IsRequired();
        entity.Property(e => e.ImagingAvailable).IsRequired();
        entity.Property(e => e.AcceptsCashPayment).IsRequired();
        entity.Property(e => e.AcceptsInsurance).IsRequired();
        entity.Property(e => e.AcceptsCardPayment).IsRequired();
        entity.Property(e => e.AvailableBeds);
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.ClinicCode).IsUnique();
        entity.HasIndex(e => e.ClinicType);
        entity.HasIndex(e => e.OrganizationId);
        entity.HasIndex(e => e.IsActive);
        entity.HasIndex(e => e.ClinicName);
        entity.HasIndex(e => e.IsEmergencyAvailable);

        entity.HasOne(e => e.Organization).WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(e => e.Doctors).WithOne(d => d.Clinic).HasForeignKey(d => d.ClinicMasterId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DoctorMasterConfiguration : IEntityTypeConfiguration<DoctorMaster>
{
    public void Configure(EntityTypeBuilder<DoctorMaster> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.PractitionerId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.DoctorCode).IsRequired().HasMaxLength(50);
        entity.Property(e => e.DoctorName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Specialization).HasMaxLength(100);
        entity.Property(e => e.SubSpecialization).HasMaxLength(100);
        entity.Property(e => e.QualificationDegree).HasMaxLength(50);
        entity.Property(e => e.UniversityName).HasMaxLength(255);
        entity.Property(e => e.YearsOfExperience);
        entity.Property(e => e.IsConsultant).IsRequired();
        entity.Property(e => e.ConsultationFee).HasPrecision(18, 2);
        entity.Property(e => e.FollowupFee).HasPrecision(18, 2);
        entity.Property(e => e.CurrencyCode).HasMaxLength(3);
        entity.Property(e => e.ClinicMasterId).HasMaxLength(100);
        entity.Property(e => e.IsAvailableForAppointments).IsRequired();
        entity.Property(e => e.AvailableSlotsPerDay);
        entity.Property(e => e.Board).HasMaxLength(100);
        entity.Property(e => e.BoardLicenseNumber).HasMaxLength(100);
        entity.Property(e => e.BoardLicenseExpiry);
        entity.Property(e => e.ResearchPapers);
        entity.Property(e => e.IsTeachingMember).IsRequired();
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.CreatedBy).HasMaxLength(100);
        entity.Property(e => e.ModifiedBy).HasMaxLength(100);

        entity.HasIndex(e => e.PractitionerId).IsUnique();
        entity.HasIndex(e => e.DoctorCode).IsUnique();
        entity.HasIndex(e => e.DoctorName);
        entity.HasIndex(e => e.Specialization);
        entity.HasIndex(e => e.ClinicMasterId);
        entity.HasIndex(e => e.IsActive);
        entity.HasIndex(e => e.IsAvailableForAppointments);
        entity.HasIndex(e => e.IsConsultant);

        entity.HasOne(e => e.Practitioner).WithOne(p => p.DoctorMaster).HasForeignKey<DoctorMaster>(e => e.PractitionerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Clinic).WithMany(c => c.Doctors).HasForeignKey(e => e.ClinicMasterId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(e => e.Qualifications).WithOne(q => q.Doctor).HasForeignKey(q => q.DoctorMasterId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DoctorQualificationConfiguration : IEntityTypeConfiguration<DoctorQualification>
{
    public void Configure(EntityTypeBuilder<DoctorQualification> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.DoctorMasterId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.QualificationType).IsRequired().HasMaxLength(100);
        entity.Property(e => e.QualificationName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.UniversityName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.IssuedDate).IsRequired();
        entity.Property(e => e.IsExpiring).IsRequired();
        entity.Property(e => e.ExpiryDate);
        entity.Property(e => e.CertificateNumber).HasMaxLength(100);
        entity.Property(e => e.VerificationStatus).HasMaxLength(50);
        entity.Property(e => e.CreatedBy).HasMaxLength(100);

        entity.HasIndex(e => e.DoctorMasterId);
        entity.HasIndex(e => e.QualificationType);
        entity.HasIndex(e => e.VerificationStatus);
        entity.HasIndex(e => e.IsExpiring);
        entity.HasIndex(e => e.ExpiryDate);
        entity.HasIndex(e => e.CertificateNumber);

        entity.HasOne(e => e.Doctor).WithMany(d => d.Qualifications).HasForeignKey(e => e.DoctorMasterId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ErrorCodeMasterConfiguration : IEntityTypeConfiguration<ErrorCodeMaster>
{
    public void Configure(EntityTypeBuilder<ErrorCodeMaster> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(100);
        entity.Property(e => e.ErrorCode).IsRequired().HasMaxLength(50);
        entity.Property(e => e.ErrorDescription).IsRequired().HasMaxLength(500);
        entity.Property(e => e.ErrorCategory).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Severity).IsRequired().HasMaxLength(50);
        entity.Property(e => e.IsRecoverable).IsRequired();
        entity.Property(e => e.AllowsAppeal).IsRequired();
        entity.Property(e => e.StandardAppealDays).IsRequired();
        entity.Property(e => e.RecommendedAction).HasMaxLength(1000);
        entity.Property(e => e.NphiesCodeSystem).IsRequired().HasMaxLength(500);
        entity.Property(e => e.AdjudicationImpact).HasMaxLength(50);
        entity.Property(e => e.Notes).HasMaxLength(1000);
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.CreatedDate).IsRequired();
        entity.Property(e => e.LastModifiedDate);

        entity.HasIndex(e => e.ErrorCode).IsUnique();
        entity.HasIndex(e => e.ErrorCategory);
        entity.HasIndex(e => e.Severity);
        entity.HasIndex(e => e.IsActive);
        entity.HasIndex(e => e.IsRecoverable);
        entity.HasIndex(e => e.AllowsAppeal);
        entity.HasIndex(e => e.AdjudicationImpact);
        entity.HasIndex(e => new { e.ErrorCategory, e.Severity });
    }
}
