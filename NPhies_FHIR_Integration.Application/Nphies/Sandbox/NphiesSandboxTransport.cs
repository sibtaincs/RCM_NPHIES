using System.Collections.Concurrent;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Infrastructure.Nphies;
using FhirTask = Hl7.Fhir.Model.Task;

namespace NPhies_FHIR_Integration.Application.Nphies.Sandbox;

public sealed class NphiesSandboxOptions
{
    public const string SectionName = "Nphies:Sandbox";

    /// <summary>When true every outbound message is answered by <see cref="NphiesSandboxTransport"/> instead of NPHIES.</summary>
    public bool Enabled { get; set; }
}

/// <summary>
/// Routes messages to the in-process NPHIES simulator when <c>Nphies:Sandbox:Enabled</c> is set (or inside a
/// <see cref="NphiesSandboxTransport.Activate"/> scope); otherwise to the real mTLS transport.
/// </summary>
public sealed class NphiesTransportRouter : INphiesTransport
{
    private readonly IServiceProvider _services;
    private readonly NphiesSandboxTransport _sandbox;
    private readonly NphiesSandboxOptions _options;

    public NphiesTransportRouter(IServiceProvider services, NphiesSandboxTransport sandbox, IOptions<NphiesSandboxOptions> options)
    {
        _services = services;
        _sandbox = sandbox;
        _options = options.Value;
    }

    public Task<NphiesTransportResponse> SendAsync(NphiesTransportRequest request, CancellationToken cancellationToken = default) =>
        _options.Enabled || NphiesSandboxTransport.IsActive
            ? _sandbox.SendAsync(request, cancellationToken)
            : _services.GetRequiredService<NphiesTransport>().SendAsync(request, cancellationToken);
}

/// <summary>
/// Deterministic in-process NPHIES simulator used for development, demos and seeding. It answers each message the
/// way the HSB does (see the IG examples 1b/3b/7b/12b/13a/18a): eligibility responses, adjudicated or queued
/// ClaimResponses, Task responses for status-check/cancel, acknowledgements, and a per-provider poll queue holding
/// deferred ClaimResponses, PaymentReconciliations and CommunicationRequests.
/// </summary>
public sealed class NphiesSandboxTransport : INphiesTransport
{
    private const string PayerBase = "http://sandbox-payer.nphies.local";
    private const string Endpoint = "sandbox://nphies/$process-message";

    private static readonly AsyncLocal<int> ActiveScopes = new();
#pragma warning disable CS0618
    private static readonly FhirJsonParser Parser = new(new ParserSettings { PermissiveParsing = true, AcceptUnknownMembers = true, AllowUnrecognizedEnums = true });
#pragma warning restore CS0618
    private static readonly FhirJsonSerializer Serializer = new();

    private static readonly (string Code, string Display)[] DenialReasons =
    {
        ("MN-1-1", "Service is not clinically justified based on clinical practice guideline"),
        ("BE-1-4", "Service is not covered under the policy benefits"),
        ("CV-1-3", "Diagnosis is inconsistent with the service"),
        ("AD-1-4", "Prior authorization was required but not obtained"),
        ("SE-1-6", "Invalid or inconsistent service code"),
    };

    private static readonly string[] InformationRequests =
    {
        "Please provide the latest medical report supporting the requested services.",
        "Please attach the laboratory results (HbA1c, LFT and lipid profile) referenced in the claim.",
        "Please provide the operative notes and discharge summary for this encounter.",
    };

    private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _queues = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Routes messages on the current async flow to the simulator until disposed (used by the sample seeder).</summary>
    public static IDisposable Activate()
    {
        ActiveScopes.Value++;
        return new Scope();
    }

    public static bool IsActive => ActiveScopes.Value > 0;

    /// <summary>Number of deferred messages waiting for <paramref name="providerLicense"/>'s next poll.</summary>
    public int Pending(string providerLicense) => _queues.TryGetValue(providerLicense, out var q) ? q.Count : 0;

