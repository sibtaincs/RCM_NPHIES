namespace NPhies_FHIR_Integration.Application.Rcm.Dtos;

/// <summary>
/// Full editable shape of a claim / prior authorization. Used for both reading (GET .../edit) and writing
/// (POST create / PUT update). Server-computed fields (net, tax, totals, status, flags) are ignored on write.
/// </summary>
public sealed class ClaimEditDto
{
    public string? Id { get; set; }
    public string? ClaimNumber { get; set; }

    /// <summary>claim | preauthorization.</summary>
    public string Use { get; set; } = "claim";
    /// <summary>institutional | oral | pharmacy | professional | vision.</summary>
    public string ClaimType { get; set; } = "professional";
    /// <summary>op | ip | emr.</summary>
    public string? ClaimSubType { get; set; } = "op";
    public string Priority { get; set; } = "normal";
    public string? PayeeType { get; set; } = "provider";

    public string PatientId { get; set; } = string.Empty;
    public string CoverageId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string InsurerId { get; set; } = string.Empty;
    public string? PractitionerId { get; set; }
    public string? LocationId { get; set; }
    public string? EncounterId { get; set; }

    public DateTime? BillablePeriodStart { get; set; }
    public DateTime? BillablePeriodEnd { get; set; }
    public DateTime? ServicedPeriodStart { get; set; }
    public DateTime? ServicedPeriodEnd { get; set; }

    /// <summary>Comma separated preAuthRef values (claims only).</summary>
    public string? PreAuthorizationRef { get; set; }
    public string? EpisodeIdentifierValue { get; set; }
    public string? ReferralIdentifier { get; set; }
    public string? PrescriptionIdentifier { get; set; }
    public DateTime? AccidentDate { get; set; }
    public string? AccidentType { get; set; }
    public string? EligibilityOfflineReference { get; set; }
    public DateTime? EligibilityOfflineDate { get; set; }
    public DateTime? AuthorizationOfflineDate { get; set; }
    public bool IsShadowBilling { get; set; }
    public bool? IsTransfer { get; set; }
    /// <summary>Newborn using the mother's coverage (NPHIES extension-newborn).</summary>
    public bool IsNewborn { get; set; }
    /// <summary>Read-only: the patient who holds the selected coverage (the mother, for a newborn claim).</summary>
    public string? CoverageHolderPatientId { get; set; }

    public List<ClaimEditItemDto> Items { get; set; } = new();
    public List<ClaimEditDiagnosisDto> Diagnoses { get; set; } = new();
    public List<ClaimEditCareTeamDto> CareTeam { get; set; } = new();
    public List<ClaimEditSupportingInfoDto> SupportingInfo { get; set; } = new();

    /// <summary>Why the change was made (stored in the audit trail).</summary>
    public string? ChangeReason { get; set; }

