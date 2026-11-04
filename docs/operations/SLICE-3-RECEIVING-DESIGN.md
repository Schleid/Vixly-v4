# Slice 3: Receiving, inspection and tracking - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 3, ADR-0017) |
| Date | 2026-10-18 |
| Builds on | Slices 1 and 2; ADR-0018 (milestones), ADR-0021 (tracking events), ADR-0022 (receiving and inspection) |
| Status | **Owner answers received and settled 2026-10-18** (sections 9 and 11). 3A delivered; 3B built and reworked for the scan-only dock (see [verification](SLICE-3B-VERIFICATION.md)); 3C next |

## 1. Outcome

- At the dock a clerk only **scans**: each package's carrier barcode is matched to its customer and its **ID label** prints at once. When the carrier's delivery is all scanned, one **warehouse receipt** (`WR…`) covers it (owner, 2026-10-18; section 11).
- At the **inspection station** each package is checked, weighed, measured and photographed, and some are drawn for compliance inspection. Then it is put on a shelf.
- Every package passes a **condition check**; some get a **compliance inspection**. Problems become **holds** that tell the customer what to do.
- Customers **pre-alert** packages, see "your package arrived" with photos, and answer holds on the portal.
- Every step is a **tracking event** (ADR-0021): one history for staff, customers and, later, public tracking.
- **A parcel always belongs to a shipment** (owner, 2026-10-18). A package received from a merchant starts or joins the customer's open shipment at that warehouse (ADR-0018 owner answer); the shipment is booked (service, destination, charges) in Slice 5. Charges and payment are not part of this slice.

## 2. Sub-slices (each verified and delivered on its own, as in Slice 1)

| Sub-slice | Content | Waits for |
|---|---|---|
| **3A Tracking core** | Event catalogue, append-only event store, idempotency keys, milestone and flag projection on the package, void events; staff history view | nothing |
| **3B Merchant intake** | Pre-alerts (customer portal), the scan app and carrier deliveries, the match, the delivery receipt, the 2 x 1 ID label and the dock printers, the inspection station (condition, photos, weight and size, random draw), storage locations and putaway, the unidentified queue | Q-R1, Q-R3, Q-R4 |
| **3C Inspection and holds** | Condition check, compliance rules and the random share, the inspection queue per warehouse, outcomes, holds with reasons, the restricted-item list | Q-R5 |
| ~~3D Counter drop-off~~ | Moved to Slice 5 (owner, 2026-10-18; section 11) | |
| **3E Customer side and unclaimed** | "Received" notices with photos, holds answered on the portal (document, value, decision about a damaged package), reminders, unclaimed and unidentified expiry with staff approval | Q-R6, Q-R7 |

## 3. Data (schema `ops`, every table with `TenantId`)

