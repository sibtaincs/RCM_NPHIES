using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportingInfoAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentContentType",
                table: "ClaimSupportingInfos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentData",
                table: "ClaimSupportingInfos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentTitle",
                table: "ClaimSupportingInfos",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "ClaimSupportingInfos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentContentType",
                table: "ClaimSupportingInfos");

            migrationBuilder.DropColumn(
                name: "AttachmentData",
                table: "ClaimSupportingInfos");

            migrationBuilder.DropColumn(
                name: "AttachmentTitle",
                table: "ClaimSupportingInfos");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "ClaimSupportingInfos");
        }
    }
}
