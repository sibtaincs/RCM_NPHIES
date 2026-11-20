using System.Collections.Concurrent;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
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
    /// <summary>The reply to each queueable message, by provider and MessageHeader.id: what a duplicate gets back.</summary>
    private readonly ConcurrentDictionary<string, string> _answered = new(StringComparer.Ordinal);
    /// <summary>The deferred final response of a message that was answered "queued".</summary>
    private readonly ConcurrentDictionary<string, string> _deferred = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, bool> _offlinePayers = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Requests the payer pended ("deferred" priority), by request identifier, until the provider communicates.</summary>
    private readonly ConcurrentDictionary<string, PendedRequest> _pended = new(StringComparer.Ordinal);
    /// <summary>The payer's latest final decision on each claim, by request identifier: what a re-adjudication reopens.</summary>
    private readonly ConcurrentDictionary<string, Adjudicated> _adjudicated = new(StringComparer.Ordinal);
    /// <summary>Request identifiers the provider cancelled.</summary>
    private readonly ConcurrentDictionary<string, bool> _cancelled = new(StringComparer.Ordinal);

    private sealed class Adjudicated
    {
        public Adjudicated(NphiesTransportRequest request, MessageHeader header, Claim claim, Decision decision, ClaimResponse response)
        {
            Request = request;
            Header = header;
            Claim = claim;
            Decision = decision;
            Response = response;
        }

        public NphiesTransportRequest Request { get; }
        public MessageHeader Header { get; }
        public Claim Claim { get; }
        public Decision Decision { get; }
        public ClaimResponse Response { get; set; }
        /// <summary>Services the payer approved on re-adjudication.</summary>
        public HashSet<int> Overturned { get; } = new();
        public int Answers { get; set; }
    }

    /// <summary>
    /// How the payer answers a re-adjudication communication (Claim Re-submission guide): null or "approve" reissues
    /// the ClaimResponse with the named services approved (<c>reissue-reason = adjudication</c>, step 4.1); "uphold"
    /// keeps its decision and explains why in a <c>notification</c> CommunicationRequest (step 4.2). Both arrive by poll.
    /// "none" only acknowledges: the payer is still reviewing (a test can then <see cref="Deliver"/> its own answer).
    /// </summary>
    public string? ReadjudicateAs { get; set; }

    private sealed class PendedRequest
    {
        public PendedRequest(string providerLicense, NphiesTransportRequest request, MessageHeader header, Claim claim, ClaimResponse final, string deferredJson)
        {
            ProviderLicense = providerLicense;
            Request = request;
            Header = header;
            Claim = claim;
            Final = final;
            DeferredJson = deferredJson;
        }

        public string ProviderLicense { get; }
        public NphiesTransportRequest Request { get; }
        public MessageHeader Header { get; }
        public Claim Claim { get; }
        public ClaimResponse Final { get; }
        public string DeferredJson { get; }
        public bool InformationRequested { get; set; }
    }

    /// <summary>
    /// Puts a message the payer initiates (an advanced authorization, a communication request, a reissued response) in
    /// the provider's queue, to be collected by the next poll.
    /// </summary>
    public void Deliver(string providerLicense, string messageJson) =>
        _queues.GetOrAdd(providerLicense, _ => new ConcurrentQueue<string>()).Enqueue(messageJson);

    /// <summary>
    /// A claim or prior authorization with more items than this is a "large claim": NPHIES answers it itself
    /// (tags <c>large-claim</c> and <c>nphies-generated</c>, outcome queued) and the result arrives by poll.
    /// </summary>
    public int LargeClaimItemThreshold { get; set; } = NphiesLimits.LargeClaimItemThreshold;

    /// <summary>
    /// Simulates a payer that is offline or slow: NPHIES answers its claims and prior authorizations itself
    /// (sender NPHIES, tag <c>nphies-generated</c>, outcome queued) and the payer's response arrives by poll.
    /// </summary>
    public void SetPayerOffline(string payerLicense, bool offline = true)
    {
        if (offline) _offlinePayers[payerLicense] = true;
        else _offlinePayers.TryRemove(payerLicense, out _);
    }

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

        // Eligibility, status-check and poll are real time only: NPHIES never stores their answers.
        var queueable = request.EventCode is not (NphiesMessageEvents.EligibilityRequest or NphiesMessageEvents.StatusCheck or NphiesMessageEvents.PollRequest);
        var key = ctx.ProviderLicense + "|" + header.Id;
        if (queueable && _answered.TryGetValue(key, out var stored))
            return Reply(DuplicateAnswer(ctx, key, stored));

        Bundle reply = request.EventCode switch
        {
            NphiesMessageEvents.EligibilityRequest => Eligibility(ctx, bundle),
            NphiesMessageEvents.PriorAuthRequest or NphiesMessageEvents.ClaimRequest => Adjudicate(ctx, bundle),
            NphiesMessageEvents.BatchRequest => Batch(ctx, bundle),
            NphiesMessageEvents.StatusCheck or NphiesMessageEvents.CancelRequest => TaskResponse(ctx, bundle),
            NphiesMessageEvents.PollRequest => Poll(ctx, bundle),
            NphiesMessageEvents.Communication => Communication(ctx, bundle),
            _ => Message(ctx, NphiesMessageEvents.Acknowledgement, null, ctx.Header.Id),
        };

        // Tell the provider when more messages are waiting in its queue, as NPHIES does.
        if (Pending(ctx.ProviderLicense) > 0)
            Tag(reply, NphiesMetaTags.QueuedMessages);

        var json = Serializer.SerializeToString(reply);
        if (queueable) _answered[key] = json;
        return Reply(json);
    }

    private static Task<NphiesTransportResponse> Reply(string json) => System.Threading.Tasks.Task.FromResult(
        new NphiesTransportResponse(HttpStatusCode.OK, json, Endpoint, TimeSpan.FromMilliseconds(40)));

    /// <summary>
    /// A message sent again with the same MessageHeader.id is a duplicate: NPHIES returns the latest response it has
    /// stored. That is the final response once the provider has collected it by poll; until then it is the first
    /// answer again ("queued").
    /// </summary>
    private string DuplicateAnswer(Context ctx, string key, string stored)
    {
        if (!_deferred.TryGetValue(key, out var final) || _pended.Values.Any(p => p.DeferredJson == final)) return stored;
        var stillQueued = _queues.TryGetValue(ctx.ProviderLicense, out var queue) && queue.Contains(final);
        return stillQueued ? stored : final;
    }

    private static void Tag(Bundle message, params string[] codes)
    {
        var header = message.Entry.Select(e => e.Resource).OfType<MessageHeader>().First();
        header.Meta ??= new Meta();
        foreach (var code in codes.Where(c => header.Meta.Tag.All(t => t.Code != c)))
            header.Meta.Tag.Add(new Coding(NphiesCodeSystems.MetaTags, code));
    }

    // ============================== Eligibility ==============================

    private static Bundle Eligibility(Context ctx, Bundle request)
    {
        var req = request.Entry.Select(e => e.Resource).OfType<CoverageEligibilityRequest>().First();
        var key = req.Identifier.FirstOrDefault()?.Value ?? req.Id ?? string.Empty;
        var coverageRef = req.Insurance.FirstOrDefault()?.Coverage?.Reference;
        var today = DateTime.UtcNow.Date;

        // The members the pseudo-payer INS-FHIR knows by Iqama number (Eligibility test cases V4.0); anyone else is
        // eligible nine times in ten, decided by their identity number so the answer never changes between runs.
        var identities = request.Entry.Select(e => e.Resource).OfType<Patient>()
            .SelectMany(p => p.Identifier).Select(i => i.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (identities.Contains(UnknownMemberId))
        {
            var notFound = new CoverageEligibilityResponse
            {
                Id = Guid.NewGuid().ToString(),
                Meta = Profile(NphiesProfiles.Base + "eligibility-response"),
                Status = FinancialResourceStatusCodes.Active,
                Purpose = req.Purpose.Select(p => (CoverageEligibilityResponse.EligibilityResponsePurpose?)(int?)p).ToList(),
                Patient = req.Patient,
                Created = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
                Request = new ResourceReference { Identifier = req.Identifier.FirstOrDefault() },
                Outcome = ClaimProcessingCodes.Error,
                Disposition = $"The entered identity number ({UnknownMemberId}) does not exist in our system.",
                Insurer = req.Insurer,
            };
            notFound.Identifier.Add(new Identifier(PayerBase + "/coverageeligibilityresponse", "EligResp_" + Hash(key)));
            notFound.Error.Add(new CoverageEligibilityResponse.ErrorsComponent
            {
                Code = new CodeableConcept(NphiesCodeSystems.AdjudicationError, "1658", "No member found with the supplied patient credentials", null),
            });
            return Message(ctx, NphiesMessageEvents.EligibilityResponse, notFound, ctx.Header.Id);
        }
        var eligible = identities.Contains(ActiveMemberId)
                       || (!identities.Contains(InactiveMemberId) && Hash(identities.FirstOrDefault() ?? req.Patient?.Reference ?? key) % 10 != 9);

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
            new CodeableConcept(NphiesCodeSystems.SiteEligibility, eligible ? "eligible" : "not-active")));

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
        var reply = Message(ctx, NphiesMessageEvents.EligibilityResponse, response, ctx.Header.Id);

        // Discovery without a coverage: the payer returns every active policy it holds for the member (two here).
        var discovery = req.Purpose.Any(p => p == CoverageEligibilityRequest.EligibilityRequestPurpose.Discovery);
        if (discovery && coverageRef is null && eligible)
        {
            response.Insurance.Clear();
            foreach (var (member, plan) in new[] { ("0000000001", "Insurance Plan A"), ("0000000002", "Insurance Plan B") })
            {
                var coverage = new Coverage
                {
                    Id = Guid.NewGuid().ToString(),
                    Meta = Profile(NphiesProfiles.Base + "coverage"),
                    Status = FinancialResourceStatusCodes.Active,
                    Type = new CodeableConcept(NphiesCodeSystems.CoverageType, "EHCPOL"),
                    Beneficiary = req.Patient,
                    Subscriber = req.Patient,
                    Relationship = new CodeableConcept(NphiesCodeSystems.SubscriberRelationship, "self"),
                    Period = new Period { Start = today.AddMonths(-6).ToString("yyyy-MM-dd"), End = today.AddMonths(6).ToString("yyyy-MM-dd") },
                };
                coverage.Identifier.Add(new Identifier(PayerBase + "/memberid", member));
                if (req.Insurer != null) coverage.Payor.Add(req.Insurer);
                coverage.Class.Add(new Coverage.ClassComponent
                {
                    Type = new CodeableConcept("http://terminology.hl7.org/CodeSystem/coverage-class", "plan"),
                    Value = plan,
                    Name = plan,
                });
                var url = "urn:uuid:" + coverage.Id;
                reply.Entry.Add(new Bundle.EntryComponent { FullUrl = url, Resource = coverage });
                var copy = (CoverageEligibilityResponse.InsuranceComponent)insurance.DeepCopy();
                copy.Coverage = new ResourceReference(url);
                response.Insurance.Add(copy);
            }
        }
        return reply;
    }

    /// <summary>Members of the pseudo-payer INS-FHIR used by the Eligibility onboarding test cases (V4.0).</summary>
    public const string ActiveMemberId = "2555555559";
    public const string InactiveMemberId = "2555555550";
    public const string UnknownMemberId = "2333333333";

    // ============================== Claims / prior authorizations ==============================

    private Bundle Adjudicate(Context ctx, Bundle request)
    {
        var claim = request.Entry.Select(e => e.Resource).OfType<Claim>().First();
        var responseEvent = claim.Use == ClaimUseCode.Preauthorization ? NphiesMessageEvents.PriorAuthResponse : NphiesMessageEvents.ClaimResponse;

        // The payer has no member with this identity (Claims test case 5): an error answer, not adjudicated.
        if (request.Entry.Select(e => e.Resource).OfType<Patient>().SelectMany(p => p.Identifier).Any(i => i.Value == UnknownMemberId))
        {
            var error = NewClaimResponse(claim, "error");
            error.Outcome = ClaimProcessingCodes.Error;
            error.Disposition = $"The entered identity number ({UnknownMemberId}) does not exist in our system.";
            error.Error.Add(new ClaimResponse.ErrorComponent
            {
                Code = new CodeableConcept(NphiesCodeSystems.AdjudicationError, "1658", "No member found with the supplied patient credentials", null),
            });
            return Message(ctx, responseEvent, error, ctx.Header.Id);
        }

        var decision = Decide(claim);

        // NPHIES answers for the payer when the claim is large or the payer is offline; the payer's answer comes by poll.
        var largeClaim = claim.Item.Count > LargeClaimItemThreshold;
        var payerOffline = ctx.PayerLicense != null && _offlinePayers.ContainsKey(ctx.PayerLicense);
        var heldByNphies = largeClaim || payerOffline;

        if (decision != Decision.Queued && !heldByNphies)
        {
            var final = BuildClaimResponse(claim, decision);
            Remember(ctx, claim, decision, final);
            EnqueueFollowUps(ctx, claim, final);
            return Message(ctx, responseEvent, final, ctx.Header.Id);
        }

        // NPHIES accepted the message but the payer adjudicates later: reply "queued" now, deliver via poll.
        var deferredDecision = decision != Decision.Queued ? decision : DecideFinal(claim);
        var deferred = BuildClaimResponse(claim, deferredDecision);
        Remember(ctx, claim, deferredDecision, deferred);
        var deferredMessage = Message(ctx, responseEvent, deferred, ctx.Header.Id);
        var deferredJson = Serializer.SerializeToString(deferredMessage);
        _deferred[ctx.ProviderLicense + "|" + ctx.Header.Id] = deferredJson;
        if (!heldByNphies && Key(claim) is { Length: > 0 } pendedId)
        {
            // Pended by the payer ("deferred" priority): it decides by the next poll, or after the provider's communication
            // when it asks for more information first (AskForInformationWhenPended; Authorization test cases 1, 2, 8, 9).
            _pended[pendedId] = new PendedRequest(ctx.ProviderLicense, ctx.Request, ctx.Header, claim, deferred, deferredJson);
            return Message(ctx, responseEvent, BuildQueuedResponse(claim), ctx.Header.Id);
        }
        Enqueue(ctx.ProviderLicense, deferredMessage);
        EnqueueFollowUps(ctx, claim, deferred);

        var queued = Message(ctx, responseEvent, BuildQueuedResponse(claim, heldByNphies), ctx.Header.Id, fromNphies: heldByNphies);
        if (heldByNphies) Tag(queued, NphiesMetaTags.NphiesGenerated);
        if (largeClaim) Tag(queued, NphiesMetaTags.LargeClaim);
        return queued;
    }

    private Bundle Batch(Context ctx, Bundle request)
    {
        foreach (var inner in request.Entry.Select(e => e.Resource).OfType<Bundle>())
        {
            var claim = inner.Entry.Select(e => e.Resource).OfType<Claim>().FirstOrDefault();
            if (claim is null) continue;
            var innerHeader = inner.Entry.Select(e => e.Resource).OfType<MessageHeader>().FirstOrDefault();
            var decision = DecideFinal(claim);
            var response = BuildClaimResponse(claim, decision);
            Remember(ctx, claim, decision, response);
            // The payer first acknowledges each claim as queued, then sends its decision; both carry the batch extensions.
            var queued = BuildQueuedResponse(claim);
            foreach (var url in new[] { NphiesExtensions.BatchIdentifier, NphiesExtensions.BatchNumber, NphiesExtensions.BatchPeriod })
                if (claim.GetExtension(url) is { } batchExtension)
                {
                    queued.Extension.Add((Extension)batchExtension.DeepCopy());
                    response.Extension.Add((Extension)batchExtension.DeepCopy());
                }
            Enqueue(ctx.ProviderLicense, Message(ctx, NphiesMessageEvents.ClaimResponse, queued, innerHeader?.Id ?? ctx.Header.Id));
            Enqueue(ctx.ProviderLicense, Message(ctx, NphiesMessageEvents.ClaimResponse, response, innerHeader?.Id ?? ctx.Header.Id));
            EnqueueFollowUps(ctx, claim, response);
        }
        return Message(ctx, NphiesMessageEvents.Acknowledgement, null, ctx.Header.Id);
    }

    private enum Decision { Approved, Partial, Rejected, Queued }

    /// <summary>
    /// When set ("approved", "partial" or "rejected"), the payer's final decision on every claim and prior authorization,
    /// for test cases that expect a given outcome (Claims test cases V4.0 #2, #3 and #8).
    /// </summary>
    public string? DecideAs { get; set; }

    private Decision Decide(Claim claim)
    {
        // Priority "deferred" asks for a pended answer: queued now, the decision by poll (Authorization test cases V6.0).
        if (claim.Priority?.Coding.Any(c => c.Code == "deferred") == true) return Decision.Queued;
        return DecideFinal(claim);
    }

    /// <summary>The payer's decision once it adjudicates; never "queued".</summary>
    private Decision DecideFinal(Claim claim)
    {
        switch (DecideAs?.ToLowerInvariant())
        {
            case "approved": return Decision.Approved;
            case "partial": return Decision.Partial;
            case "rejected": return Decision.Rejected;
        }
        // An unlisted drug code is not authorized: that line is rejected and the rest approved (Authorization test case 5).
        if (claim.Use == ClaimUseCode.Preauthorization && claim.Item.Count > 1 && claim.Item.Any(IsUnlistedDrug)) return Decision.Partial;
        // Resubmissions/appeals/updates (Claim.related) are approved so the appeal flow can be demonstrated end to end.
        if (claim.Related.Count > 0) return Decision.Approved;
        return (Hash(Key(claim)) % 10) switch
        {
            <= 5 => Decision.Approved,
            6 or 7 => Decision.Partial,
            8 => Decision.Rejected,
            _ => Hash(Key(claim)) % 3 == 0 ? Decision.Partial : Decision.Approved,
        };
    }

    private static ClaimResponse BuildQueuedResponse(Claim claim, bool heldByNphies = false)
    {
        var response = NewClaimResponse(claim, "queued");
        response.Outcome = ClaimProcessingCodes.Queued;
        response.Disposition = heldByNphies
            ? "Message received by NPHIES and queued for the payer."
            : "Message received; adjudication is in progress.";
        return response;
    }

    /// <param name="overturned">
    /// Services the payer approved in full on re-adjudication; when set, the header outcome follows the items.
    /// </param>
    private static ClaimResponse BuildClaimResponse(Claim claim, Decision decision, ISet<int>? overturned = null, string suffix = "final")
    {
        var h = Hash(Key(claim));
        var response = NewClaimResponse(claim, suffix);
        var deniedSequence = decision == Decision.Partial && claim.Item.Count > 1
            ? (claim.Item.FirstOrDefault(IsUnlistedDrug) ?? claim.Item.Last()).Sequence
            : null;
        var reason = DenialReasons[h % DenialReasons.Length];
        var totals = new Dictionary<string, decimal>();
        int deniedItems = 0, reducedItems = 0;

        foreach (var item in claim.Item)
        {
            var net = item.Net?.Value ?? (item.UnitPrice?.Value ?? 0m) * (item.Quantity?.Value ?? 1m);
            var tax = ItemMoney(item, NphiesExtensions.Tax) ?? 0m;
            var reopened = overturned != null && item.Sequence is { } seq && overturned.Contains(seq);
            var denied = !reopened && (decision == Decision.Rejected || (deniedSequence.HasValue && item.Sequence == deniedSequence));
            var reduced = !reopened && decision == Decision.Partial && deniedSequence is null; // single-item claim: 70% allowed.
            if (denied) deniedItems++;
            if (reduced) reducedItems++;

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

        if (overturned != null)
            decision = deniedItems == claim.Item.Count && claim.Item.Count > 0 ? Decision.Rejected
                : deniedItems + reducedItems > 0 ? Decision.Partial
                : Decision.Approved;
        var outcome = decision switch { Decision.Rejected => "rejected", Decision.Partial => "partial", _ => "approved" };
        response.Extension.Add(new Extension(NphiesExtensions.AdjudicationOutcome, new CodeableConcept(NphiesCodeSystems.AdjudicationOutcome, outcome)));
        response.Outcome = ClaimProcessingCodes.Complete;
        response.Disposition = decision switch
        {
            Decision.Rejected => $"Claim rejected: {reason.Display}.",
            Decision.Partial => $"Partially approved: {reason.Display}.",
            _ => "All services approved.",
        };
        if (overturned is { Count: > 0 })
            response.Disposition = $"Re-adjudicated: service {string.Join(", ", overturned.Order())} approved. {response.Disposition}";
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

    private static PaymentReconciliation BuildPayment(Claim claim, ClaimResponse response, decimal payable, bool underpay, string detailType = "payment", string suffix = "")
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
        pr.Identifier.Add(new Identifier(PayerBase + "/paymentreconciliation", $"PR_{key}{suffix}"));
        var detail = new PaymentReconciliation.DetailsComponent
        {
            Type = new CodeableConcept(NphiesCodeSystems.PaymentType, detailType),
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
        request.ReasonCode.Add(new CodeableConcept(NphiesCodeSystems.CommunicationReason, NphiesCommunicationReasons.MissingInformation));
        return request;
    }

    // ============================== Task (status-check / cancel) / poll ==============================

    private Bundle TaskResponse(Context ctx, Bundle request)
    {
        var task = request.Entry.Select(e => e.Resource).OfType<FhirTask>().First();
        var cancel = ctx.Request.EventCode == NphiesMessageEvents.CancelRequest;
        var focusValue = task.Focus?.Identifier?.Value;
        if (cancel && focusValue != null) _cancelled[focusValue] = true;
        var queued = !cancel && focusValue != null && (_pended.ContainsKey(focusValue) || _queues.TryGetValue(ctx.ProviderLicense, out var q)
                     && q.Any(json => json.Contains(focusValue, StringComparison.Ordinal) && json.Contains("\"ClaimResponse\"", StringComparison.Ordinal)));

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
        ProgressPended(ctx.ProviderLicense);
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
            foreach (var json in Take(queue, count, MessageTypes(task, "include-message-type"), MessageTypes(task, "exclude-message-type")))
            {
                var inner = (Bundle)Parser.Parse<Resource>(json);
                reply.Entry.Add(new Bundle.EntryComponent { FullUrl = "urn:uuid:" + inner.Id, Resource = inner });
            }
        }
        return reply;
    }

    /// <summary>
    /// A communication from the provider. When it is about a request the payer pended, the payer now decides and the
    /// response goes to the provider's queue.
    /// </summary>
    private Bundle Communication(Context ctx, Bundle request)
    {
        var communication = request.Entry.Select(e => e.Resource).OfType<Communication>().FirstOrDefault();
        var about = communication?.About.Select(a => a.Identifier?.Value).FirstOrDefault(v => v != null);
        if (communication != null && communication.ReasonCode.Any(r => r.Coding.Any(c => c.Code == NphiesCommunicationReasons.ReAdjudication)))
            return ReAdjudicate(ctx, communication, about);
        if (about != null && _pended.TryRemove(about, out var pended))
        {
            Deliver(pended.ProviderLicense, pended.DeferredJson);
            EnqueueFollowUps(new Context(pended.Request, pended.Header), pended.Claim, pended.Final);
        }
        return Message(ctx, NphiesMessageEvents.Acknowledgement, null, ctx.Header.Id);
    }

    /// <summary>
    /// A re-adjudication communication (Claim Re-submission guide). NPHIES checks that it is about a claim that was
    /// posted and is active (BV-00148); the payer then reissues its response or explains in a notification why not.
    /// </summary>
    private Bundle ReAdjudicate(Context ctx, Communication communication, string? about)
    {
        if (about is null || _cancelled.ContainsKey(about) || !_adjudicated.TryGetValue(about, out var adjudicated))
            return ErrorAcknowledgement(ctx, "BV-00148", "The provided resource identifier in the about element is invalid");

        if (string.Equals(ReadjudicateAs, "none", StringComparison.OrdinalIgnoreCase))
            return Message(ctx, NphiesMessageEvents.Acknowledgement, null, ctx.Header.Id);
        var named = communication.Payload.Select(NphiesResponseParser.ClaimItemSequence).OfType<int>().ToHashSet();
        if (named.Count == 0) named = adjudicated.Claim.Item.Select(i => i.Sequence).OfType<int>().ToHashSet();
        var original = new Context(adjudicated.Request, adjudicated.Header);

        Bundle answer;
        ClaimResponse? previous = null, reissued = null;
        lock (adjudicated)
        {
            var n = ++adjudicated.Answers;
            if (string.Equals(ReadjudicateAs, "uphold", StringComparison.OrdinalIgnoreCase))
                answer = Message(original, NphiesMessageEvents.CommunicationRequest, BuildReAdjudicationNotice(adjudicated.Claim, named, n), null);
            else
            {
                previous = adjudicated.Response;
                adjudicated.Overturned.UnionWith(named);
                reissued = BuildClaimResponse(adjudicated.Claim, adjudicated.Decision, adjudicated.Overturned, $"reissue{n}");
                reissued.Extension.Add(new Extension(NphiesExtensions.AdjudicationReissue, new CodeableConcept(NphiesCodeSystems.ReissueReason, "adjudication")));
                adjudicated.Response = reissued;
                answer = Message(original, NphiesMessageEvents.ClaimResponse, reissued, adjudicated.Header.Id);
            }
        }
        Enqueue(ctx.ProviderLicense, answer);
        if (previous != null && reissued != null) EnqueueReissuePayment(original, adjudicated.Claim, previous, reissued);
        return Message(ctx, NphiesMessageEvents.Acknowledgement, null, ctx.Header.Id);
    }

    /// <summary>
    /// The payer pays what the reissue added: an adjustment on top of an earlier payment, or a first payment when it had
    /// paid nothing (the same rule as <see cref="EnqueueFollowUps"/>).
    /// </summary>
    private void EnqueueReissuePayment(Context ctx, Claim claim, ClaimResponse previous, ClaimResponse reissued)
    {
        if (claim.Use != ClaimUseCode.Claim || Hash(Key(claim)) % 4 == 3) return;
        var before = Total(previous, "benefit") + Total(previous, "tax");
        var after = Total(reissued, "benefit") + Total(reissued, "tax");
        var paidBefore = Total(previous, "benefit") > 0m;
        var due = after - (paidBefore ? before : 0m);
        if (due <= 0m) return;
        Enqueue(ctx.ProviderLicense, Message(ctx, NphiesMessageEvents.PaymentReconciliation,
            BuildPayment(claim, reissued, due, underpay: false, detailType: paidBefore ? "adjustment" : "payment", suffix: "_" + reissued.Identifier.First().Value), null));
    }

    /// <summary>Step 4.2: the payer keeps its decision on the named services and says why.</summary>
    private static CommunicationRequest BuildReAdjudicationNotice(Claim claim, IEnumerable<int> sequences, int n)
    {
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
        request.Identifier.Add(new Identifier(PayerBase + "/communicationrequest", $"CommReq_{Hash(Key(claim))}_readj{n}"));
        request.Category.Add(new CodeableConcept(NphiesCodeSystems.CommunicationCategory, "notification"));
        request.About.Add(new ResourceReference { Type = "Claim", Identifier = claim.Identifier.FirstOrDefault() });
        foreach (var sequence in sequences.Order())
        {
            var payload = new CommunicationRequest.PayloadComponent { Content = new FhirString("The provided information does not justify the services.") };
            payload.Extension.Add(new Extension(NphiesExtensions.ClaimItemSequence, new PositiveInt(sequence)));
            request.Payload.Add(payload);
        }
        if (claim.Provider != null) request.Recipient.Add(claim.Provider);
        request.ReasonCode.Add(new CodeableConcept(NphiesCodeSystems.CommunicationReason, NphiesCommunicationReasons.ReAdjudication));
        return request;
    }

    /// <summary>An acknowledgement carrying an NPHIES validation error (IG: Acknowledgement Message with an OperationOutcome).</summary>
    private static Bundle ErrorAcknowledgement(Context ctx, string code, string display)
    {
        var outcome = new OperationOutcome { Id = Guid.NewGuid().ToString() };
        outcome.Issue.Add(new OperationOutcome.IssueComponent
        {
            Severity = OperationOutcome.IssueSeverity.Error,
            Code = OperationOutcome.IssueType.BusinessRule,
            Details = new CodeableConcept(NphiesCodeSystems.AdjudicationError, code, display, null),
        });
        var bundle = Message(ctx, NphiesMessageEvents.Acknowledgement, null, ctx.Header.Id);
        var header = (MessageHeader)bundle.Entry[0].Resource;
        header.Response!.Code = MessageHeader.ResponseType.FatalError;
        header.Response.Details = new ResourceReference("urn:uuid:" + outcome.Id);
        bundle.Entry.Add(new Bundle.EntryComponent { FullUrl = "urn:uuid:" + outcome.Id, Resource = outcome });
        return bundle;
    }

    /// <summary>Keeps the payer's final decision on a claim so a re-adjudication communication can reopen it.</summary>
    private void Remember(Context ctx, Claim claim, Decision decision, ClaimResponse response)
    {
        if (claim.Use == ClaimUseCode.Claim && Key(claim) is { Length: > 0 } key)
            _adjudicated[key] = new Adjudicated(ctx.Request, ctx.Header, claim, decision, response);
    }

    /// <summary>
    /// When set, the payer asks for more information (a CommunicationRequest) about each pended request and decides only
    /// after the provider's communication (Authorization test case 2). Otherwise it decides before the next poll.
    /// </summary>
    public bool AskForInformationWhenPended { get; set; }

    /// <summary>What the payer has done with the provider's pended requests by the time it polls.</summary>
    private void ProgressPended(string providerLicense)
    {
        foreach (var (identifier, pended) in _pended.Where(p => p.Value.ProviderLicense == providerLicense).ToList())
        {
            if (!AskForInformationWhenPended)
            {
                if (!_pended.TryRemove(identifier, out _)) continue;
                Deliver(providerLicense, pended.DeferredJson);
                EnqueueFollowUps(new Context(pended.Request, pended.Header), pended.Claim, pended.Final);
            }
            else if (!pended.InformationRequested)
            {
                pended.InformationRequested = true;
                Enqueue(providerLicense, Message(new Context(pended.Request, pended.Header), NphiesMessageEvents.CommunicationRequest, BuildCommunicationRequest(pended.Claim), null));
            }
        }
    }

    /// <summary>An unlisted drug (code 99999999999999): billed with the SFDA code alongside, and not covered by the sandbox payer.</summary>
    private static bool IsUnlistedDrug(Claim.ItemComponent item) =>
        item.ProductOrService?.Coding.Any(c => c.Code == "99999999999999") == true;

    /// <summary>The message event codes a poll asks for or leaves out (Task.input include-message-type / exclude-message-type).</summary>
    private static HashSet<string> MessageTypes(FhirTask? task, string inputType) =>
        (task?.Input ?? new List<FhirTask.ParameterComponent>())
            .Where(i => i.Type?.Coding.Any(c => c.Code == inputType) == true)
            .Select(i => i.Value switch { Code c => c.Value, Coding c => c.Code, CodeableConcept cc => cc.Coding.FirstOrDefault()?.Code, FhirString s => s.Value, _ => null })
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

    /// <summary>Takes up to <paramref name="count"/> queued messages of the wanted types, oldest first; the others stay queued in order.</summary>
    private static List<string> Take(ConcurrentQueue<string> queue, int count, HashSet<string> include, HashSet<string> exclude)
    {
        var all = new List<string>();
        while (queue.TryDequeue(out var json)) all.Add(json);
        var taken = new List<string>();
        foreach (var json in all)
        {
            var eventCode = ((Bundle)Parser.Parse<Resource>(json)).Entry.Select(e => e.Resource).OfType<MessageHeader>().FirstOrDefault()?.Event is Coding c ? c.Code : null;
            var wanted = (include.Count == 0 || (eventCode != null && include.Contains(eventCode)))
                         && (eventCode is null || !exclude.Contains(eventCode));
            if (wanted && taken.Count < count) taken.Add(json);
            else queue.Enqueue(json);
        }
        return taken;
    }

    // ============================== helpers ==============================

    private void Enqueue(string providerLicense, Bundle message) =>
        _queues.GetOrAdd(providerLicense, _ => new ConcurrentQueue<string>()).Enqueue(Serializer.SerializeToString(message));

    private static Bundle Message(Context ctx, string eventCode, Resource? focus, string? respondingTo, bool fromNphies = false)
    {
        var header = new MessageHeader
        {
            Id = Guid.NewGuid().ToString(),
            Meta = Profile(NphiesProfiles.MessageHeader),
            Event = new Coding(NphiesCodeSystems.MessageEvents, eventCode),
            Sender = new ResourceReference
            {
                Type = "Organization",
                Identifier = ctx.PayerLicense is null || fromNphies
                    ? new Identifier(NphiesIdentifierSystems.Nphies, NphiesIdentifierSystems.NphiesValue)
                    : new Identifier(NphiesIdentifierSystems.PayerLicense, ctx.PayerLicense),
            },
            Source = new MessageHeader.MessageSourceComponent { Endpoint = ctx.PayerLicense is null || fromNphies ? NphiesIdentifierSystems.NphiesEndpoint : PayerBase },
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
