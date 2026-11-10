using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using FhirTask = Hl7.Fhir.Model.Task;

namespace NPhies_FHIR_Integration.Application.Nphies.Parsing;

/// <summary>Default <see cref="INphiesResponseParser"/>.</summary>
public sealed class NphiesResponseParser : INphiesResponseParser
{
    private readonly INphiesErrorCatalog _catalog;

#pragma warning disable CS0618 // ParserSettings is the supported way to configure the legacy FhirJsonParser in SDK 5.x
    private static readonly FhirJsonParser JsonParser = new(new ParserSettings
    {
        PermissiveParsing = true,
        AcceptUnknownMembers = true,
        AllowUnrecognizedEnums = true,
    });
#pragma warning restore CS0618

    public NphiesResponseParser(INphiesErrorCatalog? catalog = null)
    {
        _catalog = catalog ?? NphiesErrorCatalog.Instance;
    }

    public Resource ParseResource(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonParser.Parse<Resource>(json);
    }

    public NphiesResponse Parse(string json)
    {
        Resource resource;
        try
        {
            resource = ParseResource(json);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException
                                       || ex.GetType().Name.Contains("Deserialization", StringComparison.Ordinal))
        {
            return new NphiesResponse
            {
                Errors = { new NphiesError("PARSE", $"Response is not valid FHIR JSON: {ex.Message}", null, NphiesErrorSource.Transport) },
            };
        }

        return Parse(resource);
    }

    public NphiesResponse Parse(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return resource switch
        {
            Bundle bundle => ParseBundle(bundle),
            OperationOutcome outcome => new NphiesResponse
            {
                Resource = outcome,
                Errors = ErrorsFrom(outcome).ToList(),
            },
            _ => new NphiesResponse
            {
                Resource = resource,
                Errors = { new NphiesError("UNEXPECTED", $"Unexpected response resource type {resource.TypeName}.", null, NphiesErrorSource.Transport) },
            },
        };
    }

    private NphiesResponse ParseBundle(Bundle bundle)
    {
        var header = bundle.Entry.Select(e => e.Resource).OfType<MessageHeader>().FirstOrDefault();
        var errors = new List<NphiesError>();
        var focus = ResolveFocus(bundle, header);

        if (header?.Response?.Details?.Reference is { } detailsRef
            && Resolve(bundle, detailsRef) is OperationOutcome details)
        {
            errors.AddRange(ErrorsFrom(details));
        }

        foreach (var outcome in bundle.Entry.Select(e => e.Resource).OfType<OperationOutcome>())
        {
            if (!ReferenceEquals(outcome, header?.Response?.Details?.Reference is { } r ? Resolve(bundle, r) : null))
                errors.AddRange(ErrorsFrom(outcome));
        }

        NphiesEligibilityResult? eligibility = null;
        NphiesClaimAdjudicationResult? adjudication = null;
        NphiesTaskResult? task = null;
        NphiesPaymentReconciliationResult? reconciliation = null;
        NphiesCommunicationRequestResult? communicationRequest = null;

        switch (focus)
        {
            case CoverageEligibilityResponse eligibilityResponse:
                eligibility = ParseEligibility(eligibilityResponse, bundle);
                errors.AddRange(eligibilityResponse.Error.Select(e => Error(e.Code, null, NphiesErrorSource.EligibilityResponse)));
                break;
            case ClaimResponse claimResponse:
                adjudication = ParseClaimResponse(claimResponse);
                errors.AddRange(claimResponse.Error.Select(e => Error(e.Code, e.ItemSequence, NphiesErrorSource.ClaimResponse)));
                break;
            case FhirTask fhirTask:
                task = ParseTask(fhirTask);
                errors.AddRange(fhirTask.Output
                    .Where(o => Code(o.Type) == "error" && o.Value is CodeableConcept)
                    .Select(o => Error((CodeableConcept)o.Value, null, NphiesErrorSource.TaskOutput)));
                break;
            case PaymentReconciliation paymentReconciliation:
                reconciliation = ParseReconciliation(paymentReconciliation);
                break;
            case CommunicationRequest request:
                communicationRequest = ParseCommunicationRequest(request);
                break;
        }

        var inner = bundle.Entry
            .Select(e => e.Resource)
            .OfType<Bundle>()
            .Select(ParseBundle)
            .ToList();

        return new NphiesResponse
        {
            Resource = bundle,
            BundleId = bundle.Id,
            MessageId = header?.Id,
            EventCode = (header?.Event as Coding)?.Code,
            RequestMessageId = header?.Response?.Identifier,
            ResponseCode = header?.Response?.CodeElement?.ObjectValue as string,
            SenderLicense = header?.Sender?.Identifier?.Value,
            Errors = errors,
            Eligibility = eligibility,
            Adjudication = adjudication,
            Task = task,
            PaymentReconciliation = reconciliation,
            CommunicationRequest = communicationRequest,
            InnerMessages = inner,
        };
    }

