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
| Front desk: find the customer | `Counter/CounterPage.razor` + `CounterFindBar` | `/staff/counter` | Styled | 2026-10-20 | FE | Compact 32px search bar on top of every counter page, aligned Find and Clear buttons, subtle background, focus ring, keyboard accesskey N |
| Front desk: the customer | `Counter/CounterCustomerPage.razor` + `CounterCustomerCard` | `/staff/counter/customers/{id}` | Styled | 2026-10-20 | FE | 3-column executive layout, attention-grabbing standing badge with indicator dot, 2x2 tabular metric figures, clean responsive grid |
| Front desk: the shipment | `Counter/CounterShipmentPage.razor` | `/staff/counter/shipments/{id}` | Changed | 2026-10-20 | FE | Summary strip with route/service/progress chips, single-row 32px parcel entry toolbar with baseline-aligned Add button, inline count correction toolbar · **Changed (Slice 5F):** a final shipment shows its invoice line (`CounterInvoiceNote`: number, total, to pay, PDF) |
| Front desk: shipment estimate (part) | `Counter/EstimatePanel.razor` | beside the counter forms | Styled | 2026-10-20 | FE | Side panel card elevation, live indicator badge, scrollable body, executive Total card with bold monospace numbers, what-if comparison list |
| Front desk: insurance choice (part) | `Counter/InsuranceChoice.razor` | counter new shipment and shipment | Styled | 2026-10-20 | FE | Compact radio product options, clear premium basis, acceptance checkbox |
| Front desk: signature (part) | in `Counter/CounterShipmentPage.razor` | `/staff/counter/shipments/{id}` | Styled | 2026-10-20 | FE | Signature canvas frame with clear/save actions, clean inline layout |
| Front desk: supplies (part) | `Counter/CounterSuppliesPanel.razor` | counter shipment and supplies order | Styled | 2026-10-20 | FE | Slice 5E: Styled 32px scan box, table rows with quantity, discount & reason inputs, approval drawer, totals |
| Front desk: choose a supply | `Counter/CounterSupplyChoices.razor` | after a name matching several supplies | Styled | 2026-10-20 | FE | Choice rows with code badge, name, price, and primary Add button |
| Front desk: supplies order | `Counter/CounterSupplyOrderPage.razor` | `/staff/counter/orders/{id}` | Changed | 2026-10-20 | FE | Slice 5E: Order executive bar with status chip, close link, supplies panel, aligned actions · **Changed (Slice 5F):** a confirmed order shows its invoice line in place of Cancel, with the void hint |
| Front desk: the invoice (part) | `Counter/CounterInvoiceNote.razor` | counter shipment (final) and supplies order (confirmed) | Styled | 2026-10-22 | FE | Slice 5F. One-line flex container with status badge, PDF print button and Pay action button |
| Customer list | `Customers/CustomerListPage.razor` | `/staff/customers` | Styled | 2026-10-22 | FE | Brief §5 list anatomy: removed intro explanation, single-line ops-list-bar with autofocus & clear search, compact 36px table with status pills, and ops-list-pager |
| Customer record | `Customers/CustomerPage.razor` | `/staff/customers/{id}` | Styled | 2026-10-22 | FE | Brief §5 record anatomy: summary strip with status pill and quick + Shipment action, compact 4-col label-over-value facts grid, single-line status form, and credit account link |
| Customer's incoming packages (part) | `Customers/CustomerIncomingPackages.razor` | inside the customer record | Styled | 2026-10-22 | FE | Compact table with parcel links, carrier/tracking, warehouse, contents, status pills and top action to all customer parcels |
| Shipments desk | `Shipments/ShipmentsPage.razor` | `/staff/shipments` | On hold | 2026-10-20 | Backend | Counter flow being redesigned (shipment from the customer, parcel count, finalize) · **Changed (Slice 5F step 2):** a payment state chip beside the number of final shipments |
| Shipment record | `Shipments/ShipmentPage.razor` | `/staff/shipments/{id}` | On hold | 2026-10-20 | Backend | Same. Slice 5C added: parcels against the declared count, final amount |
| Quotes | `Charges/QuotesPage.razor` | `/staff/quotes` | Styled | 2026-10-20 | FE | Removed intro hint; side-by-side/stacked ratings layout; recent quotes list table anatomy with status badges and withdraw action |
| All quotes | `Charges/QuoteListPage.razor` | `/staff/quotes/list` | Styled | 2026-10-20 | FE | Single-line search filter bar with autofocus, status count chips (All, Open, Expired, Withdrawn), compact 36px table rows with sticky header, pager |
| Tickets | `Tickets/TicketsPage.razor` | `/staff/tickets` | Styled | 2026-10-20 | FE | Brief §5 list anatomy: single-line filter bar with autofocus, status filter chips, compact 36px table rows with sticky header, pager |
| Ticket | `Tickets/TicketPage.razor` | `/staff/tickets/{id}` | Styled | 2026-10-20 | FE | Brief §5 record anatomy: 2-column workstation layout with 21rem elevated side rail, key facts card, chronological conversation stream |

