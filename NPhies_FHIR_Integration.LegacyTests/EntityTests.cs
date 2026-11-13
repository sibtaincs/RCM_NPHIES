using NPhies_FHIR_Integration.Domain.Entities;
using Xunit;

namespace NPhies_FHIR_Integration.Tests.Domain;

/// <summary>
/// Unit tests for Patient entity
/// </summary>
public class PatientTests
{
    [Fact]
    public void GetFullName_ShouldReturnCombinedName()
    {
        // Arrange
        var patient = new Patient
        {
            FirstName = "Mohammed",
    LastName = "Al-Mutairi"
     };

  // Act
        var fullName = patient.GetFullName();

        // Assert
        Assert.Equal("Mohammed Al-Mutairi", fullName);
    }

    [Fact]
    public void GetAge_ShouldCalculateCorrectAge()
    {
     // Arrange
   var birthDate = DateTime.Now.AddYears(-30);
     var patient = new Patient
        {
    DateOfBirth = birthDate
        };

   // Act
        var age = patient.GetAge();

        // Assert
     Assert.Equal(30, age);
    }

    [Fact]
    public void GetAge_ShouldHandleBirthdayNotYetThisYear()
    {
        // Arrange
  var today = DateTime.Today;
        var birthDate = new DateTime(today.Year - 25, today.Month, today.Day);
        if (birthDate > today)
        {
            birthDate = birthDate.AddDays(-1);
      }

        var patient = new Patient
        {
            DateOfBirth = birthDate
   };

        // Act
        var age = patient.GetAge();

        // Assert
    Assert.True(age == 24 || age == 25);
    }

    [Fact]
    public void Patient_ShouldInitializeWithDefaultStatus()
    {
        // Arrange & Act
      var patient = new Patient();

      // Assert
        Assert.Equal("active", patient.Status);
    }

    [Fact]
    public void Patient_ShouldInitializeWithEmptyCollections()
    {
    // Arrange & Act
        var patient = new Patient();

// Assert
   Assert.NotNull(patient.Coverages);
 Assert.NotNull(patient.EligibilityRequests);
   Assert.NotNull(patient.Claims);
        Assert.Empty(patient.Coverages);
        Assert.Empty(patient.EligibilityRequests);
     Assert.Empty(patient.Claims);
    }

    [Theory]
    [InlineData("M")]
    [InlineData("F")]
    [InlineData("O")]
    [InlineData("U")]
    public void Patient_ShouldAcceptValidGenders(string gender)
 {
        // Arrange & Act
        var patient = new Patient { Gender = gender };

        // Assert
     Assert.Equal(gender, patient.Gender);
    }
}

/// <summary>
/// Unit tests for Coverage entity
/// </summary>
public class CoverageTests
{
    [Fact]
  public void Coverage_ShouldInitializeWithDefaultStatus()
    {
  // Arrange & Act
   var coverage = new Coverage();

 // Assert
        Assert.Equal("active", coverage.Status);
    }

    [Fact]
    public void Coverage_ShouldAcceptValidPolicyNumber()
    {
        // Arrange & Act
        var coverage = new Coverage { PolicyNumber = "POL-2024-001" };

        // Assert
   Assert.Equal("POL-2024-001", coverage.PolicyNumber);
    }

    [Fact]
    public void Coverage_ShouldTrackFinancialData()
    {
    // Arrange & Act
        var coverage = new Coverage
        {
            AnnualDeductible = 500,
        Copay = 50,
  CoinsurancePercent = 20,
 OutOfPocketMax = 2000
        };

      // Assert
        Assert.Equal(500, coverage.AnnualDeductible);
        Assert.Equal(50, coverage.Copay);
        Assert.Equal(20, coverage.CoinsurancePercent);
        Assert.Equal(2000, coverage.OutOfPocketMax);
    }

    [Fact]
    public void Coverage_ShouldInitializeWithEmptyCollections()
    {
   // Arrange & Act
var coverage = new Coverage();

        // Assert
      Assert.NotNull(coverage.EligibilityRequests);
        Assert.NotNull(coverage.Claims);
    Assert.Empty(coverage.EligibilityRequests);
Assert.Empty(coverage.Claims);
    }
}

/// <summary>
/// Unit tests for Organization entity
/// </summary>
public class OrganizationTests
{
    [Fact]
    public void Organization_ShouldInitializeWithEmptyCollections()
    {
     // Arrange & Act
        var org = new Organization();

        // Assert
        Assert.NotNull(org.Locations);
        Assert.NotNull(org.Practitioners);
        Assert.NotNull(org.SubmittedClaims);
        Assert.NotNull(org.ProcessedClaims);
 Assert.NotNull(org.EligibilityRequests);
        Assert.NotNull(org.EligibilityResponses);
    }

    [Theory]
    [InlineData("prov")]
    [InlineData("ins")]
    public void Organization_ShouldAcceptValidTypes(string orgType)
    {
        // Arrange & Act
        var org = new Organization { OrganizationType = orgType };

     // Assert
        Assert.Equal(orgType, org.OrganizationType);
    }