    public Task<NphiesTransportResponse> SendAsync(NphiesTransportRequest request, CancellationToken cancellationToken = default)
    {
        var bundle = (Bundle)Parser.Parse<Resource>(request.Json);
        var header = bundle.Entry.Select(e => e.Resource).OfType<MessageHeader>().FirstOrDefault()
                     ?? throw new InvalidOperationException("Request bundle has no MessageHeader.");
        var ctx = new Context(request, header);

        Bundle reply = request.EventCode switch
        {
            NphiesMessageEvents.EligibilityRequest => Eligibility(ctx, bundle),
            NphiesMessageEvents.PriorAuthRequest or NphiesMessageEvents.ClaimRequest => Adjudicate(ctx, bundle),
            NphiesMessageEvents.BatchRequest => Batch(ctx, bundle),
            NphiesMessageEvents.StatusCheck or NphiesMessageEvents.CancelRequest => TaskResponse(ctx, bundle),
            NphiesMessageEvents.PollRequest => Poll(ctx, bundle),
            _ => Message(ctx, NphiesMessageEvents.Acknowledgement, null, ctx.Header.Id),
        };
        return System.Threading.Tasks.Task.FromResult(
            new NphiesTransportResponse(HttpStatusCode.OK, Serializer.SerializeToString(reply), Endpoint, TimeSpan.FromMilliseconds(40)));
    }

    // ============================== Eligibility ==============================

    private static Bundle Eligibility(Context ctx, Bundle request)
    {
        var req = request.Entry.Select(e => e.Resource).OfType<CoverageEligibilityRequest>().First();
        var key = req.Identifier.FirstOrDefault()?.Value ?? req.Id ?? string.Empty;
        var eligible = Hash(req.Patient?.Reference ?? key) % 10 != 9;
        var coverageRef = req.Insurance.FirstOrDefault()?.Coverage?.Reference;
        var today = DateTime.UtcNow.Date;

        var response = new CoverageEligibilityResponse
        {
            Id = Guid.NewGuid().ToString(),
            Meta = Profile(NphiesProfiles.Base + "eligibility-response"),
            Status = FinancialResourceStatusCodes.Active,
            Purpose = req.Purpose.Select(p => (CoverageEligibilityResponse.EligibilityResponsePurpose?)(int?)p).ToList(),
            Patient = req.Patient,
            Serviced = req.Serviced,
            Created = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            Request = new ResourceReference { Identifier = req.Identifier.FirstOrDefault() },
            Outcome = ClaimProcessingCodes.Complete,
            Disposition = eligible ? "Coverage is in force; benefits returned." : "Member coverage is not in force for the requested date.",
            Insurer = req.Insurer,
        };
        response.Identifier.Add(new Identifier(PayerBase + "/coverageeligibilityresponse", "EligResp_" + Hash(key)));
        response.Extension.Add(new Extension(NphiesExtensions.SiteEligibility,
            new CodeableConcept(NphiesCodeSystems.SiteEligibility, eligible ? "eligible" : "not-eligible")));

        var insurance = new CoverageEligibilityResponse.InsuranceComponent
        {
            Coverage = new ResourceReference(coverageRef ?? "Coverage/unknown"),
            Inforce = eligible,
            BenefitPeriod = new Period { Start = today.AddMonths(-6).ToString("yyyy-MM-dd"), End = today.AddMonths(6).ToString("yyyy-MM-dd") },
        };
        if (!eligible)
        {
            insurance.Extension.Add(new Extension(NphiesExtensions.NotInForceReason, new CodeableConcept(NphiesCodeSystems.Adjudication, "expired")));
        }
        else
        {
            foreach (var (category, name, limit, used) in new[]
                     {
                         ("1", "Medical Care", 250000m, 12500m),
                         ("2", "Surgical", 150000m, 0m),
                         ("3", "Consultation", 5000m, 900m),
                         ("17", "Pharmacy", 20000m, 1750m),
                     })
            {
                var item = new CoverageEligibilityResponse.ItemsComponent
                {
                    Category = new CodeableConcept(NphiesCodeSystems.BenefitCategory, category),
                    Name = name,
                    Network = new CodeableConcept(NphiesCodeSystems.BenefitNetwork, "in"),
                    Unit = new CodeableConcept(NphiesCodeSystems.BenefitUnit, "individual"),
                    Term = new CodeableConcept(NphiesCodeSystems.BenefitTerm, "annual"),
                };
                item.Benefit.Add(new CoverageEligibilityResponse.BenefitComponent
                {
                    Type = new CodeableConcept(NphiesCodeSystems.BenefitType, "benefit"),
                    Allowed = Sar(limit),
                    Used = Sar(used),
                });
                item.Benefit.Add(new CoverageEligibilityResponse.BenefitComponent
                {
                    Type = new CodeableConcept(NphiesCodeSystems.BenefitType, "copay-percent"),
                    Allowed = new UnsignedInt(10),
                });
                insurance.Item.Add(item);
            }
        }
        response.Insurance.Add(insurance);
        return Message(ctx, NphiesMessageEvents.EligibilityResponse, response, ctx.Header.Id);
    }

