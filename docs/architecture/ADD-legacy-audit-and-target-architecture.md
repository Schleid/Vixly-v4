> **Evidence document, not a rule set.** This is the audit of the legacy Vixly system and the target-architecture reasoning that led to V4. It was written with a migration framing; V4 is built from scratch. Where anything here conflicts with [`VIXLY-GLOBAL-STANDARD.md`](../global/VIXLY-GLOBAL-STANDARD.md), the Global Standard wins. In particular, its file-organization examples and the Version 1 "extraction" phases are superseded by sections 5 and 12 of the Global Standard. The `Shared.UI` ownership model (components take parameters and callbacks only; pages bind to DTOs) is superseded for feature components by [ADR-0001](../decisions/ADR-0001-component-architecture.md); only primitives stay display-only.

# Vixly → Blazor Hybrid Architecture Decision Document

2026-09-16 · Prepared for Jude

Vixly runs today as three independently deployable .NET 10 Blazor Server applications — Control/Platform, Operations, and Public — plus one plane-agnostic shared library (`VixlyShared`) that carries no UI code. The brief's fourth plane, Customer Portal, is **not** a separate deployable: it lives inside `Vixly.Operations.Web` at the `/customer` path, sharing the same process, memory cache, Data Protection key ring, and database context factory as internal warehouse/staff tooling, split apart only by a runtime cookie-scheme router. That fusion — not any single plane's code quality — is the largest architectural risk this audit found, and it is the first decision this document asks the owner to make explicitly before a hybrid migration proceeds.

All four planes render exclusively as Blazor **Server** (interactive server components over a persistent SignalR circuit). No Blazor WebAssembly, no .NET MAUI, and no offline code path exists anywhere in the tree today — including in the warehouse scanning workflows this brief wants to run offline-first. No Razor Class Library exists either: layout, FluentUI usage, CSS, and pages are independently re-implemented three times, once per plane, with zero shared component surface.

The repository also already contains a substantial, dated internal audit trail (2026-08-24 through 2026-08-27) that independently corroborates much of what this brief asked us to look for: unbounded list/timeline reads, N+1-shaped dashboard queries, a warehouse-scope authorization gap, a public tracking endpoint that can leak another customer's payment amount, no domain-event or outbox layer, and a production-readiness pass that closed 10 of 15 prior findings while leaving automated tests, cross-module paging, and a payment-provider adapter open. Full citations are in the audit section below.

Two of the brief's assumptions also do not match the current codebase and are called out rather than quietly assumed away: Public plane today ships only a subscription-plan catalog and tenant self-onboarding — no rate calculator or lead-capture module was found — and Control-plane wildcard/custom-domain routing is an approved, reviewed **design document that has not been built**; tenants resolve today by `{slug}.vixly.ltd` only.

The target architecture proposed here keeps the existing three-solution boundary (Platform / Operations / Public) **and keeps a single tenant hostname serving both customer and staff traffic** — exactly what the repository's own approved custom-domain design already intends. What changes is what sits *behind* that hostname: a thin edge routing tier splits `/customer/**` traffic to its own Customer Portal process instead of letting it share Operations' process, cache, and Data Protection keys, backed by a new `Vixly.Shared.UI` Razor Class Library and a reusable core application layer. Native shells (.NET MAUI Blazor Hybrid) are proposed only for Operations (warehouse) and Customer Portal, where offline access and native device integration (camera/scanner, secure credential storage) earn their cost. Public and Control stay server-rendered web — hybrid delivery buys them nothing. A transactional outbox is proposed as a prerequisite for any offline queue, since the current direct-write history model has a confirmed durability gap that an offline sync layer would only make worse.

This review was performed read-only against the connected Vixly repository on the owner's machine — source, configuration, and existing internal documentation only, no execution, no database connection, and no code, file, or configuration changes were made, consistent with this session's read-only constraint. Findings are cross-referenced wherever possible against the repository's own prior internal assessments (`Docs/repository-cleanup/`, `ControlPlane/docs/`), which are more authoritative than a single-pass external read because they include build/test verification this review could not repeat. Every recommendation below is a proposal requiring explicit owner approval before implementation; none has been implemented.

## Phase 1 — Four-Plane Audit

### Public Plane

`Vixly.Public.Web` (.NET 10, Blazor Server, FluentUI) currently ships two feature areas only — tenant self-onboarding and a subscription-plan catalog (`Features/Onboarding`, `Features/Plans`). No rate calculator or lead-capture module exists in this repository. `.MapStaticAssets()` (.NET 10 fingerprinted static assets) gives a good caching baseline, but because the app is exclusively interactive Blazor Server, marketing/landing pages ship no meaningful content without a live SignalR circuit to the origin — a real SEO and Core Web Vitals risk for anonymous and crawler traffic that nothing in the app offsets with static or streaming rendering. `Program.cs` registers no rate limiter of its own; Public relies entirely on `Vixly.Platform.API`'s downstream "Public" policy (100 req/min/IP), so any endpoint Public serves directly is unprotected at its own edge. On the positive side, the repository's own cleanup report confirms Public was deliberately decoupled from backend Application/Domain assemblies by introducing Public-owned wire enums with matching numeric values (`Docs/repository-cleanup/007-final-report.md`) — a boundary worth preserving in the hybrid target.

### Control / Platform Plane

`Vixly.Platform.Domain/Application/Infrastructure/Web` plus `Vixly.Platform.API` follow a layered Clean Architecture shape (`Docs/repository-cleanup/002-project-dependency-map.md`). Authorization is centralized through `IPlatformAuthorizationGate` / `PlatformUseCaseAuthorizationGate`, invoked by presentation orchestrators ahead of protected use cases (`ControlPlane/docs/production-security-observability-2026-08-24.md`). The wildcard/custom-domain routing this brief asked us to audit is an approved but **unimplemented** design (`ControlPlane/docs/custom-domains/002-custom-domain-technical-design.md`); current tenant resolution is `{slug}.vixly.ltd` only. The production-readiness review (`ControlPlane/docs/production-readiness-review-2026-08-24.md`) found and closed a Critical unauthenticated tenant-mutation test page, a JWT-audience-validation bug, and a staging `AllowedHosts="*"` wildcard; still open are zero automated test projects across the solution, unpaged tenant/invoice/subscription list reads, concurrency tokens missing on most aggregates beyond merchant/platform settings, and no concrete payment-provider adapter behind `IPlatformPaymentProvider`. Session security is comparatively strong here: cookies are explicit `HttpOnly`/`Secure`/`SameSite=Lax`, and Data Protection keys are persisted to the shared platform database keyed by a configurable `CookieVersion`, so all Control instances can safely share and deliberately rotate sessions — a materially better position than Operations (below). Recurring billing/collections jobs now use SQL Server application-lock leases for single-owner execution across instances.

