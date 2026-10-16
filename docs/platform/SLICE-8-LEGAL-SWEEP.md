# Slice 8: Legal and settings - V3 behaviour sweep

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Sweep done, 2026-10-16.** Decisions in section 6 are proposals for the owner; answers go into the design note |
| Source | V3 `ControlPlane` (read-only): `Domain/Entities/Legal` (6 files), `Domain/Entities/Settings/Platform` (`PlatformSetting`, `PlatformMaintenanceSetting`), `Domain/Entities/Platform/PlatformCompanyProfile`, `Infrastructure/Persistence/Configurations/Legal` and `Settings/Platform`, `API/Controllers/Legal/InternalTermsController`, `API/Services/Legal/TermsAgreementService`, `API/Controllers/Public/TenantRegistrationController` (terms steps), `API/Controllers/Public/PlatformMaintenanceController`, `Application/UseCases/Platform/Maintenance`, `Application/UseCases/Settings`, `Application/Security` (permission codes and catalog), console `Pages/Settings/Terms/TermsWorkspace`, `Pages/Legal/TenantTermsRegister` and `TenantTermsDetail`, `Pages/Settings/PlatformMaintenance`, `Pages/SystemMaintenance`, `Pages/Settings/PlatformSettings`. Payment extract: settings repository, recurring billing job (maintenance check), two test files |
| Rule | Replicate behaviour, not code. Classification as in Slices 1 to 7: Active, Duplicate, Dead, Disconnected-wanted, Moved |
| Feeds | Slice 8 design note; Slice 2 (registration acceptance moves from configuration to published documents, language recorded, sales-led tenants); Slice 9 (contracts for Operations and Customer to read legal status, record acceptance and read maintenance status); Operations (owner prompt, acceptance screen, enforcement after the deadline, maintenance banner) |

## 1. How the evidence was gathered

Each legal entity, EF configuration, service method, controller action and console page was traced to what calls it. V3 has **no legal use case in Application**: all legal logic sits in one API service that uses the database context directly, and in three Razor pages that open the database context themselves. Maintenance has one Application use case. Billing settings, collections settings and the company profile were checked only to confirm what V4 already ported in Slices 4 to 6. The snapshot lacks the API legal DTOs, the database context and migrations, `Program.cs` (internal API key, rate limits, forwarded headers), the file storage service used for legal PDFs, the maintenance routing middleware and all Operations code. So "nobody calls it" means nothing in the snapshot does, and how Operations shows or enforces terms is known only from the fields Platform returns. V4 code under `src/Platform` was read to see what Slice 8 must add. Detailed notes with file and line evidence: `v3-legal-sweep.md`.

V3 treats legal acceptance as **one acceptance per tenant per Terms version**, given by the tenant owner. A Terms version is an uploaded PDF (URL plus SHA-256 hash) that "incorporates" exact versions of other policies (Privacy, DPA and so on), so one click covers them all. There is no text in the database and no language.

## 2. Use cases (none in Application; 6 service methods, 4 console actions, 3 maintenance actions)

