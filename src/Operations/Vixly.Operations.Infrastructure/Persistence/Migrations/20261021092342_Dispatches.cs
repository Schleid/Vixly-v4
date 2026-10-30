using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Dispatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DispatchPrefix",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(6)",
                unicode: false,
                maxLength: 6,
                nullable: false,
                defaultValue: "DSP");

            migrationBuilder.AddColumn<Guid>(
                name: "DispatchId",
                schema: "ops",
                table: "Bundles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Carriers",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    AirlinePrefix = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carriers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dispatches",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationCountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    ServiceCode = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    CutoffUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BundleCount = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DepartedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArrivedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Legs = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dispatches_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dispatches_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CarrierRates",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarrierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    BundleType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PerKg = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    Minimum = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    PerBundle = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierRates_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "ops",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarrierRates_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_DispatchId",
                schema: "ops",
                table: "Bundles",
                column: "DispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_TenantId_DispatchId",
                schema: "ops",
                table: "Bundles",
                columns: new[] { "TenantId", "DispatchId" },
                filter: "[DispatchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierRates_CarrierId",
                schema: "ops",
                table: "CarrierRates",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierRates_DestinationWarehouseId",
                schema: "ops",
                table: "CarrierRates",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierRates_TenantId",
                schema: "ops",
                table: "CarrierRates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierRates_TenantId_CarrierId_CountryCode_StartsOn",
                schema: "ops",
                table: "CarrierRates",
                columns: new[] { "TenantId", "CarrierId", "CountryCode", "StartsOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Carriers_TenantId",
                schema: "ops",
                table: "Carriers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Carriers_TenantId_Name",
                schema: "ops",
                table: "Carriers",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_DestinationWarehouseId",
                schema: "ops",
                table: "Dispatches",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_TenantId",
                schema: "ops",
                table: "Dispatches",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_TenantId_Number",
                schema: "ops",
                table: "Dispatches",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_TenantId_RequestKey",
                schema: "ops",
                table: "Dispatches",
                columns: new[] { "TenantId", "RequestKey" },
                unique: true,
                filter: "[RequestKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_TenantId_WarehouseId_Status",
                schema: "ops",
                table: "Dispatches",
                columns: new[] { "TenantId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_WarehouseId",
                schema: "ops",
                table: "Dispatches",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bundles_Dispatches_DispatchId",
                schema: "ops",
                table: "Bundles",
                column: "DispatchId",
                principalSchema: "ops",
                principalTable: "Dispatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // A company already using DSP for another kind gets DSPT (or DPCH): two kinds never share a prefix.
            migrationBuilder.Sql(@"EXEC(N'UPDATE [ops].[TenantSettings] SET [DispatchPrefix] = CASE WHEN N''DSPT'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix], [SupplyOrderPrefix], [CreditNotePrefix], [BundlePrefix], [BoxPrefix]) THEN N''DPCH'' ELSE N''DSPT'' END WHERE N''DSP'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix], [SupplyOrderPrefix], [CreditNotePrefix], [BundlePrefix], [BoxPrefix])');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bundles_Dispatches_DispatchId",
                schema: "ops",
                table: "Bundles");

            migrationBuilder.DropTable(
                name: "CarrierRates",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Dispatches",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Carriers",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_DispatchId",
                schema: "ops",
                table: "Bundles");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_TenantId_DispatchId",
                schema: "ops",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "DispatchPrefix",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DispatchId",
                schema: "ops",
                table: "Bundles");
        }
    }
}
