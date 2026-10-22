# Slice 1: Staff and tenant setup - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 3) |
| Status | **Building in five parts.** 1A (landing page, sign-in, owner invitation) and 1B (public site) built 2026-10-18, see the [1A verification record](SLICE-1A-VERIFICATION.md). Owner decisions on the [sweep](SLICE-0-1-FOUNDATION-AND-SETUP-SWEEP.md) taken 2026-10-17 (section 10). |
| Depends on | Slice 0; Platform Slices 2, 7, 8 and 9; B12 (company change requests) |

## 1. Outcome

A newly paid tenant's owner receives an email in their language, sets a password and a second factor on `{slug}.techcrafters.ltd`, accepts the Terms, enrols the computer they work from, and lands on a setup checklist. They set the billing currency and units, create branches and warehouses, invite staff and give them roles limited to a branch or a warehouse. Staff sign in only from licensed devices. Every change is audited, everything is in English, French and Spanish, and no screen says "Vixly".

## 1a. Build order (owner, 2026-10-18: "the tenant portal slug resolution, then the landing and the login page")

| Part | Contents |
|---|---|
| **1A** (built) | Tenant landing with the tenant name and the sign-in form; password, authenticator app and recovery codes; owner invitation from the Platform event; sessions (15 minutes idle, 12 hours); sign-out; audit; texts in en, fr and es |
| **1B** (built) | The tenant's public site, brand, locations and contacts per country, site editor; staff sign-in moves to `/staff` ([design](SLICE-1B-TENANT-SITE-DESIGN.md), owner, 2026-10-18) |
| **1C** | **Onboarding completion** (owner, 2026-10-18, section 1b): first device (the tenant goes live), device checks on every session, the Devices screen, the welcome email, forgot and reset password, Terms acceptance when a new version is due, access levels (read-only, payment required), Owner support recovery and second-factor reset (D-O13) |
| 1D | Staff: Administrator role, staff invitations, roles with scope, staff lifecycle, step-up (passkeys moved to their own slice: see the [Slice 1D record](SLICE-1D-VERIFICATION.md)) |
| 1E | Settings, company, billing page, numbering, setup checklist, audit log screen (shipping services wait for the catalogue: see the [Slice 1E record](SLICE-1E-VERIFICATION.md)) |

## 1b. Onboarding, end to end (owner, 2026-10-18)

The registration crosses the public site, Control (Platform) and the tenant host. Each email comes from the plane that owns the step.

| # | Where | Step | Email |
|---|---|---|---|
| 1 | Public site, Control | The prospect fills in the form and accepts the Terms | Control: verify your email |
| 2 | Public site, Control | The prospect verifies the email, chooses a plan and pays (or starts a trial) | |
| 3 | Control, Operations | Control publishes `account_setup_ready`; Operations creates the Owner and the brand | Operations: set up your account (password) |
| 4 | Tenant host | The Owner sets a password, enrols an authenticator app, keeps the recovery codes | |
| 5 | Tenant host, Control | The Owner asks to enrol this computer; Control sends the first-device link | Control: enrol your first computer |
| 6 | Tenant host, Control | The Owner opens the link on the computer; the device enrols, the registration completes and the tenant becomes Active in one Control commit | |
| 7 | Control | **Last:** the onboarding is complete | Control: welcome, with the links to the staff sign-in and the public site |

- **Terms:** accepted at registration (step 1) and recorded by Control when the tenant becomes Active (Platform Slice 8), so setup has no Terms step. Operations shows the Terms page to the Owner only when a newer version must be accepted, and the tenant becomes read-only when it is overdue.
- **Forgot your password:** every sign-in page has the link. The reset link is emailed to the account's address, lasts 60 minutes and works once; the new password ends every session, and the second factor is still asked at the next sign-in. This is how the Owner resets their password too (owner, 2026-10-18); a lost second factor goes to Vixly support (D-O13).
- **Device proof in browsers (1C):** the rotating secret in the `__Host-vx4-device` cookie. The device key pair is kept for scanners and installed apps, which arrive with the operational slices.

