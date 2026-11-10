using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using NPhies_FHIR_Integration.Application.Mapping;
using NPhies_FHIR_Integration.Domain.Entities;
using Hl7.Fhir.Model;
using System;

namespace NPhies_FHIR_Integration.Tests.Mapping;

/// <summary>
/// Unit tests for EntityToFhirMapper
/// </summary>
public class EntityToFhirMapperTests
{
    private readonly IEntityToFhirMapper _mapper;
    private readonly Mock<ILogger<EntityToFhirMapper>> _loggerMock;

    public EntityToFhirMapperTests()
    {
        _loggerMock = new Mock<ILogger<EntityToFhirMapper>>();
        _mapper = new EntityToFhirMapper(_loggerMock.Object);
    }

    #region Patient Mapping Tests

    [Fact]
    public void MapToFhirPatient_ValidPatient_ReturnsFhirPatient()
    {
        // Arrange
        var patient = new Patient
        {
            Id = "pat-123",
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
            City = "Riyadh",
            State = "Riyadh",
            PostalCode = "12345",
            Country = "SA",
            IsActive = true
        };

        // Act
        var fhirPatient = _mapper.MapToFhirPatient(patient);

        // Assert
        Assert.NotNull(fhirPatient);
        Assert.Equal("pat-123", fhirPatient.Id);
        Assert.Equal(2, fhirPatient.Identifier.Count);
        Assert.Contains(fhirPatient.Identifier, i => i.System == "urn:oid:2.16.840.1.113883.3.571.1.1" && i.Value == "1234567890");
        Assert.Contains(fhirPatient.Identifier, i => i.System == "http://nphies.sa/identifier/member-id" && i.Value == "MRN-12345");
        Assert.Equal("Ahmed", fhirPatient.Name[0].Family);
        Assert.Equal("Mohammed", fhirPatient.Name[0].Given.First());
        Assert.Equal("1990-01-01", fhirPatient.BirthDate);
        Assert.Equal(AdministrativeGender.Male, fhirPatient.Gender);
        Assert.True(fhirPatient.Active);
    }

    [Fact]
    public void MapToFhirPatient_FemalePatient_ReturnsCorrectGender()
    {
        // Arrange
        var patient = new Patient
        {
            Id = "pat-456",
            NationalId = "0987654321",
            MRN = "MRN-67890",
            FirstName = "Fatima",
            LastName = "Ali",
            DateOfBirth = new DateTime(1995, 5, 15),
            Gender = "F"
        };

        // Act
        var fhirPatient = _mapper.MapToFhirPatient(patient);

        // Assert
        Assert.Equal(AdministrativeGender.Female, fhirPatient.Gender);
    }

    #endregion

    #region Coverage Mapping Tests

    [Fact]
    public void MapToFhirCoverage_ValidCoverage_ReturnsFhirCoverage()
    {
        // Arrange
        var coverage = new Coverage
        {
            Id = "cov-123",
            PolicyNumber = "POL-123456",
            MemberID = "MEM-123456",
            CoverageType = "EHCPOL",
            PatientId = "pat-123",
            SubscriberPatientId = "pat-123",
            SubscriberRelationship = "self",
            InsurerId = "org-payer-001",
            CoverageStartDate = new DateTime(2024, 1, 1),
            CoverageEndDate = new DateTime(2024, 12, 31)
        };

        // Act
        var fhirCoverage = _mapper.MapToFhirCoverage(coverage);

        // Assert
        Assert.NotNull(fhirCoverage);
        Assert.Equal("cov-123", fhirCoverage.Id);
        Assert.Equal(2, fhirCoverage.Identifier.Count);
        Assert.Contains(fhirCoverage.Identifier, i => i.System == "http://nphies.sa/identifier/policy-number" && i.Value == "POL-123456");
        Assert.Equal(FinancialResourceStatusCodes.Active, fhirCoverage.Status);
        Assert.Equal("Patient/pat-123", fhirCoverage.Beneficiary.Reference);
        Assert.Equal("Patient/pat-123", fhirCoverage.Subscriber.Reference);
        Assert.Equal("Organization/org-payer-001", fhirCoverage.Payor[0].Reference);
    }

