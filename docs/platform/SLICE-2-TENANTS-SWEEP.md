# Slice 2: Tenants - V3 behaviour sweep

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Sweep done, 2026-10-14. Slice 1 signed off. Brand and landing page removed from Platform (ADR-0012 Amendment 1). All decisions taken 2026-10-14 (owner agreed D-T1 to D-T4, D-T6 and D-T8 as proposed). Next: design note.** |
| Source | V3 `ControlPlane` (read-only): `src/Vixly.Platform.Domain`, `src/Vixly.Platform.Application`, `src/Vixly.Platform.Web/Components`, `Vixly.Platform.API/Controllers` |
| Rule | Replicate behaviour, not code. Classification as in Slice 1: Active, Duplicate, Dead, Disconnected-wanted, Moved |
| Feeds | Slice 2 design note; ADR-0012 as amended (white label is Operations work) |

## 1. How the evidence was gathered

Each Tenants use case under `UseCases/Tenants` and `UseCases/PortalAccess` was traced to the console pages and API controllers that inject it. Entities and enums were read for their states and rules.

## 2. Use cases (25)

| V3 use case | Called from | Status | V4 outcome |
|---|---|---|---|
| GetTenants | TenantWorkspace, CollectionSubscriptionAccess | Active | `ListPlatformTenants`, paged (P-1: V3 loads every tenant) |
| GetTenantById | TenantWorkspace, CollectionSubscriptionAccess | Active | `GetPlatformTenant` |
| SearchTenantByPhone | BillingWorkspace | Duplicate | Folded into the tenant list search (name, code, slug, email, phone) |
| ManageTenantLifecycle | TenantWorkspace, CollectionSubscriptionAccess | Active | Split into one use case per transition (activate, suspend, reactivate, deactivate, ban), each with its own permission and history event |
| GetTenantCompanyProfile | API `TenantCompanyProfileController` | Active | `GetPlatformTenantProfile`; also exposed to Operations through `Contracts.Platform` (Slice 9) |
| UpdateTenantCompanyLogo | API `TenantCompanyProfileController` | Active | **Moved** to Operations with the tenant brand (ADR-0012 Amendment 1) |
| RegisterTenant | nobody | Dead | Not ported; see D-T8 for staff-created tenants |
| StartTenantRegistration | API public `register` | Active | `StartPlatformTenantRegistration` (anonymous, rate-limited) |
| CheckPortalSlugAvailability | API public `portal-slugs/{slug}/availability` | Active | `CheckPortalSlugAvailability` (anonymous, rate-limited, no enumeration of names) |
| VerifyTenantRegistrationEmail | API public `verify-email` | Active | Kept |
| ResendTenantRegistrationVerification | API public | Active | Kept, throttled per registration and per IP |
| GetTenantRegistrationStatus | API public | Active | Kept; answers the next step only |
| UpdateTenantRegistrationEmail | nobody | Disconnected-wanted | Kept behind the resend flow: correct a mistyped email before verification |
| SelectTenantPlan | API public | Active | **Waits for Slice 3** (plans) |
| GetTenantRegistrationInvoice | API public | Active | **Waits for Slice 4** (invoices) |
| RegisterTenantRegistrationCardPayment | API public | Active | **Waits for Slice 5** (payments) |
| CompleteTenantRegistrationAfterVerifiedPayment | BillingWorkspace | Active | **Waits for Slice 5** |
| ResumeTenantRegistrationAccountSetup, ActivateTenantAccount | API public | Active | **Moved** to Operations: the tenant owner's account is an Operations user (ADR-0003). Platform raises "registration ready for account setup" |
| GenerateFirstDeviceSetupLink, CompleteFirstDeviceSetup, RegisterFirstDevice | API public | Active | **Moved** to Licensing, Slice 7 |
| ValidatePortalAccess, ValidatePortalRuntime, VerifyTenantDevice | API `PortalAccessController` | Active, near-duplicates | **Moved** to Slice 7 and merged into one validation (plane section 5) |

## 3. Entities and rules found

