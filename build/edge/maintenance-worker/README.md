# The maintenance Worker: setting it up in Cloudflare

| | |
|---|---|
| Date | 2026-10-29 |
| What | `worker.js`: the Cloudflare Worker in front of the V4 sites on `vixly.ltd` |
| Design | [Maintenance at the edge](../../../docs/platform/MAINTENANCE-WORKER-DESIGN.md) (owner, 2026-10-23 and 2026-10-28) |
| How it is installed | The Cloudflare dashboard (owner, 2026-10-28): paste one file. Nothing to install on any computer. |

## What it does
- **During a maintenance window** published in Control, it shows the maintenance page itself, without contacting the servers. The page shows the company's name and logo, never "Vixly". IT's pass link still lets them through.
- **When a server fails** (502, 503 or 504, or a Cloudflare 52x error such as 522), it shows the same page with a general message. This covers an update copy with `app_offline.htm` too. Program calls (`/agent`, `/api`, `/internal`) get a short JSON answer.
- **Everything else** goes to the servers untouched, including the live connections of the staff pages.
- **If the Worker itself fails** or the free daily allowance runs out, the sites keep working as if there were no Worker ("fail open").
- **Control (`control.vixly.ltd`) is never closed.**

## Before you start: two secrets
Make two different random values of **64 hexadecimal characters** (0-9 and a-f only; the hosting panel damages `+ / =`). On your own PC, any of these works:
- Python: `python -c "import secrets; print(secrets.token_hex(32))"`
- PowerShell 7: `[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)).ToLower()`
- A password manager's generator set to 64 characters, hexadecimal

| Name | Goes into |
|---|---|
| **Maintenance secret** | The Worker (`EDGE_MAINTENANCE_SECRET`) and Control (`Edge__MaintenanceSecret`) |
| **Brand secret** | The Worker (`EDGE_BRAND_SECRET`) and Operations Web (`Edge__BrandSecret`) |

Keep them in your password manager. Never put them in a file that is copied or committed.

## 1. Create the Worker
1. Cloudflare dashboard → **Workers & Pages** → **Create** → **Worker** (start from "Hello World").
2. Name it `vixly-edge`, then **Deploy**.
3. **Edit code**: delete everything, paste the whole of `worker.js`, then **Deploy**.

## 2. Create its store
1. **Storage & Databases** → **KV** → **Create** a namespace named `vixly-edge`.
2. Back on the Worker → **Settings** → **Bindings** → **Add** → **KV namespace**:
   - variable name `VIXLY_EDGE`;
   - namespace `vixly-edge`.

## 3. Its settings
Worker → **Settings** → **Variables and Secrets** → **Add**:

| Type | Name | Value |
|---|---|---|
| Text | `BASE_DOMAIN` | `vixly.ltd` |
| Text | `EDGE_HOST` | `api.vixly.ltd` |
| Secret | `EDGE_MAINTENANCE_SECRET` | the maintenance secret |
| Secret | `EDGE_BRAND_SECRET` | the brand secret |

**Deploy** again if the dashboard asks.

## 4. The routes
Cloudflare dashboard → the **vixly.ltd** site → **Workers Routes** → **Add route**. First check under **DNS** that `vixly.ltd`, `www` and `*` are **Proxied** (orange cloud): a Worker only runs on proxied names. `ops-internal` stays DNS only.

| Route | Worker | Failure mode |
|---|---|---|
| `vixly.ltd/*` | `vixly-edge` | **Fail open** |
| `*.vixly.ltd/*` | `vixly-edge` | **Fail open** |
| `api.vixly.ltd/__edge/*` | `vixly-edge` | **Fail open** |
| `api.vixly.ltd/*` | **None** | |
| `control.vixly.ltd/*` | **None** | |

**To save the daily allowance** (100,000 Worker requests a day on the free plan), also add these with Worker **None**. The sites' files then go straight to the servers:
- `*.vixly.ltd/_framework/*`, `*.vixly.ltd/_content/*`
- `*.vixly.ltd/css/*`, `*.vixly.ltd/js/*`, `*.vixly.ltd/lib/*`, `*.vixly.ltd/img/*`
- `*.vixly.ltd/customer/_framework/*`, `*.vixly.ltd/customer/_content/*`, `*.vixly.ltd/customer/css/*`, `*.vixly.ltd/customer/js/*`

Cloudflare uses the most specific route that matches, so these take precedence over `*.vixly.ltd/*`.

## 5. Tell the sites where the Worker is
On SmarterASP.net, then recycle each site:

| Site | Setting | Value |
|---|---|---|
| Control | `Edge__MaintenanceUrl` (or `Edge:MaintenanceUrl` in `appsettings.Production.json`) | `https://api.vixly.ltd/__edge/maintenance` |
| Control | `Edge__MaintenanceSecret` (environment variable only) | the maintenance secret |
| Operations Web | `Edge__BrandUrl` (or `Edge:BrandUrl` in `appsettings.Production.json`) | `https://api.vixly.ltd/__edge/brand` |
| Operations Web | `Edge__BrandSecret` (environment variable only) | the brand secret |

Without these, the sites send nothing and work as before. Staging has no Worker, so leave them empty there.

## 6. Checks
1. **The sites work as before:** open `galeo.vixly.ltd`, the customer portal and the staff sign-in.
2. **Your location still reaches the servers:** sign in as staff, then open your sign-in history. It must show your real country. The device rules depend on it. If it shows something else, remove the routes (step 4) and tell Claude.
3. **Control reaches the Worker:** within the hour, **Control → System → Jobs** shows *Maintenance sent to Cloudflare* as a success, with 1 handled.
4. **The company brand reaches the Worker:** in Operations, publish the website again (Website → Publish). The nightly round (06:00 UTC) also sends every company's brand.
5. **A short test window, at a quiet hour:**
   - in Control → Maintenance, publish a window starting now, 10 minutes long, closing **Company websites** only, notice 0;
   - within about a minute, `galeo.vixly.ltd` shows the maintenance page with the company's logo and name, while `/staff` and `/customer` still work;
   - open `galeo.vixly.ltd/maintenance-pass/{code}`: the website opens for you;
   - press **End now**: within about a minute the website is open for everyone.

## Updating the Worker
Open the Worker → **Edit code**, paste the new `worker.js`, **Deploy**. The store and the settings stay.

## Turning it off
Delete the routes of step 4. The sites work as before; Control's hourly send then fails on the Jobs page until `Edge__MaintenanceUrl` is removed from Control.

## The tests
`node --test build/edge/maintenance-worker` (Node 22). They cover the sites, the page, the pass link, the safety net, the signed endpoints and their refusals.
