# Slice 4D: Origin intake - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-19 |
| Built on | Slice 4C; not committed here: the dev team owns git |
| Design | [Slice 4D design note](SLICE-4D-ORIGIN-INTAKE-DESIGN.md), approved by the owner on 2026-10-19 with Q-D1 to Q-D5 as proposed |
| Status | **All four steps built and verified by automated tests: 1 (pre-alerts first, shipments, incoming list), 2 (shipping labels), 3 (undeclared parcels), 4 (tickets for a missing package); waiting for owner sign-off** |

Slice 4D is delivered in steps, each verified on its own:

| Step | Content | Status |
|---|---|---|
| 1 | Pre-alerts first, any number of shipments, packages moved between them, the waiting shipment, the service, the incoming list | Built (this record) |
| 2 | The shipping label at inspection, labels to print | Built (section 5) |
| 3 | Undeclared parcels: inventory, aging, storage fee, return, disposal or auction, the count | Built (section 6) |
| 4 | Tickets for a missing package | Built (section 7) |

## 1. Step 1: what was built

### For the customer (`/customer/packages`)

- **Announce a package** on its own (`/customer/packages/new`): the package, the warehouse it is shipped to (asked only when the customer has several mailbox addresses) and, optionally, one of its shipments. Without a shipment it shows **"Not in a shipment yet"**.
- **Shipments** (`/customer/packages/shipments/new`): where it goes, the recipient, the **service** (air, ocean, ground... only those the company prices for the customer; or "let the company choose"), the options, and:
  - the customer's packages in no shipment yet, ticked to put them in;
  - optionally a new package typed with it;
  - or nothing: goods the customer drops off (the counter takes them in Slice 5).
