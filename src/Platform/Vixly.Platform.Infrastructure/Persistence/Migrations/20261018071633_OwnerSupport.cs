using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OwnerSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupportRequestId",
                schema: "platform",
                table: "DeviceLinks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OwnerSupportRequests",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IdentityCheck = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReplaceDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DecidedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerSupportRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnerSupportRequests_Devices_ReplaceDeviceId",
                        column: x => x.ReplaceDeviceId,
                        principalSchema: "platform",
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerSupportRequests_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLinks_SupportRequestId",
                schema: "platform",
                table: "DeviceLinks",
                column: "SupportRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerSupportRequests_PlatformTenantId_Status",
                schema: "platform",
                table: "OwnerSupportRequests",
                columns: new[] { "PlatformTenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerSupportRequests_ReplaceDeviceId",
                schema: "platform",
                table: "OwnerSupportRequests",
                column: "ReplaceDeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceLinks_OwnerSupportRequests_SupportRequestId",
                schema: "platform",
                table: "DeviceLinks",
                column: "SupportRequestId",
                principalSchema: "platform",
                principalTable: "OwnerSupportRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceLinks_OwnerSupportRequests_SupportRequestId",
                schema: "platform",
                table: "DeviceLinks");

            migrationBuilder.DropTable(
                name: "OwnerSupportRequests",
                schema: "platform");

            migrationBuilder.DropIndex(
                name: "IX_DeviceLinks_SupportRequestId",
                schema: "platform",
                table: "DeviceLinks");

            migrationBuilder.DropColumn(
                name: "SupportRequestId",
                schema: "platform",
                table: "DeviceLinks");
        }
    }
}
