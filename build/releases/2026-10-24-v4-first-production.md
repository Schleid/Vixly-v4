# Release V4 1.0: first production install, weekend of 2026-10-24

| | |
|---|---|
| Packages | Up to 117 (Slice 12, the five follow-ups, maintenance pages, the settings check, the V3 migration tool) |
| Tested | Full test run, 19 suites; staging to be checked by the owner before the copy |
| First install or update | **First install** on SmarterASP.net, **replacing V3**: follow `docs/global/V4-SWITCH-OVER-PLAN.md` with `docs/global/FIRST-INSTALL-CHECKLIST.md` |

## 1. What is in it
- **Everything built so far:** Control, Platform API, Operations (staff, company websites), the customer portal, the public site.
- **Slice 12:** badge or PIN on licensed devices, the driver's route offline, live location, shelf counts, Vixly Print pairing.
- **Control:**
  - invitation emails that fail say "saved, email failed";
  - the Jobs page;
  - maintenance windows.
- **Public site:** the plans page lists the text messages, carrier data and API features.
- **Hostnames** on `vixly.ltd`.
- **New since staging:**
  - **settings check:** a site with a missing required setting shows a page naming it instead of failing to start;
  - **update pages:** `app_offline.htm` pages for updates;
  - **V3 migration tool** (`build/tools/V3Migration`): copies V3's staff, customers (same passwords), open shipments, parcels and issued invoices. Rehearsed on the 2026-10-23 V3 backup: 16 copied, no problems, money equal (301.42).

## 2. Database
- **Script:** create the database in the panel, choose it in SSMS, then run `build/scripts/sql/vixly-v4-database.sql` (first run creates everything).
- **Migrations after the script:** Platform 23, Operations 56, Customer 1.

## 3. Settings
- **Every site:** all **R** settings of `docs/global/SETTINGS.md`. This is a first install, so none exist yet.
- **Since the last staging deploy:**

| Site | Setting | | What to set |
|---|---|---|---|
| Operations Web | `OperationsMaps:TileUrl`, `Attribution` | W | The map provider's tile address and credit (production needs an account; staging uses OpenStreetMap) |
| Operations Web | `OperationsPrintHelper:AppName` | | `Vixly Print` (already in `appsettings.json`) |
| Operations Web and API | `OperationsFiles:Root` | R | Now refused if it is inside a site folder: move it outside on staging too |
| Platform API | `PlatformInternal:Hosts`, `PlatformInternal:Callers` | R | Now checked at start |

## 4. Order and checks
As in the switch-over plan: Phase 1 (V4 beside V3), Phase 2 (freeze V3, move the names, migrate, check), Phase 3 (retire V3 after a week).

**Last check, with a real browser:**
- a sign-up on the public site, which also tests Turnstile;
- a staff sign-in at the company;
- the customer portal;
- an email received;
- Control's Jobs page shows runs after the first hour.

## 5. Known limits and follow-ups
- **The Cloudflare Worker** (maintenance at the edge, brand on the page) comes after this install.
- **The Vixly Print app** must be built and published to the stores (Android and iOS workloads, store accounts).
- **Card payments** need the Stripe keys.
- **Live maps** need the map provider account.
- **V3 draft invoices and finished shipments** are not copied (the report lists them); V3's backup keeps them.
