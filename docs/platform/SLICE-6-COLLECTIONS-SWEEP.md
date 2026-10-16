# Slice 6: Collections - V3 behaviour sweep

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Sweep done, 2026-10-16.** Decisions in section 6 are proposals for the owner; answers go into the design note |
| Source | V3 `ControlPlane` (read-only): `src/Vixly.Platform.Application/UseCases/Collections` (6 units and 4 empty folders), `Services/Collections`, `Services/Tenants/TenantSuspensionStateService`, `Services/Subscriptions/TenantSubscriptionAccessRecoveryService`, `UseCases/Settings/Platform/*CollectionsSettings`, `UseCases/PaymentCollection/ProcessAutomaticInvoiceCollections`, `Domain/Entities/Collections`, `Domain/Enums/Collections`, `Domain/Entities/Settings/Platform/PlatformCollectionsSetting`, `Infrastructure/Services/Collections`, console `BillingOperations/Collections` and `Settings/PlatformSettings/Components/CollectionSettings`, `scripts/database/*CollectionPaymentExtensions*`, payment recovery tests |
| Rule | Replicate behaviour, not code. Classification as in Slices 1 to 5: Active, Duplicate, Dead, Disconnected-wanted, Moved |
| Feeds | Slice 6 design note; Slice 3 (new status Blocked for non-payment, D-S6, D-S14, D-S17); Slice 4 (payment clears the block); Slice 5 (retry charges, D-P13); Slice 7 (licences while blocked); Slice 8 (collections settings move here); Slice 9 and Operations (access enforcement) |

## 1. How the evidence was gathered

Each collections use case, service, background job and console page was traced to the pages and use cases that inject it, and the entities, enums, EF configurations and staging SQL were read for their rules. The full V3 snapshot holds all collections code; the payment extract added the extension migration script and the payment-recovery tests. Neither tree has repository implementations, an Infrastructure DI file or a host, so "nobody calls it" means nothing in the snapshot does. The DI file says a hosted service runs the collections engine; that service is not in the snapshot. V4 code under `src/Platform` was read to see what Slice 6 can reuse. Detailed notes with file and line evidence: `v3-collections-sweep.md`.

V3 has one "collections engine": a daily job that opens a case per delinquent tenant, emails up to three reminders per overdue invoice, then suspends the tenant **and** its subscription, and reactivates both once nothing is overdue. Card retries (days 0, 3, 7) are a separate job that knows nothing about the case.

## 2. Use cases (6, plus 7 outside the folder that drive it)

| V3 use case | Called from | Status | V4 outcome |
|---|---|---|---|
| ProcessCollections | Nobody (registered only; the hosted service is missing) | Disconnected-wanted | Collections clock in the Worker: open case, reminders, retries, block, extension expiry; one tenant per commit (D-C3, D-C18) |
| ListCollectionCases | Collections workspace | Active; whole list in memory, two extra queries per case, no permission | Paged case list with filters (D-C14) |
| GetCollectionCase | Collections workspace | Active; no permission; shows raw error text | Case page with history (D-C14, D-C15) |
| GetCollectionsDashboard | Collections workspace | Active; counts only, loads every case with all actions | Dashboard from aggregate queries, amounts by currency (D-C13) |
| GrantPaymentExtension | Collections workspace "Grant payment extension" | Active; **no permission check**, no step-up, no length or count limit; only after suspension | Extension with permission, step-up, limits; also before the block (D-C7) |
| ClearMonetarySuspension | Collections workspace "Clear monetary suspension" | Active; manual fallback when payment recovery failed; checks a subscription permission | Gone: payment clears the block in its own commit; "check again now" for staff (D-C6, D-C8) |
| Get/UpdatePlatformCollectionsSettings (Settings) | `/settings/platform-settings` | Active; no defaults, no version check, gate only in the page | Collections settings with defaults and step-up, built in this slice (D-C12) |
| CollectionPolicyEvaluator (`Services/Collections`) | Nobody | Dead; Duplicate of the copy inside ProcessCollections | One evaluation rule in the domain |
| TenantSuspensionStateService (Tenants) | Engine, extension, clear, tenant lifecycle, payment recovery | Active; **suspends the tenant account for money** | Gone for money: Blocked for non-payment on the subscription only; account suspension stays Slice 2/3 (D-C4) |
| TenantSubscriptionAccessRecoveryService (Subscriptions) | RegisterPayment, RegisterTenantRegularPayment | Active; runs after the payment commit, in a second commit, ignores the auto-reactivate setting | Slice 4 payment applier clears Blocked in the payment's commit (D-C6) |
| ManageTenantLifecycle "reconcile subscription access" and the subscription access panel | Collections workspace | Active; loads every tenant into a drop-down | Gone: staff never unblock money through tenant lifecycle |
| ProcessAutomaticInvoiceCollections (Payment collection) | Nobody (registered only) | Disconnected-wanted; Moved | Day 0 is Slice 5 (one attempt); days 3 and 7 are Slice 6 retries through the Slice 5 charge path (D-C9) |
| ReactivateCollectionCase, RecordCollectionContact, ResolveCollectionCase, RetryCollectionPayment | Empty folders | Dead (never written) | Contact notes kept as a new action (D-C15); resolve and reactivate are automatic; retry is Slice 5 "charge saved card" |
| Collections emails (`ICollectionsNotificationService`, extension notice, recipient resolver) | Engine, extension | Active; English, company email, no pay link, sent before commit | Outbox mail handlers, billing contact, tenant language, pay link (D-C11) |

