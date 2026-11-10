using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Domain.CodeableConcept.Models;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Seeders;

/// <summary>
/// Single consolidated database seeder for all NPHIES master &amp; reference data.
/// Idempotent: each section only seeds when its anchor table is empty, so it is
/// safe to run on every application startup.
///
/// Sections:
///   1. Service Codes
///   2. Diagnosis Codes (ICD-10)
///   3. Medication Codes
///   4. Modifier Codes
///   5. Benefit Codes
///   6. Payers &amp; Policies
///   7. Providers (Organization -> Practitioner -> Clinic -> Doctor -> Qualification)
///   8. CodeableConcepts (CodeSystem -> Concept -> ValueSet -> Map -> ProfileElement -> MessageType -> RequiredElement)
/// </summary>
public static class DatabaseSeeder
{
    private const string SeedUser = "SYSTEM_SEED";

    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("🌱 Database seeding started...");

        await SeedServiceCodesAsync(context, logger);
        await SeedDiagnosisCodesAsync(context, logger);
        await SeedMedicationCodesAsync(context, logger);
        await SeedModifierCodesAsync(context, logger);
        await SeedBenefitCodesAsync(context, logger);
        await SeedMedicalDeviceCodesAsync(context, logger);
        await SeedNphiesCodeMappingsAsync(context, logger);
        await SeedErrorCodesAsync(context, logger);
        await SeedPayersAndPoliciesAsync(context, logger);
        await SeedPolicyBenefitCoveragesAsync(context, logger);
        await SeedClaimSubmissionRulesAsync(context, logger);
        await SeedProvidersAsync(context, logger);
        await SeedCodeableConceptsAsync(context, logger);
        await SeedValidationRulesAsync(context, logger);