    private static Resource? ResolveFocus(Bundle bundle, MessageHeader? header)
    {
        foreach (var focus in header?.Focus ?? Enumerable.Empty<ResourceReference>())
        {
            if (focus.Reference is { } reference && Resolve(bundle, reference) is { } resolved)
                return resolved;
        }

        return bundle.Entry.Select(e => e.Resource).FirstOrDefault(r => r is not null and not MessageHeader and not Bundle);
    }

    private static Resource? Resolve(Bundle bundle, string reference)
    {
        var entry = bundle.Entry.FirstOrDefault(e => string.Equals(e.FullUrl, reference, StringComparison.OrdinalIgnoreCase));
        if (entry != null) return entry.Resource;

        // Fall back to Type/id matching (NPHIES examples sometimes vary host or casing).
        var parts = reference.TrimEnd('/').Split('/');
        if (parts.Length < 2) return null;
        var (type, id) = (parts[^2], parts[^1]);
        return bundle.Entry.Select(e => e.Resource).FirstOrDefault(r =>
            r != null && string.Equals(r.TypeName, type, StringComparison.OrdinalIgnoreCase) && r.Id == id);
    }

    #region Resource parsers

    private static NphiesEligibilityResult ParseEligibility(CoverageEligibilityResponse response, Bundle bundle) => new()
    {
        Id = response.Id,
        Identifier = response.Identifier.FirstOrDefault()?.Value,
        RequestIdentifier = response.Request?.Identifier?.Value,
        Purposes = response.PurposeElement.Select(p => p.ObjectValue as string).OfType<string>().ToList(),
        Outcome = response.OutcomeElement?.ObjectValue as string,
        Disposition = response.Disposition,
        SiteEligibility = ExtensionCode(response, NphiesExtensions.SiteEligibility),
        PreAuthRef = response.PreAuthRef,
        Insurance = response.Insurance.Select(insurance => new NphiesEligibilityInsurance
        {
            CoverageReference = insurance.Coverage?.Reference,
            InForce = insurance.Inforce,
            NotInForceReason = ExtensionCode(insurance, NphiesExtensions.NotInForceReason),
            BenefitPeriodStart = insurance.BenefitPeriod?.Start,
            BenefitPeriodEnd = insurance.BenefitPeriod?.End,
            Items = insurance.Item.Select(item => new NphiesBenefitItem
            {
                Category = Code(item.Category),
                ProductOrService = Code(item.ProductOrService),
                Excluded = item.Excluded,
                Name = item.Name,
                Description = item.Description,
                Network = Code(item.Network),
                Unit = Code(item.Unit),
                Term = Code(item.Term),
                AuthorizationRequired = item.AuthorizationRequired,
                Benefits = item.Benefit.Select(benefit => new NphiesBenefit
                {
                    Type = Code(benefit.Type),
                    AllowedMoney = (benefit.Allowed as Money)?.Value,
                    UsedMoney = (benefit.Used as Money)?.Value,
                    Allowed = benefit.Allowed is Money ? null : benefit.Allowed?.ToString(),
                    Used = benefit.Used is Money ? null : benefit.Used?.ToString(),
                }).ToList(),
            }).ToList(),
        }).ToList(),
        Coverages = bundle.Entry.Select(e => e.Resource).OfType<Coverage>().ToList(),
    };

