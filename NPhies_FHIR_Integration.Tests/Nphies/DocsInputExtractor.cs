using Hl7.Fhir.Model;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Models;
using FhirTask = Hl7.Fhir.Model.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies;

/// <summary>
/// Reverse-maps an NPHIES docs example bundle into builder inputs so the builder can be checked against
/// the official examples (the builder must regenerate the same structure from the same data).
/// </summary>
internal static class DocsInputExtractor
{
    public static T Envelope<T>(Bundle bundle, T input) where T : NphiesMessageInput
    {
        var headerEntry = bundle.Entry[0];
        var header = (MessageHeader)headerEntry.Resource;
        var focusUrl = header.Focus.First().Reference;
        input.MessageId = header.Id;
        input.BundleId = bundle.Id;
        input.Timestamp = bundle.TimestampElement?.ToString();
        input.SourceEndpoint = header.Source?.Endpoint;
        input.MessageHeaderFullUrlAsUrl = !headerEntry.FullUrl.StartsWith("urn:", StringComparison.Ordinal);
        input.FullUrlBase = BaseOf(focusUrl);
        return input;
    }

    public static ClaimRequestInput Claim(Bundle bundle)
    {
        var claim = Single<Claim>(bundle);
        var input = Envelope(bundle, new ClaimRequestInput());

        input.ClaimId = claim.Id;
        input.Identifier = Id(claim.Identifier.First());
        input.Type = Enum.Parse<NphiesClaimType>(Code(claim.Type)!, ignoreCase: true);
        input.SubType = claim.SubType == null ? NphiesClaimSubType.None : Enum.Parse<NphiesClaimSubType>(Code(claim.SubType)!, true);
        input.Use = claim.Use == ClaimUseCode.Preauthorization ? NphiesClaimUse.PreAuthorization : NphiesClaimUse.Claim;
        input.Status = Literal(claim.StatusElement)!;
        input.Created = claim.Created;
        input.Priority = Code(claim.Priority)!;
        input.PayeeType = Code(claim.Payee?.Type) ?? "provider";
        input.BillablePeriod = Period(claim.BillablePeriod);
        input.Total = Money(claim.Total);

        input.Provider = Organization(Resolve<Organization>(bundle, claim.Provider), NphiesOrganizationKind.Provider);
        input.Insurer = Organization(Resolve<Organization>(bundle, claim.Insurer), NphiesOrganizationKind.Insurer);
        var patient = Resolve<Patient>(bundle, claim.Patient);
        input.Patient = Patient(patient);

        var insurance = claim.Insurance.First();
        var coverage = Resolve<Coverage>(bundle, insurance.Coverage);
        input.Coverage = Coverage(coverage, patient.Id);
        if (coverage.Subscriber != null && IdOf(coverage.Subscriber.Reference) != patient.Id)
        {
            var subscriber = TryResolve<Patient>(bundle, coverage.Subscriber);
            if (subscriber != null) input.Subscriber = Patient(subscriber);
        }

        if (coverage.PolicyHolder?.Reference?.Contains("Organization/") == true)
        {
            var holder = TryResolve<Organization>(bundle, coverage.PolicyHolder);
            if (holder != null) input.Policyholder = Organization(holder, NphiesOrganizationKind.Policyholder);
        }

        input.PreAuthRefs = insurance.PreAuthRef.Select(r => new PreAuthRefInput(r)).ToList();
        input.Practitioners = bundle.Entry.Select(e => e.Resource).OfType<Practitioner>().Select(Practitioner).ToList();
        input.PractitionerRoles = bundle.Entry.Select(e => e.Resource).OfType<PractitionerRole>().Select(Role).ToList();

        input.EligibilityOfflineReference = (Ext(claim, NphiesExtensions.EligibilityOfflineReference) as FhirString)?.Value;
        input.EligibilityOfflineDate = (Ext(claim, NphiesExtensions.EligibilityOfflineDate) as FhirDateTime)?.Value;
        input.AuthorizationOfflineDate = (Ext(claim, NphiesExtensions.AuthorizationOfflineDate) as FhirDateTime)?.Value;
        if (Ext(claim, NphiesExtensions.Episode) is Identifier episode) input.Episode = Id(episode);
        if (Ext(claim, NphiesExtensions.Transfer) is FhirBoolean transfer) input.Transfer = transfer.Value;
        if (Ext(claim, NphiesExtensions.Encounter) is ResourceReference encounterRef)
            input.Encounter = Encounter(Resolve<Encounter>(bundle, encounterRef));
        if (Ext(claim, NphiesExtensions.BatchIdentifier) is Identifier batchId)
        {
            input.Batch = new BatchInfoInput
            {
                BatchIdentifier = Id(batchId),
                BatchNumber = (Ext(claim, NphiesExtensions.BatchNumber) as PositiveInt)?.Value ?? 1,
                BatchPeriod = Period(Ext(claim, NphiesExtensions.BatchPeriod) as Period) ?? new NphiesPeriod(null, null),
            };
        }

        if (claim.Prescription != null)
            input.VisionPrescription = VisionPrescription(Resolve<VisionPrescription>(bundle, claim.Prescription));

        input.Related = claim.Related.Select(r => new RelatedClaimInput
        {
            ClaimIdentifier = Id(r.Claim.Identifier),
            Relationship = Code(r.Relationship)!,
            IncludeType = r.Claim.Type != null,
        }).ToList();

        input.CareTeam = claim.CareTeam.Select(c => new CareTeamInput
        {
            Sequence = c.Sequence ?? 1,
            PractitionerId = IdOf(c.Provider.Reference)!,
            UsePractitionerRole = c.Provider.Reference?.Contains("/PractitionerRole/") == true,
            Role = Code(c.Role)!,
            Qualification = Code(c.Qualification)!,
        }).ToList();

        input.Diagnoses = claim.Diagnosis.Select(d =>
        {
            var concept = (CodeableConcept)d.Diagnosis;
            var coding = concept.Coding.First();
            return new DiagnosisInput
            {
                Sequence = d.Sequence ?? 1,
                System = coding.System,
                Code = coding.Code,
                Display = coding.Display,
                Type = Code(d.Type.First())!,
                OnAdmission = Code(d.OnAdmission),
                ConditionOnset = Code(d.GetExtensionValue<CodeableConcept>(NphiesExtensions.ConditionOnset)),
            };
        }).ToList();

        input.SupportingInfo = claim.SupportingInfo.Select(SupportingInfo).ToList();
        input.Items = claim.Item.Select(Item).ToList();
        return input;
    }

