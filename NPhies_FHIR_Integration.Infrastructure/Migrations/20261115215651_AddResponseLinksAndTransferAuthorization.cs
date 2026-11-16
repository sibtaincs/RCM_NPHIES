using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseLinksAndTransferAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponseIdentifierSystem",
                table: "EligibilityChecks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EligibilityCheckId",
                table: "Claims",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferAuthorizationNumber",
                table: "ClaimResponses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransferAuthorizationPeriodEnd",
                table: "ClaimResponses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransferAuthorizationPeriodStart",
                table: "ClaimResponses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferAuthorizationProvider",
                table: "ClaimResponses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponseIdentifierSystem",
                table: "EligibilityChecks");

            migrationBuilder.DropColumn(
                name: "EligibilityCheckId",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "TransferAuthorizationNumber",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "TransferAuthorizationPeriodEnd",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "TransferAuthorizationPeriodStart",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "TransferAuthorizationProvider",
                table: "ClaimResponses");
        }
    }
}
