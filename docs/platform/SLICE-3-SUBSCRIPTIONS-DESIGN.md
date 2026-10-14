# Slice 3: Subscriptions - design note

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Built and verified, 2026-10-14; waiting for owner sign-off.** D-S2, D-S4, D-S7 and D-S12 settled by the owner; the rest as proposed, approved with "you can start building". Build notes in section 10. **Section 11 (Slice 3b: discounts, promotions, offer links, one-time charges) approved by the owner, 2026-10-14** |
| Inputs | [Sweep](SLICE-3-SUBSCRIPTIONS-SWEEP.md), Slice 2 design (outbox D-T17, registration D-T4, tenant page sections), ADR-0012 (white-label entitlement), plane document sections 3 to 7 |
| Depends on | Slice 2 (signed 2026-10-14) |

## 1. Outcome

Staff manage Vixly's plans: a code, a name and description in English, French and Spanish, and versions that carry the prices, limits, features and trial. Publishing a new version never changes what a tenant pays in its current billing period; the new price applies from the tenant's next period (D-S2).

Each tenant has at most one current subscription, with its history. A company that registers chooses a plan, a term and a number of licences, and either starts a trial or waits for payment (Slice 5). Staff see the subscription on the tenant page and can change the plan, the licence quantity or the term, extend or end a trial, cancel or resume, set limit overrides, and record an offline payment. Upgrades and more licences apply now and are prorated; downgrades and fewer licences apply at renewal (D-S4, D-S5).

Every change is history, audit and an outbox message. Slice 3 creates no invoice: Billing (Slice 4) turns the messages into invoices. Other planes read a tenant's entitlements (features and effective limits) through `Contracts.Platform` (Slice 9).

## 2. Data (schema `platform`)

| Entity | Table | Notes |
|---|---|---|
| `PlatformPlan` | `Plans` | Code (2 to 32 characters, upper case, unique, never changes), status (Active, Retired), public flag (shown in the registration catalogue), current version id, display order |
| `PlatformPlanTranslation` | `PlanTranslations` | Plan, language (en, fr, es), name (required in English), description. Feature and limit names come from resources, not data |
| `PlatformPlanVersion` | `PlanVersions` | Plan, number (1, 2, 3...), status (Draft, Published, Superseded), currency (ISO 4217, one per plan, D-S7), monthly price per licence, annual price per licence (stated per year), setup fee, minimum licences (default 2, D-S5), trial days (empty = no trial, D-S12), published by and at. Money is `decimal(19,4)`, shown to 2 places |
| `PlatformPlanVersionLimit` | `PlanVersionLimits` | Owned by the version: limit code (`max_licensed_devices`, `max_warehouses`), value 0 or more |
| `PlatformPlanVersionFeature` | `PlanVersionFeatures` | Owned by the version: feature code (the six V3 features, stable codes) |
| `PlatformSubscription` | `Subscriptions` | Tenant, plan, plan version, term (Monthly, Annual), status, licence quantity, current period start and end, trial end, cancel-at-period-end flag, ended at and reason. **One current subscription per tenant:** filtered unique index on tenant where status is not Ended (D-S3) |
| `PlatformSubscriptionScheduledChange` | owned by the subscription | At most one: target plan and version, target term, target quantity, effective at (the period end), requested by and at |
| `PlatformSubscriptionEvent` | `SubscriptionEvents` | Append-only history: event code plus arguments (D-4), from and to values, reason, actor, time. The app login has no UPDATE or DELETE on it |
| `PlatformTenantLimitOverride` | `TenantLimitOverrides` | Tenant, limit code, value, reason, set by and at, optional end date, removed by and at. At most one open override per tenant and limit |

## 3. Rules

### Plans and versions (D-S1, D-S2, D-S7)

- A new plan starts with version 1 as a draft. Only a draft is editable: prices, setup fee, currency (version 1 only), minimum licences, trial days, limits and features.
- **Publishing** a draft needs `platform.plans.publish` and step-up. It needs an English name, both limits, and a monthly or annual price above zero. The previous published version becomes Superseded; the new one becomes current.
- **Existing subscribers keep their version for the rest of their current period.** At their next renewal they move to the plan's current version (owner, D-S2). If the price goes up, the billing contact is told in advance (D-S13).
- Name, description and translations are editable at any time (they are not part of the price).
- **Retiring** a plan stops new subscriptions and plan changes into it. Subscribers stay on it and renew on it until they change plan. A retired plan can be reactivated.
- A plan's currency never changes after version 1 (one currency per plan, D-S7).

