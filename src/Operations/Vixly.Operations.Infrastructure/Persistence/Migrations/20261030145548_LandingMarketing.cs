using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LandingMarketing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Draft_ShowDelivered",
                schema: "ops",
                table: "TenantBrands",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Draft_ShowRates",
                schema: "ops",
                table: "TenantBrands",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Draft_ShowTestimonials",
                schema: "ops",
                table: "TenantBrands",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Published_ShowDelivered",
                schema: "ops",
                table: "TenantBrands",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Published_ShowRates",
                schema: "ops",
                table: "TenantBrands",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Published_ShowTestimonials",
                schema: "ops",
                table: "TenantBrands",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "SiteTestimonials",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Place = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Quote = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ConsentRecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsentRecordedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Shown = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTestimonials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteTestimonials_TenantId",
                schema: "ops",
                table: "SiteTestimonials",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTestimonials_TenantId_SortOrder",
                schema: "ops",
                table: "SiteTestimonials",
                columns: new[] { "TenantId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteTestimonials",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "Draft_ShowDelivered",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_ShowRates",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_ShowTestimonials",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_ShowDelivered",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_ShowRates",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_ShowTestimonials",
                schema: "ops",
                table: "TenantBrands");
        }
    }
}
