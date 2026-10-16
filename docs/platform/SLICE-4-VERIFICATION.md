# Slice 4: Billing - verification record

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Date | 2026-10-16 |
| Built on | `8c3ccca` (dev team HEAD), changes not committed: the dev team owns git |
| Design | [Design note](SLICE-4-BILLING-DESIGN.md) (build notes in section 10), [sweep](SLICE-4-BILLING-SWEEP.md) |
| Status | **Built and verified; waiting for owner sign-off** |

## 1. Automated checks

Release build of `Vixly.Platform.slnx`: no errors, no warnings.

| Test project | Before (8c3ccca) | After |
|---|---|---|
| Platform.Domain.Tests | 232 | 248 |
| Platform.Application.Tests | 163 | 169 |
| Platform.Infrastructure.Tests (with SQL Server) | 35 | 41 |
| Platform.Api.Tests | 8 | 8 |
| Platform.Web.Tests | 31 | 31 |
| Architecture.Tests | 3,603 | 4,067 |
| Shared.Kernel.Tests | 81 | 81 |

All pass.

What the new tests cover: invoice totals, discount and tax per line to the cent (half away from zero); zero totals refused; partial and full payments, overpayment, missing reference, future date and paid invoices refused; reversal reopening the balance once; credit notes and voids with four eyes, void only before payment, credit after payment recorded as money owed back; past due once, and never on the due day itself; gap-free numbers per kind and year; tax rate choice (region first, newest in force); settings defaults; the subscription activated by its paid invoice and Past due cleared; first-period invoice from the "started" message with setup fee, one-time charges and the Quebec rate, billed once when the message is replayed; no invoice and a retry when the company profile is missing; card payments refused by hand; the clock marking an unpaid renewal and its subscription Past due; the database mapping, one invoice per source event, two hosts racing for a number, a new sequence taken twice in one commit, settings, profile, tax rates and stored documents, and the PDF renderer for invoice, credit note and receipt. `InvoiceEvents` and `BillingDocuments` are append-only in the global script (DENY UPDATE, DELETE to the app login).

## 2. End-to-end run (console, fresh Development database)

Fresh database from `build/scripts/sql/vixly-v4-database.sql`, first SuperUser (Jude), a sales manager (Ada), then `--seed-demo-tenants`.

| # | Check | Result |
|---|---|---|
| 1 | Seed | Company profile, 5 tax rates, 8 invoices for the paying demo subscriptions: 3 paid, 1 part-paid, 4 open, with receipts |
| 2 | Navigation | Invoices under Commercial, Billing settings under System |
| 3 | Invoices list | 8 invoices with tenant, type, status, dates, total and balance; status, overdue and date filters |
| 4 | Invoice page | Facts, bill-to snapshot, translated lines, totals, "no tax was charged" when 0 % |
| 5 | Download PDF | `200 application/pdf`, `Cache-Control: no-store, private`; anonymous request redirected to sign-in; Spanish invoice renders in Spanish with Spanish number and date formats |
| 6 | Record payment (cheque, reference) | Step-up asked; amount defaults to the balance; invoice Paid; receipt RCT-2026-000005 issued and emailed with the PDF attached |
| 7 | Reverse it with a reason | Step-up asked; payment Reversed with reason and author; invoice back to Issued |
| 8 | Credit note of 10.00 | Requested by Jude; Jude has no decide button; appears in Ada's Approvals as "Credit note"; Ada approves; CN-2026-000001 issued; balance 140.00 |
| 9 | History | Issued, emailed, payment recorded and reversed, credit requested and issued, each with actor and reason |
| 10 | Billing settings | Payment terms 14 days and instructions saved (step-up); values shown after saving; company profile shown |
| 11 | Tax rates | 5 seeded rates; US-FL 6 % added (step-up) and in force |
| 12 | Tenant page | Billing section: open balance, tax on new invoices, tax ID saved, the tenant's invoices; Slice 3 "Record offline payment" no longer offered |
| 13 | Emails | Invoice, credit note and receipt emails in the tenant's language, each with its PDF |

## 3. Defects found and fixed during verification

- **Overdue on the day of issue.** An invoice due on issue showed as overdue at once. Past due now starts the day after the due date (D-B8), with one rule used by the domain, the queries and the clock; a test covers it.
- **Billing settings showed old values after saving.** Settings and number sequences were cached on a service that, in the console, lives as long as the browser connection. The cache was removed; the repository looks at rows of the current transaction first, so one commit still continues a sequence it started (test added).

## 4. For the dev team

- **Database:** run the updated global script (Slice 4 tables, tenant tax columns, append-only rules).
- **QuestPDF licence:** the Community licence is free for companies under USD 1M yearly revenue; otherwise buy a licence and set `PlatformDocuments:QuestPdfLicense` (`Community`, `Professional` or `Enterprise`).
- **Company profile:** invoices are not issued until the profile is filled in under Billing settings; subscription messages wait in the outbox and are billed once it is.
- **Backups** of the files changed on disk are in `_to_delete/backup-slice4-*`, with the delivery package; delete `_to_delete` when satisfied.

## 5. Open items

- Owner sign-off of Slice 4.
- `GET /registration/invoice` moves to Slice 5 (design note section 10).
