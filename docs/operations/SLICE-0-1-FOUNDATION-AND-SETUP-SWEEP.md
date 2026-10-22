# Slices 0 and 1: Foundation, staff and tenant setup - sweep

| | |
|---|---|
| Plane | Operations and Customer (Stage 3) |
| Status | **Sweep done, 2026-10-17. Owner answered D-O2 (required for Owner and Administrators, optional for others, tenant can require it for all), D-O6 (15 minutes for every tenant) and D-O7 (one currency per tenant only); the rest as proposed. Next: design notes for [Slice 0](SLICE-0-FOUNDATION-DESIGN.md) and [Slice 1](SLICE-1-STAFF-AND-SETUP-DESIGN.md).** |
| Source | V3 `OperationPlane` (read-only, copied for review): `UseCases/Authentication`, `UseCases/TenantUsers`, `UseCases/RolesAndPermissions`, `UseCases/SystemPreferences`, `UseCases/CompanyProfile`, `UseCases/Warehouses`; `Services/Security/OperationsPermissions.cs`; the [V3 review](V3-OPERATIONS-REVIEW.md); Platform Slices 2, 7 and 9 for what Operations must call |
| Rule | Replicate behaviour, not code |
| Feeds | Slice 0 and Slice 1 design notes; the [plane document](OPERATIONS-PLANE.md) |

## 1. Scope

The owner's first slice, "Foundation and tenant setup", is split into two gates, as Platform did with its Slice 0 and Slice 1 (D-O1):

- **Slice 0, Foundation:** solutions, `ops` schema, authorization context and gate, tenant from the host name, Platform client, outbox and clocks, Data Protection, hosting, Customer.Web skeleton, the Operations.Api caller channel, architecture tests for both planes. No screens a tenant would use.
- **Slice 1, Staff and tenant setup:** the tenant owner's first sign-in, staff accounts and sign-in, roles and scope, licensed devices, tenant settings, branches and warehouses, numbering, brand basics.

## 2. What V3 does

### Staff accounts and sign-in

| V3 | Finding | V4 proposal |
|---|---|---|
| `LoginTenantUser` | Email and password only, tenant from the host. No second factor anywhere in Operations | Second factor: question D-O2 |
| `CreateTenantUserSetupToken`, `ValidateTenantUserSetupToken`, `CompleteTenantUserSetup` | Staff invited by a setup link; they choose a password | Kept: invitation link, password rules as Platform (12+ characters, breached-password check, BCrypt cost 12, M-4) |
| `Suspend`, `Lock`, `Reactivate`, `Deactivate` account | Statuses PendingSetup, Active, Suspended, Deactivated, Locked | Same lifecycle as Platform staff (Invited, Active, Suspended, Locked, Deactivated); reason, permission, step-up and audit on every change; the Owner can never be suspended by someone else |
| `TenantUserAuthorityLevel` | Standard, Manager, Administrator, Owner, used beside roles | Roles decide permissions; only **Owner** remains special (one per tenant, protected, transfer by the Owner only) |
| `TenantUser.WarehouseId` | One warehouse per user; scope otherwise enforced in the menu (SEC-01) | Scope on each role grant: whole tenant, a branch, or a warehouse; enforced in repositories (plane section 5) |
| Idle-session timeout, `SecurityActionThrottle` | Throttled login, reset, registration | Kept: per-IP and per-account limits, lockout; idle timeout length is question D-O6 |
| `PortalEntryMiddleware` + Platform calls | Staff work only from a licensed device | Kept, through Platform Slice 7: device key pair or rotating secret on every use, positive answer cached at most 5 minutes |

### Roles and permissions

- V3: tenant-defined roles (`TenantStaffRole`) over a catalogue of **121 permissions** in 26 areas (dispatch 10, staff 9, shipments 9, credit 9, pricing 7, export documents 7, reports 6, roles 5, events 5, licensing 5, and so on), with a scope type (tenant-wide, regional, warehouse).
- V4: the catalogue stays in code, named `ops.<area>.<action>`, grown slice by slice (Slice 1 adds only staff, roles, settings, branches, warehouses, devices, brand). System roles **Owner** and **Administrator** are created with the tenant; tenants add their own.

### Tenant settings

