using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for CoverageEligibilityRequest operations
/// </summary>
public class CoverageEligibilityRequestRepository : Repository<CoverageEligibilityRequest>, ICoverageEligibilityRequestRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    public CoverageEligibilityRequestRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get request with all related data
    /// </summary>
    public async Task<CoverageEligibilityRequest?> GetWithDetailsAsync(string requestId)
    {
        return await DbSet.AsNoTracking()
  .Include(r => r.Patient)
     .Include(r => r.Coverage)
  .Include(r => r.Provider)
            .Include(r => r.Insurer)
    .Include(r => r.MessageHeader)
            .Include(r => r.Items)
            .ThenInclude(i => i.Modifiers)
            .Include(r => r.Response)
 .ThenInclude(r => r.BenefitBalances)
       .ThenInclude(b => b.Benefits)
          .FirstOrDefaultAsync(r => r.Id == requestId);
    }

    /// <summary>
    /// Get requests by patient ID
    /// </summary>
    public async Task<IEnumerable<CoverageEligibilityRequest>> GetByPatientIdAsync(string patientId)
    {
        return await DbSet.AsNoTracking()
              .Where(r => r.PatientId == patientId)
                   .Include(r => r.Coverage)
          .Include(r => r.Provider)
                .OrderByDescending(r => r.CreatedAt)
              .ToListAsync();
    }

    /// <summary>
    /// Get requests by coverage ID
    /// </summary>
    public async Task<IEnumerable<CoverageEligibilityRequest>> GetByCoverageIdAsync(string coverageId)
    {
        return await DbSet.AsNoTracking()
                .Where(r => r.CoverageId == coverageId)
                .Include(r => r.Patient)
     .Include(r => r.Provider)
             .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
    }

    /// <summary>
    /// Get requests by provider ID
    /// </summary>
    public async Task<IEnumerable<CoverageEligibilityRequest>> GetByProviderIdAsync(string providerId)
    {
        return await DbSet.AsNoTracking()
   .Where(r => r.ProviderId == providerId)
      .Include(r => r.Patient)
       .Include(r => r.Coverage)
      .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get requests by status
    /// </summary>
    public async Task<IEnumerable<CoverageEligibilityRequest>> GetByStatusAsync(string status)
    {
        return await DbSet.AsNoTracking()
   .Where(r => r.Status == status)
      .Include(r => r.Patient)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get requests pending response
    /// </summary>
    public async Task<IEnumerable<CoverageEligibilityRequest>> GetPendingRequestsAsync()
    {
        return await DbSet.AsNoTracking()
            .Where(r => r.Status == "submitted" || r.Status == "acknowledged")
            .Include(r => r.Patient)
            .Include(r => r.MessageHeader)
         .OrderByDescending(r => r.SubmittedAt)
   .ToListAsync();
    }

    /// <summary>
    /// Get request by message UUID
    /// </summary>
    public async Task<CoverageEligibilityRequest?> GetByMessageUUIDAsync(string messageUUID)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(r => r.MessageUUID == messageUUID);
    }

    /// <summary>
    /// Get request by request ID with response
    /// </summary>
    public async Task<CoverageEligibilityRequest?> GetWithResponseAsync(string requestId)
    {
        return await DbSet.AsNoTracking()
                 .Include(r => r.Response)
               .ThenInclude(r => r!.BenefitBalances)
           .ThenInclude(b => b.Benefits)
           .Include(r => r.Response)
        .ThenInclude(r => r!.Errors)
                 .FirstOrDefaultAsync(r => r.Id == requestId);
    }
}

