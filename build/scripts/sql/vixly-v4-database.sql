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
-- Vixly.Platform.Infrastructure: InitialIdentity, SecondFactorsSessionsAudit, DataProtectionKeys, SignInChallenges, SessionStepUpFailures, PasskeyCeremonies
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
