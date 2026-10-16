# Slice 6: Collections - design note

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Approved by the owner, 2026-10-16** (section 9). Built; see the [verification record](SLICE-6-VERIFICATION.md) |
| Inputs | [Sweep](SLICE-6-COLLECTIONS-SWEEP.md), Slice 3 design (D-S6, D-S10, D-S14, D-S17), Slice 4 design (overdue rule, payments, credit notes), Slice 5 design (D-P13 hand-over, card charge path), ADR-0007 (Platform Worker) |
| Depends on | Slices 3 to 5 (built) |

## 1. Outcome

When a tenant does not pay, Vixly follows one clear, configurable path instead of suspending the account. Each overdue tenant gets one collections case. The tenant gets reminders and a final warning that names the block date, and the saved card is retried if the tenant agreed to automatic charging. If the bill is still unpaid on the block date, the subscription becomes **Blocked for non-payment**. The tenant's own staff are limited to read-only, then to no access, as settings say. The tenant's **customers are never affected**. Paying clears the block at once. Billing staff see every case, can give more time (extensions) and add notes. A dashboard shows what is overdue and what was recovered. The account itself stays in good standing: Suspended stays for conduct only (D-S17).

## 2. Data (schema `platform`)

| Entity | Table | Notes |
|---|---|---|
| `PlatformCollectionsSettings` | `CollectionsSettings` | One row. Enabled; reminder days (1 and 5); final warning day (10); block day (14); card retry days (3 and 7); minimum notice between final warning and block (3 days); read-only days after the block before no access (7; 0 = no access at once; empty = stay read-only); extension length (14 days) and number per case (2); Pending payment read-only days (7, moved from configuration); long-block flag (60 days). Row version; changes apply to future steps only |
| `PlatformCollectionCase` | `CollectionCases` | Tenant, subscription, opened at, oldest unpaid due date, stage (Opened, Reminder 1, Reminder 2, Final warning, Blocked), planned block date, extension until, extensions used, hold reason (credit-note request waiting), last checked, closed at and why (paid, credited, voided, subscription ended). **At most one open case per tenant** (filtered unique index); row version |
| `PlatformCollectionCaseEvent` | `CollectionCaseEvents` | Append-only history: opened, reminder sent, final warning, card retry (invoice, day, transaction), blocked, read-only ended (no access), extension granted (until, internal reason) and ended, note, checked again, unblocked, closed. Codes and ids only, no provider or exception text. DENY UPDATE, DELETE to the app login |
| Subscriptions (changed) | `Subscriptions` | New status **Blocked for non-payment** (value 6). Remembers the status before the block and when the block started |
| Card transactions (changed) | `CardTransactions` | New origin **Retry** (value 7) |

## 3. Rules

### Which tenants and invoices (D-C1, D-C2)

