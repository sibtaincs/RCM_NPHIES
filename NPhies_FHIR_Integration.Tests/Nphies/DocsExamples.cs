using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace NPhies_FHIR_Integration.Tests.Nphies;

/// <summary>Loads the NPHIES example messages copied from nphies-fhir-docs-main into the test output.</summary>
internal static class DocsExamples
{
    public static string Directory => Path.Combine(AppContext.BaseDirectory, "DocsExamples");

    public static readonly string[] RequestFiles =
    {
        "1 - Preauth Institutional Request.json",
        "2a - First Preauth Dental Request with multiple items.json",
        "2c - Second Preauth Dental Request.json",
        "4 - Preauth Pharmacy Request.json",
        "5a - Preauth Professional Request.json",
        "6 - Preauth Vision Prescription Request.json",
        "7a - Claim Dental Request.json",
        "8 - Claim Institutional Request.json",
        "9 - Claim Pharmacy Request.json",
        "9b - StatusCheck - Request.json",
        "10 - Claim Professional Request.json",
        "11 - Claim Vision Request.json",
        "12a - Poll Request.json",
        "13b - Payment Notice.json",
        "14a - Eligibilty Request Ben-Val.json",
        "15a - Eligibilty Request Discovery.json",
        "16 - Batch request.json",
        "17a - Cancel Request.json",
        "18b - Communication response - solicited.json",
        "18c - Communication response - unsolicited.json",
        "19 - Shadow billing.json",
        "Eligibility Request_d4629f67-8212-4082-b2f6-f77d6674d74f.json",
        "Preauth Professional Outpatient Request.json",
    };

    public static readonly string[] ResponseFiles =
    {
        "2b - First Response for first Preauth Dental Request.json",
        "2d - First Response for second Preauth Dental Request.json",
        "2e - Second Response for second Preauth Dental Request.json",
        "5b - Preauth Professional Response.json",
        "7b - Claim Response.json",
        "9c - StatusCheck - Response (Do Not Process).json",
        "12b - Poll Response (Do Not Process).json",
        "13a - Payment Reconciliation.json",
        "14b - Eligibility Response Ben-Val.json",
        "15b - Eligibility Response Discovery - Outcome_Complete_out-network.json",
        "15c - Eligibility Response Discovery - Outcome_Error.json",
        "17b - Cancel Response (Do not process).json",
        "18a - Communication Request.json",
        "Eligibility Response_d845ef69-9a53-e9b9-f114-db0bc8a7f7bf.json",
    };

    public static IEnumerable<object[]> Requests => RequestFiles.Select(f => new object[] { f });
    public static IEnumerable<object[]> Responses => ResponseFiles.Select(f => new object[] { f });

    public static string ReadJson(string file) => File.ReadAllText(Path.Combine(Directory, file));

    public static Bundle LoadBundle(string file)
    {
#pragma warning disable CS0618 // ParserSettings is the supported way to relax NPHIES-specific deviations in Firely 5.x
        var parser = new FhirJsonParser(new ParserSettings
        {
            AcceptUnknownMembers = true,
            AllowUnrecognizedEnums = true,
            PermissiveParsing = true,
        });
#pragma warning restore CS0618
        return parser.Parse<Bundle>(ReadJson(file));
    }
}
