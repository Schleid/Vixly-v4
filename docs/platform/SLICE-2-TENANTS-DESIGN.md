# Slice 2: Tenants - design note

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Draft for owner review, 2026-10-14.** Decisions D-T1 to D-T8 agreed in the sweep; D-T9 to D-T14 below are proposals |
| Inputs | [Sweep](SLICE-2-TENANTS-SWEEP.md), ADR-0003, ADR-0010 (H-2), ADR-0012 as amended, plane document sections 4 to 7 |
| Depends on | Slice 1 (signed 2026-10-14) |

## 1. Outcome

Staff see every tenant in a paged, searchable directory. They open a tenant to see and edit its company profile and portal slug, and move it through its lifecycle (activate, suspend, reactivate, deactivate, ban) with a reason, step-up where it matters, and a history that reads in English, French or Spanish. Staff can create a tenant directly for sales-led onboarding.

A company can register itself through the public API: it checks a portal slug, starts a registration, and verifies its email. The registration then waits at "choose a plan" until Slice 3. Every change is audited. Nothing about the tenant's brand, its users or its devices lives here.

## 2. Data (schema `platform`)

| Class | Table | Notes |
|---|---|---|
| `PlatformTenant` | `Tenants` | Aggregate root. `TenantCode` (immutable, unique), `Status` (D-T1), `DefaultLanguage`, status timestamps, `CurrentSlug` (copy of the current slug row, for reads and Edge) |
| `PlatformTenantCompanyProfile` | `TenantCompanyProfiles` | One per tenant, owned by the aggregate. Legal name, trading name (optional), company email and phone, address (line 1 and 2, city, state or province, postal code, country as ISO 3166-1 alpha-2). No logo (Operations brand). Tax number arrives with invoices (Slice 4) |
| `PlatformTenantSuspension` | `TenantSuspensions` | Owned by the aggregate (D-T2). Category, reason, suspended by and at, resolved by and at. At most one open |
| `PlatformTenantLifecycleEvent` | `TenantLifecycleEvents` | Append-only, owned by the aggregate. Event code plus arguments (D-4), from and to status, reason, category, actor, time. Saved in the same commit as the change (D-2) |
| `PlatformPortalSlug` | `PortalSlugs` | **One row per slug ever used or claimed (D-T10).** Slug (unique index), tenant or registration that holds it, state (Pending, Current, Retired), held-until. The unique index is what stops two tenants getting one address |
| `PlatformTenantRegistration` | `TenantRegistrations` | Self-service sign-up. Status chain from the sweep (only the first two steps are reachable in this slice). Company details as typed, owner name, email, phone and language, slug, tenant id once created, access-token hash, timestamps per step, cancelled by and reason |
| `PlatformRegistrationToken` | `RegistrationTokens` | Email-verification tokens: hash only (V3 stored the token in clear), 24-hour expiry, used time |

Every Id is assigned in code (D-5). One migration adds these tables.

**Not in this slice:** `PlatformTenantActivityDaily` and `PlatformTenantActivityLatest` (D-T14). The tenant brand and custom domains (Operations). The tenant owner's account (Operations, ADR-0003).

## 3. Rules

### Lifecycle (D-T1)

| Move | From | To | Permission | Needs |
|---|---|---|---|---|
| Activate | Draft, Pending activation | Active | `platform.tenants.activate` | |
| Suspend | Active | Suspended | `platform.tenants.suspend` | Category, reason, step-up |
| Reactivate | Suspended, Deactivated | Active | `platform.tenants.reactivate` | Reason, step-up. Closes the open suspension |
| Deactivate | Draft, Pending activation, Active, Suspended | Deactivated | `platform.tenants.deactivate` | Reason, step-up |
| Ban | any but Banned | Banned (terminal) | `platform.tenants.ban` and SuperUser authority | Reason, step-up |

Pending activation is entered by Slice 5 (payment completed). Nothing in this slice moves a tenant into it.

Each move writes one lifecycle event and one audit entry in the same commit. Slice 3 reacts to these events to suspend or restore the subscription (D-T6).

### Codes and slugs (D-T3)

- **Tenant code:**
  - Generated as `TEN-` plus 6 Crockford base-32 characters (D-T13). Uniqueness is retried on collision.
  - It never changes. V3 codes (`TEN-` plus 4 hex) are kept when tenants migrate.
