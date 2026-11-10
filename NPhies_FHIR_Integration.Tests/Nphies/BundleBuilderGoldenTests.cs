using System.Text.Json.Nodes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Infrastructure.Configuration;

namespace NPhies_FHIR_Integration.Tests.Nphies;

/// <summary>
/// Golden tests: every outgoing docs example is reverse-mapped to builder inputs, rebuilt, and compared
/// structurally (element paths, values, extensions, profiles) with the original.
/// </summary>
[TestClass]
public class BundleBuilderGoldenTests
{
    private readonly NphiesBundleBuilder _builder = new(Options.Create(new NphiesConfiguration()));

    public TestContext TestContext { get; set; } = null!;

    public static IEnumerable<object[]> Cases => new[]
    {
        "1 - Preauth Institutional Request.json",
        "2a - First Preauth Dental Request with multiple items.json",
        "2c - Second Preauth Dental Request.json",
        "4 - Preauth Pharmacy Request.json",
        "5a - Preauth Professional Request.json",
        "6 - Preauth Vision Prescription Request.json",
        "Preauth Professional Outpatient Request.json",
        "7a - Claim Dental Request.json",
        "8 - Claim Institutional Request.json",
        "9 - Claim Pharmacy Request.json",
        "10 - Claim Professional Request.json",
        "11 - Claim Vision Request.json",
        "19 - Shadow billing.json",
        "14a - Eligibilty Request Ben-Val.json",
        "15a - Eligibilty Request Discovery.json",
        "Eligibility Request_d4629f67-8212-4082-b2f6-f77d6674d74f.json",
        "9b - StatusCheck - Request.json",
        "17a - Cancel Request.json",
        "12a - Poll Request.json",
        "18b - Communication response - solicited.json",
        "18c - Communication response - unsolicited.json",
        "13b - Payment Notice.json",
        "16 - Batch request.json",
    }.Select(f => new object[] { f });

    [DataTestMethod]
    [DynamicData(nameof(Cases))]
    public void Builder_reproduces_docs_example(string file)
    {
        var expected = DocsExamples.LoadBundle(file);
        var actual = Build(expected);

        AssertEquivalentMessage(UpgradeLegacyExample(expected), actual, file);

        var validation = new NphiesBundleValidator().Validate(actual);
        Assert.IsTrue(validation.IsValid, validation.ToString());
    }

    [TestMethod]
    public void Batch_assigns_batch_extensions_and_rejects_mixed_insurers()
    {
        var source = DocsExamples.LoadBundle("16 - Batch request.json");
        var input = DocsInputExtractor.Batch(source);
        input.AssignBatchExtensions = true;
        foreach (var claim in input.Claims) claim.Batch = null;

        var batch = _builder.BuildBatchRequest(input);

        var claims = batch.Entry.Select(e => e.Resource).OfType<Bundle>()
            .Select(b => b.Entry.Select(e => e.Resource).OfType<Claim>().Single()).ToList();
        CollectionAssert.AreEqual(new[] { 1, 2 }, claims.Select(c => ((PositiveInt)c.GetExtension(Application.Nphies.Constants.NphiesExtensions.BatchNumber).Value).Value!.Value).ToList());

        input.Claims[1].Insurer.License = "OTHER";
        XAssert.Throws<ArgumentException>(() => _builder.BuildBatchRequest(input));
    }

    [TestMethod]
    public void Generated_ids_and_timestamp_are_filled_when_not_supplied()
    {
        var input = DocsInputExtractor.Poll(DocsExamples.LoadBundle("12a - Poll Request.json"));
        input.MessageId = null;
        input.BundleId = null;
        input.Timestamp = null;
        input.Count = 50;

        var bundle = _builder.BuildPollRequest(input);

        Assert.IsTrue(Guid.TryParse(bundle.Id, out _));
        Assert.IsTrue(Guid.TryParse(bundle.Entry[0].Resource.Id, out _));
        Assert.IsNotNull(bundle.Timestamp);
        XAssert.Throws<ArgumentOutOfRangeException>(() => _builder.BuildPollRequest(new Application.Nphies.Models.PollRequestInput { Count = 101 }));
    }

    private Bundle Build(Bundle source)
    {
        var header = (MessageHeader)source.Entry[0].Resource;
        return (header.Event as Coding)!.Code switch
        {
            "priorauth-request" => _builder.BuildPriorAuthRequest(DocsInputExtractor.Claim(source)),
            "claim-request" => _builder.BuildClaimRequest(DocsInputExtractor.Claim(source)),
            "eligibility-request" => _builder.BuildEligibilityRequest(DocsInputExtractor.Eligibility(source)),
            "status-check" => _builder.BuildStatusCheck(DocsInputExtractor.ClaimTask(source)),
            "cancel-request" => _builder.BuildCancelRequest(DocsInputExtractor.ClaimTask(source)),
            "poll-request" => _builder.BuildPollRequest(DocsInputExtractor.Poll(source)),
            "communication" => _builder.BuildCommunication(DocsInputExtractor.Communication(source)),
            "payment-notice" => _builder.BuildPaymentNotice(DocsInputExtractor.PaymentNotice(source)),
            "batch-request" => _builder.BuildBatchRequest(DocsInputExtractor.Batch(source)),
            var other => throw new NotSupportedException(other),
        };
    }

