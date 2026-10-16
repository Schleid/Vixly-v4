using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Collections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BlockedUtc",
                schema: "platform",
                table: "Subscriptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusBeforeBlock",
                schema: "platform",
                table: "Subscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CollectionCases",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    OldestDueUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PlannedBlockUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BlockedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NoAccessUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExtensionUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExtensionsUsed = table.Column<int>(type: "int", nullable: false),
                    ExtensionEndingNoticeSent = table.Column<bool>(type: "bit", nullable: false),
                    Held = table.Column<bool>(type: "bit", nullable: false),
                    LastStepUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastCheckedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CloseReason = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionCases_Subscriptions_PlatformSubscriptionId",
                        column: x => x.PlatformSubscriptionId,
                        principalSchema: "platform",
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionCases_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CollectionsSettings",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Reminder1Day = table.Column<int>(type: "int", nullable: false),
                    Reminder2Day = table.Column<int>(type: "int", nullable: false),
                    FinalWarningDay = table.Column<int>(type: "int", nullable: false),
                    BlockDay = table.Column<int>(type: "int", nullable: false),
                    RetryDays = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    MinimumNoticeDays = table.Column<int>(type: "int", nullable: false),
                    BlockedReadOnlyDays = table.Column<int>(type: "int", nullable: true),
                    ExtensionMaxDays = table.Column<int>(type: "int", nullable: false),
                    ExtensionsPerCase = table.Column<int>(type: "int", nullable: false),
                    PendingPaymentReadOnlyDays = table.Column<int>(type: "int", nullable: false),
                    LongBlockDays = table.Column<int>(type: "int", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionsSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionsSettings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CollectionCaseEvents",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformCollectionCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RetryKey = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    Arguments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionCaseEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionCaseEvents_CollectionCases_PlatformCollectionCaseId",
                        column: x => x.PlatformCollectionCaseId,
                        principalSchema: "platform",
                        principalTable: "CollectionCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionCaseEvents_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionCaseEvents_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCaseEvents_ActorUserId",
                schema: "platform",
                table: "CollectionCaseEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCaseEvents_PlatformCollectionCaseId_CreatedUtc",
                schema: "platform",
                table: "CollectionCaseEvents",
                columns: new[] { "PlatformCollectionCaseId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCaseEvents_PlatformTenantId",
                schema: "platform",
                table: "CollectionCaseEvents",
                column: "PlatformTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCaseEvents_RetryKey",
                schema: "platform",
                table: "CollectionCaseEvents",
                column: "RetryKey",
                unique: true,
                filter: "[RetryKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCases_OneOpenCasePerTenant",
                schema: "platform",
                table: "CollectionCases",
                column: "PlatformTenantId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCases_PlatformSubscriptionId",
                schema: "platform",
                table: "CollectionCases",
                column: "PlatformSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCases_Status_Stage",
                schema: "platform",
                table: "CollectionCases",
                columns: new[] { "Status", "Stage" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionsSettings_Key",
                schema: "platform",
                table: "CollectionsSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionsSettings_UpdatedByUserId",
                schema: "platform",
                table: "CollectionsSettings",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionCaseEvents",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "CollectionsSettings",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "CollectionCases",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "BlockedUtc",
                schema: "platform",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "StatusBeforeBlock",
                schema: "platform",
                table: "Subscriptions");
        }
    }
}
