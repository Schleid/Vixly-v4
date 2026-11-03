# Updating production

| | |
|---|---|
| Date | 2026-10-23 |
| Hosting | SmarterASP.net, shared, no PowerShell; files by WinSCP, staging by Web Deploy |
| Maintenance page during the copy | `build/maintenance/app_offline/<site>/app_offline.htm` |
| Cloudflare Worker (built 2026-10-29) | Once set up, it serves the company-branded maintenance page for published windows and while a site is down for its copy ([setup](../../build/edge/maintenance-worker/README.md), [design](../platform/MAINTENANCE-WORKER-DESIGN.md)) |

## 1. The owner's way today, reviewed

**Today:** Web Deploy to staging, test there, then WinSCP to production, either:
- **(A)** erase everything except the settings files, then copy everything; or
- **(B)** copy only the files that changed.

**What is good:**
- Production gets only what was tested on staging.
- The settings files stay on the server and are never overwritten from a PC.

**Risks, and how to remove them:**

| Risk | With | What happens | Fix |
|---|---|---|---|
| The site runs while its files change | A and B | The application restarts in the middle of the copy, with old and new DLLs mixed. Locked DLLs fail to copy, and visitors get errors for the length of the copy. | Upload `app_offline.htm` first, wait about 5 seconds, copy, then delete it last (section 3) |
| A changed file is missed | B | A DLL, `*.deps.json`, `*.runtimeconfig.json`, `*.staticwebassets.endpoints.json` or a fingerprinted file under `wwwroot` stays old. The site starts but fails on one page, or serves old scripts and styles. | Let WinSCP choose the files: **Synchronize, mirror mode** (section 3), not a hand-picked list |
| Old files stay behind | B | A file deleted from the project stays on the server. Usually harmless; a leftover DLL can occasionally be loaded. | Mirror mode deletes them |
| Files the site keeps are erased | A | **`OperationsFiles:Root`** holds every uploaded photo, signature and document. If it points inside the site folder, erasing the folder destroys them. The same goes for the Public site's `PublicSite:KeysPath` (form keys) and any log folder. | Keep `OperationsFiles:Root` **outside** the site folders, in a folder of its own that Operations.Web and Operations.Api share. Check this before the first update. |
| The files are not the ones tested | A and B | A new build on the PC for production can differ from what went to staging. | Publish once to a folder. Web Deploy that folder to staging and copy the same folder to production. |
| The database is behind the code | A and B | A new version reads a table that does not exist yet (as seen with `platform.MaintenanceWindows`). | Run the global SQL script **before** copying files. It only adds, so the old version keeps working on the new database. |
| No way back | A and B | A bad update has nothing to return to. | Keep the previous publish folder. A rollback is the same mirror, from the old folder. Back up the database first. |

**Verdict:**
- **(A) and (B) are both fine** once each copy runs behind `app_offline.htm`.
- **(B) is fine** when WinSCP's mirror chooses the files.
- **(A) must keep** the files folder and keys out of the erase.

Use **one way**: the mirror. It copies only what changed, deletes what is gone, and never touches the excluded files.

## 2. Before the update
0. **Release notes:** read them (`build/releases/`). They list the database changes and every new or changed setting, by site (names only; see [SETTINGS.md](SETTINGS.md)). Set new settings on staging first, then production. A site missing a required setting does not start: it shows "This site is not set up yet" with the names to set.
1. **Staging is green:** the new version was tested there from the same publish folder.
2. **Database:** back up production (SmarterASP control panel, or a `.bak` from SSMS).
3. **Files:** keep the previous publish folder (for example `publish\2026-10-23`).
4. **Uploads:** check that `OperationsFiles:Root` is outside every site folder.
5. **Mirror rules:** in WinSCP, save one **Synchronize** setup per site (it is remembered):
   - **Direction:** Remote; **Mode:** Mirror files;
   - **Compare:** Modification time and File size; **Delete files:** on;
   - **Exclude mask:**
     ```
     | web.config; appsettings.Production.json; appsettings.Staging.json; app_offline.htm; logs/; App_Data/
     ```
   - Add any other folder the site writes to (for example the Public keys folder, if inside the site).
   - **operations-web:** also exclude `customer/`. The customer portal is an application inside that folder and is updated on its own.

## 3. The update, site by site

**Order** (callers after what they call):
1. the SQL script: **the same file for staging and production**. Open it in SSMS, choose the database in the toolbar (staging: `VixlyV4`; production: `db_ac936e_vixlyv4`), Execute. Passwords stay empty: it keeps the existing logins and refreshes their grants. Check the last grid: the migration counts of the release notes;
2. Platform API;
3. Control;
4. Operations API;
5. Operations Web;
6. the customer portal;
7. the public site.

**For each site:**
1. **Close it:** upload that site's `app_offline.htm` to the site folder (`control/`, `platform-api/`, `operations-web/`, `operations-api/`, `customer/` or `public/`). The server stops the application and shows the page, with a 503 status.
2. **Wait** about 5 seconds, so the application lets go of its files.
3. **Copy:** run the saved **Synchronize** from the publish folder. Look at the list before you accept it:
   - no `web.config` or `appsettings.*.json`;
   - no deletions in a folder of uploads.
4. **Open it:** delete `app_offline.htm`. The application starts on the next request.
5. **Check it:** open `/health/ready` (`Healthy`; `Degraded` means a setting warning, so read the log) and the home page. If it shows "This site is not set up yet", set the settings it names and recycle the site.

**Afterwards**, sign in to Control and Operations, then open one company website and the portal.

**If something is wrong:** put `app_offline.htm` back, mirror from the previous publish folder, and delete `app_offline.htm`. The database script only adds tables and columns, so the old version keeps working on it.

## 4. The maintenance pages
- **Six pages** live in `build/maintenance/app_offline/`, one per site.
- **What they say:** the update usually takes a few minutes and nothing saved is lost. They reload every 30 seconds and show English, French or Spanish from the browser.
- **Self-contained:** no outside files, because the server serves nothing else while the page is there.
- **Who may say "Vixly":** Control, Platform API and the public site. The company websites, staff workspaces and customer portals serve every company, so their page names nobody (ADR-0012 W-1). The company's own name and logo come with the Cloudflare Worker.
- **Never in the project's published output:** an `app_offline.htm` that ships with a publish would take the site down. They live under `build/`, outside every project.
