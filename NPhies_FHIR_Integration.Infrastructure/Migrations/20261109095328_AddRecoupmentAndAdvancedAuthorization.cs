using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoupmentAndAdvancedAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DetailIdentifier",
                table: "PaymentReconciliationDetails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PredecessorIdentifier",
                table: "PaymentReconciliationDetails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdvancedAuthorizations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdentifierValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PreAuthRef = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PreAuthPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreAuthPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClaimType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AdjudicationOutcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Disposition = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PatientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PatientIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PatientName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InsurerLicense = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServiceProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServiceProviderLicense = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferringProviderLicense = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TotalEligible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalBenefit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiagnosesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FhirClaimResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UsedByClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvancedAuthorizations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationDetails_DetailIdentifier",
                table: "PaymentReconciliationDetails",
                column: "DetailIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_AdvancedAuthorizations_IdentifierValue",
                table: "AdvancedAuthorizations",
                column: "IdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_AdvancedAuthorizations_PatientId",
                table: "AdvancedAuthorizations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvancedAuthorizations_PreAuthRef",
                table: "AdvancedAuthorizations",
                column: "PreAuthRef");

            migrationBuilder.CreateIndex(
                name: "IX_AdvancedAuthorizations_ServiceProviderId",
                table: "AdvancedAuthorizations",
                column: "ServiceProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvancedAuthorizations");

            migrationBuilder.DropIndex(
                name: "IX_PaymentReconciliationDetails_DetailIdentifier",
                table: "PaymentReconciliationDetails");

            migrationBuilder.DropColumn(
                name: "DetailIdentifier",
                table: "PaymentReconciliationDetails");

            migrationBuilder.DropColumn(
                name: "PredecessorIdentifier",
                table: "PaymentReconciliationDetails");
        }
    }
}
