using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupportAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOutside",
                schema: "ops",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OutsideDays",
                schema: "ops",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "OutsideEndsOn",
                schema: "ops",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OutsideFromMinute",
                schema: "ops",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OutsideToMinute",
                schema: "ops",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupportRequests",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reference = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    AgentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DoneByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DoneUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DoneNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportRequests_Users_AgentUserId",
                        column: x => x.AgentUserId,
                        principalSchema: "ops",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_AgentUserId",
                schema: "ops",
                table: "SupportRequests",
                column: "AgentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_CustomerId",
                schema: "ops",
                table: "SupportRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_TenantId",
                schema: "ops",
                table: "SupportRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_TenantId_Status_CreatedUtc",
                schema: "ops",
                table: "SupportRequests",
                columns: new[] { "TenantId", "Status", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportRequests",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "IsOutside",
                schema: "ops",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OutsideDays",
                schema: "ops",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OutsideEndsOn",
                schema: "ops",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OutsideFromMinute",
                schema: "ops",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OutsideToMinute",
                schema: "ops",
                table: "Users");
        }
    }
}