    [Fact]
    public void MapToFhirCoverage_DependentCoverage_UsesSubscriberPatient()
    {
        // Arrange
        var coverage = new Coverage
        {
            Id = "cov-456",
            PolicyNumber = "POL-789",
            MemberID = "MEM-789",
            PatientId = "pat-child",
            SubscriberPatientId = "pat-father",
            SubscriberRelationship = "child",
            InsurerId = "org-payer-001",
            CoverageStartDate = new DateTime(2024, 1, 1),
            CoverageEndDate = new DateTime(2024, 12, 31)
        };

        // Act
        var fhirCoverage = _mapper.MapToFhirCoverage(coverage);

        // Assert
        Assert.Equal("Patient/pat-child", fhirCoverage.Beneficiary.Reference);
        Assert.Equal("Patient/pat-father", fhirCoverage.Subscriber.Reference);
        Assert.Equal("child", fhirCoverage.Relationship.Coding[0].Code);
    }

    #endregion

    #region Organization Mapping Tests

    [Fact]
    public void MapToFhirOrganization_Provider_UsesProviderLicense()
    {
        // Arrange
        var organization = new Organization
        {
            Id = "org-123",
            OrganizationName = "ABC Hospital",
            LicenseNumber = "PR-123456",
            OrganizationType = "provider",
            Status = "active",
            PhoneNumber = "+966112345678",
            Email = "info@abchospital.sa"
        };

        // Act
        var fhirOrg = _mapper.MapToFhirOrganization(organization);

        // Assert
        Assert.NotNull(fhirOrg);
        Assert.Equal("org-123", fhirOrg.Id);
        Assert.Equal("ABC Hospital", fhirOrg.Name);
        Assert.True(fhirOrg.Active);
        Assert.Contains(fhirOrg.Identifier, i => i.System == "http://nphies.sa/license/provider-license" && i.Value == "PR-123456");
        Assert.Equal("prov", fhirOrg.Type[0].Coding[0].Code);
    }

    [Fact]
    public void MapToFhirOrganization_Payer_UsesPayerLicense()
    {
        // Arrange
        var organization = new Organization
        {
            Id = "org-456",
            OrganizationName = "XYZ Insurance",
            LicenseNumber = "PY-789012",
            OrganizationType = "payer",
            Status = "active"
        };

        // Act
        var fhirOrg = _mapper.MapToFhirOrganization(organization);

        // Assert
        Assert.Contains(fhirOrg.Identifier, i => i.System == "http://nphies.sa/license/payer-license" && i.Value == "PY-789012");
        Assert.Equal("ins", fhirOrg.Type[0].Coding[0].Code);
    }

    #endregion

    #region MessageHeader Tests

    [Fact]
    public void CreateMessageHeader_ValidEvent_ReturnsMessageHeader()
    {
        // Arrange
        var organization = new Organization
        {
            Id = "org-123",
            OrganizationName = "ABC Hospital",
            LicenseNumber = "PR-123456"
        };

        // Act
        var messageHeader = _mapper.CreateMessageHeader("eligibility-request", organization);

        // Assert
        Assert.NotNull(messageHeader);
        Assert.NotNull(messageHeader.Id);
        Assert.Equal("eligibility-request", messageHeader.Event.Code);
        Assert.Equal("http://nphies.sa/terminology/CodeSystem/ksa-message-events", messageHeader.Event.System);
        Assert.Equal("ABC Hospital", messageHeader.Source.Name);
        Assert.Equal("PR-123456", messageHeader.Source.Endpoint);
        Assert.Equal("NPHIES", messageHeader.Destination[0].Name);
    }

    #endregion

    #region Eligibility Request Tests