### Operations Plane

`Vixly.Operations.Domain/Application/Infrastructure/Web`, .NET 10, Blazor Server. This project hosts internal warehouse/staff screens (counter sales, dispatch and bundle operations, staff, device management, reports) *and* customer-facing screens in the same process — see Customer Portal below. No `<Virtualize>` or `ShouldRender` override was found in the Operations Razor tree searched; bundle and dispatch list reads (`BundleRepository.GetBundlesAsync`, `DispatchRepository.GetDispatchesAsync`) load full object graphs with `.Include()`s and no server-side paging (`ControlPlane/docs/quality/003-performance-assessment-and-improvement-plan.md`, priority P1). On a warehouse floor doing continuous scanning, that combination — unbounded EF materialization feeding an un-virtualized Blazor Server render — is the most probable source of the UI re-rendering and responsiveness cost this brief asked us to find. No SignalR hub or WebSocket code exists beyond Blazor Server's own circuit transport: "real-time" today is the framework's default re-render-over-the-wire behavior, not an application-level event stream, and there is no offline capability anywhere — a dropped connection during scanning drops the circuit and any in-progress UI state with it. The platform's own event-architecture assessment (`ControlPlane/docs/architecture/events/001-current-state-event-architecture-assessment.md`) found no domain-event bus, outbox, or subscriber infrastructure; operational history is written directly inside the same transaction as the business write, with one confirmed exception — bundle creation commits the bundle *before* its history event is saved, so the event can be silently lost. This matters directly for hybrid migration: an offline scan queue has no existing outbox or idempotency layer to build on and would need one built first. Two open security findings sit in this plane: a warehouse-scoped staff permission that does not actually enforce the caller's warehouse when resolving cross-warehouse operational-event history (medium), and a logout error path that echoes a raw HTTP response body instead of a sanitized message (low) — both in `ControlPlane/docs/security/004-security-assessment-and-hardening-plan.md`, neither fixed in source. Data Protection keys are persisted to local `App_Data/DataProtectionKeys` per instance, unlike Control's shared-database approach, and have not been verified safe for horizontal scaling.

### Customer Portal

This is the most consequential finding relative to the brief's four-plane framing: **there is no separate Customer Portal deployable.** Customer-facing screens live inside `Vixly.Operations.Web` under the `/customer` path, authenticated by a second cookie scheme (`VixlyCustomerAuth`, cookie name `vixly.customer.auth`) selected at runtime by a policy scheme (`VixlySmartAuth`) that inspects the request path, the Blazor `_blazor` negotiate referer, and a customer-cookie sniff to decide which scheme a given circuit gets (`OperationPlane/src/Vixly.Operations.Web/Program.cs`). The consequence: Customer Portal shares its process, Data Protection key ring, in-memory cache, and `DbContext` factory with internal staff warehouse tooling. A crash, memory-pressure spike, or vulnerability in either surface affects the other — there is no process- or deployment-level isolation between "a customer viewing an invoice" and "a warehouse worker scanning inbound freight," a materially weaker boundary than the rest of the system already has between Control, Operations, and Public. Both staff and customer cookies use a deliberately short `ExpireTimeSpan` of 5 minutes with `SlidingExpiration=false` and a custom idle-deadline stamp on `OnValidatePrincipal` (`Vixly.Web.SessionSecurity.IdleSessionSecurity`), `Cookie.MaxAge=2 days`, `HttpOnly=true`, and `SecurePolicy=Always` for the customer cookie — a real, purpose-built design worth preserving, but one that is currently a browser cookie and does not map onto a native MAUI shell without a token/secure-storage redesign (see Code Examples). A `CustomerOnly` authorization policy requires a `CustomerId` claim, and `CustomerDashboardRules` correctly scopes both tenant and customer identity for invoice/credit visibility; however `GetTrackingTimelineUseCase`'s `PublicOnly` flag is visibility filtering, not customer-ownership authorization, and `RegisterInvoicePaymentUseCase` writes a public-visible history description containing the payment amount — a real cross-customer information-disclosure risk on exactly the package-tracking surface this brief calls out (`ControlPlane/docs/security/004`, medium severity, unresolved). `CustomerDashboardReader.GetAsync` issues eight sequential queries per dashboard load (plus a conditional ninth), and `CustomerSidebar` refreshes eligibility on two uncoalesced triggers — neither is severe alone, but both compound in a model where every round-trip also costs a SignalR round-trip to render. No browser `localStorage`/IndexedDB usage was found, which is expected for Blazor Server and is a reasonable starting point for the brief's "no browser localStorage" security goal — but it also means today's Customer Portal has no client-side cache to build an offline mode on top of.

## 4-Plane Architecture Matrix

