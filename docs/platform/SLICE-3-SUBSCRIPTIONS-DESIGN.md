# Slice 3: Subscriptions - design note

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Draft for owner review, 2026-10-14.** D-S2, D-S4, D-S7 and D-S12 settled by the owner; D-S1, D-S3, D-S5, D-S6, D-S8 to D-S11 as proposed in the sweep; D-S13 to D-S16 below are new proposals |
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
| Suspended | Access paused (tenant suspended, or Collections in Slice 6) | Tenant suspended (D-S10) |
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