| V3 | Fields | V4 proposal |
|---|---|---|
| `TenantSystemPreferences` | Preferred weight unit (kg or lb), volumetric divisor (default 5000, **ignored** by the code), allow Collect (ignored) | `OperationsTenantSettings`: weight unit and dimension unit for display; weights stored in kilograms and dimensions in centimetres (D-O8); volumetric divisor per shipping service (D-O8); payment gate (ADR-0018: paid first by default, Collect and credit allowed by setting); delivery modes offered (pickup, home delivery, both) |
| `TenantPaymentSettings` | Cash, office or portal Zelle, Zelle details and QR, card provider metadata | Moves to Slice 9 (Finance); Slice 1 keeps only the billing currency |
| `TenantContactPhoneNumbers`, `CompanyProfile` | Contact numbers; company profile edited in Operations | Legal details live in Platform and change only by request (B12). Operations shows them, sends change requests (`/tenant/company-change`), and edits contacts, language and time zone through `PUT /tenant` |
| Landing page and logo | In V3 Platform | `OperationsTenantBrand` (ADR-0012 W-2): name, logos, colour preset, sender name, support contacts; draft and publish. The landing page itself waits for the marketplace (ADR-0017) |
| (none) | No time zone anywhere (review 3.14) | The tenant time zone from Platform is used for every "today" and every date shown |
| Currency | None (review 3.12) | Question D-O7 |

### Warehouses and branches

- V3 `Warehouse`: code, name, country, city, **free-text status**, address, manager, "is dispatch hub", assigned hub, distance to hub. One mailbox warehouse per tenant (`TenantCustomerMailbox`).
- V4 (D-O10): **Branch** = an office of the tenant in a country (origin or destination), with a counter; **Warehouse** belongs to a branch and has roles: receiving (mailbox address), dispatch hub, pickup point. Status is an enum. The mailbox address customers see is set per receiving warehouse (Slice 2 uses it). Partner agents (O-14) come with Slice 8.

### Numbering

V3 mixes per-tenant yearly sequences (invoice, receipt, dispatch, SLI) with ad-hoc generators (shipment, customer, bundle, reception) and global unique indexes. V4: one `OperationsNumberSequence` per tenant, kind and year; prefixes are tenant settings with defaults (`CUS`, `SHP`, `WR`, `BND`, `DSP`, `INV`, `RCP`); allocation is concurrency-safe; V3 codes are kept as legacy codes.

## 3. What Platform already gives Operations

| Need | Platform (built, Slice 9) |
|---|---|
| Which tenant a host name belongs to | `GET /internal/v1/portal/{slug}` and batch (scope `portal`) |
| Tenant profile, contacts, language, time zone | `GET /tenant`, `PUT /tenant` (owner, admin, step-up) |
| Plan, features, limits, seats | `GET /tenant/entitlements`, `/tenant/subscription` |
| Devices | enrol, verify, challenge, approve, deny, release; first-device link; owner recovery |
| Legal acceptance | `GET /tenant/legal`, `POST /tenant/legal/accept` |
| Company changes | `GET`, `POST`, `DELETE /tenant/company-change` (B12) |
| Changes to follow | `GET /events`: tenant status, slug corrected, subscription status, plan version, devices, seats, legal |

**Gap found:** nothing tells Operations that a newly registered tenant has paid and its owner can set up their account. Platform moves the registration to "pending account setup" silently (`PlatformRegistrationPaidHandler`). Proposal D-O3: Platform adds the event `platform.registration.account_setup_ready.v1` (tenant and registration ids only, since the feed is not split by tenant) on every move to "pending account setup", paid or trial; Operations reads the owner contact with a tenant-scoped call and invites the owner. A second gap: Platform's portal lookup answers "not found" while a tenant is `PendingActivation`, so Operations could not serve the owner's setup pages; Platform answers "setting up" to the `operations` caller (Slice 0 design).

## 4. Foundation findings (Slice 0)

