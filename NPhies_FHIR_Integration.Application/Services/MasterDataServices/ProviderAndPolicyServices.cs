using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Services.MasterDataServices;

#region Payer Policy Master Service
public class PayerPolicyMasterService : MasterDataServiceBase<PayerPolicyMaster, DTOs.PayerPolicyMasterDto>, IPayerPolicyMasterService
{
    public PayerPolicyMasterService(ApplicationDbContext context, IMapper mapper, ILogger<PayerPolicyMasterService> logger)
   : base(context, mapper, logger) { }

    protected override DbSet<PayerPolicyMaster> GetDbSet() => _context.PayerPolicyMasters;

public async Task<List<DTOs.PayerPolicyMasterDto>> GetByPayerAsync(string payerMasterId)
    {
        var items = await GetDbSet().Where(p => p.PayerMasterId == payerMasterId).ToListAsync();
    return _mapper.Map<List<DTOs.PayerPolicyMasterDto>>(items);
    }

    public async Task<DTOs.PayerPolicyMasterDto?> GetByPolicyCodeAsync(string policyCode)
    {
        var item = await GetDbSet().FirstOrDefaultAsync(p => p.PolicyCode == policyCode);
        return item == null ? null : _mapper.Map<DTOs.PayerPolicyMasterDto>(item);
    }

    public async Task<List<DTOs.PayerPolicyMasterDto>> GetActivePoliciesAsync()
    {
        var now = DateTime.UtcNow;
  var items = await GetDbSet()
     .Where(p => p.IsPolicyActive && p.IsActive && 
         p.EffectiveFromDate <= now && 
              (p.EffectiveToDate == null || p.EffectiveToDate >= now))
            .ToListAsync();
        return _mapper.Map<List<DTOs.PayerPolicyMasterDto>>(items);
    }

  protected override IQueryable<PayerPolicyMaster> ApplySearchFilter(IQueryable<PayerPolicyMaster> query, string searchTerm)
    {
  return query.Where(p => p.PolicyCode.Contains(searchTerm) || p.PolicyName.Contains(searchTerm));
    }
}
#endregion

#region Policy Benefit Coverage Service
public class PolicyBenefitCoverageService : MasterDataServiceBase<PolicyBenefitCoverage, DTOs.PolicyBenefitCoverageDto>, IPolicyBenefitCoverageService
{
    public PolicyBenefitCoverageService(ApplicationDbContext context, IMapper mapper, ILogger<PolicyBenefitCoverageService> logger)
        : base(context, mapper, logger) { }

    protected override DbSet<PolicyBenefitCoverage> GetDbSet() => _context.PolicyBenefitCoverages;

    public async Task<List<DTOs.PolicyBenefitCoverageDto>> GetBypolicyAsync(string policyMasterId)
    {
        var items = await GetDbSet().Where(p => p.PolicyMasterId == policyMasterId).ToListAsync();
        return _mapper.Map<List<DTOs.PolicyBenefitCoverageDto>>(items);
    }

    public async Task<List<DTOs.PolicyBenefitCoverageDto>> GetByCategoryAsync(string serviceCategory)
    {
     var items = await GetDbSet().Where(p => p.ServiceCategory == serviceCategory).ToListAsync();
  return _mapper.Map<List<DTOs.PolicyBenefitCoverageDto>>(items);
    }

    public async Task<List<DTOs.PolicyBenefitCoverageDto>> GetExcludedBenefitsAsync()
    {
        var items = await GetDbSet().Where(p => p.IsExcluded && p.IsActive).ToListAsync();
        return _mapper.Map<List<DTOs.PolicyBenefitCoverageDto>>(items);
    }

    protected override IQueryable<PolicyBenefitCoverage> ApplySearchFilter(IQueryable<PolicyBenefitCoverage> query, string searchTerm)
    {
        return query.Where(p => p.BenefitType!.Contains(searchTerm) || p.ServiceCategory!.Contains(searchTerm));
    }
}
#endregion

