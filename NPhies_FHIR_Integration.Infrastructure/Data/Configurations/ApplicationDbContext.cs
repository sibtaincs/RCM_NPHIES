using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.CodeableConcept.Models;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

/// <summary>
/// Application DbContext for NPhies FHIR Integration
/// Manages all domain entities and their relationships
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Constructor with DbContextOptions
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
     base.OnConfiguring(optionsBuilder);
        // Suppress pending model changes warning - Phase 3 services are application-layer only
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    // DbSets for all domain entities

    /// <summary>
    /// Patient (Member) entities
    /// </summary>
    public DbSet<Patient> Patients { get; set; } = null!;

    /// <summary>
    /// Coverage (Insurance Policy) entities
    /// </summary>
    public DbSet<Coverage> Coverages { get; set; } = null!;

    /// <summary>
    /// Organization (Provider/Insurer) entities
    /// </summary>
  public DbSet<Organization> Organizations { get; set; } = null!;

    /// <summary>
    /// Location (Service Facility) entities
    /// </summary>
    public DbSet<Location> Locations { get; set; } = null!;

    /// <summary>
    /// Practitioner (Healthcare Provider) entities
    /// </summary>
    public DbSet<Practitioner> Practitioners { get; set; } = null!;

    /// <summary>
  /// MessageHeader (FHIR Message Envelope) entities
    /// </summary>
    public DbSet<MessageHeader> MessageHeaders { get; set; } = null!;

    /// <summary>
    /// CoverageEligibilityRequest entities
    /// </summary>
    public DbSet<CoverageEligibilityRequest> CoverageEligibilityRequests { get; set; } = null!;

    /// <summary>
    /// EligibilityItem entities
    /// </summary>
    public DbSet<EligibilityItem> EligibilityItems { get; set; } = null!;

    /// <summary>
    /// EligibilityItemModifier entities
    /// </summary>
    public DbSet<EligibilityItemModifier> EligibilityItemModifiers { get; set; } = null!;

    /// <summary>
  /// CoverageEligibilityResponse entities
    /// </summary>
    public DbSet<CoverageEligibilityResponse> CoverageEligibilityResponses { get; set; } = null!;

    /// <summary>
    /// BenefitBalance entities
    /// </summary>
    public DbSet<BenefitBalance> BenefitBalances { get; set; } = null!;

    /// <summary>
    /// Benefit entities
    /// </summary>
    public DbSet<Benefit> Benefits { get; set; } = null!;

    /// <summary>
    /// EligibilityError entities
    /// </summary>
    public DbSet<EligibilityError> EligibilityErrors { get; set; } = null!;

    /// <summary>
    /// Encounter entities
    /// </summary>
    public DbSet<Encounter> Encounters { get; set; } = null!;

    /// <summary>
    /// Claim entities
    /// </summary>
    public DbSet<Claim> Claims { get; set; } = null!;

    /// <summary>
    /// ClaimItem entities
    /// </summary>
    public DbSet<ClaimItem> ClaimItems { get; set; } = null!;

    /// <summary>
    /// ClaimItemDetail entities
    /// </summary>
    public DbSet<ClaimItemDetail> ClaimItemDetails { get; set; } = null!;

    /// <summary>
    /// ClaimDiagnosis entities
    /// </summary>
    public DbSet<ClaimDiagnosis> ClaimDiagnoses { get; set; } = null!;

    /// <summary>
    /// ClaimCareTeam entities
    /// </summary>
    public DbSet<ClaimCareTeam> ClaimCareTeams { get; set; } = null!;

    /// <summary>
    /// ClaimSupportingInfo entities
    /// </summary>
    public DbSet<ClaimSupportingInfo> ClaimSupportingInfos { get; set; } = null!;

    /// <summary>
    /// ClaimRelated entities
    /// </summary>
    public DbSet<ClaimRelated> ClaimRelatedClaims { get; set; } = null!;

    /// <summary>
    /// ClaimResponse entities
