namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// Coverage DTO for API responses
/// </summary>
public class CoverageDto : BaseDto
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string MemberID { get; set; } = string.Empty;
    public string CoverageType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string InsurerId { get; set; } = string.Empty;
    public DateTime CoverageStartDate { get; set; }
    public DateTime CoverageEndDate { get; set; }
    public string RelationToSubscriber { get; set; } = string.Empty;
    public decimal AnnualDeductible { get; set; }
    public decimal DeductibleMet { get; set; }
    public decimal Copay { get; set; }
    public decimal CoinsurancePercent { get; set; }
    public decimal OutOfPocketMax { get; set; }

    /// <summary>
    /// Check if coverage is currently active
    /// </summary>
    public bool IsCurrentlyActive => DateTime.UtcNow >= CoverageStartDate && DateTime.UtcNow <= CoverageEndDate && Status == "active";
}

/// <summary>
/// Create Coverage DTO for API requests
/// </summary>
public class CreateCoverageDto
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string MemberID { get; set; } = string.Empty;
    public string CoverageType { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string InsurerId { get; set; } = string.Empty;
    public DateTime CoverageStartDate { get; set; }
    public DateTime CoverageEndDate { get; set; }
    public string RelationToSubscriber { get; set; } = string.Empty;
    public decimal AnnualDeductible { get; set; }
    public decimal Copay { get; set; }
    public decimal CoinsurancePercent { get; set; }
    public decimal OutOfPocketMax { get; set; }
}

/// <summary>
/// Update Coverage DTO for API requests
/// </summary>
public class UpdateCoverageDto
{
    public string Status { get; set; } = string.Empty;
    public decimal DeductibleMet { get; set; }
    public DateTime? CoverageEndDate { get; set; }
}

/// <summary>
/// Coverage search result DTO
/// </summary>
public class CoverageSearchResultDto
{
    public IEnumerable<CoverageDto> Items { get; set; } = new List<CoverageDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

/// <summary>
/// Coverage with patient info DTO
/// </summary>
public class CoverageWithPatientDto : CoverageDto
{
    public string PatientName { get; set; } = string.Empty;
    public string PatientMRN { get; set; } = string.Empty;
}

/// <summary>
/// Expiring coverage DTO
/// </summary>
public class ExpiringCoverageDto
{
    public string Id { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public DateTime CoverageEndDate { get; set; }
    public int DaysUntilExpiry => (int)(CoverageEndDate - DateTime.UtcNow).TotalDays;
}
