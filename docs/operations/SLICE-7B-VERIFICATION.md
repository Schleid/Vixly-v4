# Slice 7 step 2: The pickup counter - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-21 |
| Built on | Slice 7 step 1, destination receiving (package 85); not committed here: the dev team owns git |
| Design | [Slice 7 design note](SLICE-7-DESTINATION-DESIGN.md), sections 2 (R-4, R-5, R-6, R-9, R-10, the owner's answers) and 5 (S-12 to S-19, settled while building) |
| Status | **Built, reviewed independently, fixed and verified by automated tests.** Step 3 (home delivery routes) next |

## 1. What was built

- **Settings → Pickup** (`/staff/settings/pickup`, R-4, R-5, R-10): the proof asked at the counter (an ID and a signature by default; a pickup code and a photo when switched on); destination charges on or off; storage at the destination on or off, with its free days and its fee per parcel per day (off by default).
- **The pickup counter** (`/staff/pickup`): the shipments ready at the warehouse, oldest first, found by number, recipient, the person named or the last digits of a phone.
- **A shipment at the counter** (`/staff/pickup/{id}`): the customer, the recipient and the person named; the parcels here and the storage not billed yet; the invoices with a link to the cashier.
  - **Bill** (R-4, R-10): customs duties, local taxes and local fees typed at the counter, and the storage owed to that day, on a destination invoice paid at the cashier. A later visit is billed for the days since.
  - **Pay, then release** (R-9): the release form shows only once every invoice is paid or on account and the storage owed is billed.
  - **Release** (R-5, R-6): who collects (the recipient, the person named or the customer), the ID shown and its last 4 characters (never the full number), a signature on screen, the pickup code, a photo, as the company asks. The customer sees *Collected at* the place.
- **The pickup code** (R-5): six digits, emailed with the ready-for-pickup email or from the counter; kept protected with the server's key; five wrong tries stop it.
- **Who may collect** (R-6): on the portal, once a pickup shipment is booked and until it is collected, the customer names one person (name and phone) or no one.
- **Reminders** (R-10): while the company charges storage, one email per shipment waiting after 7, 14 and 21 days, then a final notice after 25.
- **Rights**: *serve the pickup counter*, per warehouse; payments stay with the cashier's rights.

## 2. Database

The global script adds **PickupReleases** (who collected, the ID's kind and last 4 characters, the proof files' keys, whether the code matched, who released it); on **Shipments** the person named, the protected pickup code with its date and wrong tries, when the first parcel was ready, the reminders sent, when it was collected and the last bill's request key; on **Parcels** when it was ready, the destination's storage terms and how far its storage was billed; on **TenantSettings** the proof (ID and signature for every company), destination charges and storage (off, 7 free days). Invoices gain the kind *destination*.

Checked: the script runs on an empty database twice, and twice on a database upgraded through every earlier package to package 85 with demo data.

## 3. Independent review and fixes

| # | Finding | Fix |
|---|---|---|
| 1 | After a first bill, storage of a later visit and later charges could never be billed | Storage is billed to a date per parcel; a later visit owes the days since, on another destination invoice |
| 2 | Nothing limited guesses of the six-digit code, or code emails | Five wrong tries stop the code (saved and audited); a new code at most every two minutes |
| 3 | A voided destination invoice blocked the shipment for good | Several destination invoices are allowed; a double click is caught by its request key |
| 4 | The code's hash could be reversed from a database copy | Kept protected with the server's Data Protection key, bound to the shipment, valid 30 days |
| 5 | The ready notice could email a code that was never saved | The code still valid is reused, or a new one is saved before it is sent; the shipment is marked told after sending |
| 6 | Reminders read the 500 oldest parcels, could stall and repeat | Kept on the shipment; only shipments with a step due are read |
| 7 | Destination invoices were called *order* on the PDF, the invoice page and the portal | Worded *at the destination* |
| 8 | Ready overwrote the origin's storage terms | The destination's terms are kept apart |
| 9 | Proof files could stay behind; the release form had no size limit | Removed unless the release is saved; limited to the photo's size |
| 10 | The counter listed parcels held by customs and shipments not booked | Only what can be released |
| 11 | Storage already billed still showed as owed | Only what is not billed yet |
| 12 | A person could be named on a home-delivery shipment | Pickup shipments only |
| 13 | A request key of another shipment answered as a success | Matched with its shipment |
| 14 | The signature was checked by its first bytes only | Also read as a PNG image |
| 15 | `%` and `_` in the search acted as wildcards | Searched as typed |
| 16 | Second pass: tries sent at the same moment were not all counted | Each try is counted in the database in one statement before the code is compared; the right code starts the tries again |
| 17 | Second pass: a customer paying at the cashier could cross into a new storage day and be asked to bill again | Within a day of a bill nothing more is owed; the next visit's bill counts from the last one |
| 18 | Second pass: a shipment partly collected could hold up the reminders | Only shipments with a parcel still ready, of companies charging storage, are read |

The second pass confirmed the other fixes. Noted, not changed (low): a customer naming a person is not emailed about it; proof images are kept but no page shows them yet; a new code may be sent every two minutes, each with five tries (about 150 tries an hour, all audited); a company that switches storage on later sends the latest reminder due at once; parcels of a shipment ready later follow the shipment's first reminder date.

## 4. Automated tests

| Suite | Result |
|---|---|
| Operations (domain, application, infrastructure, web, API, customer web, architecture) | All pass: domain 245, application 93, infrastructure 65, web 101, API 31, customer web 40, customer infrastructure 1, architecture 13 139 |

New for this step:

- Domain (4): a release keeps who collected and only the ID's last 4 characters; the code's five tries and a person named only on a booked pickup shipment; the pickup settings (ID and signature by default, storage needs a fee); storage from the day a parcel is ready, billed per visit, and the shipment's reminder steps.
- Application (2): the code kept protected, bound to its shipment, expiring; the reminder steps.
- `Pickup_BillsChargesAndStorage_PaysFirst_ThenReleasesWithProof` (web): the settings saved (a fee needed for storage); ready at the destination; the person named (a local phone refused); found by that person's phone; not found at the origin; storage of 3 days at 1.50 billed first with the duties and the local fee, once for a double click, nothing more to bill; refused until paid at the destination's cashier; the code: none sent, then sent by email; the ID, the signature and the code each checked; five wrong tries stop it, a new code not within two minutes, then sent; released once for a double click; the parcel *delivered*, the shipment collected, the person named locked; the counter no longer lists it; someone without the right sees nothing.
- `Customer_NamesWhoMayCollect_OnceBooked` (API and portal): refused before booking, a local phone refused, saved, cleared; another customer not found.

Not covered by an automated test yet: the reminder job end to end; a photo taken at the counter.

## 5. Styling

New screens are in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*: Pickup counter, Pickup of a shipment, Pickup settings; the customer's Shipment row notes the *who may collect* form.
