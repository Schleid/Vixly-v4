# Slice 3: Subscriptions - verification record

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Date | 2026-10-14 |
| Built on | `225e72c` (dev team HEAD), changes not committed: the dev team owns git |
| Design | [Design note](SLICE-3-SUBSCRIPTIONS-DESIGN.md), [sweep](SLICE-3-SUBSCRIPTIONS-SWEEP.md) |
| Status | **Built and verified; waiting for owner sign-off** |

## 1. Automated checks

Release build of `Vixly.Platform.slnx`: no errors, no new warnings. The only warnings are the 10 NU1902/NU1903 package advisories already present at `225e72c` (section 4).

| Test project | Before (225e72c) | After |
|---|---|---|
| Platform.Domain.Tests | 168 | 208 |
| Platform.Application.Tests | 134 | 147 |
| Platform.Infrastructure.Tests (with SQL Server) | 23 + 5 skipped | 32 |
| Platform.Api.Tests | 8 | 8 |
| Platform.Web.Tests | 31 | 31 |
| Architecture.Tests | 2,308 | 2,958 |
| Shared.Kernel.Tests | 81 | 81 |

All pass. The infrastructure tests were run three times in a row to rule out ordering effects.

What the new tests cover: plan drafts, publish, supersede, retire and reactivate; pricing and proration to the cent against V3's arithmetic; every subscription state move allowed and refused; trial start, extend, end and expiry; upgrade now and downgrade at renewal, with the active-licence and minimum checks; renewal onto the plan's current version (old price until the period ends, D-S2); cancellation and resume; tenant suspended, reactivated and closed; limit overrides with an end date; use-case permissions, step-up, history, audit and outbox in one commit; the database mapping, the one-current-subscription index, a renewal saved once when two hosts race, and the list queries. The append-only rule on `SubscriptionEvents` is in the global script (DENY UPDATE, DELETE to the app login), as for tenant history.

## 2. End-to-end run (console and API, Development database)

Fresh database from `build/scripts/sql/vixly-v4-database.sql`, first SuperUser, then `--seed-demo-tenants`.

| # | Check | Result |
|---|---|---|
| 1 | Seed | 22 tenants, 4 plans, 19 subscriptions: 2 trial, 3 pending payment, 9 active, 2 suspended with the tenant, 3 ended with the tenant |
| 2 | Plans list | 4 plans with version, prices, public flag, subscriber count; Legacy Europe shown Retired |
| 3 | Publish Growth version 2 with a stale second factor | Step-up asked, then published; version 1 Superseded; price preview 12 licences = 324.00 USD |
| 4 | New plan STARTER-CA (CAD) | Created, priced, published in one go; French name saved |
| 5 | Subscriptions list | Filters by status, plan and renewal; ended hidden by default |
| 6 | Pending payment: record offline payment | Active, period runs one month |
| 7 | Upgrade Growth 3 to Enterprise 12, monthly | Preview "applies now", charged now 405.00 USD, next period 480.00 USD; applied |
| 8 | Downgrade to Basic 4 | Preview "applies at renewal on 11/14/2026"; scheduled; then cancelled |
| 9 | Cancel at period end, then withdraw | Warning shown with the end date; withdrawn |
| 10 | Warehouse override 12 with a stale second factor | Step-up asked; override shown with reason and author; removed |
| 11 | Trial: extend, then end | New end date shown; ended trial is Pending payment |
| 12 | Staff start for a tenant without a subscription (D-S15) | Started on Basic with its 14-day trial |
| 13 | History | Every action listed with actor and reason, in English and French |
| 14 | French | Plans, statuses, amounts (9,00 CAD) and dates in French formats |
| 15 | `GET /registration/plans` | Only Basic, Growth, Enterprise (retired and private plans hidden); Spanish names with `Accept-Language: es`; `Cache-Control: public, max-age=300` |

Screenshots were taken in light and dark themes for the plans list, plan page, subscriptions list, change preview and the tenant Subscription section.

## 3. Defect found and fixed during verification

Publishing a second version could fail with a duplicate key on `IX_PlanVersions_OnePublished`, depending on the order EF sent the two version updates. The index was removed; the rule is kept by the plan aggregate and the `Plans` row version. The migration was regenerated before delivery (`20261014162220_Subscriptions`).