/// <summary>
/// Repository implementation for CoverageEligibilityResponse operations
/// </summary>
public class CoverageEligibilityResponseRepository : Repository<CoverageEligibilityResponse>, ICoverageEligibilityResponseRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    public CoverageEligibilityResponseRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get response with all related data
    /// </summary>
    public async Task<CoverageEligibilityResponse?> GetWithDetailsAsync(string responseId)
    {
        return await DbSet.AsNoTracking()
      .Include(r => r.EligibilityRequest)
            .Include(r => r.Insurer)
            .Include(r => r.Patient)
           .Include(r => r.Coverage)
          .Include(r => r.BenefitBalances)
                .ThenInclude(b => b.Benefits)
                .Include(r => r.Errors)
                .FirstOrDefaultAsync(r => r.Id == responseId);
    }

    /// <summary>
    /// Get responses by request ID
    /// </summary>
    public async Task<CoverageEligibilityResponse?> GetByRequestIdAsync(string requestId)
    {
        return await DbSet.AsNoTracking()
            .Include(r => r.BenefitBalances)
            .ThenInclude(b => b.Benefits)
            .Include(r => r.Errors)
        .FirstOrDefaultAsync(r => r.EligibilityRequestId == requestId);
    }

    /// <summary>
    /// Get responses by insurer ID
    /// </summary>
    public async Task<IEnumerable<CoverageEligibilityResponse>> GetByInsurerIdAsync(string insurerId)
    {
        return await DbSet.AsNoTracking()
                  .Where(r => r.InsurerId == insurerId)
              .Include(r => r.Patient)
           .OrderByDescending(r => r.ResponseCreatedAt)
                  .ToListAsync();
    }

    /// <summary>
    /// Get responses by outcome
    /// </summary>
    public async Task<IEnumerable<CoverageEligibilityResponse>> GetByOutcomeAsync(string outcome)
    {
        return await DbSet.AsNoTracking()
            .Where(r => r.Outcome == outcome)
     .Include(r => r.BenefitBalances)
            .OrderByDescending(r => r.ResponseCreatedAt)
       .ToListAsync();
    }

    /// <summary>
    /// Get responses with errors
    /// </summary>
    public async Task<IEnumerable<CoverageEligibilityResponse>> GetWithErrorsAsync()
    {
        return await DbSet.AsNoTracking()
    .Where(r => r.Errors.Any())
            .Include(r => r.Errors)
       .OrderByDescending(r => r.ResponseCreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get response by UUID
    /// </summary>
    public async Task<CoverageEligibilityResponse?> GetByResponseUUIDAsync(string responseUUID)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResponseUUID == responseUUID);
    }
}

/// <summary>
/// Repository implementation for Patient operations
/// </summary>
public class PatientRepository : Repository<Patient>, IPatientRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    public PatientRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get patient by MRN
    /// </summary>
    public async Task<Patient?> GetByMRNAsync(string mrn)
    {
        return await DbSet.AsNoTracking()
              .FirstOrDefaultAsync(p => p.MRN == mrn);
    }

    /// <summary>
    /// Get patient with all coverage and eligibility data
    /// </summary>
    public async Task<Patient?> GetWithCoverageAndEligibilityAsync(string patientId)
    {
        return await DbSet.AsNoTracking()
            .Include(p => p.Coverages)
   .Include(p => p.EligibilityRequests)
            .ThenInclude(r => r.Response)
    .FirstOrDefaultAsync(p => p.Id == patientId);
    }

    /// <summary>
    /// Get patient by national ID
    /// </summary>
    public async Task<Patient?> GetByNationalIdAsync(string nationalId)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(p => p.NationalId == nationalId);
    }

    /// <summary>
    /// Check if patient exists by MRN
    /// </summary>
    public async Task<bool> ExistsByMRNAsync(string mrn)
    {
        return await DbSet.AnyAsync(p => p.MRN == mrn);
    }
}

/// <summary>
/// Repository implementation for Coverage operations
/// </summary>
public class CoverageRepository : Repository<Coverage>, ICoverageRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    public CoverageRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get coverage by policy number
    /// </summary>
    public async Task<Coverage?> GetByPolicyNumberAsync(string policyNumber)
    {
        return await DbSet.AsNoTracking()
    .FirstOrDefaultAsync(c => c.PolicyNumber == policyNumber);
    }

    /// <summary>
    /// Get active coverage for patient
    /// </summary>
    public async Task<IEnumerable<Coverage>> GetActiveByPatientIdAsync(string patientId)
    {
        var now = DateTime.UtcNow.Date;
        return await DbSet.AsNoTracking()
                 .Where(c => c.PatientId == patientId &&
        c.Status == "active" &&
                c.CoverageStartDate <= now &&
           c.CoverageEndDate >= now)
               .ToListAsync();
    }

    /// <summary>
    /// Get coverage with all related data
    /// </summary>
    public async Task<Coverage?> GetWithDetailsAsync(string coverageId)
    {
        return await DbSet.AsNoTracking()
            .Include(c => c.Patient)
  .Include(c => c.Insurer)
      .Include(c => c.EligibilityRequests)
         .FirstOrDefaultAsync(c => c.Id == coverageId);
    }

    /// <summary>
    /// Check if coverage is active on specific date
    /// </summary>
    public async Task<bool> IsCoverageActiveAsync(string coverageId, DateTime date)
    {
        return await DbSet.AnyAsync(c => c.Id == coverageId &&
       c.Status == "active" &&
 c.CoverageStartDate <= date &&
           c.CoverageEndDate >= date);
    }
}

