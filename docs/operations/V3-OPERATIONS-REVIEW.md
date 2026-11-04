# Vixly V3 Operations Plane: review for the V4 rewrite

Date: 2026-10-16. Read-only review of `C:\Users\schleid\source\repos\DotNet10\Vixly\OperationPlane` and parts of `SqlScripts`, `Docs` and `AGENTS.md.txt`. Nothing on the device was changed. I copied source snapshots (bin/obj/.codex-build excluded) into this container and analysed them there.

Path shorthand used below:

- `D/` = `OperationPlane/src/Vixly.Operations.Domain/`
- `A/` = `OperationPlane/src/Vixly.Operations.Application/`
- `UC/` = `A/UseCases/`
- `I/` = `OperationPlane/src/Vixly.Operations.Infrastructure/`
- `W/` = `OperationPlane/src/Vixly.Operations.Web/`
- `OP/` = `OperationPlane/` (root docs and SQL)

Findings marked **(verify)** come from reading the code, not from running it. Confirm them with the owner or against data.

---

## 1. Inventory

### Projects and size (.cs + .razor only)

| Project | Files | Lines | Notes |
|---|---|---|---|
| Domain | 89 | 8.7k | Entities, enums, a single value object. Mostly anemic. Some guard methods. |
| Application, excluding UseCases | 243 | 14.1k | Interfaces, models, pricing resolvers, numbering, security, tracking services |
| Application UseCases | 463 | 42.3k | 185 `*UseCase` classes in 22 folders (request/response/use case per folder) |
| Infrastructure | 204 | 25.6k | EF Core 10 (SQL Server), 67 migrations, repositories, QuestPDF documents, SMTP, Platform API clients |
| Web (Blazor Server) | 265 | 64.1k | 184 .razor files, 58 routable pages, 5 MVC controllers, FluentUI 5 |
| Tests | 14 | 1.1k | Operations.Tests (xUnit) and Ui.Tests (bUnit). About 30 test methods. |

Stack: net10.0, EF Core 10.0.13, QuestPDF 2026.9.1, BCrypt.Net-Next, Microsoft.FluentUI.AspNetCore.Components 5.0.1 (`I/Vixly.Operations.Infrastructure.csproj`, `W/Vixly.Operations.Web.csproj`).

The repo root also holds about 20 hand-written SQL deploy scripts (`OP/*.sql`, `OP/sql/*.sql`) and about 60 markdown docs. Many of the docs cover the payment-provider framework "MTPPIF" and are not specific to operations.

### Bounded contexts and modules (use-case folders)

| Module | Use cases | Main pages (routes) |
|---|---|---|
| Customers and casillero | Customers (23), Mailbox (4), InboundDeliveries (1) | `/customers`, `/customer-credit-accounts`, `/operation-control/inbound-deliveries`; portal `/customer/*` |
| Receiving / warehouse | InboundReception (3), Warehouses incl. Bundles (16) | `/operation-control/warehouse-reception`, `/warehouse`, `/operation-control/parcel-operations/parcel-queue`, `/operation-control/parcel-operations/bundles` |
| Shipments and parcels | Shipments (27) | `/operation-control/shipments`, `/shipments`, `/operation-control/parcels` |
| Consolidation / manifests | Bundles (in Warehouses), ExportDocuments/SLI (8) | bundles page, `/export-documents/shippers-letter-of-instruction` |
| Dispatch | Dispatching (10) | `/operation-control/dispatching` |
| Delivery | None. Delivery exists only as manual tracking events. | `/tracking`, `/operation-control/operational-events` |
| Invoicing / charges | Billing (15) | `/operation-control/billing/invoices`, `/operation-control/billing/payments` |
| Payments / credit | Billing payments, customer credit ledger, TenantPaymentSettings (cash, Zelle, card metadata) | payments page, `/operation-control/payment-settings`, `/customer/credit-account` |
| Rates / pricing | Pricing (33), ShippingServices (3) | `/settings/pricing-engine`, `/settings/shipping-services` |
| Tracking / events | Tracking (2), OperationalEvents (2) | `/tracking`, `/operation-control/operational-events` |
| Users / staff / RBAC | TenantUsers (12), RolesAndPermissions (5), Authentication (7) | `/operation-control/tenant-users`, `/administration/roles-and-permissions`, `/staff/*` |
| Devices / licensing | Licensing (5), calls the Platform API | `/operation-control/licensing`, `/device/register`, `/setup-first-device`, `/reassignlicense` |
| Tenant settings | SystemPreferences (2), CompanyProfile (2), Contacts (3), Subscriptions (2) | `/operation-control/system-preferences`, `.../landing-page`, `/company-profile`, `/operation-control/contact-numbers` |
| Reports | Two PDF reports only (invoice listing, bundle manifest) | `/reporting` is an "under construction" stub (`W/Components/Pages/Reports/ReportsWorkspace.razor`) |
| Counter sales | None | `/counter-sales` is a stub page |
| Notifications | Email for auth flows only (verification, password reset, staff invitation) (`A/Interfaces/EmailServices/IEmailSender.cs`). No SMS or WhatsApp, and no operational notifications. | n/a |
| Customer portal | register, verify email, login/reset, mailbox pre-alerts, create a shipment from pre-alerts, shipments/parcels, invoices (with Zelle QR), credit account, profile | `/customer/*` |
| Integrations | Platform internal API (company profile, subscriptions, licensing, portal access) over HTTP with the `X-Vixly-Internal-Key` header. No carrier, customs or payment-gateway integration. | n/a |

---

## 2. Modules in detail

### 2.1 Customers and casillero (mailbox)

Entities:

