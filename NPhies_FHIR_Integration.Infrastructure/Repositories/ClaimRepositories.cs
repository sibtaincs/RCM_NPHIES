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

/// <summary>
/// Repository interface for ClaimItem operations
/// </summary>
public interface IClaimItemRepository : IRepository<ClaimItem>
{
    /// <summary>
    /// Get claim items by claim ID
    /// </summary>
    Task<IEnumerable<ClaimItem>> GetByClaimIdAsync(string claimId);

    /// <summary>
    /// Get claim item by product code
    /// </summary>
    Task<IEnumerable<ClaimItem>> GetByProductCodeAsync(string productCode);

    /// <summary>
    /// Get claim items with patient invoices
    /// </summary>
    Task<IEnumerable<ClaimItem>> GetWithPatientInvoicesAsync(string claimId);
}

/// <summary>
/// Repository implementation for ClaimItem operations
/// </summary>
public class ClaimItemRepository : Repository<ClaimItem>, IClaimItemRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ClaimItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get claim items by claim ID
    /// </summary>
    public async Task<IEnumerable<ClaimItem>> GetByClaimIdAsync(string claimId)
    {
        return await DbSet.AsNoTracking()
    .Where(i => i.ClaimId == claimId)
     .OrderBy(i => i.Sequence)
   .ToListAsync();
    }

    /// <summary>
    /// Get claim item by product code
    /// </summary>
    public async Task<IEnumerable<ClaimItem>> GetByProductCodeAsync(string productCode)
    {
        return await DbSet.AsNoTracking()
        .Where(i => i.ProductOrServiceCode == productCode)
                 .ToListAsync();
    }

    /// <summary>
    /// Get claim items with patient invoices
    /// </summary>
    public async Task<IEnumerable<ClaimItem>> GetWithPatientInvoicesAsync(string claimId)
    {
        return await DbSet.AsNoTracking()
             .Where(i => i.ClaimId == claimId && !string.IsNullOrEmpty(i.PatientInvoiceValue))
        .OrderBy(i => i.Sequence)
                 .ToListAsync();
    }
}

/// <summary>
/// Repository interface for ClaimDiagnosis operations
/// </summary>
public interface IClaimDiagnosisRepository : IRepository<ClaimDiagnosis>
{
    /// <summary>
    /// Get claim diagnoses by claim ID
    /// </summary>
    Task<IEnumerable<ClaimDiagnosis>> GetByClaimIdAsync(string claimId);

    /// <summary>
    /// Get diagnosis by code
    /// </summary>
    Task<IEnumerable<ClaimDiagnosis>> GetByCodeAsync(string diagnosisCode);

    /// <summary>
    /// Get principal diagnosis for claim
    /// </summary>
    Task<ClaimDiagnosis?> GetPrincipalDiagnosisAsync(string claimId);

    /// <summary>
    /// Get on-admission diagnoses
    /// </summary>
    Task<IEnumerable<ClaimDiagnosis>> GetOnAdmissionDiagnosesAsync(string claimId);
}

/// <summary>
/// Repository implementation for ClaimDiagnosis operations
/// </summary>
public class ClaimDiagnosisRepository : Repository<ClaimDiagnosis>, IClaimDiagnosisRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ClaimDiagnosisRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get claim diagnoses by claim ID
    /// </summary>
    public async Task<IEnumerable<ClaimDiagnosis>> GetByClaimIdAsync(string claimId)
    {
        return await DbSet.AsNoTracking()
                 .Where(d => d.ClaimId == claimId)
                 .OrderBy(d => d.Sequence)
         .ToListAsync();
    }

    /// <summary>
    /// Get diagnosis by code
    /// </summary>
    public async Task<IEnumerable<ClaimDiagnosis>> GetByCodeAsync(string diagnosisCode)
    {
        return await DbSet.AsNoTracking()
      .Where(d => d.DiagnosisCode == diagnosisCode)
       .ToListAsync();
    }

    /// <summary>
    /// Get principal diagnosis for claim
    /// </summary>
    public async Task<ClaimDiagnosis?> GetPrincipalDiagnosisAsync(string claimId)
    {
        return await DbSet.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.ClaimId == claimId && d.DiagnosisType == "principal");
    }

    /// <summary>
    /// Get on-admission diagnoses
    /// </summary>
    public async Task<IEnumerable<ClaimDiagnosis>> GetOnAdmissionDiagnosesAsync(string claimId)
    {
        return await DbSet.AsNoTracking()
      .Where(d => d.ClaimId == claimId && d.OnAdmissionCode == "y")
      .OrderBy(d => d.Sequence)
     .ToListAsync();
    }
}

