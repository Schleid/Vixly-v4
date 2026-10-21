-- =========================================================
-- FILE: vixly-v4-database.sql
--
-- PURPOSE:
-- The one script that creates or upgrades the Vixly V4
-- database: database, schemas, tables, indexes, migration
-- history and (optionally) the SQL logins and grants.
--
-- HOW TO RUN
-- - SSMS: open this file, connect to the server
--   (developer machine: GDELL\SQLEXPRESS), press Execute.
--   SQLCMD mode is not needed.
-- - Command line:
--     sqlcmd -S GDELL\SQLEXPRESS -E -C -i vixly-v4-database.sql
--
-- SAFE TO RUN AGAIN
-- - Every step checks first. Migrations already applied are
--   skipped (recorded in each schema's __EFMigrationsHistory).
--   Run the newest version of this file after every pull.
--
-- SECTIONS
--   0. Settings (edit here only)
--   1. Database
--   2. Platform plane, schema [platform] (EF migrations, generated)
--   3. Operations plane, schema [ops] (EF migrations, generated)
--   4. Customer plane, schema [customer] (EF migrations, generated)
--   5. Logins and grants (only for the planes whose passwords are given)
--
-- IMPORTANT NOTES
-- - Sections 2, 3 and 4 are generated from the EF migrations
--   and rebuilt with every new migration; never edit them by hand.
-- - Never save real passwords in this file (S-11). Type them
--   in section 0 before running and do not commit them.
-- - Shared setup: every plane in one database, VixlyV4, each
--   in its own schema (D-1, ADR-0010). For the separate
--   setup, run each plane's section against its own database.
-- =========================================================

-- Session options that indexed and filtered objects need.
-- SSMS sets them by default; sqlcmd and some tools do not.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO

-- =========================================================
-- 0. SETTINGS
-- Leave the passwords empty on a developer machine that
-- uses Windows authentication: section 5 is then skipped.
-- Staging and production: fill the passwords of the planes
-- being set up, run, then clear them. A plane whose
-- passwords are empty keeps its logins as they are.
-- =========================================================
EXEC sys.sp_set_session_context @key = N'vixly.platform_app_pwd',    @value = N'';
EXEC sys.sp_set_session_context @key = N'vixly.platform_migr_pwd',   @value = N'';
EXEC sys.sp_set_session_context @key = N'vixly.operations_app_pwd',  @value = N'';
EXEC sys.sp_set_session_context @key = N'vixly.operations_migr_pwd', @value = N'';
EXEC sys.sp_set_session_context @key = N'vixly.customer_app_pwd',    @value = N'';
GO

-- =========================================================
-- 1. DATABASE
-- =========================================================
IF DB_ID(N'VixlyV4') IS NULL
BEGIN
    CREATE DATABASE [VixlyV4];
    PRINT N'Created database VixlyV4.';
END
ELSE
    PRINT N'Database VixlyV4 already exists.';
GO

USE [VixlyV4];
GO

-- =========================================================
-- 2. PLATFORM PLANE (schema [platform])
-- Generated from the EF migrations of
-- Vixly.Platform.Infrastructure: InitialIdentity, SecondFactorsSessionsAudit, DataProtectionKeys, SignInChallenges, SessionStepUpFailures, PasskeyCeremonies, Tenants, Subscriptions, CommercialFlexibility, Billing, Payments, Collections, Licensing, Zelle, Legal, Contracts, Leads, TenantChanges, OwnerSupport
-- =========================================================
IF OBJECT_ID(N'[platform].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'platform') IS NULL EXEC(N'CREATE SCHEMA [platform];');
    CREATE TABLE [platform].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    IF SCHEMA_ID(N'platform') IS NULL EXEC(N'CREATE SCHEMA [platform];');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE TABLE [platform].[Departments] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE TABLE [platform].[Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [SystemKey] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE TABLE [platform].[Users] (
        [Id] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [EmployeeNumber] nvarchar(50) NULL,
        [PhoneNumber] nvarchar(16) NULL,
        [PreferredLanguage] nvarchar(8) NOT NULL,
        [AuthorityLevel] int NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE TABLE [platform].[RolePermissions] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRoleId] uniqueidentifier NOT NULL,
        [PermissionCode] varchar(100) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RolePermissions_Roles_PlatformRoleId] FOREIGN KEY ([PlatformRoleId]) REFERENCES [platform].[Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE TABLE [platform].[RoleAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformUserId] uniqueidentifier NOT NULL,
        [PlatformRoleId] uniqueidentifier NOT NULL,
        [AssignedByUserId] uniqueidentifier NOT NULL,
        [ExpiresUtc] datetimeoffset NULL,
        [RevokedByUserId] uniqueidentifier NULL,
        [RevokedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_RoleAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleAssignments_Roles_PlatformRoleId] FOREIGN KEY ([PlatformRoleId]) REFERENCES [platform].[Roles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RoleAssignments_Users_PlatformUserId] FOREIGN KEY ([PlatformUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE TABLE [platform].[UserAccounts] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformUserId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [PasswordHash] nvarchar(200) NULL,
        [FailedSignInCount] int NOT NULL,
        [SignInLockoutEndsUtc] datetimeoffset NULL,
        [LastSignInUtc] datetimeoffset NULL,
        [ActivatedUtc] datetimeoffset NULL,
        [StatusReason] nvarchar(500) NULL,
        [StatusChangedByUserId] uniqueidentifier NULL,
        [StatusChangedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_UserAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserAccounts_Users_PlatformUserId] FOREIGN KEY ([PlatformUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE TABLE [platform].[UserDepartments] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformUserId] uniqueidentifier NOT NULL,
        [PlatformDepartmentId] uniqueidentifier NOT NULL,
        [IsPrimary] bit NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_UserDepartments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserDepartments_Departments_PlatformDepartmentId] FOREIGN KEY ([PlatformDepartmentId]) REFERENCES [platform].[Departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserDepartments_Users_PlatformUserId] FOREIGN KEY ([PlatformUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_Name] ON [platform].[Departments] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_RoleAssignments_PlatformRoleId] ON [platform].[RoleAssignments] ([PlatformRoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_RoleAssignments_PlatformUserId_PlatformRoleId] ON [platform].[RoleAssignments] ([PlatformUserId], [PlatformRoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionCode] ON [platform].[RolePermissions] ([PermissionCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolePermissions_PlatformRoleId_PermissionCode] ON [platform].[RolePermissions] ([PlatformRoleId], [PermissionCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [platform].[Roles] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Roles_SystemKey] ON [platform].[Roles] ([SystemKey]) WHERE [SystemKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserAccounts_PlatformUserId] ON [platform].[UserAccounts] ([PlatformUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserDepartments_PlatformDepartmentId] ON [platform].[UserDepartments] ([PlatformDepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserDepartments_PlatformUserId_PlatformDepartmentId] ON [platform].[UserDepartments] ([PlatformUserId], [PlatformDepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [platform].[Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014010824_InitialIdentity'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014010824_InitialIdentity', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE TABLE [platform].[AccountTokens] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformUserId] uniqueidentifier NOT NULL,
        [Purpose] int NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [UsedUtc] datetimeoffset NULL,
        [RevokedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_AccountTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountTokens_Users_PlatformUserId] FOREIGN KEY ([PlatformUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE TABLE [platform].[AuditEntries] (
        [Id] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [ActionCode] varchar(100) NOT NULL,
        [PermissionCode] varchar(100) NULL,
        [TargetType] varchar(100) NOT NULL,
        [TargetId] uniqueidentifier NULL,
        [ArgumentsJson] nvarchar(max) NULL,
        [BeforeJson] nvarchar(max) NULL,
        [AfterJson] nvarchar(max) NULL,
        [Reason] nvarchar(500) NULL,
        [IpAddress] varchar(45) NULL,
        [UserAgent] nvarchar(512) NULL,
        [CorrelationId] varchar(64) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_AuditEntries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE TABLE [platform].[PasswordResetRequests] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [IpAddress] varchar(45) NULL,
        [LinkSent] bit NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_PasswordResetRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE TABLE [platform].[UserAuthenticatorApps] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformUserId] uniqueidentifier NOT NULL,
        [EncryptedSecret] varchar(1000) NOT NULL,
        [ConfirmedUtc] datetimeoffset NULL,
        [LastAcceptedTimeStep] bigint NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_UserAuthenticatorApps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserAuthenticatorApps_Users_PlatformUserId] FOREIGN KEY ([PlatformUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE TABLE [platform].[UserPasskeys] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformUserId] uniqueidentifier NOT NULL,
        [CredentialId] varbinary(1023) NOT NULL,
        [PublicKey] varbinary(2048) NOT NULL,
        [SignatureCounter] bigint NOT NULL,
        [Aaguid] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [LastUsedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_UserPasskeys] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPasskeys_Users_PlatformUserId] FOREIGN KEY ([PlatformUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE TABLE [platform].[UserRecoveryCodes] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformUserId] uniqueidentifier NOT NULL,
        [CodeHash] char(64) NOT NULL,
        [UsedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_UserRecoveryCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserRecoveryCodes_Users_PlatformUserId] FOREIGN KEY ([PlatformUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE TABLE [platform].[UserSessions] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformUserId] uniqueidentifier NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [IpAddress] varchar(45) NULL,
        [UserAgent] nvarchar(512) NULL,
        [LastSeenUtc] datetimeoffset NOT NULL,
        [SecondFactorUtc] datetimeoffset NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [EndedUtc] datetimeoffset NULL,
        [EndReason] int NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSessions_Users_PlatformUserId] FOREIGN KEY ([PlatformUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE INDEX [IX_AccountTokens_PlatformUserId_Purpose] ON [platform].[AccountTokens] ([PlatformUserId], [Purpose]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountTokens_TokenHash] ON [platform].[AccountTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_ActionCode_CreatedUtc] ON [platform].[AuditEntries] ([ActionCode], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_ActorUserId_CreatedUtc] ON [platform].[AuditEntries] ([ActorUserId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_CreatedUtc] ON [platform].[AuditEntries] ([CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_TargetId_CreatedUtc] ON [platform].[AuditEntries] ([TargetId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE INDEX [IX_PasswordResetRequests_Email_CreatedUtc] ON [platform].[PasswordResetRequests] ([Email], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE INDEX [IX_PasswordResetRequests_IpAddress_CreatedUtc] ON [platform].[PasswordResetRequests] ([IpAddress], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserAuthenticatorApps_PlatformUserId] ON [platform].[UserAuthenticatorApps] ([PlatformUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserPasskeys_CredentialId] ON [platform].[UserPasskeys] ([CredentialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE INDEX [IX_UserPasskeys_PlatformUserId] ON [platform].[UserPasskeys] ([PlatformUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE INDEX [IX_UserRecoveryCodes_PlatformUserId] ON [platform].[UserRecoveryCodes] ([PlatformUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE INDEX [IX_UserSessions_PlatformUserId_EndedUtc] ON [platform].[UserSessions] ([PlatformUserId], [EndedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserSessions_TokenHash] ON [platform].[UserSessions] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014011657_SecondFactorsSessionsAudit'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014011657_SecondFactorsSessionsAudit', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014012923_DataProtectionKeys'
)
BEGIN
    CREATE TABLE [platform].[DataProtectionKeys] (
        [Id] int NOT NULL IDENTITY,
        [FriendlyName] nvarchar(max) NULL,
        [Xml] nvarchar(max) NULL,
        CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014012923_DataProtectionKeys'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014012923_DataProtectionKeys', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014014008_SignInChallenges'
)
BEGIN
    CREATE TABLE [platform].[SignInChallenges] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformUserId] uniqueidentifier NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [Stage] int NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [FailedAttempts] int NOT NULL,
        [CompletedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_SignInChallenges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SignInChallenges_Users_PlatformUserId] FOREIGN KEY ([PlatformUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014014008_SignInChallenges'
)
BEGIN
    CREATE INDEX [IX_SignInChallenges_ExpiresUtc] ON [platform].[SignInChallenges] ([ExpiresUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014014008_SignInChallenges'
)
BEGIN
    CREATE INDEX [IX_SignInChallenges_PlatformUserId] ON [platform].[SignInChallenges] ([PlatformUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014014008_SignInChallenges'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SignInChallenges_TokenHash] ON [platform].[SignInChallenges] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014014008_SignInChallenges'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014014008_SignInChallenges', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014023639_SessionStepUpFailures'
)
BEGIN
    ALTER TABLE [platform].[UserSessions] ADD [FailedStepUpCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014023639_SessionStepUpFailures'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014023639_SessionStepUpFailures', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014031417_PasskeyCeremonies'
)
BEGIN
    CREATE TABLE [platform].[PasskeyCeremonies] (
        [Id] uniqueidentifier NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [Purpose] int NOT NULL,
        [PlatformUserId] uniqueidentifier NULL,
        [OptionsJson] varchar(max) NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [CompletedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_PasskeyCeremonies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasskeyCeremonies_Users_PlatformUserId] FOREIGN KEY ([PlatformUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014031417_PasskeyCeremonies'
)
BEGIN
    CREATE INDEX [IX_PasskeyCeremonies_ExpiresUtc] ON [platform].[PasskeyCeremonies] ([ExpiresUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014031417_PasskeyCeremonies'
)
BEGIN
    CREATE INDEX [IX_PasskeyCeremonies_PlatformUserId] ON [platform].[PasskeyCeremonies] ([PlatformUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014031417_PasskeyCeremonies'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PasskeyCeremonies_TokenHash] ON [platform].[PasskeyCeremonies] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014031417_PasskeyCeremonies'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014031417_PasskeyCeremonies', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE TABLE [platform].[OutboxMessages] (
        [Id] uniqueidentifier NOT NULL,
        [Type] varchar(100) NOT NULL,
        [StreamId] uniqueidentifier NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [ProcessedUtc] datetimeoffset NULL,
        [Attempts] int NOT NULL,
        [NextAttemptUtc] datetimeoffset NULL,
        [LastErrorCode] varchar(100) NULL,
        [LeasedUntilUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE TABLE [platform].[Tenants] (
        [Id] uniqueidentifier NOT NULL,
        [TenantCode] varchar(50) NOT NULL,
        [CurrentSlug] varchar(63) NOT NULL,
        [Status] int NOT NULL,
        [StatusChangedUtc] datetimeoffset NOT NULL,
        [ActivatedUtc] datetimeoffset NULL,
        [Origin] int NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [DefaultLanguage] varchar(8) NOT NULL,
        [TimeZone] varchar(64) NOT NULL,
        [AccountManagerUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [CompanyEmail] nvarchar(254) NOT NULL,
        [LegalName] nvarchar(200) NOT NULL,
        [CompanyPhone] varchar(16) NOT NULL,
        [TradingName] nvarchar(200) NULL,
        [City] nvarchar(100) NOT NULL,
        [CountryCode] char(2) NOT NULL,
        [AddressLine1] nvarchar(200) NOT NULL,
        [AddressLine2] nvarchar(200) NULL,
        [PostalCode] nvarchar(20) NULL,
        [StateProvince] nvarchar(100) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tenants_Users_AccountManagerUserId] FOREIGN KEY ([AccountManagerUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tenants_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE TABLE [platform].[TenantContacts] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [Role] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [Phone] varchar(16) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TenantContacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantContacts_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE TABLE [platform].[TenantLifecycleEvents] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [EventCode] varchar(100) NOT NULL,
        [FromStatus] int NULL,
        [ToStatus] int NOT NULL,
        [Category] int NULL,
        [Reason] nvarchar(500) NULL,
        [ActorUserId] uniqueidentifier NULL,
        [Arguments] nvarchar(max) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TenantLifecycleEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantLifecycleEvents_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE TABLE [platform].[TenantNotes] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [AuthorUserId] uniqueidentifier NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TenantNotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantNotes_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantNotes_Users_AuthorUserId] FOREIGN KEY ([AuthorUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE TABLE [platform].[TenantRegistrations] (
        [Id] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [OwnerName] nvarchar(200) NOT NULL,
        [OwnerEmail] nvarchar(254) NOT NULL,
        [OwnerPhone] varchar(16) NOT NULL,
        [Language] varchar(8) NOT NULL,
        [TimeZone] varchar(64) NOT NULL,
        [Slug] varchar(63) NOT NULL,
        [PlatformTenantId] uniqueidentifier NULL,
        [AccessTokenHash] char(64) NOT NULL,
        [AccessExpiresUtc] datetimeoffset NOT NULL,
        [TermsVersion] nvarchar(50) NOT NULL,
        [PrivacyVersion] nvarchar(50) NOT NULL,
        [ConsentIpAddress] varchar(45) NULL,
        [LastVerificationSentUtc] datetimeoffset NULL,
        [VerificationWindowStartUtc] datetimeoffset NULL,
        [VerificationSendsInWindow] int NOT NULL,
        [EmailVerifiedUtc] datetimeoffset NULL,
        [ClosedUtc] datetimeoffset NULL,
        [CancelledByUserId] uniqueidentifier NULL,
        [CancellationReason] nvarchar(500) NULL,
        [RowVersion] rowversion NULL,
        [CompanyEmail] nvarchar(254) NOT NULL,
        [LegalName] nvarchar(200) NOT NULL,
        [CompanyPhone] varchar(16) NOT NULL,
        [TradingName] nvarchar(200) NULL,
        [City] nvarchar(100) NOT NULL,
        [CountryCode] char(2) NOT NULL,
        [AddressLine1] nvarchar(200) NOT NULL,
        [AddressLine2] nvarchar(200) NULL,
        [PostalCode] nvarchar(20) NULL,
        [StateProvince] nvarchar(100) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TenantRegistrations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantRegistrations_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantRegistrations_Users_CancelledByUserId] FOREIGN KEY ([CancelledByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE TABLE [platform].[TenantSuspensions] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [Category] int NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [SuspendedByUserId] uniqueidentifier NOT NULL,
        [ResolvedByUserId] uniqueidentifier NULL,
        [ResolvedUtc] datetimeoffset NULL,
        [ResolutionReason] nvarchar(500) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TenantSuspensions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantSuspensions_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE TABLE [platform].[TenantTags] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [Value] varchar(32) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TenantTags] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantTags_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE TABLE [platform].[PortalSlugs] (
        [Id] uniqueidentifier NOT NULL,
        [Slug] varchar(63) NOT NULL,
        [State] int NOT NULL,
        [PlatformTenantId] uniqueidentifier NULL,
        [PlatformTenantRegistrationId] uniqueidentifier NULL,
        [HeldUntilUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_PortalSlugs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PortalSlugs_TenantRegistrations_PlatformTenantRegistrationId] FOREIGN KEY ([PlatformTenantRegistrationId]) REFERENCES [platform].[TenantRegistrations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PortalSlugs_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE TABLE [platform].[RegistrationTokens] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantRegistrationId] uniqueidentifier NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [UsedUtc] datetimeoffset NULL,
        [RevokedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_RegistrationTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RegistrationTokens_TenantRegistrations_PlatformTenantRegistrationId] FOREIGN KEY ([PlatformTenantRegistrationId]) REFERENCES [platform].[TenantRegistrations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_OutboxMessages_NextAttemptUtc_CreatedUtc] ON [platform].[OutboxMessages] ([NextAttemptUtc], [CreatedUtc]) WHERE [ProcessedUtc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_OutboxMessages_StreamId_CreatedUtc] ON [platform].[OutboxMessages] ([StreamId], [CreatedUtc]) WHERE [ProcessedUtc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_PortalSlugs_PlatformTenantId] ON [platform].[PortalSlugs] ([PlatformTenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_PortalSlugs_PlatformTenantRegistrationId] ON [platform].[PortalSlugs] ([PlatformTenantRegistrationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PortalSlugs_Slug] ON [platform].[PortalSlugs] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_RegistrationTokens_PlatformTenantRegistrationId_UsedUtc_RevokedUtc] ON [platform].[RegistrationTokens] ([PlatformTenantRegistrationId], [UsedUtc], [RevokedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RegistrationTokens_TokenHash] ON [platform].[RegistrationTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_TenantContacts_Email] ON [platform].[TenantContacts] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantContacts_PlatformTenantId_Role] ON [platform].[TenantContacts] ([PlatformTenantId], [Role]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_TenantLifecycleEvents_PlatformTenantId_CreatedUtc] ON [platform].[TenantLifecycleEvents] ([PlatformTenantId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_TenantNotes_AuthorUserId] ON [platform].[TenantNotes] ([AuthorUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_TenantNotes_PlatformTenantId_CreatedUtc] ON [platform].[TenantNotes] ([PlatformTenantId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantRegistrations_AccessTokenHash] ON [platform].[TenantRegistrations] ([AccessTokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_TenantRegistrations_CancelledByUserId] ON [platform].[TenantRegistrations] ([CancelledByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_TenantRegistrations_OwnerEmail] ON [platform].[TenantRegistrations] ([OwnerEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_TenantRegistrations_PlatformTenantId] ON [platform].[TenantRegistrations] ([PlatformTenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_TenantRegistrations_Status_CreatedUtc] ON [platform].[TenantRegistrations] ([Status], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_Tenants_AccountManagerUserId] ON [platform].[Tenants] ([AccountManagerUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_Tenants_CreatedByUserId] ON [platform].[Tenants] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tenants_CurrentSlug] ON [platform].[Tenants] ([CurrentSlug]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_Tenants_Status_CreatedUtc] ON [platform].[Tenants] ([Status], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tenants_TenantCode] ON [platform].[Tenants] ([TenantCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_TenantSuspensions_PlatformTenantId_ResolvedUtc] ON [platform].[TenantSuspensions] ([PlatformTenantId], [ResolvedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantTags_PlatformTenantId_Value] ON [platform].[TenantTags] ([PlatformTenantId], [Value]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    CREATE INDEX [IX_TenantTags_Value] ON [platform].[TenantTags] ([Value]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014085528_Tenants'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014085528_Tenants', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE TABLE [platform].[Plans] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(32) NOT NULL,
        [Status] int NOT NULL,
        [IsPublic] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Plans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Plans_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE TABLE [platform].[TenantLimitOverrides] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Value] int NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [EndsUtc] datetimeoffset NULL,
        [SetByUserId] uniqueidentifier NOT NULL,
        [RemovedUtc] datetimeoffset NULL,
        [RemovedByUserId] uniqueidentifier NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TenantLimitOverrides] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantLimitOverrides_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantLimitOverrides_Users_RemovedByUserId] FOREIGN KEY ([RemovedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantLimitOverrides_Users_SetByUserId] FOREIGN KEY ([SetByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE TABLE [platform].[PlanTranslations] (
        [PlatformPlanId] uniqueidentifier NOT NULL,
        [Language] varchar(8) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(1000) NULL,
        CONSTRAINT [PK_PlanTranslations] PRIMARY KEY ([PlatformPlanId], [Language]),
        CONSTRAINT [FK_PlanTranslations_Plans_PlatformPlanId] FOREIGN KEY ([PlatformPlanId]) REFERENCES [platform].[Plans] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE TABLE [platform].[PlanVersions] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformPlanId] uniqueidentifier NOT NULL,
        [Number] int NOT NULL,
        [Status] int NOT NULL,
        [CurrencyCode] char(3) NOT NULL,
        [MonthlyPricePerLicence] decimal(19,4) NOT NULL,
        [AnnualPricePerLicence] decimal(19,4) NOT NULL,
        [SetupFee] decimal(19,4) NOT NULL,
        [MinimumLicences] int NOT NULL,
        [TrialDays] int NULL,
        [PublishedUtc] datetimeoffset NULL,
        [PublishedByUserId] uniqueidentifier NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_PlanVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlanVersions_Plans_PlatformPlanId] FOREIGN KEY ([PlatformPlanId]) REFERENCES [platform].[Plans] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PlanVersions_Users_PublishedByUserId] FOREIGN KEY ([PublishedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE TABLE [platform].[PlanVersionFeatures] (
        [Feature] int NOT NULL,
        [PlanVersionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_PlanVersionFeatures] PRIMARY KEY ([PlanVersionId], [Feature]),
        CONSTRAINT [FK_PlanVersionFeatures_PlanVersions_PlanVersionId] FOREIGN KEY ([PlanVersionId]) REFERENCES [platform].[PlanVersions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE TABLE [platform].[PlanVersionLimits] (
        [Kind] int NOT NULL,
        [PlanVersionId] uniqueidentifier NOT NULL,
        [Value] int NOT NULL,
        CONSTRAINT [PK_PlanVersionLimits] PRIMARY KEY ([PlanVersionId], [Kind]),
        CONSTRAINT [FK_PlanVersionLimits_PlanVersions_PlanVersionId] FOREIGN KEY ([PlanVersionId]) REFERENCES [platform].[PlanVersions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE TABLE [platform].[Subscriptions] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [PlatformPlanId] uniqueidentifier NOT NULL,
        [PlanVersionId] uniqueidentifier NOT NULL,
        [Term] int NOT NULL,
        [Licences] int NOT NULL,
        [Status] int NOT NULL,
        [StatusChangedUtc] datetimeoffset NOT NULL,
        [TrialEndsUtc] datetimeoffset NULL,
        [PeriodStartUtc] datetimeoffset NULL,
        [PeriodEndUtc] datetimeoffset NULL,
        [SetupFeeCharged] bit NOT NULL,
        [CancelAtPeriodEnd] bit NOT NULL,
        [CancellationRequestedUtc] datetimeoffset NULL,
        [StatusBeforeSuspension] int NULL,
        [EndedUtc] datetimeoffset NULL,
        [EndReason] int NULL,
        [ScheduledPlanId] uniqueidentifier NULL,
        [ScheduledVersionId] uniqueidentifier NULL,
        [ScheduledTerm] int NULL,
        [ScheduledLicences] int NULL,
        [ScheduledEffectiveUtc] datetimeoffset NULL,
        [ScheduledByUserId] uniqueidentifier NULL,
        [ScheduledUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Subscriptions_PlanVersions_PlanVersionId] FOREIGN KEY ([PlanVersionId]) REFERENCES [platform].[PlanVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Subscriptions_Plans_PlatformPlanId] FOREIGN KEY ([PlatformPlanId]) REFERENCES [platform].[Plans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Subscriptions_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE TABLE [platform].[SubscriptionEvents] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformSubscriptionId] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [EventCode] varchar(100) NOT NULL,
        [FromStatus] int NULL,
        [ToStatus] int NOT NULL,
        [Reason] nvarchar(500) NULL,
        [ActorUserId] uniqueidentifier NULL,
        [Arguments] nvarchar(max) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_SubscriptionEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubscriptionEvents_Subscriptions_PlatformSubscriptionId] FOREIGN KEY ([PlatformSubscriptionId]) REFERENCES [platform].[Subscriptions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Plans_Code] ON [platform].[Plans] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE INDEX [IX_Plans_CreatedByUserId] ON [platform].[Plans] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE INDEX [IX_Plans_Status_IsPublic_DisplayOrder] ON [platform].[Plans] ([Status], [IsPublic], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PlanVersions_OneDraft] ON [platform].[PlanVersions] ([PlatformPlanId]) WHERE [Status] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlanVersions_PlatformPlanId_Number] ON [platform].[PlanVersions] ([PlatformPlanId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE INDEX [IX_PlanVersions_PublishedByUserId] ON [platform].[PlanVersions] ([PublishedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE INDEX [IX_SubscriptionEvents_PlatformSubscriptionId_CreatedUtc] ON [platform].[SubscriptionEvents] ([PlatformSubscriptionId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE INDEX [IX_SubscriptionEvents_PlatformTenantId_CreatedUtc] ON [platform].[SubscriptionEvents] ([PlatformTenantId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Subscriptions_OneCurrentPerTenant] ON [platform].[Subscriptions] ([PlatformTenantId]) WHERE [Status] <> 5');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE INDEX [IX_Subscriptions_PlanVersionId] ON [platform].[Subscriptions] ([PlanVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE INDEX [IX_Subscriptions_PlatformPlanId_Status] ON [platform].[Subscriptions] ([PlatformPlanId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE INDEX [IX_Subscriptions_Status_PeriodEndUtc] ON [platform].[Subscriptions] ([Status], [PeriodEndUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE INDEX [IX_Subscriptions_Status_TrialEndsUtc] ON [platform].[Subscriptions] ([Status], [TrialEndsUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TenantLimitOverrides_OneOpen] ON [platform].[TenantLimitOverrides] ([PlatformTenantId], [Kind]) WHERE [RemovedUtc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE INDEX [IX_TenantLimitOverrides_RemovedByUserId] ON [platform].[TenantLimitOverrides] ([RemovedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    CREATE INDEX [IX_TenantLimitOverrides_SetByUserId] ON [platform].[TenantLimitOverrides] ([SetByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014162220_Subscriptions'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014162220_Subscriptions', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    DROP INDEX [IX_PlanVersions_OneDraft] ON [platform].[PlanVersions];
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    ALTER TABLE [platform].[Subscriptions] ADD [SetupFeeOverride] decimal(19,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    ALTER TABLE [platform].[PlanVersions] ADD [RejectedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    ALTER TABLE [platform].[PlanVersions] ADD [RejectionReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    ALTER TABLE [platform].[PlanVersions] ADD [SubmittedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    ALTER TABLE [platform].[PlanVersions] ADD [SubmittedUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE TABLE [platform].[Offers] (
        [Id] uniqueidentifier NOT NULL,
        [TokenHash] varchar(128) NULL,
        [BoundEmail] nvarchar(254) NULL,
        [CompanyName] nvarchar(200) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [PlatformPlanId] uniqueidentifier NOT NULL,
        [Term] int NOT NULL,
        [Licences] int NOT NULL,
        [DiscountKind] int NULL,
        [DiscountValue] decimal(19,4) NULL,
        [DiscountDuration] int NULL,
        [DiscountPeriods] int NULL,
        [DiscountEndsUtc] datetimeoffset NULL,
        [SetupFeeOverride] decimal(19,4) NULL,
        [StartTrial] bit NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [UsesAllowed] int NOT NULL,
        [Uses] int NOT NULL,
        [Status] int NOT NULL,
        [PreparedByUserId] uniqueidentifier NOT NULL,
        [SubmittedUtc] datetimeoffset NULL,
        [DecidedByUserId] uniqueidentifier NULL,
        [DecidedUtc] datetimeoffset NULL,
        [DecisionReason] nvarchar(500) NULL,
        [IssuedUtc] datetimeoffset NULL,
        [RevokedByUserId] uniqueidentifier NULL,
        [RevokedUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Offers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Offers_Plans_PlatformPlanId] FOREIGN KEY ([PlatformPlanId]) REFERENCES [platform].[Plans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Offers_Users_DecidedByUserId] FOREIGN KEY ([DecidedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Offers_Users_PreparedByUserId] FOREIGN KEY ([PreparedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Offers_Users_RevokedByUserId] FOREIGN KEY ([RevokedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE TABLE [platform].[OneTimeCharges] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformSubscriptionId] uniqueidentifier NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [Amount] decimal(19,4) NOT NULL,
        [CurrencyCode] char(3) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [AddedByUserId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [CancelledByUserId] uniqueidentifier NULL,
        [CancelledUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_OneTimeCharges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OneTimeCharges_Subscriptions_PlatformSubscriptionId] FOREIGN KEY ([PlatformSubscriptionId]) REFERENCES [platform].[Subscriptions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OneTimeCharges_Users_AddedByUserId] FOREIGN KEY ([AddedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OneTimeCharges_Users_CancelledByUserId] FOREIGN KEY ([CancelledByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE TABLE [platform].[Promotions] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(32) NOT NULL,
        [DiscountKind] int NOT NULL,
        [DiscountValue] decimal(19,4) NOT NULL,
        [DiscountDuration] int NOT NULL,
        [DiscountPeriods] int NULL,
        [DiscountEndsUtc] datetimeoffset NULL,
        [ValidFromUtc] datetimeoffset NOT NULL,
        [ValidToUtc] datetimeoffset NULL,
        [Term] int NULL,
        [AtRegistration] bit NOT NULL,
        [ByStaff] bit NOT NULL,
        [RedemptionLimit] int NULL,
        [Redemptions] int NOT NULL,
        [Status] int NOT NULL,
        [DesignedByUserId] uniqueidentifier NOT NULL,
        [SubmittedByUserId] uniqueidentifier NULL,
        [DecidedByUserId] uniqueidentifier NULL,
        [DecidedUtc] datetimeoffset NULL,
        [DecisionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Promotions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Promotions_Users_DecidedByUserId] FOREIGN KEY ([DecidedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Promotions_Users_DesignedByUserId] FOREIGN KEY ([DesignedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Promotions_Users_SubmittedByUserId] FOREIGN KEY ([SubmittedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE TABLE [platform].[SalesSettings] (
        [Id] uniqueidentifier NOT NULL,
        [Key] varchar(20) NOT NULL,
        [MaxPercentWithoutApproval] decimal(5,2) NULL,
        [NoEndWithoutApproval] bit NOT NULL,
        [MaxOfferValidityDays] int NULL,
        [DiscountEndNoticeDays] int NULL,
        [PromotionCodesAtRegistration] bit NOT NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_SalesSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalesSettings_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE TABLE [platform].[SubscriptionDiscounts] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformSubscriptionId] uniqueidentifier NOT NULL,
        [DiscountKind] int NOT NULL,
        [DiscountValue] decimal(19,4) NOT NULL,
        [DiscountDuration] int NOT NULL,
        [DiscountPeriods] int NULL,
        [DiscountEndsUtc] datetimeoffset NULL,
        [Source] int NOT NULL,
        [SourceId] uniqueidentifier NULL,
        [Reason] nvarchar(500) NOT NULL,
        [GrantedByUserId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [DecidedByUserId] uniqueidentifier NULL,
        [DecidedUtc] datetimeoffset NULL,
        [DecisionReason] nvarchar(500) NULL,
        [PeriodsUsed] int NOT NULL,
        [LastPeriodStartUtc] datetimeoffset NULL,
        [EndedUtc] datetimeoffset NULL,
        [EndReason] int NULL,
        [EndNoticeSentUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_SubscriptionDiscounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubscriptionDiscounts_Subscriptions_PlatformSubscriptionId] FOREIGN KEY ([PlatformSubscriptionId]) REFERENCES [platform].[Subscriptions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SubscriptionDiscounts_Users_DecidedByUserId] FOREIGN KEY ([DecidedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubscriptionDiscounts_Users_GrantedByUserId] FOREIGN KEY ([GrantedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE TABLE [platform].[OfferCharges] (
        [Id] int NOT NULL IDENTITY,
        [Description] nvarchar(200) NOT NULL,
        [Amount] decimal(19,4) NOT NULL,
        [OfferId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_OfferCharges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OfferCharges_Offers_OfferId] FOREIGN KEY ([OfferId]) REFERENCES [platform].[Offers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE TABLE [platform].[PromotionNames] (
        [Language] varchar(10) NOT NULL,
        [PromotionId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_PromotionNames] PRIMARY KEY ([PromotionId], [Language]),
        CONSTRAINT [FK_PromotionNames_Promotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [platform].[Promotions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE TABLE [platform].[PromotionPlans] (
        [PlatformPlanId] uniqueidentifier NOT NULL,
        [PromotionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_PromotionPlans] PRIMARY KEY ([PromotionId], [PlatformPlanId]),
        CONSTRAINT [FK_PromotionPlans_Plans_PlatformPlanId] FOREIGN KEY ([PlatformPlanId]) REFERENCES [platform].[Plans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PromotionPlans_Promotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [platform].[Promotions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE TABLE [platform].[SalesFixedLimits] (
        [CurrencyCode] char(3) NOT NULL,
        [SalesSettingsId] uniqueidentifier NOT NULL,
        [Amount] decimal(19,4) NOT NULL,
        CONSTRAINT [PK_SalesFixedLimits] PRIMARY KEY ([SalesSettingsId], [CurrencyCode]),
        CONSTRAINT [FK_SalesFixedLimits_SalesSettings_SalesSettingsId] FOREIGN KEY ([SalesSettingsId]) REFERENCES [platform].[SalesSettings] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PlanVersions_OneDraft] ON [platform].[PlanVersions] ([PlatformPlanId]) WHERE [Status] IN (0, 3)');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_PlanVersions_RejectedByUserId] ON [platform].[PlanVersions] ([RejectedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_PlanVersions_SubmittedByUserId] ON [platform].[PlanVersions] ([SubmittedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_OfferCharges_OfferId] ON [platform].[OfferCharges] ([OfferId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_Offers_DecidedByUserId] ON [platform].[Offers] ([DecidedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_Offers_PlatformPlanId] ON [platform].[Offers] ([PlatformPlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_Offers_PreparedByUserId] ON [platform].[Offers] ([PreparedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_Offers_RevokedByUserId] ON [platform].[Offers] ([RevokedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_Offers_Status_ExpiresUtc] ON [platform].[Offers] ([Status], [ExpiresUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Offers_TokenHash] ON [platform].[Offers] ([TokenHash]) WHERE [TokenHash] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_OneTimeCharges_AddedByUserId] ON [platform].[OneTimeCharges] ([AddedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_OneTimeCharges_CancelledByUserId] ON [platform].[OneTimeCharges] ([CancelledByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_OneTimeCharges_PlatformSubscriptionId] ON [platform].[OneTimeCharges] ([PlatformSubscriptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_OneTimeCharges_Status] ON [platform].[OneTimeCharges] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_PromotionPlans_PlatformPlanId] ON [platform].[PromotionPlans] ([PlatformPlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Promotions_Code] ON [platform].[Promotions] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_Promotions_DecidedByUserId] ON [platform].[Promotions] ([DecidedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_Promotions_DesignedByUserId] ON [platform].[Promotions] ([DesignedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_Promotions_Status] ON [platform].[Promotions] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_Promotions_SubmittedByUserId] ON [platform].[Promotions] ([SubmittedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalesSettings_Key] ON [platform].[SalesSettings] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_SalesSettings_UpdatedByUserId] ON [platform].[SalesSettings] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_SubscriptionDiscounts_DecidedByUserId] ON [platform].[SubscriptionDiscounts] ([DecidedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_SubscriptionDiscounts_GrantedByUserId] ON [platform].[SubscriptionDiscounts] ([GrantedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SubscriptionDiscounts_OnePending] ON [platform].[SubscriptionDiscounts] ([PlatformSubscriptionId]) WHERE [Status] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_SubscriptionDiscounts_PlatformSubscriptionId_Status] ON [platform].[SubscriptionDiscounts] ([PlatformSubscriptionId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    CREATE INDEX [IX_SubscriptionDiscounts_Source_SourceId] ON [platform].[SubscriptionDiscounts] ([Source], [SourceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    ALTER TABLE [platform].[PlanVersions] ADD CONSTRAINT [FK_PlanVersions_Users_RejectedByUserId] FOREIGN KEY ([RejectedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    ALTER TABLE [platform].[PlanVersions] ADD CONSTRAINT [FK_PlanVersions_Users_SubmittedByUserId] FOREIGN KEY ([SubmittedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014183204_CommercialFlexibility'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014183204_CommercialFlexibility', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    ALTER TABLE [platform].[Tenants] ADD [TaxExempt] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    ALTER TABLE [platform].[Tenants] ADD [TaxExemptionReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    ALTER TABLE [platform].[Tenants] ADD [TaxExemptionReference] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    ALTER TABLE [platform].[Tenants] ADD [TaxId] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE TABLE [platform].[BillingSettings] (
        [Id] uniqueidentifier NOT NULL,
        [Key] varchar(16) NOT NULL,
        [PaymentTermsDays] int NULL,
        [InvoicePrefix] varchar(8) NOT NULL,
        [CreditNotePrefix] varchar(8) NOT NULL,
        [ReceiptPrefix] varchar(8) NOT NULL,
        [PaperSize] int NOT NULL,
        [EmailDocuments] bit NOT NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_BillingSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BillingSettings_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE TABLE [platform].[CompanyProfile] (
        [Id] uniqueidentifier NOT NULL,
        [Key] varchar(16) NOT NULL,
        [TaxId] nvarchar(40) NULL,
        [Website] nvarchar(200) NULL,
        [Logo] varbinary(max) NULL,
        [LogoContentType] varchar(16) NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [Email] nvarchar(254) NOT NULL,
        [LegalName] nvarchar(200) NOT NULL,
        [Phone] varchar(16) NOT NULL,
        [TradingName] nvarchar(200) NULL,
        [City] nvarchar(100) NOT NULL,
        [CountryCode] char(2) NOT NULL,
        [AddressLine1] nvarchar(200) NOT NULL,
        [AddressLine2] nvarchar(200) NULL,
        [PostalCode] nvarchar(20) NULL,
        [StateProvince] nvarchar(100) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_CompanyProfile] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CompanyProfile_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE TABLE [platform].[Invoices] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [PlatformSubscriptionId] uniqueidentifier NULL,
        [Kind] int NOT NULL,
        [Number] varchar(32) NOT NULL,
        [SourceKey] varchar(120) NOT NULL,
        [Status] int NOT NULL,
        [CurrencyCode] char(3) NOT NULL,
        [Language] varchar(8) NOT NULL,
        [IssuedUtc] datetimeoffset NOT NULL,
        [DueUtc] datetimeoffset NOT NULL,
        [PeriodStartUtc] datetimeoffset NULL,
        [PeriodEndUtc] datetimeoffset NULL,
        [NetTotal] decimal(18,2) NOT NULL,
        [DiscountTotal] decimal(18,2) NOT NULL,
        [TaxTotal] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [PaidTotal] decimal(18,2) NOT NULL,
        [CreditedTotal] decimal(18,2) NOT NULL,
        [Balance] decimal(18,2) NOT NULL,
        [PastDueUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CustomerAddress] nvarchar(600) NOT NULL,
        [CustomerEmail] nvarchar(254) NOT NULL,
        [CustomerName] nvarchar(200) NOT NULL,
        [CustomerTaxId] nvarchar(40) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Subscriptions_PlatformSubscriptionId] FOREIGN KEY ([PlatformSubscriptionId]) REFERENCES [platform].[Subscriptions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE TABLE [platform].[NumberSequences] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Year] int NOT NULL,
        [NextValue] int NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_NumberSequences] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE TABLE [platform].[TaxRates] (
        [Id] uniqueidentifier NOT NULL,
        [CountryCode] char(2) NOT NULL,
        [RegionCode] nvarchar(64) NULL,
        [Rate] decimal(9,4) NOT NULL,
        [Label] nvarchar(40) NOT NULL,
        [ValidFromUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TaxRates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaxRates_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE TABLE [platform].[BillingTexts] (
        [Id] int NOT NULL IDENTITY,
        [Language] varchar(8) NOT NULL,
        [PaymentInstructions] nvarchar(1000) NULL,
        [Footer] nvarchar(1000) NULL,
        [BillingSettingsId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_BillingTexts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BillingTexts_BillingSettings_BillingSettingsId] FOREIGN KEY ([BillingSettingsId]) REFERENCES [platform].[BillingSettings] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE TABLE [platform].[BillingDocuments] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [PlatformInvoiceId] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [Number] varchar(32) NOT NULL,
        [Language] varchar(8) NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [Sha256] char(64) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_BillingDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BillingDocuments_Invoices_PlatformInvoiceId] FOREIGN KEY ([PlatformInvoiceId]) REFERENCES [platform].[Invoices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE TABLE [platform].[CreditNotes] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformInvoiceId] uniqueidentifier NOT NULL,
        [Number] varchar(32) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [TaxPart] decimal(18,2) NOT NULL,
        [IsVoid] bit NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [Status] int NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [DecidedByUserId] uniqueidentifier NULL,
        [DecidedUtc] datetimeoffset NULL,
        [DecisionReason] nvarchar(500) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_CreditNotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CreditNotes_Invoices_PlatformInvoiceId] FOREIGN KEY ([PlatformInvoiceId]) REFERENCES [platform].[Invoices] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CreditNotes_Users_DecidedByUserId] FOREIGN KEY ([DecidedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CreditNotes_Users_RequestedByUserId] FOREIGN KEY ([RequestedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE TABLE [platform].[InvoiceEvents] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformInvoiceId] uniqueidentifier NOT NULL,
        [EventCode] varchar(100) NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [Reason] nvarchar(500) NULL,
        [Arguments] nvarchar(max) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_InvoiceEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoiceEvents_Invoices_PlatformInvoiceId] FOREIGN KEY ([PlatformInvoiceId]) REFERENCES [platform].[Invoices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE TABLE [platform].[InvoiceLines] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformInvoiceId] uniqueidentifier NOT NULL,
        [Position] int NOT NULL,
        [Kind] int NOT NULL,
        [TextKey] varchar(100) NOT NULL,
        [Arguments] nvarchar(max) NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [Net] decimal(18,2) NOT NULL,
        [TaxRate] decimal(9,4) NOT NULL,
        [TaxLabel] nvarchar(40) NULL,
        [Tax] decimal(18,2) NOT NULL,
        [SourceId] uniqueidentifier NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_InvoiceLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoiceLines_Invoices_PlatformInvoiceId] FOREIGN KEY ([PlatformInvoiceId]) REFERENCES [platform].[Invoices] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE TABLE [platform].[Payments] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformInvoiceId] uniqueidentifier NOT NULL,
        [ReceiptNumber] varchar(32) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Method] int NOT NULL,
        [ReceivedUtc] datetimeoffset NOT NULL,
        [Reference] nvarchar(100) NULL,
        [Note] nvarchar(500) NULL,
        [RecordedByUserId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [ReversedByUserId] uniqueidentifier NULL,
        [ReversedUtc] datetimeoffset NULL,
        [ReversalReason] nvarchar(500) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Invoices_PlatformInvoiceId] FOREIGN KEY ([PlatformInvoiceId]) REFERENCES [platform].[Invoices] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Payments_Users_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Users_ReversedByUserId] FOREIGN KEY ([ReversedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BillingDocuments_Kind_OwnerId] ON [platform].[BillingDocuments] ([Kind], [OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_BillingDocuments_PlatformInvoiceId] ON [platform].[BillingDocuments] ([PlatformInvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BillingSettings_Key] ON [platform].[BillingSettings] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_BillingSettings_UpdatedByUserId] ON [platform].[BillingSettings] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_BillingTexts_BillingSettingsId] ON [platform].[BillingTexts] ([BillingSettingsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CompanyProfile_Key] ON [platform].[CompanyProfile] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_CompanyProfile_UpdatedByUserId] ON [platform].[CompanyProfile] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_CreditNotes_DecidedByUserId] ON [platform].[CreditNotes] ([DecidedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CreditNotes_Number] ON [platform].[CreditNotes] ([Number]) WHERE [Number] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CreditNotes_OnePending] ON [platform].[CreditNotes] ([PlatformInvoiceId]) WHERE [Status] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_CreditNotes_RequestedByUserId] ON [platform].[CreditNotes] ([RequestedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_InvoiceEvents_PlatformInvoiceId_CreatedUtc] ON [platform].[InvoiceEvents] ([PlatformInvoiceId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_InvoiceLines_PlatformInvoiceId] ON [platform].[InvoiceLines] ([PlatformInvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_Number] ON [platform].[Invoices] ([Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_Invoices_PlatformSubscriptionId_Status] ON [platform].[Invoices] ([PlatformSubscriptionId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_Invoices_PlatformTenantId_IssuedUtc] ON [platform].[Invoices] ([PlatformTenantId], [IssuedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_SourceKey] ON [platform].[Invoices] ([SourceKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_Invoices_Status_DueUtc] ON [platform].[Invoices] ([Status], [DueUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NumberSequences_Kind_Year] ON [platform].[NumberSequences] ([Kind], [Year]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_Payments_PlatformInvoiceId] ON [platform].[Payments] ([PlatformInvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_ReceiptNumber] ON [platform].[Payments] ([ReceiptNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_Payments_ReceivedUtc] ON [platform].[Payments] ([ReceivedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_Payments_RecordedByUserId] ON [platform].[Payments] ([RecordedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_Payments_ReversedByUserId] ON [platform].[Payments] ([ReversedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_TaxRates_CountryCode_RegionCode_ValidFromUtc] ON [platform].[TaxRates] ([CountryCode], [RegionCode], [ValidFromUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    CREATE INDEX [IX_TaxRates_CreatedByUserId] ON [platform].[TaxRates] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016013712_Billing'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016013712_Billing', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    ALTER TABLE [platform].[Invoices] ADD [RefundedTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE TABLE [platform].[MerchantAccounts] (
        [Id] uniqueidentifier NOT NULL,
        [Key] varchar(20) NOT NULL,
        [Provider] int NOT NULL,
        [Mode] int NOT NULL,
        [PublishableKey] varchar(255) NULL,
        [AcceptedCurrencies] varchar(40) NOT NULL,
        [CardPaymentsEnabled] bit NOT NULL,
        [LastCheckedUtc] datetimeoffset NULL,
        [LastCheckSucceeded] bit NOT NULL,
        [LastCheckCode] varchar(60) NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_MerchantAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MerchantAccounts_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE TABLE [platform].[PaymentLinks] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [PlatformInvoiceId] uniqueidentifier NULL,
        [TokenHash] char(64) NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [UsedUtc] datetimeoffset NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_PaymentLinks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentLinks_Invoices_PlatformInvoiceId] FOREIGN KEY ([PlatformInvoiceId]) REFERENCES [platform].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentLinks_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentLinks_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE TABLE [platform].[ProviderEvents] (
        [Id] uniqueidentifier NOT NULL,
        [Provider] int NOT NULL,
        [ProviderEventId] varchar(255) NOT NULL,
        [EventType] varchar(100) NOT NULL,
        [ObjectId] varchar(255) NULL,
        [ProcessedUtc] datetimeoffset NULL,
        [Outcome] varchar(40) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_ProviderEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE TABLE [platform].[SavedCards] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [Provider] int NOT NULL,
        [ProviderPaymentMethodId] varchar(255) NOT NULL,
        [Brand] varchar(20) NOT NULL,
        [Last4] char(4) NOT NULL,
        [ExpiryMonth] int NOT NULL,
        [ExpiryYear] int NOT NULL,
        [AddedVia] int NOT NULL,
        [Status] int NOT NULL,
        [IsDefault] bit NOT NULL,
        [RemovedByUserId] uniqueidentifier NULL,
        [RemovedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_SavedCards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SavedCards_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SavedCards_Users_RemovedByUserId] FOREIGN KEY ([RemovedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE TABLE [platform].[TenantCardProfiles] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [Provider] int NOT NULL,
        [Mode] int NOT NULL,
        [ProviderCustomerId] varchar(255) NOT NULL,
        [AutoCharge] bit NOT NULL,
        [ConsentEmail] nvarchar(254) NULL,
        [ConsentTextVersion] varchar(20) NULL,
        [ConsentUtc] datetimeoffset NULL,
        [AutoChargeOffByUserId] uniqueidentifier NULL,
        [AutoChargeOffUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TenantCardProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantCardProfiles_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantCardProfiles_Users_AutoChargeOffByUserId] FOREIGN KEY ([AutoChargeOffByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE TABLE [platform].[CardTransactions] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [PlatformInvoiceId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Origin] int NOT NULL,
        [Provider] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [CurrencyCode] char(3) NOT NULL,
        [Status] int NOT NULL,
        [IdempotencyKey] varchar(60) NOT NULL,
        [ProviderCheckoutId] varchar(255) NULL,
        [ProviderPaymentId] varchar(255) NULL,
        [ProviderRefundId] varchar(255) NULL,
        [SavedCardId] uniqueidentifier NULL,
        [CreditNoteId] uniqueidentifier NULL,
        [ChargeTransactionId] uniqueidentifier NULL,
        [PlatformPaymentId] uniqueidentifier NULL,
        [FailureCode] varchar(60) NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [CompletedUtc] datetimeoffset NULL,
        [Dispute] int NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_CardTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CardTransactions_CardTransactions_ChargeTransactionId] FOREIGN KEY ([ChargeTransactionId]) REFERENCES [platform].[CardTransactions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CardTransactions_Invoices_PlatformInvoiceId] FOREIGN KEY ([PlatformInvoiceId]) REFERENCES [platform].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CardTransactions_SavedCards_SavedCardId] FOREIGN KEY ([SavedCardId]) REFERENCES [platform].[SavedCards] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CardTransactions_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CardTransactions_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_CardTransactions_ChargeTransactionId] ON [platform].[CardTransactions] ([ChargeTransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_CardTransactions_CreatedByUserId] ON [platform].[CardTransactions] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CardTransactions_IdempotencyKey] ON [platform].[CardTransactions] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CardTransactions_OneOpenChargePerInvoice] ON [platform].[CardTransactions] ([PlatformInvoiceId]) WHERE [Kind] = 1 AND [Status] IN (1, 2, 3)');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_CardTransactions_PlatformInvoiceId_CreatedUtc] ON [platform].[CardTransactions] ([PlatformInvoiceId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_CardTransactions_PlatformTenantId_CreatedUtc] ON [platform].[CardTransactions] ([PlatformTenantId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_CardTransactions_ProviderCheckoutId] ON [platform].[CardTransactions] ([ProviderCheckoutId]) WHERE [ProviderCheckoutId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_CardTransactions_ProviderPaymentId] ON [platform].[CardTransactions] ([ProviderPaymentId]) WHERE [ProviderPaymentId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_CardTransactions_ProviderRefundId] ON [platform].[CardTransactions] ([ProviderRefundId]) WHERE [ProviderRefundId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_CardTransactions_SavedCardId] ON [platform].[CardTransactions] ([SavedCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_CardTransactions_Status_LastModifiedUtc] ON [platform].[CardTransactions] ([Status], [LastModifiedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MerchantAccounts_Key] ON [platform].[MerchantAccounts] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_MerchantAccounts_UpdatedByUserId] ON [platform].[MerchantAccounts] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_PaymentLinks_CreatedByUserId] ON [platform].[PaymentLinks] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_PaymentLinks_PlatformInvoiceId] ON [platform].[PaymentLinks] ([PlatformInvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_PaymentLinks_PlatformTenantId_Kind] ON [platform].[PaymentLinks] ([PlatformTenantId], [Kind]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentLinks_TokenHash] ON [platform].[PaymentLinks] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProviderEvents_Provider_ProviderEventId] ON [platform].[ProviderEvents] ([Provider], [ProviderEventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_SavedCards_PlatformTenantId_Status] ON [platform].[SavedCards] ([PlatformTenantId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SavedCards_ProviderPaymentMethodId] ON [platform].[SavedCards] ([ProviderPaymentMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_SavedCards_RemovedByUserId] ON [platform].[SavedCards] ([RemovedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_TenantCardProfiles_AutoCharge] ON [platform].[TenantCardProfiles] ([AutoCharge]) WHERE [AutoCharge] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE INDEX [IX_TenantCardProfiles_AutoChargeOffByUserId] ON [platform].[TenantCardProfiles] ([AutoChargeOffByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantCardProfiles_PlatformTenantId] ON [platform].[TenantCardProfiles] ([PlatformTenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016041409_Payments'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016041409_Payments', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    ALTER TABLE [platform].[Subscriptions] ADD [BlockedUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    ALTER TABLE [platform].[Subscriptions] ADD [StatusBeforeBlock] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    CREATE TABLE [platform].[CollectionCases] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [PlatformSubscriptionId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [Stage] int NOT NULL,
        [OldestDueUtc] datetimeoffset NOT NULL,
        [PlannedBlockUtc] datetimeoffset NULL,
        [BlockedUtc] datetimeoffset NULL,
        [NoAccessUtc] datetimeoffset NULL,
        [ExtensionUntilUtc] datetimeoffset NULL,
        [ExtensionsUsed] int NOT NULL,
        [ExtensionEndingNoticeSent] bit NOT NULL,
        [Held] bit NOT NULL,
        [LastStepUtc] datetimeoffset NULL,
        [LastCheckedUtc] datetimeoffset NULL,
        [ClosedUtc] datetimeoffset NULL,
        [CloseReason] int NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_CollectionCases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CollectionCases_Subscriptions_PlatformSubscriptionId] FOREIGN KEY ([PlatformSubscriptionId]) REFERENCES [platform].[Subscriptions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CollectionCases_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    CREATE TABLE [platform].[CollectionsSettings] (
        [Id] uniqueidentifier NOT NULL,
        [Key] varchar(20) NOT NULL,
        [Enabled] bit NOT NULL,
        [Reminder1Day] int NOT NULL,
        [Reminder2Day] int NOT NULL,
        [FinalWarningDay] int NOT NULL,
        [BlockDay] int NOT NULL,
        [RetryDays] varchar(40) NOT NULL,
        [MinimumNoticeDays] int NOT NULL,
        [BlockedReadOnlyDays] int NULL,
        [ExtensionMaxDays] int NOT NULL,
        [ExtensionsPerCase] int NOT NULL,
        [PendingPaymentReadOnlyDays] int NOT NULL,
        [LongBlockDays] int NOT NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_CollectionsSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CollectionsSettings_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    CREATE TABLE [platform].[CollectionCaseEvents] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformCollectionCaseId] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [EventCode] varchar(100) NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [Note] nvarchar(1000) NULL,
        [RetryKey] varchar(80) NULL,
        [Arguments] nvarchar(max) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_CollectionCaseEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CollectionCaseEvents_CollectionCases_PlatformCollectionCaseId] FOREIGN KEY ([PlatformCollectionCaseId]) REFERENCES [platform].[CollectionCases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CollectionCaseEvents_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CollectionCaseEvents_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    CREATE INDEX [IX_CollectionCaseEvents_ActorUserId] ON [platform].[CollectionCaseEvents] ([ActorUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    CREATE INDEX [IX_CollectionCaseEvents_PlatformCollectionCaseId_CreatedUtc] ON [platform].[CollectionCaseEvents] ([PlatformCollectionCaseId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    CREATE INDEX [IX_CollectionCaseEvents_PlatformTenantId] ON [platform].[CollectionCaseEvents] ([PlatformTenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CollectionCaseEvents_RetryKey] ON [platform].[CollectionCaseEvents] ([RetryKey]) WHERE [RetryKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CollectionCases_OneOpenCasePerTenant] ON [platform].[CollectionCases] ([PlatformTenantId]) WHERE [Status] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    CREATE INDEX [IX_CollectionCases_PlatformSubscriptionId] ON [platform].[CollectionCases] ([PlatformSubscriptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    CREATE INDEX [IX_CollectionCases_Status_Stage] ON [platform].[CollectionCases] ([Status], [Stage]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CollectionsSettings_Key] ON [platform].[CollectionsSettings] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    CREATE INDEX [IX_CollectionsSettings_UpdatedByUserId] ON [platform].[CollectionsSettings] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016055942_Collections'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016055942_Collections', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE TABLE [platform].[DeviceLinks] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [PlatformTenantRegistrationId] uniqueidentifier NULL,
        [TokenHash] varchar(64) NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [UsedUtc] datetimeoffset NULL,
        [VoidedUtc] datetimeoffset NULL,
        [Tries] int NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_DeviceLinks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeviceLinks_TenantRegistrations_PlatformTenantRegistrationId] FOREIGN KEY ([PlatformTenantRegistrationId]) REFERENCES [platform].[TenantRegistrations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DeviceLinks_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE TABLE [platform].[Devices] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [Number] varchar(12) NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [Kind] int NOT NULL,
        [Proof] int NOT NULL,
        [PublicKey] varchar(200) NULL,
        [SecretHash] varchar(64) NULL,
        [SecretIssuedUtc] datetimeoffset NULL,
        [PreviousSecretHash] varchar(64) NULL,
        [PreviousSecretValidUntilUtc] datetimeoffset NULL,
        [Status] int NOT NULL,
        [SeatHeld] bit NOT NULL,
        [Reason] int NULL,
        [RequestedByReference] nvarchar(100) NULL,
        [RequestedByName] nvarchar(120) NULL,
        [ApprovedUtc] datetimeoffset NULL,
        [ReleaseRequestedUtc] datetimeoffset NULL,
        [EndedUtc] datetimeoffset NULL,
        [EndedByTenant] bit NOT NULL,
        [LastSeenUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Devices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Devices_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE TABLE [platform].[TenantLicensing] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [SeatsInUse] int NOT NULL,
        [LastDeviceNumber] int NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TenantLicensing] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantLicensing_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE TABLE [platform].[DeviceChallenges] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformDeviceId] uniqueidentifier NOT NULL,
        [ChallengeHash] varchar(64) NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [UsedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_DeviceChallenges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeviceChallenges_Devices_PlatformDeviceId] FOREIGN KEY ([PlatformDeviceId]) REFERENCES [platform].[Devices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE TABLE [platform].[DeviceEvents] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [PlatformDeviceId] uniqueidentifier NULL,
        [EventCode] varchar(100) NOT NULL,
        [ActorReference] nvarchar(100) NULL,
        [ActorName] nvarchar(120) NULL,
        [ActorUserId] uniqueidentifier NULL,
        [Reason] int NULL,
        [Note] nvarchar(500) NULL,
        [Arguments] nvarchar(max) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_DeviceEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeviceEvents_Devices_PlatformDeviceId] FOREIGN KEY ([PlatformDeviceId]) REFERENCES [platform].[Devices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DeviceEvents_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DeviceEvents_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DeviceChallenges_ChallengeHash] ON [platform].[DeviceChallenges] ([ChallengeHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE INDEX [IX_DeviceChallenges_PlatformDeviceId] ON [platform].[DeviceChallenges] ([PlatformDeviceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE INDEX [IX_DeviceEvents_ActorUserId] ON [platform].[DeviceEvents] ([ActorUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE INDEX [IX_DeviceEvents_PlatformDeviceId_CreatedUtc] ON [platform].[DeviceEvents] ([PlatformDeviceId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE INDEX [IX_DeviceEvents_PlatformTenantId_EventCode_CreatedUtc] ON [platform].[DeviceEvents] ([PlatformTenantId], [EventCode], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE INDEX [IX_DeviceLinks_PlatformTenantId_Kind] ON [platform].[DeviceLinks] ([PlatformTenantId], [Kind]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE INDEX [IX_DeviceLinks_PlatformTenantRegistrationId] ON [platform].[DeviceLinks] ([PlatformTenantRegistrationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DeviceLinks_TokenHash] ON [platform].[DeviceLinks] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Devices_PlatformTenantId_Number] ON [platform].[Devices] ([PlatformTenantId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE INDEX [IX_Devices_PlatformTenantId_Status] ON [platform].[Devices] ([PlatformTenantId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Devices_SecretHash] ON [platform].[Devices] ([SecretHash]) WHERE [SecretHash] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE INDEX [IX_Devices_Status_CreatedUtc] ON [platform].[Devices] ([Status], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantLicensing_PlatformTenantId] ON [platform].[TenantLicensing] ([PlatformTenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016074231_Licensing'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016074231_Licensing', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016083010_Zelle'
)
BEGIN
    CREATE TABLE [platform].[ZelleAccounts] (
        [Id] uniqueidentifier NOT NULL,
        [Key] varchar(20) NOT NULL,
        [Enabled] bit NOT NULL,
        [RecipientName] nvarchar(100) NOT NULL,
        [Email] nvarchar(254) NULL,
        [Tag] nvarchar(50) NULL,
        [Phone] varchar(32) NULL,
        [QrCode] varbinary(max) NULL,
        [QrCodeContentType] varchar(20) NULL,
        [QrCodeVersion] int NOT NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_ZelleAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ZelleAccounts_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016083010_Zelle'
)
BEGIN
    CREATE TABLE [platform].[ZelleClaims] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [PlatformInvoiceId] uniqueidentifier NOT NULL,
        [InvoiceNumber] varchar(32) NOT NULL,
        [CurrencyCode] char(3) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [SentOn] date NOT NULL,
        [SenderName] nvarchar(100) NOT NULL,
        [Reference] nvarchar(60) NOT NULL,
        [Source] int NOT NULL,
        [Status] int NOT NULL,
        [ReceivedAmount] decimal(18,2) NULL,
        [ReceivedUtc] datetimeoffset NULL,
        [PlatformPaymentId] uniqueidentifier NULL,
        [RejectReason] nvarchar(500) NULL,
        [StaffNote] nvarchar(1000) NULL,
        [DecidedByUserId] uniqueidentifier NULL,
        [DecidedUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_ZelleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ZelleClaims_Invoices_PlatformInvoiceId] FOREIGN KEY ([PlatformInvoiceId]) REFERENCES [platform].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ZelleClaims_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ZelleClaims_Users_DecidedByUserId] FOREIGN KEY ([DecidedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016083010_Zelle'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ZelleAccounts_Key] ON [platform].[ZelleAccounts] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016083010_Zelle'
)
BEGIN
    CREATE INDEX [IX_ZelleAccounts_UpdatedByUserId] ON [platform].[ZelleAccounts] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016083010_Zelle'
)
BEGIN
    CREATE INDEX [IX_ZelleClaims_DecidedByUserId] ON [platform].[ZelleClaims] ([DecidedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016083010_Zelle'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ZelleClaims_OneWaiting] ON [platform].[ZelleClaims] ([PlatformInvoiceId]) WHERE [Status] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016083010_Zelle'
)
BEGIN
    CREATE INDEX [IX_ZelleClaims_PlatformTenantId_Status] ON [platform].[ZelleClaims] ([PlatformTenantId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016083010_Zelle'
)
BEGIN
    CREATE INDEX [IX_ZelleClaims_Status_CreatedUtc] ON [platform].[ZelleClaims] ([Status], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016083010_Zelle'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016083010_Zelle', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    ALTER TABLE [platform].[CompanyProfile] ADD [SupportEmail] nvarchar(254) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    ALTER TABLE [platform].[CompanyProfile] ADD [SupportPhone] varchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE TABLE [platform].[LegalDocuments] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Version] nvarchar(20) NOT NULL,
        [Status] int NOT NULL,
        [AcceptanceRequired] bit NOT NULL,
        [EffectiveUtc] datetimeoffset NULL,
        [DeadlineUtc] datetimeoffset NULL,
        [PublishedUtc] datetimeoffset NULL,
        [PublishedByUserId] uniqueidentifier NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_LegalDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LegalDocuments_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LegalDocuments_Users_PublishedByUserId] FOREIGN KEY ([PublishedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE TABLE [platform].[LegalDocumentFiles] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformLegalDocumentId] uniqueidentifier NOT NULL,
        [Language] varchar(8) NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [Sha256] char(64) NOT NULL,
        [SizeBytes] int NOT NULL,
        [Summary] nvarchar(2000) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_LegalDocumentFiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LegalDocumentFiles_LegalDocuments_PlatformLegalDocumentId] FOREIGN KEY ([PlatformLegalDocumentId]) REFERENCES [platform].[LegalDocuments] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE TABLE [platform].[LegalDocumentParts] (
        [Id] int NOT NULL IDENTITY,
        [IncludedDocumentId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Version] nvarchar(20) NOT NULL,
        [PlatformLegalDocumentId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_LegalDocumentParts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LegalDocumentParts_LegalDocuments_IncludedDocumentId] FOREIGN KEY ([IncludedDocumentId]) REFERENCES [platform].[LegalDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LegalDocumentParts_LegalDocuments_PlatformLegalDocumentId] FOREIGN KEY ([PlatformLegalDocumentId]) REFERENCES [platform].[LegalDocuments] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE TABLE [platform].[TenantTermsAcceptances] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [PlatformLegalDocumentId] uniqueidentifier NOT NULL,
        [Version] nvarchar(20) NOT NULL,
        [Sha256] char(64) NOT NULL,
        [Language] varchar(8) NOT NULL,
        [Source] int NOT NULL,
        [AcceptedUtc] datetimeoffset NOT NULL,
        [ActorReference] nvarchar(100) NULL,
        [ActorName] nvarchar(120) NULL,
        [ActorRole] varchar(40) NULL,
        [ClientIp] varchar(45) NULL,
        [UserAgent] nvarchar(400) NULL,
        [PlatformTenantRegistrationId] uniqueidentifier NULL,
        [RecordedByUserId] uniqueidentifier NULL,
        [Reason] nvarchar(500) NULL,
        [Evidence] varbinary(max) NULL,
        [EvidenceContentType] varchar(40) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TenantTermsAcceptances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantTermsAcceptances_LegalDocuments_PlatformLegalDocumentId] FOREIGN KEY ([PlatformLegalDocumentId]) REFERENCES [platform].[LegalDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantTermsAcceptances_TenantRegistrations_PlatformTenantRegistrationId] FOREIGN KEY ([PlatformTenantRegistrationId]) REFERENCES [platform].[TenantRegistrations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantTermsAcceptances_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantTermsAcceptances_Users_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE TABLE [platform].[TermsReminders] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [PlatformLegalDocumentId] uniqueidentifier NOT NULL,
        [Step] int NOT NULL,
        [Language] varchar(8) NOT NULL,
        [SentByUserId] uniqueidentifier NULL,
        [Reason] nvarchar(500) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TermsReminders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TermsReminders_LegalDocuments_PlatformLegalDocumentId] FOREIGN KEY ([PlatformLegalDocumentId]) REFERENCES [platform].[LegalDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TermsReminders_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TermsReminders_Users_SentByUserId] FOREIGN KEY ([SentByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LegalDocumentFiles_PlatformLegalDocumentId_Language] ON [platform].[LegalDocumentFiles] ([PlatformLegalDocumentId], [Language]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE INDEX [IX_LegalDocumentParts_IncludedDocumentId] ON [platform].[LegalDocumentParts] ([IncludedDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE INDEX [IX_LegalDocumentParts_PlatformLegalDocumentId] ON [platform].[LegalDocumentParts] ([PlatformLegalDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE INDEX [IX_LegalDocuments_CreatedByUserId] ON [platform].[LegalDocuments] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE INDEX [IX_LegalDocuments_Kind_PublishedUtc] ON [platform].[LegalDocuments] ([Kind], [PublishedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LegalDocuments_Kind_Version] ON [platform].[LegalDocuments] ([Kind], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LegalDocuments_OnePublished] ON [platform].[LegalDocuments] ([Kind]) WHERE [Status] = 2');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE INDEX [IX_LegalDocuments_PublishedByUserId] ON [platform].[LegalDocuments] ([PublishedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE INDEX [IX_TenantTermsAcceptances_PlatformLegalDocumentId] ON [platform].[TenantTermsAcceptances] ([PlatformLegalDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantTermsAcceptances_PlatformTenantId_PlatformLegalDocumentId] ON [platform].[TenantTermsAcceptances] ([PlatformTenantId], [PlatformLegalDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE INDEX [IX_TenantTermsAcceptances_PlatformTenantRegistrationId] ON [platform].[TenantTermsAcceptances] ([PlatformTenantRegistrationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE INDEX [IX_TenantTermsAcceptances_RecordedByUserId] ON [platform].[TenantTermsAcceptances] ([RecordedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TermsReminders_OncePerStep] ON [platform].[TermsReminders] ([PlatformTenantId], [PlatformLegalDocumentId], [Step]) WHERE [Step] <> 20');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE INDEX [IX_TermsReminders_PlatformLegalDocumentId] ON [platform].[TermsReminders] ([PlatformLegalDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE INDEX [IX_TermsReminders_PlatformTenantId_CreatedUtc] ON [platform].[TermsReminders] ([PlatformTenantId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    CREATE INDEX [IX_TermsReminders_SentByUserId] ON [platform].[TermsReminders] ([SentByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016091612_Legal'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016091612_Legal', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016110140_Contracts'
)
BEGIN
    CREATE TABLE [platform].[PublishedEvents] (
        [Id] uniqueidentifier NOT NULL,
        [Sequence] bigint NOT NULL IDENTITY,
        [Type] varchar(100) NOT NULL,
        [PlatformTenantId] uniqueidentifier NULL,
        [OutboxMessageId] uniqueidentifier NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [OccurredUtc] datetimeoffset NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_PublishedEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016110140_Contracts'
)
BEGIN
    CREATE INDEX [IX_PublishedEvents_CreatedUtc] ON [platform].[PublishedEvents] ([CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016110140_Contracts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PublishedEvents_OutboxMessageId_Type] ON [platform].[PublishedEvents] ([OutboxMessageId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016110140_Contracts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PublishedEvents_Sequence] ON [platform].[PublishedEvents] ([Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016110140_Contracts'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016110140_Contracts', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016175744_Leads'
)
BEGIN
    CREATE TABLE [platform].[SalesLeads] (
        [Id] uniqueidentifier NOT NULL,
        [CompanyName] nvarchar(200) NOT NULL,
        [ContactName] nvarchar(120) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [Phone] varchar(40) NULL,
        [CountryCode] char(2) NULL,
        [Kind] int NOT NULL,
        [Message] nvarchar(2000) NULL,
        [Language] varchar(8) NOT NULL,
        [SourcePage] nvarchar(100) NULL,
        [ClientIp] varchar(45) NULL,
        [Status] int NOT NULL,
        [StatusChangedUtc] datetimeoffset NOT NULL,
        [StaffNote] nvarchar(1000) NULL,
        [HandledByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_SalesLeads] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalesLeads_Users_HandledByUserId] FOREIGN KEY ([HandledByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016175744_Leads'
)
BEGIN
    CREATE INDEX [IX_SalesLeads_HandledByUserId] ON [platform].[SalesLeads] ([HandledByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016175744_Leads'
)
BEGIN
    CREATE INDEX [IX_SalesLeads_Status_CreatedUtc] ON [platform].[SalesLeads] ([Status], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016175744_Leads'
)
BEGIN
    CREATE INDEX [IX_SalesLeads_Status_StatusChangedUtc] ON [platform].[SalesLeads] ([Status], [StatusChangedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016175744_Leads'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016175744_Leads', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016214930_TenantChanges'
)
BEGIN
    CREATE TABLE [platform].[TenantChangeRequests] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [Status] int NOT NULL,
        [SubmittedByReference] nvarchar(200) NOT NULL,
        [SubmittedByName] nvarchar(200) NOT NULL,
        [DecidedUtc] datetimeoffset NULL,
        [DecidedByUserId] uniqueidentifier NULL,
        [DecisionNote] nvarchar(1000) NULL,
        [RowVersion] rowversion NULL,
        [PreviousEmail] nvarchar(254) NOT NULL,
        [PreviousLegalName] nvarchar(200) NOT NULL,
        [PreviousPhone] varchar(16) NOT NULL,
        [PreviousTradingName] nvarchar(200) NULL,
        [PreviousCity] nvarchar(100) NOT NULL,
        [PreviousCountryCode] char(2) NOT NULL,
        [PreviousAddressLine1] nvarchar(200) NOT NULL,
        [PreviousAddressLine2] nvarchar(200) NULL,
        [PreviousPostalCode] nvarchar(20) NULL,
        [PreviousStateProvince] nvarchar(100) NULL,
        [ProposedEmail] nvarchar(254) NOT NULL,
        [ProposedLegalName] nvarchar(200) NOT NULL,
        [ProposedPhone] varchar(16) NOT NULL,
        [ProposedTradingName] nvarchar(200) NULL,
        [ProposedCity] nvarchar(100) NOT NULL,
        [ProposedCountryCode] char(2) NOT NULL,
        [ProposedAddressLine1] nvarchar(200) NOT NULL,
        [ProposedAddressLine2] nvarchar(200) NULL,
        [ProposedPostalCode] nvarchar(20) NULL,
        [ProposedStateProvince] nvarchar(100) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_TenantChangeRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantChangeRequests_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantChangeRequests_Users_DecidedByUserId] FOREIGN KEY ([DecidedByUserId]) REFERENCES [platform].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016214930_TenantChanges'
)
BEGIN
    CREATE TABLE [platform].[TenantChangeProofs] (
        [PlatformTenantChangeRequestId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(200) NOT NULL,
        [ContentType] varchar(50) NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [SizeBytes] int NOT NULL,
        [Sha256] char(64) NOT NULL,
        CONSTRAINT [PK_TenantChangeProofs] PRIMARY KEY ([PlatformTenantChangeRequestId]),
        CONSTRAINT [FK_TenantChangeProofs_TenantChangeRequests_PlatformTenantChangeRequestId] FOREIGN KEY ([PlatformTenantChangeRequestId]) REFERENCES [platform].[TenantChangeRequests] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016214930_TenantChanges'
)
BEGIN
    CREATE INDEX [IX_TenantChangeRequests_DecidedByUserId] ON [platform].[TenantChangeRequests] ([DecidedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016214930_TenantChanges'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TenantChangeRequests_OnePending] ON [platform].[TenantChangeRequests] ([PlatformTenantId]) WHERE [Status] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016214930_TenantChanges'
)
BEGIN
    CREATE INDEX [IX_TenantChangeRequests_PlatformTenantId_CreatedUtc] ON [platform].[TenantChangeRequests] ([PlatformTenantId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016214930_TenantChanges'
)
BEGIN
    CREATE INDEX [IX_TenantChangeRequests_Status_CreatedUtc] ON [platform].[TenantChangeRequests] ([Status], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016214930_TenantChanges'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016214930_TenantChanges', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018071633_OwnerSupport'
)
BEGIN
    ALTER TABLE [platform].[DeviceLinks] ADD [SupportRequestId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018071633_OwnerSupport'
)
BEGIN
    CREATE TABLE [platform].[OwnerSupportRequests] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformTenantId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [IdentityCheck] nvarchar(500) NOT NULL,
        [ReplaceDeviceId] uniqueidentifier NULL,
        [RequestedBy] uniqueidentifier NOT NULL,
        [RequestedUtc] datetimeoffset NOT NULL,
        [Status] int NOT NULL,
        [DecidedBy] uniqueidentifier NULL,
        [DecidedUtc] datetimeoffset NULL,
        [DecisionNote] nvarchar(500) NULL,
        [CompletedUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_OwnerSupportRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OwnerSupportRequests_Devices_ReplaceDeviceId] FOREIGN KEY ([ReplaceDeviceId]) REFERENCES [platform].[Devices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OwnerSupportRequests_Tenants_PlatformTenantId] FOREIGN KEY ([PlatformTenantId]) REFERENCES [platform].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018071633_OwnerSupport'
)
BEGIN
    CREATE INDEX [IX_DeviceLinks_SupportRequestId] ON [platform].[DeviceLinks] ([SupportRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018071633_OwnerSupport'
)
BEGIN
    CREATE INDEX [IX_OwnerSupportRequests_PlatformTenantId_Status] ON [platform].[OwnerSupportRequests] ([PlatformTenantId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018071633_OwnerSupport'
)
BEGIN
    CREATE INDEX [IX_OwnerSupportRequests_ReplaceDeviceId] ON [platform].[OwnerSupportRequests] ([ReplaceDeviceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018071633_OwnerSupport'
)
BEGIN
    ALTER TABLE [platform].[DeviceLinks] ADD CONSTRAINT [FK_DeviceLinks_OwnerSupportRequests_SupportRequestId] FOREIGN KEY ([SupportRequestId]) REFERENCES [platform].[OwnerSupportRequests] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [platform].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018071633_OwnerSupport'
)
BEGIN
    INSERT INTO [platform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018071633_OwnerSupport', N'10.0.13');
END;

COMMIT;
GO

-- =========================================================
-- 3. OPERATIONS PLANE (schema [ops])
-- Generated from the EF migrations of
-- Vixly.Operations.Infrastructure: OperationsFoundation, StaffSignIn, TenantSite, ContactPhones, StaffAndRoles, TenantSettings, ShippingServices, Customers, CustomersSelfService, TrackingCore, Receiving, ReceivingStations, CustomerShipments, InspectionAndHolds, HoldAnswersAndUnclaimed, ChargesEngine, ZonesAndPriceVersions, RoutesAndModes, OriginIntake, ShippingLabels, UndeclaredParcels, Tickets, ShipmentsDesk, InsuranceModule, FrontDeskCounter, InsuranceProducts, CounterSignature, SuppliesCatalogue, SuppliesStock, SuppliesAtCounter, InvoicesAndCreditNotes
-- =========================================================
IF OBJECT_ID(N'[ops].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'ops') IS NULL EXEC(N'CREATE SCHEMA [ops];');
    CREATE TABLE [ops].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024152_OperationsFoundation'
)
BEGIN
    IF SCHEMA_ID(N'ops') IS NULL EXEC(N'CREATE SCHEMA [ops];');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024152_OperationsFoundation'
)
BEGIN
    CREATE TABLE [ops].[DataProtectionKeys] (
        [Id] int NOT NULL IDENTITY,
        [FriendlyName] nvarchar(max) NULL,
        [Xml] nvarchar(max) NULL,
        CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024152_OperationsFoundation'
)
BEGIN
    CREATE TABLE [ops].[JobRuns] (
        [Id] uniqueidentifier NOT NULL,
        [Job] varchar(100) NOT NULL,
        [FinishedUtc] datetimeoffset NULL,
        [Handled] int NOT NULL,
        [Failed] int NOT NULL,
        [ErrorCode] varchar(100) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_JobRuns] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024152_OperationsFoundation'
)
BEGIN
    CREATE TABLE [ops].[OutboxMessages] (
        [Id] uniqueidentifier NOT NULL,
        [Type] varchar(100) NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [StreamId] uniqueidentifier NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [ProcessedUtc] datetimeoffset NULL,
        [Attempts] int NOT NULL,
        [NextAttemptUtc] datetimeoffset NULL,
        [LastErrorCode] varchar(100) NULL,
        [LeasedUntilUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024152_OperationsFoundation'
)
BEGIN
    CREATE TABLE [ops].[PlatformFeedPositions] (
        [Id] uniqueidentifier NOT NULL,
        [Feed] varchar(50) NOT NULL,
        [Position] bigint NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        CONSTRAINT [PK_PlatformFeedPositions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024152_OperationsFoundation'
)
BEGIN
    CREATE INDEX [IX_JobRuns_Job_CreatedUtc] ON [ops].[JobRuns] ([Job], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024152_OperationsFoundation'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_OutboxMessages_NextAttemptUtc_CreatedUtc] ON [ops].[OutboxMessages] ([NextAttemptUtc], [CreatedUtc]) WHERE [ProcessedUtc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024152_OperationsFoundation'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_OutboxMessages_StreamId_CreatedUtc] ON [ops].[OutboxMessages] ([StreamId], [CreatedUtc]) WHERE [ProcessedUtc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024152_OperationsFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlatformFeedPositions_Feed] ON [ops].[PlatformFeedPositions] ([Feed]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024152_OperationsFoundation'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018024152_OperationsFoundation', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE TABLE [ops].[AuditEntries] (
        [Id] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [ActionCode] varchar(100) NOT NULL,
        [PermissionCode] varchar(100) NULL,
        [TargetType] varchar(100) NOT NULL,
        [TargetId] uniqueidentifier NULL,
        [ArgumentsJson] nvarchar(max) NULL,
        [BeforeJson] nvarchar(max) NULL,
        [AfterJson] nvarchar(max) NULL,
        [Reason] nvarchar(500) NULL,
        [IpAddress] varchar(45) NULL,
        [UserAgent] nvarchar(512) NULL,
        [CorrelationId] varchar(64) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AuditEntries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE TABLE [ops].[TenantBrands] (
        [Id] uniqueidentifier NOT NULL,
        [DisplayName] nvarchar(150) NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_TenantBrands] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE TABLE [ops].[Users] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(200) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [PreferredLanguage] nvarchar(8) NOT NULL,
        [IsOwner] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE TABLE [ops].[AccountTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Purpose] int NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [UsedUtc] datetimeoffset NULL,
        [RevokedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AccountTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [ops].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE TABLE [ops].[SignInChallenges] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [Stage] int NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [FailedAttempts] int NOT NULL,
        [CompletedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_SignInChallenges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SignInChallenges_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [ops].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE TABLE [ops].[UserAccounts] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [PasswordHash] varchar(200) NULL,
        [FailedSignInCount] int NOT NULL,
        [SignInLockoutEndsUtc] datetimeoffset NULL,
        [LastSignInUtc] datetimeoffset NULL,
        [ActivatedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserAccounts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [ops].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE TABLE [ops].[UserAuthenticatorApps] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [EncryptedSecret] varchar(1000) NOT NULL,
        [ConfirmedUtc] datetimeoffset NULL,
        [LastAcceptedTimeStep] bigint NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserAuthenticatorApps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserAuthenticatorApps_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [ops].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE TABLE [ops].[UserRecoveryCodes] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [CodeHash] char(64) NOT NULL,
        [UsedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserRecoveryCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserRecoveryCodes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [ops].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE TABLE [ops].[UserSessions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [IpAddress] varchar(45) NULL,
        [UserAgent] nvarchar(512) NULL,
        [IsSetupSession] bit NOT NULL,
        [LastSeenUtc] datetimeoffset NOT NULL,
        [SecondFactorUtc] datetimeoffset NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [EndedUtc] datetimeoffset NULL,
        [EndReason] int NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [ops].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_AccountTokens_TenantId] ON [ops].[AccountTokens] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountTokens_TokenHash] ON [ops].[AccountTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_AccountTokens_UserId_Purpose] ON [ops].[AccountTokens] ([UserId], [Purpose]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_TenantId] ON [ops].[AuditEntries] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_TenantId_ActorUserId_CreatedUtc] ON [ops].[AuditEntries] ([TenantId], [ActorUserId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_TenantId_CreatedUtc] ON [ops].[AuditEntries] ([TenantId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_TenantId_TargetId_CreatedUtc] ON [ops].[AuditEntries] ([TenantId], [TargetId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_SignInChallenges_ExpiresUtc] ON [ops].[SignInChallenges] ([ExpiresUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_SignInChallenges_TenantId] ON [ops].[SignInChallenges] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SignInChallenges_TokenHash] ON [ops].[SignInChallenges] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_SignInChallenges_UserId] ON [ops].[SignInChallenges] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantBrands_TenantId_One] ON [ops].[TenantBrands] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_UserAccounts_TenantId] ON [ops].[UserAccounts] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserAccounts_UserId] ON [ops].[UserAccounts] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_UserAuthenticatorApps_TenantId] ON [ops].[UserAuthenticatorApps] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserAuthenticatorApps_UserId] ON [ops].[UserAuthenticatorApps] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_UserRecoveryCodes_TenantId] ON [ops].[UserRecoveryCodes] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_UserRecoveryCodes_UserId] ON [ops].[UserRecoveryCodes] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_TenantId_Email] ON [ops].[Users] ([TenantId], [Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_TenantId_Owner] ON [ops].[Users] ([TenantId]) WHERE [IsOwner] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_UserSessions_TenantId] ON [ops].[UserSessions] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserSessions_TokenHash] ON [ops].[UserSessions] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    CREATE INDEX [IX_UserSessions_UserId_EndedUtc] ON [ops].[UserSessions] ([UserId], [EndedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018035749_StaffSignIn'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018035749_StaffSignIn', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [DraftChangedUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_AccentColour] char(7) NOT NULL DEFAULT '#0E7490';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_DisplayName] nvarchar(150) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_FacebookUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_FaviconImageId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_HeroImageId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_InstagramUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_LinkedInUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_LogoDarkImageId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_LogoImageId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_Preset] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_PrimaryColour] char(7) NOT NULL DEFAULT '#1D4ED8';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_Services] int NOT NULL DEFAULT 63;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_ShowFaq] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_ShowSteps] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_TikTokUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_XUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Draft_YouTubeUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [PublishedUtc] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [PublishedVersion] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_AccentColour] char(7) NOT NULL DEFAULT '#0E7490';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_FacebookUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_FaviconImageId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_HeroImageId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_InstagramUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_LinkedInUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_LogoDarkImageId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_LogoImageId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_Preset] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_PrimaryColour] char(7) NOT NULL DEFAULT '#1D4ED8';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_Services] int NOT NULL DEFAULT 63;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_ShowFaq] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_ShowSteps] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_TikTokUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_XUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    ALTER TABLE [ops].[TenantBrands] ADD [Published_YouTubeUrl] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    EXEC(N'UPDATE [ops].[TenantBrands] SET [Draft_DisplayName] = [DisplayName] WHERE [Draft_DisplayName] = N''''');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE TABLE [ops].[Branches] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(12) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Address_Line1] nvarchar(200) NOT NULL,
        [Address_Line2] nvarchar(200) NULL,
        [Address_City] nvarchar(100) NOT NULL,
        [Address_Region] nvarchar(100) NULL,
        [Address_PostalCode] nvarchar(20) NULL,
        [Address_CountryCode] char(2) NOT NULL,
        [Phone] varchar(20) NULL,
        [Email] nvarchar(254) NULL,
        [OpeningHours] nvarchar(200) NULL,
        [Side] int NOT NULL,
        [Status] int NOT NULL,
        [ShowOnSite] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE TABLE [ops].[CountryContacts] (
        [Id] uniqueidentifier NOT NULL,
        [CountryCode] char(2) NOT NULL,
        [Label] nvarchar(60) NULL,
        [Phone] varchar(20) NOT NULL,
        [WhatsApp] varchar(20) NULL,
        [Email] nvarchar(254) NULL,
        [Hours] nvarchar(200) NULL,
        [SortOrder] int NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_CountryContacts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE TABLE [ops].[SiteImages] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [ContentType] varchar(20) NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [Width] int NOT NULL,
        [Height] int NOT NULL,
        [Sha256] char(64) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_SiteImages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE TABLE [ops].[SiteTexts] (
        [Id] uniqueidentifier NOT NULL,
        [BrandId] uniqueidentifier NOT NULL,
        [Stage] int NOT NULL,
        [Language] varchar(8) NOT NULL,
        [Key] varchar(40) NOT NULL,
        [Value] nvarchar(600) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_SiteTexts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SiteTexts_TenantBrands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [ops].[TenantBrands] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE TABLE [ops].[Warehouses] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [Code] varchar(12) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Address_Line1] nvarchar(200) NOT NULL,
        [Address_Line2] nvarchar(200) NULL,
        [Address_City] nvarchar(100) NOT NULL,
        [Address_Region] nvarchar(100) NULL,
        [Address_PostalCode] nvarchar(20) NULL,
        [Address_CountryCode] char(2) NOT NULL,
        [Phone] varchar(20) NULL,
        [Email] nvarchar(254) NULL,
        [OpeningHours] nvarchar(200) NULL,
        [Roles] int NOT NULL,
        [AcceptingDeliveries] bit NOT NULL,
        [Status] int NOT NULL,
        [ShowOnSite] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouses_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [ops].[Branches] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE INDEX [IX_Branches_TenantId] ON [ops].[Branches] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Branches_TenantId_Code] ON [ops].[Branches] ([TenantId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE INDEX [IX_CountryContacts_TenantId] ON [ops].[CountryContacts] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE INDEX [IX_SiteImages_TenantId] ON [ops].[SiteImages] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SiteTexts_BrandId_Stage_Language_Key] ON [ops].[SiteTexts] ([BrandId], [Stage], [Language], [Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE INDEX [IX_SiteTexts_TenantId] ON [ops].[SiteTexts] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE INDEX [IX_Warehouses_BranchId] ON [ops].[Warehouses] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE INDEX [IX_Warehouses_TenantId] ON [ops].[Warehouses] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Warehouses_TenantId_Code] ON [ops].[Warehouses] ([TenantId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018044825_TenantSite'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018044825_TenantSite', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018061716_ContactPhones'
)
BEGIN
    ALTER TABLE [ops].[CountryContacts] ADD [Phones] varchar(252) NOT NULL DEFAULT '';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018061716_ContactPhones'
)
BEGIN
    EXEC(N'UPDATE [ops].[CountryContacts] SET [Phones] = [Phone] WHERE [Phones] = N''''');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018061716_ContactPhones'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[CountryContacts]') AND [c].[name] = N'Phone');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [ops].[CountryContacts] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [ops].[CountryContacts] DROP COLUMN [Phone];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018061716_ContactPhones'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018061716_ContactPhones', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018073015_StaffAndRoles'
)
BEGIN
    ALTER TABLE [ops].[Users] ADD [IsAdministrator] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018073015_StaffAndRoles'
)
BEGIN
    ALTER TABLE [ops].[UserAccounts] ADD [StatusChangedUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018073015_StaffAndRoles'
)
BEGIN
    ALTER TABLE [ops].[UserAccounts] ADD [StatusReason] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018073015_StaffAndRoles'
)
BEGIN
    CREATE TABLE [ops].[RoleGrants] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        [Scope] int NOT NULL,
        [ScopeId] uniqueidentifier NULL,
        [GrantedBy] uniqueidentifier NULL,
        [GrantedUtc] datetimeoffset NOT NULL,
        [RevokedBy] uniqueidentifier NULL,
        [RevokedUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_RoleGrants] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018073015_StaffAndRoles'
)
BEGIN
    CREATE TABLE [ops].[Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(60) NOT NULL,
        [IsAdministrator] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Permissions] varchar(2000) NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018073015_StaffAndRoles'
)
BEGIN
    CREATE INDEX [IX_RoleGrants_TenantId] ON [ops].[RoleGrants] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018073015_StaffAndRoles'
)
BEGIN
    CREATE INDEX [IX_RoleGrants_TenantId_RoleId] ON [ops].[RoleGrants] ([TenantId], [RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018073015_StaffAndRoles'
)
BEGIN
    CREATE INDEX [IX_RoleGrants_TenantId_UserId] ON [ops].[RoleGrants] ([TenantId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018073015_StaffAndRoles'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Roles_TenantId_Administrator] ON [ops].[Roles] ([TenantId]) WHERE [IsAdministrator] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018073015_StaffAndRoles'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_TenantId_Name] ON [ops].[Roles] ([TenantId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018073015_StaffAndRoles'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018073015_StaffAndRoles', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018081207_TenantSettings'
)
BEGIN
    CREATE TABLE [ops].[NumberSequences] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Year] int NOT NULL,
        [LastNumber] bigint NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_NumberSequences] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018081207_TenantSettings'
)
BEGIN
    CREATE TABLE [ops].[TenantSettings] (
        [Id] uniqueidentifier NOT NULL,
        [CurrencyCode] char(3) NOT NULL,
        [WeightUnit] int NOT NULL,
        [DimensionUnit] int NOT NULL,
        [AllowCollect] bit NOT NULL,
        [AllowCredit] bit NOT NULL,
        [DeliveryModes] int NOT NULL,
        [RequireSecondFactorForAllStaff] bit NOT NULL,
        [ShipmentPrefix] varchar(6) NOT NULL,
        [ReceivingPrefix] varchar(6) NOT NULL,
        [InvoicePrefix] varchar(6) NOT NULL,
        [PaymentPrefix] varchar(6) NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_TenantSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018081207_TenantSettings'
)
BEGIN
    CREATE INDEX [IX_NumberSequences_TenantId] ON [ops].[NumberSequences] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018081207_TenantSettings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NumberSequences_TenantId_Kind_Year] ON [ops].[NumberSequences] ([TenantId], [Kind], [Year]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018081207_TenantSettings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantSettings_TenantId] ON [ops].[TenantSettings] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018081207_TenantSettings'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018081207_TenantSettings', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018131846_ShippingServices'
)
BEGIN
    CREATE TABLE [ops].[ShippingServiceSetups] (
        [Id] uniqueidentifier NOT NULL,
        [ServiceCode] varchar(40) NOT NULL,
        [Offered] bit NOT NULL,
        [Label] nvarchar(60) NULL,
        [MinDays] int NOT NULL,
        [MaxDays] int NOT NULL,
        [VolumetricDivisor] decimal(10,3) NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ShippingServiceSetups] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018131846_ShippingServices'
)
BEGIN
    CREATE INDEX [IX_ShippingServiceSetups_TenantId] ON [ops].[ShippingServiceSetups] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018131846_ShippingServices'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShippingServiceSetups_TenantId_ServiceCode] ON [ops].[ShippingServiceSetups] ([TenantId], [ServiceCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018131846_ShippingServices'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018131846_ShippingServices', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [CustomerPrefix] varchar(6) NOT NULL DEFAULT 'C';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE TABLE [ops].[Customers] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(20) NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [CompanyName] nvarchar(150) NULL,
        [Email] nvarchar(254) NOT NULL,
        [Phone] varchar(20) NOT NULL,
        [PreferredLanguage] nvarchar(8) NOT NULL,
        [Status] int NOT NULL,
        [StatusReason] nvarchar(300) NULL,
        [StatusChangedUtc] datetimeoffset NULL,
        [CreditAllowed] bit NOT NULL,
        [CreditReason] nvarchar(300) NULL,
        [CreditChangedUtc] datetimeoffset NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [PasswordHash] varchar(200) NULL,
        [FailedSignInCount] int NOT NULL,
        [LockoutEndsUtc] datetimeoffset NULL,
        [EmailVerifiedUtc] datetimeoffset NULL,
        [LastSignInUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE TABLE [ops].[CustomerSessions] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [IpAddress] varchar(45) NULL,
        [UserAgent] nvarchar(512) NULL,
        [LastSeenUtc] datetimeoffset NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [EndedUtc] datetimeoffset NULL,
        [EndReason] int NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_CustomerSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerSessions_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [ops].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE TABLE [ops].[CustomerTokens] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [Purpose] int NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [UsedUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_CustomerTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerTokens_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [ops].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE INDEX [IX_Customers_TenantId] ON [ops].[Customers] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_TenantId_Code] ON [ops].[Customers] ([TenantId], [Code]) WHERE [Code] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_TenantId_Email] ON [ops].[Customers] ([TenantId], [Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE INDEX [IX_Customers_TenantId_LastName_FirstName] ON [ops].[Customers] ([TenantId], [LastName], [FirstName]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE INDEX [IX_Customers_TenantId_Phone] ON [ops].[Customers] ([TenantId], [Phone]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE INDEX [IX_CustomerSessions_CustomerId] ON [ops].[CustomerSessions] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE INDEX [IX_CustomerSessions_TenantId] ON [ops].[CustomerSessions] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE INDEX [IX_CustomerSessions_TenantId_CustomerId_EndedUtc] ON [ops].[CustomerSessions] ([TenantId], [CustomerId], [EndedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerSessions_TokenHash] ON [ops].[CustomerSessions] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE INDEX [IX_CustomerTokens_CustomerId] ON [ops].[CustomerTokens] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE INDEX [IX_CustomerTokens_TenantId] ON [ops].[CustomerTokens] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE INDEX [IX_CustomerTokens_TenantId_CustomerId_Purpose] ON [ops].[CustomerTokens] ([TenantId], [CustomerId], [Purpose]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerTokens_TokenHash] ON [ops].[CustomerTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132848_Customers'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018132848_Customers', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018191409_CustomersSelfService'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[Customers]') AND [c].[name] = N'CreatedByUserId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ops].[Customers] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [ops].[Customers] DROP COLUMN [CreatedByUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018191409_CustomersSelfService'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018191409_CustomersSelfService', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018205702_TrackingCore'
)
BEGIN
    ALTER TABLE [ops].[Customers] ADD CONSTRAINT [AK_Customers_TenantId_Id] UNIQUE ([TenantId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018205702_TrackingCore'
)
BEGIN
    CREATE TABLE [ops].[Parcels] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NULL,
        [Milestone] int NULL,
        [Flags] int NOT NULL,
        [HoldReasons] int NOT NULL,
        [LastEventUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Parcels] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Parcels_Customers_TenantId_CustomerId] FOREIGN KEY ([TenantId], [CustomerId]) REFERENCES [ops].[Customers] ([TenantId], [Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018205702_TrackingCore'
)
BEGIN
    CREATE TABLE [ops].[TrackingEvents] (
        [Id] uniqueidentifier NOT NULL,
        [SubjectKind] int NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        [Code] varchar(100) NOT NULL,
        [ArgumentsJson] nvarchar(2000) NULL,
        [OccurredUtc] datetimeoffset NOT NULL,
        [TimeZone] varchar(64) NULL,
        [LocationCode] varchar(32) NULL,
        [Actor] int NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [DeviceId] uniqueidentifier NULL,
        [Source] int NOT NULL,
        [Visibility] int NOT NULL,
        [IdempotencyKey] varchar(100) NOT NULL,
        [VoidsEventId] uniqueidentifier NULL,
        [Note] nvarchar(500) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_TrackingEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018205702_TrackingCore'
)
BEGIN
    CREATE INDEX [IX_Parcels_TenantId] ON [ops].[Parcels] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018205702_TrackingCore'
)
BEGIN
    CREATE INDEX [IX_Parcels_TenantId_CustomerId] ON [ops].[Parcels] ([TenantId], [CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018205702_TrackingCore'
)
BEGIN
    CREATE INDEX [IX_Parcels_TenantId_Milestone] ON [ops].[Parcels] ([TenantId], [Milestone]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018205702_TrackingCore'
)
BEGIN
    CREATE INDEX [IX_TrackingEvents_TenantId] ON [ops].[TrackingEvents] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018205702_TrackingCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TrackingEvents_TenantId_IdempotencyKey] ON [ops].[TrackingEvents] ([TenantId], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018205702_TrackingCore'
)
BEGIN
    CREATE INDEX [IX_TrackingEvents_TenantId_SubjectKind_SubjectId_OccurredUtc] ON [ops].[TrackingEvents] ([TenantId], [SubjectKind], [SubjectId], [OccurredUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018205702_TrackingCore'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TrackingEvents_TenantId_VoidsEventId] ON [ops].[TrackingEvents] ([TenantId], [VoidsEventId]) WHERE [VoidsEventId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018205702_TrackingCore'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018205702_TrackingCore', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [ParcelPrefix] varchar(6) NOT NULL DEFAULT 'PK';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [Carrier] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [CarrierTrackingNumber] varchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [CurrencyCode] varchar(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [DeclaredContents] nvarchar(300) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [DeclaredValue] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [HeightCm] decimal(7,1) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [LengthCm] decimal(7,1) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [Number] varchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [OriginKey] varchar(80) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [PreAlertId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [ShipmentId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [StorageLocationId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [WarehouseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [WeightKg] decimal(9,3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [WidthCm] decimal(7,1) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE TABLE [ops].[ParcelPhotos] (
        [Id] uniqueidentifier NOT NULL,
        [ParcelId] uniqueidentifier NOT NULL,
        [Kind] varchar(20) NOT NULL,
        [StorageKey] varchar(200) NOT NULL,
        [ContentType] varchar(50) NOT NULL,
        [SizeBytes] int NOT NULL,
        [Sha256] varchar(64) NOT NULL,
        [TakenByUserId] uniqueidentifier NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ParcelPhotos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ParcelPhotos_Parcels_ParcelId] FOREIGN KEY ([ParcelId]) REFERENCES [ops].[Parcels] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE TABLE [ops].[PreAlerts] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [Carrier] nvarchar(40) NOT NULL,
        [TrackingNumber] varchar(40) NOT NULL,
        [Store] nvarchar(100) NULL,
        [Contents] nvarchar(300) NOT NULL,
        [DeclaredValue] decimal(12,2) NOT NULL,
        [CurrencyCode] varchar(3) NOT NULL,
        [ExpectedDate] date NULL,
        [Status] int NOT NULL,
        [ParcelId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_PreAlerts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PreAlerts_Customers_TenantId_CustomerId] FOREIGN KEY ([TenantId], [CustomerId]) REFERENCES [ops].[Customers] ([TenantId], [Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE TABLE [ops].[Shipments] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [OriginWarehouseId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Shipments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Shipments_Customers_TenantId_CustomerId] FOREIGN KEY ([TenantId], [CustomerId]) REFERENCES [ops].[Customers] ([TenantId], [Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE TABLE [ops].[StorageLocations] (
        [Id] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Code] varchar(20) NOT NULL,
        [Zone] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_StorageLocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StorageLocations_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE TABLE [ops].[WarehouseReceipts] (
        [Id] uniqueidentifier NOT NULL,
        [Number] varchar(20) NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [ParcelId] uniqueidentifier NOT NULL,
        [ReceivedByUserId] uniqueidentifier NOT NULL,
        [Condition] varchar(30) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_WarehouseReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarehouseReceipts_Parcels_ParcelId] FOREIGN KEY ([ParcelId]) REFERENCES [ops].[Parcels] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_Parcels_PreAlertId] ON [ops].[Parcels] ([PreAlertId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_Parcels_ShipmentId] ON [ops].[Parcels] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_Parcels_StorageLocationId] ON [ops].[Parcels] ([StorageLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Parcels_TenantId_Number] ON [ops].[Parcels] ([TenantId], [Number]) WHERE [Number] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Parcels_TenantId_OriginKey] ON [ops].[Parcels] ([TenantId], [OriginKey]) WHERE [OriginKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_Parcels_TenantId_ShipmentId] ON [ops].[Parcels] ([TenantId], [ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_Parcels_TenantId_WarehouseId_CarrierTrackingNumber] ON [ops].[Parcels] ([TenantId], [WarehouseId], [CarrierTrackingNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_Parcels_WarehouseId] ON [ops].[Parcels] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_ParcelPhotos_ParcelId] ON [ops].[ParcelPhotos] ([ParcelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_ParcelPhotos_TenantId] ON [ops].[ParcelPhotos] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_ParcelPhotos_TenantId_ParcelId] ON [ops].[ParcelPhotos] ([TenantId], [ParcelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_PreAlerts_TenantId] ON [ops].[PreAlerts] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_PreAlerts_TenantId_CustomerId] ON [ops].[PreAlerts] ([TenantId], [CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PreAlerts_TenantId_CustomerId_TrackingNumber] ON [ops].[PreAlerts] ([TenantId], [CustomerId], [TrackingNumber]) WHERE [Status] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_PreAlerts_TenantId_TrackingNumber_Status] ON [ops].[PreAlerts] ([TenantId], [TrackingNumber], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_Shipments_TenantId] ON [ops].[Shipments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Shipments_TenantId_CustomerId_OriginWarehouseId] ON [ops].[Shipments] ([TenantId], [CustomerId], [OriginWarehouseId]) WHERE [Status] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_StorageLocations_TenantId] ON [ops].[StorageLocations] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StorageLocations_TenantId_WarehouseId_Code] ON [ops].[StorageLocations] ([TenantId], [WarehouseId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_StorageLocations_WarehouseId] ON [ops].[StorageLocations] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_WarehouseReceipts_ParcelId] ON [ops].[WarehouseReceipts] ([ParcelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_WarehouseReceipts_TenantId] ON [ops].[WarehouseReceipts] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WarehouseReceipts_TenantId_Number] ON [ops].[WarehouseReceipts] ([TenantId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    CREATE INDEX [IX_WarehouseReceipts_TenantId_ParcelId] ON [ops].[WarehouseReceipts] ([TenantId], [ParcelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD CONSTRAINT [FK_Parcels_PreAlerts_PreAlertId] FOREIGN KEY ([PreAlertId]) REFERENCES [ops].[PreAlerts] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD CONSTRAINT [FK_Parcels_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [ops].[Shipments] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD CONSTRAINT [FK_Parcels_StorageLocations_StorageLocationId] FOREIGN KEY ([StorageLocationId]) REFERENCES [ops].[StorageLocations] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD CONSTRAINT [FK_Parcels_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018220715_Receiving'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018220715_Receiving', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    DELETE FROM [ops].[WarehouseReceipts];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    ALTER TABLE [ops].[WarehouseReceipts] DROP CONSTRAINT [FK_WarehouseReceipts_Parcels_ParcelId];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    DROP INDEX [IX_WarehouseReceipts_ParcelId] ON [ops].[WarehouseReceipts];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    DROP INDEX [IX_WarehouseReceipts_TenantId_ParcelId] ON [ops].[WarehouseReceipts];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[WarehouseReceipts]') AND [c].[name] = N'Condition');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [ops].[WarehouseReceipts] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [ops].[WarehouseReceipts] DROP COLUMN [Condition];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    EXEC sp_rename N'[ops].[WarehouseReceipts].[ParcelId]', N'DeliveryId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    ALTER TABLE [ops].[WarehouseReceipts] ADD [Carrier] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    ALTER TABLE [ops].[WarehouseReceipts] ADD [DamagedCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    ALTER TABLE [ops].[WarehouseReceipts] ADD [OpenedUtc] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    ALTER TABLE [ops].[WarehouseReceipts] ADD [PieceCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [ArrivedDamaged] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [DeliveryId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE TABLE [ops].[InboundDeliveries] (
        [Id] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Carrier] nvarchar(40) NULL,
        [OpenedByUserId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [ClosedUtc] datetimeoffset NULL,
        [ReceiptId] uniqueidentifier NULL,
        [OpenKey] varchar(60) NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_InboundDeliveries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InboundDeliveries_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE TABLE [ops].[LabelPrinters] (
        [Id] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Name] nvarchar(60) NOT NULL,
        [Connection] int NOT NULL,
        [Address] nvarchar(120) NOT NULL,
        [Dpi] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_LabelPrinters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LabelPrinters_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE TABLE [ops].[PrintAgents] (
        [Id] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Name] nvarchar(60) NOT NULL,
        [KeyHash] varchar(128) NOT NULL,
        [IsActive] bit NOT NULL,
        [LastSeenUtc] datetimeoffset NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_PrintAgents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PrintAgents_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE TABLE [ops].[PrintJobs] (
        [Id] uniqueidentifier NOT NULL,
        [PrinterId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Payload] varchar(4000) NOT NULL,
        [Status] int NOT NULL,
        [Attempts] int NOT NULL,
        [TakenUtc] datetimeoffset NULL,
        [FinishedUtc] datetimeoffset NULL,
        [ErrorCode] varchar(60) NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_PrintJobs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PrintJobs_LabelPrinters_PrinterId] FOREIGN KEY ([PrinterId]) REFERENCES [ops].[LabelPrinters] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WarehouseReceipts_DeliveryId] ON [ops].[WarehouseReceipts] ([DeliveryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_WarehouseReceipts_TenantId_WarehouseId_CreatedUtc] ON [ops].[WarehouseReceipts] ([TenantId], [WarehouseId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_Parcels_DeliveryId] ON [ops].[Parcels] ([DeliveryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_Parcels_TenantId_DeliveryId] ON [ops].[Parcels] ([TenantId], [DeliveryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_InboundDeliveries_TenantId] ON [ops].[InboundDeliveries] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_InboundDeliveries_TenantId_OpenKey] ON [ops].[InboundDeliveries] ([TenantId], [OpenKey]) WHERE [OpenKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_InboundDeliveries_TenantId_WarehouseId_Status] ON [ops].[InboundDeliveries] ([TenantId], [WarehouseId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_InboundDeliveries_WarehouseId] ON [ops].[InboundDeliveries] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_LabelPrinters_TenantId] ON [ops].[LabelPrinters] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_LabelPrinters_TenantId_WarehouseId] ON [ops].[LabelPrinters] ([TenantId], [WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_LabelPrinters_WarehouseId] ON [ops].[LabelPrinters] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_PrintAgents_TenantId] ON [ops].[PrintAgents] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_PrintAgents_TenantId_WarehouseId] ON [ops].[PrintAgents] ([TenantId], [WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_PrintAgents_WarehouseId] ON [ops].[PrintAgents] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_PrintJobs_PrinterId] ON [ops].[PrintJobs] ([PrinterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_PrintJobs_TenantId] ON [ops].[PrintJobs] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    CREATE INDEX [IX_PrintJobs_TenantId_WarehouseId_Status_CreatedUtc] ON [ops].[PrintJobs] ([TenantId], [WarehouseId], [Status], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD CONSTRAINT [FK_Parcels_InboundDeliveries_DeliveryId] FOREIGN KEY ([DeliveryId]) REFERENCES [ops].[InboundDeliveries] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    ALTER TABLE [ops].[WarehouseReceipts] ADD CONSTRAINT [FK_WarehouseReceipts_InboundDeliveries_DeliveryId] FOREIGN KEY ([DeliveryId]) REFERENCES [ops].[InboundDeliveries] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018231727_ReceivingStations'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018231727_ReceivingStations', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    DROP INDEX [IX_Shipments_TenantId_CustomerId_OriginWarehouseId] ON [ops].[Shipments];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [DeliveryAddress_City] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [DeliveryAddress_CountryCode] char(2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [DeliveryAddress_Line1] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [DeliveryAddress_Line2] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [DeliveryAddress_PostalCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [DeliveryAddress_Region] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [DestinationBranchId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [DestinationCountryCode] char(2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [Options] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [RecipientName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [RecipientPhone] varchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[PreAlerts] ADD [ShipmentId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD CONSTRAINT [AK_Shipments_TenantId_Id] UNIQUE ([TenantId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Branches] ADD CONSTRAINT [AK_Branches_TenantId_Id] UNIQUE ([TenantId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Shipments_TenantId_CustomerId_OriginWarehouseId] ON [ops].[Shipments] ([TenantId], [CustomerId], [OriginWarehouseId]) WHERE [Status] = 0 AND [DestinationCountryCode] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    CREATE INDEX [IX_Shipments_TenantId_CustomerId_Status] ON [ops].[Shipments] ([TenantId], [CustomerId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    CREATE INDEX [IX_Shipments_TenantId_DestinationBranchId] ON [ops].[Shipments] ([TenantId], [DestinationBranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    CREATE INDEX [IX_PreAlerts_TenantId_ShipmentId] ON [ops].[PreAlerts] ([TenantId], [ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[PreAlerts] ADD CONSTRAINT [FK_PreAlerts_Shipments_TenantId_ShipmentId] FOREIGN KEY ([TenantId], [ShipmentId]) REFERENCES [ops].[Shipments] ([TenantId], [Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD CONSTRAINT [FK_Shipments_Branches_TenantId_DestinationBranchId] FOREIGN KEY ([TenantId], [DestinationBranchId]) REFERENCES [ops].[Branches] ([TenantId], [Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019015148_CustomerShipments'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019015148_CustomerShipments', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [InspectionPercent] int NOT NULL DEFAULT 5;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE TABLE [ops].[InspectionRules] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Value] nvarchar(60) NULL,
        [IsActive] bit NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_InspectionRules] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE TABLE [ops].[Inspections] (
        [Id] uniqueidentifier NOT NULL,
        [ParcelId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Why] varchar(20) NOT NULL,
        [Reason] nvarchar(200) NULL,
        [SelectedByUserId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [StartedByUserId] uniqueidentifier NULL,
        [StartedUtc] datetimeoffset NULL,
        [FinishedByUserId] uniqueidentifier NULL,
        [FinishedUtc] datetimeoffset NULL,
        [Result] varchar(40) NULL,
        [ItemsFound] nvarchar(1000) NULL,
        [Notes] nvarchar(1000) NULL,
        [CustomerAction] nvarchar(500) NULL,
        [HoldReason] int NOT NULL,
        [Fee] decimal(12,2) NULL,
        [FeeCurrency] varchar(3) NULL,
        [CalledCustomerUtc] datetimeoffset NULL,
        [CallNotes] nvarchar(500) NULL,
        [ApprovedByUserId] uniqueidentifier NULL,
        [ApprovedUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Inspections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Inspections_Parcels_ParcelId] FOREIGN KEY ([ParcelId]) REFERENCES [ops].[Parcels] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Inspections_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE TABLE [ops].[RestrictedItems] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Words] nvarchar(500) NOT NULL,
        [Kind] int NOT NULL,
        [Countries] varchar(300) NULL,
        [Carriers] nvarchar(300) NULL,
        [Requirement] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_RestrictedItems] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE INDEX [IX_InspectionRules_TenantId] ON [ops].[InspectionRules] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InspectionRules_TenantId_Kind_Value] ON [ops].[InspectionRules] ([TenantId], [Kind], [Value]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE INDEX [IX_Inspections_ParcelId] ON [ops].[Inspections] ([ParcelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE INDEX [IX_Inspections_TenantId] ON [ops].[Inspections] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE INDEX [IX_Inspections_TenantId_ParcelId_CreatedUtc] ON [ops].[Inspections] ([TenantId], [ParcelId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Inspections_TenantId_ParcelId_Open] ON [ops].[Inspections] ([TenantId], [ParcelId]) WHERE [Status] <> 3');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE INDEX [IX_Inspections_TenantId_WarehouseId_Status_CreatedUtc] ON [ops].[Inspections] ([TenantId], [WarehouseId], [Status], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE INDEX [IX_Inspections_WarehouseId] ON [ops].[Inspections] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE INDEX [IX_RestrictedItems_TenantId] ON [ops].[RestrictedItems] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RestrictedItems_TenantId_Name] ON [ops].[RestrictedItems] ([TenantId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043645_InspectionAndHolds'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019043645_InspectionAndHolds', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019054858_HoldAnswersAndUnclaimed'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [UnclaimedDays] int NOT NULL DEFAULT 30;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019054858_HoldAnswersAndUnclaimed'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [RemindersSent] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019054858_HoldAnswersAndUnclaimed'
)
BEGIN
    CREATE TABLE [ops].[HoldAnswers] (
        [Id] uniqueidentifier NOT NULL,
        [ParcelId] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [Reason] int NOT NULL,
        [Kind] int NOT NULL,
        [Status] int NOT NULL,
        [Contents] nvarchar(300) NULL,
        [DeclaredValue] decimal(12,2) NULL,
        [CurrencyCode] varchar(3) NULL,
        [Choice] varchar(20) NULL,
        [Note] nvarchar(500) NULL,
        [DocumentKey] varchar(200) NULL,
        [DocumentType] varchar(40) NULL,
        [DocumentName] nvarchar(120) NULL,
        [DocumentBytes] int NULL,
        [DocumentSha256] varchar(64) NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [ReviewedUtc] datetimeoffset NULL,
        [ReviewNote] nvarchar(500) NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_HoldAnswers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HoldAnswers_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [ops].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HoldAnswers_Parcels_ParcelId] FOREIGN KEY ([ParcelId]) REFERENCES [ops].[Parcels] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019054858_HoldAnswersAndUnclaimed'
)
BEGIN
    CREATE INDEX [IX_HoldAnswers_CustomerId] ON [ops].[HoldAnswers] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019054858_HoldAnswersAndUnclaimed'
)
BEGIN
    CREATE INDEX [IX_HoldAnswers_ParcelId] ON [ops].[HoldAnswers] ([ParcelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019054858_HoldAnswersAndUnclaimed'
)
BEGIN
    CREATE INDEX [IX_HoldAnswers_TenantId] ON [ops].[HoldAnswers] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019054858_HoldAnswersAndUnclaimed'
)
BEGIN
    CREATE INDEX [IX_HoldAnswers_TenantId_ParcelId_CreatedUtc] ON [ops].[HoldAnswers] ([TenantId], [ParcelId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019054858_HoldAnswersAndUnclaimed'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_HoldAnswers_TenantId_ParcelId_Reason_Open] ON [ops].[HoldAnswers] ([TenantId], [ParcelId], [Reason]) WHERE [Status] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019054858_HoldAnswersAndUnclaimed'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019054858_HoldAnswersAndUnclaimed', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE TABLE [ops].[ChargeExtras] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [IsGovernmentFee] bit NOT NULL,
        [Amount] decimal(12,3) NOT NULL,
        [Calculation] int NOT NULL,
        [Per] int NOT NULL,
        [Trigger] int NOT NULL,
        [Words] nvarchar(500) NULL,
        [ServiceCode] varchar(40) NULL,
        [CountryCode] char(2) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ChargeExtras] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE TABLE [ops].[InsurancePlans] (
        [Id] uniqueidentifier NOT NULL,
        [Method] int NOT NULL,
        [Percent] decimal(6,3) NULL,
        [MinimumPremium] decimal(12,2) NULL,
        [MaxValue] decimal(12,2) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Bands] nvarchar(max) NULL,
        CONSTRAINT [PK_InsurancePlans] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE TABLE [ops].[Quotes] (
        [Id] uniqueidentifier NOT NULL,
        [Number] varchar(20) NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [ServiceCode] varchar(40) NOT NULL,
        [CountryCode] char(2) NULL,
        [Consolidated] bit NOT NULL,
        [Insured] bit NOT NULL,
        [StandardTotal] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [CurrencyCode] varchar(3) NOT NULL,
        [PreferredReason] nvarchar(300) NULL,
        [ValidUntilUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [RequestKey] varchar(64) NULL,
        [Status] int NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Lines] nvarchar(max) NULL,
        [Pieces] nvarchar(max) NULL,
        CONSTRAINT [PK_Quotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Quotes_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [ops].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE TABLE [ops].[RateCards] (
        [Id] uniqueidentifier NOT NULL,
        [ServiceCode] varchar(40) NOT NULL,
        [CountryCode] char(2) NULL,
        [CustomerId] uniqueidentifier NULL,
        [Basis] int NOT NULL,
        [UnitPrice] decimal(14,4) NOT NULL,
        [SmallPackageLimit] decimal(10,2) NULL,
        [SmallPackageFee] decimal(12,2) NULL,
        [MinimumCharge] decimal(12,2) NULL,
        [ServiceFee] decimal(12,2) NULL,
        [ConsolidationFee] decimal(12,2) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Bands] nvarchar(max) NULL,
        [Items] nvarchar(max) NULL,
        CONSTRAINT [PK_RateCards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RateCards_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [ops].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE INDEX [IX_ChargeExtras_TenantId] ON [ops].[ChargeExtras] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ChargeExtras_TenantId_Name] ON [ops].[ChargeExtras] ([TenantId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsurancePlans_TenantId] ON [ops].[InsurancePlans] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE INDEX [IX_Quotes_CustomerId] ON [ops].[Quotes] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE INDEX [IX_Quotes_TenantId] ON [ops].[Quotes] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE INDEX [IX_Quotes_TenantId_CustomerId_CreatedUtc] ON [ops].[Quotes] ([TenantId], [CustomerId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Quotes_TenantId_Number] ON [ops].[Quotes] ([TenantId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Quotes_TenantId_RequestKey] ON [ops].[Quotes] ([TenantId], [RequestKey]) WHERE [RequestKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE INDEX [IX_RateCards_CustomerId] ON [ops].[RateCards] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE INDEX [IX_RateCards_TenantId] ON [ops].[RateCards] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE INDEX [IX_RateCards_TenantId_CustomerId] ON [ops].[RateCards] ([TenantId], [CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RateCards_TenantId_ServiceCode_CountryCode_CustomerId] ON [ops].[RateCards] ([TenantId], [ServiceCode], [CountryCode], [CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019124552_ChargesEngine'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019124552_ChargesEngine', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD [ManagerUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD [TimeZone] varchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [TimeZone] varchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [DestinationZoneId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[RateCards] ADD [ZoneId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[Quotes] ADD [ZoneId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[ChargeExtras] ADD [Versions] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[ChargeExtras] ADD [ZoneId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE TABLE [ops].[ItemTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [Words] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ItemTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE TABLE [ops].[Zones] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(12) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [CountryCode] char(2) NOT NULL,
        [HubWarehouseId] uniqueidentifier NULL,
        [Delivery] int NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [Cities] nvarchar(max) NOT NULL,
        [PickupBranchIds] nvarchar(max) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Zones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Zones_Warehouses_HubWarehouseId] FOREIGN KEY ([HubWarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[RateCards] ADD [Versions] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[InsurancePlans] ADD [Versions] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[ChargeExtras] ADD [Delivery] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DROP INDEX [IX_RateCards_TenantId_ServiceCode_CountryCode_CustomerId] ON [ops].[RateCards];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    EXEC(N'INSERT INTO [ops].[Zones] ([Id], [TenantId], [Code], [Name], [CountryCode], [HubWarehouseId], [Delivery], [IsActive], [Cities], [PickupBranchIds], [CreatedUtc])
    SELECT b.[Id], b.[TenantId], b.[Code], b.[Name], b.[Address_CountryCode],
        (SELECT TOP 1 w.[Id] FROM [ops].[Warehouses] w WHERE w.[BranchId] = b.[Id] AND (w.[Roles] & 2) = 2 AND w.[Status] = 0 ORDER BY w.[Code]),
        COALESCE((SELECT TOP 1 s.[DeliveryModes] FROM [ops].[TenantSettings] s WHERE s.[TenantId] = b.[TenantId]), 1) | 1,
        CASE WHEN b.[Status] = 0 THEN 1 ELSE 0 END,
        N''[]'',
        N''["'' + LOWER(CONVERT(nvarchar(36), b.[Id])) + N''"]'',
        SYSDATETIMEOFFSET()
    FROM [ops].[Branches] b
    WHERE b.[Side] <> 0 AND NOT EXISTS (SELECT 1 FROM [ops].[Zones] z WHERE z.[TenantId] = b.[TenantId] AND z.[Code] = b.[Code]);');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    EXEC(N'UPDATE s SET [DestinationZoneId] = s.[DestinationBranchId]
    FROM [ops].[Shipments] s
    WHERE s.[DestinationBranchId] IS NOT NULL AND EXISTS (SELECT 1 FROM [ops].[Zones] z WHERE z.[Id] = s.[DestinationBranchId]);');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    EXEC(N'UPDATE s SET [DestinationZoneId] = (SELECT MIN(CONVERT(nvarchar(36), z.[Id])) FROM [ops].[Zones] z
        WHERE z.[TenantId] = s.[TenantId] AND z.[CountryCode] = s.[DestinationCountryCode] AND z.[IsActive] = 1)
    FROM [ops].[Shipments] s
    WHERE s.[DestinationZoneId] IS NULL AND s.[DestinationCountryCode] IS NOT NULL
        AND (SELECT COUNT(*) FROM [ops].[Zones] z WHERE z.[TenantId] = s.[TenantId] AND z.[CountryCode] = s.[DestinationCountryCode] AND z.[IsActive] = 1) = 1;');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    EXEC(N'INSERT INTO [ops].[ItemTypes] ([Id], [TenantId], [Name], [Words], [IsActive], [CreatedUtc])
    SELECT NEWID(), x.[TenantId], x.[Name], NULL, 1, SYSDATETIMEOFFSET()
    FROM (SELECT DISTINCT c.[TenantId], LEFT(LTRIM(RTRIM(j.[Name])), 80) AS [Name]
          FROM [ops].[RateCards] c CROSS APPLY OPENJSON(c.[Items]) WITH ([Name] nvarchar(200) ''$.Name'') j
          WHERE c.[Items] IS NOT NULL AND ISJSON(c.[Items]) = 1 AND LEN(LTRIM(RTRIM(ISNULL(j.[Name], N'''')))) > 0) x
    WHERE NOT EXISTS (SELECT 1 FROM [ops].[ItemTypes] t WHERE t.[TenantId] = x.[TenantId] AND t.[Name] = x.[Name]);');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    EXEC(N'UPDATE c SET [Versions] = (
        SELECT LOWER(CONVERT(nvarchar(36), NEWID())) AS [Id], CONVERT(nvarchar(40), c.[CreatedUtc], 127) AS [EffectiveFromUtc],
            c.[MinimumCharge] AS [MinimumCharge], c.[ServiceFee] AS [ServiceFee], c.[ConsolidationFee] AS [ConsolidationFee],
            JSON_QUERY((SELECT b.[From], b.[Kind], b.[Amount], b.[Base] FROM (
                SELECT CAST(0 AS decimal(12, 2)) AS [From], 1 AS [Kind], CAST(c.[SmallPackageFee] AS decimal(14, 4)) AS [Amount], CAST(NULL AS decimal(12, 2)) AS [Base] WHERE c.[SmallPackageFee] IS NOT NULL
                UNION ALL
                SELECT CAST(s.[Start] AS decimal(12, 2)), 2, CAST(ISNULL((SELECT TOP 1 o.[UnitPrice]
                    FROM OPENJSON(CASE WHEN ISJSON(c.[Bands]) = 1 THEN c.[Bands] ELSE N''[]'' END) WITH ([FromWeight] decimal(12, 2) ''$.FromWeight'', [UnitPrice] decimal(14, 4) ''$.UnitPrice'') o
                    WHERE o.[FromWeight] <= s.[Start] ORDER BY o.[FromWeight] DESC), c.[UnitPrice]) AS decimal(14, 4)), NULL
                FROM (SELECT CASE WHEN c.[SmallPackageFee] IS NOT NULL THEN FLOOR(c.[SmallPackageLimit]) + 1 ELSE 0 END AS [Start]) s
                UNION ALL
                SELECT j.[FromWeight], 2, j.[UnitPrice], NULL
                FROM OPENJSON(CASE WHEN ISJSON(c.[Bands]) = 1 THEN c.[Bands] ELSE N''[]'' END) WITH ([FromWeight] decimal(12, 2) ''$.FromWeight'', [UnitPrice] decimal(14, 4) ''$.UnitPrice'') j
                WHERE j.[FromWeight] > CASE WHEN c.[SmallPackageFee] IS NOT NULL THEN FLOOR(c.[SmallPackageLimit]) + 1 ELSE 0 END) b
                ORDER BY b.[From] FOR JSON PATH, INCLUDE_NULL_VALUES)) AS [Bands],
            JSON_QUERY(ISNULL((SELECT LOWER(CONVERT(nvarchar(36), t.[Id])) AS [ItemTypeId], j.[Price]
                FROM OPENJSON(CASE WHEN ISJSON(c.[Items]) = 1 THEN c.[Items] ELSE N''[]'' END) WITH ([Name] nvarchar(200) ''$.Name'', [Price] decimal(12, 2) ''$.Price'') j
                JOIN [ops].[ItemTypes] t ON t.[TenantId] = c.[TenantId] AND t.[Name] = LEFT(LTRIM(RTRIM(j.[Name])), 80)
                FOR JSON PATH), N''[]'')) AS [Items]
        FOR JSON PATH, INCLUDE_NULL_VALUES)
    FROM [ops].[RateCards] c;');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    EXEC(N'INSERT INTO [ops].[Zones] ([Id], [TenantId], [Code], [Name], [CountryCode], [HubWarehouseId], [Delivery], [IsActive], [Cities], [PickupBranchIds], [CreatedUtc])
    SELECT NEWID(), x.[TenantId], LEFT(N''ZONE-'' + x.[CountryCode], 20), x.[CountryCode], x.[CountryCode], NULL, 2, 0, N''[]'', N''[]'', SYSDATETIMEOFFSET()
    FROM (SELECT DISTINCT c.[TenantId], c.[CountryCode] FROM [ops].[RateCards] c WHERE c.[CountryCode] IS NOT NULL) x
    WHERE NOT EXISTS (SELECT 1 FROM [ops].[Zones] z WHERE z.[TenantId] = x.[TenantId] AND z.[CountryCode] = x.[CountryCode])
        AND NOT EXISTS (SELECT 1 FROM [ops].[Zones] z WHERE z.[TenantId] = x.[TenantId] AND z.[Code] = LEFT(N''ZONE-'' + x.[CountryCode], 20));');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    EXEC(N'UPDATE c SET [ZoneId] = (SELECT TOP 1 z.[Id] FROM [ops].[Zones] z WHERE z.[TenantId] = c.[TenantId] AND z.[CountryCode] = c.[CountryCode] ORDER BY z.[Code])
    FROM [ops].[RateCards] c WHERE c.[CountryCode] IS NOT NULL;');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    EXEC(N'INSERT INTO [ops].[RateCards] ([Id], [TenantId], [ServiceCode], [CustomerId], [Basis], [IsActive], [Versions], [ZoneId], [CreatedUtc], [CountryCode], [UnitPrice])
    SELECT NEWID(), c.[TenantId], c.[ServiceCode], c.[CustomerId], c.[Basis], c.[IsActive], JSON_MODIFY(c.[Versions], ''$[0].Id'', LOWER(CONVERT(nvarchar(36), NEWID()))), z.[Id], c.[CreatedUtc], NULL, c.[UnitPrice]
    FROM [ops].[RateCards] c JOIN [ops].[Zones] z ON z.[TenantId] = c.[TenantId] AND z.[CountryCode] = c.[CountryCode] AND z.[Id] <> c.[ZoneId]
    WHERE c.[CountryCode] IS NOT NULL
        AND NOT EXISTS (SELECT 1 FROM [ops].[RateCards] o WHERE o.[TenantId] = c.[TenantId] AND o.[ServiceCode] = c.[ServiceCode] AND o.[ZoneId] = z.[Id]
            AND ((o.[CustomerId] IS NULL AND c.[CustomerId] IS NULL) OR o.[CustomerId] = c.[CustomerId]));');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    EXEC(N';WITH [ranked] AS (
        SELECT c.[Id], ROW_NUMBER() OVER (PARTITION BY c.[TenantId], c.[ServiceCode], c.[ZoneId], c.[CustomerId] ORDER BY CASE WHEN c.[CountryCode] IS NULL THEN 0 ELSE 1 END, c.[CreatedUtc]) AS [n]
        FROM [ops].[RateCards] c)
    DELETE FROM [ops].[RateCards] WHERE [Id] IN (SELECT [Id] FROM [ranked] WHERE [n] > 1);');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    EXEC(N'UPDATE e SET [Versions] = (SELECT LOWER(CONVERT(nvarchar(36), NEWID())) AS [Id], CONVERT(nvarchar(40), e.[CreatedUtc], 127) AS [EffectiveFromUtc],
        e.[Amount] AS [Amount], e.[Calculation] AS [Calculation], e.[Per] AS [Per] FOR JSON PATH)
    FROM [ops].[ChargeExtras] e;');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    EXEC(N'UPDATE p SET [Versions] = (SELECT LOWER(CONVERT(nvarchar(36), NEWID())) AS [Id], CONVERT(nvarchar(40), p.[CreatedUtc], 127) AS [EffectiveFromUtc],
        p.[Method] AS [Method], p.[Percent] AS [Percent], p.[MinimumPremium] AS [MinimumPremium], p.[MaxValue] AS [MaxValue],
        JSON_QUERY(CASE WHEN ISJSON(p.[Bands]) = 1 THEN p.[Bands] ELSE N''[]'' END) AS [Bands]
        FOR JSON PATH, INCLUDE_NULL_VALUES)
    FROM [ops].[InsurancePlans] p;');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[RateCards]') AND [c].[name] = N'Bands');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [ops].[RateCards] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [ops].[RateCards] DROP COLUMN [Bands];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[RateCards]') AND [c].[name] = N'Items');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [ops].[RateCards] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [ops].[RateCards] DROP COLUMN [Items];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[RateCards]') AND [c].[name] = N'ConsolidationFee');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [ops].[RateCards] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [ops].[RateCards] DROP COLUMN [ConsolidationFee];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[RateCards]') AND [c].[name] = N'CountryCode');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [ops].[RateCards] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [ops].[RateCards] DROP COLUMN [CountryCode];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[RateCards]') AND [c].[name] = N'MinimumCharge');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [ops].[RateCards] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [ops].[RateCards] DROP COLUMN [MinimumCharge];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[RateCards]') AND [c].[name] = N'ServiceFee');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [ops].[RateCards] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [ops].[RateCards] DROP COLUMN [ServiceFee];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[RateCards]') AND [c].[name] = N'SmallPackageFee');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [ops].[RateCards] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [ops].[RateCards] DROP COLUMN [SmallPackageFee];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[RateCards]') AND [c].[name] = N'SmallPackageLimit');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [ops].[RateCards] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [ops].[RateCards] DROP COLUMN [SmallPackageLimit];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[RateCards]') AND [c].[name] = N'UnitPrice');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [ops].[RateCards] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [ops].[RateCards] DROP COLUMN [UnitPrice];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[InsurancePlans]') AND [c].[name] = N'Bands');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [ops].[InsurancePlans] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [ops].[InsurancePlans] DROP COLUMN [Bands];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[InsurancePlans]') AND [c].[name] = N'MaxValue');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [ops].[InsurancePlans] DROP CONSTRAINT ' + @var13 + ';');
    ALTER TABLE [ops].[InsurancePlans] DROP COLUMN [MaxValue];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[InsurancePlans]') AND [c].[name] = N'Method');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [ops].[InsurancePlans] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [ops].[InsurancePlans] DROP COLUMN [Method];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var15 nvarchar(max);
    SELECT @var15 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[InsurancePlans]') AND [c].[name] = N'MinimumPremium');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [ops].[InsurancePlans] DROP CONSTRAINT ' + @var15 + ';');
    ALTER TABLE [ops].[InsurancePlans] DROP COLUMN [MinimumPremium];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var16 nvarchar(max);
    SELECT @var16 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[InsurancePlans]') AND [c].[name] = N'Percent');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [ops].[InsurancePlans] DROP CONSTRAINT ' + @var16 + ';');
    ALTER TABLE [ops].[InsurancePlans] DROP COLUMN [Percent];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var17 nvarchar(max);
    SELECT @var17 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[ChargeExtras]') AND [c].[name] = N'Amount');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [ops].[ChargeExtras] DROP CONSTRAINT ' + @var17 + ';');
    ALTER TABLE [ops].[ChargeExtras] DROP COLUMN [Amount];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var18 nvarchar(max);
    SELECT @var18 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[ChargeExtras]') AND [c].[name] = N'Calculation');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [ops].[ChargeExtras] DROP CONSTRAINT ' + @var18 + ';');
    ALTER TABLE [ops].[ChargeExtras] DROP COLUMN [Calculation];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    DECLARE @var19 nvarchar(max);
    SELECT @var19 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[ChargeExtras]') AND [c].[name] = N'Per');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [ops].[ChargeExtras] DROP CONSTRAINT ' + @var19 + ';');
    ALTER TABLE [ops].[ChargeExtras] DROP COLUMN [Per];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE INDEX [IX_Warehouses_ManagerUserId] ON [ops].[Warehouses] ([ManagerUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE INDEX [IX_Shipments_DestinationZoneId] ON [ops].[Shipments] ([DestinationZoneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RateCards_TenantId_ServiceCode_ZoneId_CustomerId] ON [ops].[RateCards] ([TenantId], [ServiceCode], [ZoneId], [CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE INDEX [IX_RateCards_ZoneId] ON [ops].[RateCards] ([ZoneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE INDEX [IX_Quotes_ZoneId] ON [ops].[Quotes] ([ZoneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE INDEX [IX_ChargeExtras_ZoneId] ON [ops].[ChargeExtras] ([ZoneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE INDEX [IX_ItemTypes_TenantId] ON [ops].[ItemTypes] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ItemTypes_TenantId_Name] ON [ops].[ItemTypes] ([TenantId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE INDEX [IX_Zones_HubWarehouseId] ON [ops].[Zones] ([HubWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE INDEX [IX_Zones_TenantId] ON [ops].[Zones] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Zones_TenantId_Code] ON [ops].[Zones] ([TenantId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[ChargeExtras] ADD CONSTRAINT [FK_ChargeExtras_Zones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [ops].[Zones] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[Quotes] ADD CONSTRAINT [FK_Quotes_Zones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [ops].[Zones] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[RateCards] ADD CONSTRAINT [FK_RateCards_Zones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [ops].[Zones] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD CONSTRAINT [FK_Shipments_Zones_DestinationZoneId] FOREIGN KEY ([DestinationZoneId]) REFERENCES [ops].[Zones] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD CONSTRAINT [FK_Warehouses_Users_ManagerUserId] FOREIGN KEY ([ManagerUserId]) REFERENCES [ops].[Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019144549_ZonesAndPriceVersions'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019144549_ZonesAndPriceVersions', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[ChargeExtras] DROP CONSTRAINT [FK_ChargeExtras_Zones_ZoneId];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Quotes] DROP CONSTRAINT [FK_Quotes_Zones_ZoneId];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[RateCards] DROP CONSTRAINT [FK_RateCards_Zones_ZoneId];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Shipments] DROP CONSTRAINT [FK_Shipments_Zones_DestinationZoneId];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    DROP INDEX [IX_RateCards_TenantId_ServiceCode_ZoneId_CustomerId] ON [ops].[RateCards];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    DROP INDEX [IX_InsurancePlans_TenantId] ON [ops].[InsurancePlans];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    DROP INDEX [IX_RateCards_ZoneId] ON [ops].[RateCards];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    DROP INDEX [IX_Quotes_ZoneId] ON [ops].[Quotes];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    DROP INDEX [IX_ChargeExtras_ZoneId] ON [ops].[ChargeExtras];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    DROP INDEX [IX_Shipments_DestinationZoneId] ON [ops].[Shipments];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD [DeliversHome] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD [HomeArea] nvarchar(max) NOT NULL DEFAULT N'[]';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD [HubWarehouseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD [OperatedBy] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD [PartnerEmail] nvarchar(254) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD [PartnerName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD [PartnerPhone] varchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [DestinationWarehouseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [InsuranceAcceptedUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[RateCards] ADD [OriginWarehouseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[RateCards] ADD [DestinationWarehouseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[RateCards] ADD [Customs] int NOT NULL DEFAULT 2;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Quotes] ADD [OriginWarehouseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Quotes] ADD [DestinationWarehouseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[InsurancePlans] ADD [Mode] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[ChargeExtras] ADD [Mode] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[ChargeExtras] ADD [OriginWarehouseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[ChargeExtras] ADD [DestinationWarehouseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    EXEC(N'CREATE TABLE [ops].[ZoneWarehouseMap] ([ZoneId] uniqueidentifier NOT NULL PRIMARY KEY, [WarehouseId] uniqueidentifier NULL);
    INSERT INTO [ops].[ZoneWarehouseMap] ([ZoneId], [WarehouseId])
    SELECT z.[Id], COALESCE(
        (SELECT TOP 1 w.[Id] FROM [ops].[Warehouses] w WHERE w.[TenantId] = z.[TenantId] AND w.[Status] = 0
            AND w.[BranchId] IN (SELECT TRY_CONVERT(uniqueidentifier, p.[value]) FROM OPENJSON(CASE WHEN ISJSON(z.[PickupBranchIds]) = 1 THEN z.[PickupBranchIds] ELSE N''[]'' END) p) ORDER BY CASE WHEN (w.[Roles] & 4) = 4 THEN 0 WHEN (w.[Roles] & 2) = 2 THEN 1 ELSE 2 END, w.[Code]),
        (SELECT TOP 1 w.[Id] FROM [ops].[Warehouses] w WHERE w.[TenantId] = z.[TenantId] AND w.[Status] = 0 AND w.[BranchId] = z.[Id] ORDER BY CASE WHEN (w.[Roles] & 4) = 4 THEN 0 WHEN (w.[Roles] & 2) = 2 THEN 1 ELSE 2 END, w.[Code]),
        (SELECT w.[Id] FROM [ops].[Warehouses] w WHERE w.[Id] = z.[HubWarehouseId]))
    FROM [ops].[Zones] z;');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    EXEC(N'UPDATE w SET
        [Roles] = w.[Roles] | CASE WHEN a.[Pickup] = 1 THEN 4 ELSE 0 END,
        [DeliversHome] = a.[Home],
        [HomeArea] = CASE WHEN a.[Home] = 1 AND a.[Anywhere] = 0 THEN ISNULL((SELECT N''['' + STRING_AGG(N''"'' + STRING_ESCAPE(c.[City], ''json'') + N''"'', N'','') + N'']''
            FROM (SELECT DISTINCT TRIM(cc.[value]) AS [City]
                FROM [ops].[Zones] zz JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = zz.[Id]
                CROSS APPLY OPENJSON(CASE WHEN ISJSON(zz.[Cities]) = 1 THEN zz.[Cities] ELSE N''[]'' END) cc
                WHERE m.[WarehouseId] = w.[Id] AND zz.[IsActive] = 1 AND (zz.[Delivery] & 2) = 2 AND TRIM(cc.[value]) <> N'''') c), N''[]'') ELSE N''[]'' END,
        [HubWarehouseId] = (SELECT TOP 1 zz.[HubWarehouseId] FROM [ops].[Zones] zz JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = zz.[Id]
            WHERE m.[WarehouseId] = w.[Id] AND zz.[IsActive] = 1 AND zz.[HubWarehouseId] IS NOT NULL AND zz.[HubWarehouseId] <> w.[Id] ORDER BY zz.[Code])
    FROM [ops].[Warehouses] w
    CROSS APPLY (SELECT
            MAX(CASE WHEN (zz.[Delivery] & 1) = 1 THEN 1 ELSE 0 END) AS [Pickup],
            MAX(CASE WHEN (zz.[Delivery] & 2) = 2 THEN 1 ELSE 0 END) AS [Home],
            MAX(CASE WHEN (zz.[Delivery] & 2) = 2 AND (CASE WHEN ISJSON(zz.[Cities]) = 1 THEN JSON_VALUE(zz.[Cities], ''$[0]'') END) IS NULL THEN 1 ELSE 0 END) AS [Anywhere],
            COUNT(*) AS [Zones]
        FROM [ops].[Zones] zz JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = zz.[Id]
        WHERE m.[WarehouseId] = w.[Id] AND zz.[IsActive] = 1) a
    WHERE a.[Zones] > 0;');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    EXEC(N'UPDATE c SET [DestinationWarehouseId] = m.[WarehouseId], [IsActive] = CASE WHEN m.[WarehouseId] IS NULL THEN 0 ELSE c.[IsActive] END
    FROM [ops].[RateCards] c JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = c.[ZoneId];');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    EXEC(N';WITH [ranked] AS (
        SELECT c.[Id], ROW_NUMBER() OVER (PARTITION BY c.[TenantId], c.[ServiceCode], c.[OriginWarehouseId], c.[DestinationWarehouseId], c.[CustomerId] ORDER BY c.[IsActive] DESC, c.[CreatedUtc]) AS [n]
        FROM [ops].[RateCards] c)
    DELETE FROM [ops].[RateCards] WHERE [Id] IN (SELECT [Id] FROM [ranked] WHERE [n] > 1);');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    EXEC(N'UPDATE e SET [DestinationWarehouseId] = m.[WarehouseId], [IsActive] = CASE WHEN m.[WarehouseId] IS NULL THEN 0 ELSE e.[IsActive] END
    FROM [ops].[ChargeExtras] e JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = e.[ZoneId];');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    EXEC(N'UPDATE q SET [DestinationWarehouseId] = m.[WarehouseId], [OriginWarehouseId] = NULL
    FROM [ops].[Quotes] q JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = q.[ZoneId];');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    EXEC(N'UPDATE s SET [DestinationWarehouseId] = m.[WarehouseId]
    FROM [ops].[Shipments] s JOIN [ops].[ZoneWarehouseMap] m ON m.[ZoneId] = s.[DestinationZoneId];');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    EXEC(N'UPDATE p SET [Versions] = ISNULL((SELECT N''['' + STRING_AGG(JSON_MODIFY(v.[value], ''$.Coverage'', JSON_QUERY(N''{"Cover":1,"InsurerName":null,"PolicyNumber":null,"InsurerContact":null,"Risks":3,"Exclusions":511,"OtherExclusions":null,"LiabilityPerUnit":null,"LiabilityPerShipment":null,"Payout":2,"DeductibleAmount":null,"DeductiblePercent":null,"MaxPerItem":null,"DamageClaimDays":7,"LossClaimDays":21,"Proof":3,"FiledBy":1,"TermsEn":null,"TermsFr":null,"TermsEs":null}'')), N'','') WITHIN GROUP (ORDER BY CONVERT(int, v.[key])) + N'']''
        FROM OPENJSON(CASE WHEN ISJSON(p.[Versions]) = 1 THEN p.[Versions] ELSE N''[]'' END) v), N''[]''), [Mode] = 1
    FROM [ops].[InsurancePlans] p
    WHERE JSON_QUERY(p.[Versions], ''$[0].Coverage'') IS NULL;');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    EXEC(N'INSERT INTO [ops].[InsurancePlans] ([Id], [TenantId], [Mode], [IsActive], [Versions], [CreatedUtc])
    SELECT NEWID(), p.[TenantId], m.[Mode], p.[IsActive], p.[Versions], SYSDATETIMEOFFSET()
    FROM [ops].[InsurancePlans] p CROSS JOIN (VALUES (2), (3)) m([Mode])
    WHERE p.[Mode] = 1 AND NOT EXISTS (SELECT 1 FROM [ops].[InsurancePlans] o WHERE o.[TenantId] = p.[TenantId] AND o.[Mode] = m.[Mode]);');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    EXEC(N'DROP TABLE [ops].[ZoneWarehouseMap];');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    DECLARE @var20 nvarchar(max);
    SELECT @var20 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[RateCards]') AND [c].[name] = N'ZoneId');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [ops].[RateCards] DROP CONSTRAINT ' + @var20 + ';');
    ALTER TABLE [ops].[RateCards] DROP COLUMN [ZoneId];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    DECLARE @var21 nvarchar(max);
    SELECT @var21 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[Quotes]') AND [c].[name] = N'ZoneId');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [ops].[Quotes] DROP CONSTRAINT ' + @var21 + ';');
    ALTER TABLE [ops].[Quotes] DROP COLUMN [ZoneId];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    DECLARE @var22 nvarchar(max);
    SELECT @var22 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[ChargeExtras]') AND [c].[name] = N'ZoneId');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [ops].[ChargeExtras] DROP CONSTRAINT ' + @var22 + ';');
    ALTER TABLE [ops].[ChargeExtras] DROP COLUMN [ZoneId];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    DECLARE @var23 nvarchar(max);
    SELECT @var23 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ops].[Shipments]') AND [c].[name] = N'DestinationZoneId');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [ops].[Shipments] DROP CONSTRAINT ' + @var23 + ';');
    ALTER TABLE [ops].[Shipments] DROP COLUMN [DestinationZoneId];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    DROP TABLE [ops].[Zones];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    CREATE INDEX [IX_Warehouses_HubWarehouseId] ON [ops].[Warehouses] ([HubWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    CREATE INDEX [IX_Shipments_DestinationWarehouseId] ON [ops].[Shipments] ([DestinationWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    CREATE INDEX [IX_RateCards_OriginWarehouseId] ON [ops].[RateCards] ([OriginWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    CREATE INDEX [IX_RateCards_DestinationWarehouseId] ON [ops].[RateCards] ([DestinationWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    CREATE INDEX [IX_Quotes_OriginWarehouseId] ON [ops].[Quotes] ([OriginWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    CREATE INDEX [IX_Quotes_DestinationWarehouseId] ON [ops].[Quotes] ([DestinationWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    CREATE INDEX [IX_ChargeExtras_OriginWarehouseId] ON [ops].[ChargeExtras] ([OriginWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    CREATE INDEX [IX_ChargeExtras_DestinationWarehouseId] ON [ops].[ChargeExtras] ([DestinationWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    CREATE INDEX [IX_InsurancePlans_TenantId] ON [ops].[InsurancePlans] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RateCards_TenantId_ServiceCode_OriginWarehouseId_DestinationWarehouseId_CustomerId] ON [ops].[RateCards] ([TenantId], [ServiceCode], [OriginWarehouseId], [DestinationWarehouseId], [CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsurancePlans_TenantId_Mode] ON [ops].[InsurancePlans] ([TenantId], [Mode]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[ChargeExtras] ADD CONSTRAINT [FK_ChargeExtras_Warehouses_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[ChargeExtras] ADD CONSTRAINT [FK_ChargeExtras_Warehouses_OriginWarehouseId] FOREIGN KEY ([OriginWarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Quotes] ADD CONSTRAINT [FK_Quotes_Warehouses_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Quotes] ADD CONSTRAINT [FK_Quotes_Warehouses_OriginWarehouseId] FOREIGN KEY ([OriginWarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[RateCards] ADD CONSTRAINT [FK_RateCards_Warehouses_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[RateCards] ADD CONSTRAINT [FK_RateCards_Warehouses_OriginWarehouseId] FOREIGN KEY ([OriginWarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD CONSTRAINT [FK_Shipments_Warehouses_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD CONSTRAINT [FK_Warehouses_Warehouses_HubWarehouseId] FOREIGN KEY ([HubWarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019184840_RoutesAndModes'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019184840_RoutesAndModes', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019222905_OriginIntake'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [RequestKey] varchar(60) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019222905_OriginIntake'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [ServiceCode] varchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019222905_OriginIntake'
)
BEGIN
    ALTER TABLE [ops].[PreAlerts] ADD [WarehouseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019222905_OriginIntake'
)
BEGIN
    EXEC(N'UPDATE p SET [WarehouseId] = COALESCE(
        (SELECT s.[OriginWarehouseId] FROM [ops].[Shipments] s WHERE s.[Id] = p.[ShipmentId]),
        (SELECT r.[WarehouseId] FROM [ops].[Parcels] r WHERE r.[Id] = p.[ParcelId]),
        (SELECT TOP 1 w.[Id] FROM [ops].[Warehouses] w WHERE w.[TenantId] = p.[TenantId] AND w.[Status] = 0 AND (w.[Roles] & 1) = 1 ORDER BY w.[Code]))
    FROM [ops].[PreAlerts] p WHERE p.[WarehouseId] IS NULL;');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019222905_OriginIntake'
)
BEGIN
    ALTER TABLE [ops].[Warehouses] ADD CONSTRAINT [AK_Warehouses_TenantId_Id] UNIQUE ([TenantId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019222905_OriginIntake'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Shipments_TenantId_CustomerId_RequestKey] ON [ops].[Shipments] ([TenantId], [CustomerId], [RequestKey]) WHERE [RequestKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019222905_OriginIntake'
)
BEGIN
    CREATE INDEX [IX_PreAlerts_TenantId_WarehouseId_Status_ExpectedDate] ON [ops].[PreAlerts] ([TenantId], [WarehouseId], [Status], [ExpectedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019222905_OriginIntake'
)
BEGIN
    ALTER TABLE [ops].[PreAlerts] ADD CONSTRAINT [FK_PreAlerts_Warehouses_TenantId_WarehouseId] FOREIGN KEY ([TenantId], [WarehouseId]) REFERENCES [ops].[Warehouses] ([TenantId], [Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019222905_OriginIntake'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019222905_OriginIntake', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019224620_ShippingLabels'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [Number] varchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019224620_ShippingLabels'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [ShippingLabelUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019224620_ShippingLabels'
)
BEGIN
    ALTER TABLE [ops].[LabelPrinters] ADD [Media] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019224620_ShippingLabels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Shipments_TenantId_Number] ON [ops].[Shipments] ([TenantId], [Number]) WHERE [Number] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019224620_ShippingLabels'
)
BEGIN
    CREATE INDEX [IX_Parcels_TenantId_WarehouseId_ShippingLabelUtc] ON [ops].[Parcels] ([TenantId], [WarehouseId], [ShippingLabelUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019224620_ShippingLabels'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019224620_ShippingLabels', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [StorageFeePerDay] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [StorageFreeDays] int NOT NULL DEFAULT 30;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [LabelCode] varchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [LabelName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [LabelSender] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [StorageFeePerDay] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    ALTER TABLE [ops].[Parcels] ADD [StorageFreeDays] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    CREATE TABLE [ops].[StockCounts] (
        [Id] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [StartedByUserId] uniqueidentifier NOT NULL,
        [ClosedUtc] datetimeoffset NULL,
        [ClosedByUserId] uniqueidentifier NULL,
        [OpenWarehouseId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Lines] nvarchar(max) NULL,
        CONSTRAINT [PK_StockCounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockCounts_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    CREATE INDEX [IX_StockCounts_TenantId] ON [ops].[StockCounts] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StockCounts_TenantId_OpenWarehouseId] ON [ops].[StockCounts] ([TenantId], [OpenWarehouseId]) WHERE [OpenWarehouseId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    CREATE INDEX [IX_StockCounts_TenantId_WarehouseId_CreatedUtc] ON [ops].[StockCounts] ([TenantId], [WarehouseId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    CREATE INDEX [IX_StockCounts_WarehouseId] ON [ops].[StockCounts] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019233034_UndeclaredParcels'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019233034_UndeclaredParcels', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020001102_Tickets'
)
BEGIN
    CREATE TABLE [ops].[Tickets] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [Carrier] nvarchar(40) NOT NULL,
        [TrackingNumber] varchar(40) NOT NULL,
        [Store] nvarchar(100) NULL,
        [OrderNumber] nvarchar(60) NULL,
        [DeliveredOn] date NULL,
        [Contents] nvarchar(300) NOT NULL,
        [ProofKey] nvarchar(400) NULL,
        [ProofContentType] varchar(100) NULL,
        [ProofSha256] varchar(64) NULL,
        [ParcelId] uniqueidentifier NULL,
        [ResolvedUtc] datetimeoffset NULL,
        [LastCommandKey] varchar(60) NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Messages] nvarchar(max) NULL,
        CONSTRAINT [PK_Tickets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tickets_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [ops].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tickets_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020001102_Tickets'
)
BEGIN
    CREATE INDEX [IX_Tickets_CustomerId] ON [ops].[Tickets] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020001102_Tickets'
)
BEGIN
    CREATE INDEX [IX_Tickets_TenantId] ON [ops].[Tickets] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020001102_Tickets'
)
BEGIN
    CREATE INDEX [IX_Tickets_TenantId_CustomerId_CreatedUtc] ON [ops].[Tickets] ([TenantId], [CustomerId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020001102_Tickets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tickets_TenantId_CustomerId_TrackingNumber] ON [ops].[Tickets] ([TenantId], [CustomerId], [TrackingNumber]) WHERE [Status] <> 3');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020001102_Tickets'
)
BEGIN
    CREATE INDEX [IX_Tickets_TenantId_WarehouseId_Status_CreatedUtc] ON [ops].[Tickets] ([TenantId], [WarehouseId], [Status], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020001102_Tickets'
)
BEGIN
    CREATE INDEX [IX_Tickets_WarehouseId] ON [ops].[Tickets] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020001102_Tickets'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020001102_Tickets', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020010151_ShipmentsDesk'
)
BEGIN
    CREATE INDEX [IX_Shipments_TenantId_OriginWarehouseId_CreatedUtc] ON [ops].[Shipments] ([TenantId], [OriginWarehouseId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020010151_ShipmentsDesk'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020010151_ShipmentsDesk', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    ALTER TABLE [ops].[InsurancePlans] ADD [PolicyId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE TABLE [ops].[InsurancePolicies] (
        [Id] uniqueidentifier NOT NULL,
        [Scope] int NOT NULL,
        [Kind] int NOT NULL,
        [InsurerName] nvarchar(120) NULL,
        [InsurerEmail] nvarchar(120) NULL,
        [InsurerPhone] nvarchar(120) NULL,
        [ClaimsEmail] nvarchar(120) NULL,
        [ClaimsPhone] nvarchar(120) NULL,
        [BrokerName] nvarchar(120) NULL,
        [BrokerEmail] nvarchar(120) NULL,
        [BrokerPhone] nvarchar(120) NULL,
        [PolicyNumber] nvarchar(60) NULL,
        [StartsOn] date NULL,
        [EndsOn] date NULL,
        [CurrencyCode] varchar(3) NOT NULL,
        [LimitPerShipment] decimal(14,2) NULL,
        [LimitPerConveyance] decimal(14,2) NULL,
        [LimitPerLocation] decimal(14,2) NULL,
        [LimitPerYear] decimal(14,2) NULL,
        [DeductibleAmount] decimal(14,2) NULL,
        [DeductiblePercent] decimal(6,3) NULL,
        [DeductibleMinimum] decimal(14,2) NULL,
        [Risks] int NOT NULL,
        [Modes] int NOT NULL,
        [Territory] nvarchar(300) NULL,
        [Exclusions] int NOT NULL,
        [OtherExclusions] nvarchar(2000) NULL,
        [NoticeDays] int NULL,
        [Declarations] int NOT NULL,
        [Notes] nvarchar(2000) NULL,
        [IsArchived] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Documents] nvarchar(max) NULL,
        CONSTRAINT [PK_InsurancePolicies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE TABLE [ops].[LiabilityTerms] (
        [Id] uniqueidentifier NOT NULL,
        [Mode] int NOT NULL,
        [Basis] int NOT NULL,
        [Amount] decimal(14,4) NULL,
        [WeightUnit] int NOT NULL,
        [Cap] decimal(14,2) NULL,
        [CustomerText] nvarchar(2000) NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_LiabilityTerms] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE TABLE [ops].[InsuranceCertificates] (
        [Id] uniqueidentifier NOT NULL,
        [Number] varchar(40) NOT NULL,
        [ShipmentId] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [PlanId] uniqueidentifier NOT NULL,
        [PolicyId] uniqueidentifier NULL,
        [Mode] int NOT NULL,
        [AcceptedUtc] datetimeoffset NOT NULL,
        [Status] int NOT NULL,
        [DeclaredValue] decimal(14,2) NULL,
        [Premium] decimal(14,2) NULL,
        [CurrencyCode] varchar(3) NULL,
        [VoidedUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_InsuranceCertificates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InsuranceCertificates_InsurancePlans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [ops].[InsurancePlans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InsuranceCertificates_InsurancePolicies_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [ops].[InsurancePolicies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InsuranceCertificates_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [ops].[Shipments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE INDEX [IX_InsurancePlans_PolicyId] ON [ops].[InsurancePlans] ([PolicyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE INDEX [IX_InsuranceCertificates_PlanId] ON [ops].[InsuranceCertificates] ([PlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE INDEX [IX_InsuranceCertificates_PolicyId] ON [ops].[InsuranceCertificates] ([PolicyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE INDEX [IX_InsuranceCertificates_ShipmentId] ON [ops].[InsuranceCertificates] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE INDEX [IX_InsuranceCertificates_TenantId] ON [ops].[InsuranceCertificates] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceCertificates_TenantId_Number] ON [ops].[InsuranceCertificates] ([TenantId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE INDEX [IX_InsuranceCertificates_TenantId_PolicyId_CreatedUtc] ON [ops].[InsuranceCertificates] ([TenantId], [PolicyId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_InsuranceCertificates_TenantId_ShipmentId] ON [ops].[InsuranceCertificates] ([TenantId], [ShipmentId]) WHERE [Status] <> 3');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE INDEX [IX_InsurancePolicies_TenantId] ON [ops].[InsurancePolicies] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE INDEX [IX_InsurancePolicies_TenantId_IsArchived_EndsOn] ON [ops].[InsurancePolicies] ([TenantId], [IsArchived], [EndsOn]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE INDEX [IX_LiabilityTerms_TenantId] ON [ops].[LiabilityTerms] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LiabilityTerms_TenantId_Mode] ON [ops].[LiabilityTerms] ([TenantId], [Mode]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    ALTER TABLE [ops].[InsurancePlans] ADD CONSTRAINT [FK_InsurancePlans_InsurancePolicies_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [ops].[InsurancePolicies] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020031306_InsuranceModule'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020031306_InsuranceModule', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020094001_FrontDeskCounter'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [BookedCurrencyCode] varchar(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020094001_FrontDeskCounter'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [BookedTotal] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020094001_FrontDeskCounter'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [BookedUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020094001_FrontDeskCounter'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [CreatedAtCounter] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020094001_FrontDeskCounter'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [ParcelCount] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020094001_FrontDeskCounter'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020094001_FrontDeskCounter', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    DROP INDEX [IX_InsurancePlans_TenantId_Mode] ON [ops].[InsurancePlans];
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [StaffMayChangeInsurance] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [InsurancePlanId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [InsuranceRecordedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    ALTER TABLE [ops].[InsurancePlans] ADD [Name] nvarchar(60) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    ALTER TABLE [ops].[InsurancePlans] ADD [PitchEn] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    ALTER TABLE [ops].[InsurancePlans] ADD [PitchEs] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    ALTER TABLE [ops].[InsurancePlans] ADD [PitchFr] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    ALTER TABLE [ops].[InsurancePlans] ADD [Position] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    EXEC(N'UPDATE [ops].[InsurancePlans] SET [Name] = CASE [Mode] WHEN 2 THEN N''Ocean insurance'' WHEN 3 THEN N''Ground insurance'' ELSE N''Air insurance'' END WHERE [Name] = N''''');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    EXEC(N'UPDATE s SET s.[InsurancePlanId] = p.[Id]
    FROM [ops].[Shipments] s
    JOIN [ops].[InsurancePlans] p ON p.[TenantId] = s.[TenantId]
        AND p.[Mode] = CASE WHEN s.[ServiceCode] LIKE N''ocean%'' THEN 2 WHEN s.[ServiceCode] LIKE N''ground%'' THEN 3 ELSE 1 END
    WHERE (s.[Options] & 4) = 4 AND s.[InsurancePlanId] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    CREATE INDEX [IX_Shipments_InsurancePlanId] ON [ops].[Shipments] ([InsurancePlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    CREATE INDEX [IX_InsurancePlans_TenantId_Mode_Position] ON [ops].[InsurancePlans] ([TenantId], [Mode], [Position]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD CONSTRAINT [FK_Shipments_InsurancePlans_InsurancePlanId] FOREIGN KEY ([InsurancePlanId]) REFERENCES [ops].[InsurancePlans] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020190419_InsuranceProducts'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020190419_InsuranceProducts', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020194145_CounterSignature'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [CustomerSignatureKey] varchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020194145_CounterSignature'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [CustomerSignatureRecordedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020194145_CounterSignature'
)
BEGIN
    ALTER TABLE [ops].[Shipments] ADD [CustomerSignedUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020194145_CounterSignature'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020194145_CounterSignature', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020211635_SuppliesCatalogue'
)
BEGIN
    CREATE TABLE [ops].[Supplies] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(30) NOT NULL,
        [NameEn] nvarchar(80) NOT NULL,
        [NameFr] nvarchar(80) NULL,
        [NameEs] nvarchar(80) NULL,
        [Unit] nvarchar(30) NULL,
        [Category] int NOT NULL,
        [IsTaxed] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Prices] nvarchar(max) NULL,
        CONSTRAINT [PK_Supplies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020211635_SuppliesCatalogue'
)
BEGIN
    CREATE TABLE [ops].[SupplyBarcodes] (
        [Id] uniqueidentifier NOT NULL,
        [SupplyId] uniqueidentifier NOT NULL,
        [Code] varchar(48) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_SupplyBarcodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupplyBarcodes_Supplies_SupplyId] FOREIGN KEY ([SupplyId]) REFERENCES [ops].[Supplies] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020211635_SuppliesCatalogue'
)
BEGIN
    CREATE INDEX [IX_Supplies_TenantId] ON [ops].[Supplies] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020211635_SuppliesCatalogue'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Supplies_TenantId_Code] ON [ops].[Supplies] ([TenantId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020211635_SuppliesCatalogue'
)
BEGIN
    CREATE INDEX [IX_SupplyBarcodes_SupplyId] ON [ops].[SupplyBarcodes] ([SupplyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020211635_SuppliesCatalogue'
)
BEGIN
    CREATE INDEX [IX_SupplyBarcodes_TenantId] ON [ops].[SupplyBarcodes] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020211635_SuppliesCatalogue'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SupplyBarcodes_TenantId_Code] ON [ops].[SupplyBarcodes] ([TenantId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020211635_SuppliesCatalogue'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020211635_SuppliesCatalogue', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [CountSupplyStock] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [SupplyStockOut] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE TABLE [ops].[SupplyCounts] (
        [Id] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [OpenedByUserId] uniqueidentifier NOT NULL,
        [ClosedByUserId] uniqueidentifier NULL,
        [ClosedUtc] datetimeoffset NULL,
        [Cancelled] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Lines] nvarchar(max) NULL,
        CONSTRAINT [PK_SupplyCounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupplyCounts_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE TABLE [ops].[SupplyMovements] (
        [Id] uniqueidentifier NOT NULL,
        [SupplyId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitCost] decimal(14,4) NULL,
        [Reason] int NULL,
        [Note] nvarchar(300) NULL,
        [Supplier] nvarchar(80) NULL,
        [Reference] nvarchar(60) NULL,
        [LinkId] uniqueidentifier NULL,
        [ByUserId] uniqueidentifier NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_SupplyMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupplyMovements_Supplies_SupplyId] FOREIGN KEY ([SupplyId]) REFERENCES [ops].[Supplies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplyMovements_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE TABLE [ops].[SupplyStocks] (
        [Id] uniqueidentifier NOT NULL,
        [SupplyId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [OnHand] int NOT NULL,
        [ReorderLevel] int NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_SupplyStocks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupplyStocks_Supplies_SupplyId] FOREIGN KEY ([SupplyId]) REFERENCES [ops].[Supplies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplyStocks_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE TABLE [ops].[SupplyTransfers] (
        [Id] uniqueidentifier NOT NULL,
        [FromWarehouseId] uniqueidentifier NOT NULL,
        [ToWarehouseId] uniqueidentifier NOT NULL,
        [Note] nvarchar(300) NULL,
        [SentByUserId] uniqueidentifier NOT NULL,
        [ReceivedByUserId] uniqueidentifier NULL,
        [ReceivedUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Lines] nvarchar(max) NULL,
        CONSTRAINT [PK_SupplyTransfers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupplyTransfers_Warehouses_FromWarehouseId] FOREIGN KEY ([FromWarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplyTransfers_Warehouses_ToWarehouseId] FOREIGN KEY ([ToWarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyCounts_TenantId] ON [ops].[SupplyCounts] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyCounts_TenantId_WarehouseId_ClosedUtc] ON [ops].[SupplyCounts] ([TenantId], [WarehouseId], [ClosedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyCounts_WarehouseId] ON [ops].[SupplyCounts] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyMovements_SupplyId] ON [ops].[SupplyMovements] ([SupplyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyMovements_TenantId] ON [ops].[SupplyMovements] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyMovements_TenantId_LinkId] ON [ops].[SupplyMovements] ([TenantId], [LinkId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyMovements_TenantId_SupplyId_WarehouseId_CreatedUtc] ON [ops].[SupplyMovements] ([TenantId], [SupplyId], [WarehouseId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyMovements_WarehouseId] ON [ops].[SupplyMovements] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyStocks_SupplyId] ON [ops].[SupplyStocks] ([SupplyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyStocks_TenantId] ON [ops].[SupplyStocks] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SupplyStocks_TenantId_SupplyId_WarehouseId] ON [ops].[SupplyStocks] ([TenantId], [SupplyId], [WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyStocks_TenantId_WarehouseId] ON [ops].[SupplyStocks] ([TenantId], [WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyStocks_WarehouseId] ON [ops].[SupplyStocks] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyTransfers_FromWarehouseId] ON [ops].[SupplyTransfers] ([FromWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyTransfers_TenantId] ON [ops].[SupplyTransfers] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyTransfers_TenantId_ReceivedUtc] ON [ops].[SupplyTransfers] ([TenantId], [ReceivedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    CREATE INDEX [IX_SupplyTransfers_ToWarehouseId] ON [ops].[SupplyTransfers] ([ToWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020213345_SuppliesStock'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020213345_SuppliesStock', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [SupplyDiscountLinePercent] decimal(5,2) NOT NULL DEFAULT 10.0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [SupplyDiscountOrderPercent] decimal(5,2) NOT NULL DEFAULT 10.0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [SupplyDiscountReasonRequired] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [SupplyDiscountsAllowed] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [SupplyOrderPrefix] varchar(6) NOT NULL DEFAULT 'SO';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    EXEC(N'UPDATE [ops].[TenantSettings] SET [SupplyOrderPrefix] = N''SPO'' WHERE N''SO'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix])');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    CREATE TABLE [ops].[SupplySales] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [ShipmentId] uniqueidentifier NULL,
        [Number] varchar(20) NULL,
        [Status] int NOT NULL,
        [CurrencyCode] char(3) NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [ConfirmedUtc] datetimeoffset NULL,
        [StockMoved] bit NOT NULL,
        [LastKey] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Lines] nvarchar(max) NULL,
        CONSTRAINT [PK_SupplySales] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupplySales_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [ops].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplySales_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [ops].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplySales_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    CREATE INDEX [IX_SupplySales_CustomerId] ON [ops].[SupplySales] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    CREATE INDEX [IX_SupplySales_ShipmentId] ON [ops].[SupplySales] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    CREATE INDEX [IX_SupplySales_TenantId] ON [ops].[SupplySales] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    CREATE INDEX [IX_SupplySales_TenantId_CustomerId_Status] ON [ops].[SupplySales] ([TenantId], [CustomerId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SupplySales_TenantId_Number] ON [ops].[SupplySales] ([TenantId], [Number]) WHERE [Number] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SupplySales_TenantId_ShipmentId] ON [ops].[SupplySales] ([TenantId], [ShipmentId]) WHERE [ShipmentId] IS NOT NULL AND [Status] <> 3');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    CREATE INDEX [IX_SupplySales_WarehouseId] ON [ops].[SupplySales] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020222530_SuppliesAtCounter'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020222530_SuppliesAtCounter', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [ChargesTaxPercent] decimal(6,3) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [CreditNotePrefix] varchar(6) NOT NULL DEFAULT 'CN';
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    EXEC(N'UPDATE [ops].[TenantSettings] SET [CreditNotePrefix] = CASE WHEN N''CRN'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix], [SupplyOrderPrefix]) THEN N''CRNT'' ELSE N''CRN'' END WHERE N''CN'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix], [SupplyOrderPrefix])');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [FreightTaxPercent] decimal(6,3) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [InsuranceTaxPercent] decimal(6,3) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [InvoiceNoteEn] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [InvoiceNoteEs] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [InvoiceNoteFr] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [SuppliesTaxPercent] decimal(6,3) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [TaxName] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [TaxNumber] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    ALTER TABLE [ops].[TenantSettings] ADD [TaxPerKind] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE TABLE [ops].[Invoices] (
        [Id] uniqueidentifier NOT NULL,
        [Number] varchar(20) NOT NULL,
        [Kind] int NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [ShipmentId] uniqueidentifier NULL,
        [SupplySaleId] uniqueidentifier NULL,
        [Reference] varchar(30) NOT NULL,
        [Route] nvarchar(300) NULL,
        [ServiceCode] varchar(40) NULL,
        [CurrencyCode] char(3) NOT NULL,
        [Language] varchar(5) NOT NULL,
        [CompanyName] nvarchar(200) NOT NULL,
        [TaxNumber] nvarchar(40) NULL,
        [WarehouseName] nvarchar(200) NOT NULL,
        [CustomerCode] varchar(20) NULL,
        [CustomerName] nvarchar(200) NOT NULL,
        [CustomerCompany] nvarchar(200) NULL,
        [CustomerPhone] varchar(40) NOT NULL,
        [CustomerEmail] nvarchar(320) NOT NULL,
        [TaxName] nvarchar(40) NULL,
        [TaxPerKind] bit NOT NULL,
        [Note] nvarchar(1000) NULL,
        [IssuedOn] date NOT NULL,
        [IssuedUtc] datetimeoffset NOT NULL,
        [Status] int NOT NULL,
        [VoidedUtc] datetimeoffset NULL,
        [Gross] decimal(14,2) NOT NULL,
        [DiscountTotal] decimal(14,2) NOT NULL,
        [Subtotal] decimal(14,2) NOT NULL,
        [TaxTotal] decimal(14,2) NOT NULL,
        [Total] decimal(14,2) NOT NULL,
        [CreditedTotal] decimal(14,2) NOT NULL,
        [PaidTotal] decimal(14,2) NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Lines] nvarchar(max) NULL,
        [Taxes] nvarchar(max) NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [ops].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [ops].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_SupplySales_SupplySaleId] FOREIGN KEY ([SupplySaleId]) REFERENCES [ops].[SupplySales] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [ops].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE TABLE [ops].[CreditNotes] (
        [Id] uniqueidentifier NOT NULL,
        [InvoiceId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Status] int NOT NULL,
        [Number] varchar(20) NULL,
        [Reason] nvarchar(500) NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [RequestedUtc] datetimeoffset NOT NULL,
        [RequestKey] uniqueidentifier NOT NULL,
        [DecidedByUserId] uniqueidentifier NULL,
        [DecidedUtc] datetimeoffset NULL,
        [RefusalNote] nvarchar(500) NULL,
        [Total] decimal(14,2) NOT NULL,
        [DecidedOn] date NULL,
        [RowVersion] rowversion NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [LastModifiedUtc] datetimeoffset NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Lines] nvarchar(max) NULL,
        [Taxes] nvarchar(max) NULL,
        CONSTRAINT [PK_CreditNotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CreditNotes_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [ops].[Invoices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE INDEX [IX_CreditNotes_InvoiceId] ON [ops].[CreditNotes] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE INDEX [IX_CreditNotes_TenantId] ON [ops].[CreditNotes] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CreditNotes_TenantId_InvoiceId] ON [ops].[CreditNotes] ([TenantId], [InvoiceId]) WHERE [Status] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE INDEX [IX_CreditNotes_TenantId_InvoiceId_Status] ON [ops].[CreditNotes] ([TenantId], [InvoiceId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CreditNotes_TenantId_Number] ON [ops].[CreditNotes] ([TenantId], [Number]) WHERE [Number] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CreditNotes_TenantId_RequestKey] ON [ops].[CreditNotes] ([TenantId], [RequestKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE INDEX [IX_CreditNotes_TenantId_WarehouseId_Status] ON [ops].[CreditNotes] ([TenantId], [WarehouseId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE INDEX [IX_Invoices_CustomerId] ON [ops].[Invoices] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE INDEX [IX_Invoices_ShipmentId] ON [ops].[Invoices] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE INDEX [IX_Invoices_SupplySaleId] ON [ops].[Invoices] ([SupplySaleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId] ON [ops].[Invoices] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId_CustomerId_IssuedUtc] ON [ops].[Invoices] ([TenantId], [CustomerId], [IssuedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_TenantId_Number] ON [ops].[Invoices] ([TenantId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Invoices_TenantId_ShipmentId] ON [ops].[Invoices] ([TenantId], [ShipmentId]) WHERE [ShipmentId] IS NOT NULL AND [Kind] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Invoices_TenantId_SupplySaleId] ON [ops].[Invoices] ([TenantId], [SupplySaleId]) WHERE [SupplySaleId] IS NOT NULL AND [Kind] = 2');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId_WarehouseId_IssuedUtc] ON [ops].[Invoices] ([TenantId], [WarehouseId], [IssuedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    CREATE INDEX [IX_Invoices_WarehouseId] ON [ops].[Invoices] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021020121_InvoicesAndCreditNotes'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021020121_InvoicesAndCreditNotes', N'10.0.13');
END;

COMMIT;
GO

-- =========================================================
-- 4. CUSTOMER PLANE (schema [customer])
-- Generated from the EF migrations of
-- Vixly.Customer.Infrastructure: CustomerFoundation
-- The customer schema holds only the Customer.Web key ring.
-- =========================================================
IF OBJECT_ID(N'[customer].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'customer') IS NULL EXEC(N'CREATE SCHEMA [customer];');
    CREATE TABLE [customer].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [customer].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024801_CustomerFoundation'
)
BEGIN
    IF SCHEMA_ID(N'customer') IS NULL EXEC(N'CREATE SCHEMA [customer];');
END;

IF NOT EXISTS (
    SELECT * FROM [customer].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024801_CustomerFoundation'
)
BEGIN
    CREATE TABLE [customer].[DataProtectionKeys] (
        [Id] int NOT NULL IDENTITY,
        [FriendlyName] nvarchar(max) NULL,
        [Xml] nvarchar(max) NULL,
        CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [customer].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018024801_CustomerFoundation'
)
BEGIN
    INSERT INTO [customer].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018024801_CustomerFoundation', N'10.0.13');
END;

COMMIT;
GO




-- =========================================================
-- 5. LOGINS AND GRANTS (D-1)
-- 5a. Platform: skipped when its passwords in section 0 are empty.
-- =========================================================
SET NOCOUNT ON;
DECLARE @appPwd  nvarchar(256) = CONVERT(nvarchar(256), SESSION_CONTEXT(N'vixly.platform_app_pwd'));
DECLARE @migrPwd nvarchar(256) = CONVERT(nvarchar(256), SESSION_CONTEXT(N'vixly.platform_migr_pwd'));
DECLARE @sql nvarchar(max);

IF ISNULL(@appPwd, N'') = N'' OR ISNULL(@migrPwd, N'') = N''
BEGIN
    PRINT N'Section 5a skipped: no Platform passwords given (Windows authentication).';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'vixly_platform_app')
    BEGIN
        SET @sql = N'CREATE LOGIN [vixly_platform_app] WITH PASSWORD = ' + QUOTENAME(@appPwd, '''') + N', CHECK_POLICY = ON;';
        EXEC (@sql);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'vixly_platform_migrator')
    BEGIN
        SET @sql = N'CREATE LOGIN [vixly_platform_migrator] WITH PASSWORD = ' + QUOTENAME(@migrPwd, '''') + N', CHECK_POLICY = ON;';
        EXEC (@sql);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'vixly_platform_app')
        CREATE USER [vixly_platform_app] FOR LOGIN [vixly_platform_app] WITH DEFAULT_SCHEMA = [platform];

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'vixly_platform_migrator')
        CREATE USER [vixly_platform_migrator] FOR LOGIN [vixly_platform_migrator] WITH DEFAULT_SCHEMA = [platform];

    -- The app reads and writes the platform schema only.
    GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[platform] TO [vixly_platform_app];

    -- The audit log is append-only for the app.
    DENY UPDATE, DELETE ON OBJECT::[platform].[AuditEntries] TO [vixly_platform_app];
    -- Tenant and subscription history are append-only too (Slices 2 and 3, D-2).
    DENY UPDATE, DELETE ON OBJECT::[platform].[TenantLifecycleEvents] TO [vixly_platform_app];
    DENY UPDATE, DELETE ON OBJECT::[platform].[TenantNotes] TO [vixly_platform_app];
    DENY UPDATE, DELETE ON OBJECT::[platform].[SubscriptionEvents] TO [vixly_platform_app];
    -- Invoice history is append-only, and a stored PDF is never changed (Slice 4, D-B10).
    DENY UPDATE, DELETE ON OBJECT::[platform].[InvoiceEvents] TO [vixly_platform_app];
    DENY UPDATE, DELETE ON OBJECT::[platform].[BillingDocuments] TO [vixly_platform_app];
    -- Collections history is append-only (Slice 6, D-C15).
    DENY UPDATE, DELETE ON OBJECT::[platform].[CollectionCaseEvents] TO [vixly_platform_app];
    -- Device and licensing history is append-only (Slice 7, D-L14).
    DENY UPDATE, DELETE ON OBJECT::[platform].[DeviceEvents] TO [vixly_platform_app];
    -- Terms acceptances are evidence: never changed or deleted (Slice 8).
    DENY UPDATE, DELETE ON OBJECT::[platform].[TenantTermsAcceptances] TO [vixly_platform_app];
    -- Slice 9: published events never change; old ones are removed by the event feed clock.
    DENY UPDATE ON OBJECT::[platform].[PublishedEvents] TO [vixly_platform_app];

    -- The migrator changes the platform schema only.
    GRANT ALTER, CONTROL ON SCHEMA::[platform] TO [vixly_platform_migrator];
    GRANT CREATE TABLE TO [vixly_platform_migrator];

    PRINT N'Section 5a done: platform logins and grants are in place.';
END
GO

-- =========================================================
-- 5b. OPERATIONS AND CUSTOMER LOGINS (Stage 3, Slice 0)
-- Each login is created only when its password is given.
-- The Operations migrator also changes the customer schema
-- (the customer portal belongs to Operations).
-- =========================================================
SET NOCOUNT ON;
DECLARE @opsAppPwd  nvarchar(256) = CONVERT(nvarchar(256), SESSION_CONTEXT(N'vixly.operations_app_pwd'));
DECLARE @opsMigrPwd nvarchar(256) = CONVERT(nvarchar(256), SESSION_CONTEXT(N'vixly.operations_migr_pwd'));
DECLARE @custAppPwd nvarchar(256) = CONVERT(nvarchar(256), SESSION_CONTEXT(N'vixly.customer_app_pwd'));
DECLARE @sql nvarchar(max);

IF ISNULL(@opsAppPwd, N'') <> N''
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'vixly_operations_app')
    BEGIN
        SET @sql = N'CREATE LOGIN [vixly_operations_app] WITH PASSWORD = ' + QUOTENAME(@opsAppPwd, '''') + N', CHECK_POLICY = ON;';
        EXEC (@sql);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'vixly_operations_app')
        CREATE USER [vixly_operations_app] FOR LOGIN [vixly_operations_app] WITH DEFAULT_SCHEMA = [ops];

    -- The Operations app reads and writes the ops schema only; it never sees platform or customer.
    GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[ops] TO [vixly_operations_app];
    -- The audit log is append-only for the app (Slice 1).
    DENY UPDATE, DELETE ON OBJECT::[ops].[AuditEntries] TO [vixly_operations_app];
    -- Tracking events are append-only too: a wrong event is voided by a new one (Slice 3A, ADR-0021).
    DENY UPDATE, DELETE ON OBJECT::[ops].[TrackingEvents] TO [vixly_operations_app];
    -- Slice 3B: a warehouse receipt is a record; a photo is evidence. Neither is changed once written.
    DENY UPDATE, DELETE ON OBJECT::[ops].[WarehouseReceipts] TO [vixly_operations_app];
    DENY UPDATE ON OBJECT::[ops].[ParcelPhotos] TO [vixly_operations_app];
    PRINT N'Section 5b: Operations app login and grants are in place.';
END
ELSE
    PRINT N'Section 5b: Operations app login skipped (no password).';

IF ISNULL(@opsMigrPwd, N'') <> N''
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'vixly_operations_migrator')
    BEGIN
        SET @sql = N'CREATE LOGIN [vixly_operations_migrator] WITH PASSWORD = ' + QUOTENAME(@opsMigrPwd, '''') + N', CHECK_POLICY = ON;';
        EXEC (@sql);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'vixly_operations_migrator')
        CREATE USER [vixly_operations_migrator] FOR LOGIN [vixly_operations_migrator] WITH DEFAULT_SCHEMA = [ops];

    GRANT ALTER, CONTROL ON SCHEMA::[ops] TO [vixly_operations_migrator];
    GRANT ALTER, CONTROL ON SCHEMA::[customer] TO [vixly_operations_migrator];
    GRANT CREATE TABLE TO [vixly_operations_migrator];
    PRINT N'Section 5b: Operations migrator login and grants are in place.';
END
ELSE
    PRINT N'Section 5b: Operations migrator login skipped (no password).';

IF ISNULL(@custAppPwd, N'') <> N''
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'vixly_customer_app')
    BEGIN
        SET @sql = N'CREATE LOGIN [vixly_customer_app] WITH PASSWORD = ' + QUOTENAME(@custAppPwd, '''') + N', CHECK_POLICY = ON;';
        EXEC (@sql);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'vixly_customer_app')
        CREATE USER [vixly_customer_app] FOR LOGIN [vixly_customer_app] WITH DEFAULT_SCHEMA = [customer];

    -- Customer.Web keeps only its key ring here; business data comes through Operations.Api.
    GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[customer] TO [vixly_customer_app];
    PRINT N'Section 5b: Customer app login and grants are in place.';
END
ELSE
    PRINT N'Section 5b: Customer app login skipped (no password).';
GO

-- Clear the passwords from the session.
EXEC sys.sp_set_session_context @key = N'vixly.platform_app_pwd',    @value = NULL;
EXEC sys.sp_set_session_context @key = N'vixly.platform_migr_pwd',   @value = NULL;
EXEC sys.sp_set_session_context @key = N'vixly.operations_app_pwd',  @value = NULL;
EXEC sys.sp_set_session_context @key = N'vixly.operations_migr_pwd', @value = NULL;
EXEC sys.sp_set_session_context @key = N'vixly.customer_app_pwd',    @value = NULL;
GO

-- =========================================================
-- CHECK: tables per schema and one row per migration
-- =========================================================
SELECT s.name AS [schema], COUNT(t.object_id) AS [tables]
FROM sys.schemas AS s
LEFT JOIN sys.tables AS t ON t.schema_id = s.schema_id
WHERE s.name IN (N'platform', N'ops', N'customer')
GROUP BY s.name;

SELECT N'platform' AS [schema], MigrationId FROM [platform].[__EFMigrationsHistory]
UNION ALL SELECT N'ops', MigrationId FROM [ops].[__EFMigrationsHistory]
UNION ALL SELECT N'customer', MigrationId FROM [customer].[__EFMigrationsHistory]
ORDER BY [schema], MigrationId;
GO
