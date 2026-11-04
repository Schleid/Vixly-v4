using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartnerAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PartnerHandlesMoney",
                schema: "ops",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PartnerWarehouseId",
                schema: "ops",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PartnerWarehouseId",
                schema: "ops",
                table: "Users",
                column: "PartnerWarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Warehouses_PartnerWarehouseId",
                schema: "ops",
                table: "Users",
                column: "PartnerWarehouseId",
                principalSchema: "ops",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Warehouses_PartnerWarehouseId",
                schema: "ops",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PartnerWarehouseId",
                schema: "ops",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PartnerHandlesMoney",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "PartnerWarehouseId",
                schema: "ops",
                table: "Users");
        }
    }
}
