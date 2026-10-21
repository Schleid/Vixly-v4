# Slice 2: Tenants - design note

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Approved for build, 2026-10-14.** D-T1 to D-T8 agreed in the sweep; D-T9 to D-T14 settled with the owner (D-T11 changed: slugs are permanent once a tenant has been active); industry-standard additions D-T15 to D-T18 chosen by the owner |
| Inputs | [Sweep](SLICE-2-TENANTS-SWEEP.md), ADR-0003, ADR-0010 (H-2), ADR-0012 as amended, plane document sections 4 to 7 |
| Depends on | Slice 1 (signed 2026-10-14) |

## 1. Outcome

Staff see every tenant in a paged, searchable directory. They open a tenant to see and edit its company profile, correct its portal slug while it has never been active, and move it through its lifecycle (activate, suspend, reactivate, deactivate, ban) with a reason, step-up where it matters, and a history that reads in English, French or Spanish. Staff can create a tenant directly for sales-led onboarding. Like mature B2B consoles, each tenant has named contacts, a time zone, an account manager, tags and an internal notes timeline. Its contacts are told when its access changes, and every change is published reliably for the slices that react to it.

A company can register itself through the public API: it checks a portal slug, starts a registration, and verifies its email. The registration then waits at "choose a plan" until Slice 3. Every change is audited. Nothing about the tenant's brand, its users or its devices lives here.

## 2. Data (schema `platform`)

| Class | Table | Notes |
|---|---|---|
| `PlatformTenant` | `Tenants` | Aggregate root. `TenantCode` (immutable, unique), `Status` (D-T1), `DefaultLanguage`, `TimeZone` (IANA id, D-T15), `AccountManagerUserId` (optional, an active Platform user, D-T16), status timestamps, `CurrentSlug` (copy of the current slug row, for reads and Edge) |
| `PlatformTenantCompanyProfile` | `TenantCompanyProfiles` | One per tenant, owned by the aggregate. Legal name, trading name (optional), company email and phone, address (line 1 and 2, city, state or province, postal code, country as ISO 3166-1 alpha-2). No logo (Operations brand). Tax number arrives with invoices (Slice 4) |
| `PlatformTenantContact` | `TenantContacts` | Owned by the aggregate (D-T15). Role (Owner, Billing, Technical), name, email, optional phone. Owner is required; Billing and Technical fall back to Owner when empty. One contact per role |
| `PlatformTenantTag` | `TenantTags` | Owned by the aggregate (D-T16). Lowercase `a-z`, `0-9` and hyphen, 2 to 32 characters, at most 10 per tenant. Internal: never shown to the tenant |
| `PlatformTenantNote` | `TenantNotes` | Its own aggregate, so the tenant never loads its notes (D-T16). Body up to 4,000 characters, author, time. Append-only: a correction is a new note |
| `PlatformOutboxMessage` | `OutboxMessages` | Written in the same commit as the change (D-T17). Message type with version (`platform.tenant.status_changed.v1`), JSON payload, occurred, processed, attempts, last error code |
| `PlatformTenantSuspension` | `TenantSuspensions` | Owned by the aggregate (D-T2). Category, reason, suspended by and at, resolved by and at. At most one open |
| `PlatformTenantLifecycleEvent` | `TenantLifecycleEvents` | Append-only, owned by the aggregate. Event code plus arguments (D-4), from and to status, reason, category, actor, time. Saved in the same commit as the change (D-2) |
| `PlatformPortalSlug` | `PortalSlugs` | **One row per slug claimed (D-T10).** Slug (unique index), the tenant or registration that holds it, state (Pending or Permanent), held-until for a pending claim. The unique index is what stops two tenants getting one address. A permanent row is never deleted, so a slug is never reused |
| `PlatformTenantRegistration` | `TenantRegistrations` | Self-service sign-up. Status chain from the sweep (only the first two steps are reachable in this slice). Company details as typed, owner name, email, phone and language, time zone, slug, accepted terms and privacy versions with time and client IP (D-T18), tenant id once created, access-token hash, timestamps per step, cancelled by and reason |
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
- **The slug is the tenant's address for good (D-T11).**
  - Customers reach the company and its portal at `{slug}.<base domain>`, and passkeys are bound to that hostname (ADR-0012 W-6).
  - Changing it would break every link, bookmark and QR code customers hold.