### Staff: warehouse

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Dock scanning | `Parcels/ScanPage.razor` | `/staff/scan` | Needs styling | 2026-10-20 | Backend | Station anatomy; uses `wwwroot/js/ops-scan.js` (keep its element ids) |
| Receiving (deliveries and receipts) | `Parcels/ReceivingPage.razor` | `/staff/receiving` | Styled | 2026-10-20 | FE | Removed hint; top actions bar; open deliveries table with pieces status; compact receipts table with sticky header; pager |
| Incoming (expected packages) | Parcels/IncomingPage.razor | /staff/parcels/incoming | Styled | 2026-10-20 | FE | Single-line filter bar with autofocus, carrier & warehouse selects, compact date ranges, status count chips, 36px table rows, pager |
| Inspection station | `Parcels/InspectionPage.razor` + `InspectionStationFrame`, `InspectionCheckForm`, `InspectionSketch`, `InspectionSummaryPanel` | `/staff/inspection` | Styled | 2026-10-20 | FE | Dropped decorative sketch, eyebrow and numbered guide steps per brief; kept high-contrast focused scan box; 4-column measurements grid |
| Inspection queue | `Inspection/InspectionQueuePage.razor` | `/staff/inspection/queue` | Styled | 2026-10-20 | FE | Brief §5 list anatomy: single-line filter bar with warehouse selector, compact 36px table rows with sticky header, pager |
| Compliance inspection | `Inspection/InspectionWorkPage.razor` + `InspectionOutcomeForm` | `/staff/inspection/queue/{id}` | Styled | 2026-10-20 | FE | Brief §5 record anatomy: 2-column workstation layout with 21rem sticky summary rail, compact multi-column decision form |
| Holds | `Inspection/HoldsPage.razor` + `HoldAnswersCell`, `UnclaimedPanel` | `/staff/holds` | Styled | 2026-10-20 | FE | Peer tabs: Holds (reasons filter chips, sticky table, inline release), Approvals, and Unclaimed (unclaimed parcels, storage fee settings) |
| Parcels list | `Parcels/ParcelListPage.razor` | `/staff/parcels` | On hold | 2026-10-20 | Backend | To be folded into Inventory (owner, 2026-10-20) |
| Parcel record | `Parcels/ParcelPage.razor` + `ShipmentInstructionsPanel`, `ShippingLabelPanel`, `UndeclaredLabelPanel` | `/staff/parcels/{id}` | Needs styling | 2026-10-20 | Backend | Record anatomy; history, photos, label |
| Labels to print | `Parcels/LabelsToPrintPage.razor` | `/staff/parcels/labels` | Needs styling | 2026-10-20 | Backend | List anatomy |
| Stock counts | `Parcels/StockCountsPage.razor` | `/staff/parcels/counts` | Needs styling | 2026-10-20 | Backend | Part of Inventory later |
| Stock count | `Parcels/StockCountPage.razor` | `/staff/parcels/counts/{id}` | Needs styling | 2026-10-20 | Backend | |
| Storage locations (shelves) | `Parcels/StorageLocationsPage.razor` + `ZoneSelect` | `/staff/locations/warehouse/{id}/storage` | Needs styling | 2026-10-20 | Backend | |
| Label printers | `Parcels/PrintersPage.razor` + `PrinterForm` | `/staff/locations/warehouse/{id}/printers` | Needs styling | 2026-10-20 | Backend | |
| Bundles desk | `Bundles/BundlesPage.razor` + `NewBundleForm` | `/staff/bundles` | Styled | 2026-10-21 | FE | Slice 6A. Ready to bundle table with inline new bundle form, bundles list with status pills, and 3-column responsive new bundle drawer |
| Bundle | `Bundles/BundlePage.razor` + `BundleShipmentsPanel`, `BundleParcelsTable` | `/staff/bundles/{id}` | Needs styling | 2026-10-21 | Backend | Slice 6A. Route, state, service, type, size policy, filled; close with weight and three sides; void with a reason; label (4 x 6 PDF); its shipments' completeness (`data-split`, `data-complete`); its parcels · **Review 2026-10-21:** the bundle list (`BundleListPanel`): add parcels from the computer (choices with checkboxes), lines with a state (entered, scanned, missing, blocked), counts Entered · Scanned · Missing, remove a line |
| Fill a bundle (station) | `Bundles/FillBundlePage.razor` | `/staff/bundles/fill`, `/staff/bundles/{id}/fill` | Changed | 2026-10-21 | Backend | Slice 6A. Brief §5 station anatomy: large 44px focused barcode laser scan box (`ops-bundle-fill__scan`), bundle header with live meter, last scan focus card, running parcels table with inline take out · **Changed (review 2026-10-21):** the bundle list panel (`BundleListPanel`) beside the scans; a parcel not on the list is refused with a message |
| Consolidated shipments | `Bundles/RepackBoxesPage.razor` | `/staff/bundles/boxes` | Styled | 2026-10-21 | FE | Slice 6A. Consolidated shipments list with company default way indicator, inline switch dropdown and start box action |
| Repack box | `Bundles/RepackBoxPage.razor` | `/staff/bundles/boxes/{id}` | Needs styling | 2026-10-21 | Backend | Slice 6A. Scan the shipment's parcels in (`data-in-box` on rows), shelves of the others, close with weight and size, void, label (4 x 6 PDF) |

