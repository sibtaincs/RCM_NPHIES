using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.ApiService.Security.Models;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.Security.Services;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<ApiResponse> LogoutAsync(string userId);
    Task<ApiResponse<JwtTokenModel>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<UserSecurityContext?> GetUserContextAsync(string userId);
}

/// <summary>
/// Database-backed authentication service.
/// Verifies credentials with PBKDF2 hashing, enforces account lockout,
/// and issues JWTs whose role claims come from the user's stored roles.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionResolver _permissionResolver;

    public AuthenticationService(
        ApplicationDbContext dbContext,
        IJwtService jwtService,
        IPasswordHashingService passwordHashingService,
        ILogger<AuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor,
        IPermissionResolver permissionResolver)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _passwordHashingService = passwordHashingService ?? throw new ArgumentNullException(nameof(passwordHashingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _permissionResolver = permissionResolver ?? throw new ArgumentNullException(nameof(permissionResolver));
    }

    private string? CurrentIp => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    private string? CurrentUserAgent => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

    /// <summary>
 /// Authenticate a user against the database.
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
 if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
 return Fail("Username and password are required");
 }

  var username = request.Username.Trim();

 var user = await _dbContext.Users
     .FirstOrDefaultAsync(u => u.Username == username && u.DeletedAt == null);

   // Generic message avoids leaking which usernames exist.
    if (user is null)
   {
     _logger.LogWarning("Login attempt for unknown user {Username}", username);
                RecordLoginAttempt(null, username, false, "User not found");
         await _dbContext.SaveChangesAsync();
    return Fail("Invalid username or password");
       }

            if (!user.IsActive)
            {
      RecordLoginAttempt(user.Id, username, false, "Account inactive");
        await _dbContext.SaveChangesAsync();
       return Fail("This account is inactive. Please contact an administrator.");
            }

            // Auto-release an expired lockout, otherwise reject.
         if (user.IsLocked)
            {
      if (user.LockedUntilAt.HasValue && user.LockedUntilAt.Value <= DateTime.UtcNow)
                {
     user.IsLocked = false;
         user.LockedUntilAt = null;
                 user.FailedLoginAttempts = 0;
     }
        else
          {
           RecordLoginAttempt(user.Id, username, false, "Account locked");
          await _dbContext.SaveChangesAsync();
     return Fail("This account is locked. Please try again later.");
      }
       }

      var passwordValid = _passwordHashingService.VerifyPassword(
       request.Password, user.PasswordHash, user.PasswordSalt);

     if (!passwordValid)
    {
           user.FailedLoginAttempts++;

    if (user.FailedLoginAttempts >= SecurityConstants.ACCOUNT_LOCKOUT_THRESHOLD)
         {
           user.IsLocked = true;
         user.LockedUntilAt = DateTime.UtcNow
           .AddMinutes(SecurityConstants.ACCOUNT_LOCKOUT_DURATION_MINUTES);

   _logger.LogWarning("User {Username} locked after {Count} failed attempts",
              username, user.FailedLoginAttempts);
    }

      RecordLoginAttempt(user.Id, username, false, "Invalid password");
        await _dbContext.SaveChangesAsync();
                return Fail("Invalid username or password");
     }

            // ---- Success: reset counters ----
     user.FailedLoginAttempts = 0;
      user.IsLocked = false;
  user.LockedUntilAt = null;
            user.LastLoginAt = DateTime.UtcNow;

     var userContext = await BuildContextAsync(user);
    var token = _jwtService.GenerateToken(userContext);

    // Persist the refresh token so it can be validated and revoked later.
    _dbContext.RefreshTokens.Add(new RefreshToken
      {
       UserId = user.Id,
           Token = token.RefreshToken ?? string.Empty,
       ExpiresAt = DateTime.UtcNow.AddDays(7),
     IpAddress = CurrentIp,
 UserAgent = CurrentUserAgent
       });

      RecordLoginAttempt(user.Id, username, true, null);
      await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {Username} logged in successfully", username);

          return new LoginResponse
 {
           Success = true,
          Message = "Login successful",
        Token = token,
     User = userContext,
          MfaRequired = false
            };
   }
        catch (Exception ex)
      {
        _logger.LogError(ex, "Error during login");
return Fail("An error occurred during login");
        }
    }

    /// <summary>
    /// Revoke all active refresh tokens for the user.
    /// </summary>
    public async Task<ApiResponse> LogoutAsync(string userId)
    {
     try
     {
            var tokens = await _dbContext.RefreshTokens
  .Where(t => t.UserId == userId && !t.IsRevoked)
  .ToListAsync();

            foreach (var token in tokens)
     {
    token.IsRevoked = true;
   token.RevokedAt = DateTime.UtcNow;
     token.RevokedBy = userId;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {UserId} logged out; {Count} token(s) revoked", userId, tokens.Count);
  return new ApiResponse { Success = true, Message = "Logout successful" };
   }
     catch (Exception ex)
  {
       _logger.LogError(ex, "Error during logout");
          return new ApiResponse { Success = false, Message = "An error occurred during logout" };
   }
    }

    /// <summary>
    /// Exchange a stored, unexpired refresh token for a new JWT (with rotation).
    /// </summary>
    public async Task<ApiResponse<JwtTokenModel>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
   return FailToken("Refresh token is required");
            }

  var stored = await _dbContext.RefreshTokens
        .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

   if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
{
      _logger.LogWarning("Refresh rejected: token missing, revoked, or expired");
        return FailToken("Invalid or expired refresh token");
     }

            var user = await _dbContext.Users
   .FirstOrDefaultAsync(u => u.Id == stored.UserId && u.DeletedAt == null);

   if (user is null || !user.IsActive || user.IsLocked)
 {
                return FailToken("User is no longer able to sign in");
            }

            var newToken = _jwtService.GenerateToken(await BuildContextAsync(user));

