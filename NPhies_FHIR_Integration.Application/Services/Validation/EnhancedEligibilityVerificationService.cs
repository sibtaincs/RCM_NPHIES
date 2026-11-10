using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Services.Validation
{
 /// <summary>
    /// Enhanced Eligibility Verification Service
    /// NPHIES-specific coverage eligibility verification
    /// Implements NPHIES eligibility query requirements
    /// </summary>
    public interface IEnhancedEligibilityVerificationService
    {
        Task<EligibilityVerificationResult> VerifyEligibilityAsync(EligibilityQueryDto query);
        Task<CoverageDetailsDto> GetCoverageDetailsAsync(string subscriberId, string insurerId);
        Task<bool> IsEligibleOnDateAsync(string subscriberId, string planId, DateTime serviceDate);
        Task<List<BenefitDto>> GetActiveBenefitsAsync(string subscriberId, string insurerId);
        Task<EligibilityCache> GetCachedEligibilityAsync(string subscriberId);
        Task InvalidateEligibilityCacheAsync(string subscriberId);
  }

    /// <summary>
    /// Eligibility query DTO
    /// </summary>
    public class EligibilityQueryDto
    {
        public string SubscriberId { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string InsurerId { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public DateTime ServiceDate { get; set; }
  public string ProviderId { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Eligibility verification result DTO
    /// </summary>
    public class EligibilityVerificationResult
    {
        public bool IsEligible { get; set; }
     public string Status { get; set; } = string.Empty; // active, inactive, pending, terminated
 public DateTime? EffectiveDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string GroupNumber { get; set; } = string.Empty;
  public List<string> EligibilityIssues { get; set; } = new();
        public List<BenefitDto> AvailableBenefits { get; set; } = new();
  public DateTime VerificationTime { get; set; } = DateTime.UtcNow;
        public string VerificationSource { get; set; } = "NPHIES Portal";
public int CacheAgeMinutes { get; set; }
    }

    /// <summary>
/// Coverage details DTO
    /// </summary>
    public class CoverageDetailsDto
    {
        public string SubscriberId { get; set; } = string.Empty;
        public string CoverageName { get; set; } = string.Empty;
        public string CoverageType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? EffectiveDate { get; set; }
     public DateTime? TerminationDate { get; set; }
   public string GroupNumber { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string PayorName { get; set; } = string.Empty;
        public string PayorId { get; set; } = string.Empty;
        public List<DependentDto> Dependents { get; set; } = new();
    }

    /// <summary>
    /// Dependent DTO
 /// </summary>
    public class DependentDto
    {
      public string DependentId { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
    }

    /// <summary>
    /// Benefit DTO
    /// </summary>
    public class BenefitDto
    {
        public string BenefitCode { get; set; } = string.Empty;
  public string BenefitName { get; set; } = string.Empty;
        public string BenefitType { get; set; } = string.Empty;
  public decimal Deductible { get; set; }
  public decimal DeductibleMet { get; set; }
        public decimal CopayAmount { get; set; }
      public decimal CoinsurancePercent { get; set; }
        public decimal OutOfPocketMax { get; set; }
        public decimal OutOfPocketMet { get; set; }
        public bool IsActive { get; set; }
        public string CoverageLevel { get; set; } = string.Empty; // individual, family, etc.
    }

    /// <summary>
    /// Eligibility cache DTO
    /// </summary>
    public class EligibilityCache
    {
        public string SubscriberId { get; set; } = string.Empty;
        public EligibilityVerificationResult Result { get; set; }
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// Eligibility verification backed by persisted data: the patient's <see cref="Coverage"/> and the latest
    /// NPHIES eligibility response (<see cref="EligibilityCheck"/>). A response younger than the cache window is
    /// reused; otherwise, when a provider is supplied, a live NPHIES eligibility request is sent through
    /// <see cref="IRcmEligibilityService"/>.
    /// </summary>
    public class EnhancedEligibilityVerificationService : IEnhancedEligibilityVerificationService
    {
        private static readonly TimeSpan CacheWindow = TimeSpan.FromHours(24);
        // Invalidation marks: eligibility responses received before this instant are ignored for the subscriber.
        private static readonly ConcurrentDictionary<string, DateTime> Invalidated = new(StringComparer.OrdinalIgnoreCase);

        private readonly ApplicationDbContext _db;
        private readonly IRcmEligibilityService _eligibility;
        private readonly ILogger<EnhancedEligibilityVerificationService> _logger;

        public EnhancedEligibilityVerificationService(
            ApplicationDbContext db,
            IRcmEligibilityService eligibility,
            ILogger<EnhancedEligibilityVerificationService> logger)
        {
            _db = db;
            _eligibility = eligibility;
            _logger = logger;
        }

        public async Task<EligibilityVerificationResult> VerifyEligibilityAsync(EligibilityQueryDto query)
        {
            if (query == null || (string.IsNullOrWhiteSpace(query.SubscriberId) && string.IsNullOrWhiteSpace(query.PatientId)))
                return new EligibilityVerificationResult { IsEligible = false, Status = "unknown", EligibilityIssues = { "SubscriberId or PatientId is required" } };

            var serviceDate = query.ServiceDate == default ? DateTime.UtcNow.Date : query.ServiceDate.Date;
            var coverage = await FindCoverageAsync(query.SubscriberId, query.InsurerId, query.PatientId, query.PlanId);
            if (coverage == null)
            {
                return new EligibilityVerificationResult
                {
                    IsEligible = false,
                    Status = "not_found",
                    VerificationSource = "Local coverage registry",
                    EligibilityIssues = { $"Coverage not found for subscriber '{query.SubscriberId}'" }
                };
            }

            var check = await LatestCheckAsync(coverage, CacheWindow);
            var source = "NPHIES eligibility response";
            if (check == null && !string.IsNullOrWhiteSpace(query.ProviderId))
            {
                try
                {
                    var live = await _eligibility.CheckAsync(new EligibilityCheckRequestDto
                    {
                        PatientId = coverage.PatientId,
                        CoverageId = coverage.Id,
                        ProviderId = query.ProviderId,
                        InsurerId = coverage.InsurerId,
                        Purposes = new List<string> { "benefits", "validation" },
                        ServicedDate = serviceDate,
                    });
                    check = await _db.EligibilityChecks.AsNoTracking().FirstOrDefaultAsync(e => e.Id == live.Id);
                    source = "NPHIES eligibility (live)";
                }
                catch (RcmWorkflowException ex)
                {
                    _logger.LogWarning("Live NPHIES eligibility failed for coverage {CoverageId}: {Message}", coverage.Id, ex.Message);
                }
            }

            var result = new EligibilityVerificationResult
            {
                EffectiveDate = coverage.CoverageStartDate == default ? null : coverage.CoverageStartDate,
                TerminationDate = coverage.CoverageEndDate == default ? null : coverage.CoverageEndDate,
                PlanName = coverage.PlanCode ?? coverage.CoverageClassValue ?? coverage.CoverageType,
                GroupNumber = coverage.PolicyNumber,
                VerificationTime = check?.RespondedAt ?? DateTime.UtcNow,
                CacheAgeMinutes = check?.RespondedAt is { } at ? (int)(DateTime.UtcNow - at).TotalMinutes : 0,
            };

            if (check != null && check.Status != "Pending")
            {
                result.VerificationSource = source;
                result.IsEligible = check.IsEligible ?? check.InForce ?? false;
                result.Status = check.InForce == true ? "active" : check.InForce == false ? "inactive" : coverage.Status;
                if (check.BenefitPeriodStart.HasValue) result.EffectiveDate = check.BenefitPeriodStart;
                if (check.BenefitPeriodEnd.HasValue) result.TerminationDate = check.BenefitPeriodEnd;
                result.AvailableBenefits = ParseBenefits(check.BenefitsJson);
                if (!string.IsNullOrWhiteSpace(check.NotInForceReason)) result.EligibilityIssues.Add($"Not in force: {check.NotInForceReason}");
                if (!string.IsNullOrWhiteSpace(check.SiteEligibility) && check.SiteEligibility != "eligible")
                    result.EligibilityIssues.Add($"Site eligibility: {check.SiteEligibility}");
                if (!string.IsNullOrWhiteSpace(check.Disposition)) result.EligibilityIssues.Add(check.Disposition);
                if (check.ServicedDate.Date != serviceDate && (result.TerminationDate.HasValue && serviceDate > result.TerminationDate.Value.Date))
                    result.EligibilityIssues.Add("Service date is after the benefit period");
            }
            else
            {
                result.VerificationSource = "Local coverage registry";
                result.Status = coverage.Status;
                result.IsEligible = ValidateEligibility(coverage, serviceDate);
                result.EligibilityIssues = IdentifyEligibilityIssues(coverage, serviceDate);
                result.EligibilityIssues.Add("No NPHIES eligibility response on file; result is based on the stored coverage only");
                result.AvailableBenefits = CoverageBenefits(coverage);
            }

            return result;
        }

        public async Task<CoverageDetailsDto> GetCoverageDetailsAsync(string subscriberId, string insurerId)
        {
            var coverage = await FindCoverageAsync(subscriberId, insurerId, null, null);
            if (coverage == null) return null!;

            var dependents = await _db.Coverages.AsNoTracking()
                .Include(c => c.Patient)
                .Where(c => c.Id != coverage.Id && c.InsurerId == coverage.InsurerId
                    && (c.SubscriberPatientId == coverage.PatientId || (c.SubscriberId != null && c.SubscriberId == coverage.MemberID)))
                .ToListAsync();

            return new CoverageDetailsDto
            {
                SubscriberId = coverage.MemberID,
                CoverageName = coverage.PlanCode ?? coverage.CoverageClassValue ?? coverage.CoverageType,
                CoverageType = coverage.CoverageType,
                Status = coverage.Status,
                EffectiveDate = coverage.CoverageStartDate == default ? null : coverage.CoverageStartDate,
                TerminationDate = coverage.CoverageEndDate == default ? null : coverage.CoverageEndDate,
                GroupNumber = coverage.PolicyNumber,
                GroupName = coverage.CoverageClassValue ?? string.Empty,
                PayorName = coverage.Insurer?.OrganizationName ?? string.Empty,
                PayorId = coverage.InsurerId,
                Dependents = dependents.Select(d => new DependentDto
                {
                    DependentId = d.MemberID,
                    Relationship = string.IsNullOrWhiteSpace(d.RelationToSubscriber) ? d.SubscriberRelationship : d.RelationToSubscriber,
                    Name = d.Patient?.FullName ?? $"{d.Patient?.FirstName} {d.Patient?.LastName}".Trim(),
                    DateOfBirth = d.Patient?.DateOfBirth,
                }).ToList(),
            };
        }

        public async Task<bool> IsEligibleOnDateAsync(string subscriberId, string planId, DateTime serviceDate)
        {
            var coverage = await FindCoverageAsync(subscriberId, null, null, planId);
            if (coverage == null) return false;
            var check = await LatestCheckAsync(coverage, null);
            if (check?.InForce == false) return false;
            return ValidateEligibility(coverage, serviceDate.Date);
        }

        public async Task<List<BenefitDto>> GetActiveBenefitsAsync(string subscriberId, string insurerId)
        {
            var coverage = await FindCoverageAsync(subscriberId, insurerId, null, null);
            if (coverage == null) return new List<BenefitDto>();
            var check = await LatestCheckAsync(coverage, null);
            var benefits = ParseBenefits(check?.BenefitsJson);
            return (benefits.Count > 0 ? benefits : CoverageBenefits(coverage)).Where(b => b.IsActive).ToList();
        }

        public async Task<EligibilityCache> GetCachedEligibilityAsync(string subscriberId)
        {
            var coverage = await FindCoverageAsync(subscriberId, null, null, null);
            if (coverage == null) return null!;
            var check = await LatestCheckAsync(coverage, CacheWindow);
            if (check?.RespondedAt == null) return null!;

            var result = await VerifyEligibilityAsync(new EligibilityQueryDto
            {
                SubscriberId = subscriberId,
                InsurerId = coverage.InsurerId,
                PatientId = coverage.PatientId,
                ServiceDate = check.ServicedDate,
            });
            return new EligibilityCache
            {
                SubscriberId = subscriberId,
                Result = result,
                CachedAt = check.RespondedAt.Value,
                ExpiresAt = check.RespondedAt.Value.Add(CacheWindow),
            };
        }

        public Task InvalidateEligibilityCacheAsync(string subscriberId)
        {
            if (!string.IsNullOrWhiteSpace(subscriberId)) Invalidated[subscriberId] = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        private async Task<Coverage?> FindCoverageAsync(string? subscriberId, string? insurerId, string? patientId, string? planId)
        {
            var q = _db.Coverages.AsNoTracking().Include(c => c.Insurer).AsQueryable();
            if (!string.IsNullOrWhiteSpace(subscriberId))
                q = q.Where(c => c.MemberID == subscriberId || c.CoverageIdentifierValue == subscriberId || c.SubscriberId == subscriberId);
            if (!string.IsNullOrWhiteSpace(patientId)) q = q.Where(c => c.PatientId == patientId);
            if (!string.IsNullOrWhiteSpace(insurerId)) q = q.Where(c => c.InsurerId == insurerId);
            if (!string.IsNullOrWhiteSpace(planId)) q = q.Where(c => c.PlanCode == planId || c.PolicyNumber == planId);
            // Prefer the member's own coverage, then active, then most recent.
            return await q.OrderByDescending(c => c.MemberID == subscriberId)
                .ThenByDescending(c => c.Status == "active")
                .ThenByDescending(c => c.CoverageEndDate)
                .FirstOrDefaultAsync();
        }

        private async Task<EligibilityCheck?> LatestCheckAsync(Coverage coverage, TimeSpan? maxAge)
        {
            var q = _db.EligibilityChecks.AsNoTracking()
                .Where(e => e.RespondedAt != null && (e.CoverageId == coverage.Id || (e.CoverageId == null && e.PatientId == coverage.PatientId && e.InsurerId == coverage.InsurerId)));
            if (maxAge.HasValue)
            {
                var since = DateTime.UtcNow - maxAge.Value;
                q = q.Where(e => e.RespondedAt >= since);
            }
            if (Invalidated.TryGetValue(coverage.MemberID, out var invalidatedAt))
                q = q.Where(e => e.RespondedAt > invalidatedAt);
            return await q.OrderByDescending(e => e.RespondedAt).FirstOrDefaultAsync();
        }

        private static List<BenefitDto> ParseBenefits(string? json)
        {
            var list = new List<BenefitDto>();
            if (string.IsNullOrWhiteSpace(json)) return list;
            try
            {
                using var doc = JsonDocument.Parse(json);
                foreach (var insurance in doc.RootElement.EnumerateArray())
                {
                    var inForce = !insurance.TryGetProperty("InForce", out var f) || f.ValueKind != JsonValueKind.False;
                    if (!insurance.TryGetProperty("Items", out var items)) continue;
                    foreach (var item in items.EnumerateArray())
                    {
                        var dto = new BenefitDto
                        {
                            BenefitCode = Str(item, "Category") ?? Str(item, "ProductOrService") ?? string.Empty,
                            BenefitName = Str(item, "Name") ?? Str(item, "Description") ?? Str(item, "Category") ?? string.Empty,
                            CoverageLevel = Str(item, "Unit") ?? string.Empty,
                            IsActive = inForce && !(item.TryGetProperty("Excluded", out var ex) && ex.ValueKind == JsonValueKind.True),
                        };
                        var types = new List<string>();
                        if (item.TryGetProperty("Benefits", out var benefits))
                        {
                            foreach (var b in benefits.EnumerateArray())
                            {
                                var type = Str(b, "Type") ?? string.Empty;
                                types.Add(type);
                                var allowed = Dec(b, "AllowedMoney") ?? Dec(b, "Allowed");
                                var used = Dec(b, "UsedMoney") ?? Dec(b, "Used");
                                switch (type)
                                {
                                    case "deductible":
                                        dto.Deductible = allowed ?? 0; dto.DeductibleMet = used ?? 0; break;
                                    case "copay":
                                        dto.CopayAmount = allowed ?? 0; break;
                                    case "copay-percent":
                                        dto.CoinsurancePercent = allowed ?? 0; break;
                                    case "copay-maximum":
                                    case "benefit":
                                        dto.OutOfPocketMax = allowed ?? dto.OutOfPocketMax; dto.OutOfPocketMet = used ?? dto.OutOfPocketMet; break;
                                }
                            }
                        }
                        dto.BenefitType = string.Join(",", types.Where(t => t.Length > 0).Distinct());
                        list.Add(dto);
                    }
                }
            }
            catch (JsonException)
            {
            }
            return list;
        }

        private static string? Str(JsonElement e, string name)
            => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static decimal? Dec(JsonElement e, string name)
        {
            if (!e.TryGetProperty(name, out var v)) return null;
            if (v.ValueKind == JsonValueKind.Number) return v.GetDecimal();
            return v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        private static List<BenefitDto> CoverageBenefits(Coverage coverage) => new()
        {
            new BenefitDto
            {
                BenefitCode = coverage.PlanCode ?? coverage.CoverageType,
                BenefitName = "Plan cost-sharing (stored coverage)",
                BenefitType = "plan",
                Deductible = coverage.AnnualDeductible,
                DeductibleMet = coverage.DeductibleMet,
                CopayAmount = coverage.Copay,
                CoinsurancePercent = coverage.CoinsurancePercentValue ?? coverage.CoinsurancePercent,
                OutOfPocketMax = coverage.MaxCopay ?? coverage.OutOfPocketMax,
                IsActive = coverage.Status == "active",
                CoverageLevel = coverage.SubscriberRelationship,
            }
        };

        private static bool ValidateEligibility(Coverage coverage, DateTime serviceDate)
            => coverage.Status == "active" && IsWithinPeriod(coverage, serviceDate);

        private static bool IsWithinPeriod(Coverage coverage, DateTime serviceDate)
            => (coverage.CoverageStartDate == default || serviceDate >= coverage.CoverageStartDate.Date)
               && (coverage.CoverageEndDate == default || serviceDate <= coverage.CoverageEndDate.Date);

        private static List<string> IdentifyEligibilityIssues(Coverage coverage, DateTime serviceDate)
        {
            var issues = new List<string>();
            if (coverage.Status != "active") issues.Add($"Coverage status is {coverage.Status}");
            if (coverage.CoverageStartDate != default && serviceDate < coverage.CoverageStartDate.Date)
                issues.Add($"Coverage starts on {coverage.CoverageStartDate:yyyy-MM-dd}");
            if (coverage.CoverageEndDate != default && serviceDate > coverage.CoverageEndDate.Date)
                issues.Add($"Coverage ended on {coverage.CoverageEndDate:yyyy-MM-dd}");
            return issues;
        }
    }
}