## 3. Entities and rules found

| V3 | Contents | V4 proposal |
|---|---|---|
| `TenantCollectionCase` | One unresolved case per tenant (filtered unique index on tenant where status is not Resolved); pinned to one subscription id; states Open, Suspended, ResolvedPendingReactivation, ExtensionActive, Resolved; opened, suspended, resolved, reactivated dates; extension end, granted by, reason. No row version | `PlatformCollectionCase`: tenant, subscription, stage (None, Reminded 1, Reminded 2, Final warning, Blocked), status (Open, Extension, Closed), opened and closed, close reason (paid, credited, subscription ended), extension end, extension count, row version. One open case per tenant (D-C1) |
| `CollectionAction` | Type (first, second reminder, final warning, suspended, resolved, reactivated, extension granted, expired, re-suspended), outcome Completed or Failed, invoice id (reminders only), free-text details up to 2000 with operator GUIDs and exception text. Unique completed (case, type, invoice) for types 1 to 6 | `PlatformCollectionCaseEvent`: append-only history, code, arguments (ids, dates, amounts, codes), actor, time; written in the same commit as the change, with an audit entry. No free text from exceptions (D-C16) |
| `PlatformCollectionsSetting` | Enabled; first, second, final-warning and suspend days after overdue (each greater than the last, no upper bound); automatically reactivate. No defaults; missing row means off | `PlatformCollectionsSettings` singleton with defaults, bounds, row version (D-C12) |
| `CollectionPolicyEvaluation` | Days overdue as UTC date difference; flags per threshold | Domain rule using Slice 4's `OverdueCutoff` |
| Card retry stages | Days 0, 3, 7 after due, hard-coded; key `invoice:{id}:automatic:day-{n}` | Retry days in settings (3, 7); one card transaction per invoice and stage (D-C9) |

V3 values fixed in code: reminder failure cooldown 24 hours; extension form default 7 days with no maximum; clear-suspension reason 5 to 500 characters; retries on days 0, 3, 7.

## 4. Behaviour found

### States and what suspension does

- A case opens when a tenant has an Issued or part-paid invoice with a balance after its due date. All overdue invoices of the tenant join the same case.
- "Monetary suspension" sets the **tenant** to Suspended and the subscription to Suspended. Portal entry then fails with `tenant_suspended`, the same result as a compliance or fraud suspension. The tenant lifecycle page refuses to reactivate while collections owns the suspension.
- An extension restores tenant and subscription to Active until a date; during it the engine does nothing for that tenant. When it expires the case reopens and the tenant is suspended again in the same run if still overdue.
- When nothing is overdue the case resolves. A payment reactivates at once (recovery service); the daily engine reactivates only if "automatically reactivate" is on, otherwise staff press "Clear monetary suspension".
- Device licences are not touched; they stop working only because portal validation checks the tenant and subscription first.

### Schedule (days after the due date)

| Step | V3 |
|---|---|
| Card attempts | 0, 3, 7 (separate job, not linked to the case) |
| First, second reminder, final warning | Typed by staff; no defaults |
| Suspension | Typed by staff; must be after the final warning |
| Catch-up | Reminders: only the highest due stage is sent. Card attempts: each missed stage on consecutive runs |

### Emails

Reminder 1, reminder 2, final warning, "subscription suspended", "payment extension confirmed". English, sent to the company email, one email per overdue invoice and stage, with invoice number, due date, days overdue and balance. No pay link, no payment instructions, no block date. No email when access is restored or an extension is about to end.

### Console screens found