/// <summary>
/// Repository implementation for Organization operations
/// </summary>
public class OrganizationRepository : Repository<Organization>, IOrganizationRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    public OrganizationRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get organization by license number
    /// </summary>
    public async Task<Organization?> GetByLicenseNumberAsync(string licenseNumber)
    {
        return await DbSet.AsNoTracking()
   .FirstOrDefaultAsync(o => o.LicenseNumber == licenseNumber);
    }

    /// <summary>
    /// Get all providers
    /// </summary>
    public async Task<IEnumerable<Organization>> GetProvidersAsync()
    {
        return await DbSet.AsNoTracking()
    .Where(o => o.OrganizationType == "prov" && o.Status == "active")
            .ToListAsync();
    }

    /// <summary>
    /// Get all insurers
    /// </summary>
    public async Task<IEnumerable<Organization>> GetInsurersAsync()
    {
        return await DbSet.AsNoTracking()
           .Where(o => o.OrganizationType == "ins" && o.Status == "active")
             .ToListAsync();
    }

    /// <summary>
    /// Get organization with all locations and practitioners
    /// </summary>
    public async Task<Organization?> GetWithDetailsAsync(string organizationId)
    {
        return await DbSet.AsNoTracking()
          .Include(o => o.Locations)
            .Include(o => o.Practitioners)
              .FirstOrDefaultAsync(o => o.Id == organizationId);
    }
}

/// <summary>
/// Repository implementation for BenefitBalance operations
/// </summary>
public class BenefitBalanceRepository : Repository<BenefitBalance>, IBenefitBalanceRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    public BenefitBalanceRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get benefit balances for a response with benefits
    /// </summary>
    public async Task<IEnumerable<BenefitBalance>> GetWithBenefitsAsync(string responseId)
    {
        return await DbSet.AsNoTracking()
.Where(b => b.EligibilityResponseId == responseId)
    .Include(b => b.Benefits)
     .ToListAsync();
    }

    /// <summary>
    /// Get benefit balance by category
    /// </summary>
    public async Task<BenefitBalance?> GetByCategoryAsync(string responseId, string category)
    {
        return await DbSet.AsNoTracking()
    .Include(b => b.Benefits)
            .FirstOrDefaultAsync(b => b.EligibilityResponseId == responseId && b.Category == category);
    }

    /// <summary>
    /// Get all benefit balances for a response
    /// </summary>
    public async Task<IEnumerable<BenefitBalance>> GetByResponseIdAsync(string responseId)
    {
        return await DbSet.AsNoTracking()
    .Where(b => b.EligibilityResponseId == responseId)
 .Include(b => b.Benefits)
      .OrderBy(b => b.SequenceNumber)
 .ToListAsync();
    }
}

/// <summary>
/// Repository implementation for EligibilityError operations
/// </summary>
public class EligibilityErrorRepository : Repository<EligibilityError>, IEligibilityErrorRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    public EligibilityErrorRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get errors for a request
    /// </summary>
    public async Task<IEnumerable<EligibilityError>> GetRequestErrorsAsync(string requestId)
    {
        return await DbSet.AsNoTracking()
          .Where(e => e.EligibilityRequestId == requestId)
  .ToListAsync();
    }

    /// <summary>
    /// Get errors for a response
    /// </summary>
    public async Task<IEnumerable<EligibilityError>> GetResponseErrorsAsync(string responseId)
    {
        return await DbSet.AsNoTracking()
            .Where(e => e.EligibilityResponseId == responseId)
   .ToListAsync();
    }

    /// <summary>
    /// Get critical errors
    /// </summary>
    public async Task<IEnumerable<EligibilityError>> GetCriticalErrorsAsync()
    {
        return await DbSet.AsNoTracking()
            .Where(e => e.Severity == "fatal" || e.Severity == "error")
     .OrderByDescending(e => e.ErrorOccurredAt)
      .ToListAsync();
    }

    /// <summary>
    /// Get errors by code
    /// </summary>
    public async Task<IEnumerable<EligibilityError>> GetByErrorCodeAsync(string errorCode)
    {
        return await DbSet.AsNoTracking()
       .Where(e => e.ErrorCode == errorCode)
      .OrderByDescending(e => e.ErrorOccurredAt)
  .ToListAsync();
    }
}
