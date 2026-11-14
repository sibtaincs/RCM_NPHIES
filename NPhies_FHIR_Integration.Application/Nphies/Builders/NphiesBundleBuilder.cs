using System.Globalization;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Models;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using FhirTask = Hl7.Fhir.Model.Task;

namespace NPhies_FHIR_Integration.Application.Nphies.Builders;

/// <summary>Default <see cref="INphiesBundleBuilder"/> producing messages shaped like the NPHIES examples.</summary>
public sealed class NphiesBundleBuilder : INphiesBundleBuilder
{
    private static readonly TimeSpan KsaOffset = TimeSpan.FromHours(3);
    private readonly NphiesSenderConfiguration _sender;
    private readonly int _batchMaxClaims;

    public NphiesBundleBuilder(IOptions<NphiesConfiguration> options)
    {
        var config = options.Value;
        _sender = config.Sender;
        _batchMaxClaims = config.BatchMaxClaims > 0 ? config.BatchMaxClaims : NphiesLimits.BatchMaxClaims;
    }

    #region Messages

    public Bundle BuildEligibilityRequest(EligibilityRequestInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ctx = new Ctx(input, _sender);

        var provider = ctx.Add(BuildOrganization(input.Provider, NphiesOrganizationKind.Provider));
        var insurer = ctx.Add(BuildOrganization(input.Insurer, NphiesOrganizationKind.Insurer));
        var patient = ctx.Add(BuildPatient(input.Patient));
        string? subscriberUrl = input.Subscriber != null ? ctx.Add(BuildPatient(input.Subscriber)) : null;
        string? policyholderUrl = input.Policyholder != null
            ? ctx.Add(BuildOrganization(input.Policyholder, NphiesOrganizationKind.Policyholder))
            : null;
        string? coverageUrl = input.Coverage != null
            ? ctx.Add(BuildCoverage(input.Coverage, ctx, patient, subscriberUrl, insurer, policyholderUrl))
            : null;
        string? facilityUrl = input.Facility != null ? ctx.Add(BuildLocation(input.Facility, provider)) : null;

        var request = new CoverageEligibilityRequest
        {
            Id = IdOrNew(input.RequestId),
            Meta = Profile(NphiesProfiles.EligibilityRequest),
            Identifier = { Identifier(input.Identifier) },
            Status = ParseEnum<FinancialResourceStatusCodes>(input.Status),
            Priority = Concept(NphiesCodeSystems.ProcessPriority, input.Priority),
            Patient = Ref(patient),
            Created = input.Created,
            Provider = Ref(provider),
            Insurer = Ref(insurer),
        };
        foreach (var purpose in input.Purposes)
        {
            request.PurposeElement.Add(new Code<CoverageEligibilityRequest.EligibilityRequestPurpose>(
                ParseEnum<CoverageEligibilityRequest.EligibilityRequestPurpose>(purpose)));
        }

        if (input.ServicedPeriod != null) request.Serviced = Period(input.ServicedPeriod);
        else if (input.ServicedDate != null) request.Serviced = new Date(input.ServicedDate);
        if (facilityUrl != null) request.Facility = Ref(facilityUrl);
        if (input.Newborn.HasValue)
            request.Extension.Add(new Extension(NphiesExtensions.Newborn, new FhirBoolean(input.Newborn.Value)));
        if (input.Transfer.HasValue)
            request.Extension.Add(new Extension(NphiesExtensions.Transfer, new FhirBoolean(input.Transfer.Value)));
        if (coverageUrl != null)
        {
            request.Insurance.Add(new CoverageEligibilityRequest.InsuranceComponent
            {
                Focal = input.CoverageFocal,
                Coverage = Ref(coverageUrl),
            });
        }

        var focus = ctx.AddFirst(request);
        return ctx.Finish(NphiesMessageEvents.EligibilityRequest, PayerDestination(input.Insurer), focus);
    }

    public Bundle BuildPriorAuthRequest(ClaimRequestInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.Use = NphiesClaimUse.PreAuthorization;
        return BuildClaimMessage(input, NphiesMessageEvents.PriorAuthRequest);
    }

    public Bundle BuildClaimRequest(ClaimRequestInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.Use = NphiesClaimUse.Claim;
        return BuildClaimMessage(input, NphiesMessageEvents.ClaimRequest);
    }

    public Bundle BuildBatchRequest(BatchRequestInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Claims.Count == 0)
            throw new ArgumentException("A batch-request requires at least one claim.", nameof(input));
        if (input.Claims.Count > _batchMaxClaims)
            throw new ArgumentException($"A batch-request may contain at most {_batchMaxClaims} claims.", nameof(input));

        var insurerLicenses = input.Claims.Select(c => c.Insurer.License).Distinct(StringComparer.Ordinal).ToList();
        if (insurerLicenses.Count > 1)
            throw new ArgumentException("All claims in a batch-request must be addressed to the same insurer.", nameof(input));

        var ctx = new Ctx(input, _sender);
        var focus = new List<string>();
        for (var i = 0; i < input.Claims.Count; i++)
        {
            var claim = input.Claims[i];
            if (input.AssignBatchExtensions)
            {
                claim.Batch = new BatchInfoInput
                {
                    BatchIdentifier = input.BatchIdentifier,
                    BatchNumber = i + 1,
                    BatchPeriod = input.BatchPeriod,
                };
            }

            claim.FullUrlBase ??= input.FullUrlBase;
            claim.SourceEndpoint ??= input.SourceEndpoint;
            claim.MessageHeaderFullUrlAsUrl = true;
            var inner = BuildClaimRequest(claim);
            focus.Add(ctx.Add(inner));
        }

