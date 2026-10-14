# Vixly Global Engineering Standard (V4)

| | |
|---|---|
| Status | v1.3, 2026-10-13 (adds ADR-0010: schema per plane, table naming, V4 hostnames, independence from V3, build order; and ADR-0007: Platform Worker). v1.2 approved by the owner 2026-10-06; v1.3 changes accepted by the owner 2026-10-13. Section 1 white-label paragraph amended 2026-10-14 (ADR-0012 Amendment 1). Binding for every team and every project in this repository. |
| Owner | Architecture. Changes only by ADR in `docs/decisions/`. |
| Audience | Control, Operations, Customer and Frontend/UI teams. |

## 0. Authority and how to use this document

This is the one document every team refers to. Read sections 1 to 5 before writing code, and section 11 before opening a pull request. Every rule has an ID (S-1, P-2, D-2, C-1) so reviews can cite it.

When documents disagree, precedence is: this standard, then the plane documents in `docs/<plane>/`, then `docs/architecture/` (evidence, not rules), then the legacy Vixly repository (read-only reference, never a source of rules).

V4 is built from scratch. It is not built on top of the legacy system. Where legacy code contradicts this standard, this standard wins. Compatibility is kept at the boundaries (section 9), never by bending internal structure.

## 1. The platform

| Component | What it is | Deployables | Owner | Stage |
|---|---|---|---|---|
| Platform (Control plane) | Tenants, subscriptions, plans, entitlements, platform billing, Vixly's shipping-service catalogue, tenant activity | `Platform.Web` (staff console), `Platform.Api`, `Platform.Worker` (Windows Service, ADR-0007) | Control team | 1 |
| Operations | Shipments, parcels, bundles, dispatch, invoices, credit, tracking, tenant staff accounts (ADR-0003) | `Operations.Web` (staff / warehouse), `Operations.Api` (internal) | Operations team | 3 |
| Customer | A customer's own view of their invoices, shipments and tracking | `Customer.Web` | Customer team | 4 |
| Edge | Reverse proxy: tenant resolution and path routing | `Vixly.Edge` | Control team | 5 |
| Public | Plans catalog and tenant self-onboarding | `Public.Web` | Control team | 2 |
| Shared | `Kernel`, `Localization`, `UI` + `UI.Gallery`, `PlatformServices` (reserved) | Libraries | Architecture / Frontend/UI | 1 / parallel |

**One hostname per tenant.** A tenant is served from one hostname, `{slug}.<base domain>` and a custom domain later (hostnames below). Customers never get a different domain: a customer reaches the portal at `{tenant-host}/customer`. Edge resolves the hostname to a tenant (through the Platform API) and routes `/customer/**` to `Customer.Web` and everything else to `Operations.Web`. The Platform console and the Public site are not tenant-hostname surfaces and Edge does not front them. Custom-domain resolution is designed in the legacy repo (`ControlPlane/docs/custom-domains/002-custom-domain-technical-design.md`) but not built; Edge ships with slug resolution and is structured so custom-domain resolution slots in without a rewrite.

**Hostnames.** V4 runs on `techcrafters.ltd` until tenants migrate from V3, so no V3 cookie (V3 writes domain-wide `.vixly.ltd` cookies) ever reaches V4 (ADR-0010).

| Purpose | Until migration | After the last tenant migrates |
|---|---|---|
| Public site and sign-up | `vixly.techcrafters.ltd` | `vixly.ltd` |
| Platform console (behind Cloudflare Access) | `vixly-control.techcrafters.ltd` | `control.vixly.ltd` |
| Platform API | `vixly-api.techcrafters.ltd` | `api.vixly.ltd` or internal only |
| Tenant host (Edge → Operations, Customer at `/customer`) | `{slug}.techcrafters.ltd` | `{slug}.vixly.ltd` |
| Staging | same names with `-staging` (`vixly-control-staging.techcrafters.ltd`, `{slug}-staging.techcrafters.ltd`) | unchanged |