    public static EligibilityRequestInput Eligibility(Bundle bundle)
    {
        var request = Single<CoverageEligibilityRequest>(bundle);
        var input = Envelope(bundle, new EligibilityRequestInput());
        input.RequestId = request.Id;
        input.Identifier = Id(request.Identifier.First());
        input.Purposes = request.PurposeElement.Select(p => p.ObjectValue as string).OfType<string>().ToList();
        input.Status = Literal(request.StatusElement)!;
        input.Created = request.Created;
        input.Priority = Code(request.Priority) ?? "normal";
        input.ServicedDate = (request.Serviced as Date)?.Value;
        input.ServicedPeriod = Period(request.Serviced as Period);
        var patient = Resolve<Patient>(bundle, request.Patient);
        input.Patient = Patient(patient);
        input.Provider = Organization(Resolve<Organization>(bundle, request.Provider), NphiesOrganizationKind.Provider);
        input.Insurer = Organization(Resolve<Organization>(bundle, request.Insurer), NphiesOrganizationKind.Insurer);
        if (request.Facility != null) input.Facility = Location(Resolve<Location>(bundle, request.Facility));

        var insurance = request.Insurance.FirstOrDefault();
        if (insurance != null)
        {
            var coverage = Resolve<Coverage>(bundle, insurance.Coverage);
            input.Coverage = Coverage(coverage, patient.Id);
            input.CoverageFocal = insurance.Focal;
            if (coverage.Subscriber != null && IdOf(coverage.Subscriber.Reference) != patient.Id)
            {
                var subscriber = TryResolve<Patient>(bundle, coverage.Subscriber);
                if (subscriber != null) input.Subscriber = Patient(subscriber);
            }

            if (coverage.PolicyHolder?.Reference?.Contains("Organization/") == true)
            {
                var holder = TryResolve<Organization>(bundle, coverage.PolicyHolder);
                if (holder != null) input.Policyholder = Organization(holder, NphiesOrganizationKind.Policyholder);
            }
        }

        return input;
    }

