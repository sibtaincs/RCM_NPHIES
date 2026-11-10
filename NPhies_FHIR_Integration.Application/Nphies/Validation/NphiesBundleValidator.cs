using Hl7.Fhir.Model;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using FhirTask = Hl7.Fhir.Model.Task;

namespace NPhies_FHIR_Integration.Application.Nphies.Validation;

public enum NphiesValidationSeverity
{
    Warning,
    Error,
}

/// <summary>A single validation finding. <see cref="RuleId"/> reuses NPHIES rule ids (e.g. BV-00059) where one exists.</summary>
public sealed record NphiesValidationIssue(string RuleId, NphiesValidationSeverity Severity, string Message, string? Expression = null);

public sealed class NphiesValidationResult
{
    public List<NphiesValidationIssue> Issues { get; } = new();

    public bool IsValid => Issues.All(i => i.Severity != NphiesValidationSeverity.Error);

    public IEnumerable<NphiesValidationIssue> Errors => Issues.Where(i => i.Severity == NphiesValidationSeverity.Error);

    public override string ToString() => string.Join(Environment.NewLine, Issues.Select(i => $"[{i.Severity}] {i.RuleId} {i.Expression}: {i.Message}"));
}

/// <summary>
/// Validates outgoing NPHIES message bundles before they are sent using the business rules from
/// "nphies ValidationErrorCodes" that can be evaluated locally, plus the structural envelope rules.
/// IG profile conformance (StructureDefinitions / terminology from the embedded nphies-fs package) is checked by
/// <see cref="NphiesProfileValidator"/>; <see cref="CompositeNphiesBundleValidator"/> combines both.
/// </summary>
public interface INphiesBundleValidator
{
    /// <param name="bundle">Outgoing message bundle.</param>
    /// <param name="serializedSizeBytes">Optional serialized size, checked against the 10 MB NPHIES limit.</param>
    NphiesValidationResult Validate(Bundle bundle, long? serializedSizeBytes = null);
}

public sealed class NphiesBundleValidator : INphiesBundleValidator
{
    private const decimal MoneyTolerance = 0.01m;

    private static readonly HashSet<string> KnownEvents = new(StringComparer.Ordinal)
    {
        NphiesMessageEvents.EligibilityRequest, NphiesMessageEvents.PriorAuthRequest, NphiesMessageEvents.ClaimRequest,
        NphiesMessageEvents.BatchRequest, NphiesMessageEvents.StatusCheck, NphiesMessageEvents.CancelRequest,
        NphiesMessageEvents.PaymentNotice, NphiesMessageEvents.Communication, NphiesMessageEvents.Acknowledgement,
        NphiesMessageEvents.PollRequest, NphiesMessageEvents.PrescriberRequest,
        NphiesMessageEvents.EligibilityResponse, NphiesMessageEvents.PriorAuthResponse, NphiesMessageEvents.ClaimResponse,
        NphiesMessageEvents.BatchResponse, NphiesMessageEvents.StatusResponse, NphiesMessageEvents.CancelResponse,
        NphiesMessageEvents.PaymentReconciliation, NphiesMessageEvents.CommunicationRequest,
        NphiesMessageEvents.PollResponse, NphiesMessageEvents.AdvancedAuthorization, NphiesMessageEvents.PrescriberResponse,
        NphiesMessageEvents.ErrorNotice,
    };

    private static readonly Dictionary<string, Type> FocusTypes = new(StringComparer.Ordinal)
    {
        [NphiesMessageEvents.EligibilityRequest] = typeof(CoverageEligibilityRequest),
        [NphiesMessageEvents.PriorAuthRequest] = typeof(Claim),
        [NphiesMessageEvents.ClaimRequest] = typeof(Claim),
        [NphiesMessageEvents.BatchRequest] = typeof(Bundle),
        [NphiesMessageEvents.StatusCheck] = typeof(FhirTask),
        [NphiesMessageEvents.CancelRequest] = typeof(FhirTask),
        [NphiesMessageEvents.PollRequest] = typeof(FhirTask),
        [NphiesMessageEvents.PaymentNotice] = typeof(PaymentNotice),
        [NphiesMessageEvents.Communication] = typeof(Communication),
        [NphiesMessageEvents.Acknowledgement] = typeof(FhirTask),
    };

