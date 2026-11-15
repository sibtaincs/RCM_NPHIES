using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Sandbox;
using NPhies_FHIR_Integration.Infrastructure.Configuration;

namespace NPhies_FHIR_Integration.Application.Nphies.Discovery;

/// <summary>Configuration of the Check Insurance API (section <c>Nphies:Discovery</c>).</summary>
public sealed class NphiesDiscoveryOptions
{
    public const string SectionName = "Nphies:Discovery";
    public const string ProductionUrl = "https://hsb.nphies.sa/check-insurance";
    public const string OnboardingUrl = "http://hsb.oba.nphies.sa/check-insurance";

    /// <summary>Full URL of the API. When empty: production or onboarding (OBA), by <c>Nphies:Environment</c>.</summary>
    public string? Url { get; set; }

    /// <summary>
    /// Value of the <c>Authorization</c> header, when NPHIES issued one for this API. The guide lists errors for a
    /// missing or invalid header (108 to 112) but not its format, so it is sent exactly as configured.
    /// </summary>
    public string? AuthorizationHeader { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>The beneficiary registries behind the Check Insurance API (<c>SystemType</c>).</summary>
public enum InsuranceSystemType
{
    /// <summary>Nationals and residents, by national id or Iqama (10 digits).</summary>
    Hidp = 1,
    /// <summary>Visitors, by visa, passport or border number.</summary>
    Vidp = 2,
    /// <summary>Tourists, by visa or passport number.</summary>
    Scth = 3,
    /// <summary>Premium residency, by visa or passport number.</summary>
    Uidp = 4,
    /// <summary>Hajj and Umrah visitors, by passport number.</summary>
    Huidp = 5,
}

/// <summary>One policy returned by the Check Insurance API. Fields a registry does not return stay null.</summary>
public sealed class DiscoveredPolicy
{
    public string? PolicyNumber { get; set; }
    public string? InsuranceCompanyEn { get; set; }
    public string? InsuranceCompanyAr { get; set; }
    public string? InsuranceCompanyId { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ClassName { get; set; }
    public string? NetworkId { get; set; }
    /// <summary>Deductible or copay percentage (HIDP).</summary>
    public decimal? DeductibleRate { get; set; }
    /// <summary>Maximum amount chargeable to the beneficiary (HIDP).</summary>
    public decimal? MaxLimit { get; set; }
    /// <summary>1 = main beneficiary, 2 = dependent (HIDP).</summary>
    public string? BeneficiaryType { get; set; }
    /// <summary>Member number: BeneficiaryNumber (HIDP) or MembershipCode (HUIDP).</summary>
    public string? BeneficiaryNumber { get; set; }
    public string? SponsorNumber { get; set; }
    public string? PolicyHolder { get; set; }
    public string? InsuranceStatus { get; set; }
    public string? InsuranceType { get; set; }
    public string? Name { get; set; }
    /// <summary>male | female, from the API's 1 | 2.</summary>
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? IdentityNumber { get; set; }
    public string? PassportNumber { get; set; }
    public string? VisaNumber { get; set; }
    public string? BorderNumber { get; set; }
    /// <summary>Every field exactly as the API returned it.</summary>
    public Dictionary<string, string?> Raw { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class InsuranceDiscoveryResult
{
    public List<DiscoveredPolicy> Policies { get; set; } = new();
    /// <summary>API error code 101 to 112, or null. 105 means no record was found.</summary>
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int HttpStatus { get; set; }
    public bool Simulated { get; set; }
    public bool NotFound => ErrorCode == NphiesInsuranceDiscoveryErrors.NoRecordFound;
    public bool IsError => ErrorCode != null && !NotFound;
}

/// <summary>Error codes of the Check Insurance API (Beneficiary Discovery Insurance guide V1.0, section 4).</summary>
public static class NphiesInsuranceDiscoveryErrors
{
    public const string KeyRequired = "101";
    public const string IdentityLength = "102";
    public const string IdentityDigits = "103";
    public const string UndefinedSystemType = "104";
    public const string NoRecordFound = "105";

    private static readonly Dictionary<string, string> Descriptions = new(StringComparer.Ordinal)
    {
        ["101"] = "Key is required",
        ["102"] = "Identity number should be 10 digits",
        ["103"] = "Identity number should contain only numbers",
        ["104"] = "Undefined system type",
        ["105"] = "No record found",
        ["106"] = "System type is required",
        ["107"] = "System type should contain only numbers",
        ["108"] = "Invalid Authorization Header",
        ["109"] = "Invalid Header",
        ["110"] = "Invalid Header Token",
        ["111"] = "Invalid Header Token - Timeout Exception",
        ["112"] = "error reading authentication header",
    };

    public static string? Describe(string? code) => code != null && Descriptions.TryGetValue(code, out var d) ? d : null;

    public static bool IsKnown(string? code) => code != null && Descriptions.ContainsKey(code);
}

/// <summary>
/// The NPHIES Check Insurance API (beneficiary discovery). It is a plain REST GET, not a FHIR message: providers must
/// find a patient's insurer by identity number this way instead of asking the patient (CHI mandate, January 2020).
/// </summary>
public interface INphiesInsuranceDiscoveryClient
{
    Task<InsuranceDiscoveryResult> CheckAsync(string patientKey, InsuranceSystemType systemType, CancellationToken ct = default);
}

public sealed class NphiesInsuranceDiscoveryClient : INphiesInsuranceDiscoveryClient
{
    public const string HttpClientName = "NphiesInsuranceDiscovery";

    private readonly HttpClient _http;
    private readonly NphiesConfiguration _nphies;
    private readonly NphiesDiscoveryOptions _options;
    private readonly NphiesSandboxOptions _sandbox;
    private readonly ILogger<NphiesInsuranceDiscoveryClient> _logger;

    public NphiesInsuranceDiscoveryClient(HttpClient http, IOptions<NphiesConfiguration> nphies, IOptions<NphiesDiscoveryOptions> options,
        IOptions<NphiesSandboxOptions> sandbox, ILogger<NphiesInsuranceDiscoveryClient> logger)
    {
        _http = http;
        _nphies = nphies.Value;
        _options = options.Value;
        _sandbox = sandbox.Value;
        _logger = logger;
    }

    /// <summary>The checks the API itself makes (errors 101 to 104), so an obviously bad key is never sent.</summary>
    public static InsuranceDiscoveryResult? Validate(string? patientKey, InsuranceSystemType systemType)
    {
        static InsuranceDiscoveryResult Fail(string code) => new() { ErrorCode = code, ErrorMessage = NphiesInsuranceDiscoveryErrors.Describe(code) };

        if (string.IsNullOrWhiteSpace(patientKey)) return Fail(NphiesInsuranceDiscoveryErrors.KeyRequired);
        if (!Enum.IsDefined(systemType)) return Fail(NphiesInsuranceDiscoveryErrors.UndefinedSystemType);
        if (systemType == InsuranceSystemType.Hidp)
        {
            var key = patientKey.Trim();
            if (!key.All(char.IsAsciiDigit)) return Fail(NphiesInsuranceDiscoveryErrors.IdentityDigits);
            if (key.Length != 10) return Fail(NphiesInsuranceDiscoveryErrors.IdentityLength);
        }
        return null;
    }

    public async Task<InsuranceDiscoveryResult> CheckAsync(string patientKey, InsuranceSystemType systemType, CancellationToken ct = default)
    {
        if (Validate(patientKey, systemType) is { } invalid) return invalid;
        var key = patientKey.Trim();

        if (_sandbox.Enabled || NphiesSandboxTransport.IsActive)
            return InsuranceDiscoverySimulator.Answer(key, systemType);

        var baseUrl = !string.IsNullOrWhiteSpace(_options.Url) ? _options.Url!
            : _nphies.IsProduction ? NphiesDiscoveryOptions.ProductionUrl : NphiesDiscoveryOptions.OnboardingUrl;
        var url = $"{baseUrl}?PatientKey={Uri.EscapeDataString(key)}&SystemType={(int)systemType}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(_options.AuthorizationHeader))
            request.Headers.TryAddWithoutValidation("Authorization", _options.AuthorizationHeader);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 60)));
        try
        {
            using var response = await _http.SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            var result = InsuranceDiscoveryParser.Parse(body, (int)response.StatusCode);
            // The patient key is an identity number: log the outcome, never the key or the body.
            _logger.LogInformation("NPHIES check-insurance (system type {SystemType}) -> HTTP {Status}, {Policies} policy(ies), error {Error}",
                (int)systemType, (int)response.StatusCode, result.Policies.Count, result.ErrorCode ?? "none");
            return result;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new InsuranceDiscoveryResult { ErrorCode = "timeout", ErrorMessage = $"NPHIES check-insurance did not answer within {_options.TimeoutSeconds} seconds." };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "NPHIES check-insurance could not be reached");
            return new InsuranceDiscoveryResult { ErrorCode = "transport", ErrorMessage = $"NPHIES check-insurance could not be reached: {ex.Message}" };
        }
    }
}

/// <summary>
/// Reads a Check Insurance response. The guide lists the fields of each registry but not the JSON envelope, so this
/// accepts a single policy object, an array of them, or an object that wraps such an array, and matches field names
/// without regard to case.
/// </summary>
public static class InsuranceDiscoveryParser
{
    private static readonly string[] ErrorCodeNames = { "ErrorCode", "Code", "StatusCode", "Error" };
    private static readonly string[] ErrorTextNames = { "ErrorDescription", "ErrorMessage", "Message", "Description", "Error" };
    private static readonly string[] DateFormats = { "yyyy-MM-dd", "dd-MM-yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy/MM/dd", "dd/MM/yyyy" };