    // ============================== Claims / prior authorizations ==============================

    private Bundle Adjudicate(Context ctx, Bundle request)
    {
        var claim = request.Entry.Select(e => e.Resource).OfType<Claim>().First();
        var decision = Decide(claim);
        var responseEvent = claim.Use == ClaimUseCode.Preauthorization ? NphiesMessageEvents.PriorAuthResponse : NphiesMessageEvents.ClaimResponse;

        if (decision != Decision.Queued)
        {
            var final = BuildClaimResponse(claim, decision);
            EnqueueFollowUps(ctx, claim, final);
            return Message(ctx, responseEvent, final, ctx.Header.Id);
        }

        // NPHIES accepted the message but the payer adjudicates later: reply "queued" now, deliver via poll.
        var deferred = BuildClaimResponse(claim, Hash(Key(claim)) % 3 == 0 ? Decision.Partial : Decision.Approved);
        Enqueue(ctx.ProviderLicense, Message(ctx, responseEvent, deferred, ctx.Header.Id));
        EnqueueFollowUps(ctx, claim, deferred);
        return Message(ctx, responseEvent, BuildQueuedResponse(claim), ctx.Header.Id);
    }

    private Bundle Batch(Context ctx, Bundle request)
    {
        foreach (var inner in request.Entry.Select(e => e.Resource).OfType<Bundle>())
        {
            var claim = inner.Entry.Select(e => e.Resource).OfType<Claim>().FirstOrDefault();
            if (claim is null) continue;
            var innerHeader = inner.Entry.Select(e => e.Resource).OfType<MessageHeader>().FirstOrDefault();
            var decision = Decide(claim) == Decision.Queued ? Decision.Approved : Decide(claim);
            var response = BuildClaimResponse(claim, decision);
            Enqueue(ctx.ProviderLicense, Message(ctx, NphiesMessageEvents.ClaimResponse, response, innerHeader?.Id ?? ctx.Header.Id));
            EnqueueFollowUps(ctx, claim, response);
        }
        return Message(ctx, NphiesMessageEvents.Acknowledgement, null, ctx.Header.Id);
    }

    private enum Decision { Approved, Partial, Rejected, Queued }

    private static Decision Decide(Claim claim)
    {
        // Resubmissions/appeals/updates (Claim.related) are approved so the appeal flow can be demonstrated end to end.
        if (claim.Related.Count > 0) return Decision.Approved;
        return (Hash(Key(claim)) % 10) switch
        {
            <= 5 => Decision.Approved,
            6 or 7 => Decision.Partial,
            8 => Decision.Rejected,
            _ => Decision.Queued,
        };
    }

    private static ClaimResponse BuildQueuedResponse(Claim claim)
    {
        var response = NewClaimResponse(claim, "queued");
        response.Outcome = ClaimProcessingCodes.Queued;
        response.Disposition = "Message received; adjudication is in progress.";
        return response;
    }

