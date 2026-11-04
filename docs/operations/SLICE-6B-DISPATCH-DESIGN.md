# Slice 6B: Dispatch - design note

| | |
|---|---|
| Plane | Operations (Stage 4) |
| Date | 2026-10-21 |
| Builds on | Slice 6A (bundles), ADR-0018 (milestones), ADR-0021 (tracking events), ADR-0019 (double-click protection) |
| Source | V3's dispatch code (OperationPlane: `Dispatch`, `DispatchLeg`, `DispatchBundle`, the dispatch use cases, the bundle manifest and the Shipper's Letter of Instruction), read on 2026-10-21 |
| Status | **Agreed with the owner on 2026-10-21** (section 4). **Built, both steps; waiting for owner review** ([verification record](SLICE-6B-VERIFICATION.md)) |

## 1. What V3 does

- **A dispatch** (`DSP` number) leaves one origin warehouse, with a cutoff time and notes. States: *planned* → *ready* → *in transit* → *completed*, or *cancelled*.
- **Bundles** are attached while it is planned; a bundle must be closed, and attaching it makes it *ready for dispatch* (a *manifested* event). Ready needs at least one bundle and one leg.
- **Legs**: ground, air or sea; from and to (free text); scheduled and actual departure and arrival; driver, vehicle, carrier and a reference (the air waybill or bill of lading number). A leg departs, arrives, is delayed or cancelled.
- **Start** needs every bundle ready; the bundles become *dispatched*. **Complete** needs every leg arrived.
- **Events** are written on the dispatch, its legs and its bundles, all **internal**: the customer's parcels see nothing from dispatch.
- **Documents**: a bundle manifest (number, destination, parcels, weights, the shipment numbers) and a **Shipper's Letter of Instruction** (SLI) for air exports: shipper and consignee, account number, final destination, prepaid and collect charges, declared value for customs, tax id, nature and quantity of goods, special instructions, the documents that travel (air waybill, commercial invoice, OFAC licence, SED, packing list), signature; one SLI for several bundles; draft, finalized, voided.
- No carrier list, no carrier fees, no cost per shipment.

## 2. What V4 should do better (Claude's reading)

| # | Draft decision |
|---|---|
| G-1 | **A dispatch** (DSP) from one origin warehouse to one destination warehouse or country, by one service, closed bundles only; the same lifecycle as V3. A bundle in a dispatch can no longer be voided (6A, D-8). |
| G-2 | **Legs** as in V3, with the times staff actually record (a late entry keeps the real time, ADR-0021), a carrier from the company's list and the reference (AWB, B/L, truck or tracking number). |
| G-3 | **The customer follows**: each parcel of a dispatched bundle gets *manifested*, *departed origin*, *in transit* and *arrived in the destination country* from the dispatch's own steps (milestones 6 to 9), never the bundle or the carrier names. |
| G-4 | **Documents**: the dispatch manifest (bundles, pieces, weights, shipments, consignees) for the carrier and customs; the SLI as in V3. |
| G-5 | **Arrival** at the destination warehouse is the destination's receiving scan (Slice 7), not a button. |

## 3. Cost (open)

The closed bundle's weight, size and volumetric weight are stored (6A). Carrier fees and the cost per shipment wait for the owner's answer in section 4.

## 4. The owner's answers (2026-10-21)

| # | Answer |
|---|---|
| A-1 | **The customer follows the dispatch** automatically: departed, in transit, arrived in the destination country (G-3). |
| A-2 | **Carrier fees**: air per chargeable kilogram (actual or volumetric); ocean per bundle (piece). |
| A-3 | **Documents**: the dispatch manifest, the bundle manifest, the Shipper's Letter of Instruction, and the air waybills. |
| A-4 | **The master air waybill (MAWB)** is issued by the airline or its agent; V4 records its number on the air leg. |
| A-5 | **House air waybills (HAWB)**: one per customer shipment, printed by V4 under the dispatch's MAWB. |
| A-6 | **Cost per shipment**: the carrier's cost shared by chargeable weight. |
| A-7 | **Carrier rates** per carrier and destination, in settings. |
| A-8 | **Arrival** is the destination warehouse's receiving scan (Slice 7); the dispatcher can also mark a leg arrived. |

## 5. Decisions