    public static InsuranceDiscoveryResult Parse(string? body, int httpStatus)
    {
        var result = new InsuranceDiscoveryResult { HttpStatus = httpStatus };
        if (string.IsNullOrWhiteSpace(body))
        {
            if (httpStatus is < 200 or >= 300) (result.ErrorCode, result.ErrorMessage) = ("http", $"NPHIES check-insurance answered HTTP {httpStatus} with no body.");
            return result;
        }

        JsonDocument doc;
        try { doc = JsonDocument.Parse(body); }
        catch (JsonException)
        {
            // A bare error code or plain text.
            var text = body.Trim().Trim('"');
            (result.ErrorCode, result.ErrorMessage) = NphiesInsuranceDiscoveryErrors.IsKnown(text)
                ? (text, NphiesInsuranceDiscoveryErrors.Describe(text))
                : ("unreadable", $"NPHIES check-insurance answered HTTP {httpStatus} with a body that is not JSON.");
            return result;
        }

        using (doc)
        {
            // A bare error code, as a JSON number or string.
            if (doc.RootElement.ValueKind is JsonValueKind.Number or JsonValueKind.String)
            {
                var bare = doc.RootElement.ValueKind == JsonValueKind.String ? doc.RootElement.GetString()?.Trim() : doc.RootElement.GetRawText();
                if (NphiesInsuranceDiscoveryErrors.IsKnown(bare))
                    (result.ErrorCode, result.ErrorMessage) = (bare, NphiesInsuranceDiscoveryErrors.Describe(bare));
            }
            foreach (var element in Objects(doc.RootElement))
            {
                var fields = Fields(element);
                var code = ErrorCodeNames.Select(n => fields.GetValueOrDefault(n)).FirstOrDefault(NphiesInsuranceDiscoveryErrors.IsKnown);
                if (code != null)
                {
                    result.ErrorCode ??= code;
                    result.ErrorMessage ??= ErrorTextNames.Select(n => fields.GetValueOrDefault(n)).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v) && v != code)
                                            ?? NphiesInsuranceDiscoveryErrors.Describe(code);
                    continue;
                }
                if (fields.ContainsKey("PolicyNumber") || fields.ContainsKey("InsuranceCompanyID") || fields.ContainsKey("MembershipCode"))
                    result.Policies.Add(ToPolicy(fields));
            }
        }

