using System.Text.Json;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Application.Rcm;

public enum RcmErrorKind { BadRequest, NotFound, Conflict }

/// <summary>Business-rule failure in an RCM workflow; controllers map <see cref="Kind"/> to 400/404/409.</summary>
public sealed class RcmWorkflowException : Exception
{
    public RcmErrorKind Kind { get; }
    public List<string> Errors { get; }

    public RcmWorkflowException(RcmErrorKind kind, string message, IEnumerable<string>? errors = null) : base(message)
    {
        Kind = kind;
        Errors = errors?.ToList() ?? new List<string>();
    }

    public static RcmWorkflowException NotFound(string what, string id) => new(RcmErrorKind.NotFound, $"{what} '{id}' was not found.");
    public static RcmWorkflowException Conflict(string message) => new(RcmErrorKind.Conflict, message);
    public static RcmWorkflowException Invalid(string message, IEnumerable<string>? errors = null) => new(RcmErrorKind.BadRequest, message, errors);
}

/// <summary>Entity → DTO projections shared by the RCM services.</summary>
public static class RcmProjections
{
    public static object? Json(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<JsonElement>(json); }
        catch (JsonException) { return json; }
    }

    public static RcmClaimSummaryDto ToSummary(Claim c, string? preAuthRef = null, decimal? approvedTax = null)
    {
        var dto = ToSummaryCore(c, preAuthRef);
        dto.ApprovedTax = approvedTax;
        if (c.ApprovedAmount.HasValue)
            dto.ExpectedPayment = c.ApprovedAmount + (approvedTax ?? 0m);
        if (dto.ExpectedPayment.HasValue && c.PaidAmount.HasValue)
            dto.PaymentVariance = dto.ExpectedPayment - c.PaidAmount;
        return dto;
    }

    private static RcmClaimSummaryDto ToSummaryCore(Claim c, string? preAuthRef) => new()
    {
        Id = c.Id,
        ClaimNumber = c.ClaimNumber,
        Identifier = c.ClaimIdentifierValue,
        Use = c.Use,
        ClaimType = c.ClaimType,
        ClaimSubType = c.ClaimSubType,
        PatientId = c.PatientId,
        PatientName = c.Patient is null ? null : (string.IsNullOrWhiteSpace(c.Patient.FullName) ? $"{c.Patient.FirstName} {c.Patient.LastName}".Trim() : c.Patient.FullName),
        PatientNameArabic = c.Patient?.FullNameArabic,
        ProviderId = c.ProviderId,
        InsurerId = c.InsurerId,
        InsurerName = c.Insurer?.OrganizationName,
        Status = c.Status,
        NphiesStatus = c.NphiesStatus ?? RcmClaimStatus.Draft,
        Outcome = c.NphiesOutcome,
        AdjudicationOutcome = c.AdjudicationOutcome,
        Total = c.Total,
        ApprovedAmount = c.ApprovedAmount,
        PatientShareAmount = c.PatientShareAmount,
        PaidAmount = c.PaidAmount,
        PreAuthRef = preAuthRef ?? c.PreAuthorizationRef,
        PreAuthPeriodEnd = c.PreAuthPeriodEnd,
        IsShadowBilling = c.IsShadowBilling,
        BatchIdentifier = c.BatchIdentifier,
        CreatedDate = c.CreatedDate,
        SubmittedAt = c.SubmittedAt,
        AdjudicatedAt = c.AdjudicatedAt,
        PaidAt = c.PaidAt,
    };

    public static ClaimAdjudicationDto ToDto(ClaimResponse r) => new()
    {
        ClaimResponseId = r.Id,
        ClaimId = r.ClaimId,
        ResponseIdentifier = r.ResponseIdentifierValue,
        Outcome = r.Outcome,
        AdjudicationOutcome = r.AdjudicationOutcome,
        Disposition = r.Disposition,
        PreAuthRef = r.PreAuthRef,
        PreAuthPeriodStart = r.PreAuthPeriodStart,
        PreAuthPeriodEnd = r.PreAuthPeriodEnd,
        TotalSubmitted = r.TotalSubmitted,
        TotalEligible = r.TotalEligible,
        TotalBenefit = r.TotalBenefit,
        TotalCopay = r.TotalCopay,
        TotalTax = r.TotalTax,
        TotalPatientShare = r.TotalPatientShare,
        PaymentAmount = r.PaymentAmount,
        PaymentDate = r.PaymentDate,
        ReceivedAt = r.ReceivedAt,
        Items = r.Items.OrderBy(i => i.ItemSequence).Select(i => new ItemAdjudicationDto
        {
            Sequence = i.ItemSequence,
            Outcome = i.Outcome,
            Submitted = i.Submitted,
            Eligible = i.Eligible,
            Benefit = i.Benefit,
            Copay = i.Copay,
            Deductible = i.Deductible,
            Tax = i.Tax,
            PatientShare = i.PatientShare,
            ApprovedQuantity = i.ApprovedQuantity,
            ReasonCode = i.ReasonCode,
            ReasonDisplay = i.ReasonDisplay,
        }).ToList(),
    };

    public static TransactionDto ToDto(NphiesTransaction t) => new()
    {
        Id = t.Id,
        Direction = t.Direction,
        Operation = t.Operation,
        EventCode = t.EventCode,
        MessageId = t.MessageId,
        EntityType = t.EntityType,
        EntityId = t.EntityId,
        Status = t.Status,
        ResponseCode = t.ResponseCode,
        Outcome = t.Outcome,
        ErrorSummary = t.ErrorSummary,
        SentAt = t.SentAt,
        CompletedAt = t.CompletedAt,
    };

    public static DenialDto ToDto(ClaimDenial d, string? claimNumber = null, string? itemCode = null, string? insurerName = null) => new()
    {
        Id = d.Id,
        ClaimId = d.ClaimId,
        ClaimNumber = claimNumber ?? d.Claim?.ClaimNumber,
        ClaimResponseId = d.ClaimResponseId,
        ItemSequence = d.ItemSequence,
        ItemCode = itemCode,
        ReasonCode = d.ReasonCode,
        ReasonDisplay = d.ReasonDisplay,
        Category = d.Category,
        SubmittedAmount = d.SubmittedAmount,
        DeniedAmount = d.DeniedAmount,
        Status = d.Status,
        DenialDate = d.DenialDate,
        AppealDeadline = d.AppealDeadline,
        DaysToDeadline = (int)Math.Floor((d.AppealDeadline.Date - DateTime.UtcNow.Date).TotalDays),
        AppealRequestId = d.AppealRequestId,
        ResubmissionClaimId = d.ResubmissionClaimId,
        ProviderId = d.ProviderId,
        InsurerId = d.InsurerId,
        InsurerName = insurerName,
        Notes = d.Notes,
    };

    public static CommunicationRequestDto ToDto(CommunicationRequest r) => new()
    {
        Id = r.Id,
        Identifier = r.IdentifierValue,
        Status = r.Status,
        Category = r.Category,
        Priority = r.Priority,
        ClaimId = r.ClaimId,
        ClaimIdentifier = r.AboutIdentifierValue,
        PayloadText = r.PayloadContent,
        Payloads = Json(r.PayloadsJson),
        Reason = r.Reason,
        ReceivedAt = r.ReceivedAt ?? r.CreatedAt,
        RespondedCommunicationId = r.RespondedCommunicationId,
        RespondedAt = r.RespondedAt,
    };

    public static CommunicationDto ToDto(Communication c) => new()
    {
        Id = c.Id,
        Identifier = c.IdentifierValue,
        Direction = c.Direction,
        Status = c.Status,
        Category = c.Category,
        Priority = c.Priority,
        ClaimId = c.ClaimId,
        CommunicationRequestId = c.CommunicationRequestId,
        PayloadText = c.PayloadContent,
        Payloads = Json(c.PayloadsJson),
        ProcessingStatus = c.ProcessingStatus,
        SentAt = c.SentAt,
        CreatedAt = c.CreatedAt,
    };

    public static PaymentDetailDto ToDto(PaymentReconciliationDetail d, string? claimNumber = null) => new()
    {
        Id = d.Id,
        PaymentReconciliationId = d.PaymentReconciliationId,
        DetailType = d.DetailType,
        DetailIdentifier = d.DetailIdentifier,
        PredecessorIdentifier = d.PredecessorIdentifier,
        RequestIdentifier = d.RequestIdentifierValue,
        ResponseIdentifier = d.ResponseIdentifierValue,
        Date = d.DetailDate,
        Amount = d.Amount,
        ComponentPayment = d.ComponentPayment,
        EarlyFee = d.EarlyFee,
        NphiesFee = d.NphiesFee,
        MatchedClaimId = d.MatchedClaimId,
        ClaimNumber = claimNumber,
        ExpectedAmount = d.ExpectedAmount,
        VarianceAmount = d.VarianceAmount,
        MatchStatus = d.MatchStatus,
    };

    public static PaymentReconciliationDto ToDto(PaymentReconciliation p, IReadOnlyDictionary<string, string>? claimNumbers = null) => new()
    {
        Id = p.Id,
        PaymentReconciliationId = p.PaymentReconciliationId,
        Identifier = p.IdentifierValue,
        Status = p.Status,
        Outcome = p.Outcome,
        Disposition = p.Disposition,
        PeriodStart = p.PeriodStart,
        PeriodEnd = p.PeriodEnd,
        PaymentDate = p.PaymentDate,
        PaymentAmount = p.PaymentAmount,
        PaymentCurrency = p.PaymentCurrency,
        PaymentIdentifier = p.PaymentIdentifierValue,
        PayerId = p.PaymentIssuerId,
        PayerName = p.PaymentIssuer?.OrganizationName,
        MatchStatus = p.MatchStatus,
        ExpectedAmount = p.ExpectedAmount,
        VarianceAmount = p.VarianceAmount,
        TotalRecouped = p.Details.Where(d => d.MatchStatus == RcmPaymentMatching.Recoupment).Sum(d => d.ComponentPayment ?? d.Amount),
        TotalNphiesFee = p.Details.Sum(d => d.NphiesFee ?? 0m),
        TotalEarlyFee = p.Details.Sum(d => d.EarlyFee ?? 0m),
        AcknowledgementStatus = p.AcknowledgementStatus,
        AcknowledgedAt = p.AcknowledgedAt,
        AcknowledgementError = p.AcknowledgementError,
        ReceivedAt = p.ReceivedAt,
        Details = p.Details.Select(d => ToDto(d, d.MatchedClaimId != null && claimNumbers != null && claimNumbers.TryGetValue(d.MatchedClaimId, out var n) ? n : null)).ToList(),
    };

    public static EligibilityCheckDto ToDto(EligibilityCheck e) => new()
    {
        Id = e.Id,
        PatientId = e.PatientId,
        CoverageId = e.CoverageId,
        ProviderId = e.ProviderId,
        InsurerId = e.InsurerId,
        Purposes = e.Purposes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
        ServicedDate = e.ServicedDate,
        RequestIdentifier = e.RequestIdentifierValue,
        MessageId = e.MessageId,
        Status = e.Status,
        Outcome = e.Outcome,
        IsEligible = e.IsEligible,
        InForce = e.InForce,
        SiteEligibility = e.SiteEligibility,
        Disposition = e.Disposition,
        NotInForceReason = e.NotInForceReason,
        BenefitPeriodStart = e.BenefitPeriodStart,
        BenefitPeriodEnd = e.BenefitPeriodEnd,
        ResponseIdentifier = e.ResponseIdentifier,
        Benefits = Json(e.BenefitsJson),
        DiscoveredCoverages = Json(e.DiscoveredCoveragesJson),
        Errors = Json(e.ErrorsJson),
        CreatedAt = e.CreatedAt,
        RespondedAt = e.RespondedAt,
    };
}