| V3 page | State | V4 |
|---|---|---|
| `/billing/collections` workspace | Dashboard tiles, case list with client-side search and status filter, case detail, timeline, subscription access panel, extension and clear actions. Page requires only sign-in | Collections page (paged list, filters), case page (history, invoices, actions), tenant page Collections section; every use case gated (D-C13, D-C14) |
| Dashboard | Counts by state, cases with failed actions, oldest case age, last activity | Amounts overdue by currency, ageing, blocked tenants, extensions ending, retries and emails failing (D-C13) |
| Settings, Collections tab | Enable, four day fields starting at 0, auto-reactivate | Collections settings page with defaults and step-up (D-C12) |

## 5. Defects and risks seen in V3 (fixed by design in V4)

1. Non-payment suspends the tenant account, the status staff use for compliance, security or fraud (contradicts D-S17).
2. A tenant with no company email is suspended without any reminder; the final warning never states the suspension date.
3. Anyone signed in to the console can grant an extension: no permission, no step-up, no limit on length or number of extensions. Extensions are possible only after suspension, so staff cannot record a promise to pay in time.
4. Clearing a suspension checks a subscription permission; the list, case and dashboard queries have no permission check.
5. Reminder emails are sent before the action is saved: a failed save sends them again. No outbox; a failed suspension notice is silently dropped.
6. One email per overdue invoice per stage; a tenant with three invoices gets three reminders the same day.
7. English only, invariant date and number formats, sent to the company email only, no pay link or payment instructions.
8. The extension email shows the staff's internal reason and lists invoices not yet due.
9. A first reminder on day 0 is allowed by the settings but always refused by the email service.
10. No default settings and no upper bounds; settings are saved last-write-wins.
11. Two rules for reactivation: payment always reactivates, the daily engine only when a setting is on. Payment and access recovery run in two commits; a failure in the second is only logged.
12. No row version on the case: the engine, payment recovery, extension and clear can overwrite each other. Case creation and the first reminder commit separately.
13. Unbounded lists: every overdue invoice and every open case with all actions per run, every tenant in a drop-down, every invoice in the extension email.
14. Exception messages and operator GUIDs stored as free text and shown to staff.
15. Card retries ignore the case: they run during extensions, send their own emails on reminder days, and do not stop at the block.
16. A case pinned to an old subscription id is skipped at resolution and stays open for ever after a subscription change.
17. Wall-clock `DateTime.UtcNow` in every use case; the extension deadline is the end of the chosen day in the server's time zone.
18. Leftovers: a second, unused evaluator; a redundant index; the reactivation date reused for extensions; a "suspension failed" record never written; four empty use-case folders.
19. No tests for the engine, extensions, settings or emails; only payment recovery is tested.

## 6. Decisions

Proposals for the owner; answers go into the Slice 6 design note.

