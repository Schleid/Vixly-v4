# Slice 0: Foundation (Operations and Customer) - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 3) |
| Status | **Built 2026-10-17; see the [verification record](SLICE-0-VERIFICATION.md).** Built on the [sweep](SLICE-0-1-FOUNDATION-AND-SETUP-SWEEP.md) and the [plane document](OPERATIONS-PLANE.md). |
| Depends on | Platform Stage 1 and Public Stage 2 as delivered; ADR-0014, ADR-0017, ADR-0019 |

## 1. Outcome

Both Stage 3 hosts run, empty but complete: `Operations.Web` answers on `{slug}.techcrafters.ltd` for a live tenant and refuses unknown or suspended ones; `Customer.Web` answers on `{slug}.techcrafters.ltd/customer`; `Operations.Api` accepts only signed callers. The `ops` schema exists in the global SQL script; the architecture tests cover both planes. A tenant cannot see another tenant's data by construction, and a test proves it. No business screen yet.

## 2. Projects created

| Project | Holds |
|---|---|
| `Vixly.Operations.Domain` | `OperationsEntity` base (with `TenantId`), first errors |
| `Vixly.Operations.Application` | `OperationsAuthorizationContext`, `IOperationsAuthorizationGate`, ports (Platform client, clock, outbox, unit of work), audit trail |
| `Vixly.Operations.Infrastructure` | `OperationsDbContext` (schema `ops`), tenant query filter, Platform client, outbox store, Data Protection store |
| `Vixly.Operations.Api` | Internal host: caller tokens, health |
| `Vixly.Operations.Web` | Staff host: tenant resolution, cookie scheme, outbox and clock runner, health, error page |
| `Vixly.Operations.Resources` | en, fr, es texts |
| `Vixly.Customer.Web` | Customer host at `/customer`: cookie scheme, Operations.Api client, health |
| `Vixly.Customer.Infrastructure` | Data Protection keys in the `customer` schema only (ADR-0010) |
| `Vixly.Customer.Resources` | en, fr, es texts |
| `Vixly.Contracts.Operations` | Error shape and caller-token claims (no endpoints yet) |
| `Vixly.Operations.slnx` | Both planes, their tests and the shared projects |

`Customer.Domain` and `Customer.Application` stay README-only until a slice needs them (Customer has read models only).

## 3. Decisions

### Tenant isolation (D-F1)

- Every Operations entity derives from `OperationsEntity`, which carries `TenantId`, set in the constructor and never changed. Child tables carry it too.
- **System tables are the only exception**, each marked `IOperationsSystemEntity` and listed here: the outbox (`OutboxMessages`, which records the tenant it concerns but is read by the dispatcher across tenants), `JobRuns`, `PlatformFeedPositions` and `DataProtectionKeys`. The architecture test allows exactly these.
- `OperationsDbContext` applies a global query filter `TenantId == current tenant` to every entity. The current tenant comes from the authorization context; there is no default, so a query outside a tenant context throws instead of returning everything.
- Saving checks that every added or modified entity belongs to the current tenant.
- Cross-tenant work (clocks, the event feed) runs through a separate, explicitly named system context and is reviewed per use.
- **Tests:** an architecture test fails any entity without `TenantId` that is not on the system list; an integration test reads and writes as tenant A and proves tenant B's rows are invisible and unwritable.

### Tenant from the host name (D-F2, sweep D-O9)

- `Operations.Web` takes `{slug}` from the host (`TenantHosting:BaseDomains`, H-1) and asks Platform `GET /internal/v1/portal/{slug}`, cached 60 seconds. `Customer.Web` asks `Operations.Api` instead, so only Operations calls Platform (D-F4).
- Platform answers "not found" for unknown, deactivated and banned tenants: a 404 page with no tenant name. Suspended: a localized "not available" page. Active with access level Read-only or Payment required: staff may sign in; Slice 1 decides what they may do.
- **Setting up** (Platform change, section 5): for a tenant still `Draft` or `PendingActivation` (a tenant from the public sign-up stays `Draft` until it is reviewed), Platform answers the `operations` caller "setting up" instead of "not found"; every other caller still gets "not found", and Operations.Api tells Customer.Web "not found" too. Operations then serves only the owner setup pages (Slice 1), never the staff or customer screens.
- A signed-in session is bound to its tenant; a session cookie presented on another tenant's host is refused.
- Development maps `localhost` to a configured test slug.

### Platform client (D-F3)

- ES256 service tokens (ADR-0014). Operations is a registered caller with scopes `tenant`, `portal` and `events`; Platform's caller list gains the `operations` key in configuration.
- Answers cached as Platform allows (entitlements 5 minutes, portal 60 seconds); a device check positive answer at most 5 minutes.
- The event feed (`GET /events`) is read by a clock every minute; Operations stores the last position. Tenant status changes drop the tenant cache at once.
- Platform errors arrive as code plus text (ADR-0014); Operations shows its own text for known codes and the generic error with a reference otherwise (S-9).

### Customer.Web to Operations.Api (D-F4)