/// </summary>
public DbSet<ClaimResponse> ClaimResponses { get; set; } = null!;

    /// <summary>
    /// ClaimResponseInsurance entities
  /// </summary>
    public DbSet<ClaimResponseInsurance> ClaimResponseInsurances { get; set; } = null!;

    /// <summary>
    /// ClaimResponseAddItem entities
  /// </summary>
    public DbSet<ClaimResponseAddItem> ClaimResponseAddItems { get; set; } = null!;

    /// <summary>
    /// ClaimResponseAdjudication entities
    /// </summary>
    public DbSet<ClaimResponseAdjudication> ClaimResponseAdjudications { get; set; } = null!;

    /// <summary>
    /// ClaimResponseTotal entities
    /// </summary>
    public DbSet<ClaimResponseTotal> ClaimResponseTotals { get; set; } = null!;

    /// <summary>
  /// ClaimResponseDiagnosisExt entities
    /// </summary>
    public DbSet<ClaimResponseDiagnosisExt> ClaimResponseDiagnosesExt { get; set; } = null!;

    /// <summary>
    /// ClaimResponseSupportingInfoExt entities
    /// </summary>
    public DbSet<ClaimResponseSupportingInfoExt> ClaimResponseSupportingInfosExt { get; set; } = null!;

    /// <summary>
    /// CancellationRequest entities (for claim cancellation requests)
    /// </summary>
    public DbSet<CancellationRequest> CancellationRequests { get; set; } = null!;

    /// <summary>
  /// CancellationResponse entities (for claim cancellation responses)
    /// </summary>
    public DbSet<CancellationResponse> CancellationResponses { get; set; } = null!;

    /// <summary>
    /// Communication entities (for insurer-provider communications)
    /// </summary>
    public DbSet<Communication> Communications { get; set; } = null!;

    /// <summary>
    /// CommunicationRequest entities (insurer-provider communication requests)
    /// </summary>
    public DbSet<CommunicationRequest> CommunicationRequests { get; set; } = null!;

    /// <summary>
    /// PollingRecord entities (polling request/response history and audit trail)
    /// </summary>
    public DbSet<PollingRecord> PollingRecords { get; set; } = null!;

    // MASTER DATA TABLES

    /// <summary>
    /// ServiceCodeMaster - Medical services/procedures with NPHIES mappings
    /// </summary>
    public DbSet<ServiceCodeMaster> ServiceCodeMasters { get; set; } = null!;

    /// <summary>
    /// MedicationCodeMaster - Medications with NPHIES mappings
 /// </summary>
public DbSet<MedicationCodeMaster> MedicationCodeMasters { get; set; } = null!;

    /// <summary>
    /// MedicalDeviceCodeMaster - Medical devices with NPHIES mappings
    /// </summary>
    public DbSet<MedicalDeviceCodeMaster> MedicalDeviceCodeMasters { get; set; } = null!;

    /// <summary>
    /// DiagnosisCodeMaster - ICD diagnosis codes with NPHIES mappings
/// </summary>
    public DbSet<DiagnosisCodeMaster> DiagnosisCodeMasters { get; set; } = null!;

    /// <summary>
    /// ModifierCodeMaster - Procedure modifiers
    /// </summary>
    public DbSet<ModifierCodeMaster> ModifierCodeMasters { get; set; } = null!;

    /// <summary>
    /// BenefitCodeMaster - Benefit category codes
    /// </summary>
    public DbSet<BenefitCodeMaster> BenefitCodeMasters { get; set; } = null!;

    /// <summary>
    /// PayerMaster - Insurance companies/payers
    /// </summary>
    public DbSet<PayerMaster> PayerMasters { get; set; } = null!;

  /// <summary>
    /// PayerPolicyMaster - Insurance policies
    /// </summary>
  public DbSet<PayerPolicyMaster> PayerPolicyMasters { get; set; } = null!;

    /// <summary>
    /// PolicyBenefitCoverage - Benefits covered under each policy
    /// </summary>
    public DbSet<PolicyBenefitCoverage> PolicyBenefitCoverages { get; set; } = null!;

    /// <summary>
    /// ClaimSubmissionRules - Validation rules for claims
    /// </summary>
    public DbSet<ClaimSubmissionRules> ClaimSubmissionRules { get; set; } = null!;

    /// <summary>
    /// NphiesCodeMapping - Centralized code mappings
    /// </summary>
    public DbSet<NphiesCodeMapping> NphiesCodeMappings { get; set; } = null!;

    /// <summary>
    /// ClinicMaster - Extended clinic information
    /// </summary>
    public DbSet<ClinicMaster> ClinicMasters { get; set; } = null!;

    /// <summary>
    /// DoctorMaster - Extended doctor information
    /// </summary>
    public DbSet<DoctorMaster> DoctorMasters { get; set; } = null!;

    /// <summary>
    /// DoctorQualification - Doctor qualifications
    /// </summary>
    public DbSet<DoctorQualification> DoctorQualifications { get; set; } = null!;

    /// <summary>
    /// ErrorCodeMaster - NPHIES error codes (1,682 codes)