    [Fact]
    public void MapToFhirEligibilityRequest_ValidRequest_ReturnsFhirRequest()
    {
        // Arrange
        var request = new CoverageEligibilityRequest
        {
            Id = "req-123",
            RequestId = "REQ-123456",
            Priority = "normal",
            ServiceDate = new DateTime(2024, 6, 15)
        };

        // Act
        var fhirRequest = _mapper.MapToFhirEligibilityRequest(
            request,
          "Patient/pat-123",
    "Coverage/cov-123",
          "Organization/org-prov",
            "Organization/org-payer"
   );

        // Assert
        Assert.NotNull(fhirRequest);
        Assert.Equal("req-123", fhirRequest.Id);
        Assert.Equal(FinancialResourceStatusCodes.Active, fhirRequest.Status);
        Assert.Equal("Patient/pat-123", fhirRequest.Patient.Reference);
        Assert.Equal("Coverage/cov-123", fhirRequest.Insurance[0].Coverage.Reference);
        Assert.Equal("Organization/org-prov", fhirRequest.Provider.Reference);
        Assert.Equal("Organization/org-payer", fhirRequest.Insurer.Reference);
        Assert.Contains(fhirRequest.Purpose, p => p == CoverageEligibilityRequest.EligibilityRequestPurpose.Discovery);
        Assert.Contains(fhirRequest.Purpose, p => p == CoverageEligibilityRequest.EligibilityRequestPurpose.Benefits);
    }

    #endregion

    #region Claim Tests

    [Fact]
    public void MapToFhirClaim_ValidClaim_ReturnsFhirClaim()
    {
        // Arrange
        var claim = new Claim
        {
            Id = "claim-123",
            ClaimNumber = "CLM-123456",
            ClaimType = "institutional",
            ClaimSubType = "ip",
            Use = "claim",
            Priority = "normal",
            Total = 5000.00m
        };

        // Act
        var fhirClaim = _mapper.MapToFhirClaim(
               claim,
               "Patient/pat-123",
          "Organization/org-prov",
               "Organization/org-payer",
         "Coverage/cov-123"
        );

        // Assert
        Assert.NotNull(fhirClaim);
        Assert.Equal("claim-123", fhirClaim.Id);
        Assert.Equal(FinancialResourceStatusCodes.Active, fhirClaim.Status);
        Assert.Equal("institutional", fhirClaim.Type.Coding[0].Code);
        Assert.Equal(ClaimUseCode.Claim, fhirClaim.Use);
        Assert.Equal(5000.00m, fhirClaim.Total.Value);
        Assert.Equal(Money.Currencies.SAR, fhirClaim.Total.Currency);
    }

    [Fact]
    public void MapToFhirClaim_PreAuthClaim_UsesPreauthorization()
    {
        // Arrange
        var claim = new Claim
        {
            Id = "preauth-123",
            ClaimNumber = "PA-123456",
            ClaimType = "professional",
            Use = "preauthorization",
            Total = 3000.00m
        };

        // Act
        var fhirClaim = _mapper.MapToFhirClaim(
            claim,
            "Patient/pat-123",
    "Organization/org-prov",
    "Organization/org-payer",
"Coverage/cov-123"
        );

        // Assert
        Assert.Equal(ClaimUseCode.Preauthorization, fhirClaim.Use);
    }

    #endregion

    #region Extension and Identifier Tests

    [Fact]
    public void CreateNphiesExtension_ValidData_ReturnsExtension()
    {
        // Arrange
        var value = new FhirString("test-value");

        // Act
        var extension = _mapper.CreateNphiesExtension("http://nphies.sa/fhir/StructureDefinition/extension-test", value);

        // Assert
        Assert.NotNull(extension);
        Assert.Equal("http://nphies.sa/fhir/StructureDefinition/extension-test", extension.Url);
        Assert.IsType<FhirString>(extension.Value);
    }

    [Fact]
    public void CreateNphiesIdentifier_ValidData_ReturnsIdentifier()
    {
        // Arrange & Act
        var identifier = _mapper.CreateNphiesIdentifier("http://nphies.sa/identifier/test", "TEST-123");

        // Assert
        Assert.NotNull(identifier);
        Assert.Equal("http://nphies.sa/identifier/test", identifier.System);
        Assert.Equal("TEST-123", identifier.Value);
    }

    #endregion
}