- `Customer` (`D/Entities/Customers/Customer.cs`): TenantId, CustomerCode (`CUST-0001`, the casillero number), Type, BusinessName/FullName/ContactName, Email, PhoneNumber, IsEmailVerified, IsCreditAllowed, address (Line1/2, City, State, PostalCode, Country), Status, Notes.
- `CustomerAccount`: CustomerId, PasswordHash (BCrypt), IsActive.
- Tokens for verification, setup and password reset.
- `TenantCustomerMailbox`: one per tenant, MailboxWarehouseId. This is the US warehouse whose address is shown to customers as their mailbox address.
- `CustomerMailboxItem` (`D/Entities/Customers/CustomerMailboxItem.cs`): the customer pre-alert. Fields: CustomerId, ShipmentId?, MailboxWarehouseId, Carrier, ExternalTrackingNumber (unique, per migration `20260903080442_EnforceUniqueCustomerMailboxTrackingNumber`), ExpectedDeliveryDateUtc, Category, PurchaseValue, ExpectedParcelCount, Status.
- `CustomerCreditAccount` + `CustomerCreditLedgerEntry`: CreditLimit, CurrentBalance, a value-object account number, and an immutable ledger.

Statuses (verbatim):

- `CustomerStatus`: Active=1, Inactive=2
- `CustomerType`: Individual=1, Business=2
- `CustomerMailboxItemStatus`: InTransit=1, Received=2, Cancelled=3, Completed=4
- `CustomerCreditAccountStatus`: Active=1, Suspended=2, Closed=3
- `CustomerCreditDirection`: Debit=1, Credit=2
- `CustomerCreditEntryType`: InvoiceCharge=1, Payment=2, Adjustment=3, WriteOff=4, Reversal=5, CreditIncrease=6, CreditDecrease=7, Refund=8

Flows:

- The customer self-registers on the portal, verifies email and sets a password. Staff can also create customers.
- The customer creates pre-alerts, then picks pre-alerts to create a shipment (`UC/Shipments/CreateCustomerShipment/CreateCustomerShipmentUseCase.cs`). The parcel count is the sum of ExpectedParcelCount, and the origin is the mailbox warehouse.
- Staff see expected inbound deliveries (`UC/InboundDeliveries/...`).

### 2.2 Receiving / warehouse

- `Warehouse` (`D/Entities/Warehouses/Warehouse.cs`): Code, Name, Country, City, Status (**free-text string**), address, manager contact, IsDispatchHub, AssignedDispatchHubWarehouseId, DistanceToHubMiles.
- `WarehouseReception` (`D/Entities/Logistics/WarehouseReception.cs`): ReceptionNumber (`WR-yyyyMMddHHmmssfff`), WarehouseId, ReceivedByTenantUserId, ShipmentId?, CarrierTrackingNumber, Status, ReceivedOnUtc.
- `WarehouseReceptionStatus`: Matched=1, Orphan=2 (stored as a string column).
- Receiving scans a carrier tracking number and matches it against `Shipment.TrackingNumber` (`UC/InboundReception/ReceiveParcelByCarrierTracking/ReceiveParcelByCarrierTrackingUseCase.cs`). If nothing matches, the reception is recorded as an orphan.

### 2.3 Shipments and parcels

`Shipment` (`D/Entities/Shipments/Shipments.cs`) fields:

- ShipmentCode (`SHP-000001`), TrackingNumber (`{serviceCode}-{8 random digits}-{check digit}`, assigned at release)
- TenantUserId?, CustomerId, PartnerId?
- OriginWarehouseId, DestinationWarehouseId, ServiceTypeId (= TenantShippingServiceSetup.Id)
- RecipientName, RecipientPhoneNumber, ParcelCount (declared), ShipmentDateUtc
- Status, Notes, IsInsured, InsuranceProviderId?, IsConsolidated, ChargeConsolidation

There is no delivery-address field. Delivery is to the destination warehouse; home delivery is not modelled.

`Parcel` (`D/Entities/Shipments/Parcel.cs`): ShipmentId, UserId, ParcelCode (`{tracking}-{seq:000}`), Description, Notes, Length, Width, Height, Weight (kg), VolumetricWeight, DeclaredValue, PricingItemTypeId?, CubicFeet?, Quantity?. **Parcel has no status and no TenantId.**

`ShipmentStatus` (`D/Enums/Shipments/ShipmentStatus.cs`), verbatim: Draft=1, Created=2, ReadyForDispatch=3, InTransit=4, Delivered=5, Cancelled=6.

`ShippingTransactionStatus`: Open=1, Closed=2, Cancelled=3. This enum is defined but no entity uses it.

**The real state machine is much smaller than the enum.**

- `CreateShipmentUseCase` creates shipments directly as `Created` (`UC/Shipments/CreateShipment/CreateShipmentUseCase.cs`, `status: ShipmentStatus.Created`).
- The only other transition is `Cancel()`.
- Nothing in the code ever sets ReadyForDispatch, InTransit or Delivered. A grep for `ShipmentStatus.(ReadyForDispatch|InTransit|Delivered)` finds only read-side filters in `I/Repositories/Dashboard/OperationsDashboardRepository.cs` and `I/Repositories/Customers/CustomerDashboardReader.cs`.
- Real progress lives only in `TrackingEvents` rows.

Shipment flow as built:

1. Create the shipment. A draft invoice is created at the same time.
2. Add parcels (`CreateParcel` for air, `CreateSeaParcel` for sea). Each parcel triggers an invoice recalculation. When the last declared parcel is entered on a non-consolidated shipment, the invoice is auto-issued.
3. Payment. The full balance must be paid in one payment session. Alternatively, staff mark the invoice "Collect".
4. "Release" (`A/Services/ShipmentOperationalReleaseService.cs`) assigns the tracking number and parcel codes.
5. Staff scan parcel codes into bundles.

`ShippingServices`:

- `PlatformShippingService` (platform-wide): Code, Name, TransportType (Air=1, Sea=2, Ground=3).
- `TenantShippingServiceSetup` (per tenant): InvoiceLabel, FlatFee, ExpectedDeliveryBusinessDays, IsActive.
- `ShipmentServiceAvailabilityValidator` (`A/Services/ShippingServices/...`) checks that a pricing profile and rates exist for the destination.

### 2.4 Consolidation, manifests, export documents

`Bundle` (`D/Entities/Logistics/Bundle.cs`) fields:

- BundleNumber (`BND-yyyyMMddHHmmss`, **globally unique index**), Barcode
- DestinationWarehouseId, DestinationCountry, DestinationWarehouseName, ServiceTypeId
- ParcelCount, TotalParcelActualWeight, TotalParcelChargeableWeight
- bundle physical metrics (weight and dimensions)
- `BundleParcels`

`BundleStatus`, verbatim: Open=0, Closed=1, ReadyForDispatch=2, Dispatched=3, Cancelled=4. Transitions are Open → Closed → ReadyForDispatch → Dispatched. Nothing reaches Cancelled.

`ShippersLetterOfInstruction` (SLI): full export form with shipper/consignee, charges, declared value for customs, federal tax id, nature of goods, required-document flags (AWB, commercial invoice, OFAC, SED, packing list) and linked bundles. Statuses: Draft=1, Finalized=2, Void=3. A PDF is generated.

Documents (QuestPDF, `I/Documents/**`): invoice, payment receipt, invoice listing report, shipment shipping label, parcel code label, parcel operational label, shipment code label, bundle label, bundle manifest, SLI.

### 2.5 Dispatch

- `Dispatch`: DispatchNumber (`DSP-yyyy-000001` via NumberSequencer), OriginWarehouseId, Status, CutoffDateTime, Legs, DispatchBundles.
- `DispatchLeg`: Type, From/To (free text), scheduled/actual times, DriverName, VehicleIdentifier, Carrier, ReferenceNumber, Status.
- `DispatchStatus`: Planned=0, Ready=1, InTransit=2, Completed=3, Cancelled=4
- `DispatchLegStatus`: Planned=0, Departed=1, Arrived=2, Delayed=3, Cancelled=4
- `DispatchLegType`: Ground=0, Air=1, Sea=2

Transitions: Planned → Ready (needs at least one bundle and one leg) → InTransit (all bundles ReadyForDispatch; bundles become Dispatched) → Completed (all legs Arrived).

There are no use cases for cancel dispatch or mark leg delayed. **Dispatch events are recorded on the Dispatch and Bundle subjects as non-public events. They never reach the shipment, the parcel or the customer** (`UC/Dispatching/StartDispatch/StartDispatchUseCase.cs`, `UC/Dispatching/CompleteDispatch/CompleteDispatchUseCase.cs`).

### 2.6 Delivery

There is no delivery module, no proof of delivery, no pickup signature and no last-mile driver flow. "ArrivedAtDestinationWarehouse / ReadyForPickup / OutForDelivery / Delivered" exist only as tracking-event codes that staff enter by hand (`W/Components/Pages/OperationControl/Tracking/Parts/ManualTrackingEventPanel.razor`, `.../OperationalEvents/Parts/OperationalEventEntryForm.razor`).

### 2.7 Tracking events (the de-facto milestone stream)

`TrackingEvent` (`D/Entities/Tracking/TrackingEvent.cs`): SubjectType, SubjectId, WarehouseId?, TenantUserId?, Source, EventCode, Title, Description, OperationalNotes, EventDateTimeUtc, IsPublicVisible.

Verbatim enums:

- `TrackingEventCode`:
  - ShipmentCreated=1, ParcelAdded=2, ReceivedAtWarehouse=3
  - InspectionStarted=4, InspectionPassed=5, InspectionFailed=6
  - PaymentReceived=7, ReadyForDispatch=8, Manifested=9, Dispatched=10, InTransit=11
  - ArrivedAtDestinationCountry=12, EnteredCustoms=13, CustomsCleared=14, ArrivedAtDestinationWarehouse=15
  - ReadyForPickup=16, OutForDelivery=17, Delivered=18
  - HoldPlaced=19, HoldReleased=20, Delayed=21, ReturnedToSender=22, Exception=23, OperationalNote=24
- `TrackingSubjectType`: Shipment=1, Parcel=2, Bundle=3, Dispatch=4, DispatchLeg=5
- `TrackingEventSource`: System=1, Staff=2
- `HoldReasonType` (persisted through `TrackingEvent.Title`):
  - None=0, Documentation=1, Payment=2, Inspection=3, Customs=4, CustomerRequest=5, OperationalReview=6, SecurityReview=7
  - Damage=8, Routing=9, Transportation=10, ManifestReview=11, DispatchReview=12, CapacityIssue=13, WeatherDelay=14, CarrierIssue=15, Other=99

Only two system events are public: ShipmentCreated (`CreateShipmentUseCase`) and PaymentReceived/release (`RegisterInvoicePaymentUseCase`). A matched reception is also public. The shipment list computes a "status age" from the latest lifecycle event (`I/Repositories/Shipments/ShipmentRepository.cs`, `GetLatestShipmentLifecycleDatesAsync`).

### 2.8 Invoicing, charges and pricing

- `CustomerInvoice` (`D/Entities/Billing/CustomerInvoice.cs`): one per shipment (unique TenantId+ShipmentId). Fields: InvoiceNumber (`INV-yyyy-000001`), SubTotal, TaxAmount, TotalAmount, AmountPaid, BalanceDue, DueDateUtc, Status. Lines hold Items, and items carry ItemType, ChargeableBasis, PriceRule, Charge, Total and ParcelId.
- `CustomerInvoiceStatus`: Draft=0, Issued=1, Collect=2, Paid=3, Cancelled=4. Draft → Issued → (Collect) → Paid. Cancel works from Draft only. There is no void, no credit note and no refund of issued or paid invoices.
- `CustomerInvoiceLineItemType`: Service=1, Transport=2, Consolidation=3, Surcharge=4, Insurance=5
- `CustomerPaymentMethod`: Cash=0, Card=1, BankTransfer=2, MobileMoney=3, Voucher=4, Credit=5, Zelle=6
- Payments: a `CustomerPaymentSession` (receipt `REC-yyyy-000001`) holds several `CustomerPayment` lines. The session must equal the full balance (`UC/Billing/RegisterInvoicePayment/RegisterInvoicePaymentUseCase.cs`). The Credit method posts a debit to the customer credit ledger.