#region Clinic Master Service
public class ClinicMasterService : MasterDataServiceBase<ClinicMaster, DTOs.ClinicMasterDto>, IClinicMasterService
{
    public ClinicMasterService(ApplicationDbContext context, IMapper mapper, ILogger<ClinicMasterService> logger)
      : base(context, mapper, logger) { }

    protected override DbSet<ClinicMaster> GetDbSet() => _context.ClinicMasters;

    public async Task<DTOs.ClinicMasterDto?> GetByClinicCodeAsync(string clinicCode)
 {
     var item = await GetDbSet().FirstOrDefaultAsync(c => c.ClinicCode == clinicCode);
        return item == null ? null : _mapper.Map<DTOs.ClinicMasterDto>(item);
 }

    public async Task<List<DTOs.ClinicMasterDto>> GetByOrganizationAsync(string organizationId)
    {
        var items = await GetDbSet().Where(c => c.OrganizationId == organizationId).ToListAsync();
        return _mapper.Map<List<DTOs.ClinicMasterDto>>(items);
    }

    public async Task<List<DTOs.ClinicMasterDto>> GetByTypeAsync(string clinicType)
    {
   var items = await GetDbSet().Where(c => c.ClinicType == clinicType).ToListAsync();
   return _mapper.Map<List<DTOs.ClinicMasterDto>>(items);
    }

    protected override IQueryable<ClinicMaster> ApplySearchFilter(IQueryable<ClinicMaster> query, string searchTerm)
    {
        return query.Where(c => c.ClinicCode.Contains(searchTerm) || c.ClinicName.Contains(searchTerm) || c.ClinicType!.Contains(searchTerm));
    }
}
#endregion

#region Doctor Master Service
public class DoctorMasterService : MasterDataServiceBase<DoctorMaster, DTOs.DoctorMasterDto>, IDoctorMasterService
{
    public DoctorMasterService(ApplicationDbContext context, IMapper mapper, ILogger<DoctorMasterService> logger)
  : base(context, mapper, logger) { }

    protected override DbSet<DoctorMaster> GetDbSet() => _context.DoctorMasters;

    public async Task<DTOs.DoctorMasterDto?> GetByDoctorCodeAsync(string doctorCode)
    {
        var item = await GetDbSet().FirstOrDefaultAsync(d => d.DoctorCode == doctorCode);
        return item == null ? null : _mapper.Map<DTOs.DoctorMasterDto>(item);
    }

  public async Task<List<DTOs.DoctorMasterDto>> GetBySpecializationAsync(string specialization)
    {
     var items = await GetDbSet().Where(d => d.Specialization == specialization).ToListAsync();
    return _mapper.Map<List<DTOs.DoctorMasterDto>>(items);
    }

    public async Task<List<DTOs.DoctorMasterDto>> GetByClinicAsync(string clinicMasterId)
    {
        var items = await GetDbSet().Where(d => d.ClinicMasterId == clinicMasterId).ToListAsync();
  return _mapper.Map<List<DTOs.DoctorMasterDto>>(items);
    }

    public async Task<List<DTOs.DoctorMasterDto>> GetAvailableForAppointmentsAsync()
    {
        var items = await GetDbSet().Where(d => d.IsAvailableForAppointments && d.IsActive).ToListAsync();
        return _mapper.Map<List<DTOs.DoctorMasterDto>>(items);
    }

 protected override IQueryable<DoctorMaster> ApplySearchFilter(IQueryable<DoctorMaster> query, string searchTerm)
    {
        return query.Where(d => d.DoctorCode.Contains(searchTerm) || d.DoctorName.Contains(searchTerm) || d.Specialization!.Contains(searchTerm));
    }
}
#endregion

