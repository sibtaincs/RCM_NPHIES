using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NPhies_FHIR_Integration.Application.Services.FHIR;
using NPhies_FHIR_Integration.Application.Mapping;
using NPhies_FHIR_Integration.Domain.Entities;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.IO;

namespace NPhies_FHIR_Integration.Tests.Manual;

/// <summary>
/// Manual test for creating and inspecting FHIR bundles
/// Run this to see actual JSON output
/// </summary>
public class ManualBundleCreationTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== NPHIES FHIR Bundle Creation Test ===\n");

        // Setup
        var mapperLogger = new Mock<ILogger<EntityToFhirMapper>>();
        var bundleLogger = new Mock<ILogger<FhirBundleService>>();

        var mapper = new EntityToFhirMapper(mapperLogger.Object);
        var bundleService = new FhirBundleService(bundleLogger.Object, mapper);

        // Create test data
        var patient = CreateTestPatient();
        var coverage = CreateTestCoverage();
        var provider = CreateTestProvider();
        var insurer = CreateTestInsurer();
        var eligibilityRequest = CreateTestEligibilityRequest();
        var claim = CreateTestClaim();

        Console.WriteLine("1. Testing Eligibility Request Bundle Creation...\n");
        await TestEligibilityBundle(bundleService, eligibilityRequest, patient, coverage, provider, insurer);

        Console.WriteLine("\n" + new string('=', 80) + "\n");

        Console.WriteLine("2. Testing Claim Request Bundle Creation...\n");
        await TestClaimBundle(bundleService, claim, patient, coverage, provider, insurer);

        Console.WriteLine("\n=== All Tests Complete ===");
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static async Task TestEligibilityBundle(
   IFhirBundleService bundleService,
      CoverageEligibilityRequest request,
        Patient patient,
        Coverage coverage,
Organization provider,
  Organization insurer)
    {
        try
        {
            // Create bundle
            var bundle = await bundleService.CreateEligibilityRequestBundleAsync(
              request, patient, coverage, provider, insurer);

            Console.WriteLine($"✓ Bundle created successfully");
            Console.WriteLine($"  - Bundle ID: {bundle.Id}");
            Console.WriteLine($"  - Bundle Type: {bundle.Type}");
            Console.WriteLine($"  - Timestamp: {bundle.Timestamp}");
            Console.WriteLine($"  - Entry Count: {bundle.Entry.Count}");

            // List all resources
            Console.WriteLine($"\n  Resources in bundle:");
            foreach (var entry in bundle.Entry)
            {
                Console.WriteLine($"    - {entry.Resource.TypeName} (ID: {entry.Resource.Id})");
            }

            // Serialize to JSON
            var json = await bundleService.SerializeToJsonAsync(bundle);
            Console.WriteLine($"\n  JSON Size: {json.Length} characters");

            // Validate
            var validation = await bundleService.ValidateBundleAsync(bundle);
            Console.WriteLine($"\n  Validation Result: {(validation.IsValid ? "VALID ✓" : "INVALID ✗")}");
            if (validation.Errors.Any())
            {
                Console.WriteLine("  Errors:");
                foreach (var error in validation.Errors)
                {
                    Console.WriteLine($"    - {error}");
                }
            }
            if (validation.Warnings.Any())
            {
                Console.WriteLine("  Warnings:");
                foreach (var warning in validation.Warnings)
                {
                    Console.WriteLine($"    - {warning}");
                }
            }

            // Save to file
            var filename = $"eligibility-request-{DateTime.Now:yyyyMMdd-HHmmss}.json";
            await File.WriteAllTextAsync(filename, json);
            Console.WriteLine($"\n  ✓ Bundle saved to: {filename}");

            // Pretty print sample
            Console.WriteLine($"\n  Sample JSON (first 500 chars):");
            Console.WriteLine($"  {json.Substring(0, Math.Min(500, json.Length))}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
            Console.WriteLine($"  Stack: {ex.StackTrace}");
        }
    }

    private static async Task TestClaimBundle(
      IFhirBundleService bundleService,
   Claim claim,
   Patient patient,
        Coverage coverage,
  Organization provider,
     Organization insurer)
    {
        try
        {
            // Create bundle
            var bundle = await _bundleService.CreateClaimRequestBundleAsync(
             claim, patient, provider, insurer, coverage);

            Console.WriteLine($"✓ Bundle created successfully");
            Console.WriteLine($"  - Bundle ID: {bundle.Id}");
            Console.WriteLine($"  - Bundle Type: {bundle.Type}");
            Console.WriteLine($"  - Entry Count: {bundle.Entry.Count}");

            // List all resources
            Console.WriteLine($"\n  Resources in bundle:");
            foreach (var entry in bundle.Entry)
            {
                Console.WriteLine($"    - {entry.Resource.TypeName} (ID: {entry.Resource.Id})");
            }

            // Serialize
            var json = await bundleService.SerializeToJsonAsync(bundle);
            Console.WriteLine($"\n  JSON Size: {json.Length} characters");

            // Validate
            var validation = await bundleService.ValidateBundleAsync(bundle);
            Console.WriteLine($"\n  Validation Result: {(validation.IsValid ? "VALID ✓" : "INVALID ✗")}");

            // Save
            var filename = $"claim-request-{DateTime.Now:yyyyMMdd-HHmmss}.json";
            await File.WriteAllTextAsync(filename, json);
            Console.WriteLine($"\n  ✓ Bundle saved to: {filename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }

    #region Test Data

    private static Patient CreateTestPatient() => new Patient
    {
        Id = "pat-test-001",
        NationalId = "1234567890",
        MRN = "MRN-12345",
        FirstName = "Mohammed",
        LastName = "Ahmed",
        FullName = "Mohammed Ahmed",
        DateOfBirth = new DateTime(1990, 1, 1),
        Gender = "M",
        Phone = "+966501234567",
        Email = "mohammed.ahmed@example.com",
        AddressLine1 = "123 King Fahd Road",
        AddressLine2 = "Al Olaya",
        City = "Riyadh",
        State = "Riyadh",
        PostalCode = "12345",
        Country = "SA",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static Coverage CreateTestCoverage() => new Coverage
    {
        Id = "cov-test-001",
        PolicyNumber = "POL-123456",
        MemberID = "MEM-123456",
        CoverageType = "EHCPOL",
        PatientId = "pat-test-001",
        SubscriberPatientId = "pat-test-001",
        SubscriberRelationship = "self",
        InsurerId = "org-payer-001",
        CoverageStartDate = new DateTime(2024, 1, 1),
        CoverageEndDate = new DateTime(2024, 12, 31),
        Status = "active",
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    private static Organization CreateTestProvider() => new Organization
    {
        Id = "org-prov-001",
        OrganizationName = "ABC Hospital",
        LicenseNumber = "PR-123456",
        OrganizationType = "provider",
        Status = "active",
        PhoneNumber = "+966112345678",
        Email = "info@abchospital.sa",
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    private static Organization CreateTestInsurer() => new Organization
    {
        Id = "org-payer-001",
        OrganizationName = "XYZ Insurance Company",
        LicenseNumber = "PY-789012",
        OrganizationType = "payer",
        Status = "active",
        PhoneNumber = "+966113456789",
        Email = "contact@xyzinsurance.sa",
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    private static CoverageEligibilityRequest CreateTestEligibilityRequest() => new CoverageEligibilityRequest
    {
        Id = "req-test-001",
        RequestId = "REQ-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
        MessageUUID = Guid.NewGuid().ToString(),
        Priority = "normal",
        Status = "active",
        ServiceDate = DateTime.Today.AddDays(7),
        PatientId = "pat-test-001",
        CoverageId = "cov-test-001",
        ProviderId = "org-prov-001",
        InsurerId = "org-payer-001",
        RequestCreatedAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    private static Claim CreateTestClaim() => new Claim
    {
        Id = "claim-test-001",
        ClaimNumber = "CLM-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
        ClaimType = "institutional",
        ClaimSubType = "ip",
        Use = "claim",
        Priority = "normal",
        Total = 5000.00m,
        Status = "active",
        PatientId = "pat-test-001",
        ProviderId = "org-prov-001",
        PayerId = "org-payer-001",
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    #endregion
}
