# Slice 8: Legal and settings - verification record

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Date | 2026-10-16 |
| Built on | Slice 7 and the Slice 5 Zelle update as delivered (not committed: the dev team owns git) |
| Design | [Design note](SLICE-8-LEGAL-DESIGN.md) (build notes in section 10), [sweep](SLICE-8-LEGAL-SWEEP.md) |
| Status | **Built and verified; waiting for owner sign-off** |

## 1. Automated checks

Release build of `Vixly.Platform.slnx`: no errors, no warnings.

| Test project | Before (Zelle update) | After |
|---|---|---|
| Platform.Domain.Tests | 281 | 286 |
| Platform.Application.Tests | 204 | 210 |
| Platform.Infrastructure.Tests (with SQL Server) | 53 | 54 |
| Platform.Api.Tests | 8 | 8 |
| Platform.Web.Tests | 31 | 31 |
| Architecture.Tests | 5,339 | 5,698 |
| Shared.Kernel.Tests | 81 | 81 |

All pass.

What the new tests cover:
- **Documents:** version labels; publishing needs all three languages; Terms must include Privacy and DPA; acceptance-required Terms take effect at least 30 days after publishing; published versions never change; publishing supersedes the previous version; only drafts are deleted; drafts are never public.
- **Files:** must be real PDFs within 10 MB; Platform makes the SHA-256; summaries are plain text.
- **Acceptance:** owner only; the version and file hash shown must still be current; a double click counts once; staff records need a reason, evidence and step-up.
- **Read-only rule:** due tenants keep full access; overdue tenants are read-only in entitlements and the licensing access check; accepting restores full access at once; a notice-only version asks nothing more.
- **Deadlines:** 30 days from publishing, or from activation for newer tenants.
- **Reminders:** notice at publish, 14, 7 and 1 days before, then weekly for 4 weeks; once each; only the latest after downtime; none once accepted; manual ones need a reason and wait 24 hours; notice-only versions send one notice to each tenant.
- **Registration:** the acceptance given at registration is recorded when the tenant becomes active, with the language and IP; a replayed message records nothing more.
- **Database:** Terms with parts and files round-trip; one published version per kind; one acceptance per tenant and version; each automatic reminder once; the register and the reminder list work out status and deadline in SQL.
- **Permissions:** view-only staff cannot create drafts or send reminders.

`TenantTermsAcceptances` is append-only in the global script (DENY UPDATE, DELETE to the app login).

## 2. End-to-end run (console and API, fresh Development database)

Fresh database from the global script, first SuperUser (Jude), `--seed-demo-tenants`. The clocks ran every minute.

| # | Check | Result |
|---|---|---|
| 1 | Demo legal documents | Privacy, DPA and Terms 2026-10 published; Terms need acceptance by 30 days later |
| 2 | Notices | One "new Terms to accept" email per active or suspended tenant (16), in the tenant's language, with the portal link |
| 3 | New Terms 2026-12 | Publishing without files refused with a clear message; three PDFs with summaries uploaded; published with step-up; includes Privacy and DPA 2026-10; 2026-10 superseded |
| 4 | Public API | `GET /legal/terms?language=fr` returns 2026-12 with its parts and the French summary; the PDF downloads as an attachment; a draft is not served |
| 5 | Deadline passed | Register "Overdue" lists the tenants; the tenant page shows "Overdue: read-only" |
| 6 | Manual reminder | Sent, with the "read-only until you accept" email; a second one within 24 hours refused |
| 7 | Offline acceptance (step-up) | Recorded with the signed copy; tenant shows Accepted with the evidence (staff, reason, hash) |
| 8 | Evidence protection | The app login cannot change acceptances (permission denied) |
| 9 | Menu and outbox | "Legal documents" under System; no failing outbox message |

Screens were captured for the documents list, a published version, the register and the tenant Legal section.

## 3. Not covered here

- The owner's accept screen and the read-only enforcement are in Operations; Slice 9 exposes `GetPlatformTenantLegalStatus` and `AcceptPlatformLegalTerms`. They are covered by the application tests above.
- The registration page itself is the Public plane's; the API calls it needs are in place.
- Maintenance windows: later, with Edge (owner decision).