    public static ClaimTaskInput ClaimTask(Bundle bundle)
    {
        var task = Single<FhirTask>(bundle);
        var input = Envelope(bundle, new ClaimTaskInput());
        input.TaskId = task.Id;
        input.Identifier = Id(task.Identifier.First());
        input.ClaimIdentifier = Id(task.Focus.Identifier);
        input.Provider = Organization(Resolve<Organization>(bundle, task.Requester), NphiesOrganizationKind.Provider);
        input.Insurer = Organization(Resolve<Organization>(bundle, task.Owner), NphiesOrganizationKind.Insurer);
        input.AuthoredOn = task.AuthoredOn;
        input.Priority = Literal(task.PriorityElement) ?? "routine";
        input.Reason = Code(task.ReasonCode);
        input.CancelCode = Code(task.Code) ?? "cancel";
        return input;
    }

    public static PollRequestInput Poll(Bundle bundle)
    {
        var task = Single<FhirTask>(bundle);
        var input = Envelope(bundle, new PollRequestInput());
        input.TaskId = task.Id;
        var identifier = task.Identifier.First();
        input.Identifier = Id(identifier);
        input.IdentifierUseOfficial = identifier.Use == Identifier.IdentifierUse.Official;
        input.Provider = Organization(bundle.Entry.Select(e => e.Resource).OfType<Organization>().First(), NphiesOrganizationKind.Provider);
        input.AuthoredOn = task.AuthoredOn;
        input.Priority = Literal(task.PriorityElement) ?? "stat";
        foreach (var parameter in task.Input)
        {
            switch (Code(parameter.Type))
            {
                case "include-message-type": input.IncludeMessageTypes.Add(((Code)parameter.Value).Value); break;
                case "exclude-message-type": input.ExcludeMessageTypes.Add(((Code)parameter.Value).Value); break;
                case "count": input.Count = ((PositiveInt)parameter.Value).Value; break;
                case "period": input.Period = Period((Period)parameter.Value); break;
            }
        }

        return input;
    }

    public static CommunicationInput Communication(Bundle bundle)
    {
        var communication = Single<Communication>(bundle);
        var input = Envelope(bundle, new CommunicationInput());
        input.CommunicationId = communication.Id;
        input.Identifier = Id(communication.Identifier.First());
        var basedOn = communication.BasedOn.FirstOrDefault();
        if (basedOn?.Identifier != null) input.BasedOnCommunicationRequest = Id(basedOn.Identifier);
        input.AboutClaim = Id(communication.About.First().Identifier);
        input.Status = Literal(communication.StatusElement)!;
        input.Category = Code(communication.Category.First())!;
        input.Priority = Literal(communication.PriorityElement) ?? "routine";
        input.Reason = Coding(communication.ReasonCode.FirstOrDefault());
        input.Patient = Patient(Resolve<Patient>(bundle, communication.Subject));
        input.Provider = Organization(Resolve<Organization>(bundle, communication.Sender), NphiesOrganizationKind.Provider);
        input.Insurer = Organization(Resolve<Organization>(bundle, communication.Recipient.First()), NphiesOrganizationKind.Insurer);
        input.Payloads = communication.Payload.Select(p => new CommunicationPayloadInput
        {
            ContentString = (p.Content as FhirString)?.Value,
            ContentAttachment = Attachment(p.Content as Attachment),
        }).ToList();
        return input;
    }

    public static PaymentNoticeInput PaymentNotice(Bundle bundle)
    {
        var notice = Single<PaymentNotice>(bundle);
        var header = (MessageHeader)bundle.Entry[0].Resource;
        var input = Envelope(bundle, new PaymentNoticeInput());
        input.PaymentNoticeId = notice.Id;
        input.Identifier = Id(notice.Identifier.First());
        input.PaymentIdentifier = Id(notice.Payment.Identifier);
        input.ReconciliationMessageId = header.Response?.Identifier;
        input.Created = notice.Created;
        input.PaymentDate = notice.PaymentDateElement?.Value;
        input.Amount = Money(notice.Amount)!;
        input.PaymentStatus = Code(notice.PaymentStatus) ?? "cleared";
        input.Provider = Organization(Resolve<Organization>(bundle, notice.Provider), NphiesOrganizationKind.Provider);
        return input;
    }

