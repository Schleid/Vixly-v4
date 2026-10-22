# Slice 6A: Bundles - design note (draft for the owner)

| | |
|---|---|
| Plane | Operations (Stage 4) |
| Date | 2026-10-20 |
| Builds on | Slice 5C (final shipments), ADR-0018 (milestones), ADR-0021 (tracking events), ADR-0019 (double-click protection) |
| Source | The owner's walk-through of V3 bundles with Claude, 2026-10-20 |
| Status | **Built 2026-10-21** as section 5 says, after the cashier (Slice 5F step 2a), since bundling needs release on payment. The owner answered every question on 2026-10-21 (section 4). Verification: [Slice 6A verification](SLICE-6A-VERIFICATION.md) |

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

## 4. The owner's answers (2026-10-21)

| # | Answer |
|---|---|
| A-1 | **Payment before bundling:** settled by billing (Slice 5F, F-3): an unpaid prepaid shipment is held and cannot be bundled; collect shipments and charges to a credit account are released. |
| A-2 | **Target:** a company setting: *warn and accept* or *refuse past the target*. |
| A-3 | **Consolidation:** both ways exist: the parcels repacked into one new box (weighed and measured, with its own label) before bundling, or kept together unchanged in one bundle. *Which one applies, and who chooses: see the question below.* |
| A-4 | **No reopening.** A closed bundle that is wrong is **voided**: its parcels go back to *ready to bundle* and the work starts again in a new bundle. Replaces B-5's reopen. |
| A-5 | **Bundle types are the company's own** (Settings: box, sack, pallet, container, or its own words); chosen when the bundle is created and printed on its label. |
| A-6 | **Labels:** the same 4×6 label printers as parcels (Claude's default; the owner may change it). |

- **A-3 (answered):** the company sets the default way in its settings; the ship manager can switch it for a consolidated shipment (for example too many parcels to repack).
- **A-4 (answered):** voiding a closed bundle is for the ship manager (*manage bundles*), with a reason, and only until the bundle is on a manifest.

## 5. As built (from 2026-10-21)

B-1 to B-9 with the owner's answers (section 4):

| # | Decision |
|---|---|
| D-1 | **Company settings** (Settings → Bundles): the bundle types (the company's own words: box, sack, pallet...), *when a bundle reaches its target*: warn and accept, or refuse; *consolidated shipments*: repacked into one box, or kept together in one bundle; the bundle prefix (BD) and the box prefix (CB) under Numbering. |
| D-2 | **A bundle**: origin warehouse, destination (a destination warehouse, or a country for home delivery), service, type, size policy (free fill, or a target in actual or volumetric weight in the company's unit), number and 4×6 label. States: open, closed, voided (manifested and departed come with dispatch). |
| D-3 | **Fill by scan**: the bundle's barcode first, then parcels. Accepted only at the bundle's warehouse, in a final shipment **paid** (F-3; a shipment made final before billing has no invoice and passes), clear of holds and inspections, not in a bundle, the destination and service matching. Each refusal says why. The same scan twice is a repeat. Beside each scan: the shipment, how many of its parcels are in bundles, and where the missing ones are (their shelf). A split across bundles is a warning. |
| D-4 | **Consolidated shipments** (the customer's option): the company's way applies, and the ship manager may switch it for a shipment. *Repacked*: its parcels are scanned into a **box** (numbered CB, its own label), weighed and measured, and the box is scanned into a bundle (its parcels go in with it); a loose parcel of that shipment is refused in a bundle. *Kept together*: its parcels go into one bundle; one already in another bundle refuses the next. |
| D-5 | **Target**: the parcels' weight (actual or volumetric, as the bundle's policy) against the target: past it, warn or refuse as the company chose. |
| D-6 | **Take out** a parcel from an open bundle (a wrong scan): its *packed* event is voided (ADR-0021). |
| D-7 | **Close**: gross weight and length, width, height; the volumetric weight with the service's divisor; the parcel count and the parcels' weight for comparison. Its label prints again with the weight. A closed bundle takes nothing more and is never reopened. |
| D-8 | **Void** (owner, A-4): an empty open bundle, or a closed one until it is manifested, by the ship manager with a reason; its parcels' *packed* events are voided and they are ready to bundle again. |
| D-9 | **Events**: each parcel put in a bundle gets *packed for departure* (customer-visible, milestone Consolidated; the bundle number is internal). |
| D-10 | **Screens**: **Bundles** (*manage bundles*): ready to bundle (final, paid parcels not in a bundle, by destination and service, with counts and weight), open and closed bundles, *New bundle*; a bundle's page (parcels, shipments' completeness, close, void, label). **Fill** (*fill bundles*): one scan box, keyboard-first, the last scans with their result. **Boxes** for consolidated shipments. |
| D-11 | **Rights**: *manage bundles* and *fill bundles*, per warehouse. |

**Details settled while building** (2026-10-21):

- **Closing** a bundle or a repack box is open to *manage bundles* and *fill bundles* alike: the person at the scale weighs it. Voiding stays with *manage bundles*.
- **Paid** (D-3): a shipment whose invoice is unpaid or waiting for finance (Zelle, cheque, transfer) is refused; a voided invoice asks nothing. Collect shipments and credit accounts are released by Slice 5F step 2b when it is built; until then they wait for payment like any other.
- **A repack box** takes every parcel of its shipment before it closes (they are repacked together); a parcel in a box goes into a bundle only with its box, and *Take out* on one of its parcels takes the whole box out. A box is voided while in no bundle; voiding the bundle takes its boxes out, still closed.
- **A ready parcel** is one of a final shipment, still at the warehouse, in no bundle, measured and clear of holds and inspections. The fill station checks the same at each scan.
- **Labels** are 4 x 6 PDFs (bundle: route, service, type, parcels, barcode and, once closed, weight and size; box: route, recipient, service, weight, its barcode and the shipment's).
- **Numbering**: two prefixes under Settings → Numbering, BD and CB; a company already using one of them for another kind of number gets BDL (or BNDL) and CBX (or RPK).
- **Events**: *packed* on each parcel (customer: *Packed for departure*; staff: the bundle number), *repacked* on each parcel put in a box (internal); *bundle opened*, *closed* (weight and size) and *voided* (with the reason) on the bundle. Taking out or voiding voids the parcel's event, so its milestone goes back to final.
- **The way stays with the shipment** once its first parcel is boxed or bundled, so a later change of the company's way never strands it; scans of one consolidated shipment's parcels at the same instant clash, and the second is scanned again. A bundle holding only part of a kept-together shipment cannot be closed.
- **Left as is**: a bundle or box number is used even when its creation then fails.

## 6. Not in this slice

Dispatch, manifests, carriers and their fees (Slice 6B, after reading the V3 dispatch code with the owner); air waybills and container numbers; costs per shipment.