    private static ClaimResponse BuildClaimResponse(Claim claim, Decision decision)
    {
        var h = Hash(Key(claim));
        var response = NewClaimResponse(claim, "final");
        var deniedSequence = decision == Decision.Partial && claim.Item.Count > 1 ? claim.Item.Last().Sequence : null;
        var reason = DenialReasons[h % DenialReasons.Length];
        var totals = new Dictionary<string, decimal>();

        foreach (var item in claim.Item)
        {
            var net = item.Net?.Value ?? (item.UnitPrice?.Value ?? 0m) * (item.Quantity?.Value ?? 1m);
            var tax = ItemMoney(item, NphiesExtensions.Tax) ?? 0m;
            var denied = decision == Decision.Rejected || (deniedSequence.HasValue && item.Sequence == deniedSequence);
            var reduced = decision == Decision.Partial && deniedSequence is null; // single-item claim: 70% allowed.

            var eligibleBase = Math.Max(0m, net - tax);
            var eligible = denied ? 0m : reduced ? Math.Round(eligibleBase * 0.7m, 2) : eligibleBase;
            var share = ItemMoney(item, NphiesExtensions.PatientShare);
            var copay = denied ? 0m : share is { } s && s <= eligible ? s : Math.Round(eligible * 0.1m, 2);
            var benefit = eligible - copay;
            var itemTax = denied ? 0m : tax;

            var ri = new ClaimResponse.ItemComponent { ItemSequence = item.Sequence };
            ri.Extension.Add(new Extension(NphiesExtensions.AdjudicationOutcome,
                new CodeableConcept(NphiesCodeSystems.AdjudicationOutcome, denied ? "rejected" : "approved")));
            ri.Adjudication.Add(Adj("submitted", net, denied || reduced ? reason.Code : null, reason.Display));
            ri.Adjudication.Add(Adj("eligible", eligible));
            ri.Adjudication.Add(Adj("benefit", benefit));
            ri.Adjudication.Add(Adj("copay", copay));
            ri.Adjudication.Add(Adj("tax", itemTax));
            ri.Adjudication.Add(Adj("patientShare", copay));
            ri.Adjudication.Add(new ClaimResponse.AdjudicationComponent
            {
                Category = new CodeableConcept(NphiesCodeSystems.KsaAdjudication, "approved-quantity"),
                Value = denied ? 0m : item.Quantity?.Value ?? 1m,
            });
            response.Item.Add(ri);

            Add(totals, "submitted", net);
            Add(totals, "eligible", eligible);
            Add(totals, "benefit", benefit);
            Add(totals, "copay", copay);
            Add(totals, "tax", itemTax);
            Add(totals, "patientShare", copay);
        }

        var outcome = decision switch { Decision.Rejected => "rejected", Decision.Partial => "partial", _ => "approved" };
        response.Extension.Add(new Extension(NphiesExtensions.AdjudicationOutcome, new CodeableConcept(NphiesCodeSystems.AdjudicationOutcome, outcome)));
        response.Outcome = ClaimProcessingCodes.Complete;
        response.Disposition = decision switch
        {
            Decision.Rejected => $"Claim rejected: {reason.Display}.",
            Decision.Partial => $"Partially approved: {reason.Display}.",
            _ => "All services approved.",
        };
        foreach (var (category, amount) in totals)
            response.Total.Add(new ClaimResponse.TotalComponent { Category = Category(category), Amount = Sar(amount) });

        if (claim.Use == ClaimUseCode.Preauthorization && decision != Decision.Rejected)
        {
            response.PreAuthRef = $"PA-{h % 1000000:000000}";
            response.PreAuthPeriod = new Period
            {
                Start = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                End = DateTime.UtcNow.Date.AddDays(90).ToString("yyyy-MM-dd"),
            };
        }
        return response;
    }

    private static ClaimResponse NewClaimResponse(Claim claim, string suffix)
    {
        var response = new ClaimResponse
        {
            Id = Guid.NewGuid().ToString(),
            Meta = Profile(NphiesProfiles.Base + (claim.Use == ClaimUseCode.Preauthorization ? "priorauth-response" : "claim-response")),
            Status = FinancialResourceStatusCodes.Active,
            Type = claim.Type,
            SubType = claim.SubType,
            Use = claim.Use,
            Patient = claim.Patient,
            Created = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            Insurer = claim.Insurer,
            Requestor = claim.Provider,
            Request = new ResourceReference { Identifier = claim.Identifier.FirstOrDefault() },
            PayeeType = new CodeableConcept(NphiesCodeSystems.PayeeType, "provider"),
        };
        response.Identifier.Add(new Identifier(PayerBase + "/claimresponse", $"resp_{Hash(Key(claim))}_{suffix}"));
        return response;
    }