 [Fact]
    public void Organization_ShouldStoreContactInformation()
    {
        // Arrange & Act
        var org = new Organization
        {
            Email = "contact@example.com",
   PhoneNumber = "+966501234567",
         Website = "https://example.com"
        };

      // Assert
        Assert.Equal("contact@example.com", org.Email);
        Assert.Equal("+966501234567", org.PhoneNumber);
        Assert.Equal("https://example.com", org.Website);
    }
}

/// <summary>
/// Unit tests for EligibilityItem entity
/// </summary>
public class EligibilityItemTests
{
    [Fact]
    public void EligibilityItem_ShouldInitializeWithEmptyModifiers()
    {
      // Arrange & Act
  var item = new EligibilityItem();

        // Assert
        Assert.NotNull(item.Modifiers);
        Assert.Empty(item.Modifiers);
    }

    [Fact]
    public void EligibilityItem_ShouldStoreCategoryInformation()
    {
    // Arrange & Act
        var item = new EligibilityItem
 {
            Category = "medical",
      CategorySystem = "http://nphies.sa/services",
          ProductOrServiceCode = "99213"
        };

        // Assert
        Assert.Equal("medical", item.Category);
        Assert.Equal("http://nphies.sa/services", item.CategorySystem);
  Assert.Equal("99213", item.ProductOrServiceCode);
    }
}

/// <summary>
/// Unit tests for CoverageEligibilityRequest entity
/// </summary>
public class CoverageEligibilityRequestTests
{
    [Fact]
    public void CoverageEligibilityRequest_ShouldInitializeWithEmptyItems()
    {
        // Arrange & Act
     var request = new CoverageEligibilityRequest();

        // Assert
    Assert.NotNull(request.Items);
        Assert.Empty(request.Items);
    }

    [Fact]
    public void CoverageEligibilityRequest_ShouldStoreStatusInformation()
    {
// Arrange & Act
     var request = new CoverageEligibilityRequest
     {
     Status = "active",
    Priority = "normal",
         RequestType = "eligibility"
        };

        // Assert
        Assert.Equal("active", request.Status);
        Assert.Equal("normal", request.Priority);
        Assert.Equal("eligibility", request.RequestType);
    }
}

/// <summary>
/// Unit tests for CoverageEligibilityResponse entity
/// </summary>
public class CoverageEligibilityResponseTests
{
[Fact]
    public void CoverageEligibilityResponse_ShouldInitializeWithEmptyCollections()
    {
        // Arrange & Act
        var response = new CoverageEligibilityResponse();

     // Assert
   Assert.NotNull(response.BenefitBalances);
   Assert.NotNull(response.Errors);
    Assert.Empty(response.BenefitBalances);
    Assert.Empty(response.Errors);
    }

    [Fact]
    public void CoverageEligibilityResponse_ShouldStoreResponseData()
    {
   // Arrange & Act
        var response = new CoverageEligibilityResponse
        {
  Status = "active",
      Outcome = "complete",
       IsInForce = true
        };

        // Assert
      Assert.Equal("active", response.Status);
    Assert.Equal("complete", response.Outcome);
     Assert.True(response.IsInForce);
    }
}

/// <summary>
/// Unit tests for BenefitBalance entity
/// </summary>
public class BenefitBalanceTests
{
    [Fact]
    public void BenefitBalance_ShouldInitializeWithEmptyBenefits()
    {
        // Arrange & Act
        var balance = new BenefitBalance();

   // Assert
        Assert.NotNull(balance.Benefits);
        Assert.Empty(balance.Benefits);
    }

    [Fact]
 public void BenefitBalance_ShouldStoreCategory()
{
        // Arrange & Act
        var balance = new BenefitBalance
        {
            Category = "medical",
            CategorySystem = "http://nphies.sa/benefits"
        };

        // Assert
        Assert.Equal("medical", balance.Category);
    Assert.Equal("http://nphies.sa/benefits", balance.CategorySystem);
    }
}

/// <summary>
/// Unit tests for Benefit entity
/// </summary>
public class BenefitTests
{
    [Fact]
    public void Benefit_ShouldStoreBenefitType()
    {
   // Arrange & Act
 var benefit = new Benefit
        {
    BenefitType = "copay",
            BenefitTypeSystem = "http://nphies.sa/benefit-type",
        AllowedAmount = 50,
            AllowedCurrency = "SAR"
        };

  // Assert
        Assert.Equal("copay", benefit.BenefitType);
        Assert.Equal(50, benefit.AllowedAmount);
      Assert.Equal("SAR", benefit.AllowedCurrency);
    }

    [Fact]
    public void Benefit_ShouldHandlePercentageBenefits()
    {
        // Arrange & Act
        var benefit = new Benefit
        {
    BenefitType = "coinsurance",
  PercentageAmount = 20
        };

      // Assert
        Assert.Equal("coinsurance", benefit.BenefitType);
    Assert.Equal(20, benefit.PercentageAmount);
  }
}
