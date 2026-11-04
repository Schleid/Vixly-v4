using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitCardPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OtherTenders",
                schema: "ops",
                table: "CardCharges",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ZelleTermsAccepted",
                schema: "ops",
                table: "CardCharges",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtherTenders",
                schema: "ops",
                table: "CardCharges");

            migrationBuilder.DropColumn(
                name: "ZelleTermsAccepted",
                schema: "ops",
                table: "CardCharges");
        }
    }
}
