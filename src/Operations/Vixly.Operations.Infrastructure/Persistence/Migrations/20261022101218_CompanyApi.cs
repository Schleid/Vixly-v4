using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompanyApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiEvents",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    KeyId = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    SecretHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Hint = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastUsedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEndpoints",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Events = table.Column<int>(type: "int", nullable: false),
                    SecretProtected = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: false),
                    SecretHint = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    DisabledForFailures = table.Column<bool>(type: "bit", nullable: false),
                    LastSuccessUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailingSinceUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingAfter = table.Column<long>(type: "bigint", nullable: false),
                    CustomersAfter = table.Column<long>(type: "bigint", nullable: false),
                    InvoicesAfter = table.Column<long>(type: "bigint", nullable: false),
                    PaymentsAfter = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEndpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveries",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndpointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastAttemptUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastStatusCode = table.Column<int>(type: "int", nullable: true),
                    LastError = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    LeaseUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookDeliveries_WebhookEndpoints_EndpointId",
                        column: x => x.EndpointId,
                        principalSchema: "ops",
                        principalTable: "WebhookEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_TenantId_CreatedUtc",
                schema: "ops",
                table: "TrackingEvents",
                columns: new[] { "TenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_CreatedUtc",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_CreatedUtc",
                schema: "ops",
                table: "Payments",
                columns: new[] { "TenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_CreatedUtc",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_CreatedUtc",
                schema: "ops",
                table: "Invoices",
                columns: new[] { "TenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_CreatedUtc",
                schema: "ops",
                table: "Customers",
                columns: new[] { "TenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiEvents_CreatedUtc",
                schema: "ops",
                table: "ApiEvents",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ApiEvents_TenantId",
                schema: "ops",
                table: "ApiEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiEvents_TenantId_Kind_Version",
                schema: "ops",
                table: "ApiEvents",
                columns: new[] { "TenantId", "Kind", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyId",
                schema: "ops",
                table: "ApiKeys",
                column: "KeyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_TenantId",
                schema: "ops",
                table: "ApiKeys",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_EndpointId_EventId",
                schema: "ops",
                table: "WebhookDeliveries",
                columns: new[] { "EndpointId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_TenantId",
                schema: "ops",
                table: "WebhookDeliveries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_TenantId_CreatedUtc",
                schema: "ops",
                table: "WebhookDeliveries",
                columns: new[] { "TenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_TenantId_Status_NextAttemptUtc",
                schema: "ops",
                table: "WebhookDeliveries",
                columns: new[] { "TenantId", "Status", "NextAttemptUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEndpoints_TenantId",
                schema: "ops",
                table: "WebhookEndpoints",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEndpoints_TenantId_Active",
                schema: "ops",
                table: "WebhookEndpoints",
                columns: new[] { "TenantId", "Active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiEvents",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "ApiKeys",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "WebhookDeliveries",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "WebhookEndpoints",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_TrackingEvents_TenantId_CreatedUtc",
                schema: "ops",
                table: "TrackingEvents");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId_CreatedUtc",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId_CreatedUtc",
                schema: "ops",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_TenantId_CreatedUtc",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_CreatedUtc",
                schema: "ops",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_CreatedUtc",
                schema: "ops",
                table: "Customers");
        }
    }
}
