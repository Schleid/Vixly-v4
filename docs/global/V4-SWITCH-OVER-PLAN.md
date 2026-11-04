# Switching production from V3 to V4

| | |
|---|---|
| Date | 2026-10-23 |
| Decided by the owner | Option A: replace V3, then retire it completely. One pool per V4 site. 64-bit pools if the panel allows it. Shrink `galeologistics.net` (and `techcrafters` a little) to free memory. Copy **people and open work** with the migration tool; register the company fresh in Control. |
| Checked on the hosting (2026-10-23, read-only) | Six sites today (V3 `api`, `control`, `operations` = `galeo.vixly.ltd`, `vixly` = `vixly.ltd`; `galeologistics.net`; `techcrafters.ltd`). Three 32-bit pools: V3's four sites share one (1,024 MB), `galeologistics` (1,024 MB), `techcrafters` (512 MB); 3,072 MB in the plan, 2,560 given. Databases 2 of 20. .NET 10 runs there (V3). Plan renewal due Nov 11, 2026 (owner: renewing next week). |
| Uses | [First install](FIRST-INSTALL-CHECKLIST.md), [Settings](SETTINGS.md), [Update runbook](PRODUCTION-UPDATE-RUNBOOK.md), `build/tools/V3Migration/README.md` |

## Phase 1: build V4 beside V3 (V3 keeps serving)

1. **Memory:**
   - shrink the `galeologistics` pool to 512 MB, and `techcrafters` to 384 MB;
   - test both sites afterwards.
2. **Six new sites, each with its own pool** (64-bit if the pool settings allow; V4 publishes as portable .NET, so either works):

   | Site | Pool | Temporary test name |
   |---|---|---|
   | Platform API | 256 MB | the panel's temporary URL |
   | Control | 512 MB | the panel's temporary URL |
   | Operations API | 256 MB | `ops-internal.vixly.ltd` (new name, nothing to move; DNS only in Cloudflare) |
   | Operations Web | 512 MB | the panel's temporary URL |
   | Customer portal | 256 MB | **an application at `/customer` inside the Operations Web site**, with its own pool |
   | Public site | 256 MB | the panel's temporary URL |

   The total is 2,048 MB. After V3 retires, its 1,024 MB pool is deleted, which leaves room to grow.
3. **Database:** create the V4 database in the panel, choose it in SSMS, and run the global script.
4. **Settings and secrets** per site ([Settings](SETTINGS.md)). Each site's own temporary name goes in its `AllowedHosts` until the switch.
5. **Copy each site's publish folder** (no `app_offline.htm` needed: nothing runs there yet). A missing setting shows "This site is not set up yet".
6. **Control:**
   - create the first SuperUser;
   - set up roles, plans, legal documents and billing;
   - register the company **with the same slug as in V3**, by invitation.
7. **Operations (as the company's owner):**
   - Settings;
   - locations with **V3's warehouse codes** (`US-FLL`, `HT-CAY`, a pickup point at the destination);
   - services, prices, extras, insurance;
   - staff roles.

   Company addresses only work once `{slug}.vixly.ltd` points at V4, so this step happens in Phase 2, step 4.
8. **Rehearse the migration** against the V4 database: a dry run, then read the report.

## Phase 2: the switch (a quiet window, about an hour)

1. **Announce it:**
   - in V3, tell the company and its customers the time;
   - in V4 Control, publish a maintenance window for the company sites and portals (it shows once they are on V4).
2. **Freeze V3:** put `app_offline.htm` in V3's sites, then back up V3's database.
3. **Move the names** in the panel's Domain Manager, one site at a time: remove the name from the V3 site, add it to the V4 site, and put the SSL certificate on the V4 site:
   - `api.vixly.ltd` → Platform API;
   - `control.vixly.ltd` → Control;
   - `galeo.vixly.ltd` (the company's address; any other company address) → Operations Web, with the `/customer` application;
   - `vixly.ltd`, `www.vixly.ltd` → public site.
   
   Then set each site's real `AllowedHosts`.
4. **Finish the company's setup in Operations,** now at its real address: settings, locations, services and prices (Phase 1, step 7).
5. **Migrate:** run the tool with `--apply` against the V4 database, reading V3's frozen database. Check that the report shows **(equal)** and no PROBLEM, then do its to-do list.
6. **Check, with a real browser:**
   - a staff sign-in (the same password as in V3);
   - a customer sign-in on the portal (the same password);
   - the two open shipments and the collect invoice at the cashier;
   - an email received;
   - a public sign-up (Turnstile).
7. **Open:** end the maintenance window and tell the company.

**Rolling back during the window:** move the names back to the V3 sites and remove V3's `app_offline.htm`. V3's data has not changed since the freeze.

## Phase 3: retire V3 (the owner: "retire V3 completely")
1. **Keep V3's database backup** (download the `.bak`). Keep the V3 sites stopped, not deleted, for a week.
2. **After a week with no rollback:** delete the V3 sites and their pool, then give the memory to the V4 pools that need it.
3. **Keep the V3 database** read-only in the panel, or as the downloaded backup, for lookups and records.
