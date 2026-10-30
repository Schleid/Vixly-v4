# Slice 6B: Dispatch - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-21 |
| Built on | Slice 5F step 2b, credit accounts (package 82); not committed here: the dev team owns git |
| Design | [Slice 6B design note](SLICE-6B-DISPATCH-DESIGN.md), sections 4 (the owner's answers), 5 (D-1 to D-8) and 7 (settled while building) |
| Status | **Built (steps 1 and 2), reviewed independently twice, fixed and verified by automated tests.** Operational events (Slice 6C) next, before Slice 8 (owner) |

## 1. What was built

- **Settings → Carriers** (D-1): the company's airlines, ocean lines, forwarders and trucking companies: name (unique), kind, account number, the airline's three-digit prefix, switched off without losing history. **Rates** per destination warehouse or country, from a start date (today or later; removed only before they start): air per chargeable weight in the company's unit with a minimum, ocean and ground per bundle for a bundle type or any.
- **The dispatch desk** (D-2): the dispatches of the person's warehouses, open or all; closed bundles waiting per warehouse; *plan a dispatch* (warehouse, destination, service, cutoff, notes; a double click plans one; DSP numbering, Settings → Numbering).
- **A dispatch** (D-2 to D-4): fitting closed bundles listed and added with one click or scanned (a bundle on a dispatch can no longer be voided; taken off while planned); legs with carrier (its kind sets the mode), from and to, schedule, reference, driver and vehicle (one leg per form, double clicks included); *close* (manifest) with at least one bundle and one leg; each leg *departed* and *arrived* with the time it happened; *cancel* with a reason before it leaves, its bundles free again.
- **The customer follows** (D-5, B-1): each parcel is manifested at closing, *departed the origin (by air, sea or road)* at the first departure, *in transit at* each later leg's start, *arrived in the destination country* (its name in the customer's language) at the last arrival; the dispatch's own events are internal. Cancelled after closing, the manifested events are voided and the parcels go back to consolidated.
- **Documents** (D-6, B-2, B-4): the dispatch manifest (legs, bundles with weight, size and pieces, each shipment with customer, consignee, pieces, weight, value and contents; *draft* before closing), each bundle's manifest, a house air waybill per shipment under the master air waybill (the air leg's reference with the airline's prefix), all of them in one PDF or one at a time, and the Shipper's Letter of Instruction with what the dispatcher adds (prepaid or collect, export filing reference, documents attached, handling instructions). Letter paper; the company's name only.
- **The carrier's cost** (D-7, B-3): worked out when the dispatch leaves from the rates in force on the departure day, shared among its shipments by chargeable weight; *work out again*; *correct* with a note (audited).
- **Rights** (D-8): *manage dispatches* per warehouse; *manage carriers*.

## 2. Database

The global script adds **Carriers** (name unique per company), **CarrierRates**, **Dispatches** (number and form key unique, legs as JSON, cost and letter fields), **DispatchShipments** (house number unique, one per shipment per dispatch, cost share), on **Bundles** `DispatchId` (indexed), and on **TenantSettings** `DispatchPrefix` (DSP; a company already using DSP for another number gets DSPT, or DPCH).

Checked: the script runs on an empty database twice, and twice on a database upgraded through every earlier package to package 82 with demo data.

## 3. Independent reviews and fixes

Two independent reviews, one per step. Fixed:

| # | Finding | Fix |
|---|---|---|
| 1 | Closing, departing, arriving and cancelling ran three queries per parcel: a large dispatch would time out | The parcels are read once and their events recorded in bulk (a few queries in all), the history read-only |
| 2 | Manifests and papers read shipments one by one and tracked every parcel | One query each, read-only |
| 3 | A parcel put on hold after its bundle closed could still be manifested and leave | Closing and leaving are refused while a parcel is on hold, blocked or waiting for a check |
| 4 | A typo in a leg's time (year 0202) failed the page | Times older than 30 days are refused with the reason |
| 5 | A double click on *add leg* added two legs | The leg form has its key |
| 6 | The fitting bundles and the desk filtered after a row limit, so a busy warehouse could show none | Filtered in SQL; counts counted in SQL |
| 7 | The customer read the country code | The country's name in the reader's language (also for staff) |
| 8 | More than 1,000 bundles would have been cut silently | At most 500 bundles per dispatch, refused with the reason |
| 9 | The cost was counted on every carrier leg and a carrier without rates made it unknown | Each carrier once; a carrier without rates costs nothing (B-3) |
| 10 | Waybills printed the whole route's ends, not the flight's | The air legs' airports (B-4) |
| 11 | A waybill's goods included parcels travelling on another dispatch | Only this dispatch's parcels |
| 12 | Shares could be negative after rounding; a minimum with four decimals was kept unrounded | Largest-remainder cents; the cost rounded to the cent |
| 13 | A correction before departure had no currency; the letter printed before closing with no value; values in different currencies were added | The company's currency; printed once closed; listed per currency |
| 14 | A sixth leg posted twice said *too many legs*; ocean dispatches offered air waybills; cancelling with a manifested event voided by hand failed | Key checked first; air waybills only with an air leg; that event left as it is |
| 15 | Removing a leg could leave two legs with the same number; a closed dispatch could lose its last leg | Legs renumbered; a closed dispatch keeps one |

Noted, not changed: what staff type as a leg's *from* reaches the customer (B-7, a hint on the form); dispatches closed before step 2 (none in production) have no house numbers.

## 4. Automated tests

| Suite | Result |
|---|---|
| Operations (domain, application, infrastructure, web, API, customer web, architecture) | All pass: domain 235, application 91, infrastructure 65, web 97, API 30, customer web 40, customer infrastructure 1, architecture 12 741 |
| Platform and Public | Unchanged by this slice |

New for this slice:

- Domain (6): a carrier's prefix and how its rates count; a dispatch takes only closed bundles that fit and a manifested bundle cannot be voided; legs leave and arrive in order at real times; the cost uses the rate in force, at least the minimum, and shares to the cent; a correction with a note, the letter's fields, the leg form key and the 30-day window; cancelled before leaving with a reason, at most six legs, renumbered.
- `Dispatch_ManifestsBundles_DepartsAndArrives_AndTheParcelFollows` (web): carriers (prefix refused for ocean, a name taken twice refused), an air rate with a minimum; a paid shipment bundled and closed, the bundle manifest; a dispatch planned twice with one form is one; the bundle scanned in twice is one; closing refused without a leg, closed with the carrier's leg (kind set by the carrier): the parcel manifested, the bundle can no longer be voided; the manifest, house air waybills (all and one; another shipment refused) and the letter of instruction; cancelled after closing: the parcel back to consolidated, the papers gone; planned again with two legs (one form posted twice is one leg), closed, master air waybill with the airline's prefix; arrival before departure and a future time refused; departed origin by air at the typed time; the cost 50.00 (the minimum), corrected to 75.00 with a note (without one refused), worked out again keeping the correction, the shipment's share 75.00; cancelling after leaving refused; in transit at SDQ; arrived in the destination country; the parcel's events and the dispatch's; a second dispatch cancelled with a reason (without one refused); a worker without the right sees neither page.

## 5. Styling

New screens are in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*: Dispatch desk, Dispatch (with its legs and papers panels), Carriers, Carrier; Settings (dispatch prefix) and the bundle page (manifest link) noted.
