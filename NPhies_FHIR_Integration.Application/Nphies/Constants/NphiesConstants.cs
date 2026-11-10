namespace NPhies_FHIR_Integration.Application.Nphies.Constants;

/// <summary>
/// NPHIES StructureDefinition (profile) canonicals, as used in <c>meta.profile</c> of the
/// NPHIES example messages (IG "nphies-fs", business version 1.0.0).
/// </summary>
public static class NphiesProfiles
{
    public const string Base = "http://nphies.sa/fhir/ksa/nphies-fs/StructureDefinition/";
    public const string Version = "1.0.0";

    public static string Of(string name) => $"{Base}{name}|{Version}";

    public static readonly string Bundle = Of("bundle");
    public static readonly string MessageHeader = Of("message-header");
    public static readonly string MessageHeaderErrorNotice = Of("MessageHeader-ErrorNotice");
    /// <summary>Legacy Task profile used by the 2.x docs examples; superseded by status/cancel/poll-request.</summary>
    public static readonly string Task = Of("task");
    public static readonly string StatusRequest = Of("status-request");
    public static readonly string CancelRequest = Of("cancel-request");
    public static readonly string PollRequest = Of("poll-request");
    public static readonly string PaymentNotice = Of("payment-notice");
    public static readonly string PaymentReconciliation = Of("payment-reconciliation");
    public static readonly string Communication = Of("communication");
    public static readonly string CommunicationRequest = Of("communication-request");
    public static readonly string Coverage = Of("coverage");
    public static readonly string EligibilityRequest = Of("eligibility-request");
    public static readonly string EligibilityResponse = Of("eligibility-response");
    public static readonly string ClaimResponse = Of("claim-response");
    public static readonly string PriorAuthResponse = Of("prior-auth-response");
    public static readonly string Encounter = Of("encounter");
    public static readonly string Location = Of("location");
    public static readonly string Patient = Of("patient");
    public static readonly string Practitioner = Of("practitioner");
    public static readonly string PractitionerRole = Of("practitioner-role");
    public static readonly string VisionPrescription = Of("vision-prescription");
    public static readonly string InsurerOrganization = Of("insurer-organization");
    public static readonly string ProviderOrganization = Of("provider-organization");
    public static readonly string PolicyholderOrganization = Of("policyholder-organization");
    public static readonly string Organization = Of("organization");

    public static readonly string InstitutionalClaim = Of("institutional-claim");
    public static readonly string OralClaim = Of("oral-claim");
    public static readonly string PharmacyClaim = Of("pharmacy-claim");
    public static readonly string ProfessionalClaim = Of("professional-claim");
    public static readonly string VisionClaim = Of("vision-claim");

    public static readonly string InstitutionalPriorAuth = Of("institutional-priorauth");
    public static readonly string OralPriorAuth = Of("oral-priorauth");
    public static readonly string PharmacyPriorAuth = Of("pharmacy-priorauth");
    public static readonly string ProfessionalPriorAuth = Of("professional-priorauth");
    public static readonly string VisionPriorAuth = Of("vision-priorauth");

    /// <summary>Returns the Claim profile for the given claim type ("institutional", "oral", ...) and use.</summary>
    public static string ForClaim(string claimTypeCode, bool preAuthorization) =>
        Of(preAuthorization ? $"{claimTypeCode}-priorauth" : $"{claimTypeCode}-claim");
}

/// <summary>NPHIES extension URLs (all under the nphies-fs StructureDefinition base, without version).</summary>
public static class NphiesExtensions
{
    private const string B = NphiesProfiles.Base + "extension-";

    // Claim
    public const string EligibilityOfflineReference = B + "eligibility-offline-reference";
    public const string EligibilityOfflineDate = B + "eligibility-offline-date";
    public const string AuthorizationOfflineDate = B + "authorization-offline-date";
    public const string Episode = B + "episode";
    public const string Transfer = B + "transfer";
    public const string Encounter = B + "encounter";
    public const string BatchIdentifier = B + "batch-identifier";
    public const string BatchNumber = B + "batch-number";
    public const string BatchPeriod = B + "batch-period";
    public const string EligibilityResponse = B + "eligibility-response";
    public const string KsaDiagnosisRelatedGroup = B + "ksa-diagnosis-related-group";