        var first = input.Claims[0];
        return ctx.Finish(NphiesMessageEvents.BatchRequest, PayerDestination(first.Insurer), focus.ToArray(), first.Provider);
    }

    public Bundle BuildStatusCheck(ClaimTaskInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return BuildClaimTask(input, NphiesMessageEvents.StatusCheck, "status", reason: null);
    }

    public Bundle BuildCancelRequest(ClaimTaskInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return BuildClaimTask(input, NphiesMessageEvents.CancelRequest, input.CancelCode, input.Reason ?? "WI");
    }

    public Bundle BuildPollRequest(PollRequestInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Count is < 1 or > NphiesLimits.PollMaxMessages)
            throw new ArgumentOutOfRangeException(nameof(input), $"Poll count must be between 1 and {NphiesLimits.PollMaxMessages}.");

        var ctx = new Ctx(input, _sender);
        var provider = ctx.Add(BuildOrganization(input.Provider, NphiesOrganizationKind.Provider));

        var identifier = Identifier(input.Identifier);
        if (input.IdentifierUseOfficial) identifier.Use = Hl7.Fhir.Model.Identifier.IdentifierUse.Official;

        var task = new FhirTask
        {
            Id = IdOrNew(input.TaskId),
            Meta = Profile(NphiesProfiles.PollRequest),
            Identifier = { identifier },
            Status = FhirTask.TaskStatus.Requested,
            Intent = FhirTask.TaskIntent.Order,
            Priority = ParseEnum<RequestPriority>(input.Priority),
            Code = Concept(NphiesCodeSystems.TaskCode, "poll"),
            AuthoredOn = input.AuthoredOn,
            LastModified = input.AuthoredOn,
            Requester = Ref(provider),
            Owner = NphiesRef(null),
        };

        foreach (var type in input.IncludeMessageTypes)
            task.Input.Add(TaskInput("include-message-type", new Code(type)));
        foreach (var type in input.ExcludeMessageTypes)
            task.Input.Add(TaskInput("exclude-message-type", new Code(type)));
        if (input.Count.HasValue)
            task.Input.Add(TaskInput("count", new PositiveInt(input.Count.Value)));
        if (input.Period != null)
            task.Input.Add(TaskInput("period", Period(input.Period)));

        var focus = ctx.AddFirst(task);
        return ctx.Finish(NphiesMessageEvents.PollRequest, NphiesDestination(), focus);
    }

    public Bundle BuildCommunication(CommunicationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ctx = new Ctx(input, _sender);
        var patient = ctx.Add(BuildPatient(input.Patient));
        var provider = ctx.Add(BuildOrganization(input.Provider, NphiesOrganizationKind.Provider));
        var insurer = ctx.Add(BuildOrganization(input.Insurer, NphiesOrganizationKind.Insurer));

        var communication = new Communication
        {
            Id = IdOrNew(input.CommunicationId),
            Meta = Profile(NphiesProfiles.Communication),
            Identifier = { Identifier(input.Identifier) },
            Status = ParseEnum<EventStatus>(input.Status),
            Category = { Concept(NphiesCodeSystems.CommunicationCategory, input.Category) },
            Priority = ParseEnum<RequestPriority>(input.Priority),
            Subject = Ref(patient),
            About = { TypedRef("Claim", input.AboutClaim) },
            Recipient = { Ref(insurer) },
            Sender = Ref(provider),
        };
        if (input.BasedOnCommunicationRequest != null)
            communication.BasedOn.Add(TypedRef("CommunicationRequest", input.BasedOnCommunicationRequest));
        if (input.Reason != null)
            communication.ReasonCode.Add(Concept(input.Reason));

        foreach (var payload in input.Payloads)
        {
            DataType content = payload.ContentAttachment != null
                ? Attachment(payload.ContentAttachment)
                : new FhirString(payload.ContentString ?? string.Empty);
            communication.Payload.Add(new Communication.PayloadComponent { Content = content });
        }

        var focus = ctx.AddFirst(communication);
        return ctx.Finish(NphiesMessageEvents.Communication, PayerDestination(input.Insurer), focus);
    }

    public Bundle BuildPaymentNotice(PaymentNoticeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ctx = new Ctx(input, _sender);
        var provider = ctx.Add(BuildOrganization(input.Provider, NphiesOrganizationKind.Provider));

        var notice = new PaymentNotice
        {
            Id = IdOrNew(input.PaymentNoticeId),
            Meta = Profile(NphiesProfiles.PaymentNotice),
            Identifier = { Identifier(input.Identifier) },
            Status = FinancialResourceStatusCodes.Active,
            Created = input.Created,
            Provider = Ref(provider),
            Payment = new ResourceReference { Identifier = Identifier(input.PaymentIdentifier) },
            Payee = Ref(provider),
            Recipient = new ResourceReference
            {
                Type = "Organization",
                Identifier = new Identifier(NphiesIdentifierSystems.Nphies, NphiesIdentifierSystems.NphiesValue)
                {
                    Type = Concept(NphiesCodeSystems.OrganizationType, "other"),
                },
            },
            Amount = Money(input.Amount),
            PaymentStatus = Concept(NphiesCodeSystems.PaymentStatus, input.PaymentStatus),
        };
        if (input.PaymentDate != null) notice.PaymentDateElement = new Date(input.PaymentDate);

        var focus = ctx.AddFirst(notice);
        var bundle = ctx.Finish(NphiesMessageEvents.PaymentNotice, NphiesDestination(), focus);
        if (!string.IsNullOrEmpty(input.ReconciliationMessageId))
        {
            var header = (MessageHeader)bundle.Entry[0].Resource;
            header.Response = new MessageHeader.ResponseComponent
            {
                Identifier = input.ReconciliationMessageId,
                Code = MessageHeader.ResponseType.Ok,
            };
        }

        return bundle;
    }

    #endregion

    #region Claim

    private Bundle BuildClaimMessage(ClaimRequestInput input, string eventCode)
    {
        if (input.Items.Count == 0)
            throw new ArgumentException("A claim requires at least one item.", nameof(input));

        var ctx = new Ctx(input, _sender);
        var provider = ctx.Add(BuildOrganization(input.Provider, NphiesOrganizationKind.Provider));
        var insurer = ctx.Add(BuildOrganization(input.Insurer, NphiesOrganizationKind.Insurer));
        var patient = ctx.Add(BuildPatient(input.Patient));
        string? subscriberUrl = input.Subscriber != null ? ctx.Add(BuildPatient(input.Subscriber)) : null;
        string? policyholderUrl = input.Policyholder != null
            ? ctx.Add(BuildOrganization(input.Policyholder, NphiesOrganizationKind.Policyholder))
            : null;
        var coverage = ctx.Add(BuildCoverage(input.Coverage, ctx, patient, subscriberUrl, insurer, policyholderUrl));

        var practitionerUrls = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var practitioner in input.Practitioners)
            practitionerUrls[practitioner.Id] = ctx.Add(BuildPractitioner(practitioner));
        var roleUrls = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var role in input.PractitionerRoles)
            roleUrls[role.Id] = ctx.Add(BuildPractitionerRole(role, provider));

        string? encounterUrl = input.Encounter != null ? ctx.Add(BuildEncounter(input.Encounter, patient, provider)) : null;
        string? prescriptionUrl = input.VisionPrescription != null
            ? ctx.Add(BuildVisionPrescription(input.VisionPrescription, patient))
            : null;

        var preAuth = input.Use == NphiesClaimUse.PreAuthorization;
        var typeCode = ClaimTypeCode(input.Type);
        var claim = new Claim
        {
            Id = IdOrNew(input.ClaimId),
            Meta = Profile(NphiesProfiles.ForClaim(typeCode, preAuth)),
            Identifier = { Identifier(input.Identifier) },
            Status = ParseEnum<FinancialResourceStatusCodes>(input.Status),
            Type = Concept(NphiesCodeSystems.ClaimType, typeCode),
            Use = preAuth ? ClaimUseCode.Preauthorization : ClaimUseCode.Claim,
            Patient = Ref(patient),
            Created = input.Created,
            Insurer = Ref(insurer),
            Provider = Ref(provider),
            Priority = Concept(NphiesCodeSystems.ProcessPriority, input.Priority),
            Payee = new Claim.PayeeComponent { Type = Concept(NphiesCodeSystems.PayeeType, input.PayeeType) },
        };

        if (input.SubType != NphiesClaimSubType.None)
            claim.SubType = Concept(NphiesCodeSystems.ClaimSubType, input.SubType.ToString().ToLowerInvariant());
        if (input.BillablePeriod != null) claim.BillablePeriod = Period(input.BillablePeriod);
        if (prescriptionUrl != null) claim.Prescription = Ref(prescriptionUrl);

        AddClaimExtensions(claim, input, encounterUrl);

        foreach (var related in input.Related)
        {
            var reference = new ResourceReference { Identifier = Identifier(related.ClaimIdentifier) };
            if (related.IncludeType) reference.Type = "Claim";
            claim.Related.Add(new Claim.RelatedClaimComponent
            {
                Claim = reference,
                Relationship = Concept(NphiesCodeSystems.RelatedClaimRelationship, related.Relationship),
            });
        }

        foreach (var member in input.CareTeam)
        {
            var url = member.UsePractitionerRole
                ? roleUrls.GetValueOrDefault(member.PractitionerId) ?? ctx.Url("PractitionerRole", member.PractitionerId)
                : practitionerUrls.GetValueOrDefault(member.PractitionerId) ?? ctx.Url("Practitioner", member.PractitionerId);
            claim.CareTeam.Add(new Claim.CareTeamComponent
            {
                Sequence = member.Sequence,
                Provider = Ref(url),
                Role = Concept(NphiesCodeSystems.ClaimCareTeamRole, member.Role),
                Qualification = Concept(NphiesCodeSystems.PracticeCodes, member.Qualification),
            });
        }

        foreach (var info in input.SupportingInfo)
            claim.SupportingInfo.Add(SupportingInfo(info));

        foreach (var diagnosis in input.Diagnoses)
        {
            var component = new Claim.DiagnosisComponent
            {
                Sequence = diagnosis.Sequence,
                Diagnosis = Concept(diagnosis.System, diagnosis.Code, diagnosis.Display),
                Type = { Concept(NphiesCodeSystems.DiagnosisType, diagnosis.Type) },
            };
            if (diagnosis.OnAdmission != null)
                component.OnAdmission = Concept(NphiesCodeSystems.DiagnosisOnAdmission, diagnosis.OnAdmission);
            if (diagnosis.ConditionOnset != null)
                component.Extension.Add(new Extension(NphiesExtensions.ConditionOnset,
                    Concept(NphiesCodeSystems.ConditionOnset, diagnosis.ConditionOnset)));
            claim.Diagnosis.Add(component);
        }

        var insurance = new Claim.InsuranceComponent { Sequence = 1, Focal = true, Coverage = Ref(coverage) };
        if (input.PreAuthRefs.Count > 0)
            insurance.PreAuthRef = input.PreAuthRefs.Select(r => r.Value).ToList();
        claim.Insurance.Add(insurance);

        decimal total = 0;
        foreach (var item in input.Items)
        {
            var component = ClaimItem(item);
            total += component.Net?.Value ?? 0;
            claim.Item.Add(component);
        }

        claim.Total = input.Total != null ? Money(input.Total) : Money(new NphiesMoney(total));

        var focus = ctx.AddFirst(claim);
        return ctx.Finish(eventCode, PayerDestination(input.Insurer), focus);
    }

    private static void AddClaimExtensions(Claim claim, ClaimRequestInput input, string? encounterUrl)
    {
        if (input.Batch != null)
        {
            claim.Extension.Add(new Extension(NphiesExtensions.BatchIdentifier, Identifier(input.Batch.BatchIdentifier)));
            claim.Extension.Add(new Extension(NphiesExtensions.BatchNumber, new PositiveInt(input.Batch.BatchNumber)));
            claim.Extension.Add(new Extension(NphiesExtensions.BatchPeriod, Period(input.Batch.BatchPeriod)));
        }

        if (input.EligibilityOfflineReference != null)
            claim.Extension.Add(new Extension(NphiesExtensions.EligibilityOfflineReference, new FhirString(input.EligibilityOfflineReference)));
        if (input.EligibilityOfflineDate != null)
            claim.Extension.Add(new Extension(NphiesExtensions.EligibilityOfflineDate, new FhirDateTime(input.EligibilityOfflineDate)));
        if (input.AuthorizationOfflineDate != null)
            claim.Extension.Add(new Extension(NphiesExtensions.AuthorizationOfflineDate, new FhirDateTime(input.AuthorizationOfflineDate)));
        if (input.Episode != null)
            claim.Extension.Add(new Extension(NphiesExtensions.Episode, Identifier(input.Episode)));
        if (input.Transfer.HasValue)
            claim.Extension.Add(new Extension(NphiesExtensions.Transfer, new FhirBoolean(input.Transfer.Value)));
        if (input.Newborn.HasValue)
            claim.Extension.Add(new Extension(NphiesExtensions.Newborn, new FhirBoolean(input.Newborn.Value)));
        // Earlier responses are not in this bundle, so they are referenced by business identifier (as in the IG examples).
        if (input.EligibilityResponse != null)
            claim.Extension.Add(new Extension(NphiesExtensions.EligibilityResponse,
                new ResourceReference { Identifier = Identifier(input.EligibilityResponse) }));
        // The prior-auth profiles do not allow priorauthResponse; only claims carry it.
        if (input.PriorAuthResponse != null && input.Use == NphiesClaimUse.Claim)
            claim.Extension.Add(new Extension(NphiesExtensions.PriorAuthResponse,
                new ResourceReference { Identifier = Identifier(input.PriorAuthResponse) }));
        if (encounterUrl != null)
            claim.Extension.Add(new Extension(NphiesExtensions.Encounter, Ref(encounterUrl)));
    }

    private static Claim.SupportingInformationComponent SupportingInfo(SupportingInfoInput info)
    {
        var component = new Claim.SupportingInformationComponent
        {
            Sequence = info.Sequence,
            Category = Concept(NphiesCodeSystems.ClaimInformationCategory, info.Category),
        };
        if (info.Code != null) component.Code = Concept(info.Code);
        if (info.TimingPeriod != null) component.Timing = Period(info.TimingPeriod);
        else if (info.TimingDate != null) component.Timing = new Date(info.TimingDate);

        if (info.ValueAttachment != null) component.Value = Attachment(info.ValueAttachment);
        else if (info.ValueQuantity.HasValue)
        {
            component.Value = new Quantity
            {
                Value = info.ValueQuantity.Value,
                System = info.ValueQuantityUnit != null ? NphiesCodeSystems.Ucum : null,
                Code = info.ValueQuantityUnit,
            };
        }
        else if (info.ValueString != null) component.Value = new FhirString(info.ValueString);
        else if (info.ValueBoolean.HasValue) component.Value = new FhirBoolean(info.ValueBoolean.Value);
        else if (info.ValueReference != null) component.Value = Ref(info.ValueReference);

        if (info.Reason != null) component.Reason = Concept(info.Reason);
        return component;
    }

    private static Claim.ItemComponent ClaimItem(ClaimItemInput item)
    {
        var component = new Claim.ItemComponent
        {
            Sequence = item.Sequence,
            CareTeamSequence = item.CareTeamSequence.Select(s => (int?)s).ToList(),
            DiagnosisSequence = item.DiagnosisSequence.Select(s => (int?)s).ToList(),
            InformationSequence = item.InformationSequence.Select(s => (int?)s).ToList(),
            ProductOrService = Concept(item.ProductOrService),
            Quantity = new Quantity { Value = item.Quantity },
            UnitPrice = Money(item.UnitPrice),
        };

        if (item.Tax != null) component.Extension.Add(new Extension(NphiesExtensions.Tax, Money(item.Tax)));
        if (item.PatientShare != null) component.Extension.Add(new Extension(NphiesExtensions.PatientShare, Money(item.PatientShare)));
        if (item.Package.HasValue) component.Extension.Add(new Extension(NphiesExtensions.Package, new FhirBoolean(item.Package.Value)));
        if (item.PatientInvoice != null) component.Extension.Add(new Extension(NphiesExtensions.PatientInvoice, Identifier(item.PatientInvoice)));
        if (item.PayerShare != null) component.Extension.Add(new Extension(NphiesExtensions.PayerShare, Money(item.PayerShare)));
        if (item.Maternity.HasValue) component.Extension.Add(new Extension(NphiesExtensions.Maternity, new FhirBoolean(item.Maternity.Value)));

        if (item.ServicedPeriod != null) component.Serviced = Period(item.ServicedPeriod);
        else if (item.ServicedDate != null) component.Serviced = new Date(item.ServicedDate);
        if (item.Factor.HasValue) component.Factor = item.Factor;
        if (item.BodySite != null) component.BodySite = Concept(item.BodySite);
        foreach (var subSite in item.SubSite) component.SubSite.Add(Concept(subSite));

        var net = item.Net ?? new NphiesMoney(
            item.Quantity * item.UnitPrice.Value * (item.Factor ?? 1m) + (item.Tax?.Value ?? 0m),
            item.UnitPrice.Currency);
        component.Net = Money(net);

        foreach (var detail in item.Details)
        {
            var detailComponent = new Claim.DetailComponent
            {
                Sequence = detail.Sequence,
                ProductOrService = Concept(detail.ProductOrService),
                Quantity = new Quantity { Value = detail.Quantity },
            };
            if (detail.UnitPrice != null) detailComponent.UnitPrice = Money(detail.UnitPrice);
            if (detail.Factor.HasValue) detailComponent.Factor = detail.Factor;
            var detailNet = detail.Net ?? (detail.UnitPrice != null
                ? new NphiesMoney(detail.Quantity * detail.UnitPrice.Value * (detail.Factor ?? 1m), detail.UnitPrice.Currency)
                : null);
            if (detailNet != null) detailComponent.Net = Money(detailNet);
            component.Detail.Add(detailComponent);
        }

        return component;
    }

    internal static string ClaimTypeCode(NphiesClaimType type) => type switch
    {
        NphiesClaimType.Institutional => "institutional",
        NphiesClaimType.Oral => "oral",
        NphiesClaimType.Pharmacy => "pharmacy",
        NphiesClaimType.Professional => "professional",
        NphiesClaimType.Vision => "vision",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    #endregion

    #region Tasks

    private Bundle BuildClaimTask(ClaimTaskInput input, string eventCode, string taskCode, string? reason)
    {
        var ctx = new Ctx(input, _sender);
        var provider = ctx.Add(BuildOrganization(input.Provider, NphiesOrganizationKind.Provider));
        var insurer = ctx.Add(BuildOrganization(input.Insurer, NphiesOrganizationKind.Insurer));

        var task = new FhirTask
        {
            Id = IdOrNew(input.TaskId),
            Meta = Profile(eventCode == NphiesMessageEvents.StatusCheck ? NphiesProfiles.StatusRequest : NphiesProfiles.CancelRequest),
            Identifier = { Identifier(input.Identifier) },
            Status = FhirTask.TaskStatus.Requested,
            Intent = FhirTask.TaskIntent.Order,
            Priority = ParseEnum<RequestPriority>(input.Priority),
            Code = Concept(NphiesCodeSystems.TaskCode, taskCode),
            Focus = TypedRef("Claim", input.ClaimIdentifier),
            AuthoredOn = input.AuthoredOn,
            LastModified = input.AuthoredOn,
            Requester = Ref(provider),
            Owner = Ref(insurer),
        };
        if (reason != null) task.ReasonCode = Concept(NphiesCodeSystems.TaskReasonCode, reason);

        var focus = ctx.AddFirst(task);
        return ctx.Finish(eventCode, PayerDestination(input.Insurer), focus);
    }

    private static FhirTask.ParameterComponent TaskInput(string type, DataType value) => new()
    {
        Type = Concept(NphiesCodeSystems.TaskInputType, type),
        Value = value,
    };

    #endregion

    #region Resources

    internal static Organization BuildOrganization(OrganizationInput input, NphiesOrganizationKind kind)
    {
        var (profile, system, typeCode) = kind switch
        {
            NphiesOrganizationKind.Provider => (NphiesProfiles.ProviderOrganization, NphiesIdentifierSystems.ProviderLicense, "prov"),
            NphiesOrganizationKind.Insurer => (NphiesProfiles.InsurerOrganization, NphiesIdentifierSystems.PayerLicense, "ins"),
            _ => (NphiesProfiles.PolicyholderOrganization, NphiesIdentifierSystems.Organization, (string?)null),
        };

        var identifier = new Identifier(input.IdentifierSystem ?? system, input.License);
        if (input.IdentifierUseOfficial || kind == NphiesOrganizationKind.Policyholder)
            identifier.Use = Hl7.Fhir.Model.Identifier.IdentifierUse.Official;
        if (kind == NphiesOrganizationKind.Insurer && !string.IsNullOrEmpty(input.InsurerIdentifierType))
            identifier.Type = Concept(NphiesCodeSystems.V2IdentifierType, input.InsurerIdentifierType);

        var organization = new Organization
        {
            Id = IdOrNew(input.Id),
            Meta = Profile(profile),
            Identifier = { identifier },
            Active = true,
            Name = input.Name,
        };
        if (typeCode != null) organization.Type.Add(Concept(NphiesCodeSystems.OrganizationType, typeCode));
        if (input.ProviderType != null)
            organization.Extension.Add(new Extension(NphiesExtensions.ProviderType, Concept(input.ProviderType)));
        return organization;
    }

    internal static Patient BuildPatient(PatientInput input)
    {
        var (typeSystem, typeCode, typeDisplay, system) = input.IdentifierKind switch
        {
            PatientIdentifierKind.Iqama => (NphiesCodeSystems.V2IdentifierType, "PRC", "iqama", NphiesIdentifierSystems.Iqama),
            PatientIdentifierKind.Passport => (NphiesCodeSystems.V2IdentifierType, "PPN", "passport", NphiesIdentifierSystems.Passport),
            PatientIdentifierKind.BorderNumber => (NphiesCodeSystems.PatientIdentifierType, "BN", "border number", NphiesIdentifierSystems.BorderNumber),
            PatientIdentifierKind.DisplacedPerson => (NphiesCodeSystems.PatientIdentifierType, "DP", "displaced person", "http://nphies.sa/identifier/displacedperson"),
            PatientIdentifierKind.MedicalRecord => (NphiesCodeSystems.V2IdentifierType, "MR", "medical record number", "http://provider.com/mrn"),
            _ => (NphiesCodeSystems.V2IdentifierType, "NI", "national card", NphiesIdentifierSystems.NationalId),
        };

        var patient = new Patient
        {
            Id = IdOrNew(input.Id),
            Meta = Profile(NphiesProfiles.Patient),
            Identifier =
            {
                new Identifier(input.IdentifierSystem ?? system, input.IdentifierValue)
                {
                    Type = Concept(typeSystem, typeCode, input.IdentifierTypeDisplay ?? typeDisplay),
                },
            },
            Active = true,
            Name = { HumanName(input.NameText, input.Family, input.Given, official: true) },
            BirthDate = input.BirthDate,
        };
        if (!string.IsNullOrWhiteSpace(input.IdentifierCountry))
            patient.Identifier[0].Extension.Add(new Extension(NphiesExtensions.IdentifierCountry,
                Concept(NphiesCodeSystems.Iso3166, input.IdentifierCountry)));

        if (!string.IsNullOrEmpty(input.Phone))
            patient.Telecom.Add(new ContactPoint(ContactPoint.ContactPointSystem.Phone, null, input.Phone));

        patient.GenderElement = new Code<AdministrativeGender>(ParseEnum<AdministrativeGender>(input.Gender));
        var ksaGender = input.KsaGender ?? input.Gender;
        patient.GenderElement.Extension.Add(new Extension(
            NphiesExtensions.KsaAdministrativeGender,
            Concept(NphiesCodeSystems.KsaAdministrativeGender, ksaGender, GenderDisplay(ksaGender))));

        if (input.Occupation != null)
            patient.Extension.Add(new Extension(NphiesExtensions.Occupation, Concept(NphiesCodeSystems.Occupation, input.Occupation)));
        if (input.MaritalStatus != null)
            patient.MaritalStatus = Concept(
                input.MaritalStatus == "UNK" ? NphiesCodeSystems.V3NullFlavor : NphiesCodeSystems.V3MaritalStatus,
                input.MaritalStatus);
        if (input.Deceased.HasValue)
            patient.Deceased = new FhirBoolean(input.Deceased.Value);
        return patient;
    }

    private static Coverage BuildCoverage(
        CoverageInput input, Ctx ctx, string patientUrl, string? subscriberUrl, string insurerUrl, string? policyholderUrl)
    {
        var coverage = new Coverage
        {
            Id = IdOrNew(input.Id),
            Meta = Profile(NphiesProfiles.Coverage),
            Identifier = { Identifier(input.MemberId) },
            Status = ParseEnum<FinancialResourceStatusCodes>(input.Status),
            Type = Concept(NphiesCodeSystems.CoverageType, input.Type, input.TypeDisplay),
            Beneficiary = Ref(patientUrl),
            Relationship = Concept(NphiesCodeSystems.SubscriberRelationship, input.Relationship, input.RelationshipDisplay),
            Payor = { Ref(insurerUrl) },
        };

        if (input.PolicyHolderOrganizationId != null)
            coverage.PolicyHolder = Ref(policyholderUrl ?? ctx.Url("Organization", input.PolicyHolderOrganizationId));
        else if (input.PolicyHolderPatientId != null)
            coverage.PolicyHolder = Ref(ctx.Url("Patient", input.PolicyHolderPatientId));

        if (!input.OmitSubscriber)
        {
            coverage.Subscriber = Ref(input.SubscriberPatientId != null
                ? ctx.Url("Patient", input.SubscriberPatientId)
                : subscriberUrl ?? patientUrl);
        }

        if (input.SubscriberId != null) coverage.SubscriberId = input.SubscriberId;
        if (input.Period != null) coverage.Period = Period(input.Period);
        foreach (var coverageClass in input.Classes)
        {
            coverage.Class.Add(new Coverage.ClassComponent
            {
                Type = Concept(NphiesCodeSystems.CoverageClass, coverageClass.Type),
                Value = coverageClass.Value,
                Name = coverageClass.Name,
            });
        }

        if (input.Network != null) coverage.Network = input.Network;
        return coverage;
    }

    private static Practitioner BuildPractitioner(PractitionerInput input) => new()
    {
        Id = IdOrNew(input.Id),
        Meta = Profile(NphiesProfiles.Practitioner),
        Identifier =
        {
            new Identifier(NphiesIdentifierSystems.PractitionerLicense, input.License)
            {
                Type = Concept(NphiesCodeSystems.V2IdentifierType, input.IdentifierType),
            },
        },
        Active = true,
        Name = { HumanName(input.NameText, input.Family, input.Given, input.NameUseOfficial) },
    };

    private static PractitionerRole BuildPractitionerRole(PractitionerRoleInput input, string organizationUrl)
    {
        var role = new PractitionerRole
        {
            Id = IdOrNew(input.Id),
            Meta = Profile(NphiesProfiles.PractitionerRole),
            Identifier = { new Identifier(NphiesCodeSystems.PractitionerRole, input.RoleCode) },
            Active = true,
            Practitioner = new ResourceReference
            {
                Type = "Practitioner",
                Identifier = new Identifier(NphiesIdentifierSystems.PractitionerLicenses, input.PractitionerLicense),
            },
            Organization = Ref(organizationUrl),
            Code = { Concept(NphiesCodeSystems.PractitionerRole, input.RoleCode) },
        };
        if (input.Specialty != null) role.Specialty.Add(Concept(NphiesCodeSystems.PracticeCodes, input.Specialty));
        return role;
    }

    private static Location BuildLocation(LocationInput input, string organizationUrl)
    {
        var location = new Location
        {
            Id = IdOrNew(input.Id),
            Meta = Profile(NphiesProfiles.Location),
            Identifier = { new Identifier(NphiesIdentifierSystems.LocationLicense, input.License) },
            Status = ParseEnum<Location.LocationStatus>(input.Status),
            Name = input.Name,
            ManagingOrganization = Ref(organizationUrl),
        };
        if (input.Type != null) location.Type.Add(Concept(input.Type));
        return location;
    }

    private static Encounter BuildEncounter(EncounterInput input, string patientUrl, string providerUrl)
    {
        var encounter = new Encounter
        {
            Id = IdOrNew(input.Id),
            Meta = Profile(NphiesProfiles.Encounter),
            Status = ParseEnum<Encounter.EncounterStatus>(input.Status),
            Class = new Coding(input.Class.System, input.Class.Code, input.Class.Display),
            Subject = Ref(patientUrl),
            ServiceProvider = Ref(providerUrl),
        };
        if (input.Identifier != null) encounter.Identifier.Add(Identifier(input.Identifier));
        if (input.ServiceEventType != null)
            encounter.Extension.Add(new Extension(NphiesExtensions.ServiceEventType, Concept(input.ServiceEventType)));
        if (input.Period != null) encounter.Period = Period(input.Period);
        return encounter;
    }

    private static VisionPrescription BuildVisionPrescription(VisionPrescriptionInput input, string patientUrl)
    {
        var prescription = new VisionPrescription
        {
            Id = IdOrNew(input.Id),
            Meta = Profile(NphiesProfiles.VisionPrescription),
            Identifier = { Identifier(input.Identifier) },
            Status = FinancialResourceStatusCodes.Active,
            Created = input.Created,
            Patient = Ref(patientUrl),
            DateWritten = input.DateWritten,
            Prescriber = new ResourceReference
            {
                Type = "Practitioner",
                Identifier = new Identifier(NphiesIdentifierSystems.PractitionerLicenses, input.PrescriberLicense)
                {
                    Type = Concept(NphiesCodeSystems.V2IdentifierType, "MD"),
                },
            },
        };

        foreach (var lens in input.Lenses)
        {
            var spec = new VisionPrescription.LensSpecificationComponent
            {
                Product = Concept(NphiesCodeSystems.LensType, lens.Product),
                Eye = ParseEnum<VisionPrescription.VisionEyes>(lens.Eye),
                Sphere = lens.Sphere,
                Cylinder = lens.Cylinder,
                Axis = lens.Axis,
                Add = lens.Add,
                Power = lens.Power,
            };
            if (lens.PrismAmount.HasValue && lens.PrismBase != null)
            {
                spec.Prism.Add(new VisionPrescription.PrismComponent
                {
                    Amount = lens.PrismAmount,
                    Base = ParseEnum<VisionPrescription.VisionBase>(lens.PrismBase),
                });
            }

            prescription.LensSpecification.Add(spec);
        }

        return prescription;
    }

    #endregion

    #region Helpers

    private static MessageHeader.MessageDestinationComponent PayerDestination(OrganizationInput insurer) => new()
    {
        Endpoint = NphiesIdentifierSystems.PayerEndpoint(insurer.License),
        Receiver = new ResourceReference
        {
            Type = "Organization",
            Identifier = new Identifier(insurer.IdentifierSystem ?? NphiesIdentifierSystems.PayerLicense, insurer.License),
        },
    };

    private static MessageHeader.MessageDestinationComponent NphiesDestination() => new()
    {
        Endpoint = NphiesIdentifierSystems.NphiesEndpoint,
        Receiver = NphiesRef("Organization"),
    };

    private static ResourceReference NphiesRef(string? type) => new()
    {
        Type = type,
        Identifier = new Identifier(NphiesIdentifierSystems.Nphies, NphiesIdentifierSystems.NphiesValue),
    };

    private static ResourceReference TypedRef(string type, NphiesIdentifier identifier) => new()
    {
        Type = type,
        Identifier = Identifier(identifier),
    };

    private static ResourceReference Ref(string url) => new(url);

    private static Meta Profile(string profile) => new() { Profile = new[] { profile } };

    private static Identifier Identifier(NphiesIdentifier identifier) => new(identifier.System, identifier.Value);

    private static CodeableConcept Concept(string system, string code, string? display = null) =>
        new() { Coding = { new Coding(system, code, display) } };

    private static CodeableConcept Concept(NphiesCoding coding) => Concept(coding.System, coding.Code, coding.Display);

    private static CodeableConcept Concept(NphiesConcept concept)
    {
        var result = new CodeableConcept { Text = concept.Text };
        foreach (var coding in concept.Codings)
            result.Coding.Add(new Coding(coding.System, coding.Code, coding.Display));
        return result;
    }

    private static Money Money(NphiesMoney money) => new()
    {
        Value = money.Value,
        Currency = ParseEnum<Money.Currencies>(money.Currency),
    };

    private static Period Period(NphiesPeriod period) => new()
    {
        Start = period.Start,
        End = period.End,
    };

    private static Attachment Attachment(AttachmentInput input)
    {
        var attachment = new Attachment
        {
            ContentType = input.ContentType,
            Title = input.Title,
            Creation = input.Creation,
            Url = input.Url,
        };
        if (!string.IsNullOrEmpty(input.Data)) attachment.Data = Convert.FromBase64String(input.Data);
        return attachment;
    }

    private static HumanName HumanName(string? text, string? family, IEnumerable<string> given, bool official)
    {
        var name = new HumanName { Text = text, Family = family, Given = given.ToList() };
        if (official) name.Use = Hl7.Fhir.Model.HumanName.NameUse.Official;
        return name;
    }

    private static string GenderDisplay(string code) => code switch
    {
        "male" => "Male",
        "female" => "Female",
        "unknown" => "Unknown",
        "other" => "Other",
        _ => code,
    };

    internal static T ParseEnum<T>(string literal) where T : struct, Enum =>
        EnumUtility.ParseLiteral<T>(literal, ignoreCase: true)
        ?? throw new ArgumentException($"'{literal}' is not a valid {typeof(T).Name} code.");

    internal static string IdOrNew(string? id) => string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;

    internal static string Now() =>
        DateTimeOffset.UtcNow.ToOffset(KsaOffset).ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);

    /// <summary>Per-message state: entries, fullUrl base, message header settings.</summary>
    private sealed class Ctx
    {
        private readonly NphiesMessageInput _input;
        private readonly NphiesSenderConfiguration _sender;
        private readonly List<Bundle.EntryComponent> _entries = new();
        private int _focusIndex;

        public Ctx(NphiesMessageInput input, NphiesSenderConfiguration sender)
        {
            _input = input;
            _sender = sender;
            Base = (input.FullUrlBase ?? sender.ProviderBaseUrl ?? "http://provider.com").TrimEnd('/');
        }

        public string Base { get; }

        public string Url(string type, string id) => $"{Base}/{type}/{id}";

        public string Add(Resource resource)
        {
            var url = Url(resource.TypeName, resource.Id);
            _entries.Add(new Bundle.EntryComponent { FullUrl = url, Resource = resource });
            return url;
        }

        /// <summary>Adds the focal resource, placed directly after the MessageHeader.</summary>
        public string AddFirst(Resource resource)
        {
            var url = Url(resource.TypeName, resource.Id);
            _entries.Insert(_focusIndex++, new Bundle.EntryComponent { FullUrl = url, Resource = resource });
            return url;
        }

        public Bundle Finish(string eventCode, MessageHeader.MessageDestinationComponent destination, string focus) =>
            Finish(eventCode, destination, new[] { focus }, provider: null);

        public Bundle Finish(
            string eventCode,
            MessageHeader.MessageDestinationComponent destination,
            string[] focus,
            OrganizationInput? provider)
        {
            var license = ProviderLicense(provider);
            var header = new MessageHeader
            {
                Id = IdOrNew(_input.MessageId),
                Meta = Profile(NphiesProfiles.MessageHeader),
                Event = new Coding(NphiesCodeSystems.MessageEvents, eventCode),
                Destination = { destination },
                Sender = new ResourceReference
                {
                    Type = "Organization",
                    Identifier = new Identifier(NphiesIdentifierSystems.ProviderLicense, license),
                },
                Source = new MessageHeader.MessageSourceComponent
                {
                    Endpoint = _input.SourceEndpoint
                        ?? _sender.SourceEndpoint
                        ?? NphiesIdentifierSystems.ProviderEndpoint(license),
                },
            };
            foreach (var reference in focus) header.Focus.Add(new ResourceReference(reference));

            var headerUrl = _input.MessageHeaderFullUrlAsUrl ? Url("MessageHeader", header.Id) : $"urn:uuid:{header.Id}";
            var bundle = new Bundle
            {
                Id = IdOrNew(_input.BundleId),
                Meta = Profile(NphiesProfiles.Bundle),
                Type = Bundle.BundleType.Message,
                Timestamp = DateTimeOffset.Parse(_input.Timestamp ?? Now(), CultureInfo.InvariantCulture),
            };
            bundle.Entry.Add(new Bundle.EntryComponent { FullUrl = headerUrl, Resource = header });
            bundle.Entry.AddRange(_entries);
            return bundle;
        }

        private string ProviderLicense(OrganizationInput? provider)
        {
            if (!string.IsNullOrEmpty(provider?.License)) return provider.License;
            var org = _entries
                .Select(e => e.Resource)
                .OfType<Organization>()
                .FirstOrDefault(o => o.Meta?.Profile?.Contains(NphiesProfiles.ProviderOrganization) == true);
            var license = org?.Identifier.FirstOrDefault()?.Value;
            return !string.IsNullOrEmpty(license) ? license : _sender.ProviderLicense;
        }
    }

    #endregion
}
