# Slice 0: Foundation (Operations and Customer) - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 3), with one Platform change |
| Date | 2026-10-17 |
| Built on | The dev team's tree after Stage 2, B12 and the Stage 3 documents; not committed here: the dev team owns git |
| Design | [Slice 0 design](SLICE-0-FOUNDATION-DESIGN.md), [sweep](SLICE-0-1-FOUNDATION-AND-SETUP-SWEEP.md), [plane document](OPERATIONS-PLANE.md), ADR-0014, ADR-0017, ADR-0019, [ADR-0020](../decisions/ADR-0020-shared-security-library.md) (rejected) |
| Status | **Built and verified by automated tests; waiting for owner sign-off.** No business screen yet, by design |

## 1. What was built

- **Projects (`Vixly.Operations.slnx`, new):** `Contracts.Operations`; `Operations.Domain`, `.Application`, `.Infrastructure`, `.Api`, `.Web`, `.Resources`; `Customer.Infrastructure`, `.Resources`, `.Web`; seven test projects. `Customer.Domain` and `Customer.Application` stay README-only.
- **Tenant isolation (D-F1):** every tenant entity derives from `OperationsEntity` (tenant set once, never empty). `OperationsDbContext` filters every tenant table by the current tenant, throws when a tenant table is queried with no tenant, and refuses to save a row of another tenant. The system tables are exactly the outbox, job runs, the Platform feed position and the key ring, each marked `IOperationsSystemEntity`.
- **Tenant from the host (D-F2):** `Operations.Web` reads `{slug}` from the host and asks Platform `GET /internal/v1/portal/{slug}` (cached 60 seconds, forgotten at once when the Platform feed reports a status, address or subscription change). Unknown hosts: 404. Suspended: "not available" (403). Draft or pending activation: only `/setup`. Platform unreachable: "try again" (503), never 404.
- **Platform change:** the portal lookup answers "setting up" for a `Draft` or `PendingActivation` tenant, only to the caller `operations`.
- **Platform client (D-F3):** ES256 service tokens as the caller `operations` (60 seconds, unique id); the event-feed clock runs every minute in `Operations.Web`, keeps its position in `ops.PlatformFeedPositions` and records each run in `ops.JobRuns`.
- **Operations.Api (D-F4):** internal host only. Caller tokens checked for signature, audience, lifetime (at most 2 minutes), replay and scope; unknown host or endpoint gives 404. First endpoint: `GET /internal/v1/tenants/by-host/{slug}` (scope `customer`); a tenant being set up reads as not found to customers.
- **Customer.Web (D-F4, D-F5):** serves only under `/customer`; asks Operations.Api which tenant the host is (signed as the caller `customer`, cached 60 seconds); never calls Platform and never reads a staff cookie. Its cookies are `__Secure-vx4-customer*` with `Path=/customer`; its key ring is `customer.DataProtectionKeys`.
- **Outbox (D-F6):** messages typed `ops.*.vN`, written in the same commit, leased with `UPDLOCK, READPAST`, kept in order per stream, run in the message's tenant scope, backed off 1, 2, 4 minutes and set aside after 10 attempts.
- **Commits (D-F10, ADR-0019):** a commit that has started always finishes; both web hosts and the API call `UseVixlyClientDisconnects()`; both web shells load the form guard.
- **Status pages:** 60 per minute per address on each web host (section 6); over the limit gives 429.
- **Texts:** `OperationsText` (23 keys) and `CustomerText` (9 keys) in en, fr and es, with a text for every declared `ops.*` error. Neither says "Vixly".
- **Database:** migrations `OperationsFoundation` (schema `ops`: `DataProtectionKeys`, `JobRuns`, `OutboxMessages`, `PlatformFeedPositions`) and `CustomerFoundation` (schema `customer`: `DataProtectionKeys`). The one global SQL script now builds all three schemas and creates `vixly_operations_app`, `vixly_operations_migrator` and `vixly_customer_app` when their passwords are given.
- **Rules:** the architecture tests now cover Operations and Customer: entity prefixes and the tenant rule with its system list, use-case shape, error prefixes and texts, markup text keys and budgets, the double-click rule, form-guard shells, and no "Vixly" in tenant texts.

## 2. Automated checks

Release builds of `Vixly.Platform.slnx`, `Vixly.Public.slnx` and `Vixly.Operations.slnx`: no errors, no warnings.

| Test project | Tests |
|---|---|
| Operations.Domain.Tests (new) | 11 |
| Operations.Application.Tests (new) | 21 |
| Operations.Infrastructure.Tests (new, with SQL Server) | 16 |
| Operations.Api.Tests (new) | 14 |
| Operations.Web.Tests (new) | 12 |
| Customer.Infrastructure.Tests (new) | 1 |
| Customer.Web.Tests (new) | 19 |
| Platform.Domain.Tests | 301 |
| Platform.Application.Tests (one new) | 232 |
| Platform.Infrastructure.Tests (with SQL Server) | 62 |
| Platform.Api.Tests | 20 |
| Platform.Web.Tests | 42 |
| Public.Web.Tests | 23 |
| Shared.Kernel.Tests | 81 |
| Shared.UI.Tests | 9 |
| Architecture.Tests | 7,059 |

