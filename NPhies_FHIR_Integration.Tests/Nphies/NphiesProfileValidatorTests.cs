using Hl7.Fhir.Model;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Infrastructure.Configuration;

namespace NPhies_FHIR_Integration.Tests.Nphies;

/// <summary>Local NPHIES IG profile validation against the official docs examples and known-bad mutations.</summary>
[TestClass]
public class NphiesProfileValidatorTests
{
    private static readonly NphiesProfileValidator Validator = new(NphiesConformancePackage.Embedded);

    [TestMethod]
    public void Embedded_package_loads_profiles_and_terminology()
    {
        var package = Validator.Package;
        Assert.AreEqual(0, package.LoadErrors.Count, string.Join(Environment.NewLine, package.LoadErrors));
        Assert.AreEqual("nphies-fs", package.PackageName);
        Assert.IsTrue(package.Profiles.Count > 150, $"profiles: {package.Profiles.Count}");
        Assert.IsTrue(package.ValueSets.Count > 50, $"value sets: {package.ValueSets.Count}");
        Assert.IsNotNull(package.FindProfile("http://nphies.sa/fhir/ksa/nphies-fs/StructureDefinition/institutional-claim|1.0.0"));
        Assert.IsTrue(package.Expand("http://nphies.sa/terminology/ValueSet/claim-subtype")?.ContainsCode("ip") == true);
    }

    [DataTestMethod]
    [DynamicData(nameof(DocsExamples.Requests), typeof(DocsExamples))]
    public void Every_docs_request_is_validated_without_internal_failures(string file)
    {
        var result = Validator.Validate(DocsExamples.LoadBundle(file));

        Assert.IsFalse(result.Issues.Any(i => i.RuleId == NphiesProfileValidator.RuleInternal), result.ToString());
        Assert.IsTrue(result.Issues.All(i => !string.IsNullOrWhiteSpace(i.Message)));
    }

    [TestMethod]
    public void Missing_required_element_is_reported()
    {
        var bundle = DocsExamples.LoadBundle("7a - Claim Dental Request.json");
        Claim(bundle).Insurance.Clear();

        var result = Validator.Validate(bundle);

        Assert.IsTrue(result.Errors.Any(i => i.RuleId == NphiesProfileValidator.RuleCardinality && i.Message.Contains("Claim.insurance")), result.ToString());
    }

    [TestMethod]
    public void Code_outside_required_value_set_is_reported()
    {
        var bundle = DocsExamples.LoadBundle("7a - Claim Dental Request.json");
        var before = Validator.Validate(bundle).Errors.Count(i => i.RuleId == NphiesProfileValidator.RuleCode);
        Claim(bundle).SubType = new CodeableConcept("http://nphies.sa/terminology/CodeSystem/claim-subtype", "not-a-subtype");

        var result = Validator.Validate(bundle);

        Assert.IsTrue(result.Errors.Count(i => i.RuleId == NphiesProfileValidator.RuleCode) > before, result.ToString());
        Assert.IsTrue(result.Errors.Any(i => i.Message.Contains("not-a-subtype")), result.ToString());
    }

    [TestMethod]
    public void Nphies_business_invariant_is_evaluated()
    {
        var bundle = DocsExamples.LoadBundle("7a - Claim Dental Request.json");
        // The example carries both a beneficiary and a subscriber Patient with iqama identifiers.
        var iqamas = bundle.Entry.Select(e => e.Resource).OfType<Patient>()
            .SelectMany(p => p.Identifier).Where(i => i.Type?.Coding.Any(c => c.Code == "PRC") == true).ToList();
        Assert.IsTrue(iqamas.Count > 0);

        iqamas.ForEach(i => i.Value = "0000000002");
        Assert.IsTrue(Validator.Validate(bundle).Errors.Any(i => i.RuleId == "BV-00798"));

        iqamas.ForEach(i => i.Value = "2000000002");
        Assert.IsFalse(Validator.Validate(bundle).Issues.Any(i => i.RuleId == "BV-00798"));
    }

    [TestMethod]
    public void Package_invariant_requires_details_as_its_text_states()
    {
        var bundle = DocsExamples.LoadBundle("19 - Shadow billing.json");
        var package = Claim(bundle).Item.First(i => i.Detail.Count > 0 && i.Extension.Any(e => e.Url.EndsWith("extension-package")));
        Assert.IsFalse(Validator.Validate(bundle).Issues.Any(i => i.RuleId == "BV-00036"));

        package.Detail.Clear();

        Assert.IsTrue(Validator.Validate(bundle).Errors.Any(i => i.RuleId == "BV-00036"));
    }

    [TestMethod]
    public void Closed_slicing_rejects_unknown_identifier_type()
    {
        var bundle = DocsExamples.LoadBundle("7a - Claim Dental Request.json");
        var patient = bundle.Entry.Select(e => e.Resource).OfType<Patient>().First();
        patient.Identifier[0].Type = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v2-0203", "XX");

        var result = Validator.Validate(bundle);

        Assert.IsTrue(result.Errors.Any(i => i.RuleId == NphiesProfileValidator.RuleSlice && i.Message.Contains("Patient.identifier")), result.ToString());
    }

    [TestMethod]
    public void Unknown_profile_is_a_warning()
    {
        var bundle = DocsExamples.LoadBundle("7a - Claim Dental Request.json");
        Claim(bundle).Meta.Profile = new[] { "http://nphies.sa/fhir/ksa/nphies-fs/StructureDefinition/does-not-exist|1.0.0" };

        var issue = Validator.Validate(bundle).Issues.Single(i => i.RuleId == NphiesProfileValidator.RuleProfile && i.Message.Contains("does-not-exist"));

        Assert.AreEqual(NphiesValidationSeverity.Warning, issue.Severity);
    }

    [TestMethod]
    public void Composite_mode_controls_profile_severity()
    {
        var bundle = DocsExamples.LoadBundle("7a - Claim Dental Request.json");
        Claim(bundle).Insurance.Clear();
        var rules = new NphiesBundleValidator();
        var rulesOnly = rules.Validate(bundle).Issues.Count;

        var off = new CompositeNphiesBundleValidator(rules, Validator, NphiesProfileValidationMode.Off).Validate(bundle);
        var warn = new CompositeNphiesBundleValidator(rules, Validator, NphiesProfileValidationMode.Warn).Validate(bundle);
        var enforce = new CompositeNphiesBundleValidator(rules, Validator, NphiesProfileValidationMode.Enforce).Validate(bundle);

        Assert.AreEqual(rulesOnly, off.Issues.Count);
        Assert.IsTrue(warn.Issues.Count > rulesOnly);
        Assert.IsFalse(warn.Issues.Any(i => i.RuleId.StartsWith("PRF-") && i.Severity == NphiesValidationSeverity.Error));
        Assert.IsTrue(enforce.Errors.Any(i => i.RuleId == NphiesProfileValidator.RuleCardinality));
    }

    private static Claim Claim(Bundle bundle) => bundle.Entry.Select(e => e.Resource).OfType<Claim>().First();
}

