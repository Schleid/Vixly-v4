using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContactPhones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phones",
                schema: "ops",
                table: "CountryContacts",
                type: "varchar(252)",
                unicode: false,
                maxLength: 252,
                nullable: false,
                defaultValue: "");

            // Keep the one number each contact had. Dynamic SQL: the idempotent script compiles a migration as one batch.
            migrationBuilder.Sql("EXEC(N'UPDATE [ops].[CountryContacts] SET [Phones] = [Phone] WHERE [Phones] = N''''');");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "ops",
                table: "CountryContacts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Phones",
                schema: "ops",
                table: "CountryContacts");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "ops",
                table: "CountryContacts",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
