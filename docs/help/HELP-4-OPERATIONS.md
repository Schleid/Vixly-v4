# Help stage 4: Operations

| | |
|---|---|
| Status | **H4a verification in progress, 2026-10-25**. All 27 guides in en/fr/es and available setup links are built; see [verification](HELP-4A-VERIFICATION.md) for checks and remaining walkthroughs. Owner authorized starting after Public V2, overriding the original HELP-3 gate. Five parts, H4a to H4e, each need their own verification and sign-off. |
| Application | `src/Operations/Vixly.Operations.Web`, staff side at `{company host}/staff` |
| Articles | `src/Operations/Vixly.Operations.Resources/Help/{en,fr,es}/` |
| Help address | `/staff/help` and `/staff/help/{slug}`, signed-in staff and partner agents |
| Brand | The company: `{brandName}`. Never "Vixly". |

## 1. Purpose of this help

Owner sequencing update, 2026-10-25: start Operations Version 1 after Public Version 2, without implying completion or sign-off of the remaining Control or Customer guides. Begin with H4a. Operations Version 1 is text-only; screenshot annotation is currently authorized for Public Version 2 only (ADR-0023 Amendment 1).

Helps a company's people run their logistics business in the workspace, each seeing only the help for the work they do:
- owners and administrators set the company up;
- clerks, inspectors and warehouse workers receive and check packages;
- cashiers take money;
- managers bundle and dispatch;
- partner agents and drivers deliver at destination;
- managers read reports.

## 2. Wiring (built in H4a)

- **Pages:**
  - help home and article pages under `/staff/help`, in the staff layout;
  - same device and access gates as every `/staff` page (partner agents included).
- **Navigation:**
  - *Help* is the last item of the menu (`OperationsNavigation.razor`), visible to everyone signed in;
  - `VxHelpLink` goes in the layout header.
- **Workspace home:** the setup checklist on `/staff` retains each task action and adds reader-filtered help links as its articles are published. H4a covers settings, branch, receiving warehouse and services. Website and staff help links arrive with H4e.
- **Reader rules** (HELP-0 section 6):
  - The permissions of the staff member's grants, at any warehouse, are read from the same source as the menu (`GetOperationsMenu`).
  - Plan-feature conditions come from the plan features already loaded for the company.
  - Partner agents hold only their partner role's permissions.
- **Brand:** `{brandName}` and `{portalAddress}` come from the company's published brand and host.

## 3. Sections and articles

Sections: `getting-started`, `setting-up`, `receiving-and-checking`, `at-the-counter`, `money`, `consolidation-and-dispatch`, `at-destination`, `team-and-company`, `website-and-integrations`, `reports`.

Permissions are any-of. The `ops.` prefix is omitted below and is written in full in front matter. Plan conditions are listed where they apply.

### H4a: Getting started and setting up

| Section | Slug | Title (en) | Permissions (any) | Conditions | Routes |
|---|---|---|---|---|---|
| getting-started | `workspace-at-a-glance` | Your workspace at a glance | (none) | | `/staff` |
| getting-started | `first-sign-in` | Your first sign-in | (none) | | |
| getting-started | `your-account` | Your password, recovery codes and language | (none) | | `/staff/account` |
| getting-started | `this-device` | Using an approved device | (none) | | |
| getting-started | `signing-in-from-another-country` | Signing in from another country | (none) | | |
| setting-up | `setup-checklist` | Set up your company step by step | settings.edit | | |
| setting-up | `company-settings` | Company settings | settings.view | | `/staff/settings` |
| setting-up | `branches-and-warehouses` | Branches and warehouses | branches.view, warehouses.view | | `/staff/locations` |
| setting-up | `customers-mailbox` | Choose the customers' mailbox | warehouses.manage | | |
| setting-up | `storage-locations` | Storage locations and their labels | warehouses.manage | | `/staff/locations/warehouse/{warehouseId:guid}/storage` |
| setting-up | `printers` | Label printers and the print program | printers.manage | | `/staff/locations/warehouse/{warehouseId:guid}/printers/` |
| setting-up | `shipping-services` | Shipping services | services.view | | `/staff/services` |
| setting-up | `price-lists` | Price lists and versions | charges.configure | | `/staff/charges` |
| setting-up | `zones-and-routes` | Choose the route for a price | charges.configure | | |
| setting-up | `extras-and-item-types` | Extras and item types | charges.configure | | |
| setting-up | `publish-prices` | Show prices on the website and the portal | charges.configure | | |
| setting-up | `insurance-setup` | Insurance products and documents | insurance.view | | `/staff/insurance/` |
| setting-up | `inspection-rules` | Inspection rules and restricted items | inspection.configure | | `/staff/inspection/setup` |
| setting-up | `invoice-settings` | Invoice settings | settings.view | | `/staff/settings/invoices` |
| setting-up | `payment-settings` | Payment methods: cards, Zelle and transfers | settings.view | | `/staff/settings/payments` |
| setting-up | `credit-settings` | Credit account settings | settings.view | | `/staff/settings/credit` |
| setting-up | `bundle-settings` | Bundle settings | settings.view | | `/staff/settings/bundles` |
| setting-up | `event-settings` | Event causes | settings.view | | `/staff/settings/events` |
| setting-up | `pickup-settings` | Pickup settings | settings.view | | `/staff/settings/pickup` |
| setting-up | `delivery-settings` | Delivery settings | settings.view | | `/staff/settings/delivery` |
| setting-up | `supplies-catalogue` | Supplies catalogue and prices | charges.configure | | `/staff/supplies` |
| setting-up | `carriers` | Carriers and their rates | carriers.manage | | `/staff/carriers` |