| Class | Table | Notes |
|---|---|---|
| `OperationsTrackingEvent` | `TrackingEvents` | Append-only (the application login gets no UPDATE or DELETE). Subject type and id, code, arguments (JSON, typed per code), occurred time with time zone, recorded time, location code, actor and device, source, visibility, idempotency key (unique per tenant), voided-event id, note |
| `OperationsParcel` | `Parcels` | One physical package, belonging to a shipment. Customer (empty while unidentified), carrier and carrier tracking number, weight and dimensions (kg and cm, shown in the tenant's units), declared contents and value, **milestone and flags as a projection** of its events, current warehouse and storage location |
| `OperationsShipment` (opened in 3B, booked in Slice 5) | `Shipments` | Customer, origin warehouse, open or booked; its parcels. Service, destination, charges and documents arrive with Slices 4 and 5 |
| `OperationsPreAlert` | `PreAlerts` | Written by the customer: carrier, tracking number, store, contents, value, expected date. Matched to a package on receipt. One open pre-alert per customer and number; several customers may announce one number (section 4) |
| `OperationsInboundDelivery` | `InboundDeliveries` | One carrier's drop at a warehouse: carrier, opened by and when, open or closed, its receipt. Every parcel names the delivery it came with |
| `OperationsWarehouseReceipt` | `WarehouseReceipts` | One per delivery, issued when it is closed. Number from the tenant sequence (kind `Receiving`, prefix in settings). Who, where, carrier, opened and closed times, pieces and damaged pieces. Never changed once written (the application login has no UPDATE or DELETE) |
| `OperationsLabelPrinter` | `LabelPrinters` | Per warehouse: name, connection (network address and port, or a printer installed on the dock computer), 203 or 300 dpi, in use |
| `OperationsPrintAgent` | `PrintAgents` | A dock print program: name, warehouse, hash of its key, last seen, stopped |
| `OperationsPrintJob` | `PrintJobs` | One label for one printer (ZPL built by Operations); pending, taken, printed or failed |
| `OperationsParcelPhoto` | `ParcelPhotos` | Kind (label, package, damage, contents), file key, type, size and SHA-256, taken by and when. The file itself lives in the file store (Q-R4, D-R7); the row is never changed |
| `OperationsStorageLocation` | `StorageLocations` | Per warehouse: code (for example `A-03-2`), zone (receiving, inspection, storage, hold, outbound), active |
| `OperationsInspection` | `Inspections` | Package, kind (condition, compliance), reason chosen (rule, random, flagged), result, notes, inspector, photos |
| `OperationsInspectionRule` | `InspectionRules` | Tenant rule: category, destination country, declared value threshold, carrier; always inspect |
| `OperationsRestrictedItem` | `RestrictedItems` | Tenant list: item, prohibited or restricted, destinations, carriers, what is needed (permit, document) |
| `OperationsHold` | `Holds` | Package, reason (ADR-0018 list), what the customer must do, opened and closed by and when, outcome |

## 4. Merchant intake (3B)

Owner direction (2026-10-18): in a busy warehouse there is no time to type. The clerk scans and sticks on labels; nothing else happens at the dock.

### 4.1 The dock: scan, label, receipt

1. **Open the carrier's delivery** in the scan app (`/staff/scan`): choose the carrier (UPS, FedEx, USPS, Amazon, DHL or other). The app runs on a fixed station with a USB or Bluetooth scanner, on a handheld, or on a phone with its camera. Pressing twice opens one delivery.
2. **Scan each package's carrier barcode**, in any order. The system reads the tracking number out of the barcode (USPS with its ZIP prefix, FedEx 34- and 22-digit codes, scanner prefixes) and matches the customer:
   - by the customers' **open pre-alerts** with that number (ADR-0022);
   - several customers may announce one number, so nobody can block or take another's package by announcing it first. When more than one customer announced it, nobody gets the parcel (review of 3B);
   - never by name alone (owner, Q-R1). Nothing matched: the parcel is **unidentified** (hold *unidentified*) and goes to the unidentified queue. A clerk assigns it only after the customer identifies the package (carrier tracking number, sender or order number); the assignment event records what was verified.
3. **One scan, one label.** Each scan prints the parcel's **ID label** at once (section 11, point 3), so the clerk never looks for the next package of a customer's batch: whichever package comes next gets the next label.
   - 2 x 1 inch: the parcel ID (`PK…`) as a Code 128 barcode and in print, and the customer code large for sorting by eye (or *UNIDENTIFIED*).
   - Printed on the dock printer through the print program, or by the device's own browser printer.
   - The same scan sent twice (a retry, the offline queue) counts once. A package scanned again while it is at the warehouse prints its label again; it never becomes a second parcel. The database allows one such parcel, so two clerks scanning at once still get one.
4. **Damage seen at the dock**: the *damaged* key before the scan opens the hold *damaged* at once and counts the package as damaged on the receipt.
5. **Close the delivery**: the **warehouse receipt** is issued for the whole delivery (pieces, damaged pieces, the list) as a PDF for the driver. Closing twice gives the same receipt. A closed delivery takes no new package.
6. **Matched without a pre-alert** (assigned later): the customer is asked for contents and value (hold *awaiting customer information*, answered on the portal in 3E).

Each parcel joins the customer's open shipment at the warehouse (D-R1). Events written: `pre_alerted` (the customer's announcement, at its own time), `received_at_origin` (milestone 1; the parcel now waits for its check), `hold_opened`; later `customer_assigned` and `hold_released` for an unidentified parcel given to its customer.

### 4.2 The inspection station

1. The clerk **scans the ID label** (or the carrier barcode) at `/staff/inspection`: the parcel, its customer (code, name, phone) and the other parcels of the shipment are shown.
2. **Condition**: intact, damaged, wet, opened or re-taped, contents missing. Anything but intact puts the parcel on hold *damaged* and needs a photo of the damage.
3. **Photos**: the package always the first time; the label, the damage and the contents as needed.
4. **Weight and size**, in the company's units (stored in kg and cm). The scale and dimensioner plug into the same fields later.
5. **Random compliance draw**: on the first check, about one parcel in twenty (D-R4) is selected; the station says "set it aside". Rules and the inspection queue follow in 3C.

Events written: `photo_added`, `condition_recorded`, `checked` (weight and size), `hold_opened`, `inspection_required` (why: random).

### 4.3 Putaway and later

**Putaway**: choose the storage location on the parcel's page; a package needing inspection goes to the inspection zone first.