- **The one exception is a correction before the tenant goes live.**
  - Staff may correct a slug only while the tenant has **never been activated** (no activation time recorded; status Draft or Pending activation).
  - It needs `platform.tenants.edit`, a reason and step-up.
  - The old slug's row is removed, since it was never public. The correction is a lifecycle event and an audit entry.
  - Once a tenant has been Active, even if it was later deactivated, its slug is fixed.
- **Slugs are never reused.**
  - A deactivated or banned tenant keeps its slug row.
  - Otherwise a stranger could take over an address the old company's customers still use.
- **A different address later** is a custom domain under the White Label entitlement (Operations, ADR-0012 W-5). The slug address keeps working beside it.
- **Availability:** a slug is free when it has no row, or its only row is a lapsed pending claim.

### Registration (D-T4, D-T9)

1. **Start** (anonymous) validates the details and claims the slug as Pending for 24 hours. The Public form shows the full address and asks the registrant to confirm it before submitting. It then emails a verification link and returns a registration access token.
   - Public.Web keeps that token in an HttpOnly cookie. Only its hash is stored. It lasts 7 days.
   - Nothing about other tenants or registrations is revealed: a taken slug gives the same error as the slug check, and an email address can register more than once.
2. **Verify email** (anonymous, token from the link) does the following in one commit:
   - creates the `PlatformTenant` in Draft, with its company profile;
   - makes the slug claim Permanent;
   - moves the registration to Pending plan selection.