- **H-1** Base domains are configuration, never code: `TenantHosting:BaseDomains` and `PlatformHosting`. No hostname literal in source. Links in emails and PDFs are built from this configuration.
- **H-2** Registration rejects reserved slugs: `www`, `mail`, `vixly`, `vixly-control`, `vixly-api`, `control`, `api`, `admin`, `edge`, `status`, `staging`, anything ending `-staging`, and any name TechCrafters already uses in DNS. The list is configuration with this set as its floor.

**White label (ADR-0012, amended 2026-10-14).** White label belongs to the Operations plane: tenant-facing surfaces (Operations, Customer, the tenant landing page, tenant emails and documents) show the tenant's brand, never "Vixly". Tenant text uses `{brandName}`, the brand is Operations data exposed through `Contracts.Operations`, theming is presets plus allowlisted tokens through one `ThemeProvider`, and a plan entitlement from `Contracts.Platform` decides what a tenant may change. The Platform plane (Control) is proprietary to TechCrafters: never white-labelled, never offered to anyone; it runs plans, billing and collections. The Public site is always Vixly.

**Languages.** V4 is offered in English (primary, source and fallback), French and Spanish. Every screen, menu, error, email, document and history entry is multilingual. The rules are in 5.5 and ADR-0005.

**Process isolation.** Every deployable has its own process, cookie scheme, Data Protection key ring and cache. No two planes share process memory, keys or a `DbContext`.

## 2. Repository layout

```
V4/
├── AGENTS.md  README.md  .gitignore  .editorconfig
├── docs/
│   ├── global/          this document
│   ├── architecture/    evidence and reference
│   ├── decisions/       ADRs
│   └── platform/ operations/ customer/ public/ edge/ shared/    per-plane documents
├── src/
│   ├── Platform/        Vixly.Platform.{Domain,Application,Infrastructure,Api,Web,Worker,Resources}
│   ├── Operations/      Vixly.Operations.{Domain,Application,Infrastructure,Api,Web,Resources}
│   ├── Customer/        Vixly.Customer.{Domain,Application,Infrastructure,Web,Resources}
│   ├── Public/          Vixly.Public.Web
│   ├── Edge/            Vixly.Edge
│   ├── Contracts/       Vixly.Contracts.Platform, Vixly.Contracts.Operations
│   ├── Shared/          Vixly.Shared.{Kernel,Localization,UI,UI.Gallery,PlatformServices*}
│   └── Hybrid/          reserved (V2+; the legacy Hybrid roadmap phases are retired, ADR-0010)
├── tests/               one test project per source project, plus tests/Architecture
└── build/               ci/ and scripts/
```

Rules:

- Project name = folder name = assembly name = root namespace. Names follow `Vixly.<Plane>.<Layer>`.
- Layers are `Domain`, `Application`, `Infrastructure`, `Api`, `Web`, plus `Resources` (the plane's `.resx` text in en, fr and es; no code beyond a marker type). Public and Edge are single projects; each holds its own resources.
- Every source project has a test project of the same name plus `.Tests` under `tests/<Plane>/`.
- Reserved folders (`Hybrid`, `Shared.PlatformServices`) hold only a README until their stage opens.
- Solution files are created at the stage that opens a plane, never ahead of it.

## 3. Dependency rules

Enforced by `tests/Architecture/Vixly.Architecture.Tests`. A violation fails the build.

| Project | May reference |
|---|---|
| `*.Domain` | `Vixly.Shared.Kernel` only |
| `*.Application` | its own `Domain`, `Shared.Kernel`, and other planes' `Contracts` (behind ports) |
| `*.Infrastructure` | its own `Application` and `Domain`, `Contracts`, its own `Resources` and `Shared.Localization` (to render emails and documents) |
| `*.Api`, `*.Web`, `*.Worker` | its own `Application`; `Infrastructure` for the composition root only (`Program.cs` / DI registration, never in pages or controllers); its own `Contracts`; its own `Resources` and `Shared.Localization`; `Shared.UI` (Web only) |
| `Vixly.Public.Web` | `Contracts.Platform`, `Shared.UI` |
| `Vixly.Edge` | `Contracts.Platform`, `Shared.Kernel` |
| `*.Resources` | `Shared.Kernel` only |
| `Shared.Localization` | `Shared.Kernel` and ASP.NET Core localization only |
| `Contracts.*` | BCL only |
| `Shared.Kernel` | BCL only |
| `Shared.UI` | ASP.NET Core components and FluentUI only |

`*.Domain` and `*.Application` never reference any `*.Resources` project or `Shared.Localization`: user-facing text is produced only in the presentation, API and rendering layers (5.5).

Cross-plane rules:

1. **C-0** A plane never references another plane's Domain, Application, Infrastructure, Api or Web project. Planes talk over HTTP through `Vixly.Contracts.*`.
2. Customer never touches the Operations database. Operations owns the data (it builds the invoice); Customer presents a customer-scoped view of it, read through the Operations internal API.
3. `Operations.Api` is internal. Edge never routes to it publicly.
4. Contracts hold no logic. Wire enums carry explicit numeric values. Changes are additive; a breaking change is a new version plus an ADR.
5. `Shared.UI` references no plane project and no `Contracts.*` and never reads `HttpContext`. Its **primitives** (buttons, inputs, grids, dialogs, badges) take parameters and `EventCallback`s only, which lets the Frontend/UI team build and test them without a backend. `Shared.UI` also holds the plane-agnostic `PresenterBase` and `PresenterComponent<TPresenter>` (see 5.3). Feature components are not primitives: they live in each plane's `*.Web` project (ADR-0001).
6. `Shared.Kernel` holds no entities and no plane vocabulary. It grows only by ADR.

## 4. Naming

### 4.1 Every entity carries its platform

An entity class starts with the plane that owns it. No bare business nouns in C#. In SQL the plane's schema names the owner, so tables do not repeat the prefix (ADR-0010, option B).

| Plane | Schema | Class | DbSet | SQL table | Repository |
|---|---|---|---|---|---|
| Platform | `platform` | `PlatformInvoice` | `Invoices` | `platform.Invoices` | `IPlatformInvoiceRepository` |
| Operations | `ops` | `OperationsInvoice` | `Invoices` | `ops.Invoices` | `IOperationsInvoiceRepository` |
| Customer | `customer` | `CustomerInvoice` (read model) | (none: reads come from the Operations API) | (none) | (read models: no repository) |

Why: the same business concept legitimately exists in several planes. Vixly billing a tenant for a subscription (`PlatformInvoice`), Operations building the shipment invoice (`OperationsInvoice`) and the customer-scoped view of that invoice (`CustomerInvoice`) are different things with different owners and lifecycles. A bare `Invoice` is ambiguous in code, in review, in contracts and in UI. In SQL the schema already removes that ambiguity, so `ops.OperationsInvoices` would only stutter.

Applies to entities, aggregate roots, EF entity classes and repositories. Also:

- `DbSet` names and table names are the plural noun without the prefix. Each entity's EF configuration sets the table name explicitly (`builder.ToTable("Invoices")`) so a class rename never renames a table silently.
- Translation tables follow the same rule: `platform.SubscriptionPlanTranslations` for `PlatformSubscriptionPlanTranslation`.
- Wire types in `Contracts.*` carry the owning plane plus `Dto`: `OperationsInvoiceDto`. The consumer maps it into its own entity or read model.
- Value objects, enums and use cases live in a plane-named namespace and need no prefix, but must not reuse an entity's name.
- Avoid stutter: name by role (`OperationsCustomer`, `OperationsCustomerInvoice`; not `CustomerCustomer`). Inside Operations, `Customer…` in a name describes the subject (the tenant's customer), never the Customer plane.
- A bare `Invoice`, `Shipment`, `Tenant`, `Customer` as an entity class in any plane is a review blocker. A table name carrying a plane prefix is a review blocker.

### 4.2 Other names

- Use cases are `<Verb><Noun>` (`CreateShipment`) in `UseCases/<Area>/<VerbNoun>/`, holding exactly three files (section 5.2).
- Authorization types are `<Plane>AuthorizationContext` and `I<Plane>AuthorizationGate`.
- Error codes are lowercase, dot-separated, plane first: `platform.identity.email_invalid`. Kernel codes start `shared.`. The resource key of an error text equals its code.
- Translation tables are `<Entities>Translations`, named after the entity they translate, for example `platform.SubscriptionPlanTranslations` (class `PlatformSubscriptionPlanTranslation`).

## 5. File and code organization

The legacy codebase already follows a banner-comment convention in its use cases. V4 keeps it and hardens it. Where V4 is stricter than legacy, it says so.

### 5.1 File header (every `.cs` and `.razor` file)

The header sits directly above the type declaration (after `using`s and the file-scoped namespace). In Razor it follows the directives. The file banner is 57 `=` characters wide.

```csharp
namespace Vixly.Operations.Application.UseCases.Shipments.CreateShipment;

// =========================================================
// FILE: CreateShipmentUseCase.cs
//
// PURPOSE:
// Creates a shipment and immediately creates its Draft Invoice.
//
// RESPONSIBILITIES:
// - Validate trusted tenant context
// - Validate shipment creation input
// - Create and persist the shipment aggregate
//
// IMPORTANT NOTES:
// - RBAC is enforced before this use case is called
// - TenantId must come from trusted caller context
// =========================================================
public sealed class CreateShipmentUseCase
```

```razor
@page "/reporting"
@inject OperationsPageTitleState PageTitleState
@* =========================================================
   FILE: ReportsWorkspace.razor

   PURPOSE:
   What the page is for.

   RESPONSIBILITIES:
   - ...

   IMPORTANT NOTES:
   - No business logic belongs here
   ========================================================= *@
```

`IMPORTANT NOTES` is where trust-boundary invariants, ordering constraints and "do not" rules live. It is not optional filler.

### 5.2 Three files per use case

`<VerbNoun>Request.cs`, `<VerbNoun>Response.cs`, `<VerbNoun>UseCase.cs`, together in one folder. All three are `public sealed`.

**Request.** Properties are `{ get; init; }` (stricter than legacy Operations' `set`), grouped by section banners. A Request never carries trusted identity (tenant, user, customer, roles): that comes only from the authorization context (S-2).

**Response.** Properties are `{ get; private init; }`. It is created only through static factories, `Success(...)` and `Failure(Error error)` (an error code with arguments, never a sentence, 5.5), so a use case never returns a half-built response.

**UseCase.** In order:

1. Header (5.1).
2. `private readonly` fields, one per dependency.
3. A constructor that null-checks every dependency: `_x = x ?? throw new ArgumentNullException(nameof(x));`. This is a V4 hardening rule. Legacy checked only some dependencies in both planes.
4. One public method: `ExecuteAsync(<Plane>AuthorizationContext authorizationContext, <VerbNoun>Request request, CancellationToken cancellationToken = default)`. The typed authorization context is always first.
5. Inside `ExecuteAsync`, sections introduced by a three-line banner (53 `=` characters, indented to the code), in this order: `TRUSTED CONTEXT VALIDATION`, `REQUEST VALIDATION`, `DEPENDENCY RESOLUTION`, then the business steps named for what they do (`SHIPMENT CREATION`), then `PERSISTENCE`. History and tracking records are written in the same unit of work as the change (D-2).
6. Private helpers at the bottom, each preceded by a banner `// METHOD: GENERATE SHIPMENT CODE` (spaced, upper case, member indentation, 53 `=`).

```csharp
    public async Task<CreateShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext authorizationContext,
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        // =====================================================
        // TRUSTED CONTEXT VALIDATION
        // =====================================================

        ...
    }

    // =====================================================
    // METHOD: GENERATE SHIPMENT CODE
    // =====================================================
    private async Task<string> GenerateShipmentCodeAsync(...)
```

### 5.3 Razor and UI code (ADR-0001)

The UI has three tiers. Detail, rationale and a worked example are in [ADR-0001](../decisions/ADR-0001-component-architecture.md); the rules below are binding.

1. **Primitives** (`Vixly.Shared.UI`) are purely presentational: parameters in, `EventCallback`s out, no use cases, no plane references. Owned by Frontend/UI.
2. **Feature components** live in the plane's `*.Web` project under `Features/<Area>/<Feature>/` and own one responsibility end to end: loading, validation, busy and error states, submit, and the use-case calls for that responsibility.
3. **Pages** hold the route and compose feature components, nothing else.

A feature component is a folder:

```
Features/<Area>/<Feature>/
  <Feature>.razor             markup                      (Frontend/UI owns)
  <Feature>.razor.css         scoped styles               (Frontend/UI owns)
  <Feature>Presenter.cs       state, commands, use cases  (plane team owns)
  <Feature>State.cs           immutable state snapshot    (plane team owns)
  Sections/                   sub-components for form sections
```

- **UI-1** One responsibility, named as a noun phrase of the UI (`ShipmentEditor`, `ParcelList`). If its description needs "and", split it.
- **UI-2** Inputs are identities and filters (`ShipmentId`, `CustomerId`). Outputs are events that report facts (`OnSaved`, `OnCancelled`), never commands to the parent. A component never receives data it could load itself.
- **UI-3** Siblings never reach into each other. Shared state is, in order of preference: the URL (selection, tab, filters); then a scoped `WorkspaceContext` carrying typed change notifications that interested components subscribe to and react to by refreshing themselves.
- **UI-4** A presenter is a plain C# class with no Blazor types. It takes its use cases and the plane's authorization-context accessor by constructor injection (every dependency null-checked, 5.2), exposes an immutable `State` and a `StateChanged` event, and derives from `PresenterBase`, which provides `RunAsync` (busy flag, cancellation tied to the component's lifetime, generic error mapping per S-9). The component derives from `PresenterComponent<TPresenter>`, an `OwningComponentBase<TPresenter>`, so the presenter's DI scope lives and dies with the component.
- **UI-5** A component that calls a use case has a presenter. A trivial component (no use case, no workflow) may keep a few lines of logic in a code-behind `.razor.cs`.
- **UI-6** Pages are 80 lines or fewer, inject no use case, gate or authorization accessor, and contain no `@code` beyond reading route parameters.
- **UI-7** Size budgets, **enforced in CI** (build fails): markup file 250 lines, presenter 400 lines, presenter 7 injected dependencies, component 6 `[Parameter]`s. Over budget means split. An exception needs an ADR.
- **UI-8** Feature components use Application use cases and Shared.UI primitives. Never Infrastructure, never another plane. The architecture test also fails any type under `*.Pages` that depends on a `*.UseCases` namespace.
- **UI-9** Presenters are unit-tested with plain xUnit, no Blazor host. A bUnit smoke test per feature component is recommended.
- **UI-10** Markup contains no literal user-facing text. Text, labels, placeholders, tooltips and accessibility text come from the localizer (5.5). A deliberate exception is marked `@* i18n-ignore *@`.
- **UI-11** Layouts tolerate text 40 percent longer than English: no fixed-width labels or buttons, and truncation always has a tooltip. Check with the `en-XA` pseudo-locale.

Styling uses Shared.UI components and CSS custom properties, never hard-coded colors, so theming can be added later without rework. Business rules still live in Domain and Application; a presenter orchestrates one screen's workflow and holds UI state, it does not decide business outcomes.

### 5.4 Formatting

UTF-8 with BOM, CRLF line endings, four-space indentation, one type per file, file name = type name. Enforced by `.editorconfig`.

### 5.5 Localization (ADR-0005)

English is the source and the fallback; French and Spanish are first-class. Rules:

- **L-1** No user-facing text below the presentation layer. Domain and Application return and throw an `Error` (stable code plus named, typed arguments); they never produce a sentence. `DomainException` and `Response.Failure` take an `Error`. Tests assert codes.
- **L-2** Text lives in `.resx` resources in en, fr and es, owned by the project that shows it: `Shared.UI` for primitives, `Vixly.<Plane>.Resources` for plane text (errors, enum labels, emails, documents). `Domain` and `Application` reference neither Resources nor `Shared.Localization` (section 3).
- **L-3** Placeholders are named (`{count}`), never positional and never built by concatenation. Plurals use CLDR categories per language through the shared plural helper (French treats 0 and 1 as singular).
- **L-4** Language is chosen in this order: user preference, tenant default, `Accept-Language`, `en`. The Public site carries the language in the URL (`/en`, `/fr`, `/es`). Emails and PDFs use the recipient's language.
- **L-5** System-owned content is translated through `<Entities>Translations` tables with a mandatory `en` row. Tenant-entered landing-page and portal text may be entered per language and falls back to the tenant's default language, then `en`. Operational data is never translated.
- **L-6** Enum labels, statuses and permission names are resolved from resources by member name, never stored as text. History and audit rows store an event code plus arguments (D-4).
- **L-7** Documents (invoices, receipts, terms acceptances) record the language they were issued in and are regenerated in that language.
- **L-8** Wire and database formats are invariant (ISO 8601, `.` decimals, UTC). Display and input follow the user's culture. Money carries its ISO currency code; currency is never inferred from language.
- **L-9** All text columns are `nvarchar`. Searchable text uses an accent-insensitive collation. User-visible text is never compared with `ToLower()`.
- **L-10** The build fails when: a key is missing in any language; a value is empty; placeholder names differ between languages; a declared `Error` has no text; Domain or Application references a resource or localization assembly; or markup contains literal text. Machine-drafted fr and es text is allowed with the resource comment `needs-review`; it passes the build and fails the release gate until a native reviewer clears it.

## 6. Security baseline

Every item is mandatory. Findings from the legacy audit are cited so the reason survives.

- **S-1** Authorization is centralized and happens in two places: RBAC through the plane's `I<Plane>AuthorizationGate` before the use case runs, ownership and scope checks inside the use case. Menu or UI visibility is never authorization (legacy SEC-01: warehouse scope was only enforced at the menu layer).
- **S-2** Identity comes from trusted context only. Tenant id, user id, roles, scopes and customer id come from the authorization context built from the authenticated principal, never from a request body, route, query string or form.
- **S-3** Visibility is not ownership. A "public only" filter hides internal data; it does not prove the caller may see this record. Customer and public reads verify ownership of the specific record (legacy SEC-03: a public tracking path could expose another customer's payment amount).
- **S-4** Cookies are host-only (no `Domain`) and carry V4 names: the `__Host-` prefix (`__Host-vx4-auth`, `__Host-vx4-device`), except the customer cookie, which is `__Secure-vx4-customer` because it is scoped to `Path=/customer`. Every cookie is `HttpOnly`, `Secure` always, `SameSite=Lax`, with a short `ExpireTimeSpan`, idle-deadline stamping and a bounded `MaxAge`. One cookie scheme per process. No policy-scheme router that guesses the scheme from path, referer or cookie. V4 never writes a domain-wide cookie and ignores any `vx-*` cookie it receives.
- **S-5** Data Protection keys are persisted to the plane's own schema under a per-app name, never to local disk or memory. Each deployable has its own key ring and an application name that differs from V3's.
- **S-6** Authentication, registration, device-registration and public endpoints carry named rate-limit policies. Every anonymous endpoint is listed in its plane document with its policy.
- **S-7** Internal APIs authenticate every caller (internal API key header handler at minimum, with an allowed-caller list), are never routed publicly, and receive tenant context explicitly.
- **S-8** CORS uses explicit per-environment origins, never a wildcard. `AllowedHosts` is restricted outside Development.
- **S-9** Never echo raw HTTP responses, exception text or stack traces to a UI or API consumer (legacy SEC-02). Show a generic, localized error with a correlation id (5.5). APIs return an error code, its arguments and the correlation id, not display text.
- **S-10** Every web host sends CSP, HSTS, `X-Content-Type-Options` and frame protection. Relaxations that Blazor Server needs (legacy allows `'unsafe-inline'` and `'unsafe-eval'`) are recorded as tracked exceptions, not silently copied.
- **S-11** No secrets in the repository. Secrets come from host environment variables today; Google Secret Manager is the planned second source. Code reads them only through `IConfiguration`, so switching source is configuration, not code.
- **S-12** Every host exposes `/health/live` and `/health/ready`, writes structured logs with no PII, tokens or secrets, and supports OpenTelemetry export by configuration.
- **S-13** Every use case that reads or writes tenant data has a test proving tenant A cannot see tenant B. Operations adds a warehouse-scope role matrix.

## 7. Performance baseline

- **P-1** No unbounded reads. Every list use case takes a page and page size (with a hard cap) and returns projections, using no-tracking queries (legacy P1: bundle, dispatch and timeline reads were unbounded).
- **P-2** No sequential fan-out. Independent queries for one screen run together or are consolidated, and a dashboard states its query count (legacy: eight sequential queries).
- **P-3** Every cross-process call takes a `CancellationToken` and has a timeout. Retries only for idempotent calls, with backoff. No per-item loops over HTTP: contracts expose list and batch endpoints.
- **P-4** Long lists use paging or `<Virtualize>`. High-frequency components (scan input) override `ShouldRender()`. Keystroke bursts are debounced.
- **P-5** Hosts enable Brotli and Gzip compression, long-lived caching for fingerprinted media and fonts, revalidation for CSS and JS, and output caching for read-mostly public endpoints.
- **P-6** A query pattern introduced in a pull request ships with its supporting index in the same migration.

## 8. Data integrity

- **D-1** Each plane owns a SQL schema (`platform`, `ops`, `customer`), its own migrations history table in that schema, its own connection string (`ConnectionStrings:<Plane>`) and its own SQL login granted rights on that schema only. Planes may share a database or use separate databases; nothing may depend on which. No foreign keys, joins, views or synonyms cross schemas. Cross-plane data moves only through `Contracts.*` (C-0). Migrations live in the plane's Infrastructure project, ship as an EF bundle and are applied by the pipeline with a separate migration login; no host migrates its database at startup. `/health/ready` fails if the schema is missing or its migration level does not match the build.
- **D-5** Keys assigned in code are declared. Every entity whose `Id` is set in its constructor is configured `ValueGeneratedNever()` by one convention in each `DbContext`, checked by an architecture test, so a child added to a tracked aggregate is inserted, never updated (legacy: collection actions were sent as UPDATEs and the case never closed).
- **D-6** No silent catch. A `catch` that continues logs the exception with the entity or tenant id and the correlation id, and counts as a failure in the job run (ADR-0007). `catch { count++; }` fails review.
- **D-2** A state change and its history or tracking record commit in one unit of work, one `SaveChanges` (legacy bug: bundle creation saved the bundle before recording its history event, with no later save, so the event could be lost).
- **D-3** Use cases do not publish cross-process notifications directly. A transactional outbox is a Version 2 prerequisite for any offline sync. Until it exists, cross-plane effects are pull-based: Customer reads Operations through the API.
- **D-4** History and audit records store an event code plus typed arguments, never a rendered sentence, so the same record reads correctly in every language (ADR-0005).

## 9. Compatibility

V4 is a new system that runs beside V3. It does not replace V3 in place and does not serve V3's planes (ADR-0010).

- **M-1** Independent. No V4 project references V3, calls a V3 endpoint or reads a V3 table. V3 never learns V4 exists. Cookie names and domains, Data Protection application names, email sender identity, storage prefixes, payment webhook endpoints and secrets, SQL logins and job names all differ between the two.
- **M-2** Separate data. V4 uses its own databases (`VixlyV4`, or `VixlyV4Platform`, `VixlyV4Operations`, `VixlyV4Customer`). Only the one-off migration tool under `build/` reads V3's database, and it reads it only.
- **M-3** A tenant lives in exactly one system. Tenants move one at a time after V4 is tested end to end: freeze in V3, migrate with a reconciliation report, switch DNS, activate in V4 and mark the tenant `MigratedToV4` in V3 so it is never billed twice. V3 keeps the frozen copy for a 14-day rollback window.
- **M-4** Password hashes move unchanged. V4 verifies BCrypt hashes, so migrated staff and customers keep their passwords.
- The legacy repository is a read-only reference for behaviour, never for code: V4 rebuilds what V3 does for users, and V3 code that has no active purpose is not carried over.

Internal structure and naming never bend to match legacy.

## 10. Teams and contracts

| Team | Owns |
|---|---|
| Control | `Platform.*`, `Contracts.Platform`, `Vixly.Edge`, `Public.Web` |
| Operations | `Operations.*`, `Contracts.Operations` (jointly with Customer) |
| Customer | `Customer.*` |
| Frontend/UI | `Shared.UI`, `Shared.UI.Gallery`; the `.razor` and `.razor.css` files of feature components (via CODEOWNERS) |
| Architecture | `Shared.Kernel`, `tests/Architecture`, `build/`, this document |

Plane teams own the presenters and state of their feature components (`*Presenter.cs`, `*State.cs`). `CODEOWNERS` enforces this split inside a single component folder; the first pull request that introduces a feature component is reviewed by both teams.

- **C-1** A cross-team contract (anything under `Contracts.*`, or a public Shared.UI component API) changes by pull request reviewed by its owner and at least one consumer.
- **C-2** Producer and consumer agree the shape before either side finishes. No fait accompli.
- **C-3** Plane teams deliver presenters, state and view models; Frontend/UI delivers the markup and styles that bind to them. Frontend/UI reviews every change to a feature component's `.razor`; the plane team reviews every change to its presenter and state. Nobody hand-writes a second, UI-specific shape of the same data.
- **C-4** `Shared.UI` and the gallery are not gated by plane stages and can progress in parallel.

## 11. Definition of done (every pull request)

- [ ] Follows sections 3, 4 and 5 (the architecture tests pass).
- [ ] Every new file has its header; every use case has Request, Response, UseCase.
- [ ] UI changes follow 5.3: pages are thin, feature components own their behavior in a presenter, and the size budgets (UI-7) pass.
- [ ] Authorization (S-1 to S-3) and tenant isolation (S-13) are tested.
- [ ] Every list read is paged (P-1); no sequential fan-out (P-2).
- [ ] State change and history commit together (D-2).
- [ ] Any new anonymous endpoint has a rate-limit policy and is listed in the plane document.
- [ ] No secrets, no raw error echo, no hard-coded colors.
- [ ] Localization (5.5): every new user-facing string exists in en, fr and es; failures are `Error` codes; no literal text in markup.
- [ ] The plane document in `docs/<plane>/` is updated.

## 12. Build order and verification gates

Planes are built one at a time, in order. Within a plane the order is Domain, Application, Infrastructure, Api, Web.

| Stage | Scope |
|---|---|
| 1 | Platform, `Contracts.Platform`, `Shared.Kernel`, `Architecture.Tests` |
| 2 | Public (goes live with Platform, before Operations) |
| 3 | Operations, `Contracts.Operations` |
| 4 | Customer |
| 5 | Edge |
| Parallel | `Shared.UI` and `Shared.UI.Gallery` |

A plane is verified, and the next one opens, only when:

1. It builds without warnings and the architecture tests pass.
2. Unit and integration tests are green, including authorization and tenant-isolation tests.
3. The definition of done is met on every change in the plane, including complete en, fr and es resources.
4. Every anonymous endpoint and its rate-limit policy is listed.
5. `docs/<plane>/` holds the build brief, the design notes and a verification record.
6. The owner signs the verification record.

## 13. Open decisions

| ID | Decision | Status / default applied until resolved |
|---|---|---|
| O-1 | Entity prefix for Operations. The two examples given were `PlatformInvoice` and `CustomerInvoice`; `Operations<Entity>` extends the rule to the third plane for consistency. | **Resolved** by [ADR-0002](../decisions/ADR-0002-resolve-open-decisions-o1-o2-o5.md): applied as written in 4.1 |
| O-2 | Existing data: does V4 reuse existing schemas and data, or start with a fresh schema plus a data migration? Decides the Platform Infrastructure design. | **Resolved** by ADR-0002: fresh schema plus a separate one-off migration tool |
| O-3 | Owner of the Public site. | **Resolved** by ADR-0010: Control team |
| O-4 | Where Edge runs (hosting topology). | Unresolved. Must be answered before Stage 5 |
| O-5 | V4 hardening beyond legacy: typed authorization context for every plane, null-check on every constructor dependency, `init` on every Request. | **Resolved** by ADR-0002: applied as written in 5.2 |
| O-9 | Plane databases: shared or separate | **Resolved** by ADR-0010: either, by configuration (D-1) |
| O-10 | Table naming with schemas | **Resolved** by ADR-0010: classes prefixed, tables unprefixed in the plane schema (4.1) |
| O-11 | V4 hostnames until migration | **Resolved** by ADR-0010: `techcrafters.ltd` (section 1) |
