using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DestinationReceiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ClearsCustoms",
                schema: "ops",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PickupEmails",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PickupNotifiedUtc",
                schema: "ops",
                table: "Shipments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Onward",
                schema: "ops",
                table: "Dispatches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReceivedUtc",
                schema: "ops",
                table: "Dispatches",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceivingWarehouseId",
                schema: "ops",
                table: "Dispatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArrivedUtc",
                schema: "ops",
                table: "Bundles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentWarehouseId",
                schema: "ops",
                table: "Bundles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OpenedUtc",
                schema: "ops",
                table: "Bundles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ArrivalDiscrepancies",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArrivalDiscrepancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArrivalDiscrepancies_Dispatches_DispatchId",
                        column: x => x.DispatchId,
                        principalSchema: "ops",
                        principalTable: "Dispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArrivalDiscrepancies_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchBundles",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArrivedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MissingUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchBundles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchBundles_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalSchema: "ops",
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchBundles_Dispatches_DispatchId",
                        column: x => x.DispatchId,
                        principalSchema: "ops",
                        principalTable: "Dispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_ReceivingWarehouseId",
                schema: "ops",
                table: "Dispatches",
                column: "ReceivingWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_TenantId_ReceivingWarehouseId_Status",
                schema: "ops",
                table: "Dispatches",
                columns: new[] { "TenantId", "ReceivingWarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_CurrentWarehouseId",
                schema: "ops",
                table: "Bundles",
                column: "CurrentWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_TenantId_CurrentWarehouseId",
                schema: "ops",
                table: "Bundles",
                columns: new[] { "TenantId", "CurrentWarehouseId" },
                filter: "[CurrentWarehouseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArrivalDiscrepancies_DispatchId",
                schema: "ops",
                table: "ArrivalDiscrepancies",
                column: "DispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ArrivalDiscrepancies_TenantId",
                schema: "ops",
                table: "ArrivalDiscrepancies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ArrivalDiscrepancies_TenantId_DispatchId",
                schema: "ops",
                table: "ArrivalDiscrepancies",
                columns: new[] { "TenantId", "DispatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_ArrivalDiscrepancies_TenantId_DispatchId_BundleId",
                schema: "ops",
                table: "ArrivalDiscrepancies",
                columns: new[] { "TenantId", "DispatchId", "BundleId" },
                unique: true,
                filter: "[Status] = 1 AND [BundleId] IS NOT NULL AND [ParcelId] IS NULL AND [DispatchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArrivalDiscrepancies_TenantId_DispatchId_ParcelId",
                schema: "ops",
                table: "ArrivalDiscrepancies",
                columns: new[] { "TenantId", "DispatchId", "ParcelId" },
                unique: true,
                filter: "[Status] = 1 AND [ParcelId] IS NOT NULL AND [DispatchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArrivalDiscrepancies_TenantId_Status_WarehouseId",
                schema: "ops",
                table: "ArrivalDiscrepancies",
                columns: new[] { "TenantId", "Status", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ArrivalDiscrepancies_WarehouseId",
                schema: "ops",
                table: "ArrivalDiscrepancies",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchBundles_BundleId",
                schema: "ops",
                table: "DispatchBundles",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchBundles_DispatchId",
                schema: "ops",
                table: "DispatchBundles",
                column: "DispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchBundles_TenantId",
                schema: "ops",
                table: "DispatchBundles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchBundles_TenantId_BundleId",
                schema: "ops",
                table: "DispatchBundles",
                columns: new[] { "TenantId", "BundleId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchBundles_TenantId_DispatchId_BundleId",
                schema: "ops",
                table: "DispatchBundles",
                columns: new[] { "TenantId", "DispatchId", "BundleId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bundles_Warehouses_CurrentWarehouseId",
                schema: "ops",
                table: "Bundles",
                column: "CurrentWarehouseId",
                principalSchema: "ops",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dispatches_Warehouses_ReceivingWarehouseId",
                schema: "ops",
                table: "Dispatches",
                column: "ReceivingWarehouseId",
                principalSchema: "ops",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Slice 7 (R-11): the bundles already on a dispatch get their link row.
            migrationBuilder.Sql(@"EXEC(N'INSERT INTO [ops].[DispatchBundles] ([Id], [TenantId], [DispatchId], [BundleId], [CreatedUtc]) SELECT NEWID(), [b].[TenantId], [b].[DispatchId], [b].[Id], SYSDATETIMEOFFSET() FROM [ops].[Bundles] AS [b] WHERE [b].[DispatchId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [ops].[DispatchBundles] AS [l] WHERE [l].[DispatchId] = [b].[DispatchId] AND [l].[BundleId] = [b].[Id]);');");
            // The warehouse that receives each dispatch already planned: the hub serving its destination, unless it leaves from that hub; then its destination.
            migrationBuilder.Sql(@"EXEC(N'UPDATE [d] SET [ReceivingWarehouseId] = CASE WHEN [w].[HubWarehouseId] IS NOT NULL AND [w].[HubWarehouseId] <> [d].[DestinationWarehouseId] AND [w].[HubWarehouseId] <> [d].[WarehouseId] AND [o].[Address_CountryCode] <> [w].[Address_CountryCode] THEN [w].[HubWarehouseId] ELSE [d].[DestinationWarehouseId] END, [Onward] = CASE WHEN [w].[HubWarehouseId] = [d].[WarehouseId] OR [o].[Address_CountryCode] = [w].[Address_CountryCode] THEN CAST(1 AS bit) ELSE [d].[Onward] END FROM [ops].[Dispatches] AS [d] INNER JOIN [ops].[Warehouses] AS [w] ON [w].[Id] = [d].[DestinationWarehouseId] INNER JOIN [ops].[Warehouses] AS [o] ON [o].[Id] = [d].[WarehouseId] WHERE [d].[ReceivingWarehouseId] IS NULL;');");
            // Dispatches that arrived before destination receiving existed are taken as received (they may still be scanned).
            migrationBuilder.Sql(@"EXEC(N'UPDATE [ops].[Dispatches] SET [ReceivedUtc] = COALESCE([ArrivedUtc], SYSDATETIMEOFFSET()) WHERE [Status] = 4 AND [ReceivedUtc] IS NULL;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bundles_Warehouses_CurrentWarehouseId",
                schema: "ops",
                table: "Bundles");

            migrationBuilder.DropForeignKey(
                name: "FK_Dispatches_Warehouses_ReceivingWarehouseId",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropTable(
                name: "ArrivalDiscrepancies",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "DispatchBundles",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_Dispatches_ReceivingWarehouseId",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropIndex(
                name: "IX_Dispatches_TenantId_ReceivingWarehouseId_Status",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_CurrentWarehouseId",
                schema: "ops",
                table: "Bundles");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_TenantId_CurrentWarehouseId",
                schema: "ops",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "ClearsCustoms",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "PickupEmails",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "PickupNotifiedUtc",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Onward",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "ReceivedUtc",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "ReceivingWarehouseId",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "ArrivedUtc",
                schema: "ops",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "CurrentWarehouseId",
                schema: "ops",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "OpenedUtc",
                schema: "ops",
                table: "Bundles");
        }
    }
}
