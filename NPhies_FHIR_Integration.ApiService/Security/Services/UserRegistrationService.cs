using NPhies_FHIR_Integration.ApiService.Security.Models;
using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.Security.Services;

/// <summary>
/// User registration service
/// </summary>
public interface IUserRegistrationService
{
    Task<(bool success, string message, UserSecurityContext? user)> RegisterUserAsync(UserRegistrationRequest request);
    Task<bool> UserExistsAsync(string username, string email);
 Task<User?> GetUserByUsernameAsync(string username);
}

/// <summary>
/// User registration service implementation
/// </summary>
public class UserRegistrationService : IUserRegistrationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ILogger<UserRegistrationService> _logger;

    public UserRegistrationService(
        ApplicationDbContext dbContext,
        IPasswordHashingService passwordHashingService,
        ILogger<UserRegistrationService> logger)
    {
        _dbContext = dbContext;
        _passwordHashingService = passwordHashingService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    public async Task<(bool success, string message, UserSecurityContext? user)> RegisterUserAsync(UserRegistrationRequest request)
    {
      try
        {
       // Validate input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
           return (false, "Username and password are required", null);
     }

    if (request.Password.Length < 8)
     {
    return (false, "Password must be at least 8 characters long", null);
            }

            // Check if user already exists
            var existingUser = await _dbContext.Users
   .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

if (existingUser != null)
            {
          return (false, "Username or email already exists", null);
     }

  // Hash password
 var (passwordHash, passwordSalt) = _passwordHashingService.HashPassword(request.Password);

        // Create new user
            var newUser = new User
     {
    Id = Guid.NewGuid().ToString(),
          Username = request.Username,
       Email = request.Email,
       FirstName = request.FirstName,
              LastName = request.LastName,
        PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
        Roles = request.Roles ?? new List<string> { SecurityConstants.USER_ROLE },
          IsActive = true,
     IsEmailVerified = false,
        IsMfaEnabled = false,
      IsLocked = false,
   FailedLoginAttempts = 0,
  CreatedAt = DateTime.UtcNow,
        CreatedBy = "system"
        };

    // Add user to database
     _dbContext.Users.Add(newUser);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {Username} registered successfully", request.Username);

            // Create user context
            var userContext = new UserSecurityContext
          {
     UserId = newUser.Id,
     Username = newUser.Username,
     Email = newUser.Email,
            Roles = newUser.Roles,
            IsMfaVerified = false,
    LoginTime = DateTime.UtcNow
     };

   return (true, "User registered successfully", userContext);
   }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
   return (false, "An error occurred during registration", null);
   }
    }

    /// <summary>
    /// Check if user already exists
    /// </summary>
    public async Task<bool> UserExistsAsync(string username, string email)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.Username == username || u.Email == email);
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _dbContext.Users
         .FirstOrDefaultAsync(u => u.Username == username);
  }
}

/// <summary>
/// User registration request
/// </summary>
public class UserRegistrationRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string>? Roles { get; set; }
}

/// <summary>
/// User registration response
/// </summary>
public class UserRegistrationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
  public UserSecurityContext? User { get; set; }
}
