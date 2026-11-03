# Hosting review of 2026-10-22: background jobs and the side-by-side rollout

The two project documents *Background jobs: V3 compared with V4* and *V4 side-by-side rollout on vixly.ltd* were checked against the code on 2026-10-22. This records what was built and what remains open.

## 1. Built

| Item | What | Where |
|---|---|---|
| Forwarded company address | Behind the Cloudflare Worker, `X-Forwarded-Host` is used only for a host in `Hosting:EdgeHosts`, only from Cloudflare (`Hosting:TrustedProxyNetworks`), and only with the Worker's secret (`X-Vixly-Edge` = `Hosting:EdgeSecret`, constant-time comparison). The secret header is always removed. Off unless configured. All five hosts. | `Shared.Hosting/VixlyEdgeHost.cs`, `VixlyEdgeSettings.cs`; Global Standard H-3 |
| Sign-in clean-up | Expired sign-in rows are removed every hour: sessions a year after expiry, since they are the sign-in history; challenges, passkey ceremonies and links 30 days after. Batched deletes of 2,000 rows. Device links are kept, because they carry support history. | Platform: `PlatformSignInCleanupClock` in the subscription clock. Operations: `OperationsSignInCleanup` in `OperationsHousekeepingWorker`, recorded in `ops.JobRuns` (`ops.housekeeping.sign_in`) |
| ADR-0007 | Amendment 1: jobs run in the web apps on shared hosting; the Worker stays the later option | `docs/decisions/ADR-0007-platform-worker.md` |

## 2. Production settings for the Worker

Set these on each V4 site behind the Worker:

- `Hosting__EdgeHosts__0=vixly.ltd` and `Hosting__EdgeHosts__1=*.vixly.ltd`;
- `Hosting__EdgeSecret`, an environment variable of 32 or more random characters, the same value the Worker sends as `X-Vixly-Edge`;
- `AllowedHosts` must include the origin's own name, for example `v4-origin.vixly.ltd`.

## 3. Open

1. **Hostnames conflict.** The Global Standard (ADR-0010) runs V4 on `techcrafters.ltd` until migration, and every production `appsettings.json` follows it. The side-by-side rollout (owner, 2026-10-21) runs V4 on `vixly.ltd` behind the Worker. The owner chooses; ADR-0010 and the production settings then follow. V4's cookies are all `__Host-vx4-*`, so V3's domain-wide `.vixly.ltd` cookies cannot collide with them.
2. **What the Worker passes on, to test on a `vixly.ltd` test name:**
   - the visitor's address (`CF-Connecting-IP`) and country (`CF-IPCountry`) after a Worker subrequest. The country decides the R-6 device block, so a wrong country would block devices;
   - live connections (WebSockets) for Control, the only interactive application.
3. **Staging cannot test the Worker.** Staging is on Dynu (`vixly.freeddns.org`), not on Cloudflare.
4. **A company's own domain** (white label) does not pass through the `*.vixly.ltd` route. It needs Cloudflare's custom domains service (Cloudflare for SaaS); the same Worker can then serve the "has moved" and maintenance pages.
5. **Still owed from ADR-0007:** a Platform run record with alerts and "run now", and pausing billing and collections during a maintenance window.

## 4. Tests

- `VixlyEdgeHostTests` (6):
  - with the secret, the forwarded address is used;
  - without the secret, with a wrong one, for a host outside the list, from outside Cloudflare, and without settings, it is ignored.
- `OperationsSignInCleanupDatabaseTests`: across companies, old sessions and links go, recent ones stay, and a second run removes nothing.
- `PlatformSignInCleanupDatabaseTests`: the same for Control.
