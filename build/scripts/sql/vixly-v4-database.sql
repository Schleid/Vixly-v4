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
--   skipped (recorded in [platform].[__EFMigrationsHistory]).
--   Run the newest version of this file after every pull.
--
-- SECTIONS
--   0. Settings (edit here only)
--   1. Database
--   2. Platform plane schema (EF migrations, generated)
--   3. Logins and grants (only when passwords are given)
--   Operations and Customer sections are added when those
--   planes are built (Global Standard section 12).
--
-- IMPORTANT NOTES
-- - Section 2 is generated from the EF migrations and is
--   rebuilt with every new migration; never edit it by hand.
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
-- Leave both passwords empty on a developer machine that
-- uses Windows authentication: section 3 is then skipped.
-- Staging and production: fill both, run, then clear them.
-- =========================================================
EXEC sys.sp_set_session_context @key = N'vixly.platform_app_pwd',  @value = N'';
EXEC sys.sp_set_session_context @key = N'vixly.platform_migr_pwd', @value = N'';
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
-- Vixly.Platform.Infrastructure: InitialIdentity, SecondFactorsSessionsAudit, DataProtectionKeys, SignInChallenges, SessionStepUpFailures, PasskeyCeremonies, Tenants, Subscriptions, CommercialFlexibility
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

-- =========================================================
-- 3. LOGINS AND GRANTS (D-1)
-- Skipped when the passwords in section 0 are empty.
-- =========================================================
SET NOCOUNT ON;
DECLARE @appPwd  nvarchar(256) = CONVERT(nvarchar(256), SESSION_CONTEXT(N'vixly.platform_app_pwd'));
DECLARE @migrPwd nvarchar(256) = CONVERT(nvarchar(256), SESSION_CONTEXT(N'vixly.platform_migr_pwd'));
DECLARE @sql nvarchar(max);

IF ISNULL(@appPwd, N'') = N'' OR ISNULL(@migrPwd, N'') = N''
BEGIN
    PRINT N'Section 3 skipped: no passwords given (Windows authentication).';
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

    -- The migrator changes the platform schema only.
    GRANT ALTER, CONTROL ON SCHEMA::[platform] TO [vixly_platform_migrator];
    GRANT CREATE TABLE TO [vixly_platform_migrator];

    PRINT N'Section 3 done: platform logins and grants are in place.';
END
GO

-- Clear the passwords from the session.
EXEC sys.sp_set_session_context @key = N'vixly.platform_app_pwd',  @value = NULL;
EXEC sys.sp_set_session_context @key = N'vixly.platform_migr_pwd', @value = NULL;
GO

-- =========================================================
-- CHECK: expect 17 tables in [platform] and one row per
-- migration
-- =========================================================
SELECT s.name AS [schema], COUNT(t.object_id) AS [tables]
FROM sys.schemas AS s
LEFT JOIN sys.tables AS t ON t.schema_id = s.schema_id
WHERE s.name IN (N'platform', N'ops', N'customer')
GROUP BY s.name;

SELECT MigrationId FROM [platform].[__EFMigrationsHistory] ORDER BY MigrationId;
GO