### Subscription states (D-S6)

| Status | Meaning | Reached by |
|---|---|---|
| Trialing | Free use until the trial end | Start with a trial |
| Pending payment | Waiting for the first payment, or the trial ended unpaid | Start without a trial; trial end |
| Active | Paid and in a period | Payment (Slice 5), or staff record an offline payment |
| Past due | A renewal invoice is unpaid | Billing (Slice 4) |
| Suspended | Access paused because the **account** is suspended; never used for money (D-S17) | Tenant suspended (D-S10); restored when the tenant is reactivated |
| Blocked for non-payment | Access blocked because the subscription is unpaid; the account stays in good standing (D-S17) | Collections (Slice 6), after Pending payment or Past due runs out; cleared by payment. Not built in Slice 3 |
| Ended | Over; kept as history | Cancelled at period end, cancelled at once, tenant deactivated or banned |

Cancellation is a flag, not a status: an Active subscription cancelled by the tenant or staff stays Active until the period end, then Ends. Staff can resume it before then. Staff can also end it at once (reason, step-up).

### Trial (D-S12, owner: flexible)

- A plan version may offer a trial of N days. Registration and staff may start a subscription in Trialing.
- **The trial ends when the tenant moves to a paid plan, or when its end date passes**, whichever comes first.
- Staff can extend a trial (new end date, reason) or end it now. Both are history and audit.
- At the trial end the subscription moves to Pending payment. What the tenant can still use while unpaid is D-S14.

### Changes (D-S4, D-S5)

| Change | When | Money |
|---|---|---|
| Upgrade (higher price per licence for the same term) | Now | Prorated difference for the rest of the period, published for Billing |
| Downgrade | At period end, as a scheduled change | None now |
| More licences | Now | Prorated charge for the added licences |
| Fewer licences | At period end; never below active licences (Slice 7) nor the plan's minimum | None now |
| Term change (monthly ↔ annual) | At period end | New term's price from the next period |
| Moving from Trialing to a paid plan | Now: the trial ends, status Pending payment | The first period's full price |

- One scheduled change at a time; it can be cancelled until it applies. A new change replaces the scheduled one after confirmation.
- A downgrade or a decrease is refused while active licences exceed the target (V3 rule kept).
- Every change is previewed first (`PreviewPlatformSubscriptionChange`): today's prorated amount, the next period's amount, the effective date.

### Pricing (D-S9)

- Period price = licences × price per licence for the term (annual price is per year). The setup fee is charged once, on the first paid period.
- Proration = amount × days left in the period ÷ days in the period, rounded half away from zero to 2 places (V3 arithmetic).
- **No tax in Slice 3.** Amounts are before tax; Billing adds tax from the tenant's country and region.

### Renewal

- At the period end: apply the scheduled change; move to the plan's current version (D-S2); end the subscription if cancellation was requested; otherwise start the next period and publish "renewed" for Billing.
- Until the Worker (Slice 4), a clock job in `Platform.Web` checks once an hour for trials and periods that have ended, like the outbox dispatcher. Each subscription is handled once, under a lease.

### Limit overrides (D-S8)

- Staff with `platform.subscriptions.override_limits`, a reason and step-up set a value for one limit, optionally until a date. The override wins over the plan while it is open.
- The tenant page shows the plan value, the override and its end. History and audit for set, change and remove.

### Tenant status (D-S10)

An outbox handler reacts to tenant status messages from Slice 2:

| Tenant | Subscription |
|---|---|
| Suspended | Suspended: no renewal, the period end does not move |
| Reactivated | Back to its status before suspension; a period that ended meanwhile renews at once |
| Deactivated or banned | Ended at once, reason "tenant closed" |

### Messages for other slices (D-S11)

`platform.subscription.started.v1`, `.trial_extended.v1`, `.changed.v1` (with prorated amount), `.change_scheduled.v1`, `.renewed.v1`, `.status_changed.v1`, `.cancellation_requested.v1`, `.ended.v1`; `platform.plan.version_published.v1`. Each carries ids, codes, amounts and currency only.

