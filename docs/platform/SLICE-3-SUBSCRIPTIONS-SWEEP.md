# Slice 3: Subscriptions - V3 behaviour sweep

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Sweep done, 2026-10-14. Slice 2 signed off. Owner settled D-S2, D-S4, D-S7 and D-S12 on 2026-10-14 (section 6); the rest are carried into the [design note](SLICE-3-SUBSCRIPTIONS-DESIGN.md) for approval.** |
| Source | V3 `ControlPlane` (read-only): `src/Vixly.Platform.Domain` (Entities, Enums and Services under `Subscriptions`), `src/Vixly.Platform.Application/UseCases/Subscriptions`, `src/Vixly.Platform.Infrastructure/Services/SubscriptionEntitlementService.cs`, `src/Vixly.Platform.Web/Components/Pages/Subscriptions` and `Tenants`, `Vixly.Platform.API/Controllers/Subscriptions` |
| Rule | Replicate behaviour, not code. Classification as in Slices 1 and 2: Active, Duplicate, Dead, Disconnected-wanted, Moved |
| Feeds | Slice 3 design note; Slice 2 registration ("choose a plan"); Slice 4 (invoices), Slice 7 (licensing) |

## 1. How the evidence was gathered

Each use case under `UseCases/Subscriptions` was traced to the console pages, API controllers and other use cases that inject it. The entities, enums, the pricing calculator and the entitlement service were read for their states and rules.

## 2. Use cases (16)

| V3 use case | Called from | Status | V4 outcome |
|---|---|---|---|
| GetSubscriptionPlans | Plans pages, TenantWorkspace, registration API, Operations internal API | Active | `ListPlatformPlans` (staff, paged) and a public catalogue for registration (anonymous, active public plans only) |
| GetSubscriptionPlanById | nobody | Disconnected-wanted | `GetPlatformPlan` for the plan page |
| CreateSubscriptionPlan | Plans pages | Active | `CreatePlatformPlan` (created as a draft, D-S2) |
| UpdateSubscriptionPlan | Plans page | Active | Replaced by plan versions (D-S2): editing prices or limits publishes a new version; name and description stay editable |
| ActivateSubscriptionPlan, DeactivateSubscriptionPlan | Plans pages | Active | `PublishPlatformPlan`, `RetirePlatformPlan` (retired plans stay on existing subscriptions) |
| AssignTenantSubscription | Registration `SelectTenantPlan` only | Active | `StartPlatformSubscription` (registration and console). No longer creates and issues the invoice itself: Billing (Slice 4) reacts to a "subscription started" message |
| ListTenantSubscriptions | TenantWorkspace, Operations internal API | Active | `GetPlatformTenantSubscription` (current) plus paged subscription history |
| GetTenantSubscriptionDetails | nobody | Duplicate | Folded into `GetPlatformTenantSubscription` |
| RequestSubscriptionPlanChange | Operations internal API (`plan-upgrade`) | Active | `ChangePlatformSubscriptionPlan` with upgrade now and downgrade at renewal (D-S4); exposed to Operations through `Contracts.Platform` |
| CancelPendingSubscriptionChange | nobody | Disconnected-wanted | `CancelPlatformScheduledChange` |
| PreviewSubscriptionUpgrade | nobody (the console computes its own figures) | Disconnected-wanted | `PreviewPlatformSubscriptionChange`, used before every change |
| PreviewSubscriptionPricing | nobody | Disconnected-wanted | `PreviewPlatformPlanPrice` for the public catalogue and the console |
| SetTenantLimitOverride, RemoveTenantLimitOverride | nobody (no screen) | Disconnected-wanted | Kept with a reason, an optional end date, step-up and history (D-S8) |
| RestoreTenantSubscriptionAccess | Folder only, no file | Dead | Covered by tenant reactivation (D-S10) |
| Capacity expansion (Operations internal API, Billing use case) | Operations | Active | Licence quantity change (D-S5); its invoice is Slice 4 |

## 3. Entities and rules found

| V3 | Contents | V4 proposal |
|---|---|---|
| `SubscriptionPlan` | Code (upper case, unique), name, description, active flag; price, one-time fee, monthly price per device, "yearly price per device"; one currency; billing interval (Monthly, Annual, OneTime); limits; features. Activation needs one price above zero | `PlatformPlan` + `PlatformPlanVersion` (D-S2). Name and description per language (plane section 7: `PlanTranslations`) |
| `SubscriptionPlanLimit` | `MaxLicensedDevices`, `MaxWarehouses`; value 0 or more; both required | `PlatformPlanLimit` on the version. Same two limits; a fixed catalogue with stable codes |
| `SubscriptionPlanFeature` | Homepage content customization, portal theme customization, custom domain, white-label portal, advanced analytics, data export | `PlatformPlanFeature` on the version. Same six, stable codes; the white-label ones are the entitlement ADR-0012 refers to |
| `TenantSubscription` | One per tenant ever; status PendingPayment, Active, Suspended, Expired, Cancelled; start and end dates; device count; billed-through date; one pending plan change | `PlatformSubscription`: one current subscription per tenant, history kept (D-S3); status model in D-S6 |
| `TenantLimitOverride` | Tenant, limit, value. No reason, no end, no author | `PlatformTenantLimitOverride` with reason, author, optional end date (D-S8) |
| `SubscriptionPricingCalculator` | Setup fee + unit price × devices, monthly proration by days, capacity expansion proration, tax at one global rate, rounding half away from zero to 2 decimals | `PlatformPricing` domain service, same arithmetic and rounding, without tax (D-S9) |
| `SubscriptionEntitlementService` | Is the subscription active and paid; effective limit = override else plan | `GetPlatformTenantEntitlements` exposed through `Contracts.Platform` (Slice 9); Operations checks counts against it |
| Business rules | At least 2 device licences; licence count may not exceed the plan limit nor drop below active licences; downgrade refused while active licences exceed the target limit; introductory trial = one free month, monthly only | Kept, with the rule values in configuration where they are commercial choices (D-S5, D-S7) |