        if (result.Policies.Count > 0) (result.ErrorCode, result.ErrorMessage) = (null, null);
        else if (result.ErrorCode is null && httpStatus is < 200 or >= 300)
            (result.ErrorCode, result.ErrorMessage) = ("http", $"NPHIES check-insurance answered HTTP {httpStatus}.");
        return result;
    }

    /// <summary>The root object, the objects of a root array, and the objects of any array one level down.</summary>
    private static IEnumerable<JsonElement> Objects(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.Object)) yield return item;
            yield break;
        }
        if (root.ValueKind != JsonValueKind.Object) yield break;
        yield return root;
        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
                foreach (var item in property.Value.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.Object)) yield return item;
            else if (property.Value.ValueKind == JsonValueKind.Object)
                yield return property.Value;
        }
    }

    private static Dictionary<string, string?> Fields(JsonElement element)
    {
        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            fields[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.Value.GetRawText(),
                _ => null,
            };
        }
        return fields;
    }

    private static DiscoveredPolicy ToPolicy(Dictionary<string, string?> f)
    {
        string? Text(params string[] names) => names.Select(n => f.GetValueOrDefault(n)).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
        return new DiscoveredPolicy
        {
            PolicyNumber = Text("PolicyNumber"),
            InsuranceCompanyEn = Text("InsuranceCompanyEN"),
            InsuranceCompanyAr = Text("InsuranceCompanyAR"),
            InsuranceCompanyId = Text("InsuranceCompanyID"),
            IssueDate = Date(Text("IssueDate")),
            ExpiryDate = Date(Text("ExpiryDate")),
            ClassName = Text("ClassName"),
            NetworkId = Text("NetworkID"),
            DeductibleRate = Number(Text("DeductibleRate")),
            MaxLimit = Number(Text("MaxLimit")),
            BeneficiaryType = Text("BeneficiaryType"),
            BeneficiaryNumber = Text("BeneficiaryNumber", "MembershipCode"),
            SponsorNumber = Text("SponsorNumber"),
            PolicyHolder = Text("PolicyHolder"),
            InsuranceStatus = Text("InsuranceStatus"),
            InsuranceType = Text("InsuranceType"),
            Name = Text("Name", "InsuredName"),
            Gender = Text("Gender") switch { "1" => "male", "2" => "female", var other => other },
            Nationality = Text("NationalityID", "NationalityCode", "Nationality"),
            DateOfBirth = Date(Text("DateOfBirth")),
            IdentityNumber = Text("IdentityNumber"),
            PassportNumber = Text("PassportNumber"),
            VisaNumber = Text("VisaNumber"),
            BorderNumber = Text("BorderNumber"),
            Raw = f,
        };
    }

    private static DateTime? Date(string? value) =>
        value != null && DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date
        : value != null && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ? date
        : null;

    private static decimal? Number(string? value) =>
        value != null && decimal.TryParse(value.TrimEnd('%'), NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;
}

