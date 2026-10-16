# Slice 4: Billing - design note

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Approved by the owner, 2026-10-15** (section 9). Built 2026-10-16 (section 10); [verification record](SLICE-4-VERIFICATION.md) |
| Inputs | [Sweep](SLICE-4-BILLING-SWEEP.md), Slice 3 design (messages D-S11, pricing D-S9, non-payment status D-S17, section 11 discounts and charges), plane document sections 3 to 7, ADR-0005 (languages), ADR-0007 (worker) |
| Depends on | Slice 3 (built 2026-10-14) |

## 1. Outcome

Billing turns what Slice 3 says is due into invoices, without anyone typing them. A subscription that starts, ends its trial, upgrades or renews produces an invoice in the tenant's currency and language, with tax from the tenant's country, the discount shown as its own line and any pending one-time charges added. Each invoice gets the next number when it is issued, a stored PDF and an email to the tenant's billing contact.

Staff record payments received outside the card provider (bank transfer, cash, cheque, Zelle, other), in part or in full, and each payment gets a numbered receipt. Mistakes are corrected with credit notes, never by editing an issued invoice. Paying the invoice a subscription waits for activates it; an unpaid renewal past its due date marks the subscription Past due. Card payments (Slice 5) and reminders, blocking and recovery (Slice 6) build on this.

## 2. Data (schema `platform`)

| Entity | Table | Notes |
|---|---|---|
| `PlatformInvoice` | `Invoices` | Tenant, subscription, number (unique, set at issue), kind (First period, Change, Renewal, Charges), status (Issued, Paid, Void; part-paid and overdue are computed), currency, language, issue date, due date, period covered, totals (net, discount, tax, total, paid, credited, balance), source key (unique: one invoice per subscription event), billing contact snapshot, issuer and customer snapshots (name, address, tax ID), row version |
| `PlatformInvoiceLine` | `InvoiceLines` | Kind (Plan period, Setup fee, Upgrade proration, One-time charge, Discount), text key and arguments (translated at render), quantity, unit price, net, tax rate and label, tax amount, period, source id (charge, discount) |
| `PlatformPayment` | `Payments` | Invoice, tenant, receipt number, amount, method, received date, reference, note, recorded by, status (Recorded, Reversed), reversal reason, row version |
| `PlatformCreditNote` | `CreditNotes` | Invoice, number, amount, tax part, reason, status (Requested, Issued, Rejected), requested by, decided by (four eyes, D-B6) |
| `PlatformBillingDocument` | `BillingDocuments` | Invoice, credit note or receipt; language; PDF bytes; SHA-256; created at. Written once, never changed (D-B10) |
| `PlatformNumberSequence` | `NumberSequences` | Kind (invoice, credit note, receipt) and year; next value. Locked row per issue (D-B2) |
| `PlatformBillingSettings` | `BillingSettings` | Payment terms, number prefixes, paper size, invoice email on/off, footer and payment instructions per language; issuer details (D-B12) |
| `PlatformTaxRate` | `TaxRates` | Country, optional region, rate, label per language, valid from (D-B1) |
| `PlatformInvoiceEvent` | `InvoiceEvents` | Append-only history (issued, emailed, payment recorded or reversed, credited, voided, past due) |
| Tenants (changed) | `Tenants` | + optional tax ID and tax exemption (reason, reference) on the company (D-B16) |

Money is `decimal(18,2)`, rates `decimal(9,4)`. Lines are rounded to the cent half away from zero; tax is computed per line and summed.

## 3. Rules

### Where invoices come from (D-B3, D-B5)

| Slice 3 message | Invoice |
|---|---|
| Started, waiting for payment | First period (prorated by Slice 3) + setup fee (or its override) + pending one-time charges − discount |
| Trial ended | Same as above |
| Changed, charged now | Upgrade proration for the rest of the period, due on issue |
| Renewed | Next period − discount + pending one-time charges |
| One-time charge "bill now" | The charge alone |

- One invoice per source event (unique source key), so a replayed message never bills twice.
- A total of zero is not invoiced; Slice 3 already activates when nothing is due.
- Each invoice is built and issued in one transaction with its history, audit, outbox message and number. One tenant failing never stops another.

### Payments (D-B7)

- Staff record a payment on an issued invoice: amount up to the balance, method, received date, reference (required except cash), note. Step-up.
- Partial payments are allowed; overpayment is refused.
- A recorded payment can be reversed (bounced cheque, wrong entry) with a reason; the balance reopens. Step-up.
- When the invoice a subscription is waiting for is paid in full, Billing publishes "invoice paid"; Slice 3 activates the subscription (replacing its own "record offline payment") and registration completes.

### Past due (D-B8)

- The day after its due date an unpaid renewal or change invoice marks the subscription Past due (access unchanged). Paying it clears Past due.
- Blocking for non-payment, reminders and grace are Slice 6.

### Credit notes and void (D-B6)

- An issued invoice is never edited. A credit note (full or partial, with reason) lowers its balance; after payment it records money owed back (refund itself: Slice 5 for cards, recorded by hand otherwise).
- Void: only while nothing has been paid; it is a full credit note plus status Void.
- Credit notes and voids are requested by billing staff and decided by the sales manager, never by the requester (four eyes).

### Tax (D-B1)

- Rate from the tenant's country (and region when set), on the date of issue, stored on each line with its label.
- No rate for the country: 0 % and the invoice says no tax was charged.
- A tax-exempt tenant (reason and reference) gets 0 %.
- Prices are before tax (D-S9).

### Numbers (D-B2)

- One platform sequence per document kind and year: `INV-2026-000001`, `CN-2026-000001`, `RCT-2026-000001`. Prefixes are settings.
- Assigned when the document is issued, under a row lock, inside the issuing transaction: no gaps, no duplicates.

