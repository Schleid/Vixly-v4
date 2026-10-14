# Slice 1: Identity and access - V3 behaviour sweep

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Sweep complete, 2026-10-13. Owner decisions D-S1 to D-S3 taken the same day (section 5).** |
| Source | V3 `ControlPlane/src` at `main` HEAD (read-only) |
| Rule | Replicate behaviour, not code (upgrade plan). Classification: Active, Duplicate, Dead, Disconnected-wanted, Moved |
| Feeds | Slice 1 design note; ADR-0006 (staff identity) |

## 1. How the evidence was gathered

Every Identity use case, entity, Razor page, hosted service and maintenance panel in V3 was traced to its callers: who injects or calls each type outside its own folder. A use case is **Active** when a page or endpoint calls it. It is **Dead** when only the DI registration names it. It is **Disconnected** when nothing names it at all, not even DI. Usage logs were not used for this area, because every Active item sits behind a console page that operators use.

## 2. Use cases (42)

| V3 use case | Called from | Status | V4 outcome |
|---|---|---|---|
| LoginPlatformUser | `POST /auth/login` (Program.cs) | Active | Rebuilt with MFA (ADR-0006), antiforgery, lockout kept |
| RequestPlatformPasswordReset | ForgotPassword page | Active | Kept; request log keeps throttling (S-6) |
| ResetPlatformPassword | ResetPassword page | Active | Kept |
| CreatePlatformUserAccount | SetupAccount page (invitation link) | Active | Kept as "accept invitation" |
| SendPlatformUserInvitation | Users workspace | Active | Kept; email in recipient's language (L-4) |
| ResetPlatformUserPassword (admin sets a temporary password) | Users workspace | Active, **unsafe** | **Decision D-S1.** Recommended: drop; the admin sends a reset link instead |
| CreatePlatformUser, UpdatePlatformUser, GetPlatformUser, ListPlatformUsers | Users workspace (GetPlatformUser also Billing workspace) | Active | Kept; list paged (P-1); no date of birth |
| GetPlatformUserAccount | Users workspace | Active | Kept, merged into the user detail read |
| AssignPlatformUserRole, RevokePlatformUserRole, GetPlatformUserRoles | Users workspace | Active | Kept; assignment history kept; time-boxed grants added |
| UpdatePlatformUserRoles | DI only | Dead | Dropped (duplicate of Assign/Revoke) |
| UpdatePlatformUserRoleAssignments, GetPlatformUserRoleAssignments | DI only | Dead | Dropped (duplicates) |
| ActivatePlatformUserAccount, DeactivatePlatformUserAccount, LockPlatformUserAccount, SuspendPlatformUserAccount | nothing, not even DI | Disconnected | **Decision D-S2.** Recommended: keep; staff offboarding needs them |
| ChangePlatformUserPassword (own password) | nothing, not even DI | Disconnected | Keep: needed for MFA enrolment and self-service; see finding F-2 |
| CreatePlatformRole, UpdatePlatformRole, ActivatePlatformRole, DeactivatePlatformRole, ListPlatformRoles | Roles workspace (List also Users workspace) | Active | Kept; `PlatformRole` gets real domain methods (V3 has public setters) |
| GetPlatformRolePermissions, UpdatePlatformRolePermissions | Roles workspace, role permission editor | Active | Kept; every change audited |
| Create/Update/Activate/Deactivate/GetPlatformDepartment, ListPlatformDepartments, ListPlatformDepartmentUsers, Assign/RemovePlatformUserDepartment, SetPrimaryPlatformUserDepartment | Departments workspace | Active | **Decision D-S3** (department scope) |
| ListPlatformUserDepartments | DI only | Dead | Dropped (the workspace uses ListPlatformDepartmentUsers) |
| RepairPlatformAuthoritySynchronization | Maintenance workspace | Active | Replaced: V4 keeps the SuperUser invariant inside the domain, so there is nothing to repair |
| LoginTenantUser | DI only | Moved | Operations (ADR-0003) |

Result: 32 Active, 4 Dead, 5 Disconnected, 1 Moved.

## 3. Other items