- **Portal slug format:** 3 to 63 characters; lowercase letters, digits and inner hyphens; no leading, trailing or double hyphen. This is a DNS label.
- **Reserved slugs:** H-2 is the floor. The list is configuration (`Platform:Tenants:ReservedSlugs`) added to the floor, never replacing it.
- **Changing a slug:** staff only, with `platform.tenants.edit`, a reason and step-up (D-T11).
  - The old slug becomes Retired and is held for 90 days.
  - The console warns before saving. The tenant's users must enrol their passkeys again, because a passkey is bound to the hostname (ADR-0012 W-6).
- **Availability:** a slug is free when it has no row, or its only row is a lapsed hold or a lapsed pending claim.

### Registration (D-T4, D-T9)

1. **Start** (anonymous) validates the details and claims the slug as Pending for 24 hours. It then emails a verification link and returns a registration access token.
   - Public.Web keeps that token in an HttpOnly cookie. Only its hash is stored. It lasts 7 days.
   - Nothing about other tenants or registrations is revealed: a taken slug gives the same error as the slug check, and an email address can register more than once.
2. **Verify email** (anonymous, token from the link) does the following in one commit:
   - creates the `PlatformTenant` in Draft, with its company profile;
   - turns the slug claim Current;
   - moves the registration to Pending plan selection.
