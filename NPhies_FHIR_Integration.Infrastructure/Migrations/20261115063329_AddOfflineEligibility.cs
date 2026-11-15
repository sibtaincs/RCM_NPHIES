using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfflineEligibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOffline",
                table: "EligibilityChecks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfflineDate",
                table: "EligibilityChecks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfflineReference",
                table: "EligibilityChecks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOffline",
                table: "EligibilityChecks");

            migrationBuilder.DropColumn(
                name: "OfflineDate",
                table: "EligibilityChecks");

            migrationBuilder.DropColumn(
                name: "OfflineReference",
                table: "EligibilityChecks");
        }
    }
}