    private static NphiesClaimAdjudicationResult ParseClaimResponse(ClaimResponse response) => new()
    {
        Id = response.Id,
        Identifier = response.Identifier.FirstOrDefault()?.Value,
        RequestIdentifier = response.Request?.Identifier?.Value,
        RequestIdentifierSystem = response.Request?.Identifier?.System,
        Use = response.UseElement?.ObjectValue as string,
        ClaimType = Code(response.Type),
        Outcome = response.OutcomeElement?.ObjectValue as string,
        AdjudicationOutcome = ExtensionCode(response, NphiesExtensions.AdjudicationOutcome),
        ReissueReason = ExtensionCode(response, NphiesExtensions.AdjudicationReissue),
        Disposition = response.Disposition,
        PreAuthRef = response.PreAuthRef,
        PreAuthPeriodStart = response.PreAuthPeriod?.Start,
        PreAuthPeriodEnd = response.PreAuthPeriod?.End,
        Totals = response.Total.Select(t => new NphiesAdjudication(
            Code(t.Category) ?? string.Empty, t.Amount?.Value, CurrencyOf(t.Amount), null, null)).ToList(),
        Items = response.Item.Select(item => new NphiesItemAdjudication
        {
            ItemSequence = item.ItemSequence ?? 0,
            Outcome = ExtensionCode(item, NphiesExtensions.AdjudicationOutcome),
            Adjudications = Adjudications(item.Adjudication),
            Details = item.Detail.Select(detail => new NphiesItemAdjudication
            {
                ItemSequence = detail.DetailSequence ?? 0,
                Outcome = ExtensionCode(detail, NphiesExtensions.AdjudicationOutcome),
                Adjudications = Adjudications(detail.Adjudication),
            }).ToList(),
        }).ToList(),
        Payment = response.Payment?.Amount is { } amount ? new NphiesAmount(amount.Value, CurrencyOf(amount)) : null,
        PaymentDate = response.Payment?.Date,
        ProcessNotes = response.ProcessNote.Select(n => n.Text).OfType<string>().ToList(),
        CommunicationRequests = response.CommunicationRequest
            .Select(r => r.Reference ?? r.Identifier?.Value)
            .OfType<string>()
            .ToList(),
    };

    private static NphiesTaskResult ParseTask(FhirTask task) => new()
    {
        Id = task.Id,
        Identifier = task.Identifier.FirstOrDefault()?.Value,
        Code = Code(task.Code),
        Status = task.StatusElement?.ObjectValue as string,
        StatusReason = Code(task.StatusReason),
        FocusIdentifier = task.Focus?.Identifier?.Value,
        FocusIdentifierSystem = task.Focus?.Identifier?.System,
        ProcessingStatus = task.Output
            .Where(o => Code(o.Type) == "status")
            .Select(o => o.Value switch
            {
                Code code => code.Value,
                CodeableConcept concept => Code(concept),
                FhirString text => text.Value,
                _ => null,
            })
            .FirstOrDefault(v => v != null),
        ResponseReferences = task.Output
            .Where(o => Code(o.Type) == "response")
            .Select(o => (o.Value as ResourceReference)?.Reference)
            .OfType<string>()
            .ToList(),
    };

    private static NphiesPaymentReconciliationResult ParseReconciliation(PaymentReconciliation reconciliation) => new()
    {
        Id = reconciliation.Id,
        Identifier = reconciliation.Identifier.FirstOrDefault()?.Value,
        IdentifierSystem = reconciliation.Identifier.FirstOrDefault()?.System,
        Outcome = reconciliation.OutcomeElement?.ObjectValue as string,
        Disposition = reconciliation.Disposition,
        PeriodStart = reconciliation.Period?.Start,
        PeriodEnd = reconciliation.Period?.End,
        PaymentDate = reconciliation.PaymentDate,
        PaymentAmount = reconciliation.PaymentAmount is { } amount ? new NphiesAmount(amount.Value, CurrencyOf(amount)) : null,
        PaymentIdentifier = reconciliation.PaymentIdentifier?.Value,
        PaymentMethod = Code(reconciliation.PaymentIdentifier?.Type),
        Details = reconciliation.Detail.Select(detail => new NphiesReconciliationDetail
        {
            Type = Code(detail.Type),
            Identifier = detail.Identifier?.Value,
            Predecessor = detail.Predecessor?.Value,
            RequestIdentifier = detail.Request?.Identifier?.Value,
            ResponseIdentifier = detail.Response?.Identifier?.Value,
            Date = detail.Date,
            Amount = detail.Amount?.Value,
            ComponentPayment = ExtensionMoney(detail, NphiesExtensions.ComponentPayment),
            ComponentEarlyFee = ExtensionMoney(detail, NphiesExtensions.ComponentEarlyFee),
            ComponentNphiesFee = ExtensionMoney(detail, NphiesExtensions.ComponentNphiesFee),
        }).ToList(),
    };

