using System.Text.Json;
using System.Text.Json.Serialization;

namespace NPhies_FHIR_Integration.Infrastructure.FHIR;

/// <summary>
/// FHIR JSON serialization utilities
/// Handles conversion between domain entities and FHIR-compliant JSON
/// </summary>
public static class FhirJsonSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true,
   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
      new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

  /// <summary>
 /// Serialize object to FHIR-compliant JSON string
    /// </summary>
    public static string Serialize<T>(T obj) where T : class
    {
        if (obj == null)
          return string.Empty;

        try
    {
            return JsonSerializer.Serialize(obj, DefaultOptions);
        }
  catch (JsonException ex)
 {
       throw new InvalidOperationException($"Failed to serialize FHIR object: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserialize FHIR JSON string to object
    /// </summary>
public static T? Deserialize<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
 {
       return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }
        catch (JsonException ex)
        {
          throw new InvalidOperationException($"Failed to deserialize FHIR JSON: {ex.Message}", ex);
   }
    }

    /// <summary>
 /// Get JSON serializer options for FHIR
    /// </summary>
    public static JsonSerializerOptions GetOptions()
    {
 return new(DefaultOptions);
    }
}

/// <summary>
/// FHIR Bundle structure for message wrapping
/// </summary>
public class FhirBundle
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "Bundle";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("meta")]
public FhirMeta? Meta { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "message";

  [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("entry")]
    public List<FhirBundleEntry> Entry { get; set; } = new();
}

/// <summary>
/// FHIR Bundle Meta information
/// </summary>
public class FhirMeta
{
    [JsonPropertyName("profile")]
    public List<string> Profile { get; set; } = new();
}

/// <summary>
/// FHIR Bundle Entry
/// </summary>
public class FhirBundleEntry
{
    [JsonPropertyName("fullUrl")]
public string FullUrl { get; set; } = string.Empty;

    [JsonPropertyName("resource")]
    public JsonElement Resource { get; set; }
}

/// <summary>
/// FHIR Coding structure
/// </summary>
public class FhirCoding
{
    [JsonPropertyName("system")]
    public string System { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("display")]
    public string? Display { get; set; }
}

/// <summary>
/// FHIR CodeableConcept structure
/// </summary>
public class FhirCodeableConcept
{
    [JsonPropertyName("coding")]
    public List<FhirCoding> Coding { get; set; } = new();

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// FHIR Identifier structure
/// </summary>
public class FhirIdentifier
{
    [JsonPropertyName("system")]
    public string System { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// FHIR Money structure
/// </summary>
public class FhirMoney
{
    [JsonPropertyName("value")]
    public decimal Value { get; set; }

 [JsonPropertyName("currency")]
    public string Currency { get; set; } = "SAR";
}

/// <summary>
/// FHIR Period structure
/// </summary>
public class FhirPeriod
{
    [JsonPropertyName("start")]
    public DateTime? Start { get; set; }

    [JsonPropertyName("end")]
    public DateTime? End { get; set; }
}

/// <summary>
/// FHIR Reference structure
/// </summary>
public class FhirReference
{
    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    [JsonPropertyName("display")]
    public string? Display { get; set; }
}

/// <summary>
/// FHIR Address structure
/// </summary>
public class FhirAddress
{
    [JsonPropertyName("use")]
    public string? Use { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("line")]
    public List<string>? Line { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("postalCode")]
  public string? PostalCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

/// <summary>
/// FHIR ContactPoint structure
/// </summary>
public class FhirContactPoint
{
    [JsonPropertyName("system")]
    public string System { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("use")]
    public string? Use { get; set; }
}

/// <summary>
/// FHIR HumanName structure
/// </summary>
public class FhirHumanName
{
    [JsonPropertyName("use")]
public string? Use { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("given")]
    public List<string>? Given { get; set; }
}
