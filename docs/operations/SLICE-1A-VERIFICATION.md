# Slice 1A: Tenant landing and staff sign-in - verification record

| | |
|---|---|
| Plane | Operations (Stage 3), with one Platform change |
| Date | 2026-10-18 |
| Built on | Slice 0 as delivered, and the dev team's tree with the public-site illustrations; not committed here: the dev team owns git |
| Design | [Slice 1 design](SLICE-1-STAFF-AND-SETUP-DESIGN.md) (part 1A of section 1a), [Slice 0 verification](SLICE-0-VERIFICATION.md) |
| Status | **Built and verified by automated tests; waiting for owner sign-off.** Parts 1B and 1C follow |

## 1. What was built

- **Landing page:** `{slug}.techcrafters.ltd/` shows the tenant's name and the staff sign-in form, in the tenant's language, with language links. Signed in, the same address shows the start page with the staff member's name and a sign-out button. No page says "Vixly".
- **Owner invitation (D-O3):** Platform publishes `platform.registration.account_setup_ready.v1` (ids only) when a registration is paid, or at once for a trial or a plan with nothing due. Operations reads it from the feed, writes an outbox message, and in the tenant's scope reads the owner contact and company names from Platform (`GET /tenant`), creates the Owner (Invited) and the brand name, and emails a 72-hour link to `{slug}.techcrafters.ltd/invitation`. A replayed event invites nobody twice.
- **Sign-in:**
  - The flow: password, then the authenticator app code, or a recovery code.
  - An Owner with no authenticator app enrols one first: QR code and manual key, issued under the tenant's name, then ten recovery codes shown once.
  - An Owner never gets a session with a password alone (D-O2).
  - A wrong email and a wrong password look the same and take the same time.
  - Five wrong passwords lock the account for 15 minutes. A pending sign-in lasts 5 minutes and allows 5 wrong codes, and each code works only once.
- **Sessions (D-O6):** server records with a hashed token in the `__Host-vx4-ops` cookie. They end after 15 idle minutes or 12 hours. A warning appears at 13 minutes with a "Stay signed in" button, and the page leaves at 15. Sign-out ends the session. A session is bound to its tenant, so a cookie presented on another tenant's host finds nothing. Sessions started while the tenant is being set up are marked as setup sessions.
- **Tenant being set up:** only `/setup`, `/invitation`, `/sign-in/*`, `/sign-out`, `/culture` and `/session` answer; everything else goes to `/setup`. Signed in, `/setup` lists the steps that remain: terms and the first computer (part 1B).
- **Audit:** `ops.AuditEntries`, append-only (the app login cannot update or delete). Sign-in, failures, refusals, invitation and sign-out are recorded in the same commit.
- **Security code:** Operations' own BCrypt (cost 12, upgraded on sign-in), TOTP, password policy and SHA-256 token hashing (ADR-0020 rejected).
- **Limits:** sign-in 10 per minute, second factor 20 per minute, invitation 10 per hour, per address. Every form has antiforgery and the form guard (ADR-0019).
- **Data:** migration `StaffSignIn` adds the tables `Users`, `UserAccounts`, `UserAuthenticatorApps`, `UserRecoveryCodes`, `UserSessions`, `SignInChallenges`, `AccountTokens`, `AuditEntries` and `TenantBrands`, each with `TenantId`:
  - email unique per tenant;
  - one Owner and one brand per tenant.
- **Texts:** 94 keys in `OperationsText` (en, fr, es), including the invitation email.

## 2. Automated checks

Release builds of the three solutions: no errors, no warnings.

| Test project | Slice 0 | Now |
|---|---|---|
| Operations.Domain.Tests | 11 | 29 |
| Operations.Application.Tests | 21 | 40 |
| Operations.Infrastructure.Tests (SQL Server) | 16 | 21 |
| Operations.Api.Tests | 14 | 14 |
| Operations.Web.Tests (SQL Server for sign-in) | 12 | 17 |
| Customer tests | 20 | 20 |
| Platform tests (Domain, Application, Infrastructure, Api, Web) | 657 | 657 |
| Public.Web, Shared.Kernel, Shared.UI | 113 | 113 |
| Architecture.Tests | 7,059 | 7,404 |

All 8,315 pass. The global SQL script runs twice on an empty server without errors. With the Operations password given, `vixly_operations_app` is refused `DELETE` on `ops.AuditEntries`.

What the new tests cover:
- **End to end over HTTP with a real database:**
  - invitation email;
  - password page with the tenant name;
  - authenticator enrolment with a real TOTP code;
  - ten recovery codes;
  - the setup page with the owner's name and the idle warning;
  - sign-out;
  - sign-in again with a lower-case recovery code;
  - landing name and language;
  - "Vixly" never shown;
  - wrong password message;
  - a post without antiforgery refused;
  - `/setup` closed to live tenants;
  - a session cookie refused on another tenant's host.
- **Use cases:** code required for an Owner; enrolment when none; identical answers for wrong email and wrong password; lockout and its end; one session per pending sign-in; five wrong codes; five-minute expiry; tenant name as the authenticator issuer; recovery codes once each; weak passwords refused; invitation single-use; setup sessions; 15-minute idle end; sign-out.
- **Owner invitation:** owner, brand, link and language from Platform; once only; a missing owner contact fails with a code; a mail failure saves nothing, so the retry works.
- **Domain:** account, lockout, authenticator time steps, recovery codes, session idle and absolute limits, touch once a minute, pending sign-in limits, link lifetime and purpose, token hash shape, audit codes, brand name.
- **Database:** user with account, app and codes round-trips; the same email in two tenants; a second Owner or brand refused; sessions, links and audit invisible to another tenant.
- **Platform:** the event is written when paid, and for a trial; the contract golden file gains the event.

## 3. Not covered here

- Part 1B and its device checks. Until then a session needs no licensed device. This is safe for now: only Owners exist, every Owner has a second factor, and no tenant can go live without the first device, which arrives in 1B.
- Password reset, passkeys, staff other than the Owner, roles and step-up: part 1B.
- A run against a live Platform: the HTTP tests stand Platform in. It belongs with part 1B's run, from a real registration to a live tenant.
- The final look of the sign-in pages: Frontend/UI.

## 4. Deployment notes

- **Platform.Api:** the `operations` caller needs the `tenant` scope for `GET /tenant`, as well as `portal` and `events`.
- **Operations.Web:**
  - `OperationsMail:FromAddress`, `SmtpHost`, `SmtpPort`, `UseSsl`, `UserName` and `Password` (secret). The sender name is the tenant's name.
  - `OperationsLinks:PortalUrlTemplate`, default `https://{slug}.techcrafters.ltd`.
  - Development writes emails to the `.mail` folder at the repository root.
- **Database:** run the newest global SQL script (migration `StaffSignIn`).
