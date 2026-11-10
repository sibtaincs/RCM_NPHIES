using AutoMapper;
using Moq;
using NPhies_FHIR_Integration.Application.Mapping;
using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Domain.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Repositories;
using Xunit;

namespace NPhies_FHIR_Integration.Tests.Application;

/// <summary>
/// Unit tests for EligibilityService
/// </summary>
public class EligibilityServiceTests
{
    private IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<EligibilityMappingProfile>();
        });
        return config.CreateMapper();
    }

  [Fact]
    public async Task SubmitEligibilityRequestAsync_ShouldThrowWhenPatientIdEmpty()
 {
        // Arrange
     var mockRequestRepo = new Mock<ICoverageEligibilityRequestRepository>();
        var mockResponseRepo = new Mock<ICoverageEligibilityResponseRepository>();
 var mockPatientRepo = new Mock<IPatientRepository>();
var mockCoverageRepo = new Mock<ICoverageRepository>();
        var mockOrgRepo = new Mock<IOrganizationRepository>();
   var mapper = CreateMapper();
  var mockFhirMapper = new Mock<IFhirToEntityMapper>();

var service = new EligibilityService(
            mockRequestRepo.Object,
      mockResponseRepo.Object,
          mockPatientRepo.Object,
            mockCoverageRepo.Object,
       mockOrgRepo.Object,
      mapper,
    mockFhirMapper.Object
        );

        var request = new CoverageEligibilityRequestDto { PatientId = "", CoverageId = "cov-1" };

  // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SubmitEligibilityRequestAsync(request)
        );
   Assert.Contains("Patient ID is required", exception.Message);
}

    [Fact]
 public async Task SubmitEligibilityRequestAsync_ShouldThrowWhenCoverageIdEmpty()
    {
        // Arrange
        var mockRequestRepo = new Mock<ICoverageEligibilityRequestRepository>();
        var mockResponseRepo = new Mock<ICoverageEligibilityResponseRepository>();
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockCoverageRepo = new Mock<ICoverageRepository>();
        var mockOrgRepo = new Mock<IOrganizationRepository>();
        var mapper = CreateMapper();
var mockFhirMapper = new Mock<IFhirToEntityMapper>();

  var service = new EligibilityService(
      mockRequestRepo.Object,
        mockResponseRepo.Object,
  mockPatientRepo.Object,
            mockCoverageRepo.Object,
            mockOrgRepo.Object,
      mapper,
   mockFhirMapper.Object
    );

        var request = new CoverageEligibilityRequestDto { PatientId = "pat-1", CoverageId = "" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
        () => service.SubmitEligibilityRequestAsync(request)
        );
        Assert.Contains("Coverage ID is required", exception.Message);
    }

    [Fact]
    public async Task SubmitEligibilityRequestAsync_ShouldReturnDtoWithGeneratedId()
    {
        // Arrange
        var mockRequestRepo = new Mock<ICoverageEligibilityRequestRepository>();
        var mockResponseRepo = new Mock<ICoverageEligibilityResponseRepository>();
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockCoverageRepo = new Mock<ICoverageRepository>();
        var mockOrgRepo = new Mock<IOrganizationRepository>();
        var mapper = CreateMapper();
        var mockFhirMapper = new Mock<IFhirToEntityMapper>();

        mockRequestRepo.Setup(r => r.AddAsync(It.IsAny<CoverageEligibilityRequest>()))
       .ReturnsAsync((CoverageEligibilityRequest r) => r);
        mockRequestRepo.Setup(r => r.SaveChangesAsync())
   .ReturnsAsync(1);

        var service = new EligibilityService(
    mockRequestRepo.Object,
            mockResponseRepo.Object,
          mockPatientRepo.Object,
            mockCoverageRepo.Object,
            mockOrgRepo.Object,
            mapper,
  mockFhirMapper.Object
        );

        var request = new CoverageEligibilityRequestDto 
        { 
     PatientId = "pat-1", 
            CoverageId = "cov-1",
     ServiceType = "medical",
    Status = "active"
        };

  // Act
        var result = await service.SubmitEligibilityRequestAsync(request);

        // Assert
        Assert.NotNull(result);
     Assert.NotEmpty(result.MessageUUID);
        Assert.NotEmpty(result.RequestId);
        Assert.Equal("active", result.Status);
        mockRequestRepo.Verify(r => r.AddAsync(It.IsAny<CoverageEligibilityRequest>()), Times.Once);
        mockRequestRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetEligibilityRequestAsync_ShouldReturnRequestDto()
    {
        // Arrange
        var requestId = "req-123";
  var mockRequest = new CoverageEligibilityRequest
        {
            Id = requestId,
  RequestId = "REQ-001",
       Status = "active",
   PatientId = "pat-1"
        };

  var mockRequestRepo = new Mock<ICoverageEligibilityRequestRepository>();
    var mockResponseRepo = new Mock<ICoverageEligibilityResponseRepository>();
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockCoverageRepo = new Mock<ICoverageRepository>();
  var mockOrgRepo = new Mock<IOrganizationRepository>();
        var mapper = CreateMapper();
        var mockFhirMapper = new Mock<IFhirToEntityMapper>();

        mockRequestRepo.Setup(r => r.GetWithDetailsAsync(requestId))
         .ReturnsAsync(mockRequest);

      var service = new EligibilityService(
 mockRequestRepo.Object,
   mockResponseRepo.Object,
        mockPatientRepo.Object,
            mockCoverageRepo.Object,
     mockOrgRepo.Object,
   mapper,
            mockFhirMapper.Object
        );

        // Act
        var result = await service.GetEligibilityRequestAsync(requestId);

        // Assert
   Assert.NotNull(result);
 Assert.Equal("pat-1", result.PatientId);
      Assert.Equal("active", result.Status);
    mockRequestRepo.Verify(r => r.GetWithDetailsAsync(requestId), Times.Once);
    }

    [Fact]
    public async Task GetEligibilityRequestAsync_ShouldReturnNullWhenNotFound()
    {
    // Arrange
        var requestId = "non-existent";
        var mockRequestRepo = new Mock<ICoverageEligibilityRequestRepository>();
        var mockResponseRepo = new Mock<ICoverageEligibilityResponseRepository>();
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockCoverageRepo = new Mock<ICoverageRepository>();
var mockOrgRepo = new Mock<IOrganizationRepository>();
        var mapper = CreateMapper();
        var mockFhirMapper = new Mock<IFhirToEntityMapper>();

      mockRequestRepo.Setup(r => r.GetWithDetailsAsync(requestId))
  .ReturnsAsync((CoverageEligibilityRequest)null!);

      var service = new EligibilityService(
            mockRequestRepo.Object,
            mockResponseRepo.Object,
mockPatientRepo.Object,
        mockCoverageRepo.Object,
         mockOrgRepo.Object,
      mapper,
   mockFhirMapper.Object
        );

// Act
        var result = await service.GetEligibilityRequestAsync(requestId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_ShouldReturnCollectionOfDtos()
    {
   // Arrange
        var mockRequests = new List<CoverageEligibilityRequest>
  {
      new CoverageEligibilityRequest { Id = "req-1", RequestId = "REQ-001", Status = "submitted" },
       new CoverageEligibilityRequest { Id = "req-2", RequestId = "REQ-002", Status = "acknowledged" }
   };

        var mockRequestRepo = new Mock<ICoverageEligibilityRequestRepository>();
        var mockResponseRepo = new Mock<ICoverageEligibilityResponseRepository>();
        var mockPatientRepo = new Mock<IPatientRepository>();
var mockCoverageRepo = new Mock<ICoverageRepository>();
        var mockOrgRepo = new Mock<IOrganizationRepository>();
        var mapper = CreateMapper();
      var mockFhirMapper = new Mock<IFhirToEntityMapper>();

 mockRequestRepo.Setup(r => r.GetPendingRequestsAsync())
.ReturnsAsync(mockRequests);

        var service = new EligibilityService(
            mockRequestRepo.Object,
         mockResponseRepo.Object,
        mockPatientRepo.Object,
      mockCoverageRepo.Object,
            mockOrgRepo.Object,
            mapper,
            mockFhirMapper.Object
      );

        // Act
 var result = await service.GetPendingRequestsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task CheckCoverageEligibilityAsync_ShouldThrowWhenPatientNotFound()
    {
        // Arrange
   var patientId = "non-existent";
   var mockRequestRepo = new Mock<ICoverageEligibilityRequestRepository>();
        var mockResponseRepo = new Mock<ICoverageEligibilityResponseRepository>();
        var mockPatientRepo = new Mock<IPatientRepository>();
     var mockCoverageRepo = new Mock<ICoverageRepository>();
        var mockOrgRepo = new Mock<IOrganizationRepository>();
        var mapper = CreateMapper();
        var mockFhirMapper = new Mock<IFhirToEntityMapper>();

        mockPatientRepo.Setup(p => p.GetByIdAsync(patientId))
       .ReturnsAsync((Patient)null!);

        var service = new EligibilityService(
        mockRequestRepo.Object,
  mockResponseRepo.Object,
    mockPatientRepo.Object,
     mockCoverageRepo.Object,
         mockOrgRepo.Object,
          mapper,
 mockFhirMapper.Object
        );

      // Act & Assert
     var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
  () => service.CheckCoverageEligibilityAsync(patientId, "cov-1", "medical")
        );
        Assert.Contains("Patient with ID", exception.Message);
    }

[Fact]
    public async Task CheckCoverageEligibilityAsync_ShouldThrowWhenCoverageNotFound()
    {
        // Arrange
var patientId = "pat-1";
        var coverageId = "non-existent";
        var mockPatient = new Patient { Id = patientId };

      var mockRequestRepo = new Mock<ICoverageEligibilityRequestRepository>();
   var mockResponseRepo = new Mock<ICoverageEligibilityResponseRepository>();
 var mockPatientRepo = new Mock<IPatientRepository>();
  var mockCoverageRepo = new Mock<ICoverageRepository>();
      var mockOrgRepo = new Mock<IOrganizationRepository>();
        var mapper = CreateMapper();
        var mockFhirMapper = new Mock<IFhirToEntityMapper>();

        mockPatientRepo.Setup(p => p.GetByIdAsync(patientId))
     .ReturnsAsync(mockPatient);
        mockCoverageRepo.Setup(c => c.GetByIdAsync(coverageId))
 .ReturnsAsync((Coverage)null!);

        var service = new EligibilityService(
            mockRequestRepo.Object,
mockResponseRepo.Object,
            mockPatientRepo.Object,
   mockCoverageRepo.Object,
     mockOrgRepo.Object,
   mapper,
            mockFhirMapper.Object
     );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
      () => service.CheckCoverageEligibilityAsync(patientId, coverageId, "medical")
        );
        Assert.Contains("Coverage with ID", exception.Message);
    }
}
