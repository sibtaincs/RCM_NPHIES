namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimSupportingInfo entity - represents supporting information in a claim
/// FHIR Resource: Claim.supportingInfo
/// Examples: chief complaint, vital signs, clinical notes, etc.
/// </summary>
public class ClaimSupportingInfo : BaseEntity
{
    /// <summary>
    /// Claim ID this supporting info belongs to
    /// </summary>
    public string ClaimId { get; set; } = string.Empty;

    /// <summary>
    /// Claim
    /// </summary>
    public Claim? Claim { get; set; }

    /// <summary>
    /// Supporting info sequence number
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
/// Category code (e.g., "chief-complaint", "vital-sign-systolic", "vital-sign-diastolic", "height", "weight", etc.)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
 /// Category system URL (e.g., "http://nphies.sa/terminology/CodeSystem/claim-information-category")
 /// </summary>
    public string? CategorySystem { get; set; }

    /// <summary>
    /// Category display text
    /// </summary>
 public string? CategoryDisplay { get; set; }

    /// <summary>
    /// Code value (if applicable)
    /// </summary>
    public string? CodeValue { get; set; }

    /// <summary>
    /// Code system URL (if applicable)
  /// </summary>
  public string? CodeSystem { get; set; }

    /// <summary>
    /// String/text value
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// Quantity value (numeric)
    /// </summary>
    public decimal? QuantityValue { get; set; }

    /// <summary>
    /// Quantity unit (e.g., "mm[Hg]", "cm", "kg", "/min", "%", "Cel")
    /// </summary>
    public string? QuantityUnit { get; set; }

    /// <summary>
    /// Quantity unit system (e.g., "http://unitsofmeasure.org")
    /// </summary>
    public string? QuantitySystem { get; set; }

    /// <summary>
    /// Date value (if applicable)
    /// </summary>
    public DateTime? DateValue { get; set; }

    /// <summary>
    /// Boolean value (if applicable)
    /// </summary>
    public bool? BooleanValue { get; set; }

    /// <summary>
  /// Notes/remarks for this supporting info
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>valueAttachment title (e.g. lab report file name).</summary>
    public string? AttachmentTitle { get; set; }

    /// <summary>valueAttachment MIME type (application/pdf, image/jpeg, ...).</summary>
    public string? AttachmentContentType { get; set; }

    /// <summary>valueAttachment base64 content.</summary>
    public string? AttachmentData { get; set; }

    /// <summary>Stored document whose content is embedded as valueAttachment.data when the bundle is built (preferred over <see cref="AttachmentData"/>).</summary>
    public string? DocumentId { get; set; }

    /// <summary>valueAttachment URL (when the content is hosted rather than embedded).</summary>
    public string? AttachmentUrl { get; set; }
}
