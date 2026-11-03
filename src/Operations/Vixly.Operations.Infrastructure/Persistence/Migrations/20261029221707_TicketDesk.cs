using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TicketDesk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_TenantId_CustomerId_TrackingNumber",
                schema: "ops",
                table: "Tickets");

            migrationBuilder.AddColumn<Guid>(
                name: "AgentUserId",
                schema: "ops",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscalationsSent",
                schema: "ops",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpenedBy",
                schema: "ops",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentId",
                schema: "ops",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TakenByUserId",
                schema: "ops",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TakenUtc",
                schema: "ops",
                table: "Tickets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Topic",
                schema: "ops",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketResolveDays",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "TicketTakeHours",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TenantId_CustomerId_TrackingNumber",
                schema: "ops",
                table: "Tickets",
                columns: new[] { "TenantId", "CustomerId", "TrackingNumber" },
                unique: true,
                filter: "[Status] <> 3 AND [OpenedBy] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TenantId_Topic_Status_CreatedUtc",
                schema: "ops",
                table: "Tickets",
                columns: new[] { "TenantId", "Topic", "Status", "CreatedUtc" });

            // Owner, 2026-10-29 (ticket desk): the call tickets of package C become tickets of the desk (topic Other, opened by an agent),
            // at the package's warehouse, else the shipment's destination, else its origin; the closing note becomes a staff message.
            migrationBuilder.Sql(@"EXEC(N'
INSERT INTO [ops].[Tickets] ([Id], [TenantId], [CustomerId], [WarehouseId], [Status], [Carrier], [TrackingNumber], [Contents], [ParcelId], [ResolvedUtc],
    [Messages], [CreatedUtc], [LastModifiedUtc], [Topic], [OpenedBy], [AgentUserId], [ShipmentId], [TakenByUserId], [TakenUtc], [EscalationsSent])
SELECT r.[Id], r.[TenantId], r.[CustomerId], COALESCE(p.[WarehouseId], s.[DestinationWarehouseId], s.[OriginWarehouseId]),
    CASE WHEN r.[Status] = 2 THEN 3 ELSE 1 END, N'''', LEFT(r.[Reference], 40), N'''', r.[ParcelId], r.[DoneUtc],
    (SELECT m.[FromStaff], m.[UserId], m.[Text], m.[SentUtc]
     FROM (SELECT CAST(1 AS bit) AS [FromStaff], r.[AgentUserId] AS [UserId], r.[Text] AS [Text], r.[CreatedUtc] AS [SentUtc]
           UNION ALL
           SELECT CAST(1 AS bit), r.[DoneByUserId], r.[DoneNote], r.[DoneUtc] WHERE r.[DoneNote] IS NOT NULL AND r.[DoneUtc] IS NOT NULL) AS m
     ORDER BY m.[SentUtc] FOR JSON PATH),
    r.[CreatedUtc], r.[LastModifiedUtc], 4, 1, r.[AgentUserId], r.[ShipmentId], r.[DoneByUserId], r.[DoneUtc], 0
FROM [ops].[SupportRequests] r
LEFT JOIN [ops].[Parcels] p ON p.[Id] = r.[ParcelId]
LEFT JOIN [ops].[Shipments] s ON s.[Id] = r.[ShipmentId]
WHERE COALESCE(p.[WarehouseId], s.[DestinationWarehouseId], s.[OriginWarehouseId]) IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM [ops].[Tickets] t WHERE t.[Id] = r.[Id])')");

            migrationBuilder.DropTable(
                name: "SupportRequests",
                schema: "ops");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_TenantId_CustomerId_TrackingNumber",
                schema: "ops",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_TenantId_Topic_Status_CreatedUtc",
                schema: "ops",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AgentUserId",
                schema: "ops",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EscalationsSent",
                schema: "ops",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "OpenedBy",
                schema: "ops",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ShipmentId",
                schema: "ops",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TakenByUserId",
                schema: "ops",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TakenUtc",
                schema: "ops",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Topic",
                schema: "ops",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TicketResolveDays",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "TicketTakeHours",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.CreateTable(
                name: "SupportRequests",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoneByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DoneNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DoneUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reference = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportRequests_Users_AgentUserId",
                        column: x => x.AgentUserId,
                        principalSchema: "ops",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TenantId_CustomerId_TrackingNumber",
                schema: "ops",
                table: "Tickets",
                columns: new[] { "TenantId", "CustomerId", "TrackingNumber" },
                unique: true,
                filter: "[Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_AgentUserId",
                schema: "ops",
                table: "SupportRequests",
                column: "AgentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_CustomerId",
                schema: "ops",
                table: "SupportRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_TenantId",
                schema: "ops",
                table: "SupportRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_TenantId_Status_CreatedUtc",
                schema: "ops",
                table: "SupportRequests",
                columns: new[] { "TenantId", "Status", "CreatedUtc" });
        }
    }
}
