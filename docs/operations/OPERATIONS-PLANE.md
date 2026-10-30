# Operations and Customer planes - Stage 3 build document

| | |
|---|---|
| Status | **Draft for owner review, 2026-10-17.** Built from the owner's Operations plan (decisions O-1 to O-16), the [V3 Operations review](V3-OPERATIONS-REVIEW.md), ADR-0017 and ADR-0018. Slices are verified one at a time, as in Platform. |
| Owner | Operations team, with the Customer team for the customer side (ADR-0017) |
| Governed by | [Global Standard](../global/VIXLY-GLOBAL-STANDARD.md), ADR-0001, ADR-0003, ADR-0005, ADR-0012, ADR-0014, ADR-0017, ADR-0018, ADR-0019, ADR-0021, ADR-0022 |
| Replaces | The Version 1 Customer and Operations briefs (extraction framing) |

## 1. What Stage 3 builds

The plane a tenant runs its business on, and the portal its customers use:

- **Operations** (`Operations.Web`, `Operations.Api`): the tenant's staff, warehouses and branches, customers and mailboxes, receiving, charges, shipments, consolidation, dispatch, delivery, finance. It owns this data in the `ops` schema (D-1).
- **Customer** (`Customer.Web`): a customer's own view at `{tenant-host}/customer`: sign-up, mailbox address, pre-alerts, shipments, tracking, invoices and payment. It owns no business data; it reads and writes through `Operations.Api` (cross-plane rule 2). Its only schema, `customer`, holds its Data Protection keys (ADR-0010).
- **`Contracts.Operations`**: the wire types between Customer and Operations (and later Edge and the apps), owned jointly (section 10 of the standard).