| Item | Status | V4 outcome |
|---|---|---|
| `PlatformPermissionCodes` (69 codes), `PlatformPermissionCatalog` | Active | Codes kept as the seed list. Names and descriptions move to resources (L-6). The permission table is synchronised from code at deploy time, never edited by hand |
| Roles PlatformSuperUser, PlatformSecurityAdministrator, PlatformAdministrator | Active | Kept as protected system roles |
| `PlatformAdministrativeSafetyService` (no self-lockout, never remove the last administrator) | Active | Kept as domain rules with tests. This is the most valuable behaviour in the area |
| `PlatformAuthoritySynchronizationService` (keeps the SuperUser role assignment in step with `AuthorityLevel`) | Active | Behaviour kept, mechanism replaced: one domain method changes both, so they cannot drift |
| Per-account failed-login lockout (`FailedLoginAttemptCount`, `LoginLockoutEndsOnUtc`) | Active | Kept |
| 5-minute idle logout, warning before timeout | Active | Kept (S-4) |
| Session generation from `SessionSecurity:CookieVersion` (logs everyone out) | Active | Kept as the global switch; per-user session list and revoke added (ADR-0006) |
| `PlatformPasswordResetTokenCleanupService` (hosted in the web process) | Active | Becomes a Worker job (ADR-0007) |
| `PlatformPasswordResetRequestLog` | Active | Kept for throttling; retention limit added |
| `PlatformEffectivePermissionService` (menu visibility) | Active | Kept for the menu only; never authorization (S-1) |
| Maintenance: PlatformRoleSynchronizationPanel, PlatformPermissionSynchronizationPanel, PlatformInvariantValidationPanel | Dead (the workspace renders only the authority repair panel) | Dropped |
| Policies PlatformOwnerOnly, PlatformAdminOnly, BillingManagersOnly, PlatformSupportOnly | Dead | Dropped (already in the upgrade plan) |
| `PlatformUser.DateOfBirth` | Active field, no purpose | Dropped (upgrade plan) |
| `PlatformUser.EmployeeNumber` | Active | Kept, optional |
| SQL scripts for roles, departments and setup tokens under `SqlScripts/` | Dead | V4 uses EF migrations only |

## 4. Findings that change Slice 1

- **F-1: department-scoped roles are not enforced.** `PlatformRole.ScopedDepartmentId` is stored and shown in the UI. Neither `PlatformAuthorizationService` nor `PlatformPermissionEvaluator` reads it. A "Billing department" role grants its permissions platform-wide. → Decision D-S3.
- **F-2: "must change password" is never enforced.** An admin-set temporary password sets `MustChangePassword = true`. The login use case never checks the flag, and the change-password use case is not wired up. So a user can keep a password the administrator knows. → Decision D-S1. Any temporary credential in V4 is forced to change before any other page loads.
- **F-3: no way to offboard staff.** The console cannot lock, suspend or deactivate an account. The domain methods exist, but the use cases are not registered. Today a departing employee keeps access until someone edits the database. → Decision D-S2.
- **F-4: `PlatformRole` has public setters.** Any code can rename or reactivate a role without passing a rule. V4 uses domain methods only (upgrade plan "Invariants").

## 5. Owner decisions (taken 2026-10-13)

| ID | Question | Recommendation (accepted by the owner) |
|---|---|---|
| D-S1 | Admin-set temporary passwords | **Decided: drop.** The administrator sends a reset or invitation link; nobody but the user ever knows a password |
| D-S2 | Lock, suspend, deactivate staff accounts from the console | **Decided: add.** Keep them behind the security-administrator permission, with a reason and an audit row; deactivation also ends that user's sessions |
| D-S3 | Department-scoped roles | **Decided: (a), departments as labels.** The options were: either (a) drop department scope: departments stay as an organisational label, and roles are platform-wide, as V3 effectively behaves today; or (b) enforce scope, which needs a department on every record a permission protects (tenants, invoices, cases). Recommended: (a) now, (b) later if a real need appears |

## 6. Slice 1 scope after the sweep

32 Active behaviours, rebuilt to the standard, plus the additions in the upgrade plan and ADR-0006: MFA (passkeys, TOTP fallback), step-up confirmation, session list and revoke, time-boxed role grants, the "who has this permission" view, audit from the first command (`PlatformAuditEntry`), and en/fr/es text throughout. Per the decisions above: account lifecycle actions (D-S2) and own-password change are added. Admin-set passwords (D-S1) and department-scoped roles (D-S3) are not carried over: `PlatformRole` has no department, and `PlatformDepartment` groups staff only. The migration tool maps every V3 role as platform-wide, which is how V3 already behaves.