    // Claim.item
    public const string Tax = B + "tax";
    public const string PatientShare = B + "patient-share";
    public const string PayerShare = B + "payer-share";
    public const string Package = B + "package";
    public const string PatientInvoice = B + "patientInvoice";
    public const string Maternity = B + "maternity";

    // Claim.diagnosis
    public const string ConditionOnset = B + "condition-onset";

    // ClaimResponse
    public const string AdjudicationOutcome = B + "adjudication-outcome";
    public const string AdjudicationReissue = B + "adjudication-reissue";
    public const string TransferAuthorizationNumber = B + "transferAuthorizationNumber";
    public const string TransferAuthorizationPeriod = B + "transferAuthorizationPeriod";
    public const string TransferAuthorizationProvider = B + "transferAuthorizationProvider";
    public const string OriginalRequest = B + "original-request";
    public const string OriginalResponse = B + "original-response";

    // CoverageEligibilityResponse
    public const string SiteEligibility = B + "siteEligibility";
    public const string NotInForceReason = B + "not-in-force-reason";

    // PaymentReconciliation
    public const string ComponentPayment = B + "component-payment";
    public const string ComponentEarlyFee = B + "component-early-fee";
    public const string ComponentNphiesFee = B + "component-nphies-fee";

    // Other resources
    public const string ServiceEventType = B + "serviceEventType";
    public const string ProviderType = B + "provider-type";
    public const string KsaAdministrativeGender = B + "ksa-administrative-gender";
    public const string Occupation = B + "occupation";
    public const string IdentifierCountry = B + "identifier-country";
    public const string PatientReligion = B + "patient-religion";
    public const string ErrorExpression = B + "error-expression";
}

/// <summary>Code systems used by NPHIES messages.</summary>
public static class NphiesCodeSystems
{
    private const string N = "http://nphies.sa/terminology/CodeSystem/";
    private const string T = "http://terminology.hl7.org/CodeSystem/";

    // NPHIES
    public const string MessageEvents = N + "ksa-message-events";
    public const string ClaimSubType = N + "claim-subtype";
    public const string PracticeCodes = N + "practice-codes";
    public const string DiagnosisType = N + "diagnosis-type";
    public const string DiagnosisOnAdmission = N + "diagnosis-on-admission";
    public const string ConditionOnset = N + "condition-onset";
    public const string FdiOralRegion = N + "fdi-oral-region";
    public const string FdiToothSurface = N + "fdi-tooth-surface";
    public const string BodySite = N + "body-site";
    public const string SubSite = N + "subsite";
    public const string ClaimInformationCategory = N + "claim-information-category";
    public const string VisitReason = N + "visit-reason";
    public const string InvestigationResult = N + "investigation-result";
    public const string MissingToothReason = N + "missing-tooth-reason";
    public const string Services = N + "services";
    public const string Procedures = N + "procedures";
    public const string Imaging = N + "imaging";
    public const string Laboratory = N + "laboratory";
    public const string MedicalDevices = N + "medical-devices";
    public const string MedicationCodes = N + "medication-codes";
    public const string OralHealthOp = N + "oral-health-op";
    public const string OralHealthIp = N + "oral-health-ip";
    public const string TransportationSrca = N + "transportation-srca";
    public const string RelatedClaimRelationship = N + "related-claim-relationship";
    public const string AdjudicationOutcome = N + "adjudication-outcome";
    public const string ReissueReason = N + "reissue-reason";
    public const string KsaAdjudication = N + "ksa-adjudication";
    public const string AdjudicationReason = N + "adjudication-reason";
    public const string AdjudicationError = N + "adjudication-error";
    public const string CoverageType = N + "coverage-type";
    public const string CoverageCopayType = N + "coverage-copay-type";
    public const string SiteEligibility = N + "siteEligibility";
    public const string BenefitType = N + "benefit-type";
    public const string BenefitCategory = N + "benefit-category";
    public const string ServiceEventType = N + "service-event-type";
    public const string ProviderType = N + "provider-type";
    public const string OrganizationType = N + "organization-type";
    public const string KsaAdministrativeGender = N + "ksa-administrative-gender";
    public const string Occupation = N + "occupation";