    private readonly int _batchMaxClaims;
    private readonly int _pollMaxCount;
    private readonly long _maxMessageSize;

    public NphiesBundleValidator(int batchMaxClaims = NphiesLimits.BatchMaxClaims,
                                 int pollMaxCount = NphiesLimits.PollMaxMessages,
                                 long maxMessageSizeBytes = NphiesLimits.MaxMessageSizeBytes)
    {
        _batchMaxClaims = batchMaxClaims;
        _pollMaxCount = pollMaxCount;
        _maxMessageSize = maxMessageSizeBytes;
    }

    public NphiesValidationResult Validate(Bundle bundle, long? serializedSizeBytes = null)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        var result = new NphiesValidationResult();

        if (serializedSizeBytes > _maxMessageSize)
            Error(result, "NPHIES-SIZE", $"Message size {serializedSizeBytes} bytes exceeds the NPHIES limit of {_maxMessageSize} bytes.", "Bundle");

        ValidateMessage(bundle, result, "Bundle", isInnerBatchMessage: false);
        return result;
    }

    private void ValidateMessage(Bundle bundle, NphiesValidationResult result, string path, bool isInnerBatchMessage)
    {
        if (bundle.Type != Bundle.BundleType.Message)
            Error(result, "BV-00130", "Bundle.type SHALL be \"message\".", $"{path}.type");
        if (string.IsNullOrWhiteSpace(bundle.Id))
            Error(result, "NPHIES-BUNDLEID", "Bundle.id is required.", $"{path}.id");
        if (bundle.Timestamp is null)
            Error(result, "IC-00010", "Bundle.timestamp is required.", $"{path}.timestamp");
        if (bundle.Entry.Count == 0)
        {
            Error(result, "NPHIES-NOENTRY", "Bundle SHALL contain at least the MessageHeader entry.", $"{path}.entry");
            return;
        }

        ValidateProfile(bundle, result, path);

        if (bundle.Entry[0].Resource is not MessageHeader header)
        {
            Error(result, "BV-00126", "The first Bundle entry SHALL be the MessageHeader.", $"{path}.entry[0]");
            return;
        }

        var fullUrls = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < bundle.Entry.Count; i++)
        {
            var entry = bundle.Entry[i];
            var entryPath = $"{path}.entry[{i}]";
            if (string.IsNullOrWhiteSpace(entry.FullUrl))
                Error(result, "DT-00003", "Bundle.entry.fullUrl is required.", $"{entryPath}.fullUrl");
            else if (!fullUrls.Add(entry.FullUrl))
                Error(result, "NPHIES-DUPURL", $"Duplicate Bundle.entry.fullUrl '{entry.FullUrl}'.", $"{entryPath}.fullUrl");
            else if (entry.FullUrl.Contains("/_history/", StringComparison.Ordinal))
                Error(result, "FR-00008", "fullUrl cannot be a version specific reference.", $"{entryPath}.fullUrl");

            if (entry.Resource is null)
                Error(result, "IC-01435", "Bundle.entry.resource is required.", $"{entryPath}.resource");
            else if (entry.Resource is not Bundle)
                ValidateProfile(entry.Resource, result, $"{entryPath}.resource");
        }

        var eventCode = (header.Event as Coding)?.Code;
        ValidateHeader(header, eventCode, result, $"{path}.entry[0].resource");

        var focus = header.Focus.Select(f => f.Reference is { } r ? Resolve(bundle, r) : null).ToList();
        for (var i = 0; i < header.Focus.Count; i++)
        {
            if (focus[i] is null)
                Error(result, "RE-00100", $"MessageHeader.focus '{header.Focus[i].Reference}' does not resolve to a bundle entry.", $"MessageHeader.focus[{i}]");
        }

        if (eventCode != NphiesMessageEvents.BatchRequest && header.Focus.Count != 1)
            Error(result, "BV-00221", "MessageHeader.focus SHALL have exactly one main resource reference for non-batch messages.", "MessageHeader.focus");

        if (eventCode != null && FocusTypes.TryGetValue(eventCode, out var expectedType))
        {
            foreach (var resolved in focus.OfType<Resource>())
            {
                if (!expectedType.IsInstanceOfType(resolved))
                    Error(result, "BV-00167", $"MessageHeader.focus for '{eventCode}' SHALL be {expectedType.Name}, found {resolved.TypeName}.", "MessageHeader.focus");
            }
        }

        if (isInnerBatchMessage && eventCode != NphiesMessageEvents.ClaimRequest)
            Error(result, "BV-00383", "MessageHeader.eventCoding SHALL be \"claim-request\" in each bundle entry within a batch claim request.", $"{path}.entry[0].resource.eventCoding");

        switch (focus.FirstOrDefault())
        {
            case Claim claim:
                ValidateClaim(claim, eventCode, bundle, result, isInnerBatchMessage);
                break;
            case FhirTask task when eventCode == NphiesMessageEvents.PollRequest:
                ValidatePoll(task, result);
                break;
            case FhirTask task when eventCode is NphiesMessageEvents.StatusCheck or NphiesMessageEvents.CancelRequest:
                if (task.Focus?.Identifier?.Value is null && task.Focus?.Reference is null)
                    Error(result, "IC-00398", $"Task.focus SHALL identify the request targeted by '{eventCode}'.", "Task.focus");
                break;
        }

        if (eventCode == NphiesMessageEvents.BatchRequest)
            ValidateBatch(bundle, header, focus, result);
    }

    private static void ValidateHeader(MessageHeader header, string? eventCode, NphiesValidationResult result, string path)
    {
        if (string.IsNullOrWhiteSpace(header.Id))
            Error(result, "IC-00380", "MessageHeader.id is required.", $"{path}.id");
        if (eventCode is null)
            Error(result, "IC-00377", "MessageHeader.eventCoding is required.", $"{path}.eventCoding");
        else if (!KnownEvents.Contains(eventCode))
            Error(result, "BV-00545", $"Unknown MessageHeader.eventCoding '{eventCode}'.", $"{path}.eventCoding");
        else if ((header.Event as Coding)?.System != NphiesCodeSystems.MessageEvents)
            Error(result, "IC-00377", $"MessageHeader.eventCoding.system SHALL be {NphiesCodeSystems.MessageEvents}.", $"{path}.eventCoding.system");

        if (header.Destination.Count == 0)
            Error(result, "IC-00375", "MessageHeader.destination is required.", $"{path}.destination");
        else if (header.Destination.Any(d => string.IsNullOrWhiteSpace(d.Endpoint) || d.Receiver?.Identifier?.Value is null))
            Error(result, "BV-00218", "MessageHeader.destination SHALL have an endpoint and receiver identifier.", $"{path}.destination");

        if (header.Sender?.Identifier?.Value is null)
            Error(result, "BV-00176", "MessageHeader.sender SHALL carry the sender license identifier.", $"{path}.sender");
        if (string.IsNullOrWhiteSpace(header.Source?.Endpoint))
            Error(result, "IC-00228", "MessageHeader.source.endpoint is required.", $"{path}.source");
    }

    private static void ValidateClaim(Claim claim, string? eventCode, Bundle bundle, NphiesValidationResult result, bool inBatch)
    {
        var use = claim.UseElement?.ObjectValue as string;
        if (eventCode == NphiesMessageEvents.ClaimRequest && use != "claim")
            Error(result, "BV-00165", "Claim.use SHALL be \"claim\" for a claim-request.", "Claim.use");
        if (eventCode == NphiesMessageEvents.PriorAuthRequest && use != "preauthorization")
            Error(result, "BV-00165", "Claim.use SHALL be \"preauthorization\" for a priorauth-request.", "Claim.use");

        var type = claim.Type?.Coding.FirstOrDefault()?.Code;
        var subType = claim.SubType?.Coding.FirstOrDefault()?.Code;
        if (type is null)
            Error(result, "IC-00063", "Claim.type is required.", "Claim.type");
        if (subType is null)
            Warning(result, "NPHIES-SUBTYPE", "Claim.subType should be provided (required by the IG; some docs examples omit it).", "Claim.subType");
        else
        {
            var (ruleId, allowed) = type switch
            {
                "institutional" => ("BV-00364", new[] { "ip" }),
                "professional" => ("BV-00365", new[] { "op", "emr" }),
                "oral" => ("BV-00366", new[] { "op" }),
                "vision" => ("BV-00367", new[] { "op" }),
                "pharmacy" => ("BV-00368", new[] { "op" }),
                _ => (string.Empty, Array.Empty<string>()),
            };
            if (allowed.Length > 0 && !allowed.Contains(subType, StringComparer.OrdinalIgnoreCase))
                Error(result, ruleId, $"Claim.subType '{subType}' is not allowed for Claim.type '{type}' (allowed: {string.Join(", ", allowed)}).", "Claim.subType");
        }

        if (type != null && use != null)
        {
            var expected = NphiesProfiles.ForClaim(type, use == "preauthorization");
            if (claim.Meta?.Profile.Contains(expected) != true)
                Error(result, "BV-00225", $"Claim.meta.profile SHALL be {expected} for type '{type}' and use '{use}'.", "Claim.meta.profile");
        }

        foreach (var reference in new[] { claim.Patient, claim.Provider, claim.Insurer })
        {
            if (reference?.Reference is { } r && Resolve(bundle, r) is null)
                Error(result, "NPHIES-REF", $"Reference '{r}' does not resolve to a bundle entry.", "Claim");
        }

        if (claim.Patient is null) Error(result, "IC-00050", "Claim.patient is required.", "Claim.patient");
        if (claim.Provider is null) Error(result, "IC-00056", "Claim.provider is required.", "Claim.provider");
        if (claim.Insurer is null) Error(result, "IC-00030", "Claim.insurer is required.", "Claim.insurer");
        if (claim.Priority is null) Error(result, "IC-00053", "Claim.priority is required.", "Claim.priority");
        if (claim.Insurance.Count == 0) Error(result, "IC-00026", "Claim.insurance is required.", "Claim.insurance");
        if (claim.Insurance.Count > 1 && claim.Insurance.Count(i => i.Focal == true) != 1)
            Error(result, "BV-00400", "Exactly one Claim.insurance SHALL be focal.", "Claim.insurance.focal");
        if (claim.Item.Count == 0) Error(result, "IC-00031", "Claim.item is required.", "Claim.item");

        Unique(claim.CareTeam.Select(c => c.Sequence), "BV-00002", "Claim.careTeam.sequence", result);
        Unique(claim.Diagnosis.Select(d => d.Sequence), "BV-00454", "Claim.diagnosis.sequence", result);
        Unique(claim.SupportingInfo.Select(s => s.Sequence), "BV-00453", "Claim.supportingInfo.sequence", result);
        Unique(claim.Item.Select(i => i.Sequence), "BV-00044", "Claim.item.sequence", result);

        var careTeam = claim.CareTeam.Select(c => c.Sequence).ToHashSet();
        var diagnoses = claim.Diagnosis.Select(d => d.Sequence).ToHashSet();
        var information = claim.SupportingInfo.Select(s => s.Sequence).ToHashSet();

        if (inBatch && claim.Extension.All(e => e.Url != NphiesExtensions.BatchIdentifier))
            Error(result, "NPHIES-BATCHID", "Claims in a batch SHALL carry the batch-identifier extension.", "Claim.extension");

        decimal total = 0;
        var totalKnown = true;
        foreach (var item in claim.Item)
        {
            var itemPath = $"Claim.item[{item.Sequence}]";
            foreach (var s in item.CareTeamSequence.Where(s => !careTeam.Contains(s)))
                Error(result, "BV-00442", $"careTeamSequence {s} does not match a Claim.careTeam.sequence.", $"{itemPath}.careTeamSequence");
            foreach (var s in item.DiagnosisSequence.Where(s => !diagnoses.Contains(s)))
                Error(result, "BV-00444", $"diagnosisSequence {s} does not match a Claim.diagnosis.sequence.", $"{itemPath}.diagnosisSequence");
            foreach (var s in item.InformationSequence.Where(s => !information.Contains(s)))
                Error(result, "BV-00443", $"informationSequence {s} does not match a Claim.supportingInfo.sequence.", $"{itemPath}.informationSequence");
            if (item.InformationSequence.Count() != item.InformationSequence.Distinct().Count())
                Error(result, "BV-00502", "Claim.item.informationSequence SHALL be unique within the Claim.item.", $"{itemPath}.informationSequence");

            if (item.ProductOrService?.Coding.Count is null or 0)
                Error(result, "IC-00044", "Claim.item.productOrService is required.", $"{itemPath}.productOrService");
            if (use == "claim" && item.Serviced is null)
                Error(result, "BV-00061", "Claim.item.serviced[x] is required when Claim.use = claim.", $"{itemPath}.serviced");

            if (item.Net?.Currency is { } netCurrency && netCurrency != Money.Currencies.SAR)
                Error(result, "BV-00093", "Claim.item.net.currency SHALL be SAR.", $"{itemPath}.net.currency");

            if (item.Net?.Value is { } net)
            {
                total += net;
                var tax = ExtensionMoney(item, NphiesExtensions.Tax) ?? 0m;
                if (item.Quantity?.Value is { } quantity && item.UnitPrice?.Value is { } price)
                {
                    var expected = quantity * price * (item.Factor ?? 1m) + tax;
                    if (Math.Abs(expected - net) > MoneyTolerance)
                        Warning(result, "BV-00251", $"Claim.item.net {net} != (quantity * unitPrice * factor) + tax = {expected}.", $"{itemPath}.net");
                }
            }
            else
            {
                totalKnown = false;
                Error(result, "IC-00043", "Claim.item.net is required.", $"{itemPath}.net");
            }
        }

        if (claim.Total?.Currency is { } totalCurrency && totalCurrency != Money.Currencies.SAR)
            Error(result, "BV-00096", "Claim.total.currency SHALL be SAR.", "Claim.total.currency");
        if (claim.Total?.Value is null)
            Error(result, "IC-00062", "Claim.total is required.", "Claim.total");
        else if (totalKnown && Math.Abs(claim.Total.Value.Value - total) > MoneyTolerance)
            Error(result, "BV-00059", $"Claim.total {claim.Total.Value} SHALL equal the sum of Claim.item.net ({total}).", "Claim.total");
    }

    private void ValidatePoll(FhirTask task, NphiesValidationResult result)
    {
        var periods = task.Input.Count(i => i.Value is Period);
        if (periods > 1)
            Error(result, "BV-00216", "Task.input SHALL contain only one period element.", "Task.input");

        foreach (var input in task.Input.Where(i => i.Type?.Coding.FirstOrDefault()?.Code == "count"))
        {
            if (input.Value is PositiveInt { Value: { } count } && count > _pollMaxCount)
                Error(result, "NPHIES-POLLCOUNT", $"Poll count {count} exceeds the NPHIES maximum of {_pollMaxCount}.", "Task.input.valuePositiveInt");
            if (input.Value is Integer { Value: { } intCount } && intCount > _pollMaxCount)
                Error(result, "NPHIES-POLLCOUNT", $"Poll count {intCount} exceeds the NPHIES maximum of {_pollMaxCount}.", "Task.input.valueInteger");
        }
    }

    private void ValidateBatch(Bundle bundle, MessageHeader header, List<Resource?> focus, NphiesValidationResult result)
    {
        var inner = bundle.Entry.Select(e => e.Resource).OfType<Bundle>().ToList();
        if (inner.Count == 0)
            Error(result, "BV-00237", "A batch-request SHALL contain at least one claim bundle.", "Bundle.entry");
        if (inner.Count > _batchMaxClaims)
            Error(result, "BV-00373", $"A batch-request SHALL contain at most {_batchMaxClaims} claims (found {inner.Count}).", "Bundle.entry");
        if (focus.Count != inner.Count)
            Error(result, "BV-00221", "Batch MessageHeader.focus SHALL reference every claim bundle in the batch.", "MessageHeader.focus");

        var sender = header.Sender?.Identifier?.Value;
        var receiver = header.Destination.FirstOrDefault()?.Receiver?.Identifier?.Value;
        for (var i = 0; i < inner.Count; i++)
        {
            var path = $"Bundle.entry[{bundle.Entry.FindIndex(e => ReferenceEquals(e.Resource, inner[i]))}].resource";
            ValidateMessage(inner[i], result, path, isInnerBatchMessage: true);
            var innerHeader = inner[i].Entry.FirstOrDefault()?.Resource as MessageHeader;
            if (innerHeader?.Sender?.Identifier?.Value != sender)
                Error(result, "BV-00227", "Claim MessageHeader.sender SHALL match the batch MessageHeader.sender.", $"{path}.entry[0].resource.sender");
            if (innerHeader?.Destination.FirstOrDefault()?.Receiver?.Identifier?.Value != receiver)
                Error(result, "BV-00238", "Claim MessageHeader.destination.receiver SHALL match the batch MessageHeader.destination.receiver.", $"{path}.entry[0].resource.destination");
        }
    }

    #region Helpers

    private static void ValidateProfile(Resource resource, NphiesValidationResult result, string path)
    {
        var profiles = resource.Meta?.Profile.ToList() ?? new List<string>();
        if (profiles.Count == 0)
        {
            Error(result, "BV-00225", $"{resource.TypeName}.meta.profile is required.", $"{path}.meta.profile");
            return;
        }

        foreach (var profile in profiles)
        {
            if (!profile.StartsWith(NphiesProfiles.Base, StringComparison.Ordinal) || !profile.Contains('|'))
                Error(result, "BV-00225", $"Profile '{profile}' SHALL be an NPHIES StructureDefinition URL including the version (|{NphiesProfiles.Version}).", $"{path}.meta.profile");
        }
    }

    private static void Unique(IEnumerable<int?> values, string ruleId, string element, NphiesValidationResult result)
    {
        foreach (var duplicate in values.Where(v => v.HasValue).GroupBy(v => v).Where(g => g.Count() > 1))
            Error(result, ruleId, $"{element} {duplicate.Key} SHALL be unique within the claim.", element);
        if (values.Any(v => v is null))
            Error(result, ruleId, $"{element} is required.", element);
    }

    private static Resource? Resolve(Bundle bundle, string reference)
    {
        var entry = bundle.Entry.FirstOrDefault(e => string.Equals(e.FullUrl, reference, StringComparison.Ordinal));
        if (entry != null) return entry.Resource;

        var parts = reference.TrimEnd('/').Split('/');
        if (parts.Length < 2) return null;
        var (type, id) = (parts[^2], parts[^1]);
        return bundle.Entry.Select(e => e.Resource).FirstOrDefault(r => r != null && r.TypeName == type && r.Id == id);
    }

    private static decimal? ExtensionMoney(IExtendable element, string url) =>
        (element.Extension.FirstOrDefault(e => e.Url == url)?.Value as Money)?.Value;

    private static void Error(NphiesValidationResult result, string ruleId, string message, string? expression) =>
        result.Issues.Add(new NphiesValidationIssue(ruleId, NphiesValidationSeverity.Error, message, expression));

    private static void Warning(NphiesValidationResult result, string ruleId, string message, string? expression) =>
        result.Issues.Add(new NphiesValidationIssue(ruleId, NphiesValidationSeverity.Warning, message, expression));

    #endregion
}
