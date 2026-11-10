using NPhies_FHIR_Integration.Application.Nphies.Validation;

namespace NPhies_FHIR_Integration.Tests.Nphies;

[TestClass]
public class DocsExampleValidationTests
{
    private readonly NphiesBundleValidator _validator = new();

    public TestContext TestContext { get; set; } = null!;

    [DataTestMethod]
    [DynamicData(nameof(DocsExamples.Requests), typeof(DocsExamples))]
    public void Docs_request_examples_pass_validation(string file)
    {
        var bundle = DocsExamples.LoadBundle(file);

        var result = _validator.Validate(bundle);

        foreach (var issue in result.Issues)
            TestContext.WriteLine($"{issue.Severity} {issue.RuleId} {issue.Expression}: {issue.Message}");
        Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.Errors.Select(e => $"{e.RuleId} {e.Expression}: {e.Message}")));
    }

    [TestMethod]
    public void Missing_message_header_is_rejected()
    {
        var bundle = DocsExamples.LoadBundle("10 - Claim Professional Request.json");
        bundle.Entry.RemoveAt(0);

        var result = _validator.Validate(bundle);

        Assert.IsFalse(result.IsValid);
        XAssert.Contains(result.Errors, e => e.RuleId == "BV-00126");
    }

    [TestMethod]
    public void Claim_total_mismatch_is_rejected()
    {
        var bundle = DocsExamples.LoadBundle("10 - Claim Professional Request.json");
        var claim = bundle.Entry.Select(e => e.Resource).OfType<Hl7.Fhir.Model.Claim>().Single();
        claim.Total = new Hl7.Fhir.Model.Money { Value = (claim.Total?.Value ?? 0) + 100, Currency = Hl7.Fhir.Model.Money.Currencies.SAR };

        var result = _validator.Validate(bundle);

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void Oversized_message_is_rejected()
    {
        var bundle = DocsExamples.LoadBundle("12a - Poll Request.json");

        var result = _validator.Validate(bundle, 11L * 1024 * 1024);

        XAssert.Contains(result.Errors, e => e.RuleId == "NPHIES-SIZE");
    }
}
