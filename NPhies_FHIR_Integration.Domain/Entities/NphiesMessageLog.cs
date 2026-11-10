namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Audit record of a raw NPHIES message exchange (request and response bundles as sent/received).
/// Contains PHI: access must be restricted.
/// </summary>
public class NphiesMessageLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>MessageHeader.id of the outgoing message (correlates with MessageHeader.response.identifier).</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Bundle.id of the outgoing message.</summary>
    public string BundleId { get; set; } = string.Empty;

    /// <summary>Optional business correlation id (e.g. claim identifier) supplied by the caller.</summary>
    public string? CorrelationId { get; set; }

    public string EventCode { get; set; } = string.Empty;
    public string? ResponseEventCode { get; set; }

    /// <summary>"Outbound" for messages we send.</summary>
    public string Direction { get; set; } = "Outbound";

    public string Endpoint { get; set; } = string.Empty;
    public int? HttpStatus { get; set; }

    /// <summary>MessageHeader.response.code (ok, transient-error, fatal-error).</summary>
    public string? ResponseCode { get; set; }

    public string? ResponseMessageId { get; set; }
    public string RequestJson { get; set; } = string.Empty;
    public string? ResponseJson { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? DurationMs { get; set; }
}
