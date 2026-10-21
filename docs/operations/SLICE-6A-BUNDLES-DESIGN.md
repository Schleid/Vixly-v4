# Slice 6A: Bundles - design note (draft for the owner)

| | |
|---|---|
| Plane | Operations (Stage 4) |
| Date | 2026-10-20 |
| Builds on | Slice 5C (final shipments), ADR-0018 (milestones), ADR-0021 (tracking events), ADR-0019 (double-click protection) |
| Source | The owner's walk-through of V3 bundles with Claude, 2026-10-20 |
| Status | **Draft: nothing is built.** Decisions B-1 to B-9 are Claude's reading of what the owner said. The questions in section 4 need the owner's answer before building |

## 1. What the owner said (2026-10-20)

- A **bundle** is what the company ships: the carrier's unit (a box, a sack, a pallet), holding parcels for **one destination and one service**.
- The **ship manager** creates the bundles. A destination can have **one or several open bundles**, depending on how many parcels there are.
- The ship manager decides **how big** a bundle is, by **weight or volumetric weight** depending on the service's policy. Or it is **free fill**: add everything, the cost is worked out after.
- Workers fill bundles after inspection, in the parcel queue. They **scan the bundle's barcode first**, then each parcel once. A parcel for another destination or service is **refused**: the system catches the parcel for the Dominican Republic going into the Les Cayes bundle.
- While scanning, the worker sees the parcel's **shipment**: how many parcels it has, how many are in bundles, and where the missing ones are ("2 of 3, missing one on shelf A-03"). The manager sees the same totals.
- Keeping a shipment's parcels together is desirable, but mistakes happen. **Splitting a shipment across bundles is allowed, with a warning.**
- When a bundle is **closed**, its **weight and dimensions** are recorded. They are used with the carrier's fees to work out the company's cost of the shipment.
- Every bundle action writes an event (ADR-0021): the company sees all of them, the customer only the public ones.
- V4 should improve on V3, not copy it.

## 2. Decisions (draft)

| # | Decision |
|---|---|
| B-1 | **A bundle** belongs to an origin warehouse and has a destination (a destination warehouse, or a country for home delivery), a service, a number (`BD` + sequence) and a printable **label with a barcode**. Its states: **Open** → **Closed** (weighed and measured) → later **Manifested** and **Departed** (dispatch, Slice 6B). Only an empty open bundle can be cancelled. |
| B-2 | **Size policy**, chosen by the ship manager when creating it: **free fill**, or a **target** in actual weight or volumetric weight (kg or lb in the company's unit; the service's policy suggests which). The target is a guide: the worker is warned when it is reached, never blocked. *To confirm.* |
| B-3 | **Filling by scan.** The worker scans the bundle, then parcels. Each parcel is accepted only if it is at the bundle's warehouse, in a **final** (booked) shipment, clear of holds and inspections, not already in a bundle, and its shipment's destination and service match the bundle's. Otherwise it is refused with the reason (wrong destination, wrong service, on hold, not final, already in bundle BD…). The same scan twice is a repeat, not an error. |
| B-4 | **The shipment beside each scan:** "Shipment SH… · 2 of 3 in bundles · missing: P… on shelf A-03". When the shipment's other parcels are in **another** bundle, a warning says so (split, allowed). |
| B-5 | **Take out** a parcel from an open bundle (a wrong scan), with an event. Not from a closed bundle; reopening a closed bundle is a manager's action with a reason. *To confirm.* |
| B-6 | **Close:** the worker enters the bundle's gross weight and its length, width and height. The system keeps them with the volumetric weight (the service's divisor), the parcel count, and the sum of the parcels' weights for comparison. A closed bundle takes no parcels. Its label is printed again with the weight. |
| B-7 | **Events** (ADR-0021): on the bundle: created, closed, reopened, cancelled. On each parcel: *consolidated* (milestone Consolidated), *taken out of the bundle*. Customer-visible: only *consolidated* ("Packed for departure"); the bundle's number and the worker are internal. |
| B-8 | **Screens:** **Bundles** (ship manager): the open and closed bundles by destination and service, with parcel count, weight against target, and *+ New bundle*; a bundle's page with its parcels and its shipments' completeness. **Fill a bundle** (worker): one scan box, the bundle on top, the last scans with their result and shipment context, large and keyboard-first for scanners. **Ready to bundle:** the final shipments' parcels not yet in a bundle, grouped by destination and service, with shelves, so the manager knows how many bundles to open. |
| B-9 | **Rights:** *manage bundles* (create, close, reopen, cancel: ship manager) and *fill bundles* (scan in and take out: workers), both scoped to warehouses. |

## 3. Cost (for later)

The closed bundle's weight and size are stored now. Carrier fees (per kg, per volumetric kg, per piece, minimums) and the cost per shipment, sharing the bundle's cost among its shipments by weight, come with dispatch and carriers (Slice 6B), once the owner has shown how the fees are agreed with carriers.

## 4. Questions for the owner

1. **Payment before bundling:** can a final shipment go into a bundle before it is paid? (Milestones place *Awaiting payment* and *Released* before *Consolidated*; collect shipments are paid at destination.)
2. **Target:** a warning only, or does the bundle refuse parcels past the target?
3. **Consolidation:** is a consolidated shipment (the customer's option) packed by the company into one box *before* bundling (a new box with its own weight), or are its parcels simply kept together in one bundle?
4. **Reopen:** may a closed bundle be reopened, and by whom?
5. **Bundle types:** do you need a type (box, sack, pallet, container) on the bundle and on its label?
6. **Labels:** the same label printers as parcel labels (4x6), or another size?

## 5. Not in this slice

Dispatch, manifests, carriers and their fees (Slice 6B, after reading the V3 dispatch code with the owner); air waybills and container numbers; costs per shipment.
