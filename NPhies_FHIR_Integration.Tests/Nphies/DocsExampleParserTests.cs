using NPhies_FHIR_Integration.Application.Nphies.Parsing;

namespace NPhies_FHIR_Integration.Tests.Nphies;

[TestClass]
public class DocsExampleParserTests
{
    private readonly NphiesResponseParser _parser = new();

    private NphiesResponse Parse(string file) => _parser.Parse(DocsExamples.ReadJson(file));

    [DataTestMethod]
    [DynamicData(nameof(DocsExamples.Responses), typeof(DocsExamples))]
    public void Every_docs_response_parses_with_header_and_event(string file)
    {
        var response = Parse(file);

        Assert.IsNotNull(response.Bundle);
        Assert.IsFalse(string.IsNullOrEmpty(response.MessageId));
        Assert.IsFalse(string.IsNullOrEmpty(response.EventCode));
        XAssert.DoesNotContain(response.Errors, e => e.Source == NphiesErrorSource.Transport);
    }

    [TestMethod]
    public void Claim_response_exposes_totals_and_item_adjudication()
    {
        var response = Parse("7b - Claim Response.json");

        Assert.AreEqual("claim-response", response.EventCode);
        Assert.AreEqual("54cf5884-662c-4f1d-85a2-a4a923a99060", response.RequestMessageId);
        Assert.AreEqual("ok", response.ResponseCode);
        Assert.IsTrue(response.IsSuccess);
        var adjudication = XAssert.IsType<NphiesClaimAdjudicationResult>(response.Adjudication);
        Assert.AreEqual("complete", adjudication.Outcome);
        Assert.AreEqual(230m, adjudication.TotalOf("submitted"));
        Assert.AreEqual(170m, adjudication.TotalOf("benefit"));
        Assert.AreEqual(25.5m, adjudication.TotalOf("tax"));
        Assert.AreEqual(200m, adjudication.TotalOf("eligible"));
        XAssert.Single(adjudication.Items);
    }

    [TestMethod]
    public void Priorauth_response_exposes_preauth_reference()
    {
        var response = Parse("2e - Second Response for second Preauth Dental Request.json");

        Assert.AreEqual("priorauth-response", response.EventCode);
        var adjudication = XAssert.IsType<NphiesClaimAdjudicationResult>(response.Adjudication);
        Assert.AreEqual("623525114", adjudication.PreAuthRef);
        Assert.AreEqual(3, adjudication.Items.Count);
        Assert.AreEqual(1851.5m, adjudication.TotalOf("benefit"));
    }

    [TestMethod]
    public void Eligibility_response_benefits_validation()
    {
        var response = Parse("14b - Eligibility Response Ben-Val.json");

        var eligibility = XAssert.IsType<NphiesEligibilityResult>(response.Eligibility);
        Assert.AreEqual("complete", eligibility.Outcome);
        Assert.IsTrue(eligibility.IsEligible);
        Assert.IsTrue(eligibility.Insurance.Single().InForce);
    }

    [TestMethod]
    public void Eligibility_error_response_maps_error_codes()
    {
        var response = Parse("15c - Eligibility Response Discovery - Outcome_Error.json");

        Assert.IsFalse(response.IsSuccess);
        var error = XAssert.Single(response.Errors);
        Assert.AreEqual("1658", error.Code);
        Assert.AreEqual(NphiesErrorSource.EligibilityResponse, error.Source);
        Assert.IsFalse(response.Eligibility!.IsEligible);
    }

    [TestMethod]
    public void Status_and_cancel_responses_expose_task_status()
    {
        var status = Parse("9c - StatusCheck - Response (Do Not Process).json");
        Assert.AreEqual("status-response", status.EventCode);
        Assert.AreEqual("completed", status.Task!.Status);
        Assert.AreEqual("status", status.Task.Code);
        Assert.IsFalse(string.IsNullOrEmpty(status.Task.ProcessingStatus));

        var cancel = Parse("17b - Cancel Response (Do not process).json");
        Assert.AreEqual("cancel-response", cancel.EventCode);
        Assert.AreEqual("completed", cancel.Task!.Status);
        Assert.AreEqual("cancel", cancel.Task.Code);
    }

    [TestMethod]
    public void Poll_response_parses_inner_messages()
    {
        var response = Parse("12b - Poll Response (Do Not Process).json");

        Assert.AreEqual("poll-response", response.EventCode);
        XAssert.NotEmpty(response.Task!.ResponseReferences);
        var inner = XAssert.Single(response.InnerMessages);
        Assert.AreEqual("claim-response", inner.EventCode);
        Assert.IsNotNull(inner.Adjudication);
    }

    [TestMethod]
    public void Payment_reconciliation_is_parsed()
    {
        var response = Parse("13a - Payment Reconciliation.json");

        Assert.AreEqual("payment-reconciliation", response.EventCode);
        var recon = XAssert.IsType<NphiesPaymentReconciliationResult>(response.PaymentReconciliation);
        Assert.AreEqual(193.55m, recon.PaymentAmount!.Value);
        Assert.AreEqual("URN-987590432322", recon.PaymentIdentifier);
        XAssert.Single(recon.Details);
    }

    [TestMethod]
    public void Communication_request_is_parsed()
    {
        var response = Parse("18a - Communication Request.json");

        Assert.AreEqual("communication-request", response.EventCode);
        var request = XAssert.IsType<NphiesCommunicationRequestResult>(response.CommunicationRequest);
        Assert.AreEqual("active", request.Status);
        Assert.AreEqual("req_00112489089", request.AboutClaimIdentifier);
        XAssert.NotEmpty(request.PayloadText);
    }

    [TestMethod]
    public void Operation_outcome_errors_are_enriched_from_catalog()
    {
        const string json = """
        {"resourceType":"OperationOutcome","issue":[{"severity":"error","code":"invalid",
          "details":{"coding":[{"system":"http://nphies.sa/terminology/CodeSystem/validation-error","code":"BV-00126"}]},
          "expression":["Bundle.entry[0]"]}]}
        """;

        var response = _parser.Parse(json);

        Assert.IsFalse(response.IsSuccess);
        var error = XAssert.Single(response.Errors);
        Assert.AreEqual("BV-00126", error.Code);
        Assert.AreEqual(NphiesErrorSource.OperationOutcome, error.Source);
        Assert.IsFalse(string.IsNullOrEmpty(error.Message));
    }

    [TestMethod]
    public void Garbage_input_yields_parse_error()
    {
        var response = _parser.Parse("not json");

        var error = XAssert.Single(response.Errors);
        Assert.AreEqual("PARSE", error.Code);
        Assert.AreEqual(NphiesErrorSource.Transport, error.Source);
    }
}