| Topic | V3 | V4 proposal |
|---|---|---|
| Tenant isolation | No query filter; 12 child tables without `TenantId` | `TenantId` on every table; a `DbContext` query filter from the authorization context; architecture test; S-13 tests (plane section 5) |
| Tenant resolution | Cookie claim `tenant_id` | From the host name through Platform, cached 60 s; the session must belong to that tenant (D-O9). Edge later only routes |
| Data Protection | Local disk | `ops` schema (S-5), app names `Vixly.V4.Operations.Web`, `Vixly.V4.Customer.Web` |
| Cookies | Policy-scheme router picks staff or customer cookie | One scheme per process: `__Host-vx4-ops` on Operations.Web, `__Secure-vx4-customer` with `Path=/customer` on Customer.Web (S-4) |
| Platform calls | `X-Vixly-Internal-Key` header | ES256 service tokens with tenant claim (ADR-0014); Operations gets scopes `tenant`, `portal`, `events` |
| Customer.Web to Operations.Api | (same process) | Signed caller tokens like ADR-0014, carrying tenant and customer; Operations.Api checks ownership (S-3) |
| Background work | None reliable | Outbox and clocks inside Operations.Web, as Platform runs them in its console (hosting checklist H2; no separate service needed) |
| Database | Shared with Platform, hand-run scripts | `ops` schema, `ConnectionStrings:Operations`, its own login; migrations only, added to the one global SQL script |
| Tests | About 30 | Architecture tests extended to both planes from day one |

## 5. Owner sign-in flow (new tenant)

1. Tenant pays its first invoice on the public site, or starts a trial; Platform marks the registration "pending account setup" and publishes `account_setup_ready` (D-O3).
2. Operations creates the **Owner** (Invited) from the registration's owner contact and emails a setup link to `{slug}.techcrafters.ltd` in the owner's language.
3. The owner sets a password and enrols the second factor (D-O2), then accepts the current Terms (Platform `/tenant/legal/accept`).
4. Operations asks Platform for the first-device link; the owner opens it on the computer they work from; the device enrols and the tenant becomes Active (Platform Slice 7, one commit).
5. The owner lands on the setup checklist: currency, units, branches and warehouses, staff.

Migrated V3 staff keep their passwords (M-4) and enrol the second factor at their first V4 sign-in if D-O2 requires it.

## 6. Decisions

| # | Question | Proposal |
|---|---|---|
| D-O1 | How to split the first slice | Slice 0 Foundation and Slice 1 Staff and tenant setup, each with its own gate |
| D-O2 | Second factor for tenant staff | **Owner question.** Options: (a) required for everyone, passkey or authenticator app, as Platform staff (ADR-0006); (b) required for the Owner and Administrators, optional for others, with a tenant setting to require it for everyone; (c) optional. Recommended: (b), because scan-station staff already work only from licensed devices |
| D-O3 | Owner's first sign-in | Platform publishes `account_setup_ready`; Operations invites the owner (section 5) |
| D-O4 | Staff email | Unique per tenant; the same person may work for two tenants with two accounts |
| D-O5 | Roles | Fixed permission catalogue in code; Owner and Administrator system roles; tenant roles with scope per grant (tenant, branch, warehouse); authority levels dropped except Owner |
| D-O6 | Idle sign-out for tenant staff | **Owner question.** Platform signs staff out after 5 idle minutes. Options: 15 minutes, 30 minutes, or a tenant setting between 5 and 60 (default 15). Recommended: tenant setting, default 15; 12-hour absolute limit |
| D-O7 | Billing currency | **Owner question.** The owner chose "each tenant picks its currency" (ADR-0018), but the plan's fix 10 says "invoice currency per customer". Options: (a) one currency per tenant now; (b) one per tenant, plus a per-customer currency with exchange rates by date in Slice 9 |
| D-O8 | Units | Weights stored in kilograms (3 decimals), dimensions in centimetres (1 decimal); shown and entered in the tenant's units; volumetric divisor per shipping service in the matching units (air 6000 cm³/kg = 166 in³/lb by default) |
| D-O9 | Tenant from the host | Operations.Web resolves `{slug}` through Platform itself and refuses unknown or suspended tenants; Edge only routes |
| D-O10 | Branches and warehouses | Branch (office, origin or destination) holds warehouses with roles (receiving, dispatch hub, pickup point); status enum |
| D-O11 | Background work | Outbox and clocks run inside Operations.Web |
| D-O12 | Numbering | One per-tenant sequence per kind and year; prefixes are tenant settings; V3 codes kept as legacy codes |