- Same token design as ADR-0014: Customer.Web is the registered caller `customer` with the single scope `customer`; it signs a 60-second, single-use ES256 token carrying the tenant and, once signed in, the customer id.
- Customer.Web never calls Platform. Operations.Api answers it which tenant a host belongs to, from Operations' own Platform cache.
- Operations.Api trusts the customer id only from that token and checks ownership on every record (S-3).
- Operations.Api is never published by Edge (cross-plane rule 3) and has its own `AllowedHosts`.

### Cookies and keys (D-F5)

| Host | Cookie | Path | Key ring |
|---|---|---|---|
| Operations.Web | `__Host-vx4-ops` (session id only, checked against the database) | `/` | `ops.DataProtectionKeys`, app `Vixly.V4.Operations.Web` |
| Customer.Web | `__Secure-vx4-customer` (session id only, checked through Operations.Api); its antiforgery and culture cookies are `__Secure-vx4-customer-af` and `__Secure-vx4-customer-lang` | `/customer` | `customer.DataProtectionKeys`, app `Vixly.V4.Customer.Web` |
| Operations.Web | `__Host-vx4-device`: the device id and, for browsers without a usable key pair, the rotating secret (Slice 1) | `/` | same as above |

One scheme per process (S-4). Both apps share one origin, so the browser also sends the staff cookies (`Path=/`) to `/customer`. Therefore:
- the customer cookie never reaches a staff route (its path is `/customer`);
- Customer.Web ignores every staff cookie: it never authenticates from them and never logs them (test);
- Customer.Web's CSP and headers are its own; no staff page is framed or scripted from `/customer`.

### Commits and abandoned requests (D-F10, ADR-0019)

`OperationsUnitOfWork` saves without the request's cancellation token once the commit starts, and every host calls `UseVixlyClientDisconnects()`, as Platform does.

### Background work (D-F6, sweep D-O11)

The outbox, the Platform event-feed reader and later clocks run as hosted services inside `Operations.Web`, as Platform does in its console. Each run is recorded in `ops.JobRuns`; failures are logged with the message id (as B10).

### Security code (D-F7, [ADR-0020](../decisions/ADR-0020-shared-security-library.md), rejected)

Platform and Operations are separate apps that share data only through their APIs (owner, 2026-10-17). Operations therefore has its own security code in `Operations.Infrastructure`: service-token signing (to Platform) and checking (Customer.Web to Operations.Api) in Slice 0; password hashing, password policy, TOTP and passkeys in Slice 1. It follows the same rules as Platform (ADR-0006, ADR-0014) with its own tests.

### Database (D-F8)

- Schema `ops`, `ConnectionStrings:Operations`, login `vixly_operations_app` with rights on `ops` only (D-1); migrations history table in `ops`.
- The one global SQL script gains the Operations migrations and logins; the Customer plane's login has rights on `customer` only.
- The `ValueGeneratedNever` convention (D-5) and the no-silent-catch rule (D-6) as in Platform.

### Errors and texts (D-F9)

`OperationsErrors` codes start `ops.`; Customer texts start `customer.`. Resources in en, fr, es with the L-10 build checks. Tenant-facing resources use `{brandName}`, and a check fails a literal "Vixly" in them (ADR-0012 W-1).

## 4. Architecture tests added

- Dependency rules for every Operations and Customer project (section 3 of the standard).
- One type per file, headers, BOM and CRLF, three files per use case, use-case gate first.
- Entities prefixed `Operations`, tables unprefixed in `ops`, every entity carries `TenantId`.
- No literal text in markup; every `T["key"]` exists; no "Vixly" in tenant-facing resources.
- Double-click rule (ADR-0019) for `Operations.Web` and `Customer.Web`.

## 5. Platform change needed

- Portal lookup: answer "setting up" (with the tenant id) to the `operations` caller for a `Draft` or `PendingActivation` tenant; every other caller still gets "not found".
- Caller entries `operations` (scopes `tenant`, `portal`, `events`); the `customer` caller is registered with Operations.Api, not with Platform.

## 6. Anonymous endpoints

| Endpoint | Host | Limit |
|---|---|---|
| `GET /health/live`, `/health/ready` | All three | none |
| Not-found and not-available pages | Operations.Web, Customer.Web | 60 per minute per IP |

## 7. Customer side

`Customer.Web` runs at `/customer` with its own cookie, key ring and Operations.Api client, and shows a placeholder portal page, the "not found" page for unknown hosts and tenants being set up, and the "not available" page for suspended tenants. The tenant's name and look arrive with sign-in in Slice 2.

## 8. Done when (in addition to the slice gate)

- A query without a tenant context throws; a tenant-B row is invisible and unwritable from tenant A (integration test).
- Unknown, deactivated and banned slugs give 404; suspended gives "not available"; setting-up gives only the setup pages; live gives the page (HTTP tests).
- Operations.Api refuses a missing, expired, replayed or wrong-scope caller token; Customer.Web's token is accepted only with the `customer` scope, and only for the tenant it names.
- The customer cookie never reaches a staff route, and Customer.Web never authenticates from a staff cookie (HTTP tests).
- The global SQL script builds `ops` and `customer` with their logins; the app login cannot write outside its schema.
- The architecture tests cover both planes and pass.

## 9. Owner decisions

D-F7: no shared security library (ADR-0020 rejected, 2026-10-17). Everything else follows the standard and the sweep.
