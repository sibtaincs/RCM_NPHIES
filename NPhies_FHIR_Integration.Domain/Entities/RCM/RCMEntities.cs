using System;
using System.Collections.Generic;

namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Payment reconciliation record
/// </summary>
public class PaymentReconciliationEntity
{
    public Guid Id { get; set; }
    public string ReconciliationNumber { get; set; } = string.Empty;
    public Guid ClaimId { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public string Status { get; set; } = "Unmatched";
    public DateTime? PaymentDate { get; set; }
    public DateTime ReconciliationDate { get; set; }
    public string? Notes { get; set; }
    public string? ReconciledBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public virtual Claim Claim { get; set; } = null!;
}

/// <summary>
/// Adjudication result for claim items
/// </summary>
public class AdjudicationResult
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public int ClaimItemSequence { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SubmittedAmount { get; set; }
    public decimal AllowedAmount { get; set; }
    public decimal DeductibleAmount { get; set; }
    public decimal CopayAmount { get; set; }
    public decimal CoinsuranceAmount { get; set; }
    public decimal InsuranceResponsibility { get; set; }
    public decimal PatientResponsibility { get; set; }
    public DateTime AdjudicatedDate { get; set; }
    public string? AdjudicationNarrative { get; set; }
    public string? RulesApplied { get; set; } // JSON
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public virtual Claim Claim { get; set; } = null!;
}

/// <summary>
/// RCM workflow state tracking
/// </summary>
public class RCMWorkflowState
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public string WorkflowStep { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnteredDate { get; set; }
    public DateTime? ExitedDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public string? PerformedBy { get; set; }

    // Navigation property
    public virtual Claim Claim { get; set; } = null!;
}
