using System.Security.Cryptography;
using System.Text;

namespace NPhies_FHIR_Integration.ApiService.Security.Services;

/// <summary>
/// Password hashing service using PBKDF2 algorithm
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Hash a password with PBKDF2 algorithm
    /// </summary>
    (string hash, string salt) HashPassword(string password);

    /// <summary>
  /// Verify a password against a hash
 /// </summary>
    bool VerifyPassword(string password, string hash, string salt);
}

/// <summary>
/// Password hashing service implementation using PBKDF2
/// </summary>
public class PasswordHashingService : IPasswordHashingService
{
    // PBKDF2 parameters
    private const int KeySize = 64; // 512 bits
private const int IterationCount = 10000; // NIST recommends at least 10,000
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// Generate a cryptographic salt
  /// </summary>
    private static byte[] GenerateSalt()
    {
        byte[] salt = new byte[16]; // 128 bits
 using (var rng = RandomNumberGenerator.Create())
        {
         rng.GetBytes(salt);
      }
     return salt;
    }

 /// <summary>
    /// Hash a password using PBKDF2
    /// </summary>
    public (string hash, string salt) HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
    throw new ArgumentException("Password cannot be empty", nameof(password));

        var salt = GenerateSalt();
        var passwordBytes = Encoding.UTF8.GetBytes(password);

 // Derive key using PBKDF2
        var pbkdf2 = new Rfc2898DeriveBytes(
  passwordBytes,
 salt,
       IterationCount,
     Algorithm
  );

   byte[] hash = pbkdf2.GetBytes(KeySize);

        // Format: salt + hash (both base64 encoded)
        string saltBase64 = Convert.ToBase64String(salt);
string hashBase64 = Convert.ToBase64String(hash);

        return (hashBase64, saltBase64);
    }

    /// <summary>
    /// Verify a password against a stored hash
 /// </summary>
    public bool VerifyPassword(string password, string hash, string salt)
  {
        if (string.IsNullOrWhiteSpace(password))
        return false;

    if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
    return false;

        try
 {
 // Decode salt from base64
  byte[] saltBytes = Convert.FromBase64String(salt);

            // Derive key using the same parameters
    byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
  var pbkdf2 = new Rfc2898DeriveBytes(
  passwordBytes,
    saltBytes,
  IterationCount,
       Algorithm
   );

      byte[] computedHash = pbkdf2.GetBytes(KeySize);

          // Decode stored hash
   byte[] storedHash = Convert.FromBase64String(hash);

 // Constant-time comparison to prevent timing attacks
return ConstantTimeEquals(computedHash, storedHash);
      }
  catch
   {
    return false;
        }
    }

  /// <summary>
    /// Constant-time byte array comparison to prevent timing attacks
    /// </summary>
    private static bool ConstantTimeEquals(byte[] a, byte[] b)
    {
    if (a.Length != b.Length)
  return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
       result |= a[i] ^ b[i];
    }

   return result == 0;
    }
}
