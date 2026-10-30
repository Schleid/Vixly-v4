using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RoutesAndModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Slice 4C: zones become destination warehouses; prices go on routes (origin and destination warehouses).
            migrationBuilder.DropForeignKey(name: "FK_ChargeExtras_Zones_ZoneId", schema: "ops", table: "ChargeExtras");
            migrationBuilder.DropForeignKey(name: "FK_Quotes_Zones_ZoneId", schema: "ops", table: "Quotes");
            migrationBuilder.DropForeignKey(name: "FK_RateCards_Zones_ZoneId", schema: "ops", table: "RateCards");
            migrationBuilder.DropForeignKey(name: "FK_Shipments_Zones_DestinationZoneId", schema: "ops", table: "Shipments");

            migrationBuilder.DropIndex(name: "IX_RateCards_TenantId_ServiceCode_ZoneId_CustomerId", schema: "ops", table: "RateCards");
            migrationBuilder.DropIndex(name: "IX_InsurancePlans_TenantId", schema: "ops", table: "InsurancePlans");
            migrationBuilder.DropIndex(name: "IX_RateCards_ZoneId", schema: "ops", table: "RateCards");
            migrationBuilder.DropIndex(name: "IX_Quotes_ZoneId", schema: "ops", table: "Quotes");
            migrationBuilder.DropIndex(name: "IX_ChargeExtras_ZoneId", schema: "ops", table: "ChargeExtras");
            migrationBuilder.DropIndex(name: "IX_Shipments_DestinationZoneId", schema: "ops", table: "Shipments");

            migrationBuilder.AddColumn<bool>(name: "DeliversHome", schema: "ops", table: "Warehouses", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "HomeArea", schema: "ops", table: "Warehouses", type: "nvarchar(max)", nullable: false, defaultValue: "[]");
            migrationBuilder.AddColumn<Guid>(name: "HubWarehouseId", schema: "ops", table: "Warehouses", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<int>(name: "OperatedBy", schema: "ops", table: "Warehouses", type: "int", nullable: false, defaultValue: 1);
            migrationBuilder.AddColumn<string>(name: "PartnerEmail", schema: "ops", table: "Warehouses", type: "nvarchar(254)", maxLength: 254, nullable: true);
            migrationBuilder.AddColumn<string>(name: "PartnerName", schema: "ops", table: "Warehouses", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "PartnerPhone", schema: "ops", table: "Warehouses", type: "varchar(20)", unicode: false, maxLength: 20, nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "DestinationWarehouseId", schema: "ops", table: "Shipments", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "InsuranceAcceptedUtc", schema: "ops", table: "Shipments", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "OriginWarehouseId", schema: "ops", table: "RateCards", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "DestinationWarehouseId", schema: "ops", table: "RateCards", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Customs", schema: "ops", table: "RateCards", type: "int", nullable: false, defaultValue: 2);
            migrationBuilder.AddColumn<Guid>(name: "OriginWarehouseId", schema: "ops", table: "Quotes", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "DestinationWarehouseId", schema: "ops", table: "Quotes", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Mode", schema: "ops", table: "InsurancePlans", type: "int", nullable: false, defaultValue: 1);
            migrationBuilder.AddColumn<int>(name: "Mode", schema: "ops", table: "ChargeExtras", type: "int", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "OriginWarehouseId", schema: "ops", table: "ChargeExtras", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "DestinationWarehouseId", schema: "ops", table: "ChargeExtras", type: "uniqueidentifier", nullable: true);

            // Each zone maps to a destination warehouse: one of its pickup branches (a pickup point first, then a hub), else the warehouse of the branch it was made from, else its hub.
            migrationBuilder.Sql(@"EXEC(N'CREATE TABLE [ops].[ZoneWarehouseMap] ([ZoneId] uniqueidentifier NOT NULL PRIMARY KEY, [WarehouseId] uniqueidentifier NULL);
INSERT INTO [ops].[ZoneWarehouseMap] ([ZoneId], [WarehouseId])
SELECT z.[Id], COALESCE(
    (SELECT TOP 1 w.[Id] FROM [ops].[Warehouses] w WHERE w.[TenantId] = z.[TenantId] AND w.[Status] = 0
        AND w.[BranchId] IN (SELECT TRY_CONVERT(uniqueidentifier, p.[value]) FROM OPENJSON(CASE WHEN ISJSON(z.[PickupBranchIds]) = 1 THEN z.[PickupBranchIds] ELSE N''[]'' END) p) ORDER BY CASE WHEN (w.[Roles] & 4) = 4 THEN 0 WHEN (w.[Roles] & 2) = 2 THEN 1 ELSE 2 END, w.[Code]),
    (SELECT TOP 1 w.[Id] FROM [ops].[Warehouses] w WHERE w.[TenantId] = z.[TenantId] AND w.[Status] = 0 AND w.[BranchId] = z.[Id] ORDER BY CASE WHEN (w.[Roles] & 4) = 4 THEN 0 WHEN (w.[Roles] & 2) = 2 THEN 1 ELSE 2 END, w.[Code]),
    (SELECT w.[Id] FROM [ops].[Warehouses] w WHERE w.[Id] = z.[HubWarehouseId]))
FROM [ops].[Zones] z;');");

            // The warehouse takes what its zones in use offered, all of them together: pickup (the pickup-point role), home delivery in
            // their cities (anywhere in the country when one of them named none), the hub. A zone not in use changes nothing.
            migrationBuilder.Sql(@"EXEC(N'UPDATE w SET
    [Roles] = w.[Roles] | CASE WHEN a.[Pickup] = 1 THEN 4 ELSE 0 END,
    [DeliversHome] = a.[Home],
    [HomeArea] = CASE WHEN a.[Home] = 1 AND a.[Anywhere] = 0 THEN ISNULL((SELECT N''['' + STRING_AGG(N''""'' + STRING_ESCAPE(c.[City], ''json'') + N''""'', N'','') + N'']''
        FROM (SELECT DISTINCT TRIM(cc.[value]) AS [City]
            FROM [ops].[Zones] zz JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = zz.[Id]
            CROSS APPLY OPENJSON(CASE WHEN ISJSON(zz.[Cities]) = 1 THEN zz.[Cities] ELSE N''[]'' END) cc
            WHERE m.[WarehouseId] = w.[Id] AND zz.[IsActive] = 1 AND (zz.[Delivery] & 2) = 2 AND TRIM(cc.[value]) <> N'''') c), N''[]'') ELSE N''[]'' END,
    [HubWarehouseId] = (SELECT TOP 1 zz.[HubWarehouseId] FROM [ops].[Zones] zz JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = zz.[Id]
        WHERE m.[WarehouseId] = w.[Id] AND zz.[IsActive] = 1 AND zz.[HubWarehouseId] IS NOT NULL AND zz.[HubWarehouseId] <> w.[Id] ORDER BY zz.[Code])
FROM [ops].[Warehouses] w
CROSS APPLY (SELECT
        MAX(CASE WHEN (zz.[Delivery] & 1) = 1 THEN 1 ELSE 0 END) AS [Pickup],
        MAX(CASE WHEN (zz.[Delivery] & 2) = 2 THEN 1 ELSE 0 END) AS [Home],
        MAX(CASE WHEN (zz.[Delivery] & 2) = 2 AND (CASE WHEN ISJSON(zz.[Cities]) = 1 THEN JSON_VALUE(zz.[Cities], ''$[0]'') END) IS NULL THEN 1 ELSE 0 END) AS [Anywhere],
        COUNT(*) AS [Zones]
    FROM [ops].[Zones] zz JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = zz.[Id]
    WHERE m.[WarehouseId] = w.[Id] AND zz.[IsActive] = 1) a
WHERE a.[Zones] > 0;');");

            migrationBuilder.Sql(@"EXEC(N'UPDATE c SET [DestinationWarehouseId] = m.[WarehouseId], [IsActive] = CASE WHEN m.[WarehouseId] IS NULL THEN 0 ELSE c.[IsActive] END
FROM [ops].[RateCards] c JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = c.[ZoneId];');");

            // Two lists that now share a route keep the one in use, then the oldest.
            migrationBuilder.Sql(@"EXEC(N';WITH [ranked] AS (
    SELECT c.[Id], ROW_NUMBER() OVER (PARTITION BY c.[TenantId], c.[ServiceCode], c.[OriginWarehouseId], c.[DestinationWarehouseId], c.[CustomerId] ORDER BY c.[IsActive] DESC, c.[CreatedUtc]) AS [n]
    FROM [ops].[RateCards] c)
DELETE FROM [ops].[RateCards] WHERE [Id] IN (SELECT [Id] FROM [ranked] WHERE [n] > 1);');");

            migrationBuilder.Sql(@"EXEC(N'UPDATE e SET [DestinationWarehouseId] = m.[WarehouseId], [IsActive] = CASE WHEN m.[WarehouseId] IS NULL THEN 0 ELSE e.[IsActive] END
FROM [ops].[ChargeExtras] e JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = e.[ZoneId];');");

            migrationBuilder.Sql(@"EXEC(N'UPDATE q SET [DestinationWarehouseId] = m.[WarehouseId], [OriginWarehouseId] = NULL
FROM [ops].[Quotes] q JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = q.[ZoneId];');");

            migrationBuilder.Sql(@"EXEC(N'UPDATE s SET [DestinationWarehouseId] = m.[WarehouseId]
FROM [ops].[Shipments] s JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = s.[DestinationZoneId];');");

            // Insurance: each version gets the standard terms (who covers, exclusions, claims); the plan becomes the air plan, copied for ocean and ground.
            migrationBuilder.Sql(@"EXEC(N'UPDATE p SET [Versions] = ISNULL((SELECT N''['' + STRING_AGG(JSON_MODIFY(v.[value], ''$.Coverage'', JSON_QUERY(N''{""Cover"":1,""InsurerName"":null,""PolicyNumber"":null,""InsurerContact"":null,""Risks"":3,""Exclusions"":511,""OtherExclusions"":null,""LiabilityPerUnit"":null,""LiabilityPerShipment"":null,""Payout"":2,""DeductibleAmount"":null,""DeductiblePercent"":null,""MaxPerItem"":null,""DamageClaimDays"":7,""LossClaimDays"":21,""Proof"":3,""FiledBy"":1,""TermsEn"":null,""TermsFr"":null,""TermsEs"":null}'')), N'','') WITHIN GROUP (ORDER BY CONVERT(int, v.[key])) + N'']''
    FROM OPENJSON(CASE WHEN ISJSON(p.[Versions]) = 1 THEN p.[Versions] ELSE N''[]'' END) v), N''[]''), [Mode] = 1
FROM [ops].[InsurancePlans] p
WHERE JSON_QUERY(p.[Versions], ''$[0].Coverage'') IS NULL;');");

            migrationBuilder.Sql(@"EXEC(N'INSERT INTO [ops].[InsurancePlans] ([Id], [TenantId], [Mode], [IsActive], [Versions], [CreatedUtc])
SELECT NEWID(), p.[TenantId], m.[Mode], p.[IsActive], p.[Versions], SYSDATETIMEOFFSET()
FROM [ops].[InsurancePlans] p CROSS JOIN (VALUES (2), (3)) m([Mode])
WHERE p.[Mode] = 1 AND NOT EXISTS (SELECT 1 FROM [ops].[InsurancePlans] o WHERE o.[TenantId] = p.[TenantId] AND o.[Mode] = m.[Mode]);');");

            migrationBuilder.Sql(@"EXEC(N'DROP TABLE [ops].[ZoneWarehouseMap];');");

            migrationBuilder.DropColumn(name: "ZoneId", schema: "ops", table: "RateCards");
            migrationBuilder.DropColumn(name: "ZoneId", schema: "ops", table: "Quotes");
            migrationBuilder.DropColumn(name: "ZoneId", schema: "ops", table: "ChargeExtras");
            migrationBuilder.DropColumn(name: "DestinationZoneId", schema: "ops", table: "Shipments");

            migrationBuilder.DropTable(name: "Zones", schema: "ops");

            migrationBuilder.CreateIndex(name: "IX_Warehouses_HubWarehouseId", schema: "ops", table: "Warehouses", column: "HubWarehouseId");
            migrationBuilder.CreateIndex(name: "IX_Shipments_DestinationWarehouseId", schema: "ops", table: "Shipments", column: "DestinationWarehouseId");
            migrationBuilder.CreateIndex(name: "IX_RateCards_OriginWarehouseId", schema: "ops", table: "RateCards", column: "OriginWarehouseId");
            migrationBuilder.CreateIndex(name: "IX_RateCards_DestinationWarehouseId", schema: "ops", table: "RateCards", column: "DestinationWarehouseId");
            migrationBuilder.CreateIndex(name: "IX_Quotes_OriginWarehouseId", schema: "ops", table: "Quotes", column: "OriginWarehouseId");
            migrationBuilder.CreateIndex(name: "IX_Quotes_DestinationWarehouseId", schema: "ops", table: "Quotes", column: "DestinationWarehouseId");
            migrationBuilder.CreateIndex(name: "IX_ChargeExtras_OriginWarehouseId", schema: "ops", table: "ChargeExtras", column: "OriginWarehouseId");
            migrationBuilder.CreateIndex(name: "IX_ChargeExtras_DestinationWarehouseId", schema: "ops", table: "ChargeExtras", column: "DestinationWarehouseId");
            migrationBuilder.CreateIndex(name: "IX_InsurancePlans_TenantId", schema: "ops", table: "InsurancePlans", column: "TenantId");
            migrationBuilder.CreateIndex(name: "IX_RateCards_TenantId_ServiceCode_OriginWarehouseId_DestinationWarehouseId_CustomerId", schema: "ops", table: "RateCards", columns: new[] { "TenantId", "ServiceCode", "OriginWarehouseId", "DestinationWarehouseId", "CustomerId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_InsurancePlans_TenantId_Mode", schema: "ops", table: "InsurancePlans", columns: new[] { "TenantId", "Mode" }, unique: true);

            migrationBuilder.AddForeignKey(name: "FK_ChargeExtras_Warehouses_DestinationWarehouseId", schema: "ops", table: "ChargeExtras", column: "DestinationWarehouseId", principalSchema: "ops", principalTable: "Warehouses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_ChargeExtras_Warehouses_OriginWarehouseId", schema: "ops", table: "ChargeExtras", column: "OriginWarehouseId", principalSchema: "ops", principalTable: "Warehouses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Quotes_Warehouses_DestinationWarehouseId", schema: "ops", table: "Quotes", column: "DestinationWarehouseId", principalSchema: "ops", principalTable: "Warehouses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Quotes_Warehouses_OriginWarehouseId", schema: "ops", table: "Quotes", column: "OriginWarehouseId", principalSchema: "ops", principalTable: "Warehouses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_RateCards_Warehouses_DestinationWarehouseId", schema: "ops", table: "RateCards", column: "DestinationWarehouseId", principalSchema: "ops", principalTable: "Warehouses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_RateCards_Warehouses_OriginWarehouseId", schema: "ops", table: "RateCards", column: "OriginWarehouseId", principalSchema: "ops", principalTable: "Warehouses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Shipments_Warehouses_DestinationWarehouseId", schema: "ops", table: "Shipments", column: "DestinationWarehouseId", principalSchema: "ops", principalTable: "Warehouses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Warehouses_Warehouses_HubWarehouseId", schema: "ops", table: "Warehouses", column: "HubWarehouseId", principalSchema: "ops", principalTable: "Warehouses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChargeExtras_Warehouses_DestinationWarehouseId",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.DropForeignKey(
                name: "FK_ChargeExtras_Warehouses_OriginWarehouseId",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Warehouses_DestinationWarehouseId",
                schema: "ops",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Warehouses_OriginWarehouseId",
                schema: "ops",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_RateCards_Warehouses_DestinationWarehouseId",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropForeignKey(
                name: "FK_RateCards_Warehouses_OriginWarehouseId",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Warehouses_DestinationWarehouseId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Warehouses_HubWarehouseId",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_HubWarehouseId",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_RateCards_DestinationWarehouseId",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropIndex(
                name: "IX_RateCards_TenantId_ServiceCode_OriginWarehouseId_DestinationWarehouseId_CustomerId",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_DestinationWarehouseId",
                schema: "ops",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePlans_TenantId",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePlans_TenantId_Mode",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropIndex(
                name: "IX_ChargeExtras_DestinationWarehouseId",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.DropColumn(
                name: "DeliversHome",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "HomeArea",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "HubWarehouseId",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "OperatedBy",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "PartnerEmail",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "PartnerName",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "PartnerPhone",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "InsuranceAcceptedUtc",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Customs",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "DestinationWarehouseId",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "DestinationWarehouseId",
                schema: "ops",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Mode",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "DestinationWarehouseId",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.DropColumn(
                name: "Mode",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.RenameColumn(
                name: "DestinationWarehouseId",
                schema: "ops",
                table: "Shipments",
                newName: "DestinationZoneId");

            migrationBuilder.RenameIndex(
                name: "IX_Shipments_DestinationWarehouseId",
                schema: "ops",
                table: "Shipments",
                newName: "IX_Shipments_DestinationZoneId");

            migrationBuilder.RenameColumn(
                name: "OriginWarehouseId",
                schema: "ops",
                table: "RateCards",
                newName: "ZoneId");

            migrationBuilder.RenameIndex(
                name: "IX_RateCards_OriginWarehouseId",
                schema: "ops",
                table: "RateCards",
                newName: "IX_RateCards_ZoneId");

            migrationBuilder.RenameColumn(
                name: "OriginWarehouseId",
                schema: "ops",
                table: "Quotes",
                newName: "ZoneId");

            migrationBuilder.RenameIndex(
                name: "IX_Quotes_OriginWarehouseId",
                schema: "ops",
                table: "Quotes",
                newName: "IX_Quotes_ZoneId");

            migrationBuilder.RenameColumn(
                name: "OriginWarehouseId",
                schema: "ops",
                table: "ChargeExtras",
                newName: "ZoneId");

            migrationBuilder.RenameIndex(
                name: "IX_ChargeExtras_OriginWarehouseId",
                schema: "ops",
                table: "ChargeExtras",
                newName: "IX_ChargeExtras_ZoneId");

            migrationBuilder.CreateTable(
                name: "Zones",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Delivery = table.Column<int>(type: "int", nullable: false),
                    HubWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PickupBranchIds = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Zones_Warehouses_HubWarehouseId",
                        column: x => x.HubWarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RateCards_TenantId_ServiceCode_ZoneId_CustomerId",
                schema: "ops",
                table: "RateCards",
                columns: new[] { "TenantId", "ServiceCode", "ZoneId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlans_TenantId",
                schema: "ops",
                table: "InsurancePlans",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Zones_HubWarehouseId",
                schema: "ops",
                table: "Zones",
                column: "HubWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Zones_TenantId",
                schema: "ops",
                table: "Zones",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Zones_TenantId_Code",
                schema: "ops",
                table: "Zones",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChargeExtras_Zones_ZoneId",
                schema: "ops",
                table: "ChargeExtras",
                column: "ZoneId",
                principalSchema: "ops",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Zones_ZoneId",
                schema: "ops",
                table: "Quotes",
                column: "ZoneId",
                principalSchema: "ops",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RateCards_Zones_ZoneId",
                schema: "ops",
                table: "RateCards",
                column: "ZoneId",
                principalSchema: "ops",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Zones_DestinationZoneId",
                schema: "ops",
                table: "Shipments",
                column: "DestinationZoneId",
                principalSchema: "ops",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
