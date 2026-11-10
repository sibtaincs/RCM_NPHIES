using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimDualApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MedicalReviewNotes",
                table: "Claims",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalReviewStatus",
                table: "Claims",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "MedicalReviewedAt",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalReviewerId",
                table: "Claims",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalReviewNotes",
                table: "Claims",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalReviewStatus",
                table: "Claims",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "TechnicalReviewedAt",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalReviewerId",
                table: "Claims",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Claims_MedicalReviewStatus",
                table: "Claims",
                column: "MedicalReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_TechnicalReviewStatus",
                table: "Claims",
                column: "TechnicalReviewStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Claims_MedicalReviewStatus",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_TechnicalReviewStatus",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "MedicalReviewNotes",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "MedicalReviewStatus",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "MedicalReviewedAt",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "MedicalReviewerId",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "TechnicalReviewNotes",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "TechnicalReviewStatus",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "TechnicalReviewedAt",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "TechnicalReviewerId",
                table: "Claims");
        }
    }
}