    /// <summary>Codes of the required occupation value set (nphies-fs 1.0.0).</summary>
    public static readonly IReadOnlySet<string> OccupationCodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "education", "housewife", "business", "student", "medical field", "skilled worker", "administration",
        "military", "unknown", "unemployed", "agriculture", "marine", "others", "oil industries",
    };

    /// <summary>HL7 v3 MaritalStatus codes plus the NullFlavor UNK accepted by the NPHIES binding.</summary>
    public static readonly IReadOnlyList<string> MaritalStatusCodes = new[] { "A", "D", "I", "L", "M", "P", "S", "T", "U", "W", "UNK" };

    /// <summary>ISO 3166-1 alpha-3 country codes (identifier-country extension, value set iso3166-1-3).</summary>
    public static readonly IReadOnlyList<string> CountryAlpha3 = (
        "AFG ALA ALB DZA ASM AND AGO AIA ATA ATG ARG ARM ABW AUS AUT AZE BHS BHR BGD BRB BLR BEL BLZ BEN BMU BTN BOL BES BIH BWA BVT BRA IOT " +
        "BRN BGR BFA BDI CPV KHM CMR CAN CYM CAF TCD CHL CHN CXR CCK COL COM COG COD COK CRI CIV HRV CUB CUW CYP CZE DNK DJI DMA DOM ECU EGY " +
        "SLV GNQ ERI EST SWZ ETH FLK FRO FJI FIN FRA GUF PYF ATF GAB GMB GEO DEU GHA GIB GRC GRL GRD GLP GUM GTM GGY GIN GNB GUY HTI HMD VAT " +
        "HND HKG HUN ISL IND IDN IRN IRQ IRL IMN ISR ITA JAM JPN JEY JOR KAZ KEN KIR PRK KOR KWT KGZ LAO LVA LBN LSO LBR LBY LIE LTU LUX MAC " +
        "MDG MWI MYS MDV MLI MLT MHL MTQ MRT MUS MYT MEX FSM MDA MCO MNG MNE MSR MAR MOZ MMR NAM NRU NPL NLD NCL NZL NIC NER NGA NIU NFK MKD " +
        "MNP NOR OMN PAK PLW PSE PAN PNG PRY PER PHL PCN POL PRT PRI QAT REU ROU RUS RWA BLM SHN KNA LCA MAF SPM VCT WSM SMR STP SAU SEN SRB " +
        "SYC SLE SGP SXM SVK SVN SLB SOM ZAF SGS SSD ESP LKA SDN SUR SJM SWE CHE SYR TWN TJK TZA THA TLS TGO TKL TON TTO TUN TUR TKM TCA TUV " +
        "UGA UKR ARE GBR USA UMI URY UZB VUT VEN VNM VGB VIR WLF ESH YEM ZMB ZWE").Split(' ');

    /// <summary>ISO 3166 country codes (identifier-country uses alpha-3).</summary>
    public const string Iso3166 = "urn:iso:std:iso:3166";
    public const string PatientIdentifierType = N + "patient-identifier-type";
    public const string PractitionerRole = N + "practitioner-role";
    public const string TaskCode = N + "task-code";
    public const string TaskInputType = N + "task-input-type";
    public const string TaskOutputType = N + "task-output-type";
    public const string TaskReasonCode = N + "task-reason-code";
    public const string TaskStatusReason = N + "task-status-reason";
    public const string MetaTags = N + "meta-tags";
    public const string PaymentMethod = N + "payment-method";
    public const string LensType = N + "lens-type";
    public const string CommunicationReason = N + "communication-reason";
    public const string ClaimPaymentType = N + "claim-payment-type";
    public const string ServiceType = N + "service-type";
    public const string AdmitSource = N + "admit-source";
    public const string DischargeDisposition = N + "discharge-disposition";

    // HL7
    public const string ClaimType = T + "claim-type";
    public const string ProcessPriority = T + "processpriority";
    public const string PayeeType = T + "payeetype";
    public const string ClaimCareTeamRole = T + "claimcareteamrole";
    public const string Adjudication = T + "adjudication";
    public const string CoverageClass = T + "coverage-class";
    public const string SubscriberRelationship = T + "subscriber-relationship";
    public const string V2IdentifierType = T + "v2-0203";
    public const string V3MaritalStatus = T + "v3-MaritalStatus";
    public const string V3NullFlavor = T + "v3-NullFlavor";
    public const string V3ActCode = T + "v3-ActCode";
    public const string V3RoleCode = T + "v3-RoleCode";
    public const string PaymentStatus = T + "paymentstatus";
    public const string PaymentType = T + "payment-type";
    public const string CommunicationCategory = T + "communication-category";
    public const string BenefitNetwork = T + "benefit-network";
    public const string BenefitTerm = T + "benefit-term";
    public const string BenefitUnit = T + "benefit-unit";
    public const string PaymentAdjustmentReason = T + "payment-adjustment-reason";

    // External
    public const string Icd10Am = "http://hl7.org/fhir/sid/icd-10-am";
    public const string Loinc = "http://loinc.org";
    public const string Ucum = "http://unitsofmeasure.org";
}

