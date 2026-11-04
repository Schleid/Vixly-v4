using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AskForReviews",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ConsentRecordedUtc",
                schema: "ops",
                table: "SiteTestimonials",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<Guid>(
                name: "ConsentRecordedByUserId",
                schema: "ops",
                table: "SiteTestimonials",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CustomerAgreedUtc",
                schema: "ops",
                table: "SiteTestimonials",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                schema: "ops",
                table: "SiteTestimonials",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "HiddenReason",
                schema: "ops",
                table: "SiteTestimonials",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "NameStyle",
                schema: "ops",
                table: "SiteTestimonials",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedUtc",
                schema: "ops",
                table: "SiteTestimonials",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedUtc",
                schema: "ops",
                table: "SiteTestimonials",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewInviteDismissedUtc",
                schema: "ops",
                table: "Customers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTestimonials_TenantId_CustomerId_One",
                schema: "ops",
                table: "SiteTestimonials",
                columns: new[] { "TenantId", "CustomerId" },
                unique: true,
                filter: "[CustomerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTestimonials_TenantId_ReviewedUtc",
                schema: "ops",
                table: "SiteTestimonials",
                columns: new[] { "TenantId", "ReviewedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SiteTestimonials_TenantId_CustomerId_One",
                schema: "ops",
                table: "SiteTestimonials");

            migrationBuilder.DropIndex(
                name: "IX_SiteTestimonials_TenantId_ReviewedUtc",
                schema: "ops",
                table: "SiteTestimonials");

            migrationBuilder.DropColumn(
                name: "AskForReviews",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "CustomerAgreedUtc",
                schema: "ops",
                table: "SiteTestimonials");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                schema: "ops",
                table: "SiteTestimonials");

            migrationBuilder.DropColumn(
                name: "HiddenReason",
                schema: "ops",
                table: "SiteTestimonials");

            migrationBuilder.DropColumn(
                name: "NameStyle",
                schema: "ops",
                table: "SiteTestimonials");

            migrationBuilder.DropColumn(
                name: "ReviewedUtc",
                schema: "ops",
                table: "SiteTestimonials");

            migrationBuilder.DropColumn(
                name: "SubmittedUtc",
                schema: "ops",
                table: "SiteTestimonials");

            migrationBuilder.DropColumn(
                name: "ReviewInviteDismissedUtc",
                schema: "ops",
                table: "Customers");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ConsentRecordedUtc",
                schema: "ops",
                table: "SiteTestimonials",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ConsentRecordedByUserId",
                schema: "ops",
                table: "SiteTestimonials",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