// Rotate: revoke the old token and link it to its replacement.
    stored.IsRevoked = true;
stored.RevokedAt = DateTime.UtcNow;
   stored.ReplacedByToken = newToken.RefreshToken;

            _dbContext.RefreshTokens.Add(new RefreshToken
     {
        UserId = user.Id,
    Token = newToken.RefreshToken ?? string.Empty,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
      IpAddress = CurrentIp,
         UserAgent = CurrentUserAgent
            });

            await _dbContext.SaveChangesAsync();

         return new ApiResponse<JwtTokenModel>
       {
            Success = true,
    Message = "Token refreshed successfully",
   Data = newToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return FailToken("An error occurred while refreshing token");
        }
    }

    /// <summary>
    /// Load the security context for a user id.
    /// </summary>
    public async Task<UserSecurityContext?> GetUserContextAsync(string userId)
    {
        try
        {
         var user = await _dbContext.Users
         .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

            return user is null ? null : await BuildContextAsync(user);
        }
        catch (Exception ex)
     {
    _logger.LogError(ex, "Error getting user context");
            return null;
        }
    }

    // ===================== Helpers =====================

    private async Task<UserSecurityContext> BuildContextAsync(User user)
    {
var context = new UserSecurityContext
        {
      UserId = user.Id,
     Username = user.Username,
     Email = user.Email,
   Roles = user.Roles ?? new List<string>(),
     OrganizationId = user.OrganizationId,
     DepartmentId = user.DepartmentId,
IsMfaVerified = false,
      LoginTime = DateTime.UtcNow,
    IpAddress = CurrentIp,
   UserAgent = CurrentUserAgent
      };

    // Effective permissions come from the database (role -> permission grants),
        // so administrators can change access without a redeploy.
        context.Permissions = await _permissionResolver.GetPermissionsForRolesAsync(context.Roles);
        return context;
    }

    private void RecordLoginAttempt(string? userId, string username, bool success, string? failureReason)
    {
        _dbContext.LoginAttempts.Add(new LoginAttempt
      {
    UserId = userId,
            Username = username,
            IpAddress = CurrentIp ?? string.Empty,
      UserAgent = CurrentUserAgent,
          IsSuccessful = success,
     FailureReason = failureReason
        });
    }

    private static LoginResponse Fail(string message) => new() { Success = false, Message = message };

    private static ApiResponse<JwtTokenModel> FailToken(string message) =>
    new() { Success = false, Message = message };

    /// <summary>
    /// Coarse-grained permissions derived from a role name.
    /// </summary>
private static List<string> GetPermissionsForRole(string role) => role switch
    {
   SecurityConstants.ADMIN_ROLE => new List<string>
        {
            SecurityConstants.READ_PERMISSION,
  SecurityConstants.CREATE_PERMISSION,
      SecurityConstants.UPDATE_PERMISSION,
            SecurityConstants.DELETE_PERMISSION,
       SecurityConstants.APPROVE_PERMISSION,
         SecurityConstants.ADMIN_PERMISSION
    },
        SecurityConstants.CLAIMS_PROCESSOR_ROLE => new List<string>
        {
 SecurityConstants.READ_CLAIMS_PERMISSION,
      SecurityConstants.CREATE_CLAIMS_PERMISSION,
         SecurityConstants.UPDATE_CLAIMS_PERMISSION
        },
      SecurityConstants.CLAIMS_REVIEWER_ROLE
        or SecurityConstants.TECHNICAL_REVIEWER_ROLE
      or SecurityConstants.MEDICAL_REVIEWER_ROLE => new List<string>
        {
     SecurityConstants.READ_CLAIMS_PERMISSION,
            SecurityConstants.APPROVE_PERMISSION,
      SecurityConstants.REJECT_PERMISSION
    },
        SecurityConstants.RCM_PROCESSOR_ROLE => new List<string>
        {
         SecurityConstants.READ_PERMISSION,
     SecurityConstants.UPDATE_PERMISSION,
  SecurityConstants.APPROVE_PERMISSION
        },
        SecurityConstants.RCM_VIEWER_ROLE
            or SecurityConstants.AUDITOR_ROLE => new List<string> { SecurityConstants.READ_PERMISSION },
        _ => new List<string> { SecurityConstants.READ_PERMISSION }
    };
}