| Plane | Current deployment | Target deployment | Performance strategy | Security controls | Data/state isolation |
| --- | --- | --- | --- | --- | --- |
| Public | Blazor Server web, no rate limiter of its own | Blazor Server web (unchanged) — hybrid delivery buys nothing for anonymous marketing/quote traffic | Prerendered/streaming SSR for landing + catalog; `Vixly.Shared.UI` components; edge/CDN caching in front of `.MapStaticAssets()` | Own rate-limit policy at the edge instead of relying solely on the API; keep existing Public-owned DTO decoupling from Platform assemblies | No tenant data held; stateless beyond onboarding session |
| Control / Platform | Blazor Server web + REST API, shared platform DB | Blazor Server web + API (unchanged) — no case for a native/offline Control app | Cursor/bounded paging on tenant, invoice, subscription lists; keep SQL-lease job coordination | OIDC/PKCE for API callers; close PR-004 (tests) and PR-013/014 (payment adapter, secrets) before any further plane depends on Control's API contract | One authoritative tenant/entitlement store; Operations and Public reach it only through the versioned API, never the DB directly |
| Operations (warehouse) | Blazor Server web, co-hosted with Customer Portal | **.NET MAUI Blazor Hybrid** (desktop/handheld scanner) + thin Blazor Server web fallback, both consuming `Vixly.Shared.UI` | `<Virtualize>` + bounded/paged bundle & dispatch queries; offline-first local store (SQLCipher) with a background sync queue built on a new outbox | Native credential vault (Windows DPAPI / Android Keystore) for staff tokens; enforce caller warehouse scope server-side (closes SEC-01) | Deployed and processed separately from Customer Portal; warehouse-scoped reads enforced at the query layer, not just the menu |
| Customer Portal | **Not a separate deployable** — fused into Operations Web under `/customer`, sharing process, cache, and DataProtection keys | Separate process behind a shared edge gateway at the same tenant hostname (see Target Architecture): **.NET MAUI Blazor Hybrid** for mobile/desktop customers + Blazor Server web for browser customers, split out of Operations Web | `<Virtualize>` on shipment/invoice lists; coalesce the 8-query dashboard read; offline read cache for tracking/invoices only (no offline writes without idempotency) | OAuth2/OIDC + PKCE, native secure storage instead of a shared server cookie; customer-owned authorization check added to tracking timeline (closes SEC-03) | Own process, own Data Protection key ring, own cache — no shared address space with warehouse staff tooling |

## Feature Gap Analysis vs. Industry Standards

Vixly today, per the audit above, is a package-forwarding/3PL operating system: tenant onboarding and a plan catalog (Public), warehouse receiving/bundling/dispatch and counter sales (Operations), milestone-based shipment history (`TrackingEvent`), invoicing with recurring billing and collections/dunning (Control + Operations), and a customer dashboard for shipments, invoices, and credit accounts. The comparison below checks that footprint against published feature checklists for modern logistics SaaS platforms, freight-forwarding software, warehouse management systems, and shipment-visibility portals — not to chase every enterprise TMS feature, but to flag what a buyer in this category would expect and either confirm it exists, is partial, or is genuinely missing.

| Category | Feature | Industry standard | Vixly today | Recommendation |
| --- | --- | --- | --- | --- |
| Customer self-service | Branded customer tracking portal | Expected baseline | **Present** — customer dashboard + milestone tracking timeline | Close the SEC-03 ownership gap already flagged, then extend milestone detail |
| Customer self-service | Proactive status notifications (email/SMS) | Expected baseline | **Missing** — notifications exist for collections/dunning and password reset, not shipment milestones | High priority: trigger on `TrackingEvent` writes once the outbox (already recommended) exists |
| Customer self-service | Public self-service rate/quote calculator | Standard for freight-forwarder sales workflow | **Missing** — Public plane ships only onboarding + plan catalog; Operations already has an origin-surcharge-zone pricing module to build on | High priority: closes the gap this brief itself assumed didn't exist |
| Customer self-service | Proof of delivery capture (signature/photo) | Standard | **Missing** — no capture on the `Delivered` event | Medium priority, low cost: attach to the existing Delivered event write |
| Customer self-service | Returns management workflow | Standard | **Missing** entirely | Medium priority — real gap given how mature invoicing/credit already is |
| Customer self-service | Self-service control actions (reschedule, redirect, hold-for-pickup) | Standard for mature visibility platforms | **Missing** | Lower priority; sequence after notifications and POD |
| Freight operations | Exception/hold detection and alerting | Standard | **Disconnected** — `TrackingEventBlockerEvaluator` already exists in source with no active caller | Near-zero-cost win: wire up code that's already written |
| Freight operations | Multi-modal shipment handling (air / ocean / land) | Standard for forwarders | **Partial** — distinct sea-parcel vs. general parcel use cases exist; no evidence of air-specific handling | Confirm with owner whether air freight is in scope before building further |
| Freight operations | Trade/customs document generation (BOL, AWB, customs declarations) | Standard for forwarders | **Partial** — a document numbering/registry system exists (`VixlyShared/Documents`) but no customs-specific documents were found | Scope only if Vixly/Galeo handles cross-border customs directly |
| Freight operations | External carrier connectivity (label purchase, rate shopping) | Standard for forwarders reselling capacity | **Not evidenced** | Scope question, not a gap by default: confirm whether any leg (e.g. last-mile) uses a third-party carrier that should be integrated |
| Warehouse operations | Receiving / put-away | Standard WMS baseline | **Present** — `ReceiveParcelByCarrierTrackingUseCase` and inbound reception module | — |
| Warehouse operations | Bin/location management | Standard WMS baseline | **Not evidenced** in the areas searched | Verify with the owner — likely a real gap once warehouses exceed simple bundle-level grouping |
| Warehouse operations | Barcode-driven pick/pack/dispatch | Standard WMS baseline | **Partial** — dispatch/bundle workflows exist, but the audit found no `<Virtualize>`/`ShouldRender` or scan-throttling, so it isn't tuned for real scanner throughput yet | Covered by the rendering-performance work already recommended under Target Architecture |
| Warehouse operations | Lot/batch/serial traceability | Standard WMS baseline | **Present in spirit** via parcel/shipment IDs and `TrackingEvent` history | — adequate for a parcel-forwarding business, not a manufacturing lot system |
| Billing & finance | Automated invoicing, recurring billing, dunning | Standard | **Present, mature** — `RegisterInvoicePaymentUseCase`, `RecurringInvoiceBackgroundService`, idempotent collections attempts | One of Vixly's strongest areas today |
| Platform & compliance | Documented external API / webhook surface for tenant integrations | Standard for a SaaS platform positioning (e.g. e-commerce auto-shipment creation) | **Missing** — today's API is internal (Operations/Public → Control), not a tenant-facing integration surface | Medium-high priority: a genuine SaaS-positioning differentiator, distinct from the internal API work already planned |
| Platform & compliance | Formal audit trail on administrative changes | Standard | **Partial** — operator identity now recorded on merchant config per the production-readiness review; not yet consistent everywhere | Extend the pattern already adopted rather than invent a new one |
| Platform & compliance | Cloud-native horizontal scalability | Standard | **Partial** — Control's SQL-lease job coordination and shared-database key storage scale; Operations' local Data Protection key storage (already flagged) does not | Same fix already recommended under Target Architecture, not a separate workstream |
| Analytics | Configurable, role-based KPI dashboards / lane and staff performance scorecards | Standard | **Partial** — an Operations Reports page exists at the route level only — it is an explicit placeholder ("Reports are under construction"); see the dedicated note below | High priority — a bigger, harder gap than the table format suggests; see the dedicated note below |

