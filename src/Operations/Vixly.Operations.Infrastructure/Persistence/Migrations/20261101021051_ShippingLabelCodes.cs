using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShippingLabelCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabelCode",
                schema: "ops",
                table: "ShippingServiceSetups",
                type: "char(3)",
                unicode: false,
                fixedLength: true,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShippingServiceSetups_LabelCode",
                schema: "ops",
                table: "ShippingServiceSetups",
                sql: "[LabelCode] IS NULL OR (LEN([LabelCode]) = 3 AND [LabelCode] COLLATE Latin1_General_BIN NOT LIKE '%[^A-Z]%')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ShippingServiceSetups_LabelCode",
                schema: "ops",
                table: "ShippingServiceSetups");

            migrationBuilder.DropColumn(
                name: "LabelCode",
                schema: "ops",
                table: "ShippingServiceSetups");
        }
    }
}
