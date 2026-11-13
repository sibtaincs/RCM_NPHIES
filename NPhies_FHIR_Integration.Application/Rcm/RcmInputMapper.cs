using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Models;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm;

/// <summary>Maps persisted RCM entities onto the messaging-core builder inputs.</summary>
public interface IRcmInputMapper
{
    /// <summary>Provider-assigned identifier system for a resource kind, e.g. <c>claim</c>, <c>authorization</c>, <c>task</c>.</summary>
    string IdentifierSystem(string kind);

    /// <summary>Loads a claim with every relation needed to build a claim / prior-auth message.</summary>
    Task<Claim?> LoadClaimGraphAsync(string claimId, CancellationToken ct = default);

    /// <summary>Ensures the claim has a provider identifier (system + value) and returns it.</summary>
    NphiesIdentifier EnsureClaimIdentifier(Claim claim);

    /// <summary>
    /// Fills the values the NPHIES claim profiles require (1..1) but that a user or HIS may leave empty:
    /// <c>Claim.extension:episode</c> and <c>Claim.item.extension:patientInvoice</c>. Defaults are derived from
    /// the claim number and written onto the entity, so a resubmission or follow-up cloned from this claim
    /// inherits the same episode.
    /// </summary>
    void EnsureClaimDefaults(Claim claim);

    Task<ClaimRequestInput> MapClaimAsync(Claim claim, CancellationToken ct = default);

    PatientInput MapPatient(Patient patient);
    OrganizationInput MapOrganization(Organization organization, NphiesOrganizationKind kind);
    CoverageInput MapCoverage(Coverage coverage, Patient patient);

    /// <summary>
    /// Loads the coverage subscriber (when not the beneficiary) and policyholder organization so the bundle carries
    /// resolvable references. Dangling ids on <paramref name="coverageInput"/> are repaired (subscriber falls back to the
    /// beneficiary; policyholder falls back to the subscriber patient).
    /// </summary>
    Task<(PatientInput? Subscriber, OrganizationInput? Policyholder)> LoadCoveragePartiesAsync(
        Coverage coverage, Patient patient, CoverageInput coverageInput, CancellationToken ct = default);
}

public sealed class RcmInputMapper : IRcmInputMapper
{
    private readonly ApplicationDbContext _db;
    private readonly Documents.IClaimDocumentStorage _documents;
    private readonly string _identifierBase;

    public RcmInputMapper(ApplicationDbContext db, IOptions<NphiesConfiguration> nphies, IOptions<RcmOptions> rcm, Documents.IClaimDocumentStorage documents)
    {
        _db = db;
        _documents = documents;
        var baseUrl = rcm.Value.IdentifierBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = nphies.Value.Sender.ProviderBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = "http://provider.com";
        _identifierBase = baseUrl.TrimEnd('/');
    }

    public string IdentifierSystem(string kind) => $"{_identifierBase}/{kind}";