## 4. For the dev team

- **Package advisories (present before Slice 3):** transitive `System.Security.Cryptography.Pkcs` 9.0.11 (NU1902, NU1903 high) and `AngleSharp` 1.2.0 (NU1902), although `Directory.Packages.props` lists newer versions. Suggested fix: `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>` in `Directory.Packages.props`. Not changed, because Frontend/UI and the dev team own that file.
- **Database:** run the updated global script (adds the Slice 3 tables and the append-only rule on `SubscriptionEvents`).
- **Backups** of the 24 files changed on disk are in `_to_delete/backup-slice3-20261014162703`, with the delivery package; delete `_to_delete` when satisfied.

## 5. Open items

- Owner sign-off of Slice 3.
- Activity privacy settled by the owner on 2026-10-14 (country only, 90 days, `platform.tenants.view_activity`, privacy notice); recorded in the Slice 2 design note for Stage 3.

## 6. Slice 3b: commercial flexibility (2026-10-14)

Design: section 11 of the [design note](SLICE-3-SUBSCRIPTIONS-DESIGN.md). Built on the same base; delivered as one package with a conflict check (no conflicts; 224 new files, 62 changed, texts and three shared Frontend files merged line by line, never overwritten).

### 6.1 Automated checks

| Test project | After Slice 3 | After Slice 3b |
|---|---|---|
| Platform.Domain.Tests | 208 | 232 |
| Platform.Application.Tests | 147 | 163 |
| Platform.Infrastructure.Tests (with SQL Server) | 32 | 35 |
| Platform.Api.Tests | 8 | 8 |
| Platform.Web.Tests | 31 | 31 |
| Architecture.Tests | 2,958 | 3,603 |
| Shared.Kernel.Tests | 81 | 81 |

Release build: no errors, no new warnings. All pass.

### 6.2 End-to-end run (console, fresh Development database)

| # | Check | Result |
|---|---|---|
| 1 | Seed with one staff member | 2 draft or waiting promotions, 2 draft or waiting offers, 2 discounts (one applied, one waiting); approved items held back (four eyes) |
| 2 | Second staff member (Ada: CFO and sales manager), seed again | 3 approved promotions (active, paused, ended), offers issued and revoked |
| 3 | Approvals queue | Jude sees nothing (all requests are his own); Ada sees offer, promotion, discount and new promotion |
| 4 | Promotion AUTUMN10 designed by Jude and submitted | Waiting for approval; no decision card for Jude |
| 5 | Ada approves | Active; French name added |
| 6 | Growth draft submitted by Jude | Fields locked, "waiting for the CFO"; Ada "Approve and publish" publishes version 2 |
| 7 | Offer link for Globex (10% for 2 years, setup fee waived, 500 USD workshop) | No limits set, so it goes to the sales manager; Ada approves and sees the link once; after reload it is gone; Jude "Get a new link" issues a different one |
| 8 | Sales limits: 20% without approval, ending notice 7 days (step-up) | Saved |
| 9 | Discount 15% for 3 periods | Applied at once; next period shows net and "before discount"; history line |
| 10 | Discount 40% with no end | Sent to the sales manager; approval email sent; Ada approves on the tenant page |
| 11 | One-time charge 250 EUR | Listed "To bill"; history line |
| 12 | Promotion codes by staff | Draft code refused ("not active"); monthly-only code refused on an annual subscription |
| 13 | Subscriptions filter "With a discount" | 3 subscriptions |

### 6.3 For the dev team

- **Database:** run the updated global script (adds the Slice 3b tables).
- **Navigation:** 12 entries now; at 1400 px the last one is cut off. Frontend/UI to group them (for example a "Sales" menu).
- **Log noise:** leaving a list page while it loads logs "connection is closed" from the presenter (the component's scope is disposed). Harmless and not new; could be logged as information.
- **Backups** of the changed files are in `_to_delete/backup-slice3b-*`, with the package.
- **Not built:** the public registration site (`Vixly.Public.Web`, stage not open) will need the promotion code field and the `/register?offer=` page; the API is ready.

