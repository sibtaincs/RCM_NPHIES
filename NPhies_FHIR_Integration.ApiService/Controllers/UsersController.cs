using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Services;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Admin-only user management: create users, assign roles, lock/unlock, reset passwords.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SecurityConstants.ADMIN_ROLE)]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHashingService _passwordHasher;
    private readonly ILogger<UsersController> _logger;

    /// <summary>All role names this system recognises.</summary>
    public static readonly string[] AllRoles =
    [
        SecurityConstants.ADMIN_ROLE,
     SecurityConstants.RCM_PROCESSOR_ROLE,
        SecurityConstants.RCM_VIEWER_ROLE,
        SecurityConstants.CLAIMS_PROCESSOR_ROLE,
        SecurityConstants.CLAIMS_REVIEWER_ROLE,
        SecurityConstants.TECHNICAL_REVIEWER_ROLE,
        SecurityConstants.MEDICAL_REVIEWER_ROLE,
     SecurityConstants.PAYER_USER_ROLE,
        SecurityConstants.PROVIDER_USER_ROLE,
     SecurityConstants.COMPLIANCE_OFFICER_ROLE,
        SecurityConstants.AUDITOR_ROLE,
        SecurityConstants.ACCOUNTANT_ROLE,
        SecurityConstants.USER_ROLE
    ];

    public UsersController(
        ApplicationDbContext dbContext,
        IPasswordHashingService passwordHasher,
        ILogger<UsersController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// List the role names available for assignment.
    /// Sourced from the data-driven AppRoles catalogue so custom roles are included.
    /// </summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _dbContext.AppRoles
 .AsNoTracking()
   .Where(r => r.IsActive)
   .OrderBy(r => r.Name)
     .Select(r => r.Name)
        .ToListAsync();

    // Fall back to the built-in list if the catalogue has not been seeded yet.
    return Ok(roles.Count > 0 ? roles : AllRoles);
    }

    /// <summary>
    /// Paged list of users with optional search over username/email/name.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? search = null)
    {
        var query = _dbContext.Users.AsNoTracking().Where(u => u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
         u.Username.Contains(term) ||
             u.Email.Contains(term) ||
     u.FirstName.Contains(term) ||
     u.LastName.Contains(term));
        }

        var total = await query.CountAsync();
        var users = await query
              .OrderBy(u => u.Username)
                   .Skip(skip)
                 .Take(Math.Clamp(take, 1, 200))
                   .Select(u => ToDto(u))
                .ToListAsync();

        return Ok(new { items = users, total });
    }

    /// <summary>
    /// Get a single user by id.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _dbContext.Users.AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

        return user is null
            ? NotFound(new { message = $"User {id} not found" })
          : Ok(ToDto(user));
    }

    /// <summary>
    /// Create a new user with the given roles.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var validation = ValidateNewUser(dto);
        if (validation is not null) return BadRequest(new { message = validation });

        var assignable = await GetAssignableRolesAsync();
        var unknownRoles = dto.Roles!.Where(r => !assignable.Contains(r)).ToList();
        if (unknownRoles.Count > 0)
      return BadRequest(new { message = $"Unknown role(s): {string.Join(", ", unknownRoles)}" });

        var exists = await _dbContext.Users
       .AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email);

        if (exists)
            return BadRequest(new { message = "Username or email already exists" });

        var (hash, salt) = _passwordHasher.HashPassword(dto.Password);

        var user = new User
        {
            Username = dto.Username.Trim(),
            Email = dto.Email.Trim(),
            FirstName = dto.FirstName?.Trim() ?? string.Empty,
            LastName = dto.LastName?.Trim() ?? string.Empty,
            PasswordHash = hash,
            PasswordSalt = salt,
            Roles = dto.Roles!.Distinct().ToList(),
            IsActive = true,
            OrganizationId = dto.OrganizationId,
            DepartmentId = dto.DepartmentId,
            CreatedBy = User.Identity?.Name ?? "admin"
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created user {Username} with roles {Roles}",
   user.Username, string.Join(",", user.Roles));

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ToDto(user));
    }

    /// <summary>
    /// Update a user's profile details and roles.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto dto)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        if (user is null) return NotFound(new { message = $"User {id} not found" });

        if (dto.Roles is not null)
        {
            var assignable = await GetAssignableRolesAsync();
       var invalid = dto.Roles.Where(r => !assignable.Contains(r)).ToList();
            if (invalid.Count > 0)
         return BadRequest(new { message = $"Unknown role(s): {string.Join(", ", invalid)}" });

            // Prevent removing the last remaining administrator.
            if (user.Roles.Contains(SecurityConstants.ADMIN_ROLE) &&
        !dto.Roles.Contains(SecurityConstants.ADMIN_ROLE) &&
           await IsLastAdminAsync(user.Id))
            {
                return BadRequest(new { message = "Cannot remove the Admin role from the last administrator." });
            }

            user.Roles = dto.Roles.Distinct().ToList();
        }

        if (dto.Email is not null) user.Email = dto.Email.Trim();
        if (dto.FirstName is not null) user.FirstName = dto.FirstName.Trim();
        if (dto.LastName is not null) user.LastName = dto.LastName.Trim();
        if (dto.OrganizationId is not null) user.OrganizationId = dto.OrganizationId;
        if (dto.DepartmentId is not null) user.DepartmentId = dto.DepartmentId;

        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = User.Identity?.Name ?? "admin";

        await _dbContext.SaveChangesAsync();
        return Ok(ToDto(user));
    }

    /// <summary>
    /// Activate or deactivate a user.
    /// </summary>
    [HttpPost("{id}/set-active")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetActive(string id, [FromQuery] bool isActive)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        if (user is null) return NotFound(new { message = $"User {id} not found" });

        if (!isActive && await IsLastAdminAsync(user.Id) && user.Roles.Contains(SecurityConstants.ADMIN_ROLE))
            return BadRequest(new { message = "Cannot deactivate the last administrator." });

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(ToDto(user));
    }

    /// <summary>
    /// Clear a lockout and reset the failed-attempt counter.
    /// </summary>
    [HttpPost("{id}/unlock")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        if (user is null) return NotFound(new { message = $"User {id} not found" });

        user.IsLocked = false;
        user.LockedUntilAt = null;
        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(ToDto(user));
    }

    /// <summary>
    /// Set a new password for a user and revoke their active sessions.
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) ||
       dto.NewPassword.Length < SecurityConstants.MINIMUM_PASSWORD_LENGTH)
        {
            return BadRequest(new
            {
                message = $"Password must be at least {SecurityConstants.MINIMUM_PASSWORD_LENGTH} characters."
            });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        if (user is null) return NotFound(new { message = $"User {id} not found" });

        var (hash, salt) = _passwordHasher.HashPassword(dto.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.IsLocked = false;
        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;

        // Force re-login everywhere.
        var tokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == id && !t.IsRevoked).ToListAsync();
        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Password reset for user {Username}", user.Username);

        return Ok(new { message = "Password reset successfully" });
    }

    /// <summary>
    /// Soft-delete a user.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        if (user is null) return NotFound(new { message = $"User {id} not found" });

        if (await IsLastAdminAsync(user.Id) && user.Roles.Contains(SecurityConstants.ADMIN_ROLE))
            return BadRequest(new { message = "Cannot delete the last administrator." });

        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    // ===================== Helpers =====================

    /// <summary>
    /// The set of role names that may be assigned, taken from the data-driven
    /// AppRoles catalogue with a fallback to the compiled-in list.
    /// </summary>
    private async Task<HashSet<string>> GetAssignableRolesAsync()
    {
        var roles = await _dbContext.AppRoles
     .AsNoTracking()
      .Where(r => r.IsActive)
   .Select(r => r.Name)
      .ToListAsync();

    return roles.Count > 0
    ? new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase)
     : new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> IsLastAdminAsync(string excludingUserId)
    {
        var otherAdmins = await _dbContext.Users
    .Where(u => u.Id != excludingUserId && u.DeletedAt == null && u.IsActive)
            .Select(u => u.Roles)
 .ToListAsync();

        return !otherAdmins.Any(roles => roles.Contains(SecurityConstants.ADMIN_ROLE));
    }

    private static string? ValidateNewUser(CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username)) return "Username is required";
        if (string.IsNullOrWhiteSpace(dto.Email)) return "Email is required";
        if (string.IsNullOrWhiteSpace(dto.Password) ||
   dto.Password.Length < SecurityConstants.MINIMUM_PASSWORD_LENGTH)
   return $"Password must be at least {SecurityConstants.MINIMUM_PASSWORD_LENGTH} characters";

      if (dto.Roles is null || dto.Roles.Count == 0) return "At least one role is required";

     return null;
    }

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Roles = u.Roles,
        IsActive = u.IsActive,
        IsLocked = u.IsLocked,
        FailedLoginAttempts = u.FailedLoginAttempts,
        LastLoginAt = u.LastLoginAt,
        CreatedAt = u.CreatedAt,
        OrganizationId = u.OrganizationId,
        DepartmentId = u.DepartmentId
    };
}

/// <summary>User projection returned by the admin API (never exposes password data).</summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? OrganizationId { get; set; }
    public string? DepartmentId { get; set; }
}

/// <summary>Payload for creating a user.</summary>
public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<string>? Roles { get; set; }
    public string? OrganizationId { get; set; }
    public string? DepartmentId { get; set; }
}

/// <summary>Payload for updating a user. Null members are left unchanged.</summary>
public class UpdateUserDto
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<string>? Roles { get; set; }
    public string? OrganizationId { get; set; }
    public string? DepartmentId { get; set; }
}

/// <summary>Payload for an admin password reset.</summary>
public class ResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}
