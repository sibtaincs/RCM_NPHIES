using System.Security.Cryptography;

namespace NPhies_FHIR_Integration.ApiService.Security;

/// <summary>
/// Resolves secrets that must never live in appsettings files. Provide them through user-secrets
/// (development), environment variables (e.g. <c>JwtSettings__Secret</c>, <c>ConnectionStrings__DefaultConnection</c>)
/// or a secret store.
/// </summary>
public static class SecretConfiguration
{
    public const string JwtSecretKey = "JwtSettings:Secret";
    public const string ConnectionStringName = "DefaultConnection";
    public const int MinimumJwtSecretLength = 32;

    private const string DevelopmentConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=NPhiesDb;Trusted_Connection=true;TrustServerCertificate=true;";

    /// <summary>
    /// Validates required secrets at startup. In Development, missing values fall back to a local database and an
    /// ephemeral random JWT key (tokens are invalidated on restart); in other environments startup fails.
    /// </summary>
    public static void EnsureSecrets(ConfigurationManager configuration, IHostEnvironment environment)
    {
        var secret = configuration[JwtSecretKey];
        if (string.IsNullOrWhiteSpace(secret))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException(
                    $"'{JwtSecretKey}' is not configured. Set it via user-secrets or the JwtSettings__Secret environment variable.");
            configuration[JwtSecretKey] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        }
        else if (secret.Length < MinimumJwtSecretLength)
        {
            throw new InvalidOperationException($"'{JwtSecretKey}' must be at least {MinimumJwtSecretLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(ConnectionStringName)))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' is not configured. Set ConnectionStrings__{ConnectionStringName}.");
            configuration[$"ConnectionStrings:{ConnectionStringName}"] = DevelopmentConnectionString;
        }
    }

    /// <summary>Reads the JWT signing secret (validated by <see cref="EnsureSecrets"/>).</summary>
    public static string GetJwtSecret(IConfiguration configuration)
    {
        var secret = configuration[JwtSecretKey];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < MinimumJwtSecretLength)
            throw new InvalidOperationException($"'{JwtSecretKey}' is missing or shorter than {MinimumJwtSecretLength} characters.");
        return secret;
    }
}