    public static BatchRequestInput Batch(Bundle bundle)
    {
        var input = Envelope(bundle, new BatchRequestInput { AssignBatchExtensions = false });
        input.Claims = bundle.Entry.Select(e => e.Resource).OfType<Bundle>().Select(Claim).ToList();
        var first = input.Claims[0].Batch!;
        input.BatchIdentifier = first.BatchIdentifier;
        input.BatchPeriod = first.BatchPeriod;
        return input;
    }

    #region Resources

    private static OrganizationInput Organization(Organization org, NphiesOrganizationKind kind)
    {
        var identifier = org.Identifier.First();
        var defaultSystem = kind switch
        {
            NphiesOrganizationKind.Provider => NphiesIdentifierSystems.ProviderLicense,
            NphiesOrganizationKind.Insurer => NphiesIdentifierSystems.PayerLicense,
            _ => NphiesIdentifierSystems.Organization,
        };
        return new OrganizationInput
        {
            Id = org.Id,
            License = identifier.Value,
            Name = org.Name,
            Kind = kind,
            IdentifierSystem = identifier.System == defaultSystem ? null : identifier.System,
            IdentifierUseOfficial = identifier.Use == Identifier.IdentifierUse.Official,
            ProviderType = Coding(Ext(org, NphiesExtensions.ProviderType) as CodeableConcept),
            InsurerIdentifierType = identifier.Type?.Coding.FirstOrDefault()?.Code,
        };
    }

    private static PatientInput Patient(Patient patient)
    {
        var identifier = patient.Identifier.First();
        var typeCoding = identifier.Type?.Coding.FirstOrDefault();
        var name = patient.Name.FirstOrDefault();
        var ksaGender = patient.GenderElement?.Extension
            .FirstOrDefault(e => e.Url == NphiesExtensions.KsaAdministrativeGender)?.Value as CodeableConcept;
        return new PatientInput
        {
            Id = patient.Id,
            IdentifierKind = typeCoding?.Code switch
            {
                "PRC" => PatientIdentifierKind.Iqama,
                "PPN" => PatientIdentifierKind.Passport,
                "BN" => PatientIdentifierKind.BorderNumber,
                "DP" => PatientIdentifierKind.DisplacedPerson,
                "MR" => PatientIdentifierKind.MedicalRecord,
                _ => PatientIdentifierKind.NationalId,
            },
            IdentifierValue = identifier.Value,
            IdentifierSystem = identifier.System,
            IdentifierTypeDisplay = typeCoding?.Display,
            NameText = name?.Text,
            Family = name?.Family,
            Given = name?.Given.ToList() ?? new List<string>(),
            Phone = patient.Telecom.FirstOrDefault()?.Value,
            Gender = Literal(patient.GenderElement) ?? "unknown",
            KsaGender = Code(ksaGender),
            BirthDate = patient.BirthDate,
            MaritalStatus = Code(patient.MaritalStatus),
            Occupation = Code(Ext(patient, NphiesExtensions.Occupation) as CodeableConcept),
            Deceased = (patient.Deceased as FhirBoolean)?.Value,
        };
    }

    private static CoverageInput Coverage(Coverage coverage, string patientId)
    {
        var typeCoding = coverage.Type?.Coding.FirstOrDefault();
        var relationship = coverage.Relationship?.Coding.FirstOrDefault();
        var subscriberId = IdOf(coverage.Subscriber?.Reference);
        return new CoverageInput
        {
            Id = coverage.Id,
            MemberId = Id(coverage.Identifier.First()),
            Status = Literal(coverage.StatusElement)!,
            Type = typeCoding?.Code ?? "EHCPOL",
            TypeDisplay = typeCoding?.Display,
            OmitSubscriber = coverage.Subscriber == null,
            SubscriberPatientId = subscriberId != null && subscriberId != patientId ? subscriberId : null,
            SubscriberId = coverage.SubscriberId,
            PolicyHolderOrganizationId = coverage.PolicyHolder?.Reference?.Contains("Organization/") == true ? IdOf(coverage.PolicyHolder.Reference) : null,
            PolicyHolderPatientId = coverage.PolicyHolder?.Reference?.Contains("Patient/") == true ? IdOf(coverage.PolicyHolder.Reference) : null,
            Relationship = relationship?.Code ?? "self",
            RelationshipDisplay = relationship?.Display,
            Period = Period(coverage.Period),
            Classes = coverage.Class.Select(c => new CoverageClassInput
            {
                Type = Code(c.Type)!,
                Value = c.Value,
                Name = c.Name,
            }).ToList(),
            Network = coverage.Network,
        };
    }

