# UI styling inventory

The shared list of every Operations, customer-portal and Control screen, and where each stands for styling. **The backend team** adds a row when it builds or changes a screen. **The Frontend team** updates it as it styles. **The owner** approves. Read it together with the [Operations UI/UX brief](OPERATIONS-UI-UX-BRIEF.md).

## How to use this file

**Status values** (one per row):

| Status | Meaning | Who sets it |
|---|---|---|
| **Needs styling** | Built by the backend team, never styled | Backend, when the screen is built |
| **Restyle** | Styled before the brief of 2026-10-20; must be redone to the brief | Backend, once |
| **On hold** | The screen is being redesigned with the owner: **do not style yet** | Backend |
| **In progress** | The FE team is working on it | FE |
| **Styled** | Done by the FE team; waiting for the owner's review | FE |
| **Approved** | The owner reviewed and accepted it | FE, recording the owner's decision |
| **Changed** | Approved before, but its content changed since: check it again | Backend, when it changes an approved screen |

**Rules:**
- **The backend team** adds a row in the same delivery that builds a new screen, with status *Needs styling*. When it changes the content of a styled or approved screen (new fields, new section, a new rule), it sets *Changed* and says what changed in Notes. It never sets *Styled* or *Approved*.
- **The FE team** moves a row to *In progress*, then to *Styled*, with the date and a line on what was done. It sets *Approved* only after the owner's review, naming the date of that review.
- Screens *On hold* are not styled until the backend team moves them to *Needs styling*.
- **Keep one row per screen.** Parts used by many screens (shell, menu, field groups) are in the second table. Always update **Updated** (date) and **By** (Backend or FE).
- This file in the repository is the source; the copy in the Project is refreshed from it.

## Screens

### Staff: workspace and front desk

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Home | `SignIn/WorkspacePage.razor` | `/staff` | On hold | 2026-10-20 | Backend | To become a dashboard of counters (brief, section 3); content being settled with the owner |
| Customer list | `Customers/CustomerListPage.razor` | `/staff/customers` | On hold | 2026-10-20 | Backend | Becomes the front-desk customer hub (brief, 7.1) |
| Customer record | `Customers/CustomerPage.razor` | `/staff/customers/{id}` | On hold | 2026-10-20 | Backend | Same; tabs by rights; no credit account section |
| Customer's incoming packages (part) | `Customers/CustomerIncomingPackages.razor` | inside the customer record | On hold | 2026-10-20 | Backend | Part of the hub |
| Shipments desk | `Shipments/ShipmentsPage.razor` | `/staff/shipments` | On hold | 2026-10-20 | Backend | Counter flow being redesigned (shipment from the customer, parcel count, finalize) |
| Shipment record | `Shipments/ShipmentPage.razor` | `/staff/shipments/{id}` | On hold | 2026-10-20 | Backend | Same |
| Quotes | `Charges/QuotesPage.razor` | `/staff/quotes` | On hold | 2026-10-20 | Backend | Gets a list of past quotes with their status first |
| Tickets | `Tickets/TicketsPage.razor` | `/staff/tickets` | Needs styling | 2026-10-20 | Backend | List anatomy (brief, 5) |
| Ticket | `Tickets/TicketPage.razor` | `/staff/tickets/{id}` | Needs styling | 2026-10-20 | Backend | Record anatomy; messages, lookalike packages, proof |