Pricing model (`D/Entities/Pricing/*`; `PricingCalculationType`: FlatAmount=1, RatePerWeightUnit=2, RatePerVolumeUnit=3, RatePerQuantityUnit=4, Percentage=5; `PricingProfileType`: Air=1, Sea=2):

- **PricingProfile**: one active profile per TenantShippingServiceSetup, Air or Sea, plus a `ServiceCharge` (a flat per-shipment "service premium" line).
- **Air** (`A/Services/Pricing/AirPricingResolver.cs`):
  - chargeable weight = max(actual kg, volumetric).
  - Pick the `AirPricingTier` for profile + destination warehouse + weight band (WeightFrom/WeightTo) + effective dates.
  - Amount is Flat or Rate × chargeable weight, with an optional MinimumCharge.
- **Sea, "by barrel / by item"** (`A/Services/Pricing/SeaPricingResolver.cs`): pick the `SeaPricingRule` for profile + destination + `PricingItemType` (tenant-defined, e.g. barrel, box, TV). Amount is Flat, Rate × CubicFeet, or Rate × Quantity.
- **Operational charges ("extra fees")** (`A/Services/Pricing/PricingOperationalChargeResolver.cs`): `OperationalChargeDefinition` (name, required flag) plus `OperationalChargeRule` per profile + warehouse. Flat or Percentage of the transportation total, with an optional MinimumAmount. A charge applies when it is required, or when the shipment has ChargeConsolidation and the definition **name equals "Consolidation"**.
- **Insurance** (`UC/Billing/RecalculateInvoice/Composition/InsuranceChargeResolver.cs`): `InsuranceProvider` + `InsuranceTier` bands on declared value. FlatFee or DeclaredValue × Percentage, per parcel.
- **Taxes and government fees: none.** `RecalculateInvoiceUseCase` always passes `taxAmount: 0m` (`UC/Billing/RecalculateInvoice/RecalculateInvoiceUseCase.cs`). There is no duties, customs or government-fee concept, and no currency field ("$" is hard-coded in line descriptions in `InvoiceComposer.cs`).
- Invoice composition order (`UC/Billing/RecalculateInvoice/Composition/InvoiceComposer.cs`): Service Charge, then one Transport line per parcel, then Insurance, then operational charges.
- Legacy and dead pricing code: `ConsolidationSetting` + `ConsolidationResolver` are registered in DI but unused. `TenantShippingServiceSetup.FlatFee` is never used in invoice math.

### 2.9 Staff, RBAC, devices

- `TenantUser` (+ `TenantUserAccount`): WarehouseId?, AuthorityLevel, RoleId, JobTitleId, EmployeeNumber.
- `TenantUserAuthorityLevel`: Standard=0, Manager=1, Administrator=2, Owner=3
- `TenantUserAccountStatus`: PendingSetup=0, Active=1, Suspended=2, Deactivated=3, Locked=4
- `TenantUserSetupTokenStatus`: Pending=0, Used=1, Expired=2, Revoked=3
- `PermissionScopeType`: TenantWide=0, Regional=1, WarehouseScoped=2
- Roles and permissions are tenant-defined (`TenantStaffRole`, `TenantStaffRolePermission`, catalog in `A/Services/Security/OperationsPermissions.cs`).
- Device licensing (staff can only work from a licensed device) is enforced through Platform API calls and `W/Middleware/PortalEntryMiddleware.cs` cookies.
- `SecurityActionThrottle` provides throttling for login, reset and registration.

### 2.10 Numbering

`NumberSequence` (TenantId, SequenceType, Year, LastSequenceNumber) with `A/Services/Numbering/NumberSequencer.cs` produces `{PREFIX}-{yyyy}-{000000}`. It is used for Invoice, Receipt, Dispatch and SLI. Shipment, Customer, Bundle and Reception codes are produced by separate ad-hoc generators (see section 4).

---

## 3. Data

- **Single shared SQL Server database (`VixlyDb`, schema `dbo`) for the Platform and Operations planes.**
  - `SqlScripts/PurgeTenantData_Production.sql` deletes Operations tables and Platform tables (`Tenants`, `TenantSubscriptions`, `Invoices`, `DeviceLicenses` and so on) in one transaction.
  - Local default connection: `W/appsettings.json` (Trusted_Connection, no secret). The secret values in all appsettings files are empty. Good.
- **Multi-tenancy is a shared schema with a `TenantId` column on 36 root entities. There are no EF global query filters** (no `HasQueryFilter` anywhere). Every repository method must remember to add `TenantId == x`.
- The tenant comes from the auth cookie claim `tenant_id` (`W/Services/Portal/PortalBlazorUserContextAccessor.cs`).
- Child tables with **no TenantId**:
  - Parcels, BundleParcels
  - CustomerInvoiceLines, CustomerInvoiceLineItems
  - DispatchLegs, DispatchBundles, ShippersLetterOfInstructionBundles
  - AirPricingTiers, SeaPricingRules
  - CustomerAccounts and the customer token tables
  - TenantUserAccounts, TenantPermissions (global catalog)
- Tables (EF `ToTable`):
  - Tenant users and RBAC: TenantUsers, TenantUserAccounts, TenantUserSetupTokens, TenantUserPasswordResetTokens, TenantStaffRoles, TenantStaffJobTitles, TenantPermissions, TenantStaffRolePermissions
  - Customers: Customers, CustomerAccounts, CustomerAccountSetupTokens, CustomerEmailVerificationTokens, CustomerPasswordResetTokens, CustomerMailboxItems, TenantCustomerMailboxes, CustomerCreditAccounts, CustomerCreditLedgerEntries
  - Logistics: Warehouses, Shipments, Parcels, Bundles, BundleParcels, WarehouseReceptions, Dispatches, DispatchLegs, DispatchBundles, ShippersLettersOfInstruction(+Bundles)
  - Pricing and services: PlatformShippingServices, TenantShippingServiceSetups, PricingProfiles, PricingItemTypes, AirPricingTiers, SeaPricingRules, OperationalChargeDefinitions, OperationalChargeRules, InsuranceProviders, InsuranceTiers, ConsolidationSettings
  - Billing: CustomerInvoices, CustomerInvoiceLines, CustomerInvoiceLineItems, CustomerPayments, CustomerPaymentSessions
  - Tenant settings and misc: NumberSequences, TrackingEvents, SecurityActionThrottles, TenantSystemPreferences, TenantPaymentSettings, TenantContactPhoneNumbers, TenantOperationalProvisioningStates
