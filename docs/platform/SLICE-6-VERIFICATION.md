# Slice 6: Collections - verification record

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Date | 2026-10-16 |
| Built on | `8c3ccca` plus Slices 4 and 5 (not committed: the dev team owns git) |
| Design | [Design note](SLICE-6-COLLECTIONS-DESIGN.md) (build notes in section 10), [sweep](SLICE-6-COLLECTIONS-SWEEP.md) |
| Status | **Built and verified; waiting for owner sign-off** |

## 1. Automated checks

Release build of `Vixly.Platform.slnx`: no errors, no warnings.

| Test project | Before (Slice 5) | After |
|---|---|---|
| Platform.Domain.Tests | 258 | 268 |
| Platform.Application.Tests | 180 | 189 |
| Platform.Infrastructure.Tests (with SQL Server) | 48 | 50 |
| Platform.Api.Tests | 8 | 8 |
| Platform.Web.Tests | 31 | 31 |
| Architecture.Tests | 4,494 | 4,752 |
| Shared.Kernel.Tests | 81 | 81 |

All pass.

What the new tests cover:
- **Settings:** the owner's defaults; steps out of order refused; a block day past 60 refused; retry days on or after the block refused.
- **Timeline:** reminders on days 1 and 5, final warning on day 10 with the block on day 14, never two steps on one day. Catch-up after downtime sends only the final warning, and the block waits for the minimum notice.
- **Block levels:** read-only for the set days, then payment required, recorded once. A pending credit-note request holds the case.
- **Extensions:** the last day is included; the length limit; the per-case limit; the higher permission; lifting a block; the "ends soon" notice; resuming at the final warning with a new block date.
- **Closing and retries:** closing a blocked case records "unblocked". The retry key is unique per invoice and day.
- **Subscription:** blocked from Active, Past due or Pending payment; changes refused while blocked; payment clears the block only when nothing else is overdue; account suspension returns to Blocked.
- **The job end to end over fakes:** case, emails, block, entitlements, no access, payment, then "access restored".
- **Emails:** the overdue invoices with pay links; the final warning also goes to the owner; nothing is sent once the bill is settled.
- **Card retries:** only with consent and a retryable failure, once per invoice and day, never after a final failure.
- **Use cases:** extensions need the permission and step-up, and past the limit the higher permission. Notes and "check again" need manage.
- **Pending payment:** blocked only with an overdue invoice to pay.
- **Account status:** a suspended account pauses collections; a closed one ends the case.
- **Settings update:** needs configure and step-up.
- **Database:** settings, case, history and the blocked subscription round-trip. The database refuses a second open case and a second retry for the same day, even from another host. The job's tenant list, the case list, overdue invoices and the dashboard are covered.

`CollectionCaseEvents` is append-only in the global script (DENY UPDATE, DELETE to the app login).

## 2. End-to-end run (console and API, fresh Development database)

Fresh database from the global script, first SuperUser (Jude), `--seed-demo-tenants`, card payments on with the simulated provider. The collections job ran every minute. Days were moved by changing due dates and planned block dates in the database.

| # | Check | Result |
|---|---|---|
| 1 | Settings page | Defaults 1, 5, 10, 14; retries 3, 7; read-only 7 days; extensions 14 days, 2 per case |
| 2 | Invoice 11 days overdue | One case opened at "Final warning", without the two reminders; email to the billing contact with the block date in the subject and a pay link in the body |
| 3 | Block date reached | Case Blocked; subscription Blocked for non-payment; "Access limited" email; case page shows "Read-only for the tenant's staff" and that customers are never affected; tenant page Collections section links to the case |
| 4 | Extension (step-up) | Subscription back to Past due; "More time to pay until ..." email with the date only, no reason; history shows the extension with its internal reason |
| 5 | Note and "Check again now" | Both in the history with the staff member's name |
| 6 | Paying from the email's pay link | Invoice Paid; case closed as settled; subscription Active |
| 7 | Second tenant, read-only days passed | Case records no access; tenant page shows "Blocked for non-payment" and "Payment required" |
| 8 | Collections page | Counts by stage, blocked subscriptions, overdue total and ageing, recovered amount; case list with status filter |
| 9 | Settings save | A bad timeline is refused with a clear message; a valid change is saved with step-up |
| 10 | Menu and outbox | "Collections" under Commercial, "Collections settings" under System; no failing outbox message |

## 3. Defects found and fixed during verification

- **A Pending payment subscription with no invoice was blocked.** The demo tenant's first invoice was never issued. It was blocked with nothing it could pay, and no email went out. It is now blocked only when an invoice is overdue (test added).
- **Email subjects showed "{blockDate}".** The subject now gets the dates too.
- **Every open case was rewritten each minute.** The job saved a "last checked" time on every run. Quiet runs now write nothing.

## 4. For the dev team

- **Database:** run the updated global script. It adds the `CollectionsSettings`, `CollectionCases` and `CollectionCaseEvents` tables and two columns on `Subscriptions`, and makes collections history append-only.
- **Operations plane (Stage 3)** must enforce the access level from the entitlements:
  - Read-only: the tenant's staff can read but not change anything.
  - Payment required: only the owner and the billing contact can sign in, and only to the payment page.
  - **Never limit customer-facing features.**
- **Configuration:** `PlatformSubscriptions:UnpaidReadOnlyDays` in the console's `appsettings.json` is no longer read and can be deleted.
- **Roles:** the system roles gain the five `platform.collections.*` permissions at the next start:
  - Billing clerk: view, manage, extend.
  - Sales manager: view, extend beyond the limit.
  - CFO: view, configure.
  - Administrator: all five.
- **Backups** of the files changed on disk are in `_to_delete/backup-slice6-*`, with the delivery package.

## 5. Open items

- Owner sign-off of Slice 6.
- The Operations plane's enforcement of the access levels (Stage 3).
