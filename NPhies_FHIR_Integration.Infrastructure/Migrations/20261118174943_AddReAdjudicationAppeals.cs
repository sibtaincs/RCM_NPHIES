using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReAdjudicationAppeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItemSequences",
                table: "Communications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonCode",
                table: "Communications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemSequences",
                table: "CommunicationRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReissueReason",
                table: "ClaimResponses",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppealMethod",
                table: "AppealRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Resubmission");

            migrationBuilder.AddColumn<string>(
                name: "CommunicationId",
                table: "AppealRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemSequences",
                table: "Communications");

            migrationBuilder.DropColumn(
                name: "ReasonCode",
                table: "Communications");

            migrationBuilder.DropColumn(
                name: "ItemSequences",
                table: "CommunicationRequests");

            migrationBuilder.DropColumn(
                name: "ReissueReason",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "AppealMethod",
                table: "AppealRequests");

            migrationBuilder.DropColumn(
                name: "CommunicationId",
                table: "AppealRequests");
        }
    }
}
