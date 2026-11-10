using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Seeders;

/// <summary>
/// Realistic sample data for development/demo: extra payers + policies, NPHIES code masters taken from the
/// NPHIES IG examples, three hospitals (locations, practitioners, clinics, doctors) and, per hospital,
/// patients, coverages, encounters and claims / prior authorizations of every claim type with items, details,
/// diagnoses, care team, supporting info, related claims, vision prescriptions, oral details, shadow billing
/// and accidents. Every row is inserted only when its natural key is missing, so it is safe on every startup.
/// </summary>
public static class SampleDataSeeder
{
    public const string SeedUser = "SAMPLE_SEED";
    public const string ClaimPrefix = "SMP-";

    private const string N = "http://nphies.sa/terminology/CodeSystem/";
    private const string Icd10Am = "http://hl7.org/fhir/sid/icd-10-am";
    private const string Ucum = "http://unitsofmeasure.org";

    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger, DateTime? today = null)
    {
        var now = (today ?? DateTime.UtcNow).Date;
        logger.LogInformation("🌱 Sample data seeding started...");

        await SeedCodeMastersAsync(db, logger);
        var payers = await SeedPayersAsync(db, logger, now);
        var insurers = await SeedInsurerOrganizationsAsync(db, payers, logger);
        var hospitals = new List<Hospital>();
        foreach (var spec in HospitalSpecs)
            hospitals.Add(await SeedHospitalAsync(db, spec, logger));

        var claims = 0;
        for (var h = 0; h < hospitals.Count; h++)
            claims += await SeedClaimsAsync(db, hospitals[h], h, payers, insurers, now);

        logger.LogInformation("✅ Sample data seeding completed ({Claims} new sample claims/preauths).", claims);
    }

    // ===================================================================== code masters

    private static async Task SeedCodeMastersAsync(ApplicationDbContext db, ILogger logger)
    {
        var added = 0;
        added += await AddMissingAsync(db, db.ServiceCodeMasters, s => s.ServiceCode, new[]
        {
            Service("83600-00-00", "Hospital accommodation - per day", "Inpatient", 1200m, N + "services"),
            Service("83600-00-10", "Consultation - general practitioner", "Consultation", 250m, N + "services"),
            Service("83620-00-10", "Consultation - specialist follow-up", "Consultation", 300m, N + "services"),
            Service("38618-00-00", "Coronary artery bypass graft", "Surgical", 45000m, N + "procedures", auth: true),
            Service("40703-02-00", "Craniotomy with excision of lesion", "Surgical", 38000m, N + "procedures", auth: true),
            Service("11000-00-00", "Electroencephalography", "Diagnostic", 900m, N + "procedures"),
            Service("90901-00-00", "Anaesthesia - general", "Surgical", 3500m, N + "procedures"),
            Service("50115-00-00", "Arthroscopy of knee", "Surgical", 7800m, N + "procedures", auth: true),
            Service("42503-00-02", "Eye examination with refraction", "Optical", 200m, N + "procedures"),
            Service("96092-00-10", "Supply of spectacle lenses", "Optical", 450m, N + "procedures", auth: true),
            Service("012", "Periodic oral examination", "Dental", 150m, N + "oral-health-op"),
            Service("111", "Removal of plaque and calculus", "Dental", 250m, N + "oral-health-op"),
            Service("322", "Surgical removal of tooth", "Dental", 600m, N + "oral-health-op"),
            Service("515", "Root canal treatment - one canal", "Dental", 900m, N + "oral-health-op"),
            Service("618", "Full crown - metallic", "Dental", 1800m, N + "oral-health-op", auth: true),
            Service("58500-00-00", "Magnetic resonance imaging - knee", "Radiology", 1600m, N + "imaging", auth: true),
            Service("66500-00-00", "Lipid profile", "Laboratory", 120m, N + "laboratory"),
        });
        added += await AddMissingAsync(db, db.DiagnosisCodeMasters, d => d.DiagnosisCode, new[]
        {
            Diagnosis("R07.1", "Chest pain on breathing", "Symptoms", "Moderate"),
            Diagnosis("I21.9", "Acute myocardial infarction, unspecified", "Circulatory", "Severe"),
            Diagnosis("C71.7", "Malignant neoplasm of brain stem", "Neoplasms", "Severe"),
            Diagnosis("J39.3", "Upper respiratory tract hypersensitivity reaction", "Respiratory", "Mild"),
            Diagnosis("J06.9", "Acute upper respiratory infection, unspecified", "Respiratory", "Mild"),
            Diagnosis("N94.6", "Dysmenorrhoea, unspecified", "Genitourinary", "Mild"),
            Diagnosis("S83.1", "Dislocation of knee", "Injury", "Moderate"),
            Diagnosis("R51", "Headache", "Symptoms", "Mild"),
            Diagnosis("M54.5", "Low back pain", "Musculoskeletal", "Mild"),
            Diagnosis("K01.1", "Impacted teeth", "Dental", "Mild"),
            Diagnosis("K02.1", "Caries of dentine", "Dental", "Mild"),
            Diagnosis("K04.0", "Pulpitis", "Dental", "Moderate"),
            Diagnosis("T36.9", "Adverse effect of systemic antibiotic, unspecified", "Injury", "Mild"),
            Diagnosis("Z01.0", "Examination of eyes and vision", "Factors", "Mild"),
            Diagnosis("H52.1", "Myopia", "Eye", "Mild"),
            Diagnosis("H52.4", "Presbyopia", "Eye", "Mild"),
        });
        added += await AddMissingAsync(db, db.MedicationCodeMasters, m => m.MedicationCode, new[]
        {
            Medication("06285096001627", "Paracetamol 500 mg film-coated tablets (24)", "Paracetamol", "500mg", "Tablet", 9.50m),
            Medication("05944736008570", "Amoxicillin 500 mg capsules (20)", "Amoxicillin", "500mg", "Capsule", 21.00m),
            Medication("06285074000277", "Ibuprofen 400 mg tablets (30)", "Ibuprofen", "400mg", "Tablet", 14.25m),
            Medication("06281086011234", "Azithromycin 250 mg tablets (6)", "Azithromycin", "250mg", "Tablet", 38.00m),
            Medication("06285147000154", "Atorvastatin 20 mg tablets (30)", "Atorvastatin", "20mg", "Tablet", 62.00m),
        });
        added += await AddMissingAsync(db, db.ModifierCodeMasters, m => m.ModifierCode, new[]
        {
            new ModifierCodeMaster { ModifierCode = "51", ModifierName = "Multiple Procedures", ModifierType = "Procedural", ImpactOnCharges = "Decrease", ChargePercentage = 50m, NphiesModifierCode = "51", IsActive = true, CreatedBy = SeedUser },
            new ModifierCodeMaster { ModifierCode = "80", ModifierName = "Assistant Surgeon", ModifierType = "Procedural", ImpactOnCharges = "Decrease", ChargePercentage = 20m, NphiesModifierCode = "80", IsActive = true, CreatedBy = SeedUser },
        });
        added += await AddMissingAsync(db, db.MedicalDeviceCodeMasters, d => d.DeviceCode, new[]
        {
            new MedicalDeviceCodeMaster { DeviceCode = "DEV-0006", DeviceName = "Coronary Stent (Drug-Eluting)", DeviceType = "Implant", Manufacturer = "Generic", NphiesDeviceCode = "DEV-0006", IsNphiesMapped = true, UnitPrice = 9500m, CurrencyCode = "SAR", IsImplantable = true, IsReusable = false, IsActive = true, CreatedBy = SeedUser },
            new MedicalDeviceCodeMaster { DeviceCode = "DEV-0007", DeviceName = "Knee Brace", DeviceType = "Orthotic", Manufacturer = "Generic", NphiesDeviceCode = "DEV-0007", IsNphiesMapped = true, UnitPrice = 350m, CurrencyCode = "SAR", IsImplantable = false, IsReusable = true, IsActive = true, CreatedBy = SeedUser },
        });
        added += await AddMissingAsync(db, db.BenefitCodeMasters, b => b.BenefitCode, new[]
        {
            new BenefitCodeMaster { BenefitCode = "6", BenefitName = "Maternity", BenefitCategory = "Inpatient", IsActive = true, CreatedBy = SeedUser },
            new BenefitCodeMaster { BenefitCode = "7", BenefitName = "Optical", BenefitCategory = "Vision", IsActive = true, CreatedBy = SeedUser },
            new BenefitCodeMaster { BenefitCode = "8", BenefitName = "Dental Treatment", BenefitCategory = "Dental", IsActive = true, CreatedBy = SeedUser },
            new BenefitCodeMaster { BenefitCode = "9", BenefitName = "Emergency", BenefitCategory = "Emergency", IsActive = true, CreatedBy = SeedUser },
        });
        added += await AddMissingAsync(db, db.ErrorCodeMasters, e => e.ErrorCode, new[]
        {
            Error("MN-1-1", "Service is not clinically justified based on the clinical practice guideline", "medical-necessity", "Error", true, "Attach clinical notes justifying the service", "Denied"),
            Error("CV-1-3", "Benefit limit exhausted", "coverage", "Error", false, "Bill the patient or another payer", "Denied"),
            Error("AD-3-5", "Service is not compatible with the diagnosis", "adjudication", "Error", true, "Correct diagnosis/service pairing", "Denied"),
            Error("SE-1-6", "Frequency limit exceeded for the service", "benefit", "Error", true, "Provide justification for repeat service", "Partial"),
            Error("BE-1-4", "Prior authorization not obtained", "authorization", "Error", true, "Obtain prior authorization and resubmit", "Denied"),
        });
        logger.LogInformation("✅ Sample code masters: {Count} rows added.", added);
    }

    private static ServiceCodeMaster Service(string code, string name, string category, decimal price, string system, bool auth = false) => new()
    {
        ServiceCode = code,
        ServiceName = name,
        ServiceCategory = category,
        DefaultPrice = price,
        CurrencyCode = "SAR",
        NphiesServiceCode = code,
        NphiesServiceName = name,
        IsNphiesMapped = true,
        MappingValidationStatus = "Valid",
        IsRequiresAuthorization = auth,
        DefaultAuthorizationDays = auth ? 30 : null,
        ServiceDescription = name,
        NphiesCategoryCode = system[(system.LastIndexOf('/') + 1)..],
        IsActive = true,
        CreatedBy = SeedUser,
    };

    private static DiagnosisCodeMaster Diagnosis(string code, string name, string category, string severity) => new()
    {
        DiagnosisCode = code,
        DiagnosisName = name,
        DiagnosisCategory = category,
        DiagnosisType = "Principal",
        Severity = severity,
        NphiesDiagnosisCode = code,
        IsNphiesMapped = true,
        IsActive = true,
        CreatedBy = SeedUser,
    };

    private static MedicationCodeMaster Medication(string code, string name, string ingredient, string strength, string form, decimal price) => new()
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
        IsActive = true,
        CreatedBy = SeedUser,
    };

    private static ErrorCodeMaster Error(string code, string description, string category, string severity, bool recoverable, string action, string impact) => new()
    {
        ErrorCode = code,
        ErrorDescription = description,
        ErrorCategory = category,
        Severity = severity,
        IsRecoverable = recoverable,
        AllowsAppeal = true,
        StandardAppealDays = 60,
        RecommendedAction = action,
        NphiesCodeSystem = "http://nphies.sa/terminology/CodeSystem/adjudication-reason",
        AdjudicationImpact = impact,
        IsActive = true,
        CreatedDate = DateTime.UtcNow,
    };

    // ===================================================================== payers / policies

    private sealed record PayerSpec(string PayerId, string Name, string Arabic, string NphiesId, string City, string Domain, (string Code, string Name, decimal Premium, decimal Deductible)[] Policies);

    private static readonly PayerSpec[] PayerSpecs =
    {
        new("PAY-BUPA", "Bupa Arabia", "بوبا العربية", "INS-FHIR-0001", "Jeddah", "bupa.com.sa", new[] { ("BUPA-GOLD", "Bupa Gold Plan", 15000m, 500m), ("BUPA-SILVER", "Bupa Silver Plan", 8000m, 1000m), ("BUPA-PLAT", "Bupa Platinum Plan", 26000m, 0m) }),
        new("PAY-TAWN", "Tawuniya", "التعاونية", "INS-FHIR-0002", "Riyadh", "tawuniya.com.sa", new[] { ("TAWN-PREMIUM", "Tawuniya Premium Plan", 20000m, 250m), ("TAWN-STANDARD", "Tawuniya Standard Plan", 9000m, 750m) }),
        new("PAY-MDGF", "MedGulf", "ميدغلف", "INS-FHIR-0003", "Riyadh", "medgulf.com.sa", new[] { ("MDGF-VIP", "MedGulf VIP Plan", 18000m, 300m), ("MDGF-BASIC", "MedGulf Basic (CCHI Essential)", 4500m, 1500m) }),
        new("PAY-MLTH", "Malath Insurance", "ملاذ للتأمين", "INS-FHIR-0004", "Riyadh", "malath.com.sa", new[] { ("MLTH-GOLD", "Malath Gold Plan", 12000m, 500m), ("MLTH-BRONZE", "Malath Bronze Plan", 5000m, 1200m) }),
        new("PAY-ARTK", "Al Rajhi Takaful", "تكافل الراجحي", "INS-FHIR-0005", "Riyadh", "alrajhitakaful.com", new[] { ("ARTK-A", "Al Rajhi Takaful Class A", 14000m, 400m), ("ARTK-C", "Al Rajhi Takaful Class C", 6000m, 1000m) }),
        new("PAY-GIGS", "GIG Saudi", "جي آي جي السعودية", "INS-FHIR-0006", "Al Khobar", "gig.sa", new[] { ("GIGS-PLUS", "GIG Health Plus", 11000m, 500m), ("GIGS-CORE", "GIG Health Core", 5500m, 1000m) }),
    };

    private static async Task<List<PayerMaster>> SeedPayersAsync(ApplicationDbContext db, ILogger logger, DateTime now)
    {
        var existing = await db.PayerMasters.Include(p => p.Policies).ToListAsync();
        int payersAdded = 0, policiesAdded = 0;
        foreach (var spec in PayerSpecs)
        {
            var payer = existing.FirstOrDefault(p => p.PayerId == spec.PayerId);
            if (payer is null)
            {
                payer = new PayerMaster
                {
                    PayerId = spec.PayerId,
                    PayerName = spec.Name,
                    PayerNameArabic = spec.Arabic,
                    PayerType = "Insurance",
                    LicenseNumber = "CCHI-" + spec.NphiesId[^4..],
                    NphiesPayerId = spec.NphiesId,
                    NphiesConnectionStatus = "Connected",
                    IsNphiesMember = true,
                    IsNphiesIntegrated = true,
                    ContactEmail = "providers@" + spec.Domain,
                    ContactPhone = "+9668001" + spec.NphiesId[^4..] + "00",
                    Website = "https://www." + spec.Domain,
                    City = spec.City,
                    Country = "Saudi Arabia",
                    ContractStartDate = new DateTime(now.Year - 1, 1, 1),
                    ContractEndDate = new DateTime(now.Year + 1, 12, 31),
                    SupportedClaimTypes = "institutional,professional,oral,pharmacy,vision",
                    SupportedEligibilityTypes = "benefits,validation,discovery",
                    PaymentCycle = "Monthly",
                    AverageTurnaroundDays = 14,
                    MaxClaimsPerDay = 5000,
                    MaxClaimAmount = 500000m,
                    CurrencyCode = "SAR",
                    IsActive = true,
                    CreatedBy = SeedUser,
                };
                db.PayerMasters.Add(payer);
                existing.Add(payer);
                payersAdded++;
            }

            foreach (var (code, name, premium, deductible) in spec.Policies)
            {
                if (payer.Policies.Any(p => p.PolicyCode == code) || await db.PayerPolicyMasters.AnyAsync(p => p.PolicyCode == code))
                    continue;
                payer.Policies.Add(new PayerPolicyMaster
                {
                    PayerMasterId = payer.Id,
                    PolicyCode = code,
                    PolicyName = name,
                    PolicyType = "Group",
                    CoverageType = "Comprehensive",
                    CoverageLevel = premium >= 15000m ? "VIP" : premium >= 8000m ? "Standard" : "Basic",
                    NetworkType = premium >= 15000m ? "Open" : "PPO",
                    AnnualPremium = premium,
                    PremiumFrequency = "Annual",
                    CurrencyCode = "SAR",
                    AnnualDeductible = deductible,
                    MaxOutOfPocket = 5000m,
                    CopaymentAmount = 50m,
                    CoinsurancePercentage = premium >= 15000m ? 0m : 20m,
                    CoverageLimitPerYear = premium >= 15000m ? 1000000m : 500000m,
                    PreAuthRequiredForAmount = 1000m,
                    RequiresPriorAuth = true,
                    EffectiveFromDate = new DateTime(now.Year - 1, 1, 1),
                    EffectiveToDate = new DateTime(now.Year + 1, 12, 31),
                    IsPolicyActive = true,
                    CreatedBy = SeedUser,
                });
                policiesAdded++;
            }
        }
        await db.SaveChangesAsync();

        // Benefit coverages: one row per policy and service category.
        var categories = new (string Category, string Benefit, decimal Pct, bool PreAuth)[]
        {
            ("Consultation", "Medical Care", 100m, false), ("Laboratory", "Diagnostic", 80m, false),
            ("Radiology", "Diagnostic", 80m, true), ("Surgical", "Surgery", 90m, true),
            ("Inpatient", "Inpatient", 90m, true), ("Dental", "Dental Treatment", 70m, false),
            ("Optical", "Optical", 60m, true), ("Pharmacy", "Pharmacy", 80m, false),
            ("Maternity", "Maternity", 100m, true), ("Emergency", "Emergency", 100m, false),
        };
        var policies = await db.PayerPolicyMasters.ToListAsync();
        var coverageKeys = (await db.PolicyBenefitCoverages.Select(c => new { c.PolicyMasterId, c.ServiceCategory }).ToListAsync())
            .Select(k => (k.PolicyMasterId, k.ServiceCategory)).ToHashSet();
        var coveragesAdded = 0;
        foreach (var policy in policies)
        foreach (var (category, benefit, pct, preAuth) in categories)
        {
            if (coverageKeys.Contains((policy.Id, category))) continue;
            db.PolicyBenefitCoverages.Add(new PolicyBenefitCoverage
            {
                PolicyMasterId = policy.Id,
                ServiceCategory = category,
                BenefitType = benefit,
                CoveragePercentage = pct,
                RequiresPreAuth = preAuth,
                RequiresReferral = false,
                PreAuthValidityDays = preAuth ? 30 : null,
                IsExcluded = false,
                IsWaitingPeriodApplicable = category == "Maternity",
                CreatedBy = SeedUser,
            });
            coveragesAdded++;
        }

        var payersWithRules = (await db.ClaimSubmissionRules.Select(r => r.PayerMasterId).Distinct().ToListAsync()).ToHashSet();
        var rulesAdded = 0;
        foreach (var payer in existing.Where(p => !payersWithRules.Contains(p.Id)))
        {
            db.ClaimSubmissionRules.AddRange(
                Rule(payer.Id, "Max Claim Amount", "Financial", "Claim total must not exceed limit", 500000m, 50, 1),
                Rule(payer.Id, "Timely Filing", "Temporal", "Claim must be submitted within 90 days of service", null, null, 2, maxDays: 90),
                Rule(payer.Id, "Medical Report Required", "Documentation", "High-value claims require a medical report", 10000m, null, 3, requiresReport: true));
            rulesAdded += 3;
        }
        await db.SaveChangesAsync();
        logger.LogInformation("✅ Sample payers: {P} payers, {Pol} policies, {C} benefit coverages, {R} submission rules added.",
            payersAdded, policiesAdded, coveragesAdded, rulesAdded);

        return await db.PayerMasters.Include(p => p.Policies).Where(p => p.IsActive && p.NphiesPayerId != null)
            .OrderBy(p => p.PayerId).ToListAsync();
    }

    private static ClaimSubmissionRules Rule(string payerId, string name, string type, string condition, decimal? maxAmount, int? maxItems, int priority,
        int? maxDays = null, bool requiresReport = false) => new()
    {
        PayerMasterId = payerId,
        RuleName = name,
        RuleType = type,
        RuleCondition = condition,
        MaxClaimAmount = maxAmount,
        MaxItemsPerClaim = maxItems,
        RequiresInvoice = true,
        RequiresMedicalReport = requiresReport,
        RequiresPhotos = false,
        MaxDaysForSubmission = maxDays,
        IsActive = true,
        Priority = priority,
        CreatedBy = SeedUser,
    };

    /// <summary>Insurer Organization per payer (linked by convention: Organization.NphiesPayerId == PayerMaster.NphiesPayerId).</summary>
    private static async Task<Dictionary<string, Organization>> SeedInsurerOrganizationsAsync(ApplicationDbContext db, List<PayerMaster> payers, ILogger logger)
    {
        var result = new Dictionary<string, Organization>();
        var added = 0;
        foreach (var payer in payers)
        {
            var org = await db.Organizations.FirstOrDefaultAsync(o => o.OrganizationType == "ins" && (o.NphiesPayerId == payer.NphiesPayerId || o.LicenseNumber == payer.NphiesPayerId));
            if (org is null)
            {
                var spec = PayerSpecs.FirstOrDefault(s => s.PayerId == payer.PayerId);
                org = new Organization
                {
                    OrganizationName = payer.PayerName,
                    LicenseNumber = payer.NphiesPayerId!,
                    LicenseSystem = "http://nphies.sa/license/payer-license",
                    OrganizationType = "ins",
                    SpecializationType = "insurer",
                    NphiesOrganizationId = payer.NphiesPayerId,
                    NphiesPayerId = payer.NphiesPayerId,
                    Website = payer.Website ?? (spec is null ? string.Empty : "https://www." + spec.Domain),
                    Email = payer.ContactEmail ?? string.Empty,
                    PhoneNumber = payer.ContactPhone ?? "+966800000000",
                    AddressLine1 = "Head Office",
                    AddressLine2 = string.Empty,
                    City = payer.City ?? "Riyadh",
                    State = payer.City ?? "Riyadh",
                    PostalCode = "11411",
                    AddressCountry = "SA",
                    Status = "active",
                };
                db.Organizations.Add(org);
                added++;
            }
            result[payer.Id] = org;
        }
        await db.SaveChangesAsync();
        logger.LogInformation("✅ Sample insurer organizations: {Count} added.", added);
        return result;
    }

    // ===================================================================== hospitals

    private sealed record HospitalSpec(string Code, string Name, string License, string City, string Region, string Postal, string Domain, string Street);

    private static readonly HospitalSpec[] HospitalSpecs =
    {
        new("RCH", "Riyadh Care Hospital", "N-F-10001", "Riyadh", "Riyadh", "12271", "riyadhcare.sa", "Olaya Street"),
        new("JNH", "Jeddah National Hospital", "N-F-10002", "Jeddah", "Makkah", "23431", "jnh.sa", "Prince Sultan Road"),
        new("DSM", "Dammam Specialist Medical Center", "N-F-10003", "Dammam", "Eastern Province", "32241", "dsmc.sa", "King Saud Street"),
    };

    private sealed record PractitionerSpec(string Key, string First, string Last, string Practice, string Specialty, string ClinicKey);

    private static readonly PractitionerSpec[] PractitionerSpecs =
    {
        new("cardio", "Ahmed", "Al-Mansour", "08.26", "Cardiac Surgery", "GEN"),
        new("im", "Hind", "Al-Subaie", "08.00", "Internal Medicine", "GEN"),
        new("gp", "Tariq", "Al-Juhani", "10.08", "Family Medicine", "GEN"),
        new("dental", "Mona", "Al-Faraj", "22.00", "General Dentistry", "DEN"),
        new("eye", "Ziyad", "Al-Bishi", "11.09", "Ophthalmology", "OPT"),
    };

    private sealed class Hospital
    {
        public HospitalSpec Spec { get; init; } = null!;
        public Organization Org { get; set; } = null!;
        public Location Main { get; set; } = null!;
        public Location Clinic { get; set; } = null!;
        public Dictionary<string, Practitioner> Practitioners { get; } = new();
        public string Base => "http://" + Spec.Domain;
    }

    private static async Task<Hospital> SeedHospitalAsync(ApplicationDbContext db, HospitalSpec spec, ILogger logger)
    {
        var h = new Hospital { Spec = spec };
        h.Org = await db.Organizations.FirstOrDefaultAsync(o => o.LicenseNumber == spec.License) ?? AddOrganization(db, spec);

        h.Main = await EnsureLocationAsync(db, h, $"{spec.Code}-LOC-MAIN", $"{spec.Name} - Main Building", "HOSP", "Hospital");
        h.Clinic = await EnsureLocationAsync(db, h, $"{spec.Code}-LOC-OPC", $"{spec.Name} - Outpatient Clinics", "OPC", "Outpatient clinic");

        var clinics = new Dictionary<string, ClinicMaster>();
        foreach (var (key, name, services) in new[]
                 {
                     ("GEN", "General & Internal Medicine Clinic", "Consultation, Cardiology, Internal Medicine"),
                     ("DEN", "Dental Clinic", "Dental examination, Endodontics, Prosthodontics"),
                     ("OPT", "Ophthalmology & Optical Clinic", "Eye examination, Refraction, Spectacles"),
                 })
        {
            var code = $"{spec.Code}-CLN-{key}";
            var clinic = await db.ClinicMasters.FirstOrDefaultAsync(c => c.ClinicCode == code);
            if (clinic is null)
            {
                clinic = new ClinicMaster
                {
                    OrganizationId = h.Org.Id,
                    ClinicName = $"{spec.Name} {name}",
                    ClinicCode = code,
                    ClinicType = key == "GEN" ? "General" : "Specialty",
                    SpecializedServices = services,
                    NumberOfDoctors = 6,
                    NumberOfNurses = 12,
                    AccreditationLevel = "CBAHI",
                    WorkingHoursFrom = new TimeOnly(8, 0),
                    WorkingHoursTo = new TimeOnly(22, 0),
                    IsEmergencyAvailable = key == "GEN",
                    PharmacyAvailable = true,
                    LabAvailable = key == "GEN",
                    ImagingAvailable = key != "DEN",
                    AcceptsCashPayment = true,
                    AcceptsInsurance = true,
                    AcceptsCardPayment = true,
                    IsActive = true,
                    CreatedBy = SeedUser,
                };
                db.ClinicMasters.Add(clinic);
            }
            clinics[key] = clinic;
        }

        var n = 0;
        foreach (var p in PractitionerSpecs)
        {
            n++;
            var license = $"N-P-{spec.License[^5..]}{n}";
            var practitioner = await db.Practitioners.FirstOrDefaultAsync(x => x.LicenseNumber == license);
            if (practitioner is null)
            {
                practitioner = new Practitioner
                {
                    FirstName = p.First,
                    LastName = p.Last,
                    LicenseNumber = license,
                    PractitionerLicenseNumber = license,
                    LicenseSystem = "http://nphies.sa/license/practitioner-license",
                    Specialization = p.Practice,
                    Qualification = p.Specialty,
                    Title = "Dr.",
                    Email = $"{p.First.ToLowerInvariant()}.{p.Last.ToLowerInvariant().Replace("-", "")}@{spec.Domain}",
                    Phone = $"+96655{spec.License[^4..]}{n:00}",
                    OrganizationId = h.Org.Id,
                    Status = "active",
                    LicenseIssuingAuthority = "Saudi Commission for Health Specialties (SCFHS)",
                    PractitionerRole = "doctor",
                };
                db.Practitioners.Add(practitioner);

                var doctor = new DoctorMaster
                {
                    PractitionerId = practitioner.Id,
                    DoctorCode = $"{spec.Code}-DOC-{n:00}",
                    DoctorName = $"Dr. {p.First} {p.Last}",
                    Specialization = p.Specialty,
                    QualificationDegree = p.Key == "dental" ? "BDS" : "MBBS",
                    UniversityName = n % 2 == 0 ? "King Abdulaziz University" : "King Saud University",
                    YearsOfExperience = 6 + n * 3,
                    IsConsultant = n % 2 == 1,
                    ConsultationFee = 250m + n * 50m,
                    FollowupFee = 150m + n * 25m,
                    CurrencyCode = "SAR",
                    ClinicMasterId = clinics[p.ClinicKey].Id,
                    IsAvailableForAppointments = true,
                    AvailableSlotsPerDay = 20,
                    Board = "Saudi Board of " + p.Specialty,
                    BoardLicenseNumber = $"SB-{spec.Code}-{n:000}",
                    IsActive = true,
                    CreatedBy = SeedUser,
                };
                db.DoctorMasters.Add(doctor);
                db.DoctorQualifications.Add(new DoctorQualification
                {
                    DoctorMasterId = doctor.Id,
                    QualificationType = "Degree",
                    QualificationName = doctor.QualificationDegree!,
                    UniversityName = doctor.UniversityName,
                    IssuedDate = new DateTime(2005 + n, 6, 1),
                    IsExpiring = false,
                    CertificateNumber = $"{doctor.QualificationDegree}-{spec.Code}-{n:000}",
                    VerificationStatus = "Verified",
                    CreatedBy = SeedUser,
                });
                db.DoctorQualifications.Add(new DoctorQualification
                {
                    DoctorMasterId = doctor.Id,
                    QualificationType = "Board Certification",
                    QualificationName = "Saudi Board of " + p.Specialty,
                    UniversityName = "Saudi Commission for Health Specialties",
                    IssuedDate = new DateTime(2012 + n, 9, 1),
                    ExpiryDate = new DateTime(DateTime.UtcNow.Year + 2, 9, 1),
                    IsExpiring = true,
                    CertificateNumber = $"SCFHS-{spec.Code}-{n:000}",
                    VerificationStatus = "Verified",
                    CreatedBy = SeedUser,
                });
            }
            h.Practitioners[p.Key] = practitioner;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("✅ Sample hospital ready: {Name} ({License}).", spec.Name, spec.License);
        return h;
    }

    private static Organization AddOrganization(ApplicationDbContext db, HospitalSpec spec)
    {
        var org = new Organization
        {
            OrganizationName = spec.Name,
            LicenseNumber = spec.License,
            LicenseSystem = "http://nphies.sa/license/provider-license",
            OrganizationType = "prov",
            ProviderTypeCode = "1",
            SpecializationType = "Hospital",
            NphiesOrganizationId = spec.License,
            NphiesProviderId = spec.License,
            Website = "https://www." + spec.Domain,
            Email = "rcm@" + spec.Domain,
            PhoneNumber = "+96611" + spec.License[^5..] + "00",
            AddressLine1 = spec.Street,
            AddressLine2 = string.Empty,
            City = spec.City,
            State = spec.Region,
            PostalCode = spec.Postal,
            AddressCountry = "SA",
            Status = "active",
            TaxRegistrationNumber = "3" + spec.License[^5..] + "000000003",
        };
        db.Organizations.Add(org);
        return org;
    }

    private static async Task<Location> EnsureLocationAsync(ApplicationDbContext db, Hospital h, string license, string name, string type, string typeDescription)
    {
        var location = await db.Locations.FirstOrDefaultAsync(l => l.LocationLicense == license);
        if (location != null) return location;
        location = new Location
        {
            LocationName = name,
            LocationLicense = license,
            LicenseSystem = "http://nphies.sa/license/location-license",
            OrganizationId = h.Org.Id,
            FacilityType = type,
            FacilityTypeDescription = typeDescription,
            AddressLine1 = h.Spec.Street,
            AddressLine2 = string.Empty,
            City = h.Spec.City,
            State = h.Spec.Region,
            PostalCode = h.Spec.Postal,
            Country = "SA",
            Phone = h.Org.PhoneNumber,
            Email = h.Org.Email,
            Status = "active",
        };
        db.Locations.Add(location);
        return location;
    }

    // ===================================================================== patients, coverages, claims

    private sealed record PersonSpec(string First, string Last, string FirstAr, string LastAr, string Gender, string IdType);

    private static readonly PersonSpec[] People =
    {
        new("Mohammed", "Al-Qahtani", "محمد", "القحطاني", "M", "NI"),
        new("Noura", "Al-Otaibi", "نورة", "العتيبي", "F", "NI"),
        new("Imran", "Siddiqui", "عمران", "صديقي", "M", "PRC"),
        new("Reem", "Al-Shehri", "ريم", "الشهري", "F", "NI"),
        new("Joseph", "Mathew", "جوزيف", "ماثيو", "M", "PPN"),
        new("Aisha", "Khan", "عائشة", "خان", "F", "PRC"),
        new("Abdullah", "Al-Harbi", "عبدالله", "الحربي", "M", "NI"),
        new("Sara", "Al-Zahrani", "سارة", "الزهراني", "F", "NI"),
        new("Ahmed", "Hassan", "أحمد", "حسن", "M", "PRC"),
        new("Layla", "Haddad", "ليلى", "حداد", "F", "PPN"),
        new("Khalid", "Al-Ghamdi", "خالد", "الغامدي", "M", "NI"),
        new("Fatima", "Rahman", "فاطمة", "رحمن", "F", "PRC"),
        new("Faisal", "Al-Dosari", "فيصل", "الدوسري", "M", "NI"),
        new("Huda", "Al-Mutairi", "هدى", "المطيري", "F", "NI"),
        new("Rajesh", "Kumar", "راجيش", "كومار", "M", "PRC"),
        new("Mariam", "Al-Anazi", "مريم", "العنزي", "F", "NI"),
        new("Omar", "Farouk", "عمر", "فاروق", "M", "PPN"),
        new("Yara", "Al-Shammari", "يارا", "الشمري", "F", "NI"),
    };

    private const int PatientsPerHospital = 6;

    /// <summary>ISO 3166-1 alpha-3 passport issuing countries for the passport holders.</summary>
    private static readonly string[] Nationalities = { "IND", "LBN", "EGY", "PAK", "PHL" };

    private sealed class Party
    {
        public Patient Patient { get; init; } = null!;
        public Coverage Coverage { get; init; } = null!;
        public bool Vat => Patient.NationalIdType != "NI";
    }

    private static async Task<List<Party>> EnsurePatientsAsync(ApplicationDbContext db, Hospital h, int hIndex,
        List<PayerMaster> payers, Dictionary<string, Organization> insurers, DateTime now)
    {
        var parties = new List<Party>();
        for (var i = 0; i < PatientsPerHospital; i++)
        {
            var global = hIndex * PatientsPerHospital + i;
            var person = People[global % People.Length];
            var mrn = $"{ClaimPrefix}{h.Spec.Code}-MRN-{i + 1:000}";
            var patient = await db.Patients.FirstOrDefaultAsync(p => p.MRN == mrn);
            if (patient is null)
            {
                var (idValue, idSystem) = person.IdType switch
                {
                    "NI" => ($"10{global + 1:00000000}", "http://nphies.sa/identifier/nationalid"),
                    "PRC" => ($"24{global + 1:00000000}", "http://nphies.sa/identifier/iqama"),
                    _ => ($"P{7000000 + global + 1}", "http://nphies.sa/identifier/passportnumber"),
                };
                patient = new Patient
                {
                    MRN = mrn,
                    IdentifierSystem = h.Base + "/mrn",
                    NationalId = idValue,
                    NationalIdSystem = idSystem,
                    NationalIdType = person.IdType,
                    FirstName = person.First,
                    LastName = person.Last,
                    FullName = $"{person.First} {person.Last}",
                    FirstNameArabic = person.FirstAr,
                    LastNameArabic = person.LastAr,
                    FullNameArabic = $"{person.FirstAr} {person.LastAr}",
                    DateOfBirth = new DateTime(1960 + global * 2 % 50, 1 + global % 12, 1 + global * 3 % 27),
                    Gender = person.Gender,
                    MaritalStatus = global % 3 == 0 ? "U" : "M",
                    Occupation = (global % 4) switch { 0 => "business", 1 => "administration", 2 => "education", _ => "medical field" },
                    NationalIdCountry = person.IdType == "PPN" ? Nationalities[global % Nationalities.Length] : null,
                    Active = true,
                    Email = $"{person.First.ToLowerInvariant()}.{person.Last.ToLowerInvariant().Replace("-", "")}{global + 1}@mail.sa",
                    Phone = $"+9665{global + 1:00000000}",
                    AddressLine1 = h.Spec.Street,
                    AddressLine2 = string.Empty,
                    City = h.Spec.City,
                    State = h.Spec.Region,
                    PostalCode = h.Spec.Postal,
                    Country = "SA",
                    Status = "active",
                };
                db.Patients.Add(patient);
            }

            var policyNumber = $"{ClaimPrefix}{h.Spec.Code}-POL-{i + 1:000}";
            var coverage = await db.Coverages.FirstOrDefaultAsync(c => c.PolicyNumber == policyNumber);
            if (coverage is null)
            {
                var payer = payers[global % payers.Count];
                var policy = payer.Policies.OrderBy(p => p.PolicyCode).ElementAt(global % payer.Policies.Count);
                var memberId = $"{payer.PayerId[4..]}{global + 1:0000000}";
                coverage = new Coverage
                {
                    PolicyNumber = policyNumber,
                    MemberID = memberId,
                    CoverageIdentifierSystem = $"http://{(PayerSpecs.FirstOrDefault(s => s.PayerId == payer.PayerId)?.Domain ?? "payer.com")}/memberid",
                    CoverageIdentifierValue = memberId,
                    CoverageType = "EHCPOL",
                    PlanCode = policy.PolicyCode,
                    CoverageClass = policy.PolicyName,
                    CoverageClassType = "group",
                    CoverageClassValue = $"CB{100 + global}",
                    Status = "active",
                    SubscriberRelationship = "self",
                    RelationToSubscriber = "self",
                    SubscriberMRN = mrn,
                    SubscriberPatientId = patient.Id,
                    PatientId = patient.Id,
                    InsurerId = insurers[payer.Id].Id,
                    CoverageStartDate = now.AddDays(-300),
                    CoverageEndDate = now.AddDays(65),
                    AnnualDeductible = policy.AnnualDeductible,
                    Copay = policy.CopaymentAmount ?? 50m,
                    CoinsurancePercent = policy.CoinsurancePercentage ?? 20m,
                    OutOfPocketMax = policy.MaxOutOfPocket ?? 5000m,
                    MaxCopay = 500m,
                    MaxCopayCurrency = "SAR",
                };
                db.Coverages.Add(coverage);
            }
            parties.Add(new Party { Patient = patient, Coverage = coverage });
        }
        await db.SaveChangesAsync();
        return parties;
    }

    private sealed record ItemSpec(string Code, string System, string Display, decimal Qty, decimal Price,
        string Dx = "1", string? Info = null, string? BodySite = null, string? SubSite = null, string? Alt = null, int CareTeam = 1, int Days = 0,
        (string Code, string Display, decimal Qty, decimal Price)[]? Details = null);

    private sealed record InfoSpec(string Category, string? Code = null, string? CodeSystem = null, string? Text = null,
        decimal? Quantity = null, string? Unit = null, DateTime? Date = null);

    private static readonly HashSet<string> TimedInfoCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "vital-sign-systolic", "vital-sign-diastolic", "vital-sign-height", "vital-sign-weight", "pulse", "temperature",
        "respiratory-rate", "oxygen-saturation", "birth-weight", "employmentImpacted", "icu-hours", "lab-test", "ventilation-hours",
        "onset", "last-menstrual-period", "missingtooth",
    };

    /// <summary>
    /// Adds the supporting-info slices the nphies-fs 1.0.0 professional/institutional profiles require (min ≥ 1)
    /// and dates timed observations, so the sample claims conform to the current IG. Existing entries are kept;
    /// sequences referenced by items stay stable because defaults are appended.
    /// </summary>
    private static IEnumerable<InfoSpec> CompleteClinicalInfo(string type, InfoSpec[] info, DateTime date,
        (string Code, string Display, string Type)[] diagnoses)
    {
        foreach (var i in info)
            yield return TimedInfoCategories.Contains(i.Category) && i.Date is null ? i with { Date = date.AddHours(9) } : i;
        if (type is not ("professional" or "institutional")) yield break;

        var present = info.Select(i => i.Category).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var principal = diagnoses.FirstOrDefault();
        var at = date.AddHours(9);
        var defaults = new List<InfoSpec>
        {
            new("chief-complaint", principal.Code, Icd10Am, principal.Display),
            new("history-of-present-illness", Text: "Symptoms started recently; no prior episodes."),
            new("patient-history", Text: "No known allergies. No chronic illness."),
            new("physical-examination", Text: "General condition stable."),
            new("treatment-plan", Text: "Treatment as per protocol; follow up in clinic."),
            new("vital-sign-systolic", Quantity: 120, Unit: "mm[Hg]", Date: at),
            new("vital-sign-diastolic", Quantity: 80, Unit: "mm[Hg]", Date: at),
            new("vital-sign-height", Quantity: 170, Unit: "cm", Date: at),
            new("vital-sign-weight", Quantity: 72, Unit: "kg", Date: at),
            new("temperature", Quantity: 37m, Unit: "Cel", Date: at),
            new("pulse", Quantity: 78, Unit: "/min", Date: at),
            new("respiratory-rate", Quantity: 16, Unit: "/min", Date: at),
            new("oxygen-saturation", Quantity: 98, Unit: "%", Date: at),
        };
        defaults.Add(new("investigation-result", "INP", N + "investigation-result", "Investigation(s) not performed"));
        foreach (var d in defaults.Where(d => !present.Contains(d.Category)))
            yield return d;
    }

    private static async Task<int> SeedClaimsAsync(ApplicationDbContext db, Hospital h, int hIndex,
        List<PayerMaster> payers, Dictionary<string, Organization> insurers, DateTime now)
    {
        var parties = await EnsurePatientsAsync(db, h, hIndex, payers, insurers, now);
        var created = 0;
        var seq = 0;
        DateTime Day(int index) => now.AddDays(-((index * 16 + hIndex * 5) % 175) - 1);

        async Task<Claim?> Add(string type, string subType, bool preAuth, Party party, string practitionerKey, DateTime date,
            (string Code, string Display, string Type)[] diagnoses, InfoSpec[] info, ItemSpec[] items, Action<Claim>? customize = null,
            string? encounterClass = null, int stayDays = 0)
        {
            seq++;
            var number = $"{ClaimPrefix}{h.Spec.Code}-{(preAuth ? "PA" : "CL")}-{seq:0000}";
            if (await db.Claims.AnyAsync(c => c.ClaimNumber == number)) return null;

            var practitioner = h.Practitioners[practitionerKey];
            var end = date.AddDays(stayDays);
            var claim = new Claim
            {
                ClaimNumber = number,
                ClaimIdentifierSystem = h.Base + (preAuth ? "/authorization" : "/claim"),
                ClaimIdentifierValue = "req_" + number,
                Status = "active",
                ClaimType = type,
                ClaimTypeSystem = "http://terminology.hl7.org/CodeSystem/claim-type",
                ClaimSubType = subType,
                ClaimSubTypeSystem = N + "claim-subtype",
                Use = preAuth ? "preauthorization" : "claim",
                CreatedDate = preAuth ? date.AddDays(-2) : end,
                ServicedPeriodStart = date,
                ServicedPeriodEnd = end,
                BillablePeriodStart = date,
                BillablePeriodEnd = end,
                Priority = "normal",
                PrioritySystem = "http://terminology.hl7.org/CodeSystem/processpriority",
                PatientId = party.Patient.Id,
                CoverageId = party.Coverage.Id,
                ProviderId = h.Org.Id,
                InsurerId = party.Coverage.InsurerId,
                PractitionerId = practitioner.Id,
                LocationId = type is "oral" or "vision" ? h.Clinic.Id : h.Main.Id,
                PayeeType = "provider",
                PayeeTypeSystem = "http://terminology.hl7.org/CodeSystem/payeetype",
                TotalCurrency = "SAR",
                EpisodeIdentifierSystem = h.Base + "/episode",
                EpisodeIdentifierValue = $"EP-{h.Spec.Code}-{seq:0000}",
                TechnicalReviewStatus = seq % 7 == 0 ? "Pending" : "Approved",
                MedicalReviewStatus = seq % 7 == 0 ? "Pending" : "Approved",
                IsShadowBilling = false,
            };

            if (encounterClass != null)
            {
                var encounterId = $"{ClaimPrefix}{h.Spec.Code}-ENC-{seq:0000}";
                var encounter = await db.Encounters.FirstOrDefaultAsync(e => e.EncounterId == encounterId);
                if (encounter is null)
                {
                    encounter = new Encounter
                    {
                        EncounterId = encounterId,
                        IdentifierSystem = h.Base + "/encounter",
                        IdentifierValue = encounterId,
                        Status = preAuth ? "planned" : "finished",
                        Class = encounterClass,
                        ServiceType = encounterClass == "IMP" ? "cardiology" : "general",
                        PatientId = party.Patient.Id,
                        PeriodStart = date.AddHours(9),
                        PeriodEnd = preAuth ? null : end.AddHours(encounterClass == "IMP" ? 11 : 10),
                        AdmitSource = encounterClass == "IMP" ? "IA" : null,
                        AdmitSourceSystem = encounterClass == "IMP" ? N + "admit-source" : null,
                        ServiceEventType = encounterClass == "IMP" ? "ICSE" : "SCSE",
                        IntendedLengthOfStay = encounterClass == "IMP" ? "IO" : null,
                        ServiceProviderId = h.Org.Id,
                    };
                    db.Encounters.Add(encounter);
                }
                claim.EncounterId = encounter.Id;
            }

            claim.CareTeam.Add(new ClaimCareTeam
            {
                ClaimId = claim.Id, Sequence = 1, PractitionerId = practitioner.Id,
                Role = "primary", RoleSystem = "http://terminology.hl7.org/CodeSystem/claimcareteamrole",
                Qualification = practitioner.Specialization, QualificationSystem = N + "practice-codes",
                QualificationDisplay = practitioner.Qualification,
            });
            if (type == "institutional")
            {
                var assist = h.Practitioners["im"];
                claim.CareTeam.Add(new ClaimCareTeam
                {
                    ClaimId = claim.Id, Sequence = 2, PractitionerId = assist.Id,
                    Role = "assist", RoleSystem = "http://terminology.hl7.org/CodeSystem/claimcareteamrole",
                    Qualification = assist.Specialization, QualificationSystem = N + "practice-codes",
                    QualificationDisplay = assist.Qualification,
                });
            }

            var d = 0;
            foreach (var (code, display, dxType) in diagnoses)
                claim.Diagnoses.Add(new ClaimDiagnosis
                {
                    ClaimId = claim.Id, Sequence = ++d, DiagnosisCode = code, DiagnosisSystem = Icd10Am, DiagnosisDisplay = display,
                    DiagnosisType = dxType, DiagnosisTypeSystem = N + "diagnosis-type",
                    OnAdmissionCode = type == "institutional" ? "y" : null,
                    OnAdmissionSystem = type == "institutional" ? N + "diagnosis-on-admission" : null,
                    ConditionOnsetCode = type == "institutional" && !preAuth ? "NR" : null,
                });

            var s = 0;
            foreach (var i in CompleteClinicalInfo(type, info, date, diagnoses))
                claim.SupportingInfo.Add(new ClaimSupportingInfo
                {
                    ClaimId = claim.Id, Sequence = ++s, Category = i.Category, CategorySystem = N + "claim-information-category",
                    CodeValue = i.Code, CodeSystem = i.CodeSystem, StringValue = i.Text,
                    QuantityValue = i.Quantity, QuantityUnit = i.Unit, QuantitySystem = i.Quantity.HasValue ? Ucum : null,
                    DateValue = i.Date,
                });

            var n = 0;
            foreach (var spec in items)
            {
                n++;
                var gross = spec.Qty * spec.Price;
                var tax = party.Vat ? Math.Round(gross * 0.15m, 2) : 0m;
                var net = gross + tax;
                var copayPct = (party.Coverage.CoinsurancePercent > 0 ? party.Coverage.CoinsurancePercent : 10m) / 100m;
                var item = new ClaimItem
                {
                    ClaimId = claim.Id,
                    Sequence = n,
                    CareTeamSequence = spec.CareTeam,
                    ProductOrServiceCode = spec.Code,
                    ProductOrServiceSystem = spec.System,
                    ProductOrServiceDisplay = spec.Display,
                    AltProductOrServiceCode = spec.Alt,
                    AltProductOrServiceSystem = spec.Alt is null ? null : h.Base + "/CodeSystem/services",
                    Quantity = spec.Qty,
                    UnitPrice = spec.Price,
                    Factor = 1m,
                    Tax = tax,
                    TaxRate = party.Vat ? 15m : 0m,
                    Net = net,
                    PatientShare = Math.Min(Math.Round(net * copayPct, 2), 500m),
                    PatientShareCurrency = "SAR",
                    IsPackage = spec.Details is { Length: > 0 },
                    BodySiteCode = spec.BodySite,
                    BodySiteSystem = spec.BodySite is null ? null : type == "oral" ? N + "fdi-oral-region" : N + "body-site",
                    SubSiteCode = spec.SubSite,
                    SubSiteSystem = spec.SubSite is null ? null : type == "oral" ? N + "fdi-tooth-surface" : N + "sub-site",
                    DiagnosisSequence = spec.Dx,
                    InformationSequence = spec.Info,
                    PatientInvoiceSystem = h.Base + "/patientInvoice",
                    PatientInvoiceValue = $"INV-{number}-{n}",
                    LocationId = claim.LocationId,
                };
                if (spec.Days > 0)
                {
                    item.ServicedPeriodStart = date;
                    item.ServicedPeriodEnd = date.AddDays(spec.Days);
                }
                else
                {
                    item.ServicedDate = date;
                }
                var ds = 0;
                foreach (var detail in spec.Details ?? Array.Empty<(string, string, decimal, decimal)>())
                    item.Details.Add(new ClaimItemDetail
                    {
                        ClaimItemId = item.Id, Sequence = ++ds, ProductOrServiceCode = detail.Code, ProductOrServiceSystem = spec.System,
                        ProductOrServiceDisplay = detail.Display, Quantity = detail.Qty, UnitPrice = detail.Price, Net = detail.Qty * detail.Price,
                    });
                claim.Items.Add(item);
            }
            claim.Total = claim.Items.Sum(i => i.Net ?? 0m);

            customize?.Invoke(claim);
            db.Claims.Add(claim);
            created++;
            return claim;
        }

        var services = N + "services";
        var procedures = N + "procedures";
        var oral = N + "oral-health-op";
        var meds = N + "medication-codes";
        var p = parties;

        // 1. Professional OP consultation claim (female patient: last menstrual period).
        var consult = await Add("professional", "op", false, p[1], "gp", Day(1),
            new[] { ("N94.6", "Dysmenorrhoea, unspecified", "principal"), ("R51", "Headache", "secondary") },
            new[]
            {
                new InfoSpec("chief-complaint", "N94.6", Icd10Am, "Lower abdominal pain during menses"),
                new InfoSpec("patient-history", Text: "No known allergies. No previous surgeries."),
                new InfoSpec("history-of-present-illness", Text: "Cramping pain for 2 days with headache."),
                new InfoSpec("physical-examination", Text: "Mild suprapubic tenderness, vitals stable."),
                new InfoSpec("treatment-plan", Text: "NSAIDs and follow up in 2 weeks."),
                new InfoSpec("last-menstrual-period", Date: Day(1).AddDays(-2)),
                new InfoSpec("vital-sign-systolic", Quantity: 118, Unit: "mm[Hg]"),
                new InfoSpec("vital-sign-diastolic", Quantity: 76, Unit: "mm[Hg]"),
            },
            new[]
            {
                new ItemSpec("83600-00-10", services, "Consultation - general practitioner", 1, 250m, "1,2", "1,2,3"),
                new ItemSpec("66500-00-00", N + "laboratory", "Lipid profile", 1, 120m, "1"),
            }, encounterClass: "AMB");

        // 2-3. Institutional IP prior authorization (CABG) and the claim that consumes it.
        InfoSpec[] inpatientInfo =
        {
            new("chief-complaint", "R07.1", Icd10Am, "Chest pain on exertion radiating to left arm"),
            new("reason-for-visit", "new-visit", N + "visit-reason"),
            new("vital-sign-systolic", Quantity: 145, Unit: "mm[Hg]"),
            new("vital-sign-diastolic", Quantity: 92, Unit: "mm[Hg]"),
            new("vital-sign-height", Quantity: 172, Unit: "cm"),
            new("vital-sign-weight", Quantity: 88, Unit: "kg"),
            new("temperature", Quantity: 37.1m, Unit: "Cel"),
            new("pulse", Quantity: 96, Unit: "/min"),
            new("respiratory-rate", Quantity: 18, Unit: "/min"),
            new("oxygen-saturation", Quantity: 95, Unit: "%"),
            new("icu-hours", Quantity: 48, Unit: "h"),
            new("ventilation-hours", Quantity: 12, Unit: "h"),
            new("investigation-result", "other", N + "investigation-result", "Coronary angiography: triple vessel disease"),
        };
        var cabgItems = new[]
        {
            new ItemSpec("38618-00-00", procedures, "Coronary artery bypass graft", 1, 45000m, "1,2", "1,2,13", CareTeam: 1,
                Details: new[] { ("90901-00-00", "Anaesthesia - general", 1m, 3500m), ("11000-00-00", "Electroencephalography", 1m, 900m) }),
            new ItemSpec("83600-00-00", services, "Hospital accommodation - per day", 5, 1200m, "1", "11", CareTeam: 2, Days: 5),
        };
        var preAuth = await Add("institutional", "ip", true, p[0], "cardio", Day(2),
            new[] { ("R07.1", "Chest pain on breathing", "principal"), ("I21.9", "Acute myocardial infarction, unspecified", "secondary") },
            inpatientInfo, cabgItems, encounterClass: "IMP", stayDays: 5);
        await Add("institutional", "ip", false, p[0], "cardio", Day(2),
            new[] { ("R07.1", "Chest pain on breathing", "principal"), ("I21.9", "Acute myocardial infarction, unspecified", "secondary") },
            inpatientInfo, cabgItems,
            c =>
            {
                c.PreAuthorizationRef = $"PA-{h.Spec.Code}-{hIndex + 1}{seq:0000}";
                c.PreAuthPeriodEnd = Day(2).AddDays(30);
                if (preAuth != null)
                    c.RelatedClaims.Add(new ClaimRelated
                    {
                        ClaimId = c.Id, RelatedClaimIdentifierSystem = preAuth.ClaimIdentifierSystem, RelatedClaimIdentifierValue = preAuth.ClaimIdentifierValue,
                        Relationship = "associated", RelationshipSystem = "http://terminology.hl7.org/CodeSystem/related-claim-relationship",
                        RelationshipDisplay = "Associated prior authorization", ReferencedClaimId = preAuth.Id,
                    });
            }, encounterClass: "IMP", stayDays: 5);

        // 4-5. Oral prior authorization (crown) and oral claim (exam + root canal) with tooth details.
        await Add("oral", "op", true, p[2], "dental", Day(4),
            new[] { ("K04.0", "Pulpitis", "principal") },
            new[] { new InfoSpec("chief-complaint", "K04.0", Icd10Am, "Pain in lower left molar") },
            new[] { new ItemSpec("618", oral, "Full crown - metallic", 1, 1800m, BodySite: "37", SubSite: "O") });
        var oralClaim = await Add("oral", "op", false, p[2], "dental", Day(5),
            new[] { ("K02.1", "Caries of dentine", "principal"), ("K01.1", "Impacted teeth", "secondary") },
            new[] { new InfoSpec("chief-complaint", "K02.1", Icd10Am, "Sensitivity on upper right canine") },
            new[]
            {
                new ItemSpec("012", oral, "Periodic oral examination", 1, 150m),
                new ItemSpec("515", oral, "Root canal treatment - one canal", 1, 900m, BodySite: "13", SubSite: "M"),
                new ItemSpec("111", oral, "Removal of plaque and calculus", 1, 250m),
            });
        if (oralClaim != null)
        {
            var rct = oralClaim.Items.Single(i => i.Sequence == 2);
            db.OralDetails.Add(new OralDetail { ClaimItemId = rct.Id, ToothCode = "13", ToothCodeSystem = N + "fdi-oral-region", ToothSurface = "M", ToothSurfaceSystem = N + "fdi-tooth-surface", IsExtraction = false, Notes = "Endodontic treatment" });
        }

        // 6. Pharmacy OP claim with days-supply.
        await Add("pharmacy", "op", false, p[3], "gp", Day(6),
            new[] { ("J06.9", "Acute upper respiratory infection, unspecified", "principal"), ("T36.9", "Adverse effect of systemic antibiotic, unspecified", "secondary") },
            new[] { new InfoSpec("days-supply", Quantity: 7, Unit: "d"), new InfoSpec("days-supply", Quantity: 5, Unit: "d") },
            new[]
            {
                new ItemSpec("06285096001627", meds, "Paracetamol 500 mg film-coated tablets (24)", 1, 9.50m, "1", "1"),
                new ItemSpec("05944736008570", meds, "Amoxicillin 500 mg capsules (20)", 1, 21.00m, "1", "2"),
            },
            c => c.PrescriptionIdentifier = $"RX-{h.Spec.Code}-{seq:0000}");

        // 7-8. Vision prior authorization (lenses) and vision claim with a prescription.
        await Add("vision", "op", true, p[4], "eye", Day(7),
            new[] { ("H52.1", "Myopia", "principal") }, Array.Empty<InfoSpec>(),
            new[] { new ItemSpec("96092-00-10", procedures, "Supply of spectacle lenses", 2, 450m) });
        var visionClaim = await Add("vision", "op", false, p[4], "eye", Day(8),
            new[] { ("Z01.0", "Examination of eyes and vision", "principal"), ("H52.1", "Myopia", "secondary") },
            Array.Empty<InfoSpec>(),
            new[]
            {
                new ItemSpec("42503-00-02", procedures, "Eye examination with refraction", 1, 200m),
                new ItemSpec("96092-00-10", procedures, "Supply of spectacle lenses", 2, 450m, "2"),
            });
        if (visionClaim != null)
        {
            var lens = visionClaim.Items.Single(i => i.Sequence == 2);
            db.VisionPrescriptions.AddRange(
                new VisionPrescription { ClaimItemId = lens.Id, LensSpecification = "OD -1.25 / -0.50 x 180", Product = "lens", Eye = "right", Sphere = -1.25m, Cylinder = -0.50m, Axis = 180, Brand = "Essilor" },
                new VisionPrescription { ClaimItemId = lens.Id, LensSpecification = "OS -1.00 / -0.25 x 170", Product = "lens", Eye = "left", Sphere = -1.00m, Cylinder = -0.25m, Axis = 170, Brand = "Essilor" });
        }

        // 9. Shadow billing: NPHIES code plus the hospital's internal code.
        await Add("professional", "op", false, p[5], "im", Day(9),
            new[] { ("M54.5", "Low back pain", "principal") },
            new[] { new InfoSpec("chief-complaint", "M54.5", Icd10Am, "Low back pain for 3 weeks") },
            new[]
            {
                new ItemSpec("83620-00-10", services, "Consultation - specialist follow-up", 1, 300m, Alt: $"{h.Spec.Code}-CONS-SPEC"),
                new ItemSpec("58500-00-00", N + "imaging", "Magnetic resonance imaging - knee", 1, 1600m, Alt: $"{h.Spec.Code}-MRI-01"),
            },
            c => c.IsShadowBilling = true, encounterClass: "AMB");

        // 10. Emergency visit after a road traffic accident.
        await Add("professional", "op", false, p[0], "gp", Day(10),
            new[] { ("S83.1", "Dislocation of knee", "principal") },
            new[]
            {
                new InfoSpec("chief-complaint", "S83.1", Icd10Am, "Right knee pain after car accident"),
                new InfoSpec("onset", "S83.1", Icd10Am, Date: Day(10)),
            },
            new[]
            {
                new ItemSpec("83600-00-10", services, "Consultation - general practitioner", 1, 250m, BodySite: "RIGHT"),
                new ItemSpec("50115-00-00", procedures, "Arthroscopy of knee", 1, 7800m),
            },
            c =>
            {
                c.AccidentDate = Day(10);
                c.AccidentType = "MVA";
                c.AccidentTypeSystem = "http://terminology.hl7.org/CodeSystem/v3-ActCode";
                db.ClaimAccidents.Add(new ClaimAccident
                {
                    ClaimId = c.Id, AccidentDate = Day(10), AccidentType = "MVA", AccidentTypeSystem = c.AccidentTypeSystem,
                    AccidentLocation = "King Fahd Road", AccidentLocationCity = h.Spec.City, AccidentLocationState = h.Spec.Region,
                    AccidentLocationCountry = "SA", PoliceReportNumber = $"NJM-{h.Spec.Code}-{seq:00000}", IsEmergency = true,
                    Notes = "Najm report attached",
                });
            }, encounterClass: "EMER");

        // 11. Resubmission of claim 1 (related "prior").
        if (consult != null)
            await Add("professional", "op", false, p[1], "gp", Day(1),
                new[] { ("N94.6", "Dysmenorrhoea, unspecified", "principal"), ("R51", "Headache", "secondary") },
                new[] { new InfoSpec("chief-complaint", "N94.6", Icd10Am, "Lower abdominal pain during menses") },
                new[] { new ItemSpec("83600-00-10", services, "Consultation - general practitioner", 1, 250m, "1,2", "1") },
                c => c.RelatedClaims.Add(new ClaimRelated
                {
                    ClaimId = c.Id, RelatedClaimIdentifierSystem = consult.ClaimIdentifierSystem, RelatedClaimIdentifierValue = consult.ClaimIdentifierValue,
                    Relationship = "prior", RelationshipSystem = "http://terminology.hl7.org/CodeSystem/related-claim-relationship",
                    RelationshipDisplay = "Resubmission of rejected claim", ReferencedClaimId = consult.Id, Notes = "Corrected diagnosis linkage",
                }), encounterClass: "AMB");

        // 12. Professional OP follow-up for another patient (volume for reports).
        await Add("professional", "op", false, p[3], "im", Day(12),
            new[] { ("J39.3", "Upper respiratory tract hypersensitivity reaction", "principal") },
            new[] { new InfoSpec("chief-complaint", "J39.3", Icd10Am, "Recurrent sneezing and nasal congestion") },
            new[] { new ItemSpec("83620-00-10", services, "Consultation - specialist follow-up", 1, 300m) }, encounterClass: "AMB");

        await db.SaveChangesAsync();
        return created;
    }

    // ===================================================================== helpers

    private static async Task<int> AddMissingAsync<T>(ApplicationDbContext db, DbSet<T> set, Expression<Func<T, string>> key, IEnumerable<T> rows) where T : class
    {
        var existing = (await set.AsNoTracking().Select(key).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var compiled = key.Compile();
        var missing = rows.Where(r => !existing.Contains(compiled(r))).ToList();
        if (missing.Count == 0) return 0;
        set.AddRange(missing);
        await db.SaveChangesAsync();
        return missing.Count;
    }
}