/// <summary>
/// Repository interface for ClaimResponse operations
/// </summary>
public interface IClaimResponseRepository : IRepository<ClaimResponse>
{
    /// <summary>
    /// Get response with all details
    /// </summary>
    Task<ClaimResponse?> GetWithDetailsAsync(string responseId);

    /// <summary>
    /// Get response by claim ID
    /// </summary>
    Task<ClaimResponse?> GetByClaimIdAsync(string claimId);

    /// <summary>
    /// Get responses by pre-auth reference
    /// </summary>
    Task<IEnumerable<ClaimResponse>> GetByPreAuthRefAsync(string preAuthRef);

    /// <summary>
    /// Get responses by status
    /// </summary>
    Task<IEnumerable<ClaimResponse>> GetByStatusAsync(string status);

    /// <summary>
    /// Get responses by insurer ID
    /// </summary>
    Task<IEnumerable<ClaimResponse>> GetByInsurerIdAsync(string insurerId);
}

/// <summary>
/// Repository implementation for ClaimResponse operations
/// </summary>
public class ClaimResponseRepository : Repository<ClaimResponse>, IClaimResponseRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ClaimResponseRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get response with all details
    /// </summary>
    public async Task<ClaimResponse?> GetWithDetailsAsync(string responseId)
    {
        return await DbSet.AsNoTracking()
         .Include(r => r.Patient)
      .Include(r => r.Insurer)
            .Include(r => r.Insurance)
    .Include(r => r.AddItems)
          .Include(r => r.Totals)
            .Include(r => r.DiagnosesExt)
        .Include(r => r.SupportingInfoExt)
            .Include(r => r.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == responseId);
    }

    /// <summary>
    /// Get response by claim ID
    /// </summary>
    public async Task<ClaimResponse?> GetByClaimIdAsync(string claimId)
    {
        return await DbSet.AsNoTracking()
.Include(r => r.Insurance)
            .Include(r => r.Items)
            .Where(r => r.ClaimId == claimId)
            .OrderByDescending(r => r.ReceivedAt ?? r.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get responses by pre-auth reference
    /// </summary>
    public async Task<IEnumerable<ClaimResponse>> GetByPreAuthRefAsync(string preAuthRef)
    {
        return await DbSet.AsNoTracking()
            .Where(r => r.PreAuthRef == preAuthRef)
       .Include(r => r.Patient)
          .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();
    }

    /// <summary>
    /// Get responses by status
    /// </summary>
    public async Task<IEnumerable<ClaimResponse>> GetByStatusAsync(string status)
    {
        return await DbSet.AsNoTracking()
              .Where(r => r.ClaimResponseStatus == status)
             .Include(r => r.Patient)
                 .OrderByDescending(r => r.CreatedAt)
                   .ToListAsync();
    }

    /// <summary>
    /// Get responses by insurer ID
    /// </summary>
    public async Task<IEnumerable<ClaimResponse>> GetByInsurerIdAsync(string insurerId)
    {
        return await DbSet.AsNoTracking()
      .Where(r => r.InsurerId == insurerId)
       .Include(r => r.Patient)
         .OrderByDescending(r => r.CreatedAt)
       .ToListAsync();
    }
}