    private static NphiesCommunicationRequestResult ParseCommunicationRequest(CommunicationRequest request)
    {
        var about = request.About.FirstOrDefault();
        return new NphiesCommunicationRequestResult
        {
            Id = request.Id,
            Identifier = request.Identifier.FirstOrDefault()?.Value,
            IdentifierSystem = request.Identifier.FirstOrDefault()?.System,
            Status = request.StatusElement?.ObjectValue as string,
            Category = request.Category.Select(Code).FirstOrDefault(c => c != null),
            Priority = request.PriorityElement?.ObjectValue as string,
            AboutClaimIdentifier = about?.Identifier?.Value,
            AboutClaimIdentifierSystem = about?.Identifier?.System,
            PayloadText = request.Payload.Select(p => (p.Content as FhirString)?.Value).OfType<string>().ToList(),
            PayloadAttachments = request.Payload.Select(p => p.Content as Attachment).OfType<Attachment>().ToList(),
            Reason = request.ReasonCode.Select(Code).FirstOrDefault(c => c != null),
        };
    }

    #endregion

    #region Errors

    private IEnumerable<NphiesError> ErrorsFrom(OperationOutcome outcome)
    {
        foreach (var issue in outcome.Issue)
        {
            var code = issue.Details?.Coding.FirstOrDefault()?.Code
                       ?? issue.CodeElement?.ObjectValue as string
                       ?? "unknown";
            var message = issue.Details?.Text
                          ?? issue.Details?.Coding.FirstOrDefault()?.Display
                          ?? _catalog.Describe(code)
                          ?? issue.Diagnostics;
            yield return new NphiesError(
                code,
                message,
                issue.Expression?.FirstOrDefault() ?? issue.Location?.FirstOrDefault(),
                NphiesErrorSource.OperationOutcome,
                Severity: issue.SeverityElement?.ObjectValue as string);
        }
    }

    private NphiesError Error(CodeableConcept? concept, int? itemSequence, NphiesErrorSource source)
    {
        var coding = concept?.Coding.FirstOrDefault();
        var code = coding?.Code ?? "unknown";
        var expression = concept?.Extension
            .Concat(coding?.Extension ?? Enumerable.Empty<Extension>())
            .FirstOrDefault(e => e.Url == NphiesExtensions.ErrorExpression)?.Value?.ToString();
        var message = coding?.Display ?? concept?.Text ?? _catalog.Describe(code);
        return new NphiesError(code, message, expression, source, itemSequence, "error");
    }

    #endregion

    #region Helpers

    private static List<NphiesAdjudication> Adjudications(IEnumerable<ClaimResponse.AdjudicationComponent> adjudications) =>
        adjudications.Select(a => new NphiesAdjudication(
            Code(a.Category) ?? string.Empty,
            a.Amount?.Value,
            CurrencyOf(a.Amount),
            a.Value,
            Code(a.Reason))).ToList();

    private static string? CurrencyOf(Money? money) => money?.CurrencyElement?.ObjectValue as string;

    private static string? Code(CodeableConcept? concept) => concept?.Coding.FirstOrDefault()?.Code;

    private static string? ExtensionCode(IExtendable element, string url) =>
        element.Extension.FirstOrDefault(e => e.Url == url)?.Value switch
        {
            CodeableConcept concept => Code(concept),
            Coding coding => coding.Code,
            Code code => code.Value,
            FhirString text => text.Value,
            _ => null,
        };

    private static decimal? ExtensionMoney(IExtendable element, string url) =>
        (element.Extension.FirstOrDefault(e => e.Url == url)?.Value as Money)?.Value;

    #endregion
}