3. **Correct email** (anonymous, access token) works only before verification. It sends a new link and voids the old one (V3's unused `UpdateTenantRegistrationEmail`).
4. **Resend** (anonymous with the access token, or staff) is limited to 3 per hour per registration.
5. **Status** (anonymous, access token) returns the next step only, never the stored details.
6. **Expiry:**
   - An unverified registration expires after 24 hours, which frees its slug.
   - A verified one expires after 30 days at "choose a plan". Its Draft tenant stays for staff to deactivate.
   - Expiry is checked when the registration is read until the Worker opens in Slice 4.
7. **Staff** can list registrations, open one, resend its email and cancel it with a reason. Cancelling releases a pending slug.

### Other rules

- The legal name is required, up to 200 characters; the trading name is optional, up to 200.
- The email uses `EmailAddress` and the phone uses `PhoneNumber` from `Shared.Kernel`.
- The country must be a known ISO code.
- Default language is one of en, fr and es. It comes from the registration's language and staff can change it.

## 4. Permissions

`platform.tenants.view`, `.create`, `.edit`, `.activate`, `.suspend`, `.reactivate`, `.deactivate`, `.ban`. The registrations screens use `view`; resend and cancel use `edit`. The `administrator` system role gets all eight; `security_administrator` gets none. Ban also needs SuperUser authority, as change-authority does in Slice 1.

## 5. Use cases (three files each, gate first)

| Area | Use cases |
|---|---|
| Tenants (staff) | `ListPlatformTenants` (paged; search by name, code, slug, email or phone; status filter), `GetPlatformTenant`, `CreatePlatformTenant` (D-T8, Draft), `UpdatePlatformTenantProfile`, `ChangePlatformTenantSlug`, `ActivatePlatformTenant`, `SuspendPlatformTenant`, `ReactivatePlatformTenant`, `DeactivatePlatformTenant`, `BanPlatformTenant`, `ListPlatformTenantLifecycleEvents` (paged) |
| Registrations (staff) | `ListPlatformTenantRegistrations` (paged, status filter), `GetPlatformTenantRegistration`, `ResendRegistrationVerification`, `CancelPlatformTenantRegistration` |
| Registration (anonymous) | `CheckPortalSlugAvailability`, `StartPlatformTenantRegistration`, `VerifyRegistrationEmail`, `CorrectRegistrationEmail`, `ResendMyRegistrationVerification`, `GetMyRegistrationStatus` |

Merged from V3: `GetTenants`, `SearchTenantByPhone` → `ListPlatformTenants`; `ManageTenantLifecycle` → five use cases. Not ported: `RegisterTenant` (dead). Moved out of the slice: see the sweep, section 2.

**Error codes** (prefix `platform.tenants.` and `platform.registrations.`): `not_found`, `legal_name_required`, `legal_name_too_long`, `trading_name_too_long`, `country_invalid`, `address_required`, `language_not_supported`, `slug_invalid`, `slug_reserved`, `slug_taken`, `slug_unchanged`, `move_not_allowed` (with `from` and `to`), `already_banned`, `suspension_category_invalid`, `reason_required`, `reason_too_long`, `ban_needs_superuser`; `registrations.not_found`, `registrations.link_invalid` (expired, used or unknown: one code), `registrations.already_verified`, `registrations.expired`, `registrations.cancelled`, `registrations.resend_too_soon`. Each code has en, fr and es text.

**Audit codes:** `platform.tenants.created`, `.profile_updated`, `.slug_changed`, `.activated`, `.suspended`, `.reactivated`, `.deactivated`, `.banned`; `platform.registrations.started`, `.email_verified`, `.email_corrected`, `.verification_resent`, `.cancelled`. Anonymous steps are audited with no actor and the client IP, as Slice 1 does for sign-in.

## 6. Platform.Api opens (anonymous endpoints, S-6)

`Vixly.Platform.Api` is created in this slice:
- It is hosted at `vixly-api.techcrafters.ltd` (ADR-0010) and built on `Shared.Hosting` (headers, health, forwarded IP).
- CORS allows only the Public site's origin. Internal endpoints for Edge and Operations (JWT, `Contracts.Platform`) come in Slice 9.
- No session cookies: the registration access token is a bearer header that Public.Web sends server-to-server.

| Endpoint | Policy |
|---|---|
| `GET /registration/slugs/{slug}` | 30 per minute per client IP. Answers available, or the reason code only |
| `POST /registration` | 5 per hour per client IP; bot check (D-T12) |
| `POST /registration/verify` | 10 per hour per client IP; single-use token, 24 hours |
| `POST /registration/email`, `POST /registration/resend` | 10 per hour per client IP and 3 per hour per registration; access token required |
| `GET /registration/status` | 60 per hour per client IP; access token required |

Request bodies are capped at 16 KB. Responses carry error codes plus resolved text in the caller's `Accept-Language` (plane section 7, Slice 9 rule applied early).

## 7. Console screens (Frontend/UI handoff)

- **Tenants:** list with search, status filter and pager.
- **New tenant:** form.
- **Tenant page:**
  - Summary (code, status, slug and address, default language);
  - Company profile (edit);
  - Portal slug (change, with the passkey warning);
  - Lifecycle panel (only the moves allowed from the current status; category, reason, step-up);
  - History (paged events in the reader's language);
  - Audit link.
- **Registrations:** list with status filter; registration page with steps and times, resend and cancel.

The same patterns as Slice 1 apply (see the FE report). Subscriptions, invoices and licences join the tenant page in their own slices.

## 8. Done when (in addition to the slice gate)

- Every lifecycle move:
  - is tested from every status, allowed and refused;
  - writes one event and one audit entry in the same commit.
- A slug can never belong to two tenants:
  - a domain test;
  - a database test on the unique index;
  - two registrations racing for one slug, where one wins.
- Reserved slugs are refused, H-2's floor included, whatever the configuration says.
- Registration:
  - A verification token works once and never after 24 hours.
  - Only the token's hash is stored.
  - The status endpoint reveals nothing but the next step.
- Rate-limit and body-size tests pass on `Platform.Api`, and its headers test passes.
- Every tenant query is paged (P-1). The list does not load profiles it does not show.
- en, fr and es resources are complete, history included.

## 9. Owner decisions (proposed)

| # | Question | Proposal |
|---|---|---|
| D-T9 | When is the tenant created during self-registration? | When the email is verified, not at start (V3 created the tenant, its profile and an owner user at start). Unverified sign-ups leave no tenant behind |
| D-T10 | How is slug uniqueness kept across tenants, registrations and retired slugs? | One `PortalSlugs` table with a unique index on the slug |
| D-T11 | Slug change and passkeys | Allowed for staff with reason and step-up; the console warns that the tenant's users must re-enrol passkeys (W-6) |
| D-T12 | Bot protection on public registration | Cloudflare Turnstile on the Public form, checked by `Platform.Api` before any work. Needs a Turnstile site key and secret in configuration. Without it, rate limits only |
| D-T13 | Tenant code format for new tenants | `TEN-` + 6 Crockford base-32 characters (about 1 billion codes; V3's 4 hex gave 65,536). Existing codes kept |
| D-T14 | Tenant activity (counts pulled from Operations) | Deferred until Operations exposes its endpoint (Stage 3); the plane table is updated |

## 10. Progress

| Step | State |
|---|---|
| Design note | Draft, 2026-10-14 |
| Domain: tenant aggregate, lifecycle, slugs, registration | Not started |
| Application: use cases, queries, errors, audit codes | Not started |
| Infrastructure: EF configurations, migration, repositories, queries, registration mailer | Not started |
| Api: host, endpoints, rate limits, CORS, tests | Not started |
| Web: tenant and registration screens | Not started |
| Resources: en, fr, es | Not started |
| Verification record | Not started |