What Stage 3 does not build: Platform features (tenants, plans, Vixly's billing, devices and licences, legal terms stay in Platform and are read through `Contracts.Platform`); Edge (Stage 5); the theme marketplace (Platform Slice 10, after the migration, ADR-0017).

**Go-live is all at once** (O-13): no V3 tenant moves until Stage 3, the migration tool and the Edge routing needed on the tenant host are verified together. The order below follows what depends on what, not a release plan. The standard still says tenants move one at a time (M-3, ADR-0010 item 5); an ADR amending M-3 for the weekend move is written with the migration plan, before any migration work starts.

## 2. Evidence base (legacy, read-only)

V3 Operations: about 185 use cases in 22 folders, 184 Razor files, 67 migrations plus about 20 hand-run scripts, about 30 tests. Its weaknesses and its parity list are in the [V3 review](V3-OPERATIONS-REVIEW.md); the plan's 18 "Fixes over V3" are the commitments. V4 does not copy V3 code. It uses V3 as the list of behaviours tenants rely on (section 5 of the review), and the migration tool reads V3 only to move data (M-2).

## 3. Slices

Each slice goes through all layers (Domain, Application, Infrastructure, Api, Web) on both planes and passes the gate (section 8) before the next starts. Each slice has a sweep (evidence and open questions), a design note with a **Customer side** section, and a verification record covering both planes (ADR-0017).

| Slice | Context | Operations side | Customer side | Depends on |
|---|---|---|---|---|
| 0 | Foundation | Solutions, `ops` schema and migrations history, `OperationsAuthorizationContext` and gate, tenant from the host name, Platform client (service tokens, entitlements, event feed), outbox and worker, hosting, architecture tests for both planes | `Customer.Web` host at `/customer` with its own cookie and key ring, the Operations.Api caller channel | none |
| 1 | Staff and tenant setup | Staff accounts and sign-in, roles and permissions, warehouse scope, licensed devices, tenant settings (currency, units, payment gate, delivery modes), branches and warehouses, numbering, brand basics | (none) | 0 |
| 2 | Customers and mailboxes | Customer records, casillero codes, mailbox addresses, credit flag | Self-registration with bot check (ADR-0019), email verification, sign-in, password reset, profile, mailbox address | 1 |
| 3 | Receiving, inspection and tracking | Tracking core (ADR-0021); merchant-to-warehouse intake at a scan-only dock: one 2 x 1 ID label per scan, one warehouse receipt per carrier delivery, dock printers through a print program (counter drop-off moved to Slice 5); an inspection station for condition, weight, size and photos in the tenant's file store; inspection queue with a condition check on every package (damaged means held until the customer is reached) and compliance inspection by rules plus a random share; holds; unidentified and unclaimed packages; locations and putaway (ADR-0022, O-2) | Pre-alerts; "received" notices with photos; holds with what to do | 2 |
| 4 | Charges engine | Rate cards, charge rules and fee packs (O-11), government fees as pass-through lines (O-10), insurance on the customer's request (owner, 2026-10-18), consolidation charges, one rating engine behind quotes and invoices: an invoice is built from the service charges and the parcels' data | Rate calculator and estimates | 2 |
| 5 | Shipments and documents | Shipments and parcels (a parcel always belongs to a shipment), service and booking (the customer chooses destination, recipient, consolidation, refrigeration and insurance when creating the shipment, 3C step 1), milestones (ADR-0018), holds, shipping labels and documents; **counter drop-off** with its invoice (moved from Slice 3, owner 2026-10-18) | Shipments, insurance request, tracking (ownership checked, S-3), documents | 3, 4 |
| 6 | Consolidation and carrier schedules | Bundles, manifests, carrier schedules (manual in every plan, carrier data in higher plans, O-9), export filing data (O-3) | Departure and arrival dates on tracking | 5 |
| 7 | Dispatch | Dispatches and legs, cut-offs | Milestone updates | 6 |
| 8 | Delivery and proof | Destination receiving, pickup counter, home-delivery routes, proof of delivery, partner agents' limited login (O-14) | Delivery address and pickup choice, proof shown | 7 |
| 9 | Finance | Invoices, payments (cash drawer with shift close, card through the tenant's provider, Zelle, cash on delivery, O-15), credit accounts, receivables and payables, profit per shipment | Invoices and payment | 4, 5 |
| 10 | Integrations | QuickBooks Online (O-7), messaging (O-8), carrier data providers, tenant API and webhooks | Notifications by email, SMS, WhatsApp | 9 |
| 11 | Reporting | Operational KPIs, aging, profit, scheduled exports | (none) | 9 |
| 12 | Handheld and driver apps | .NET MAUI scan and driver apps on the outbox with an offline queue (O-4, O-5) | Branded customer app (later) | 8 |

Which of slices 10 to 12 must be ready before the migration weekend is decided from what V3 tenants use today; reports and counter sales are stubs in V3 (review, section 1).

Two workstreams run beside the slices: **the migration tool** under `build/` (rehearsed on a copy of production; derives milestones per ADR-0018 and keeps V3 codes as legacy codes), and **the Edge routing** needed on the tenant host (Stage 5 scope, scheduled before go-live).

## 4. Entity names (rule 4.1)

Classes carry the `Operations` prefix; tables are the plural noun in the `ops` schema. The Customer plane has read models only (`Customer...`), no tables beyond its keys.

| V3 | V4 | Note |
|---|---|---|
| TenantUser, TenantUserAccount, tokens | `OperationsUser`, `OperationsUserAccount`, `OperationsUserSession`, `OperationsUserToken` | ADR-0003. Sign-in rules in the Slice 1 design |
| TenantStaffRole, TenantStaffRolePermission, TenantPermissions | `OperationsRole`, `OperationsRolePermission`, `OperationsRoleGrant` | Permission catalogue in code, as in Platform; scope (tenant, branch, warehouse) on the grant |
| TenantStaffJobTitle | `OperationsJobTitle` | |
| Warehouse | `OperationsWarehouse`, `OperationsBranch` | Status is an enum, not free text (review 3.11) |
| TenantSystemPreferences | `OperationsTenantSettings` | One row per tenant: currency, units, payment gate, delivery modes, security and numbering settings. Contacts, language and time zone stay in Platform (`PUT /tenant`); payment methods come with Slice 9; volumetric divisors sit on each shipping-service setup |
| Landing page, logo, colours | `OperationsTenantBrand` | ADR-0012 W-2: draft and publish |
| NumberSequence | `OperationsNumberSequence` | Every code comes from a per-tenant sequence (review 2.10) |
| Customer, CustomerAccount, tokens | `OperationsCustomer`, `OperationsCustomerAccount`, `OperationsCustomerSession`, `OperationsCustomerToken` | Customer sessions are server records in Operations, so a reset or suspension ends them; Customer.Web holds only the cookie |
| TenantCustomerMailbox | `OperationsMailboxAddress` | |
| CustomerMailboxItem | `OperationsPreAlert` | |
| WarehouseReception | `OperationsWarehouseReceipt` | Per-tenant `WR` numbers (review 3.7); both intakes end in one (ADR-0022) |
| (new) | `OperationsInspection`, `OperationsInspectionRule`, `OperationsRestrictedItem`, `OperationsDropOffDeclaration`, `OperationsPackagePhoto`, `OperationsStorageLocation` | ADR-0022 |
| Shipment, Parcel | `OperationsShipment`, `OperationsParcel` | Milestones per ADR-0018 |
| TrackingEvent | `OperationsTrackingEvent` | Append-only: what, when, where, who, how, why, visibility, evidence; idempotency key; corrections void, never edit (ADR-0021) |
| Bundle, BundleParcel | `OperationsBundle`, `OperationsBundleParcel` | A parcel sits in one open bundle (unique index, review 3.8) |
| Dispatch, DispatchLeg, DispatchBundle | `OperationsDispatch`, `OperationsDispatchLeg`, `OperationsDispatchBundle` | |
| ShippersLetterOfInstruction | `OperationsExportFiling` | Data for the licensed filer (O-3) |
| PricingProfile, AirPricingTier, SeaPricingRule, OperationalCharge*, Insurance* | `OperationsRateCard`, `OperationsChargeRule`, `OperationsFeePack`, `OperationsInsuranceTier` | One rating engine (fix 16) |
| TenantShippingServiceSetup | `OperationsShippingServiceSetup` | Keyed by `PlatformShippingService` code from `Contracts.Platform` |
| CustomerInvoice, lines, payments, sessions | `OperationsInvoice`, `OperationsInvoiceLine`, `OperationsPayment`, `OperationsCashDrawerShift` | Money with currency (L-8) |
| CustomerCreditAccount, ledger | `OperationsCreditAccount`, `OperationsCreditLedgerEntry` | |
| (new) | `OperationsAuditEntry`, `OperationsOutboxMessage`, `OperationsJobRun` | As in Platform |
| (new) | `OperationsProofOfDelivery`, `OperationsDeliveryRoute`, `OperationsCarrierSchedule`, `OperationsVendorBill` | Slices 6 to 9 |

## 5. Trust and data rules specific to Stage 3

- **Tenant isolation is enforced by the data layer, not remembered by each query** (review 3: no global filter, 12 child tables without `TenantId`). Every Operations table carries `TenantId`, child tables included, except a short list of marked system tables (Slice 0, D-F1); the `DbContext` applies a tenant query filter from the authorization context; an architecture test fails an entity without `TenantId`; S-13 tests prove tenant A cannot see tenant B for every read and write.
- **Warehouse and branch scope is a repository filter** with a two-warehouse role matrix test (fix 3, S-13).
- **The tenant comes from the host name**, resolved through Platform (`GET /internal/v1/portal/{slug}`, cached; Customer.Web asks Operations.Api), never from a cookie claim alone; the session must belong to that tenant.
- **Customer identity comes from the Customer cookie and is passed to Operations.Api in the signed caller token**; every customer read checks that the record belongs to that customer (S-3, fix 4).
- **Operations.Api is internal** (cross-plane rule 3): callers sign short-lived tokens (ADR-0014 pattern, extended to Operations).
- **Platform is the source of truth** for tenant status, plan, entitlements, seats, devices and legal acceptance; Operations caches them and follows the Platform event feed (`GET /events`). A suspended tenant's staff and customers are refused.
- **Money** carries its currency on every amount (L-8); one billing currency per tenant (owner, 2026-10-17; this replaces the plan's "invoice currency per customer"). **Weights and dimensions** are stored in one canonical unit and shown in the tenant's units (review 3.4).
- **Every code** (customer, shipment, parcel, receipt, bundle, dispatch, invoice, receipt of payment) comes from a per-tenant sequence; V3 codes migrate as legacy codes.
- **Times** are stored in UTC and shown in the tenant's time zone, which V3 never did (review 3.14).
- **Repeats**: every command is protected against double clicks (ADR-0019); commands customers see or that move money take an idempotency key.

## 6. Multilingual and white label

Operations and Customer are English, French and Spanish end to end (5.5), and tenant surfaces never say "Vixly" (ADR-0012 W-1): their resources use `{brandName}`. Customer emails and documents go out in the customer's language with the tenant's brand.

## 7. Performance specifics

Paged, projected reads everywhere (P-1); one read model per dashboard (P-2); virtualized lists; debounced scan input with `ShouldRender` overrides (P-4); indexes ship with the query (P-6). The scan screens get a measured target in the Slice 3 design.

## 8. Gate for each slice

As in Platform, for both planes: builds with no warnings; architecture tests pass; unit, database and HTTP tests; authorization, tenant-isolation and warehouse-scope tests; anonymous endpoints listed with their rate limits; en, fr and es resources complete; design note with its Customer side; verification record signed by the owner.

## 9. Anonymous endpoints (to be filled per slice)

| Endpoint | Plane | Purpose | Limit |
|---|---|---|---|
| (Slice 1) staff sign-in pages | Operations.Web | Sign-in, second factor, invitation, password reset | per IP and per account |
| (Slice 2) `POST /customer/sign-up` | Customer.Web | Self sign-up; the bot check runs first; the same answer for known emails | 5 per hour per IP |
| (Slice 2) `POST /customer/sign-in` | Customer.Web | Email and password | 10 per minute per IP; 5 failures lock the account 15 minutes |
| (Slice 2) `POST /customer/forgot-password`, `/customer/verify/resend` | Customer.Web | Reset and confirmation links; the same answer for unknown emails | 5 per hour per IP |
| (Slice 2) `POST /customer/verify`, `/customer/reset-password` | Customer.Web | Single-use links; GET only shows a button | 10 per hour per IP |

## 10. Decisions

Taken: O-1 to O-16 (Operations plan), ADR-0017 (built together; marketplace in Platform; site content in Operations), ADR-0019 (double-click protection, bot check before sending), ADR-0018 (milestones, accepted 2026-10-17), ADR-0021 (tracking events) and ADR-0022 (receiving and inspection), accepted 2026-10-17. Slices 0 and 1: [sweep](SLICE-0-1-FOUNDATION-AND-SETUP-SWEEP.md), [Slice 0 design](SLICE-0-FOUNDATION-DESIGN.md) and [verification](SLICE-0-VERIFICATION.md), [Slice 1 design](SLICE-1-STAFF-AND-SETUP-DESIGN.md) [1A verification](SLICE-1A-VERIFICATION.md), [1B tenant site design](SLICE-1B-TENANT-SITE-DESIGN.md); [Slice 2 design](SLICE-2-CUSTOMERS-DESIGN.md) and [verification](SLICE-2-VERIFICATION.md); [Slice 3 design](SLICE-3-RECEIVING-DESIGN.md), [3A verification](SLICE-3A-VERIFICATION.md), [3B verification](SLICE-3B-VERIFICATION.md); [3C design](SLICE-3C-INSPECTION-DESIGN.md) and [verification](SLICE-3C-VERIFICATION.md). Open questions for each later slice are listed in its sweep.