    public Task<Claim?> LoadClaimGraphAsync(string claimId, CancellationToken ct = default) =>
        _db.Claims
            .Include(c => c.Patient)
            .Include(c => c.Coverage)
            .Include(c => c.Provider)
            .Include(c => c.Insurer)
            .Include(c => c.Practitioner)
            .Include(c => c.Encounter)
            .Include(c => c.ServiceLocation)
            .Include(c => c.Items).ThenInclude(i => i.Details)
            .Include(c => c.Diagnoses)
            .Include(c => c.CareTeam).ThenInclude(ct2 => ct2.Practitioner)
            .Include(c => c.SupportingInfo)
            .Include(c => c.RelatedClaims)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == claimId, ct);

    public NphiesIdentifier EnsureClaimIdentifier(Claim claim)
    {
        var isPreAuth = IsPreAuth(claim);
        if (string.IsNullOrWhiteSpace(claim.ClaimIdentifierSystem))
            claim.ClaimIdentifierSystem = IdentifierSystem(isPreAuth ? "authorization" : "claim");
        if (string.IsNullOrWhiteSpace(claim.ClaimIdentifierValue))
            claim.ClaimIdentifierValue = $"req_{claim.ClaimNumber}";
        return new NphiesIdentifier(claim.ClaimIdentifierSystem!, claim.ClaimIdentifierValue!);
    }

    public void EnsureClaimDefaults(Claim claim)
    {
        if (string.IsNullOrWhiteSpace(claim.EpisodeIdentifierValue))
        {
            claim.EpisodeIdentifierValue = DefaultEpisode(claim);
            claim.EpisodeIdentifierSystem = null;
        }

        foreach (var item in claim.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.PatientInvoiceValue)) continue;
            item.PatientInvoiceValue = DefaultPatientInvoice(claim);
            item.PatientInvoiceSystem = null;
        }
    }

    /// <summary>Default episode-of-care identifier: one episode per original claim (clones copy it).</summary>
    public static string DefaultEpisode(Claim claim) => $"ep_{claim.ClaimNumber}";

    /// <summary>Default patient invoice number: the claim number, shared by every line of the claim.</summary>
    public static string DefaultPatientInvoice(Claim claim) => $"inv_{claim.ClaimNumber}";

    public static bool IsPreAuth(Claim claim) =>
        claim.Use.Contains("preauth", StringComparison.OrdinalIgnoreCase);

    public async Task<ClaimRequestInput> MapClaimAsync(Claim claim, CancellationToken ct = default)
    {
        if (claim.Patient is null || claim.Coverage is null || claim.Provider is null || claim.Insurer is null)
            throw new InvalidOperationException($"Claim {claim.Id} is missing patient, coverage, provider or insurer.");

        var type = ParseClaimType(claim.ClaimType);
        var itemIds = claim.Items.Select(i => i.Id).ToList();
        var vision = itemIds.Count == 0 ? new List<VisionPrescription>()
            : await _db.VisionPrescriptions.AsNoTracking().Where(v => itemIds.Contains(v.ClaimItemId)).ToListAsync(ct);
        var oral = itemIds.Count == 0 ? new List<OralDetail>()
            : await _db.OralDetails.AsNoTracking().Where(o => itemIds.Contains(o.ClaimItemId)).ToListAsync(ct);

        var patient = MapPatient(claim.Patient);
        var input = new ClaimRequestInput
        {
            ClaimId = SafeId(claim.Id),
            Identifier = EnsureClaimIdentifier(claim),
            Type = type,
            SubType = ParseSubType(claim.ClaimSubType, type),
            Use = IsPreAuth(claim) ? NphiesClaimUse.PreAuthorization : NphiesClaimUse.Claim,
            Status = "active",
            Created = Date(claim.CreatedDate == default ? DateTime.UtcNow : claim.CreatedDate)!,
            Priority = string.IsNullOrWhiteSpace(claim.Priority) ? "normal" : claim.Priority.ToLowerInvariant(),
            PayeeType = string.IsNullOrWhiteSpace(claim.PayeeType) ? "provider" : claim.PayeeType!,
            BillablePeriod = Period(claim.BillablePeriodStart ?? claim.ServicedPeriodStart, claim.BillablePeriodEnd ?? claim.ServicedPeriodEnd),
            Patient = patient,
            Provider = MapOrganization(claim.Provider, NphiesOrganizationKind.Provider),
            Insurer = MapOrganization(claim.Insurer, NphiesOrganizationKind.Insurer),
            Coverage = MapCoverage(claim.Coverage, claim.Patient),
            EligibilityOfflineReference = claim.EligibilityOfflineReference,
            EligibilityOfflineDate = DateTimeIso(claim.EligibilityOfflineDate),
            AuthorizationOfflineDate = DateTimeIso(claim.AuthorizationOfflineDate),
            Transfer = claim.IsTransfer,
        };

        EnsureClaimDefaults(claim);
        input.Episode = new NphiesIdentifier(claim.EpisodeIdentifierSystem ?? IdentifierSystem("episode"), claim.EpisodeIdentifierValue!);

        // A prior-auth carries the granted ref in ClaimResponse.PreAuthRef; only claims reference prior auths.
        if (!IsPreAuth(claim) && !string.IsNullOrWhiteSpace(claim.PreAuthorizationRef))
            input.PreAuthRefs.AddRange(claim.PreAuthorizationRef!
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => new PreAuthRefInput(v)));

        if (!string.IsNullOrWhiteSpace(claim.BatchIdentifier) && claim.BatchNumber is int bn)
            input.Batch = new BatchInfoInput
            {
                BatchIdentifier = new NphiesIdentifier(IdentifierSystem("batch"), claim.BatchIdentifier!),
                BatchNumber = bn,
                BatchPeriod = Period(claim.BillablePeriodStart ?? claim.ServicedPeriodStart, claim.BillablePeriodEnd ?? claim.ServicedPeriodEnd)
                    ?? new NphiesPeriod(Date(claim.CreatedDate), Date(claim.CreatedDate)),
            };

        // Practitioners + care team
        var practitioners = new Dictionary<string, Practitioner>();
        foreach (var member in claim.CareTeam.Where(m => m.Practitioner != null))
            practitioners[member.PractitionerId] = member.Practitioner!;
        if (claim.Practitioner != null) practitioners.TryAdd(claim.Practitioner.Id, claim.Practitioner);
        input.Practitioners.AddRange(practitioners.Values.Select(MapPractitioner));

        if (claim.CareTeam.Count > 0)
        {
            input.CareTeam.AddRange(claim.CareTeam.OrderBy(m => m.Sequence).Select(m => new CareTeamInput
            {
                Sequence = m.Sequence,
                PractitionerId = SafeId(m.PractitionerId),
                Role = string.IsNullOrWhiteSpace(m.Role) ? "primary" : m.Role!,
                Qualification = m.Qualification ?? m.Practitioner?.Specialization ?? string.Empty,
            }));
        }
        else if (claim.Practitioner != null)
        {
            input.CareTeam.Add(new CareTeamInput
            {
                Sequence = 1,
                PractitionerId = SafeId(claim.Practitioner.Id),
                Role = "primary",
                Qualification = claim.Practitioner.Specialization ?? string.Empty,
            });
        }

        input.Diagnoses.AddRange(claim.Diagnoses.OrderBy(d => d.Sequence).Select(d => new DiagnosisInput
        {
            Sequence = d.Sequence,
            Code = d.DiagnosisCode,
            Display = d.DiagnosisDisplay,
            System = string.IsNullOrWhiteSpace(d.DiagnosisSystem) ? NphiesCodeSystems.Icd10Am : d.DiagnosisSystem!,
            Type = string.IsNullOrWhiteSpace(d.DiagnosisType) ? "principal" : d.DiagnosisType!,
            OnAdmission = d.OnAdmissionCode,
            ConditionOnset = !string.IsNullOrWhiteSpace(d.ConditionOnsetCode) ? d.ConditionOnsetCode
                : type == NphiesClaimType.Institutional && !IsPreAuth(claim) ? "NR" : null,
        }));

        var documents = await LoadAttachedDocumentsAsync(claim, ct);
        input.SupportingInfo.AddRange(claim.SupportingInfo.OrderBy(s => s.Sequence).Select(s => MapSupportingInfo(s, documents)));

        foreach (var item in claim.Items.OrderBy(i => i.Sequence))
            input.Items.Add(MapItem(claim, item, type, oral.FirstOrDefault(o => o.ClaimItemId == item.Id)));

        input.Related.AddRange(claim.RelatedClaims
            .Where(r => !string.IsNullOrWhiteSpace(r.RelatedClaimIdentifierValue))
            .Select(r => new RelatedClaimInput
            {
                ClaimIdentifier = new NphiesIdentifier(r.RelatedClaimIdentifierSystem ?? claim.ClaimIdentifierSystem!, r.RelatedClaimIdentifierValue!),
                Relationship = string.IsNullOrWhiteSpace(r.Relationship) ? "prior" : r.Relationship!,
            }));

        if (claim.Encounter != null)
            input.Encounter = MapEncounter(claim.Encounter);
        else if (type is NphiesClaimType.Institutional or NphiesClaimType.Professional or NphiesClaimType.Oral)
            input.Encounter = SynthesizeEncounter(claim, input);

        if (type == NphiesClaimType.Vision && vision.Count > 0)
        {
            var prescriber = claim.Practitioner ?? practitioners.Values.FirstOrDefault();
            input.VisionPrescription = new VisionPrescriptionInput
            {
                Id = SafeId("vp-" + claim.Id),
                Identifier = new NphiesIdentifier(IdentifierSystem("visionprescription"), claim.ClaimNumber),
                Created = Date(claim.CreatedDate)!,
                DateWritten = Date(claim.ServicedPeriodStart ?? claim.CreatedDate)!,
                PrescriberLicense = prescriber?.PractitionerLicenseNumber ?? prescriber?.LicenseNumber ?? string.Empty,
                Lenses = vision.Select(v => new LensSpecificationInput
                {
                    Product = string.IsNullOrWhiteSpace(v.Product) ? "lens" : v.Product!,
                    Eye = string.IsNullOrWhiteSpace(v.Eye) ? "right" : v.Eye!,
                    Sphere = v.Sphere,
                    Cylinder = v.Cylinder,
                    Axis = v.Axis,
                    PrismAmount = v.Prism,
                    PrismBase = v.PrismBase,
                    Add = v.Add,
                    Power = v.Power,
                }).ToList(),
            };
        }

        var (subscriber, policyholder) = await LoadCoveragePartiesAsync(claim.Coverage, claim.Patient, input.Coverage, ct);
        input.Subscriber = subscriber;
        input.Policyholder = policyholder;

        var total = claim.Total > 0 ? claim.Total : input.Items.Sum(i => i.Net?.Value ?? 0);
        input.Total = new NphiesMoney(total, string.IsNullOrWhiteSpace(claim.TotalCurrency) ? "SAR" : claim.TotalCurrency);
        return input;
    }

    private ClaimItemInput MapItem(Claim claim, ClaimItem item, NphiesClaimType type, OralDetail? oral)
    {
        var product = new NphiesConcept();
        product.Codings.Add(new NphiesCoding(
            string.IsNullOrWhiteSpace(item.ProductOrServiceSystem) ? DefaultProductSystem(type, claim.ClaimSubType) : item.ProductOrServiceSystem!,
            item.ProductOrServiceCode,
            item.ProductOrServiceDisplay));
        // Shadow billing: the provider's internal code travels as a second coding.
        if (!string.IsNullOrWhiteSpace(item.AltProductOrServiceCode))
            product.Codings.Add(new NphiesCoding(item.AltProductOrServiceSystem ?? IdentifierSystem("CodeSystem/services"), item.AltProductOrServiceCode!));

        var quantity = item.Quantity is > 0 ? item.Quantity.Value : 1m;
        var unitPrice = item.UnitPrice ?? 0m;
        var factor = item.Factor;
        var net = item.Net ?? Math.Round(quantity * unitPrice * (factor ?? 1m) + (item.Tax ?? 0m), 2);

        var result = new ClaimItemInput
        {
            Sequence = item.Sequence,
            CareTeamSequence = item.CareTeamSequence is int cs ? new List<int> { cs } : (claim.CareTeam.Count > 0 || claim.Practitioner != null ? new List<int> { 1 } : new List<int>()),
            DiagnosisSequence = ParseSequences(item.DiagnosisSequence, claim.Diagnoses.Count > 0 ? claim.Diagnoses.Min(d => d.Sequence) : (int?)null),
            InformationSequence = ParseSequences(item.InformationSequence, null),
            ProductOrService = product,
            Quantity = quantity,
            UnitPrice = new NphiesMoney(unitPrice),
            Factor = factor,
            Net = new NphiesMoney(net),
            Tax = new NphiesMoney(item.Tax ?? 0m),
            PatientShare = new NphiesMoney(item.PatientShare ?? 0m),
            PayerShare = new NphiesMoney(Math.Max(0, net - (item.PatientShare ?? 0m))),
            Package = item.IsPackage,
            Maternity = type is NphiesClaimType.Institutional or NphiesClaimType.Professional or NphiesClaimType.Pharmacy ? item.IsMaternity : null,
        };

        if (item.ServicedPeriodStart.HasValue)
            result.ServicedPeriod = Period(item.ServicedPeriodStart, item.ServicedPeriodEnd);
        else
            result.ServicedDate = Date(item.ServicedDate ?? claim.ServicedPeriodStart ?? claim.CreatedDate);

        // EnsureClaimDefaults (called by MapClaimAsync before the items are mapped) guarantees a value.
        result.PatientInvoice = new NphiesIdentifier(item.PatientInvoiceSystem ?? IdentifierSystem("patientInvoice"), item.PatientInvoiceValue!);

        var bodySite = item.BodySiteCode ?? oral?.ToothCode;
        if (!string.IsNullOrWhiteSpace(bodySite))
        {
            var bsSystem = item.BodySiteSystem ?? oral?.ToothCodeSystem
                ?? (type == NphiesClaimType.Oral ? NphiesCodeSystems.FdiOralRegion : NphiesCodeSystems.BodySite);
            result.BodySite = new NphiesConcept { Codings = { new NphiesCoding(bsSystem, bodySite!) } };
        }
        var subSite = item.SubSiteCode ?? oral?.ToothSurface;
        if (!string.IsNullOrWhiteSpace(subSite))
        {
            var ssSystem = item.SubSiteSystem ?? oral?.ToothSurfaceSystem
                ?? (type == NphiesClaimType.Oral ? NphiesCodeSystems.FdiToothSurface : NphiesCodeSystems.SubSite);
            foreach (var code in subSite!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                result.SubSite.Add(new NphiesCoding(ssSystem, code));
        }

        foreach (var detail in item.Details.OrderBy(d => d.Sequence))
        {
            var dp = new NphiesConcept();
            dp.Codings.Add(new NphiesCoding(detail.ProductOrServiceSystem ?? DefaultProductSystem(type, claim.ClaimSubType), detail.ProductOrServiceCode, detail.ProductOrServiceDisplay));
            if (!string.IsNullOrWhiteSpace(detail.AltProductOrServiceCode))
                dp.Codings.Add(new NphiesCoding(detail.AltProductOrServiceSystem ?? IdentifierSystem("CodeSystem/services"), detail.AltProductOrServiceCode!));
            var dq = detail.Quantity is > 0 ? detail.Quantity.Value : 1m;
            result.Details.Add(new ItemDetailInput
            {
                Sequence = detail.Sequence,
                ProductOrService = dp,
                Quantity = dq,
                UnitPrice = detail.UnitPrice is decimal up ? new NphiesMoney(up) : null,
                Net = new NphiesMoney(detail.Net ?? dq * (detail.UnitPrice ?? 0m)),
            });
        }

        return result;
    }

    /// <summary>Stored documents referenced by the claim's supporting info, with their content as base64.</summary>
    private async Task<Dictionary<string, (ClaimDocument Doc, string Base64)>> LoadAttachedDocumentsAsync(Claim claim, CancellationToken ct)
    {
        var result = new Dictionary<string, (ClaimDocument, string)>();
        var ids = claim.SupportingInfo.Where(s => !string.IsNullOrWhiteSpace(s.DocumentId)).Select(s => s.DocumentId!).Distinct().ToList();
        if (ids.Count == 0) return result;
        var docs = await _db.ClaimDocuments.AsNoTracking().Where(d => ids.Contains(d.Id)).ToListAsync(ct);
        foreach (var id in ids)
        {
            var doc = docs.FirstOrDefault(d => d.Id == id)
                      ?? throw new InvalidOperationException($"Claim {claim.ClaimNumber}: attached document '{id}' no longer exists.");
            var data = await _documents.ReadAsync(doc.StorageProvider, doc.StorageKey, ct)
                       ?? throw new InvalidOperationException($"Claim {claim.ClaimNumber}: content of document '{doc.Title ?? doc.FileName}' is missing from the {doc.StorageProvider} store.");
            result[id] = (doc, Convert.ToBase64String(data));
        }
        return result;
    }

    private static SupportingInfoInput MapSupportingInfo(ClaimSupportingInfo s, IReadOnlyDictionary<string, (ClaimDocument Doc, string Base64)> documents)
    {
        var info = new SupportingInfoInput
        {
            Sequence = s.Sequence,
            Category = s.Category,
            ValueString = s.StringValue,
            ValueBoolean = s.BooleanValue,
            ValueQuantity = s.QuantityValue,
            ValueQuantityUnit = s.QuantityUnit,
        };
        if (!string.IsNullOrWhiteSpace(s.CodeValue))
            info.Code = new NphiesConcept { Codings = { new NphiesCoding(s.CodeSystem ?? NphiesCodeSystems.ClaimInformationCategory, s.CodeValue!) } };
        if (s.DateValue.HasValue)
        {
            // Measurement-type slices (vitals, lab-test, icu/ventilation hours, ...) require timingPeriod.
            if (PeriodTimedCategories.Contains(s.Category))
                info.TimingPeriod = new NphiesPeriod(DateTimeIso(s.DateValue), DateTimeIso(s.DateValue));
            else
                info.TimingDate = Date(s.DateValue);
        }
        if (s.DocumentId != null && documents.TryGetValue(s.DocumentId, out var stored))
            info.ValueAttachment = new AttachmentInput
            {
                ContentType = stored.Doc.ContentType,
                Title = s.AttachmentTitle ?? stored.Doc.Title ?? stored.Doc.FileName,
                Data = stored.Base64,
                Creation = Date(stored.Doc.ServiceDate ?? stored.Doc.CreatedAt),
            };
        else if (!string.IsNullOrWhiteSpace(s.AttachmentData) || !string.IsNullOrWhiteSpace(s.AttachmentUrl))
            info.ValueAttachment = new AttachmentInput
            {
                ContentType = string.IsNullOrWhiteSpace(s.AttachmentContentType) ? "application/pdf" : s.AttachmentContentType!,
                Title = s.AttachmentTitle,
                Data = string.IsNullOrWhiteSpace(s.AttachmentData) ? null : s.AttachmentData,
                Url = string.IsNullOrWhiteSpace(s.AttachmentUrl) ? null : s.AttachmentUrl,
                Creation = Date(s.DateValue ?? s.CreatedAt),
            };
        return info;
    }

    public PatientInput MapPatient(Patient p)
    {
        var name = !string.IsNullOrWhiteSpace(p.FullName) ? p.FullName
            : $"{p.FirstName} {p.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(name)) name = p.FullNameArabic;

        var input = new PatientInput
        {
            Id = SafeId(p.Id),
            IdentifierKind = ParseIdentifierKind(p.NationalIdType, p.NationalId),
            IdentifierValue = string.IsNullOrWhiteSpace(p.NationalId) ? p.MRN : p.NationalId,
            IdentifierSystem = string.IsNullOrWhiteSpace(p.NationalIdSystem) || !p.NationalIdSystem!.StartsWith("http") ? null : p.NationalIdSystem,
            IdentifierCountry = string.IsNullOrWhiteSpace(p.NationalIdCountry) ? null : p.NationalIdCountry.Trim().ToUpperInvariant(),
            NameText = name,
            Family = string.IsNullOrWhiteSpace(p.LastName) ? p.LastNameArabic : p.LastName,
            Phone = string.IsNullOrWhiteSpace(p.Phone) ? null : p.Phone,
            Gender = NormalizeGender(p.Gender),
            BirthDate = Date(p.DateOfBirth)!,
            MaritalStatus = string.IsNullOrWhiteSpace(p.MaritalStatus) ? "UNK" : p.MaritalStatus,
            Occupation = string.IsNullOrWhiteSpace(p.Occupation) ? "unknown" : p.Occupation,
            Deceased = p.Deceased ? true : null,
        };
        var given = string.IsNullOrWhiteSpace(p.FirstName) ? p.FirstNameArabic : p.FirstName;
        if (!string.IsNullOrWhiteSpace(given)) input.Given.Add(given!);
        return input;
    }

    public OrganizationInput MapOrganization(Organization o, NphiesOrganizationKind kind)
    {
        var license = kind == NphiesOrganizationKind.Insurer
            ? FirstNonEmpty(o.NphiesPayerId, o.LicenseNumber)
            : FirstNonEmpty(o.NphiesProviderId, o.LicenseNumber);
        return new OrganizationInput
        {
            Id = SafeId(o.Id),
            License = license,
            Name = o.OrganizationName,
            Kind = kind,
            IdentifierSystem = !string.IsNullOrWhiteSpace(o.LicenseSystem) && o.LicenseSystem.StartsWith("http") ? o.LicenseSystem : null,
            ProviderType = kind == NphiesOrganizationKind.Provider && !string.IsNullOrWhiteSpace(o.ProviderTypeCode)
                ? new NphiesCoding(o.ProviderTypeSystem ?? NphiesCodeSystems.ProviderType, o.ProviderTypeCode!)
                : null,
            // Insurer identifier slice: TPAs use NIIP, payors NII.
            InsurerIdentifierType = string.Equals(o.OrganizationType, "tpa", StringComparison.OrdinalIgnoreCase) ? "NIIP" : "NII",
        };
    }

    public CoverageInput MapCoverage(Coverage c, Patient patient)
    {
        var input = new CoverageInput
        {
            Id = SafeId(c.Id),
            MemberId = new NphiesIdentifier(
                string.IsNullOrWhiteSpace(c.CoverageIdentifierSystem) ? "http://payer.com/memberid" : c.CoverageIdentifierSystem!,
                FirstNonEmpty(c.CoverageIdentifierValue, c.MemberID, c.PolicyNumber)),
            Status = string.IsNullOrWhiteSpace(c.Status) ? "active" : c.Status.ToLowerInvariant(),
            Type = string.IsNullOrWhiteSpace(c.CoverageType) ? "EHCPOL" : c.CoverageType,
            Relationship = string.IsNullOrWhiteSpace(c.SubscriberRelationship) ? "self" : c.SubscriberRelationship.ToLowerInvariant(),
            SubscriberPatientId = SafeId(c.SubscriberPatientId ?? patient.Id),
            PolicyHolderOrganizationId = string.IsNullOrWhiteSpace(c.PolicyHolderOrganizationId) ? null : SafeId(c.PolicyHolderOrganizationId!),
            Period = c.CoverageStartDate == default ? null : Period(c.CoverageStartDate, c.CoverageEndDate == default ? null : c.CoverageEndDate),
        };
        // Coverage.policyHolder is 1..1 in the IG: without a policyholder organization the subscriber holds the policy.
        if (input.PolicyHolderOrganizationId == null) input.PolicyHolderPatientId = input.SubscriberPatientId;
        if (!string.IsNullOrWhiteSpace(c.CoverageClassValue))
            input.Classes.Add(new CoverageClassInput { Type = c.CoverageClassType ?? "group", Value = c.CoverageClassValue!, Name = c.PlanCode });
        else if (!string.IsNullOrWhiteSpace(c.PlanCode))
            input.Classes.Add(new CoverageClassInput { Type = "plan", Value = c.PlanCode!, Name = c.CoverageClass });
        return input;
    }

    public async Task<(PatientInput? Subscriber, OrganizationInput? Policyholder)> LoadCoveragePartiesAsync(
        Coverage coverage, Patient patient, CoverageInput coverageInput, CancellationToken ct = default)
    {
        PatientInput? subscriber = null;
        if (!string.IsNullOrWhiteSpace(coverage.SubscriberPatientId) && coverage.SubscriberPatientId != patient.Id)
        {
            var subscriberEntity = coverage.SubscriberPatient
                ?? await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == coverage.SubscriberPatientId, ct);
            if (subscriberEntity != null) subscriber = MapPatient(subscriberEntity);
            else coverageInput.SubscriberPatientId = SafeId(patient.Id);
        }

        OrganizationInput? policyholder = null;
        if (!string.IsNullOrWhiteSpace(coverage.PolicyHolderOrganizationId))
        {
            var org = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == coverage.PolicyHolderOrganizationId, ct);
            if (org != null) policyholder = MapOrganization(org, NphiesOrganizationKind.Policyholder);
            else coverageInput.PolicyHolderOrganizationId = null;
        }
        if (coverageInput.PolicyHolderOrganizationId == null)
            coverageInput.PolicyHolderPatientId = coverageInput.SubscriberPatientId;
        return (subscriber, policyholder);
    }

    private static PractitionerInput MapPractitioner(Practitioner p)
    {
        var input = new PractitionerInput
        {
            Id = SafeId(p.Id),
            License = FirstNonEmpty(p.PractitionerLicenseNumber, p.LicenseNumber),
            IdentifierType = "MD",
            NameText = $"{p.Title} {p.FirstName} {p.LastName}".Trim(),
            Family = p.LastName,
        };
        if (!string.IsNullOrWhiteSpace(p.FirstName)) input.Given.Add(p.FirstName);
        return input;
    }

    private EncounterInput MapEncounter(Encounter e)
    {
        var classCode = string.IsNullOrWhiteSpace(e.Class) ? "AMB" : e.Class.ToUpperInvariant();
        var input = new EncounterInput
        {
            Id = SafeId(e.Id),
            Status = string.IsNullOrWhiteSpace(e.Status) ? "finished" : e.Status.ToLowerInvariant(),
            Class = new NphiesCoding(NphiesCodeSystems.V3ActCode, classCode),
            Period = Period(e.PeriodStart, e.PeriodEnd),
            // Encounter.identifier is 1..1 in the IG.
            Identifier = new NphiesIdentifier(e.IdentifierSystem ?? IdentifierSystem("encounter"),
                string.IsNullOrWhiteSpace(e.IdentifierValue) ? SafeId(e.Id) : e.IdentifierValue!),
        };
        if (!string.IsNullOrWhiteSpace(e.ServiceEventType))
            input.ServiceEventType = new NphiesCoding(e.ServiceEventTypeSystem ?? NphiesCodeSystems.ServiceEventType, e.ServiceEventType!);
        return input;
    }

    /// <summary>claim-information-category codes whose IG slices require timing[x]:timingPeriod.</summary>
    private static readonly HashSet<string> PeriodTimedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "birth-weight", "employmentImpacted", "icu-hours", "lab-test", "oxygen-saturation", "pulse", "respiratory-rate",
        "temperature", "ventilation-hours", "vital-sign-diastolic", "vital-sign-height", "vital-sign-systolic", "vital-sign-weight",
    };

    /// <summary>
    /// Institutional, professional and oral claims require the encounter extension. When the claim has no recorded
    /// encounter, one is derived from the claim sub-type and serviced period.
    /// </summary>
    private EncounterInput SynthesizeEncounter(Claim claim, ClaimRequestInput input)
    {
        var (classCode, classDisplay) = input.SubType switch
        {
            NphiesClaimSubType.Ip => ("IMP", "inpatient encounter"),
            NphiesClaimSubType.Emr => ("EMER", "emergency"),
            _ => ("AMB", "ambulatory"),
        };
        var start = claim.ServicedPeriodStart ?? claim.BillablePeriodStart ?? claim.CreatedDate;
        var end = claim.ServicedPeriodEnd ?? claim.BillablePeriodEnd;
        var id = SafeId("enc-" + claim.Id);
        return new EncounterInput
        {
            Id = id,
            Identifier = new NphiesIdentifier(IdentifierSystem("encounter"), "enc_" + claim.ClaimNumber),
            Status = IsPreAuth(claim) ? "planned" : "finished",
            Class = new NphiesCoding(NphiesCodeSystems.V3ActCode, classCode, classDisplay),
            Period = new NphiesPeriod(DateTimeIso(start), end.HasValue ? DateTimeIso(end) : null),
        };
    }

    // ===== helpers =====

    public static NphiesClaimType ParseClaimType(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "institutional" => NphiesClaimType.Institutional,
        "oral" or "dental" => NphiesClaimType.Oral,
        "pharmacy" => NphiesClaimType.Pharmacy,
        "vision" => NphiesClaimType.Vision,
        _ => NphiesClaimType.Professional,
    };

    public static NphiesClaimSubType ParseSubType(string? value, NphiesClaimType type) => (value ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "ip" or "inpatient" => NphiesClaimSubType.Ip,
        "emr" or "emergency" => NphiesClaimSubType.Emr,
        "op" or "outpatient" => NphiesClaimSubType.Op,
        _ => type == NphiesClaimType.Institutional ? NphiesClaimSubType.Ip : NphiesClaimSubType.Op,
    };

    public static string DefaultProductSystem(NphiesClaimType type, string? subType) => type switch
    {
        NphiesClaimType.Pharmacy => NphiesCodeSystems.MedicationCodes,
        NphiesClaimType.Oral => string.Equals(subType, "ip", StringComparison.OrdinalIgnoreCase) ? NphiesCodeSystems.OralHealthIp : NphiesCodeSystems.OralHealthOp,
        _ => NphiesCodeSystems.Services,
    };

    public static PatientIdentifierKind ParseIdentifierKind(string? type, string? value)
    {
        switch ((type ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "PRC": case "IQAMA": case "RESIDENT": return PatientIdentifierKind.Iqama;
            case "PPN": case "PASSPORT": return PatientIdentifierKind.Passport;
            case "BN": case "BORDER": case "BORDERNUMBER": return PatientIdentifierKind.BorderNumber;
            case "DP": case "DISPLACED": return PatientIdentifierKind.DisplacedPerson;
            case "MR": case "MRN": return PatientIdentifierKind.MedicalRecord;
            case "NI": case "NATIONALID": case "NATIONAL": return PatientIdentifierKind.NationalId;
        }
        // KSA convention: national ids start with 1, iqamas with 2.
        if (!string.IsNullOrEmpty(value) && value.Length == 10 && value[0] == '2') return PatientIdentifierKind.Iqama;
        return PatientIdentifierKind.NationalId;
    }

    private static string NormalizeGender(string? gender) => (gender ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "m" or "male" => "male",
        "f" or "female" => "female",
        "o" or "other" => "other",
        _ => "unknown",
    };

    private static List<int> ParseSequences(string? csv, int? fallback)
    {
        var list = (csv ?? string.Empty)
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var v) ? v : (int?)null)
            .Where(v => v.HasValue).Select(v => v!.Value).Distinct().ToList();
        if (list.Count == 0 && fallback.HasValue) list.Add(fallback.Value);
        return list;
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    /// <summary>FHIR ids allow [A-Za-z0-9-.]{1,64}.</summary>
    public static string SafeId(string id)
    {
        var chars = id.Where(c => char.IsLetterOrDigit(c) || c is '-' or '.').ToArray();
        var s = new string(chars);
        return s.Length > 64 ? s[..64] : (s.Length == 0 ? Guid.NewGuid().ToString("N") : s);
    }

    public static string? Date(DateTime? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? DateTimeIso(DateTime? value) => value?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    private static NphiesPeriod? Period(DateTime? start, DateTime? end) =>
        start is null && end is null ? null : new NphiesPeriod(DateTimeIso(start), DateTimeIso(end));
}
