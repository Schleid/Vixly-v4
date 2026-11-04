# Maintenance at the edge: the Cloudflare Worker part

| | |
|---|---|
| Date | 2026-10-23 |
| Status | **Built 2026-10-29** (package 165): `build/edge/maintenance-worker/` with its [setup sheet](../../build/edge/maintenance-worker/README.md). Section 7 lists what changed from this design (owner, 2026-10-28). |
| Builds on | [Control: job records and maintenance](CONTROL-JOBS-AND-MAINTENANCE-2026-10-23.md), the V4 side-by-side rollout (same Worker, `*.vixly.ltd/*`) |
| Until then | `app_offline.htm` during each copy ([Production update runbook](../global/PRODUCTION-UPDATE-RUNBOOK.md)) |

## Owner decisions (2026-10-23)

| # | Question | Answer |
|---|---|---|
| W-M1 | Order | `app_offline.htm` pages now; the Worker after the production update |
| W-M2 | Brand on a company's maintenance page | **Yes:** the company's name and logo, so the customer knows where they landed |
| W-M3 | Control writing to Cloudflare | Accepted **as long as there is no security risk**. See section 2: no Cloudflare API token is used at all. |
| W-M4 | Page when the servers fail outside a window | **Yes** |

## 1. What the Worker does

For every request on `*.vixly.ltd` (and, later, company domains through Cloudflare for SaaS):

1. **A published window is running for this site:**
   - the Worker answers itself with the maintenance page (503, `Retry-After`), without contacting the servers;
   - a valid pass cookie lets the request through.
2. **A window is announced:** the request goes through as usual, and the sites show their own banner (already built).
3. **Otherwise the request goes to the server** (and to V3 or V4 by the rollout state). If the server answers 502, 503 or 504, or does not answer within 25 seconds, the Worker serves the same maintenance page with a generic message.
   - The server's own 503 maintenance page is passed through; it already carries the window.
   - The safety net catches only failures; 4xx and 500 answers pass through.
4. **Calls from programs** (`/agent/*`, `/api/*`, `/internal/*`, any request asking for JSON) get `503` with `Retry-After` and a short JSON body, never a page.

**Which site a request belongs to** (same codes as the sites):

| Request | Code |
|---|---|
| `vixly.ltd`, `www.vixly.ltd` | `public` |
| `{slug}.vixly.ltd/staff/*`, `/agent/*`, `/maps/*` | `staff` |
| `{slug}.vixly.ltd/customer/*` | `customers` |
| `{slug}.vixly.ltd/*` (the rest) | `companies` |
| `control.vixly.ltd` | never closed |

## 2. How Control tells the Worker, without a Cloudflare API token

