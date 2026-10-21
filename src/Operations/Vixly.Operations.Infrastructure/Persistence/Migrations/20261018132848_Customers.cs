using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Customers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerPrefix",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(6)",
                unicode: false,
                maxLength: 6,
                nullable: false,
                defaultValue: "C");

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StatusChangedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreditAllowed = table.Column<bool>(type: "bit", nullable: false),
                    CreditReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreditChangedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PasswordHash = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    FailedSignInCount = table.Column<int>(type: "int", nullable: false),
                    LockoutEndsUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EmailVerifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSignInUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSessions",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    LastSeenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndReason = table.Column<int>(type: "int", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSessions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerTokens",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerTokens_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId",
                schema: "ops",
                table: "Customers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Code",
                schema: "ops",
                table: "Customers",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Email",
                schema: "ops",
                table: "Customers",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_LastName_FirstName",
                schema: "ops",
                table: "Customers",
                columns: new[] { "TenantId", "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Phone",
                schema: "ops",
                table: "Customers",
                columns: new[] { "TenantId", "Phone" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_CustomerId",
                schema: "ops",
                table: "CustomerSessions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_TenantId",
                schema: "ops",
                table: "CustomerSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_TenantId_CustomerId_EndedUtc",
                schema: "ops",
                table: "CustomerSessions",
                columns: new[] { "TenantId", "CustomerId", "EndedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_TokenHash",
                schema: "ops",
                table: "CustomerSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTokens_CustomerId",
                schema: "ops",
                table: "CustomerTokens",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTokens_TenantId",
                schema: "ops",
                table: "CustomerTokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTokens_TenantId_CustomerId_Purpose",
                schema: "ops",
                table: "CustomerTokens",
                columns: new[] { "TenantId", "CustomerId", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTokens_TokenHash",
                schema: "ops",
                table: "CustomerTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSessions",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "CustomerTokens",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "CustomerPrefix",
                schema: "ops",
                table: "TenantSettings");
        }
    }
}
