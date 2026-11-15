using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;

namespace NPhies_FHIR_Integration.Tests.Nphies;

/// <summary>
/// Meta-tags and the three kinds of failure a provider can receive (Error received by HCP guideline):
/// NPHIES validation errors, payer business errors and rejections.
/// </summary>
[TestClass]
public class ResponseClassificationTests
{
    private readonly NphiesResponseParser _parser = new();

    private NphiesResponse Parse(string file) => _parser.Parse(DocsExamples.ReadJson(file));

    private NphiesResponse Parse(Bundle bundle) => _parser.Parse(new FhirJsonSerializer().SerializeToString(bundle));

    /// <summary>Loads a docs example and lets the test change it before it is parsed.</summary>
    private NphiesResponse ParseModified(string file, Action<Bundle> change)
    {
        var bundle = DocsExamples.LoadBundle(file);
        change(bundle);
        return Parse(bundle);
    }

    private static MessageHeader Header(Bundle bundle) => bundle.Entry.Select(e => e.Resource).OfType<MessageHeader>().First();

    private static void Tag(Bundle bundle, params string[] codes)
    {
        var header = Header(bundle);
        header.Meta ??= new Meta();
        foreach (var code in codes) header.Meta.Tag.Add(new Coding(NphiesCodeSystems.MetaTags, code));
    }

    [TestMethod]
    public void Poll_response_from_the_docs_carries_queued_messages()
    {
        var response = Parse("12b - Poll Response (Do Not Process).json");

        CollectionAssert.Contains(response.MetaTags, NphiesMetaTags.QueuedMessages);
        Assert.IsTrue(response.HasQueuedMessages);
    }

    [TestMethod]
    public void Approved_claim_response_has_no_error_kind_and_no_tags()
    {
        var response = Parse("7b - Claim Response.json");

        Assert.IsNull(response.ErrorKind);
        Assert.AreEqual(0, response.MetaTags.Count);
        Assert.IsFalse(response.IsNphiesGenerated);
        Assert.IsFalse(response.IsLargeClaim);
    }

    [TestMethod]
    public void Eligibility_outcome_error_from_the_payer_is_a_payer_business_error()
    {
        var response = Parse("15c - Eligibility Response Discovery - Outcome_Error.json");

        Assert.AreEqual(NphiesResponseErrorKind.PayerBusinessError, response.ErrorKind);
    }

    [TestMethod]
    public void Rejected_adjudication_is_a_rejection()
    {
        var response = ParseModified("7b - Claim Response.json", bundle =>
        {
            var claimResponse = bundle.Entry.Select(e => e.Resource).OfType<ClaimResponse>().First();
            claimResponse.GetExtension(NphiesExtensions.AdjudicationOutcome)!.Value =
                new CodeableConcept(NphiesCodeSystems.AdjudicationOutcome, "rejected");
        });

        Assert.AreEqual("rejected", response.Adjudication!.AdjudicationOutcome);
        Assert.AreEqual(NphiesResponseErrorKind.Rejection, response.ErrorKind);
    }

    [TestMethod]
    public void Nphies_rule_codes_and_operation_outcomes_are_nphies_validation_errors()
    {
        var withRule = ParseModified("7b - Claim Response.json", bundle =>
            bundle.Entry.Select(e => e.Resource).OfType<ClaimResponse>().First()
                .Error.Add(new ClaimResponse.ErrorComponent { Code = new CodeableConcept("http://nphies.sa/terminology/CodeSystem/adjudication-error", "BV-00002") }));
        Assert.AreEqual(NphiesResponseErrorKind.NphiesValidation, withRule.ErrorKind);

        var outcome = _parser.Parse(new OperationOutcome
        {
            Issue = { new OperationOutcome.IssueComponent { Severity = OperationOutcome.IssueSeverity.Error, Code = OperationOutcome.IssueType.Invalid } },
        });
        Assert.AreEqual(NphiesResponseErrorKind.NphiesValidation, outcome.ErrorKind);
    }

    [TestMethod]
    public void Large_claim_and_nphies_generated_tags_are_exposed()
    {
        var response = ParseModified("7b - Claim Response.json", bundle => Tag(bundle, NphiesMetaTags.LargeClaim, NphiesMetaTags.NphiesGenerated));

        Assert.IsTrue(response.IsLargeClaim);
        Assert.IsTrue(response.IsNphiesGenerated);
        Assert.IsFalse(response.HasQueuedMessages);
    }

    [TestMethod]
    public void A_response_sent_by_the_nphies_licence_is_nphies_generated()
    {
        var response = ParseModified("7b - Claim Response.json", bundle =>
        {
            var header = Header(bundle);
            header.Sender = new ResourceReference { Identifier = new Identifier(NphiesIdentifierSystems.Nphies, NphiesIdentifierSystems.NphiesValue) };
        });

        Assert.AreEqual(NphiesIdentifierSystems.Nphies, response.SenderSystem);
        Assert.IsTrue(response.IsNphiesGenerated);
    }

    [TestMethod]
    public void Tags_on_other_code_systems_are_ignored()
    {
        var response = ParseModified("7b - Claim Response.json", bundle =>
        {
            var header = Header(bundle);
            header.Meta.Tag.Add(new Coding("http://example.org/tags", NphiesMetaTags.LargeClaim));
        });

        Assert.IsFalse(response.IsLargeClaim);
    }
}
