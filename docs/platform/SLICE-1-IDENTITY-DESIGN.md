# Slice 1: Identity and access - design note

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Approved for build, 2026-10-13** (ADR-0006 and ADR-0011 accepted) |
| Inputs | [Sweep](SLICE-1-IDENTITY-SWEEP.md) (decisions D-S1 to D-S3), ADR-0006, ADR-0011, upgrade plan Slice 1 row |
| Depends on | Slice 0 (signed) |

## 1. Outcome

A staff member is invited, sets a password and enrols a passkey or authenticator. They then sign in to `vixly-control.techcrafters.ltd` behind Cloudflare Access and reach a console whose menu and actions match their roles. Security administrators manage users, roles, departments and sessions, and every change is audited. Everything is in English, French and Spanish.

## 2. Data (schema `platform`; class → table, Global Standard 4.1)

| Class | Table | Notes |
|---|---|---|
| `PlatformUser` | `Users` | Name, email (`EmailAddress`, unique), optional employee number and phone, `PreferredLanguage`, `ThemePreference`, `AuthorityLevel`. No date of birth |
| `PlatformUserAccount` | `UserAccounts` | One per user. Status (Invited, Active, Suspended, Locked, Deactivated) with reason and actor; BCrypt hash; failed-attempt count and lockout end; last sign-in |
| `PlatformUserPasskey` | `UserPasskeys` | Credential id, public key, sign count, AAGUID, user-given name, created and last used |
| `PlatformUserAuthenticatorApp` | `UserAuthenticatorApps` | TOTP secret encrypted with Data Protection; confirmed time; last accepted time step (no code reuse) |
| `PlatformUserRecoveryCode` | `UserRecoveryCodes` | Hash only; used time |
| `PlatformUserSession` | `UserSessions` | Session id (random, 256-bit), created, last seen, absolute expiry, IP, user agent, second-factor time (for step-up), ended time and reason |
| `PlatformAccountToken` | `AccountTokens` | One table for invitation and password-reset tokens (V3 had two). Purpose, hash, expiry, used time |
| `PlatformPasswordResetRequest` | `PasswordResetRequests` | Throttle log; rows older than 30 days are removed by a Worker job |
| `PlatformRole` | `Roles` | Name per language through `RoleTranslations` for system roles; custom roles use the name the creator typed. Protected flag for system roles. No department (D-S3) |
| `PlatformPermission` | `Permissions` | Synchronised from code at deploy; never edited in the UI |
| `PlatformRolePermission` | `RolePermissions` | |
| `PlatformRoleAssignment` | `RoleAssignments` | Assigned by and at, optional expiry (time-boxed grant), revoked by and at. History is never deleted |
| `PlatformDepartment` | `Departments` | Label only |
| `PlatformUserDepartment` | `UserDepartments` | One primary per user |
| `PlatformAuditEntry` | `AuditEntries` | Append-only: actor, permission used, action code plus arguments (D-4), target, before and after (JSON), IP, user agent, reason, correlation id. The app login has INSERT and SELECT only |
| (EF Data Protection) | `DataProtectionKeys` | Key ring of `Platform.Web` (S-5) |

Every Id is assigned in code (D-5). The first EF migration of V4 is created in this slice.

## 3. Permissions

V3's 69 codes become the seed, renamed to the V4 error-code style (`platform.users.create`). V3 has both `X.Read` and `X.View` in every area, with no difference in use; V4 keeps one, `view`. Identity codes:

`platform.users.view`, `.create`, `.edit`, `.invite`, `.send_password_reset`, `.lock`, `.suspend`, `.reactivate`, `.deactivate`, `.reset_second_factor`, `.end_sessions`; `platform.roles.view`, `.create`, `.edit`, `.deactivate`, `.edit_permissions`, `.assign`; `platform.departments.view`, `.create`, `.edit`, `.deactivate`, `.manage_members`; `platform.audit.view`.

System roles: PlatformSuperUser (everything), PlatformSecurityAdministrator (identity and audit), PlatformAdministrator (business administration, no identity). Their names and descriptions come from resources in en, fr and es.

## 4. Use cases (each three files, gate first, Global Standard 5.2)

