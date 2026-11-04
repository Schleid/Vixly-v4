using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BundlesAndRepackBoxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoxPrefix",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(6)",
                unicode: false,
                maxLength: 6,
                nullable: false,
                defaultValue: "CB");

            migrationBuilder.AddColumn<string>(
                name: "BundlePrefix",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(6)",
                unicode: false,
                maxLength: 6,
                nullable: false,
                defaultValue: "BD");

            migrationBuilder.AddColumn<int>(
                name: "BundleTargetRule",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "BundleTypes",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(372)",
                maxLength: 372,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConsolidationWay",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // A company already using BD or CB for another kind of number gets BDL (BNDL when BDL is taken) or CBX (RPK), so its prefixes stay different. In EXEC: compiled after the columns exist.
            migrationBuilder.Sql(@"EXEC(N'UPDATE [ops].[TenantSettings] SET [BundlePrefix] = CASE WHEN N''BDL'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix], [SupplyOrderPrefix], [CreditNotePrefix]) THEN N''BNDL'' ELSE N''BDL'' END WHERE N''BD'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix], [SupplyOrderPrefix], [CreditNotePrefix])');");
            migrationBuilder.Sql(@"EXEC(N'UPDATE [ops].[TenantSettings] SET [BoxPrefix] = CASE WHEN N''CBX'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix], [SupplyOrderPrefix], [CreditNotePrefix]) THEN N''RPK'' ELSE N''CBX'' END WHERE N''CB'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix], [SupplyOrderPrefix], [CreditNotePrefix])');");

            migrationBuilder.AddColumn<int>(
                name: "ConsolidationWay",
                schema: "ops",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BoxId",
                schema: "ops",
                table: "Parcels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BundleId",
                schema: "ops",
                table: "Parcels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bundles",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationCountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    ServiceCode = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Basis = table.Column<int>(type: "int", nullable: false),
                    TargetKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Divisor = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ParcelCount = table.Column<int>(type: "int", nullable: false),
                    ParcelsWeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    ParcelsVolumetricKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    LengthCm = table.Column<decimal>(type: "decimal(8,1)", precision: 8, scale: 1, nullable: true),
                    WidthCm = table.Column<decimal>(type: "decimal(8,1)", precision: 8, scale: 1, nullable: true),
                    HeightCm = table.Column<decimal>(type: "decimal(8,1)", precision: 8, scale: 1, nullable: true),
                    VolumetricKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VoidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bundles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bundles_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bundles_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepackBoxes",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ParcelCount = table.Column<int>(type: "int", nullable: false),
                    ParcelsWeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    LengthCm = table.Column<decimal>(type: "decimal(8,1)", precision: 8, scale: 1, nullable: true),
                    WidthCm = table.Column<decimal>(type: "decimal(8,1)", precision: 8, scale: 1, nullable: true),
                    HeightCm = table.Column<decimal>(type: "decimal(8,1)", precision: 8, scale: 1, nullable: true),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VoidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepackBoxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepackBoxes_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalSchema: "ops",
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepackBoxes_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "ops",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepackBoxes_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_BoxId",
                schema: "ops",
                table: "Parcels",
                column: "BoxId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_BundleId",
                schema: "ops",
                table: "Parcels",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_BoxId",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "BoxId" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_BundleId",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "BundleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_DestinationWarehouseId",
                schema: "ops",
                table: "Bundles",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_TenantId",
                schema: "ops",
                table: "Bundles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_TenantId_Number",
                schema: "ops",
                table: "Bundles",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_TenantId_RequestKey",
                schema: "ops",
                table: "Bundles",
                columns: new[] { "TenantId", "RequestKey" },
                unique: true,
                filter: "[RequestKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_TenantId_WarehouseId_Status",
                schema: "ops",
                table: "Bundles",
                columns: new[] { "TenantId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_WarehouseId",
                schema: "ops",
                table: "Bundles",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_RepackBoxes_BundleId",
                schema: "ops",
                table: "RepackBoxes",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_RepackBoxes_ShipmentId",
                schema: "ops",
                table: "RepackBoxes",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepackBoxes_TenantId",
                schema: "ops",
                table: "RepackBoxes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RepackBoxes_TenantId_BundleId",
                schema: "ops",
                table: "RepackBoxes",
                columns: new[] { "TenantId", "BundleId" });

            migrationBuilder.CreateIndex(
                name: "IX_RepackBoxes_TenantId_Number",
                schema: "ops",
                table: "RepackBoxes",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepackBoxes_TenantId_ShipmentId",
                schema: "ops",
                table: "RepackBoxes",
                columns: new[] { "TenantId", "ShipmentId" },
                unique: true,
                filter: "[Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_RepackBoxes_TenantId_WarehouseId_Status",
                schema: "ops",
                table: "RepackBoxes",
                columns: new[] { "TenantId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RepackBoxes_WarehouseId",
                schema: "ops",
                table: "RepackBoxes",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_Bundles_BundleId",
                schema: "ops",
                table: "Parcels",
                column: "BundleId",
                principalSchema: "ops",
                principalTable: "Bundles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_RepackBoxes_BoxId",
                schema: "ops",
                table: "Parcels",
                column: "BoxId",
                principalSchema: "ops",
                principalTable: "RepackBoxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_Bundles_BundleId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_RepackBoxes_BoxId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropTable(
                name: "RepackBoxes",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Bundles",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_BoxId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_BundleId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_TenantId_BoxId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_TenantId_BundleId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "BoxPrefix",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "BundlePrefix",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "BundleTargetRule",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "BundleTypes",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ConsolidationWay",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ConsolidationWay",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "BoxId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "BundleId",
                schema: "ops",
                table: "Parcels");
        }
    }
}
