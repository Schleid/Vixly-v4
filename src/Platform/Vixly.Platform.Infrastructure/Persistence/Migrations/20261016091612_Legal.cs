using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Legal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupportEmail",
                schema: "platform",
                table: "CompanyProfile",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportPhone",
                schema: "platform",
                table: "CompanyProfile",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LegalDocuments",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AcceptanceRequired = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeadlineUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalDocuments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegalDocuments_Users_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LegalDocumentFiles",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformLegalDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Language = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", maxLength: 10485760, nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    SizeBytes = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalDocumentFiles_LegalDocuments_PlatformLegalDocumentId",
                        column: x => x.PlatformLegalDocumentId,
                        principalSchema: "platform",
                        principalTable: "LegalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegalDocumentParts",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncludedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PlatformLegalDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalDocumentParts_LegalDocuments_IncludedDocumentId",
                        column: x => x.IncludedDocumentId,
                        principalSchema: "platform",
                        principalTable: "LegalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegalDocumentParts_LegalDocuments_PlatformLegalDocumentId",
                        column: x => x.PlatformLegalDocumentId,
                        principalSchema: "platform",
                        principalTable: "LegalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantTermsAcceptances",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformLegalDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    Language = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    AcceptedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ActorRole = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    ClientIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    PlatformTenantRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Evidence = table.Column<byte[]>(type: "varbinary(max)", maxLength: 5242880, nullable: true),
                    EvidenceContentType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantTermsAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantTermsAcceptances_LegalDocuments_PlatformLegalDocumentId",
                        column: x => x.PlatformLegalDocumentId,
                        principalSchema: "platform",
                        principalTable: "LegalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantTermsAcceptances_TenantRegistrations_PlatformTenantRegistrationId",
                        column: x => x.PlatformTenantRegistrationId,
                        principalSchema: "platform",
                        principalTable: "TenantRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantTermsAcceptances_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantTermsAcceptances_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TermsReminders",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformLegalDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    SentByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermsReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TermsReminders_LegalDocuments_PlatformLegalDocumentId",
                        column: x => x.PlatformLegalDocumentId,
                        principalSchema: "platform",
                        principalTable: "LegalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TermsReminders_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TermsReminders_Users_SentByUserId",
                        column: x => x.SentByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentFiles_PlatformLegalDocumentId_Language",
                schema: "platform",
                table: "LegalDocumentFiles",
                columns: new[] { "PlatformLegalDocumentId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentParts_IncludedDocumentId",
                schema: "platform",
                table: "LegalDocumentParts",
                column: "IncludedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentParts_PlatformLegalDocumentId",
                schema: "platform",
                table: "LegalDocumentParts",
                column: "PlatformLegalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_CreatedByUserId",
                schema: "platform",
                table: "LegalDocuments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_Kind_PublishedUtc",
                schema: "platform",
                table: "LegalDocuments",
                columns: new[] { "Kind", "PublishedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_Kind_Version",
                schema: "platform",
                table: "LegalDocuments",
                columns: new[] { "Kind", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_OnePublished",
                schema: "platform",
                table: "LegalDocuments",
                column: "Kind",
                unique: true,
                filter: "[Status] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_PublishedByUserId",
                schema: "platform",
                table: "LegalDocuments",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantTermsAcceptances_PlatformLegalDocumentId",
                schema: "platform",
                table: "TenantTermsAcceptances",
                column: "PlatformLegalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantTermsAcceptances_PlatformTenantId_PlatformLegalDocumentId",
                schema: "platform",
                table: "TenantTermsAcceptances",
                columns: new[] { "PlatformTenantId", "PlatformLegalDocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantTermsAcceptances_PlatformTenantRegistrationId",
                schema: "platform",
                table: "TenantTermsAcceptances",
                column: "PlatformTenantRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantTermsAcceptances_RecordedByUserId",
                schema: "platform",
                table: "TenantTermsAcceptances",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TermsReminders_OncePerStep",
                schema: "platform",
                table: "TermsReminders",
                columns: new[] { "PlatformTenantId", "PlatformLegalDocumentId", "Step" },
                unique: true,
                filter: "[Step] <> 20");

            migrationBuilder.CreateIndex(
                name: "IX_TermsReminders_PlatformLegalDocumentId",
                schema: "platform",
                table: "TermsReminders",
                column: "PlatformLegalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TermsReminders_PlatformTenantId_CreatedUtc",
                schema: "platform",
                table: "TermsReminders",
                columns: new[] { "PlatformTenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TermsReminders_SentByUserId",
                schema: "platform",
                table: "TermsReminders",
                column: "SentByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegalDocumentFiles",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "LegalDocumentParts",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantTermsAcceptances",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TermsReminders",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "LegalDocuments",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "SupportEmail",
                schema: "platform",
                table: "CompanyProfile");

            migrationBuilder.DropColumn(
                name: "SupportPhone",
                schema: "platform",
                table: "CompanyProfile");
        }
    }
}
