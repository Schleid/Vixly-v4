# Settings of the V4 sites

| | |
|---|---|
| Date | 2026-10-23 |
| Checked by | Each site, before it starts (`Vixly.Shared.Hosting.Settings`): the rules live in `PlatformWebSettings`, `PlatformApiSettings`, `OperationsWebSettings`, `OperationsApiSettings`, `CustomerWebSettings` and `PublicSiteSettings`. Keep this page in step with them. |
| Used by | [First install](FIRST-INSTALL-CHECKLIST.md), [Updating production](PRODUCTION-UPDATE-RUNBOOK.md), the release notes |

## How the check works
- **Required (R):** a required setting missing or wrong stops the site from starting. Outside Development, every request then gets **"This site is not set up yet"** (503) listing the setting names and their environment-variable form, never a value. `/health/live` still answers. The log has the same list, at Critical.
- **Warning (W):** the site starts. The log has a warning when the site starts, and `/health/ready` reports `Degraded` (the HTTP status stays 200).
- **In Development** every problem is a warning: local defaults and user secrets fill the gaps.
- **Where settings go:**
  - **Plain values:** in `appsettings.Production.json` (or `appsettings.Staging.json`).
  - **Secrets (S):** as environment variables only, never in a file that is copied or committed. The variable name is the key with `__` for `:`, for example `PlatformMail__Password`. On SmarterASP.net, set them in the control panel or in the site's `web.config` (`<environmentVariable>`); `web.config` is excluded from every copy.
- **Lists** (shown as `[]`): `Key__0`, `Key__1`… as variables, or a JSON array in the file.
- **Every site also checks:**
  - `AllowedHosts` (W): the site's own host names, never `*`;
  - `Hosting:EdgeSecret` (R, S, 32 or more characters), once `Hosting:EdgeHosts` is set for the Cloudflare Worker.

## Control (Platform.Web)

| Setting | | What it is |
|---|---|---|
| `ConnectionStrings:Platform` | R, S | The Platform database, login `vixly_platform_app` |
| `PlatformHosting:ConsoleBaseUrl` | R | `https://control.vixly.ltd` |
| `PlatformHosting:PublicBaseUrl` | R | `https://vixly.ltd` |
| `PlatformTenants:TenantBaseDomain` | R | `vixly.ltd` |
| `PlatformMail:FromAddress` | R | `noreply@vixly.ltd` |
| `PlatformMail:SmtpHost` | R | The mail server (unless `PlatformMail:PickupDirectory`) |
| `PlatformMail:Password` | W, S | Without it every email fails |
| `PlatformPayments:Stripe:SecretKey` | W, S | Card payments are off without it |
| `DataProtection:KeyEncryptionKey` | R, S | Locks Platform's key ring in the database (staff sign-in, authenticator keys): 32 random bytes, base64. **The same value on Control and Platform API**, different from Operations' |
| `PlatformPayments:Stripe:WebhookSecret` | W, S | The same value as the API's: without it Control reports the card provider keys as not configured |
| `PlatformSales:TechOs:Url` | W | TechOS's full event address: `https://techcrafters.ltd/api/sales-sources/vixly/events` (staging `https://techcrafters.dyndns.ws/api/sales-sources/vixly/events`); the site alone is refused |
| `PlatformSales:TechOs:Secret` | W, S | The secret TechOS holds for the Vixly source |
| `PlatformSales:TechOs:Source` | | `vixly`, in Control's `appsettings.json`; an older `appsettings.json` without it turns the feed off |
| `PlatformSubscriptionClock:Enabled`, `PlatformOutbox:Enabled` | | `true` (the hourly jobs and the email outbox) |
| `Edge:MaintenanceUrl` | | `https://api.vixly.ltd/__edge/maintenance`, once the Cloudflare Worker is set up ([setup](../../build/edge/maintenance-worker/README.md)). Empty: nothing sent (staging) |
| `Edge:MaintenanceSecret` | S | 64 hexadecimal characters, the Worker's `EDGE_MAINTENANCE_SECRET` |

## Platform API

| Setting | | What it is |
|---|---|---|
| `ConnectionStrings:Platform` | R, S | As Control |
| `PlatformHosting:*`, `PlatformTenants:TenantBaseDomain`, `PlatformMail:*`, `PlatformPayments:Stripe:SecretKey`, `DataProtection:KeyEncryptionKey` | R / W | As Control (the same key-encryption key) |
| `PlatformApi:AllowedOrigins[]` | W | `https://vixly.ltd` |
| `PlatformBotCheck:SecretKey` | R, S | Cloudflare Turnstile secret, unless `PlatformBotCheck:Enabled=false` |
| `PlatformInternal:Hosts[]` | R | The host name Operations calls (internal API) |
| `PlatformInternal:Callers` | R | Per caller (`operations`, `public`, later `edge`): `Scopes[]` and `Keys:{keyId}` = the caller's **public** key |
| `PlatformPayments:Stripe:WebhookSecret` | W, S | Stripe events are refused without it |

## Operations Web (company websites and staff)

