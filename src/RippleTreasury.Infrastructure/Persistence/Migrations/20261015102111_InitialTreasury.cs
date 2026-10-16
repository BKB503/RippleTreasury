using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RippleTreasury.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTreasury : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MockBankExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", nullable: false),
                    Evidence = table.Column<string>(type: "TEXT", nullable: false),
                    FinancialExecutions = table.Column<int>(type: "INTEGER", nullable: false),
                    InstructionHash = table.Column<string>(type: "TEXT", nullable: false),
                    AmountInMinorUnits = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockBankExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientReference = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    TraceParent = table.Column<string>(type: "TEXT", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentBatches_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    BatchId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdempotencyRequests_PaymentBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "PaymentBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdempotencyRequests_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentInstructions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientReference = table.Column<string>(type: "TEXT", nullable: false),
                    AmountInMinorUnits = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", nullable: false),
                    Beneficiary = table.Column<string>(type: "TEXT", nullable: false),
                    DestinationCountry = table.Column<string>(type: "TEXT", nullable: false),
                    BankProviderCode = table.Column<string>(type: "TEXT", nullable: false),
                    PaymentPurpose = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedExecutionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Scenario = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessReference = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentInstructions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentInstructions_PaymentBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "PaymentBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    BankReference = table.Column<string>(type: "TEXT", nullable: true),
                    LeaseOwner = table.Column<string>(type: "TEXT", nullable: true),
                    LeaseUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextAttemptAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SubmissionCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentExecutions_PaymentInstructions_Id",
                        column: x => x.Id,
                        principalTable: "PaymentInstructions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPartySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: false),
                    LegalName = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    GroupReference = table.Column<string>(type: "TEXT", nullable: true),
                    AccountReference = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPartySnapshots", x => x.Id);
                    table.CheckConstraint("CK_PartySnapshots_Role", "Role IN ('Payer', 'Payee')");
                    table.ForeignKey(
                        name: "FK_PaymentPartySnapshots_PaymentInstructions_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "PaymentInstructions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorDisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    ActorKind = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Outcome = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    CommandId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAttempts_PaymentExecutions_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "PaymentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    ActorDisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    ActorKind = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Before = table.Column<string>(type: "TEXT", nullable: false),
                    After = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    AttemptId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PolicyVersion = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAuditEvents_PaymentExecutions_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "PaymentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActiveAttemptId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LeaseUntil = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationCases_PaymentExecutions_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "PaymentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "CreatedAt", "Name" },
                values: new object[] { "demo-tenant", new DateTime(2026, 10, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Northstar Treasury Group" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRequests_BatchId",
                table: "IdempotencyRequests",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRequests_TenantId_Key",
                table: "IdempotencyRequests",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_CommandId",
                table: "PaymentAttempts",
                column: "CommandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_PaymentId_StartedAt",
                table: "PaymentAttempts",
                columns: new[] { "PaymentId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditEvents_PaymentId_At",
                table: "PaymentAuditEvents",
                columns: new[] { "PaymentId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentBatches_TenantId_ClientReference",
                table: "PaymentBatches",
                columns: new[] { "TenantId", "ClientReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentExecutions_ExecutionId",
                table: "PaymentExecutions",
                column: "ExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentExecutions_Status_NextAttemptAt_LeaseUntil",
                table: "PaymentExecutions",
                columns: new[] { "Status", "NextAttemptAt", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInstructions_BatchId_ClientReference",
                table: "PaymentInstructions",
                columns: new[] { "BatchId", "ClientReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPartySnapshots_PaymentId_Role",
                table: "PaymentPartySnapshots",
                columns: new[] { "PaymentId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationCases_PaymentId",
                table: "ReconciliationCases",
                column: "PaymentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyRequests");

            migrationBuilder.DropTable(
                name: "MockBankExecutions");

            migrationBuilder.DropTable(
                name: "PaymentAttempts");

            migrationBuilder.DropTable(
                name: "PaymentAuditEvents");

            migrationBuilder.DropTable(
                name: "PaymentPartySnapshots");

            migrationBuilder.DropTable(
                name: "ReconciliationCases");

            migrationBuilder.DropTable(
                name: "PaymentExecutions");

            migrationBuilder.DropTable(
                name: "PaymentInstructions");

            migrationBuilder.DropTable(
                name: "PaymentBatches");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