| # | Decision |
|---|---|
| D-1 | **Carriers** (Settings → Carriers): the company's airlines, forwarders, ocean lines and trucking companies: name, kind (air, ocean, ground), the account number with them, the airline's three-digit prefix for air waybills, active or not. **Rates** per carrier and destination (a destination warehouse or a country): air, a rate per chargeable kg with a minimum; ocean, a rate per bundle by bundle type (or one rate for any). Rates have a start date, so a change applies from that day. |
| D-2 | **A dispatch** (DSP, Settings → Numbering): from one origin warehouse to one destination (warehouse, or country for home delivery), for one service, with a cutoff time and notes. States: *planned* → *closed* (manifested: nothing more is added) → *departed* → *arrived*, or *cancelled* while planned or closed. |
| D-3 | **Bundles** join a planned dispatch only when closed, for its destination and service, and in no other dispatch; a bundle on a dispatch is *manifested* and can no longer be voided. Taking a bundle out is possible while the dispatch is planned. |
| D-4 | **Legs**: kind (air, ocean, ground), carrier from the list, from and to, scheduled times, the reference (MAWB number for air, bill of lading for ocean, truck or tracking number), driver and vehicle for ground. Departed and arrived are recorded with the time they happened. The first leg departing departs the dispatch. |
| D-5 | **The customer follows** (A-1): closing the dispatch writes *manifested* on each of its parcels (internal), the first departure writes *departed origin* (customer-visible), every later leg *in transit*, the last leg's arrival *arrived in the destination country*. The carrier, the MAWB and the bundle stay internal. |
| D-6 | **Documents** (A-3): the dispatch manifest (bundles with weight, size and pieces; each shipment with its HAWB, shipper, consignee, pieces, weight, contents and declared value); the bundle manifest (one page per bundle); the HAWB of each shipment (A-5), on Letter paper, under the MAWB of the air leg; the Shipper's Letter of Instruction (as V3, prefilled from the company, the dispatch and its bundles). |
| D-7 | **Cost** (A-2, A-6): when the dispatch departs, its cost is worked out from the carrier's rates in force: air, the bundles' chargeable weight (the greater of actual and volumetric, at the service's divisor) times the rate, at least the minimum; ocean, each bundle at its type's rate. Each shipment gets its share by chargeable weight; the dispatch page shows the cost and each shipment's share, and the dispatcher can correct the total with a note. |
| D-8 | **Rights**: *manage dispatches* (create, add bundles and legs, close, depart, arrive, cancel, documents) per warehouse; *manage carriers* (Settings → Carriers). |

## 6. Steps

| Step | Content |
|---|---|
| 1 | Carriers and rates; the dispatch with bundles and legs; the customer's events; the dispatch and bundle manifests |
| 2 | House air waybills; the Shipper's Letter of Instruction; the cost per shipment |

## 7. Settled while building (Claude, industry practice; can be changed after review)

| # | Point |
|---|---|
| B-1 | **Manifested is shown to the customer** ("Manifested for departure", without the dispatch number), not internal as D-5 said: every milestone is customer-visible (ADR-0018), and the milestone itself showed anyway. Cancelling a closed dispatch voids it and the parcel goes back to consolidated. |
| B-2 | **House numbers** are the dispatch's number and the shipment's place on it (DSP26000001-001), given when the dispatch closes: no new prefix to set up. Cancelled, they go with the dispatch. |
| B-3 | **The carrier's cost**: each carrier on the dispatch is counted once (a rate is for the whole trip to the destination); a carrier with no rates at all (the company's own truck) costs nothing; a carrier with rates but none for this destination leaves the cost unknown, and the dispatcher types it from the carrier's invoice with a note. Worked out at the first departure, again on request; a correction stays until cleared. Shares by chargeable weight, rounded down to the cent with the cents left over to the largest remainders. |
| B-4 | **House air waybills** only for a dispatch with an air leg; their airports are the first air leg's departure and the last air leg's arrival (the master air waybill's), the whole route in *routing*. Goods: the contents of the shipment's parcels on this dispatch; *personal effects* when none was declared. The letter of instruction says *consolidation as per attached manifest*, lists declared values per currency, and prints once the dispatch is closed. |
| B-5 | **Leg times** are typed where the company works; at most 30 days back, never in the future; each leg after the previous one arrived. A dispatch has at most 6 legs and 500 bundles; a closed one keeps at least one leg. |
| B-6 | **A parcel on hold**, blocked or waiting for a check (a hold opened after its bundle closed) stops the dispatch from closing and from leaving. |
| B-7 | **Places typed for a leg reach the customer** (*in transit at SDQ*): the form says to type a city or an airport, never a carrier. |