/// </summary>
    public DbSet<ErrorCodeMaster> ErrorCodeMasters { get; set; } = null!;

    /// <summary>
    /// CodeSystem - Terminology source metadata (CodeSystems like HL7, NPHIES, WHO)
    /// </summary>
    public DbSet<CodeSystemEntity> CodeSystems { get; set; } = null!;

    /// <summary>
    /// Concept - Individual codes within each CodeSystem (~50,000+ codes)
    /// </summary>
    public DbSet<ConceptEntity> Concepts { get; set; } = null!;


  /// <summary>
    /// ValueSet - Logical groups of codes for specific use cases
    /// </summary>
    public DbSet<ValueSetEntity> ValueSets { get; set; } = null!;

    /// <summary>
    /// ValueSetCodeSystemMap - Links ValueSets to CodeSystems (M:N relationship)
    /// </summary>
    public DbSet<ValueSetCodeSystemMapEntity> ValueSetCodeSystemMaps { get; set; } = null!;

  /// <summary>
    /// ProfileElement - FHIR path bindings to ValueSets
    /// </summary>
    public DbSet<ProfileElementEntity> ProfileElements { get; set; } = null!;

    /// <summary>
    /// ConceptCodeFilter - Specific code restrictions for ValueSets
    /// </summary>
    public DbSet<ConceptCodeFilterEntity> ConceptCodeFilters { get; set; } = null!;

  /// <summary>
    /// ValidationRule - NPHIES-specific validation constraints (1,682+ rules)
    /// </summary>
    public DbSet<ValidationRuleEntity> ValidationRules { get; set; } = null!;

    /// <summary>
    /// NphiesMessageType - Message type definitions (eligibility, claim, etc.)
    /// </summary>
    public DbSet<NphiesMessageTypeEntity> NphiesMessageTypes { get; set; } = null!;

    /// <summary>
    /// NphiesMessageRequiredElement - Required fields per message type
    /// </summary>
    public DbSet<NphiesMessageRequiredElementEntity> NphiesMessageRequiredElements { get; set; } = null!;

    /// <summary>
    /// User entities
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Access-control roles (data-driven role catalogue).
    /// </summary>
    public DbSet<NPhies_FHIR_Integration.Domain.Entities.Access.AppRole> AppRoles { get; set; } = null!;

    /// <summary>
    /// Access-control permissions.
    /// </summary>
    public DbSet<NPhies_FHIR_Integration.Domain.Entities.Access.AppPermission> AppPermissions { get; set; } = null!;

    /// <summary>
    /// Role-to-permission grants.
    /// </summary>
 public DbSet<NPhies_FHIR_Integration.Domain.Entities.Access.AppRolePermission> AppRolePermissions { get; set; } = null!;

    /// <summary>
    /// Refresh token entities
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// Login attempt tracking
    /// </summary>
    public DbSet<LoginAttempt> LoginAttempts { get; set; } = null!;

    /// <summary>
    /// Audit logs
    /// </summary>
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    /// <summary>
    /// API rate limit logs
  /// </summary>
    public DbSet<ApiRateLimitLog> RateLimitLogs { get; set; } = null!;

    // PRE-AUTHORIZATION ENTITIES

    /// <summary>
    /// Pre-Authorization Requests
    /// </summary>
    public DbSet<PreAuthorizationRequest> PreAuthorizationRequests { get; set; } = null!;

    /// <summary>
    /// Pre-Authorization Items
    /// </summary>
    public DbSet<PreAuthorizationItem> PreAuthorizationItems { get; set; } = null!;

    /// <summary>
    /// Pre-Authorization Diagnoses
    /// </summary>
    public DbSet<PreAuthorizationDiagnosis> PreAuthorizationDiagnoses { get; set; } = null!;

    /// <summary>
    /// Pre-Authorization Supporting Info
    /// </summary>
    public DbSet<PreAuthorizationSupportingInfo> SupportingInfos { get; set; } = null!;

    /// <summary>
    /// Pre-Authorization Responses
    /// </summary>
    public DbSet<PreAuthorizationResponse> PreAuthorizationResponses { get; set; } = null!;

    /// <summary>
    /// Pre-Authorization Response Items
    /// </summary>
    public DbSet<PreAuthorizationResponseItem> PreAuthorizationResponseItems { get; set; } = null!;

    /// <summary>
  /// Pre-Authorization Response Errors
    /// </summary>
    public DbSet<PreAuthorizationResponseError> PreAuthorizationResponseErrors { get; set; } = null!;

  // CLAIM EXTENSION ENTITIES

    /// <summary>
    /// Claim Accidents
    /// </summary>
    public DbSet<ClaimAccident> ClaimAccidents { get; set; } = null!;

    /// <summary>
    /// Claim Item Modifiers
    /// </summary>
    public DbSet<ClaimItemModifier> ClaimItemModifiers { get; set; } = null!;

    /// <summary>
    /// Claim Procedures