- **The risk with a token.** As far as I know, a Cloudflare API token that may write to Workers KV may write to every KV store of the account, not just one. That includes the rollout state, where V3 or V4 is chosen per company. A leaked token could then send a company to the wrong system.
- **The design: the Worker owns its store.** Control sends the windows to the Worker itself, and only the Worker writes its KV:
  - **Endpoint:** `POST https://control.vixly.ltd/__edge/maintenance`, answered by the Worker (it never reaches Control's server).
  - **Signature:** HMAC-SHA256 with a shared secret over method, path, timestamp and body. Headers: `X-Vixly-Edge-Time`, `X-Vixly-Edge-Signature`. The Worker refuses anything older than 5 minutes, and anything already seen within those 5 minutes (nonce in KV).
  - **Secrets:** `Edge:MaintenanceSecret` on Control is an environment variable of 32 or more random characters. The same value is a Worker secret (`wrangler secret put`), never in code or KV.
  - **Body:** the **whole** list of open windows: id, start, end, sites, messages, and the SHA-256 hash of the pass code. Never the pass code itself.
    - The Worker replaces its copy; sending twice changes nothing.
    - Control sends it on every publish, cancel and *End now* (through the outbox, retried until the Worker accepts), and once an hour from the clock as a check (a new job, `edge_sync`, on the Jobs page).
- **What a leak of that secret allows:** publishing maintenance windows, which IT can end from Control. It cannot touch the rollout state, DNS or anything else in Cloudflare.

## 3. The company's brand (W-M2)

- **What the Worker keeps per company,** at `brand:{slug}` in KV:
  - the display name;
  - the logo as a small image (up to 100 KB, stored with the record, because the server may be down);
  - the main colour;
  - the default language.
- **Who sends it:** Operations.Web, signed the same way with its own secret (`Edge:BrandSecret`), whenever the company publishes its website or changes its brand. Plus a nightly full send.
- **What the page shows:**
  - **Company website, portal, staff:** the company's logo and name, its colour, and "Under maintenance, back by {time}" in the visitor's language. **Never "Vixly"** (ADR-0012 W-1).
  - **Public site:** Vixly's brand.
  - **Unknown company:** a neutral page.

## 4. The pass link at the edge
- **The link:** `/maintenance-pass/{code}` on any closed site.
- **The check:** the Worker hashes the code (SHA-256, the same as Platform's `HashToken`) and compares it with the window's hash.
- **The cookie:** `__Host-vx-edge-pass`, HMAC-signed by the Worker, holding the window id and its end, and good until the window ends. Then the Worker redirects to `/`.
- **The application's own pass cookie** (`__Host-vx-maintenance`) is still set by the site once the request reaches it. Both are needed while both layers close.

## 5. Limits and costs to confirm before building
- **Requests.** The free Workers plan allows about 100,000 requests a day. KV reads and writes have their own daily allowances (as far as I know, about 100,000 reads and 1,000 writes). Confirm current figures on Cloudflare's pricing page.
- **Caching.** The Worker keeps the windows and brands in memory for 30 seconds per instance, so most requests read nothing from KV. With one customer, the free plan is expected to be enough; the next step is Workers Paid at about $5 a month.
- **Delay.** A change reaches every Cloudflare location within about a minute, so a window opens or closes up to a minute late at the edge. The sites' own check (every minute) covers the same moment.
- **Staging.** Staging (Dynu) is not on Cloudflare. Test the Worker on a test name under `vixly.ltd` that points at staging, as the rollout plan says.

## 6. Work to build

| # | Where | What |
|---|---|---|
| 1 | Worker | The maintenance check, the page (branded and generic), the pass cookie, the safety net, and the `__edge/*` signed endpoints with KV writes |
| 2 | Platform | `PlatformEdgeMaintenancePublisher`: an outbox message on every window change, a handler that sends the signed list, and the `edge_sync` clock step |
| 3 | Operations | Brand send on publish and nightly, with the logo resized to 100 KB or less |
| 4 | Settings | `Edge:MaintenanceUrl`, `Edge:MaintenanceSecret` (Control), `Edge:BrandSecret` (Operations), and the Worker's secrets |
| 5 | Tests | Signature, age and replay refusal (Worker); whole-list send and retry (Platform) |

## 7. As built (owner, 2026-10-28)

V3 is retired, so the Worker no longer chooses between V3 and V4: it passes every request to the server, untouched, and steps in only for maintenance or a failing server. The owner approved these changes from the design above:

1. **The signed endpoints are on `api.vixly.ltd/__edge/*`**, not `control.vixly.ltd`: Control is behind Cloudflare Access, which would refuse Control's own server. The signature is the protection, as before.
2. **No 25-second timer.** SmarterASP can be slow to wake an idle site; a timer would show the page during a normal slow start. Cloudflare's own timeout (about 100 seconds) applies, and its 52x errors count as failures.
3. **During a copy, the Worker's page replaces `app_offline.htm`.** The sites' own maintenance answer carries `X-Vixly-Maintenance` and passes through untouched; any other 502, 503 or 504 gets the Worker's page.
4. **Live connections (WebSockets) pass straight through.**
5. **Free plan, routes set to "Fail open".** The sites' files can be left off the route to save the daily allowance (setup sheet, step 4).
6. **Installed from the Cloudflare dashboard** by pasting `worker.js` (owner's option A).

**Also decided while building:**
- **Replay and order:** instead of a nonce, the Worker keeps the time of the last accepted message and refuses any message that is not newer. A replay or an old retry arriving after a cancel changes nothing, and no extra KV write is needed.
- **Scheduled windows are sent too,** so the Worker closes the sites on time even if Control is down at the start.
- **The logo** is the published website logo, sent only when it is 100 KB or less (PNG, JPEG or WebP); otherwise the page shows the company's name. Nothing is resized.
- **The pass link** works at the root (`/maintenance-pass/{code}`) and under the site's first segment (`/staff/maintenance-pass/{code}`), in the Worker and on the sites. Before, a window closing only the staff workspaces could not be passed on the sites. Control now shows both forms.
- **The cookie** `__Host-vx-edge-pass` holds the window id and end, signed with a key derived from the maintenance secret, so the Worker needs only two secrets.
- **The page language:** the visitor's browser, then the company's default language, then English. Times are in UTC, as on the sites.

| Part | Where |
|---|---|
| Worker and its tests | `build/edge/maintenance-worker/worker.js`, `worker.test.mjs` (`node --test`) |
| Signature (shared) | `Vixly.Shared.Kernel.Security.VixlyEdgeSignature`; the same test value is checked in .NET and in the Worker |
| Control | `PlatformEdgeMaintenance`, `PlatformEdgeMaintenanceHandler` (outbox `platform.edge.maintenance_changed.v1`), `PlatformEdgeMaintenanceClock` (job `edge_sync`), `PlatformEdgeClient` |
| Operations | `OperationsEdgeBrands`, `OperationsEdgeBrandHandler` (outbox `ops.edge.brand_changed.v1` on publish), `OperationsEdgeBrandWorker` (06:00 UTC, job `ops.edge.brands`), `OperationsEdgeClient` |
| Sites | `X-Vixly-Maintenance` on their maintenance answer; the pass link under a site's segment |
