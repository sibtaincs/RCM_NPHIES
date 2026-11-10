using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data;
using NPhies_FHIR_Integration.Infrastructure.Repositories;
using Xunit;

namespace NPhies_FHIR_Integration.Tests.Infrastructure;

/// <summary>
/// Unit tests for PatientRepository
/// </summary>
public class PatientRepositoryTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
     .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetByMRNAsync_ShouldReturnPatientWithMatchingMRN()
    {
    // Arrange
    var context = CreateInMemoryContext();
   var repository = new PatientRepository(context);

    var patient = new Patient
 {
  Id = Guid.NewGuid().ToString(),
     MRN = "MRN-001",
     FirstName = "Ahmed",
   LastName = "Hassan",
            Gender = "M"
        };

        await repository.AddAsync(patient);
      await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetByMRNAsync("MRN-001");

        // Assert
        Assert.NotNull(result);
 Assert.Equal("MRN-001", result.MRN);
    Assert.Equal("Ahmed", result.FirstName);
  }

    [Fact]
  public async Task GetByMRNAsync_ShouldReturnNullForNonExistentMRN()
    {
     // Arrange
        var context = CreateInMemoryContext();
var repository = new PatientRepository(context);

    // Act
  var result = await repository.GetByMRNAsync("NON-EXISTENT");

  // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsByMRNAsync_ShouldReturnTrueForExistingMRN()
    {
   // Arrange
        var context = CreateInMemoryContext();
    var repository = new PatientRepository(context);

       var patient = new Patient
        {
           Id = Guid.NewGuid().ToString(),
            MRN = "MRN-002",
            FirstName = "Fatima",
    LastName = "Al-Qahtani",
      Gender = "F"
 };

      await repository.AddAsync(patient);
  await repository.SaveChangesAsync();

      // Act
        var exists = await repository.ExistsByMRNAsync("MRN-002");

        // Assert
        Assert.True(exists);
    }

   [Fact]
 public async Task ExistsByMRNAsync_ShouldReturnFalseForNonExistentMRN()
    {
        // Arrange
        var context = CreateInMemoryContext();
       var repository = new PatientRepository(context);

      // Act
     var exists = await repository.ExistsByMRNAsync("DOES-NOT-EXIST");

        // Assert
   Assert.False(exists);
  }
}

/// <summary>
/// Unit tests for CoverageRepository
/// </summary>
public class CoverageRepositoryTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
  .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;

  return new ApplicationDbContext(options);
  }

   [Fact]
    public async Task GetByPolicyNumberAsync_ShouldReturnCoverageWithMatchingPolicy()
    {
      // Arrange
        var context = CreateInMemoryContext();
   var repository = new CoverageRepository(context);

   var coverage = new Coverage
    {
    Id = Guid.NewGuid().ToString(),
       PolicyNumber = "POL-2024-001",
          MemberID = "MEM-001",
 Status = "active"
        };

        await repository.AddAsync(coverage);
       await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetByPolicyNumberAsync("POL-2024-001");

        // Assert
      Assert.NotNull(result);
  Assert.Equal("POL-2024-001", result.PolicyNumber);
 Assert.Equal("MEM-001", result.MemberID);
    }

  [Fact]
    public async Task GetByPolicyNumberAsync_ShouldReturnNullForNonExistentPolicy()
    {
    // Arrange
        var context = CreateInMemoryContext();
       var repository = new CoverageRepository(context);

// Act
        var result = await repository.GetByPolicyNumberAsync("NON-EXISTENT");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task IsCoverageActiveAsync_ShouldReturnTrueForActiveOnDate()
    {
  // Arrange
      var context = CreateInMemoryContext();
        var repository = new CoverageRepository(context);

     var today = DateTime.UtcNow.Date;
        var coverage = new Coverage
        {
      Id = Guid.NewGuid().ToString(),
    PolicyNumber = "POL-2024-002",
      MemberID = "MEM-002",
  Status = "active",
            CoverageStartDate = today.AddDays(-10),
   CoverageEndDate = today.AddDays(10)
 };

  await repository.AddAsync(coverage);
    await repository.SaveChangesAsync();

 // Act
 var isActive = await repository.IsCoverageActiveAsync(coverage.Id, today);

   // Assert
   Assert.True(isActive);
    }

    [Fact]
    public async Task IsCoverageActiveAsync_ShouldReturnFalseForInactiveOnDate()
    {
        // Arrange
   var context = CreateInMemoryContext();
        var repository = new CoverageRepository(context);

     var today = DateTime.UtcNow.Date;
   var coverage = new Coverage
        {
  Id = Guid.NewGuid().ToString(),
PolicyNumber = "POL-2024-003",
  MemberID = "MEM-003",
      Status = "active",
     CoverageStartDate = today.AddDays(10),
       CoverageEndDate = today.AddDays(20)
        };

        await repository.AddAsync(coverage);
        await repository.SaveChangesAsync();

    // Act
        var isActive = await repository.IsCoverageActiveAsync(coverage.Id, today);

        // Assert
        Assert.False(isActive);
    }
}

