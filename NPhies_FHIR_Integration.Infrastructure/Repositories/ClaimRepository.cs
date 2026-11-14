using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Claim operations
/// </summary>
public interface IClaimRepository : IRepository<Claim>
{
    /// <summary>
    /// Get claim with all details
    /// </summary>
    Task<Claim?> GetWithDetailsAsync(string claimId);

    /// <summary>
    /// Get claims by patient ID
    /// </summary>
    Task<IEnumerable<Claim>> GetByPatientIdAsync(string patientId);

    /// <summary>
    /// Get claims by provider ID
    /// </summary>
    Task<IEnumerable<Claim>> GetByProviderIdAsync(string providerId);

    /// <summary>
    /// Get claims by status
    /// </summary>
    Task<IEnumerable<Claim>> GetByStatusAsync(string status);

    /// <summary>
    /// Get claims by episode ID
    /// </summary>
    Task<IEnumerable<Claim>> GetByEpisodeIdAsync(string episodeId);

    /// <summary>
    /// Get claim by claim number
    /// </summary>
    Task<Claim?> GetByClaimNumberAsync(string claimNumber);
}

/// <summary>
/// Repository implementation for Claim operations
/// </summary>
public class ClaimRepository : Repository<Claim>, IClaimRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ClaimRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get claim with all details
    /// </summary>
    public async Task<Claim?> GetWithDetailsAsync(string claimId)
    {
        return await DbSet.AsNoTracking()
           .Include(c => c.Patient)
         .Include(c => c.Provider)
        .Include(c => c.Insurer)
      .Include(c => c.Coverage)
            .ThenInclude(c => c!.Insurer)
        .Include(c => c.Coverage)
          .ThenInclude(c => c!.SubscriberPatient)
                .Include(c => c.MessageHeader)
            .Include(c => c.Practitioner)
            .ThenInclude(p => p!.DoctorMaster)
            .ThenInclude(d => d!.Clinic)
            .Include(c => c.CareTeam)
        .Include(c => c.Diagnoses)
                .Include(c => c.Items)
              .ThenInclude(i => i.Details)
          .Include(c => c.SupportingInfo)
              .Include(c => c.RelatedClaims)
            .Include(c => c.Encounter)
            .Include(c => c.ServiceLocation)
            .Include(c => c.ClaimResponses)
            .ThenInclude(response => response.AddItems)
            .ThenInclude(item => item.Adjudications)
                // Split: a single JOIN across 8 collections explodes into a cartesian product and times out.
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == claimId);
    }

    /// <summary>
    /// Get claims by patient ID
    /// </summary>
    public async Task<IEnumerable<Claim>> GetByPatientIdAsync(string patientId)
    {
        return await DbSet.AsNoTracking()
        .Where(c => c.PatientId == patientId)
            .Include(c => c.Provider)
     .OrderByDescending(c => c.CreatedAt)
 .ToListAsync();
    }

    /// <summary>
    /// Get claims by provider ID
    /// </summary>
    public async Task<IEnumerable<Claim>> GetByProviderIdAsync(string providerId)
    {
        return await DbSet.AsNoTracking()
   .Where(c => c.ProviderId == providerId)
            .Include(c => c.Patient)
          .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get claims by status
    /// </summary>
    public async Task<IEnumerable<Claim>> GetByStatusAsync(string status)
    {
        return await DbSet.AsNoTracking()
  .Where(c => c.Status == status)
       .Include(c => c.Patient)
         .OrderByDescending(c => c.CreatedAt)
   .ToListAsync();
    }

    /// <summary>
    /// Get claims by episode ID
    /// </summary>
    public async Task<IEnumerable<Claim>> GetByEpisodeIdAsync(string episodeId)
    {
        return await DbSet.AsNoTracking()
            .Where(c => c.EpisodeIdentifierValue == episodeId)
            .Include(c => c.Patient)
     .Include(c => c.Provider)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get claim by claim number
    /// </summary>
    public async Task<Claim?> GetByClaimNumberAsync(string claimNumber)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClaimNumber == claimNumber);
    }
}
