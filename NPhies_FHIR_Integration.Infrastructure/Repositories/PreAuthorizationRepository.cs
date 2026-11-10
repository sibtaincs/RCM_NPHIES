using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Infrastructure.Repositories;

/// <summary>
/// Pre-Authorization repository implementation
/// </summary>
public class PreAuthorizationRepository : Repository<PreAuthorizationRequest>, IPreAuthorizationRepository
{
    public PreAuthorizationRepository(ApplicationDbContext context) : base(context)
    {
    }

 /// <summary>
    /// Get pre-authorization request with all related data
    /// </summary>
    public async Task<PreAuthorizationRequest?> GetByIdWithDetailsAsync(string id)
    {
    return await Context.PreAuthorizationRequests
      .Include(p => p.Patient)
        .Include(p => p.Provider)
            .Include(p => p.Insurer)
        .Include(p => p.Coverage)
    .Include(p => p.MessageHeader)
     .Include(p => p.Items)
                .ThenInclude(i => i.Location)
            .Include(p => p.Items)
  .ThenInclude(i => i.Practitioner)
 .Include(p => p.Diagnoses)
            .Include(p => p.SupportingInfo)
          .Include(p => p.Response)
       .ThenInclude(r => r!.ResponseItems)
          .Include(p => p.Response)
     .ThenInclude(r => r!.Errors)
 .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Get pre-authorization requests by patient ID
    /// </summary>
    public async Task<IEnumerable<PreAuthorizationRequest>> GetByPatientIdAsync(string patientId)
    {
        return await Context.PreAuthorizationRequests
         .Include(p => p.Provider)
     .Include(p => p.Insurer)
        .Include(p => p.Items)
        .Include(p => p.Response)
.Where(p => p.PatientId == patientId)
         .OrderByDescending(p => p.EnteredDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get pre-authorization requests by provider ID
    /// </summary>
    public async Task<IEnumerable<PreAuthorizationRequest>> GetByProviderIdAsync(string providerId)
    {
    return await Context.PreAuthorizationRequests
       .Include(p => p.Patient)
   .Include(p => p.Insurer)
      .Include(p => p.Items)
            .Include(p => p.Response)
  .Where(p => p.ProviderId == providerId)
       .OrderByDescending(p => p.EnteredDate)
      .ToListAsync();
    }

    /// <summary>
    /// Get pre-authorization requests by insurer ID
    /// </summary>
    public async Task<IEnumerable<PreAuthorizationRequest>> GetByInsurerIdAsync(string insurerId)
    {
        return await Context.PreAuthorizationRequests
 .Include(p => p.Patient)
            .Include(p => p.Provider)
  .Include(p => p.Items)
       .Include(p => p.Response)
      .Where(p => p.InsurerId == insurerId)
          .OrderByDescending(p => p.EnteredDate)
     .ToListAsync();
    }

    /// <summary>
    /// Get pre-authorization request by request identifier
    /// </summary>
    public async Task<PreAuthorizationRequest?> GetByRequestIdentifierAsync(string requestIdentifier)
    {
        return await Context.PreAuthorizationRequests
     .Include(p => p.Patient)
     .Include(p => p.Provider)
          .Include(p => p.Insurer)
            .Include(p => p.Items)
            .Include(p => p.Response)
        .FirstOrDefaultAsync(p => p.RequestIdentifier == requestIdentifier);
    }

    /// <summary>
    /// Get pre-authorization requests by status
    /// </summary>
    public async Task<IEnumerable<PreAuthorizationRequest>> GetByStatusAsync(string status)
    {
        return await Context.PreAuthorizationRequests
          .Include(p => p.Patient)
          .Include(p => p.Provider)
     .Include(p => p.Insurer)
   .Include(p => p.Items)
       .Include(p => p.Response)
            .Where(p => p.Status == status)
.OrderByDescending(p => p.EnteredDate)
            .ToListAsync();
}

    /// <summary>
    /// Get pre-authorization requests by type
    /// </summary>
    public async Task<IEnumerable<PreAuthorizationRequest>> GetByTypeAsync(string type)
    {
     return await Context.PreAuthorizationRequests
            .Include(p => p.Patient)
         .Include(p => p.Provider)
            .Include(p => p.Insurer)
            .Include(p => p.Items)
    .Include(p => p.Response)
            .Where(p => p.Type == type)
            .OrderByDescending(p => p.EnteredDate)
        .ToListAsync();
    }

    /// <summary>
    /// Get active pre-authorization requests for a patient
    /// </summary>
    public async Task<IEnumerable<PreAuthorizationRequest>> GetActiveByPatientIdAsync(string patientId)
    {
        return await Context.PreAuthorizationRequests
            .Include(p => p.Provider)
 .Include(p => p.Insurer)
    .Include(p => p.Items)
      .Include(p => p.Response)
     .Where(p => p.PatientId == patientId && p.Status == "active")
            .OrderByDescending(p => p.EnteredDate)
         .ToListAsync();
    }

  /// <summary>
    /// Get pending pre-authorization requests (no response received yet)
    /// </summary>
    public async Task<IEnumerable<PreAuthorizationRequest>> GetPendingRequestsAsync()
    {
        return await Context.PreAuthorizationRequests
         .Include(p => p.Patient)
  .Include(p => p.Provider)
       .Include(p => p.Insurer)
       .Include(p => p.Items)
       .Where(p => p.Response == null && p.Status == "active")
     .OrderByDescending(p => p.EnteredDate)
     .ToListAsync();
    }

    /// <summary>
    /// Get pre-authorization requests submitted in date range
    /// </summary>
    public async Task<IEnumerable<PreAuthorizationRequest>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
  return await Context.PreAuthorizationRequests
        .Include(p => p.Patient)
            .Include(p => p.Provider)
  .Include(p => p.Insurer)
  .Include(p => p.Items)
 .Include(p => p.Response)
         .Where(p => p.EnteredDate >= startDate && p.EnteredDate <= endDate)
            .OrderByDescending(p => p.EnteredDate)
 .ToListAsync();
    }

    /// <summary>
    /// Check if a pre-authorization request identifier already exists
    /// </summary>
    public async Task<bool> RequestIdentifierExistsAsync(string requestIdentifier)
    {
        return await Context.PreAuthorizationRequests
        .AnyAsync(p => p.RequestIdentifier == requestIdentifier);
    }

/// <summary>
    /// Get pre-authorization statistics for a provider
    /// </summary>
 public async Task<PreAuthStatistics> GetStatisticsAsync(string providerId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = Context.PreAuthorizationRequests
         .Include(p => p.Response)
      .Where(p => p.ProviderId == providerId);

        if (fromDate.HasValue)
       query = query.Where(p => p.EnteredDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.EnteredDate <= toDate.Value);

   var requests = await query.ToListAsync();

        var stats = new PreAuthStatistics
        {
            TotalRequests = requests.Count,
  ActiveRequests = requests.Count(p => p.Status == "active"),
   CompletedRequests = requests.Count(p => p.Status == "completed"),
         CancelledRequests = requests.Count(p => p.Status == "cancelled"),
            PendingRequests = requests.Count(p => p.Response == null && p.Status == "active"),
       ApprovedRequests = requests.Count(p => p.Response != null && p.Response.IsApproved),
   DeniedRequests = requests.Count(p => p.Response != null && p.Response.Outcome == "error")
        };

      // Calculate average processing time for completed requests with responses
        var completedWithTime = requests
            .Where(p => p.Response != null && p.Response.ResponseDate > p.EnteredDate)
    .Select(p => (p.Response!.ResponseDate - p.EnteredDate).TotalHours)
         .ToList();

        stats.AverageProcessingTimeHours = completedWithTime.Any() 
      ? (decimal)completedWithTime.Average() 
            : 0;

        return stats;
    }
}
