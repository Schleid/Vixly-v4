using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BranchesAsCountries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Side",
                schema: "ops",
                table: "Branches");

            migrationBuilder.AddColumn<string>(
                name: "TemplateKey",
                schema: "ops",
                table: "Roles",
                type: "varchar(40)",
                unicode: false,
                maxLength: 40,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address_Line1",
                schema: "ops",
                table: "Branches",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Address_CountryCode",
                schema: "ops",
                table: "Branches",
                type: "char(2)",
                unicode: false,
                fixedLength: true,
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "char(2)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Address_City",
                schema: "ops",
                table: "Branches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                schema: "ops",
                table: "Branches",
                type: "char(2)",
                unicode: false,
                fixedLength: true,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            // Owner, 2026-10-29: each branch's country comes from its address (in EXEC, so the idempotent script compiles before the column exists).
            migrationBuilder.Sql("EXEC(N'UPDATE [ops].[Branches] SET [CountryCode] = [Address_CountryCode] WHERE [CountryCode] = N'''' AND [Address_CountryCode] IS NOT NULL');");

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                schema: "ops",
                table: "Branches",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalNotes",
                schema: "ops",
                table: "Branches",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                schema: "ops",
                table: "Branches",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemplateKey",
                schema: "ops",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                schema: "ops",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "LegalName",
                schema: "ops",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "LegalNotes",
                schema: "ops",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                schema: "ops",
                table: "Branches");

            migrationBuilder.AlterColumn<string>(
                name: "Address_Line1",
                schema: "ops",
                table: "Branches",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address_CountryCode",
                schema: "ops",
                table: "Branches",
                type: "char(2)",
                unicode: false,
                fixedLength: true,
                maxLength: 2,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "char(2)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address_City",
                schema: "ops",
                table: "Branches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Side",
                schema: "ops",
                table: "Branches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