    /// <summary>Queues the payer's follow-up messages: a payment for payable claims and occasional information requests.</summary>
    private void EnqueueFollowUps(Context ctx, Claim claim, ClaimResponse response)
    {
        var h = Hash(Key(claim));
        var benefit = Total(response, "benefit");
        var tax = Total(response, "tax");

        if (claim.Use == ClaimUseCode.Claim && benefit > 0 && h % 4 != 3)
            Enqueue(ctx.ProviderLicense, Message(ctx, NphiesMessageEvents.PaymentReconciliation,
                BuildPayment(claim, response, benefit + tax, underpay: h % 7 == 0), null));

        if (h % 5 == 2)
            Enqueue(ctx.ProviderLicense, Message(ctx, NphiesMessageEvents.CommunicationRequest, BuildCommunicationRequest(claim), null));
    }

    private static PaymentReconciliation BuildPayment(Claim claim, ClaimResponse response, decimal payable, bool underpay)
    {
        var component = underpay ? Math.Round(payable * 0.95m, 2) : payable;
        var nphiesFee = -Math.Round(component * 0.01m, 2);
        var amount = component + nphiesFee;
        var today = DateTime.UtcNow.Date;
        var key = Hash(Key(claim));

        var pr = new PaymentReconciliation
        {
            Id = Guid.NewGuid().ToString(),
            Meta = Profile(NphiesProfiles.Base + "payment-reconciliation"),
            Status = FinancialResourceStatusCodes.Active,
            Period = new Period { Start = today.AddDays(-30).ToString("yyyy-MM-dd"), End = today.ToString("yyyy-MM-dd") },
            Created = today.ToString("yyyy-MM-dd"),
            PaymentIssuer = claim.Insurer,
            Requestor = claim.Provider,
            Outcome = ClaimProcessingCodes.Complete,
            Disposition = underpay ? "Payment issued after contractual adjustment." : "Payment issued in full.",
            PaymentDate = today.ToString("yyyy-MM-dd"),
            PaymentAmount = Sar(amount),
            PaymentIdentifier = new Identifier("http://sandbox-bank.sa/payment", $"URN-{key:0000000000}")
            {
                Type = new CodeableConcept(NphiesCodeSystems.PaymentMethod, "eft"),
            },
        };
        pr.Identifier.Add(new Identifier(PayerBase + "/paymentreconciliation", $"PR_{key}"));
        var detail = new PaymentReconciliation.DetailsComponent
        {
            Type = new CodeableConcept(NphiesCodeSystems.PaymentType, "payment"),
            Request = new ResourceReference { Type = "Claim", Identifier = claim.Identifier.FirstOrDefault() },
            Response = new ResourceReference { Type = "ClaimResponse", Identifier = response.Identifier.FirstOrDefault() },
            Date = today.ToString("yyyy-MM-dd"),
            Amount = Sar(amount),
        };
        detail.Extension.Add(new Extension(NphiesExtensions.ComponentPayment, Sar(component)));
        detail.Extension.Add(new Extension(NphiesExtensions.ComponentEarlyFee, Sar(0m)));
        detail.Extension.Add(new Extension(NphiesExtensions.ComponentNphiesFee, Sar(nphiesFee)));
        pr.Detail.Add(detail);
        return pr;
    }

    private static CommunicationRequest BuildCommunicationRequest(Claim claim)
    {
        var h = Hash(Key(claim));
        var request = new CommunicationRequest
        {
            Id = Guid.NewGuid().ToString(),
            Meta = Profile(NphiesProfiles.Base + "communication-request"),
            Status = RequestStatus.Active,
            Priority = RequestPriority.Routine,
            Subject = claim.Patient,
            Sender = claim.Insurer,
            AuthoredOn = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
        };
        request.Identifier.Add(new Identifier(PayerBase + "/communicationrequest", $"CommReq_{h}"));
        request.Category.Add(new CodeableConcept(NphiesCodeSystems.CommunicationCategory, "instruction"));
        request.About.Add(new ResourceReference { Type = "Claim", Identifier = claim.Identifier.FirstOrDefault() });
        request.Payload.Add(new CommunicationRequest.PayloadComponent { Content = new FhirString(InformationRequests[h % InformationRequests.Length]) });
        if (claim.Provider != null) request.Recipient.Add(claim.Provider);
        request.ReasonCode.Add(new CodeableConcept(NphiesCodeSystems.CommunicationReason, "Missing-info"));
        return request;
    }