#region Doctor Qualification Service
public class DoctorQualificationService : MasterDataServiceBase<DoctorQualification, DTOs.DoctorQualificationDto>, IDoctorQualificationService
{
    public DoctorQualificationService(ApplicationDbContext context, IMapper mapper, ILogger<DoctorQualificationService> logger)
      : base(context, mapper, logger) { }

    protected override DbSet<DoctorQualification> GetDbSet() => _context.DoctorQualifications;

    public async Task<List<DTOs.DoctorQualificationDto>> GetByDoctorAsync(string doctorMasterId)
    {
        var items = await GetDbSet().Where(d => d.DoctorMasterId == doctorMasterId).ToListAsync();
        return _mapper.Map<List<DTOs.DoctorQualificationDto>>(items);
    }

    public async Task<List<DTOs.DoctorQualificationDto>> GetByTypeAsync(string qualificationType)
    {
     var items = await GetDbSet().Where(d => d.QualificationType == qualificationType).ToListAsync();
        return _mapper.Map<List<DTOs.DoctorQualificationDto>>(items);
    }

    public async Task<List<DTOs.DoctorQualificationDto>> GetVerifiedQualificationsAsync()
    {
  var items = await GetDbSet().Where(d => d.VerificationStatus == "Verified" && d.IsActive).ToListAsync();
        return _mapper.Map<List<DTOs.DoctorQualificationDto>>(items);
    }

    protected override IQueryable<DoctorQualification> ApplySearchFilter(IQueryable<DoctorQualification> query, string searchTerm)
  {
        return query.Where(d => d.QualificationType.Contains(searchTerm) || d.QualificationName.Contains(searchTerm));
    }
}
#endregion

#region Claim Submission Rules Service
public class ClaimSubmissionRulesService : MasterDataServiceBase<ClaimSubmissionRules, DTOs.ClaimSubmissionRulesDto>, IClaimSubmissionRulesService
{
    public ClaimSubmissionRulesService(ApplicationDbContext context, IMapper mapper, ILogger<ClaimSubmissionRulesService> logger)
  : base(context, mapper, logger) { }

    protected override DbSet<ClaimSubmissionRules> GetDbSet() => _context.ClaimSubmissionRules;

    public async Task<List<DTOs.ClaimSubmissionRulesDto>> GetByPayerAsync(string payerMasterId)
    {
   var items = await GetDbSet()
 .Where(r => r.PayerMasterId == payerMasterId && r.IsActive)
    .OrderBy(r => r.Priority)
            .ToListAsync();
        return _mapper.Map<List<DTOs.ClaimSubmissionRulesDto>>(items);
    }

    public async Task<List<DTOs.ClaimSubmissionRulesDto>> GetByPolicyAsync(string policyMasterId)
    {
     var items = await GetDbSet()
        .Where(r => r.PolicyMasterId == policyMasterId && r.IsActive)
            .OrderBy(r => r.Priority)
        .ToListAsync();
        return _mapper.Map<List<DTOs.ClaimSubmissionRulesDto>>(items);
    }

    public async Task<List<DTOs.ClaimSubmissionRulesDto>> GetApplicableRulesAsync(string payerMasterId, string? policyMasterId = null)
    {
        var query = GetDbSet().Where(r => r.IsActive && (r.PayerMasterId == payerMasterId || r.PayerMasterId == null));

        if (!string.IsNullOrEmpty(policyMasterId))
        {
            query = query.Where(r => r.PolicyMasterId == policyMasterId || r.PolicyMasterId == null);
        }

        var items = await query.OrderBy(r => r.Priority).ToListAsync();
        return _mapper.Map<List<DTOs.ClaimSubmissionRulesDto>>(items);
    }

    protected override IQueryable<ClaimSubmissionRules> ApplySearchFilter(IQueryable<ClaimSubmissionRules> query, string searchTerm)
    {
        return query.Where(r => r.RuleName.Contains(searchTerm) || r.RuleType!.Contains(searchTerm));
    }
}
#endregion