        logger.LogInformation("✅ Database seeding completed.");
    }

    // ─────────────────────────────────────────────────────────────
    // 1. Service Codes
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedServiceCodesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.ServiceCodeMasters.AnyAsync())
        {
            logger.LogInformation("↩️  ServiceCodeMasters already populated. Skipping.");
            return;
        }

        var services = new List<ServiceCodeMaster>
     {
          NewService("99213", "Office Visit - Established Patient", "Consultation", 150.00m, "83100-00-10"),
         NewService("99203", "Office Visit - New Patient", "Consultation", 250.00m, "83100-00-00"),
            NewService("80050", "General Health Panel", "Diagnostic", 200.00m, "73050-00-10"),
       NewService("85025", "Complete Blood Count (CBC)", "Laboratory", 75.00m, "73025-00-00"),
      NewService("71046", "Chest X-Ray, 2 Views", "Radiology", 180.00m, "56010-00-00"),
            NewService("93000", "Electrocardiogram (ECG)", "Diagnostic", 120.00m, "11700-00-00"),
            NewService("70450", "CT Scan Head/Brain", "Radiology", 850.00m, "56001-00-00", requiresAuth: true, authDays: 30),
         NewService("29881", "Knee Arthroscopy", "Surgical", 6500.00m, "49560-00-00", requiresAuth: true, authDays: 60),
        };

        await context.ServiceCodeMasters.AddRangeAsync(services);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded {Count} service codes.", services.Count);
    }

    private static ServiceCodeMaster NewService(
        string code, string name, string category, decimal price,
        string? nphiesCode, bool requiresAuth = false, int? authDays = null) => new()
        {
            ServiceCode = code,
            ServiceName = name,
            ServiceCategory = category,
            DefaultPrice = price,
            CurrencyCode = "SAR",
            NphiesServiceCode = nphiesCode,
            NphiesServiceName = name,
            IsNphiesMapped = nphiesCode is not null,
            MappingValidationStatus = nphiesCode is not null ? "Valid" : "Pending",
            IsRequiresAuthorization = requiresAuth,
            DefaultAuthorizationDays = authDays,
            IsActive = true,
            CreatedBy = SeedUser
        };

    // ─────────────────────────────────────────────────────────────
    // 2. Diagnosis Codes (ICD-10)
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedDiagnosisCodesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.DiagnosisCodeMasters.AnyAsync())
        {
            logger.LogInformation("↩️  DiagnosisCodeMasters already populated. Skipping.");
            return;
        }

        var diagnoses = new List<DiagnosisCodeMaster>
        {
      NewDiagnosis("E11.9", "Type 2 diabetes mellitus without complications", "Endocrine", "Chronic"),
      NewDiagnosis("I10", "Essential (primary) hypertension", "Circulatory", "Chronic"),
            NewDiagnosis("J06.9", "Acute upper respiratory infection, unspecified", "Respiratory", "Acute"),
       NewDiagnosis("K21.9", "Gastro-esophageal reflux disease without esophagitis", "Digestive", "Chronic"),
            NewDiagnosis("M54.5", "Low back pain", "Musculoskeletal", "Acute"),
  NewDiagnosis("R51", "Headache", "Symptoms", "Acute"),
            NewDiagnosis("J45.909", "Unspecified asthma, uncomplicated", "Respiratory", "Chronic"),
      NewDiagnosis("N39.0", "Urinary tract infection, site not specified", "Genitourinary", "Acute"),
        };

        await context.DiagnosisCodeMasters.AddRangeAsync(diagnoses);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded {Count} diagnosis codes.", diagnoses.Count);
    }

    private static DiagnosisCodeMaster NewDiagnosis(
        string code, string name, string category, string severity) => new()
        {
            DiagnosisCode = code,
            DiagnosisName = name,
            DiagnosisCategory = category,
            DiagnosisType = "Principal",
            Severity = severity,
            NphiesDiagnosisCode = code,
            IsNphiesMapped = true,
            RequiresDocumentation = false,
            IsActive = true,
            CreatedBy = SeedUser
        };

    // ─────────────────────────────────────────────────────────────
    // 3. Medication Codes
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedMedicationCodesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.MedicationCodeMasters.AnyAsync())
        {
            logger.LogInformation("↩️  MedicationCodeMasters already populated. Skipping.");
            return;
        }

        var meds = new List<MedicationCodeMaster>
  {
     NewMed("06285051", "Metformin 500mg Tablet", "Metformin", "500mg", "Tablet", 25.00m),
            NewMed("06285052", "Amlodipine 5mg Tablet", "Amlodipine", "5mg", "Tablet", 30.00m),
            NewMed("06285053", "Amoxicillin 500mg Capsule", "Amoxicillin", "500mg", "Capsule", 18.00m),
NewMed("06285054", "Omeprazole 20mg Capsule", "Omeprazole", "20mg", "Capsule", 22.00m),
     NewMed("06285055", "Paracetamol 500mg Tablet", "Paracetamol", "500mg", "Tablet", 8.00m),
            NewMed("06285056", "Salbutamol 100mcg Inhaler", "Salbutamol", "100mcg", "Inhaler", 45.00m),
        };

        await context.MedicationCodeMasters.AddRangeAsync(meds);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded {Count} medication codes.", meds.Count);
    }

    private static MedicationCodeMaster NewMed(
           string code, string name, string ingredient, string strength, string form, decimal price) => new()
           {
               MedicationCode = code,
               MedicationName = name,
               ActiveIngredient = ingredient,
               Strength = strength,
               Unit = "mg",
               Form = form,
               Manufacturer = "Generic",
               NphiesMedicationCode = code,
               IsNphiesMapped = true,
               UnitPrice = price,
               CurrencyCode = "SAR",
               IsControlledSubstance = false,
               IsActive = true,
               CreatedBy = SeedUser
           };

    // ─────────────────────────────────────────────────────────────
    // 4. Modifier Codes
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedModifierCodesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.ModifierCodeMasters.AnyAsync())
        {
            logger.LogInformation("↩️  ModifierCodeMasters already populated. Skipping.");
            return;
        }

        var modifiers = new List<ModifierCodeMaster>
        {
  NewModifier("25", "Significant, Separately Identifiable E/M Service", "Procedural", null),
      NewModifier("59", "Distinct Procedural Service", "Procedural", null),
       NewModifier("50", "Bilateral Procedure", "Procedural", 150m),
      NewModifier("RT", "Right Side", "Anatomical", null),
            NewModifier("LT", "Left Side", "Anatomical", null),
   };

        await context.ModifierCodeMasters.AddRangeAsync(modifiers);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded {Count} modifier codes.", modifiers.Count);
    }

    private static ModifierCodeMaster NewModifier(
        string code, string name, string type, decimal? chargePct) => new()
        {
            ModifierCode = code,
            ModifierName = name,
            ModifierType = type,
            ImpactOnCharges = chargePct.HasValue ? "Increase" : "None",
            ChargePercentage = chargePct,
            NphiesModifierCode = code,
            IsActive = true,
            CreatedBy = SeedUser
        };

    // ─────────────────────────────────────────────────────────────
    // 5. Benefit Codes
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedBenefitCodesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.BenefitCodeMasters.AnyAsync())
        {
            logger.LogInformation("↩️  BenefitCodeMasters already populated. Skipping.");
            return;
        }

        var benefits = new List<BenefitCodeMaster>
        {
       NewBenefit("1", "Medical Care", "Outpatient"),
            NewBenefit("2", "Surgical", "Inpatient"),
            NewBenefit("3", "Consultation", "Outpatient"),
            NewBenefit("4", "Diagnostic Dental", "Dental"),
            NewBenefit("5", "Pharmacy", "Pharmacy"),
            NewBenefit("14", "Renal Dialysis", "Specialized"),
       NewBenefit("23", "Diagnostic Imaging", "Radiology"),
        };

        await context.BenefitCodeMasters.AddRangeAsync(benefits);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded {Count} benefit codes.", benefits.Count);
    }

    private static BenefitCodeMaster NewBenefit(string code, string name, string category) => new()
    {
        BenefitCode = code,
        BenefitName = name,
        BenefitCategory = category,
        IsActive = true,
        CreatedBy = SeedUser
    };

    // ─────────────────────────────────────────────────────────────
    // 6. Payers & Policies
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedPayersAndPoliciesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.PayerMasters.AnyAsync())
        {
            logger.LogInformation("↩️  PayerMasters already populated. Skipping.");
            return;
        }

        var bupa = new PayerMaster
        {
            PayerId = "PAY-BUPA",
            PayerName = "Bupa Arabia",
            PayerNameArabic = "بوبا العربية",
            PayerType = "Insurance",
            LicenseNumber = "TMLI-1001",
            NphiesPayerId = "INS-FHIR-0001",
            NphiesConnectionStatus = "Connected",
            IsNphiesMember = true,
            IsNphiesIntegrated = true,
            ContactEmail = "providers@bupa.com.sa",
            City = "Riyadh",
            Country = "Saudi Arabia",
            PaymentCycle = "Monthly",
            MaxClaimsPerDay = 5000,
            MaxClaimAmount = 500000m,
            CurrencyCode = "SAR",
            IsActive = true,
            CreatedBy = SeedUser
        };

        var tawuniya = new PayerMaster
        {
            PayerId = "PAY-TAWN",
            PayerName = "Tawuniya",
            PayerNameArabic = "التعاونية",
            PayerType = "Insurance",
            LicenseNumber = "TMLI-1002",
            NphiesPayerId = "INS-FHIR-0002",
            NphiesConnectionStatus = "Connected",
            IsNphiesMember = true,
            IsNphiesIntegrated = true,
            ContactEmail = "providers@tawuniya.com.sa",
            City = "Riyadh",
            Country = "Saudi Arabia",
            PaymentCycle = "Monthly",
            MaxClaimsPerDay = 4000,
            MaxClaimAmount = 400000m,
            CurrencyCode = "SAR",
            IsActive = true,
            CreatedBy = SeedUser
        };

        bupa.Policies.Add(NewPolicy(bupa.Id, "BUPA-GOLD", "Bupa Gold Plan", 15000m, 500m));
        bupa.Policies.Add(NewPolicy(bupa.Id, "BUPA-SILVER", "Bupa Silver Plan", 8000m, 1000m));
        tawuniya.Policies.Add(NewPolicy(tawuniya.Id, "TAWN-PREMIUM", "Tawuniya Premium Plan", 20000m, 250m));

        await context.PayerMasters.AddRangeAsync(bupa, tawuniya);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded 2 payers with 3 policies.");
    }

    private static PayerPolicyMaster NewPolicy(
 string payerId, string code, string name, decimal premium, decimal deductible) => new()
 {
     PayerMasterId = payerId,
     PolicyCode = code,
     PolicyName = name,
     PolicyType = "Group",
     CoverageType = "Comprehensive",
     NetworkType = "PPO",
     AnnualPremium = premium,
     PremiumFrequency = "Annual",
     CurrencyCode = "SAR",
     AnnualDeductible = deductible,
     CoinsurancePercentage = 20m,
     RequiresPriorAuth = true,
     EffectiveFromDate = new DateTime(DateTime.UtcNow.Year, 1, 1),
     EffectiveToDate = new DateTime(DateTime.UtcNow.Year, 12, 31),
     IsPolicyActive = true,
     CreatedBy = SeedUser
 };

    // ─────────────────────────────────────────────────────────────
    // 6a. Medical Device Codes
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedMedicalDeviceCodesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.MedicalDeviceCodeMasters.AnyAsync())
        {
            logger.LogInformation("↩️  MedicalDeviceCodeMasters already populated. Skipping.");
            return;
        }

        var devices = new List<MedicalDeviceCodeMaster>
        {
        NewDevice("DEV-0001", "Cardiac Stent (Drug-Eluting)", "Implant", 4500m, implantable: true, reusable: false),
       NewDevice("DEV-0002", "Knee Prosthesis", "Orthopedic Implant", 12000m, implantable: true, reusable: false),
            NewDevice("DEV-0003", "Infusion Pump", "Equipment", 3500m, implantable: false, reusable: true),
            NewDevice("DEV-0004", "Blood Glucose Monitor", "Diagnostic", 250m, implantable: false, reusable: true),
 NewDevice("DEV-0005", "Surgical Mesh", "Implant", 800m, implantable: true, reusable: false),
  };

        await context.MedicalDeviceCodeMasters.AddRangeAsync(devices);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded {Count} medical device codes.", devices.Count);
    }

    private static MedicalDeviceCodeMaster NewDevice(
  string code, string name, string type, decimal price, bool implantable, bool reusable) => new()
  {
      DeviceCode = code,
      DeviceName = name,
      DeviceType = type,
      Manufacturer = "Generic",
      NphiesDeviceCode = code,
      IsNphiesMapped = true,
      UnitPrice = price,
      CurrencyCode = "SAR",
      IsImplantable = implantable,
      IsReusable = reusable,
      IsActive = true,
      CreatedBy = SeedUser
  };

    // ─────────────────────────────────────────────────────────────
    // 6b. NPHIES Code Mappings
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedNphiesCodeMappingsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.NphiesCodeMappings.AnyAsync())
        {
            logger.LogInformation("↩️  NphiesCodeMappings already populated. Skipping.");
            return;
        }

        var mappings = new List<NphiesCodeMapping>
    {
 NewMapping("99213", "Office Visit - Established", "83100-00-10", "Established Patient Consultation", "Service"),
            NewMapping("85025", "CBC", "73025-00-00", "Complete Blood Count", "Service"),
            NewMapping("E11.9", "Type 2 Diabetes", "E11.9", "Type 2 diabetes mellitus", "Diagnosis"),
            NewMapping("I10", "Hypertension", "I10", "Essential hypertension", "Diagnosis"),
         NewMapping("06285051", "Metformin 500mg", "06285051", "Metformin 500mg Tablet", "Medication"),
        };

        await context.NphiesCodeMappings.AddRangeAsync(mappings);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded {Count} NPHIES code mappings.", mappings.Count);
    }

    private static NphiesCodeMapping NewMapping(
           string localCode, string localDesc, string nphiesCode, string nphiesDesc, string codeType) => new()
           {
               LocalCode = localCode,
               LocalCodeSystem = "http://provider.local/codes",
               LocalDescription = localDesc,
               NphiesCode = nphiesCode,
               NphiesCodeSystem = "http://nphies.sa/terminology/CodeSystem/" + codeType.ToLower(),
               NphiesDescription = nphiesDesc,
               CodeType = codeType,
               IsMappingValid = true,
               MappingValidationDate = DateTime.UtcNow,
               CreatedBy = SeedUser
           };

    // ─────────────────────────────────────────────────────────────
    // 6c. Error Codes
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedErrorCodesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.ErrorCodeMasters.AnyAsync())
        {
            logger.LogInformation("↩️  ErrorCodeMasters already populated. Skipping.");
            return;
        }

        var errors = new List<ErrorCodeMaster>
{
    NewError("AD-1-1", "Diagnosis inconsistent with procedure", "adjudication", "Error", true, "Update diagnosis to be consistent with procedure", "Denied"),
      NewError("CV-1-1", "Coverage not found", "coverage", "Error", false, "Verify patient coverage details", "Denied"),
            NewError("AU-1-1", "Prior authorization required", "authorization", "Error", true, "Obtain prior authorization before resubmission", "Denied"),
    NewError("SB-1-1", "Duplicate claim", "submission", "Error", false, "Check for previously submitted claim", "Denied"),
  NewError("BF-1-1", "Service not covered", "benefit", "Error", false, "Service is excluded under the policy", "Denied"),
            NewError("NW-1-1", "Provider out of network", "network", "Warning", true, "Use in-network provider or obtain approval", "Partial"),
     NewError("VL-1-1", "Missing required field", "validation", "Error", true, "Complete all required fields and resubmit", "Denied"),
        };

        await context.ErrorCodeMasters.AddRangeAsync(errors);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded {Count} error codes.", errors.Count);
    }

    private static ErrorCodeMaster NewError(
        string code, string description, string category, string severity,
        bool recoverable, string action, string impact) => new()
        {
            ErrorCode = code,
            ErrorDescription = description,
            ErrorCategory = category,
            Severity = severity,
            IsRecoverable = recoverable,
            AllowsAppeal = true,
            StandardAppealDays = 60,
            RecommendedAction = action,
            NphiesCodeSystem = "http://nphies.sa/terminology/CodeSystem/adjudication-error",
            AdjudicationImpact = impact,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

    // ─────────────────────────────────────────────────────────────
    // 6d. Policy Benefit Coverages (depends on seeded policies)
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedPolicyBenefitCoveragesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.PolicyBenefitCoverages.AnyAsync())
        {
            logger.LogInformation("↩️  PolicyBenefitCoverages already populated. Skipping.");
            return;
        }

        var policies = await context.PayerPolicyMasters.ToListAsync();
        if (policies.Count == 0)
        {
            logger.LogWarning("⚠️  No policies found; skipping PolicyBenefitCoverage seed.");
            return;
        }

        var coverages = new List<PolicyBenefitCoverage>();
        foreach (var policy in policies)
        {
            coverages.Add(NewCoverage(policy.Id, "Consultation", "Medical Care", 100m, requiresPreAuth: false));
            coverages.Add(NewCoverage(policy.Id, "Laboratory", "Diagnostic", 80m, requiresPreAuth: false));
            coverages.Add(NewCoverage(policy.Id, "Radiology", "Diagnostic", 80m, requiresPreAuth: true));
            coverages.Add(NewCoverage(policy.Id, "Surgical", "Surgery", 90m, requiresPreAuth: true));
        }

        await context.PolicyBenefitCoverages.AddRangeAsync(coverages);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded {Count} policy benefit coverages across {P} policies.",
            coverages.Count, policies.Count);
    }

    private static PolicyBenefitCoverage NewCoverage(
        string policyId, string serviceCategory, string benefitType, decimal coveragePct, bool requiresPreAuth) => new()
        {
            PolicyMasterId = policyId,
            ServiceCategory = serviceCategory,
            BenefitType = benefitType,
            CoveragePercentage = coveragePct,
            RequiresPreAuth = requiresPreAuth,
            RequiresReferral = false,
            PreAuthValidityDays = requiresPreAuth ? 30 : null,
            IsExcluded = false,
            IsWaitingPeriodApplicable = false,
            CreatedBy = SeedUser
        };

    // ─────────────────────────────────────────────────────────────
    // 6e. Claim Submission Rules (depends on seeded payers)
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedClaimSubmissionRulesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.ClaimSubmissionRules.AnyAsync())
        {
            logger.LogInformation("↩️  ClaimSubmissionRules already populated. Skipping.");
            return;
        }

        var payers = await context.PayerMasters.ToListAsync();
        if (payers.Count == 0)
        {
            logger.LogWarning("⚠️  No payers found; skipping ClaimSubmissionRules seed.");
            return;
        }

        var rules = new List<ClaimSubmissionRules>();
        foreach (var payer in payers)
        {
            rules.Add(NewRule(payer.Id, "Max Claim Amount", "Financial", "Claim total must not exceed limit", 500000m, 50, 1));
            rules.Add(NewRule(payer.Id, "Timely Filing", "Temporal", "Claim must be submitted within 90 days of service", null, null, 2, maxDays: 90));
            rules.Add(NewRule(payer.Id, "Medical Report Required", "Documentation", "High-value claims require a medical report", 10000m, null, 3, requiresReport: true));
        }

        await context.ClaimSubmissionRules.AddRangeAsync(rules);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded {Count} claim submission rules across {P} payers.",
           rules.Count, payers.Count);
    }

    private static ClaimSubmissionRules NewRule(
        string payerId, string ruleName, string ruleType, string condition,
     decimal? maxAmount, int? maxItems, int priority,
  int? maxDays = null, bool requiresReport = false) => new()
  {
      PayerMasterId = payerId,
      RuleName = ruleName,
      RuleType = ruleType,
      RuleCondition = condition,
      MaxClaimAmount = maxAmount,
      MaxItemsPerClaim = maxItems,
      RequiresInvoice = true,
      RequiresMedicalReport = requiresReport,
      RequiresPhotos = false,
      MaxDaysForSubmission = maxDays,
      IsActive = true,
      Priority = priority,
      CreatedBy = SeedUser
  };

    // ─────────────────────────────────────────────────────────────
    // 7. Providers (Organization -> Practitioner -> Clinic -> Doctor -> Qualification)
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedProvidersAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.ClinicMasters.AnyAsync())
        {
            logger.LogInformation("↩️  ClinicMasters already populated. Skipping provider seed.");
            return;
        }

        var org = new Organization
        {
            OrganizationName = "Al Noor Medical Center",
            LicenseNumber = "PROV-10001",
            LicenseSystem = "http://nphies.sa/license/provider-license",
            OrganizationType = "prov",
            SpecializationType = "Hospital",
            NphiesOrganizationId = "PR-FHIR-1001",
            NphiesProviderId = "PR-FHIR-1001",
            Email = "info@alnoor.sa",
            PhoneNumber = "+966112223333",
            AddressLine1 = "King Fahd Road",
            City = "Riyadh",
            State = "Riyadh",
            PostalCode = "11564",
            AddressCountry = "SA",
            Status = "active"
        };
        await context.Organizations.AddAsync(org);

        var practitioner1 = new Practitioner
        {
            FirstName = "Ahmed",
            LastName = "Al-Rashid",
            LicenseNumber = "SCFHS-55001",
            LicenseSystem = "http://nphies.sa/license/practitioner-license",
            Specialization = "Cardiology",
            Qualification = "MD, FRCP",
            Title = "Dr.",
            Email = "ahmed.rashid@alnoor.sa",
            Phone = "+966501112233",
            OrganizationId = org.Id,
            Status = "active",
            LicenseIssuingAuthority = "Saudi Commission for Health Specialties (SCFHS)",
            PractitionerRole = "doctor"
        };

        var practitioner2 = new Practitioner
        {
            FirstName = "Fatima",
            LastName = "Hassan",
            LicenseNumber = "SCFHS-55002",
            LicenseSystem = "http://nphies.sa/license/practitioner-license",
            Specialization = "Pediatrics",
            Qualification = "MBBS, MRCPCH",
            Title = "Dr.",
            Email = "fatima.hassan@alnoor.sa",
            Phone = "+966501112244",
            OrganizationId = org.Id,
            Status = "active",
            LicenseIssuingAuthority = "Saudi Commission for Health Specialties (SCFHS)",
            PractitionerRole = "doctor"
        };
        await context.Practitioners.AddRangeAsync(practitioner1, practitioner2);

        var cardiologyClinic = new ClinicMaster
        {
            OrganizationId = org.Id,
            ClinicName = "Al Noor Cardiology Clinic",
            ClinicCode = "CLIN-CARD-01",
            ClinicType = "Specialty",
            SpecializedServices = "Cardiology, ECG, Echocardiography",
            NumberOfDoctors = 5,
            NumberOfNurses = 8,
            AccreditationLevel = "CBAHI",
            WorkingHoursFrom = new TimeOnly(8, 0),
            WorkingHoursTo = new TimeOnly(20, 0),
            IsEmergencyAvailable = true,
            PharmacyAvailable = true,
            LabAvailable = true,
            ImagingAvailable = true,
            AcceptsCashPayment = true,
            AcceptsInsurance = true,
            AcceptsCardPayment = true,
            IsActive = true,
            CreatedBy = SeedUser
        };

        var pediatricsClinic = new ClinicMaster
        {
            OrganizationId = org.Id,
            ClinicName = "Al Noor Pediatrics Clinic",
            ClinicCode = "CLIN-PED-01",
            ClinicType = "Specialty",
            SpecializedServices = "Pediatrics, Vaccination, Neonatal Care",
            NumberOfDoctors = 4,
            NumberOfNurses = 10,
            AccreditationLevel = "CBAHI",
            WorkingHoursFrom = new TimeOnly(8, 0),
            WorkingHoursTo = new TimeOnly(22, 0),
            IsEmergencyAvailable = true,
            PharmacyAvailable = true,
            LabAvailable = true,
            ImagingAvailable = false,
            AcceptsCashPayment = true,
            AcceptsInsurance = true,
            AcceptsCardPayment = true,
            IsActive = true,
            CreatedBy = SeedUser
        };
        await context.ClinicMasters.AddRangeAsync(cardiologyClinic, pediatricsClinic);

        var doctor1 = new DoctorMaster
        {
            PractitionerId = practitioner1.Id,
            DoctorCode = "DOC-0001",
            DoctorName = "Dr. Ahmed Al-Rashid",
            Specialization = "Cardiology",
            SubSpecialization = "Interventional Cardiology",
            QualificationDegree = "MD",
            UniversityName = "King Saud University",
            YearsOfExperience = 15,
            IsConsultant = true,
            ConsultationFee = 500m,
            FollowupFee = 300m,
            CurrencyCode = "SAR",
            ClinicMasterId = cardiologyClinic.Id,
            IsAvailableForAppointments = true,
            AvailableSlotsPerDay = 20,
            Board = "Saudi Board of Cardiology",
            BoardLicenseNumber = "SBC-1001",
            IsActive = true,
            CreatedBy = SeedUser
        };

        var doctor2 = new DoctorMaster
        {
            PractitionerId = practitioner2.Id,
            DoctorCode = "DOC-0002",
            DoctorName = "Dr. Fatima Hassan",
            Specialization = "Pediatrics",
            SubSpecialization = "Neonatology",
            QualificationDegree = "MBBS",
            UniversityName = "King Abdulaziz University",
            YearsOfExperience = 10,
            IsConsultant = true,
            ConsultationFee = 400m,
            FollowupFee = 250m,
            CurrencyCode = "SAR",
            ClinicMasterId = pediatricsClinic.Id,
            IsAvailableForAppointments = true,
            AvailableSlotsPerDay = 25,
            Board = "Saudi Board of Pediatrics",
            BoardLicenseNumber = "SBP-2002",
            IsActive = true,
            CreatedBy = SeedUser
        };
        await context.DoctorMasters.AddRangeAsync(doctor1, doctor2);

        var qualifications = new List<DoctorQualification>
        {
     new()
            {
         DoctorMasterId = doctor1.Id,
           QualificationType = "Degree",
              QualificationName = "Doctor of Medicine (MD)",
        UniversityName = "King Saud University",
    IssuedDate = new DateTime(2008, 6, 1),
        IsExpiring = false,
  CertificateNumber = "MD-2008-1001",
     VerificationStatus = "Verified",
        CreatedBy = SeedUser
            },
       new()
      {
                DoctorMasterId = doctor1.Id,
        QualificationType = "Fellowship",
   QualificationName = "FRCP (Cardiology)",
       UniversityName = "Royal College of Physicians",
    IssuedDate = new DateTime(2014, 9, 1),
                IsExpiring = false,
CertificateNumber = "FRCP-2014-556",
    VerificationStatus = "Verified",
             CreatedBy = SeedUser
            },
            new()
   {
            DoctorMasterId = doctor2.Id,
       QualificationType = "Degree",
   QualificationName = "MBBS",
             UniversityName = "King Abdulaziz University",
     IssuedDate = new DateTime(2012, 6, 1),
         IsExpiring = false,
     CertificateNumber = "MBBS-2012-3002",
       VerificationStatus = "Verified",
       CreatedBy = SeedUser
      }
        };
        await context.DoctorQualifications.AddRangeAsync(qualifications);

        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded 1 organization, 2 practitioners, 2 clinics, 2 doctors, {Q} qualifications.",
            qualifications.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 8. CodeableConcepts
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedCodeableConceptsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.CodeSystems.AnyAsync())
        {
            logger.LogInformation("↩️  CodeSystems already populated. Skipping codeable concept seed.");
            return;
        }

        var claimType = new CodeSystemEntity
        {
            Url = "http://terminology.hl7.org/CodeSystem/claim-type",
            Version = "1.0.0",
            Name = "ClaimType",
            Title = "Claim Type Codes",
            Definition = "The type or discipline-style of the claim.",
            Committee = "NPHIES",
            Oid = string.Empty,
            Copyright = string.Empty,
            SourceResource = string.Empty,
            CaseSensitive = true,
            IsActive = true
        };

        var diagnosisType = new CodeSystemEntity
        {
            Url = "http://nphies.sa/terminology/CodeSystem/diagnosis-type",
            Version = "1.0.0",
            Name = "DiagnosisType",
            Title = "NPHIES Diagnosis Type Codes",
            Definition = "The type of the diagnosis on the claim.",
            Committee = "NPHIES",
            Oid = string.Empty,
            Copyright = string.Empty,
            SourceResource = string.Empty,
            CaseSensitive = true,
            IsActive = true
        };

        var icd10 = new CodeSystemEntity
        {
            Url = "http://hl7.org/fhir/sid/icd-10",
            Version = "2019",
            Name = "ICD10",
            Title = "ICD-10 Diagnosis Codes",
            Definition = "International Classification of Diseases, 10th Revision.",
            Committee = "WHO",
            Oid = string.Empty,
            Copyright = string.Empty,
            SourceResource = string.Empty,
            CaseSensitive = true,
            IsActive = true
        };

        await context.CodeSystems.AddRangeAsync(claimType, diagnosisType, icd10);
        await context.SaveChangesAsync();

        var concepts = new List<ConceptEntity>
     {
       NewConcept(claimType.CodeSystemId, "institutional", "Institutional", "Hospital, clinic and inpatient claims.", 1),
            NewConcept(claimType.CodeSystemId, "professional", "Professional", "Professional / physician service claims.", 2),
            NewConcept(claimType.CodeSystemId, "oral", "Oral", "Dental / oral health claims.", 3),
            NewConcept(claimType.CodeSystemId, "pharmacy", "Pharmacy", "Pharmacy / medication claims.", 4),
   NewConcept(claimType.CodeSystemId, "vision", "Vision", "Vision / optical claims.", 5),

NewConcept(diagnosisType.CodeSystemId, "principal", "Principal", "The single medical diagnosis relevant to the claim.", 1),
     NewConcept(diagnosisType.CodeSystemId, "admitting", "Admitting", "Diagnosis at time of admission.", 2),
   NewConcept(diagnosisType.CodeSystemId, "discharge", "Discharge", "Diagnosis at time of discharge.", 3),
            NewConcept(diagnosisType.CodeSystemId, "secondary", "Secondary", "Additional / secondary diagnosis.", 4),

         NewConcept(icd10.CodeSystemId, "E11.9", "Type 2 diabetes mellitus without complications", null, 1),
    NewConcept(icd10.CodeSystemId, "I10", "Essential (primary) hypertension", null, 2),
    NewConcept(icd10.CodeSystemId, "J45.909", "Unspecified asthma, uncomplicated", null, 3),
        };
        await context.Concepts.AddRangeAsync(concepts);

        var claimTypeVs = new ValueSetEntity
        {
            Url = "http://nphies.sa/terminology/ValueSet/claim-type",
            Version = "1.0.0",
            Name = "ClaimTypeVS",
            Title = "Claim Type Value Set",
            Definition = "Allowed claim type codes for NPHIES claims.",
            Committee = "NPHIES",
            Oid = string.Empty,
            Copyright = string.Empty,
            SourceResource = string.Empty,
            Restrictions = string.Empty,
            IsActive = true
        };

        var diagnosisIcd10Vs = new ValueSetEntity
        {
            Url = "http://nphies.sa/terminology/ValueSet/diagnosis-icd10",
            Version = "1.0.0",
            Name = "DiagnosisICD10VS",
            Title = "Diagnosis ICD-10 Value Set",
            Definition = "Allowed ICD-10 diagnosis codes for claims.",
            Committee = "NPHIES",
            Oid = string.Empty,
            Copyright = string.Empty,
            SourceResource = string.Empty,
            Restrictions = string.Empty,
            IsActive = true
        };

        await context.ValueSets.AddRangeAsync(claimTypeVs, diagnosisIcd10Vs);
        await context.SaveChangesAsync();

        var maps = new List<ValueSetCodeSystemMapEntity>
        {
            new() { ValueSetId = claimTypeVs.ValueSetId, CodeSystemId = claimType.CodeSystemId, Sequence = 1, IsActive = true },
   new() { ValueSetId = diagnosisIcd10Vs.ValueSetId, CodeSystemId = icd10.CodeSystemId, Sequence = 1, IsActive = true },
            new() { ValueSetId = diagnosisIcd10Vs.ValueSetId, CodeSystemId = diagnosisType.CodeSystemId, Sequence = 2, IsActive = true },
    };
        await context.ValueSetCodeSystemMaps.AddRangeAsync(maps);

        var profileElements = new List<ProfileElementEntity>
  {
            new()
      {
            ProfileName = "NphiesClaim",
           Path = "Claim.type",
                ElementPath = "Claim.type",
                Definition = "The category of claim (institutional, professional, oral, pharmacy, vision).",
   PathArabic = string.Empty,
       ValueSetId = claimTypeVs.ValueSetId,
                BindingStrength = "required",
        MessageType = "claim-request",
     ResourceType = "Claim",
       IsRequired = true,
                IsActive = true
     },
  new()
     {
  ProfileName = "NphiesClaim",
                Path = "Claim.diagnosis.diagnosisCodeableConcept",
         ElementPath = "Claim.diagnosis.diagnosisCodeableConcept",
 Definition = "The ICD-10 diagnosis code for the claim item.",
           PathArabic = string.Empty,
ValueSetId = diagnosisIcd10Vs.ValueSetId,
      BindingStrength = "required",
                MessageType = "claim-request",
          ResourceType = "Claim",
          IsRequired = true,
          IsActive = true
      },
            new()
            {
        ProfileName = "NphiesClaim",
           Path = "Claim.diagnosis.type",
      ElementPath = "Claim.diagnosis.type",
                Definition = "The type of the diagnosis (principal, admitting, discharge, secondary).",
           PathArabic = string.Empty,
          ValueSetId = diagnosisIcd10Vs.ValueSetId,
 BindingStrength = "extensible",
    MessageType = "claim-request",
  ResourceType = "Claim",
       IsRequired = false,
                IsActive = true
         },
     new()
     {
    ProfileName = "NphiesPreAuthorization",
              Path = "Claim.type",
   ElementPath = "Claim.type",
    Definition = "The category of the pre-authorization request.",
 PathArabic = string.Empty,
        ValueSetId = claimTypeVs.ValueSetId,
      BindingStrength = "required",
     MessageType = "preauthorization-request",
  ResourceType = "Claim",
      IsRequired = true,
                IsActive = true
            }
        };
        await context.ProfileElements.AddRangeAsync(profileElements);

        var claimRequestMsg = new NphiesMessageTypeEntity
        {
            MessageType = "claim-request",
            MessageTypeArabic = string.Empty,
            FhirResourceType = "Claim",
            Description = "Request for claim adjudication.",
            Version = "1.0.0",
            IsActive = true
        };

        var preAuthRequestMsg = new NphiesMessageTypeEntity
        {
            MessageType = "preauthorization-request",
            MessageTypeArabic = string.Empty,
            FhirResourceType = "Claim",
            Description = "Request for pre-authorization of planned services.",
            Version = "1.0.0",
            IsActive = true
        };

        var eligibilityRequestMsg = new NphiesMessageTypeEntity
        {
            MessageType = "eligibility-request",
            MessageTypeArabic = string.Empty,
            FhirResourceType = "CoverageEligibilityRequest",
            Description = "Request to verify patient coverage eligibility.",
            Version = "1.0.0",
            IsActive = true
        };

        await context.NphiesMessageTypes.AddRangeAsync(claimRequestMsg, preAuthRequestMsg, eligibilityRequestMsg);
        await context.SaveChangesAsync();

        var requiredElements = new List<NphiesMessageRequiredElementEntity>
        {
          new() { NphiesMessageTypeId = claimRequestMsg.NphiesMessageTypeId, ElementPath = "Claim.type", ValueSetId = claimTypeVs.ValueSetId, IsRequired = true, Cardinality = "1..1", Notes = "Type of claim." },
         new() { NphiesMessageTypeId = claimRequestMsg.NphiesMessageTypeId, ElementPath = "Claim.patient", ValueSetId = null, IsRequired = true, Cardinality = "1..1", Notes = "Reference to the patient." },
   new() { NphiesMessageTypeId = claimRequestMsg.NphiesMessageTypeId, ElementPath = "Claim.diagnosis.diagnosisCodeableConcept", ValueSetId = diagnosisIcd10Vs.ValueSetId, IsRequired = true, Cardinality = "1..*", Notes = "At least one ICD-10 diagnosis is required." },
    new() { NphiesMessageTypeId = claimRequestMsg.NphiesMessageTypeId, ElementPath = "Claim.item.productOrService", ValueSetId = null, IsRequired = true, Cardinality = "1..*", Notes = "At least one billable service/product item is required." },

 new() { NphiesMessageTypeId = preAuthRequestMsg.NphiesMessageTypeId, ElementPath = "Claim.type", ValueSetId = claimTypeVs.ValueSetId, IsRequired = true, Cardinality = "1..1", Notes = "Type of pre-authorization request." },
        new() { NphiesMessageTypeId = preAuthRequestMsg.NphiesMessageTypeId, ElementPath = "Claim.item.productOrService", ValueSetId = null, IsRequired = true, Cardinality = "1..*", Notes = "Planned services requiring authorization." },

 new() { NphiesMessageTypeId = eligibilityRequestMsg.NphiesMessageTypeId, ElementPath = "CoverageEligibilityRequest.patient", ValueSetId = null, IsRequired = true, Cardinality = "1..1", Notes = "Reference to the patient." },
   new() { NphiesMessageTypeId = eligibilityRequestMsg.NphiesMessageTypeId, ElementPath = "CoverageEligibilityRequest.insurance.coverage", ValueSetId = null, IsRequired = true, Cardinality = "1..1", Notes = "Reference to the patient's coverage." },
        };
        await context.NphiesMessageRequiredElements.AddRangeAsync(requiredElements);

        await context.SaveChangesAsync();
        logger.LogInformation(
    "✅ Seeded 3 code systems, {C} concepts, 2 value sets, {M} maps, {P} profile elements, 3 message types, {R} required elements.",
       concepts.Count, maps.Count, profileElements.Count, requiredElements.Count);
    }

    private static ConceptEntity NewConcept(
        int codeSystemId, string code, string display, string? definition, int sortOrder) => new()
        {
            CodeSystemId = codeSystemId,
            Code = code,
            Display = display,
            Definition = definition ?? string.Empty,
            DisplayArabic = string.Empty,
            DefinitionArabic = string.Empty,
            ParentCode = string.Empty,
            IsActive = true,
            SortOrder = sortOrder
        };

    // ─────────────────────────────────────────────────────────────
    // 9. Validation Rules (NPHIES validation constraints)
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedValidationRulesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.ValidationRules.AnyAsync())
        {
            logger.LogInformation("↩️  ValidationRules already populated. Skipping.");
            return;
        }

        // Resolve seeded ValueSet IDs (may be null if codeable-concept seed was skipped)
        var claimTypeVs = await context.ValueSets
   .FirstOrDefaultAsync(v => v.Name == "ClaimTypeVS");
        var diagnosisVs = await context.ValueSets
            .FirstOrDefaultAsync(v => v.Name == "DiagnosisICD10VS");

        var rules = new List<ValidationRuleEntity>
   {
            NewValidationRule("VR-001", "Claim.type must be a valid claim type code", "Claim.type", "ValueSetMembership", "error", claimTypeVs?.ValueSetId),
      NewValidationRule("VR-002", "Claim.type is required", "Claim.type", "CodeExists", "error", claimTypeVs?.ValueSetId),
       NewValidationRule("VR-003", "Diagnosis code must be a valid ICD-10 code", "Claim.diagnosis.diagnosisCodeableConcept", "ValueSetMembership", "error", diagnosisVs?.ValueSetId),
            NewValidationRule("VR-004", "At least one diagnosis is required on a claim", "Claim.diagnosis", "Cardinality", "error", null),
       NewValidationRule("VR-005", "At least one billable item is required on a claim", "Claim.item", "Cardinality", "error", null),
            NewValidationRule("VR-006", "Patient reference is required", "Claim.patient", "CodeExists", "error", null),
  NewValidationRule("VR-007", "Coverage reference is required for eligibility", "CoverageEligibilityRequest.insurance.coverage", "CodeExists", "error", null),
            NewValidationRule("VR-008", "Claim total should match sum of item nets", "Claim.total", "BusinessRule", "warning", null),
        };

        await context.ValidationRules.AddRangeAsync(rules);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Seeded {Count} validation rules.", rules.Count);
    }

    private static ValidationRuleEntity NewValidationRule(
        string errorCode, string errorMessage, string fieldPath, string ruleType, string severity, int? valueSetId) => new()
        {
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            ErrorMessageArabic = string.Empty,
            FieldPath = fieldPath,
            RuleType = ruleType,
            Parameters = "{}",
            Severity = severity,
            ValueSetId = valueSetId,
            IsActive = true
        };
}