/// <summary>Identifier systems (licenses and national identifiers).</summary>
public static class NphiesIdentifierSystems
{
    public const string PayerLicense = "http://nphies.sa/license/payer-license";
    public const string ProviderLicense = "http://nphies.sa/license/provider-license";
    public const string PractitionerLicense = "http://nphies.sa/license/practitioner-license";
    public const string LocationLicense = "http://nphies.sa/license/location-license";
    public const string OrganizationLicense = "http://nphies.sa/license/organization-license";
    public const string Nphies = "http://nphies.sa/license/nphies";
    public const string NphiesValue = "NPHIES";
    public const string NphiesEndpoint = "http://nphies.sa";

    /// <summary>Practitioner license system used by PractitionerRole / VisionPrescription in the examples.</summary>
    public const string PractitionerLicenses = "http://nphies.sa/licenses/practitioner";

    public const string NationalId = "http://nphies.sa/identifier/nationalid";
    public const string Iqama = "http://nphies.sa/identifier/iqama";
    public const string BorderNumber = "http://nphies.sa/identifier/bordernumber";
    public const string Passport = "http://nphies.sa/identifier/passportnumber";
    public const string Organization = "http://nphies.sa/identifier/organization";

    public static string PayerEndpoint(string license) => $"http://nphies.sa/license/payer-license/{license}";
    public static string ProviderEndpoint(string license) => $"http://nphies.sa/license/provider-license/{license}";
}

/// <summary>NPHIES message event codes (<c>ksa-message-events</c>).</summary>
public static class NphiesMessageEvents
{
    public const string EligibilityRequest = "eligibility-request";
    public const string EligibilityResponse = "eligibility-response";
    public const string PriorAuthRequest = "priorauth-request";
    public const string PriorAuthResponse = "priorauth-response";
    public const string ClaimRequest = "claim-request";
    public const string ClaimResponse = "claim-response";
    public const string BatchRequest = "batch-request";
    public const string BatchResponse = "batch-response";
    public const string StatusCheck = "status-check";
    public const string StatusResponse = "status-response";
    public const string CancelRequest = "cancel-request";
    public const string CancelResponse = "cancel-response";
    public const string PaymentNotice = "payment-notice";
    public const string PaymentReconciliation = "payment-reconciliation";
    public const string CommunicationRequest = "communication-request";
    public const string Communication = "communication";
    public const string Acknowledgement = "acknowledgement";
    public const string PollRequest = "poll-request";
    public const string PollResponse = "poll-response";
    public const string AdvancedAuthorization = "advanced-authorization";
    public const string PrescriberRequest = "prescriber-request";
    public const string PrescriberResponse = "prescriber-response";
    public const string ErrorNotice = "error-notice";

    /// <summary>Messages that only read state and can be safely re-sent on transient failures.</summary>
    public static bool IsIdempotent(string? eventCode) =>
        eventCode is PollRequest or StatusCheck or EligibilityRequest;

    /// <summary>Maps a request event to the expected response event.</summary>
    public static string? ExpectedResponse(string? requestEvent) => requestEvent switch
    {
        EligibilityRequest => EligibilityResponse,
        PriorAuthRequest => PriorAuthResponse,
        ClaimRequest => ClaimResponse,
        BatchRequest => BatchResponse,
        StatusCheck => StatusResponse,
        CancelRequest => CancelResponse,
        PollRequest => PollResponse,
        Communication => Acknowledgement,
        PaymentNotice => Acknowledgement,
        _ => null
    };
}

/// <summary>NPHIES protocol limits.</summary>
public static class NphiesLimits
{
    public const int SessionTimeoutSeconds = 60;
    public const long MaxMessageSizeBytes = 10 * 1024 * 1024;
    public const int BatchMaxClaims = 200;
    public const int PollMaxMessages = 100;
}
