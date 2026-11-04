using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InsuranceProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InsurancePlans_TenantId_Mode",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.AddColumn<bool>(
                name: "StaffMayChangeInsurance",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InsurancePlanId",
                schema: "ops",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InsuranceRecordedByUserId",
                schema: "ops",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "ops",
                table: "InsurancePlans",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PitchEn",
                schema: "ops",
                table: "InsurancePlans",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PitchEs",
                schema: "ops",
                table: "InsurancePlans",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PitchFr",
                schema: "ops",
                table: "InsurancePlans",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                schema: "ops",
                table: "InsurancePlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Slice 5D: each plan per mode becomes a product named after its mode; insured shipments point to their mode's product (D-7).
            // In EXEC: the idempotent script compiles each batch before running it, when the new columns do not exist yet (as Slice 1B).
            migrationBuilder.Sql(@"EXEC(N'UPDATE [ops].[InsurancePlans] SET [Name] = CASE [Mode] WHEN 2 THEN N''Ocean insurance'' WHEN 3 THEN N''Ground insurance'' ELSE N''Air insurance'' END WHERE [Name] = N''''');");
            migrationBuilder.Sql(@"EXEC(N'UPDATE s SET s.[InsurancePlanId] = p.[Id]
FROM [ops].[Shipments] s
JOIN [ops].[InsurancePlans] p ON p.[TenantId] = s.[TenantId]
    AND p.[Mode] = CASE WHEN s.[ServiceCode] LIKE N''ocean%'' THEN 2 WHEN s.[ServiceCode] LIKE N''ground%'' THEN 3 ELSE 1 END
WHERE (s.[Options] & 4) = 4 AND s.[InsurancePlanId] IS NULL');");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_InsurancePlanId",
                schema: "ops",
                table: "Shipments",
                column: "InsurancePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlans_TenantId_Mode_Position",
                schema: "ops",
                table: "InsurancePlans",
                columns: new[] { "TenantId", "Mode", "Position" });

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_InsurancePlans_InsurancePlanId",
                schema: "ops",
                table: "Shipments",
                column: "InsurancePlanId",
                principalSchema: "ops",
                principalTable: "InsurancePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_InsurancePlans_InsurancePlanId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_InsurancePlanId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePlans_TenantId_Mode_Position",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "StaffMayChangeInsurance",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "InsurancePlanId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "InsuranceRecordedByUserId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "PitchEn",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "PitchEs",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "PitchFr",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "Position",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlans_TenantId_Mode",
                schema: "ops",
                table: "InsurancePlans",
                columns: new[] { "TenantId", "Mode" },
                unique: true);
        }
    }
}