## 2. Data (schema `ops`; every table carries `TenantId`)

| Class | Table | Notes |
|---|---|---|
| `OperationsUser` | `Users` | Name, email (unique per tenant, D-O4), phone, employee number, job title, preferred language, theme. `IsOwner` (exactly one per tenant) |
| `OperationsUserAccount` | `UserAccounts` | Invited, Active, Suspended, Locked, Deactivated, with reason and actor; BCrypt hash (cost 12, upgraded on sign-in, M-4); failed attempts and lockout end; last sign-in; `LegacyV3Id` for migrated staff |
| `OperationsUserPasskey`, `OperationsUserAuthenticatorApp`, `OperationsUserRecoveryCode` | `UserPasskeys`, `UserAuthenticatorApps`, `UserRecoveryCodes` | As Platform Slice 1; passkeys bound to the tenant host (ADR-0012 W-6); TOTP issuer is the tenant's display name |
| `OperationsUserSession` | `UserSessions` | Session id, created, last seen, idle deadline (15 minutes), absolute expiry (12 hours), IP, user agent, device id, second-factor time (step-up), ended time and reason |
| `OperationsUserToken` | `UserTokens` | Invitation (72 hours) and password reset (60 minutes); hash only, single use |
| `OperationsRole` | `Roles` | System roles Owner and Administrator (names from resources) and tenant roles (name as typed) |
| `OperationsRolePermission` | `RolePermissions` | Permission code from the catalogue in code |
| `OperationsRoleGrant` | `RoleGrants` | User, role, **scope** (tenant, branch or warehouse) and scope id, granted by and at, optional expiry, revoked by and at |
| `OperationsJobTitle` | `JobTitles` | Label only |
| `OperationsTenantSettings` | `TenantSettings` | Billing currency (ISO 4217; changeable only while no invoice exists, ADR-0018), weight unit (kg, lb), dimension unit (cm, in), payment gate (paid first, Collect allowed, credit allowed), delivery modes (pickup, home delivery), require second factor for all staff (bool), number prefixes, setup checklist state |
| `OperationsBranch` | `Branches` | Code, name, country, city, address, phones, time zone (defaults to the tenant's), status (Active, Inactive), side (origin, destination, both) |
| `OperationsWarehouse` | `Warehouses` | Branch, code, name, address, status (Active, Inactive), roles: receiving, dispatch hub, pickup point |
| `OperationsShippingServiceSetup` | `ShippingServiceSetups` | Per tenant and Platform shipping-service code: label, expected business days, volumetric divisor in the tenant's units (sweep D-O8) |
| `OperationsNumberSequence` | `NumberSequences` | Tenant, kind, year, last number; row version (D-O12) |
| `OperationsTenantBrand` | `TenantBrands` | Display name, logos (light, dark), favicon, colour preset and allowlisted tokens, email sender name, reply-to, support email and phone, legal footer; draft and published versions (ADR-0012 W-2). Customer documents and emails use the published version |
| `OperationsAuditEntry` | `AuditEntries` | Append-only, as Platform (the app login has INSERT and SELECT only) |
| `OperationsPlatformFeedPosition` | `PlatformFeedPositions` | Last event-feed position (system table, not per tenant) |

Weights and dimensions are stored in kilograms and centimetres whatever the tenant shows (sweep D-O8).

## 3. Rules

### Owner's first sign-in (sweep D-O3)

1. Platform publishes `platform.registration.account_setup_ready.v1` whenever a registration moves to "pending account setup": after the first invoice is paid, or at once for a trial (**Platform change, section 8**). The event carries ids only (tenant, registration); the feed is not split by tenant, so it never carries names or emails.
2. Operations reads the owner contact and language with a tenant-scoped call (`GET /tenant`), creates the Owner (Invited), the system roles, the default settings (currency from the plan's currency, units kg and cm) and a brand draft from the tenant's trading name, then emails the setup link to the tenant host.
3. The tenant is still "setting up" (Slice 0, D-F2): the host serves only the setup pages. The owner sets a password, enrols a passkey or authenticator app (required for Owners), and gets ten recovery codes. The Terms were accepted at registration (section 1b), so setup asks nothing more. This **setup session** needs no licensed device, reaches only the setup pages, and ends after 15 idle minutes like any session.
4. Operations asks Platform for the first-device link (`POST /tenant/devices/first-device-link`), and Platform emails it. The owner opens `/devices/first#token` on the computer they work from; Operations completes it (`POST /internal/v1/device-links/complete`), the device enrols and the tenant becomes Active in one Platform commit (Slice 7). From then on every session needs a licensed device. Platform then sends the welcome email, the last email of the onboarding.
5. Migrated V3 tenants follow the migration plan instead: their owner and staff exist already, keep their passwords and enrol a second factor at first sign-in if their role needs one.

### Sign-in and second factor (owner decision D-O2)

- Password step, then the second factor when the user has one. Passkey sign-in counts as both factors.
- **Required** for the Owner and for anyone holding the Administrator role; **optional** for everyone else, unless the tenant turns on "require for all staff" (Owner or Administrator, step-up, audited).
- A user who must have a second factor and has none is taken to enrolment before any page loads.
- Step-up (fresh second factor within 5 minutes) is needed for: account lifecycle actions, resetting another user's second factor, ending another user's sessions, role permission changes, reactivating a role, role grants and revocations, regenerating one's own recovery codes, device approval, denial and release, the "require for all staff" setting, the billing currency, company contacts, language and time zone (`PUT /tenant`), company change requests (B12) and publishing the brand. A user without a second factor who tries a step-up action is asked to enrol one.
- Lockout after 5 failed passwords for 15 minutes; per-IP limits (section 6); real client IP from Cloudflare only (ADR-0011).

### Sessions (owner decision D-O6)

- Idle sign-out after **15 minutes** for every tenant, with a warning before it; absolute limit 12 hours.
- Sessions are server records; a user sees and ends their own; an Administrator ends anyone's; suspend, lock and deactivate end them at once.
- A session is bound to its tenant and device.

### Licensed devices

- Every staff session (after setup) needs a device that passes Platform's check. Customers are never device-checked.
- **Proof:** a browser that supports it holds a non-extractable WebCrypto P-256 key and signs Platform's challenge; any other browser keeps the rotating secret in `__Host-vx4-device`. The check and any secret rotation run on a normal HTTP request (sign-in, and a short endpoint the page calls every few minutes), never inside a Blazor circuit, so a new secret is always written to the cookie before the old one stops working.
- A new device asks to join; an Owner or Administrator approves it in Operations when a seat is free, with step-up. Releases follow Platform's allowance and review.
- The positive check is cached at most 5 minutes; "device revoked" and "device released" from the event feed end the device's sessions.
- **Owner recovery (owner decision D-O13, 2026-10-18): through Vixly support only.** The Owner holds the highest privilege in the tenant, so nothing in Operations can start their recovery: there is no self-service "I lost my device" page.
  - **Lost device:** the Owner calls Vixly support. Staff check the caller's identity, then start owner recovery in the Control console. Platform emails the link to the Owner contact; it opens `/devices/recover#token`, which Operations completes the same way as the first-device link.
  - **Lost second factor and recovery codes:** staff start a second-factor reset in the console, and a second staff member approves it. Platform emails a single-use link to the Owner contact; it opens `/account/second-factor-reset#token` on the tenant host, where Operations checks the link with Platform, removes the old authenticator and passkeys, and enrols a new one with new recovery codes. The Owner's password is unchanged and all their sessions end.
  - **Identity check by support:** call back the Owner contact phone on file (never a number given during the call), confirm details only the tenant knows, and record the reason in the console. Both actions need `platform.licensing.manage` and step-up.
  - **Notices:** the Owner and Billing contacts are emailed when a recovery or reset starts and when it completes.
  - **Links:** Owner contact email only, 60 minutes, single use, never shown on screen or returned by an API.
- **Administrators and other staff** recover inside the tenant: the Owner (or an Administrator, for staff) resets their second factor or approves their new device in Operations, with step-up. Vixly is not involved.

### Accounts and roles (sweep D-O5)

- Lifecycle Invited, Active, Suspended, Locked, Deactivated; every change needs a reason, the permission and step-up, and writes audit.
- The Owner cannot be suspended, locked, deactivated or demoted by anyone; ownership moves only when the Owner transfers it to an active Administrator with a second factor.
- Nobody acts on their own account through admin actions; there is always an active Owner.
- A role grant has a scope: the whole tenant, one branch, or one warehouse. Reads and writes are filtered by scope in repositories (fix 3); a two-warehouse matrix test proves it.
- A user may grant only roles whose permissions they hold themselves, and only within their own scope (a warehouse-scoped manager cannot grant tenant-wide roles); domain test.
- **Platform role when Operations calls Platform:** Owner → `owner`, Administrator → `admin`, anyone else → `staff`, with the step-up result. Platform limits company contacts (`PUT /tenant`), device approval, denial and release, and company changes to owner or admin (company changes: owner only), so those permissions belong only to the system roles and cannot be given to tenant roles.

### Subscription access levels

Platform's portal answer carries the access level. **Full:** normal work. **Read-only:** staff can sign in and view, every command is refused with a clear message. **Payment required:** only the Owner and Administrators can sign in, and they see the Platform billing page (invoices and pay link) only.

### Tenant setup

- **Company:** legal details come from Platform, shown read-only, changed only by request with proof (B12, step-up, Owner). Contacts, default language and time zone are edited here and saved to Platform (`PUT /tenant`).
- **Currency** (owner decision D-O7): one billing currency per tenant; it can change only while no invoice exists (ADR-0018 says the same).
- **Units, payment gate, delivery modes:** tenant settings, used from Slice 3 on (ADR-0018).
- **Branches and warehouses** (sweep D-O10): a branch is an office in a country, on the origin side, the destination side or both; warehouses belong to a branch and are marked receiving, dispatch hub and/or pickup point. A receiving warehouse's address becomes the customers' mailbox address in Slice 2.
- **Shipping services:** the tenant picks which of Vixly's shipping services it offers (`Contracts.Platform` catalogue) and sets each one's label, business days and volumetric divisor.
- **Numbering:** one sequence per tenant, kind and year; prefixes are settings; allocation is concurrency-safe.
- **Brand:** name, logos, colour preset (contrast checked, WCAG 2.2 AA), sender name, support contacts, legal footer; draft then publish. What a tenant may change depends on its plan's branding entitlement (W-4).
- **Setup checklist:** currency and units, first branch and receiving warehouse, shipping services, brand, first staff member.

## 4. Permissions (catalogue in code, `ops.<area>.<action>`)

`ops.staff.view`, `.invite`, `.edit`, `.lock`, `.suspend`, `.reactivate`, `.deactivate`, `.reset_second_factor`, `.end_sessions`; `ops.roles.view`, `.create`, `.edit`, `.edit_permissions`, `.grant`; `ops.devices.view`, `.approve`, `.release`; `ops.settings.view`, `.edit`; `ops.company.view`, `.edit_contacts`, `.request_change`; `ops.branches.view`, `.manage`; `ops.warehouses.view`, `.manage`; `ops.services.view`, `.manage`; `ops.brand.view`, `.edit`, `.publish`; `ops.audit.view`.

System roles: **Owner** (everything, protected) and **Administrator** (everything except ownership transfer and company change requests). `ops.company.edit_contacts`, `ops.company.request_change` and `ops.devices.approve` and `.release` are reserved to the system roles (section 3, Platform role). Later slices add their codes.

## 5. Use cases

| Area | Use cases |
|---|---|
| Owner setup | `InviteTenantOwner` (event handler), `AcceptOperationsInvitation` |
| Sign-in (anonymous) | `BeginOperationsSignIn`, `CompleteOperationsSignInWithAuthenticatorCode`, `CompleteOperationsSignInWithRecoveryCode`, `BeginOperationsPasskeySignIn`, `CompleteOperationsPasskeySignIn`, `RequestOperationsPasswordReset`, `ResetOperationsPassword` |
| Own account | `GetMyOperationsAccount`, `ChangeMyOperationsPassword`, passkey add and remove, authenticator enrol and confirm, `RegenerateMyRecoveryCodes`, `ListMyOperationsSessions`, `EndMyOperationsSession`, `ConfirmOperationsSecondFactor` (step-up), `SignOutOperations` |
| Staff | `ListOperationsUsers` (paged, scope-filtered), `GetOperationsUser`, `InviteOperationsUser`, `UpdateOperationsUser`, `ResendOperationsInvitation`, `SendOperationsPasswordReset`, lock, suspend, reactivate, deactivate, `ResetOperationsSecondFactor`, `EndOperationsUserSessions`, `TransferOperationsOwnership` |
| Roles | list, get, create, update, deactivate, reactivate, `UpdateOperationsRolePermissions`, `GrantOperationsRole`, `RevokeOperationsRoleGrant`, `ListOperationsPermissionHolders` |
| Devices | `ListOperationsDevices`, `JoinOperationsDevice` (a new device asks to join), `ApproveOperationsDevice`, `DenyOperationsDevice`, `ReleaseOperationsDevice`, `WithdrawOperationsDeviceRelease`, `RequestFirstDeviceLink`, `CompleteOperationsDeviceLink` (first device and owner recovery), `StartOwnerRecovery`, `CheckOperationsDevice` (on sign-in and every few minutes) — all through Platform |
| Setup | `GetOperationsSetupState`, `AcceptOperationsTerms` (through Platform), `GetOperationsSetupChecklist` |
| Settings and company | `GetOperationsTenantSettings`, `UpdateOperationsTenantSettings`, `SetOperationsBillingCurrency`, `GetOperationsCompany`, `UpdateOperationsCompanyContacts`, `SubmitOperationsCompanyChange`, `WithdrawOperationsCompanyChange` |
| Branches and warehouses | list, get, create, update, deactivate, reactivate for each |
| Shipping services | `ListOperationsShippingServices`, `UpdateOperationsShippingServiceSetup` |
| Brand | `GetOperationsBrand`, `UpdateOperationsBrandDraft`, `PublishOperationsBrand` |
| Audit | `ListOperationsAuditEntries` (paged, filters) |

## 6. Anonymous endpoints and rate limits (S-6)

| Endpoint | Policy |
|---|---|
| `POST /sign-in` | 10 per minute per IP; lockout after 5 failures for 15 minutes; antiforgery |
| `POST /sign-in/verify`, `/sign-in/recovery`, `/sign-in/enrol` | 20 per minute per IP; 5 wrong codes per pending sign-in |
| Passkey options and sign-in | 20 per minute per IP; single-use ceremonies, 5 minutes |
| `POST /forgot-password` | 5 per hour per IP, 1 per 15 minutes per email; same answer whether or not the email exists |
| `POST /reset-password`, `/invitation` | 10 per hour per IP |
| `POST /devices/first`, `/devices/recover`, `/account/second-factor-reset` (link completion) | 10 per hour per IP; token after `#`, posted once, single use (Platform Slice 7) |
| `POST /devices/check` | 60 per hour per device; needs the session cookie |

Sign-in pages are static and load `vixly-forms.js` (ADR-0019).

## 7. Screens (presenters and state for Frontend/UI)

Sign-in (password, passkey, code, recovery), invitation with enrolment, forgot and reset password, device join ("waiting for approval"), My account (security, sessions, language, theme), Staff list and staff page (profile, roles with scope, sessions, activity), Roles with the permission matrix, Devices (pending, active, release), Company (read-only legal details, change request, contacts, language, time zone), Settings (currency, units, payment gate, delivery modes, security), Branches and warehouses, Shipping services, Brand (draft, preview, publish), Setup checklist, Audit log. Frontend/UI designs them with the workbench primitives (ADR-0016).

## 8. Platform change needed

- **Event `platform.registration.account_setup_ready.v1`** on the event feed, published on every move to "pending account setup" (paid in `PlatformRegistrationPaidHandler`, and trials when the registration starts its trial): tenant id and registration id only. The `operations` caller may read it; Edge may not.
- **Portal lookup "setting up"** for the `operations` caller (Slice 0, section 5).
- **Caller entry** `operations` with scopes `tenant`, `portal`, `events` (configuration only).
- **Owner support actions (D-O13):** the console's "Start owner recovery" gains the support identity-check record; a new "Reset owner second factor" action with a second approver; its link kind and the internal call Operations uses to complete it; notices to the Owner and Billing contacts. The Operations-started owner recovery (`POST /tenant/devices/owner-recovery`) is withdrawn.
- Platform's staff-facing B12 flow is unchanged; Operations becomes the tenant-side client.

## 9. Customer side

No customer screens in this slice. The published brand (name, logos, colours, support contacts) and the tenant's currency and units are what Slice 2's customer pages show; `Contracts.Operations` gains `OperationsTenantBrandDto` and `OperationsTenantProfileDto` for Customer.Web.

## 10. Owner decisions (2026-10-17)

- **D-O2:** second factor required for the Owner and Administrators, optional for other staff, with a tenant setting to require it for everyone.
- **D-O6:** idle sign-out after 15 minutes, the same for every tenant; 12-hour absolute limit.
- **D-O7:** one billing currency per tenant; no per-customer currency.
- **ADR-0018:** the milestone list is approved as written.
- Proposed and not questioned: D-O1, D-O3 to D-O5, D-O8 to D-O12 (sweep section 6).

### Owner decisions (2026-10-18)

- **D-O13, Owner recovery through Vixly support only:** the Owner is the highest privilege in the tenant, so a lost device or a lost second factor is handled by Vixly support, never by a self-service page (section 3, Licensed devices). A second-factor reset needs a second staff member's approval.
- **D-O14, Administrators and staff recover inside the tenant:** the Owner resets an Administrator's second factor; the Owner or an Administrator resets other staff and approves their devices.
- **D-O15, Shipping services:** Vixly offers five shipping services: air express, air standard, ocean consolidated (LCL), ocean full container (FCL) and ground. Platform publishes the list in `Contracts.Platform`. Each tenant chooses the ones it offers, for example air and ocean only, or ground only, and sets the label, business days and volumetric divisor.
- **D-O16, Passkeys later:** passkeys come after the shipment slices, as a slice of their own. Until then, the authenticator app and recovery codes are the second factor.
- **D-O17, Branding by plan (W-4):** without `portal_theme_customization` the site shows the default colours; without `homepage_content_customization` it shows the default texts and no main image. Saved choices are kept.
- **D-O18, Staging:** `vixly.freedns.org` with wildcard DNS: `control.`, `api.`, `ops-internal.` and `{slug}.`.

## 11. Done when (in addition to the slice gate)

- A new tenant's owner, paid or trial, goes from the setup email to the setup checklist, and the tenant becomes Active, in a test run against Platform; setup pages work without a device and nothing else does.
- An Owner who lost every device recovers through Vixly support and `/devices/recover`; an Owner who lost their second factor recovers through a support reset approved by a second staff member. No Operations page can start either.
- An Owner or Administrator never gets a session with a password alone; other staff do only while the tenant has not required a second factor (tests).
- A session ends after 15 idle minutes and after 12 hours (tests with a fixed clock).
- No staff session without a passing device check after setup; a revoked or released device's sessions end; a rotated secret is never lost between requests (tests with a Platform fake).
- Read-only refuses every command; Payment required shows only the billing page (tests).
- A warehouse-scoped user cannot grant beyond their scope or permissions (domain test).
- Owner protection, self-action and last-Owner rules have domain tests.
- A user limited to warehouse A never reads or changes warehouse B's records (two-warehouse matrix test).
- The billing currency cannot change once an invoice exists (domain test, ready for Slice 9).
- Every command writes one audit entry in the same commit (D-2); every list is paged.
- en, fr and es complete; no "Vixly" in tenant-facing text.