- Schema management: 67 EF migrations (`I/Persistence/Migrations`) plus about 20 hand-run SQL scripts in `OP/`. Examples: `RemoveLegacyPricingAndAdditionalCharges_Production.sql`, `SyncPaymentSessionModelSnapshot.sql`, `rbac-foundation_old.sql`. **Drift between the EF model and production is likely.** Script the real production schema before designing the migration; do not rely on the model snapshot.

### Data-quality and migration concerns for a big-bang weekend cutover

1. **Shipment.Status cannot be trusted.** Almost every row will be `Created` or `Cancelled`. V4 milestones have to be derived from `TrackingEvents` (latest event per shipment, plus bundle/dispatch membership through BundleParcels → Bundles → DispatchBundles → Dispatches/Legs). Build and test that derivation query before the weekend.
2. **Mailbox items are stuck at `InTransit`.** `MarkReceived()` and `MarkCompleted()` are never called. Only `LinkShipment` is used (`UC/Shipments/CreateCustomerShipment/...`). Derive the real state from ShipmentId and reception matches.
3. **Orphan receptions are logged with the wrong subject.** `ReceiveParcelByCarrierTrackingUseCase` writes `SubjectType=Shipment` with `SubjectId=reception.Id` for orphans. These rows point at non-existent shipments.
4. **Mixed weight units in stored data.**
   - `Parcel.Weight` is stored in kg (the UI converts from lb, `W/.../ParcelEditor.razor` `ConvertWeightToKilograms`).
   - `VolumetricWeight` uses L×W×H/166, which is the inch→lb divisor (`UC/Shipments/CreateParcel/CreateParcelUseCase.cs` const `VolumetricDivisor = 166m`, duplicated in `ParcelEditor.razor`).
   - Chargeable weight = max(kg, lb-volumetric). The units of historical invoices and parcels are ambiguous. **(verify)**
   - The tenant setting `TenantSystemPreferences.VolumetricDivisor` (default 5000) is ignored.
5. **Percentages are stored on different scales.** Insurance tiers store a fraction (UI shows `Percentage * 100`), while operational charges store whole percent (`/100m`). Normalize on import.
6. **`Shipment.TenantUserId = Guid.Empty`** on customer-portal shipments. `CreateCustomerShipmentUseCase` passes `TenantUserId = Guid.Empty`, and the config marks the column required.
7. **Codes are not uniform.**
   - Different formats: `SHP-000001` (no year), `CUST-0001`, `INV-2026-000001`, `WR-yyyyMMddHHmmssfff`, `BND-yyyyMMddHHmmss`.
   - `Bundles.BundleNumber` and `WarehouseReceptions.ReceptionNumber` have **global** (not per-tenant) unique indexes.
   - Keep the old codes as `LegacyCode`. Customers know their casillero code and tracking numbers.
8. **Parcels can sit in more than one bundle.** `BundleParcels` has no unique index on ParcelId (`I/Persistence/Configurations/Warehouses/BundleParcelConfiguration.cs`). Check for duplicates.
9. **Tenant ownership of child rows.** Child rows without TenantId need it inferred from their parent. Also check for NULL TenantIds left over from `20260415004212_AddTenantIdNullableToOperationsTables` / `Backfill...`.
10. **Hold reasons are stored as strings** in `TrackingEvent.Title`.
11. **Warehouse.Status is free text.**
12. **Invoices hold no tax or currency.**
13. **Unused or legacy tables:** ConsolidationSettings (dead) and probably leftover columns from removed pricing.
14. **Dates are all UTC with no tenant time zone anywhere.** A grep for `TimeZoneInfo` finds 0 hits. "Today" counters use UTC day boundaries.
15. **Platform data is in the same DB.** V4 must decide whether to split the databases at cutover.

---

## 4. Weaknesses and bugs worth fixing in V4

### Correctness and bugs

1. **Sea invoices cannot be composed. (verify; high)**
   - `InvoiceComposer` builds every parcel pricing request with `PricingItemTypeId = null, CubicFeet = null, Quantity = 1` (`UC/Billing/RecalculateInvoice/Composition/InvoiceComposer.cs`, about line 229).
   - `SeaPricingResolver` throws "PricingItemTypeId is required" when it gets that request.
   - `CreateSeaParcelUseCase` saves the parcel and then calls the recalculation, so the parcel persists while the invoice goes stale or the call errors.
   - `GetShipmentFinancialSummaryUseCase` passes the sea inputs correctly, so the preview and the invoice disagree. This is the duplicate-composition problem already logged as TD-0001 (`OP/TechnicalDebtRegister.md`).
2. **The air chargeable weight mixes kg and lb** (see 3.4). The tenant's `VolumetricDivisor` is ignored.
3. **`CloseShippingTransactionUseCase` is dead and broken.**
   - It requires `Status == Draft`, but shipments are created as `Created`.
   - It recalculates again after `Issue()`, which throws "Only draft invoices can be recalculated" after the commit.
   - No page uses it. (`UC/Shipments/CloseShippingTransaction/CloseShippingTransactionUseCase.cs`)
4. **Shipment status never advances** (section 2.3). The customer portal shows `shipment.Status.ToString()` (`UC/Shipments/GetCustomerShipments/GetCustomerShipmentsUseCase.cs`), so customers see "Created" until delivery.
5. **Dispatch progress never reaches shipments or customers.** Dispatch and Bundle events are private and are not linked to shipment subjects.
6. **Receiving ignores customer pre-alerts.**
   - It matches only `Shipment.TrackingNumber`, which is the Vixly-generated number. The carrier's number (`CustomerMailboxItem.ExternalTrackingNumber`) is never matched.
   - A casillero package normally arrives before any shipment exists, so most receptions in that flow will be orphans. **(verify with real data)**
