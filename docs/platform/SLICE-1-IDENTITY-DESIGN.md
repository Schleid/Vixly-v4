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
| `PlatformRolePermission` | `RolePermissions` | Stores the permission code. The list of valid codes is the permission catalogue in code, so there is no `Permissions` table to keep in step |
| `PlatformRoleAssignment` | `RoleAssignments` | Assigned by and at, optional expiry (time-boxed grant), revoked by and at. History is never deleted |
| `PlatformDepartment` | `Departments` | Label only |
| `PlatformUserDepartment` | `UserDepartments` | One primary per user |
| `PlatformAuditEntry` | `AuditEntries` | Append-only: actor, permission used, action code plus arguments (D-4), target, before and after (JSON), IP, user agent, reason, correlation id. The app login has INSERT and SELECT only |
| (EF Data Protection) | `DataProtectionKeys` | Key ring of `Platform.Web` (S-5) |

Every Id is assigned in code (D-5). The first EF migration of V4 is created in this slice.

## 3. Permissions

V3's 69 codes become the seed, renamed to the V4 error-code style (`platform.users.create`). V3 has both `X.Read` and `X.View` in every area, with no difference in use; V4 keeps one, `view`. Identity codes:

`platform.users.view`, `.create`, `.edit`, `.invite`, `.send_password_reset`, `.lock`, `.suspend`, `.reactivate`, `.deactivate`, `.reset_second_factor`, `.end_sessions`; `platform.roles.view`, `.create`, `.edit`, `.deactivate`, `.edit_permissions`, `.assign`; `platform.departments.view`, `.create`, `.edit`, `.deactivate`, `.manage_members`; `platform.audit.view`.

SuperUser is an **authority level, not a role**: a SuperUser holds every permission. V3 kept a SuperUser role in step with the authority level and needed a repair screen when the two drifted. V4 has nothing to drift. System roles: `security_administrator` (identity and audit) and `administrator` (business administration, no identity). Their names and descriptions come from resources in en, fr and es, and their permissions are set from code at deploy.

## 4. Use cases (each three files, gate first, Global Standard 5.2)

| Area | Use cases |
|---|---|
| Sign-in (anonymous) | `BeginSignIn` (password step), `CompleteSignInWithAuthenticatorApp`, `CompleteSignInWithRecoveryCode`, `BeginPasskeySignIn`, `CompletePasskeySignIn`, `RequestPasswordReset`, `ResetPassword`, `AcceptInvitation` |
| Own account | `GetMyAccount`, `ChangeMyPassword`, `BeginPasskeyRegistration`, `CompletePasskeyRegistration`, `RemoveMyPasskey`, `EnrolAuthenticatorApp`, `ConfirmAuthenticatorApp`, `RegenerateRecoveryCodes`, `ListMySessions`, `EndMySession`, `ConfirmSecondFactor` (step-up), `SignOut` |
| Users | `ListPlatformUsers` (paged), `GetPlatformUser`, `CreatePlatformUser` (sends the invitation), `UpdatePlatformUser`, `ResendInvitation`, `SendPasswordReset`, `LockPlatformUser`, `SuspendPlatformUser`, `ReactivatePlatformUser`, `DeactivatePlatformUser`, `ResetSecondFactor`, `EndUserSessions` |
| Roles | `ListPlatformRoles`, `GetPlatformRole`, `CreatePlatformRole`, `UpdatePlatformRole`, `DeactivatePlatformRole`, `ReactivatePlatformRole`, `UpdateRolePermissions`, `AssignPlatformRole`, `RevokePlatformRole`, `ListPermissionHolders` ("who has this permission"), `GetPlatformUserRoles` |
| Departments | `ListPlatformDepartments`, `GetPlatformDepartment`, `CreatePlatformDepartment`, `UpdatePlatformDepartment`, `DeactivatePlatformDepartment`, `ReactivatePlatformDepartment`, `AddPlatformDepartmentMember`, `RemovePlatformDepartmentMember`, `SetPrimaryPlatformDepartment`, `GetPlatformUserDepartments` |
| Audit | `ListAuditEntries` (paged, filter by actor, target, action, date) |

Step-up (ADR-0006 §5) is required by: lock, suspend, reactivate, deactivate, reset second factor, end user sessions, update role permissions, reactivate role, assign and revoke role.

