# Control: job records, maintenance windows and the follow-ups of 2026-10-23

| | |
|---|---|
| Date | 2026-10-23 |
| Asked by | The owner, after Slice 12 (see [Slice 12 owner answers](../operations/SLICE-12-OWNER-ANSWERS.md), "After Slice 12") |
| Builds on | [Control backlog of 2026-10-22](CONTROL-BACKLOG-2026-10-22.md), section 1; [ADR-0007](../decisions/ADR-0007-platform-worker.md); [ADR-0010 Amendment 1](../decisions/ADR-0010-standard-v1-3-data-naming-hostnames.md) |

## 1. The five items

| # | Item | Done |
|---|---|---|
| 1 | Control's invitation error says "user saved, email failed" | Invite and resend catch a mail failure. The person stays saved, the history records `platform.users.invitation_mail_failed`, and the page says: "The person was saved, but the invitation email could not be sent… use *Resend invitation*". |
| 2 | The public plans page lists the text messages, carrier data and API features | `text_messages`, `carrier_data` and `tenant_api` are shown with the other plan features |
| 3 | Maintenance windows | Section 3 |
| 4 | Hostnames on `vixly.ltd` | ADR-0010 Amendment 1, the Global Standard hostname table and the production `appsettings.json` of every site. Test hosts keep their own test domain. |
| 5 | Platform job records and a Jobs page | Section 2 |

## 2. Job records and the Jobs page

- **Each step on its own.** The subscription clock runs ten steps every hour, each in its own scope:
  1. subscriptions;
  2. billing;
  3. card payments;
  4. collections;
  5. licensing;
  6. Terms;
  7. event feed;
  8. contact requests;
  9. sign-in clean-up;
  10. job history clean-up.
  
  A step that fails is logged and recorded; the next steps still run. Before, one failure stopped the rest of the hour.
- **Record:** `platform.JobRuns`, one row per step and run:
  - the job, its start and end;
  - the items it handled;
  - whether it failed, with the error type only (never the message, which may carry data).
  
  The row is written in its own scope after the step, so a broken step never stops its record. Runs are kept 90 days.
- **Jobs page** (`/jobs`, menu *System*, right `platform.jobs.view`):
  - for each job: its last run, the outcome, the last success and the failures of the last 24 hours, with every known job listed even if it never ran;
  - the run history below, by job and *Failures only*.
- **Right:** `platform.jobs.view` is in the security administrator's role; a SuperUser has it anyway.
- **Not done:** the alert email and *Run now* of ADR-0007 (with the Worker).

## 3. Maintenance windows

### What Vixly IT does in Control (`/maintenance`, menu *System*)
- **Publish a window** (right `platform.maintenance.manage`, with step-up):
  - start and end, in UTC;
  - the sites it closes: public site, company websites, customer portals, staff workspaces;
  - the advance notice in hours (0 to 168, 48 by default);
  - an optional message in English, French and Spanish.
- **Limits:** a window starts within a year and lasts at most 7 days.
- **Control is never closed,** so a window can always be ended.
- **The pass link** is shown once, after publishing: `/maintenance-pass/{code}`, to add to the address of any site the window closes. Only its hash is kept.
- ***Cancel*** a window before it starts, or ***End now*** while it runs. Both are in the history: `platform.maintenance.scheduled` and `platform.maintenance.stopped`.
- **The page lists** the windows to come and running, then the past ones.

### What the sites do
- **Before the window,** from the notice time: a banner at the top of every page of the sites it closes, with the start, the end and the message.
- **During the window:**
  - **The page:** every request gets *Under maintenance* with the expected end and the message. The answer is 503 with `Retry-After`.
  - **Wording:** in the visitor's language. Company sites and portals never say "Vixly"; IT writes its message accordingly.
  - **Still served:** health checks and the site's files.
- **The pass link:**
  - it checks the code with Platform and sets a protected cookie (`__Host-vx-maintenance`) good until the window ends;
  - the cookie opens every site the window closes on that host;
  - a wrong code is a plain 404.
- **Reading the windows:**
  - each site reads them every minute, in the background, so no visitor waits on Platform;
  - if Platform cannot be reached, the last copy stays and nothing is closed by mistake.

### Who reads what

| Site | How it reads | Site code |
|---|---|---|
| Public site | Platform.Api `GET /maintenance?site=public` | `public` |
| Company websites, staff workspaces (Operations.Web) | Platform.Api, through the Operations Platform address | `companies`, `staff` (`/staff`, `/agent`, `/maps`) |
| Customer portals (Customer.Web) | Operations.Api `GET /internal/v1/maintenance` (customer scope), which asks Platform | `customers` |

- **Platform.Api:**
  - `GET /maintenance` is public, because what it returns is shown to every visitor anyway. It returns announced and running windows only; a window further ahead stays private. Limit: 600 a minute per address.
  - `POST /maintenance/pass`: 20 per 10 minutes per address.
- **Shared code:** `Vixly.Shared.Hosting.Maintenance`: the monitor, the middleware, the page and the banner text. Each plane gives only its source and its words.

### Still to do
- **Deploys:** while an application is being deployed it cannot answer for itself. The Cloudflare Worker of the side-by-side rollout will serve the page then, from Workers KV written by Control (backlog section 1, to design with the Worker).
- **Styling:** the banner has a plain default style (`.vx-maintenance-banner` in `vixly-theme.css`); the Frontend/UI team styles it, the Jobs page and the Maintenance page.

## 4. Database

| Migration | Changes |
|---|---|
| `JobRuns` | Table `platform.JobRuns`, indexed by job and start |
| `MaintenanceWindows` | Table `platform.MaintenanceWindows` with a row version; the pass hash is unique; indexed by end |

## 5. Settings
- **None required.** `VixlyMaintenance:Enabled` (default `true`) and `VixlyMaintenance:RefreshSeconds` (default 60) exist on each site, to turn the check off or slow it down.
- **Roles:** after publishing, give `platform.maintenance.view` and `platform.maintenance.manage` to the IT people who need them. The security administrator role has them.

## 6. Tests
- **Domain:** `PlatformMaintenanceWindowTests`:
  - times, length, sites, notice and message;
  - the status through time;
  - cancel, end early, only once.
- **Application:**
  - `PlatformMaintenanceUseCaseTests`:
    - the right and the step-up;
    - only the pass hash kept, audited;
    - a site sees only its announced and running windows;
    - the pass opens only its sites;
    - stop once.
  - `PlatformJobRunsUseCaseTests`:
    - the right;
    - every job listed;
    - an unknown job refused.
- **Database:** `PlatformJobRunDatabaseTests`:
  - recorder, summary, filters;
  - the 90-day clean-up.
- **HTTP:**
  - `PublicMaintenanceHttpTests`:
    - banner before;
    - 503 with the message during;
    - health open;
    - a wrong pass is 404;
    - the pass link opens the site;
    - Platform unreachable leaves the site open.
  - `/jobs` and `/maintenance` send a signed-out visitor to sign-in.
