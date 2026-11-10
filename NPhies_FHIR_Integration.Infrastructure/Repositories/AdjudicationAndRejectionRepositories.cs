using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Infrastructure.Repositories
{
    /// <summary>
    /// Interface for AdjudicationDetail repository operations
    /// </summary>
    public interface IAdjudicationDetailRepository : IRepository<AdjudicationDetailEntity>
    {
    /// <summary>
  /// Get adjudication details for a claim response
        /// </summary>
        Task<List<AdjudicationDetailEntity>> GetByClaimResponseIdAsync(int claimResponseId);

      /// <summary>
        /// Get adjudication details by item sequence
        /// </summary>
   Task<List<AdjudicationDetailEntity>> GetByItemSequenceAsync(int claimResponseId, int itemSequence);

        /// <summary>
        /// Get all denied items for a claim response
        /// </summary>
        Task<List<AdjudicationDetailEntity>> GetDeniedItemsAsync(int claimResponseId);

    /// <summary>
        /// Get adjudication summary for a claim response
        /// </summary>
        Task<AdjudicationSummary> GetAdjudicationSummaryAsync(int claimResponseId);
    }

    /// <summary>
    /// Adjudication summary
    /// </summary>
    public class AdjudicationSummary
    {
        public int ClaimResponseId { get; set; }
    public int TotalItems { get; set; }
 public int ApprovedItems { get; set; }
        public int DeniedItems { get; set; }
      public int PendingItems { get; set; }
 public decimal TotalApprovedAmount { get; set; }
        public decimal TotalDeniedAmount { get; set; }
        public decimal TotalDeductibleApplied { get; set; }
        public decimal TotalCoinsuranceApplied { get; set; }
        public decimal TotalOutOfPocketApplied { get; set; }
   public decimal TotalPatientResponsibility { get; set; }
 }

    /// <summary>
 /// Implementation of AdjudicationDetail repository
    /// </summary>
    public class AdjudicationDetailRepository : Repository<AdjudicationDetailEntity>, IAdjudicationDetailRepository
    {
        private readonly ApplicationDbContext _context;

        public AdjudicationDetailRepository(ApplicationDbContext context) : base(context)
      {
     _context = context;
        }

        public async Task<List<AdjudicationDetailEntity>> GetByClaimResponseIdAsync(int claimResponseId)
        {
    return await _context.Set<AdjudicationDetailEntity>()
          .Where(a => a.ClaimResponseId == claimResponseId)
     .OrderBy(a => a.ItemSequence)
         .ToListAsync();
      }

        public async Task<List<AdjudicationDetailEntity>> GetByItemSequenceAsync(int claimResponseId, int itemSequence)
   {
 return await _context.Set<AdjudicationDetailEntity>()
 .Where(a => a.ClaimResponseId == claimResponseId && a.ItemSequence == itemSequence)
   .ToListAsync();
      }

 public async Task<List<AdjudicationDetailEntity>> GetDeniedItemsAsync(int claimResponseId)
    {
        return await _context.Set<AdjudicationDetailEntity>()
 .Where(a => a.ClaimResponseId == claimResponseId && a.Status == "denied")
         .ToListAsync();
    }

        public async Task<AdjudicationSummary> GetAdjudicationSummaryAsync(int claimResponseId)
        {
 var items = await GetByClaimResponseIdAsync(claimResponseId);

     var summary = new AdjudicationSummary
   {
 ClaimResponseId = claimResponseId,
      TotalItems = items.Count,
   ApprovedItems = items.Count(i => i.Status == "approved"),
 DeniedItems = items.Count(i => i.Status == "denied"),
PendingItems = items.Count(i => i.Status == "pending"),
     TotalApprovedAmount = items.Where(i => i.Status == "approved").Sum(i => i.Amount),
    TotalDeniedAmount = items.Where(i => i.Status == "denied").Sum(i => i.Amount),
      TotalDeductibleApplied = items.Sum(i => i.DeductibleApplied),
TotalCoinsuranceApplied = items.Sum(i => i.CoinsuranceApplied),
     TotalOutOfPocketApplied = items.Sum(i => i.OutOfPocketApplied),
      TotalPatientResponsibility = items.Sum(i => i.PatientResponsibility)
       };

   return summary;
        }
    }

 /// <summary>
    /// Interface for RejectionReason repository operations
    /// </summary>
    public interface IRejectionReasonRepository : IRepository<RejectionReasonEntity>
    {
        /// <summary>
  /// Get rejection reason by code
  /// </summary>
        Task<RejectionReasonEntity> GetByCodeAsync(string reasonCode);

        /// <summary>
     /// Get all active rejection reasons
        /// </summary>
        Task<List<RejectionReasonEntity>> GetActiveReasonsAsync();

  /// <summary>
        /// Get rejection reasons for a specific entity type
        /// </summary>
  Task<List<RejectionReasonEntity>> GetByEntityTypeAsync(string entityType);

        /// <summary>
        /// Get recoverable rejection reasons
        /// </summary>
        Task<List<RejectionReasonEntity>> GetRecoverableReasonsAsync();
    }

 /// <summary>
  /// Implementation of RejectionReason repository
    /// </summary>
    public class RejectionReasonRepository : Repository<RejectionReasonEntity>, IRejectionReasonRepository
    {
        private readonly ApplicationDbContext _context;

        public RejectionReasonRepository(ApplicationDbContext context) : base(context)
        {
 _context = context;
    }

     public async Task<RejectionReasonEntity> GetByCodeAsync(string reasonCode)
    {
   return await _context.Set<RejectionReasonEntity>()
       .FirstOrDefaultAsync(r => r.RejectionCode == reasonCode);
        }

        public async Task<List<RejectionReasonEntity>> GetActiveReasonsAsync()
        {
 return await _context.Set<RejectionReasonEntity>()
       .Where(r => !r.IsResolved)
   .OrderBy(r => r.RejectionCode)
         .ToListAsync();
  }

  public async Task<List<RejectionReasonEntity>> GetByEntityTypeAsync(string entityType)
    {
    return await _context.Set<RejectionReasonEntity>()
   .Where(r => (entityType == "Claim" && r.ClaimId != null) || 
(entityType == "ClaimItem" && r.ClaimItemId != null) || 
    (entityType == "ClaimResponse" && r.ClaimResponseId != null))
       .ToListAsync();
        }

        public async Task<List<RejectionReasonEntity>> GetRecoverableReasonsAsync()
   {
      return await _context.Set<RejectionReasonEntity>()
        .Where(r => r.IsRecoverable && !r.IsResolved)
    .OrderBy(r => r.RejectionCode)
          .ToListAsync();
        }
    }
}
