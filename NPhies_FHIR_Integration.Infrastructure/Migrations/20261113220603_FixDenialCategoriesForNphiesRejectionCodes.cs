using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <summary>
    /// Data fix: NPHIES rejection codes BE-* are Billing Errors and SE-* are Supporting Evidence
    /// (IG V2.6 section 6.3.10.2). Earlier versions stored them as "Benefit" and "Coding".
    /// </summary>
    public partial class FixDenialCategoriesForNphiesRejectionCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [ClaimDenials] SET [Category] = N'BillingError' WHERE [Category] = N'Benefit' AND UPPER([ReasonCode]) LIKE N'BE%';");
            migrationBuilder.Sql("UPDATE [ClaimDenials] SET [Category] = N'SupportingEvidence' WHERE [Category] = N'Coding' AND UPPER([ReasonCode]) LIKE N'SE%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [ClaimDenials] SET [Category] = N'Benefit' WHERE [Category] = N'BillingError';");
            migrationBuilder.Sql("UPDATE [ClaimDenials] SET [Category] = N'Coding' WHERE [Category] = N'SupportingEvidence';");
        }
    }
}
