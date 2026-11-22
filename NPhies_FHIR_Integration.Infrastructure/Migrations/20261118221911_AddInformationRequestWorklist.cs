using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInformationRequestWorklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommunicationRequests_RecipientId",
                table: "CommunicationRequests");

            migrationBuilder.AddColumn<string>(
                name: "AdvancedAuthorizationId",
                table: "CommunicationRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                table: "CommunicationRequests",
                type: "datetime2",
                nullable: true);

            // Requests already waiting get the default routine target (48 hours from receipt).
            migrationBuilder.Sql(@"UPDATE CommunicationRequests SET DueAt = DATEADD(hour, 48, COALESCE(ReceivedAt, CreatedAt))
WHERE DueAt IS NULL AND RespondedCommunicationId IS NULL AND (Category IS NULL OR Category <> 'notification')");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationRequests_AdvancedAuthorizationId",
                table: "CommunicationRequests",
                column: "AdvancedAuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationRequests_RecipientId_RespondedCommunicationId_DueAt",
                table: "CommunicationRequests",
                columns: new[] { "RecipientId", "RespondedCommunicationId", "DueAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommunicationRequests_AdvancedAuthorizationId",
                table: "CommunicationRequests");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationRequests_RecipientId_RespondedCommunicationId_DueAt",
                table: "CommunicationRequests");

            migrationBuilder.DropColumn(
                name: "AdvancedAuthorizationId",
                table: "CommunicationRequests");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "CommunicationRequests");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationRequests_RecipientId",
                table: "CommunicationRequests",
                column: "RecipientId");
        }
    }
}
