using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ZonesAndPriceVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ManagerUserId",
                schema: "ops",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                schema: "ops",
                table: "Warehouses",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DestinationZoneId",
                schema: "ops",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ZoneId",
                schema: "ops",
                table: "RateCards",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ZoneId",
                schema: "ops",
                table: "Quotes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Versions",
                schema: "ops",
                table: "ChargeExtras",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ZoneId",
                schema: "ops",
                table: "ChargeExtras",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItemTypes",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Words = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Zones",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    HubWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Delivery = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Cities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PickupBranchIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

            migrationBuilder.AddColumn<string>(
                name: "Versions",
                schema: "ops",
                table: "RateCards",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Versions",
                schema: "ops",
                table: "InsurancePlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Delivery",
                schema: "ops",
                table: "ChargeExtras",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Slice 4B data: converted in place so the Slice 4 prices keep working. The old one-card-per-country index goes first:
            // a country card copied to each zone of its country would otherwise collide with it.
            migrationBuilder.DropIndex(
                name: "IX_RateCards_TenantId_ServiceCode_CountryCode_CustomerId",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.Sql(@"EXEC(N'INSERT INTO [ops].[Zones] ([Id], [TenantId], [Code], [Name], [CountryCode], [HubWarehouseId], [Delivery], [IsActive], [Cities], [PickupBranchIds], [CreatedUtc])
SELECT b.[Id], b.[TenantId], b.[Code], b.[Name], b.[Address_CountryCode],
    (SELECT TOP 1 w.[Id] FROM [ops].[Warehouses] w WHERE w.[BranchId] = b.[Id] AND (w.[Roles] & 2) = 2 AND w.[Status] = 0 ORDER BY w.[Code]),
    COALESCE((SELECT TOP 1 s.[DeliveryModes] FROM [ops].[TenantSettings] s WHERE s.[TenantId] = b.[TenantId]), 1) | 1,
    CASE WHEN b.[Status] = 0 THEN 1 ELSE 0 END,
    N''[]'',
    N''[""'' + LOWER(CONVERT(nvarchar(36), b.[Id])) + N''""]'',
    SYSDATETIMEOFFSET()
FROM [ops].[Branches] b
WHERE b.[Side] <> 0 AND NOT EXISTS (SELECT 1 FROM [ops].[Zones] z WHERE z.[TenantId] = b.[TenantId] AND z.[Code] = b.[Code]);');");

            migrationBuilder.Sql(@"EXEC(N'UPDATE s SET [DestinationZoneId] = s.[DestinationBranchId]
FROM [ops].[Shipments] s
WHERE s.[DestinationBranchId] IS NOT NULL AND EXISTS (SELECT 1 FROM [ops].[Zones] z WHERE z.[Id] = s.[DestinationBranchId]);');");

            // A home delivery takes the country's zone only when there is exactly one in use; otherwise the customer chooses it.
            migrationBuilder.Sql(@"EXEC(N'UPDATE s SET [DestinationZoneId] = (SELECT MIN(CONVERT(nvarchar(36), z.[Id])) FROM [ops].[Zones] z
    WHERE z.[TenantId] = s.[TenantId] AND z.[CountryCode] = s.[DestinationCountryCode] AND z.[IsActive] = 1)
FROM [ops].[Shipments] s
WHERE s.[DestinationZoneId] IS NULL AND s.[DestinationCountryCode] IS NOT NULL
    AND (SELECT COUNT(*) FROM [ops].[Zones] z WHERE z.[TenantId] = s.[TenantId] AND z.[CountryCode] = s.[DestinationCountryCode] AND z.[IsActive] = 1) = 1;');");

            migrationBuilder.Sql(@"EXEC(N'INSERT INTO [ops].[ItemTypes] ([Id], [TenantId], [Name], [Words], [IsActive], [CreatedUtc])
SELECT NEWID(), x.[TenantId], x.[Name], NULL, 1, SYSDATETIMEOFFSET()
FROM (SELECT DISTINCT c.[TenantId], LEFT(LTRIM(RTRIM(j.[Name])), 80) AS [Name]
      FROM [ops].[RateCards] c CROSS APPLY OPENJSON(c.[Items]) WITH ([Name] nvarchar(200) ''$.Name'') j
      WHERE c.[Items] IS NOT NULL AND ISJSON(c.[Items]) = 1 AND LEN(LTRIM(RTRIM(ISNULL(j.[Name], N'''')))) > 0) x
WHERE NOT EXISTS (SELECT 1 FROM [ops].[ItemTypes] t WHERE t.[TenantId] = x.[TenantId] AND t.[Name] = x.[Name]);');");

            migrationBuilder.Sql(@"EXEC(N'UPDATE c SET [Versions] = (
    SELECT LOWER(CONVERT(nvarchar(36), NEWID())) AS [Id], CONVERT(nvarchar(40), c.[CreatedUtc], 127) AS [EffectiveFromUtc],
        c.[MinimumCharge] AS [MinimumCharge], c.[ServiceFee] AS [ServiceFee], c.[ConsolidationFee] AS [ConsolidationFee],
        JSON_QUERY((SELECT b.[From], b.[Kind], b.[Amount], b.[Base] FROM (
            SELECT CAST(0 AS decimal(12, 2)) AS [From], 1 AS [Kind], CAST(c.[SmallPackageFee] AS decimal(14, 4)) AS [Amount], CAST(NULL AS decimal(12, 2)) AS [Base] WHERE c.[SmallPackageFee] IS NOT NULL
            UNION ALL
            SELECT CAST(s.[Start] AS decimal(12, 2)), 2, CAST(ISNULL((SELECT TOP 1 o.[UnitPrice]
                FROM OPENJSON(CASE WHEN ISJSON(c.[Bands]) = 1 THEN c.[Bands] ELSE N''[]'' END) WITH ([FromWeight] decimal(12, 2) ''$.FromWeight'', [UnitPrice] decimal(14, 4) ''$.UnitPrice'') o
                WHERE o.[FromWeight] <= s.[Start] ORDER BY o.[FromWeight] DESC), c.[UnitPrice]) AS decimal(14, 4)), NULL
            FROM (SELECT CASE WHEN c.[SmallPackageFee] IS NOT NULL THEN FLOOR(c.[SmallPackageLimit]) + 1 ELSE 0 END AS [Start]) s
            UNION ALL
            SELECT j.[FromWeight], 2, j.[UnitPrice], NULL
            FROM OPENJSON(CASE WHEN ISJSON(c.[Bands]) = 1 THEN c.[Bands] ELSE N''[]'' END) WITH ([FromWeight] decimal(12, 2) ''$.FromWeight'', [UnitPrice] decimal(14, 4) ''$.UnitPrice'') j
            WHERE j.[FromWeight] > CASE WHEN c.[SmallPackageFee] IS NOT NULL THEN FLOOR(c.[SmallPackageLimit]) + 1 ELSE 0 END) b
            ORDER BY b.[From] FOR JSON PATH, INCLUDE_NULL_VALUES)) AS [Bands],
        JSON_QUERY(ISNULL((SELECT LOWER(CONVERT(nvarchar(36), t.[Id])) AS [ItemTypeId], j.[Price]
            FROM OPENJSON(CASE WHEN ISJSON(c.[Items]) = 1 THEN c.[Items] ELSE N''[]'' END) WITH ([Name] nvarchar(200) ''$.Name'', [Price] decimal(12, 2) ''$.Price'') j
            JOIN [ops].[ItemTypes] t ON t.[TenantId] = c.[TenantId] AND t.[Name] = LEFT(LTRIM(RTRIM(j.[Name])), 80)
            FOR JSON PATH), N''[]'')) AS [Items]
    FOR JSON PATH, INCLUDE_NULL_VALUES)
FROM [ops].[RateCards] c;');");

            // A country card for a country without a destination branch keeps its prices in a zone of that country, not in use until the company sets it up.
            migrationBuilder.Sql(@"EXEC(N'INSERT INTO [ops].[Zones] ([Id], [TenantId], [Code], [Name], [CountryCode], [HubWarehouseId], [Delivery], [IsActive], [Cities], [PickupBranchIds], [CreatedUtc])
SELECT NEWID(), x.[TenantId], LEFT(N''ZONE-'' + x.[CountryCode], 20), x.[CountryCode], x.[CountryCode], NULL, 2, 0, N''[]'', N''[]'', SYSDATETIMEOFFSET()
FROM (SELECT DISTINCT c.[TenantId], c.[CountryCode] FROM [ops].[RateCards] c WHERE c.[CountryCode] IS NOT NULL) x
WHERE NOT EXISTS (SELECT 1 FROM [ops].[Zones] z WHERE z.[TenantId] = x.[TenantId] AND z.[CountryCode] = x.[CountryCode])
    AND NOT EXISTS (SELECT 1 FROM [ops].[Zones] z WHERE z.[TenantId] = x.[TenantId] AND z.[Code] = LEFT(N''ZONE-'' + x.[CountryCode], 20));');");

            migrationBuilder.Sql(@"EXEC(N'UPDATE c SET [ZoneId] = (SELECT TOP 1 z.[Id] FROM [ops].[Zones] z WHERE z.[TenantId] = c.[TenantId] AND z.[CountryCode] = c.[CountryCode] ORDER BY z.[Code])
FROM [ops].[RateCards] c WHERE c.[CountryCode] IS NOT NULL;');");

            migrationBuilder.Sql(@"EXEC(N'INSERT INTO [ops].[RateCards] ([Id], [TenantId], [ServiceCode], [CustomerId], [Basis], [IsActive], [Versions], [ZoneId], [CreatedUtc], [CountryCode], [UnitPrice])
SELECT NEWID(), c.[TenantId], c.[ServiceCode], c.[CustomerId], c.[Basis], c.[IsActive], JSON_MODIFY(c.[Versions], ''$[0].Id'', LOWER(CONVERT(nvarchar(36), NEWID()))), z.[Id], c.[CreatedUtc], NULL, c.[UnitPrice]
FROM [ops].[RateCards] c JOIN [ops].[Zones] z ON z.[TenantId] = c.[TenantId] AND z.[CountryCode] = c.[CountryCode] AND z.[Id] <> c.[ZoneId]
WHERE c.[CountryCode] IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM [ops].[RateCards] o WHERE o.[TenantId] = c.[TenantId] AND o.[ServiceCode] = c.[ServiceCode] AND o.[ZoneId] = z.[Id]
        AND ((o.[CustomerId] IS NULL AND c.[CustomerId] IS NULL) OR o.[CustomerId] = c.[CustomerId]));');");

            migrationBuilder.Sql(@"EXEC(N';WITH [ranked] AS (
    SELECT c.[Id], ROW_NUMBER() OVER (PARTITION BY c.[TenantId], c.[ServiceCode], c.[ZoneId], c.[CustomerId] ORDER BY CASE WHEN c.[CountryCode] IS NULL THEN 0 ELSE 1 END, c.[CreatedUtc]) AS [n]
    FROM [ops].[RateCards] c)
DELETE FROM [ops].[RateCards] WHERE [Id] IN (SELECT [Id] FROM [ranked] WHERE [n] > 1);');");

            migrationBuilder.Sql(@"EXEC(N'UPDATE e SET [Versions] = (SELECT LOWER(CONVERT(nvarchar(36), NEWID())) AS [Id], CONVERT(nvarchar(40), e.[CreatedUtc], 127) AS [EffectiveFromUtc],
    e.[Amount] AS [Amount], e.[Calculation] AS [Calculation], e.[Per] AS [Per] FOR JSON PATH)
FROM [ops].[ChargeExtras] e;');");

            migrationBuilder.Sql(@"EXEC(N'UPDATE p SET [Versions] = (SELECT LOWER(CONVERT(nvarchar(36), NEWID())) AS [Id], CONVERT(nvarchar(40), p.[CreatedUtc], 127) AS [EffectiveFromUtc],
    p.[Method] AS [Method], p.[Percent] AS [Percent], p.[MinimumPremium] AS [MinimumPremium], p.[MaxValue] AS [MaxValue],
    JSON_QUERY(CASE WHEN ISJSON(p.[Bands]) = 1 THEN p.[Bands] ELSE N''[]'' END) AS [Bands]
    FOR JSON PATH, INCLUDE_NULL_VALUES)
FROM [ops].[InsurancePlans] p;');");

            migrationBuilder.DropColumn(
                name: "Bands",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "Items",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "ConsolidationFee",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "MinimumCharge",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "ServiceFee",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "SmallPackageFee",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "SmallPackageLimit",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "Bands",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "Method",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "MinimumPremium",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "Percent",
                schema: "ops",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "Amount",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.DropColumn(
                name: "Calculation",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.DropColumn(
                name: "Per",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_ManagerUserId",
                schema: "ops",
                table: "Warehouses",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DestinationZoneId",
                schema: "ops",
                table: "Shipments",
                column: "DestinationZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_RateCards_TenantId_ServiceCode_ZoneId_CustomerId",
                schema: "ops",
                table: "RateCards",
                columns: new[] { "TenantId", "ServiceCode", "ZoneId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RateCards_ZoneId",
                schema: "ops",
                table: "RateCards",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_ZoneId",
                schema: "ops",
                table: "Quotes",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeExtras_ZoneId",
                schema: "ops",
                table: "ChargeExtras",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTypes_TenantId",
                schema: "ops",
                table: "ItemTypes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTypes_TenantId_Name",
                schema: "ops",
                table: "ItemTypes",
                columns: new[] { "TenantId", "Name" },
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

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Users_ManagerUserId",
                schema: "ops",
                table: "Warehouses",
                column: "ManagerUserId",
                principalSchema: "ops",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChargeExtras_Zones_ZoneId",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Zones_ZoneId",
                schema: "ops",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_RateCards_Zones_ZoneId",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Zones_DestinationZoneId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Users_ManagerUserId",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropTable(
                name: "ItemTypes",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Zones",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_ManagerUserId",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_DestinationZoneId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_RateCards_TenantId_ServiceCode_ZoneId_CustomerId",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropIndex(
                name: "IX_RateCards_ZoneId",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_ZoneId",
                schema: "ops",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_ChargeExtras_ZoneId",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.DropColumn(
                name: "ManagerUserId",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DestinationZoneId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                schema: "ops",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                schema: "ops",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Versions",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                schema: "ops",
                table: "ChargeExtras");

            migrationBuilder.RenameColumn(
                name: "Versions",
                schema: "ops",
                table: "RateCards",
                newName: "Items");

            migrationBuilder.RenameColumn(
                name: "Versions",
                schema: "ops",
                table: "InsurancePlans",
                newName: "Bands");

            migrationBuilder.RenameColumn(
                name: "Delivery",
                schema: "ops",
                table: "ChargeExtras",
                newName: "Per");

            migrationBuilder.AddColumn<string>(
                name: "Bands",
                schema: "ops",
                table: "RateCards",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConsolidationFee",
                schema: "ops",
                table: "RateCards",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                schema: "ops",
                table: "RateCards",
                type: "char(2)",
                unicode: false,
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumCharge",
                schema: "ops",
                table: "RateCards",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFee",
                schema: "ops",
                table: "RateCards",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SmallPackageFee",
                schema: "ops",
                table: "RateCards",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SmallPackageLimit",
                schema: "ops",
                table: "RateCards",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                schema: "ops",
                table: "RateCards",
                type: "decimal(14,4)",
                precision: 14,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxValue",
                schema: "ops",
                table: "InsurancePlans",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Method",
                schema: "ops",
                table: "InsurancePlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumPremium",
                schema: "ops",
                table: "InsurancePlans",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Percent",
                schema: "ops",
                table: "InsurancePlans",
                type: "decimal(6,3)",
                precision: 6,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                schema: "ops",
                table: "ChargeExtras",
                type: "decimal(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Calculation",
                schema: "ops",
                table: "ChargeExtras",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RateCards_TenantId_ServiceCode_CountryCode_CustomerId",
                schema: "ops",
                table: "RateCards",
                columns: new[] { "TenantId", "ServiceCode", "CountryCode", "CustomerId" },
                unique: true);
        }
    }
}
