namespace NPhies_FHIR_Integration.ApiService.Security.Models;

/// <summary>
/// User security context
/// </summary>
public class UserSecurityContext
{
    public string UserId { get; set; } = string.Empty;
  public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public string? OrganizationId { get; set; }
    public string? DepartmentId { get; set; }
    public bool IsMfaVerified { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime? LastActivityTime { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public bool HasRole(string role) => Roles.Contains(role);
    public bool HasPermission(string permission) => Permissions.Contains(permission);
    public bool HasAnyRole(params string[] roles) => roles.Any(r => Roles.Contains(r));
    public bool HasAllPermissions(params string[] permissions) => permissions.All(p => Permissions.Contains(p));
}

/// <summary>
/// JWT token model
/// </summary>
public class JwtTokenModel
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
  public long ExpiresIn { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Login request
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
 public bool RememberMe { get; set; }
 public string? MfaCode { get; set; }
}

/// <summary>
/// Login response
/// </summary>
public class LoginResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public JwtTokenModel? Token { get; set; }
    public UserSecurityContext? User { get; set; }
    public bool MfaRequired { get; set; }
}

/// <summary>
/// Refresh token request
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Logout request
/// </summary>
public class LogoutRequest
{
    public string? Token { get; set; }
}

/// <summary>
/// Generic API response
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Generic API response
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}
