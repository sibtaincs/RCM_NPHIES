namespace NPhies_FHIR_Integration.Application.Nphies.Models;

/// <summary>A single coding (system/code/display).</summary>
public sealed record NphiesCoding(string System, string Code, string? Display = null);

/// <summary>A codeable concept with one or more codings and optional text.</summary>
public sealed class NphiesConcept
{
    public NphiesConcept() { }

    public NphiesConcept(string system, string code, string? display = null) =>
        Codings.Add(new NphiesCoding(system, code, display));

    public List<NphiesCoding> Codings { get; set; } = new();
    public string? Text { get; set; }

    public static NphiesConcept Of(string system, string code, string? display = null) => new(system, code, display);
}

/// <summary>A business identifier (system/value).</summary>
public sealed record NphiesIdentifier(string System, string Value);

/// <summary>Money in SAR unless otherwise specified.</summary>
public sealed record NphiesMoney(decimal Value, string Currency = "SAR");

public sealed record NphiesPeriod(string? Start, string? End);

public enum NphiesClaimType { Institutional, Oral, Pharmacy, Professional, Vision }

public enum NphiesClaimSubType { None, Op, Ip, Emr }

public enum NphiesClaimUse { Claim, PreAuthorization }

public enum PatientIdentifierKind { NationalId, Iqama, BorderNumber, Passport, DisplacedPerson, MedicalRecord }

public enum NphiesOrganizationKind { Provider, Insurer, Policyholder }