| Setting | | What it is |
|---|---|---|
| `ConnectionStrings:Operations` | R, S | The Operations database, login `vixly_operations_app` |
| `TenantHosting:BaseDomains[]` | R | `vixly.ltd` |
| `OperationsPlatform:BaseUrl` | R | `https://api.vixly.ltd/` |
| `OperationsPlatform:CallerKeyId` | R | The key id Platform knows Operations by (in Platform's `PlatformInternal:Callers:operations:Keys`) |
| `OperationsPlatform:CallerPrivateKey` | R, S | P-256 private key, base64 PKCS#8 |
| `DataProtection:KeyEncryptionKey` | R, S | Locks the key ring in the database: 32 random bytes, base64 (44 characters). **The same value on Operations Web and Operations API.** Keep a copy: without it the companies' saved keys cannot be read. To change it, set the new value and put the old one in `DataProtection:PreviousKeyEncryptionKeys` |
| `OperationsLinks:PortalUrlTemplate` | R | `https://{slug}.vixly.ltd` |
| `OperationsFiles:Root` | R | Uploads. **Outside every site folder**, existing, writable, and **shared with Operations API**. A folder inside the site is refused, because an update would erase it. |
| `OperationsMail:FromAddress`, `SmtpHost` | R | As Control |
| `OperationsMail:Password` | W, S | |
| `OperationsMaps:TileUrl`, `Attribution` | W | The map provider; maps are off without it |
| `Edge:BrandUrl` | | `https://api.vixly.ltd/__edge/brand`, once the Cloudflare Worker is set up. Empty: nothing sent (staging) |
| `Edge:BrandSecret` | S | 64 hexadecimal characters, the Worker's `EDGE_BRAND_SECRET` |
| `OperationsPrintHelper:AppName` | | `Vixly Print` |

## Operations API

| Setting | | What it is |
|---|---|---|
| `ConnectionStrings:Operations`, `OperationsPlatform:*`, `OperationsLinks:PortalUrlTemplate`, `OperationsFiles:Root`, `OperationsMail:*`, `DataProtection:KeyEncryptionKey` | R / W | As Operations Web, with **the same** `OperationsFiles:Root` and **the same** key-encryption key |
| `OperationsInternal:Hosts[]` | R | `ops-internal.vixly.ltd` |
| `OperationsInternal:Callers` | R | `customer`: `Scopes` = `["customer"]`, `Keys:{keyId}` = the portal's **public** key |

## Customer portal

| Setting | | What it is |
|---|---|---|
| `ConnectionStrings:Customer` | R, S | The customer database (its key ring), login `vixly_customer_app` |
| `TenantHosting:BaseDomains[]` | R | `vixly.ltd` |
| `CustomerOperations:BaseUrl` | R | `https://ops-internal.vixly.ltd/` |
| `CustomerOperations:CallerKeyId` | R | Matches Operations API's `OperationsInternal:Callers:customer:Keys` |
| `CustomerOperations:CallerPrivateKey` | R, S | P-256 private key, base64 PKCS#8 |
| `DataProtection:KeyEncryptionKey` | R, S | Locks the portal's key ring: 32 random bytes, base64; its own value |
| `CustomerBotCheck:SiteKey`, `SecretKey` | R (secret S) | Cloudflare Turnstile, unless `CustomerBotCheck:Enabled=false` |

## Public site

| Setting | | What it is |
|---|---|---|
| `PublicSite:PlatformApiBaseUrl` | R | `https://api.vixly.ltd/` |
| `PublicSite:CanonicalBaseUrl` | R | `https://vixly.ltd` |
| `PublicSite:TurnstileSiteKey` | R | When `PublicSite:RegistrationOpen=true` |
| `PublicSite:CallerKeyId`, `CallerPrivateKey` | W (key S) | Without them Platform counts limits per server, not per visitor |
| `PublicSite:KeysPath` | W | Form keys, **outside the site folder**; without it open forms fail after each restart |
| `PublicSite:SalesEmail` | W | `sales@techcrafters.ltd` |

## Request timing (every site)

Performance plan P-1 (owner, 2026-11-04). Neither is a secret; both are optional.

| Setting | | What it is |
|---|---|---|
| `VixlyTiming:SlowRequestMs` | optional | A request slower than this is logged as a warning (`Vixly.Timing`) with its route pattern, its SQL commands and its calls to other sites. Default `1000`; `0` logs none |
| `VixlyTiming:ShowHeader` | optional | `true` adds a `Server-Timing` header (SQL, calls, total). Only Operations staff pages for owners, and Control for signed-in staff; never the public site, the customer portal or the APIs. Default `false`. Turn on for a measurement, then off |

## Background jobs start (Operations Web and Control)

Performance plan P-5 (owner, 2026-11-04). Neither is a secret; both are optional.

| Setting | | What it is |
|---|---|---|
| `VixlyJobs:StartDelaySeconds` | optional | How long after start-up the first background job begins. Default `20` (0 to 600) |
| `VixlyJobs:SpacingSeconds` | optional | How much later each next job begins. Default `5` (0 to 120). `0` and `0` start every job at once, as before |

## Keeping the sites awake

SmarterASP gives no control over the app pools' idle time-out. An outside uptime monitor calls each site's `/health/live` every 5 minutes (release notes, section 42): `vixly.ltd`, `control.vixly.ltd`, `api.vixly.ltd`, one company address (`galeo.vixly.ltd`) and its `/customer`, and `ops-internal.vixly.ltd`. `/health/live` answers one word and reads nothing.

## Making a caller key pair
- **Pairs:** one per caller:
  - Operations → Platform (`operations`);
  - customer portal → Operations (`customer`);
  - public site → Platform (`public`).
- **Make one,** on your PC, with .NET 10:
  ```
  dotnet run build/tools/new-caller-key.cs -- ops-2026-10
  ```
  It prints the key id, the **public** key and the **private** key, once, to your console only.
- **Where each part goes:**
  - **The private key:** straight into the caller's environment variable (for example `OperationsPlatform__CallerPrivateKey` on Operations Web **and** Operations API). Never save it in a file.
  - **The key id:** the caller's `...:CallerKeyId`.
  - **The public key and the key id:** the called site, as `PlatformInternal:Callers:operations:Keys:ops-2026-10` (or `OperationsInternal:Callers:customer:Keys:...`). They are not secrets and can sit in `appsettings.Production.json`.
- **Rotating:** add a second key id on the called site, switch the caller to it, then remove the first.
