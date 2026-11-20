using System.Text.Json.Nodes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Infrastructure.Configuration;

namespace NPhies_FHIR_Integration.Tests.Nphies;

/// <summary>
/// The claim item link on communication payloads (<c>extension-ClaimItemSequence</c>, ClaimItem guide V1.1) and the
/// re-adjudication reason (Claim Re-submission guide). The extension is not in the IG package (open question Q1).
/// </summary>
[TestClass]
public class CommunicationItemSequenceTests
{
    private readonly NphiesBundleBuilder _builder = new(Options.Create(new NphiesConfiguration()));

    [TestMethod]
    public void A_payload_about_one_service_carries_the_item_sequence_and_the_reason_code()
    {
        var input = DocsInputExtractor.Communication(DocsExamples.LoadBundle("18c - Communication response - unsolicited.json"));
        var profileValidator = new NphiesProfileValidator(NphiesConformancePackage.Embedded);
        // The 2.x docs example's patient and organizations miss elements the current profiles require; only new errors count.
        var baseline = profileValidator.Validate(_builder.BuildCommunication(input)).Errors.Select(e => e.Message).ToHashSet();
        input.Payloads[0].ClaimItemSequence = 3;
        input.Reason = new Application.Nphies.Models.NphiesCoding(NphiesCodeSystems.CommunicationReason, NphiesCommunicationReasons.ReAdjudication);

        var bundle = _builder.BuildCommunication(input);

        var communication = bundle.Entry.Select(e => e.Resource).OfType<Communication>().Single();
        var extension = communication.Payload[0].GetExtension(NphiesExtensions.ClaimItemSequence);
        Assert.AreEqual("http://nphies.sa/fhir/ksa/nphies-fs/StructureDefinition/extension-ClaimItemSequence", extension?.Url);
        Assert.AreEqual(3, (extension!.Value as PositiveInt)?.Value);
        Assert.IsTrue(communication.Payload.Skip(1).All(p => p.Extension.Count == 0), "Only the payload about the service carries it.");
        Assert.AreEqual("re-adjudication", communication.ReasonCode.Single().Coding.Single().Code);

        // The IG package does not define the extension: profile validation warns, it does not fail, even when enforced.
        var profile = profileValidator.Validate(bundle);
        var added = profile.Errors.Where(e => !baseline.Contains(e.Message)).ToList();
        Assert.AreEqual(0, added.Count, string.Join("; ", added.Select(e => $"{e.RuleId} {e.Message}")));
        Assert.IsTrue(profile.Issues.Any(i => i.RuleId == NphiesProfileValidator.RuleExtension && i.Message.Contains("ClaimItemSequence")));
        Assert.IsTrue(new NphiesBundleValidator().Validate(bundle).IsValid);

        // Round trip: the extractor reads the item back.
        Assert.AreEqual(3, DocsInputExtractor.Communication(bundle).Payloads[0].ClaimItemSequence);
    }

    [TestMethod]
    public void The_builder_refuses_an_item_sequence_below_one()
    {
        var input = DocsInputExtractor.Communication(DocsExamples.LoadBundle("18c - Communication response - unsolicited.json"));
        input.Payloads[0].ClaimItemSequence = 0;
        Assert.ThrowsException<ArgumentException>(() => _builder.BuildCommunication(input));
    }

    [DataTestMethod]
    [DataRow("extension-ClaimItemSequence")]
    [DataRow("extension-claimItemSequence")] // the spelling of the guide's own example
    public void A_payer_request_about_services_is_read_with_either_spelling(string name)
    {
        var bundle = JsonNode.Parse(DocsExamples.ReadJson("18a - Communication Request.json"))!;
        var request = bundle["entry"]!.AsArray().Select(e => e!["resource"]!).Single(r => (string?)r["resourceType"] == "CommunicationRequest");
        request["category"] = JsonNode.Parse("""[{"coding":[{"system":"http://terminology.hl7.org/CodeSystem/communication-category","code":"notification"}]}]""");
        request["reasonCode"] = JsonNode.Parse("""[{"coding":[{"system":"http://nphies.sa/terminology/CodeSystem/communication-reason","code":"re-adjudication"}]}]""");
        var payloads = request["payload"]!.AsArray();
        payloads.Add(JsonNode.Parse("""{"contentString":"The provided information does not justify the services"}"""));
        foreach (var (payload, sequence) in payloads.Select((p, i) => (p!, i == 0 ? 3 : 1)))
            payload["extension"] = new JsonArray(new JsonObject
            {
                ["url"] = "http://nphies.sa/fhir/ksa/nphies-fs/StructureDefinition/" + name,
                ["valuePositiveInt"] = sequence,
            });

        var parsed = new NphiesResponseParser().Parse(new FhirJsonParser().Parse<Bundle>(bundle.ToJsonString()));

        var cr = parsed.CommunicationRequest!;
        Assert.AreEqual("re-adjudication", cr.Reason);
        Assert.AreEqual("notification", cr.Category);
        CollectionAssert.AreEqual(new[] { 1, 3 }, cr.ItemSequences);
        Assert.AreEqual(3, cr.Payloads[0].ClaimItemSequence);
        Assert.AreEqual("The provided information does not justify the services", cr.Payloads[1].Text);
    }

    [DataTestMethod]
    [DataRow("re-adjudication", "re-adjudication")]
    [DataRow("Missing-info", "missing-info")]
    [DataRow(" MISSING-ATTACH ", "missing-attach")]
    [DataRow("appeal", null)]
    public void Reason_codes_are_normalized_to_the_code_system(string input, string? expected) =>
        Assert.AreEqual(expected, NphiesCommunicationReasons.Normalize(input));
}
