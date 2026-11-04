# Slice 3B: Merchant receiving - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 3) |
| Date | 2026-10-18 |
| Built on | Slice 3A; not committed here: the dev team owns git |
| Design | [Slice 3 design note](SLICE-3-RECEIVING-DESIGN.md) sections 4, 7, 8, 10 and 11 (D-R9, D-R10); ADR-0019, ADR-0021, ADR-0022 |
| Status | **Built and verified by automated tests, reworked for the scan-only dock (owner, 2026-10-18); waiting for owner sign-off.** The inspection queue, rules and holds answered by the customer follow in 3C and 3E |

## 1. What was built

### The dock: scan app (`/staff/scan`, permission `ops.receiving.receive`)

Reworked on 2026-10-18 after the owner's direction: the dock only scans and labels (design note section 4.1, D-R9).

- **One app for every device:** a fixed station with a USB or Bluetooth scanner (the scanner types the barcode and Enter), a handheld, or a phone with its camera (the browser's own barcode reader, or the bundled ZXing 0.21.3 where there is none). It can be added to the home screen. The camera is allowed on this page only (`Permissions-Policy: camera=(self)`); every other page keeps it off.
- **Open the carrier's delivery:** UPS, FedEx, USPS, Amazon, DHL or another carrier. Pressing twice opens one delivery. A delivery left open on a device continues where it stopped.
- **Scan, label, next.** Each scan:
  - reads the tracking number out of the carrier barcode: USPS with its ZIP prefix, FedEx 34- and 22-digit codes, scanner prefixes such as `]C1`; a bare ZIP routing barcode is refused;
  - matches the customer by the customers' **open pre-alerts** only. Several customers announcing one number leave the parcel unidentified, so nobody can take a neighbour's package by announcing it too (review H1). Never by name;
  - creates the parcel (`PK` + year + six digits), joins the customer's open shipment (D-R1), records the events and holds (*unidentified*, *awaiting customer information*, *damaged* when the clerk pressed **Damaged** first) and writes the customer's email to the outbox, all in one save;
  - prints the **2 x 1 inch ID label**: the parcel ID as a Code 128 barcode and in print, the customer code large (or *UNIDENTIFIED*). On the dock printer through the print program, or on the device's own printer (browser kiosk printing);
  - beeps and shows the customer code large, then waits for the next package, in any order.
- **Repeats and mistakes:**
  - the same scan sent twice (a retry, the offline queue) counts once;
  - a package scanned again while it is at the warehouse prints its label again, never a second parcel; so does the parcel's own ID label scanned by mistake;
  - the camera reports a code on every frame: the same code within 2.5 seconds is one scan;
  - two clerks scanning one package at the same moment get one parcel (one parcel per number at a warehouse while it is there).
- **Without network** the scans wait on the device and leave in order when it is back. A refused one is shown, never dropped. A scan sent more than two minutes late prints no label (it would go on the wrong package) and the app says so: the clerk scans it again.
- **Close the delivery:** the app waits for scans still being sent, then the **warehouse receipt** is issued for the whole delivery: carrier, times, pieces and damaged pieces, and the list of parcels, as a Letter PDF for the driver. Closing twice gives the same receipt. A scan saved at the same moment as the close is never left off the receipt: both change the delivery row, so one of them counts again. A closed delivery takes no new package; a package already there can still get its label again.
- **`/staff/receiving`** lists the open deliveries and the receipts, with their PDFs.

### Inspection station (`/staff/inspection`, permission `ops.inspection.check`)

- The clerk scans the **ID label** (or the carrier barcode). The parcel, its customer's code, name and phone, and the other parcels of the shipment are shown.
- **Condition:** intact, damaged, wet, opened or re-taped, contents missing. Anything but intact puts the parcel on hold *damaged*.
- **Photos:** the package always the first time; the damage when the package is not intact; the label and the contents as needed. Up to six of 10 MB. Files of a refused or failed check are removed again.
- **Weight and size** in the company's units (stored in kg and cm). Numbers with a group separator, such as "1,200", are refused, not read as 1.2.
- **Random compliance draw:** on the first check about one parcel in twenty is selected (cryptographic random number, D-R4). The station says "set it aside" and the history records *inspection required (random)*. Rules and the queue follow in 3C.
- The station is ready for the next scan straight away.

### Label printers and the print program (`/staff/locations/warehouse/{id}/printers`, permission `ops.printers.manage`)

- **Printers** per warehouse: name, a network printer (`host:port`, raw ports 9100 to 9109 only) or a printer installed on the dock computer, 203 or 300 dpi, in use. **Print a test label** checks one.
- **The print program** (`Vixly.PrintAgent`, a Windows service on a dock computer):
  - asks the company's site for waiting labels every two seconds, with its own key;
  - sends ZPL to the printer;
  - reports printed or failed.
- **The key:** shown once when the program is registered and stored only as a hash. It works only on its own company's address and stops working at once when the program is stopped.
- **A label is printed at most once and only within two minutes of the scan:**
  - a label taken, failed or late is never handed out again;
  - the program itself skips a label that expired while it waited;
  - a lost label is printed again by scanning again.
- **The program never stops on an error:**
  - a proxy page, a dropped connection or a failed report is logged, and the next labels still print;
  - it sends only to addresses of the local network.
- **Reprint** from a parcel's page on any printer of its warehouse.

### Parcels (`/staff/parcels`, permission `ops.parcels.view`)

- **The list** searches by parcel ID, carrier tracking number (or its end) or customer code, with views *All*, *On hold* and *Unidentified*, filtered by warehouse.
- **Scope:** a warehouse grant shows that warehouse's parcels only. Another warehouse's parcel answers "not found", as a missing one does.
- **One parcel's page** shows:
  - its details, with the customer's declaration from the pre-alert;
  - the receipt number and condition;
  - the photos, loaded through a checked address;
  - the full staff history (every event, voided ones marked).
- **Assign** (`ops.parcels.assign`), for an unidentified parcel only:
  - the clerk enters the customer code and what the customer proved: the tracking number, the sender or the order number;
  - the history records it;
  - the parcel joins the customer's open shipment. If the customer had announced it meanwhile, that declaration is used; otherwise the customer is asked for it.
- **Put away** on an active shelf of the parcel's own warehouse, while the parcel is still there.
- **Shelves** (`/staff/locations/warehouse/{id}/storage`, `ops.warehouses.manage`) have:
  - a code such as `A-03-2`, unique in the warehouse and never changed once given;
  - a zone: receiving, inspection, storage, hold or outbound;
  - an in-use switch.
  - **Menu:** Workspace now reads Home, Receiving, Inspection, Parcels, Customers. Each warehouse on the Locations page links to its *Shelves* and *Printers*.

### Customer portal (`/customer/packages`)

- **Announce a package:** carrier, tracking number, store, what is inside, value (in the company's currency) and expected date. The same number twice by one customer is refused. An announcement can be withdrawn until the package arrives.
- **A package that arrived before its announcement takes it at once:** the description goes on the parcel and the *awaiting information* hold is released. An unidentified parcel is never given to a customer this way; a clerk checks first.
- **My packages** lists:
  - packages with their status and what is needed;
  - open announcements.
- **One package** shows:
  - status, warehouse, receipt number, weight and size in the company's units, declaration;
  - its photos;
  - its history in the customer's words.
  - It never shows shelves, staff names, notes or staff-only steps.
- **Ownership is checked on every read** (S-3). Another customer's package or photo answers "not found". Photos pass through the portal from Operations with `no-store`, `nosniff` and a sandboxing CSP.
- **Emails, through the outbox:**
  - "your package arrived", for a package that matched an announcement;
  - "your package needs your answer", for the others;
  - each with a link to the package page.

### Files (D-R7)

- Photos go through a file store with one folder per company. The store chooses every key, and a key names the store that wrote it, so a company's own cloud storage can plug in later without changing anything else.
- A read with a made-up key, another company's key or a path that leaves the folder returns nothing.
- Files of a refused or failed check are removed again.

## 2. Automated checks

Release builds: no errors, no warnings.

| Test project | Slice 3A | 3B, first build | 3B, scan dock |
|---|---|---|---|
| Operations.Domain.Tests | 104 | 128 | 161 |
| Operations.Application.Tests | 71 | 71 | 76 |
| Operations.Infrastructure.Tests (SQL Server) | 55 | 60 | 62 |
| Operations.Api.Tests (portal + API + SQL Server) | 19 | 20 | 20 |
| Operations.Web.Tests (SQL Server) | 55 | 65 | 67 |
| Customer tests | 40 | 40 | 40 |
| Architecture.Tests | 8,944 | 9,268 | 9,536 |

All pass, and the Platform and Public solutions still pass with the shared security headers change.

The global SQL script (32 migrations) runs twice on an empty server, and twice over a database built with the previous 3B script. That previous database held a parcel and a per-parcel receipt of the first build: the upgrade removes the old receipt and keeps the parcel, with no delivery. With the application login created by the script:
- an UPDATE or DELETE on `ops.WarehouseReceipts` is refused;
- an UPDATE on `ops.ParcelPhotos` is refused;
- writes to deliveries, print jobs, pre-alerts and parcels work.

What the tests cover:

- **Over HTTP, staff side, real database:**
  - the scan app page and its camera policy; the JSON calls refused without the form token;
  - a delivery opened once; scans by pre-alert (with spaces, lower case, USPS with its ZIP prefix), unidentified and damaged; a repeat, a reprint, the parcel's own label scanned again; noise and ZIP routing codes refused;
  - two customers announcing one number; one shipment per customer and warehouse;
  - the close: pieces and damaged pieces, the same receipt twice, the PDF, no new package afterwards;
  - the customer email through the outbox handler;
  - the inspection station: lookup by ID label, carrier barcode or a scanner prefix; the package and damage photo rules; weight and size stored; the random draw; the damaged hold;
  - printers: a refused port, a test label, a label per scan with the customer code, a reprint, at most once, a failed label not handed out again; the program's key refused for another company, when forged, when missing and once stopped;
  - the unidentified queue and search; assignment only with a recorded check; shelves, put-away, label PDF, photo headers;
  - a staff member without these permissions can open, scan, close, check and see nothing.
- **End to end, customer side:** unchanged from the first build (announce, withdraw, a parcel that arrived first, photos only for the owner, "1,200" refused).
- **Database:**
  - one delivery per opening reference; one receipt per delivery; only fresh pending labels handed to the print program, and none to another company;
  - one open pre-alert per customer and number; one open shipment per customer and warehouse; one shelf code per warehouse; one parcel per number at a warehouse until it leaves;
  - the active-parcel lookup across readings and warehouses; parcel lists keep to their warehouses and filters; another company sees nothing.
- **Domain:**
  - carrier barcodes: UPS, Amazon, USPS with a 5- or 9-digit ZIP, FedEx 34- and 22-digit codes, scanner prefixes, noise; the carrier named;
  - a delivery closes once and takes no parcel afterwards; its receipt counts its pieces;
  - printers refused for a bad name, address, port or resolution; a print job printed at most once and only while fresh;
  - the projection: received waits for the check, the check clears it, in any order of arrival;
  - the first build's rules: tracking numbers, measurements, declarations, shelf codes, pre-alerts, shipments.
- **Labels and files:** the 2 x 1 ZPL label at 203 and 300 dpi, nothing typed by a person reaches the printer as a command; the PDF label and a multi-page receipt render; Code 128; tenant folders and bad keys.
- **Texts:** every new error, scan, inspection, printer and receipt text in en, fr and es.

## 3. Independent review

A separate reviewer read the code without having written it, twice.

### First build

Its findings were fixed, each with a test:

- **H1 A pre-alert could take another customer's package.** Now several customers may announce one number, and a disagreement sends the parcel to the unidentified queue.
- **H2 Two clerks could receive one package twice**, or both match one pre-alert. Now the database allows one parcel per number at a warehouse while it is there, and pre-alerts and shipments carry row versions.
- **H3 Photos written by the staff site were not found by the portal.** `OperationsFiles:Root` is now required and shared by both applications.
- **M1** files of a failed receipt are removed; **M2** a double click or a lost race is recognised as the repeat it is; **M3** "1,200" is refused; **M5** a parcel takes only its own customer's pre-alert; **L1 to L4** query filter, portal photo CSP, required photo on the server, no put-away after departure.

### Scan dock

No high findings. Fixed, with tests where they can run here:

- **M1 A scan saved while the delivery closed could be left off the receipt.** Each scan now changes the delivery row, so a scan and a close made at the same moment clash and the close counts again; the app also waits for scans still being sent.
- **M2 A queued scan refused on replay was dropped silently.** Server errors and an expired form token are retried; other refusals are shown with the barcode.
- **M3 A queued scan whose first answer was lost printed no label in the browser and was not counted.** It is now counted and printed.
- **M4 A queued scan sent after an outage printed a late label.** After two minutes a scan is sent without a label and the app says "scan it again".
- **M5 The camera printed the same label again every 2.5 seconds** while a barcode stayed in view. The window now slides with every sighting.
- **M6 Scanning the parcel's own ID label, or a ZIP routing barcode, at the dock made a bogus parcel.** The ID label now prints again; routing codes are refused.
- **M7 The print program could stop on a proxy page, and lost the rest of a batch when a report failed.** Every error is caught per label; the report failing never holds up the next label.
- **L1** after a race the scan looks again at every reading of the barcode; **L2** the inspection lookup handles scanner prefixes and finds a parcel in one query; **L3** the program skips a label that expired while it waited; **L4** network printers use raw printing ports 9100 to 9109 only, and the program sends only to local-network addresses.

**Noted, not changed:**
- A receipt or parcel number taken by a save that then failed leaves a gap. Gaps are normal for such sequences and are not reused.
- Photos are stored as sent (no resizing yet).
- The scale and the dimensioner plug into the station's fields later.
- Silent browser printing needs the browser's kiosk printing on the dock device; otherwise the print dialog opens for each label.

## 4. Deployment notes

- **Database:** run the newest global SQL script (migrations `Receiving` and `ReceivingStations`; the grants are in section 5b).
  - `ReceivingStations` removes the per-parcel receipts of the first 3B build: receipts now belong to carrier deliveries. Run it before anyone receives packages for real.
- **New required setting:** `OperationsFiles:Root`.
  - It is the folder for parcel photos, on Operations.Web **and** Operations.Api, and must be the same folder for both.
  - Keep it outside the web root and back it up with the database.
  - The applications refuse to start without it.
  - Development uses `.files` at the repository root (ignored by git).
- **Optional setting:** `OperationsDocuments:QuestPdfLicense` on Operations.Web, as in Platform (empty means Community).
- **Roles:**
  - the Owner and administrators have the new permissions;
  - other roles need the ones they use added in Roles: *Receive packages and put them away*, *Check parcels: condition, weight, size and photos*, *See the parcels, their photos and history*, *Give an unidentified parcel to its customer*, *Set up label printers and print programs*.
- **HTTPS:** the phone camera works only over HTTPS (as in production).
- **Dock devices:**
  - a fixed station or handheld opens `/staff/scan` in a browser;
  - for silent label printing on that device, start the browser in kiosk printing mode (Chrome or Edge: `--kiosk-printing`) with the 2 x 1 inch label printer as the default printer;
  - otherwise use a dock printer through the print program.
- **Print program:** see `src/Operations/Vixly.PrintAgent/README.md`. Register it on the warehouse's Printers page and set `PrintAgent__OperationsUrl` and `PrintAgent__AgentKey` as service environment variables. The key is shown once.
- **Request size:** the inspection form accepts up to about 61 MB (six photos). A proxy in front of Operations.Web must allow that on `/staff/inspection`.
