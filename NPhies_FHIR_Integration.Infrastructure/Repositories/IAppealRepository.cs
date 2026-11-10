using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Appeal management
/// Defines data access contract for appeal operations
/// </summary>
public interface IAppealRepository
{
  // ========== CREATE OPERATIONS ==========
    /// <summary>
 /// Add new appeal to repository
    /// </summary>
  Task<AppealRequest> AddAsync(AppealRequest appeal, CancellationToken cancellationToken = default);

 /// <summary>
    /// Add multiple appeals in batch
    /// </summary>
    Task AddRangeAsync(IEnumerable<AppealRequest> appeals, CancellationToken cancellationToken = default);

    // ========== READ OPERATIONS ==========
    /// <summary>
    /// Get appeal by ID
    /// </summary>
    Task<AppealRequest?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get appeal by appeal number
    /// </summary>
    Task<AppealRequest?> GetByAppealNumberAsync(string appealNumber, CancellationToken cancellationToken = default);

  /// <summary>
 /// Get all appeals (with optional filtering)
    /// </summary>
    Task<List<AppealRequest>> GetAllAsync(CancellationToken cancellationToken = default);

// ========== APPEALS BY RELATIONSHIP ==========
    /// <summary>
 /// Get all appeals for a specific claim
    /// </summary>
    Task<List<AppealRequest>> GetByClaimIdAsync(string claimId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all appeals for a specific patient
    /// </summary>
    Task<List<AppealRequest>> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default);

    /// <summary>
  /// Get all appeals for a specific insurer
    /// </summary>
    Task<List<AppealRequest>> GetByInsurerIdAsync(string insurerId, CancellationToken cancellationToken = default);

    /// <summary>
 /// Get all appeals for a specific provider
    /// </summary>
 Task<List<AppealRequest>> GetByProviderIdAsync(string providerId, CancellationToken cancellationToken = default);

    // ========== APPEAL STATUS QUERIES ==========
  /// <summary>
    /// Get active appeals (not closed/withdrawn)
    /// </summary>
    Task<List<AppealRequest>> GetActiveAppealsAsync(CancellationToken cancellationToken = default);

    /// <summary>
  /// Get appeals by status
  /// </summary>
  Task<List<AppealRequest>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
 /// Get appeals by level
    /// </summary>
 Task<List<AppealRequest>> GetByLevelAsync(int level, CancellationToken cancellationToken = default);

    // ========== TIMELINE QUERIES ==========
    /// <summary>
    /// Get appeals nearing deadline (within specified days)
/// </summary>
    Task<List<AppealRequest>> GetAppealsNearingDeadlineAsync(
    int daysThreshold = 5,
     CancellationToken cancellationToken = default);

 /// <summary>
    /// Get appeals past deadline (deadline exceeded)
    /// </summary>
    Task<List<AppealRequest>> GetAppealsPastDeadlineAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get submitted appeals not yet decided
    /// </summary>
    Task<List<AppealRequest>> GetPendingDecisionAppealsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get decided appeals (approved, denied, partial)
    /// </summary>
    Task<List<AppealRequest>> GetDecidedAppealsAsync(CancellationToken cancellationToken = default);

    // ========== UPDATE OPERATIONS ==========
    /// <summary>
    /// Update appeal
    /// </summary>
    Task<AppealRequest> UpdateAsync(AppealRequest appeal, CancellationToken cancellationToken = default);

  /// <summary>
    /// Update appeal status
    /// </summary>
    Task UpdateStatusAsync(string appealId, string newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark appeal as submitted
    /// </summary>
    Task MarkAsSubmittedAsync(string appealId, DateTime submittedDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark appeal as withdrawn
    /// </summary>
Task MarkAsWithdrawnAsync(string appealId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record appeal decision
    /// </summary>
    Task RecordDecisionAsync(
  string appealId,
        string outcome,
        decimal? approvedAmount,
        string explanation,
        CancellationToken cancellationToken = default);

    // ========== DELETE OPERATIONS ==========
    /// <summary>
    /// Delete appeal (soft delete - mark as deleted)
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    // ========== STATUS HISTORY ==========
    /// <summary>
    /// Add status history entry
    /// </summary>
    Task AddStatusHistoryAsync(
        string appealId,
  AppealStatusHistory history,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get status history for appeal
    /// </summary>
    Task<List<AppealStatusHistory>> GetStatusHistoryAsync(
        string appealId,
   CancellationToken cancellationToken = default);

// ========== DOCUMENTS ==========
    /// <summary>
    /// Add document to appeal
/// </summary>
    Task AddDocumentAsync(
        string appealId,
        AppealDocument document,
  CancellationToken cancellationToken = default);

    /// <summary>
    /// Get documents for appeal
    /// </summary>
    Task<List<AppealDocument>> GetDocumentsAsync(
    string appealId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove document from appeal
    /// </summary>
    Task RemoveDocumentAsync(string appealId, string documentId, CancellationToken cancellationToken = default);

    // ========== STATISTICS ==========
    /// <summary>
/// Get total number of appeals
/// </summary>
  Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count by status
    /// </summary>
    Task<int> GetCountByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get approval rate percentage
    /// </summary>
  Task<decimal> GetApprovalRateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total approved amount
    /// </summary>
    Task<decimal> GetTotalApprovedAmountAsync(CancellationToken cancellationToken = default);

    // ========== SAVE ==========
    /// <summary>
    /// Save changes to database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