| V3 use case | Called from | Status | V4 outcome |
|---|---|---|---|
| GetCurrentAsync (current Terms, accepted or not) | Anonymous `GET api/public/tenants/registrations/{id}/terms` | Active; registration id only; fails when nothing is published | `GetPlatformCurrentLegalDocuments(language)` for the registration page; status needs the registration access token (Slice 2) (D-Lg9) |
| AcceptOnboardingAsync | Anonymous `POST .../registrations/{id}/terms/accept` | Active; registration id only; owner looked up, not proven; check-then-insert | Acceptance taken at registration start, as V4 already does, but against published document versions and with the language (D-Lg9) |
| EnsureOnboardingAcceptedAsync | `select-plan` step | Active; **does nothing when no Terms are published**; only one step gated | Gone: V4 refuses registration start without consent; there is always a published version once Slice 8 ships (D-Lg9) |
| PublishAsync | Internal API `POST api/internal/legal/terms/publish` (Operations key) | Active; Duplicate of console publish; URL and hash from the caller, no staff identity | Gone. Publishing is a console action only (D-Lg3, D-Lg13) |
| GetTenantStatusAsync | Internal API, Operations, per tenant user | Active; returns can-accept (owner), days remaining, acceptance required, enforcement reached | `GetPlatformTenantLegalStatus` in `Contracts.Platform` (D-Lg10) |
| AcceptForTenantAsync | Internal API, Operations | Active; owner only; trusts ids in the URL; records the **Operations server** IP and user agent | `AcceptPlatformLegalTerms` from Operations with actor reference, role, language, client IP and user agent forwarded (D-Lg5, D-Lg11) |
| Console: publish Terms | `TermsWorkspace` (`/settings/terms` and `/legal/terms`) | Active; immediate publish, retires the previous version; uploads the file before the commit; incorporates all policies by default | Draft, then publish with step-up; files stored in the database; explicit choice of included documents (D-Lg2, D-Lg3) |
| Console: publish related policy | `TermsWorkspace` | Active; no confirmation; does not change what tenants accepted | Part of the same draft and publish flow (D-Lg1, D-Lg3, D-Lg4) |
| Console: approvals register | `/legal/tenant-approvals` | Active; every tenant, every acceptance of every version loaded in memory | Paged register per version, only tenants that must accept (D-Lg12) |
| Console: tenant record and send reminder | `/legal/tenant-approvals/{tenant}` | Active; only after the enforcement date; manual; English; email sent inline; no limit | Automatic reminders by outbox in the tenant's language, plus a manual one with a cooldown (D-Lg7) |
| PlatformMaintenanceUseCase.GetStatusAsync | Anonymous `GET api/platform/maintenance/status`, `/system/maintenance` page, routing middleware, recurring billing job | Active | Internal `GetPlatformMaintenanceStatus` for Edge, Operations and Customer; public page served by Edge (D-Lg14) |
| ScheduleAsync, CancelAsync | Maintenance workspace | Active; one row overwritten; security permission; no audit | `PlatformMaintenanceWindow` rows with history, settings permission, step-up, audit (D-Lg14) |
| Get/Update billing and collections settings | Platform settings workspace | Moved (V4 Slices 4 and 6) | Done |
| Company profile create, get, update | Company profile workspace | Moved (V4 Slice 4, D-B12); support email and phone not carried over | Support contact added to `PlatformCompanyProfile` (D-Lg15) |
| GetBillingSettings, SaveBillingSettings | Nobody | Dead (Slice 4 sweep) | Gone |

## 3. Entities and rules found

| V3 | Contents | V4 proposal |
|---|---|---|
| `PlatformTermsDocument` | Version, title, PDF URL, SHA-256, effective, acceptance deadline, enforcement dates, published, retired. No language, no publisher, no row version | `PlatformLegalDocument` of kind Terms: kind, version, status Draft, Published, Superseded; acceptance needed or notice only; publish, effective and deadline dates; published by; row version (D-Lg1, D-Lg3, D-Lg4) |
| `PlatformLegalDocument` | Related policy: type (Privacy, Acceptable Use, Cookie, SLA, DPA), version, title, URL, hash, effective, applicability, published, retired | Same entity, other kinds (D-Lg1) |
| `LegalDocumentApplicability` | All tenants, paid subscriptions, when applicable. A label only | Gone, or one rule if the owner wants it (D-Lg1) |
| `PlatformTermsIncorporatedDocument` | Which exact policy versions one Terms version includes (copied at publish) | `PlatformLegalDocumentPart`: the Terms version lists the exact versions it includes (D-Lg1) |
| (none) | No text per language | `PlatformLegalDocumentText`: one approved file per language (en, fr, es) per version, bytes stored in the database with SHA-256 made by Platform (D-Lg2) |
| `TenantTermsAcceptance` | Tenant, owner user id, registration id, Terms id, version, hash, URL, accepted time, IP, user agent, source, included-documents JSON. Unique per tenant and version. Never changed | `PlatformTenantTermsAcceptance`: tenant, document version, **language read**, hash, accepted time, actor (Operations user reference, display name, role) or registration, client IP and user agent as forwarded, source (Registration, Operations, Staff record). Append-only, unique per tenant and version (D-Lg5, D-Lg11) |
| `TermsAcceptanceReminder` | Tenant, Terms id, staff id, reason (20 to 1000 characters), delivery status as text, sent, failed, failure text | `PlatformTermsAcceptanceReminder`: tenant, version, kind (automatic step or manual), language, sent to, staff id and reason for manual ones; delivery through the outbox (D-Lg7) |
| `PlatformSetting` | One aggregate holding billing, collections and maintenance; singleton by convention only | No catch-all aggregate. V4 already keeps each settings record on its own (D-Lg15) |
| `PlatformMaintenanceSetting` | One window: start, end, emergency flag, message (10 to 500 characters), who scheduled, who cancelled. Overwritten each time | `PlatformMaintenanceWindow` rows: start, end, kind (planned, emergency), message per language, scope, status Scheduled, Active, Ended, Cancelled, who and when for each step (D-Lg14) |
| `PlatformCompanyProfile.SupportEmailAddress`, `SupportPhoneNumber` | Vixly support contact | Added to V4 `PlatformCompanyProfile` (D-Lg15) |

