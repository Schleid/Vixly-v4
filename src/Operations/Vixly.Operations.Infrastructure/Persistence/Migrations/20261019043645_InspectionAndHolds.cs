using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InspectionAndHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InspectionPercent",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.CreateTable(
                name: "InspectionRules",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inspections",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Why = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SelectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinishedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Result = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    ItemsFound = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CustomerAction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HoldReason = table.Column<int>(type: "int", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    FeeCurrency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: true),
                    CalledCustomerUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CallNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspections_Parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalSchema: "ops",
                        principalTable: "Parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspections_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RestrictedItems",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Words = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Countries = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    Carriers = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Requirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestrictedItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionRules_TenantId",
                schema: "ops",
                table: "InspectionRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionRules_TenantId_Kind_Value",
                schema: "ops",
                table: "InspectionRules",
                columns: new[] { "TenantId", "Kind", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_ParcelId",
                schema: "ops",
                table: "Inspections",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_TenantId",
                schema: "ops",
                table: "Inspections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_TenantId_ParcelId_CreatedUtc",
                schema: "ops",
                table: "Inspections",
                columns: new[] { "TenantId", "ParcelId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_TenantId_ParcelId_Open",
                schema: "ops",
                table: "Inspections",
                columns: new[] { "TenantId", "ParcelId" },
                unique: true,
                filter: "[Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_TenantId_WarehouseId_Status_CreatedUtc",
                schema: "ops",
                table: "Inspections",
                columns: new[] { "TenantId", "WarehouseId", "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_WarehouseId",
                schema: "ops",
                table: "Inspections",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_RestrictedItems_TenantId",
                schema: "ops",
                table: "RestrictedItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RestrictedItems_TenantId_Name",
                schema: "ops",
                table: "RestrictedItems",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspectionRules",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Inspections",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "RestrictedItems",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "InspectionPercent",
                schema: "ops",
                table: "TenantSettings");
        }
    }
}