V3 permissions: `Subscriptions.View`, `.Manage`. V4 proposal: `platform.plans.view`, `.manage`, `.publish`; `platform.subscriptions.view`, `.manage`; `platform.subscriptions.override_limits` (step-up).

## 4. Console screens found

| V3 page | State | V4 |
|---|---|---|
| `/subscriptions` SubscriptionWorkspace (353 lines) and `/subscription` Subscriptions (983 lines) | Duplicate: two routes over the same plans | One plans list and one plan page (versions, limits, features, translations, preview) |
| Plan composer, edit form, metrics, list components | Active | Plan page sections; metrics (subscribers per plan) on the list |
| TenantWorkspace subscription panel (assign, licences) | Active | Subscription section on the tenant page (owner request, 2026-10-14): plan, status, term, renewal, licences purchased, scheduled change, overrides, history |

## 5. Defects and risks seen in V3 (fixed by design in V4)

1. **Editing a plan changes every subscriber's price at once.** Plans are edited in place; a subscriber's next invoice silently uses the new price. V4 versions plans (D-S2).
2. **A tenant can never subscribe again.** "Tenant already has a subscription" is checked against every subscription ever, so a cancelled tenant is stuck (D-S3).
3. **A scheduled plan change applies the new licence count at once.** `RequestSubscriptionPlanChange` sets the device count immediately but the plan only at the next cycle, so a tenant can hold more licences than its current plan allows.
4. **Upgrades wait for the next cycle.** Both upgrades and downgrades are scheduled; the customer pays for and cannot use the better plan until renewal (D-S4). Two different definitions of "upgrade" exist (more devices, or any higher price).
5. **"Yearly price per device" is a monthly rate.** Annual billing charges `YearlyPricePerDevice × 12`, so the field holds a per-month amount under annual commitment. V4 stores an explicit annual price (D-S7).
6. **Capacity checks do not count.** `EnsureCanCreateWarehouse` and `EnsureCanRegisterDevice` only check that the limit is above zero, not that the tenant is under it.
7. **One global tax rate.** Tenants in 20 countries pay the same rate; tax belongs in Billing with per-country rules (D-S9).
8. **Assignment is not atomic.** The subscription, the invoice, its lines and its issue are saved by four use cases; a failure part-way leaves a subscription without an invoice. V4 uses the outbox (D-S11).
9. **Overrides are invisible.** No screen, no reason, no author, no end date.
10. **Free-text English errors**, as in Slices 1 and 2.

## 6. Decisions (proposals; owner answers 2026-10-14 in bold)

| # | Question | Proposal |
|---|---|---|
| D-S1 | Which plans and limits does Vixly sell? | Keep the V3 model: per-licence pricing (licences = devices) with an optional setup fee; limits `MaxLicensedDevices` and `MaxWarehouses`; the six features. Demo plans Basic, Growth, Enterprise seeded in Development only |
| D-S2 | How are plan changes handled for existing subscribers? | **Owner: existing subscribers keep their price until their current billing period ends; the new price applies from the next period.** Built with plan versions: A plan has a draft and published versions. publishing a new version never changes the current period of an existing subscriber; at renewal the subscription moves to the plan's current version |
| D-S3 | How many subscriptions per tenant? | One current subscription at a time, with history. A tenant whose subscription ended can subscribe again |
| D-S4 | When do plan changes take effect? | **Owner: as proposed.** **Upgrade now, prorated** (the tenant pays the difference for the rest of the term). **Downgrade at renewal**, refused while active licences exceed the target limit. One scheduled change at a time, cancellable. "Upgrade" means a higher recurring price per licence for the same term |
| D-S5 | Licence quantity changes | Increase now, prorated (V3's capacity expansion). Decrease at renewal, never below active licences. Minimum quantity stays 2 (configurable per plan version) |
| D-S6 | Subscription states | Trialing, Pending payment, Active, Past due, Suspended, Cancelled (at term end), Ended. Past due and non-payment blocking are driven by Billing and Collections (Slices 4 and 6). **Owner, 2026-10-14 (D-S17 in the design note):** non-payment uses its own status, Blocked for non-payment; Suspended is kept for account suspension only |
| D-S7 | Terms and prices | **Owner: one currency per plan, as V3.** Monthly and annual terms; each plan version has a monthly price per licence and an annual price per licence (stated per year), a setup fee and one currency. One-time plans are dropped (none sold) |
| D-S8 | Limit overrides | Kept: staff with `platform.subscriptions.override_limits`, a reason, step-up, an optional end date; shown on the tenant page; history and audit |
| D-S9 | Tax | Out of Slice 3: prices are shown before tax. Tax is calculated by Billing (Slice 4) from the tenant's country and region |
| D-S10 | Tenant status and subscription (Slice 2 D-T6) | An outbox handler reacts to tenant messages: suspended tenant → subscription suspended (no renewal); reactivated → resumed; deactivated or banned → cancelled at once. History on both |
| D-S11 | How do subscriptions reach Billing? | Messages through the outbox: "started", "changed", "renewed", "cancelled". Slice 4 turns them into invoices. Nothing in Slice 3 creates an invoice |
| D-S12 | Trial | **Owner: optional trial per plan, flexible: it ends when the tenant moves to a paid plan or when the trial period ends.** Staff can extend or end a trial. Optional trial days per plan version (V3: one free month, monthly only). A trial needs no payment and ends in Pending payment unless paid. Self-registration chooses a plan, then pays (Slice 5) or starts the trial |
