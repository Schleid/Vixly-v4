# Slice 8: Legal and settings - design

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Date | 2026-10-16 |
| Sweep | [SLICE-8-LEGAL-SWEEP.md](SLICE-8-LEGAL-SWEEP.md) |
| Owner decisions | 2026-10-16, section 9 |
| Status | **Built and verified; waiting for owner sign-off** ([verification](SLICE-8-VERIFICATION.md)) |

## 1. Outcome

- Vixly publishes its legal documents as versions: Terms of Service, which includes exact versions of the Privacy Policy, Data Processing Agreement, Acceptable Use Policy and Service Level Agreement. Each version is one approved PDF per language (en, fr, es) with a short plain-text "what changed" summary.
- A tenant's owner accepts the current Terms once; the acceptance records the version, the file hash, the language read, who, when, the client IP and user agent, and how (registration, Operations, or a staff record of a signed paper).
- Tenants are reminded automatically in their language. **After the deadline, the tenant's staff are read-only until the owner accepts; the tenant's customers keep operating normally.**
- Maintenance windows are **not** in this slice (owner: later, with Edge). The support email and phone join the company profile.

## 2. Data (schema `platform`)

| Table | What |
|---|---|
| `LegalDocuments` | Kind (Terms, Privacy, DPA, AcceptableUse, ServiceLevel), version label, status (Draft, Published, Superseded), acceptance required or notice only (Terms only), effective date, deadline, published at and by, row version. One published version per kind (filtered unique index); version label unique per kind |
| `LegalDocumentFiles` | One PDF per document and language: bytes (at most 10 MB, checked as a real PDF), SHA-256 made by Platform, size, summary (plain text, at most 2,000 characters) |
| `LegalDocumentParts` | Which exact versions a Terms version includes |
| `TenantTermsAcceptances` | Tenant, Terms version, hash, language, accepted at, source, actor reference, name and role, client IP, user agent, staff id and reason for a staff record, evidence file. Append-only (DENY UPDATE, DELETE), unique per tenant and version |
| `TermsReminders` | Tenant, Terms version, step (publish notice, 14, 7, 1 days before, 1 to 4 weeks after, manual), language, staff and reason for manual ones, sent at. Unique per tenant, version and automatic step |
| `CompanyProfiles` | Gains support email and support phone |

## 3. Rules

**Documents**
- A draft can be edited and its files replaced; a published version is never changed; publishing the next version of a kind supersedes the previous one.
- Publishing needs a file in all three languages, `platform.legal.publish` and step-up. A Terms version must include published versions of Privacy and DPA (AUP and SLA when published).
- Terms versions are "acceptance required" or "notice only" (owner D-Lg4). Acceptance required: effective at least 30 days after publishing (default 30); the deadline is the effective date. Notice only: effective at publish; tenants are emailed, nothing to accept.
- Other kinds are published on their own, then included by the next Terms version (they are never accepted alone).

**Who must accept and by when**
- Tenants that are Active or Suspended. Never-activated, Deactivated and Banned tenants are never asked.
- The version to accept is the latest "acceptance required" Terms version; accepting any later version also counts.
- Deadline: the version's deadline, or 30 days after the tenant was first activated when that is later (staff-created and new tenants get 30 days, D-Lg10).
- Status: Accepted, Due (with deadline), Overdue.

**Accepting**
- Owner only (D-Lg5). From Operations (Slice 9 contract) with the user reference, name, role "owner", language, client IP and user agent; Platform checks the version is current and its hash matches what was shown, else `platform.legal.version_changed`.
- At registration: the registrant accepts the current published Terms; when the tenant is activated, Platform records that acceptance (source Registration) with the registration's language, time and IP. While nothing is published yet, registration keeps checking the configured versions.
- Staff can record an offline acceptance (signed paper): `platform.legal.record_acceptance`, step-up, a reason and the evidence file.

**Overdue (owner D-Lg6, changed)**
- The tenant's entitlements and portal access answer **Read-only** (never more than the subscription allows) with a legal reason, until the owner accepts. Operations keeps the owner able to read and accept. The tenant's customers are not affected: Customer-plane features keep working (the Slice 6 rule).
- No suspension; nothing else changes.

**Reminders (D-Lg7)**
- Automatic, by the clock, in the tenant's language, to the owner: a notice when a version is published, then 14, 7 and 1 days before the deadline, then weekly for 4 weeks after it, then stop. Each step is sent once (unique row). Notice-only versions send one notice.
- Manual by staff with `platform.legal.remind`, with a reason, at most once per tenant per 24 hours.

## 4. Permissions (D-Lg13)

| Code | Administrator | Others |
|---|---|---|
| `platform.legal.view` | Yes | Security administrator, Sales manager, Salesperson, Billing clerk, CFO |
| `platform.legal.manage` (drafts and files) | Yes | |
| `platform.legal.publish` (step-up) | Yes | |
| `platform.legal.remind` | Yes | Sales manager, Salesperson |
| `platform.legal.record_acceptance` (step-up) | Yes | |

