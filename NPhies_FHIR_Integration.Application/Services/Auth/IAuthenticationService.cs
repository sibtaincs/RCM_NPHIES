namespace NPhies_FHIR_Integration.Application.Services.Auth;

/// <summary>
/// Authentication service for NPHIES API
/// </summary>
public interface IAuthenticationService
{
  /// <summary>
    /// Get access token (returns cached token if still valid)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access token</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh the access token
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access token</returns>
    Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if a token is still valid
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>True if token is valid</returns>
Task<bool> IsTokenValidAsync(string token);

    /// <summary>
    /// Clear the cached token (force refresh on next request)
    /// </summary>
 void ClearToken();
}
