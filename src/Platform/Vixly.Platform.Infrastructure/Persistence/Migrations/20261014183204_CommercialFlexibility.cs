using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CommercialFlexibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanVersions_OneDraft",
                schema: "platform",
                table: "PlanVersions");

            migrationBuilder.AddColumn<decimal>(
                name: "SetupFeeOverride",
                schema: "platform",
                table: "Subscriptions",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedByUserId",
                schema: "platform",
                table: "PlanVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "platform",
                table: "PlanVersions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                schema: "platform",
                table: "PlanVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedUtc",
                schema: "platform",
                table: "PlanVersions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Offers",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    BoundEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PlatformPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Term = table.Column<int>(type: "int", nullable: false),
                    Licences = table.Column<int>(type: "int", nullable: false),
                    DiscountKind = table.Column<int>(type: "int", nullable: true),
                    DiscountValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    DiscountDuration = table.Column<int>(type: "int", nullable: true),
                    DiscountPeriods = table.Column<int>(type: "int", nullable: true),
                    DiscountEndsUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SetupFeeOverride = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    StartTrial = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsesAllowed = table.Column<int>(type: "int", nullable: false),
                    Uses = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PreparedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IssuedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevokedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_Plans_PlatformPlanId",
                        column: x => x.PlatformPlanId,
                        principalSchema: "platform",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_Users_PreparedByUserId",
                        column: x => x.PreparedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_Users_RevokedByUserId",
                        column: x => x.RevokedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OneTimeCharges",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CancelledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneTimeCharges_Subscriptions_PlatformSubscriptionId",
                        column: x => x.PlatformSubscriptionId,
                        principalSchema: "platform",
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OneTimeCharges_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OneTimeCharges_Users_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    DiscountKind = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountDuration = table.Column<int>(type: "int", nullable: false),
                    DiscountPeriods = table.Column<int>(type: "int", nullable: true),
                    DiscountEndsUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ValidFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValidToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Term = table.Column<int>(type: "int", nullable: true),
                    AtRegistration = table.Column<bool>(type: "bit", nullable: false),
                    ByStaff = table.Column<bool>(type: "bit", nullable: false),
                    RedemptionLimit = table.Column<int>(type: "int", nullable: true),
                    Redemptions = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DesignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Promotions_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Promotions_Users_DesignedByUserId",
                        column: x => x.DesignedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Promotions_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesSettings",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    MaxPercentWithoutApproval = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NoEndWithoutApproval = table.Column<bool>(type: "bit", nullable: false),
                    MaxOfferValidityDays = table.Column<int>(type: "int", nullable: true),
                    DiscountEndNoticeDays = table.Column<int>(type: "int", nullable: true),
                    PromotionCodesAtRegistration = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesSettings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionDiscounts",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscountKind = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountDuration = table.Column<int>(type: "int", nullable: false),
                    DiscountPeriods = table.Column<int>(type: "int", nullable: true),
                    DiscountEndsUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PeriodsUsed = table.Column<int>(type: "int", nullable: false),
                    LastPeriodStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndReason = table.Column<int>(type: "int", nullable: true),
                    EndNoticeSentUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionDiscounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionDiscounts_Subscriptions_PlatformSubscriptionId",
                        column: x => x.PlatformSubscriptionId,
                        principalSchema: "platform",
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubscriptionDiscounts_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionDiscounts_Users_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfferCharges",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferCharges_Offers_OfferId",
                        column: x => x.OfferId,
                        principalSchema: "platform",
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionNames",
                schema: "platform",
                columns: table => new
                {
                    Language = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionNames", x => new { x.PromotionId, x.Language });
                    table.ForeignKey(
                        name: "FK_PromotionNames_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "platform",
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionPlans",
                schema: "platform",
                columns: table => new
                {
                    PlatformPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionPlans", x => new { x.PromotionId, x.PlatformPlanId });
                    table.ForeignKey(
                        name: "FK_PromotionPlans_Plans_PlatformPlanId",
                        column: x => x.PlatformPlanId,
                        principalSchema: "platform",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionPlans_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "platform",
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesFixedLimits",
                schema: "platform",
                columns: table => new
                {
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    SalesSettingsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesFixedLimits", x => new { x.SalesSettingsId, x.CurrencyCode });
                    table.ForeignKey(
                        name: "FK_SalesFixedLimits_SalesSettings_SalesSettingsId",
                        column: x => x.SalesSettingsId,
                        principalSchema: "platform",
                        principalTable: "SalesSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_OneDraft",
                schema: "platform",
                table: "PlanVersions",
                column: "PlatformPlanId",
                unique: true,
                filter: "[Status] IN (0, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_RejectedByUserId",
                schema: "platform",
                table: "PlanVersions",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_SubmittedByUserId",
                schema: "platform",
                table: "PlanVersions",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferCharges_OfferId",
                schema: "platform",
                table: "OfferCharges",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_DecidedByUserId",
                schema: "platform",
                table: "Offers",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_PlatformPlanId",
                schema: "platform",
                table: "Offers",
                column: "PlatformPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_PreparedByUserId",
                schema: "platform",
                table: "Offers",
                column: "PreparedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_RevokedByUserId",
                schema: "platform",
                table: "Offers",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_Status_ExpiresUtc",
                schema: "platform",
                table: "Offers",
                columns: new[] { "Status", "ExpiresUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_TokenHash",
                schema: "platform",
                table: "Offers",
                column: "TokenHash",
                unique: true,
                filter: "[TokenHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeCharges_AddedByUserId",
                schema: "platform",
                table: "OneTimeCharges",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeCharges_CancelledByUserId",
                schema: "platform",
                table: "OneTimeCharges",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeCharges_PlatformSubscriptionId",
                schema: "platform",
                table: "OneTimeCharges",
                column: "PlatformSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeCharges_Status",
                schema: "platform",
                table: "OneTimeCharges",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionPlans_PlatformPlanId",
                schema: "platform",
                table: "PromotionPlans",
                column: "PlatformPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Code",
                schema: "platform",
                table: "Promotions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_DecidedByUserId",
                schema: "platform",
                table: "Promotions",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_DesignedByUserId",
                schema: "platform",
                table: "Promotions",
                column: "DesignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Status",
                schema: "platform",
                table: "Promotions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_SubmittedByUserId",
                schema: "platform",
                table: "Promotions",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettings_Key",
                schema: "platform",
                table: "SalesSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettings_UpdatedByUserId",
                schema: "platform",
                table: "SalesSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionDiscounts_DecidedByUserId",
                schema: "platform",
                table: "SubscriptionDiscounts",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionDiscounts_GrantedByUserId",
                schema: "platform",
                table: "SubscriptionDiscounts",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionDiscounts_OnePending",
                schema: "platform",
                table: "SubscriptionDiscounts",
                column: "PlatformSubscriptionId",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionDiscounts_PlatformSubscriptionId_Status",
                schema: "platform",
                table: "SubscriptionDiscounts",
                columns: new[] { "PlatformSubscriptionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionDiscounts_Source_SourceId",
                schema: "platform",
                table: "SubscriptionDiscounts",
                columns: new[] { "Source", "SourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PlanVersions_Users_RejectedByUserId",
                schema: "platform",
                table: "PlanVersions",
                column: "RejectedByUserId",
                principalSchema: "platform",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanVersions_Users_SubmittedByUserId",
                schema: "platform",
                table: "PlanVersions",
                column: "SubmittedByUserId",
                principalSchema: "platform",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanVersions_Users_RejectedByUserId",
                schema: "platform",
                table: "PlanVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanVersions_Users_SubmittedByUserId",
                schema: "platform",
                table: "PlanVersions");

            migrationBuilder.DropTable(
                name: "OfferCharges",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "OneTimeCharges",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PromotionNames",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PromotionPlans",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "SalesFixedLimits",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "SubscriptionDiscounts",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Offers",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Promotions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "SalesSettings",
                schema: "platform");

            migrationBuilder.DropIndex(
                name: "IX_PlanVersions_OneDraft",
                schema: "platform",
                table: "PlanVersions");

            migrationBuilder.DropIndex(
                name: "IX_PlanVersions_RejectedByUserId",
                schema: "platform",
                table: "PlanVersions");

            migrationBuilder.DropIndex(
                name: "IX_PlanVersions_SubmittedByUserId",
                schema: "platform",
                table: "PlanVersions");

            migrationBuilder.DropColumn(
                name: "SetupFeeOverride",
                schema: "platform",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                schema: "platform",
                table: "PlanVersions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "platform",
                table: "PlanVersions");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                schema: "platform",
                table: "PlanVersions");

            migrationBuilder.DropColumn(
                name: "SubmittedUtc",
                schema: "platform",
                table: "PlanVersions");

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_OneDraft",
                schema: "platform",
                table: "PlanVersions",
                column: "PlatformPlanId",
                unique: true,
                filter: "[Status] = 0");
        }
    }
}
