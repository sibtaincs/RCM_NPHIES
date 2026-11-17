namespace NPhies_FHIR_Integration.Application.Rcm;

/// <summary>
/// Code lists and limits behind the claim and prior authorization checks of the claim editor (IG 6.3, Newborn v1.2 and
/// the NPHIES validation error codes). The lists are copied from the nphies-fs package so the editor can explain a
/// problem before NPHIES rejects the message.
/// </summary>
public static class RcmClaimRules
{
    private const string N = "http://nphies.sa/terminology/CodeSystem/";

    /// <summary>CodeSystem rta-diagnosis: a diagnosis from it requires accident type MVA, and MVA requires one of them.</summary>
    public static readonly HashSet<string> RoadTrafficDiagnoses = new(StringComparer.OrdinalIgnoreCase)
    {
        "V00.13", "V00.14", "V00.15", "V10.4", "V21.99", "V24.48", "V40.60", "V40.61", "V40.62", "V43.62", "V43.63", "V60.6", "V83.2", "V87.0",
    };

    /// <summary>CodeSystem work-related-diagnosis (Y96, work-related condition): requires accident type WPA, and WPA requires it.</summary>
    public static bool IsWorkRelated(string? code) => code?.Trim().StartsWith("Y96", StringComparison.OrdinalIgnoreCase) == true;

    public static bool IsRoadTraffic(string? code) => code != null && RoadTrafficDiagnoses.Contains(code.Trim());

    /// <summary>A newborn's diagnosis must include one of Z38.0 to Z38.8 (born alive, by place of birth).</summary>
    public static bool IsNewbornDiagnosis(string? code) => code?.Trim().StartsWith("Z38", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>The CodeSystem of medication codes: a pharmacy item with one needs exactly one days-supply.</summary>
    public const string MedicationCodes = N + "medication-codes";

    /// <summary>
    /// Vital signs NPHIES requires on institutional and professional requests. When one was not measured, a code from
    /// its absence-reason CodeSystem is sent in <c>supportingInfo.reason</c> instead of a value.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string System, IReadOnlyDictionary<string, string> Codes)> AbsenceReasons =
        new Dictionary<string, (string, IReadOnlyDictionary<string, string>)>(StringComparer.OrdinalIgnoreCase)
        {
            ["vital-sign-systolic"] = (N + "blood-pressure-absence-reason", BloodPressure()),
            ["vital-sign-diastolic"] = (N + "blood-pressure-absence-reason", BloodPressure()),
            ["vital-sign-height"] = (N + "height-absence-reason", new Dictionary<string, string> { ["999.9"] = "Not measured" }),
            ["vital-sign-weight"] = (N + "weight-absence-reason", new Dictionary<string, string> { ["999.9"] = "Not collected" }),
            ["temperature"] = (N + "temperature-absence-reason", new Dictionary<string, string> { ["88"] = "Not obtained", ["99"] = "Unknown" }),
            ["pulse"] = (N + "pulse-absence-reason", new Dictionary<string, string>
            {
                ["999"] = "Not stated / inadequately described", ["997"] = "Cardiac arrest", ["998"] = "Not recorded",
            }),
            ["respiratory-rate"] = (N + "respiratory-rate-absence-reason", new Dictionary<string, string>
            {
                ["88"] = "Not obtained", ["99"] = "Unknown", ["00"] = "Artificial ventilation",
            }),
            ["oxygen-saturation"] = (N + "oxygen-saturation-absence-reason", new Dictionary<string, string> { ["999"] = "Unknown", ["888"] = "Not obtained" }),
        };

    private static Dictionary<string, string> BloodPressure() => new() { ["999"] = "Unknown", ["888"] = "Not measurable" };

    /// <summary>The absence-reason CodeSystem for a vital sign, or null when the category has none.</summary>
    public static string? AbsenceReasonSystem(string? category) =>
        category != null && AbsenceReasons.TryGetValue(category, out var reasons) ? reasons.System : null;

    /// <summary>Payer codes that mean the claim is a duplicate (1677, 1678 and denial N-DC-069).</summary>
    public static readonly HashSet<string> DuplicateCodes = new(StringComparer.OrdinalIgnoreCase) { "1677", "1678", "N-DC-069" };
}
