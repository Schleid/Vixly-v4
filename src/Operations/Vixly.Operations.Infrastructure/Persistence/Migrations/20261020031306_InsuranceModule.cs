using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InsuranceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PolicyId",
                schema: "ops",
                table: "InsurancePlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InsurancePolicies",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    InsurerName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    InsurerEmail = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    InsurerPhone = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ClaimsEmail = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ClaimsPhone = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    BrokerName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    BrokerEmail = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    BrokerPhone = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PolicyNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    LimitPerShipment = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    LimitPerConveyance = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    LimitPerLocation = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    LimitPerYear = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    DeductibleAmount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    DeductiblePercent = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    DeductibleMinimum = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    Risks = table.Column<int>(type: "int", nullable: false),
                    Modes = table.Column<int>(type: "int", nullable: false),
                    Territory = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Exclusions = table.Column<int>(type: "int", nullable: false),
                    OtherExclusions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NoticeDays = table.Column<int>(type: "int", nullable: true),
                    Declarations = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Documents = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurancePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiabilityTerms",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Basis = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    WeightUnit = table.Column<int>(type: "int", nullable: false),
                    Cap = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    CustomerText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiabilityTerms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceCertificates",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    AcceptedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    Premium = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: true),
                    VoidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceCertificates_InsurancePlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "ops",
                        principalTable: "InsurancePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceCertificates_InsurancePolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalSchema: "ops",
                        principalTable: "InsurancePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceCertificates_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "ops",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlans_PolicyId",
                schema: "ops",
                table: "InsurancePlans",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCertificates_PlanId",
                schema: "ops",
                table: "InsuranceCertificates",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCertificates_PolicyId",
                schema: "ops",
                table: "InsuranceCertificates",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCertificates_ShipmentId",
                schema: "ops",
                table: "InsuranceCertificates",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCertificates_TenantId",
                schema: "ops",
                table: "InsuranceCertificates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCertificates_TenantId_Number",
                schema: "ops",
                table: "InsuranceCertificates",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCertificates_TenantId_PolicyId_CreatedUtc",
                schema: "ops",
                table: "InsuranceCertificates",
                columns: new[] { "TenantId", "PolicyId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCertificates_TenantId_ShipmentId",
                schema: "ops",
                table: "InsuranceCertificates",
                columns: new[] { "TenantId", "ShipmentId" },
                unique: true,
                filter: "[Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_TenantId",
                schema: "ops",
                table: "InsurancePolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_TenantId_IsArchived_EndsOn",
                schema: "ops",
                table: "InsurancePolicies",
                columns: new[] { "TenantId", "IsArchived", "EndsOn" });

            migrationBuilder.CreateIndex(
                name: "IX_LiabilityTerms_TenantId",
                schema: "ops",
                table: "LiabilityTerms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LiabilityTerms_TenantId_Mode",
                schema: "ops",
                table: "LiabilityTerms",
                columns: new[] { "TenantId", "Mode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InsurancePlans_InsurancePolicies_PolicyId",
                schema: "ops",
                table: "InsurancePlans",
                column: "PolicyId",
                principalSchema: "ops",
                principalTable: "InsurancePolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsurancePlans_InsurancePolicies_PolicyId",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropTable(
                name: "InsuranceCertificates",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "LiabilityTerms",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "InsurancePolicies",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePlans_PolicyId",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "PolicyId",
                schema: "ops",
                table: "InsurancePlans");
        }
    }
}
