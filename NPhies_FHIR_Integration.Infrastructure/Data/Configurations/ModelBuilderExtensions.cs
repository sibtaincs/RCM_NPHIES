using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>
/// Model Builder Extensions to apply all entity configurations
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies all entity type configurations
    /// </summary>
    public static void ApplyAllConfigurations(this ModelBuilder modelBuilder)
    {
        // Access control (roles / permissions)
        modelBuilder.ApplyConfiguration(new AppRoleConfiguration());
        modelBuilder.ApplyConfiguration(new AppPermissionConfiguration());
        modelBuilder.ApplyConfiguration(new AppRolePermissionConfiguration());

        // Core Entities
        modelBuilder.ApplyConfiguration(new PatientConfiguration());
        modelBuilder.ApplyConfiguration(new CoverageConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new LocationConfiguration());
        modelBuilder.ApplyConfiguration(new PractitionerConfiguration());
        modelBuilder.ApplyConfiguration(new MessageHeaderConfiguration());

        // Eligibility Entities
        modelBuilder.ApplyConfiguration(new CoverageEligibilityRequestConfiguration());
        modelBuilder.ApplyConfiguration(new EligibilityItemConfiguration());
        modelBuilder.ApplyConfiguration(new EligibilityItemModifierConfiguration());
        modelBuilder.ApplyConfiguration(new CoverageEligibilityResponseConfiguration());
        modelBuilder.ApplyConfiguration(new BenefitBalanceConfiguration());
        modelBuilder.ApplyConfiguration(new BenefitConfiguration());
        modelBuilder.ApplyConfiguration(new EligibilityErrorConfiguration());

        // Claim Entities
        modelBuilder.ApplyConfiguration(new EncounterConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimItemConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimItemDetailConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimDiagnosisConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimCareTeamConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimSupportingInfoConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimRelatedConfiguration());

        // Claim Response Entities
        modelBuilder.ApplyConfiguration(new ClaimResponseConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimResponseInsuranceConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimResponseAddItemConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimResponseAdjudicationConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimResponseTotalConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimResponseDiagnosisExtConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimResponseSupportingInfoExtConfiguration());

        // Task Management & Communication
        modelBuilder.ApplyConfiguration(new CancellationRequestConfiguration());
        modelBuilder.ApplyConfiguration(new CancellationResponseConfiguration());
        modelBuilder.ApplyConfiguration(new CommunicationConfiguration());
        modelBuilder.ApplyConfiguration(new CommunicationRequestConfiguration());
        modelBuilder.ApplyConfiguration(new PollingRecordConfiguration());

        // Pre-Authorization
        modelBuilder.ApplyConfiguration(new PreAuthorizationRequestConfiguration());
        modelBuilder.ApplyConfiguration(new PreAuthorizationItemConfiguration());
        modelBuilder.ApplyConfiguration(new PreAuthorizationDiagnosisConfiguration());
        modelBuilder.ApplyConfiguration(new PreAuthorizationSupportingInfoConfiguration());
        modelBuilder.ApplyConfiguration(new PreAuthorizationResponseConfiguration());
        modelBuilder.ApplyConfiguration(new PreAuthorizationResponseItemConfiguration());
        modelBuilder.ApplyConfiguration(new PreAuthorizationResponseErrorConfiguration());

        // Claim Extensions
        modelBuilder.ApplyConfiguration(new ClaimAccidentConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimItemModifierConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimProcedureConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentReconciliationConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentReconciliationDetailConfiguration());
        modelBuilder.ApplyConfiguration(new AdvancedAuthorizationConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimDocumentContentConfiguration());
        modelBuilder.ApplyConfiguration(new VisionPrescriptionConfiguration());
        modelBuilder.ApplyConfiguration(new OralDetailConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimErrorConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentNoticeConfiguration());

        // Master Data
        modelBuilder.ApplyConfiguration(new ServiceCodeMasterConfiguration());
        modelBuilder.ApplyConfiguration(new MedicationCodeMasterConfiguration());
        modelBuilder.ApplyConfiguration(new MedicalDeviceCodeMasterConfiguration());
        modelBuilder.ApplyConfiguration(new DiagnosisCodeMasterConfiguration());
        modelBuilder.ApplyConfiguration(new ModifierCodeMasterConfiguration());
        modelBuilder.ApplyConfiguration(new BenefitCodeMasterConfiguration());
        modelBuilder.ApplyConfiguration(new PayerMasterConfiguration());
        modelBuilder.ApplyConfiguration(new PayerPolicyMasterConfiguration());
        modelBuilder.ApplyConfiguration(new PolicyBenefitCoverageConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimSubmissionRulesConfiguration());
        modelBuilder.ApplyConfiguration(new NphiesCodeMappingConfiguration());
        modelBuilder.ApplyConfiguration(new ClinicMasterConfiguration());
        modelBuilder.ApplyConfiguration(new DoctorMasterConfiguration());
        modelBuilder.ApplyConfiguration(new DoctorQualificationConfiguration());
        modelBuilder.ApplyConfiguration(new ErrorCodeMasterConfiguration());

        // CodeableConcept Entities
        modelBuilder.ApplyConfiguration(new CodeSystemConfiguration());
        modelBuilder.ApplyConfiguration(new ConceptConfiguration());
        modelBuilder.ApplyConfiguration(new ValueSetConfiguration());
        modelBuilder.ApplyConfiguration(new ValueSetCodeSystemMapConfiguration());
        modelBuilder.ApplyConfiguration(new ProfileElementConfiguration());
        modelBuilder.ApplyConfiguration(new ConceptCodeFilterConfiguration());
        modelBuilder.ApplyConfiguration(new ValidationRuleConfiguration());
        modelBuilder.ApplyConfiguration(new NphiesMessageTypeConfiguration());
        modelBuilder.ApplyConfiguration(new NphiesMessageRequiredElementConfiguration());

        // User Authentication & RBAC
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new LoginAttemptConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new ApiRateLimitLogConfiguration());

        // Appeal Entities
        modelBuilder.ApplyConfiguration(new AppealRequestConfiguration());
        modelBuilder.ApplyConfiguration(new AppealStatusHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new AppealDocumentConfiguration());

        // NPHIES message audit
        modelBuilder.ApplyConfiguration(new NphiesMessageLogConfiguration());

        // RCM workflow layer
        modelBuilder.ApplyConfiguration(new NphiesTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimResponseItemConfiguration());
        modelBuilder.ApplyConfiguration(new ClaimDenialConfiguration());
        modelBuilder.ApplyConfiguration(new EligibilityCheckConfiguration());
        var rcmColumns = new RcmColumnsConfiguration();
        modelBuilder.ApplyConfiguration<Claim>(rcmColumns);
        modelBuilder.ApplyConfiguration<ClaimResponse>(rcmColumns);
        modelBuilder.ApplyConfiguration<Patient>(rcmColumns);
        modelBuilder.ApplyConfiguration<Communication>(rcmColumns);
        modelBuilder.ApplyConfiguration<CommunicationRequest>(rcmColumns);
    }
}