Domain rules with tests (carried from V3's administrative safety service):
- There is always at least one active, enrolled user holding PlatformSuperUser.
- Nobody locks, suspends, deactivates, demotes or ends the sessions of their own account through admin actions.
- A protected system role cannot be renamed, deactivated or stripped of its core permissions.
- SuperUser is the authority level only; there is no SuperUser role to synchronise (replaces V3's synchronisation service and its repair panel).

## 5. Anonymous endpoints and rate limits (S-6)

| Endpoint | Policy |
|---|---|
| `POST /sign-in` | 10 per minute per client IP; per-account lockout after 5 failures for 15 minutes; antiforgery on |
| `POST /sign-in/verify`, `/sign-in/recovery`, `/sign-in/enrol` | 20 per minute per client IP; 5 wrong codes per pending sign-in, then it is dropped |
| `POST /sign-in/passkey/options`, `/sign-in/passkey` | 20 per minute per client IP; antiforgery on; each ceremony single-use for 5 minutes |
| `POST /forgot-password` | 5 per hour per client IP, 1 per 15 minutes per email; the response is always the same, whether the email exists or not |
| `POST /reset-password`, `/invitation` | 10 per hour per client IP; tokens single-use, 60 minutes (reset) and 72 hours (invitation) |

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

## 9. Progress

| Date | Step | Result |
|---|---|---|
| 2026-10-13 | Domain: `PlatformUser` aggregate (account, role grants, departments), `PlatformRole`, `PlatformDepartment`, 30 error codes | 41 domain tests: lifecycle, lockout (5 failures, 15 minutes), own-account and last-SuperUser rules, time-boxed grants, protected system roles |
| 2026-10-13 | Infrastructure: EF configurations; migration `InitialIdentity` (Users, UserAccounts, Roles, RolePermissions, RoleAssignments, Departments, UserDepartments in `platform`) | Row versions on aggregate roots; tests prove every table is in `platform` with no prefix, and that the model has no change pending a migration. 352 checks green |
| 2026-10-13 | Domain: passkeys (counter check, limit of 10), authenticator app (no code replay), 10 single-use recovery codes, admin second-factor reset, sessions (5-minute idle, 12-hour absolute, 5-minute step-up), invitation and reset links (72 h, 60 min, single use), reset-request log, append-only audit entry | 63 domain tests |
| 2026-10-13 | Infrastructure: migration `SecondFactorsSessionsAudit` (UserPasskeys, UserAuthenticatorApps, UserRecoveryCodes, UserSessions, AccountTokens, PasswordResetRequests, AuditEntries) | Model and migrations in step |
| 2026-10-13 | Application: permission catalogue (24 identity codes, plus `platform.users.change_authority` for SuperUsers only), system roles, authorization service reading access fresh from persistence, step-up guard, audit trail; use cases Lock, Suspend, Deactivate, Reactivate | 24 Application tests (sessions end, one audit row, one commit, step-up, last-SuperUser, 403, not found, concurrency). 535 checks green |
| 2026-10-13 | Infrastructure: repositories, unit of work (concurrency conflict returns false), permission reader, BCrypt (cost 12, rehash on sign-in), secret generator, TOTP (secret protected by Data Protection), SMTP mailer (pickup folder in development), schema health check; migrations `DataProtectionKeys` and `SignInChallenges` | 17 tables in `platform` including history; one global script `build/scripts/sql/vixly-v4-database.sql` (database, migrations, logins, grants; audit table denies UPDATE and DELETE to the app login) |
| 2026-10-13 | Application: sign-in (password, then authenticator or recovery code; first-time enrolment), invitation acceptance, password reset (1 per email per 15 minutes, 5 per IP per hour), sign-out, session validator, first-SuperUser setup | 40 Application tests; unknown email and wrong password give the same answer and the same timing |
| 2026-10-13 | `Vixly.Shared.Hosting` (security headers, Cloudflare forwarded headers, health) and `Vixly.Shared.Localization`; `Platform.Resources` (115 keys in en, fr, es); `Platform.Web` sign-in pages as static server rendering with no script, server-side session scheme, `--setup-first-superuser` command | 845 checks green. End to end against SQL Server with the restricted app login: invite, enrol, recovery codes, sign out, authenticator and recovery sign-in, forgot and reset password, French and Spanish pages, health endpoints, grant denials |
| 2026-10-13 | Step-up and the Users screens. Domain: a session ends after 5 failed step-up codes in a row (migration `SessionStepUpFailures`). Application: `ConfirmSecondFactor`, `GetMyPlatformAccess`, `ListPlatformUsers`, `GetPlatformUser`, `GetPlatformUserActions`, `EndPlatformUserSessions`, `ResetPlatformUserSecondFactor`, `ResendPlatformUserInvitation`, `SendPlatformUserPasswordReset`; shared checks in `PlatformUserAdministration`. Web: `Shared.UI` presenter base, step-up dialog, user list (search, status, pages in the URL), user page (summary, account status panel, sign-in and security panel), invite form; en, fr, es text | 1,114 checks green, including new UI rules (UI-6, UI-7, UI-10) and presenter tests without a Blazor host (UI-9). Browser run against SQL Server with the restricted app login: invite, resend, accept, lock with step-up (wrong code refused, right code accepted, target signed out), reactivate, reset second factor, search, French and Spanish |
| 2026-10-13 | Passkeys (WebAuthn, Fido2NetLib 4.3). Domain: single-use, 5-minute `PlatformPasskeyCeremony` (migration `PasskeyCeremonies`). Application: `BeginPasskeySignIn`, `CompletePasskeySignIn`, `GetMyAccount`, `BeginPasskeyRegistration`, `CompletePasskeyRegistration`, `RemoveMyPasskey`, `BeginPasskeyStepUp`, `ConfirmSecondFactorWithPasskey`. Web: "Sign in with a passkey" on the sign-in page (progressive: hidden without script support), My account with sign-in methods, passkey option in the step-up dialog. Fixed: sign-out from the console header failed (missing form value); multi-collection queries now split | 1,257 checks green. Browser run with a virtual authenticator: add a passkey, sign in with it (no password), step-up with it to remove it, refused sign-in with the removed passkey, French button text |
| 2026-10-13 | My account. Application: `ChangeMyPassword` (a wrong current password counts like a wrong step-up code; other sessions end, this one stays), `EndMySessions` (one or all others, never this one), `RegenerateRecoveryCodes` (step-up), `ChangeMyLanguage`; `GetMyAccount` marks the current session. Web: profile with language, password, sign-in methods with new recovery codes, open sessions. The console now shows each signed-in user in their account language (the language links apply to the sign-in pages) | 1,328 checks green. Browser run: end the other device's session (it is sent to sign-in), switch to French and back, new recovery codes after step-up (one signs in), password change with mismatch and wrong-current refusals, sign-in with the new password |
| 2026-10-14 | Roles and departments. Application: role use cases (list, get, create, rename, deactivate, reactivate, permissions, assign, revoke, who has a permission, a user's roles) and department use cases (list, get, create, edit, deactivate, reactivate, add and remove a member, primary department, a user's departments); `PlatformSystemRoleSynchronizer`; `PlatformAccess`; no schema change. Web: Roles list, role page (details, holders, permission matrix with "who has this?"), new role, who-has-this-permission page; Departments list, department page (details, members), new department; roles and departments sections on the user page; Roles and Departments in the menu; system roles created and corrected at every start | 1,734 checks green (20 new Application tests). Browser runs against SQL Server with the restricted app login: system roles appear at start; duplicate names refused; permissions saved, then step-up asked once the window had passed; a role given until a date lets the holder see Users, deactivating it takes that away, reactivating asks for step-up; who has a permission lists SuperUsers and role holders; own roles cannot be changed; departments created, edited, deactivated; members added, made primary and removed; French and Spanish text |
| Next | First-time enrolment with a passkey; audit log screen | |

### Decisions taken while building (2026-10-13)

- **Nobody acts on a higher authority.** An administrator cannot lock, suspend, deactivate, reactivate, reset the second factor of, end the sessions of, or send links to a user whose authority level is above their own. The actor's level is read fresh from the database. This closes a gap where a security administrator could lock out SuperUsers one by one. Reversible if the owner prefers otherwise.
- **Step-up has its own attempt limit.** Interactive calls never pass through the HTTP rate limiter, so the session counts failed step-up codes and ends itself at the fifth.
- **Fresh data per operation.** A feature component keeps one DI scope for its life; each use case run now starts by clearing what the previous one loaded (`PlatformUseCaseScope`). Without it, a second action on the same screen read stale records (found by the browser run).
- **Audit IP in interactive calls.** The request IP, browser and correlation id are captured when the circuit opens and copied into each component scope (`PlatformInteractiveRequest`), so audit rows written from the console keep them.
- **`Shared.UI` started early.** Only `PresenterBase`, `PresenterComponent` and `PresenterRunOutcome`, for Frontend/UI review; no primitives.

### Decisions taken while building (2026-10-14)

- **No privilege escalation through roles.** Unless you are a SuperUser, you can only add a permission to a role, give a role, or reactivate a role when you hold every permission involved yourself. Taking access away has no such check.
- **`platform.users.change_authority` never sits in a role.** Only the SuperUser authority level carries it; the role editor does not offer it and the use case refuses it.
- **Reactivating a role needs step-up.** It hands access back to everyone still holding the role at once, so it joins the step-up list.
- **System roles are synchronised at every start**, not by a separate deploy step: a hosted service creates missing system roles and corrects drifted permissions, audited with no actor. A custom role already using a system key as its name is left alone and logged as a conflict. A failure is logged and the console still starts.
- **A role's last day ends at 23:59:59 UTC** that day, so the date shown is the date chosen.
- **Departments need no step-up.** They are labels and grant nothing (D-S3). People are added to a department from their user page; the department page lists and removes members. An inactive department keeps its members but takes no new ones. The authority rule still applies: you cannot change the departments of someone above you.
- **Roles and departments left the user summary.** Each has its own section (`UserRoleGrants`, `UserDepartments`) with its own presenter, so the summary stays within its budget and each reloads on its own.