### Staff: warehouse

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Dock scanning | `Parcels/ScanPage.razor` | `/staff/scan` | Needs styling | 2026-10-20 | Backend | Station anatomy; uses `wwwroot/js/ops-scan.js` (keep its element ids) |
| Receiving (deliveries and receipts) | `Parcels/ReceivingPage.razor` | `/staff/receiving` | Needs styling | 2026-10-20 | Backend | List anatomy |
| Incoming (expected packages) | `Parcels/IncomingPage.razor` | `/staff/parcels/incoming` | Needs styling | 2026-10-20 | Backend | V3 calls it "Inbound deliveries"; name to confirm with the owner |
| Inspection station | `Parcels/InspectionPage.razor` + `InspectionStationFrame`, `InspectionCheckForm`, `InspectionSketch`, `InspectionSummaryPanel` | `/staff/inspection` | Restyle | 2026-10-20 | Backend | Styled by FE on 2026-10-19; drop the sketch and guide text, keep the large scan field |
| Inspection queue | `Inspection/InspectionQueuePage.razor` | `/staff/inspection/queue` | Needs styling | 2026-10-20 | Backend | List anatomy |
| Compliance inspection | `Inspection/InspectionWorkPage.razor` + `InspectionOutcomeForm` | `/staff/inspection/queue/{id}` | Needs styling | 2026-10-20 | Backend | Record anatomy |
| Holds | `Inspection/HoldsPage.razor` + `HoldAnswersCell`, `UnclaimedPanel` | `/staff/holds` | Needs styling | 2026-10-20 | Backend | Tabs: holds, unclaimed |
| Parcels list | `Parcels/ParcelListPage.razor` | `/staff/parcels` | On hold | 2026-10-20 | Backend | To be folded into Inventory (owner, 2026-10-20) |
| Parcel record | `Parcels/ParcelPage.razor` + `ShipmentInstructionsPanel`, `ShippingLabelPanel`, `UndeclaredLabelPanel` | `/staff/parcels/{id}` | Needs styling | 2026-10-20 | Backend | Record anatomy; history, photos, label |
| Labels to print | `Parcels/LabelsToPrintPage.razor` | `/staff/parcels/labels` | Needs styling | 2026-10-20 | Backend | List anatomy |
| Stock counts | `Parcels/StockCountsPage.razor` | `/staff/parcels/counts` | Needs styling | 2026-10-20 | Backend | Part of Inventory later |
| Stock count | `Parcels/StockCountPage.razor` | `/staff/parcels/counts/{id}` | Needs styling | 2026-10-20 | Backend | |
| Storage locations (shelves) | `Parcels/StorageLocationsPage.razor` + `ZoneSelect` | `/staff/locations/warehouse/{id}/storage` | Needs styling | 2026-10-20 | Backend | |
| Label printers | `Parcels/PrintersPage.razor` + `PrinterForm` | `/staff/locations/warehouse/{id}/printers` | Needs styling | 2026-10-20 | Backend | |

### Staff: setup

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Prices | `Charges/ChargesPage.razor` + `ChargesTabs`, `PriceListPanel`, `ExtrasPanel`, `ItemTypesPanel`, `InsurancePlanForm` and field parts | `/staff/charges` | On hold | 2026-10-20 | Backend | Its tabs are the model for other pages, but the pricing engine is being revisited with the owner |
| Insurance | `Insurance/InsurancePage.razor` | `/staff/insurance` | Needs styling | 2026-10-20 | Backend | Slice 5B step 1: policies, covers per mode, liability per mode; use tabs |
| Insurance policy | `Insurance/PolicyPage.razor` | `/staff/insurance/policies/{id}` | Needs styling | 2026-10-20 | Backend | Long form: put it in tabs (cover, limits, duties, documents) |
| Inspection rules | `Inspection/InspectionSetupPage.razor` + `RestrictedItemFields` | `/staff/inspection/setup` | Needs styling | 2026-10-20 | Backend | |
| Locations | `Editor/LocationsPage.razor` | `/staff/locations` | Restyle | 2026-10-20 | Backend | Styled 2026-10-18 |
| Branch | `Editor/BranchFormPage.razor` | `/staff/locations/branch/{id}` | Restyle | 2026-10-20 | Backend | |
| Warehouse | `Editor/WarehouseFormPage.razor` + `DestinationFields` | `/staff/locations/warehouse/{id}` | Restyle | 2026-10-20 | Backend | Destination terms added after the 10-18 styling |
| Contact | `Editor/ContactFormPage.razor` | `/staff/locations/contact/{id}` | Restyle | 2026-10-20 | Backend | |
| Services | `Settings/ShippingServicesPage.razor` | `/staff/services` | Restyle | 2026-10-20 | Backend | |
| Settings | `Settings/SettingsPage.razor` | `/staff/settings` | Restyle | 2026-10-20 | Backend | Units, storage fee, claims settings added since |
| Website editor | `Editor/SiteEditorPage.razor` | `/staff/site` | Restyle | 2026-10-20 | Backend | |