V3 values fixed in code: console default dates effective = tomorrow, deadline = +31 days, enforcement = +38 days; reminder reason 20 to 1000 characters; maintenance message 10 to 500 characters; emergency window 2 hours; PDF only, "maximum 10 MB" (text on the screen; the check is in the missing storage code).

## 4. Behaviour found

### Publishing

- No draft. Publishing is immediate and retires the current version in the same save. A published row is never edited. This part is right and is kept.
- The Terms version and each policy are published separately. A Terms release picks the exact policy versions it includes; by default the console ticks every published policy.
- A new policy version (for example a new Privacy Policy) does not ask tenants for anything. Tenants stay bound to the policy version listed in the Terms they accepted, while the console shows the new one as current.
- Three dates: effective, acceptance deadline, enforcement. Only the deadline (days remaining) and the enforcement date (status label, reminder gate) do anything.
- The internal API can also publish, with a URL and hash the caller chooses and no check.

### Acceptance

| Path | Who | Recorded |
|---|---|---|
| Registration | Whoever holds the registration id, after email verification; recorded as the owner | Version, hash, URL, included documents, connection IP and user agent, source "PublicOnboarding" |
| Operations | Operations says which user; Platform checks the user is the Owner in its own tenant-user table | Same, but IP and user agent are the Operations server's, source "OperationsPortal" |

- One acceptance per tenant per Terms version. Only the owner may accept. The language read is not recorded.
- At registration, only the plan step checks acceptance. When nothing is published the check is skipped, so a fresh environment registers tenants with no terms at all.
- Tenants created by staff, or before the first publish, have no acceptance and no path to one except a reminder after enforcement.

### Reminders and what happens without acceptance

- No automatic reminder and no notice at publish. Operations must ask for the status per user.
- Staff can send a reminder only after the enforcement date, one at a time, with a typed reason. English email with a link to the tenant's Operations portal, sent from the page; a failure stores the raw error.
- After the enforcement date the register calls the tenant "Denied by expiration". Platform takes no action: no suspension, no read-only, no portal check. `EnforcementReached` is returned to Operations; what Operations does with it is not in the snapshot.
- The Customer plane is not involved.

### Settings and maintenance

- Billing settings, collections settings and the company profile are already in V4. The only V3 settings left are **maintenance** and the **support contact** on the company profile. There is no default language, support URL or other platform-wide value in V3.
- Maintenance: staff with the security permission schedule one window or start a 2-hour emergency window. Billing's recurring job skips its cycle while it is active (collections does not). A middleware (not in the snapshot) sends console requests to a public maintenance page, except the session endpoints. A public anonymous endpoint tells others the status. One English message. No history, no audit, no notice to tenants before a planned window.

