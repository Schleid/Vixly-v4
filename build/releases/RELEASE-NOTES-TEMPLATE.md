# Release {version}: {date}

| | |
|---|---|
| Packages | {pkg numbers} |
| Tested | Full test run {run}: {suites} suites green; on staging by {who} on {date} |
| First install or update | {update / first install: see docs/global/FIRST-INSTALL-CHECKLIST.md} |

## 1. What changed (for the owner)
- {one line per change, in plain words}

## 2. Database
- **Run** `build/scripts/sql/vixly-v4-database.sql` **before** copying files.
- **New migrations:** {Platform: ..., Operations: ..., Customer: ...}.
- **Migrations after the script:** Platform {n}, Operations {n}, Customer {n}.
- **Removes nothing:** the previous version runs on the new database (rollback is safe). {or: NOT safe, because ...}

## 3. Settings
New or changed settings, by site. Names only, never values (see docs/global/SETTINGS.md).

| Site | Setting | New / changed | R / W / S | What to set |
|---|---|---|---|---|
| {site} | {Section:Key} | {new} | {R, S} | {where the value comes from} |

{or: No setting changes.}

## 4. Order and checks
1. Back up the database, and keep the previous publish folder.
2. Settings first, on staging, then on production.
3. Run the SQL script.
4. Copy each site behind `app_offline.htm`, in this order: Platform API, Control, Operations API, Operations Web, customer portal, public site. Skip the sites this release does not change: {list}.
5. Check `/health/ready` on each, then:
   - {release-specific smoke checks};
   - sign in to Control and Operations;
   - open one company website and the portal.

## 5. Known limits and follow-ups
- {anything not done, and anything the owner must still do}