    // ---- read-only (server computed) ----
    public string? NphiesStatus { get; set; }
    public bool Editable { get; set; }
    public string? LockedReason { get; set; }
    public decimal Total { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalPatientShare { get; set; }
    public string? TechnicalReviewStatus { get; set; }
    public string? MedicalReviewStatus { get; set; }
    public List<RelatedClaimDto> Related { get; set; } = new();
}

public sealed class ClaimEditItemDto
{
    public int Sequence { get; set; }
    public string ProductOrServiceCode { get; set; } = string.Empty;
    public string? ProductOrServiceSystem { get; set; }
    public string? ProductOrServiceDisplay { get; set; }
    /// <summary>Provider internal code (shadow billing / non-standard code).</summary>
    public string? AltProductOrServiceCode { get; set; }
    public DateTime? ServicedDate { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal? Factor { get; set; }
    /// <summary>VAT percentage (e.g. 15). Tax = quantity × unitPrice × factor × rate / 100.</summary>
    public decimal? TaxRate { get; set; }
    public decimal? PatientShare { get; set; }
    public int? CareTeamSequence { get; set; }
    public List<int> DiagnosisSequences { get; set; } = new();
    public List<int> InformationSequences { get; set; } = new();
    public string? BodySiteCode { get; set; }
    public string? SubSiteCode { get; set; }
    public bool IsPackage { get; set; }
    public bool IsMaternity { get; set; }
    public string? PatientInvoiceValue { get; set; }
    public string? Notes { get; set; }
    /// <summary>Vision lens specifications (vision claims only).</summary>
    public List<ClaimEditLensDto> Lenses { get; set; } = new();

    // ---- read-only (server computed) ----
    public decimal Tax { get; set; }
    public decimal Net { get; set; }
    public int DetailCount { get; set; }
}

public sealed class ClaimEditLensDto
{
    public string? Product { get; set; } = "lens";
    public string? Eye { get; set; } = "right";
    public decimal? Sphere { get; set; }
    public decimal? Cylinder { get; set; }
    public int? Axis { get; set; }
    public decimal? Prism { get; set; }
    public string? PrismBase { get; set; }
    public decimal? Add { get; set; }
    public decimal? Power { get; set; }
}

public sealed class ClaimEditDiagnosisDto
{
    public int Sequence { get; set; }
    public string DiagnosisCode { get; set; } = string.Empty;
    public string? DiagnosisDisplay { get; set; }
    /// <summary>principal | secondary | admitting | discharge ...</summary>
    public string? DiagnosisType { get; set; } = "principal";
    public string? OnAdmissionCode { get; set; }
    /// <summary>condition-onset (COEA, CNNA, NR); required on institutional claims.</summary>
    public string? ConditionOnsetCode { get; set; }
}

public sealed class ClaimEditCareTeamDto
{
    public int Sequence { get; set; }
    public string PractitionerId { get; set; } = string.Empty;
    public string? PractitionerName { get; set; }
    /// <summary>primary | assist | supervisor | other.</summary>
    public string? Role { get; set; } = "primary";
    /// <summary>Practice specialty code (e.g. 22.00).</summary>
    public string? Qualification { get; set; }
}

public sealed class ClaimEditSupportingInfoDto
{
    public int Sequence { get; set; }
    /// <summary>claim-information-category (info, onset, attachment, chief-complaint, vital-sign-weight, ...).</summary>
    public string Category { get; set; } = "info";
    public string? CodeValue { get; set; }
    public string? CodeSystem { get; set; }
    public string? StringValue { get; set; }
    public decimal? QuantityValue { get; set; }
    public string? QuantityUnit { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? AttachmentTitle { get; set; }
    public string? AttachmentContentType { get; set; }
    /// <summary>Base64 content. On read only a flag is returned (see <see cref="HasAttachment"/>); send null to keep the stored file.</summary>
    public string? AttachmentData { get; set; }
    public string? AttachmentUrl { get; set; }
    /// <summary>Stored document (api/rcm/documents) sent as the attachment; preferred over inline <see cref="AttachmentData"/>.</summary>
    public string? DocumentId { get; set; }
    public bool HasAttachment { get; set; }
    /// <summary>Set true to remove a stored attachment.</summary>
    public bool RemoveAttachment { get; set; }
}

/// <summary>One finding of the claim health check (data-quality / NPHIES readiness rule).</summary>
public sealed class ClaimHealthIssueDto
{
    /// <summary>Error | Warning | Info.</summary>
    public string Severity { get; set; } = "Error";
    public string Code { get; set; } = string.Empty;
    /// <summary>Editor location, e.g. items[2].careTeamSequence or header.coverageId.</summary>
    public string? Field { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ClaimHealthDto
{
    public string ClaimId { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public bool Ready => !Issues.Any(i => i.Severity == "Error");
    public int ErrorCount => Issues.Count(i => i.Severity == "Error");
    public int WarningCount => Issues.Count(i => i.Severity == "Warning");
    public List<ClaimHealthIssueDto> Issues { get; set; } = new();
}

/// <summary>A lifecycle-valid way to correct a claim in its current NPHIES state.</summary>
public sealed class ClaimCorrectionOptionDto
{
    /// <summary>edit | resubmit | update | extend | cancel | nullify | communicate | appeal | status-check | create-claim.</summary>
    public string Action { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Recommended { get; set; }
}

public sealed class ClaimCorrectionOptionsDto
{
    public string ClaimId { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string NphiesStatus { get; set; } = string.Empty;
    public bool Editable { get; set; }
    public string Guidance { get; set; } = string.Empty;
    public List<ClaimCorrectionOptionDto> Options { get; set; } = new();
}

public sealed class ClaimAuditEntryDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime At { get; set; }
    public string? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public List<ClaimFieldChangeDto> Changes { get; set; } = new();
}

public sealed class ClaimFieldChangeDto
{
    public string Field { get; set; } = string.Empty;
    public string? Before { get; set; }
    public string? After { get; set; }
}

/// <summary>Field-level comparison of a claim with the version it replaces (related.claim).</summary>
public sealed class ClaimVersionCompareDto
{
    public string ClaimId { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string? PreviousClaimId { get; set; }
    public string? PreviousClaimNumber { get; set; }
    public string? Relationship { get; set; }
    public List<ClaimFieldChangeDto> Changes { get; set; } = new();
}