/// </summary>
    public DbSet<ClaimProcedure> ClaimProcedures { get; set; } = null!;

    /// <summary>
    /// Payment Reconciliations
    /// </summary>
    public DbSet<PaymentReconciliation> PaymentReconciliations { get; set; } = null!;

    /// <summary>
    /// Payment Reconciliation Details
  /// </summary>
 public DbSet<PaymentReconciliationDetail> PaymentReconciliationDetails { get; set; } = null!;

    /// <summary>Insurer-initiated (advanced) authorizations received via poll.</summary>
    public DbSet<AdvancedAuthorization> AdvancedAuthorizations { get; set; } = null!;
    public DbSet<ClaimDocument> ClaimDocuments { get; set; } = null!;
    public DbSet<ClaimDocumentContent> ClaimDocumentContents { get; set; } = null!;

    /// <summary>
    /// Vision Prescriptions
    /// </summary>
    public DbSet<VisionPrescription> VisionPrescriptions { get; set; } = null!;

    /// <summary>
    /// Oral/Dental Details
    /// </summary>
    public DbSet<OralDetail> OralDetails { get; set; } = null!;

    /// <summary>
    /// Claim Errors
    /// </summary>
    public DbSet<ClaimError> ClaimErrors { get; set; } = null!;

    /// <summary>
    /// Payment Notices
    /// </summary>
    public DbSet<PaymentNotice> PaymentNotices { get; set; } = null!;

    /// <summary>
  /// Appeal Requests
    /// </summary>
    public DbSet<AppealRequest> AppealRequests { get; set; } = null!;

 /// <summary>
    /// Appeal Status History
    /// </summary>
    public DbSet<AppealStatusHistory> AppealStatusHistories { get; set; } = null!;

    /// <summary>
/// Appeal Documents
    /// </summary>
    public DbSet<AppealDocument> AppealDocuments { get; set; } = null!;

    /// <summary>
    /// Raw NPHIES request/response message audit log
    /// </summary>
    public DbSet<NphiesMessageLog> NphiesMessageLogs { get; set; } = null!;

    // RCM workflow layer
    public DbSet<NphiesTransaction> NphiesTransactions { get; set; } = null!;
    public DbSet<ClaimResponseItem> ClaimResponseItems { get; set; } = null!;
    public DbSet<ClaimDenial> ClaimDenials { get; set; } = null!;
    public DbSet<EligibilityCheck> EligibilityChecks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
  base.OnModelCreating(modelBuilder);

        try
        {
   // Apply all entity configurations using the extension method
            modelBuilder.ApplyAllConfigurations();

  // GLOBAL: Set all Id columns (not yet explicitly configured) to HasMaxLength(100)
    // This fixes FK column length mismatches systematically
   foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
       var idProperty = entity.FindProperty("Id");
    if (idProperty?.GetMaxLength() == null && idProperty?.ClrType == typeof(string))
  {
         idProperty.SetMaxLength(100);
 }
    }
        }
     catch (Exception ex)
        {
   // Log the error for debugging
            Console.WriteLine($"? Error in OnModelCreating: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
     throw;
 }
    }
}
