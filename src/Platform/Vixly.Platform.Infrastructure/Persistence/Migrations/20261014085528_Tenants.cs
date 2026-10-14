using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    StreamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    LeasedUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    CurrentSlug = table.Column<string>(type: "varchar(63)", unicode: false, maxLength: 63, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusChangedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActivatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefaultLanguage = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    TimeZone = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    AccountManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CompanyEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyPhone = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    TradingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StateProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_Users_AccountManagerUserId",
                        column: x => x.AccountManagerUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tenants_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantContacts",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Phone = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantContacts_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantLifecycleEvents",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Arguments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantLifecycleEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantLifecycleEvents_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantNotes",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantNotes_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantNotes_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantRegistrations",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    OwnerPhone = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    Language = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    TimeZone = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "varchar(63)", unicode: false, maxLength: 63, nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccessTokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    AccessExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TermsVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrivacyVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConsentIpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    LastVerificationSentUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VerificationWindowStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VerificationSendsInWindow = table.Column<int>(type: "int", nullable: false),
                    EmailVerifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CompanyEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyPhone = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    TradingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StateProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantRegistrations_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantRegistrations_Users_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantSuspensions",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SuspendedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolutionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSuspensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSuspensions_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantTags",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantTags_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PortalSlugs",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "varchar(63)", unicode: false, maxLength: 63, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlatformTenantRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeldUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalSlugs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortalSlugs_TenantRegistrations_PlatformTenantRegistrationId",
                        column: x => x.PlatformTenantRegistrationId,
                        principalSchema: "platform",
                        principalTable: "TenantRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PortalSlugs_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationTokens",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrationTokens_TenantRegistrations_PlatformTenantRegistrationId",
                        column: x => x.PlatformTenantRegistrationId,
                        principalSchema: "platform",
                        principalTable: "TenantRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_NextAttemptUtc_CreatedUtc",
                schema: "platform",
                table: "OutboxMessages",
                columns: new[] { "NextAttemptUtc", "CreatedUtc" },
                filter: "[ProcessedUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_StreamId_CreatedUtc",
                schema: "platform",
                table: "OutboxMessages",
                columns: new[] { "StreamId", "CreatedUtc" },
                filter: "[ProcessedUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PortalSlugs_PlatformTenantId",
                schema: "platform",
                table: "PortalSlugs",
                column: "PlatformTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PortalSlugs_PlatformTenantRegistrationId",
                schema: "platform",
                table: "PortalSlugs",
                column: "PlatformTenantRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_PortalSlugs_Slug",
                schema: "platform",
                table: "PortalSlugs",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationTokens_PlatformTenantRegistrationId_UsedUtc_RevokedUtc",
                schema: "platform",
                table: "RegistrationTokens",
                columns: new[] { "PlatformTenantRegistrationId", "UsedUtc", "RevokedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationTokens_TokenHash",
                schema: "platform",
                table: "RegistrationTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantContacts_Email",
                schema: "platform",
                table: "TenantContacts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_TenantContacts_PlatformTenantId_Role",
                schema: "platform",
                table: "TenantContacts",
                columns: new[] { "PlatformTenantId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantLifecycleEvents_PlatformTenantId_CreatedUtc",
                schema: "platform",
                table: "TenantLifecycleEvents",
                columns: new[] { "PlatformTenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantNotes_AuthorUserId",
                schema: "platform",
                table: "TenantNotes",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantNotes_PlatformTenantId_CreatedUtc",
                schema: "platform",
                table: "TenantNotes",
                columns: new[] { "PlatformTenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantRegistrations_AccessTokenHash",
                schema: "platform",
                table: "TenantRegistrations",
                column: "AccessTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantRegistrations_CancelledByUserId",
                schema: "platform",
                table: "TenantRegistrations",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRegistrations_OwnerEmail",
                schema: "platform",
                table: "TenantRegistrations",
                column: "OwnerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRegistrations_PlatformTenantId",
                schema: "platform",
                table: "TenantRegistrations",
                column: "PlatformTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRegistrations_Status_CreatedUtc",
                schema: "platform",
                table: "TenantRegistrations",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_AccountManagerUserId",
                schema: "platform",
                table: "Tenants",
                column: "AccountManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CreatedByUserId",
                schema: "platform",
                table: "Tenants",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CurrentSlug",
                schema: "platform",
                table: "Tenants",
                column: "CurrentSlug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Status_CreatedUtc",
                schema: "platform",
                table: "Tenants",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantCode",
                schema: "platform",
                table: "Tenants",
                column: "TenantCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSuspensions_PlatformTenantId_ResolvedUtc",
                schema: "platform",
                table: "TenantSuspensions",
                columns: new[] { "PlatformTenantId", "ResolvedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantTags_PlatformTenantId_Value",
                schema: "platform",
                table: "TenantTags",
                columns: new[] { "PlatformTenantId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantTags_Value",
                schema: "platform",
                table: "TenantTags",
                column: "Value");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PortalSlugs",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "RegistrationTokens",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantContacts",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantLifecycleEvents",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantNotes",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantSuspensions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantTags",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantRegistrations",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "platform");
        }
    }
}