    // ============================== Task (status-check / cancel) / poll ==============================

    private Bundle TaskResponse(Context ctx, Bundle request)
    {
        var task = request.Entry.Select(e => e.Resource).OfType<FhirTask>().First();
        var cancel = ctx.Request.EventCode == NphiesMessageEvents.CancelRequest;
        var focusValue = task.Focus?.Identifier?.Value;
        var queued = !cancel && focusValue != null && _queues.TryGetValue(ctx.ProviderLicense, out var q)
                     && q.Any(json => json.Contains(focusValue, StringComparison.Ordinal) && json.Contains("\"ClaimResponse\"", StringComparison.Ordinal));

        var response = new FhirTask
        {
            Id = Guid.NewGuid().ToString(),
            Meta = Profile(NphiesProfiles.Base + "task-response"),
            Status = FhirTask.TaskStatus.Completed,
            Intent = FhirTask.TaskIntent.Order,
            Priority = RequestPriority.Routine,
            Code = task.Code,
            Focus = task.Focus,
            AuthoredOn = task.AuthoredOn,
            LastModified = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            Requester = task.Requester,
            Owner = task.Owner,
        };
        response.Identifier.Add(new Identifier(PayerBase + "/task", "TaskResp_" + Hash(task.Identifier.FirstOrDefault()?.Value ?? task.Id ?? string.Empty)));
        if (!cancel)
            response.Output.Add(new FhirTask.OutputComponent
            {
                Type = new CodeableConcept(NphiesCodeSystems.TaskOutputType, "status"),
                Value = new Code(queued ? "queued" : "complete"),
            });
        return Message(ctx, cancel ? NphiesMessageEvents.CancelResponse : NphiesMessageEvents.StatusResponse, response, ctx.Header.Id);
    }

    private Bundle Poll(Context ctx, Bundle request)
    {
        var task = request.Entry.Select(e => e.Resource).OfType<FhirTask>().FirstOrDefault();
        var count = task?.Input.FirstOrDefault(i => i.Type?.Coding.Any(c => c.Code == "count") == true)?.Value switch
        {
            PositiveInt p => p.Value ?? 100,
            UnsignedInt u => u.Value ?? 100,
            Integer i => i.Value ?? 100,
            _ => 100,
        };

        FhirTask? pollTask = null;
        if (task != null)
        {
            pollTask = new FhirTask
            {
                Id = Guid.NewGuid().ToString(),
                Meta = Profile(NphiesProfiles.Base + "task"),
                Status = FhirTask.TaskStatus.Completed,
                Intent = FhirTask.TaskIntent.Order,
                Priority = RequestPriority.Stat,
                Code = task.Code,
                AuthoredOn = task.AuthoredOn,
                LastModified = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
                Requester = task.Requester,
                Owner = task.Owner,
            };
            pollTask.Identifier.AddRange(task.Identifier);
        }
        var reply = Message(ctx, NphiesMessageEvents.PollResponse, pollTask, ctx.Header.Id);

        if (_queues.TryGetValue(ctx.ProviderLicense, out var queue))
        {
            for (var i = 0; i < count && queue.TryDequeue(out var json); i++)
            {
                var inner = (Bundle)Parser.Parse<Resource>(json);
                reply.Entry.Add(new Bundle.EntryComponent { FullUrl = "urn:uuid:" + inner.Id, Resource = inner });
            }
        }
        return reply;
    }

    // ============================== helpers ==============================

    private void Enqueue(string providerLicense, Bundle message) =>
        _queues.GetOrAdd(providerLicense, _ => new ConcurrentQueue<string>()).Enqueue(Serializer.SerializeToString(message));