| V3 | Contents | V4 proposal |
|---|---|---|
| `Tenant` | `TenantCode`, `Status`, timestamps per status | `PlatformTenant` + `DefaultLanguage` (plane section 7), legal and trading name |
| `TenantStatus` | Draft, PendingActivation, Active, Suspended (reversible), Deactivated (reversible, business closure), Banned (terminal) | Same six states (D-T1) |
| `TenantCode` | 3 to 50 characters, internal id read by Operations (`/api/internal/tenants/{id}/code`) | Kept, immutable (D-T3) |
| `PortalSlug` (on the company profile) | 3 to 63 characters, the tenant's address | Moves onto `PlatformTenant`, unique, changeable only by staff with history (D-T3) |
| `TenantCompanyProfile` | Name, slug, email, phone, address, logo URL and storage key | `PlatformTenantCompanyProfile`: the legal and billing details Control invoices against; no logo (the brand, in Operations, has it) and no slug (the tenant has it) |
| `TenantLifecycleHistory` | Previous and new status, reason, internal notes, who, when | `PlatformTenantLifecycleEvent`: event code + arguments (D-4), committed with the change (D-2) |
| `TenantManualSuspension` | Category (Compliance, Security, FraudInvestigation, Administrative), reason, resolved by and when | Kept as part of the tenant aggregate (D-T2) |
| `TenantRegistration` (+ verification token) | Status chain PendingEmailVerification → PendingPlanSelection → PendingPayment → PendingAccountSetup → PendingFirstDeviceRegistration → Completed, Cancelled, Expired | `PlatformTenantRegistration`; Slice 2 builds up to PendingPlanSelection (D-T4) |
| `TenantLandingPageConfiguration` | Free JSON content and theme, draft and publish, written by an internal API straight to the DbContext (no use case, no permission) | **Moved** to Operations: brand with allowlisted tokens and landing-page text per language (ADR-0012 as amended) |
| `TenantUser`, `TenantUserAccount`, activation and setup tokens | Tenant staff | **Moved** to Operations (ADR-0003) |
| `TenantSuspensionStateService` | Keeps tenant and subscription suspension in step | Waits for Slice 3 (D-T6) |

V3 permissions: `Tenants.Read`, `.View` (same meaning), `.Create`, `.Edit`, `.Activate`, `.Suspend`, `.Reactivate`, `.Deactivate`, `.Ban`. V4 proposal: `platform.tenants.view`, `.create`, `.edit`, `.activate`, `.suspend`, `.reactivate`, `.deactivate`, `.ban` (one of Read and View, as in Slice 1; no brand permission in Control).

## 4. Console screens found

| V3 page | State | V4 |
|---|---|---|
| `/tenants` TenantWorkspace (874 lines: list, details, registry, subscriptions, licenses, invoices, lifecycle in one page) | Active | Split per ADR-0001: tenant list, tenant page (profile, lifecycle, history); subscriptions, invoices and licenses join the tenant page in their own slices |
| `/registrants` RegistrantsWorkspace | Placeholder ("under construction") | Disconnected-wanted: a registrations list for staff (stuck registrations, resend, cancel) |
| Billing and collections pages reading tenants | Active | Read tenants through the Slice 2 queries |

## 5. Defects and risks seen in V3 (fixed by design in V4)

1. `GetTenants` loads every tenant: no paging.
2. One use case for every lifecycle move: one permission check branch, hard to audit.
3. The landing-page API writes free JSON (theme included) with no validation and no permission; ADR-0012 allows only allowlisted tokens.
4. Logos are files under the API's own `storage/tenant-logos` folder, lost on redeploy unless backed up. (Now an Operations concern.)
5. History is free text in English, so it cannot be shown in French or Spanish.

## 6. Owner decisions (agreed as proposed, 2026-10-14, unless marked otherwise)

| # | Question | Proposal |
|---|---|---|
| D-T1 | Keep the six V3 states (Draft, Pending activation, Active, Suspended, Deactivated, Banned)? | Yes. Ban is terminal and needs SuperUser authority and step-up; suspend, deactivate and ban need a reason and step-up |
| D-T2 | Keep the suspension categories (Compliance, Security, Fraud investigation, Administrative)? | Yes, as a fixed list with translated names |
| D-T3 | Keep both an internal tenant code and a public portal slug? | Yes. Code never changes; slug changes only by staff, with history and the old slug held for 90 days so links do not go to someone else |
| D-T4 | Slice 2 builds registration only up to "choose a plan"? | Yes. Plans, invoice and card payment are added in Slices 3 to 5; account setup moves to Operations; first device to Slice 7 |
| D-T5 | Branding scope in Slice 2 | **Resolved 2026-10-14 (owner):** none. White label is Operations only; Control keeps the commercial side (plan features, billing) from Slice 3 |
| D-T6 | Tenant status and subscription in step | Slice 2 changes tenant status only; Slice 3 reacts to tenant events to suspend or restore the subscription |
| D-T7 | Where logos are stored | **Withdrawn:** logos belong to the brand in Operations; decide with the Operations design |
| D-T8 | May staff create a tenant directly (sales-led onboarding)? | Yes, `CreatePlatformTenant` from the console, starting in Draft; V3's unused `RegisterTenant` is not ported |
