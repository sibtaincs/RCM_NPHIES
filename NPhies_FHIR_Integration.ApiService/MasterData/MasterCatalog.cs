using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.ApiService.MasterData;

/// <summary>A master table editable through the generic master-data screen, and where it lands in the NPHIES JSON.</summary>
public sealed class MasterDefinition
{
    public required string Table { get; init; }
    public required string Group { get; init; }
    public required string Label { get; init; }
    /// <summary>FHIR element(s) of the request bundle this master feeds.</summary>
    public required string Feeds { get; init; }
    public required string Purpose { get; init; }
    /// <summary>Columns combined to label the row in pick lists.</summary>
    public string[] Display { get; init; } = Array.Empty<string>();
    /// <summary>Columns shown first in the grid.</summary>
    public string[] Summary { get; init; } = Array.Empty<string>();
    /// <summary>Natural key used for duplicate checks (in addition to unique indexes of the EF model).</summary>
    public string[] NaturalKey { get; init; } = Array.Empty<string>();
    /// <summary>Allowed values for coded string columns (NPHIES value sets).</summary>
    public Dictionary<string, string[]> Options { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Foreign-key columns not declared in the EF model, mapped to the referenced master table.</summary>
    public Dictionary<string, string> Lookups { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Cross-field rules (NPHIES business validations) returning field → message.</summary>
    public Func<object, IEnumerable<KeyValuePair<string, string>>>? Rules { get; init; }
    public string CreatePermission { get; init; } = Permissions.MasterDataManage;
    public string UpdatePermission { get; init; } = Permissions.MasterDataManage;
    public string DeletePermission { get; init; } = Permissions.MasterDataManage;
}

/// <summary>
/// Every master that feeds a NPHIES request (eligibility, prior-auth, claim) — see docs/master-data.md for the map.
/// </summary>
public static class MasterCatalog
{
    public const string Parties = "Parties in the bundle";
    public const string Codes = "Codes in items and diagnoses";
    public const string Contracts = "Payer contracts and submission rules";
    public const string Provider = "Provider setup";
    public const string Reference = "Reference";

    private static readonly string[] Status = { "active", "inactive" };

    private static IEnumerable<KeyValuePair<string, string>> PatientRules(object entity)
    {
        if (entity is not Patient p) yield break;
        var id = p.NationalId?.Trim() ?? "";
        switch (p.NationalIdType)
        {
            case "NI" when id.Length > 0 && (id.Length != 10 || id[0] != '1' || !id.All(char.IsDigit)):
                yield return new("nationalId", "National ID must be 10 digits starting with 1 (BV-00797).");
                break;
            case "PRC" when id.Length > 0 && (id.Length != 10 || id[0] != '2' || !id.All(char.IsDigit)):
                yield return new("nationalId", "Iqama must be 10 digits starting with 2 (BV-00798).");
                break;
            case "PPN" when string.IsNullOrWhiteSpace(p.NationalIdCountry):
                yield return new("nationalIdCountry", "Issuing country is required for passports (BV-00481).");
                break;
        }
    }

    public static readonly IReadOnlyList<MasterDefinition> All = new List<MasterDefinition>
    {
        new()
        {
            Table = "Organizations", Group = Parties, Label = "Hospitals & insurers",
            Feeds = "MessageHeader.sender / destination, Claim.provider, Claim.insurer, Coverage.payor (Organization)",
            Purpose = "Provider (prov) and insurer (ins) organizations with their NPHIES licenses.",
            Display = new[] { "organizationName", "licenseNumber" },
            Summary = new[] { "organizationName", "organizationType", "licenseNumber", "nphiesProviderId", "nphiesPayerId", "city", "status" },
            NaturalKey = new[] { "licenseNumber" },
            Options = { ["organizationType"] = new[] { "prov", "ins", "tpa" }, ["status"] = Status },
        },
        new()
        {
            Table = "Locations", Group = Parties, Label = "Facilities / locations",
            Feeds = "Claim.facility, Encounter.serviceProvider, Claim.item.locationCodeableConcept (Location)",
            Purpose = "Licensed facilities belonging to a hospital.",
            Display = new[] { "locationName", "locationLicense" },
            Summary = new[] { "locationName", "locationLicense", "organizationId", "facilityType", "city", "status" },
            NaturalKey = new[] { "locationLicense" },
            Options = { ["status"] = Status },
        },
        new()
        {
            Table = "Practitioners", Group = Parties, Label = "Practitioners",
            Feeds = "Claim.careTeam.provider, careTeam.qualification (Practitioner / PractitionerRole)",
            Purpose = "Licensed clinicians with SCFHS license and specialty.",
            Display = new[] { "firstName", "lastName", "licenseNumber" },
            Summary = new[] { "firstName", "lastName", "licenseNumber", "specialization", "practitionerRole", "organizationId", "status" },
            NaturalKey = new[] { "licenseNumber" },
            Options = { ["status"] = Status, ["practitionerRole"] = new[] { "doctor", "nurse", "pharmacist", "dentist", "researcher", "teacher", "ict" } },
        },
        new()
        {
            Table = "Patients", Group = Parties, Label = "Patients",
            Feeds = "Claim.patient, CoverageEligibilityRequest.patient (Patient: identifier, name, gender, birthDate)",
            Purpose = "Beneficiaries with national ID / iqama / passport and Arabic name.",
            Display = new[] { "fullName", "firstName", "lastName", "nationalId" },
            Summary = new[] { "mrn", "firstName", "lastName", "fullNameArabic", "nationalIdType", "nationalId", "gender", "dateOfBirth" },
            NaturalKey = new[] { "nationalId" },
            Options =
            {
                ["gender"] = new[] { "M", "F", "U" },
                ["nationalIdType"] = new[] { "NI", "PRC", "PPN", "VS", "BN", "MR" },
                ["nationalIdCountry"] = NphiesCodeSystems.CountryAlpha3.ToArray(),
                ["maritalStatus"] = NphiesCodeSystems.MaritalStatusCodes.ToArray(),
                ["occupation"] = NphiesCodeSystems.OccupationCodes.ToArray(),
                ["status"] = Status,
            },
            Rules = PatientRules,
            CreatePermission = Permissions.PatientsCreate, UpdatePermission = Permissions.PatientsUpdate, DeletePermission = Permissions.PatientsUpdate,
        },
        new()
        {
            Table = "Coverages", Group = Parties, Label = "Coverages (insurance cards)",
            Feeds = "Claim.insurance.coverage, CoverageEligibilityRequest.insurance (Coverage: memberId, policy, class, relationship)",
            Purpose = "A patient's membership with an insurer.",
            Display = new[] { "memberID", "policyNumber" },
            Summary = new[] { "memberID", "policyNumber", "patientId", "insurerId", "coverageType", "relationToSubscriber", "coverageStartDate", "coverageEndDate", "status" },
            NaturalKey = new[] { "insurerId", "memberID" },
            Lookups = { ["insurerId"] = "Organizations", ["policyHolderOrganizationId"] = "Organizations", ["subscriberPatientId"] = "Patients" },
            Options =
            {
                ["coverageType"] = new[] { "EHCPOL", "PUBLICPOL" },
                ["relationToSubscriber"] = new[] { "self", "spouse", "child", "parent", "other" },
                ["subscriberRelationship"] = new[] { "self", "spouse", "child", "parent", "other" },
                ["status"] = new[] { "active", "cancelled", "draft", "entered-in-error" },
            },
            CreatePermission = Permissions.PatientsCreate, UpdatePermission = Permissions.PatientsUpdate, DeletePermission = Permissions.PatientsUpdate,
        },

        new()
        {
            Table = "ServiceCodeMasters", Group = Codes, Label = "Services & procedures",
            Feeds = "Claim.item.productOrService (services, procedures, lab, imaging), item.unitPrice default",
            Purpose = "Hospital price list mapped to NPHIES service codes.",
            Display = new[] { "serviceCode", "serviceName" },
            Summary = new[] { "serviceCode", "serviceName", "serviceCategory", "nphiesServiceCode", "defaultPrice", "isRequiresAuthorization", "isActive" },
            NaturalKey = new[] { "serviceCode" },
        },
        new()
        {
            Table = "MedicationCodeMasters", Group = Codes, Label = "Medications",
            Feeds = "Claim.item.productOrService (medication-codes) for pharmacy claims",
            Purpose = "SFDA medication codes with prices.",
        },
        new()
        {
            Table = "MedicalDeviceCodeMasters", Group = Codes, Label = "Medical devices",
            Feeds = "Claim.item.productOrService (medical-devices)",
            Purpose = "Devices and consumables billable on claims.",
        },
        new()
        {
            Table = "DiagnosisCodeMasters", Group = Codes, Label = "Diagnoses (ICD-10-AM)",
            Feeds = "Claim.diagnosis.diagnosisCodeableConcept",
            Purpose = "ICD-10-AM codes for principal / secondary diagnoses.",
        },
        new()
        {
            Table = "ModifierCodeMasters", Group = Codes, Label = "Modifiers",
            Feeds = "Claim.item.modifier",
            Purpose = "Procedure modifiers.",
        },
        new()
        {
            Table = "BenefitCodeMasters", Group = Codes, Label = "Benefit categories",
            Feeds = "CoverageEligibilityRequest.item.category, CoverageEligibilityResponse.insurance.item",
            Purpose = "Benefit categories checked during eligibility.",
        },
        new()
        {
            Table = "NphiesCodeMappings", Group = Codes, Label = "Local → NPHIES code mapping",
            Feeds = "Every coded element (translates local codes to NPHIES code systems)",
            Purpose = "Maps hospital codes to the NPHIES CodeSystem used in the bundle.",
            Display = new[] { "localCode", "nphiesCode" },
            NaturalKey = new[] { "codeType", "localCode" },
        },

        new()
        {
            Table = "PayerMasters", Group = Contracts, Label = "Payers",
            Feeds = "Claim.insurer and MessageHeader.destination license; submission limits",
            Purpose = "Insurer contract terms and NPHIES connectivity.",
            Display = new[] { "payerName", "payerId" },
            Summary = new[] { "payerId", "payerName", "payerNameArabic", "nphiesPayerId", "payerType", "maxClaimsPerDay", "isActive" },
            NaturalKey = new[] { "payerId" },
        },
        new()
        {
            Table = "PayerPolicyMasters", Group = Contracts, Label = "Payer policies / plans",
            Feeds = "Coverage.class (plan), patient share (copay, deductible), prior-auth thresholds",
            Purpose = "Plans offered by each payer.",
            Display = new[] { "policyName", "policyCode" },
            Summary = new[] { "policyCode", "policyName", "payerMasterId", "coverageType", "networkType", "copay", "coinsurancePercentage", "requiresPriorAuth", "isPolicyActive" },
            NaturalKey = new[] { "payerMasterId", "policyCode" },
            Lookups = { ["payerMasterId"] = "PayerMasters" },
        },
        new()
        {
            Table = "PolicyBenefitCoverages", Group = Contracts, Label = "Policy benefit coverage",
            Feeds = "Claim.use (preauthorization required?), expected item adjudication %",
            Purpose = "Per-service coverage percentage, exclusions and prior-auth requirements.",
            Lookups = { ["policyMasterId"] = "PayerPolicyMasters", ["serviceCodeMasterId"] = "ServiceCodeMasters" },
        },
        new()
        {
            Table = "ClaimSubmissionRules", Group = Contracts, Label = "Claim submission rules",
            Feeds = "Pre-submission validation (max amount, item count, required supportingInfo attachments)",
            Purpose = "Payer-specific checks applied before sending.",
            Display = new[] { "ruleName" },
            Lookups = { ["payerMasterId"] = "PayerMasters", ["policyMasterId"] = "PayerPolicyMasters" },
        },

        new()
        {
            Table = "ClinicMasters", Group = Provider, Label = "Clinics / departments",
            Feeds = "Encounter.serviceType and claim department (supportingInfo / encounter)",
            Purpose = "Departments of each hospital.",
            Display = new[] { "clinicName", "clinicCode" },
            NaturalKey = new[] { "organizationId", "clinicCode" },
        },
        new()
        {
            Table = "DoctorMasters", Group = Provider, Label = "Doctors",
            Feeds = "Claim.careTeam defaults (specialty, qualification) per practitioner",
            Purpose = "Practitioner profile used to fill the care team.",
            Display = new[] { "doctorName", "doctorCode" },
            NaturalKey = new[] { "doctorCode" },
            Lookups = { ["practitionerId"] = "Practitioners", ["clinicMasterId"] = "ClinicMasters" },
        },
        new()
        {
            Table = "DoctorQualifications", Group = Provider, Label = "Doctor qualifications",
            Feeds = "Claim.careTeam.qualification",
            Purpose = "Degrees and certificates of each doctor.",
            Display = new[] { "qualificationName" },
            Lookups = { ["doctorMasterId"] = "DoctorMasters" },
        },

        new()
        {
            Table = "ErrorCodeMasters", Group = Reference, Label = "NPHIES error codes",
            Feeds = "Interpretation of errors / adjudication reasons in responses",
            Purpose = "Error code catalog used to explain rejections.",
        },
    };

    private static readonly Dictionary<string, MasterDefinition> ByTable = All.ToDictionary(d => d.Table, StringComparer.OrdinalIgnoreCase);

    public static MasterDefinition? Find(string table) => ByTable.GetValueOrDefault(table);
}
