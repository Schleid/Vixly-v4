# Slice 5 update: Zelle at checkout

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Date | 2026-10-16 |
| Asked by | Owner, 2026-10-16: "keep accepting Zelle ... configuration for zelle data, like email, zelle tag, phone, company name and QR code that will be shown at check out ... A zelle payment must be verified by staff." |
| Built on | Slice 7 as delivered |
| Status | **Built and verified; waiting for owner sign-off** |

## 1. Outcome

- Vixly staff keep one set of Zelle details in the console (Payments settings): on or off, recipient name, Zelle email, Zelle tag, phone, and a QR code image.
- When Zelle is on, checkout shows them for every open US-dollar invoice: pay links from emails and the self-registration payment step.
- The customer pays from their own bank, then reports it at checkout: amount, day sent, sender name, Zelle confirmation code.
- A report never pays the invoice. Staff check the bank, then **confirm** (a Zelle payment is recorded through the normal payment rules: receipt numbered and emailed, subscription and registration move on) or **reject** with a reason the customer receives by email.

## 2. Data (schema `platform`)

| Table | What |
|---|---|
| `ZelleAccounts` | One row: enabled, recipient name, email, tag, phone, QR image (PNG/JPEG, at most 256 KB) and its version, who changed it, row version |
| `ZelleClaims` | Customer reports: tenant, invoice, amount (USD), day sent, sender, confirmation code, source (pay link or registration), status (waiting, confirmed, rejected), amount and date received, payment link, reason for the customer, internal note, who decided, row version. One waiting report per invoice (filtered unique index) |

Migration `Zelle`; the global script is rebuilt with it.

## 3. Rules

- Turning Zelle on needs a recipient name and at least one of email, tag or phone. Email and phone are checked; a tag has no spaces.
- Zelle is offered only for open invoices in US dollars (Zelle moves dollars only).
- A report: amount above zero, at most two decimals, not above the balance; sent today or within the last 30 days; sender and code required.
- One report waits per invoice; at most 3 reports per invoice in all (rejected ones count), after which the customer is asked to contact Vixly.
- Confirming uses the same permission and step-up as recording a payment by hand (`platform.billing.record_payments`). Staff can change the amount actually received; the payment rules refuse more than the balance. The Zelle code becomes the payment reference.
- Rejecting needs a reason (emailed to the customer); the internal note never leaves the console.
- Settings and the QR image need `platform.payments.configure` and step-up. Every change is audited.
- **Collections:** a waiting report pauses the timeline like a credit-note request, so a tenant is not blocked while staff check the bank. Staff deciding the report resumes it.
- **Pay links in emails:** invoice and collections emails now carry the pay link when card payments **or** Zelle are available for the invoice (before, cards only). The link reads "Pay online".

## 4. Use cases

Console: get and save the Zelle details; set or remove the QR image; list reports (waiting by default); get a report; confirm; reject.
Public (anonymous): Zelle details inside the pay-link and registration-invoice answers; the QR image; report a payment from a pay link; report a payment from the registration.

## 5. Public API (for the Public plane's checkout page)

| Call | Answer |
|---|---|
| `GET /payment-links/{token}` and `GET /registration/invoice` | New field `zelle`: `recipientName`, `email`, `tag`, `phone`, `qrCodeUrl` (relative to the API, versioned), `waiting` (amount, sentOn, reportedUtc) and `canReport`. Null when Zelle is not offered |
| `GET /payments/zelle/qr?v=N` | The QR image while Zelle is on (cached one hour; a new image gets a new URL); 404 otherwise |
| `POST /payment-links/{token}/zelle` | Body `amount`, `sentOn` (date), `senderName`, `reference`. 204, or a problem with the error code |
| `POST /registration/zelle` | Same body, with the registration bearer token |

Rate limits: the same as viewing a link and starting a checkout. The checkout page itself belongs to the Public plane, which is not built yet; until then these calls are the contract.

## 6. Console

- **Payments settings:** a Zelle card with the details, a QR preview, upload and remove.
- **Zelle payments** (menu, Commercial): reports, waiting first, with status filter and search on invoice, sender or code.
- **Zelle report page:** what the customer reported, the invoice balance now, confirm (amount, date received, note) and reject (reason for the customer, note).
- **Collections case page:** the "on hold" line now also names a Zelle report.

## 7. Emails (tenant billing contact, tenant's language)

"We received your Zelle payment notice" when reported; "We could not confirm your Zelle payment" with the reason when rejected. A confirmed report sends the usual receipt.

## 8. Verification

Release build: no errors, no warnings. All tests pass:

| Test project | Before (Slice 7) | After |
|---|---|---|
| Platform.Domain.Tests | 277 | 281 |
| Platform.Application.Tests | 198 | 204 |
| Platform.Infrastructure.Tests (with SQL Server) | 52 | 53 |
| Architecture.Tests | 5,116 | 5,339 |
| Api, Web, Shared.Kernel | 8, 31, 81 | unchanged |

End-to-end on a fresh Development database with card payments **off**:

| # | Check | Result |
|---|---|---|
| 1 | Turn Zelle on without a way to send | Refused with a clear message |
| 2 | Save details and upload a QR (step-up) | Saved; QR preview shown |
| 3 | "Email again" on an open USD invoice | Email carries "Pay online: …/pay/…" although cards are off |
| 4 | Pay-link answer | `cardPaymentsAvailable: false`, Zelle details and `qrCodeUrl`; QR served as image/png, cached |
| 5 | Report more than the balance | 400 `zelle_amount_invalid` |
| 6 | Report the balance; report again | 204; then 400 `zelle_claim_waiting`; invoice still open; checkout shows the waiting report; "We received your Zelle payment notice" email |
| 7 | Staff queue and report page | Report listed as "Waiting to be checked" |
| 8 | Reject (step-up) | Customer email with the reason; the internal note is not in it |
| 9 | Report again, then confirm (step-up) | Invoice Paid; payment method Zelle with the code as reference; receipt emailed; checkout shows the invoice settled and no Zelle offer |
| 10 | Menu and outbox | "Zelle payments" under Commercial; no failing outbox message |

## 9. Decisions taken while building (please confirm)

- Zelle is offered for **US-dollar invoices only**.
- At most **3 reports per invoice**; a send date at most **30 days** back.
- A waiting report **pauses collections** until staff decide.
- Staff can confirm a **different amount** than reported (for example a partial payment); never above the balance.
- The older free-text payment instructions in Billing settings stay as they are (they still print on invoices).