| Area | Use cases |
|---|---|
| Sign-in (anonymous) | `BeginSignIn` (password step), `CompleteSignInWithAuthenticatorApp`, `CompleteSignInWithRecoveryCode`, `BeginPasskeySignIn`, `CompletePasskeySignIn`, `RequestPasswordReset`, `ResetPassword`, `AcceptInvitation` |
| Own account | `GetMyAccount`, `ChangeMyPassword`, `BeginPasskeyRegistration`, `CompletePasskeyRegistration`, `RemoveMyPasskey`, `EnrolAuthenticatorApp`, `ConfirmAuthenticatorApp`, `RegenerateRecoveryCodes`, `ListMySessions`, `EndMySession`, `ConfirmSecondFactor` (step-up), `SignOut` |
| Users | `ListPlatformUsers` (paged), `GetPlatformUser`, `CreatePlatformUser` (sends the invitation), `UpdatePlatformUser`, `ResendInvitation`, `SendPasswordReset`, `LockPlatformUser`, `SuspendPlatformUser`, `ReactivatePlatformUser`, `DeactivatePlatformUser`, `ResetSecondFactor`, `EndUserSessions` |
| Roles | `ListPlatformRoles`, `GetPlatformRole`, `CreatePlatformRole`, `UpdatePlatformRole`, `DeactivatePlatformRole`, `ReactivatePlatformRole`, `UpdateRolePermissions`, `AssignRole`, `RevokeRole`, `ListPermissionHolders` ("who has this permission") |
| Departments | `ListDepartments`, `CreateDepartment`, `UpdateDepartment`, `DeactivateDepartment`, `ReactivateDepartment`, `AddDepartmentMember`, `RemoveDepartmentMember`, `SetPrimaryDepartment` |
| Audit | `ListAuditEntries` (paged, filter by actor, target, action, date) |

Step-up (ADR-0006 §5) is required by: lock, suspend, reactivate, deactivate, reset second factor, end user sessions, update role permissions, assign and revoke role.

Domain rules with tests (carried from V3's administrative safety service):
- There is always at least one active, enrolled user holding PlatformSuperUser.
- Nobody locks, suspends, deactivates, demotes or ends the sessions of their own account through admin actions.
- A protected system role cannot be renamed, deactivated or stripped of its core permissions.
- `AuthorityLevel` SuperUser and the PlatformSuperUser role assignment change together in one method (replaces V3's synchronisation service).

## 5. Anonymous endpoints and rate limits (S-6)

| Endpoint | Policy |
|---|---|
| `POST /auth/sign-in` | 10 per minute per client IP; per-account lockout after 5 failures for 15 minutes; antiforgery on |
| `POST /auth/second-factor` | 5 attempts per pending sign-in, then the pending sign-in is dropped |
| `POST /auth/passkey/options`, `/auth/passkey/complete` | 20 per minute per client IP |
| `POST /auth/password-reset` | 5 per hour per client IP, 1 per 15 minutes per email; the response is always the same, whether the email exists or not |
| `POST /auth/password-reset/complete`, `/auth/invitation/accept` | 10 per hour per client IP; tokens single-use, 60 minutes (reset) and 72 hours (invitation) |

Client IP is the `CF-Connecting-IP` header, trusted only from Cloudflare's address ranges (ADR-0011).

## 6. Projects created in this slice

`Vixly.Platform.Resources` (en, fr, es), `Vixly.Shared.Localization`, `Vixly.Shared.Hosting` (ADR-0011), `Vixly.Platform.Web`. `Shared.UI` primitives come from the Frontend/UI team in parallel; until they land, the presenters are built and tested without markup. Infrastructure gains the EF configurations, repositories, Data Protection store, the BCrypt, WebAuthn and TOTP adapters, and the first migration.

`Platform.Api` is not needed for identity and opens in Slice 2. `Platform.Worker` opens in Slice 4; until then the reset-request cleanup is a documented manual step.

## 7. Frontend/UI handoff

Presenters and state for: sign-in (password, passkey, second factor, recovery), invitation acceptance with enrolment, forgot and reset password, My account (security, sessions, language, theme), Users, User detail (profile, roles, departments, sessions, activity), Roles with the permission matrix, Departments, Audit log. Frontend/UI designs these flows from scratch per the Frontend Guide. V3's screens are a list of behaviours, not a layout.

## 8. Done when (in addition to the slice gate, plane document section 8)

- Every staff sign-in needs a second factor; a test proves a password alone never yields a session.
- Migrated-style BCrypt hashes at a lower cost verify, and are upgraded to cost 12 on sign-in.
- Lock, suspend and deactivate end the target's sessions immediately (integration test).
- Last-SuperUser and self-action rules have domain tests.
- Every command writes exactly one audit entry in the same commit as the change (D-2).
- Login CSRF, lockout and rate-limit tests pass; the response headers test passes on `Platform.Web`.
- en, fr and es resources are complete for every string and error this slice adds.
