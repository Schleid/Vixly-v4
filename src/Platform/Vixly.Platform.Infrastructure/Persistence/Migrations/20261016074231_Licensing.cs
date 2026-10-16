using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Licensing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceLinks",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VoidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Tries = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceLinks_TenantRegistrations_PlatformTenantRegistrationId",
                        column: x => x.PlatformTenantRegistrationId,
                        principalSchema: "platform",
                        principalTable: "TenantRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceLinks_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Proof = table.Column<int>(type: "int", nullable: false),
                    PublicKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    SecretHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    SecretIssuedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PreviousSecretHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    PreviousSecretValidUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SeatHeld = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: true),
                    RequestedByReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestedByName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ApprovedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReleaseRequestedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedByTenant = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantLicensing",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatsInUse = table.Column<int>(type: "int", nullable: false),
                    LastDeviceNumber = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantLicensing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantLicensing_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceChallenges",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceChallenges_Devices_PlatformDeviceId",
                        column: x => x.PlatformDeviceId,
                        principalSchema: "platform",
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceEvents",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ActorReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Arguments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceEvents_Devices_PlatformDeviceId",
                        column: x => x.PlatformDeviceId,
                        principalSchema: "platform",
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceEvents_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceEvents_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceChallenges_ChallengeHash",
                schema: "platform",
                table: "DeviceChallenges",
                column: "ChallengeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceChallenges_PlatformDeviceId",
                schema: "platform",
                table: "DeviceChallenges",
                column: "PlatformDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_ActorUserId",
                schema: "platform",
                table: "DeviceEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_PlatformDeviceId_CreatedUtc",
                schema: "platform",
                table: "DeviceEvents",
                columns: new[] { "PlatformDeviceId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_PlatformTenantId_EventCode_CreatedUtc",
                schema: "platform",
                table: "DeviceEvents",
                columns: new[] { "PlatformTenantId", "EventCode", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLinks_PlatformTenantId_Kind",
                schema: "platform",
                table: "DeviceLinks",
                columns: new[] { "PlatformTenantId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLinks_PlatformTenantRegistrationId",
                schema: "platform",
                table: "DeviceLinks",
                column: "PlatformTenantRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLinks_TokenHash",
                schema: "platform",
                table: "DeviceLinks",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_PlatformTenantId_Number",
                schema: "platform",
                table: "Devices",
                columns: new[] { "PlatformTenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_PlatformTenantId_Status",
                schema: "platform",
                table: "Devices",
                columns: new[] { "PlatformTenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_SecretHash",
                schema: "platform",
                table: "Devices",
                column: "SecretHash",
                filter: "[SecretHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Status_CreatedUtc",
                schema: "platform",
                table: "Devices",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantLicensing_PlatformTenantId",
                schema: "platform",
                table: "TenantLicensing",
                column: "PlatformTenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceChallenges",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "DeviceEvents",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "DeviceLinks",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantLicensing",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Devices",
                schema: "platform");
        }
    }
}