Reporting deserves more than one table row, since it changes the picture. Operations' Reports page (`Components/Pages/Reports/ReportsWorkspace.razor`, route `/reporting`) is not a thin report catalog — it is an explicit placeholder; its own header comment states "No use cases belong here yet" and "No business logic belongs here yet," and the rendered page reads "Reports are under construction." The only reporting capability that actually exists anywhere in the audited source is one PDF export, `GenerateInvoiceListingReportPdfUseCase` / `InvoiceListingReportBuilder`, reachable from Billing, not from the Reports menu. Control Plane has no dashboard, monitoring, or reporting page at all among its authenticated pages (`Vixly.Platform.Web/Components/Pages` has Tenants, Subscriptions, Registrants, BillingOperations, Settings, and Legal — no Dashboard, Monitoring, or Reports folder). So the brief's assumption of "global monitoring" on Control and general reporting on Operations isn't a partially built feature to extend; it's unbuilt on both planes. Recommend treating Reporting & Analytics as its own workstream rather than a single Analytics row: start from operational data that already exists and is cheap to expose — the Operations dashboard's existing tenant/warehouse-scoped activity query, the collections/dunning case data, and the invoice-listing report already built — before investing in a general-purpose report builder. Tenant theming and white-labeling is the other feature area big enough to warrant its own treatment; see the dedicated section below.

A few standard features are deliberately left off this table rather than marked "missing": AI-driven route optimization, RFID/IoT asset tracking, cold-chain/condition monitoring, and driver/fleet ELD management are common in enterprise TMS and last-mile fleet software, but nothing in the audit suggests Vixly currently operates its own fleet or handles temperature-sensitive freight — building any of them without that confirmed would be solving a problem Vixly may not have. Recommend the owner confirm relevance before they're scoped at all.

**If only five things get built next**, in priority order: (1) wire up the existing exception/hold evaluator — it already exists and has no caller; (2) milestone-triggered customer notifications, once the outbox lands; (3) the public rate/quote calculator, since it's both a real sales-funnel gap and something this brief itself assumed already existed; (4) proof-of-delivery capture on the Delivered event; (5) a documented external API surface for tenant integrations, as the platform's clearest SaaS-positioning gap.