### Staff: administration and account

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Staff | `Staff/StaffListPage.razor` | `/staff/people` | Restyle | 2026-10-20 | Backend | |
| Staff member | `Staff/StaffMemberPage.razor` | `/staff/people/{id}` | Restyle | 2026-10-20 | Backend | |
| Roles | `Staff/RolesPage.razor` | `/staff/roles` | Restyle | 2026-10-20 | Backend | |
| Role | `Staff/RoleFormPage.razor` | `/staff/roles/{id}` | Restyle | 2026-10-20 | Backend | Many permissions: group them in tabs or columns |
| Devices | `Editor/DevicesPage.razor` | `/staff/devices` | Restyle | 2026-10-20 | Backend | |
| Company | `Settings/CompanyPage.razor` + `CompanyTabs` | `/staff/company` | Restyle | 2026-10-20 | Backend | |
| Billing (the company's Vixly plan) | `Settings/BillingPage.razor` | `/staff/billing` | Restyle | 2026-10-20 | Backend | |
| Audit | `Settings/AuditPage.razor` | `/staff/audit` | Restyle | 2026-10-20 | Backend | |
| My account | `Staff/AccountPage.razor` | `/staff/account` | Restyle | 2026-10-20 | Backend | |
| Confirm (step-up) | `Staff/ConfirmPage.razor` | `/staff/confirm` | Restyle | 2026-10-20 | Backend | |
| Terms acceptance | `Editor/LegalPage.razor` | `/staff/legal` | Restyle | 2026-10-20 | Backend | |
| Message page | `Editor/EditorMessagePage.razor` | various | Restyle | 2026-10-20 | Backend | |

### Staff: sign-in and setup (Comfortable density allowed)

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Sign-in | `SignIn/SignInPage.razor` + `AuthShell`, `AccessIllustration` | `/staff/sign-in` | Restyle | 2026-10-20 | Backend | Also the page shown after inactivity: nothing else on it (owner) |
| Second step, enrolment, recovery, recovery codes | `SignIn/VerifyPage`, `EnrolPage`, `RecoveryPage`, `RecoveryCodesPage` | `/staff/sign-in/...` | Restyle | 2026-10-20 | Backend | |
| Forgot and reset password | `SignIn/ForgotPasswordPage`, `ResetPasswordPage` | `/staff/forgot-password`, `/staff/reset-password` | Restyle | 2026-10-20 | Backend | |
| Invitation | `SignIn/InvitationPage.razor` | `/invitation` | Restyle | 2026-10-20 | Backend | |
| Second-step reset | `SignIn/SecondFactorResetPage.razor` | `/account/second-factor-reset` | Restyle | 2026-10-20 | Backend | |
| Devices: enrol, link, waiting | `SignIn/DeviceEnrolPage`, `DeviceLinkPage`, `DeviceWaitingPage` | `/devices/...` | Restyle | 2026-10-20 | Backend | |
| First setup | `SignIn/SetupHomePage.razor` | `/setup` | Restyle | 2026-10-20 | Backend | |
| Access limited | `SignIn/AccessLimitedPage.razor` | `/staff/limited` | Restyle | 2026-10-20 | Backend | |

### Company public site (tenant branded)

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Home, locations, tracking | `Site/SiteHomePage`, `SiteLocationsPage`, `SiteTrackPage` + `SiteShell` | `/{language}`, `/{language}/locations`, `/{language}/track` | Styled | 2026-10-18 | FE | Marketing pages: outside the density rules; owner review pending |
| Not found, not available, unavailable | `Pages/NotFoundPage`, `NotAvailablePage`, `UnavailablePage` | status pages | Styled | 2026-10-18 | FE | |

### Customer portal

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Sign-in, sign-up, verify, forgot and reset password | `Portal/SignInPage`, `SignUpPage`, `VerifyPage`, `ForgotPasswordPage`, `ResetPasswordPage` | `/customer/...` | Styled | 2026-10-18 | FE | Comfortable density; owner review pending |
| Home (mailbox address) | `Portal/HomePage.razor` | `/customer` | Restyle | 2026-10-20 | Backend | Remove repeated titles and decorative sketches |
| Packages and shipments | `Portal/PackagesPage.razor` + `PutInForm`, `TakeOutForm`, `PackageMoveForm` | `/customer/packages` | Restyle | 2026-10-20 | Backend | |
| Package | `Portal/PackagePage.razor` + `HoldAnswerForms` | `/customer/packages/{id}` | Restyle | 2026-10-20 | Backend | |
| Announce a package | `Portal/PreAlertFormPage.razor` + `PackageFields` | `/customer/packages/new` | Restyle | 2026-10-20 | Backend | |
| Shipment | `Portal/ShipmentPage.razor` | `/customer/packages/shipments/{id}` | On hold | 2026-10-20 | Backend | Gets the parcel count the customer can change (owner, 2026-10-20) |
| Create or change a shipment | `Portal/ShipmentFormPage.razor` + `ShipmentDestinationFields`, `InsuranceTermsView` | `/customer/packages/shipments/new` | On hold | 2026-10-20 | Backend | Same |
| Rates calculator | `Portal/RatesPage.razor` + `RateLines` | `/customer/rates` | On hold | 2026-10-20 | Backend | Follows the pricing review |
| Tickets | `Portal/TicketsPage`, `TicketPage`, `TicketFormPage` | `/customer/tickets` | Needs styling | 2026-10-20 | Backend | |
| Profile | `Portal/ProfilePage.razor` + `AddressInputs` | `/customer/profile` | Restyle | 2026-10-20 | Backend | |
| Message page | `Portal/MessagePage.razor` | various | Restyle | 2026-10-20 | Backend | |

### Control console (Platform.Web)

The same rules apply to Control as to Operations, except the sign-in pages, which may keep the Comfortable density. Components are in `src/Platform/Vixly.Platform.Web/Components/Pages`, with their parts in `Features/`.

#### Control: overview

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Home (dashboard) | `Pages/Home.razor` | `/` | Restyle | 2026-10-20 | Backend | Dashboard: counters that open their lists; no hero |
| Approvals | `Pages/ApprovalsPage.razor` | `/approvals` | Restyle | 2026-10-20 | Backend |  |

#### Control: tenants

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Tenants | `Pages/TenantsPage.razor` | `/tenants` | Restyle | 2026-10-20 | Backend |  |
| Tenant | `Pages/TenantPage.razor` | `/tenants/{TenantId:guid}` | Restyle | 2026-10-20 | Backend | Many sections (subscription, finance, licensing, activity, legal, contacts, notes, history): use tabs |
| Registrations | `Pages/RegistrationsPage.razor` | `/tenants/registrations` | Restyle | 2026-10-20 | Backend |  |
| Registration | `Pages/RegistrationPage.razor` | `/tenants/registrations/{RegistrationId:guid}` | Restyle | 2026-10-20 | Backend |  |
| Company change requests | `Pages/TenantChangesPage.razor` | `/tenants/changes` | Restyle | 2026-10-20 | Backend |  |
| Company change request | `Pages/TenantChangePage.razor` | `/tenants/changes/{RequestId:guid}` | Restyle | 2026-10-20 | Backend |  |

#### Control: sales and plans

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Plans | `Pages/PlansPage.razor` | `/plans` | Restyle | 2026-10-20 | Backend |  |
| Plan | `Pages/PlanPage.razor` | `/plans/{PlanId:guid}` | Restyle | 2026-10-20 | Backend | Tabs: versions, translations, approval |
| New plan | `Pages/NewPlanPage.razor` | `/plans/new` | Restyle | 2026-10-20 | Backend |  |
| Subscriptions | `Pages/SubscriptionsPage.razor` | `/subscriptions` | Restyle | 2026-10-20 | Backend |  |
| Leads | `Pages/LeadsPage.razor` | `/leads` | Restyle | 2026-10-20 | Backend |  |
| Lead | `Pages/LeadPage.razor` | `/leads/{LeadId:guid}` | Restyle | 2026-10-20 | Backend |  |
| Offers | `Pages/OffersPage.razor` | `/offers` | Restyle | 2026-10-20 | Backend |  |
| Offer | `Pages/OfferPage.razor` | `/offers/{OfferId:guid}` | Restyle | 2026-10-20 | Backend |  |
| New offer | `Pages/NewOfferPage.razor` | `/offers/new` | Restyle | 2026-10-20 | Backend |  |
| Promotions | `Pages/PromotionsPage.razor` | `/promotions` | Restyle | 2026-10-20 | Backend |  |
| Promotion | `Pages/PromotionPage.razor` | `/promotions/{PromotionId:guid}` | Restyle | 2026-10-20 | Backend |  |
| New promotion | `Pages/NewPromotionPage.razor` | `/promotions/new` | Restyle | 2026-10-20 | Backend |  |
| Sales settings | `Pages/SalesSettingsPage.razor` | `/sales/settings` | Restyle | 2026-10-20 | Backend |  |

#### Control: finance

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Invoices | `Pages/InvoicesPage.razor` | `/invoices` | Restyle | 2026-10-20 | Backend |  |
| Invoice | `Pages/InvoicePage.razor` | `/invoices/{InvoiceId:guid}` | Restyle | 2026-10-20 | Backend | Summary strip, then tabs: lines, payments, credits, history |
| Card payments | `Pages/CardTransactionsPage.razor` | `/payments/transactions` | Restyle | 2026-10-20 | Backend |  |
| Zelle claims | `Pages/ZelleClaimsPage.razor` | `/payments/zelle` | Restyle | 2026-10-20 | Backend |  |
| Zelle claim | `Pages/ZelleClaimPage.razor` | `/payments/zelle/{ClaimId:guid}` | Restyle | 2026-10-20 | Backend |  |
| Collections | `Pages/CollectionsPage.razor` | `/collections` | Restyle | 2026-10-20 | Backend |  |
| Collection case | `Pages/CollectionCasePage.razor` | `/collections/{CaseId:guid}` | Restyle | 2026-10-20 | Backend | Summary strip and steps |
| Collections settings | `Pages/CollectionsSettingsPage.razor` | `/collections/settings` | Restyle | 2026-10-20 | Backend |  |
| Billing settings | `Pages/BillingSettingsPage.razor` | `/billing/settings` | Restyle | 2026-10-20 | Backend |  |
| Tax rates | `Pages/TaxRatesPage.razor` | `/billing/tax-rates` | Restyle | 2026-10-20 | Backend |  |
| Payments settings | `Pages/PaymentsSettingsPage.razor` | `/payments/settings` | Restyle | 2026-10-20 | Backend |  |

#### Control: licensing and legal

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Devices | `Pages/DevicesPage.razor` | `/devices` | Restyle | 2026-10-20 | Backend |  |
| Device | `Pages/DevicePage.razor` | `/devices/{DeviceId:guid}` | Restyle | 2026-10-20 | Backend |  |
| Legal documents | `Pages/LegalDocumentsPage.razor` | `/legal` | Restyle | 2026-10-20 | Backend |  |
| Legal document | `Pages/LegalDocumentPage.razor` | `/legal/{DocumentId:guid}` | Restyle | 2026-10-20 | Backend |  |
| Legal register | `Pages/LegalRegisterPage.razor` | `/legal/register` | Restyle | 2026-10-20 | Backend |  |

#### Control: staff and security

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Users | `Pages/UsersPage.razor` | `/users` | Restyle | 2026-10-20 | Backend |  |
| User | `Pages/UserPage.razor` | `/users/{UserId:guid}` | Restyle | 2026-10-20 | Backend |  |
| Invite user | `Pages/InviteUserPage.razor` | `/users/invite` | Restyle | 2026-10-20 | Backend |  |
| Roles | `Pages/RolesPage.razor` | `/roles` | Restyle | 2026-10-20 | Backend |  |
| Role | `Pages/RolePage.razor` | `/roles/{RoleId:guid}` | Restyle | 2026-10-20 | Backend | Permissions in columns or tabs |
| New role | `Pages/NewRolePage.razor` | `/roles/new` | Restyle | 2026-10-20 | Backend |  |
| Permission holders | `Pages/PermissionHoldersPage.razor` | `/roles/permissions/{Code}` | Restyle | 2026-10-20 | Backend |  |
| Departments | `Pages/DepartmentsPage.razor` | `/departments` | Restyle | 2026-10-20 | Backend |  |
| Department | `Pages/DepartmentPage.razor` | `/departments/{DepartmentId:guid}` | Restyle | 2026-10-20 | Backend |  |
| New department | `Pages/NewDepartmentPage.razor` | `/departments/new` | Restyle | 2026-10-20 | Backend |  |
| Audit | `Pages/AuditPage.razor` | `/audit` | Restyle | 2026-10-20 | Backend |  |
| Audit entry | `Pages/AuditEntryPage.razor` | `/audit/{AuditEntryId:guid}` | Restyle | 2026-10-20 | Backend |  |
| My account | `Pages/MyAccountPage.razor` | `/account` | Restyle | 2026-10-20 | Backend |  |

#### Control: sign-in (Comfortable density allowed)

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Sign-in | `SignIn/SignInPage` + `ControlLoginShell` | `/sign-in` | Styled | 2026-10-20 | FE | Fresh open form and slim header; 24 size/language/theme captures fit without scrolling; keyboard, error state and text contrast checked. Owner review pending |
| Second step, enrolment, recovery, recovery codes, forgot and reset password, invitation | `SignIn/VerifyPage`, `EnrolPage`, `RecoveryPage`, `RecoveryCodesPage`, `ForgotPasswordPage`, `ResetPasswordPage`, `InvitationPage` + `AuthShell` | `/sign-in/...`, `/invitation`, … | Styled | 2026-10-14 | FE | Existing presentation retained; owner review pending |

## Shared parts

| Part | Component | Status | Updated | By | Notes |
|---|---|---|---|---|---|
| Staff shell and top bar | `Editor/EditorShell.razor` | Restyle | 2026-10-20 | Backend | Compact header; no hero; search box in the top bar (brief, 6) |
| Staff menu | `Editor/OperationsNavigation.razor` | Restyle | 2026-10-20 | Backend | Job-based groups once the owner confirms them (brief, 6) |
| Read-only banner | `Editor/AccessBanner.razor` | Restyle | 2026-10-20 | Backend | One slim line |
| Task links | `Editor/OperationsTaskLinks.razor` | Restyle | 2026-10-20 | Backend | |
| Table viewport | `Editor/OperationsTableViewport.razor` | Restyle | 2026-10-20 | Backend | Becomes the list anatomy table (brief, 5) |
| Address, contact, country, status fields | `Editor/AddressFields`, `ContactFields`, `CountrySelect`, `StatusFields` | Restyle | 2026-10-20 | Backend | Field widths (brief, 2.5) |
| Control shell and menu | `Platform.Web/Components/Layout/MainLayout.razor` | Restyle | 2026-10-20 | Backend | Same shell rules as Operations: compact header, no hero, job-based menu |
| Step-up dialog (Control) | `Platform.Web/Features/StepUp/StepUpDialog.razor` | Restyle | 2026-10-20 | Backend | |
| Customer portal shell | `Portal/CustomerShell.razor` + `AccountSketch`, `CustomerAccessStory` | Restyle | 2026-10-20 | Backend | |
| Shared primitives | `Vixly.Shared.UI/Primitives/*` | Restyle | 2026-10-20 | Backend | Compact density tokens (brief, 4); a list-page and a record-page primitive (brief, 9) |

## Log

| Date | By | Change |
|---|---|---|
| 2026-10-20 | Backend | Inventory created from every screen in Operations.Web and Customer.Web; statuses set against the UI/UX brief of 2026-10-20 |
| 2026-10-20 | Backend | Control console (Platform.Web) added: 50 screens to restyle, sign-in styled (owner, 2026-10-20) |