3. **Correct email** (anonymous, access token) works only before verification. It sends a new link and voids the old one (V3's unused `UpdateTenantRegistrationEmail`).
4. **Resend** (anonymous with the access token, or staff) is limited to 3 per hour per registration.
5. **Status** (anonymous, access token) returns the next step only, never the stored details.
6. **Expiry:**
   - An unverified registration expires after 24 hours, which frees its slug.
   - A verified one expires after 30 days at "choose a plan". Its Draft tenant stays for staff to deactivate.
   - Expiry is checked when the registration is read until the Worker opens in Slice 4.
7. **Staff** can list registrations, open one, resend its email and cancel it with a reason. Cancelling releases a pending slug.

### Contacts, account manager, tags and notes (D-T15, D-T16)

- The registrant becomes the Owner contact when the email is verified. Staff edit the contacts with `platform.tenants.edit`.
- The account manager must be an active Platform user. Deactivating that user does not clear the field; the tenant list shows "inactive" beside the name so it can be reassigned.
- The list filters by tag and by account manager. Tags are typed by staff and are not translated, like custom role names.
- Notes need `platform.tenants.add_notes`. Anyone who can view the tenant can read them. They are never sent to the tenant or to another plane.

### Outbox and tenant notices (D-T17, D-T18)

- **Outbox:**
  - Every lifecycle move and slug correction adds one outbox message in the same commit as the change and its audit entry.
  - A dispatcher delivers the messages: at least once, in order per tenant, retried with back-off.
  - Each handler is idempotent, keyed by message id.
  - Until the Worker opens (Slice 4) the dispatcher runs as a hosted service in `Platform.Web`, claiming rows with `UPDLOCK, READPAST` so two instances never take the same message. It then moves to the Worker unchanged.
- **Tenant notices:** the first handler emails the tenant's Owner contact (and Billing, if different) when the tenant is suspended, reactivated, deactivated or banned.
  - It is written in the tenant's default language and is a Vixly email, because Control is Vixly's.
  - It states the new status, the suspension category and how to contact Vixly. It never includes the internal reason.
  - Slice 3 adds a second handler, to suspend or restore the subscription (D-T6).
- **Consent at registration:**
  - Start refuses unless the registrant accepts the current terms and privacy versions.
  - The versions come from configuration (`Platform:Legal:TermsVersion`, `PrivacyVersion`) until legal documents arrive in Slice 8.
  - The versions, time and client IP are kept on the registration and copied to the tenant's history when it is created.

### Other rules

- The legal name is required, up to 200 characters; the trading name is optional, up to 200.
- The email uses `EmailAddress` and the phone uses `PhoneNumber` from `Shared.Kernel`.
- The country must be a known ISO code.
- Default language is one of en, fr and es. It comes from the registration's language and staff can change it.
- The time zone must be a valid IANA id (for example `America/Toronto`). It comes from the registrant's browser, or `UTC`, and staff can change it.

## 4. Permissions

`platform.tenants.view`, `.create`, `.edit`, `.add_notes`, `.activate`, `.suspend`, `.reactivate`, `.deactivate`, `.ban`. The registrations screens use `view`; resend and cancel use `edit`. Contacts, time zone, tags and account manager use `edit`. The `administrator` system role gets all nine; `security_administrator` gets none. Ban also needs SuperUser authority, as change-authority does in Slice 1.

## 5. Use cases (three files each, gate first)

| Area | Use cases |
|---|---|
| Tenants (staff) | `ListPlatformTenants` (paged; search by name, code, slug, email or phone; filter by status, tag and account manager), `GetPlatformTenant`, `CreatePlatformTenant` (D-T8, Draft), `UpdatePlatformTenantProfile`, `CorrectPlatformTenantSlug` (never-activated tenants only), `ActivatePlatformTenant`, `SuspendPlatformTenant`, `ReactivatePlatformTenant`, `DeactivatePlatformTenant`, `BanPlatformTenant`, `ListPlatformTenantLifecycleEvents` (paged), `UpdatePlatformTenantContacts`, `UpdatePlatformTenantSettings` (default language, time zone), `AssignPlatformTenantAccountManager`, `SetPlatformTenantTags`, `AddPlatformTenantNote`, `ListPlatformTenantNotes` (paged) |
| Registrations (staff) | `ListPlatformTenantRegistrations` (paged, status filter), `GetPlatformTenantRegistration`, `ResendRegistrationVerification`, `CancelPlatformTenantRegistration` |
| Registration (anonymous) | `CheckPortalSlugAvailability`, `StartPlatformTenantRegistration`, `VerifyRegistrationEmail`, `CorrectRegistrationEmail`, `ResendMyRegistrationVerification`, `GetMyRegistrationStatus` |

Merged from V3: `GetTenants`, `SearchTenantByPhone` → `ListPlatformTenants`; `ManageTenantLifecycle` → five use cases. Not ported: `RegisterTenant` (dead). Moved out of the slice: see the sweep, section 2.

**Error codes** (prefix `platform.tenants.` and `platform.registrations.`): `not_found`, `legal_name_required`, `legal_name_too_long`, `trading_name_too_long`, `country_invalid`, `address_required`, `language_not_supported`, `slug_invalid`, `slug_reserved`, `slug_taken`, `slug_unchanged`, `time_zone_invalid`, `owner_contact_required`, `contact_role_duplicate`, `tag_invalid`, `too_many_tags`, `note_required`, `note_too_long`, `account_manager_invalid`, `terms_not_accepted`, `slug_fixed` (the tenant has been active), `move_not_allowed` (with `from` and `to`), `already_banned`, `suspension_category_invalid`, `reason_required`, `reason_too_long`, `ban_needs_superuser`; `registrations.not_found`, `registrations.link_invalid` (expired, used or unknown: one code), `registrations.already_verified`, `registrations.expired`, `registrations.cancelled`, `registrations.resend_too_soon`. Each code has en, fr and es text.

**Audit codes:** `platform.tenants.created`, `.profile_updated`, `.slug_corrected`, `.contacts_updated`, `.settings_updated`, `.account_manager_assigned`, `.tags_changed`, `.note_added`, `.activated`, `.suspended`, `.reactivated`, `.deactivated`, `.banned`; `platform.registrations.started`, `.email_verified`, `.email_corrected`, `.verification_resent`, `.cancelled`. Anonymous steps are audited with no actor and the client IP, as Slice 1 does for sign-in.

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
  - Summary (code, status, slug and address, default language, time zone, account manager, tags);
  - Contacts (owner, billing, technical);
  - Notes (timeline, add a note);
  - Company profile (edit);
  - Portal slug (shown; "Correct" only while never activated, with a reason and step-up);
  - Lifecycle panel (only the moves allowed from the current status; category, reason, step-up);
  - History (paged events in the reader's language);
  - Audit link.
- **Registrations:** list with status filter; registration page with steps and times, resend and cancel.

The same patterns as Slice 1 apply (see the FE report). Subscriptions, invoices and licences join the tenant page in their own slices.

## 8. Done when (in addition to the slice gate)

- Every lifecycle move:
  - is tested from every status, allowed and refused;
  - writes one event and one audit entry in the same commit.
- A slug can be corrected only while the tenant has never been active. A test covers a tenant that was active and then deactivated, and the correction is refused.
- A slug is never reused, including after deactivation or ban.
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
- The outbox:
  - A lifecycle move that fails to commit leaves no outbox message.
  - A processed message is never handled again; delivery is at least once, so a crash between sending and marking can repeat one notice (accepted).
  - Two dispatchers never claim one message (database test).
- A notice never contains the internal reason. The test checks the rendered email in all three languages.
- Registration without accepted terms is refused, and the accepted versions are kept.
- en, fr and es resources are complete, history included.

## 9. Owner decisions

| # | Question | Decision (2026-10-14) |
|---|---|---|
| D-T9 | When is the tenant created during self-registration? | When the email is verified, not at start (V3 created the tenant, its profile and an owner user at start). Unverified sign-ups leave no tenant behind |
| D-T10 | How is slug uniqueness kept across tenants and registrations? | One `PortalSlugs` table with a unique index on the slug; permanent rows are never deleted, so slugs are never reused |
| D-T11 | May a slug change? | **Owner, 2026-10-14: no, once the tenant has been active.** Customers would lose the company's address and every passkey would need re-enrolling. Staff may correct it only while the tenant has never been activated (reason, step-up). An incomplete registration is cancelled, not deleted, so the company can start again. A new address later is a custom domain (Operations) |
| D-T12 | Bot protection on public registration | Cloudflare Turnstile on the Public form, checked by `Platform.Api` before any work. Needs a Turnstile site key and secret in configuration. Without it, rate limits only |
| D-T13 | Tenant code format for new tenants | `TEN-` + 6 Crockford base-32 characters (about 1 billion codes; V3's 4 hex gave 65,536). Existing codes kept |
| D-T14 | Tenant activity (counts pulled from Operations) | Deferred until Operations exposes its endpoint (Stage 3); the plane table is updated |
| D-T15 | Contacts and time zone | **Added (owner).** Owner, Billing and Technical contacts replace the single company email for notices and invoices; IANA time zone on the tenant |
| D-T16 | Account manager, tags and internal notes | **Added (owner).** Notes need the new `platform.tenants.add_notes` permission |
| D-T17 | Reliable events | **Added (owner).** Transactional outbox; dispatcher in `Platform.Web` until the Worker (Slice 4) |
| D-T18 | Tenant notices and registration consent | **Added (owner).** Status-change emails to contacts without the internal reason; accepted terms and privacy versions recorded at registration |

## 10. Progress

| Step | State |
|---|---|
| Design note | Approved for build, 2026-10-14 |
| Domain: tenant aggregate, lifecycle, slugs, contacts, tags, notes, registration, outbox | Done 2026-10-14: 28 files, 102 new domain tests; all 2,051 checks green, 0 warnings |
| Application: use cases, queries, errors, audit codes | Done 2026-10-14: 28 use cases, outbox writer, dispatcher and notice handler; 33 new tests |
| Infrastructure: EF configurations, migration, repositories, queries, mailers, Turnstile | Done 2026-10-14: migration `Tenants` (10 tables), global SQL script rebuilt; 5 database tests against SQL Server |
| Api: host, endpoints, rate limits, CORS, tests | Done 2026-10-14: `Vixly.Platform.Api` opened; 8 HTTP tests |
| Web: tenant and registration screens | Done 2026-10-14: tenant directory, new tenant, tenant page (8 sections), registrations list and page; outbox worker |
| Resources: en, fr, es | Done 2026-10-14: errors, screens, history, emails, 249 country names (CLDR); fr and es `needs-review` |
| Verification record | Prepared 2026-10-14: [SLICE-2-VERIFICATION.md](SLICE-2-VERIFICATION.md), awaiting the owner |

### Decisions taken while building (2026-10-14)

- **Reactivating a tenant that was never active** returns it to Draft, not Active. Reactivation never skips activation.
- **The verification deadline follows the latest link:** 24 hours after it was sent, and never beyond the access token. The pending slug hold is extended to match.
- **The tenant does not load its history.** Each move returns its event, and the use case saves it together with the audit entry and the outbox message.
- **Tenant codes take their randomness from the caller.** The domain stays free of crypto services.

### Decisions taken while building (2026-10-14, continued)

- **Verifying the email moves the access token to whoever holds the email link.** The token the registration started with stops working, because owning the email is the stronger proof.
- **A unique-index violation is a refusal, not a crash.** The unit of work returns false (as for a concurrency conflict). Creating a tenant or a registration then reports `slug_taken`.
- **History and notes are append-only in the database too.** The global SQL script denies UPDATE and DELETE on `TenantLifecycleEvents` and `TenantNotes` to the app login, as for `AuditEntries`.
- **The outbox uses leases.** A dispatcher marks a batch with `LeasedUntilUtc` in one `UPDATE ... OUTPUT` with `UPDLOCK, READPAST`, then handles each message and saves it on its own. No transaction stays open while mail is sent.
- **The bot check fails closed.** Turnstile switched on with no secret refuses every start. Development turns the check off in `appsettings.Development.json`.
- **Country names are resources.** The 249 ISO codes have en, fr and es names generated from CLDR, so names never depend on the server's operating system.
- **Glossary.** French and Spanish screens use the glossary term for tenant (*locataire*, *inquilino*). Tenant-facing emails avoid the word and say "your company" instead.
- **Platform.Api has no cookies and no sessions.** It keeps its own Data Protection application name. Public.Web (Stage 2) will call it from its server.

### Owner correction at sign-off (2026-10-14)

- **D-T8 revised: tenant data comes from registration, not entered by staff.** The console's "New tenant" form and `CreatePlatformTenant` stay for now. A later slice replaces them, so that every tenant starts as a registration. Until then, use the form only for testing.
- **Demo data.** `dotnet run -- --seed-demo-tenants` (Development only) fills the database through the production path, registration then verified email then tenant. It adds 22 tenants across 20 countries in en, fr and es, in every reachable status, with history, contacts, tags, account managers and notes, plus 5 open or closed registrations. Every record is tagged `demo` and uses `.test` emails. A second run adds nothing.

### Owner decisions after sign-off (2026-10-14)

- **Staff invite, the company registers (D-T8, final).** The console keeps an invitation: staff send a link, and the company completes the normal registration itself. Staff never type a tenant's company data. `CreatePlatformTenant` and the "New tenant" form are replaced by "Invite a company" in a later slice.
- **The tenant maintains its own profile in Operations**, under Company profile (contacts, settings, brand). The Platform console shows it read-only.
- **Company legal data is immutable** once the tenant exists. The console's company profile editor (`UpdatePlatformTenantProfile`) becomes read-only.
- **The portal slug is fixed once the tenant has been active (D-T11, unchanged).** Staff may still correct it while the tenant has never been activated, with `platform.tenants.edit`, a reason and step-up. After first activation it never changes.
- **Still staff-owned:** lifecycle moves, account manager, tags and internal notes (D-T16).
- **Built 2026-10-16 (invite step):** `CreatePlatformTenant` and the "New tenant" form are removed. The invitation is a Sales offer link (`/register?offer=…`): the company registers itself on the public site. Platform.Api refuses a registration start without a valid offer link for that owner email while `PlatformTenants:RegistrationInviteOnly` is true (the default until public sign-up opens, D-U4); the public site shows the sign-up form only to a visitor who has such a link. The read-only company profile in the console is still to do.
- **Built 2026-10-16 (B12, owner chose "Legal data now" and "Request and approve"):** see "Company change requests" below.

### Company change requests (B12, built 2026-10-16)

Company legal data (legal name, trading name, company email and phone, postal address) is read-only in the console. Contacts, language and time zone stay editable by staff until Operations ships its company profile screen.

| Step | Who | What happens |
|---|---|---|
| Ask | Tenant owner, in Operations, with a fresh second factor | `POST /internal/v1/tenant/company-change` with the new details, a reason and one proof file (PDF, JPEG or PNG, found by its first bytes, up to 10 MB). The tenant keeps its current details. One pending request per tenant, enforced by a filtered unique index. Audited `platform.tenants.company_change_submitted`; staff holding `platform.tenants.review_changes` get an email with a link to the request |
| Withdraw | Tenant owner | `DELETE /internal/v1/tenant/company-change` closes a pending request. Audited |
| Follow | Tenant owner or admin | `GET /internal/v1/tenant/company-change` returns the latest request, or 204 when there is none |
| Review | Staff with `platform.tenants.view` | Console, Company changes (`/tenants/changes`, pending first, filter by status or tenant). The request page shows current and requested details side by side, the reason and the proof download (`no-store`, `nosniff`) |
| Decide | Staff with `platform.tenants.review_changes` (new; Administrator role) and step-up | Approve (optional note) replaces the tenant's legal details and writes tenant history `company_changed`; reject needs a note. Both are audited with before and after, and the tenant owner contact gets an email |

- **Invoices keep the details they were issued with.** Only new invoices show the approved details.
- **No other path changes legal data.** `UpdatePlatformTenantProfile` and the domain's `UpdateCompany` are removed; the only writer is an approved request.
- **Tables:** `TenantChangeRequests` (previous and proposed details as columns, status, reason, decider, note, row version) and `TenantChangeProofs` (file bytes and SHA-256), migration `TenantChanges`.
- **Later:** KYB checks on the proof, from the backlog.

### Tenant page: sections the owner asked for (2026-10-14)

The tenant page grows one section per slice that owns the data. Each section is read-only for staff, paged where it is a list, and shows a plain empty state until its slice lands.

| Section | Shows | Fed by |
|---|---|---|
| Subscription | Current plan, term, renewal date, status, add-ons | Slice 3 (Subscriptions) |
| Finance | Invoices (paged), total invoiced, paid and due, overdue amount, last payment | Slice 4 (Billing), Slice 5 (Payment collection) |
| Licensing | Licenses purchased, allocated and free; list of licenses with device, status and allocation date (paged) | Slice 7 (Licensing) |
| Activity | Sign-ins by the tenant's staff: person, time (tenant time zone), device type, country; paged, newest first | Operations through `Contracts.Platform` (Stage 3, extends D-T14) |

Activity privacy, **owner decision 2026-10-14**:

- Location is the **country only**, derived from the IP address; no city, and the console never shows the raw IP.
- Sign-in events are kept **90 days**, then only daily counts remain.
- Reading a tenant's activity needs its own permission, `platform.tenants.view_activity`, and every read is audited.
- The tenant's privacy notice says Vixly staff can see sign-in activity (person, time, device type, country) for support and security.