### Console screens found

| V3 page | State | V4 |
|---|---|---|
| Legal documents (`/settings/terms`, `/legal/terms`) | Tabs: current Terms, policy library, publish form (PDF upload, dates, policies to include, confirm box for Terms only) | Legal documents page: versions per kind, draft editor with one file per language, preview, publish with step-up (D-Lg2, D-Lg3) |
| Tenant Terms approvals (`/legal/tenant-approvals`) | Counts and table of every tenant: Approved, Pending, Denied by expiration; version picker; print | Paged register per version with filters; counts by query (D-Lg12) |
| Tenant Terms record | Evidence (IP, user agent, hash, included documents), reminder form after enforcement, reminder history with staff GUIDs; print | Tenant page Legal section: status, evidence, reminders with names, "send reminder", "record offline acceptance" (D-Lg7, D-Lg12) |
| Platform maintenance | Schedule, emergency, end or cancel; authority repair panel | Settings, Maintenance: windows list with history; repair panel already dropped in Slice 1 (D-Lg14) |
| Public `/system/maintenance` | English page with the message and end time | Served by Edge in the visitor's language (D-Lg14) |

## 5. Defects and risks seen in V3 (fixed by design in V4)

1. The internal API key (held by Operations) can publish the master Terms with any URL and any hash. Platform never checks the hash, and the console renders the URL as a link.
2. Registration acceptance and status need only the registration id. Anyone who learns it can accept for the owner or see whether they accepted.
3. Operations acceptance trusts the tenant and user ids in the URL and needs no permission.
4. The Operations path records the Operations server's IP and user agent as the owner's. The evidence of who accepted is weak where it matters most.
5. No language: one English PDF per version; the language read is not recorded. Emails and errors are English.
6. The acceptance gate is skipped while nothing is published, and only the plan step is gated.
7. Staff-created tenants and tenants older than the first version never get an acceptance path.
8. Reminders only after the enforcement date, manual only, unlimited, sent inline from the page. A crash can leave a reminder Pending for ever. Raw SMTP errors are stored and shown.
9. "Denied by expiration" has no effect in Platform. Whether Operations blocks anything is unknown, so the deadline may mean nothing.
10. New policy versions are never accepted unless new Terms are also published. The library shows them as current, which can mislead staff.
11. Legal logic outside Application: no use case, no authorization context in the service, no audit, database context used in pages.
12. Two publish paths (console and API) with different trimming rules; two routes to the same page.
13. File uploaded before the commit, so failures leave orphan public files. Only the browser filters for PDF; the version string goes raw into storage and may form a file path (storage code not in the snapshot).
14. Check-then-insert on acceptance: a double click hits the unique index and returns a server error.
15. Register loads every tenant and every acceptance of every version into memory; it includes never-activated, deactivated and banned tenants; no paging.
16. "Applicability" is shown but does nothing.
17. Maintenance: one row overwritten (no history), start can be in the past, a new window silently replaces an active one, "End" and "Cancel" are the same action, security permission instead of a settings one, no audit, no step-up, no advance notice, English only, collections not paused.
18. `PlatformSetting` singleton is not enforced in the database; the repository takes the oldest row.
19. Duplicate permission codes `PlatformSettings.Read` and `.View`.
20. Leftover compiled build folders (`.codex-bin`, `.codex-obj`) with developer machine paths sit in the source tree.
21. Tests: three domain tests on terms dates and two middleware tests; nothing on acceptance, reminders, publish or the register.

Kept from V3 because they are right: published versions are never edited; each version has a SHA-256 of the exact file; the Terms version names the exact versions of the documents it includes, so one acceptance covers a known set; acceptance rows are never changed; the acceptance checks the version and hash the user saw, so a version published in between is refused ("terms changed, review again"); only the owner accepts; maintenance status is public-safe and has no tenant data.

## 6. Decisions