    private static Bundle Message(Context ctx, string eventCode, Resource? focus, string? respondingTo)
    {
        var header = new MessageHeader
        {
            Id = Guid.NewGuid().ToString(),
            Meta = Profile(NphiesProfiles.MessageHeader),
            Event = new Coding(NphiesCodeSystems.MessageEvents, eventCode),
            Sender = new ResourceReference
            {
                Type = "Organization",
                Identifier = ctx.PayerLicense is null
                    ? new Identifier(NphiesIdentifierSystems.Nphies, NphiesIdentifierSystems.NphiesValue)
                    : new Identifier(NphiesIdentifierSystems.PayerLicense, ctx.PayerLicense),
            },
            Source = new MessageHeader.MessageSourceComponent { Endpoint = ctx.PayerLicense is null ? NphiesIdentifierSystems.NphiesEndpoint : PayerBase },
        };
        header.Destination.Add(new MessageHeader.MessageDestinationComponent
        {
            Endpoint = ctx.ProviderEndpoint,
            Receiver = new ResourceReference { Type = "Organization", Identifier = new Identifier(NphiesIdentifierSystems.ProviderLicense, ctx.ProviderLicense) },
        });
        if (respondingTo != null)
            header.Response = new MessageHeader.ResponseComponent { Identifier = respondingTo, Code = MessageHeader.ResponseType.Ok };

        var bundle = new Bundle
        {
            Id = Guid.NewGuid().ToString(),
            Meta = Profile(NphiesProfiles.Bundle),
            Type = Bundle.BundleType.Message,
            Timestamp = DateTimeOffset.UtcNow,
        };
        bundle.Entry.Add(new Bundle.EntryComponent { FullUrl = "urn:uuid:" + header.Id, Resource = header });
        if (focus != null)
        {
            var url = "urn:uuid:" + focus.Id;
            header.Focus.Add(new ResourceReference(url));
            bundle.Entry.Add(new Bundle.EntryComponent { FullUrl = url, Resource = focus });
        }
        return bundle;
    }

    private static ClaimResponse.AdjudicationComponent Adj(string category, decimal amount, string? reason = null, string? display = null)
    {
        var a = new ClaimResponse.AdjudicationComponent { Category = Category(category), Amount = Sar(amount) };
        if (reason != null) a.Reason = new CodeableConcept(NphiesCodeSystems.AdjudicationReason, reason, display, null);
        return a;
    }

    private static CodeableConcept Category(string code) => code == "patientShare"
        ? new CodeableConcept(NphiesCodeSystems.KsaAdjudication, code)
        : new CodeableConcept(NphiesCodeSystems.Adjudication, code);

    private static decimal Total(ClaimResponse response, string category) =>
        response.Total.FirstOrDefault(t => t.Category.Coding.Any(c => c.Code == category))?.Amount?.Value ?? 0m;

    private static decimal? ItemMoney(Claim.ItemComponent item, string url) =>
        (item.Extension.FirstOrDefault(e => e.Url == url)?.Value as Money)?.Value;

    private static void Add(Dictionary<string, decimal> totals, string key, decimal amount) =>
        totals[key] = totals.TryGetValue(key, out var v) ? v + amount : amount;

    private static Money Sar(decimal value) => new() { Value = Math.Round(value, 2), Currency = Money.Currencies.SAR };

    private static Meta Profile(string profile) => new() { Profile = new[] { profile } };

    private static string Key(Claim claim) => claim.Identifier.FirstOrDefault()?.Value ?? claim.Id ?? string.Empty;

    /// <summary>Stable (process-independent) FNV-1a hash so the same request always gets the same simulated outcome.</summary>
    private static int Hash(string value)
    {
        unchecked
        {
            var h = 2166136261u;
            foreach (var ch in value) h = (h ^ ch) * 16777619u;
            return (int)(h % int.MaxValue);
        }
    }

    private sealed class Context
    {
        public Context(NphiesTransportRequest request, MessageHeader header)
        {
            Request = request;
            Header = header;
            ProviderLicense = header.Sender?.Identifier?.Value ?? "unknown-provider";
            ProviderEndpoint = header.Source?.Endpoint ?? "http://provider.com";
            var receiver = header.Destination.FirstOrDefault()?.Receiver?.Identifier;
            PayerLicense = receiver?.System == NphiesIdentifierSystems.PayerLicense ? receiver.Value : null;
        }

        public NphiesTransportRequest Request { get; }
        public MessageHeader Header { get; }
        public string ProviderLicense { get; }
        public string ProviderEndpoint { get; }
        public string? PayerLicense { get; }
    }

    private sealed class Scope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ActiveScopes.Value--;
        }
    }
}
