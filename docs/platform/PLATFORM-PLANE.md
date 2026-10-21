# Platform plane (Control) - Stage 1 build document

| | |
|---|---|
| Status | **Scope approved by the owner, 2026-10-06** (ADR-0003). Slices are verified one at a time. |
| Owner | Control team |
| Governed by | [Global Standard v1.3](../global/VIXLY-GLOBAL-STANDARD.md), ADR-0001, ADR-0002, ADR-0003, ADR-0005, ADR-0007, ADR-0010 |
| Replaces | The Version 1 Control brief (extraction framing) |

## 1. What the Platform plane is

The plane Vixly itself runs: it knows who the tenants are, what they pay for, whether they may log in, and which devices are licensed. It never holds shipment or customer-operational data. Other planes learn about tenants only through `Vixly.Contracts.Platform`.

It owns: `Platform.Domain|Application|Infrastructure|Api|Web|Worker`, `Contracts.Platform`, `Public.Web` (Stage 2, goes live with Platform) and `Vixly.Edge` (Stage 5).

Its data lives in the `platform` schema, reached through `ConnectionStrings:Platform` with the `vixly_platform_app` login (D-1). That schema may sit in a database shared with the other planes (`VixlyV4`) or in its own (`VixlyV4Platform`); the code does not know which.

## 2. Evidence base (legacy, read-only)

The legacy Control plane has 134 use cases and about 90 domain files. Counts by area: Platform administration 42, Tenants 22, Subscriptions 15, Licensing 14, Billing 13, Collections 6, Settings 6, Payment collection 5, Merchant services 4, Auth 4, Portal access 3. V4 does not copy this code. It uses it as a list of behaviors the business already depends on.

## 3. Bounded contexts and build slices

Stage 1 is built in vertical slices. Each slice goes through all five layers (Domain, Application, Infrastructure, Api, Web) and passes the gate before the next starts. Within a slice, layers are built in that order.

| Slice | Context | Contains | Depends on |
|---|---|---|---|
| 0 | Foundation | `Shared.Kernel`, `Contracts.Platform` skeleton, architecture tests, root build config (`Directory.Build.props`, `Directory.Packages.props`, solutions), `PlatformAuthorizationContext`, auth gate. Addendum before sign-off (v1.3): `platform` schema and history-table convention, `ConnectionStrings:Platform`, `ValueGeneratedNever` convention (D-5), use-case-gate architecture test, one `UseVixlySecurityHeaders()` | none |
| 1 | Identity and access | Platform users, accounts, roles, permissions, departments, login, password reset, invitations | 0 |
| 2 | Tenants | Tenant directory, company profile, portal slug, registration, lifecycle and suspension, history | 1 |
| 3 | Subscriptions | Plans, features, limits, tenant subscription, limit overrides, pricing preview | 2 |
| 4 | Billing | Invoices, lines, payments, recurring generation, PDFs | 3 |
| 5 | Payment collection | Merchant account, provider connections, payment methods, automatic collection, transactions | 4 |
| 6 | Collections | Collection cases and actions, monetary suspension, extensions, dashboard | 4, 5 |
| 7 | Licensing | Devices, licenses, registration tokens, release requests, portal access validation | 2, 3 |
| 8 | Legal and settings | Terms documents, acceptance, reminders, billing and collections settings, company profile | 2 |
| 9 | Contracts and verification | Final `Contracts.Platform` surface for Edge, Operations, Customer, Public; plane verification record | all |

Slices 5 to 8 can run in parallel after slice 4 if two developers are available.

## 4. Entity names (rule 4.1: every entity carries its platform)

Proposed V4 names for the legacy entities. Classes carry the prefix; tables are the plural noun in the `platform` schema without it: `PlatformInvoice` → `platform.Invoices` (Global Standard 4.1, ADR-0010).