A pre-alert written after its package arrived is matched at once: the customer's description goes on the parcel and the *awaiting customer information* hold is released. An unidentified parcel is never given to a customer this way; a clerk checks first.

## 5. Inspection and holds (3C)

- **Condition check** on every package at the inspection station (section 4.2), photo required.
- **Compliance inspection** when a tenant rule matches, when the random draw picks it (tenant percentage, D-R4), or when staff flag it. The package shows *InspectionPending* until done.
- **Queue**: each warehouse sees its packages waiting for inspection, oldest first.
- **Outcomes**: passed; passed with notes; held for the customer (document, permit, value, fee); repacked; refused and returned to sender; refused and disposed of (customer consent or the tenant's terms, staff approval).
- **Holds block** consolidation, dispatch and handover (ADR-0018). In this slice nothing consolidates yet; the rule is enforced by the projection so Slices 5 to 8 inherit it.
- **Restricted list**: each tenant keeps its own. Vixly provides a starting list the tenant copies and edits (D-R5).

## 6. Counter drop-off (3D)

Accept at the counter, inspect afterwards (ADR-0022). Owner procedure (2026-10-18):

1. The clerk asks for the customer's **phone number**, finds the customer and verifies their details with them. A walk-in without an account registers first (D-C6: staff never create customers).
2. The clerk registers the **shipment** (service, destination), then its **parcels**: each is weighed and measured, and those facts go into the system so it can produce the invoice.
3. Each parcel gets an ID barcode.
4. The parcels go to inspection; the shipping labels go on after inspection.

Built in Slice 5, after the charges engine (section 11).

## 7. Customer side (3B and 3E)

| Page | What it does |
|---|---|
| `/customer/packages` | Pre-alerts and packages, newest first, with milestone and holds |
| `/customer/packages/new` | Pre-alert: carrier, tracking number, store, contents, value, expected date |
| `/customer/packages/{id}` | History (customer-visible events, in the customer's language), photos of their own package, what to do about a hold |

- Ownership is checked on every read (S-3): a customer sees only their own packages and photos.
- Photos are served through Customer.Web from Operations, never by a guessable address.
- Emails: "your package arrived" (with the receipt number), "action needed" for a hold, unclaimed reminders.

## 8. Staff side

| Page | Permission (new) |
|---|---|
| `/staff/scan` (the scan app) and `/staff/receiving` (deliveries and receipts) | `ops.receiving.receive` |
| `/staff/parcels` and `/staff/parcels/{id}` (history, photos, holds, label) | `ops.parcels.view` |
| `/staff/parcels/unidentified` (assigning needs `ops.parcels.assign`) | `ops.parcels.view` |
| `/staff/inspection` (the inspection station, 3B) | `ops.inspection.check` |
| `/staff/inspection` queue and outcomes (3C) | `ops.inspection.perform` |
| `/staff/locations/warehouse/{id}/printers` (label printers and print programs) | `ops.printers.manage` |
| `/staff/holds` | `ops.holds.manage` |
| `/staff/locations/warehouse/{id}/storage` | `ops.warehouses.manage` (existing) |
| `/staff/settings/inspection` (rules, random share, restricted list, unclaimed days) | `ops.inspection.configure` |
| `/staff/counter` (3D) | `ops.counter.accept` |

Unlike customers (D-C6), parcels are scoped by place: a warehouse grant gives these permissions for that warehouse's parcels (the scope rule of Slice 1D). Staff pages say *parcel*; customer pages say *package*.

The menu's **Workspace** group gains Receiving, Inspection and Parcels (3B), then Holds (3C).

## 9. Questions for the owner

*Answered 2026-10-18: Q-R1 no (D-R8); Q-R2 and Q-R3 see sections 6 and 11 (counter drop-off moves to Slice 5; ID barcode at intake, shipping label after inspection); Q-R4 yes, with each tenant able to use its own cloud storage (D-R7); Q-R5, Q-R6 and Q-R7 agreed as proposed.*

1. **Q-R1 Matching by name.** When neither a pre-alert nor a customer code matches, may the clerk assign a package to a customer found by **name alone**, or must name matches go to the unidentified queue for a second person to confirm? Proposed: the clerk may assign, and the assignment is a tracking event the customer is told about.
2. **Q-R2 Counter drop-off and D-C6.** ADR-0022 said "find or create the customer" at the counter; D-C6 now says staff never create customers. Is the proposal in section 6 right: a walk-in registers on their own phone (or a counter tablet) first, and the clerk records only the package facts? Do your counters have signature pads, or is a "customer confirmed" tick enough?
3. **Q-R3 Package number and label.** Proposed: each package gets a number `PK` + year + six digits from the tenant sequence (prefix in settings like the others), printed as a Code 128 barcode on a 4 x 6 inch thermal label (PDF), with the customer code in large type. Do your warehouses use 4 x 6 thermal printers?
4. **Q-R4 Photo storage.** Photos are many and large, so they should not live in the database. Options: (a) a folder on the web server (simplest; backups must include it); (b) object storage (Azure Blob, Amazon S3 or Cloudflare R2; costs a little, scales, survives a server move). Which does your production hosting allow? Proposed: (a) behind an interface, so (b) is a configuration change later.
5. **Q-R5 Inspection defaults.** Proposed defaults: random share 5 percent; a starting restricted list covering lithium batteries, aerosols, flammable liquids, medicine, firearms and parts, currency, perishable food, and items needing a permit. Tenants change both. Agreed?
6. **Q-R6 Unclaimed and unidentified timing.** Proposed: reminders to the customer at day 7, 14 and 21, final notice at day 25, and at day 30 (tenant setting) the package goes to a staff approval list to return to sender or dispose of. Unidentified packages: the same 30 days from receipt. Agreed?
7. **Q-R7 Holds the customer can answer online.** Proposed for this slice: upload a document or invoice (PDF, PNG, JPEG, 10 MB), confirm contents and value, and choose for a damaged package (ship as is, return to sender, or dispose of). Paying a hold fee waits for Slice 9. Agreed?

## 10. Decisions

- **D-R1** (owner) A parcel always belongs to a shipment. A merchant package starts or joins the customer's open shipment at the receiving warehouse; a counter drop-off books the shipment first.
- **D-R2** One `OperationsParcel` per physical box; it is the subject of the tracking events and carries the projection.
- **D-R3** Each parcel gets a simple ID barcode (`PK` + year + six digits from the tenant sequence; the prefix is a tenant setting) at intake: a 2 x 1 inch Code 128 label with the customer code large (owner, 2026-10-18). The owner proposed the customer's phone number as the barcode; settled on the parcel ID, because one customer has many parcels and each must be told apart at inspection and later. The phone stays the search key at the counter.
- **D-R7** (owner, Q-R4) Photos and documents go through a file store that each tenant may point at its own cloud storage (Azure Blob, Amazon S3 or Cloudflare R2, credentials kept encrypted). The default is a folder on the server (`OperationsFiles:Root`, shared by Operations.Web and Operations.Api). Built in 3B: the store chooses every key and writes the provider name into it, so a file is always read from where it was written; a tenant's own cloud provider plugs in by name later.
- **D-R8** (owner, Q-R1) Never assign a package by name alone.
- **D-R4** Random inspection share per tenant, default 5 percent, drawn with a cryptographic random number at the first check so it cannot be predicted.
- **D-R9** (owner, 2026-10-18) The dock only scans: one scan, one label, packages in any order; one receipt per carrier delivery. Checking, weighing, measuring and photographing happen at the inspection station.
- **D-R10** Dock printers are reached through a small print program on a dock computer, which asks Operations for labels with its own key. A label is printed at most once and only within two minutes, since a late label would go on the wrong package; a lost label is printed again by scanning again.
- **D-R5** Vixly ships a starting restricted list in code; a tenant copies it on first use and owns its copy.
- **D-R6** Tracking events are the only way a package's milestone or flags change; the projection is written in the same database transaction as the event.

## 11. Settled with the owner (2026-10-18)

1. **Counter drop-off moves to Slice 5** (shipments), after the charges engine, so the counter makes the shipment, its parcels and the invoice in one visit. An invoice is built from the service charges and the parcels' data (weight, size, declared value).
2. **Insurance** is part of the shipment's charges when the customer asks for it, alongside the service and consolidation (Slices 4 and 5).
3. **Labels:** every parcel gets its ID barcode at intake; the shipping label (service, destination, tracking number) goes on after inspection, when the parcel is cleared to ship.
4. **The dock and the inspection station** (later the same day): the receiving clerk only scans and labels, and gives the driver one receipt per delivery; the inspection station checks condition, weighs, measures, photographs and draws packages for compliance. The scan app works on fixed stations and handhelds, with USB, Bluetooth or Wi-Fi scanners and with a phone camera. The ID label is 2 x 1 inch (D-R3).

## 12. Done when (each sub-slice adds its own list in its verification record)

- A double scan or a replayed command never creates a second event or package.
- The package's milestone and flags always equal a full replay of its events.
- A customer sees only their own packages, photos and customer-visible events, in their language; never another tenant's.
- A warehouse-scoped clerk sees and changes only that warehouse's packages.
- en, fr and es complete; no "Vixly" on any customer page or email.
