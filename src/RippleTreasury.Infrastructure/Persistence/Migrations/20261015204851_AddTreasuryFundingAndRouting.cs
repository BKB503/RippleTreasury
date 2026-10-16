using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RippleTreasury.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTreasuryFundingAndRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryAccountIdentifier",
                table: "PaymentInstructions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryAccountIdentifierType",
                table: "PaymentInstructions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryBic",
                table: "PaymentInstructions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryReference",
                table: "PaymentInstructions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingAccountId",
                table: "PaymentInstructions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingBankProviderCode",
                table: "PaymentInstructions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingCurrencyCode",
                table: "PaymentInstructions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingOwnerReference",
                table: "PaymentInstructions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoutingPolicyVersion",
                table: "PaymentInstructions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoutingReason",
                table: "PaymentInstructions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCountry",
                table: "PaymentInstructions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FundingAccounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerReference = table.Column<string>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    BankProviderCode = table.Column<string>(type: "TEXT", nullable: false),
                    AccountReference = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundingAccounts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "FundingAccounts",
                columns: new[] { "Id", "AccountReference", "BankProviderCode", "Country", "CurrencyCode", "IsActive", "OwnerReference", "TenantId" },
                values: new object[,]
                {
                    { "funding-company-de-eur-a", "mock-account-company-de-eur-a", "bank-a", "DE", "EUR", true, "company-de", "demo-tenant" },
                    { "funding-company-de-eur-b", "mock-account-company-de-eur-b", "bank-b", "DE", "EUR", true, "company-de", "demo-tenant" },
                    { "funding-company-de-gbp-a", "mock-account-company-de-gbp-a", "bank-a", "DE", "GBP", true, "company-de", "demo-tenant" },
                    { "funding-company-de-usd-b", "mock-account-company-de-usd-b", "bank-b", "DE", "USD", true, "company-de", "demo-tenant" },
                    { "funding-company-uk-eur-a", "mock-account-company-uk-eur-a", "bank-a", "GB", "EUR", true, "company-uk", "demo-tenant" },
                    { "funding-company-uk-eur-b", "mock-account-company-uk-eur-b", "bank-b", "GB", "EUR", true, "company-uk", "demo-tenant" },
                    { "funding-company-uk-gbp-a", "mock-account-company-uk-gbp-a", "bank-a", "GB", "GBP", true, "company-uk", "demo-tenant" },
                    { "funding-company-uk-usd-b", "mock-account-company-uk-usd-b", "bank-b", "GB", "USD", true, "company-uk", "demo-tenant" },
                    { "funding-company-us-eur-a", "mock-account-company-us-eur-a", "bank-a", "US", "EUR", true, "company-us", "demo-tenant" },
                    { "funding-company-us-eur-b", "mock-account-company-us-eur-b", "bank-b", "US", "EUR", true, "company-us", "demo-tenant" },
                    { "funding-company-us-gbp-a", "mock-account-company-us-gbp-a", "bank-a", "US", "GBP", true, "company-us", "demo-tenant" },
                    { "funding-company-us-usd-b", "mock-account-company-us-usd-b", "bank-b", "US", "USD", true, "company-us", "demo-tenant" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInstructions_FundingAccountId",
                table: "PaymentInstructions",
                column: "FundingAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingAccounts_TenantId_OwnerReference_CurrencyCode",
                table: "FundingAccounts",
                columns: new[] { "TenantId", "OwnerReference", "CurrencyCode" });

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentInstructions_FundingAccounts_FundingAccountId",
                table: "PaymentInstructions",
                column: "FundingAccountId",
                principalTable: "FundingAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentInstructions_FundingAccounts_FundingAccountId",
                table: "PaymentInstructions");

            migrationBuilder.DropTable(
                name: "FundingAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentInstructions_FundingAccountId",
                table: "PaymentInstructions");

            migrationBuilder.DropColumn(
                name: "BeneficiaryAccountIdentifier",
                table: "PaymentInstructions");

            migrationBuilder.DropColumn(
                name: "BeneficiaryAccountIdentifierType",
                table: "PaymentInstructions");

            migrationBuilder.DropColumn(
                name: "BeneficiaryBic",
                table: "PaymentInstructions");

            migrationBuilder.DropColumn(
                name: "BeneficiaryReference",
                table: "PaymentInstructions");

            migrationBuilder.DropColumn(
                name: "FundingAccountId",
                table: "PaymentInstructions");

            migrationBuilder.DropColumn(
                name: "FundingBankProviderCode",
                table: "PaymentInstructions");

            migrationBuilder.DropColumn(
                name: "FundingCurrencyCode",
                table: "PaymentInstructions");

            migrationBuilder.DropColumn(
                name: "FundingOwnerReference",
                table: "PaymentInstructions");

            migrationBuilder.DropColumn(
                name: "RoutingPolicyVersion",
                table: "PaymentInstructions");

            migrationBuilder.DropColumn(
                name: "RoutingReason",
                table: "PaymentInstructions");

            migrationBuilder.DropColumn(
                name: "SourceCountry",
                table: "PaymentInstructions");
        }
    }
}