7. **Cancelling a shipment leaves its mailbox items linked.** The customer then cannot reuse them ("already linked") (`UC/Shipments/CancelShippingTransaction/...` has no mailbox handling).
8. **Holds do not block anything.** `TrackingEventBlockerEvaluator` (`A/Services/Tracking/TrackingEventBlockerEvaluator.cs`) is not referenced anywhere. Bundling (`UC/Warehouses/Bundles/ScanParcelToBundle/...`) checks neither holds nor payment, only parcel code, destination country and service.
9. **The `AllowCollectInvoice` preference is not enforced.** `MarkInvoiceAsCollectUseCase` only checks permission (`UC/Billing/MarkInvoiceAsCollect/...`, `W/.../Billing/Payments/PaymentsWorkspace.razor`).
10. **No partial payments, refunds, void or credit notes** after issue. Only a Draft invoice can be cancelled, so a paid shipment cannot be cancelled (`D/Entities/Billing/CustomerInvoice.cs`).
11. **Consolidation fee is matched by magic string.** It applies only when a definition is literally named "Consolidation" (`PricingOperationalChargeResolver.cs`, `InvoiceComposer.ResolveOperationalChargeItemType`). The customer portal passes `IsConsolidated`/`ChargeConsolidation` from the form (`W/Components/Pages/Customers/CustomerPortal/Shipments/ShipmentsWorkspace.razor`), so a customer may be able to get consolidation without the fee. **(verify)**
12. **UI typo:** the insurance tier percentage renders as `"@(tier.Percentage.Value * 100)m%"`, which prints a literal "m" (`W/.../CustomerShipments/Parts/ShipmentEditor.razor` line 349).
13. **Config/entity mismatch:** `Shipment.TenantUserId` is nullable in the entity but `IsRequired()` in `I/Persistence/Configurations/Shipments/ShipmentConfiguration.cs`.

### Concurrency, numbering, transactions

14. **Shipment and customer codes are generated in an O(n) loop.**
    - The code counts from 1 and queries the DB for each candidate until it finds a free code (`CreateShipmentUseCase.GenerateShipmentCodeAsync`, `RegisterCustomerUseCase.GenerateCustomerCodeAsync`). With 50k shipments that is 50k queries per create.
    - Two concurrent creates collide on the unique index.
    - A `NumberSequencer` exists but is not used for these codes.
15. **`NumberSequence` has no concurrency token.** Only `TenantPaymentSettings.Revision` has one (`I/Persistence/Configurations/**`, grep `IsConcurrencyToken`). Concurrent invoices or receipts can read the same LastSequenceNumber, and the unique index then throws.
16. **Invoices and credit accounts have no rowversion.** Two cashiers can pay the same invoice, or post to the same credit balance, at the same time.
17. **Timestamp and random identifiers.**
    - Bundle and reception numbers come from timestamps (`UC/Warehouses/Bundles/CreateBundles/CreateBundleUseCase.cs`, `I/Services/Logistics/WarehouseReceptionNumberGenerator.cs`). They collide under load, across tenants too, because the indexes are global.
    - Tracking numbers use a shared static `System.Random` (not thread-safe), with no retry on collision (`A/Services/Tracking/TrackingNumberGenerator.cs`).
18. **Multi-step writes are not atomic.** The shipment is saved first, then the invoice is created and recalculated in separate SaveChanges (`CreateShipmentUseCase`). The parcel is saved before the recalculation (`CreateSeaParcelUseCase`). Only two places use explicit transactions in the whole plane.

### Security and tenant isolation

19. **No global tenant filter.** Several repository methods take only an id:
    - `CustomerInvoiceRepository.GetByIdAsync(id)` and `GetByShipmentIdAsync(shipmentId)` (`I/Repositories/Billing/CustomerInvoiceRepository.cs`)
    - `ParcelRepository.GetByIdAsync(parcelId)` and `GetByShipmentIdAsync` (`I/Repositories/Shipments/ParcelRepository.cs`)
    - `InsuranceTierRepository.GetByProviderIdAsync`
    - `RecalculateInvoiceRequest` and `GetInvoiceByShipmentRequest`, which carry no tenant

    Callers usually check the tenant afterwards, but this is defense by convention only. V4 should use EF global query filters plus TenantId on every child table.
20. **Authorization is mostly in the UI layer.**
    - Only 32 of 185 use cases call the permission service themselves (my grep). The rest rely on Blazor workspace checks.
    - Workspaces with no permission check at all: SLI (`W/Components/Pages/ExportDocuments/**`), InboundDeliveries, Reports, CounterSales.
    - Unprotected use cases include pricing writes, shipment, parcel and dispatch writes, `CreateOperationalEvent` and SLI finalize/void.
    - Staff pages have no `[Authorize]` attribute and no fallback policy. Protection comes from `MainLayout` not rendering `@Body` when unauthenticated (`W/Components/Layout/MainLayout.razor`). V4 should authorize in the application layer and set a fallback policy.
21. **PII written to stdout.** `CustomerLoginUseCase` prints email, tenant id and password-check results with `Console.WriteLine` (`UC/Authentication/CustomerLogin/CustomerLoginUseCase.cs`, 7 lines).
22. **Data-protection keys** live in `W/App_Data/DataProtectionKeys` on disk, with no `ProtectKeysWith*` (`W/Program.cs`, about line 156). Do not let these into source control or backups.
23. **The tenant claim is not cross-checked against the request host or portal.** It relies on host-only cookies, which is acceptable for host-per-tenant hosting but fragile.

### Performance

24. **Whole-tenant loads in memory.**
    - `GetCustomersUseCase` loads all customers and all shipments of the tenant to count active shipments (`UC/Customers/GetCustomers/GetCustomersUseCase.cs`).
    - `GetCustomerDetailsUseCase` loads all tenant shipments to count one customer's (`UC/Customers/GetCustomerDetails/...`).