/// <summary>Organization (provider, insurer or policyholder).</summary>
public sealed class OrganizationInput
{
    public string Id { get; set; } = string.Empty;
    /// <summary>License (provider/payer) or organization identifier (policyholder).</summary>
    public string License { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public NphiesOrganizationKind Kind { get; set; }
    /// <summary>Overrides the default identifier system for the organization kind.</summary>
    public string? IdentifierSystem { get; set; }
    public bool IdentifierUseOfficial { get; set; }
    /// <summary>Optional provider-type extension code (http://nphies.sa/terminology/CodeSystem/provider-type).</summary>
    public NphiesCoding? ProviderType { get; set; }
    /// <summary>
    /// Insurer only: v2-0203 identifier type selecting the insurer identifier slice — "NII" (payor, default)
    /// or "NIIP" (TPA). Set to empty to omit the type (legacy examples).
    /// </summary>
    public string? InsurerIdentifierType { get; set; } = "NII";
}

public sealed class PatientInput
{
    public string Id { get; set; } = string.Empty;
    public PatientIdentifierKind IdentifierKind { get; set; } = PatientIdentifierKind.NationalId;
    public string IdentifierValue { get; set; } = string.Empty;
    /// <summary>Overrides the identifier system derived from <see cref="IdentifierKind"/>.</summary>
    public string? IdentifierSystem { get; set; }
    /// <summary>Overrides the identifier type display (defaults per kind, e.g. "iqama").</summary>
    public string? IdentifierTypeDisplay { get; set; }
    /// <summary>ISO 3166-1 alpha-3 issuing country; sent as the identifier-country extension (required for passports).</summary>
    public string? IdentifierCountry { get; set; }
    public string? NameText { get; set; }
    public string? Family { get; set; }
    public List<string> Given { get; set; } = new();
    public string? Phone { get; set; }
    /// <summary>FHIR administrative gender: male, female, other, unknown.</summary>
    public string Gender { get; set; } = "unknown";
    /// <summary>KSA gender extension code (male, female, unknown, U, N, A, B, C). Defaults to <see cref="Gender"/>.</summary>
    public string? KsaGender { get; set; }
    public string BirthDate { get; set; } = string.Empty;
    /// <summary>ksa-marital-status code: v3-MaritalStatus (L, D, M, U, W) or v3-NullFlavor UNK.</summary>
    public string? MaritalStatus { get; set; }
    /// <summary>occupation extension code.</summary>
    public string? Occupation { get; set; }
    public bool? Deceased { get; set; }
}

public sealed class CoverageClassInput
{
    /// <summary>coverage-class code: group, plan, subgroup, ...</summary>
    public string Type { get; set; } = "group";
    public string Value { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public sealed class CoverageInput
{
    public string Id { get; set; } = string.Empty;
    public NphiesIdentifier MemberId { get; set; } = new("http://payer.com/memberid", string.Empty);
    public string Status { get; set; } = "active";
    /// <summary>coverage-type code (EHCPOL, PUBLICPOL).</summary>
    public string Type { get; set; } = "EHCPOL";
    public string? TypeDisplay { get; set; } = "extended healthcare";
    /// <summary>Patient id of the subscriber when different from the beneficiary.</summary>
    public string? SubscriberPatientId { get; set; }
    public string? SubscriberId { get; set; }
    /// <summary>Policy holder: an Organization id (policyholder organization) or a Patient id.</summary>
    public string? PolicyHolderOrganizationId { get; set; }
    public string? PolicyHolderPatientId { get; set; }
    /// <summary>subscriber-relationship code (self, child, spouse, ...).</summary>
    public string Relationship { get; set; } = "self";
    public string? RelationshipDisplay { get; set; }
    public NphiesPeriod? Period { get; set; }
    public List<CoverageClassInput> Classes { get; set; } = new();
    public string? Network { get; set; }
    /// <summary>When true the Coverage subscriber reference is omitted (some eligibility examples).</summary>
    public bool OmitSubscriber { get; set; }
}

public sealed class PractitionerInput
{
    public string Id { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    /// <summary>v2-0203 identifier type (MD by default).</summary>
    public string IdentifierType { get; set; } = "MD";
    public string? NameText { get; set; }
    public string? Family { get; set; }
    public List<string> Given { get; set; } = new();
    public bool NameUseOfficial { get; set; } = true;
}

/// <summary>PractitionerRole (as used in the batch example).</summary>
public sealed class PractitionerRoleInput
{
    public string Id { get; set; } = string.Empty;
    public string PractitionerLicense { get; set; } = string.Empty;
    /// <summary>practitioner-role code (doctor, nurse, ...).</summary>
    public string RoleCode { get; set; } = "doctor";
    /// <summary>practice-codes specialty.</summary>
    public string? Specialty { get; set; }
}

public sealed class LocationInput
{
    public string Id { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Status { get; set; } = "active";
    public NphiesCoding? Type { get; set; }
}

public sealed class EncounterInput
{
    public string Id { get; set; } = string.Empty;
    public NphiesIdentifier? Identifier { get; set; }
    public string Status { get; set; } = "finished";
    /// <summary>v3-ActCode class (AMB, IMP, EMER, ...).</summary>
    public NphiesCoding Class { get; set; } = new("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory");
    /// <summary>service-event-type code (ICSE, SCSE).</summary>
    public NphiesCoding? ServiceEventType { get; set; }
    public NphiesPeriod? Period { get; set; }
}

public sealed class LensSpecificationInput
{
    /// <summary>lens-type code: lens or contact.</summary>
    public string Product { get; set; } = "lens";
    /// <summary>right or left.</summary>
    public string Eye { get; set; } = "right";
    public decimal? Sphere { get; set; }
    public decimal? Cylinder { get; set; }
    public int? Axis { get; set; }
    public decimal? PrismAmount { get; set; }
    /// <summary>up, down, in, out.</summary>
    public string? PrismBase { get; set; }
    public decimal? Add { get; set; }
    public decimal? Power { get; set; }
}

public sealed class VisionPrescriptionInput
{
    public string Id { get; set; } = string.Empty;
    public NphiesIdentifier Identifier { get; set; } = new(string.Empty, string.Empty);
    public string Created { get; set; } = string.Empty;
    public string DateWritten { get; set; } = string.Empty;
    public string PrescriberLicense { get; set; } = string.Empty;
    public List<LensSpecificationInput> Lenses { get; set; } = new();
}

public sealed class CareTeamInput
{
    public int Sequence { get; set; } = 1;
    /// <summary>Id of the Practitioner (or PractitionerRole when <see cref="UsePractitionerRole"/>).</summary>
    public string PractitionerId { get; set; } = string.Empty;
    public bool UsePractitionerRole { get; set; }
    /// <summary>claimcareteamrole code (primary, assist, supervisor, other).</summary>
    public string Role { get; set; } = "primary";
    /// <summary>practice-codes qualification.</summary>
    public string Qualification { get; set; } = string.Empty;
}

public sealed class DiagnosisInput
{
    public int Sequence { get; set; } = 1;
    public string Code { get; set; } = string.Empty;
    public string? Display { get; set; }
    public string System { get; set; } = "http://hl7.org/fhir/sid/icd-10-am";
    /// <summary>diagnosis-type code (principal, secondary, admitting, discharge, differential).</summary>
    public string Type { get; set; } = "principal";
    /// <summary>diagnosis-on-admission code (y, n, u).</summary>
    public string? OnAdmission { get; set; }
    /// <summary>condition-onset code (COEA, CNNA, NR); required on institutional claims.</summary>
    public string? ConditionOnset { get; set; }
}

public sealed class AttachmentInput
{
    public string ContentType { get; set; } = "application/pdf";
    public string? Title { get; set; }
    public string? Creation { get; set; }
    /// <summary>Base64 content.</summary>
    public string? Data { get; set; }
    public string? Url { get; set; }
}

/// <summary>A Claim.supportingInfo entry. Set at most one value (none for category/code-only entries).</summary>
public sealed class SupportingInfoInput
{
    public int Sequence { get; set; }
    /// <summary>claim-information-category code (info, onset, attachment, vital-sign-weight, chief-complaint, ...).</summary>
    public string Category { get; set; } = "info";
    public NphiesConcept? Code { get; set; }
    public string? TimingDate { get; set; }
    public NphiesPeriod? TimingPeriod { get; set; }
    public string? ValueString { get; set; }
    public bool? ValueBoolean { get; set; }
    public decimal? ValueQuantity { get; set; }
    /// <summary>UCUM unit code for <see cref="ValueQuantity"/> (e.g. kg, cm, mm[Hg], h, Cel, /min, %).</summary>
    public string? ValueQuantityUnit { get; set; }
    public AttachmentInput? ValueAttachment { get; set; }
    /// <summary>Reference (fullUrl or relative) for valueReference.</summary>
    public string? ValueReference { get; set; }
    public NphiesCoding? Reason { get; set; }
}

public sealed class ItemDetailInput
{
    public int Sequence { get; set; }
    public NphiesConcept ProductOrService { get; set; } = new();
    public decimal Quantity { get; set; } = 1;
    public NphiesMoney? UnitPrice { get; set; }
    public decimal? Factor { get; set; }
    public NphiesMoney? Net { get; set; }
}

public sealed class ClaimItemInput
{
    public int Sequence { get; set; }
    public List<int> CareTeamSequence { get; set; } = new();
    public List<int> DiagnosisSequence { get; set; } = new();
    public List<int> InformationSequence { get; set; } = new();
    public NphiesConcept ProductOrService { get; set; } = new();
    public string? ServicedDate { get; set; }
    public NphiesPeriod? ServicedPeriod { get; set; }
    public decimal Quantity { get; set; } = 1;
    public NphiesMoney UnitPrice { get; set; } = new(0);
    public decimal? Factor { get; set; }
    /// <summary>Net; computed as quantity * unitPrice * factor + tax when null.</summary>
    public NphiesMoney? Net { get; set; }
    public NphiesConcept? BodySite { get; set; }
    public List<NphiesCoding> SubSite { get; set; } = new();

    // Item extensions
    public NphiesMoney? Tax { get; set; }
    public NphiesMoney? PatientShare { get; set; }
    public NphiesMoney? PayerShare { get; set; }
    public bool? Package { get; set; }
    public bool? Maternity { get; set; }
    public NphiesIdentifier? PatientInvoice { get; set; }

    public List<ItemDetailInput> Details { get; set; } = new();
}

public sealed class RelatedClaimInput
{
    public NphiesIdentifier ClaimIdentifier { get; set; } = new(string.Empty, string.Empty);
    /// <summary>related-claim-relationship code (prior, associated, appealing, extend).</summary>
    public string Relationship { get; set; } = "prior";
    /// <summary>When true, the reference carries type "Claim".</summary>
    public bool IncludeType { get; set; }
}

/// <summary>Batch extensions of a Claim within a batch-request.</summary>
public sealed class BatchInfoInput
{
    public NphiesIdentifier BatchIdentifier { get; set; } = new(string.Empty, string.Empty);
    public int BatchNumber { get; set; }
    public NphiesPeriod BatchPeriod { get; set; } = new(null, null);
}

/// <summary>A preauthorization reference on Claim.insurance.preAuthRef.</summary>
public sealed record PreAuthRefInput(string Value);

/// <summary>Common message envelope overrides (ids and timestamp). Leave null for generated values.</summary>
public abstract class NphiesMessageInput
{
    public string? MessageId { get; set; }
    public string? BundleId { get; set; }
    /// <summary>ISO-8601 timestamp; defaults to now (+03:00).</summary>
    public string? Timestamp { get; set; }
    /// <summary>MessageHeader.source.endpoint override for this message.</summary>
    public string? SourceEndpoint { get; set; }
    /// <summary>When true the MessageHeader fullUrl is {base}/MessageHeader/{id}; otherwise urn:uuid:{id}.</summary>
    public bool MessageHeaderFullUrlAsUrl { get; set; }
    /// <summary>Overrides the base URL used for entry fullUrls (defaults to Sender.ProviderBaseUrl).</summary>
    public string? FullUrlBase { get; set; }
}

/// <summary>Input for a priorauth-request or claim-request.</summary>
public sealed class ClaimRequestInput : NphiesMessageInput
{
    public string ClaimId { get; set; } = string.Empty;
    public NphiesIdentifier Identifier { get; set; } = new(string.Empty, string.Empty);
    public NphiesClaimType Type { get; set; } = NphiesClaimType.Professional;
    public NphiesClaimSubType SubType { get; set; } = NphiesClaimSubType.Op;
    public NphiesClaimUse Use { get; set; } = NphiesClaimUse.Claim;
    public string Status { get; set; } = "active";
    public string Created { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    /// <summary>processpriority code (stat, normal, deferred).</summary>
    public string Priority { get; set; } = "normal";
    /// <summary>payeetype code (provider, subscriber, other).</summary>
    public string PayeeType { get; set; } = "provider";
    public NphiesPeriod? BillablePeriod { get; set; }

    public PatientInput Patient { get; set; } = new();
    /// <summary>Optional separate subscriber patient (e.g. parent of a newborn).</summary>
    public PatientInput? Subscriber { get; set; }
    public OrganizationInput Provider { get; set; } = new() { Kind = NphiesOrganizationKind.Provider };
    public OrganizationInput Insurer { get; set; } = new() { Kind = NphiesOrganizationKind.Insurer };
    public OrganizationInput? Policyholder { get; set; }
    public CoverageInput Coverage { get; set; } = new();
    public List<PreAuthRefInput> PreAuthRefs { get; set; } = new();
    public List<PractitionerInput> Practitioners { get; set; } = new();
    public List<PractitionerRoleInput> PractitionerRoles { get; set; } = new();
    public EncounterInput? Encounter { get; set; }
    public VisionPrescriptionInput? VisionPrescription { get; set; }

    public List<CareTeamInput> CareTeam { get; set; } = new();
    public List<DiagnosisInput> Diagnoses { get; set; } = new();
    public List<SupportingInfoInput> SupportingInfo { get; set; } = new();
    public List<ClaimItemInput> Items { get; set; } = new();
    public List<RelatedClaimInput> Related { get; set; } = new();
    /// <summary>Total; computed from item nets when null.</summary>
    public NphiesMoney? Total { get; set; }

    // Claim extensions
    public string? EligibilityOfflineReference { get; set; }
    public string? EligibilityOfflineDate { get; set; }
    public string? AuthorizationOfflineDate { get; set; }
    public NphiesIdentifier? Episode { get; set; }
    public bool? Transfer { get; set; }
    public BatchInfoInput? Batch { get; set; }
}

/// <summary>Input for an eligibility-request.</summary>
public sealed class EligibilityRequestInput : NphiesMessageInput
{
    public string RequestId { get; set; } = string.Empty;
    public NphiesIdentifier Identifier { get; set; } = new(string.Empty, string.Empty);
    /// <summary>Purposes: benefits, validation, discovery, auth-requirements.</summary>
    public List<string> Purposes { get; set; } = new() { "validation" };
    public string Status { get; set; } = "active";
    public string Created { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public string Priority { get; set; } = "normal";
    public string? ServicedDate { get; set; }
    public NphiesPeriod? ServicedPeriod { get; set; }

    public PatientInput Patient { get; set; } = new();
    public PatientInput? Subscriber { get; set; }
    public OrganizationInput Provider { get; set; } = new() { Kind = NphiesOrganizationKind.Provider };
    public OrganizationInput Insurer { get; set; } = new() { Kind = NphiesOrganizationKind.Insurer };
    public OrganizationInput? Policyholder { get; set; }
    /// <summary>Coverage is optional for discovery requests.</summary>
    public CoverageInput? Coverage { get; set; }
    public bool? CoverageFocal { get; set; }
    public LocationInput? Facility { get; set; }
}

/// <summary>Input for status-check / cancel-request tasks.</summary>
public sealed class ClaimTaskInput : NphiesMessageInput
{
    public string TaskId { get; set; } = string.Empty;
    public NphiesIdentifier Identifier { get; set; } = new(string.Empty, string.Empty);
    /// <summary>Identifier of the Claim being checked or cancelled.</summary>
    public NphiesIdentifier ClaimIdentifier { get; set; } = new(string.Empty, string.Empty);
    public OrganizationInput Provider { get; set; } = new() { Kind = NphiesOrganizationKind.Provider };
    public OrganizationInput Insurer { get; set; } = new() { Kind = NphiesOrganizationKind.Insurer };
    public string AuthoredOn { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public string Priority { get; set; } = "routine";
    /// <summary>For cancel: task-reason-code (WI, NP, TAS). Default WI.</summary>
    public string? Reason { get; set; }
    /// <summary>For cancel: task-code cancel or nullify.</summary>
    public string CancelCode { get; set; } = "cancel";
}

/// <summary>Input for a poll-request.</summary>
public sealed class PollRequestInput : NphiesMessageInput
{
    public string TaskId { get; set; } = string.Empty;
    public NphiesIdentifier Identifier { get; set; } = new(string.Empty, string.Empty);
    public bool IdentifierUseOfficial { get; set; } = true;
    public OrganizationInput Provider { get; set; } = new() { Kind = NphiesOrganizationKind.Provider };
    public string AuthoredOn { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public string Priority { get; set; } = "stat";
    /// <summary>Message event codes to include (e.g. claim-response, communication-request).</summary>
    public List<string> IncludeMessageTypes { get; set; } = new();
    public List<string> ExcludeMessageTypes { get; set; } = new();
    /// <summary>Max messages to return (1-100).</summary>
    public int? Count { get; set; }
    public NphiesPeriod? Period { get; set; }
}

public sealed class CommunicationPayloadInput
{
    public string? ContentString { get; set; }
    public AttachmentInput? ContentAttachment { get; set; }
}

/// <summary>Input for a communication message (solicited when <see cref="BasedOnCommunicationRequest"/> is set).</summary>
public sealed class CommunicationInput : NphiesMessageInput
{
    public string CommunicationId { get; set; } = string.Empty;
    public NphiesIdentifier Identifier { get; set; } = new(string.Empty, string.Empty);
    /// <summary>Identifier of the CommunicationRequest being answered (solicited communication).</summary>
    public NphiesIdentifier? BasedOnCommunicationRequest { get; set; }
    /// <summary>Identifier of the Claim the communication is about.</summary>
    public NphiesIdentifier AboutClaim { get; set; } = new(string.Empty, string.Empty);
    public string Status { get; set; } = "completed";
    /// <summary>communication-category code (alert, notification, reminder, instruction).</summary>
    public string Category { get; set; } = "instruction";
    public string Priority { get; set; } = "routine";
    public NphiesCoding? Reason { get; set; }
    public PatientInput Patient { get; set; } = new();
    public OrganizationInput Provider { get; set; } = new() { Kind = NphiesOrganizationKind.Provider };
    public OrganizationInput Insurer { get; set; } = new() { Kind = NphiesOrganizationKind.Insurer };
    public List<CommunicationPayloadInput> Payloads { get; set; } = new();
}

/// <summary>
/// Input for a payment-notice: the provider acknowledgement of a received PaymentReconciliation.
/// </summary>
public sealed class PaymentNoticeInput : NphiesMessageInput
{
    public string PaymentNoticeId { get; set; } = string.Empty;
    public NphiesIdentifier Identifier { get; set; } = new(string.Empty, string.Empty);
    /// <summary>Identifier of the PaymentReconciliation being acknowledged.</summary>
    public NphiesIdentifier PaymentIdentifier { get; set; } = new(string.Empty, string.Empty);
    /// <summary>MessageHeader.id of the payment-reconciliation message being acknowledged (MessageHeader.response.identifier).</summary>
    public string? ReconciliationMessageId { get; set; }
    public string Created { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public string? PaymentDate { get; set; }
    public NphiesMoney Amount { get; set; } = new(0);
    /// <summary>paymentstatus code (paid, cleared).</summary>
    public string PaymentStatus { get; set; } = "cleared";
    public OrganizationInput Provider { get; set; } = new() { Kind = NphiesOrganizationKind.Provider };
}

/// <summary>Input for a batch-request (up to 200 claims, all for the same insurer).</summary>
public sealed class BatchRequestInput : NphiesMessageInput
{
    public NphiesIdentifier BatchIdentifier { get; set; } = new(string.Empty, string.Empty);
    public NphiesPeriod BatchPeriod { get; set; } = new(null, null);
    public List<ClaimRequestInput> Claims { get; set; } = new();
    /// <summary>When true (default), batch extensions are set on each claim with batch-number = index + 1.</summary>
    public bool AssignBatchExtensions { get; set; } = true;
}
