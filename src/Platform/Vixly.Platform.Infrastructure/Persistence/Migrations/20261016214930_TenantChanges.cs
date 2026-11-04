using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantChangeRequests",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedByReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SubmittedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DecidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    PreviousEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    PreviousLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PreviousPhone = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    PreviousTradingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PreviousCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreviousCountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    PreviousAddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PreviousAddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PreviousPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PreviousStateProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProposedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    ProposedLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProposedPhone = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    ProposedTradingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProposedCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProposedCountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    ProposedAddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProposedAddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProposedPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProposedStateProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantChangeRequests_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantChangeRequests_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantChangeProofs",
                schema: "platform",
                columns: table => new
                {
                    PlatformTenantChangeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", maxLength: 10485760, nullable: false),
                    SizeBytes = table.Column<int>(type: "int", nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantChangeProofs", x => x.PlatformTenantChangeRequestId);
                    table.ForeignKey(
                        name: "FK_TenantChangeProofs_TenantChangeRequests_PlatformTenantChangeRequestId",
                        column: x => x.PlatformTenantChangeRequestId,
                        principalSchema: "platform",
                        principalTable: "TenantChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantChangeRequests_DecidedByUserId",
                schema: "platform",
                table: "TenantChangeRequests",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantChangeRequests_OnePending",
                schema: "platform",
                table: "TenantChangeRequests",
                column: "PlatformTenantId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TenantChangeRequests_PlatformTenantId_CreatedUtc",
                schema: "platform",
                table: "TenantChangeRequests",
                columns: new[] { "PlatformTenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantChangeRequests_Status_CreatedUtc",
                schema: "platform",
                table: "TenantChangeRequests",
                columns: new[] { "Status", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantChangeProofs",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantChangeRequests",
                schema: "platform");
        }
    }
}