25. **N+1 queries in pricing.**
    - For every parcel, `AirPricingResolver` re-reads the profile, then runs the whole operational-charge resolution and discards the result.
    - The charge resolver reads each definition with a separate query.
    - `StartDispatch` loads bundles one by one.
26. The shipment list filters on `x.Status.ToString() == status`, a non-sargable enum-to-string comparison (`I/Repositories/Shipments/ShipmentRepository.cs`, about line 219). Only 4 repositories page their results.

### Hard-coded values and duplication

27. **Hard-coded values:**
    - Divisor 166 (two places), lb factor 2.20462 (four places), "$" currency
    - Tax 0, "Consolidation" name
    - `SHP-`/`CUST-` formats, 2-day cookie MaxAge
28. **Financial totals are computed in at least four places:** InvoiceComposer, GetShipmentFinancialSummary, PreviewParcelFinancialSummary, PreviewShipmentFinancialSummary (TD-0001).
29. **Dead code:**
    - `ConsolidationSetting` and its CRUD use cases, `ConsolidationResolver`
    - `ShippingTransactionStatus`
    - `CloseShippingTransactionUseCase`, `TrackingEventBlockerEvaluator`
    - Number sequence types Shipment, Bundle, WarehouseReception, CommercialInvoice, MasterBillOfLading and AirWaybill (defined but unused)
    - The empty `D/Enums/Warehouses/Warehouse.cs` class
30. **Use cases without a UI:** no page for creating insurance providers or tiers, and `CreateOperationalEvent` is never injected.

### Tests and UX gaps

31. **Tests:** about 30 tests, none covering pricing, invoice composition, shipment or parcel creation, bundling, dispatch or receiving (`OP/tests/**`). The repository cleanup report states "15/15 pass" (`Docs/repository-cleanup/007-final-report.md`).
32. **UX gaps:**
    - No customer-visible milestones
    - No notifications (package received, invoice ready, ready for pickup)
    - No delivery, POD or pickup flow
    - Reports and Counter Sales are stubs
    - No delivery-address model
    - No multi-currency and no taxes or duties

---

## 5. Strengths and features V4 must keep (parity list)

- **Customer portal**
  - Self-registration with email verification and password reset
  - Casillero code and mailbox address
  - Pre-alerts with carrier, tracking, value, category and expected count
  - Create a shipment from selected pre-alerts
  - View shipments, parcels and invoices; Zelle payment instructions with QR
  - Credit-account view and profile editing
- **Staff shipment entry**
  - Air parcels (dimensions, weight in kg or lb by tenant preference, declared value)
  - Sea parcels by item type (barrel, box and so on) with cubic feet or quantity
  - Live financial preview
- **Pricing engine**
  - Per service-setup profiles (Air/Sea) with a service charge
  - Air weight tiers per destination with minimum charge and effective dates
  - Sea item-type rules per destination
  - Operational charges (flat or %, minimum, required or optional) per profile + warehouse
  - Insurance providers and declared-value tiers
  - Consolidation fee
- **Invoicing and payments**
  - Invoice auto-created, auto-recalculated and auto-issued on the final parcel
  - Multi-tender payment sessions (cash, card, transfer, mobile money, voucher, credit, Zelle) with receipt numbers and PDFs
  - "Collect" (pay on delivery) releasing the shipment
  - Customer credit accounts with limit, ledger, suspend/close and credit payments
  - Tenant payment settings: cash, office/portal Zelle, card metadata without secrets
- **Payment-gated release:** tracking number and parcel codes are assigned only when the invoice is paid or marked Collect. Bundling requires parcel codes, so unpaid freight cannot be bundled.
- **Warehouse operations**
  - Scan-based reception with orphan stack and live counters
  - Parcel queue scanning
  - Bundles with destination and service validation, physical metrics, labels and manifest PDF
  - Dispatch with multi-leg (ground/air/sea) plan, departure/arrival and cutoff
- **Export compliance:** Shipper's Letter of Instruction with finalize/void and PDF.
- **Tracking stream:** system plus staff events, public/private flag, hold/release with controlled reasons, timeline UI, status-age column.
- **Labels and documents:** shipping label, shipment code, parcel code, operational label, bundle label, invoice, receipt, invoice listing report (QuestPDF).
- **Security**
  - Tenant RBAC with roles, permissions and authority levels (Owner protected), scoped by warehouse or region
  - Staff onboarding via setup tokens, account suspend/lock/deactivate
  - Throttling of login, reset and registration; idle-session timeout
  - Device licensing; HSTS and secure cookies; Cloudflare scheme handling
- **Tenant settings:** weight unit, collect toggle, landing page and branding, contact numbers, company logo, mailbox warehouse, shipping-service setup (label, expected business days).
- **Numbering:** per-tenant, per-year sequences (INV/REC/DSP/SLI). Extend this to every code in V4.

---

## 6. Proposed V4 shipment/package milestone list

One ordered list, used on both the shipment and the parcel. A shipment's milestone is the lowest milestone among its active parcels; show "partial" when parcels differ. Holds and delays are flags that sit on top of a milestone, not milestones.

| # | V4 milestone | Meaning |
|---|---|---|
| 0 | PreAlerted | Customer declared an incoming package (casillero) |
| 1 | ReceivedAtOrigin | Physically scanned in at the origin warehouse |
| 2 | Booked | Shipment and parcels captured, charges calculated (invoice draft) |
| 3 | AwaitingPayment | Invoice issued, not paid |
| 4 | Released | Paid or approved on Collect/credit; tracking and parcel codes assigned |
| 5 | Consolidated | Scanned into a bundle |
| 6 | Manifested | Bundle closed, ready for dispatch, attached to a dispatch |
| 7 | DepartedOrigin | Dispatch started |
| 8 | InTransit | Leg departed / moving |
| 9 | ArrivedDestinationCountry | |
| 10 | InCustoms | Entered customs |
| 11 | CustomsCleared | |
| 12 | ArrivedDestinationWarehouse | |
| 13 | ReadyForPickup | |
| 14 | OutForDelivery | |
| 15 | Delivered | Terminal (with POD) |
| T | Cancelled | Terminal |
| T | ReturnedToSender | Terminal |

