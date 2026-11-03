# First install of V4 on a new server (production)

| | |
|---|---|
| Date | 2026-10-23 |
| For | The first time V4 goes on a server: production at SmarterASP.net. After that, use [Updating production](PRODUCTION-UPDATE-RUNBOOK.md) |
| Settings | [Settings of the V4 sites](SETTINGS.md): what each site needs, and what is a secret |
| Lesson from staging (2026-10-22) | The sites failed to start (500.30) on missing settings. Since 2026-10-23, a site with a missing required setting shows a page naming it instead |

Tick each line. Never write a password or key in this file or in a ticket.

## 1. Hosting (SmarterASP.net control panel)

| ✓ | Step |
|---|---|
| ☐ | **Six sites:** Control (`control.vixly.ltd`), Platform API (`api.vixly.ltd` and its internal host name), Operations Web (`*.vixly.ltd`), Operations API (`ops-internal.vixly.ltd`), customer portal (under `*.vixly.ltd/customer`, as on staging), public site (`vixly.ltd`, `www.vixly.ltd`). While V3 still holds `vixly.ltd`, follow the side-by-side plan for the names. |
| ☐ | **Runtime:** .NET 10, the ASP.NET Core module, and `ASPNETCORE_ENVIRONMENT=Production` on each site (in its `web.config`) |
| ☐ | **Uploads folder:** one folder **outside every site folder** (for example `vixly-files` beside the sites), with write permission for both Operations sites. It becomes `OperationsFiles:Root` on Operations Web and Operations API. |
| ☐ | **Public site:** a keys folder outside the site folder, write permission, `PublicSite:KeysPath` |
| ☐ | **Keep awake:** an uptime ping to `/health/live` of Control, Platform API and Operations Web every 5 minutes, so the background jobs keep running |

## 2. Cloudflare

| ✓ | Step |
|---|---|
| ☐ | **DNS:** the public names proxied (orange). The internal names (Platform's internal host, `ops-internal.vixly.ltd`) **DNS only**, not proxied (ADR D-K2) |
| ☐ | **SSL/TLS:** Full (strict); a certificate on each SmarterASP site |
| ☐ | **Turnstile:** one widget for `vixly.ltd` (public sign-up) and one for `*.vixly.ltd` (portal sign-up). Note the site keys and secrets. |
| ☐ | **Cloudflare Access** in front of Control (as on staging) |

## 3. Database

| ✓ | Step |
|---|---|
| ☐ | **Create** the database in the control panel; note its user |
| ☐ | **Run** `build/scripts/sql/vixly-v4-database.sql` from SSMS on your PC, **with that database chosen as the current database** (toolbar). The script then works in it and creates no other database. On shared hosting, leave the passwords in section 0 **empty**: the hosting user cannot create server logins, and the connection strings use the control panel's user |
| ☐ | **Check:** the script ends without errors, and each schema's `__EFMigrationsHistory` holds the counts the release notes give |

## 4. Keys and secrets (on your PC, once)

| ✓ | Step |
|---|---|
| ☐ | **Three caller key pairs:** `dotnet run build/tools/new-caller-key.cs -- <key id>` for `operations`, `customer` and `public`. Put each private key straight into its environment variables; never in a file. |
| ☐ | **Mail password:** `noreply@vixly.ltd` (`PlatformMail__Password`, `OperationsMail__Password`) |
| ☐ | **Turnstile secrets:** `PlatformBotCheck__SecretKey`, `CustomerBotCheck__SecretKey` |
| ☐ | **Stripe:** `PlatformPayments__Stripe__SecretKey` and `PlatformPayments__Stripe__WebhookSecret`, when card payments start |
| ☐ | **Map provider:** an account and its tile address (`OperationsMaps:TileUrl`, `Attribution`) |

## 5. Settings per site
For each site, every **R** line of [SETTINGS.md](SETTINGS.md):
- **plain values** in `appsettings.Production.json`;
- **secrets** as environment variables in `web.config`.

| ✓ | Site | Watch for |
|---|---|---|
| ☐ | Platform API | `PlatformInternal:Hosts` and the `operations` and `public` callers with their public keys |
| ☐ | Control | Mail; `PlatformSubscriptionClock:Enabled=true` |
| ☐ | Operations API | `OperationsInternal:Hosts`, the `customer` caller, the **same** `OperationsFiles:Root` as Operations Web |
| ☐ | Operations Web | `OperationsPlatform:*` with the `operations` private key, `OperationsFiles:Root` |
| ☐ | Customer portal | `CustomerOperations:*` with the `customer` private key; Turnstile |
| ☐ | Public site | `PublicSite:*` with the `public` private key; `RegistrationOpen` as decided |

## 6. Copy and start
1. **Order:** Platform API, Control, Operations API, Operations Web, customer portal, public site.
2. **Each site:** publish once to a folder, copy it with WinSCP, then open its address.
   - **"This site is not set up yet":** set the settings it names and recycle the site.
   - **Otherwise:** `/health/ready` must say `Healthy`. `Degraded` means a warning; read the log.

## 7. First people and the first company

| ✓ | Step |
|---|---|
| ☐ | **First SuperUser:** from the repository on your PC, with the production connection string only in that terminal:<br>`$env:ConnectionStrings__Platform="..."`<br>`dotnet run --project src/Platform/Vixly.Platform.Web -- --setup-first-superuser --email you@... --first-name ... --last-name ...`<br>It prints a one-time invitation link (72 hours). Close the terminal afterwards. |
| ☐ | **Sign in to Control:** set up the authenticator, then the roles and the IT people (including the maintenance and jobs rights) |
| ☐ | **Plans, legal documents and billing settings** in Control, as on staging |
| ☐ | **The company:** register it by invitation, **with the same slug as in V3**; the owner signs in at `{slug}.vixly.ltd/staff` and saves Settings |
| ☐ | **Its locations, services and prices** in Operations, with the **same warehouse codes as V3** (`US-FLL`, `HT-CAY`) |
| ☐ | **Freeze V3**, back it up, then copy the people and open work: `build/tools/V3Migration` (dry run, read the report, then `--apply`); see its README |
| ☐ | **Do the report's to-do list:** roles for the staff it names, anything marked PROBLEM |
| ☐ | **Last check, with a real browser:** sign-up on the public site (Turnstile), a staff sign-in, the customer portal, one email received |

## 8. After
- **Keep these files outside the repository** (a password manager, for example): the settings files, the `web.config` files, and the list of key ids.
- **Every later release:** follow [Updating production](PRODUCTION-UPDATE-RUNBOOK.md) and its release notes.