## 5. Use cases

Console: list documents; get a document; create a draft; update a draft; set a file per language; set the included parts; delete a draft; publish; download a file; register (paged, per required version, status filter, search); tenant legal status with acceptances and reminders; send a reminder; record an offline acceptance.
Public (anonymous): current published Terms with its parts, summaries and file links in a language; download a published file.
Internal (Slice 9 exposes them): tenant legal status; accept Terms.
Clock: reminders. Handler: record the registration acceptance on activation.

## 6. Contract for Operations (Slice 9)

- `GetPlatformTenantLegalStatus`: status, deadline, the version to accept (id, label, hash per language, summary), and whether access is read-only for this reason.
- `AcceptPlatformLegalTerms`: version id, hash, language, actor reference, name, role, client IP, user agent.
- Events: `platform.legal.version_published.v1`, `platform.legal.terms_accepted.v1`.
- Entitlements and portal access gain the legal reason when read-only for it.

## 7. Console screens

- **Legal documents** (menu, System): versions by kind with status; new draft.
- **Document page:** files per language with download, summaries, included versions (Terms), acceptance required, effective date, publish (step-up), delete draft.
- **Acceptance register:** tenants that must accept the current required version, with status and deadline, paged.
- **Tenant page, Legal section:** status, deadline, acceptances with evidence, reminders, "send reminder", "record offline acceptance".
- **Company profile:** support email and phone.

## 8. Done when (in addition to the slice gate)

- Publishing, acceptance and reminders work end to end in the console and through the public calls.
- An overdue tenant is read-only in entitlements and portal access, and back to normal right after the owner accepts.
- Registration records the acceptance with the language.
- Every new text exists in en, fr and es.

## 9. Owner decisions (2026-10-16)

| # | Decision |
|---|---|
| D-Lg6 | **Changed:** after the deadline the tenant becomes read-only until the owner accepts; the tenant's customers keep operating the system normally |
| D-Lg2 | Upload a PDF per language (as proposed) |
| D-Lg14 | Maintenance mode: **later** (with Edge), not in this slice |
| Others | D-Lg1, D-Lg3 to D-Lg5, D-Lg7 to D-Lg13, D-Lg15 as proposed |

## 10. Build notes

Decisions taken while building; none changes an owner decision.

- **Who must accept:** the newest Terms version marked "acceptance required". Accepting that version or any later one counts. A later "notice only" version asks nothing more.
- **Deadline for new tenants:** a tenant first activated after the version was published gets 30 days from its activation, when that is later than the version's deadline. Its first notice goes out at activation.
- **Read-only:** applied where Operations reads access: the tenant entitlements and the licensing access check (portal access and device checks). Only Full access is capped to Read-only; a lower access level (for example from collections) stays as it is. Entitlements gain a `Legal` part (status, deadline, version) while acceptance is due or overdue.
- **Registration:** the start of a registration checks the published Terms version and the Privacy version it includes; before the first publish it keeps checking the configured versions (`Platform:Legal:TermsVersion` and `PrivacyVersion`), which stay in `appsettings.json` (shared with the Frontend team). When the tenant becomes active, Platform records the acceptance from the registration (language, time, IP).
- **Publishing a Terms version** requires every other kind that has a published version to be included, and Privacy and DPA at least. A new Terms draft starts with the published versions already ticked.
- **Files** are checked as PDFs (file signature) and hashed by Platform; staff can download drafts to review them; the public can download published and superseded versions only. PDFs are always sent as attachments with `nosniff`.
- **Summaries** are plain text (no HTML), shown in emails and to the owner.
- **Reminder clock:** runs every minute with the other clocks; at most 500 tenants per run; after downtime only the latest step due is sent. Each step is a row with a unique index, so a second host never sends it twice. A reminder for Terms already accepted is skipped when the email is about to go out.
- **Emails:** new version, reminder, overdue (read-only now) and notice only, in the tenant's language, to the owner, with the link to the tenant portal's legal page (`https://{portal}.{domain}/legal`, served by Operations).
- **Staff record of a signed paper:** needs the current published Terms, a reason and the signed copy (PDF, PNG or JPEG, at most 5 MB) and step-up. The evidence file is kept with the acceptance; a download for it is not built yet.
- **Acceptances are evidence:** append-only in the global script (DENY UPDATE, DELETE to the app login).
- **Support contact:** support email and phone are fields of the company profile (Billing settings); invoices and emails keep using the configured support address until the Slice 9 contracts expose the profile to Operations.
- **Demo data** publishes Privacy, DPA and Terms "2026-10" with sample PDFs, so demo tenants are due in 30 days and the clock sends notices.
- **Maintenance windows** are not in this slice (owner: later, with Edge).