### H4b: Receiving, checking and the counter

| Section | Slug | Title (en) | Permissions (any) | Conditions | Routes |
|---|---|---|---|---|---|
| receiving-and-checking | `receive-packages-at-the-dock` | Receive packages at warehouse reception | receiving.receive | | `/staff/receiving`, `/staff/scan` |
| receiving-and-checking | `print-labels` | Labels waiting to print | parcels.view | | `/staff/parcels/labels` |
| receiving-and-checking | `unidentified-packages` | Packages nobody announced | parcels.assign | | `/staff/parcels/unidentified` |
| receiving-and-checking | `inspection-station` | Check a package at the inspection station | inspection.check | | `/staff/inspection` |
| receiving-and-checking | `inspection-queue` | Work the inspection queue | inspection.perform | | `/staff/inspection/queue` |
| receiving-and-checking | `holds` | Holds: answers, release and disposal | holds.manage | | `/staff/holds` |
| receiving-and-checking | `parcels` | Find a package | parcels.view | | `/staff/parcels` |
| receiving-and-checking | `incoming-packages` | Packages on the way | parcels.view | | `/staff/parcels/incoming` |
| receiving-and-checking | `stock-counts` | Count the packages on the shelves | parcels.assign | | `/staff/parcels/counts` |
| receiving-and-checking | `missing-package-reports` | Answer a missing-package report | parcels.view, parcels.assign, customers.view | | `/staff/tickets` |
| at-the-counter | `counter-drop-off` | Take a customer drop-off | counter.serve | | `/staff/counter` |
| at-the-counter | `customer-screens` | Customer screens at the counter | counter.serve, payments.take | | `/staff/screens` |
| at-the-counter | `quotes` | Give a customer a quote | quotes.create | | `/staff/quotes` |
| at-the-counter | `customers` | Customers and their records | customers.view | | `/staff/customers` |
| at-the-counter | `shipments-desk` | The shipments desk | shipments.view | | `/staff/shipments` |
| at-the-counter | `change-a-shipment-service` | Change a shipment's service | shipments.manage | | |

### H4c: Money

| Section | Slug | Title (en) | Permissions (any) | Conditions | Routes |
|---|---|---|---|---|---|
| money | `take-a-payment` | Take a payment at the cashier | payments.take | | `/staff/cashier` |
| money | `cash-drawers` | Open and close a cash drawer | payments.take | | `/staff/cashier/drawers` |
| money | `confirm-zelle-and-transfers` | Confirm Zelle and transfers | payments.confirm | | `/staff/cashier/verify` |
| money | `refunds` | Refund a payment | payments.refund | | |
| money | `invoices` | Invoices | invoices.view | | `/staff/invoices` |
| money | `credit-notes-and-voids` | Credit notes and voids | credits.request, credits.approve | | `/staff/invoices/credits` |
| money | `credit-accounts` | Credit accounts | customers.credit | | `/staff/credit` |
| money | `supplies-stock` | Supplies stock | supplies.stock | | `/staff/supplies/stock` |
| money | `supply-discounts` | Approve a supplies discount | discounts.approve | | |
| money | `bills-and-vendors` | Bills and vendors | bills.enter, bills.approve | | `/staff/bills`, `/staff/vendors` |
| money | `profit` | Profit per shipment | costs.view | | `/staff/profit` |

### H4d: Consolidation, dispatch and destination

| Section | Slug | Title (en) | Permissions (any) | Conditions | Routes |
|---|---|---|---|---|---|
| consolidation-and-dispatch | `bundles` | Build and close a bundle | bundles.manage | | `/staff/bundles` |
| consolidation-and-dispatch | `fill-a-bundle` | Scan packages into a bundle | bundles.fill | | |
| consolidation-and-dispatch | `repack-boxes` | Repack boxes | bundles.manage, bundles.fill | | `/staff/bundles/boxes` |
| consolidation-and-dispatch | `dispatches` | Dispatch bundles | dispatches.manage | | `/staff/dispatches` |
| consolidation-and-dispatch | `dispatch-documents` | Dispatch documents | dispatches.manage | | |
| consolidation-and-dispatch | `events` | Events: what happened to a shipment | events.view, events.add | | `/staff/events` |
| at-destination | `receive-an-arrival` | Receive an arrival | arrivals.receive | | `/staff/arrivals`, `/staff/arrivals/{dispatchId}` |
| at-destination | `customs` | Record customs | customs.record | | |
| at-destination | `arrival-discrepancies` | Missing and extra packages at arrival | arrivals.receive | | `/staff/arrivals/discrepancies` |
| at-destination | `pickup-counter` | Release packages at the pickup counter | pickup.release | | `/staff/pickup`, `/staff/pickup/{shipmentId}` |
| at-destination | `plan-deliveries` | Plan home deliveries | delivery.plan | | `/staff/delivery` |
| at-destination | `drive-a-route` | Drive your delivery route | delivery.drive | | `/staff/drive` |
| at-destination | `partner-agents-at-a-glance` | Working as a partner agent | arrivals.receive, customs.record, pickup.release, delivery.plan, delivery.drive | partner_agents | |

