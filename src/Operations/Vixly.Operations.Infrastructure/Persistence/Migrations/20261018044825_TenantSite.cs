using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DraftChangedUtc",
                schema: "ops",
                table: "TenantBrands",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Draft_AccentColour",
                schema: "ops",
                table: "TenantBrands",
                type: "char(7)",
                unicode: false,
                fixedLength: true,
                maxLength: 7,
                nullable: false,
                defaultValue: "#0E7490");

            migrationBuilder.AddColumn<string>(
                name: "Draft_DisplayName",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Draft_FacebookUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Draft_FaviconImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Draft_HeroImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Draft_InstagramUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Draft_LinkedInUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Draft_LogoDarkImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Draft_LogoImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Draft_Preset",
                schema: "ops",
                table: "TenantBrands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Draft_PrimaryColour",
                schema: "ops",
                table: "TenantBrands",
                type: "char(7)",
                unicode: false,
                fixedLength: true,
                maxLength: 7,
                nullable: false,
                defaultValue: "#1D4ED8");

            migrationBuilder.AddColumn<int>(
                name: "Draft_Services",
                schema: "ops",
                table: "TenantBrands",
                type: "int",
                nullable: false,
                defaultValue: 63);

            migrationBuilder.AddColumn<bool>(
                name: "Draft_ShowFaq",
                schema: "ops",
                table: "TenantBrands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Draft_ShowSteps",
                schema: "ops",
                table: "TenantBrands",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Draft_TikTokUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Draft_XUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Draft_YouTubeUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedUtc",
                schema: "ops",
                table: "TenantBrands",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "PublishedVersion",
                schema: "ops",
                table: "TenantBrands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Published_AccentColour",
                schema: "ops",
                table: "TenantBrands",
                type: "char(7)",
                unicode: false,
                fixedLength: true,
                maxLength: 7,
                nullable: false,
                defaultValue: "#0E7490");

            migrationBuilder.AddColumn<string>(
                name: "Published_FacebookUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Published_FaviconImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Published_HeroImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Published_InstagramUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Published_LinkedInUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Published_LogoDarkImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Published_LogoImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Published_Preset",
                schema: "ops",
                table: "TenantBrands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Published_PrimaryColour",
                schema: "ops",
                table: "TenantBrands",
                type: "char(7)",
                unicode: false,
                fixedLength: true,
                maxLength: 7,
                nullable: false,
                defaultValue: "#1D4ED8");

            migrationBuilder.AddColumn<int>(
                name: "Published_Services",
                schema: "ops",
                table: "TenantBrands",
                type: "int",
                nullable: false,
                defaultValue: 63);

            migrationBuilder.AddColumn<bool>(
                name: "Published_ShowFaq",
                schema: "ops",
                table: "TenantBrands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Published_ShowSteps",
                schema: "ops",
                table: "TenantBrands",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Published_TikTokUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Published_XUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Published_YouTubeUrl",
                schema: "ops",
                table: "TenantBrands",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            // Brands written before Slice 1B: the draft starts as the published name.
            // Dynamic SQL: the idempotent script compiles a whole migration as one batch, before the column exists.
            migrationBuilder.Sql("EXEC(N'UPDATE [ops].[TenantBrands] SET [Draft_DisplayName] = [DisplayName] WHERE [Draft_DisplayName] = N''''');");

            migrationBuilder.CreateTable(
                name: "Branches",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_Line1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address_Line2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    OpeningHours = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Side = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ShowOnSite = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CountryContacts",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    WhatsApp = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Hours = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteImages",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", maxLength: 1048576, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteImages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteTexts",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Key = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteTexts_TenantBrands_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "ops",
                        principalTable: "TenantBrands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_Line1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address_Line2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    OpeningHours = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Roles = table.Column<int>(type: "int", nullable: false),
                    AcceptingDeliveries = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ShowOnSite = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouses_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "ops",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TenantId",
                schema: "ops",
                table: "Branches",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TenantId_Code",
                schema: "ops",
                table: "Branches",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CountryContacts_TenantId",
                schema: "ops",
                table: "CountryContacts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteImages_TenantId",
                schema: "ops",
                table: "SiteImages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTexts_BrandId_Stage_Language_Key",
                schema: "ops",
                table: "SiteTexts",
                columns: new[] { "BrandId", "Stage", "Language", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTexts_TenantId",
                schema: "ops",
                table: "SiteTexts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BranchId",
                schema: "ops",
                table: "Warehouses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId",
                schema: "ops",
                table: "Warehouses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId_Code",
                schema: "ops",
                table: "Warehouses",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CountryContacts",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "SiteImages",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "SiteTexts",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Warehouses",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Branches",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "DraftChangedUtc",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_AccentColour",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_DisplayName",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_FacebookUrl",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_FaviconImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_HeroImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_InstagramUrl",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_LinkedInUrl",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_LogoDarkImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_LogoImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_Preset",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_PrimaryColour",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_Services",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_ShowFaq",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_ShowSteps",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_TikTokUrl",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_XUrl",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_YouTubeUrl",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "PublishedUtc",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "PublishedVersion",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_AccentColour",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_FacebookUrl",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_FaviconImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_HeroImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_InstagramUrl",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_LinkedInUrl",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_LogoDarkImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_LogoImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_Preset",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_PrimaryColour",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_Services",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_ShowFaq",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_ShowSteps",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_TikTokUrl",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_XUrl",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_YouTubeUrl",
                schema: "ops",
                table: "TenantBrands");
        }
    }
}