Sources: [25 Features Every Modern Logistics Software Platform Needs in 2026](https://www.dreamztech.com/blog/features-every-modern-logistics-software-platform-should-include/) · [What Features Should Freight Forwarding Software Have? A Complete Checklist for 2026](https://logistaas.com/what-features-should-freight-forwarding-software-have-a-complete-checklist-for-2026/) · [Real-Time Shipment Visibility Platforms in 2026](https://locus.sh/blogs/real-time-shipment-visibility-platforms-2026/) · [11 Key Features List of A Warehouse Management System](https://msdynamicsworld.com/blog/11-key-features-list-warehouse-management-system)

## Phase 2 — Target Blazor Hybrid Architecture

### Rendering & UI speed

Operations' unbounded bundle/dispatch reads (`BundleRepository.GetBundlesAsync`, `DispatchRepository.GetDispatchesAsync`) and the absence of `<Virtualize>` anywhere in the Razor tree are the two concrete, evidenced blockers to a responsive warehouse UI, hybrid or not — fix the query shape first, since a hybrid rewrite on top of an unbounded query just moves the same slow load onto a mobile device's weaker CPU. Once lists are paged/projected, wrap freight, parcel, and dispatch lists in `<Virtualize Items="..." ItemSize="...">`, and override `ShouldRender()` on high-frequency scan-input components so a barcode scan doesn't force a full subtree re-render on every keystroke. Debounce/throttle the barcode input handler itself (a hardware scanner fires a burst of keystrokes far faster than a human types) rather than reacting per character.

### Network & payload efficiency

Keep REST+JSON for browser-facing traffic (Public, Control, Customer Portal web) — it is simple to cache, debug, and secure, and nothing in the audit points to REST as a bottleneck. Consider gRPC only for the Operations-Hybrid ↔ Control-API server path used by warehouse sync (device-to-server, not browser-to-server), where binary framing and streaming reduce payload for high-volume scan-event batches; do not introduce gRPC into any browser-hosted plane, since Blazor Server/WASM browser support for it is weaker than for plain HTTP. Enable response compression consistently (Operations and the API already do this; extend it to Public). For hybrid clients, add an offline-first local store — SQLite via EF Core, encrypted with SQLCipher — fed by a background sync service so a lost warehouse network connection degrades to "queue locally, sync later" instead of dropping the Blazor Server circuit entirely.

### Shared Razor Class Libraries and code reuse

Today there is no RCL; layout, components, and CSS are duplicated three times. Introduce `Vixly.Shared.UI` as a Razor Class Library holding presentation-only, tenant-agnostic components (layout shells, data grids/list virtualization wrappers, form inputs, status badges, the FluentUI theme) that every web and hybrid head references. It must not reference `HttpContext`, cookies, or any plane's `Program.cs` concerns directly — interactive behavior goes through injected services and parameters/callbacks so the same component renders correctly under Blazor Server, WASM, and MAUI Blazor Hybrid hosting models. Pair it with a `Vixly.Core` (or per-plane `*.Shared.Contracts`) library for the request/response DTOs and client abstractions that Operations-Hybrid and Customer-Hybrid both need, building on the decoupling pattern Public already uses (wire enums with matching numeric values, no direct Domain/Application reference).

### Team boundary: front-end and back-end as separate delivery streams

Two teams working in parallel — one on business logic, one on presentation — is the strongest practical argument for building `Vixly.Shared.UI` now rather than treating it as migration scaffolding. The library should be the literal ownership boundary: components in it take parameters and `EventCallback`s only, never inject `Application`/`Infrastructure` services or read `HttpContext` directly, so the front-end team can build, style, and test every component without a running backend, database, or Control API. The backend team's contract to that boundary is the DTO/view-model each use case already returns (a pattern already used throughout Operations — e.g. `TrackingTimelineItem`); pages bind Shared.UI components to those DTOs, and nobody hand-writes a second, UI-specific shape of the same data.

Two concrete things make that boundary real instead of a convention people forget under deadline pressure:

1. **A component gallery host.** A small Blazor project referencing only `Vixly.Shared.UI` plus sample/fake data lets the front-end team build, style, and demo every component in isolation, with no dependency on Operations, Control, or a database being up. This is the single highest-leverage thing to build first — it decouples the two teams' day-to-day dev loops immediately, independent of how far the rest of the hybrid migration has progressed.
2. **An enforced dependency rule, not a polite one.** Add an architecture test (for example NetArchTest, or a Roslyn analyzer) that fails the build if any type under `Vixly.Shared.UI` references an `Application` or `Infrastructure` namespace. Two teams cannot reliably self-police a boundary that only exists in a document; a failing build is what actually holds it.

This same seam is also what lets Operations-Hybrid, Customer-Hybrid, and the browser hosts share components at all — a component that reaches into `HttpContext` or injects a Blazor-Server-only service cannot run inside a MAUI shell, so the dependency rule above is, incidentally, also the hybrid-portability rule.

### Clean Architecture, SOLID, and native/web service swapping

The existing Domain → Application → Infrastructure → Web layering in both Control and Operations solutions is a genuinely good foundation — preserve it. The one addition a hybrid target needs is a platform-service abstraction layer: define interfaces in Application (`IBarcodeScannerService`, `ISecureTokenStore`, `ILocalDataStore`) and register platform-specific implementations at each host's composition root via DI, so the same Application/Domain code runs unmodified across Blazor Server web, and MAUI Blazor Hybrid on Windows/Android/iOS. See Code Examples below for the concrete shape.

### Zero-trust token management

Browser-hosted planes (Public, Control, Customer web) keep the existing server-side cookie model — it is already reasonably hardened (`HttpOnly`, `Secure`, `SameSite=Lax`, short `ExpireTimeSpan` with idle-deadline stamping) and a cookie is the correct mechanism for a server-rendered Blazor Server app. Native hybrid shells (Operations-Hybrid, Customer-Hybrid) cannot use a browser cookie at all and must move to OAuth 2.0 / OIDC with PKCE against Control's identity endpoints, with the resulting tokens held in the OS credential vault (Windows DPAPI, Android Keystore, iOS Keychain) behind an `ISecureTokenStore` abstraction — never in `Preferences`, plain files, or app-local SQLite. This is a genuinely new capability, not a refactor of something that exists; Control's API currently issues JWTs for service-to-service calls (fixing the JWT-audience bug already resolved), but no interactive OIDC/PKCE flow exists yet for an end-user-held native client.

### Plane-specific security boundaries

Splitting Customer Portal into its own process (matrix above) is the precondition for real multi-tenant isolation between it, the Control Plane, and the Operations Plane — today they cannot be said to have independent security boundaries because two of the four "planes" share one process. This does **not** require a new hostname or subdomain: the tenant's single hostname (`{slug}.vixly.ltd` today, a verified custom domain once that design ships) stays the one address customers and staff both use, matching the repository's own custom-domain design, which already states that one selected hostname serves both the tenant customer portal and the staff Operations application. A thin edge routing tier — a reverse proxy such as YARP, itself an ASP.NET Core app — sits in front of that hostname and path-routes `/customer/**` to the new Customer Portal process and everything else to Operations; the existing `LoginPath="/staff/login"` convention already implies this split, it just isn't enforced at the process boundary yet. Once separated: each process gets its own Data Protection key ring (Control's shared-database approach is the pattern to copy, not Operations' local-disk approach); each auth cookie is scoped to its own path (`Path=/customer` for the customer cookie) so the browser never sends one service's cookie to the other's routes; warehouse-scope authorization moves from menu-visibility-only to an enforced query predicate (closing SEC-01); and the tracking-timeline / payment-description information-disclosure gap (SEC-03) gets a customer-ownership check before any public-facing hybrid client is allowed to call it. This edge tier is also the natural home for the hostname-to-tenant resolution algorithm the custom-domain design already specifies — it does not need to be built twice, once per process.

### Data transport and storage

Offline data on Operations-Hybrid and Customer-Hybrid devices must be encrypted at rest (SQLCipher-backed EF Core SQLite, key held in the platform secure store, never derived from a static app secret). Mobile hybrid clients should pin the Control-API and Operations-API TLS certificates to reduce MITM risk on untrusted warehouse or customer networks. Every form across all planes should route through a shared input-sanitization/validation pipeline in `Vixly.Shared.UI`/`Vixly.Core` rather than each plane re-implementing its own, which is the current state.

## Target Project Structure

Builds on the existing `ControlPlane` / `OperationPlane` / `PublicPortal` / `VixlyShared` split rather than replacing it — the layered Domain/Application/Infrastructure boundary already in Control and Operations is sound and is preserved unchanged. New elements are marked **NEW**.

```text
Vixly/
├── ControlPlane/                          (unchanged: Platform Domain/Application/Infrastructure/Web + API)
├── OperationPlane/
│   ├── src/
│   │   ├── Vixly.Operations.Domain/       (unchanged)
│   │   ├── Vixly.Operations.Application/  (unchanged, + IBarcodeScannerService, ISecureTokenStore, ILocalDataStore interfaces)  NEW interfaces
│   │   ├── Vixly.Operations.Infrastructure/ (unchanged, + outbox/idempotency tables)  NEW outbox
│   │   ├── Vixly.Operations.Web/          (unchanged: Blazor Server web — loses the /customer routes, see CustomerPlane below)
│   │   └── Vixly.Operations.Hybrid/       .NET MAUI Blazor Hybrid host (Windows/Android) — warehouse scanning, dispatch, offline sync  NEW
│   └── tests/                         (unchanged, extended)
├── CustomerPlane/                         Customer Portal, split out of OperationPlane — same tenant hostname, routed at /customer  NEW top-level plane
│   ├── src/
│   │   ├── Vixly.Customer.Application/    customer use cases moved out of Vixly.Operations.Application  NEW
│   │   ├── Vixly.Customer.Infrastructure/ own DbContext/repositories, or API-client-only if data stays owned by Operations  NEW
│   │   ├── Vixly.Customer.Web/            Blazor Server web — browser customers, mounted at {tenant-host}/customer/**  NEW (replaces /customer in Operations.Web)
│   │   └── Vixly.Customer.Hybrid/         .NET MAUI Blazor Hybrid — mobile customer app  NEW
│   └── tests/                                                                        NEW
├── Vixly.Edge/                            Reverse-proxy / gateway (YARP) in front of every tenant hostname — path-routes
│                                          /customer/** to Vixly.Customer.Web, everything else to Vixly.Operations.Web;
│                                          owns the hostname-to-tenant resolution algorithm from the custom-domain design  NEW
├── PublicPortal/                          (unchanged: Vixly.Public.Web)
├── VixlyShared/                           (unchanged: Documents, Payments — plane-agnostic domain utilities, no UI)
├── Vixly.Shared.UI/                       Razor Class Library — layout shells, virtualized list/grid wrappers,
│                                          form inputs, status badges, FluentUI theme; no HttpContext/cookie access  NEW
├── Vixly.Shared.PlatformServices/         Native-vs-web service abstractions + per-host implementations:
│   ├── Abstractions/                      IBarcodeScannerService, ISecureTokenStore, ILocalDataStore, INotificationService
│   ├── Web/                               browser-camera JS interop, server-cookie token store, no-op local store
│   └── Native/                            native scanner APIs, OS secure-storage-backed token store, SQLCipher local store  NEW
└── Docs/
    └── AssistantContext/                  (unchanged — keep this document's approved decisions here once ratified)
```

Resolved: Customer Portal reads Operations' data through an internal API rather than owning its own schema. Operations builds the invoice (and the shipment, and the credit record); Customer Portal only presents a customer-scoped view of it. Since Operations is the domain owner of that data, not Customer Portal, giving Customer Portal its own copy of it would create a second source of truth to keep in sync for no benefit — the internal-API boundary already recommended above is the right shape, not just the lower-risk one.

## Theming & White-Labeling (Plan-Gated)

This is not a green-field ask. Control Plane already has a complete, owner-reviewed design for the landing-page half of it (`ControlPlane/docs/landing-page-theming/001` through `005`). It already assumes plan-gated theming — the subscription feature catalog already defines `HomepageContentCustomization` and `PortalThemeCustomization` as separate entitlements. The design deliberately rejects a free-form page builder or arbitrary CSS/HTML/JS editor in favor of code-owned presets (three to start: Vixly Standard, Professional Light, Professional Contrast) plus a small set of allowlisted semantic tokens — brand and accent colors, surface and text colors, border-radius scale, an approved font catalog — rendered as CSS custom properties, with structured content fields validated server-side, all gated by an effective-entitlement policy checked at the UI, the command layer, and the runtime render, never by plan-name string matching. Draft/publish with immutable version history, optimistic concurrency, WCAG 2.2 AA validation on publish, and non-destructive entitlement-loss fallback are all already specified. None of it has been approved for implementation — the gate in `005-approved-design-implementation-plan.md` reads "Not approved for implementation" — so nothing here has been built, but the design work does not need to be redone.

What that design explicitly excludes is why this needs extending, not just approving. It scopes itself to only the tenant-facing public landing page and states plainly that authenticated Operations must remain visually stable and must not inherit public tenant theme variables. Customer Portal theming — the half you're asking for — isn't in that design at all. Extending it is the right move, but it should follow the identical model (presets plus allowlisted tokens, entitlement-gated, versioned, no raw CSS) for the same security and accessibility reasons already decided, not a separate, looser mechanism built under deadline pressure.

Concretely, this connects directly to the target architecture already proposed above: add a `TenantCustomerPortalThemeConfiguration` aggregate parallel to `TenantLandingPageConfiguration` (same draft/publish/version/concurrency shape, same entitlement-policy pattern), but render both through one shared theme-token component living in `Vixly.Shared.UI` — a `ThemeProvider`-style component that takes a validated effective theme and emits CSS custom properties, consumed identically by the landing page, `Vixly.Customer.Web`, and eventually the MAUI hybrid shells (native shells apply the same validated tokens to their own resource dictionaries instead of CSS, but the token contract is identical). Building this before `Vixly.Shared.UI` exists means building the renderer twice.

Theming alone doesn't make a tenant "white-label" — three more things gate that, and each already has a place in this document: a custom domain, so the tenant never shows a vixly.ltd URL (an approved-but-unbuilt design, covered in the audit above); no "Powered by Vixly" branding in footers, transactional emails, or generated documents such as invoices and the PDF reports covered above — not confirmed either way in this read-only pass, so verify before assuming it's already handled; and a tenant-set app title/favicon for the browser tab and any installed hybrid-app icon. Recommend packaging these as a single White Label entitlement bundle sitting above the individual theming features, rather than requiring a plan tier to enable four separate flags to get the effect a tenant actually asked for.

Sequencing: landing-page Phase B (already designed) is largely independent of the hybrid migration and can proceed on its own approval track today. Recommend building the customer-portal extension and the shared `ThemeProvider` after Phase 1 (Customer Portal extraction) and alongside Phase 2 (Shared UI) in the roadmap below, so the renderer is built once, in the right place, instead of once in Operations.Web now and rebuilt again after the split.

## Migration Roadmap

Sequenced so each phase de-risks the next; nothing here has been implemented, and every phase needs explicit owner sign-off before work starts.

**Phase 0 — Close the audit's open findings (no architecture change).** Fix SEC-01 (warehouse-scope enforcement), SEC-03 (public tracking payment-amount disclosure), and the bundle-creation history save-order gap; add paging to the Operations bundle/dispatch/timeline reads and the Control tenant list; add automated test projects (PR-004) before any refactor touches these paths. This phase is valuable independent of the hybrid migration and should not wait for it.

**Phase 1 — Extract Customer Portal into its own process, same hostname.** Stand up `Vixly.Edge` (a YARP reverse proxy) in front of each tenant's existing hostname, path-routing `/customer/**` to a new `Vixly.Customer.Web` and everything else to `Vixly.Operations.Web` — customers and staff keep using exactly the same `{slug}.vixly.ltd` (or future custom domain) they use today; only the process handling `/customer` changes. Move `/customer` routes, `Services/Portal`, `Models/Portal`, and `Components/Pages/Customers` out of `Vixly.Operations.Web` into `Vixly.Customer.Web`; give the new process its own Data Protection key ring and, per the open structural question above, either its own data access or a narrow internal API onto Operations' data. This is the precondition for everything else — it converts today's shared-process fusion into true process isolation without touching the tenant's URL, and it is pure decomposition with no hybrid/native work yet.

**Phase 2 — Stand up the shared layers.** Create `Vixly.Shared.UI` and `Vixly.Shared.PlatformServices` (web-only implementations first). Migrate one low-risk shared component at a time (layout shell, then list/grid virtualization wrapper) out of each plane's duplicated Components tree into the RCL, verifying each plane still builds and renders identically before moving the next. Do not attempt a big-bang UI migration. Given the two-team split, the component gallery host and the architecture-test dependency rule (see Target Architecture) are worth starting immediately, in parallel with Phase 0, rather than waiting for this phase.

**Phase 3 — Build the outbox.** Add a transactional outbox and idempotent consumer for operational history (the event-architecture assessment's own recommended "first slice": `ParcelCreated`/`ParcelAssignedToShipment`). This is a prerequisite for Phase 4's offline sync, not optional scaffolding — without it, an offline queue has no reliable way to reconcile with server state after reconnecting.

**Phase 4 — Operations Hybrid pilot.** Build `Vixly.Operations.Hybrid` (.NET MAUI) for a single warehouse workflow — recommend inbound receiving/barcode scanning, since it is the highest-frequency, most offline-sensitive workflow audited. Implement `IBarcodeScannerService`/`ISecureTokenStore`/`ILocalDataStore` native implementations, OIDC/PKCE login, SQLCipher local store, and sync against the Phase 3 outbox. Pilot with one warehouse before wider rollout.

**Phase 5 — Customer Hybrid.** Build `Vixly.Customer.Hybrid` once Phase 1's extraction and Phase 2's shared UI are stable. Scope to read-heavy workflows first (tracking, invoices) with an offline read cache; defer offline writes (shipment creation, support tickets) until the outbox pattern from Phase 3 has proven itself in Operations.

**Phase 6 — Public performance pass.** Move landing/catalog pages toward prerendered or streaming-SSR Blazor rendering and add Public's own rate-limit policy. Lowest risk, lowest urgency — sequenced last because it has no dependency on the other phases and no user-facing native component.

Control/Platform is deliberately absent from the hybrid phases: nothing in the audit or the brief's own stated goals (tenant administration, billing, global monitoring) benefits from native/offline delivery, so it stays server-rendered web throughout, receiving only the Phase 0 fixes and, if Phases 1–5 add new API consumers, the OIDC/PKCE endpoint work Phase 4 needs from it.

## Code Examples

Illustrative boilerplate showing the shape of the abstraction, not production-ready code — written for `Vixly.Shared.PlatformServices` per the target structure above. Namespaces and error handling are simplified for clarity.

### 1. Native vs. web platform service abstraction (barcode scanner)

```csharp
// Vixly.Shared.PlatformServices/Abstractions/IBarcodeScannerService.cs
namespace Vixly.Shared.PlatformServices.Abstractions;

public interface IBarcodeScannerService
{
    event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;
    Task StartScanningAsync(CancellationToken cancellationToken = default);
    Task StopScanningAsync();
    bool IsSupported { get; }
}

public sealed record BarcodeScannedEventArgs(string Value, BarcodeSymbology Symbology, DateTimeOffset ScannedAtUtc);

public enum BarcodeSymbology { Code128, Qr, Ean13, Unknown }
```

```csharp
// Vixly.Shared.PlatformServices/Web/CameraBarcodeScannerService.cs
// Used by Vixly.Operations.Web (browser) — drives a JS-interop camera scanner.
namespace Vixly.Shared.PlatformServices.Web;

public sealed class CameraBarcodeScannerService(IJSRuntime jsRuntime) : IBarcodeScannerService, IAsyncDisposable
{
    private IJSObjectReference? _module;
    public bool IsSupported => true; // capability-checked at runtime via JS interop
    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    public async Task StartScanningAsync(CancellationToken cancellationToken = default)
    {
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", cancellationToken, "./js/cameraBarcodeScanner.js");
        await _module.InvokeVoidAsync("start", cancellationToken,
            DotNetObjectReference.Create(this));
    }

    [JSInvokable]
    public void OnScan(string value, string symbology) =>
        BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(value, Enum.Parse<BarcodeSymbology>(symbology), DateTimeOffset.UtcNow));

    public async Task StopScanningAsync() { if (_module is not null) await _module.InvokeVoidAsync("stop"); }
    public async ValueTask DisposeAsync() { if (_module is not null) await _module.DisposeAsync(); }
}
```

```csharp
// Vixly.Shared.PlatformServices/Native/DeviceBarcodeScannerService.cs
// Used by Vixly.Operations.Hybrid (.NET MAUI) — drives the OS/hardware scanner API.
namespace Vixly.Shared.PlatformServices.Native;

public sealed class DeviceBarcodeScannerService : IBarcodeScannerService
{
    public bool IsSupported => DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.WinUI;
    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    public Task StartScanningAsync(CancellationToken cancellationToken = default)
    {
        // Platform-specific hardware/camera scanner SDK wired here; raises BarcodeScanned on each read.
        return Task.CompletedTask;
    }

    public Task StopScanningAsync() => Task.CompletedTask;
}
```

```csharp
// Composition root — the only place that knows which implementation is active.
// Vixly.Operations.Web/Program.cs:
builder.Services.AddScoped<IBarcodeScannerService, CameraBarcodeScannerService>();

// Vixly.Operations.Hybrid/MauiProgram.cs:
builder.Services.AddSingleton<IBarcodeScannerService, DeviceBarcodeScannerService>();

// Shared Razor component — identical across both hosts:
// @inject IBarcodeScannerService Scanner
// Scanner.BarcodeScanned += (_, e) => HandleScan(e.Value);
```

### 2. Secure native token storage (`ISecureTokenStore`)

```csharp
// Vixly.Shared.PlatformServices/Abstractions/ISecureTokenStore.cs
namespace Vixly.Shared.PlatformServices.Abstractions;

public interface ISecureTokenStore
{
    Task SaveAsync(string key, string value);
    Task<string?> GetAsync(string key);
    Task RemoveAsync(string key);
}
```

```csharp
// Vixly.Shared.PlatformServices/Native/NativeSecureTokenStore.cs
// Used by Vixly.Operations.Hybrid / Vixly.Customer.Hybrid (.NET MAUI).
// Microsoft.Maui.Storage.SecureStorage delegates to Keychain (iOS), Keystore (Android),
// and DPAPI (Windows/WinUI) per platform — no plaintext ever touches app storage.
namespace Vixly.Shared.PlatformServices.Native;

public sealed class NativeSecureTokenStore : ISecureTokenStore
{
    public Task SaveAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);

    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key)!;

    public Task RemoveAsync(string key)
    {
        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }
}
```

```csharp
// Vixly.Shared.PlatformServices/Web/ServerCookieTokenStore.cs
// Used by Blazor Server hosts (Operations.Web, Customer.Web) — no client token to store;
// the existing hardened cookie (HttpOnly, Secure, SameSite=Lax, idle-deadline stamped) is the
// credential, so this implementation is intentionally a no-op that documents the boundary.
namespace Vixly.Shared.PlatformServices.Web;

public sealed class ServerCookieTokenStore : ISecureTokenStore
{
    public Task SaveAsync(string key, string value) =>
        throw new NotSupportedException("Blazor Server hosts use the authentication cookie; no bearer token is issued to the browser.");

    public Task<string?> GetAsync(string key) => Task.FromResult<string?>(null);
    public Task RemoveAsync(string key) => Task.CompletedTask;
}
```

```csharp
// Native OIDC/PKCE login — only the hybrid hosts need this; the token that comes back
// from the authorization code + PKCE exchange is written straight into ISecureTokenStore,
// never into Preferences, app-local files, or an unencrypted SQLite table.
public async Task LoginAsync()
{
    var authResult = await oidcClient.LoginAsync(new LoginRequest()); // Duende IdentityModel.OidcClient, PKCE enforced
    await secureTokenStore.SaveAsync("access_token", authResult.AccessToken);
    await secureTokenStore.SaveAsync("refresh_token", authResult.RefreshToken);
}
```

## Open Decisions, Assumptions & Risks

This review was read-only and static: no code was executed, no database was queried, and nothing in the repository was changed, consistent with this session's constraints. The following need an explicit owner decision before Phase 0 work begins:

1. **Customer Portal extraction (Phase 1) is the load-bearing decision in this whole document.** Every downstream recommendation — separate security boundaries, a dedicated hybrid client, independent scaling — assumes it happens. If the owner instead wants Customer Portal to stay fused with Operations for cost or simplicity reasons, most of the Customer Portal-specific target architecture and Phase 1/5 roadmap steps should be reconsidered rather than built as designed here.
2. **Whether Customer Portal gets its own database/schema or continues reading Operations' data through an internal API** (noted under Target Project Structure). Resolved: Customer Portal reads through an internal API rather than owning a schema — Operations builds the data (e.g. the invoice), Customer Portal only presents it, so Operations stays the single source of truth.
3. **Whether native/offline delivery is actually wanted for Operations and Customer**, or whether the brief's "Blazor Hybrid" goal would be better served by Blazor WebAssembly (offline via service worker, no MAUI packaging/app-store overhead) instead of .NET MAUI Blazor Hybrid. MAUI is the better fit specifically because of native barcode-scanner and secure-credential-store access; if those turn out not to be firm requirements, WASM is materially cheaper to build and ship.
4. **Whether the existing internal audits (production-readiness review, security assessment, performance assessment, event-architecture assessment) are still current.** They are dated 2026-08-24 through 2026-08-27; this document treats them as the best available evidence rather than re-deriving every finding from scratch, but they should be re-verified against the current `main` before Phase 0 work is scoped.
5. **Provider selection for OIDC/PKCE and the native credential vault integration** — no identity provider or MAUI secure-storage vendor decision was found in the repository; Control's API already issues JWTs for service calls, but an end-user-facing OIDC flow is new work, not a refactor.
6. **Sequencing risk:** Phase 3 (outbox) blocking Phase 4/5 (offline sync) is a deliberate choice to avoid shipping an offline queue with no reliable reconciliation story, given the confirmed bundle-creation history-save gap. Compressing this timeline by skipping or parallelizing Phase 3 would reintroduce that class of bug into a now-offline, harder-to-debug context.

No staging, production, credential, or destructive action of any kind was taken or proposed as part of producing this document.