| # | Question | Proposal |
|---|---|---|
| D-C1 | One case per tenant or per invoice? | One open case per tenant (as V3), holding every overdue invoice of that tenant. Opened by the billing clock's "invoice past due" step, closed when no invoice is overdue (paid, credited or voided) or the subscription ends. Filtered unique index on open case per tenant, row version. A later debt opens a new case |
| D-C2 | Which invoices count? | Every Issued invoice with a balance after its due date (renewal, change, registration, one-time charges). Days count from Slice 4's overdue rule (00:00 UTC after the due date). An invoice with a credit-note request waiting for approval does not move the case to the next stage until the request is decided |
| D-C3 | Timeline and defaults | Day 1 after due: reminder 1. Day 7: reminder 2. Day 14: final warning naming the block date. Day 21: blocked. Card retries on days 3 and 7 (D-C9). All in settings. Catch-up after downtime sends only the latest due step, never several on one day, and the block happens only if the final warning was sent at least 3 days earlier (the block date moves if needed) |
| D-C4 | What does blocking do? | Slice 6 adds subscription status **Blocked for non-payment** (value 6, appended) with the status before it remembered. The tenant account stays Active; staff never suspend for money (D-S17). `AccessAt` returns None. Platform publishes `platform.subscription.status_changed.v1`; Operations enforces when its plane is built (Stage 3, contract in Slice 9): tenant staff cannot sign in, owner and billing contact see a "payment required" page with the pay link. Public site and data stay; nothing is deleted. Account suspension (D-S10) while blocked keeps Blocked as the status to return to |
| D-C5 | Pending payment after a trial or first period | Replace the computed D-S14 rule with the same status: after `UnpaidReadOnlyDays` (7) of Pending payment the collections clock sets Blocked for non-payment. One status for every unpaid block; reminders apply the same way |
| D-C6 | How does payment clear the block? | Automatically, in the commit that records the payment (Slice 4 `PlatformPaymentApplier`), when no other invoice of that subscription is overdue: Blocked goes back to the status before it (Active), the case closes on the `invoice_paid` message. Part payment does not clear it. A credit note that clears the balance clears it the same way. No "automatically reactivate" setting and no manual "clear" action |
| D-C7 | Extensions: who, how long, limits | Billing staff with `platform.collections.extend` and step-up. Allowed before or after the block ("promise to pay"). Up to 14 days each (setting), at most 2 per case (setting); a third needs `platform.collections.extend_beyond_limit` (sales manager). Reason required, internal only. During an extension the subscription is not blocked (a blocked one returns to Past due), reminders pause, card retries continue. At the end, if still overdue, the final warning step resumes with a new block date at least 3 days later. Tenant gets an email with the date only |
| D-C8 | Pausing and manual overrides | No manual block and no manual unblock. Staff with `platform.collections.manage` can add a contact note and press "check again now" (re-evaluates one tenant). Disputes use the extension. Collections is paused automatically while the tenant account is Suspended, and for tenants Draft or Pending activation |
| D-C9 | Card retries | Only for tenants who opted in to automatic charging with a valid default card, and only when Slice 5 marked the failure retryable. Not after "action required" (the tenant must act on the link). Slice 6 calls Slice 5's saved-card charge path with a new origin `Retry` (value 7), amount = balance, one transaction per invoice and retry day (unique), so a lost answer is asked again with the same key. No retries after the block or during a hold by a pending credit note |
| D-C10 | Tenants paying by hand (no card, Zelle, cheque, transfer) | Same reminders and block. The email carries the payment instructions from billing settings in the tenant's language and a pay link when cards are accepted. No retries. A cheque in the post is handled by an extension. Recording the payment (Slice 4) clears the block |
| D-C11 | Emails | Reminder 1, reminder 2, final warning (with block date), blocked, extension granted (date only), extension ends in 2 days, access restored. One email per tenant and step listing all overdue invoices with balances, total and pay link. Sent to the billing contact (owner when none); final warning and blocked also to the owner. Outbox, tenant language (en, fr, es), localised dates and amounts. No reminder on a day Slice 5 already sent a card-failure email for that invoice |
| D-C12 | Settings | `PlatformCollectionsSettings` singleton built in this slice (move from Slice 8): enabled (default on), reminder days 1, 7, 14, block day 21, retry days 3 and 7, minimum warning before block 3, extension maximum 14 days and 2 per case, Pending payment read-only days 7 (moved from configuration). Bounds: each step after the previous, block day at most 60. Row version, `platform.collections.configure` with step-up, history row. Changes apply to future steps only |
| D-C13 | Dashboard metrics | Open cases by stage; blocked subscriptions; amount overdue by currency; ageing (1-30, 31-60, 61-90, over 90 days); extensions ending in 7 days; cases blocked over 30 days; retries and emails failed in 7 days; amount recovered in 30 days. From aggregate queries, not by loading cases |
| D-C14 | Console | Collections page: paged list (P-1), filters stage, status, currency, extension, search by tenant; case page: invoices, history, notes, actions; Collections section on the tenant page. Every query and command checks its permission in the use case |
| D-C15 | History and audit | `PlatformCollectionCaseEvent` for every step (opened, reminder sent, retry, final warning, blocked, extension granted and ended, note, unblocked, closed), written in the same commit as the change, plus an audit entry for staff actions. Codes and ids only; no exception text; emails are tracked by the outbox, not by the case |
| D-C16 | Permissions | `platform.collections.view`; `platform.collections.manage` (notes, check again); `platform.collections.extend` (step-up); `platform.collections.extend_beyond_limit` (step-up); `platform.collections.configure` (step-up). Billing role: view, manage, extend. Sales manager: beyond limit. Administrator: all |
| D-C17 | Licences (Slice 7) | V3 does not couple them: licences stay assigned while suspended and fail only through portal validation. Keep that: blocking releases or revokes no licence; Slice 7 device and portal checks read the subscription access level (None while blocked). Licences stay counted and billed |
| D-C18 | Clock and concurrency | A Worker job (ADR-0007), run in the subscription clock host until the Worker opens. Batches of 50 tenants, one tenant per commit, injected clock, row version on case and subscription; a second host loses the race and skips. Handles `card_payment_failed` and `invoice_paid` messages from the outbox |
| D-C19 | Long blocks | No automatic ending. The dashboard lists subscriptions blocked longer than 60 days (setting); staff decide with Slice 3's "end now" (step-up). Ending the subscription closes the case; the debt stays on the invoices |
