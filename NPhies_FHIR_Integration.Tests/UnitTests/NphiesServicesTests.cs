using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Infrastructure.Persistence;

namespace NPhies_FHIR_Integration.Tests.UnitTests
{
    /// <summary>
    /// Unit Tests for NPHIES Message Service
    /// </summary>
    public class NphiesMessageServiceTests : IAsyncLifetime
    {
      private Mock<IRepository<NphiesMessageHeaderEntity>> _messageHeaderRepoMock;
        private Mock<IRepository<NphiesBundleEntity>> _bundleRepoMock;
        private Mock<IRepository<BundleEntryEntity>> _bundleEntryRepoMock;
      private Mock<IRepository<OrganizationEntity>> _organizationRepoMock;
     private Mock<ILogger<NphiesMessageService>> _loggerMock;
        private INphiesMessageService _service;

        public async Task InitializeAsync()
      {
 _messageHeaderRepoMock = new Mock<IRepository<NphiesMessageHeaderEntity>>();
       _bundleRepoMock = new Mock<IRepository<NphiesBundleEntity>>();
         _bundleEntryRepoMock = new Mock<IRepository<BundleEntryEntity>>();
     _organizationRepoMock = new Mock<IRepository<OrganizationEntity>>();
   _loggerMock = new Mock<ILogger<NphiesMessageService>>();

 _service = new NphiesMessageService(
    _messageHeaderRepoMock.Object,
    _bundleRepoMock.Object,
        _bundleEntryRepoMock.Object,
    _organizationRepoMock.Object,
       _loggerMock.Object
            );

  await Task.CompletedTask;
        }

        public async Task DisposeAsync()
    {
 await Task.CompletedTask;
        }

 #region CreateMessageHeader Tests