### Documents and email (D-B10, D-B11, D-B13)

- The PDF is made when the invoice, credit note or receipt is issued, in the tenant's language, and stored with its hash. Downloads return the stored file.
- The invoice email goes to the billing contact (falls back to the owner) with the PDF attached; the receipt the same way. Sent through the outbox.

## 4. Permissions

`platform.billing.view`; `platform.billing.record_payments` (step-up); `platform.billing.request_credits`; `platform.billing.approve_credits` (step-up, four eyes); `platform.billing.configure` (step-up: settings and tax rates). The existing Billing role gets view, record payments and request credits; the sales manager approves credits; the administrator holds all.

## 5. Use cases (three files each, gate first)

Bill from a subscription message (handler), list invoices, get invoice, download document, resend invoice email, record payment, reverse payment, request credit note, approve or reject credit note, request void, list payments, mark past due (clock), get and update billing settings, list and save tax rates, set tenant tax details.

## 6. Platform.Api additions

For registration: `GET /registration/invoice` (the registration's first invoice and its PDF, with the registration token). Tenant self-service lists come with Contracts in Slice 9.

## 7. Console screens

Invoices (paged list with status, overdue, tenant, number and date filters); invoice page (lines, totals, payments, credit notes, history, download, resend, record payment, reverse, request credit or void); approvals queue gains credit notes; tenant page Billing section (balance, invoices, tax details); Billing settings and Tax rates pages.

## 8. Done when (in addition to the slice gate)

- Every Slice 3 message that charges money produces exactly one invoice, also when replayed or when two hosts race.
- Totals, tax and discounts to the cent; numbers gap-free under concurrency.
- Payments: partial, full, refused overpayment, reversal; activation after the waiting invoice is paid; Past due after the due date and cleared by payment.
- Credit notes and void with four eyes; PDFs stored once and identical on download; texts in en, fr and es.

## 9. Owner decisions

Settled by the owner on 2026-10-15: D-B1, D-B6 and D-B12 as marked; all others "as proposed".

| # | Question | Proposal |
|---|---|---|
| D-B1 | Tax model | **Owner: as proposed.** Rates by country (and region), set by staff, stored on each line; no rate = 0 % with a note; tenant exemption with reason |
| D-B2 | Numbering | One platform sequence per kind and year, gap-free, assigned at issue, prefixes as settings |
| D-B3 | Invoice timing | Automatic from Slice 3 messages; renewal invoiced when the period starts (billing in advance) |
| D-B4 | Payment terms | A setting in days; until set, invoices are due on the day they are issued |
| D-B5 | Manual invoices | None; one-time charges ("bill now" or with the next invoice) and credit notes cover the cases |
| D-B6 | Corrections | **Owner: approved by the sales manager.** Credit notes (full or partial) and void before payment, each requested by billing staff and approved by the sales manager, never the requester |
| D-B7 | Payment methods by hand | Bank transfer, cash, cheque, Zelle, other; reference required except cash; partial allowed; overpayment refused; reversal with reason |
| D-B8 | Unpaid renewal | Past due the day after the due date; blocking is Slice 6 |
| D-B9 | Paid-through date | Stays as built in Slice 3: the period moves at renewal; unpaid shows as Past due |
| D-B10 | PDFs | Made at issue in the tenant's language, stored once with a hash; paper size a setting (A4 until set) |
| D-B11 | Receipts | A4 receipt per payment with its own number, emailed |
| D-B12 | Issuer details on invoices | **Owner: as proposed.** Bring the company profile (legal name, address, tax ID, contacts, logo file) into Slice 4 instead of Slice 8 |
| D-B13 | Emails | Invoice on issue and receipt on payment to the billing contact, in the tenant's language |
| D-B14 | Licences | Billing never creates licences; Slice 7 reads the subscription |
| D-B15 | Card payments and refunds | Slice 5; Slice 4 records hand-collected payments and refunds owed |
| D-B16 | Tenant tax ID | Optional tax ID and exemption on the tenant company |

## 10. Build notes (2026-10-16)

Built on `8c3ccca`. What differs from the sections above, and why:

- **Overdue means the due day has ended.** An invoice is overdue (and its subscription Past due) from 00:00 UTC on the day after its due date, so an invoice due on issue is never overdue on the day it was issued (D-B8). One rule, `PlatformInvoice.OverdueCutoff`, is used by the domain, the list filters, the tenant balances and the clock.
- **Tax label:** one label per rate (for example "GST+QST"), printed as entered in every language. Labels per language can follow if the owner wants them.
- **Payments list:** payments are listed and recorded on the invoice page; no separate payments page was built.
- **Record offline payment (Slice 3)** is no longer offered on the tenant's subscription: payments are recorded on the invoice, and paying the invoice the subscription waits for activates it. The Slice 3 use case is kept until the registration flow (Slice 5) no longer needs it.
- **`GET /registration/invoice`** (section 6) is not built yet; it lands with card payments in Slice 5, where the public site first needs it.
- **Documents:** PDFs are made with QuestPDF (Lato font bundled, no font download at run time). The licence comes from `PlatformDocuments:QuestPdfLicense` (Community until set). Downloads go through `GET /billing/documents/{invoice|credit_note|receipt}/{id}`, signed-in staff with `platform.billing.view` only, never cached.
- **Numbers and settings are read inside each transaction**, never cached on a service: in the console a service lives as long as the browser connection, so a cached copy went stale (found during verification).
- **Demo data:** `--seed-demo-tenants` (and `PlatformDemoData:SeedOnStartup`) also adds the company profile, five tax rates and an invoice for each paying demo subscription, three paid in full and one in part.

