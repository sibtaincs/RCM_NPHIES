namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// User entity for authentication and authorization
/// </summary>
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Username { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Password hash (PBKDF2 with salt)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Salt for password hashing
    /// </summary>
    public string PasswordSalt { get; set; } = string.Empty;
    
    public List<string> Roles { get; set; } = new();
    
    public bool IsActive { get; set; } = true;
    
    public bool IsEmailVerified { get; set; }
    
    public bool IsMfaEnabled { get; set; }
    
    public string? MfaSecret { get; set; }
  
    public bool IsLocked { get; set; }
    
    public int FailedLoginAttempts { get; set; }
    
    public DateTime? LastLoginAt { get; set; }
    
    public DateTime? LockedUntilAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? DeletedAt { get; set; }
    
public string? CreatedBy { get; set; }
    
    public string? UpdatedBy { get; set; }
    
    public string? OrganizationId { get; set; }
    
    public string? DepartmentId { get; set; }
    
    // Navigation properties
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    
    public virtual ICollection<LoginAttempt> LoginAttempts { get; set; } = new List<LoginAttempt>();
    
    public virtual ICollection<ApiRateLimitLog> RateLimitLogs { get; set; } = new List<ApiRateLimitLog>();
}

/// <summary>
/// Refresh token entity
/// </summary>
public class RefreshToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string UserId { get; set; } = string.Empty;
    
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }
    
    public bool IsRevoked { get; set; }
    
  public DateTime? RevokedAt { get; set; }
    
    public string? RevokedBy { get; set; }
    
    public string? ReplacedByToken { get; set; }
    
    // Navigation property
    public virtual User? User { get; set; }
}

/// <summary>
/// Login attempt tracking for security and audit
/// </summary>
public class LoginAttempt
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string? UserId { get; set; }
    
    public string Username { get; set; } = string.Empty;
    
    public string IpAddress { get; set; } = string.Empty;
    
 public string? UserAgent { get; set; }
    
    public bool IsSuccessful { get; set; }

    public string? FailureReason { get; set; }
    
  public DateTime AttemptAt { get; set; } = DateTime.UtcNow;
    
    public double? DurationMs { get; set; }
    
    // Navigation property
    public virtual User? User { get; set; }
}

/// <summary>
/// Audit log for tracking user actions
/// </summary>
public class AuditLog
{
 public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string? UserId { get; set; }
    
    public string? Username { get; set; }
    
    public string Action { get; set; } = string.Empty;
    
    public string? EntityType { get; set; }
    
    public string? EntityId { get; set; }
    
    public string? OldValues { get; set; }
    
    public string? NewValues { get; set; }
    
    public string? ChangeDetails { get; set; }
    
    public string IpAddress { get; set; } = string.Empty;
    
    public string? UserAgent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string AuditLevel { get; set; } = "Info"; // Info, Warning, Error, Critical
    
 public string? Endpoint { get; set; }
    
    public int? HttpStatusCode { get; set; }
    
    public double? DurationMs { get; set; }
    
    // Navigation property
    public virtual User? User { get; set; }
}

/// <summary>
/// API rate limit tracking
/// </summary>
public class ApiRateLimitLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string? UserId { get; set; }
    
    public string IpAddress { get; set; } = string.Empty;
    
public string Endpoint { get; set; } = string.Empty;
    
    public string HttpMethod { get; set; } = string.Empty;
    
    public int RequestCount { get; set; }
    
    public int MaxRequests { get; set; }
    
    public DateTime WindowStart { get; set; }
    
    public DateTime WindowEnd { get; set; }
    
    public bool IsRateLimited { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ResetAt { get; set; }
    
    // Navigation property
    public virtual User? User { get; set; }
}
