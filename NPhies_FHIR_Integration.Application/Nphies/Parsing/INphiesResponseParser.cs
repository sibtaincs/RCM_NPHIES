using Hl7.Fhir.Model;

namespace NPhies_FHIR_Integration.Application.Nphies.Parsing;

/// <summary>Parses NPHIES response messages (or OperationOutcome errors) into typed results.</summary>
public interface INphiesResponseParser
{
    /// <summary>Parses raw FHIR JSON. Unparseable content yields a response with a single parse error.</summary>
    NphiesResponse Parse(string json);

    /// <summary>Interprets an already-parsed resource (message Bundle or OperationOutcome).</summary>
    NphiesResponse Parse(Resource resource);

    /// <summary>Parses FHIR JSON to a resource using NPHIES-tolerant settings. Throws on invalid JSON.</summary>
    Resource ParseResource(string json);
}
