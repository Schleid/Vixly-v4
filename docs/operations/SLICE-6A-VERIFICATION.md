# Slice 6A: Bundles - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-21 |
| Built on | Slice 5F step 2a, the cashier (package 80); not committed here: the dev team owns git |
| Design | [Slice 6A design note](SLICE-6A-BUNDLES-DESIGN.md), sections 4 (the owner's answers) and 5 (D-1 to D-11, as built) |
| Status | **Built, reviewed independently, fixed and verified by automated tests.** Dispatch, manifests and carriers (Slice 6B) next, after reading the V3 dispatch code with the owner |

## 1. What was built

- **Settings → Bundles** (D-1): the company's bundle types in its own words (up to 12; none: bundles have no type); past a target, *warn and accept* or *refuse*; consolidated shipments *repacked into one box* or *kept together in one bundle*. Numbering gains the bundle (BD) and repack box (CB) prefixes.
- **The bundles desk** (D-10): *ready to bundle* grouped by warehouse, destination (a destination warehouse, or a country for home delivery) and service, with parcels, shipments, weight and those still unpaid or held; *Open bundle* on each group (type, free fill or a target in weight or volumetric weight in the company's unit); the open, closed and recently voided bundles. A double click opens one bundle.
- **A bundle** (D-2, D-7, D-8): its route, service, type, size policy, parcels and filled weight; its shipments' completeness; closed with gross weight and three sides (volumetric weight with the service's divisor kept), never reopened; voided with a reason (an empty open one, or a closed one), its parcels and boxes ready again; a 4 x 6 label, printed again with the weight once closed.
- **The fill station** (D-3, D-5, D-6): scan the bundle's (or a repack box's) barcode, then each parcel. Refused with the reason: unknown barcode, another warehouse, already in another bundle, not final, on hold or waiting for an inspection, another destination, another service, unpaid, a repacked shipment's loose parcel, a kept-together shipment split, past the target when the company refuses. The same scan twice is a repeat. Beside each scan: its shipment, how many of its parcels are in bundles and where the others are (shelf or bundle); a split is a warning. *Take out* undoes a wrong scan.
- **Consolidated shipments** (D-4): the company's way applies; the ship manager switches it for a shipment until one of its parcels is boxed or bundled. *Repacked*: a repack box (CB) takes every parcel of the shipment, is closed with its weight and size and its own 4 x 6 label, then goes into a bundle at the fill station with all its parcels. *Kept together*: its parcels go into one bundle only.
- **Events** (D-9, ADR-0021): *packed* on each parcel (customer: *Packed for departure*, milestone Consolidated; staff: the bundle number), *repacked* in a box (internal), and on the bundle *opened*, *closed* and *voided*. Taking out or voiding voids the parcel's event: its milestone goes back to final.
- **Rights** (D-11): *manage bundles* (open, void, switch a shipment's way) and *fill bundles* (scan in, take out), per warehouse; closing is open to both. Owner and Administrators hold them.

## 2. Database

The global script adds **Bundles** (number unique per company, form key unique, its counts and weights, the closing measurements, the void), **RepackBoxes** (number unique, one box not voided per shipment, its bundle), on **Parcels** `BundleId` and `BoxId` (indexed), on **Shipments** `ConsolidationWay`, and on **TenantSettings** `BundlePrefix`, `BoxPrefix`, `BundleTypes`, `BundleTargetRule` (warn by default) and `ConsolidationWay` (repack by default). A company already using BD or CB for another number gets BDL (BNDL) or CBX (RPK).

Checked: the script runs on an empty database twice, and twice on a database upgraded through every earlier package to package 80 with demo data.

## 3. Independent review and fixes

An independent review read the whole slice: no high finding; the warehouse scope of every action, the payment gate on both the parcel and the box path, a parcel never in two bundles, the counts and weights, the event voids and the migration were confirmed. Fixed:

| # | Finding | Fix |
|---|---|---|
| 1 | A bundle could be closed with only part of a kept-together shipment, leaving the rest unable to follow | Closing is refused until the shipment is whole in it (or its parcels are taken out) |
| 2 | Changing the company's consolidation way stranded a shipment already partly bundled or boxed | The way in effect is kept on the shipment when its first parcel is boxed or bundled |
| 3 | Two scans at the same instant could split a kept-together shipment | Packing a consolidated shipment's parcel touches the shipment, so the second scan clashes and is scanned again |
| 4 | A stale scan form posted again after a take-out put the parcel back without its event | A used scan key gets a fresh one, so the event is always written |
| 5 | The last-scan panel could show a parcel of another warehouse | Only a parcel at the bundle's warehouse is shown |
| 6 | *Start the box* showed to a ship manager without the fill right, and the ready counts included parcels that would be refused (a repacked shipment's loose parcels or open box) | The button shows only with the fill right; those parcels count as waiting |
| 7 | Two refusal texts were misleading (way fixed, not final) | Reworded |

Noted, not changed: collect shipments and credit accounts are held like any unpaid shipment until Slice 5F step 2b releases them (design section 5).


## 4. Automated tests

| Suite | Result |
|---|---|
| Operations (domain, application, infrastructure, web, API, customer web, architecture) | All pass: domain 224, application 91, infrastructure 65, web 95, API 30, customer web 40, customer infrastructure 1, architecture 12 239 |
| Platform and Public | Unchanged by this slice |

New for this slice:

- Domain (7): a bundle takes its destination and service and warns or refuses past its target; it is checked when made (divisor, target, country, type); closed once with weight and size, voided with a reason; a repack box takes its whole shipment, then goes into one bundle; a parcel is in one bundle and a boxed one goes with its box; packed moves the milestone to Consolidated and its void moves it back; the company's types, rule, way and ten different prefixes, and the per-shipment switch.
- `Bundles_FillByScan_RefuseWhatDoesNotBelong_AndRepackConsolidatedShipments` (web): Settings → Bundles; three final shipments (one paid, one unpaid, one consolidated of two parcels, paid); the desk's ready group; a bundle opened twice with one form is one, a type the company does not have is refused; the label; the fill station opened by the bundle's barcode; the unpaid parcel, an unknown barcode and the consolidated shipment's loose parcel refused; the paid parcel added past the 2 kg target with its shipment shown, a repeat, taken out back to final, added again; the repack box refusing to close incomplete and refused at the station until closed, filled, closed, labelled and scanned into the bundle whole; the bundle closed, a scan refused after, voided with a reason (without one refused), its parcel back to final and its box out but still closed; the three bundle events; a worker who only fills cannot open a bundle.

## 5. Styling

New screens are in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*: Bundles desk, Bundle, Fill a bundle (station), Consolidated shipments, Repack box, Bundle settings; Settings (two prefixes) noted.