   [Fact]
        public async Task CreateMessageHeaderAsync_WithValidData_ReturnsMessageHeader()
 {
    // Arrange
   var providerOrgId = 1;
   var provider = new OrganizationEntity { Id = providerOrgId, Name = "Test Hospital" };

        _organizationRepoMock.Setup(x => x.GetByIdAsync(providerOrgId))
          .ReturnsAsync(provider);

         _messageHeaderRepoMock.Setup(x => x.AddAsync(It.IsAny<NphiesMessageHeaderEntity>()))
          .Returns(Task.CompletedTask);

        _messageHeaderRepoMock.Setup(x => x.SaveChangesAsync())
   .ReturnsAsync(1);

        // Act
        var result = await _service.CreateMessageHeaderAsync(
            eventCode: "claim-request",
        primaryResourceType: "Claim",
     primaryResourceId: "123",
     providerOrgId: providerOrgId,
           reason: "Test claim"
);

     // Assert
 Assert.NotNull(result);
         Assert.Equal("claim-request", result.EventCode);
 Assert.Equal("Claim", result.PrimaryResourceType);
            Assert.Equal("123", result.PrimaryResourceId);
       Assert.Equal("NPHIES", result.PayerIdentifier);
       Assert.NotNull(result.MessageId);
            Assert.StartsWith("NPHIES-", result.MessageId);

      // Verify repository calls
            _organizationRepoMock.Verify(x => x.GetByIdAsync(providerOrgId), Times.Once);
        _messageHeaderRepoMock.Verify(x => x.AddAsync(It.IsAny<NphiesMessageHeaderEntity>()), Times.Once);
            _messageHeaderRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateMessageHeaderAsync_WithInvalidProviderId_ThrowsException()
        {
            // Arrange
    var providerOrgId = 999;

            _organizationRepoMock.Setup(x => x.GetByIdAsync(providerOrgId))
       .ReturnsAsync((OrganizationEntity)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
         () => _service.CreateMessageHeaderAsync(
   eventCode: "claim-request",
  primaryResourceType: "Claim",
         primaryResourceId: "123",
      providerOrgId: providerOrgId
          )
   );

      Assert.Contains("Provider organization", ex.Message);
        }

        #endregion

   #region CreateBundle Tests

        [Fact]
        public async Task CreateBundleAsync_WithMultipleResources_CreatesBundleWithAllEntries()
        {
            // Arrange
            var resources = new List<object>
       {
    new { Id = 1, Type = "MessageHeader" },
     new { Id = 2, Type = "Claim" },
                new { Id = 3, Type = "Patient" }
            };

   var bundle = new NphiesBundleEntity
         {
       Id = 1,
      BundleId = "BUNDLE-001",
     BundleType = "message",
     TotalEntries = 3,
    Entries = new List<BundleEntryEntity>()
  };

          _bundleRepoMock.Setup(x => x.AddAsync(It.IsAny<NphiesBundleEntity>()))
         .Callback((NphiesBundleEntity b) =>
      {
  b.Id = 1;
          bundle.Id = b.Id;
    })
     .Returns(Task.CompletedTask);

         _bundleRepoMock.Setup(x => x.SaveChangesAsync())
         .ReturnsAsync(1);

      // Act
            var result = await _service.CreateBundleAsync(resources, "claim-request");

       // Assert
            Assert.NotNull(result);
            Assert.Equal("message", result.BundleType);
            Assert.Equal("received", result.ProcessingStatus);

  // Verify repository calls
            _bundleRepoMock.Verify(x => x.AddAsync(It.IsAny<NphiesBundleEntity>()), Times.Once);
  _bundleRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
   }

      [Fact]
   public async Task CreateBundleAsync_WithEmptyResources_CreatesBundleWithZeroEntries()
        {
       // Arrange
            var resources = new List<object>();

         _bundleRepoMock.Setup(x => x.AddAsync(It.IsAny<NphiesBundleEntity>()))
            .Returns(Task.CompletedTask);

  _bundleRepoMock.Setup(x => x.SaveChangesAsync())
           .ReturnsAsync(1);

      // Act
     var result = await _service.CreateBundleAsync(resources, "claim-request");

       // Assert
  Assert.NotNull(result);
            Assert.Equal(0, result.TotalEntries);
        }

#endregion

        #region UpdateMessageStatus Tests

        [Fact]
        public async Task UpdateMessageStatusAsync_WithValidMessageId_UpdatesStatus()
  {
            // Arrange
            var messageId = 1;
            var message = new NphiesMessageHeaderEntity
            {
         Id = messageId,
        MessageId = "NPHIES-001",
            ProcessingStatus = "received"
      };

       _messageHeaderRepoMock.Setup(x => x.GetByIdAsync(messageId))
        .ReturnsAsync(message);

            _messageHeaderRepoMock.Setup(x => x.UpdateAsync(It.IsAny<NphiesMessageHeaderEntity>()))
     .Returns(Task.CompletedTask);

            _messageHeaderRepoMock.Setup(x => x.SaveChangesAsync())
    .ReturnsAsync(1);

            // Act
       await _service.UpdateMessageStatusAsync(messageId, "validated", "No errors");

            // Assert
         Assert.Equal("validated", message.ProcessingStatus);
         Assert.Equal("No errors", message.ProcessingResult);

   // Verify repository calls
   _messageHeaderRepoMock.Verify(x => x.GetByIdAsync(messageId), Times.Once);
   _messageHeaderRepoMock.Verify(x => x.UpdateAsync(It.IsAny<NphiesMessageHeaderEntity>()), Times.Once);
       _messageHeaderRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }

    /// <summary>
 /// Unit Tests for NPHIES Validation Engine
    /// </summary>
    public class NphiesValidationEngineTests
    {
        private Mock<ICodeableConceptService> _conceptServiceMock;
  private Mock<IRepository<CoverageEntity>> _coverageRepoMock;
      private Mock<IRepository<PatientEntity>> _patientRepoMock;
        private Mock<ILogger<NphiesValidationEngine>> _loggerMock;
        private INphiesValidationEngine _engine;

        public NphiesValidationEngineTests()
        {
        _conceptServiceMock = new Mock<ICodeableConceptService>();
  _coverageRepoMock = new Mock<IRepository<CoverageEntity>>();
          _patientRepoMock = new Mock<IRepository<PatientEntity>>();
            _loggerMock = new Mock<ILogger<NphiesValidationEngine>>();

            _engine = new NphiesValidationEngine(
    _conceptServiceMock.Object,
    _coverageRepoMock.Object,
       _patientRepoMock.Object,
           _loggerMock.Object
       );
     }

      #region Claim Validation Tests

        [Fact]
        public async Task ValidateClaimAsync_WithValidClaim_ReturnsValid()
  {
// Arrange
         var claim = new ClaimEntity
        {
                Id = 1,
            Type = "institutional",
         SubType = "ip",
    PatientId = 1,
                InsurerId = 1,
  ServiceDate = DateTime.UtcNow.AddDays(-1),
   BilledDate = DateTime.UtcNow,
          Total = 1000,
                Items = new List<ClaimItemEntity>
        {
 new ClaimItemEntity
                {
    Id = 1,
 Sequence = 1,
          ProductOrService = "50000",
     UnitPrice = 1000,
      Quantity = 1
             }
           }
    };

         _conceptServiceMock.Setup(x => x.ValidateCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
         .ReturnsAsync(new CodeValidationResult { IsValid = true });

            _coverageRepoMock.Setup(x => x.FindAsync(It.IsAny<Func<CoverageEntity, bool>>()))
   .ReturnsAsync(new List<CoverageEntity>
   {
         new CoverageEntity { Id = 1, Status = "active" }
                });

         // Act
       var result = await _engine.ValidateClaimAsync(claim);

  // Assert
     Assert.True(result.IsValid);
  Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateClaimAsync_WithNullClaim_ReturnsInvalid()
   {
     // Act
     var result = await _engine.ValidateClaimAsync(null);

   // Assert
         Assert.False(result.IsValid);
    Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task ValidateClaimAsync_WithMissingItems_ReturnsInvalid()
        {
     // Arrange
        var claim = new ClaimEntity
        {
                Id = 1,
    Type = "institutional",
            PatientId = 1,
     InsurerId = 1,
                ServiceDate = DateTime.UtcNow,
       BilledDate = DateTime.UtcNow,
                Total = 1000,
        Items = new List<ClaimItemEntity>()
      };

   // Act
        var result = await _engine.ValidateClaimAsync(claim);

            // Assert
       Assert.False(result.IsValid);
     var error = result.Errors.FirstOrDefault(e => e.Code == "CLAIM-ITEMS-001");
            Assert.NotNull(error);
        }

        [Fact]
        public async Task ValidateClaimAsync_WithInvalidDates_ReturnsInvalid()
   {
            // Arrange
            var claim = new ClaimEntity
            {
      Id = 1,
  Type = "institutional",
    PatientId = 1,
    InsurerId = 1,
         ServiceDate = DateTime.UtcNow.AddDays(1), // Future date
         BilledDate = DateTime.UtcNow,
  Total = 1000,
              Items = new List<ClaimItemEntity>
      {
       new ClaimItemEntity
 {
         Id = 1,
    Sequence = 1,
            ProductOrService = "50000",
      UnitPrice = 1000,
         Quantity = 1
   }
       }
};

            // Act
        var result = await _engine.ValidateClaimAsync(claim);

        // Assert
            Assert.False(result.IsValid);
    var error = result.Errors.FirstOrDefault(e => e.Code == "CLAIM-DATE-001");
            Assert.NotNull(error);
        }

    #endregion

        #region Message Validation Tests

        [Fact]
        public async Task ValidateMessageAsync_WithValidMessage_ReturnsValid()
  {
     // Arrange
          var message = new NphiesMessageHeaderEntity
            {
      Id = 1,
    MessageId = "NPHIES-001",
                EventCode = "claim-request"
            };

  // Act
 var result = await _engine.ValidateMessageAsync(message);

   // Assert
  Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
     public async Task ValidateMessageAsync_WithNullMessage_ReturnsInvalid()
        {
    // Act
            var result = await _engine.ValidateMessageAsync(null);

    // Assert
          Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        #endregion

        #region Coverage Validation Tests

 [Fact]
        public async Task ValidateCoverageAsync_WithActiveCoverage_ReturnsValid()
        {
            // Arrange
         var coverage = new CoverageEntity
  {
          Id = 1,
     Status = "active",
   Period = new Domain.ValueObjects.Period
          {
        Start = DateTime.UtcNow.AddDays(-30),
         End = DateTime.UtcNow.AddDays(30)
       }
     };

          // Act
            var result = await _engine.ValidateCoverageAsync(coverage);

   // Assert
            Assert.True(result.IsValid);
        }

    [Fact]
        public async Task ValidateCoverageAsync_WithInactiveCoverage_ReturnsInvalid()
  {
            // Arrange
      var coverage = new CoverageEntity
       {
          Id = 1,
            Status = "inactive"
      };

  // Act
            var result = await _engine.ValidateCoverageAsync(coverage);

            // Assert
      Assert.False(result.IsValid);
   var error = result.Errors.FirstOrDefault(e => e.Code == "COVERAGE-STATUS");
            Assert.NotNull(error);
        }

        #endregion
    }
}