/// <summary>
/// Unit tests for OrganizationRepository
/// </summary>
public class OrganizationRepositoryTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
     .Options;

        return new ApplicationDbContext(options);
    }

  [Fact]
    public async Task GetByLicenseNumberAsync_ShouldReturnOrganizationWithMatchingLicense()
    {
        // Arrange
        var context = CreateInMemoryContext();
      var repository = new OrganizationRepository(context);

        var org = new Organization
 {
            Id = Guid.NewGuid().ToString(),
             OrganizationName = "Test Hospital",
       LicenseNumber = "LIC-2024-001",
    OrganizationType = "prov",
         Status = "active"
        };

        await repository.AddAsync(org);
        await repository.SaveChangesAsync();

     // Act
        var result = await repository.GetByLicenseNumberAsync("LIC-2024-001");

        // Assert
     Assert.NotNull(result);
        Assert.Equal("LIC-2024-001", result.LicenseNumber);
       Assert.Equal("Test Hospital", result.OrganizationName);
    }

[Fact]
    public async Task GetProvidersAsync_ShouldReturnOnlyProviders()
    {
     // Arrange
        var context = CreateInMemoryContext();
        var repository = new OrganizationRepository(context);

     var provider = new Organization
        {
          Id = Guid.NewGuid().ToString(),
        OrganizationName = "Medical Center",
  LicenseNumber = "PROV-001",
    OrganizationType = "prov",
   Status = "active"
        };

     var insurer = new Organization
        {
       Id = Guid.NewGuid().ToString(),
 OrganizationName = "Insurance Co",
      LicenseNumber = "INS-001",
    OrganizationType = "ins",
 Status = "active"
        };

     await repository.AddAsync(provider);
        await repository.AddAsync(insurer);
     await repository.SaveChangesAsync();

        // Act
        var providers = await repository.GetProvidersAsync();

        // Assert
  Assert.NotEmpty(providers);
      Assert.All(providers, p => Assert.Equal("prov", p.OrganizationType));
    Assert.All(providers, p => Assert.Equal("active", p.Status));
    }

  [Fact]
   public async Task GetInsurersAsync_ShouldReturnOnlyInsurers()
    {
        // Arrange
       var context = CreateInMemoryContext();
var repository = new OrganizationRepository(context);

     var provider = new Organization
      {
   Id = Guid.NewGuid().ToString(),
    OrganizationName = "Medical Center",
      LicenseNumber = "PROV-002",
  OrganizationType = "prov",
   Status = "active"
        };

    var insurer = new Organization
        {
    Id = Guid.NewGuid().ToString(),
      OrganizationName = "Insurance Co",
       LicenseNumber = "INS-002",
 OrganizationType = "ins",
      Status = "active"
        };

        await repository.AddAsync(provider);
     await repository.AddAsync(insurer);
        await repository.SaveChangesAsync();

     // Act
      var insurers = await repository.GetInsurersAsync();

   // Assert
        Assert.NotEmpty(insurers);
     Assert.All(insurers, i => Assert.Equal("ins", i.OrganizationType));
     Assert.All(insurers, i => Assert.Equal("active", i.Status));
    }
}

/// <summary>
/// Unit tests for CoverageEligibilityRequestRepository
/// </summary>
public class CoverageEligibilityRequestRepositoryTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
     .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;

        return new ApplicationDbContext(options);
   }

    [Fact]
    public async Task GetPendingRequestsAsync_ShouldReturnOnlyPendingRequests()
    {
        // Arrange
  var context = CreateInMemoryContext();
        var repository = new CoverageEligibilityRequestRepository(context);

  var pendingRequest = new CoverageEligibilityRequest
        {
     Id = Guid.NewGuid().ToString(),
RequestId = "REQ-001",
     Status = "submitted"
        };

var completedRequest = new CoverageEligibilityRequest
        {
       Id = Guid.NewGuid().ToString(),
   RequestId = "REQ-002",
   Status = "responded"
        };

      await repository.AddAsync(pendingRequest);
      await repository.AddAsync(completedRequest);
      await repository.SaveChangesAsync();

  // Act
        var pending = await repository.GetPendingRequestsAsync();

// Assert
    Assert.NotEmpty(pending);
        Assert.All(pending, r => Assert.Contains(r.Status, new[] { "submitted", "acknowledged" }));
    }

  [Fact]
    public async Task GetByStatusAsync_ShouldReturnRequestsWithStatus()
    {
        // Arrange
        var context = CreateInMemoryContext();
   var repository = new CoverageEligibilityRequestRepository(context);

        var request1 = new CoverageEligibilityRequest
        {
   Id = Guid.NewGuid().ToString(),
 RequestId = "REQ-003",
     Status = "active"
        };

      var request2 = new CoverageEligibilityRequest
        {
  Id = Guid.NewGuid().ToString(),
        RequestId = "REQ-004",
     Status = "active"
     };

 await repository.AddAsync(request1);
        await repository.AddAsync(request2);
  await repository.SaveChangesAsync();

        // Act
    var requests = await repository.GetByStatusAsync("active");

  // Assert
        Assert.NotEmpty(requests);
       Assert.All(requests, r => Assert.Equal("active", r.Status));
    }
}
