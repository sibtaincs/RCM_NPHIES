using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionResponseClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdjudicationOutcome",
                table: "NphiesTransactions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorKind",
                table: "NphiesTransactions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTags",
                table: "NphiesTransactions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NphiesGenerated",
                table: "NphiesTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjudicationOutcome",
                table: "NphiesTransactions");

            migrationBuilder.DropColumn(
                name: "ErrorKind",
                table: "NphiesTransactions");

            migrationBuilder.DropColumn(
                name: "MetaTags",
                table: "NphiesTransactions");

            migrationBuilder.DropColumn(
                name: "NphiesGenerated",
                table: "NphiesTransactions");
        }
    }
}