- **Any number of open shipments** at once, to different places (the owner's cardboard maker: cartons to the Dominican Republic, Amazon tape to Peru).
- **Moving packages**: each pre-alert and each package still at the warehouse has a "Shipment" choice (its customer's open shipments from that warehouse, or none). A shipment page has "Take it out" on each package and "Put it in this shipment" for the packages in no shipment.
- **Cancelling** a shipment none of whose packages arrived keeps its pre-alerts, in no shipment (they used to be withdrawn).
- **The waiting shipment** (packages that arrived but are in none of the customer's shipments) is shown as "Packages waiting for a shipment"; it is never given instructions or sent: its packages are moved into the customer's shipments.
- A shipment form sent twice (a double click past the browser guard) creates one shipment (ADR-0019).

### At the dock

- A package goes into **the shipment its pre-alert is in** (open, with instructions, from this warehouse). It is never placed by guesswork.
- A pre-alerted package whose pre-alert is in no shipment (or in one from another warehouse) joins the customer's waiting shipment with the hold **awaiting shipment**; the customer is asked to put it in one of its shipments. The pre-alert's declaration (contents, value) is on the parcel at once.
- Received at another warehouse than announced: the pre-alert follows the package to that warehouse.
- A package that arrived first and is announced later takes the declaration; it stays in the customer's shipment if it is already in one, otherwise it waits for one.
- Staff assigning an unidentified package follow the same rules.

### For staff

- **Incoming** (`/staff/parcels/incoming`, parcels: view, the person's warehouses): every pre-alert with customer, carrier, tracking number, contents, expected day, warehouse, the parcel once received and whether it is in a shipment.
  - Views with their counts: **expected**, **past expected day**, **received**, all. Filters: expected from and to, carrier, tracking number or customer code, warehouse.
  - **Export (CSV)**, up to 5,000 rows; a cell a spreadsheet would read as a formula is made plain text.
- The parcel page shows the shipment's service.

## 2. Database

- `ops.PreAlerts.WarehouseId` (linked to the company's own warehouses only), indexed with the status and expected day for the incoming list.
- `ops.Shipments.ServiceCode`, `ops.Shipments.RequestKey` (one shipment per form, unique per customer).
- Migration `20261019222905_OriginIntake`: each existing pre-alert gets its warehouse (its shipment's origin, else where its package arrived, else the company's first receiving warehouse; a company with none leaves it empty).
- In the global script `build/scripts/sql/vixly-v4-database.sql` (19 Platform, 19 Operations, 1 Customer migrations). Tested on an empty server (run twice) and from the Slice 4 database upgraded to 4B, 4C (with pre-alerts in and out of shipments) and 4D (run twice): each pre-alert got the right warehouse.

**On the development machine: run the global SQL script once before starting the sites.**

## 3. Automated tests

| Test | What it proves |
|---|---|
| `PreAlerts_ComeFirst_AndMoveBetweenTheCustomersShipmentsFromTheirWarehouse` (Domain) | A pre-alert for a warehouse; into one shipment, then another, then out; never into another customer's shipment, one from another warehouse or the waiting shipment; received elsewhere, it follows the package; the service on the instructions |
| `Customer_AnnouncesFirst_ThenPutsPackagesIntoSeveralShipments_AndMovesThem` (Api) | Announce on its own; a shipment that takes it at once, another with no package; moved between them and out; another customer cannot move it; "back" never leaves the customer's pages; a package arriving first waits for a shipment and its hold ends when put in; the waiting shipment cannot be given instructions; a parcel never moves with another pre-alert; a package already in a shipment stays there when declared later |
| `Customer_CreatesShipments_AnnouncesPackages_AndSeesOnlyTheirOwn` (Api, updated) | The announce page; a cancelled shipment keeps its pre-alerts |
| `AnnouncementsInNoShipment_StillCount_AndAPackageAtAnotherWarehouse_IsRefused` (Api, updated) | A pre-alert in no shipment still blocks a second announcement of the number |
| `EveryCustomerForm_SentTwiceAtOnce_NeverFails` (Api, updated) | The shipment form sent twice at once creates one shipment |
| `Dock_PlacesAPackageInItsPreAlertsShipment_OrLetsItWait_AndIncomingListsEveryPreAlert` (Web) | Two shipments open: the scan puts the package in the one its pre-alert is in; a pre-alert in no shipment: the waiting shipment and the hold, with the declaration; the incoming views and counts; the CSV export, a formula kept as text |

All suites green: Operations, Platform, Public and the architecture tests.

## 4. Independent review of step 1 (2026-10-19)

An agent that had not seen the work reviewed it. Tenant isolation, ownership checks, antiforgery, the "back" address, CSV quoting and the row-version races were found sound. Its findings and what was done:

| # | Finding | Done |
|---|---|---|
| 1 | The waiting shipment could still be given instructions, leaving its packages on hold | Refused ("These packages wait for a shipment..."); its page offers each package's shipment choice and a new shipment instead |
| 2 | Announcing on its own a package already in one of the customer's shipments pulled it back to the waiting shipment; one past its origin could not be declared | It stays in its shipment; a package past its origin never moves |
| 3 | A crafted request could move a parcel with another pre-alert, splitting a pre-alert from its parcel | One package per request; a parcel always moves with its own pre-alert |
| 4 | An assigned package got "awaiting shipment" without a pre-alert, unlike the dock | The same rule in both: only a pre-alert in no shipment waits for one |
| 5 | The pre-alert's warehouse changed before its status was checked | Checked first |
| 6 | The pre-alert's warehouse link did not include the company | Linked on company and warehouse |
| 7 | Incoming counts ignored the filters; the export limit was silent | Counts follow the filters; the page says the export holds up to 5,000 rows (customer codes have no dashes, so the search is right) |
| 8 | The announce page could be sent without a mailbox; packages of every warehouse were offered on a new shipment; a double click made two shipments | No submit without a mailbox address; each package shows its warehouse when there are several; one shipment per form |

## 5. Step 2: the shipping label

### What was built

- **Printers hold ID labels or shipping labels.** Each printer of a warehouse says which (`/staff/locations/warehouse/{id}/printers`): parcel ID labels, 2 x 1 inch, at the dock, or **shipping labels, 4 x 6 inch**, at inspection. Existing printers hold ID labels. The dock scan and the ID label print use ID label printers only; a test print on a shipping label printer prints a sample shipping label.
- **Cleared** means: checked at the station, no hold, no compliance inspection waiting, still at its warehouse, and in one of the customer's shipments (not the waiting shipment).
- **At the inspection station** the clerk picks the station's shipping label printer. A parcel cleared by its check gets its shipping label at once ("PK... checked. Stick its shipping label on.").
- **Labels to print** (`/staff/parcels/labels`): parcels cleared later (a hold answered, an inspection passed, put in a shipment by the customer) whose label was never printed, oldest first, with their shelf. Print one, all of a warehouse at once (up to 50; one printed meanwhile by another clerk is skipped), or download it.
- **The parcel page** shows the shipping label: why not yet, or when it was last printed, with print and download (PDF, 4 x 6 inch; downloading marks it printed).
- **The label**: the company, the route large (origin and destination codes, MIA > JER), the recipient's name and city, pickup or home, the service, the weight, the piece (2 / 4), the shipment number with its barcode and the parcel ID with its barcode. No price and no phone or email.
- **Shipment numbers** (SH + year + sequence) are given when the customer creates a shipment; an older shipment gets its number with its first label.
- A label goes back to the labels to print when it no longer fits: the parcel moved to another shipment, the customer changed the shipment's instructions, or a new weight at the station.
- Names typed by people reach the printer as text only: the ^ and ~ that start printer commands are removed.

### Database

- `ops.LabelPrinters.Media` (1 = ID labels, the default for existing printers; 2 = shipping labels).
- `ops.Shipments.Number` (unique in a company), `ops.Parcels.ShippingLabelUtc` (indexed with the warehouse for the labels to print).
- Migration `20261019224620_ShippingLabels`, in the global script (19 Platform, 20 Operations, 1 Customer migrations); tested on an empty server and over the step 1 database, each run twice.

### Automated tests

| Test | What it proves |
|---|---|
| `ShippingLabel_HasTheRouteBarcodesAndPiece_AndNoCommandFromATypedName` (Application) | 4 x 6 at 203 and 300 dpi; the route, both barcodes, the piece; a name with ^XZ and ~JR prints as text |
| `ShippingLabel_IsAPdf` (Infrastructure) | The PDF label renders |
| `Inspection_PrintsTheShippingLabelOfAClearedParcel_AndTheOthersWaitInLabelsToPrint` (Web) | The station offers only shipping label printers; a cleared parcel's label prints at once with its shipment number (SH...) and 1 / 1; a parcel in no shipment gets none and says why; a dock printer refuses a shipping label; put in the shipment, it waits in the labels to print (a company with one warehouse prints all at once); printed, the list is empty; the second label reads 2 / 2; the PDF |

### Independent review of step 2 (2026-10-19)

Scope checks (printers of the parcel's warehouse and kind, the company's parcels), the redirects and texts in the address, the ZPL text and the migration defaults were found sound. Its findings and what was done:

| # | Finding | Done |
|---|---|---|
| 1 | A label stayed "printed" after the customer changed the shipment's instructions, or a new weight | Back to the labels to print in both cases |
| 2 | An ID label could be sent to a shipping label printer (dock scan, reprint) | ID labels only on ID label printers; the scan station lists only those |
| 3 | "Print them all" never showed for a company with one warehouse | Its warehouse is chosen by itself |
| 4 | "Print them all" could print a label twice when another clerk printed it meanwhile, and said "all" when 50 of more were sent | A parcel printed meanwhile is skipped; the page says how many were sent and the rest stay listed |
| 5 | The labels to print and the parcel page disagreed on "cleared" after the parcel was booked | One rule: received and not yet departed |
| 6 | An older shipment numbered at its first label can make a station check clash with another one at the same moment | Kept: only shipments created before this step; the clerk sends the check again |
| 7 | Printing needs only "parcels: view" | Kept, as for the ID label; a permission of its own can come with dispatch (Slice 5) |
| 8 | Printing from the labels to print lost the warehouse chosen; a failed download went to the parcel page | Both go back to the list with its warehouse |

## 6. Step 3: undeclared parcels

### What was built

- **The undeclared parcels area.** Shelves get a new zone, *undeclared parcels*. The parcel views read **Undeclared** (packages nobody could place).
- **What the label shows.** On an undeclared package's page the clerk records the name and the mailbox code written on the label, and the sender. When the code is one of the company's customers, the page says so (with the customer's name only for staff who may see customers): check with them, then assign with the new proof **"the mailbox code on the label"**, accepted only when the recorded code is that customer's. The undeclared list finds a package by that code, or by a name or sender written on it.
- **Storage fee** (holds page, Owner or administrator, Q-D3): **free days** (default 30) and a **fee per day** after them (none by default). A package keeps the terms in force when it arrived. The fee so far shows on the staff parcel page and on the customer's package page while the package waits at the warehouse; it is charged with the shipment's charges at dispatch (Slice 5).
- **Auction** (Q-D4): after the company's days, the Owner or an administrator decides **return to sender**, **dispose of** or now **sold at auction**, with a note (who bought it, for how much). Every hold ends and the history says "Sold at auction"; the company runs the auction.
- **The count** (`/staff/parcels/counts`, parcels: assign): start the count of a warehouse (one open at a time): every undeclared package checked at the station, with its shelf. Scan or pick each one **found**, or mark it **missing** with a note. A package given to its customer or decided meanwhile is set aside at closing. A count with a package missing is closed by the Owner or an administrator. Each mark is in the audit; recording the label is a tracking event.

### Database

- `ops.TenantSettings.StorageFreeDays` (default 30), `StorageFeePerDay` (none).
- `ops.Parcels.LabelName`, `LabelCode`, `LabelSender`, `StorageFreeDays`, `StorageFeePerDay` (the terms when it arrived).
- `ops.StockCounts` (the count, its lines as JSON; one open count per warehouse).
- Migration `20261019233012_UndeclaredParcels`, in the global script (19 Platform, 21 Operations, 1 Customer migrations); tested on an empty server and over the step 2 database, each run twice.

### Automated tests

| Test | What it proves |
|---|---|
| `StorageFee_IsOwedForEachDayPastTheFreeDays_OnlyWhenTheCompanySetsOne` (Domain) | The days and fee past the free days; none without a fee; the limits of the terms |
| `Label_IsRecordedWithinLimits_TheCodeInCapitals` (Domain) | The label text, its limits |
| `Count_ClosesWhenEveryParcelIsMarked_AMissingOneWithANoteAndByAManager` (Domain) | Marked, closed only when complete; missing needs a note and a manager; a clean count closed by anyone |
| `UndeclaredParcels_AreInventoried_AssignedByTheirLabel_Counted_Charged_AndSoldAtAuction` (Web) | The label recorded; the customer the code points to; found by code and by sender; assigned with "the mailbox code on the label"; a count found by scan and closed; a second count with one missing (note, then the Owner); the fee after 15 days on the terms of its arrival, not the later ones; sold at auction after 35 days |

### Independent review of step 3 (2026-10-19)

Tenant scope (the customer code lookup, the counts, the label search), the Owner-only settings, the auction's holds and texts and the migration defaults were found sound. Its findings and what was done:

| # | Finding | Done |
|---|---|---|
| 1 | The fee was worked out from today's terms, so a change applied backwards | Each package keeps the terms in force when it arrived (older ones use the company's current terms) |
| 2 | The fee would also run on a booked package waiting for the company | Only while it waits at the warehouse before booking |
| 3 | "The mailbox code on the label" could be chosen for any customer | Only when the recorded code is that customer's |
| 4 | Staff who may not see customers could learn names by typing codes | The name is shown only to staff who may see customers; others read "one of our customers" |
| 5 | The search on the label had no length limit | Cut at 100 characters, the column's length |
| 6 | The count included packages still at the dock or in inspection | Only those checked at the station |
| 7 | A package assigned during an open count had to be marked missing | Set aside as "no longer in the area" at closing |
| 8 | Marks and recorded labels left no trace | Each mark audited; recording the label is a tracking event |
| 9 | The label form showed on packages already gone | Only while the package is at the warehouse |

A count holds at most 2,000 packages; a warehouse with more would count in parts (kept as a limit).

## 7. Step 4: tickets for a missing package (A-6)

### What was built

- **For the customer** (`/customer/tickets`, "Missing packages" in the menu): report a package the carrier says was delivered but is not in the account, with the carrier, the tracking number, the store and order number, the delivery day, what is inside, a message and the carrier's **proof of delivery** (PDF, PNG or JPEG, up to 10 MB, checked by its first bytes). The customer sees the status (*we are looking*, *we answered*, *resolved*), the messages and, once staff link it, the package found. Staff are never named and nothing in the warehouse shows before staff link the package. Writing back opens the ticket again. One open report per package: the same report sent again opens the first.
- **For staff** (`/staff/tickets`, beside the parcel views; parcels: view to see, parcels: assign to act, at the ticket's warehouse): the queue of the user's warehouses, waiting ones first, filtered by status and warehouse. A ticket shows what the customer gave, the proof (a download), the messages and the **undeclared packages that resemble it**: the same tracking number, then the same end of it (the last 8 characters), then the same carrier around the delivery day. Staff **give** one to the customer (the assignment of section 6, with how they checked it) and the ticket is resolved with it, or **answer**, or **resolve** with a last word. The customer gets an email with the link to the ticket.
- The design also named matching pre-alerts; a customer's own pre-alert is matched at the dock already, so the lookalikes are packages only.

### Database

- `ops.Tickets` (the ticket, its messages as JSON, the proof's file key, type and checksum, the package found, the last form's key; one ticket not yet resolved per customer and package).
- Migration `20261019235126_Tickets`, in the global script (19 Platform, 22 Operations, 1 Customer migrations); tested on an empty server and over the step 3 database, each run twice.

### Automated tests

| Test | What it proves |
|---|---|
| `Open_KeepsWhatTheCustomerKnows_TheTrackingNumberNormalised` (Domain) | The details kept, the tracking number cleaned, the first message |
| `Open_RefusesMissingDetails_AndADeliveryDayOutOfRange` (Domain) | Contents required; a delivery day in the future or over a year old refused |
| `Answers_MoveTheStatus_TheCustomerReopensAResolvedTicket` (Domain) | Answered, open again on the customer's reply, resolved with a package; the package never replaced; a form's key is a repeat |
| `Messages_AreWithinLimits` (Domain) | Empty and too long refused; at most 100 messages |
| `Customer_ReportsAMissingPackage_SeesTheAnswer_AndOnlyTheirOwnTickets` (Api) | A text file refused as proof, a PNG kept with its checksum; the same report again opens the first; another customer gets "not found" for reading and writing; the package found and the answer shown; a reply sent twice writes once and opens the ticket again |
| `Tickets_ShowTheUndeclaredLookalikes_AnswerTheCustomer_AndGiveThePackageFound` (Web) | The queue; the customer's code and name; the package with the same tracking number end listed, another carrier's not; the proof as a sandboxed download; an answer sent twice gives one message and one email with the ticket's link; an empty answer refused; a link with a made-up check refused; the package given and the ticket resolved, out of the waiting queue |

### Independent review of step 4 (2026-10-19)

Tenant scope, the customer's ownership, the staff permissions per warehouse, the upload checks, antiforgery and the texts were found sound. Its findings and what was done:

| # | Finding | Done |
|---|---|---|
| 1 | A refused assignment could be half-saved by resolving the ticket in the same request | The assignment drops its changes when a step is refused; a refused assignment stops the link |
| 2 | An assignment saved but a resolve refused left no way to link the package | The lookalikes include the ticket's customer's own packages at the warehouse; linking one resolves the ticket |
| 3 | Matches on the end of the tracking number could be pushed out by older packages | Ranked: same number, then the same end, then carrier and day |
| 4 | An answer or reply sent twice wrote twice and sent two emails | Each form carries a key; the same key is a repeat, also when sent twice at once |
| 5 | A ticket could be resolved with any package of the customer, at any warehouse, and its package replaced | Only a package of the customer at the ticket's warehouse; a linked package is never replaced |
| 6 | The proof was shown in the browser | A download, sandboxed, as the documents answering holds |
| 7 | The stored proof stayed when saving failed with an error | Removed whenever the ticket is not saved |
| 8 | A report sent twice made two tickets | One open report per customer and package; the same report opens the first |
| 9 | The design named matching pre-alerts | Recorded above: packages only |
| 10 | The customer saw a general error on a conflict | The conflict has its customer wording |

Linking sends the customer two emails, the package's notice and the ticket's; kept, each links to its page.

The full Operations run passed. `EveryForm_SentTwiceAtOnce_NeverFails` (Web) failed in two of five full runs under heavy parallel load and passed in every run on its own and in the later full runs; it signs in with time-based codes and was not changed by this step.
