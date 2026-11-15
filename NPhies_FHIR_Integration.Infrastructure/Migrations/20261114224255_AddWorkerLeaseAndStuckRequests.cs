using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerLeaseAndStuckRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RcmStuckRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Use = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ClaimStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WaitingSince = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BundleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestIdentifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HeldByNphies = table.Column<bool>(type: "bit", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FlaggedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RcmStuckRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RcmWorkerLeases",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RcmWorkerLeases", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RcmStuckRequests_ClaimId",
                table: "RcmStuckRequests",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_RcmStuckRequests_Level_ProviderId",
                table: "RcmStuckRequests",
                columns: new[] { "Level", "ProviderId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RcmStuckRequests");

            migrationBuilder.DropTable(
                name: "RcmWorkerLeases");
        }
    }
}
