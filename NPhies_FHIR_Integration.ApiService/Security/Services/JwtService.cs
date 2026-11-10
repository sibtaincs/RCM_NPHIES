using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NPhies_FHIR_Integration.ApiService.Security.Models;
using NPhies_FHIR_Integration.ApiService.Security;

namespace NPhies_FHIR_Integration.ApiService.Security.Services;

/// <summary>
/// JWT service for token generation and validation
/// </summary>
public interface IJwtService
{
    JwtTokenModel GenerateToken(UserSecurityContext user);
    string GenerateRefreshToken();
    UserSecurityContext? ValidateToken(string token);
    bool ValidateRefreshToken(string refreshToken);
}

/// <summary>
/// JWT service implementation
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;
    private readonly string _jwtSecret;
 private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationMinutes;

 public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
   
      var jwtSettings = _configuration.GetSection("JwtSettings");
     _jwtSecret = SecretConfiguration.GetJwtSecret(_configuration);
        _jwtIssuer = jwtSettings["Issuer"] ?? "NPhiesIssuer";
   _jwtAudience = jwtSettings["Audience"] ?? "NPhiesAudience";
        _jwtExpirationMinutes = int.TryParse(jwtSettings["ExpirationMinutes"], out var exp) ? exp : 60;
    }

    public JwtTokenModel GenerateToken(UserSecurityContext user)
    {
        try
        {
var issuedAt = DateTime.UtcNow;
      var expiresAt = issuedAt.AddMinutes(_jwtExpirationMinutes);

            var claims = new List<Claim>
            {
        new Claim(SecurityConstants.USER_ID_CLAIM, user.UserId),
   new Claim(SecurityConstants.USERNAME_CLAIM, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
  new Claim(JwtRegisteredClaimNames.Iat, issuedAt.ToString("o")),
     };

          // Add optional claims
         if (!string.IsNullOrEmpty(user.Email))
 claims.Add(new Claim(SecurityConstants.EMAIL_CLAIM, user.Email));

         if (!string.IsNullOrEmpty(user.OrganizationId))
                claims.Add(new Claim(SecurityConstants.ORGANIZATION_CLAIM, user.OrganizationId));

 if (!string.IsNullOrEmpty(user.DepartmentId))
    claims.Add(new Claim(SecurityConstants.DEPARTMENT_CLAIM, user.DepartmentId));

   if (user.IsMfaVerified)
   claims.Add(new Claim(SecurityConstants.MFA_CLAIM, "true"));

 // Add roles
          foreach (var role in user.Roles)
claims.Add(new Claim(ClaimTypes.Role, role));

            // Add permissions
if (user.Permissions.Count > 0)
          claims.Add(new Claim(SecurityConstants.PERMISSION_CLAIM, string.Join(",", user.Permissions)));

         var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSecret));
       var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

     var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateJwtSecurityToken(
        issuer: _jwtIssuer,
         audience: _jwtAudience,
     subject: new ClaimsIdentity(claims),
       notBefore: issuedAt,
           expires: expiresAt,
 issuedAt: issuedAt,
signingCredentials: credentials
          );

   var accessToken = tokenHandler.WriteToken(securityToken);
    var refreshToken = GenerateRefreshToken();

return new JwtTokenModel
     {
 AccessToken = accessToken,
       RefreshToken = refreshToken,
                ExpiresIn = (long)(expiresAt - issuedAt).TotalSeconds,
         IssuedAt = issuedAt,
         ExpiresAt = expiresAt
       };
        }
        catch (Exception ex)
        {
    _logger.LogError(ex, "Error generating JWT token");
  throw;
     }
    }

  public string GenerateRefreshToken()
    {
   var randomNumber = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }
        return Convert.ToBase64String(randomNumber);
    }

    public UserSecurityContext? ValidateToken(string token)
    {
        try
        {
  var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSecret));
        var tokenHandler = new JwtSecurityTokenHandler();

 var validationParameters = new TokenValidationParameters
  {
          ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
      ValidateIssuer = true,
          ValidIssuer = _jwtIssuer,
    ValidateAudience = true,
                ValidAudience = _jwtAudience,
  ValidateLifetime = true,
    ClockSkew = TimeSpan.FromSeconds(10)
 };

       var principal = tokenHandler.ValidateToken(token, validationParameters, out var securityToken);

            var user = new UserSecurityContext
  {
      UserId = principal.FindFirst(SecurityConstants.USER_ID_CLAIM)?.Value ?? string.Empty,
      Username = principal.FindFirst(SecurityConstants.USERNAME_CLAIM)?.Value ?? string.Empty,
         Email = principal.FindFirst(SecurityConstants.EMAIL_CLAIM)?.Value,
           IsMfaVerified = bool.TryParse(principal.FindFirst(SecurityConstants.MFA_CLAIM)?.Value, out var mfa) && mfa,
 OrganizationId = principal.FindFirst(SecurityConstants.ORGANIZATION_CLAIM)?.Value
 };

// Add roles
      var roles = principal.FindAll(ClaimTypes.Role);
     user.Roles = roles.Select(r => r.Value).ToList();

            // Add permissions
       var permissionsClaim = principal.FindFirst(SecurityConstants.PERMISSION_CLAIM)?.Value;
            if (!string.IsNullOrEmpty(permissionsClaim))
            {
      user.Permissions = permissionsClaim.Split(',').ToList();
            }

         return user;
        }
catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
   return null;
        }
    }

    public bool ValidateRefreshToken(string refreshToken)
    {
        try
  {
   var tokenBytes = Convert.FromBase64String(refreshToken);
 return tokenBytes.Length > 0;
        }
        catch
        {
   return false;
        }
    }
}