Flags: OnHold(reason = V3 HoldReasonType values), Delayed, Exception, InspectionInProgress/Failed.

Note on order: in the casillero flow 0→1→2; in the counter or drop-off flow 2→1, or 1 and 2 happen together. Milestones 1 and 2 should therefore both be allowed in either order, and "next" validation should start at 3.

### Mapping from V3

| V3 value | V4 milestone / flag |
|---|---|
| ShipmentStatus.Draft | Booked |
| ShipmentStatus.Created | Booked; refine from invoice/events: Issued → AwaitingPayment, Paid/Collect → Released |
| ShipmentStatus.ReadyForDispatch | Manifested |
| ShipmentStatus.InTransit | InTransit |
| ShipmentStatus.Delivered | Delivered |
| ShipmentStatus.Cancelled | Cancelled |
| CustomerMailboxItemStatus.InTransit | PreAlerted |
| CustomerMailboxItemStatus.Received | ReceivedAtOrigin |
| CustomerMailboxItemStatus.Completed | Booked (or the linked shipment's milestone) |
| CustomerMailboxItemStatus.Cancelled | Cancelled (pre-alert only) |
| WarehouseReceptionStatus.Matched | ReceivedAtOrigin |
| WarehouseReceptionStatus.Orphan | ReceivedAtOrigin + Exception flag "unidentified" |
| CustomerInvoiceStatus.Draft | Booked |
| CustomerInvoiceStatus.Issued | AwaitingPayment |
| CustomerInvoiceStatus.Collect / Paid | Released |
| CustomerInvoiceStatus.Cancelled | Cancelled (if the shipment is cancelled) |
| BundleStatus.Open | Consolidated |
| BundleStatus.Closed / ReadyForDispatch | Manifested |
| BundleStatus.Dispatched | DepartedOrigin |
| BundleStatus.Cancelled | back to Released |
| DispatchStatus.Planned / Ready | Manifested |
| DispatchStatus.InTransit | DepartedOrigin → InTransit |
| DispatchStatus.Completed | ArrivedDestinationWarehouse (assumed) |
| DispatchStatus.Cancelled | back to Released / Consolidated |
| DispatchLegStatus.Planned | Manifested |
| DispatchLegStatus.Departed | InTransit |
| DispatchLegStatus.Arrived (last leg) | ArrivedDestinationCountry or ArrivedDestinationWarehouse |
| DispatchLegStatus.Delayed | flag Delayed |
| DispatchLegStatus.Cancelled | flag Exception |
| TrackingEventCode.ShipmentCreated | Booked |
| TrackingEventCode.ParcelAdded | Booked |
| TrackingEventCode.ReceivedAtWarehouse | ReceivedAtOrigin |
| TrackingEventCode.InspectionStarted | flag InspectionInProgress |
| TrackingEventCode.InspectionPassed | clears the inspection flag |
| TrackingEventCode.InspectionFailed | flag Exception |
| TrackingEventCode.PaymentReceived | Released |
| TrackingEventCode.ReadyForDispatch | Manifested |
| TrackingEventCode.Manifested | Manifested |
| TrackingEventCode.Dispatched | DepartedOrigin |
| TrackingEventCode.InTransit | InTransit |
| TrackingEventCode.ArrivedAtDestinationCountry | ArrivedDestinationCountry |
| TrackingEventCode.EnteredCustoms | InCustoms |
| TrackingEventCode.CustomsCleared | CustomsCleared |
| TrackingEventCode.ArrivedAtDestinationWarehouse | ArrivedDestinationWarehouse |
| TrackingEventCode.ReadyForPickup | ReadyForPickup |
| TrackingEventCode.OutForDelivery | OutForDelivery |
| TrackingEventCode.Delivered | Delivered (but on a Dispatch subject it means "dispatch completed": do not map that to shipment Delivered) |
| TrackingEventCode.HoldPlaced / HoldReleased | flag OnHold set / cleared |
| TrackingEventCode.Delayed | flag Delayed |
| TrackingEventCode.ReturnedToSender | ReturnedToSender |
| TrackingEventCode.Exception | flag Exception |
| TrackingEventCode.OperationalNote | note (no milestone) |

### Open questions for the owner

1. Is there home or last-mile delivery, or is everything picked up at the destination warehouse? This decides whether OutForDelivery stays, and whether V4 needs a delivery address and POD.
2. What units are dimensions entered in (inches or cm)? Was "chargeable = max(kg, L×W×H/166)" intentional? Which divisor should V4 use per tenant? Are historical invoices billed on this mix?
3. Have sea (barrel) shipments been invoiced in production? If so, how, given the composer bug in 4.1?
4. Taxes, duties and government fees: which do tenants need on invoices (destination-country duties, fuel surcharge, VAT/GCT/ITBIS)? Which currency or currencies do tenants bill in?
5. Should a pre-alert be matched automatically on receipt by the carrier tracking number? Should receipt create the shipment (true casillero) instead of the shipment being created first?
6. Are partial payments, refunds, void and credit notes needed? Should a paid shipment be cancellable?
7. Should holds block bundling, dispatch or release in V4? Should payment gating stay mandatory, or be a per-tenant setting (the AllowCollectInvoice toggle is currently ignored)?
8. Which events should customers see, and should they be notified by email, SMS or WhatsApp?
9. Should old codes (SHP/CUST/INV/BND/WR) be preserved verbatim after migration? Customers and labels reference them.
10. Should the Platform and Operations data be split into separate databases at cutover, or stay shared?
11. Can the hand-run SQL scripts in `OP/*.sql` be retired? Is there a reliable production schema export to migrate from?
12. Consolidation: who decides whether the fee is charged, staff or customer? Should it stay a named operational charge, or become a first-class setting?
13. Reports and Counter Sales are stubs. Which reports do tenants actually use today, maybe exported by hand?
