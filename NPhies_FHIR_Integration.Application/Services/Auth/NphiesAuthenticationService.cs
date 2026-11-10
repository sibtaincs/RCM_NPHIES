using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Application.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NPhies_FHIR_Integration.Application.Services.Auth;

/// <summary>
/// NPHIES authentication service implementation
/// Handles OAuth2 client credentials flow and token caching
/// </summary>
public class NphiesAuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NphiesAuthenticationService> _logger;
    private readonly NphiesConfiguration _config;
  private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public NphiesAuthenticationService(
   HttpClient httpClient,
        ILogger<NphiesAuthenticationService> logger,
        IOptions<NphiesConfiguration> config)
  {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
 _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Get access token (cached if valid)
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Check if cached token is still valid
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
  _logger.LogDebug("Using cached access token (expires in {Minutes} minutes)",
        (_tokenExpiry - DateTime.UtcNow).TotalMinutes);
return _cachedToken;
        }

        // Need to get new token (thread-safe)
      await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
    if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            {
        return _cachedToken;
       }

         // Get new token
return await RefreshTokenAsync(cancellationToken);
        }
        finally
     {
    _tokenLock.Release();
  }
  }

    /// <summary>
    /// Refresh the access token
    /// </summary>
    public async Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
    try
{
            _logger.LogInformation("Requesting new access token from NPHIES");

         // Validate configuration
    if (string.IsNullOrEmpty(_config.Auth.Authority))
  {
      throw new NphiesAuthenticationException("Auth Authority not configured");
       }

       if (string.IsNullOrEmpty(_config.Auth.ClientId) ||
   string.IsNullOrEmpty(_config.Auth.ClientSecret))
  {
       throw new NphiesAuthenticationException("Client credentials not configured");
       }

  // Build token request
       var tokenEndpoint = _config.Auth.Authority.TrimEnd('/') + _config.Auth.TokenEndpoint;
      var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
          {
         { "grant_type", "client_credentials" },
    { "client_id", _config.Auth.ClientId },
             { "client_secret", _config.Auth.ClientSecret },
    { "scope", _config.Auth.Scope }
});

  // Send token request
       using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            request.Content = requestContent;
 request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

 var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
     {
       var error = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Token request failed with status {Status}: {Error}",
   response.StatusCode, error);

       throw new NphiesAuthenticationException(
    $"Failed to obtain access token: {response.StatusCode}");
     }

       // Parse token response
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
         var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseJson,
     new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

      if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
      throw new NphiesAuthenticationException("Invalid token response from NPHIES");
    }

            // Cache token
       _cachedToken = tokenResponse.AccessToken;

            // Calculate expiry (use 90% of actual expiry for safety margin)
            var expiresIn = tokenResponse.ExpiresIn > 0
              ? tokenResponse.ExpiresIn
    : _config.Auth.TokenCacheDurationMinutes * 60;

         _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn * 0.9);

        _logger.LogInformation("Successfully obtained access token (expires in {Minutes} minutes)",
      (_tokenExpiry - DateTime.UtcNow).TotalMinutes);

       return _cachedToken;
   }
        catch (HttpRequestException ex)
  {
        _logger.LogError(ex, "HTTP error while requesting access token");
    throw new NphiesAuthenticationException("Failed to communicate with auth server", ex);
    }
        catch (JsonException ex)
     {
     _logger.LogError(ex, "Failed to parse token response");
       throw new NphiesAuthenticationException("Invalid token response format", ex);
        }
        catch (NphiesAuthenticationException)
        {
       // Re-throw our custom exceptions
    throw;
      }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Unexpected error while requesting access token");
   throw new NphiesAuthenticationException("Unexpected authentication error", ex);
 }
    }

    /// <summary>
    /// Validate if token is still valid
    /// </summary>
    public async Task<bool> IsTokenValidAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
       return false;

        try
     {
   // Parse JWT token to check expiry
          var handler = new JwtSecurityTokenHandler();

      if (!handler.CanReadToken(token))
    {
             _logger.LogWarning("Token is not a valid JWT");
    return false;
    }

     var jwtToken = handler.ReadJwtToken(token);

       // Check expiry
            var expiry = jwtToken.ValidTo;
  var isValid = expiry > DateTime.UtcNow.AddMinutes(1); // 1 min buffer

   _logger.LogDebug("Token validation: IsValid={IsValid}, Expiry={Expiry}",
         isValid, expiry);

       return await System.Threading.Tasks.Task.FromResult(isValid);
   }
        catch (Exception ex)
        {
        _logger.LogWarning(ex, "Error validating token");
   return false;
        }
    }

    /// <summary>
    /// Clear cached token
    /// </summary>
  public void ClearToken()
    {
      _logger.LogInformation("Clearing cached access token");
   _cachedToken = null;
        _tokenExpiry = DateTime.MinValue;
    }

    /// <summary>
    /// Token response model
  /// </summary>
    private class TokenResponse
  {
   public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
  public int ExpiresIn { get; set; }
     public string? Scope { get; set; }
    }
}