| Legacy | V4 | Note |
|---|---|---|
| Tenant, TenantCompanyProfile | `PlatformTenant`, `PlatformTenantCompanyProfile` | |
| TenantLifecycleHistory | `PlatformTenantLifecycleEvent` | History is an event log; D-2 requires it to commit with the state change |
| TenantLandingPageConfiguration | **Moved to Operations** | Tenant brand and landing page are Operations data (ADR-0012 Amendment 1); Control only sells the feature |
| Registration (+ enums) | `PlatformTenantRegistration` | |
| SubscriptionPlan, Feature, Limit | `PlatformSubscriptionPlan`, `PlatformSubscriptionPlanFeature`, `PlatformSubscriptionPlanLimit` | |
| TenantSubscription, TenantLimitOverride | `PlatformTenantSubscription`, `PlatformTenantLimitOverride` | |
| Invoice, InvoiceLine, Payment | `PlatformInvoice`, `PlatformInvoiceLine`, `PlatformPayment` | |
| InvoiceCollectionAttempt, TenantBillingPaymentMethod | `PlatformInvoiceCollectionAttempt`, `PlatformTenantPaymentMethod` | |
| PlatformMerchantAccount, PlatformPaymentSettings | unchanged | Already prefixed |
| PaymentProviderConnection (+ Setting), PaymentTransaction | `PlatformPaymentProviderConnection`, `...ConnectionSetting`, `PlatformPaymentTransaction` | |
| TenantCollectionCase, CollectionAction | `PlatformCollectionCase`, `PlatformCollectionAction` | |
| Device, DeviceLicense, DeviceRegistrationToken | `PlatformDevice`, `PlatformDeviceLicense`, `PlatformDeviceRegistrationToken` | |
| PlatformUser, Account, Role, Permission, Department, tokens, logs | unchanged | Already prefixed |
| TenantTermsAcceptance, TermsAcceptanceReminder, Legal documents | `PlatformTenantTermsAcceptance`, `PlatformTermsAcceptanceReminder`, `PlatformLegalDocument` (+ terms variants) | |
| BillingSettings, Platform settings | `PlatformBillingSettings`, `PlatformSettings` | |
| TenantUser, TenantUserAccount, activation and setup tokens | **Operations plane** (ADR-0003) | Not a Platform entity; `OperationsUser`, `OperationsUserAccount` |
| ShippingService (in V3's Operations data) | `PlatformShippingService` | Moves to Platform: Vixly's catalogue of offered services (sea, air), identical for every tenant, exposed through `Contracts.Platform`; Operations keeps each tenant's setup by stable code (Slice 3) |
| (new) | `PlatformAuditEntry` | Append-only audit of every command; the app login has no UPDATE or DELETE on it (Slice 1) |
| (new) | `PlatformJobRun` | One row per Worker job run with outcome and failure count (ADR-0007, Slice 4) |
| (new) | `PlatformTenantActivityDaily`, `PlatformTenantActivityLatest` | Counts and timestamps pulled hourly from Operations; never operational content (Slice 2, Stage 3 endpoint) |

Value objects (`EmailAddress`, `PhoneNumber`) and `DomainException` are not entities. They go in `Shared.Kernel` if more than one plane needs them (each addition needs an ADR per rule 6), otherwise they stay in `Platform.Domain`.

## 5. Behavior inventory (use-case catalogue)

The legacy names are the starting list, grouped by slice. Names carry no entity prefix (they are verbs: `CreateInvoice`), but each lives in its context folder and operates on prefixed entities.

- **Identity (1):** create/update platform user, account activate/deactivate/lock/suspend, password reset/change, invitations, roles, permissions, departments (create, update, activate, assign, set primary), login, authority synchronization repair.
- **Tenants (2):** (tenant staff login is Operations scope, ADR-0003) register tenant, check slug availability, get/list/search tenants, manage lifecycle, company profile and logo, registration flow (start, verify email, resume, select plan, pay by card, complete after verified payment), first-device setup.
- **Subscriptions (3):** plan CRUD and activation, assign tenant subscription, request/cancel plan change, preview pricing and upgrade, limit overrides.
- **Billing (4):** create/issue invoice, add line, register payment, recurring generation, capacity-expansion invoice, invoice and receipt PDFs, list/get.
- **Payment collection (5):** merchant configuration, provider connection create/validate/save, collect card payment, collect invoice payment, automatic collection.
- **Collections (6):** process collections, case list/get, grant extension, clear monetary suspension, dashboard.
- **Licensing (7):** generate licenses, register/approve/deny devices, release request flow, list devices and licenses, portal access validation and runtime check.
- **Legal and settings (8):** terms upload, acceptance, reminders, billing and collections settings.

Review points: the 134 legacy use cases contain duplicates and near-duplicates (for example `GetBillingSettings` and `GetPlatformBillingSettings`, three portal-access validators, `UpdatePlatformUserRoles` and `UpdatePlatformUserRoleAssignments`). V4 merges these during each slice and records the merge in the slice's design note.

## 6. Security and performance specifics

- Every use case takes `PlatformAuthorizationContext` first. Anonymous endpoints (login, password reset, registration, slug check, public payment configuration, portal access validation) are listed in the plane's anonymous-endpoint register with their rate-limit policy before the Api layer is accepted.
- Tenant isolation (S-13) is tested for every tenant-scoped read.
- Every list is paged (P-1). `GetTenants`, invoices, collection cases and licenses are the known unbounded lists in legacy.
- State change plus history commit in one transaction (D-2): tenant lifecycle, subscription changes, invoice status, license release.

## 7. Multilingual requirements (ADR-0005)

The Platform plane is English, French and Spanish end to end. For this plane that means:

| Slice | Localization work |
|---|---|
| 0 | `Error`, `LanguageCode`, `DomainException(Error)`; `PlatformSecurityErrors`; rules that keep text out of Domain and Application |
| 1 | `PreferredLanguage` on `PlatformUser`; login, reset and invitation emails in the recipient's language; every Identity error has a code; `Vixly.Platform.Resources` project created with the first error texts |
| 2 | `DefaultLanguage` on `PlatformTenant`; tenant lifecycle history stored as event code plus arguments (D-4). Landing-page text per language moved to Operations (ADR-0012 Amendment 1) |
| 3 | `platform.SubscriptionPlanTranslations` (name, description, feature and limit labels); pricing shown with the plan's ISO currency code |
| 4 and 5 | `Language` stored on `PlatformInvoice` and receipts; PDFs regenerated in that language; payment errors as codes |
| 6 | Collection notices and dunning emails in the tenant's language |
| 7 | Device and license messages as codes |
| 8 | Legal documents as one approved text per language and version; `PlatformTenantTermsAcceptance` records version and language; reminders sent in the tenant's language |
| 9 | `Contracts.Platform` returns codes and resolved text for the requested language; no English sentences on the wire |

The legacy use-case catalogue returns English strings. Every behavior ported in section 5 returns an `Error` code instead, and the slice design note lists the codes it declares.

## 8. Gate for each slice

Builds with no warnings; architecture tests pass; unit tests for Domain and Application; authorization and tenant-isolation tests; anonymous endpoints listed; en, fr and es resources complete for everything the slice adds; slice design note in `docs/platform/`; owner signs the verification record. Then the next slice starts.

## 9. Decisions

Resolved by [ADR-0003](../decisions/ADR-0003-platform-plane-scope-and-staff-accounts.md):

- **O-6** Operations owns tenant staff accounts and the staff login. Platform keeps the tenant directory and entitlements and exposes them through `Contracts.Platform`.
- **O-7** Identity and access is the first slice after Foundation.
- **O-8** Entity names in section 4 are accepted as written.

Resolved by [ADR-0010](../decisions/ADR-0010-standard-v1-3-data-naming-hostnames.md) (Global Standard v1.3): the `platform` schema and per-plane connection string and login (D-1), class prefix with unprefixed tables (4.1), `techcrafters.ltd` hostnames until migration, Public goes live with Platform.

## 10. Backlog for later slices (owner, 2026-10-14)

Each needs its own ADR before it is built. They were chosen to bring tenant management to industry standard.

| Item | What it means | Likely slice |
|---|---|---|
| Four-eyes approval | A second staff member approves banning a tenant, and later refunds and write-offs. The request, the approval and the action are each audited; nobody approves their own request | With Slice 4 (refunds) or earlier for ban |
| Data retention and export | On request, a tenant's data is exported (Platform and Operations parts). Once a tenant is deactivated, its data is deleted after a retention period, legal holds excepted. Needs Operations | After Stage 3 |
| Business verification (KYB) | Registration documents and tax number are checked before a self-registered tenant is activated. Reduces fraud once card payments go live | With Slice 5 |
| Theme and template marketplace ([ADR-0017](../decisions/ADR-0017-operations-and-customer-built-together.md), owner 2026-10-16) | Catalog of landing-page themes and templates with versions, previews and prices; which plans include which themes; purchases billed as add-ons on the Platform invoice; which themes each tenant owns. Tenants browse and buy from an Operations "Website" screen through the internal API; their site content stays in Operations; Edge shows the site | Slice 10, after the V3 migration |