### Entitlements

`GetPlatformTenantEntitlements(tenant)` returns the subscription status, the features and the effective limits (override, else plan version). Operations compares its own counts against them (fixing V3's "limit above zero" check, sweep defect 6). Exposed through `Contracts.Platform` in Slice 9; used inside Platform now.

## 4. Permissions

`platform.plans.view`, `platform.plans.manage` (drafts, translations, retire), `platform.plans.publish` (step-up); `platform.subscriptions.view`, `platform.subscriptions.manage` (start, change, trial, cancel, resume, record offline payment; step-up for ending at once and for offline payment), `platform.subscriptions.override_limits` (step-up). System roles get them as in Slice 2 (Billing role: view and manage; SuperUser: all).

## 5. Use cases (three files each, gate first)

| Area | Use cases |
|---|---|
| Plans (staff) | `ListPlatformPlans` (paged, with subscriber counts), `GetPlatformPlan`, `CreatePlatformPlan`, `UpdatePlatformPlanDraft`, `SetPlatformPlanTranslation`, `PublishPlatformPlanVersion`, `CreatePlatformPlanDraft` (from the current version), `RetirePlatformPlan`, `ReactivatePlatformPlan`, `PreviewPlatformPlanPrice` |
| Subscriptions (staff) | `ListPlatformSubscriptions` (paged; status, plan, renewal window), `GetPlatformTenantSubscription`, `ListPlatformSubscriptionEvents` (paged), `StartPlatformSubscription`, `PreviewPlatformSubscriptionChange`, `ChangePlatformSubscription` (plan, quantity, term), `CancelPlatformScheduledChange`, `ExtendPlatformTrial`, `EndPlatformTrial`, `RequestPlatformSubscriptionCancellation`, `ResumePlatformSubscription`, `EndPlatformSubscriptionNow`, `RecordPlatformOfflinePayment`, `SetPlatformTenantLimitOverride`, `RemovePlatformTenantLimitOverride`, `GetPlatformTenantEntitlements` |
| Registration (anonymous) | `ListPublicPlatformPlans` (active public plans in the reader's language), `SelectRegistrationPlan` (plan, term, licences, trial or pay) |
| System | `ProcessDueSubscriptions` (clock job), `PlatformSubscriptionTenantStatusHandler` (outbox handler) |

V3 → V4 mapping is in the sweep, section 2. Error codes (prefix `platform.plans.` and `platform.subscriptions.`) and audit codes follow the Slice 2 pattern, each with en, fr and es text.

## 6. Platform.Api additions (anonymous, S-6)

| Endpoint | Purpose | Limits |
|---|---|---|
| `GET /registration/plans` | Public catalogue: code, name, description, currency, prices, limits, features, trial days; `Accept-Language` picks the text | 60 per minute per IP; cached 5 minutes |
| `POST /registration/plan` | With the registration access token: plan code, term, licences, trial or pay. Moves the registration to Pending payment, or, with a trial, starts the subscription in Trialing and moves the registration to Pending account setup | 10 per hour per registration |

The catalogue shows only Active, public plans with a published version. Registration with a trial skips payment; without a trial it waits for Slice 5.

## 7. Console screens (Frontend/UI handoff)

- **Plans** (`/plans`): list with code, name, status, current version, monthly and annual price, subscribers; filter by status.
- **Plan page** (`/plans/{id}`): summary; translations (en, fr, es); current version (read-only) and draft editor (prices, setup fee, minimum licences, trial days, limits, features); version history; price preview; publish (step-up) and retire.
- **Subscriptions** (`/subscriptions`): all tenants' subscriptions, filter by status, plan and renewal within N days; links to the tenant.
- **Tenant page, Subscription section** (owner request): plan and version, status, term, licences, period and renewal, trial end, scheduled change, cancellation, limits with overrides, actions allowed for the current status, history.

Same patterns as the rest of the console: FE's shared components, every action through its use case, step-up where marked.

## 8. Done when (in addition to the slice gate)

- Publishing a new version never changes a subscriber's current period; the next renewal uses the new version (domain and database tests).
- Every state move tested from every status, allowed and refused; one event, one audit entry and one outbox message per move, in the same commit.
- Proration matches V3's arithmetic on the V3 examples, to the cent.
- A tenant can never hold two current subscriptions (domain test and the filtered unique index under concurrency).
- A trial ends on its date or when the tenant moves to a paid plan, whichever comes first; staff extension is history.
- Downgrade and licence decrease are refused below active licences and the plan minimum.
- The clock job handles each due subscription once, even with two hosts running.
- Tenant suspended, reactivated, deactivated and banned each move the subscription as in section 3.
- The public catalogue never shows a retired, private or unpublished plan, and reads in en, fr and es.
- Every list is paged; en, fr and es complete.

## 9. Owner decisions

| # | Question | Decision |
|---|---|---|
| D-S2 | Plan edits and existing subscribers | **Owner, 2026-10-14:** existing subscribers keep their price until their current billing period ends |
| D-S4 | When changes take effect | **Owner, 2026-10-14:** upgrade now (prorated), downgrade at renewal |
| D-S7 | Currencies | **Owner, 2026-10-14:** one currency per plan |
| D-S12 | Trial | **Owner, 2026-10-14:** optional trial per plan; it ends when the tenant moves to a paid plan or when the trial ends; staff need flexibility (extend or end) |
| D-S13 | Price increase notice (proposal) | When a published version raises a subscriber's price, the billing contact gets an email at publish time with the new price and the date it applies (its next period). No notice for decreases |
| D-S14 | Unpaid after a trial or first period (proposal) | Pending payment keeps read-only access for 7 days (configurable), then Operations blocks sign-in until payment. Staff can record an offline payment at any time |
| D-S15 | Staff start a subscription for a tenant (proposal) | Allowed from the tenant page for a tenant without a current subscription (re-subscription, sales help); the tenant's data still comes from registration (Slice 2 correction) |
| D-S16 | Demo data (proposal) | Development seed adds Basic, Growth and Enterprise (V3's limits and features, USD) with versions, and subscriptions for the demo tenants in every status |
| D-S17 | Suspension for conduct versus non-payment | **Owner, 2026-10-14:** "Suspended" means only that the account (tenant) is suspended by staff for compliance, security, fraud or administrative reasons. Non-payment gets its own subscription status, **Blocked for non-payment**, set and cleared automatically by Billing and Collections (Slices 4 to 6); staff never suspend an account for unpaid bills |

## 10. Build notes (2026-10-14)

Built through every layer after the owner's go-ahead ("you can start building"), which also approved D-S13 to D-S16 as proposed. What changed from the note above while building:

| Topic | As built |
|---|---|
| Plan versions | `PlatformPlanVersion` is an entity inside the plan aggregate (not its own aggregate), so publish, supersede and draft rules stay in one place. Limits and features are owned rows of the version |
| One draft, one published | One draft per plan is a filtered unique index. "One published version" is kept by the aggregate and the `Plans` row version, not an index: publishing updates two version rows, and SQL Server checks a unique index after each statement, so the order EF sends them in could break it (found by the database test) |
| Clock | `PlatformSubscriptionClock` (Application) runs hourly from a hosted service in Platform.Web, like the outbox, until the Worker opens in Slice 4. Each due subscription is saved on its own; a second host loses the row-version race and skips it. Settings: `PlatformSubscriptionClock:Enabled`, `IntervalMinutes` |
| Who can start a subscription (D-S15, owner correction 2026-10-14) | Only a tenant that is Draft, Pending activation or Active. A suspended or deactivated tenant is reactivated first; a banned tenant never subscribes again. The console hides Start and says why; the use case refuses with `platform.subscriptions.tenant_cannot_subscribe` |
| Unpaid access (D-S14) | `PlatformSubscriptions:UnpaidReadOnlyDays` (7). The access level (Full, Read-only, None) is computed and shown on the tenant page; Operations enforces it in Slice 9 |
| Licences in use | `IPlatformLicenceCounter` returns 0 until Slice 7 (licensing) replaces it; downgrade and decrease checks already call it |
| Price-increase email (D-S13) | Sent by an outbox handler on "plan version published" to each affected tenant's billing contact (owner when none), in the tenant's language |
| Tenant reactions (D-S10) | An outbox handler moves the subscription when the tenant is suspended, reactivated, deactivated or banned |
| Public catalogue | `POST /registration/plan` is limited to 10 per hour per client IP (the shared registration-change policy), not per registration |
| Demo data (D-S16) | `--seed-demo-tenants` now also adds Basic, Growth, Enterprise (USD, public), a retired Legacy Europe plan (EUR), a draft version 2 on Growth, 19 subscriptions in every status and one limit override. History rows are written; outbox messages are not, so no demo emails. In Development the console also loads demo data at start-up (`PlatformDemoData:SeedOnStartup`, on by default); both seeds add only what is missing, so a database seeded before Slice 3 gets its plans and subscriptions on the next start |
| Console | Plans (`/plans`, `/plans/new`, `/plans/{id}`), Subscriptions (`/subscriptions`) and the Subscription section on the tenant page, with the shared Vx components. Publish saves the form first. A change must be previewed before it can be confirmed. Shared FE files were only added to: nav links, `_Imports`, the tenant page section link and component |

Verification: [SLICE-3-VERIFICATION.md](SLICE-3-VERIFICATION.md).

## 11. Commercial flexibility: discounts, promotions, offer links, one-time charges (Slice 3b)

**Status: approved by the owner, 2026-10-14.** Owner request: plans are too rigid for sales; Vixly needs free months, discounts for valued customers that do not affect anyone else, setup fees at will, and special terms at registration.

**Principle.** Plans stay the public price list (versions, D-S2). A *deal* is attached to one subscription and never changes a plan. Three tools sit on top of a subscription: a discount, one-time charges, and a setup fee override. Discounts reach a subscription in three ways: a staff grant, a promotion code, or an offer link.

**No hard-coded commercial values.** Every limit, duration and notice period below is a setting that management changes in the console; the code holds no default amount, percentage or number of days. While a setting is not set, the safe rule applies: the action needs approval, or the optional feature stays off.

### 11.1 Owner decisions (2026-10-14)

| # | Decision |
|---|---|
| D-S18 | A discount is a **percentage** or a **fixed amount** per billing period. **Only one discount is applied to a customer at a time** |
| D-S19 | A discount lasts a **number of billing periods**, **until a date**, or **with no end** |
| D-S20 | **Promotion codes** (campaigns, e.g. "first month free") apply **at registration**, and staff can also apply one **later** from the tenant page |
| D-S21 | **Offer links** for a valued customer: sales prepare agreed terms; the prospect registers through a private link with those terms filled in and locked |
| D-S22 | **The sales manager sets what salespeople may offer on their own** (Sales settings). Nothing is hard-coded. **Until the limits are set, every discount and offer link needs the sales manager's approval** |
| D-S23 | Staff can add **any one-time charge** (description and amount) and add, change or waive the **setup fee** per customer |
| D-S24 | A discount or offer **beyond what a salesperson may grant goes to the sales manager for approval**; it applies, or its link is issued, only once approved |
| D-S25 | The billing contact is emailed before a discount ends; how many days before is a Sales setting, and no email is sent while it is not set |
| D-S26 | **Products are designed by sales and approved by the CFO before they go live:** publishing a plan version and activating a promotion both wait for CFO approval (changes the publish step built in Slice 3) |

"First month free" and "3 months free" are a 100% discount for 1 or 3 billing periods. They differ from a trial (D-S12): a trial needs no commitment and ends in Pending payment; free months belong to a committed customer and billing starts on its own afterwards. Both are kept.

### 11.2 Who does what

| Role | Does |
|---|---|
| Salesperson | Designs plan drafts and promotions and submits them; grants discounts and prepares offer links within the limits; adds one-time charges and setup fee changes |
| Sales manager | Sets the Sales settings; approves or rejects discounts and offer links beyond a salesperson's limits; can do everything a salesperson does |
| Chief Financial Officer | Approves or rejects plan versions and promotions before they go live |

Nobody approves their own request (four eyes). Roles are the usual console roles: an organisation can give these permissions to any role it creates.

### 11.3 Data (schema `platform`)

| Entity | Table | Notes |
|---|---|---|
| `PlatformSubscriptionDiscount` | `SubscriptionDiscounts` | Subscription; kind (Percent, FixedAmount); value (percent above 0 up to 100, or an amount in the subscription's currency); duration (Periods with a count, UntilDate, NoEnd); periods used; source (Staff, Promotion, Offer) and its id; reason; granted by and at; status (Pending approval, Active, Rejected, Ended); approved or rejected by and at, with reason; ended at and why. At most one active or pending per subscription (filtered unique index); the rest kept as history |
| `PlatformPromotion` | `Promotions` | Code (unique, upper case), name per language, discount (kind, value, duration), valid from and to, eligible plans and terms, usable at registration and/or by staff, optional redemption limit and count, status (Draft, Pending approval, Active, Rejected, Paused, Ended), designed by, approved by |
| `PlatformOffer` | `Offers` | Token hash (SHA-256 of 32 random bytes; the link itself is never stored), optional prospect email the link is tied to, company name, plan, term, licences, discount, setup fee override, one-time charges, trial or not, expiry date, uses allowed and used, status (Draft, Pending approval, Issued, Rejected, Used, Expired, Revoked), prepared by, approved or rejected by, reasons |
| `PlatformOneTimeCharge` | `OneTimeCharges` | Subscription, description (shown on the invoice), amount (subscription currency), reason, added by and at, status (Pending, Billed by Slice 4, Cancelled) |
| Setup fee override | on `Subscriptions` | Amount, or waived, with reason and author; only while the setup fee is not yet charged |
| Plan version approval | on `PlanVersions` | New version status *Pending approval* between Draft and Published; submitted by, rejected by and reason |
| `PlatformSalesSettings` | `SalesSettings` | One row, row-versioned, every change audited. All values optional, none preset: largest percentage a salesperson may grant; largest fixed amount per currency; whether a salesperson may grant a discount with no end; longest offer link validity in days; days before a discount ends to email the billing contact; whether promotion codes are offered at registration |

### 11.4 Rules

**Discounts**
- One at a time (D-S18). Granting a new one ends the current one, with history; promotion codes and offer links count as that one discount, so they never stack.
- Within the salesperson's limits it applies at once; beyond them, or while the limits are not set, it is *Pending approval* and does nothing until the sales manager approves (D-S22, D-S24).
- Applies to the recurring amount of each period. A percentage also reduces prorated upgrade charges; a fixed amount is prorated with the period. Never below zero.
- Duration in periods counts billing periods (a month on monthly, a year on annual) and starts at the first paid period, after any trial.
- Survives renewals, plan changes and licence changes. A fixed amount larger than a new, lower price is capped at the price.
- Shown everywhere: tenant page (list price, discount, net price, end), previews ("480.00 USD minus 20% = 384.00 USD"), and a "Discounted" filter on Subscriptions.

**Approvals**
- Sales manager approves discounts and offer links; CFO approves plan versions and promotions (D-S26).
- Approve and reject need a reason and step-up; both are history and audit.
- Each approver has an Approvals queue in the console and gets an email for each new request.

**Plans and promotions go live only after CFO approval (D-S26)**
- A salesperson submits a plan draft; it becomes *Pending approval*; the CFO approves (it is published, D-S2 still applies to subscribers) or rejects it with a reason (it returns to Draft).
- A promotion follows the same path: Draft, Pending approval, Active. Pausing or ending an active promotion needs no approval.
- The Slice 3 publish button becomes "Submit for approval" for sales and "Approve and publish" for the CFO; the price-increase email (D-S13) is sent at approval.

**Promotion codes (D-S20)**
- Entered at the plan step of self-registration when the Sales setting allows it, or applied by staff to a subscription without a discount.
- Checked for validity dates, eligible plan and term, redemption limit and one use per tenant. A refused code says why ("expired", "not for this plan") without revealing other codes.

**Offer links (D-S21)**
- Prepared with the agreed terms and an expiry date. Issued at once when within the salesperson's limits and within the longest validity set; otherwise after sales manager approval.
- Link: `https://<public site>/register?offer=<token>`. Works for the number of uses set on it, can be tied to the prospect's email (registration must use and verify that address), and can be revoked until used.
- The registration shows the terms filled in and locked; choosing the plan starts the subscription with them. An expired or revoked link says so and points the prospect to their Vixly contact.
- Same rate limits and bot check as registration; unknown and expired tokens take the same time and get the same answer.

**One-time charges and setup fee (D-S23)**
- Any description and amount, in the subscription's currency; billed on the next invoice (Slice 4); cancellable until billed.
- The plan's setup fee is the default; staff can change or waive it for one customer before it is charged.

### 11.5 Permissions (new or changed)

| Permission | For | Step-up |
|---|---|---|
| `platform.discounts.grant` | Grant or end a discount within the limits | No |
| `platform.discounts.approve` | Approve or reject discounts and offer links (sales manager) | Yes |
| `platform.offers.manage` | Prepare, issue and revoke offer links | No |
| `platform.promotions.manage` | Design, submit, pause and end promotions | No |
| `platform.promotions.approve` | Approve or reject promotions (CFO) | Yes |
| `platform.plans.publish` (changed) | Approve or reject a submitted plan version, which publishes it (CFO) | Yes |
| `platform.plans.manage` (changed) | Also submits a draft for approval | No |
| `platform.subscriptions.charges` | One-time charges and setup fee override | No |
| `platform.sales.configure` | Sales settings (sales manager) | Yes |

New system roles: **Salesperson**, **Sales manager**, **Chief Financial Officer**, holding the permissions in 11.2. The Administrator keeps everything.

### 11.6 Screens and endpoints

- Console: **Sales settings**; **Promotions** (list and page); **Offers** (list, prepare, submit, copy link, revoke); **Approvals** queue; plan page "Submit for approval" and "Approve and publish"; on the tenant page, **Discount**, **One-time charges** and **Setup fee** panels; Subscriptions filter "Discounted".
- Platform.Api (anonymous): `POST /registration/offer` with the token in the body (the terms of a valid link, email masked), `GET /registration/promotions/{code}` (check a code for a plan and term), and `POST /registration/plan` accepts a promotion code or an offer token.
- Messages for Billing: discount granted, ended; one-time charge added, cancelled; setup fee changed.

### 11.7 Done when

- No commercial value in code: tests show that with no Sales settings every discount and offer link needs approval and the optional emails and registration codes stay off, and that changing a setting changes the outcome.
- One discount at a time, under concurrency (database test).
- Free months, percent and fixed discounts, every duration, renewals, upgrades and plan changes priced to the cent.
- Pending items do nothing until approved; nobody approves their own; plan versions and promotions go live only after CFO approval.
- Offer link: expiry, uses, email binding, revocation, tampering, and identical answers for unknown and expired tokens.
- Promotion code: dates, plans, terms, limit, one use per tenant, at registration and later.
- Every grant, approval, use and charge is history, audit and an outbox message in the same commit; en, fr and es complete.

### 11.8 Build notes (2026-10-14)

Built as designed, with these deviations and details:

- **No "one active discount" index.** SQL Server checks a filtered unique index after each statement, so replacing a discount (end the old, apply the new) could fail depending on statement order, as `IX_PlanVersions_OnePublished` did in Slice 3. The rule is kept by the subscription aggregate and its row version; "one pending discount" keeps its filtered unique index. Migration `20261014182722_CommercialFlexibility`.
- **`POST /registration/offer`** instead of `GET`: the token travels in the body, so it never appears in logs, browser history or referrers. Unknown, expired, used and revoked links all get the same answer.
- **The offer link is shown once.** Only a hash is stored. The person who issues it (or the sales manager who approves it) sees it once; "Get a new link" replaces it and the old one stops working.
- **A discount applies to periods that start while it is active.** "For N periods" counts renewals that used it; "until a date" covers periods starting before that date.
- **Nothing due, nothing to wait for.** When an offer or promotion makes the first amount zero (for example a free first month with the setup fee waived), the subscription becomes Active at once (`activated_nothing_due`), also when a trial ends.
- **Approvals queue** shows only what the viewer may decide, never their own requests (four eyes). Requesters cannot see a decision card on their own item.
- **Approval email** goes to every holder of the approving permission except the requester; **discount-ending email** only when the sales manager sets the number of days.
- **Demo data** (Development): sales limits stay empty, so everything needs approval. Drafts and waiting items are added at once; approved promotions and issued offers need a second staff member (four eyes) and are added at the next start once one exists.
- **Console:** Approvals, Promotions and Offer links in the navigation; Sales limits page at `/sales/settings` (linked from Approvals). The navigation now has 12 entries and overflows at 1400 px: for Frontend/UI to group.