    private static PractitionerInput Practitioner(Practitioner practitioner)
    {
        var identifier = practitioner.Identifier.First();
        var name = practitioner.Name.FirstOrDefault();
        return new PractitionerInput
        {
            Id = practitioner.Id,
            License = identifier.Value,
            IdentifierType = Code(identifier.Type) ?? "MD",
            NameText = name?.Text,
            Family = name?.Family,
            Given = name?.Given.ToList() ?? new List<string>(),
            NameUseOfficial = name?.Use == HumanName.NameUse.Official,
        };
    }

    private static PractitionerRoleInput Role(PractitionerRole role) => new()
    {
        Id = role.Id,
        PractitionerLicense = role.Practitioner?.Identifier?.Value ?? string.Empty,
        RoleCode = Code(role.Code.FirstOrDefault()) ?? "doctor",
        Specialty = Code(role.Specialty.FirstOrDefault()),
    };

    private static LocationInput Location(Location location) => new()
    {
        Id = location.Id,
        License = location.Identifier.First().Value,
        Name = location.Name,
        Status = Literal(location.StatusElement) ?? "active",
        Type = Coding(location.Type.FirstOrDefault()),
    };

    private static EncounterInput Encounter(Encounter encounter) => new()
    {
        Id = encounter.Id,
        Identifier = encounter.Identifier.Count > 0 ? Id(encounter.Identifier[0]) : null,
        Status = Literal(encounter.StatusElement)!,
        Class = new NphiesCoding(encounter.Class.System, encounter.Class.Code, encounter.Class.Display),
        ServiceEventType = Coding(Ext(encounter, NphiesExtensions.ServiceEventType) as CodeableConcept),
        Period = Period(encounter.Period),
    };

    private static VisionPrescriptionInput VisionPrescription(VisionPrescription prescription) => new()
    {
        Id = prescription.Id,
        Identifier = Id(prescription.Identifier.First()),
        Created = prescription.Created,
        DateWritten = prescription.DateWritten,
        PrescriberLicense = prescription.Prescriber?.Identifier?.Value ?? string.Empty,
        Lenses = prescription.LensSpecification.Select(l => new LensSpecificationInput
        {
            Product = Code(l.Product)!,
            Eye = Literal(l.EyeElement)!,
            Sphere = l.Sphere,
            Cylinder = l.Cylinder,
            Axis = l.Axis,
            PrismAmount = l.Prism.FirstOrDefault()?.Amount,
            PrismBase = Literal(l.Prism.FirstOrDefault()?.BaseElement),
            Add = l.Add,
            Power = l.Power,
        }).ToList(),
    };

    private static SupportingInfoInput SupportingInfo(Claim.SupportingInformationComponent info)
    {
        var input = new SupportingInfoInput
        {
            Sequence = info.Sequence ?? 0,
            Category = Code(info.Category)!,
            Code = Concept(info.Code),
            TimingDate = (info.Timing as Date)?.Value,
            TimingPeriod = Period(info.Timing as Period),
            Reason = Coding(info.Reason),
        };
        switch (info.Value)
        {
            case Attachment attachment: input.ValueAttachment = Attachment(attachment); break;
            case Quantity quantity:
                input.ValueQuantity = quantity.Value;
                input.ValueQuantityUnit = quantity.Code;
                break;
            case FhirString text: input.ValueString = text.Value; break;
            case FhirBoolean flag: input.ValueBoolean = flag.Value; break;
            case ResourceReference reference: input.ValueReference = reference.Reference; break;
        }

        return input;
    }

