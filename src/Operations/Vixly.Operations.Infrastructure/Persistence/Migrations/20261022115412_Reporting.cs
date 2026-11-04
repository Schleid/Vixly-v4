using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Reporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DispatchGraceHours",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReadyTargetDays",
                schema: "ops",
                table: "ShippingServiceSetups",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReportSchedules",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportKey = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Grain = table.Column<int>(type: "int", nullable: false),
                    Dimension = table.Column<int>(type: "int", nullable: true),
                    Days = table.Column<int>(type: "int", nullable: false),
                    Recipients = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NextRunUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSentUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Failures = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSchedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_TenantId_Code_OccurredUtc",
                schema: "ops",
                table: "TrackingEvents",
                columns: new[] { "TenantId", "Code", "OccurredUtc" })
                .Annotation("SqlServer:Include", new[] { "SubjectKind", "SubjectId", "LocationCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_BookedUtc",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "BookedUtc" },
                filter: "[BookedUtc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_TakenUtc",
                schema: "ops",
                table: "Payments",
                columns: new[] { "TenantId", "TakenUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_IssuedOn",
                schema: "ops",
                table: "Invoices",
                columns: new[] { "TenantId", "IssuedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_TenantId_DepartedUtc",
                schema: "ops",
                table: "Dispatches",
                columns: new[] { "TenantId", "DepartedUtc" },
                filter: "[DepartedUtc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_NextRunUtc",
                schema: "ops",
                table: "ReportSchedules",
                column: "NextRunUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_TenantId",
                schema: "ops",
                table: "ReportSchedules",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportSchedules",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_TrackingEvents_TenantId_Code_OccurredUtc",
                schema: "ops",
                table: "TrackingEvents");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId_BookedUtc",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId_TakenUtc",
                schema: "ops",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_IssuedOn",
                schema: "ops",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Dispatches_TenantId_DepartedUtc",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "DispatchGraceHours",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ReadyTargetDays",
                schema: "ops",
                table: "ShippingServiceSetups");
        }
    }
}
