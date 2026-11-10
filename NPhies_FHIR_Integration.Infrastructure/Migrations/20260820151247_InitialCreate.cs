using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenefitCodeMaster",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BenefitCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BenefitName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BenefitCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitCodeMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeSystem",
                columns: table => new
                {
                    CodeSystemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Committee = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Oid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Copyright = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceResource = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CaseSensitive = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSystem", x => x.CodeSystemId);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosisCodeMaster",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiagnosisCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiagnosisName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DiagnosisCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiagnosisType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsOnAdmission = table.Column<bool>(type: "bit", nullable: false),
                    NphiesDiagnosisCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsNphiesMapped = table.Column<bool>(type: "bit", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequiresDocumentation = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosisCodeMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorCodeMasters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ErrorCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRecoverable = table.Column<bool>(type: "bit", nullable: false),
                    AllowsAppeal = table.Column<bool>(type: "bit", nullable: false),
                    StandardAppealDays = table.Column<int>(type: "int", nullable: false),
                    RecommendedAction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NphiesCodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AdjudicationImpact = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorCodeMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalDeviceCodeMaster",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NphiesDeviceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsNphiesMapped = table.Column<bool>(type: "bit", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsImplantable = table.Column<bool>(type: "bit", nullable: false),
                    IsReusable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalDeviceCodeMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationCodeMaster",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MedicationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MedicationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ActiveIngredient = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Form = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NphiesMedicationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsNphiesMapped = table.Column<bool>(type: "bit", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsControlledSubstance = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationCodeMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModifierCodeMaster",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifierCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ModifierName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ModifierDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModifierType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ImpactOnCharges = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChargePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NphiesModifierCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModifierCodeMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NphiesCodeMapping",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocalCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LocalCodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LocalDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NphiesCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NphiesCodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NphiesDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsMappingValid = table.Column<bool>(type: "bit", nullable: false),
                    MappingValidationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NphiesCodeMapping", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NphiesMessageType",
                columns: table => new
                {
                    NphiesMessageTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MessageTypeArabic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FhirResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NphiesMessageType", x => x.NphiesMessageTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrganizationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LicenseSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LicenseTypeSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LicenseUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FhirId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderTypeSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecializationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddressText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressCountry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPhoneUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MOHLicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CHINumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NphiesOrganizationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NphiesProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NphiesPayerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaxRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MRN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NationalIdSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NationalIdType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    MaritalStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccupationSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deceased = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PhoneSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayerMaster",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PayerNameArabic = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PayerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LicenseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NphiesPayerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NphiesConnectionStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NphiesApiEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsNphiesMember = table.Column<bool>(type: "bit", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ContractStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContractEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsNphiesIntegrated = table.Column<bool>(type: "bit", nullable: false),
                    SupportedClaimTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupportedEligibilityTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentCycle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AverageTurnaroundDays = table.Column<int>(type: "int", nullable: true),
                    MaxClaimsPerDay = table.Column<int>(type: "int", nullable: false),
                    MaxClaimAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayerMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCodeMaster",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ServiceDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ServiceCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NphiesServiceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NphiesServiceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NphiesCategoryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsNphiesMapped = table.Column<bool>(type: "bit", nullable: false),
                    MappingValidationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsRequiresAuthorization = table.Column<bool>(type: "bit", nullable: false),
                    DefaultAuthorizationDays = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCodeMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Roles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    IsMfaEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MfaSecret = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedUntilAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrganizationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DepartmentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValueSet",
                columns: table => new
                {
                    ValueSetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Committee = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Oid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Copyright = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceResource = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Restrictions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueSet", x => x.ValueSetId);
                });

            migrationBuilder.CreateTable(
                name: "Concept",
                columns: table => new
                {
                    ConceptId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeSystemId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Display = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayArabic = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DefinitionArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    ParentCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CodeSystemEntityCodeSystemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concept", x => x.ConceptId);
                    table.ForeignKey(
                        name: "FK_Concept_CodeSystem_CodeSystemEntityCodeSystemId",
                        column: x => x.CodeSystemEntityCodeSystemId,
                        principalTable: "CodeSystem",
                        principalColumn: "CodeSystemId");
                    table.ForeignKey(
                        name: "FK_Concept_CodeSystem_CodeSystemId",
                        column: x => x.CodeSystemId,
                        principalTable: "CodeSystem",
                        principalColumn: "CodeSystemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CancellationRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaskId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Intent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FocusResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FocusIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FocusIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReasonCodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReasonText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AuthoredOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequesterId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FhirTaskJson = table.Column<string>(type: "ntext", nullable: true),
                    MessageHeaderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancellationRequests_Organizations_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CancellationRequests_Organizations_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicMaster",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClinicName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ClinicCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClinicType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SpecializedServices = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NumberOfBeds = table.Column<int>(type: "int", nullable: true),
                    NumberOfDoctors = table.Column<int>(type: "int", nullable: true),
                    NumberOfNurses = table.Column<int>(type: "int", nullable: true),
                    IsCertifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AccreditationLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkingHoursFrom = table.Column<TimeOnly>(type: "time", nullable: true),
                    WorkingHoursTo = table.Column<TimeOnly>(type: "time", nullable: true),
                    IsEmergencyAvailable = table.Column<bool>(type: "bit", nullable: false),
                    PharmacyAvailable = table.Column<bool>(type: "bit", nullable: false),
                    LabAvailable = table.Column<bool>(type: "bit", nullable: false),
                    ImagingAvailable = table.Column<bool>(type: "bit", nullable: false),
                    AcceptsCashPayment = table.Column<bool>(type: "bit", nullable: false),
                    AcceptsInsurance = table.Column<bool>(type: "bit", nullable: false),
                    AcceptsCardPayment = table.Column<bool>(type: "bit", nullable: false),
                    AvailableBeds = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicMaster_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LocationLicense = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LicenseSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    FacilityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FacilityTypeDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageHeaders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MessageUUID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SenderOrganizationId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    SenderLicenseSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderOrganizationLicense = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DestinationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DestinationEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DestinationOrganizationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DestinationLicenseSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FocusResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FocusResourceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SourceEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MessageTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponseTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResponseStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResponseIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BundleContent = table.Column<string>(type: "ntext", nullable: false),
                    ResponseBundleContent = table.Column<string>(type: "ntext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageHeaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageHeaders_Organizations_SenderOrganizationId",
                        column: x => x.SenderOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentNotices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PaymentNoticeId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentStatusSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PayeeId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecipientSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecipientValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FhirPaymentNoticeJson = table.Column<string>(type: "ntext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentNotices_Organizations_PayeeId",
                        column: x => x.PayeeId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentNotices_Organizations_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReconciliations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PaymentReconciliationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdentifierSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdentifierValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Disposition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentMethodType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethodSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentIdentifierSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentIdentifierValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentIssuerId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    RequestorId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    FhirPaymentReconciliationJson = table.Column<string>(type: "ntext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReconciliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentReconciliations_Organizations_PaymentIssuerId",
                        column: x => x.PaymentIssuerId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentReconciliations_Organizations_RequestorId",
                        column: x => x.RequestorId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Practitioners",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LicenseSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Qualification = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PractitionerLicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LicenseIssuingAuthority = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LicenseExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NationalIdentificationNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PractitionerRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PractitionerRoleSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Practitioners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Practitioners_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CommunicationRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CategorySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubjectPatientId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    AboutResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AboutIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AboutIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PayloadContent = table.Column<string>(type: "ntext", nullable: true),
                    RecipientId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    SenderId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    FhirCommunicationRequestJson = table.Column<string>(type: "ntext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationRequests_Organizations_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationRequests_Organizations_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationRequests_Patients_SubjectPatientId",
                        column: x => x.SubjectPatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Communications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CommunicationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BasedOnResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BasedOnIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BasedOnIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CategorySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubjectPatientId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    AboutResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AboutIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AboutIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PayloadContent = table.Column<string>(type: "ntext", nullable: true),
                    RecipientId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    SenderId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    PayloadAttachmentContentType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PayloadAttachmentData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PayloadAttachmentTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PayloadAttachmentCreation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FhirCommunicationJson = table.Column<string>(type: "ntext", nullable: true),
                    MessageHeaderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Communications_Organizations_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Communications_Organizations_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Communications_Patients_SubjectPatientId",
                        column: x => x.SubjectPatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Coverages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MemberID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CoverageIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CoverageIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CoverageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CoverageTypeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PlanCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PlanCodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CoverageClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubscriberRelationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubscriberId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubscriberPatientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Dependent = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RelationToSubscriber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PolicyHolderOrganizationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubscriberMRN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subrogation = table.Column<bool>(type: "bit", nullable: false),
                    MaxCopay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxCopayCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    CoinsurancePercentValue = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CoverageClassType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CoverageClassValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PatientId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CoverageStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CoverageEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnnualDeductible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeductibleMet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Copay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CoinsurancePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OutOfPocketMax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coverages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coverages_Organizations_InsurerId",
                        column: x => x.InsurerId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Coverages_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Coverages_Patients_SubscriberPatientId",
                        column: x => x.SubscriberPatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Encounters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EncounterId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Class = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServiceTypeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PatientId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdmitSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AdmitSourceSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServiceEventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServiceEventTypeSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntendedLengthOfStay = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IntendedLengthOfStaySystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceProviderId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    FhirEncounterJson = table.Column<string>(type: "ntext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Encounters_Organizations_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Encounters_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayerPolicyMaster",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayerMasterId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PolicyCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PolicyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PolicyNameArabic = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PolicyDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PolicyType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CoverageType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CoverageLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NetworkType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AnnualPremium = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PremiumAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PremiumFrequency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AnnualDeductible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxOutOfPocket = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OutOfPocketMax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Copay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CopaymentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CoinsurancePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CoverageLimitPerVisit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CoverageLimitPerYear = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PreAuthRequiredForAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RequiresPriorAuth = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PolicyStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PolicyEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPolicyActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayerPolicyMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayerPolicyMaster_PayerMaster_PayerMasterId",
                        column: x => x.PayerMasterId,
                        principalTable: "PayerMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValues = table.Column<string>(type: "ntext", nullable: true),
                    NewValues = table.Column<string>(type: "ntext", nullable: true),
                    ChangeDetails = table.Column<string>(type: "ntext", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuditLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    DurationMs = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMs = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RateLimitLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    MaxRequests = table.Column<int>(type: "int", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRateLimited = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResetAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RateLimitLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConceptCodeFilter",
                columns: table => new
                {
                    ConceptCodeFilterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ValueSetId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Display = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValueSetEntityValueSetId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptCodeFilter", x => x.ConceptCodeFilterId);
                    table.ForeignKey(
                        name: "FK_ConceptCodeFilter_ValueSet_ValueSetEntityValueSetId",
                        column: x => x.ValueSetEntityValueSetId,
                        principalTable: "ValueSet",
                        principalColumn: "ValueSetId");
                    table.ForeignKey(
                        name: "FK_ConceptCodeFilter_ValueSet_ValueSetId",
                        column: x => x.ValueSetId,
                        principalTable: "ValueSet",
                        principalColumn: "ValueSetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NphiesMessageRequiredElement",
                columns: table => new
                {
                    NphiesMessageRequiredElementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NphiesMessageTypeId = table.Column<int>(type: "int", nullable: false),
                    ElementPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ValueSetId = table.Column<int>(type: "int", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Cardinality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NphiesMessageTypeEntityNphiesMessageTypeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NphiesMessageRequiredElement", x => x.NphiesMessageRequiredElementId);
                    table.ForeignKey(
                        name: "FK_NphiesMessageRequiredElement_NphiesMessageType_NphiesMessageTypeEntityNphiesMessageTypeId",
                        column: x => x.NphiesMessageTypeEntityNphiesMessageTypeId,
                        principalTable: "NphiesMessageType",
                        principalColumn: "NphiesMessageTypeId");
                    table.ForeignKey(
                        name: "FK_NphiesMessageRequiredElement_NphiesMessageType_NphiesMessageTypeId",
                        column: x => x.NphiesMessageTypeId,
                        principalTable: "NphiesMessageType",
                        principalColumn: "NphiesMessageTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NphiesMessageRequiredElement_ValueSet_ValueSetId",
                        column: x => x.ValueSetId,
                        principalTable: "ValueSet",
                        principalColumn: "ValueSetId");
                });

            migrationBuilder.CreateTable(
                name: "ProfileElement",
                columns: table => new
                {
                    ProfileElementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    PathArabic = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ElementPath = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValueSetId = table.Column<int>(type: "int", nullable: true),
                    BindingStrength = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValueSetEntityValueSetId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileElement", x => x.ProfileElementId);
                    table.ForeignKey(
                        name: "FK_ProfileElement_ValueSet_ValueSetEntityValueSetId",
                        column: x => x.ValueSetEntityValueSetId,
                        principalTable: "ValueSet",
                        principalColumn: "ValueSetId");
                    table.ForeignKey(
                        name: "FK_ProfileElement_ValueSet_ValueSetId",
                        column: x => x.ValueSetId,
                        principalTable: "ValueSet",
                        principalColumn: "ValueSetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ValidationRule",
                columns: table => new
                {
                    ValidationRuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessageArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValueSetId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRule", x => x.ValidationRuleId);
                    table.ForeignKey(
                        name: "FK_ValidationRule_ValueSet_ValueSetId",
                        column: x => x.ValueSetId,
                        principalTable: "ValueSet",
                        principalColumn: "ValueSetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ValueSetCodeSystemMap",
                columns: table => new
                {
                    ValueSetCodeSystemMapId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ValueSetId = table.Column<int>(type: "int", nullable: false),
                    CodeSystemId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CodeSystemEntityCodeSystemId = table.Column<int>(type: "int", nullable: true),
                    ValueSetEntityValueSetId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueSetCodeSystemMap", x => x.ValueSetCodeSystemMapId);
                    table.ForeignKey(
                        name: "FK_ValueSetCodeSystemMap_CodeSystem_CodeSystemEntityCodeSystemId",
                        column: x => x.CodeSystemEntityCodeSystemId,
                        principalTable: "CodeSystem",
                        principalColumn: "CodeSystemId");
                    table.ForeignKey(
                        name: "FK_ValueSetCodeSystemMap_CodeSystem_CodeSystemId",
                        column: x => x.CodeSystemId,
                        principalTable: "CodeSystem",
                        principalColumn: "CodeSystemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValueSetCodeSystemMap_ValueSet_ValueSetEntityValueSetId",
                        column: x => x.ValueSetEntityValueSetId,
                        principalTable: "ValueSet",
                        principalColumn: "ValueSetId");
                    table.ForeignKey(
                        name: "FK_ValueSetCodeSystemMap_ValueSet_ValueSetId",
                        column: x => x.ValueSetId,
                        principalTable: "ValueSet",
                        principalColumn: "ValueSetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CancellationResponses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaskId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferencedRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CancellationRequestId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Intent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FocusResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FocusIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FocusIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResponseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResponseMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    AuthoredOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequesterId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResultText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FhirTaskJson = table.Column<string>(type: "ntext", nullable: true),
                    MessageHeaderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancellationResponses_CancellationRequests_CancellationRequestId",
                        column: x => x.CancellationRequestId,
                        principalTable: "CancellationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CancellationResponses_Organizations_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CancellationResponses_Organizations_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReconciliationDetails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PaymentReconciliationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DetailType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestIdentifierSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestIdentifierValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseIdentifierSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseIdentifierValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetailDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmitterId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    PayeeId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ComponentPayment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EarlyFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NphiesFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentReconciliationId1 = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReconciliationDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentReconciliationDetails_Organizations_PayeeId",
                        column: x => x.PayeeId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentReconciliationDetails_Organizations_SubmitterId",
                        column: x => x.SubmitterId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentReconciliationDetails_PaymentReconciliations_PaymentReconciliationId",
                        column: x => x.PaymentReconciliationId,
                        principalTable: "PaymentReconciliations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentReconciliationDetails_PaymentReconciliations_PaymentReconciliationId1",
                        column: x => x.PaymentReconciliationId1,
                        principalTable: "PaymentReconciliations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DoctorMaster",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PractitionerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoctorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubSpecialization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QualificationDegree = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UniversityName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: true),
                    IsConsultant = table.Column<bool>(type: "bit", nullable: false),
                    ConsultationFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FollowupFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ClinicMasterId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAvailableForAppointments = table.Column<bool>(type: "bit", nullable: false),
                    AvailableSlotsPerDay = table.Column<int>(type: "int", nullable: true),
                    Board = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BoardLicenseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BoardLicenseExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResearchPapers = table.Column<int>(type: "int", nullable: true),
                    IsTeachingMember = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorMaster_ClinicMaster_ClinicMasterId",
                        column: x => x.ClinicMasterId,
                        principalTable: "ClinicMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorMaster_Practitioners_PractitionerId",
                        column: x => x.PractitionerId,
                        principalTable: "Practitioners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoverageEligibilityRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MessageUUID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestIdentifierSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestIdentifierValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageHeaderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrioritySystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurposeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServicedPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServicedPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CoverageId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    EntererPractitionerId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    RequestCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EligibilityStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MessageStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FhirRequestBundle = table.Column<string>(type: "ntext", nullable: false),
                    FhirResponseBundle = table.Column<string>(type: "ntext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoverageEligibilityRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoverageEligibilityRequests_Coverages_CoverageId",
                        column: x => x.CoverageId,
                        principalTable: "Coverages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoverageEligibilityRequests_MessageHeaders_MessageHeaderId",
                        column: x => x.MessageHeaderId,
                        principalTable: "MessageHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoverageEligibilityRequests_Organizations_InsurerId",
                        column: x => x.InsurerId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoverageEligibilityRequests_Organizations_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoverageEligibilityRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoverageEligibilityRequests_Practitioners_EntererPractitionerId",
                        column: x => x.EntererPractitionerId,
                        principalTable: "Practitioners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreAuthorizationRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestIdentifierSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CoverageId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ServicedPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServicedPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServicedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EnteredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageHeaderId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    FhirRequestBundle = table.Column<string>(type: "ntext", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestedDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientId1 = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ProviderId1 = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreAuthorizationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreAuthorizationRequests_Coverages_CoverageId",
                        column: x => x.CoverageId,
                        principalTable: "Coverages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreAuthorizationRequests_MessageHeaders_MessageHeaderId",
                        column: x => x.MessageHeaderId,
                        principalTable: "MessageHeaders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreAuthorizationRequests_Organizations_InsurerId",
                        column: x => x.InsurerId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreAuthorizationRequests_Organizations_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreAuthorizationRequests_Organizations_ProviderId1",
                        column: x => x.ProviderId1,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreAuthorizationRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreAuthorizationRequests_Patients_PatientId1",
                        column: x => x.PatientId1,
                        principalTable: "Patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClaimIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClaimTypeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClaimSubType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClaimSubTypeSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Use = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServicedPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServicedPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrioritySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PatientId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CoverageId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    PractitionerId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    LocationId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    MessageHeaderId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    EncounterId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    PayeeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PayeeTypeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    FhirClaimBundle = table.Column<string>(type: "ntext", nullable: true),
                    EpisodeIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EpisodeIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EligibilityOfflineReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EligibilityOfflineDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AuthorizationOfflineDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccidentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccidentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccidentTypeSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FundsReserveCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FundsReserveSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReferralIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrescriptionIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OriginalPrescriptionIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PreAuthorizationRef = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BillablePeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillablePeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Claims_Coverages_CoverageId",
                        column: x => x.CoverageId,
                        principalTable: "Coverages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Claims_Encounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "Encounters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Claims_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Claims_MessageHeaders_MessageHeaderId",
                        column: x => x.MessageHeaderId,
                        principalTable: "MessageHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Claims_Organizations_InsurerId",
                        column: x => x.InsurerId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Claims_Organizations_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Claims_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Claims_Practitioners_PractitionerId",
                        column: x => x.PractitionerId,
                        principalTable: "Practitioners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimSubmissionRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayerMasterId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PolicyMasterId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RuleName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RuleCondition = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MaxClaimAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxItemsPerClaim = table.Column<int>(type: "int", nullable: true),
                    RequiresInvoice = table.Column<bool>(type: "bit", nullable: false),
                    RequiresMedicalReport = table.Column<bool>(type: "bit", nullable: false),
                    RequiresPhotos = table.Column<bool>(type: "bit", nullable: false),
                    MaxDaysForSubmission = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimSubmissionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimSubmissionRules_PayerMaster_PayerMasterId",
                        column: x => x.PayerMasterId,
                        principalTable: "PayerMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimSubmissionRules_PayerPolicyMaster_PolicyMasterId",
                        column: x => x.PolicyMasterId,
                        principalTable: "PayerPolicyMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PolicyBenefitCoverage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PolicyMasterId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceCodeMasterId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServiceCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BenefitType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CoveragePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MaxCoverageAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RequiresPreAuth = table.Column<bool>(type: "bit", nullable: false),
                    RequiresReferral = table.Column<bool>(type: "bit", nullable: false),
                    PreAuthValidityDays = table.Column<int>(type: "int", nullable: true),
                    CoverageLimitPerYear = table.Column<int>(type: "int", nullable: true),
                    CoverageLimitPerLifetime = table.Column<int>(type: "int", nullable: true),
                    IsExcluded = table.Column<bool>(type: "bit", nullable: false),
                    ExclusionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsWaitingPeriodApplicable = table.Column<bool>(type: "bit", nullable: false),
                    WaitingPeriodDays = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyBenefitCoverage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyBenefitCoverage_PayerPolicyMaster_PolicyMasterId",
                        column: x => x.PolicyMasterId,
                        principalTable: "PayerPolicyMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PolicyBenefitCoverage_ServiceCodeMaster_ServiceCodeMasterId",
                        column: x => x.ServiceCodeMasterId,
                        principalTable: "ServiceCodeMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PollingRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PollingRecordId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestTaskId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CancellationRequestId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    RequestedMessageTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseTaskId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CancellationResponseId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ResponseStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResponseReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MessagesReceived = table.Column<int>(type: "int", nullable: false),
                    ReceivedMessageTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestBundleJson = table.Column<string>(type: "ntext", nullable: true),
                    ResponseBundleJson = table.Column<string>(type: "ntext", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    CycleStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceIpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestSourceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollingRecords_CancellationRequests_CancellationRequestId",
                        column: x => x.CancellationRequestId,
                        principalTable: "CancellationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollingRecords_CancellationResponses_CancellationResponseId",
                        column: x => x.CancellationResponseId,
                        principalTable: "CancellationResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollingRecords_Organizations_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoctorQualification",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoctorMasterId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QualificationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QualificationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UniversityName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsExpiring = table.Column<bool>(type: "bit", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CertificateNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorQualification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorQualification_DoctorMaster_DoctorMasterId",
                        column: x => x.DoctorMasterId,
                        principalTable: "DoctorMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoverageEligibilityResponses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResponseUUID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestIdentifierSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestIdentifierValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseIdentifierSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseIdentifierValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EligibilityRequestId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    MessageHeaderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ResponsePurpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Disposition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiteEligibility = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiteEligibilitySystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponseReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServicedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BenefitPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BenefitPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EligibilityStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsInForce = table.Column<bool>(type: "bit", nullable: false),
                    ServicedPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServicedPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CoverageId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    IsInNetwork = table.Column<bool>(type: "bit", nullable: false),
                    NetworkStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NetworkName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CoveredServicesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExcludedServicesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LimitationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FhirResponseContent = table.Column<string>(type: "ntext", nullable: false),
                    ExplanationOfBenefits = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoverageEligibilityResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoverageEligibilityResponses_CoverageEligibilityRequests_EligibilityRequestId",
                        column: x => x.EligibilityRequestId,
                        principalTable: "CoverageEligibilityRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoverageEligibilityResponses_Coverages_CoverageId",
                        column: x => x.CoverageId,
                        principalTable: "Coverages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoverageEligibilityResponses_MessageHeaders_MessageHeaderId",
                        column: x => x.MessageHeaderId,
                        principalTable: "MessageHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoverageEligibilityResponses_Organizations_InsurerId",
                        column: x => x.InsurerId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoverageEligibilityResponses_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EligibilityRequestId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CategorySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CategoryDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProductOrServiceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductOrServiceSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProductOrServiceDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DiagnosisCodes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityItems_CoverageEligibilityRequests_EligibilityRequestId",
                        column: x => x.EligibilityRequestId,
                        principalTable: "CoverageEligibilityRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreAuthorizationDiagnoses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreAuthorizationRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    DiagnosisCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiagnosisSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiagnosisDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DiagnosisType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnAdmission = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreAuthorizationDiagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreAuthorizationDiagnoses_PreAuthorizationRequests_PreAuthorizationRequestId",
                        column: x => x.PreAuthorizationRequestId,
                        principalTable: "PreAuthorizationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreAuthorizationItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreAuthorizationRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ServiceCode = table.Column<int>(type: "int", maxLength: 100, nullable: false),
                    ServiceSystem = table.Column<int>(type: "int", maxLength: 500, nullable: false),
                    CareTeamSequence = table.Column<int>(type: "int", nullable: true),
                    DiagnosisSequence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InformationSequence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductOrServiceCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductOrServiceSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductOrServiceDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServicedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServicedPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServicedPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BodySiteCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BodySiteSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubSiteCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    PractitionerId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreAuthorizationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreAuthorizationItems_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreAuthorizationItems_Practitioners_PractitionerId",
                        column: x => x.PractitionerId,
                        principalTable: "Practitioners",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreAuthorizationItems_PreAuthorizationRequests_PreAuthorizationRequestId",
                        column: x => x.PreAuthorizationRequestId,
                        principalTable: "PreAuthorizationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreAuthorizationResponses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreAuthorizationRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResponseIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResponseIdentifierSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Disposition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreAuthRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidityPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidityPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatientId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    RequestorId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ResponseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FhirResponseBundle = table.Column<string>(type: "ntext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreAuthorizationResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreAuthorizationResponses_Organizations_InsurerId",
                        column: x => x.InsurerId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreAuthorizationResponses_Organizations_RequestorId",
                        column: x => x.RequestorId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreAuthorizationResponses_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreAuthorizationResponses_PreAuthorizationRequests_PreAuthorizationRequestId",
                        column: x => x.PreAuthorizationRequestId,
                        principalTable: "PreAuthorizationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportingInfos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreAuthorizationRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategorySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuantitySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CodeValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StringValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuantityValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportingInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportingInfos_PreAuthorizationRequests_PreAuthorizationRequestId",
                        column: x => x.PreAuthorizationRequestId,
                        principalTable: "PreAuthorizationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appeal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppealNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DenialReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppealReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppealDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimId1 = table.Column<string>(type: "nvarchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appeal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appeal_Claims_ClaimId1",
                        column: x => x.ClaimId1,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimAccidents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccidentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccidentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccidentTypeSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AccidentLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccidentLocationCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccidentLocationState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccidentLocationCountry = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    DriverLicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehiclePlateNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientTransferReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PoliceReportNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEmergency = table.Column<bool>(type: "bit", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimAccidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimAccidents_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimCareTeams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    PractitionerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RoleSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RoleDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Qualification = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QualificationSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QualificationDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimCareTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimCareTeams_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimCareTeams_Practitioners_PractitionerId",
                        column: x => x.PractitionerId,
                        principalTable: "Practitioners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimDiagnoses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    DiagnosisCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiagnosisSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiagnosisDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DiagnosisType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DiagnosisTypeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OnAdmissionCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    OnAdmissionSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimDiagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimDiagnoses_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId1 = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    CareTeamSequence = table.Column<int>(type: "int", nullable: true),
                    ProductOrServiceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductOrServiceSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProductOrServiceDisplay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AltProductOrServiceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AltProductOrServiceSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServicedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServicedPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServicedPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Net = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PatientShare = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PatientShareCurrency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPackage = table.Column<bool>(type: "bit", nullable: false),
                    IsMaternity = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PatientInvoiceSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PatientInvoiceValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BodySiteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BodySiteSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubSiteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubSiteSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Factor = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    DiagnosisSequence = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InformationSequence = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcedureSequence = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UDI = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LocationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProgramCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProgramCodeSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimItems_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimItems_Claims_ClaimId1",
                        column: x => x.ClaimId1,
                        principalTable: "Claims",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClaimItems_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimProcedures",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ProcedureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProcedureSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcedureDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProcedureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcedureType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcedureTypeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UDI = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimProcedures_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimRelatedClaims",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelatedClaimIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RelatedClaimIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelationshipSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RelationshipDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ReferencedClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimRelatedClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimRelatedClaims_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimResponses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResponseIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResponseIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClaimResponseStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClaimType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClaimTypeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClaimSubType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClaimSubTypeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Use = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PatientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PreAuthRef = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PreAuthPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreAuthPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdvancedAuthReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AdvancedAuthReasonSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServiceProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FhirClaimResponseJson = table.Column<string>(type: "ntext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimResponses_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimResponses_Organizations_InsurerId",
                        column: x => x.InsurerId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimResponses_Organizations_RequestorId",
                        column: x => x.RequestorId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimResponses_Organizations_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimResponses_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimSupportingInfos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategorySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodeValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StringValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    QuantityValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuantitySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimSupportingInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimSupportingInfos_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BenefitBalances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EligibilityResponseId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CategorySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CategoryDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenefitBalances_CoverageEligibilityResponses_EligibilityResponseId",
                        column: x => x.EligibilityResponseId,
                        principalTable: "CoverageEligibilityResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityErrors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EligibilityRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EligibilityResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorCodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ErrorDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorLocation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ErrorField = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    ErrorOccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdditionalContext = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityErrors_CoverageEligibilityRequests_EligibilityRequestId",
                        column: x => x.EligibilityRequestId,
                        principalTable: "CoverageEligibilityRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EligibilityErrors_CoverageEligibilityResponses_EligibilityResponseId",
                        column: x => x.EligibilityResponseId,
                        principalTable: "CoverageEligibilityResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityItemModifiers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EligibilityItemId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ModifierCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ModifierDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityItemModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityItemModifiers_EligibilityItems_EligibilityItemId",
                        column: x => x.EligibilityItemId,
                        principalTable: "EligibilityItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreAuthorizationResponseErrors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreAuthorizationResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorCodeSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorField = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AdditionalContext = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorLocation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreAuthorizationResponseErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreAuthorizationResponseErrors_PreAuthorizationResponses_PreAuthorizationResponseId",
                        column: x => x.PreAuthorizationResponseId,
                        principalTable: "PreAuthorizationResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreAuthorizationResponseItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreAuthorizationResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    RequestSequence = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedUnitPrice = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: true),
                    AdjudicationResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BenefitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DenialReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreAuthorizationResponseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreAuthorizationResponseItems_PreAuthorizationResponses_PreAuthorizationResponseId",
                        column: x => x.PreAuthorizationResponseId,
                        principalTable: "PreAuthorizationResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppealDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppealId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DocumentTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentSize = table.Column<long>(type: "bigint", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttachedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppealDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppealDocuments_Appeal_AppealId",
                        column: x => x.AppealId,
                        principalTable: "Appeal",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimItemDetails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimItemId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ProductOrServiceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductOrServiceSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProductOrServiceDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AltProductOrServiceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AltProductOrServiceSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Net = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimItemDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimItemDetails_ClaimItems_ClaimItemId",
                        column: x => x.ClaimItemId,
                        principalTable: "ClaimItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimItemModifiers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimItemId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ModifierCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifierSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifierDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimItemModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimItemModifiers_ClaimItems_ClaimItemId",
                        column: x => x.ClaimItemId,
                        principalTable: "ClaimItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OralDetails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimItemId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToothCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToothCodeSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToothSurface = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToothSurfaceSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsOrthodontic = table.Column<bool>(type: "bit", nullable: true),
                    OrthodonticTreatmentPhase = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsExtraction = table.Column<bool>(type: "bit", nullable: true),
                    IsImplant = table.Column<bool>(type: "bit", nullable: true),
                    IsProsthetic = table.Column<bool>(type: "bit", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OralDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OralDetails_ClaimItems_ClaimItemId",
                        column: x => x.ClaimItemId,
                        principalTable: "ClaimItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisionPrescriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimItemId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LensSpecification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Product = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Eye = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sphere = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Cylinder = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Axis = table.Column<int>(type: "int", nullable: true),
                    Prism = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrismBase = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Add = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Power = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BackCurve = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Diameter = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Duration = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DurationUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisionPrescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisionPrescriptions_ClaimItems_ClaimItemId",
                        column: x => x.ClaimItemId,
                        principalTable: "ClaimItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppealRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AppealNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AppealIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AppealIdentifierValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AppealStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AppealLevel = table.Column<int>(type: "int", nullable: false),
                    ErrorCodeBeingAppealed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppealReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SupportingDocumentation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DenialDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppealDeadlineDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppealSubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewCompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedDecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppealOutcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DecisionExplanation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AllowsEscalation = table.Column<bool>(type: "bit", nullable: false),
                    EscalatedAppealId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsWithdrawn = table.Column<bool>(type: "bit", nullable: false),
                    WithdrawnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InternalReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastStatusUpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppealRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppealRequests_ClaimResponses_ClaimResponseId",
                        column: x => x.ClaimResponseId,
                        principalTable: "ClaimResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppealRequests_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppealRequests_Organizations_InsurerId",
                        column: x => x.InsurerId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppealRequests_Organizations_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppealRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimErrors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClaimResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PreAuthRequestId = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorCodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorSeverity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorExpression = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimErrors_ClaimResponses_ClaimResponseId",
                        column: x => x.ClaimResponseId,
                        principalTable: "ClaimResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimErrors_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimErrors_PreAuthorizationRequests_PreAuthRequestId",
                        column: x => x.PreAuthRequestId,
                        principalTable: "PreAuthorizationRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClaimResponseAddItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ProductOrServiceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProductOrServiceSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProductOrServiceDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedQuantity = table.Column<int>(type: "int", nullable: true),
                    BenefitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BenefitCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    SubmittedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DiagnosisSequence = table.Column<int>(type: "int", nullable: true),
                    InformationSequence = table.Column<int>(type: "int", nullable: true),
                    IsMaternity = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimResponseAddItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimResponseAddItems_ClaimResponses_ClaimResponseId",
                        column: x => x.ClaimResponseId,
                        principalTable: "ClaimResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimResponseDiagnosesExt",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    DiagnosisCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiagnosisSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiagnosisDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DiagnosisType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DiagnosisTypeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OnAdmissionCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    OnAdmissionSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimResponseDiagnosesExt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimResponseDiagnosesExt_ClaimResponses_ClaimResponseId",
                        column: x => x.ClaimResponseId,
                        principalTable: "ClaimResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimResponseInsurances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Focal = table.Column<bool>(type: "bit", nullable: false),
                    CoverageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PreAuthReferences = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimResponseInsurances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimResponseInsurances_ClaimResponses_ClaimResponseId",
                        column: x => x.ClaimResponseId,
                        principalTable: "ClaimResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimResponseInsurances_Coverages_CoverageId",
                        column: x => x.CoverageId,
                        principalTable: "Coverages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimResponseSupportingInfosExt",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategorySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodeValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StringValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    QuantityValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuantitySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimResponseSupportingInfosExt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimResponseSupportingInfosExt_ClaimResponses_ClaimResponseId",
                        column: x => x.ClaimResponseId,
                        principalTable: "ClaimResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimResponseTotals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategorySystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimResponseTotals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimResponseTotals_ClaimResponses_ClaimResponseId",
                        column: x => x.ClaimResponseId,
                        principalTable: "ClaimResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Benefits",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BenefitBalanceId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    BenefitType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BenefitTypeSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BenefitTypeDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AllowedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllowedCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AllowedUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UsedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PercentageAmount = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BenefitStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BenefitEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Benefits_BenefitBalances_BenefitBalanceId",
                        column: x => x.BenefitBalanceId,
                        principalTable: "BenefitBalances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppealStatusHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AppealId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StatusChangeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppealStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppealStatusHistories_AppealRequests_AppealId",
                        column: x => x.AppealId,
                        principalTable: "AppealRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimResponseAdjudications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimResponseAddItemId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AdjudicationCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AdjudicationSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdjudicationDisplay = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    QuantityValue = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimResponseAdjudications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimResponseAdjudications_ClaimResponseAddItems_ClaimResponseAddItemId",
                        column: x => x.ClaimResponseAddItemId,
                        principalTable: "ClaimResponseAddItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appeal_ClaimId1",
                table: "Appeal",
                column: "ClaimId1");

            migrationBuilder.CreateIndex(
                name: "IX_AppealDocuments_AppealId",
                table: "AppealDocuments",
                column: "AppealId");

            migrationBuilder.CreateIndex(
                name: "IX_AppealDocuments_DocumentType",
                table: "AppealDocuments",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_AppealDocuments_UploadedDate",
                table: "AppealDocuments",
                column: "UploadedDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_AppealDeadlineDate",
                table: "AppealRequests",
                column: "AppealDeadlineDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_AppealIdentifierValue",
                table: "AppealRequests",
                column: "AppealIdentifierValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_AppealLevel",
                table: "AppealRequests",
                column: "AppealLevel");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_AppealNumber",
                table: "AppealRequests",
                column: "AppealNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_AppealStatus",
                table: "AppealRequests",
                column: "AppealStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_AppealStatus_AppealDeadlineDate",
                table: "AppealRequests",
                columns: new[] { "AppealStatus", "AppealDeadlineDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_AppealSubmittedDate",
                table: "AppealRequests",
                column: "AppealSubmittedDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_ClaimId",
                table: "AppealRequests",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_ClaimId_AppealLevel",
                table: "AppealRequests",
                columns: new[] { "ClaimId", "AppealLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_ClaimResponseId",
                table: "AppealRequests",
                column: "ClaimResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_ErrorCodeBeingAppealed",
                table: "AppealRequests",
                column: "ErrorCodeBeingAppealed");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_InsurerId",
                table: "AppealRequests",
                column: "InsurerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_IsActive",
                table: "AppealRequests",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_IsWithdrawn",
                table: "AppealRequests",
                column: "IsWithdrawn");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_PatientId",
                table: "AppealRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppealRequests_ProviderId",
                table: "AppealRequests",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AppealStatusHistories_AppealId",
                table: "AppealStatusHistories",
                column: "AppealId");

            migrationBuilder.CreateIndex(
                name: "IX_AppealStatusHistories_AppealId_StatusChangeDate",
                table: "AppealStatusHistories",
                columns: new[] { "AppealId", "StatusChangeDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppealStatusHistories_Status",
                table: "AppealStatusHistories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppealStatusHistories_StatusChangeDate",
                table: "AppealStatusHistories",
                column: "StatusChangeDate");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditLevel",
                table: "AuditLogs",
                column: "AuditLevel");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType",
                table: "AuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Username",
                table: "AuditLogs",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitBalances_Category",
                table: "BenefitBalances",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitBalances_EligibilityResponseId",
                table: "BenefitBalances",
                column: "EligibilityResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitCodeMaster_BenefitCategory",
                table: "BenefitCodeMaster",
                column: "BenefitCategory");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitCodeMaster_BenefitCode",
                table: "BenefitCodeMaster",
                column: "BenefitCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BenefitCodeMaster_IsActive",
                table: "BenefitCodeMaster",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_BenefitBalanceId",
                table: "Benefits",
                column: "BenefitBalanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_BenefitType",
                table: "Benefits",
                column: "BenefitType");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_FocusIdentifierValue",
                table: "CancellationRequests",
                column: "FocusIdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_OwnerId",
                table: "CancellationRequests",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_ProcessingStatus",
                table: "CancellationRequests",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_RequesterId",
                table: "CancellationRequests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_Status",
                table: "CancellationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_TaskId",
                table: "CancellationRequests",
                column: "TaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CancellationResponses_CancellationRequestId",
                table: "CancellationResponses",
                column: "CancellationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationResponses_OwnerId",
                table: "CancellationResponses",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationResponses_ProcessingStatus",
                table: "CancellationResponses",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationResponses_RequesterId",
                table: "CancellationResponses",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationResponses_Status",
                table: "CancellationResponses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationResponses_TaskId",
                table: "CancellationResponses",
                column: "TaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAccidents_AccidentDate",
                table: "ClaimAccidents",
                column: "AccidentDate");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAccidents_AccidentType",
                table: "ClaimAccidents",
                column: "AccidentType");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAccidents_ClaimId",
                table: "ClaimAccidents",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimCareTeams_ClaimId",
                table: "ClaimCareTeams",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimCareTeams_PractitionerId",
                table: "ClaimCareTeams",
                column: "PractitionerId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimCareTeams_Sequence",
                table: "ClaimCareTeams",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDiagnoses_ClaimId",
                table: "ClaimDiagnoses",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDiagnoses_DiagnosisCode",
                table: "ClaimDiagnoses",
                column: "DiagnosisCode");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDiagnoses_Sequence",
                table: "ClaimDiagnoses",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimErrors_ClaimId",
                table: "ClaimErrors",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimErrors_ClaimResponseId",
                table: "ClaimErrors",
                column: "ClaimResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimErrors_ErrorCode",
                table: "ClaimErrors",
                column: "ErrorCode");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimErrors_ErrorSeverity",
                table: "ClaimErrors",
                column: "ErrorSeverity");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimErrors_PreAuthRequestId",
                table: "ClaimErrors",
                column: "PreAuthRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItemDetails_ClaimItemId",
                table: "ClaimItemDetails",
                column: "ClaimItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItemDetails_Sequence",
                table: "ClaimItemDetails",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItemModifiers_ClaimItemId",
                table: "ClaimItemModifiers",
                column: "ClaimItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItemModifiers_ModifierCode",
                table: "ClaimItemModifiers",
                column: "ModifierCode");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItems_BodySiteCode",
                table: "ClaimItems",
                column: "BodySiteCode");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItems_ClaimId",
                table: "ClaimItems",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItems_ClaimId1",
                table: "ClaimItems",
                column: "ClaimId1");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItems_LocationId",
                table: "ClaimItems",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItems_PatientInvoiceValue",
                table: "ClaimItems",
                column: "PatientInvoiceValue");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItems_ProgramCode",
                table: "ClaimItems",
                column: "ProgramCode");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItems_Sequence",
                table: "ClaimItems",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimItems_UDI",
                table: "ClaimItems",
                column: "UDI");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimProcedures_ClaimId",
                table: "ClaimProcedures",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimProcedures_ProcedureCode",
                table: "ClaimProcedures",
                column: "ProcedureCode");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimProcedures_ProcedureDate",
                table: "ClaimProcedures",
                column: "ProcedureDate");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimRelatedClaims_ClaimId",
                table: "ClaimRelatedClaims",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimRelatedClaims_RelatedClaimIdentifierValue",
                table: "ClaimRelatedClaims",
                column: "RelatedClaimIdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseAddItems_ClaimResponseId",
                table: "ClaimResponseAddItems",
                column: "ClaimResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseAddItems_Sequence",
                table: "ClaimResponseAddItems",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseAdjudications_AdjudicationCategory",
                table: "ClaimResponseAdjudications",
                column: "AdjudicationCategory");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseAdjudications_ClaimResponseAddItemId",
                table: "ClaimResponseAdjudications",
                column: "ClaimResponseAddItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseDiagnosesExt_ClaimResponseId",
                table: "ClaimResponseDiagnosesExt",
                column: "ClaimResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseDiagnosesExt_DiagnosisCode",
                table: "ClaimResponseDiagnosesExt",
                column: "DiagnosisCode");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseDiagnosesExt_Sequence",
                table: "ClaimResponseDiagnosesExt",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseInsurances_ClaimResponseId",
                table: "ClaimResponseInsurances",
                column: "ClaimResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseInsurances_CoverageId",
                table: "ClaimResponseInsurances",
                column: "CoverageId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponses_ClaimId",
                table: "ClaimResponses",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponses_ClaimResponseStatus",
                table: "ClaimResponses",
                column: "ClaimResponseStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponses_InsurerId",
                table: "ClaimResponses",
                column: "InsurerId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponses_PatientId",
                table: "ClaimResponses",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponses_PreAuthRef",
                table: "ClaimResponses",
                column: "PreAuthRef");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponses_RequestorId",
                table: "ClaimResponses",
                column: "RequestorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponses_ResponseIdentifierValue",
                table: "ClaimResponses",
                column: "ResponseIdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponses_ServiceProviderId",
                table: "ClaimResponses",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseSupportingInfosExt_Category",
                table: "ClaimResponseSupportingInfosExt",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseSupportingInfosExt_ClaimResponseId",
                table: "ClaimResponseSupportingInfosExt",
                column: "ClaimResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseSupportingInfosExt_Sequence",
                table: "ClaimResponseSupportingInfosExt",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseTotals_Category",
                table: "ClaimResponseTotals",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseTotals_ClaimResponseId",
                table: "ClaimResponseTotals",
                column: "ClaimResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ClaimNumber",
                table: "Claims",
                column: "ClaimNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Claims_CoverageId",
                table: "Claims",
                column: "CoverageId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_EligibilityOfflineReference",
                table: "Claims",
                column: "EligibilityOfflineReference");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_EncounterId",
                table: "Claims",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_EpisodeIdentifierValue",
                table: "Claims",
                column: "EpisodeIdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_InsurerId",
                table: "Claims",
                column: "InsurerId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_LocationId",
                table: "Claims",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_MessageHeaderId",
                table: "Claims",
                column: "MessageHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_PatientId",
                table: "Claims",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_PractitionerId",
                table: "Claims",
                column: "PractitionerId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_PreAuthorizationRef",
                table: "Claims",
                column: "PreAuthorizationRef");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_PrescriptionIdentifier",
                table: "Claims",
                column: "PrescriptionIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ProviderId",
                table: "Claims",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ReferralIdentifier",
                table: "Claims",
                column: "ReferralIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Status",
                table: "Claims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Use",
                table: "Claims",
                column: "Use");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSubmissionRules_IsActive",
                table: "ClaimSubmissionRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSubmissionRules_PayerMasterId",
                table: "ClaimSubmissionRules",
                column: "PayerMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSubmissionRules_PayerMasterId_RuleType",
                table: "ClaimSubmissionRules",
                columns: new[] { "PayerMasterId", "RuleType" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSubmissionRules_PolicyMasterId",
                table: "ClaimSubmissionRules",
                column: "PolicyMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSubmissionRules_Priority",
                table: "ClaimSubmissionRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSubmissionRules_RuleType",
                table: "ClaimSubmissionRules",
                column: "RuleType");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSupportingInfos_Category",
                table: "ClaimSupportingInfos",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSupportingInfos_ClaimId",
                table: "ClaimSupportingInfos",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSupportingInfos_Sequence",
                table: "ClaimSupportingInfos",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMaster_ClinicCode",
                table: "ClinicMaster",
                column: "ClinicCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMaster_ClinicName",
                table: "ClinicMaster",
                column: "ClinicName");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMaster_ClinicType",
                table: "ClinicMaster",
                column: "ClinicType");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMaster_IsActive",
                table: "ClinicMaster",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMaster_IsEmergencyAvailable",
                table: "ClinicMaster",
                column: "IsEmergencyAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMaster_OrganizationId",
                table: "ClinicMaster",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSystem_Name",
                table: "CodeSystem",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSystem_Url",
                table: "CodeSystem",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeSystem_Version",
                table: "CodeSystem",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationRequests_AboutIdentifierValue",
                table: "CommunicationRequests",
                column: "AboutIdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationRequests_CommunicationRequestId",
                table: "CommunicationRequests",
                column: "CommunicationRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationRequests_RecipientId",
                table: "CommunicationRequests",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationRequests_SenderId",
                table: "CommunicationRequests",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationRequests_Status",
                table: "CommunicationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationRequests_SubjectPatientId",
                table: "CommunicationRequests",
                column: "SubjectPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_AboutIdentifierValue",
                table: "Communications",
                column: "AboutIdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_CommunicationId",
                table: "Communications",
                column: "CommunicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Communications_ProcessingStatus",
                table: "Communications",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_RecipientId",
                table: "Communications",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_SenderId",
                table: "Communications",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_Status",
                table: "Communications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_SubjectPatientId",
                table: "Communications",
                column: "SubjectPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Concept_Code",
                table: "Concept",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Concept_CodeSystemEntityCodeSystemId",
                table: "Concept",
                column: "CodeSystemEntityCodeSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Concept_CodeSystemId_Code",
                table: "Concept",
                columns: new[] { "CodeSystemId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConceptCodeFilter_ValueSetEntityValueSetId",
                table: "ConceptCodeFilter",
                column: "ValueSetEntityValueSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptCodeFilter_ValueSetId_Code",
                table: "ConceptCodeFilter",
                columns: new[] { "ValueSetId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityRequests_CoverageId",
                table: "CoverageEligibilityRequests",
                column: "CoverageId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityRequests_EntererPractitionerId",
                table: "CoverageEligibilityRequests",
                column: "EntererPractitionerId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityRequests_InsurerId",
                table: "CoverageEligibilityRequests",
                column: "InsurerId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityRequests_MessageHeaderId",
                table: "CoverageEligibilityRequests",
                column: "MessageHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityRequests_PatientId",
                table: "CoverageEligibilityRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityRequests_ProviderId",
                table: "CoverageEligibilityRequests",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityRequests_RequestId",
                table: "CoverageEligibilityRequests",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityRequests_Status",
                table: "CoverageEligibilityRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityResponses_CoverageId",
                table: "CoverageEligibilityResponses",
                column: "CoverageId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityResponses_EligibilityRequestId",
                table: "CoverageEligibilityResponses",
                column: "EligibilityRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityResponses_InsurerId",
                table: "CoverageEligibilityResponses",
                column: "InsurerId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityResponses_MessageHeaderId",
                table: "CoverageEligibilityResponses",
                column: "MessageHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityResponses_Outcome",
                table: "CoverageEligibilityResponses",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityResponses_PatientId",
                table: "CoverageEligibilityResponses",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityResponses_RequestId",
                table: "CoverageEligibilityResponses",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageEligibilityResponses_ResponseUUID",
                table: "CoverageEligibilityResponses",
                column: "ResponseUUID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coverages_CoverageEndDate",
                table: "Coverages",
                column: "CoverageEndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Coverages_CoverageStartDate",
                table: "Coverages",
                column: "CoverageStartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Coverages_InsurerId",
                table: "Coverages",
                column: "InsurerId");

            migrationBuilder.CreateIndex(
                name: "IX_Coverages_InsurerId_Status",
                table: "Coverages",
                columns: new[] { "InsurerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Coverages_MemberID",
                table: "Coverages",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_Coverages_PatientId",
                table: "Coverages",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Coverages_PatientId_Status",
                table: "Coverages",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Coverages_PolicyNumber",
                table: "Coverages",
                column: "PolicyNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coverages_Status",
                table: "Coverages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Coverages_SubscriberPatientId",
                table: "Coverages",
                column: "SubscriberPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisCodeMaster_DiagnosisCategory",
                table: "DiagnosisCodeMaster",
                column: "DiagnosisCategory");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisCodeMaster_DiagnosisCode",
                table: "DiagnosisCodeMaster",
                column: "DiagnosisCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisCodeMaster_IsActive",
                table: "DiagnosisCodeMaster",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisCodeMaster_IsNphiesMapped",
                table: "DiagnosisCodeMaster",
                column: "IsNphiesMapped");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisCodeMaster_NphiesDiagnosisCode",
                table: "DiagnosisCodeMaster",
                column: "NphiesDiagnosisCode");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisCodeMaster_Severity",
                table: "DiagnosisCodeMaster",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorMaster_ClinicMasterId",
                table: "DoctorMaster",
                column: "ClinicMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorMaster_DoctorCode",
                table: "DoctorMaster",
                column: "DoctorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorMaster_DoctorName",
                table: "DoctorMaster",
                column: "DoctorName");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorMaster_IsActive",
                table: "DoctorMaster",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorMaster_IsAvailableForAppointments",
                table: "DoctorMaster",
                column: "IsAvailableForAppointments");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorMaster_IsConsultant",
                table: "DoctorMaster",
                column: "IsConsultant");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorMaster_PractitionerId",
                table: "DoctorMaster",
                column: "PractitionerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorMaster_Specialization",
                table: "DoctorMaster",
                column: "Specialization");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorQualification_CertificateNumber",
                table: "DoctorQualification",
                column: "CertificateNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorQualification_DoctorMasterId",
                table: "DoctorQualification",
                column: "DoctorMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorQualification_ExpiryDate",
                table: "DoctorQualification",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorQualification_IsExpiring",
                table: "DoctorQualification",
                column: "IsExpiring");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorQualification_QualificationType",
                table: "DoctorQualification",
                column: "QualificationType");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorQualification_VerificationStatus",
                table: "DoctorQualification",
                column: "VerificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityErrors_EligibilityRequestId",
                table: "EligibilityErrors",
                column: "EligibilityRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityErrors_EligibilityResponseId",
                table: "EligibilityErrors",
                column: "EligibilityResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityErrors_ErrorCode",
                table: "EligibilityErrors",
                column: "ErrorCode");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityErrors_Severity",
                table: "EligibilityErrors",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityItemModifiers_EligibilityItemId",
                table: "EligibilityItemModifiers",
                column: "EligibilityItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityItems_Category",
                table: "EligibilityItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityItems_EligibilityRequestId",
                table: "EligibilityItems",
                column: "EligibilityRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_Class",
                table: "Encounters",
                column: "Class");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_EncounterId",
                table: "Encounters",
                column: "EncounterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_PatientId",
                table: "Encounters",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_ServiceProviderId",
                table: "Encounters",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_Status",
                table: "Encounters",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCodeMasters_AdjudicationImpact",
                table: "ErrorCodeMasters",
                column: "AdjudicationImpact");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCodeMasters_AllowsAppeal",
                table: "ErrorCodeMasters",
                column: "AllowsAppeal");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCodeMasters_ErrorCategory",
                table: "ErrorCodeMasters",
                column: "ErrorCategory");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCodeMasters_ErrorCategory_Severity",
                table: "ErrorCodeMasters",
                columns: new[] { "ErrorCategory", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCodeMasters_ErrorCode",
                table: "ErrorCodeMasters",
                column: "ErrorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCodeMasters_IsActive",
                table: "ErrorCodeMasters",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCodeMasters_IsRecoverable",
                table: "ErrorCodeMasters",
                column: "IsRecoverable");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCodeMasters_Severity",
                table: "ErrorCodeMasters",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationLicense",
                table: "Locations",
                column: "LocationLicense",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_OrganizationId",
                table: "Locations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Status",
                table: "Locations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptAt",
                table: "LoginAttempts",
                column: "AttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IpAddress",
                table: "LoginAttempts",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IsSuccessful",
                table: "LoginAttempts",
                column: "IsSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserId",
                table: "LoginAttempts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Username",
                table: "LoginAttempts",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Username_AttemptAt",
                table: "LoginAttempts",
                columns: new[] { "Username", "AttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalDeviceCodeMaster_DeviceCode",
                table: "MedicalDeviceCodeMaster",
                column: "DeviceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalDeviceCodeMaster_DeviceType",
                table: "MedicalDeviceCodeMaster",
                column: "DeviceType");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalDeviceCodeMaster_IsActive",
                table: "MedicalDeviceCodeMaster",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalDeviceCodeMaster_IsImplantable",
                table: "MedicalDeviceCodeMaster",
                column: "IsImplantable");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalDeviceCodeMaster_IsNphiesMapped",
                table: "MedicalDeviceCodeMaster",
                column: "IsNphiesMapped");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalDeviceCodeMaster_IsReusable",
                table: "MedicalDeviceCodeMaster",
                column: "IsReusable");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalDeviceCodeMaster_NphiesDeviceCode",
                table: "MedicalDeviceCodeMaster",
                column: "NphiesDeviceCode");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationCodeMaster_ActiveIngredient",
                table: "MedicationCodeMaster",
                column: "ActiveIngredient");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationCodeMaster_IsActive",
                table: "MedicationCodeMaster",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationCodeMaster_IsControlledSubstance",
                table: "MedicationCodeMaster",
                column: "IsControlledSubstance");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationCodeMaster_IsNphiesMapped",
                table: "MedicationCodeMaster",
                column: "IsNphiesMapped");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationCodeMaster_MedicationCode",
                table: "MedicationCodeMaster",
                column: "MedicationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationCodeMaster_NphiesMedicationCode",
                table: "MedicationCodeMaster",
                column: "NphiesMedicationCode");

            migrationBuilder.CreateIndex(
                name: "IX_MessageHeaders_EventCode",
                table: "MessageHeaders",
                column: "EventCode");

            migrationBuilder.CreateIndex(
                name: "IX_MessageHeaders_MessageUUID",
                table: "MessageHeaders",
                column: "MessageUUID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageHeaders_SenderOrganizationId",
                table: "MessageHeaders",
                column: "SenderOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageHeaders_Status",
                table: "MessageHeaders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ModifierCodeMaster_IsActive",
                table: "ModifierCodeMaster",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ModifierCodeMaster_ModifierCode",
                table: "ModifierCodeMaster",
                column: "ModifierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModifierCodeMaster_ModifierType",
                table: "ModifierCodeMaster",
                column: "ModifierType");

            migrationBuilder.CreateIndex(
                name: "IX_ModifierCodeMaster_NphiesModifierCode",
                table: "ModifierCodeMaster",
                column: "NphiesModifierCode");

            migrationBuilder.CreateIndex(
                name: "IX_NphiesCodeMapping_CodeType",
                table: "NphiesCodeMapping",
                column: "CodeType");

            migrationBuilder.CreateIndex(
                name: "IX_NphiesCodeMapping_IsMappingValid",
                table: "NphiesCodeMapping",
                column: "IsMappingValid");

            migrationBuilder.CreateIndex(
                name: "IX_NphiesCodeMapping_LocalCode",
                table: "NphiesCodeMapping",
                column: "LocalCode");

            migrationBuilder.CreateIndex(
                name: "IX_NphiesCodeMapping_LocalCode_CodeType",
                table: "NphiesCodeMapping",
                columns: new[] { "LocalCode", "CodeType" });

            migrationBuilder.CreateIndex(
                name: "IX_NphiesCodeMapping_NphiesCode_NphiesCodeSystem",
                table: "NphiesCodeMapping",
                columns: new[] { "NphiesCode", "NphiesCodeSystem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NphiesMessageRequiredElement_NphiesMessageTypeEntityNphiesMessageTypeId",
                table: "NphiesMessageRequiredElement",
                column: "NphiesMessageTypeEntityNphiesMessageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_NphiesMessageRequiredElement_NphiesMessageTypeId_ElementPath",
                table: "NphiesMessageRequiredElement",
                columns: new[] { "NphiesMessageTypeId", "ElementPath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NphiesMessageRequiredElement_ValueSetId",
                table: "NphiesMessageRequiredElement",
                column: "ValueSetId");

            migrationBuilder.CreateIndex(
                name: "IX_NphiesMessageType_FhirResourceType",
                table: "NphiesMessageType",
                column: "FhirResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_NphiesMessageType_MessageType",
                table: "NphiesMessageType",
                column: "MessageType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OralDetails_ClaimItemId",
                table: "OralDetails",
                column: "ClaimItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CHINumber",
                table: "Organizations",
                column: "CHINumber",
                unique: true,
                filter: "[CHINumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_LicenseNumber",
                table: "Organizations",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_MOHLicenseNumber",
                table: "Organizations",
                column: "MOHLicenseNumber",
                unique: true,
                filter: "[MOHLicenseNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_NphiesOrganizationId",
                table: "Organizations",
                column: "NphiesOrganizationId",
                filter: "[NphiesOrganizationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_NphiesPayerId",
                table: "Organizations",
                column: "NphiesPayerId",
                filter: "[NphiesPayerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_NphiesProviderId",
                table: "Organizations",
                column: "NphiesProviderId",
                filter: "[NphiesProviderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OrganizationType",
                table: "Organizations",
                column: "OrganizationType");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Status",
                table: "Organizations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_TaxRegistrationNumber",
                table: "Organizations",
                column: "TaxRegistrationNumber",
                filter: "[TaxRegistrationNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_IsActive",
                table: "Patients",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_MRN",
                table: "Patients",
                column: "MRN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Status",
                table: "Patients",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayerMaster_IsActive",
                table: "PayerMaster",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PayerMaster_IsNphiesIntegrated",
                table: "PayerMaster",
                column: "IsNphiesIntegrated");

            migrationBuilder.CreateIndex(
                name: "IX_PayerMaster_IsNphiesMember",
                table: "PayerMaster",
                column: "IsNphiesMember");

            migrationBuilder.CreateIndex(
                name: "IX_PayerMaster_NphiesConnectionStatus",
                table: "PayerMaster",
                column: "NphiesConnectionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PayerMaster_NphiesPayerId",
                table: "PayerMaster",
                column: "NphiesPayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PayerMaster_PayerId",
                table: "PayerMaster",
                column: "PayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayerMaster_PayerName",
                table: "PayerMaster",
                column: "PayerName");

            migrationBuilder.CreateIndex(
                name: "IX_PayerMaster_PayerType",
                table: "PayerMaster",
                column: "PayerType");

            migrationBuilder.CreateIndex(
                name: "IX_PayerPolicyMaster_CoverageType",
                table: "PayerPolicyMaster",
                column: "CoverageType");

            migrationBuilder.CreateIndex(
                name: "IX_PayerPolicyMaster_EffectiveFromDate",
                table: "PayerPolicyMaster",
                column: "EffectiveFromDate");

            migrationBuilder.CreateIndex(
                name: "IX_PayerPolicyMaster_EffectiveToDate",
                table: "PayerPolicyMaster",
                column: "EffectiveToDate");

            migrationBuilder.CreateIndex(
                name: "IX_PayerPolicyMaster_IsPolicyActive",
                table: "PayerPolicyMaster",
                column: "IsPolicyActive");

            migrationBuilder.CreateIndex(
                name: "IX_PayerPolicyMaster_PayerMasterId",
                table: "PayerPolicyMaster",
                column: "PayerMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_PayerPolicyMaster_PayerMasterId_PolicyCode",
                table: "PayerPolicyMaster",
                columns: new[] { "PayerMasterId", "PolicyCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PayerPolicyMaster_PolicyCode",
                table: "PayerPolicyMaster",
                column: "PolicyCode");

            migrationBuilder.CreateIndex(
                name: "IX_PayerPolicyMaster_PolicyType",
                table: "PayerPolicyMaster",
                column: "PolicyType");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_CreatedDate",
                table: "PaymentNotices",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_IdentifierValue",
                table: "PaymentNotices",
                column: "IdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_PayeeId",
                table: "PaymentNotices",
                column: "PayeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_PaymentDate",
                table: "PaymentNotices",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_PaymentIdentifierValue",
                table: "PaymentNotices",
                column: "PaymentIdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_PaymentNoticeId",
                table: "PaymentNotices",
                column: "PaymentNoticeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_PaymentStatus",
                table: "PaymentNotices",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_ProviderId",
                table: "PaymentNotices",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_ProviderId_CreatedDate",
                table: "PaymentNotices",
                columns: new[] { "ProviderId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_Status",
                table: "PaymentNotices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_Status_PaymentStatus",
                table: "PaymentNotices",
                columns: new[] { "Status", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationDetails_PayeeId",
                table: "PaymentReconciliationDetails",
                column: "PayeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationDetails_PaymentReconciliationId",
                table: "PaymentReconciliationDetails",
                column: "PaymentReconciliationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationDetails_PaymentReconciliationId1",
                table: "PaymentReconciliationDetails",
                column: "PaymentReconciliationId1");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationDetails_SubmitterId",
                table: "PaymentReconciliationDetails",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliations_PaymentIssuerId",
                table: "PaymentReconciliations",
                column: "PaymentIssuerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliations_PaymentReconciliationId",
                table: "PaymentReconciliations",
                column: "PaymentReconciliationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliations_RequestorId",
                table: "PaymentReconciliations",
                column: "RequestorId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBenefitCoverage_BenefitType",
                table: "PolicyBenefitCoverage",
                column: "BenefitType");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBenefitCoverage_IsExcluded",
                table: "PolicyBenefitCoverage",
                column: "IsExcluded");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBenefitCoverage_PolicyMasterId",
                table: "PolicyBenefitCoverage",
                column: "PolicyMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBenefitCoverage_PolicyMasterId_ServiceCodeMasterId",
                table: "PolicyBenefitCoverage",
                columns: new[] { "PolicyMasterId", "ServiceCodeMasterId" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBenefitCoverage_RequiresPreAuth",
                table: "PolicyBenefitCoverage",
                column: "RequiresPreAuth");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBenefitCoverage_ServiceCategory",
                table: "PolicyBenefitCoverage",
                column: "ServiceCategory");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBenefitCoverage_ServiceCodeMasterId",
                table: "PolicyBenefitCoverage",
                column: "ServiceCodeMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_PollingRecords_CancellationRequestId",
                table: "PollingRecords",
                column: "CancellationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PollingRecords_CancellationResponseId",
                table: "PollingRecords",
                column: "CancellationResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_PollingRecords_CycleStatus",
                table: "PollingRecords",
                column: "CycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PollingRecords_PollingRecordId",
                table: "PollingRecords",
                column: "PollingRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollingRecords_ProcessingStatus",
                table: "PollingRecords",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PollingRecords_ProviderId",
                table: "PollingRecords",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PollingRecords_RequestSentAt",
                table: "PollingRecords",
                column: "RequestSentAt");

            migrationBuilder.CreateIndex(
                name: "IX_Practitioners_LicenseExpiryDate",
                table: "Practitioners",
                column: "LicenseExpiryDate",
                filter: "[LicenseExpiryDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Practitioners_LicenseNumber",
                table: "Practitioners",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Practitioners_NationalIdentificationNumber",
                table: "Practitioners",
                column: "NationalIdentificationNumber",
                unique: true,
                filter: "[NationalIdentificationNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Practitioners_OrganizationId",
                table: "Practitioners",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Practitioners_PractitionerLicenseNumber",
                table: "Practitioners",
                column: "PractitionerLicenseNumber",
                unique: true,
                filter: "[PractitionerLicenseNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Practitioners_PractitionerRole",
                table: "Practitioners",
                column: "PractitionerRole",
                filter: "[PractitionerRole] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Practitioners_Status",
                table: "Practitioners",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationDiagnoses_DiagnosisCode",
                table: "PreAuthorizationDiagnoses",
                column: "DiagnosisCode");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationDiagnoses_PreAuthorizationRequestId",
                table: "PreAuthorizationDiagnoses",
                column: "PreAuthorizationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationItems_LocationId",
                table: "PreAuthorizationItems",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationItems_PractitionerId",
                table: "PreAuthorizationItems",
                column: "PractitionerId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationItems_PreAuthorizationRequestId",
                table: "PreAuthorizationItems",
                column: "PreAuthorizationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationItems_ServiceCode",
                table: "PreAuthorizationItems",
                column: "ServiceCode");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationRequests_CoverageId",
                table: "PreAuthorizationRequests",
                column: "CoverageId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationRequests_InsurerId",
                table: "PreAuthorizationRequests",
                column: "InsurerId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationRequests_MessageHeaderId",
                table: "PreAuthorizationRequests",
                column: "MessageHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationRequests_PatientId",
                table: "PreAuthorizationRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationRequests_PatientId1",
                table: "PreAuthorizationRequests",
                column: "PatientId1");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationRequests_ProviderId",
                table: "PreAuthorizationRequests",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationRequests_ProviderId1",
                table: "PreAuthorizationRequests",
                column: "ProviderId1");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationRequests_RequestId",
                table: "PreAuthorizationRequests",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationRequests_Status",
                table: "PreAuthorizationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationResponseErrors_PreAuthorizationResponseId",
                table: "PreAuthorizationResponseErrors",
                column: "PreAuthorizationResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationResponseErrors_Severity",
                table: "PreAuthorizationResponseErrors",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationResponseItems_PreAuthorizationResponseId",
                table: "PreAuthorizationResponseItems",
                column: "PreAuthorizationResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationResponseItems_ServiceCode",
                table: "PreAuthorizationResponseItems",
                column: "ServiceCode");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationResponses_InsurerId",
                table: "PreAuthorizationResponses",
                column: "InsurerId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationResponses_PatientId",
                table: "PreAuthorizationResponses",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationResponses_PreAuthorizationRequestId",
                table: "PreAuthorizationResponses",
                column: "PreAuthorizationRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationResponses_RequestorId",
                table: "PreAuthorizationResponses",
                column: "RequestorId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationResponses_ResponseId",
                table: "PreAuthorizationResponses",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizationResponses_Status",
                table: "PreAuthorizationResponses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileElement_MessageType",
                table: "ProfileElement",
                column: "MessageType");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileElement_ProfileName_ElementPath",
                table: "ProfileElement",
                columns: new[] { "ProfileName", "ElementPath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileElement_ValueSetEntityValueSetId",
                table: "ProfileElement",
                column: "ValueSetEntityValueSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileElement_ValueSetId",
                table: "ProfileElement",
                column: "ValueSetId");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitLogs_CreatedAt",
                table: "RateLimitLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitLogs_Endpoint",
                table: "RateLimitLogs",
                column: "Endpoint");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitLogs_IpAddress",
                table: "RateLimitLogs",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitLogs_IpAddress_Endpoint_WindowStart",
                table: "RateLimitLogs",
                columns: new[] { "IpAddress", "Endpoint", "WindowStart" });

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitLogs_IsRateLimited",
                table: "RateLimitLogs",
                column: "IsRateLimited");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitLogs_UserId",
                table: "RateLimitLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitLogs_UserId_CreatedAt",
                table: "RateLimitLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_CreatedAt",
                table: "RefreshTokens",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IsRevoked",
                table: "RefreshTokens",
                column: "IsRevoked");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodeMaster_IsActive",
                table: "ServiceCodeMaster",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodeMaster_IsNphiesMapped",
                table: "ServiceCodeMaster",
                column: "IsNphiesMapped");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodeMaster_MappingValidationStatus",
                table: "ServiceCodeMaster",
                column: "MappingValidationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodeMaster_NphiesServiceCode",
                table: "ServiceCodeMaster",
                column: "NphiesServiceCode");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodeMaster_ServiceCategory",
                table: "ServiceCodeMaster",
                column: "ServiceCategory");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodeMaster_ServiceCode",
                table: "ServiceCodeMaster",
                column: "ServiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportingInfos_Category",
                table: "SupportingInfos",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SupportingInfos_PreAuthorizationRequestId",
                table: "SupportingInfos",
                column: "PreAuthorizationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsLocked",
                table: "Users",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId",
                table: "Users",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRule_ErrorCode",
                table: "ValidationRule",
                column: "ErrorCode");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRule_RuleType",
                table: "ValidationRule",
                column: "RuleType");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRule_ValueSetId",
                table: "ValidationRule",
                column: "ValueSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueSet_Name",
                table: "ValueSet",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ValueSet_Url",
                table: "ValueSet",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValueSet_Version",
                table: "ValueSet",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_ValueSetCodeSystemMap_CodeSystemEntityCodeSystemId",
                table: "ValueSetCodeSystemMap",
                column: "CodeSystemEntityCodeSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueSetCodeSystemMap_CodeSystemId",
                table: "ValueSetCodeSystemMap",
                column: "CodeSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueSetCodeSystemMap_ValueSetEntityValueSetId",
                table: "ValueSetCodeSystemMap",
                column: "ValueSetEntityValueSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueSetCodeSystemMap_ValueSetId_CodeSystemId",
                table: "ValueSetCodeSystemMap",
                columns: new[] { "ValueSetId", "CodeSystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisionPrescriptions_ClaimItemId",
                table: "VisionPrescriptions",
                column: "ClaimItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppealDocuments");

            migrationBuilder.DropTable(
                name: "AppealStatusHistories");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BenefitCodeMaster");

            migrationBuilder.DropTable(
                name: "Benefits");

            migrationBuilder.DropTable(
                name: "ClaimAccidents");

            migrationBuilder.DropTable(
                name: "ClaimCareTeams");

            migrationBuilder.DropTable(
                name: "ClaimDiagnoses");

            migrationBuilder.DropTable(
                name: "ClaimErrors");

            migrationBuilder.DropTable(
                name: "ClaimItemDetails");

            migrationBuilder.DropTable(
                name: "ClaimItemModifiers");

            migrationBuilder.DropTable(
                name: "ClaimProcedures");

            migrationBuilder.DropTable(
                name: "ClaimRelatedClaims");

            migrationBuilder.DropTable(
                name: "ClaimResponseAdjudications");

            migrationBuilder.DropTable(
                name: "ClaimResponseDiagnosesExt");

            migrationBuilder.DropTable(
                name: "ClaimResponseInsurances");

            migrationBuilder.DropTable(
                name: "ClaimResponseSupportingInfosExt");

            migrationBuilder.DropTable(
                name: "ClaimResponseTotals");

            migrationBuilder.DropTable(
                name: "ClaimSubmissionRules");

            migrationBuilder.DropTable(
                name: "ClaimSupportingInfos");

            migrationBuilder.DropTable(
                name: "CommunicationRequests");

            migrationBuilder.DropTable(
                name: "Communications");

            migrationBuilder.DropTable(
                name: "Concept");

            migrationBuilder.DropTable(
                name: "ConceptCodeFilter");

            migrationBuilder.DropTable(
                name: "DiagnosisCodeMaster");

            migrationBuilder.DropTable(
                name: "DoctorQualification");

            migrationBuilder.DropTable(
                name: "EligibilityErrors");

            migrationBuilder.DropTable(
                name: "EligibilityItemModifiers");

            migrationBuilder.DropTable(
                name: "ErrorCodeMasters");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "MedicalDeviceCodeMaster");

            migrationBuilder.DropTable(
                name: "MedicationCodeMaster");

            migrationBuilder.DropTable(
                name: "ModifierCodeMaster");

            migrationBuilder.DropTable(
                name: "NphiesCodeMapping");

            migrationBuilder.DropTable(
                name: "NphiesMessageRequiredElement");

            migrationBuilder.DropTable(
                name: "OralDetails");

            migrationBuilder.DropTable(
                name: "PaymentNotices");

            migrationBuilder.DropTable(
                name: "PaymentReconciliationDetails");

            migrationBuilder.DropTable(
                name: "PolicyBenefitCoverage");

            migrationBuilder.DropTable(
                name: "PollingRecords");

            migrationBuilder.DropTable(
                name: "PreAuthorizationDiagnoses");

            migrationBuilder.DropTable(
                name: "PreAuthorizationItems");

            migrationBuilder.DropTable(
                name: "PreAuthorizationResponseErrors");

            migrationBuilder.DropTable(
                name: "PreAuthorizationResponseItems");

            migrationBuilder.DropTable(
                name: "ProfileElement");

            migrationBuilder.DropTable(
                name: "RateLimitLogs");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "SupportingInfos");

            migrationBuilder.DropTable(
                name: "ValidationRule");

            migrationBuilder.DropTable(
                name: "ValueSetCodeSystemMap");

            migrationBuilder.DropTable(
                name: "VisionPrescriptions");

            migrationBuilder.DropTable(
                name: "Appeal");

            migrationBuilder.DropTable(
                name: "AppealRequests");

            migrationBuilder.DropTable(
                name: "BenefitBalances");

            migrationBuilder.DropTable(
                name: "ClaimResponseAddItems");

            migrationBuilder.DropTable(
                name: "DoctorMaster");

            migrationBuilder.DropTable(
                name: "EligibilityItems");

            migrationBuilder.DropTable(
                name: "NphiesMessageType");

            migrationBuilder.DropTable(
                name: "PaymentReconciliations");

            migrationBuilder.DropTable(
                name: "PayerPolicyMaster");

            migrationBuilder.DropTable(
                name: "ServiceCodeMaster");

            migrationBuilder.DropTable(
                name: "CancellationResponses");

            migrationBuilder.DropTable(
                name: "PreAuthorizationResponses");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "CodeSystem");

            migrationBuilder.DropTable(
                name: "ValueSet");

            migrationBuilder.DropTable(
                name: "ClaimItems");

            migrationBuilder.DropTable(
                name: "CoverageEligibilityResponses");

            migrationBuilder.DropTable(
                name: "ClaimResponses");

            migrationBuilder.DropTable(
                name: "ClinicMaster");

            migrationBuilder.DropTable(
                name: "PayerMaster");

            migrationBuilder.DropTable(
                name: "CancellationRequests");

            migrationBuilder.DropTable(
                name: "PreAuthorizationRequests");

            migrationBuilder.DropTable(
                name: "CoverageEligibilityRequests");

            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.DropTable(
                name: "Coverages");

            migrationBuilder.DropTable(
                name: "Encounters");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "MessageHeaders");

            migrationBuilder.DropTable(
                name: "Practitioners");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