### Staff: setup

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Prices | `Charges/ChargesPage.razor` + `ChargesTabs`, `PriceListPanel`, `ExtrasPanel`, `ItemTypesPanel`, `InsuranceProductsPanel`, `InsurancePlanForm` and field parts | `/staff/charges` | On hold | 2026-10-21 | Backend | Its tabs are the model for other pages, but the pricing engine is being revisited with the owner. Slice 5D: insurance is now a list of products per mode (each a collapsed panel), with name, order and pitch · Review 2026-10-21: a *Published* checkbox on a regular price list (new list form and each list), with a hint |
| Supplies catalogue | `Supplies/SuppliesPage.razor` + `SupplyDetailsFields`, `SupplyPriceFields`, `SupplyStockSettingsPanel` | `/staff/supplies` | Styled | 2026-10-20 | FE | Removed intro hint, single-line search bar with category filter, compact sticky table, responsive multi-column form grid for new supplies & stock settings |
| Supply | `Supplies/SupplyPage.razor` + `SupplyPriceHistory` | `/staff/supplies/{id}` | Styled | 2026-10-21 | FE | Slice 5E. Horizontal active checkbox, side-by-side details trio, compact 3-column quantity breaks table, responsive warehouse price grid, shelf labels PDF |
| Supplies stock | `Supplies/SupplyStockPage.razor` + `SupplyStockTable`, `SupplyStockForms`, `SupplyLinesFields` | `/staff/supplies/stock` | Styled | 2026-10-20 | FE | Warehouse chips, low-stock warning indicators (data-low), and peer tabs for On Hand, Transfers in Transit, Receive, Adjust, Send, and Count |
| Supplies count and movements | `Supplies/SupplyCountPage.razor`, `Supplies/SupplyMovementsPage.razor` | `/staff/supplies/stock/counts/{id}`, `/staff/supplies/{id}/movements` | Needs styling | 2026-10-20 | Backend | Slice 5E. The count sheet (on hand, counted); the movements of a supply at a warehouse |
| Insurance | `Insurance/InsurancePage.razor` | `/staff/insurance` | Needs styling | 2026-10-20 | Backend | Slice 5B step 1: policies, covers per mode, liability per mode; use tabs |
| Insurance policy | `Insurance/PolicyPage.razor` | `/staff/insurance/policies/{id}` | Needs styling | 2026-10-20 | Backend | Long form: put it in tabs (cover, limits, duties, documents) |
| Inspection rules | `Inspection/InspectionSetupPage.razor` + `RestrictedItemFields` | `/staff/inspection/setup` | Needs styling | 2026-10-20 | Backend | |
| Locations | `Editor/LocationsPage.razor` | `/staff/locations` | Restyle | 2026-10-20 | Backend | Styled 2026-10-18 |
| Branch | `Editor/BranchFormPage.razor` | `/staff/locations/branch/{id}` | Restyle | 2026-10-20 | Backend | |
| Warehouse | `Editor/WarehouseFormPage.razor` + `DestinationFields` | `/staff/locations/warehouse/{id}` | Restyle | 2026-10-21 | Backend | Destination terms added after the 10-18 styling. Slice 7: a *Customs is cleared here* checkbox with its hint in the destination terms |
| Contact | `Editor/ContactFormPage.razor` | `/staff/locations/contact/{id}` | Restyle | 2026-10-20 | Backend | |
| Services | `Settings/ShippingServicesPage.razor` | `/staff/services` | Restyle | 2026-10-20 | Backend | |
| Settings | `Settings/SettingsPage.razor` | `/staff/settings` | Restyle | 2026-10-21 | Backend | Units, storage fee, claims settings added since; Slice 6A adds two prefixes under Numbering (bundles BD, repack boxes CB); Slice 6B adds the dispatch prefix (DSP) · Review 2026-10-21: *Show prices on the public website* checkbox |
| Invoices and taxes | `Invoices/InvoiceSettingsPage.razor` | `/staff/settings/invoices` | Styled | 2026-10-21 | FE | Slice 5F. Tax name & number duo, responsive grid for rates, horizontal per-kind radio cards, 3-column multilingual invoice note cards |
| Invoices | `Invoices/InvoicesPage.razor` | `/staff/invoices` | Changed | 2026-10-20 | FE | Brief §5 list anatomy: single-line search & state filter bar, pending requests badge, compact 36px table with status pills and tabular figures · **Changed (Slice 5F step 2):** a new state chip `awaiting` |
| Invoice | `Invoices/InvoicePage.razor` + `InvoiceLines`, `InvoiceCredits`, `InvoiceRequestForm` | `/staff/invoices/{id}` | Changed | 2026-10-20 | FE | Brief §5 record anatomy: summary strip with PDF action on right, facts grid, lines table with discount/credited, totals card with bold owed badge · **Changed (Slice 5F step 2):** the payment state and Pay in the header; payments and refunds (`PaymentsList`); the refund form (`InvoiceRefundForm`); paid, awaiting and given back lines in the totals |
| Credits to approve | `Invoices/CreditRequestsPage.razor` | `/staff/invoices/credits` | Styled | 2026-10-20 | FE | Queue anatomy: back link, status-tinted request cards with aligned Approve and Refuse buttons |
| Cashier | `Cashier/CashierPage.razor` | `/staff/cashier` | Styled | 2026-10-21 | FE | Slice 5F. Single-line scan-ready find bar (`data-scan`), Next customer action, cash drawer telemetry, matches list, table with payment status pills and Pay button |
| Cashier: pay an invoice | `Cashier/CashierInvoicePage.razor` + `ZelleTermsPanel`, `PaymentsList`, `BillAccountPanel` | `/staff/cashier/invoices/{id}` | Changed | 2026-10-22 | Backend | Slice 5F. Invoice summary, 4-column compact horizontal tender rows (method, amount, cash handed, ref), Zelle panel with horizontal checkbox and QR details, payment history. Step 4: a card panel (*Card: payment page (QR code)* and one *Card on <reader>* button per reader at this warehouse); `PaymentsList` shows a pending card refund (`data-refund-pending`) with *Finish the refund* |
| Credit accounts | `Credit/CreditAccountsPage.razor` | `/staff/credit` | Styled | 2026-10-21 | FE | Slice 5F step 2b. Single-line account opening bar with code, limit, terms, and charges link; overdue indicators and compact status pills |
| Credit account | `Credit/CreditAccountPage.razor` + `CreditPositionFacts` | `/staff/credit/customers/{id}` | Needs styling | 2026-10-21 | Backend | Slice 5F step 2b. State and reason, limit and terms, the position (owed, awaiting, available, overdue, five age buckets), change form (restricted panel), open items (`data-overdue`, `data-awaiting`), statement PDF |
| Charges to approve | `Credit/CreditChargesPage.razor` | `/staff/credit/charges` | Styled | 2026-10-21 | FE | Slice 5F step 2b. Queue table with invoice and customer links, owed & available figures, inline note input with aligned Approve and Refuse buttons |
| Customer screens | `Screens/ScreensPage.razor` | `/staff/screens` | Needs styling | 2026-10-22 | Backend | Slice 5F step 3. Intro with the screen address; the one-use code in very large type (`data-code`) after adding or *New code*; single-line add bar (warehouse, name); table (name, warehouse, state, showing, *New code* and *Remove*). Linked from the cashier |
| Screen send panel | `Screens/ScreenSendPanel.razor` on the cashier invoice and the counter shipment | `#screens` | Needs styling | 2026-10-22 | Backend | Slice 5F step 3. One row per screen of the warehouse: name, what it shows (`data-showing`), Zelle accepted (`data-zelle-accepted`), *Send to pay* / *Send to sign*, *Clear · next customer* |
| Customer screen | `Screens/ScreenShell.razor`, `ScreenPairPage.razor`, `CustomerScreenPage.razor`, `ScreenActionButton.razor`; `css/ops-screen.css`, `js/ops-screen.js` | `/screen` | Needs styling | 2026-10-22 | Backend | Slice 5F step 3. Its own full-page document for a tablet at arm's length (no staff chrome; the company's name only). Steps in `data-screen-step`: pairing code, idle welcome, invoice summary (`InvoiceLines`, amount to pay, large choice buttons), Zelle warning and terms, Zelle (QR picture, tag, contact, amount and memo in large type), card (Stripe QR and *Pay on this screen*), declaration (parcels table, insurance, signature pad `data-signature-pad`), thank you; a thank-you page for a phone back from Stripe. Reloads itself on changes (polls `/screen/version`) |
| Card payment (staff) | `Cashier/CardPaymentPage.razor` | `/staff/cashier/card/{chargeId}` | Needs styling | 2026-10-22 | Backend | Slice 5F step 4. Stripe's payment page as a QR code (`data:image/svg+xml`) with its link for the customer's phone, or *Waiting for the card on <reader>*; state in `data-card-status` (started, paid, cancelled); refreshes itself every 4 seconds (meta refresh); *Cancel* form |
| Cashier: payment taken | `Cashier/CashierPaymentPage.razor` | `/staff/cashier/payments/{id}` | Styled | 2026-10-21 | FE | Slice 5F. Change to give in very large type (`data-change`), receipt button, next customer |
| Cash drawers | `Cashier/CashDrawersPage.razor`, `Cashier/CashDrawerPage.razor` | `/staff/cashier/drawers`, `/staff/cashier/drawers/{id}` | Styled | 2026-10-21 | FE | Slice 5F. Single-line drawer opening bar with warehouse and float inputs, compact drawers history table with tabular currency figures |
| Payments to verify | `Cashier/VerifyPaymentsPage.razor` | `/staff/cashier/verify` | Styled | 2026-10-21 | FE | Slice 5F. Finance verification queue cards with inline received/not received actions |
| Payment methods | `Cashier/PaymentSettingsPage.razor` | `/staff/settings/payments` | Changed | 2026-10-22 | Backend | Slice 5F. Horizontal payment method checkboxes, 3-column other methods, QR upload duo, 3-column multilingual Zelle terms. Step 4 adds `Cashier/CardSettingsPanel.razor` (`#cards`): Stripe keys form (secret, publishable, webhook secret; only the last characters shown once kept), the webhook address to copy, *Accept cards*, readers per warehouse (label, `tmr_` id, remove), *Disconnect* |
| Credit settings | `Credit/CreditSettingsPage.razor` | `/staff/settings/credit` | Styled | 2026-10-21 | FE | Slice 5F step 2b. Compact terms field, horizontal over-limit radio cards, horizontal statement toggle |
| Bundle settings | `Bundles/BundleSettingsPage.razor` | `/staff/settings/bundles` | Styled | 2026-10-21 | FE | Slice 6A. Types editor, side-by-side duo for target rules and consolidation way with horizontal radio options |
| Dispatch desk | `Dispatch/DispatchesPage.razor` | `/staff/dispatches` | Styled | 2026-10-21 | FE | Slice 6B. Segmented view switch with free bundles telemetry, compact dispatches table, 2x2 duo grid new dispatch form |
| Dispatch | `Dispatch/DispatchPage.razor` + `DispatchLegsPanel` + `DispatchPapersPanel` | `/staff/dispatches/{id}` | Needs styling | 2026-10-21 | Backend | Slice 6B. Facts (state, service, cutoff, bundles, times); bundles with scan-in field and fitting bundles; legs table with inline remove / depart / arrive forms (`datetime-local`), add-a-leg form; shipments with house numbers and cost shares; carrier cost (work out again, correct with a note); letter of instruction (prepaid/collect radios, export reference, document checkboxes, handling); close and cancel panels; PDFs: dispatch manifest, letter of instruction, house air waybills, bundle manifests |
| Carriers | `Dispatch/CarriersPage.razor` + `CarrierForm` | `/staff/carriers` | Styled | 2026-10-21 | FE | Slice 6B. Compact carriers list with carrier form in side-by-side duos with horizontal active checkbox |
| Carrier | `Dispatch/CarrierPage.razor` + `CarrierForm` | `/staff/carriers/{id}` | Needs styling | 2026-10-21 | Backend | Slice 6B. Change the carrier; its rates by destination (per weight with minimum for air, per bundle by type for ocean and ground) with start dates; add a rate; remove one not yet started |
| Events | `Events/EventsPage.razor` | `/staff/events` | Styled | 2026-10-21 | FE | Slice 6C. Single-line search bar with autofocus, aligned find button, and hint |
| Event history | `Events/EventTimelinePage.razor` | `/staff/events/{kind}/{id}` | Styled | 2026-10-21 | FE | Slice 6C. Horizontal event kind radios, side-by-side cause/datetime duo, customer visibility indicators, inline void actions |
| Events settings | `Events/EventSettingsPage.razor` | `/staff/settings/events` | Styled | 2026-10-21 | FE | Slice 6C. Horizontal notification checkboxes, horizontal standard causes grid, 3-column multilingual cause cards |
| Arrivals | `Arrivals/ArrivalsPage.razor` + `ArrivalRows` | `/staff/arrivals` | Needs styling | 2026-10-21 | Backend | Slice 7. Warehouse select (GET form, only with two or more), link to discrepancies, *On their way here* and *Received lately* tables (dispatch number with an *onward* hint, from, state chip, departed, received, bundles) |
| Receive a dispatch | `Arrivals/ArrivalPage.razor` + `DiscrepancyRows` | `/staff/arrivals/{id}?warehouse=` | Needs styling | 2026-10-21 | Backend | Slice 7. Facts (`ops-editor__facts`), the scan form (autofocus, used with a scanner all day), bundles table with a state chip (`data-status` expected, arrived, opened, passing), parcels here of all and an *Open* button per bundle; customs form (step, whole dispatch or one bundle, customer line); *Finish* form; discrepancies table |
| Discrepancies | `Arrivals/DiscrepanciesPage.razor` + `DiscrepancyRows` | `/staff/arrivals/discrepancies` | Needs styling | 2026-10-21 | Backend | Slice 7. Open or all; each open line has an inline form (outcome select, note, *Resolve*) |
| Pickup counter | `Pickup/PickupsPage.razor` | `/staff/pickup` | Needs styling | 2026-10-21 | Backend | Slice 7 step 2. Search form (warehouse select with two or more, one field), table of shipments ready (number, recipient, person named, parcels, ready since, payment chip `data-status` paid or unpaid) |
| Pickup of a shipment | `Pickup/PickupPage.razor` | `/staff/pickup/{id}?warehouse=` | Needs styling | 2026-10-21 | Backend | Slice 7 step 2. Who may collect (`ops-editor__facts`), parcels with a state chip (ready, held, collected, here, elsewhere) and storage, invoices with a payment chip linking to the cashier, the bill form (three amounts when the company bills charges), the code form, the release form (`multipart`, collector radios, ID select and last 4, code, photo with `capture`, signature pad `data-signature-pad` with canvas), releases list |
| Pickup settings | `Pickup/PickupSettingsPage.razor` | `/staff/settings/pickup` | Needs styling | 2026-10-21 | Backend | Slice 7 step 2. Four proof checkboxes, the charges checkbox, the storage fieldset (checkbox, free days, fee) |
| Home delivery desk | `Delivery/DeliveriesPage.razor` | `/staff/delivery?warehouse=` | Needs styling | 2026-10-21 | Backend | Slice 7 step 3. Warehouse select (two or more), plan form (day, driver select, driver name in run-sheet mode, table of shipments waiting with checkboxes and a payment chip `data-status` paid, cod or unpaid), bill form (shipment select, three amounts, shown when destination charges are on), routes table (status chip `data-status` planned, out, returned, settled, cancelled), returns table with an inline form per parcel |
| Delivery route | `Delivery/DeliveryRoutePage.razor`, `Delivery/DeliveryStopForms.razor` | `/staff/delivery/routes/{id}` | Needs styling | 2026-10-21 | Backend | Slice 7 step 3. Used on a phone by drivers. Facts (`ops-editor__facts`), print and start buttons, stops as cards (`ops-delivery__stops`, `ops-delivery__stop` with `data-status` pending, delivered, failed; `ops-delivery__sequence` badge), up/down/take off buttons while planned, per stop two `details` (`ops-delivery__record`): delivered (`multipart`, receiver, ID select and last 4, code, cash read-only, photo with `capture`, signature pad `data-signature-pad` with canvas) and not delivered (reason select, note); add, cancel and settle forms. Starter styles in `css/ops-delivery.css` |
| My routes | `Delivery/DrivePage.razor` | `/staff/drive` | Needs styling | 2026-10-21 | Backend | Slice 7 step 3. A driver's routes as a list with a status chip and progress |
| Delivery settings | `Delivery/DeliverySettingsPage.razor` | `/staff/settings/delivery` | Needs styling | 2026-10-21 | Backend | Slice 7 step 3. Recording radios (phone, run sheet), cash on delivery checkbox, tries number and after-failure radios, four proof checkboxes, the email checkbox |
| Website editor | `Editor/SiteEditorPage.razor` | `/staff/site` | Restyle | 2026-10-20 | Backend | |