### H4e: Team, company, website, integrations and reports

| Section | Slug | Title (en) | Permissions (any) | Conditions | Routes |
|---|---|---|---|---|---|
| team-and-company | `invite-staff` | Invite a staff member | staff.invite | | `/staff/people` |
| team-and-company | `invite-a-partner-agent` | Invite a partner agent | staff.invite | partner_agents | `/staff/people/invite-partner` |
| team-and-company | `manage-a-staff-account` | Lock, suspend or close a staff account | staff.lock, staff.suspend, staff.reactivate, staff.deactivate, staff.reset_second_factor, staff.end_sessions | | |
| team-and-company | `roles-and-warehouses` | Roles, permissions and warehouses | roles.view | | `/staff/roles` |
| team-and-company | `licences-and-devices` | Licences and devices | devices.view | | `/staff/devices` |
| team-and-company | `company-profile` | Your company's details | company.view | | `/staff/company` |
| team-and-company | `subscription-invoices` | Your subscription invoices | company.view | | `/staff/billing` |
| team-and-company | `terms` | Accept the terms of service | company.view | | `/staff/legal` |
| team-and-company | `audit-and-activity` | Audit log and sign-in activity | audit.view | | `/staff/audit`, `/staff/activity` |
| website-and-integrations | `company-website` | Your company website | brand.view | | `/staff/site` |
| website-and-integrations | `website-look-and-content` | Change the website's look and content | brand.edit, brand.publish | website_theme | |
| website-and-integrations | `text-messages` | Text messages to customers | settings.view | text_messages | `/staff/settings/messages` |
| website-and-integrations | `api-and-webhooks` | API keys and webhooks | settings.view | tenant_api | `/staff/settings/api` |
| reports | `reports` | Run and export a report | reports.operations, reports.finance | | `/staff/reports` |
| reports | `scheduled-reports` | Send a report by email on a schedule | reports.operations, reports.finance | | |
| reports | `report-rules` | On-time rules for reports | settings.view | | `/staff/settings/reports` |

## 4. Sources to write from

- **Menu:** `Components/Editor/OperationsNavigation.razor`, `OperationsTaskLinks.razor`.
- **Pages:** `Endpoints/Operations*Endpoints.cs`, `Components/<Area>/*`.
- **Rights:**
  - `Vixly.Operations.Domain/Access/OperationsPermissions.cs`;
  - the use cases under `Vixly.Operations.Application/UseCases/<Area>/`;
  - `Security/OperationsAccess.cs` (warehouse scope, partner agents).
- **Plan features:** `Vixly.Operations.Application/Ports/OperationsPlanFeatures.cs`.
- **Labels:** `Vixly.Operations.Resources/OperationsText*.resx`, `OperationsWorkbenchText*.resx`, `OperationsLoginText*.resx`, `OperationsInspectionText*.resx`.
- **Behaviour:**
  - each slice design and verification in `docs/operations/` (Slices 1 to 11);
  - `REVIEW-ITEMS-2026-10-21.md` (bundle lists, pricing, emails, devices, activity, licences, mailbox);
  - the demo data in `DEVELOPMENT-DEMO-DATA.md` and `DEVELOPMENT-DEMO-USERS.md`, which gives a user for each role to check hiding with.

## 5. Notes for this stage

- **Menu chapters (owner, 2026-10-22).** The staff menu has four chapters: Front Office, Warehouse Operations, Back Office and Technical. The help home lists its sections under the same four chapters, in the same order, with Getting started first. Front desk is now called *Customer drop-off* and Receiving *Warehouse reception*: articles use these names.
- **Warehouse scope.** Many rights are given per warehouse. Articles say "at your warehouse" where the screens offer only the warehouses a person works at; never explain grants in a daily-work article.
- **Partner agents** see only their articles. Their workspace has fewer pages; the `partner-agents-at-a-glance` article is their starting point.
- **Settings decide what screens show** (payment methods, delivery modes, numbering, units, currency). Write "if {brandName} uses…" only where a screen changes; never assume one company's setup.
- **Areas still changing.** Parcel inventory and warehouse management (owner answers of 2026-10-22) will replace the parcels list and add storage moves. When those ship, their slice updates the `parcels`, `storage-locations` and `stock-counts` articles. If H4b is written first, write what is built then.
