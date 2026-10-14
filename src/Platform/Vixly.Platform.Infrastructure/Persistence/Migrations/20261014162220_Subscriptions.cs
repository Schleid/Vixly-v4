using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Subscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plans",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plans_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantLimitOverrides",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EndsUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SetByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemovedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RemovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantLimitOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantLimitOverrides_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantLimitOverrides_Users_RemovedByUserId",
                        column: x => x.RemovedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantLimitOverrides_Users_SetByUserId",
                        column: x => x.SetByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanTranslations",
                schema: "platform",
                columns: table => new
                {
                    PlatformPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Language = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanTranslations", x => new { x.PlatformPlanId, x.Language });
                    table.ForeignKey(
                        name: "FK_PlanTranslations_Plans_PlatformPlanId",
                        column: x => x.PlatformPlanId,
                        principalSchema: "platform",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanVersions",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    MonthlyPricePerLicence = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AnnualPricePerLicence = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SetupFee = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    MinimumLicences = table.Column<int>(type: "int", nullable: false),
                    TrialDays = table.Column<int>(type: "int", nullable: true),
                    PublishedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanVersions_Plans_PlatformPlanId",
                        column: x => x.PlatformPlanId,
                        principalSchema: "platform",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanVersions_Users_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanVersionFeatures",
                schema: "platform",
                columns: table => new
                {
                    Feature = table.Column<int>(type: "int", nullable: false),
                    PlanVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanVersionFeatures", x => new { x.PlanVersionId, x.Feature });
                    table.ForeignKey(
                        name: "FK_PlanVersionFeatures_PlanVersions_PlanVersionId",
                        column: x => x.PlanVersionId,
                        principalSchema: "platform",
                        principalTable: "PlanVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanVersionLimits",
                schema: "platform",
                columns: table => new
                {
                    Kind = table.Column<int>(type: "int", nullable: false),
                    PlanVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanVersionLimits", x => new { x.PlanVersionId, x.Kind });
                    table.ForeignKey(
                        name: "FK_PlanVersionLimits_PlanVersions_PlanVersionId",
                        column: x => x.PlanVersionId,
                        principalSchema: "platform",
                        principalTable: "PlanVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Term = table.Column<int>(type: "int", nullable: false),
                    Licences = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusChangedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TrialEndsUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodEndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SetupFeeCharged = table.Column<bool>(type: "bit", nullable: false),
                    CancelAtPeriodEnd = table.Column<bool>(type: "bit", nullable: false),
                    CancellationRequestedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StatusBeforeSuspension = table.Column<int>(type: "int", nullable: true),
                    EndedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndReason = table.Column<int>(type: "int", nullable: true),
                    ScheduledPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledTerm = table.Column<int>(type: "int", nullable: true),
                    ScheduledLicences = table.Column<int>(type: "int", nullable: true),
                    ScheduledEffectiveUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ScheduledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_PlanVersions_PlanVersionId",
                        column: x => x.PlanVersionId,
                        principalSchema: "platform",
                        principalTable: "PlanVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Plans_PlatformPlanId",
                        column: x => x.PlatformPlanId,
                        principalSchema: "platform",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionEvents",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Arguments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionEvents_Subscriptions_PlatformSubscriptionId",
                        column: x => x.PlatformSubscriptionId,
                        principalSchema: "platform",
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Code",
                schema: "platform",
                table: "Plans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plans_CreatedByUserId",
                schema: "platform",
                table: "Plans",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Status_IsPublic_DisplayOrder",
                schema: "platform",
                table: "Plans",
                columns: new[] { "Status", "IsPublic", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_OneDraft",
                schema: "platform",
                table: "PlanVersions",
                column: "PlatformPlanId",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_PlatformPlanId_Number",
                schema: "platform",
                table: "PlanVersions",
                columns: new[] { "PlatformPlanId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_PublishedByUserId",
                schema: "platform",
                table: "PlanVersions",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionEvents_PlatformSubscriptionId_CreatedUtc",
                schema: "platform",
                table: "SubscriptionEvents",
                columns: new[] { "PlatformSubscriptionId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionEvents_PlatformTenantId_CreatedUtc",
                schema: "platform",
                table: "SubscriptionEvents",
                columns: new[] { "PlatformTenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_OneCurrentPerTenant",
                schema: "platform",
                table: "Subscriptions",
                column: "PlatformTenantId",
                unique: true,
                filter: "[Status] <> 5");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanVersionId",
                schema: "platform",
                table: "Subscriptions",
                column: "PlanVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlatformPlanId_Status",
                schema: "platform",
                table: "Subscriptions",
                columns: new[] { "PlatformPlanId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status_PeriodEndUtc",
                schema: "platform",
                table: "Subscriptions",
                columns: new[] { "Status", "PeriodEndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status_TrialEndsUtc",
                schema: "platform",
                table: "Subscriptions",
                columns: new[] { "Status", "TrialEndsUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantLimitOverrides_OneOpen",
                schema: "platform",
                table: "TenantLimitOverrides",
                columns: new[] { "PlatformTenantId", "Kind" },
                unique: true,
                filter: "[RemovedUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TenantLimitOverrides_RemovedByUserId",
                schema: "platform",
                table: "TenantLimitOverrides",
                column: "RemovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantLimitOverrides_SetByUserId",
                schema: "platform",
                table: "TenantLimitOverrides",
                column: "SetByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanTranslations",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PlanVersionFeatures",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PlanVersionLimits",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "SubscriptionEvents",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantLimitOverrides",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Subscriptions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PlanVersions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Plans",
                schema: "platform");
        }
    }
}