    private void AssertEquivalentMessage(Bundle expected, Bundle actual, string context)
    {
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(Bundle.BundleType.Message, actual.Type);
        Assert.AreEqual(expected.Entry.Count, actual.Entry.Count);

        CompareResource(expected.Entry[0].Resource, actual.Entry[0].Resource, $"{context} MessageHeader");

        var expectedHeader = (MessageHeader)expected.Entry[0].Resource;
        foreach (var (expectedFocus, actualFocus) in expectedHeader.Focus.Zip(((MessageHeader)actual.Entry[0].Resource).Focus))
        {
            var e = expected.Entry.Single(x => x.FullUrl == expectedFocus.Reference).Resource;
            var a = actual.Entry.Single(x => x.FullUrl == actualFocus.Reference).Resource;
            if (e is Bundle innerExpected)
                AssertEquivalentMessage(innerExpected, (Bundle)a, $"{context} inner {innerExpected.Id}");
            else
                CompareResource(e, a, $"{context} {e.TypeName}");
        }

        CollectionAssert.AreEqual(ProfileSet(expected), ProfileSet(actual), context);
    }

    private void CompareResource(Resource expected, Resource actual, string context)
    {
        var expectedLeaves = Leaves(expected);
        var actualLeaves = Leaves(actual);

        var missingPaths = expectedLeaves.Keys.Except(actualLeaves.Keys).ToList();
        var extraPaths = actualLeaves.Keys.Except(expectedLeaves.Keys).ToList();
        var valueDiffs = expectedLeaves.Keys.Intersect(actualLeaves.Keys)
            .Where(k => !k.EndsWith(".reference", StringComparison.Ordinal))
            .Where(k => !expectedLeaves[k].SetEquals(actualLeaves[k]))
            .Select(k => $"{k}: expected [{string.Join(", ", expectedLeaves[k])}] actual [{string.Join(", ", actualLeaves[k])}]")
            .ToList();

        foreach (var p in missingPaths) TestContext.WriteLine($"{context} MISSING {p}");
        foreach (var p in extraPaths) TestContext.WriteLine($"{context} EXTRA {p}");
        foreach (var d in valueDiffs) TestContext.WriteLine($"{context} VALUE {d}");

        Assert.IsTrue(missingPaths.Count == 0 && extraPaths.Count == 0 && valueDiffs.Count == 0,
            $"{context}: missing [{string.Join(", ", missingPaths)}] extra [{string.Join(", ", extraPaths)}] values [{string.Join("; ", valueDiffs)}]");
    }

    private static List<string> ProfileSet(Bundle bundle) => bundle.Entry
        .Select(e => $"{e.Resource.TypeName}|{string.Join(",", e.Resource.Meta?.Profile ?? Enumerable.Empty<string>())}")
        .OrderBy(x => x, StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// Applies the known differences between the 2.x docs examples and the current IG (nphies-fs 1.0.0) that the
    /// builder deliberately emits: per-code Task profiles (the generic "task" profile was retired) and the typed
    /// PaymentNotice.recipient identifier (DP-Reference-2a).
    /// </summary>
    private static Bundle UpgradeLegacyExample(Bundle bundle)
    {
        foreach (var resource in bundle.Entry.Select(e => e.Resource))
        {
            if (resource is Hl7.Fhir.Model.Task task && task.Meta.Profile.Contains(Application.Nphies.Constants.NphiesProfiles.Task))
            {
                task.Meta.Profile = new[]
                {
                    task.Code?.Coding.FirstOrDefault()?.Code switch
                    {
                        "status" => Application.Nphies.Constants.NphiesProfiles.StatusRequest,
                        "poll" => Application.Nphies.Constants.NphiesProfiles.PollRequest,
                        _ => Application.Nphies.Constants.NphiesProfiles.CancelRequest,
                    },
                };
            }
            if (resource is PaymentNotice notice && notice.Recipient?.Identifier is { Type: null } recipientId)
                recipientId.Type = new CodeableConcept(Application.Nphies.Constants.NphiesCodeSystems.OrganizationType, "other");
            if (resource is Bundle inner) UpgradeLegacyExample(inner);
        }
        return bundle;
    }

    /// <summary>Flattens a resource to element-path → set of leaf values (array indices dropped, extensions keyed by URL).</summary>
    private static Dictionary<string, HashSet<string>> Leaves(Resource resource)
    {
        var node = JsonNode.Parse(resource.ToJson())!;
        var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        Walk(node, resource.TypeName, result);
        return result;
    }

    private static void Walk(JsonNode? node, string path, Dictionary<string, HashSet<string>> result)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj)
                {
                    if (key is "extension" or "modifierExtension" && value is JsonArray extensions)
                    {
                        foreach (var ext in extensions)
                        {
                            var url = ext?["url"]?.GetValue<string>() ?? "?";
                            Walk(ext, $"{path}.{key}[{url.Split('/').Last()}]", result);
                        }
                    }
                    else
                    {
                        Walk(value, $"{path}.{key}", result);
                    }
                }

                break;
            case JsonArray array:
                foreach (var item in array) Walk(item, path, result);
                break;
            case JsonValue value:
                if (!result.TryGetValue(path, out var set)) result[path] = set = new HashSet<string>(StringComparer.Ordinal);
                set.Add(Normalize(value));
                break;
        }
    }

    private static string Normalize(JsonValue value) =>
        value.TryGetValue<decimal>(out var number) ? number.ToString("0.############", System.Globalization.CultureInfo.InvariantCulture) : value.ToString();
}