Proposals for the owner; answers go into the Slice 8 design note. Mechanics that follow from V4 rules already in place (outbox, audit in the same commit, paging, error codes, en, fr and es resources, anonymous-endpoint register) are written into the recommendations and are not separate questions.

| # | Question | Options | Proposal |
|---|---|---|---|
| D-Lg1 | Which documents exist, and what does a tenant accept? | A: Terms only. B: Terms that include exact versions of the other documents, accepted once (V3). C: each document accepted on its own | **B (Recommended).** Kinds: Terms of Service, Privacy Policy, Data Processing Agreement (needed because tenants put their customers' data in Vixly), Acceptable Use Policy, Service Level Agreement. Cookie Policy is public website text and is never accepted. A Terms version lists the exact versions of the others it includes; the tenant accepts that set once. A new Privacy, DPA, AUP or SLA version is published by publishing a new Terms version that includes it. Drop `Applicability` |
| D-Lg2 | How are legal texts authored? | A: uploaded file per language (V3 has one PDF). B: text written in the console (Markdown) and shown as HTML. C: both | **A (Recommended).** One approved PDF per language (en, fr, es) per version, uploaded in the console. Platform checks it is a real PDF (file signature, 10 MB limit), makes the SHA-256 itself and stores the bytes in the database, as V4 does for the logo. Served by Platform with `Content-Disposition` and no HTML ever rendered from it, so there is no HTML injection. A plain-text "what changed" summary per language (no HTML, 2,000 characters) goes into notices and the acceptance screen. Lawyers approve files, not editor text |
| D-Lg3 | Draft and publish rules | A: immediate publish (V3). B: draft, then publish. C: draft, then a second person approves | **B (Recommended).** Draft (editable, files replaceable), then Published (never changed), then Superseded when the next version is published. Publish needs all three languages, `platform.legal.publish` and step-up, and shows who published. One published version per kind at a time (filtered unique index). C only if the owner wants four-eyes; the team is small |
| D-Lg4 | Does every new version need acceptance? | A: every version (V3). B: staff choose per version: "acceptance required" or "notice only" | **B (Recommended).** Notice only for corrections that change no rights (typos, contact details): tenants are emailed and the version is current from its effective date, no acceptance asked. Acceptance required for real changes. The choice is recorded and cannot be changed after publish |
| D-Lg5 | Who may accept for a tenant? | A: Owner only (V3). B: Owner or any tenant admin. C: Owner, or an admin the owner names in Operations | **A (Recommended)** for Stage 1: the owner binds the company. Operations checks the user is the owner (ADR-0003: Platform holds no tenant users) and sends its user reference, display name and role; Platform records them. C can come later from Operations without a Platform change. Staff may record an offline acceptance (signed paper) with a reason, the file and step-up, source "Staff record" |
| D-Lg6 | What happens when a tenant has not accepted by the deadline? | A: nothing (V3 Platform). B: reminders only. C: the owner must accept before using the back office; others continue. D: tenant becomes read-only. E: tenant blocked | **C (Recommended).** Before the deadline: a banner for the owner in Operations and reminders. After the deadline: the owner sees a blocking accept screen at sign-in and cannot use the back office until they accept; other tenant staff and the tenant's customers are not affected, so shipments never stop for a legal click. Platform never suspends for this on its own; staff can still use the Slice 2 conduct suspension if needed. The legal status carries `overdue` so Operations can enforce it |
| D-Lg7 | Reminder cadence | A: manual only, after enforcement (V3). B: automatic plus manual | **B (Recommended).** Automatic by the Worker through the outbox, in the tenant's default language, to the owner contact (Slice 2): notice at publish, then 14, 7 and 1 days before the deadline, then weekly for 4 weeks after it, then stop. Manual reminder by staff with `platform.legal.remind` at any time, with a reason code and a 24-hour cooldown per tenant. Each sent reminder is a row with its step and delivery result; no raw errors shown |
| D-Lg8 | Notice period and dates | A: three dates (V3). B: publish date, effective date, deadline = effective date | **B (Recommended).** "Acceptance required" versions: effective at least 30 days after publish (the default), deadline = effective date. "Notice only" versions: effective at publish or a chosen date. New registrations accept the newest published version, even before it is effective (as V3), so they never need a second click. Dates stored as UTC instants and shown in the viewer's time zone |
| D-Lg9 | Registration (Slice 2) | A: keep configuration versions. B: use the published documents | **B (Recommended).** Remove `Platform:Legal:TermsVersion` and `PrivacyVersion`. The registration page reads the current published Terms (with its included documents) in the registrant's language; start sends the version id and hash it showed; a mismatch gives `platform.legal.version_changed`. The registration keeps the version, language, time, client IP and user agent; tenant creation writes the `PlatformTenantTermsAcceptance` row in the same commit. Migration: existing registrations with configured "2026-10" are linked to a seeded first version |
| D-Lg10 | Tenants with no acceptance (staff-created, older tenants) | A: leave them (V3). B: they must accept the current version like any other tenant | **B (Recommended).** When a tenant is activated without an acceptance (sales-led, Slice 2), or when Slice 8 goes live for existing tenants, the current version is due with a 30-day deadline from that day, and the reminder cycle starts. Deactivated and banned tenants are never asked |
| D-Lg11 | Contracts for Operations and Customer (Slice 9) | A: Operations polls per user (V3). B: status query plus events | **B (Recommended).** `GetPlatformTenantLegalStatus(tenant)`: status (accepted, due with deadline, overdue), required version id, summary and file links per language. `AcceptPlatformLegalTerms(tenant, version id, hash, language, actor reference, display name, role, client IP, user agent)`: Platform checks the version is current and the tenant matches the caller (S-13). Events `platform.legal.version_published.v1` and `platform.legal.terms_accepted.v1`. Operations forwards the browser's IP and user agent. The Customer plane only shows links to Vixly's public Privacy Policy; tenants' customers never accept Vixly terms |
| D-Lg12 | Register and history | A: all tenants in memory (V3). B: paged register | **B (Recommended).** Paged register per version (P-1) with status filter and search; counts by query; only tenants that must accept (Active and Suspended; never-activated, Deactivated, Banned left out). Tenant page Legal section: status, the acceptance evidence, reminders with staff names, actions. Print of one tenant record kept. Acceptances are never deleted while the tenant exists, then follow the tenant's retention rule |
| D-Lg13 | Permissions | | **Recommended:** `platform.legal.view`; `platform.legal.manage` (drafts and files); `platform.legal.publish` (step-up); `platform.legal.remind`; `platform.legal.record_acceptance` (offline record, step-up). `platform.settings.maintenance` (step-up). Administrator: all; Support: view and remind; Billing: view. No internal-API publish at all |
| D-Lg14 | Does maintenance belong in this slice, and how does it work? | A: drop it. B: in Slice 8 as V3 (one window). C: in Slice 8 with history, enforcement by Edge and Operations in Slice 9. D: later, with Edge (Stage 2) | **C (Recommended).** `PlatformMaintenanceWindow` rows (history kept): planned (start at least 1 hour ahead, at most 12 hours long) or emergency (starts now, staff set the end, extendable), message per language, status Scheduled, Active, Ended early, Cancelled, Completed, each step with who and when, audit. Planned windows email tenant owners 48 hours before (outbox, tenant language) when they are at least 48 hours ahead. While active: the Worker pauses billing, collections and emails; public registration and payment pages show the maintenance page; Operations and Customer get it from `GetPlatformMaintenanceStatus` and show a banner, then a maintenance page. The Platform console stays open for staff so they can end it |
| D-Lg15 | Which other platform settings belong here? | A: a general `PlatformSettings` key and value store. B: only typed records | **B (Recommended).** No catch-all `PlatformSettings` aggregate; V4 keeps each settings record on its own (billing, collections, company profile, merchant, Zelle). Add the V3 support email and support phone to `PlatformCompanyProfile`, used in tenant emails and Operations' help page. Default language stays per tenant (Slice 2). Nothing else in V3 needs a home |