/// <summary>Deterministic answers for the sandbox: the same key always gets the same policy; keys ending in 0 have none.</summary>
public static class InsuranceDiscoverySimulator
{
    private static readonly (string Id, string En, string Ar)[] Insurers =
    {
        ("101", "Sandbox Cooperative Insurance", "التأمين التعاوني التجريبي"),
        ("102", "Sandbox Medical Insurance", "التأمين الطبي التجريبي"),
        ("103", "Sandbox National Insurance", "التأمين الوطني التجريبي"),
    };

    public static InsuranceDiscoveryResult Answer(string patientKey, InsuranceSystemType systemType)
    {
        var result = new InsuranceDiscoveryResult { HttpStatus = (int)HttpStatusCode.OK, Simulated = true };
        if (patientKey.EndsWith('0'))
        {
            result.ErrorCode = NphiesInsuranceDiscoveryErrors.NoRecordFound;
            result.ErrorMessage = NphiesInsuranceDiscoveryErrors.Describe(result.ErrorCode);
            return result;
        }

        var hash = patientKey.Aggregate(17, (h, c) => unchecked(h * 31 + c)) & 0x7fffffff;
        var insurer = Insurers[hash % Insurers.Length];
        var today = DateTime.UtcNow.Date;
        var policy = new DiscoveredPolicy
        {
            PolicyNumber = $"POL{hash % 100000000:00000000}",
            InsuranceCompanyEn = insurer.En,
            InsuranceCompanyAr = insurer.Ar,
            InsuranceCompanyId = insurer.Id,
            IssueDate = today.AddMonths(-6),
            ExpiryDate = today.AddMonths(6),
            Name = "Sandbox Beneficiary",
            Gender = hash % 2 == 0 ? "male" : "female",
            Nationality = "682",
        };
        if (systemType == InsuranceSystemType.Hidp)
        {
            policy.ClassName = hash % 3 == 0 ? "VIP" : "B";
            policy.NetworkId = $"NW{hash % 5 + 1}";
            policy.DeductibleRate = 20;
            policy.MaxLimit = 500;
            policy.BeneficiaryType = hash % 4 == 0 ? "2" : "1";
            policy.BeneficiaryNumber = $"{hash % 1000000000:000000000}";
            policy.IdentityNumber = patientKey;
        }
        else
        {
            policy.PassportNumber = patientKey;
            policy.InsuranceStatus = "Active";
            policy.BeneficiaryNumber = systemType == InsuranceSystemType.Huidp ? $"HU{hash % 1000000:000000}" : null;
        }
        result.Policies.Add(policy);
        return result;
    }
}
