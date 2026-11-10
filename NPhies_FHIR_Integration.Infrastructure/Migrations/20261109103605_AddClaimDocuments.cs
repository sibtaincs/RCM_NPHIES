using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentId",
                table: "ClaimSupportingInfos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClaimDocumentContents",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimDocumentContents", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "ClaimDocuments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EncounterId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EncounterNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EncounterType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    OriginalSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsCompressed = table.Column<bool>(type: "bit", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StorageProvider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    OriginalStorageKey = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimDocuments_Encounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "Encounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClaimDocuments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSupportingInfos_DocumentId",
                table: "ClaimSupportingInfos",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_EncounterId",
                table: "ClaimDocuments",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_InvoiceNumber",
                table: "ClaimDocuments",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_PatientId_EncounterNumber_ServiceDate",
                table: "ClaimDocuments",
                columns: new[] { "PatientId", "EncounterNumber", "ServiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_ProviderId_EncounterType_ServiceDate",
                table: "ClaimDocuments",
                columns: new[] { "ProviderId", "EncounterType", "ServiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_ProviderId_ExternalId",
                table: "ClaimDocuments",
                columns: new[] { "ProviderId", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_ProviderId_InvoiceDate",
                table: "ClaimDocuments",
                columns: new[] { "ProviderId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_Sha256",
                table: "ClaimDocuments",
                column: "Sha256");

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimSupportingInfos_ClaimDocuments_DocumentId",
                table: "ClaimSupportingInfos",
                column: "DocumentId",
                principalTable: "ClaimDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClaimSupportingInfos_ClaimDocuments_DocumentId",
                table: "ClaimSupportingInfos");

            migrationBuilder.DropTable(
                name: "ClaimDocumentContents");

            migrationBuilder.DropTable(
                name: "ClaimDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ClaimSupportingInfos_DocumentId",
                table: "ClaimSupportingInfos");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "ClaimSupportingInfos");
        }
    }
}