### Staff: administration and account

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Staff | `Staff/StaffListPage.razor` | `/staff/people` | Restyle | 2026-10-20 | Backend | |
| Staff member | `Staff/StaffMemberPage.razor` | `/staff/people/{id}` | Restyle | 2026-10-21 | Backend | Review 2026-10-21: a *Sign-ins* link beside the history link |
| Roles | `Staff/RolesPage.razor` | `/staff/roles` | Restyle | 2026-10-20 | Backend | |
| Role | `Staff/RoleFormPage.razor` | `/staff/roles/{id}` | Restyle | 2026-10-20 | Backend | Many permissions: group them in tabs or columns |
| Licences (company tab) | `Editor/DevicesPage.razor` + `CompanyTabs` | `/staff/devices` | Needs styling | 2026-10-21 | Backend | Owner, 2026-10-21: the *Licences* tab of the company (no Devices menu). Summary (in use of bought; a decrease scheduled for the renewal), then one table: licence number, device ID, name (link to the device page), type, browser, status chip (`data-status` free, pending, active, release_pending, revoked, released, denied, expired), registered, authorised by. Free licences are rows too |
| Device | `Editor/DeviceDetailPage.razor` | `/staff/devices/{id}` | Needs styling | 2026-10-21 | Backend | Owner, 2026-10-21. Facts (device ID, status, type, browser, registered, requested by, authorised by, last seen); *Approve this device?* with a code and Approve / Deny when requested; *Release its licence* (reason, code) when bound |
| Company | `Settings/CompanyPage.razor` + `CompanyTabs` | `/staff/company` | Restyle | 2026-10-20 | Backend | |
| Billing (the company's Vixly plan) | `Settings/BillingPage.razor` | `/staff/billing` | Restyle | 2026-10-20 | Backend | |
| Audit | `Settings/AuditPage.razor` | `/staff/audit` | Restyle | 2026-10-20 | Backend | |
| Sign-in activity | `Settings/ActivityPage.razor` | `/staff/activity` | Needs styling | 2026-10-21 | Backend | Review 2026-10-21. Table: member (link filters by member), signed in, signed out with how (or a *Still signed in* chip), device (name · licence), browser, country; a row flagged `data-flag="country"` with a warning chip; a hint linking to refused sign-ins in the history; newer/older paging |
| My account | `Staff/AccountPage.razor` | `/staff/account` | Restyle | 2026-10-20 | Backend | |
| Confirm (step-up) | `Staff/ConfirmPage.razor` | `/staff/confirm` | Restyle | 2026-10-20 | Backend | |
| Terms acceptance | `Editor/LegalPage.razor` | `/staff/legal` | Restyle | 2026-10-20 | Backend | |
| Message page | `Editor/EditorMessagePage.razor` | various | Restyle | 2026-10-20 | Backend | |

### Staff: sign-in and setup (Comfortable density allowed)

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Sign-in | `SignIn/SignInPage.razor` + `AuthShell`, `AccessIllustration` | `/staff/sign-in` | Styled | 2026-10-21 | FE | Executive dual-wing logistics console: atmospheric deep navy backdrop with logistics grid, bespoke multi-modal vector artwork (air/maritime/cross-dock & barcode laser), capability pillar tiles, executive topbar with live dispatch telemetry, theme switch & language pills, comfortable 44px field controls with icons, and zero-trust authorized access badge · Also the page shown after inactivity: nothing else on it (owner) |
| Second step, enrolment, recovery, recovery codes | `SignIn/VerifyPage`, `EnrolPage`, `RecoveryPage`, `RecoveryCodesPage` | `/staff/sign-in/...` | Restyle | 2026-10-20 | Backend | |
| Forgot and reset password | `SignIn/ForgotPasswordPage`, `ResetPasswordPage` | `/staff/forgot-password`, `/staff/reset-password` | Restyle | 2026-10-20 | Backend | |
| Invitation | `SignIn/InvitationPage.razor` | `/invitation` | Restyle | 2026-10-20 | Backend | |
| Second-step reset | `SignIn/SecondFactorResetPage.razor` | `/account/second-factor-reset` | Restyle | 2026-10-20 | Backend | |
| Devices: enrol, link, waiting | `SignIn/DeviceEnrolPage`, `DeviceLinkPage`, `DeviceWaitingPage` | `/devices/...` | Restyle | 2026-10-21 | Backend | Review 2026-10-21: the type is Desktop, Laptop or Mobile |
| First setup | `SignIn/SetupHomePage.razor` | `/setup` | Restyle | 2026-10-20 | Backend | |
| Access limited | `SignIn/AccessLimitedPage.razor` | `/staff/limited` | Restyle | 2026-10-20 | Backend | |

### Company public site (tenant branded)

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Home, locations, tracking | `Site/SiteHomePage`, `SiteLocationsPage`, `SiteTrackPage` + `SiteShell` | `/{language}`, `/{language}/locations`, `/{language}/track` | Styled | 2026-10-21 | Backend | Marketing pages: outside the density rules; owner review pending · Review 2026-10-21: a *Pricing* link in the site menu when the company publishes prices |
| Pricing (public) | `Site/SitePricingPage.razor` | `/{language}/pricing` | Needs styling | 2026-10-21 | Backend | Review 2026-10-21. Shown only when the company publishes prices. One card per price list (`site-pricing__list`): service, route, bands per kg or lb (or per m³/ft³), items, per package, per shipment, minimum, service and consolidation fees, *in force since* (`site-pricing__since`); a sign-up button. Marketing page: outside the density rules |
| Not found, not available, unavailable | `Pages/NotFoundPage`, `NotAvailablePage`, `UnavailablePage` | status pages | Styled | 2026-10-18 | FE | |

### Customer portal

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Sign-in, sign-up, verify, forgot and reset password | `Portal/SignInPage`, `SignUpPage`, `VerifyPage`, `ForgotPasswordPage`, `ResetPasswordPage` | `/customer/...` | Styled | 2026-10-18 | FE | Comfortable density; owner review pending |
| Home (mailbox address) | `Portal/HomePage.razor` | `/customer` | Styled | 2026-10-20 | FE | Removed repeated titles and decorative sketches; replaced with semantic SVG icons |
| Packages and shipments | `Portal/PackagesPage.razor` + `PutInForm`, `TakeOutForm`, `PackageMoveForm` | `/customer/packages` | Styled | 2026-10-20 | FE | Styled actions header bar, clean status badges, compact card layouts |
| Package | `Portal/PackagePage.razor` + `HoldAnswerForms` | `/customer/packages/{id}` | Styled | 2026-10-20 | FE | Styled top actions bar with back button, compact details grid, photo gallery cards, tracking history |
| Announce a package | `Portal/PreAlertFormPage.razor` + `PackageFields` | `/customer/packages/new` | Restyle | 2026-10-20 | Backend | |
| Shipment | `Portal/ShipmentPage.razor` | `/customer/packages/shipments/{id}` | Needs styling | 2026-10-20 | Backend | Slice 5C: shows "n of m received", the amount and a notice once final |. Slice 7 step 2: *Who may collect it* form (name, phone) on a booked pickup shipment, then *Collected on* once collected Slice 7 step 3: *Delivered on* a notice once delivered at the address
| Create or change a shipment | `Portal/ShipmentFormPage.razor` + `ShipmentDestinationFields`, `InsuranceTermsView` | `/customer/packages/shipments/new` | Styled | 2026-10-20 | FE | Removed intro sentence, required parcel count input, aligned form action buttons, beside live estimate |
| Shipment estimate (part) | `Portal/ShipmentEstimatePanel.razor` | beside the shipment form | Styled | 2026-10-20 | FE | Executive card elevation, gradient header with LIVE badge, monospace tabular numbers, executive Total Card, what-if comparison list |
| Insurance choice (part) | `Portal/InsuranceProductChoice.razor` | in the shipment form | Needs styling | 2026-10-20 | Backend | Slice 5D. No insurance or one product: name, price rule, pitch, full terms |
| Rates calculator | `Portal/RatesPage.razor` + `RateLines` | `/customer/rates` | On hold | 2026-10-20 | Backend | Follows the pricing review |
| Pricing | `Portal/PricingPage.razor` | `/customer/pricing` | Needs styling | 2026-10-21 | Backend | Review 2026-10-21. New *Pricing* item in the portal menu. The customer's own prices first (marked *Your prices*), then the regular lists: service, route, bands, items, fees, in force since; a button to the calculator |
| Tickets | `Portal/TicketsPage`, `TicketPage`, `TicketFormPage` | `/customer/tickets` | Needs styling | 2026-10-20 | Backend | |
| Invoices | `Portal/InvoicesPage.razor` + `CreditAccountPanel` | `/customer/invoices` | Needs styling | 2026-10-21 | Backend | Slice 5F. The customer's invoices: number (PDF), date, for which shipment or order, total, to pay, credit notes (PDF). New *Invoices* item in the portal menu. Step 2b: *Your credit account* on top (`#account`) when the customer has one: state, limit and terms, owed, available, overdue, open items, statement PDF. Step 4: *Pay by card* on each invoice owed when the company takes cards |
| Card payment (portal) | `Portal/CardPaymentPage.razor` | `/customer/invoices/card/{chargeId}` | Needs styling | 2026-10-22 | Backend | Slice 5F step 4. A link to Stripe's secure page while started; paid, cancelled or waiting states; back to the invoices |
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
| Tenant | `Pages/TenantPage.razor` | `/tenants/{TenantId:guid}` | Restyle | 2026-10-21 | Backend | Many sections (subscription, finance, licensing, activity, legal, contacts, notes, history): use tabs · Licensing (owner, 2026-10-21): a licence list (`TenantLicenceList`): licence number, device ID, name (link to the device page), type, status badge, requested; free licences as rows |
| Registrations | `Pages/RegistrationsPage.razor` | `/tenants/registrations` | Restyle | 2026-10-20 | Backend |  |
| Registration | `Pages/RegistrationPage.razor` | `/tenants/registrations/{RegistrationId:guid}` | Restyle | 2026-10-20 | Backend |  |
| Company change requests | `Pages/TenantChangesPage.razor` | `/tenants/changes` | Restyle | 2026-10-20 | Backend |  |
| Company change request | `Pages/TenantChangePage.razor` | `/tenants/changes/{RequestId:guid}` | Restyle | 2026-10-20 | Backend |  |

#### Control: sales and plans

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Plans | `Pages/PlansPage.razor` | `/plans` | Restyle | 2026-10-20 | Backend |  |
| Plan | `Pages/PlanPage.razor` | `/plans/{PlanId:guid}` | Styled | 2026-10-20 | FE | Brief §5 record anatomy: executive summary strip with Start Draft action on right, generous term bar top space, segmented tabs for Draft, Versions, Translations, Catalogue, and Approval |
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
| Devices | (removed) | `/devices` | Changed | 2026-10-21 | Backend | Owner, 2026-10-21: no Devices menu or list; each tenant's licences are on its page |
| Device | `Pages/DevicePage.razor` | `/devices/{DeviceId:guid}` | Restyle | 2026-10-21 | Backend | Review 2026-10-21: operating system, browser, IP address and country where it was registered; who approved it |
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
| Sign-in activity | `Pages/ActivityPage.razor` + `Features/Audit/SignInActivity` | `/activity` | Needs styling | 2026-10-21 | Backend | Review 2026-10-21. Who (link filters by person), signed in, signed out with how (or a *Still signed in* badge), IP address, country, browser, operating system; pager |
| My account | `Pages/MyAccountPage.razor` | `/account` | Restyle | 2026-10-20 | Backend |  |

#### Control: sign-in (Comfortable density allowed)

| Screen | Component | Route | Status | Updated | By | Notes |
|---|---|---|---|---|---|---|
| Sign-in | `SignIn/SignInPage` + `ControlLoginShell` | `/sign-in` | Styled | 2026-10-20 | FE | Fresh open form and slim header; 24 size/language/theme captures fit without scrolling; keyboard, error state and text contrast checked. Owner review pending |
| Second step, enrolment, recovery, recovery codes, forgot and reset password, invitation | `SignIn/VerifyPage`, `EnrolPage`, `RecoveryPage`, `RecoveryCodesPage`, `ForgotPasswordPage`, `ResetPasswordPage`, `InvitationPage` + `AuthShell` | `/sign-in/...`, `/invitation`, â€¦ | Styled | 2026-10-14 | FE | Existing presentation retained; owner review pending |

## Shared parts

| Part | Component | Status | Updated | By | Notes |
|---|---|---|---|---|---|
| Staff shell and top bar | `Editor/EditorShell.razor` | Styled | 2026-10-20 | FE | Compact 52px header, single topbar title, quick search (Ctrl+K), dark/light theme toggle, hero removed |
| Staff menu | `Editor/OperationsNavigation.razor` | Restyle | 2026-10-20 | Backend | Job-based groups once the owner confirms them (brief, 6) |
| Read-only banner | `Editor/AccessBanner.razor` | Restyle | 2026-10-20 | Backend | One slim line |
| Task links | `Editor/OperationsTaskLinks.razor` | Restyle | 2026-10-20 | Backend | |
| Table viewport | `Editor/OperationsTableViewport.razor` | Restyle | 2026-10-20 | Backend | Becomes the list anatomy table (brief, 5) |
| Address, contact, country, status fields | `Editor/AddressFields`, `ContactFields`, `CountrySelect`, `StatusFields` | Restyle | 2026-10-20 | Backend | Field widths (brief, 2.5) |
| Control shell and menu | `Platform.Web/Components/Layout/MainLayout.razor` | Restyle | 2026-10-20 | Backend | Same shell rules as Operations: compact header, no hero, job-based menu |
| Step-up dialog (Control) | `Platform.Web/Features/StepUp/StepUpDialog.razor` | Restyle | 2026-10-20 | Backend | |
| Customer portal shell | `Portal/CustomerShell.razor` + `AccountSketch`, `CustomerAccessStory` | Styled | 2026-10-20 | FE | 52px single-tier header, comfortable 40px density, decorative sketches removed from work screens |
| Shared primitives | `Vixly.Shared.UI/Primitives/*` | Restyle | 2026-10-20 | Backend | Compact density tokens (brief, 4); a list-page and a record-page primitive (brief, 9) |

## Log

| Date | By | Change |
|---|---|---|
| 2026-10-20 | Backend | Inventory created from every screen in Operations.Web and Customer.Web; statuses set against the UI/UX brief of 2026-10-20 |
| 2026-10-20 | Backend | Control console (Platform.Web) added: 50 screens to restyle, sign-in styled (owner, 2026-10-20) |
| 2026-10-20 | FE | Operations shell compact density (32px controls, 36px table rows, 52px header, quick search Ctrl+K, theme toggle) and Customer portal shell (52px header, comfortable 40px density, no decorative art on work screens) |
| 2026-10-20 | FE | Front desk counter styling: ops-counter.css linked in EditorShell, button alignment fixed (32px controls, flex-end baseline), side estimate panel given visual elevation, gradient header, live indicator and executive total card, customer card given 3-column executive layout and figures |
| 2026-10-20 | FE | Front-desk supplies, quotes workbench & list, dock receiving, inspection station (dropped sketches/guide steps), customer portal shipment estimate elevation and package actions |
| 2026-10-20 | FE | Incoming packages, supplies catalogue, customer package & shipment details, multi-column form grid layout, sidebar visual elevation, and button alignment fixes |
| 2026-10-20 | Backend | Slice 5F step 1: invoices (list, invoice, credits to approve, invoices and taxes settings), the invoice line at the counter, the portal's invoices; counter shipment and supplies order set to Changed |
| 2026-10-20 | FE | Tickets desk (list + record 2-column workstation layout), inspection queue & work page (2-column layout with 21rem rail), holds desk with peer tabs, supplies stock with warehouse tabs and form tabs, invoices desk (list, record, credits queue), and plan summary strip with tabs and right-aligned start draft button |
| 2026-10-21 | Backend | Slice 5F step 2a: the cashier, cash drawers, payments to verify, payment methods; the invoice page gains payments, refunds and the payment state; the shipments desk and the counter show the payment state; the portal's invoices show it |
| 2026-10-21 | Backend | Slice 6A: bundles desk, bundle, fill station, consolidated shipments, repack box, bundle settings; Settings gains two prefixes |
| 2026-10-21 | Backend | Slice 5F step 2b: credit accounts, credit account, charges to approve, credit settings; the cashier invoice gains *Bill the account*; the customer record links to the account; the portal invoices show the credit account |
| 2026-10-21 | Backend | Slice 6B: dispatch desk, dispatch (legs, shipments, cost, letter of instruction), carriers, carrier; the bundle page links its manifest; Settings gains the dispatch prefix |
| 2026-10-21 | Backend | Slice 6C: events workspace, event history, events settings; the parcel history shows delays and exceptions with their cause |
| 2026-10-21 | FE | Operations staff sign-in reimagined: executive topbar with live dispatch telemetry & theme toggle, atmospheric multi-layered backdrop, bespoke multi-modal vector logistics artwork, capability pillar cards, comfortable field controls with icons and password reveal |
| 2026-10-21 | FE | Screen reviews & alignments: Top-aligned inspection action on parcel page; single-row search/filter bars across inspection queue, invoices, supplies stock, cashier and events; side-by-side product/price duos and coverage trios on charges; overhauled quantity breaks into compact 3-column table; horizontal checkboxes & labels across supplies, cashier, credit, dispatches, bundles, carriers and events |
| 2026-10-21 | Backend | Review items: bundle lists (bundle, fill station), pricing (portal and public Pricing pages, *Published* on price lists, the public prices setting), device details and type, sign-in activity in Operations and Control |
| 2026-10-21 | Backend | Licences: a *Licences* tab in the company (list and device page) replaces the Devices menu in Operations; in Control the tenant page lists its licences and the Devices menu and list are removed |
| 2026-10-22 | Backend | Slice 5F step 3 (customer screen): Customer screens (staff), the screen send panel on the cashier invoice and the counter shipment, the customer screen pages; the cashier gains a *Customer screens* link |
| 2026-10-22 | Backend | Slice 5F step 4 (Stripe cards): card payment page (staff) new; the payment settings gain the card panel; the cashier invoice gains a card panel (payment page or a reader); the invoice's refunds show *Not confirmed by Stripe yet* with *Finish the refund*; the portal invoices gain *Pay by card* and a portal card page (`Portal/CardPaymentPage.razor`, `/customer/invoices/card/{chargeId}`, needs styling) |
| 2026-10-21 | Backend | Partner agents (Slice 7 step 4): *Invite a partner agent* panel on Staff (`#invite-partner`); *Partner agent · warehouse* on the staff list and the person's page (no role form); *The partner takes payments here* in a partner warehouse's destination fields; the pickup counter's owed section says only whether something is owed when the partner does not handle money |
| 2026-10-22 | FE | Operations customers workspace styling: Customer list (§5 list anatomy with single-line ops-list-bar, autofocus & clear, compact 36px table rows, ops-list-pager), Customer record (§5 record anatomy with top summary strip, 4-col label-over-value facts grid, inline single-line status form, credit account panel), Customer incoming packages (compact table replacing raw list), and Counter invoice note |