All 7,923 pass.

What the new tests cover:
- **Isolation (SQL Server):** tenant A sees only its rows and cannot find tenant B's row by id; adding or changing a tenant-B row from tenant A is refused, even when the row is attached by hand; a query with no tenant throws; the system scope sees every tenant. A test-only tenant entity stands in until Slice 3 brings real ones.
- **System tables (SQL Server):** a due message is leased once across two connections; a later message of a stream waits for the earlier one; a stale feed position write is refused; a commit cancelled before it starts writes nothing.
- **Host resolution:** each Platform status maps to its state; bad slugs never reach Platform; Platform down is an error, not "not found"; the feed clock forgets changed tenants, resumes from its saved position and records failures.
- **Outbox:** the handler runs in the message's tenant (or the system scope); failures keep their code and back off; a delivered message is never handled twice.
- **Operations.Api:** unknown host and unknown endpoint give 404; missing, forged, expired, over-long, wrong-audience and replayed tokens give 401; the wrong scope gives 403; a live tenant's answer is `no-store`; draft and pending tenants read as not found to customers; Platform down gives 503 with its code.
- **Operations.Web:** unknown and foreign hosts give the localized 404 page; suspended gives 403; Platform down gives 503; draft and pending tenants are sent to `/setup` and live tenants away from it; the tenant's language is the default; answers from Platform are cached; no page says "Vixly"; the status pages are limited per address.
- **Customer.Web:** only `/customer` is served; unknown hosts and paths give 404, suspended 403, Operations down 503; each call to Operations.Api is signed as `customer` for `vixly-operations-internal` and lives 60 seconds; answers are cached; staff cookies are ignored; the customer language cookie is read; the status pages are limited per address.
- **Signers:** the Operations token for Platform verifies with its public key and carries the tenant; no key turns it off; a key on the wrong curve is refused.
- **Global SQL script:** run twice on an empty server without errors; a second run changes nothing. With passwords given, the three logins are created and `vixly_customer_app` is refused on `ops`.

## 3. Fixed while building

- A status page shown in place of the asked path rendered the wrong page under `/customer`, because `UsePathBase` routes early. The tenant check now drops any endpoint already chosen, and both web hosts route after the tenant check.
- A 404 set before rendering made the component renderer return an empty page. The status is now set as the response starts, and only when nothing else (such as a 429) decided it; unknown paths use the router's not-found page.

## 4. Not covered here

- An end-to-end run against a live Platform with a real tenant: the HTTP tests run the real pipelines with Platform and Operations.Api stood in. It belongs with the Slice 1 run (owner setup and sign-in), which needs a tenant in each state.
- Customer sign-in, the customer session cookie and the per-tenant check on customer tokens: Slice 2. Today's only customer endpoint needs no tenant.
- An audit trail in Operations: arrives with the first audited action (Slice 1).
- The tenant's name and look on the status and portal pages: Slice 1 (staff) and Slice 2 (customer).

## 5. Deployment notes

- **Hosts:** `Operations.Web` on `{slug}.techcrafters.ltd` (Edge sends `/customer` to `Customer.Web`); `Operations.Api` on `vixly-ops-internal.techcrafters.ltd`, never published by Edge.
- **Platform.Api:** add the caller `operations` under `PlatformInternal:Callers` with scopes `tenant`, `portal` and `events` and its public key.
- **Operations.Web and Operations.Api:** `ConnectionStrings:Operations`; `OperationsPlatform:BaseUrl`, `CallerKeyId`, `CallerPrivateKey` (secret, the private half of the key above). Without a caller key the feed clock stays off and every host answers "try again".
- **Operations.Api:** `OperationsInternal:Hosts` and the caller `customer` with scope `customer` and Customer.Web's public key.
- **Customer.Web:** `ConnectionStrings:Customer`; `CustomerOperations:BaseUrl`, `CallerKeyId`, `CallerPrivateKey` (secret).
- **Both web hosts:** `TenantHosting:BaseDomains` (and `SlugSuffix` where used), same values in both.
- **Keys:** P-256, private half as base64 PKCS#8 and public half as base64 SubjectPublicKeyInfo, as for the Public caller (Stage 2).
- **Database:** run the newest global SQL script; give the Operations and Customer passwords in section 0 on staging and production.
- **Local ports:** Operations.Web 7404, Operations.Api 7405, Customer.Web 7406 (Development maps `localhost` to the slug `northwind`).