    private static ClaimItemInput Item(Claim.ItemComponent item) => new()
    {
        Sequence = item.Sequence ?? 0,
        CareTeamSequence = item.CareTeamSequence.Select(s => s ?? 0).ToList(),
        DiagnosisSequence = item.DiagnosisSequence.Select(s => s ?? 0).ToList(),
        InformationSequence = item.InformationSequence.Select(s => s ?? 0).ToList(),
        ProductOrService = Concept(item.ProductOrService)!,
        ServicedDate = (item.Serviced as Date)?.Value,
        ServicedPeriod = Period(item.Serviced as Period),
        Quantity = item.Quantity?.Value ?? 1,
        UnitPrice = Money(item.UnitPrice)!,
        Factor = item.Factor,
        Net = Money(item.Net),
        BodySite = Concept(item.BodySite),
        SubSite = item.SubSite.Select(Coding).OfType<NphiesCoding>().ToList(),
        Tax = Money(Ext(item, NphiesExtensions.Tax) as Money),
        PatientShare = Money(Ext(item, NphiesExtensions.PatientShare) as Money),
        PayerShare = Money(Ext(item, NphiesExtensions.PayerShare) as Money),
        Package = (Ext(item, NphiesExtensions.Package) as FhirBoolean)?.Value,
        Maternity = (Ext(item, NphiesExtensions.Maternity) as FhirBoolean)?.Value,
        PatientInvoice = Ext(item, NphiesExtensions.PatientInvoice) is Identifier invoice ? Id(invoice) : null,
        Details = item.Detail.Select(d => new ItemDetailInput
        {
            Sequence = d.Sequence ?? 0,
            ProductOrService = Concept(d.ProductOrService)!,
            Quantity = d.Quantity?.Value ?? 1,
            UnitPrice = Money(d.UnitPrice),
            Factor = d.Factor,
            Net = Money(d.Net),
        }).ToList(),
    };

    #endregion

    #region Helpers

    private static T Single<T>(Bundle bundle) where T : Resource => bundle.Entry.Select(e => e.Resource).OfType<T>().Single();

    private static T Resolve<T>(Bundle bundle, ResourceReference reference) where T : Resource =>
        TryResolve<T>(bundle, reference) ?? throw new InvalidOperationException($"Unresolved reference {reference.Reference}");

    private static T? TryResolve<T>(Bundle bundle, ResourceReference? reference) where T : Resource
    {
        if (reference?.Reference == null) return null;
        var entry = bundle.Entry.FirstOrDefault(e => e.FullUrl == reference.Reference)
            ?? bundle.Entry.FirstOrDefault(e => e.Resource is T && reference.Reference.EndsWith($"{e.Resource.TypeName}/{e.Resource.Id}", StringComparison.Ordinal));
        return entry?.Resource as T;
    }

    private static string BaseOf(string url)
    {
        var parts = url.Split('/');
        return string.Join('/', parts.Take(parts.Length - 2));
    }

    private static string? IdOf(string? reference) => reference?.Split('/').Last();

    private static DataType? Ext(IExtendable element, string url) => element.Extension.FirstOrDefault(e => e.Url == url)?.Value;

    private static string? Literal(PrimitiveType? element) => element?.ObjectValue?.ToString();

    private static string? Code(CodeableConcept? concept) => concept?.Coding.FirstOrDefault()?.Code;

    private static NphiesCoding? Coding(CodeableConcept? concept)
    {
        var coding = concept?.Coding.FirstOrDefault();
        return coding == null ? null : new NphiesCoding(coding.System, coding.Code, coding.Display);
    }

    private static NphiesConcept? Concept(CodeableConcept? concept)
    {
        if (concept == null) return null;
        var result = new NphiesConcept { Text = concept.Text };
        result.Codings.AddRange(concept.Coding.Select(c => new NphiesCoding(c.System, c.Code, c.Display)));
        return result;
    }

    private static NphiesIdentifier Id(Identifier identifier) => new(identifier.System, identifier.Value);

    private static NphiesMoney? Money(Money? money) =>
        money?.Value == null ? null : new NphiesMoney(money.Value.Value, Literal(money.CurrencyElement) ?? "SAR");

    private static NphiesPeriod? Period(Period? period) => period == null ? null : new NphiesPeriod(period.Start, period.End);

    private static AttachmentInput? Attachment(Attachment? attachment) => attachment == null ? null : new AttachmentInput
    {
        ContentType = attachment.ContentType,
        Title = attachment.Title,
        Creation = attachment.Creation,
        Url = attachment.Url,
        Data = attachment.Data == null ? null : Convert.ToBase64String(attachment.Data),
    };

    #endregion
}