- A case opens when a tenant first has an Issued invoice with a balance after its due date (Slice 4's rule: overdue from 00:00 UTC the day after the due date). Days in the timeline count from the oldest unpaid invoice's due date.
- It holds every overdue invoice of that tenant: renewals, changes, registration, one-time charges.
- It closes when nothing is overdue (paid, credited or voided) or the subscription ends. A later debt opens a new case.
- While a credit-note request on one of its invoices waits for approval, the case does not move to the next stage.
- Collections waits while the tenant account is Suspended (conduct, D-S10) and for tenants not yet active.

### Timeline (D-C3; owner: 14 days)

| Day after due date | Step |
|---|---|
| 1 | Reminder 1 |
| 3 | Card retry (opted-in tenants only) |
| 5 | Reminder 2 |
| 7 | Card retry (opted-in tenants only) |
| 10 | Final warning, with the block date |
| 14 | Blocked for non-payment |

- Every day is a setting. Each step must come after the one before; the block day is at most 60.
- After downtime the clock sends only the latest step due, never several on one day.
- A block always follows a final warning at least 3 days earlier (setting); if needed the block date moves.

### What blocking does (D-C4; owner: flexible, customers never blocked)

- The subscription becomes **Blocked for non-payment**; the tenant account stays Active. Nothing is deleted and no licence is released (D-C17).
- **Level 1, read-only** (for the read-only days in settings, default 7): the tenant's staff can sign in and read but change nothing. The owner and the billing contact first see a "payment required" page with the amount and the pay link.
- **Level 2, no access**: only the owner and the billing contact can sign in, and they see only the "payment required" page.
- Settings choose the levels: read-only days 0 means no access at once; leaving them empty means read-only until paid.
- **Customers are never blocked.** Everything customer-facing keeps working at both levels: the customer portal, tracking, booking, quotes, customer notifications and the public site. The limits apply only to the tenant's own back office.
- Platform publishes the status change and the access level (Full, Read-only, Payment required) through the existing subscription status message and the entitlements answer. The Operations plane enforces it when it is built (Stage 3); this rule is part of that contract.
- If the account is suspended while blocked, the subscription keeps Blocked as the status to return to.

### Pending payment (D-C5)

After the Pending payment read-only days (7), the collections clock moves the subscription to Blocked for non-payment as well, so there is one unpaid status with the same levels, emails and clearing. This replaces the computed rule of D-S14.

### Clearing (D-C6)

- Recording a payment (card or by hand) or issuing a credit note that leaves no other invoice of the subscription overdue returns the subscription to the status before the block, in the same commit. The case closes and the tenant gets "access restored".
- A part payment does not clear the block.
- There is no manual unblock and no "reactivate automatically" setting: paying is the only way out, apart from an extension.

### Extensions (D-C7; owner: 14 days, 2 per case)

- Billing staff with `platform.collections.extend` and step-up give a date up to 14 days ahead (setting), with an internal reason, at most 2 times per case (setting). A third needs `platform.collections.extend_beyond_limit` (sales manager).
- Allowed before or after the block. While it runs, reminders pause, a blocked subscription goes back to the status before the block, and card retries continue.
- When it ends unpaid, the case resumes at the final warning with a new block date at least the minimum notice later.
- The tenant gets an email with the new date only; the reason stays internal.

### Card retries (D-C9)

- Only for tenants with automatic charging on and a valid default card, and only when Slice 5 marked the failure retryable. Not after "action required": the tenant must act on the link.
- Slice 6 calls Slice 5's saved-card charge path with origin Retry, for the invoice balance. One retry per invoice and retry day; a lost answer is asked again with the same key.
- No retries once blocked or while a credit-note request holds the case.

### Tenants who pay by hand (D-C10)

The same steps and block. Emails carry the payment instructions from Billing settings in the tenant's language, and a pay link when card payments are on for the currency. No card retries. A cheque in the post is handled with an extension.

### Emails (D-C11)

Reminder 1, reminder 2, final warning (with the block date), blocked, extension granted (date only), extension ends in 2 days, access restored. One email per tenant and step, listing every overdue invoice with its balance, the total and a pay link. Sent to the billing contact (the owner when there is none); final warning and blocked also go to the owner. Sent through the outbox, in the tenant's language (en, fr, es), with local dates and amounts. No reminder for an invoice on a day Slice 5 already sent a card-failure email for it.

### Clock and concurrency (D-C18)

A collections job runs in the subscription clock host until the Platform Worker opens (ADR-0007). It works in batches of 50 tenants, one tenant per commit, with an injected clock and row versions on case and subscription, so a second host loses the race and skips. Outbox handlers react to "invoice paid", "card payment failed" and credit-note decisions.

### Long blocks (D-C19)

No automatic ending. The dashboard lists subscriptions blocked longer than 60 days (setting); staff decide with Slice 3's "end now" (step-up). Ending the subscription closes the case; the debt stays on the invoices.

## 4. Permissions (D-C16)

| Permission | Step-up | Use |
|---|---|---|
| `platform.collections.view` | | Dashboard, cases, tenant section |
| `platform.collections.manage` | | Add a note, check a tenant again now |
| `platform.collections.extend` | Yes | Give an extension within the limits |
| `platform.collections.extend_beyond_limit` | Yes | An extension past the per-case limit |
| `platform.collections.configure` | Yes | Collections settings |

Billing clerk: view, manage, extend. Sales manager: view, extend beyond the limit. CFO: view, configure. Administrator: all.

## 5. Use cases

Get and update collections settings; collections dashboard; list cases (paged, filters: stage, status, currency, extension, search); get a case (invoices, history, notes, actions); a tenant's collections summary; add a note; check a tenant again now; grant an extension; the collections clock (open, advance, retry, block, move read-only to no access, end extensions, close); handlers for invoice paid, card payment failed and credit-note decisions; clear the block on payment (Billing).

## 6. Contract for the Operations plane

- Entitlements gain the access level (Full, Read-only, Payment required) and, when blocked, the block date, the amount due by currency and whether a pay link can be offered.
- "Payment required" means: tenant staff cannot sign in, except the owner and the billing contact, who see only the payment page.
- Customer-facing features ignore the block entirely.

## 7. Console screens

- **Collections page** (Commercial menu): dashboard tiles (open cases by stage, blocked subscriptions, amount overdue by currency, ageing 1-30, 31-60, 61-90 and over 90 days, extensions ending in 7 days, long blocks, recovered in 30 days, failed retries in 7 days) above the paged case list.
- **Case page**: tenant, stage, block date or extension, overdue invoices with balances, history with notes, and actions (note, check again, extension).
- **Tenant page**: a Collections section with the open case, if any.
- **Collections settings** (System menu).

## 8. Done when (in addition to the slice gate)

- No path suspends an account for money; Blocked for non-payment is set and cleared only by the rules above.
- A tenant's customers are never limited by a block.
- One open case per tenant; one email per tenant and step; never several steps on one day; never a block without a final warning at least the minimum notice before it.
- Payment that leaves nothing overdue restores access in the same commit; part payment does not.
- Retries only with consent and never twice for the same invoice and day.
- Extensions within limits, with step-up and history; the reason never reaches the tenant.
- History and audit in the same commit; texts in en, fr and es; every list paged.

## 9. Owner decisions

Settled by the owner on 2026-10-16:

- **Timeline:** 14 days (reminders on days 1 and 5, final warning on day 10, block on day 14, card retries on days 3 and 7), all in settings.
- **Blocking:** flexible: read-only first for a set number of days, then no access, as settings say. During the block the owner and the billing contact always get the payment page; other staff read only while read-only lasts.
- **Customers:** the tenant's customer-facing operations are never blocked.
- **Extensions:** 14 days each, 2 per case, a third by the sales manager.
- **Everything else:** as proposed in the [sweep](SLICE-6-COLLECTIONS-SWEEP.md), section 6.

## 10. Build notes

Decisions taken while building; none changes an owner decision.

- **Access levels.** The subscription access answer gains **Payment required** (value 3) next to Full, Read-only and None. A blocked subscription is Read-only for the read-only days in settings, then Payment required. A Pending payment subscription past its read-only days is now Payment required too; Slice 3 answered None there. None stays for Suspended (account) and Ended.
- **Read-only days come from Collections settings.** The configuration key `PlatformSubscriptions:UnpaidReadOnlyDays` is no longer read. It is still in the console's `appsettings.json`, which this slice does not edit because the Frontend team shares the file. The dev team can delete it.
- **Pending payment is blocked only with something to pay.** Only when one of its invoices is overdue (found in the end-to-end run). Without an invoice, for example when the first invoice could not be issued, the tenant is never blocked with nothing it could pay.
- **Days count from the oldest unpaid due date when the case opened.** Paying the oldest invoice of several does not restart the timeline.
- **One step per day.** After downtime only the latest step due is taken. The block date moves so that it always comes at least the minimum notice after the final warning.
- **Card retries.** One per invoice and retry day, claimed in the history before the card is charged (filtered unique key), so two hosts never retry twice. After downtime only the latest retry day due is tried. A retry needs the last charge to have failed for a reason that is not final (an expired, lost or stolen card is never retried).
- **A blocked subscription does not renew,** and plan changes are refused while it is blocked. After payment the period continues and the next renewal runs as usual.
- **A credit note that settles the bill** clears the block or Past due on the job's next run (every minute by default), not in the credit note's own commit. A payment clears it in the payment's own commit, as designed.
- **Access restored.** When an extension already lifted the block, paying sends no "access restored" email; the tenant had full access since the extension.
- **Reminders and card-failure emails on the same day.** Not built as a rule. With the default days they never fall on the same day: the automatic charge happens on the due date, reminders from day 1.
- **Settings history** is kept as an audit entry with the values before and after; no separate history table.
- **Quiet runs write nothing.** A tenant whose case has nothing new costs one read per run, not a write.
- **Entitlements** (contract for Operations, section 6) gain `NonPayment`: block date, amount due per currency, and whether a pay link can be offered.
- **Email subjects** carry the block date or the extension date in the tenant's language (found in the end-to-end run).
