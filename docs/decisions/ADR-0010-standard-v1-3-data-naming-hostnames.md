# ADR-0010: Global Standard v1.3: plane schemas, table naming, V4 hostnames and independence from V3

- Status: **Accepted** (owner decisions, 2026-10-13)
- Date: 2026-10-13
- Deciders: Jude (owner)
- Affects: Global Standard sections 1, 2, 3, 4.1, 6 (S-4, S-5, S-11), 8 (D-1, D-5, D-6), 9, 10, 12, 13; Platform plane document; every plane's Infrastructure

## Context

The Platform V4 upgrade plan (walkthrough and code review of V3, October 2026) raised decisions the v1.2 standard did not settle:

- V3 keeps every plane's tables in `dbo` of one database with one migrations history table. The owner wants Platform data and Operations data to be able to share a database or live in separate ones, with no code change.
- With a schema per plane, the v1.2 table rule (`OperationsInvoices`) repeats the owner twice in SQL (`ops.OperationsInvoices`).
- V4 is a new system beside V3, not an in-place replacement, and must not collide with V3's hostnames or its domain-wide `.vixly.ltd` cookies.
- Public must go live with Platform, before Operations.
- The V3 collections defect (child rows sent as UPDATEs, failure swallowed by a bare catch) showed two rules worth making global.

## Decision

1. **D-1 amended.** Each plane owns a SQL schema (`platform`, `ops`, `customer`), its own migrations history table in that schema, its own connection string (`ConnectionStrings:<Plane>`) and its own SQL login with rights on that schema only. Planes may share a database (`VixlyV4`) or use separate ones (`VixlyV4Platform`, `VixlyV4Operations`, `VixlyV4Customer`); nothing may depend on which. No cross-schema foreign keys, joins, views or synonyms. Migrations ship as EF bundles applied by the pipeline; no host migrates at startup.
2. **Naming option B.** Entity classes keep the owner prefix (`OperationsShipment`); `DbSet`s and tables drop it inside the plane schema (`ops.Shipments`). Each EF configuration names its table explicitly. Option A (prefix in table names too) was rejected as stutter; option C (`Tenant…` / `Customer…` subjects) was rejected because it needed ten judgment calls and collides with the Customer plane prefix. ADR-0002 O-1 stands for classes.
3. **Hostnames until migration** are on `techcrafters.ltd`: `vixly.techcrafters.ltd`, `vixly-control.techcrafters.ltd`, `vixly-api.techcrafters.ltd`, `{slug}.techcrafters.ltd`, with `-staging` variants. Base domains are configuration. Reserved slugs are enforced at registration. `vixly.ltd` names move to V4 after the last tenant migrates.
4. **Cookies** are host-only with V4 names: `__Host-vx4-*`, and `__Secure-vx4-customer` for the path-scoped customer cookie. V4 ignores V3's `vx-*` cookies.
5. **Independence and migration.** V3 and V4 never call each other or share a database. Only the one-off migration tool under `build/` reads V3. Tenants move one at a time after end-to-end testing (freeze, migrate, reconcile, switch DNS, activate, mark `MigratedToV4`; 14-day rollback window). BCrypt hashes move unchanged.
6. **Build order:** Platform, Public, Operations, Customer, Edge. O-3 resolved: Control owns Public.
7. **Secrets** come from host environment variables, read only through `IConfiguration`; Google Secret Manager is the planned second source.
8. **New data rules:** D-5 (keys assigned in code are configured `ValueGeneratedNever()` by convention, with an architecture test) and D-6 (no silent catch).
9. The legacy Hybrid migration roadmap (Phases 1–6) is retired in favour of the V4 stage order; `src/Hybrid` stays reserved for V2+.

## Consequences

- Slice 0 gains an addendum before sign-off: `platform` default schema and history table, `ConnectionStrings:Platform`, the D-5 convention and test, the use-case-gate architecture test and a shared `UseVixlySecurityHeaders()`.
- CI later runs the Operations integration tests against both the shared and the separate database setups.
- SQL reads stay short (`platform.Invoices`); C# stays unambiguous (`PlatformInvoice`).
- The Customer plane gets a small `customer` schema for its Data Protection keys even though it reads business data through the Operations API.
- Global Standard updated to v1.3.
