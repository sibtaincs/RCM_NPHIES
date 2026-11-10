using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPhies_FHIR_Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRcmWorkflowLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentReconciliationDetails_PaymentReconciliations_PaymentReconciliationId1",
                table: "PaymentReconciliationDetails");

            migrationBuilder.DropIndex(
                name: "IX_PaymentReconciliationDetails_PaymentReconciliationId1",
                table: "PaymentReconciliationDetails");

            migrationBuilder.DropColumn(
                name: "PaymentReconciliationId1",
                table: "PaymentReconciliationDetails");

            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "PaymentReconciliations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcknowledgementError",
                table: "PaymentReconciliations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcknowledgementStatus",
                table: "PaymentReconciliations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAmount",
                table: "PaymentReconciliations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchStatus",
                table: "PaymentReconciliations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unmatched");

            migrationBuilder.AddColumn<string>(
                name: "MessageId",
                table: "PaymentReconciliations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentNoticeMessageId",
                table: "PaymentReconciliations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAt",
                table: "PaymentReconciliations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VarianceAmount",
                table: "PaymentReconciliations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestIdentifierValue",
                table: "PaymentReconciliationDetails",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAmount",
                table: "PaymentReconciliationDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchStatus",
                table: "PaymentReconciliationDetails",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unmatched");

            migrationBuilder.AddColumn<string>(
                name: "MatchedClaimId",
                table: "PaymentReconciliationDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VarianceAmount",
                table: "PaymentReconciliationDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstNameArabic",
                table: "Patients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullNameArabic",
                table: "Patients",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastNameArabic",
                table: "Patients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimId",
                table: "Communications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommunicationRequestId",
                table: "Communications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "Communications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "outbound");

            migrationBuilder.AddColumn<string>(
                name: "PayloadsJson",
                table: "Communications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "Communications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimId",
                table: "CommunicationRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageId",
                table: "CommunicationRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayloadsJson",
                table: "CommunicationRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "CommunicationRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAt",
                table: "CommunicationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAt",
                table: "CommunicationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RespondedCommunicationId",
                table: "CommunicationRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdjudicatedAt",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdjudicationOutcome",
                table: "Claims",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                table: "Claims",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchIdentifier",
                table: "Claims",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BatchNumber",
                table: "Claims",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsShadowBilling",
                table: "Claims",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTransfer",
                table: "Claims",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastMessageId",
                table: "Claims",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NphiesOutcome",
                table: "Claims",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NphiesStatus",
                table: "Claims",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "Claims",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PatientShareAmount",
                table: "Claims",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreAuthPeriodEnd",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdjudicationOutcome",
                table: "ClaimResponses",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Disposition",
                table: "ClaimResponses",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                table: "ClaimResponses",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentAmount",
                table: "ClaimResponses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "ClaimResponses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAt",
                table: "ClaimResponses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseMessageId",
                table: "ClaimResponses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalBenefit",
                table: "ClaimResponses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCopay",
                table: "ClaimResponses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEligible",
                table: "ClaimResponses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPatientShare",
                table: "ClaimResponses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSubmitted",
                table: "ClaimResponses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTax",
                table: "ClaimResponses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClaimDenials",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemSequence = table.Column<int>(type: "int", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReasonDisplay = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubmittedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeniedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DenialDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppealDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppealRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResubmissionClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimDenials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimDenials_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimResponseItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimResponseId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ItemSequence = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Submitted = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Eligible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Benefit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Copay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Deductible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PatientShare = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ApprovedQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReasonDisplay = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdjudicationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimResponseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimResponseItems_ClaimResponses_ClaimResponseId",
                        column: x => x.ClaimResponseId,
                        principalTable: "ClaimResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityChecks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CoverageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InsurerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Purposes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ServicedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestIdentifierValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsEligible = table.Column<bool>(type: "bit", nullable: true),
                    InForce = table.Column<bool>(type: "bit", nullable: true),
                    SiteEligibility = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Disposition = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NotInForceReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BenefitPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BenefitPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseIdentifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BenefitsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiscoveredCoveragesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityChecks_Coverages_CoverageId",
                        column: x => x.CoverageId,
                        principalTable: "Coverages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EligibilityChecks_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NphiesTransactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BundleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestIdentifierSystem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestIdentifierValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ResponseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResponseMessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResponseEventCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NphiesTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliations_MatchStatus",
                table: "PaymentReconciliations",
                column: "MatchStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliations_MessageId",
                table: "PaymentReconciliations",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationDetails_MatchedClaimId",
                table: "PaymentReconciliationDetails",
                column: "MatchedClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationDetails_RequestIdentifierValue",
                table: "PaymentReconciliationDetails",
                column: "RequestIdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_ClaimId",
                table: "Communications",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationRequests_ClaimId",
                table: "CommunicationRequests",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationRequests_MessageId",
                table: "CommunicationRequests",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_BatchIdentifier",
                table: "Claims",
                column: "BatchIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ClaimIdentifierValue",
                table: "Claims",
                column: "ClaimIdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_NphiesStatus",
                table: "Claims",
                column: "NphiesStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponses_ResponseMessageId",
                table: "ClaimResponses",
                column: "ResponseMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDenials_ClaimId_ClaimResponseId_ItemSequence_ReasonCode",
                table: "ClaimDenials",
                columns: new[] { "ClaimId", "ClaimResponseId", "ItemSequence", "ReasonCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDenials_ReasonCode",
                table: "ClaimDenials",
                column: "ReasonCode");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDenials_Status",
                table: "ClaimDenials",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseItems_ClaimId",
                table: "ClaimResponseItems",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponseItems_ClaimResponseId",
                table: "ClaimResponseItems",
                column: "ClaimResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityChecks_CoverageId",
                table: "EligibilityChecks",
                column: "CoverageId");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityChecks_PatientId_CreatedAt",
                table: "EligibilityChecks",
                columns: new[] { "PatientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityChecks_RequestIdentifierValue",
                table: "EligibilityChecks",
                column: "RequestIdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_NphiesTransactions_Direction_MessageId",
                table: "NphiesTransactions",
                columns: new[] { "Direction", "MessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NphiesTransactions_EntityType_EntityId",
                table: "NphiesTransactions",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_NphiesTransactions_RequestIdentifierValue",
                table: "NphiesTransactions",
                column: "RequestIdentifierValue");

            migrationBuilder.CreateIndex(
                name: "IX_NphiesTransactions_SentAt",
                table: "NphiesTransactions",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_NphiesTransactions_Status",
                table: "NphiesTransactions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimDenials");

            migrationBuilder.DropTable(
                name: "ClaimResponseItems");

            migrationBuilder.DropTable(
                name: "EligibilityChecks");

            migrationBuilder.DropTable(
                name: "NphiesTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentReconciliations_MatchStatus",
                table: "PaymentReconciliations");

            migrationBuilder.DropIndex(
                name: "IX_PaymentReconciliations_MessageId",
                table: "PaymentReconciliations");

            migrationBuilder.DropIndex(
                name: "IX_PaymentReconciliationDetails_MatchedClaimId",
                table: "PaymentReconciliationDetails");

            migrationBuilder.DropIndex(
                name: "IX_PaymentReconciliationDetails_RequestIdentifierValue",
                table: "PaymentReconciliationDetails");

            migrationBuilder.DropIndex(
                name: "IX_Communications_ClaimId",
                table: "Communications");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationRequests_ClaimId",
                table: "CommunicationRequests");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationRequests_MessageId",
                table: "CommunicationRequests");

            migrationBuilder.DropIndex(
                name: "IX_Claims_BatchIdentifier",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_ClaimIdentifierValue",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_NphiesStatus",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_ClaimResponses_ResponseMessageId",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "PaymentReconciliations");

            migrationBuilder.DropColumn(
                name: "AcknowledgementError",
                table: "PaymentReconciliations");

            migrationBuilder.DropColumn(
                name: "AcknowledgementStatus",
                table: "PaymentReconciliations");

            migrationBuilder.DropColumn(
                name: "ExpectedAmount",
                table: "PaymentReconciliations");

            migrationBuilder.DropColumn(
                name: "MatchStatus",
                table: "PaymentReconciliations");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "PaymentReconciliations");

            migrationBuilder.DropColumn(
                name: "PaymentNoticeMessageId",
                table: "PaymentReconciliations");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "PaymentReconciliations");

            migrationBuilder.DropColumn(
                name: "VarianceAmount",
                table: "PaymentReconciliations");

            migrationBuilder.DropColumn(
                name: "ExpectedAmount",
                table: "PaymentReconciliationDetails");

            migrationBuilder.DropColumn(
                name: "MatchStatus",
                table: "PaymentReconciliationDetails");

            migrationBuilder.DropColumn(
                name: "MatchedClaimId",
                table: "PaymentReconciliationDetails");

            migrationBuilder.DropColumn(
                name: "VarianceAmount",
                table: "PaymentReconciliationDetails");

            migrationBuilder.DropColumn(
                name: "FirstNameArabic",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "FullNameArabic",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "LastNameArabic",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ClaimId",
                table: "Communications");

            migrationBuilder.DropColumn(
                name: "CommunicationRequestId",
                table: "Communications");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Communications");

            migrationBuilder.DropColumn(
                name: "PayloadsJson",
                table: "Communications");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "Communications");

            migrationBuilder.DropColumn(
                name: "ClaimId",
                table: "CommunicationRequests");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "CommunicationRequests");

            migrationBuilder.DropColumn(
                name: "PayloadsJson",
                table: "CommunicationRequests");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "CommunicationRequests");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "CommunicationRequests");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "CommunicationRequests");

            migrationBuilder.DropColumn(
                name: "RespondedCommunicationId",
                table: "CommunicationRequests");

            migrationBuilder.DropColumn(
                name: "AdjudicatedAt",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "AdjudicationOutcome",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "BatchIdentifier",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "IsShadowBilling",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "IsTransfer",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "NphiesOutcome",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "NphiesStatus",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "PatientShareAmount",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "PreAuthPeriodEnd",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "AdjudicationOutcome",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "Disposition",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "PaymentAmount",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "ResponseMessageId",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "TotalBenefit",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "TotalCopay",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "TotalEligible",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "TotalPatientShare",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "TotalSubmitted",
                table: "ClaimResponses");

            migrationBuilder.DropColumn(
                name: "TotalTax",
                table: "ClaimResponses");

            migrationBuilder.AlterColumn<string>(
                name: "RequestIdentifierValue",
                table: "PaymentReconciliationDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReconciliationId1",
                table: "PaymentReconciliationDetails",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationDetails_PaymentReconciliationId1",
                table: "PaymentReconciliationDetails",
                column: "PaymentReconciliationId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentReconciliationDetails_PaymentReconciliations_PaymentReconciliationId1",
                table: "PaymentReconciliationDetails",
                column: "PaymentReconciliationId1",
                principalTable: "PaymentReconciliations",
                principalColumn: "Id");
        }
    }
}
