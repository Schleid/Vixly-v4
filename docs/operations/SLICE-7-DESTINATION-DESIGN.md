# Slice 7: Destination receiving, pickup and delivery - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4); the plan's slice 8 "Delivery and proof" |
| Date | 2026-10-21 |
| Builds on | Slice 6B (dispatch), 6C (operational events), 5C (counter, signature), 5F (invoices, cash drawers, credit), 4C (destinations, hubs, partners), 3E (reminders, unclaimed) |
| Source | V3 (OperationPlane), read on 2026-10-21: the last leg's arrival writes an internal "arrived at destination warehouse" event; customs, ready for pickup, out for delivery and delivered exist only as codes typed by hand; no receiving at destination, no pickup counter, no routes, no proof of delivery, no partner login |
| Status | **Agreed with the owner on 2026-10-21** (section 2). Steps 1 and 2 built and verified ([step 1](SLICE-7A-VERIFICATION.md), [step 2](SLICE-7B-VERIFICATION.md)); steps 3 and 4 next |

## 1. What V4 has to start from

- Destinations are warehouses (pickup point, home delivery in an area, or both), run by the company or a partner, possibly served through a hub (4C).
- A dispatch arrives in the destination country; the parcels' milestone is *arrived in the destination country* (6B). Milestones 10 to 15 (in customs, cleared, arrived at the destination warehouse, ready for pickup, out for delivery, delivered) exist but nothing writes them.
- Shipments carry the recipient, pickup or home delivery and the address; invoices, payments, cash drawers and credit accounts exist (5F).

## 2. Decisions (owner, 2026-10-21)

| # | Decision |
|---|---|
| R-1 | **Arrival: bundles, then parcels.** Each bundle is scanned off the truck against the manifest; then each parcel as its bundle is opened. Parcels get *arrived at the destination warehouse*; the dispatch's last leg is marked arrived by the first scan if it was not already. |
| R-2 | **Missing and extra parcels go on a discrepancy list** for the origin and destination to resolve; the customer is told (an exception, 6C) only when staff confirm it. |
| R-3 | **Customs per destination (setting).** A destination that clears customs records *in customs*, *cleared* or *held* on a dispatch or bundle, reaching every parcel; others skip it. |
| R-4 | **Destination charges (setting).** A company that wants it adds duties, taxes and local fees as a destination invoice before release; others never see it. |
| R-5 | **Pickup proof (setting).** ID checked with its last digits noted (the full number is never stored) and a signature on screen, on by default; a pickup code sent to the customer and a photo of the person, switched on by the company. |
| R-6 | **A person the customer names** (name, phone) on the portal may collect, with the same proof. |
| R-7 | **Home delivery with routes:** parcels grouped into a driver's route, a run sheet printed, each stop delivered (signature or photo) or failed with a reason; after a set number of failed tries the parcel waits for pickup or goes back. |
| R-8 | **Partner agents:** a login limited to their own destination warehouse: receive, customs, release and deliver; only those parcels and recipients; no prices, invoices or other customers. **Partner access is a plan feature (owner, 2026-10-21):** Platform decides which plans include it, and a Platform administrator switches it on or off in the plan in Control; a company never switches it itself. Platform's feature list gains *partner agents* (`partner_agents`), read by Operations like the site's branding features; while the company's plan does not have it, no partner login can be created or signs in. Unlike branding, it fails closed: when Platform cannot answer, partners are refused. |
| R-9 | **Pay, then release.** The pickup counter takes payment (its cash drawer) before release; a driver collects cash on delivery, settled with the route. |
| R-10 | **Storage at the destination (setting):** free days, then a fee per day, reminders and a final notice, as at the origin; off by default. |
| R-11 | **Onward from a hub:** an onward dispatch (a truck leg) from the hub to the final warehouse, with the same scans, manifest and customer events. |

## 3. Proposed steps

| Step | Content |
|---|---|
| 1 | Destination receiving (bundles, parcels, discrepancies), customs, onward dispatch from a hub; *ready for pickup* with the customer's email |
| 2 | The pickup counter: who may collect (portal), proof settings, pay then release, proof shown to the customer; destination charges; storage at the destination |
| 3 | Home delivery routes: run sheet, delivered or failed with proof, cash on delivery and the route's settlement |
| 4 | Partner agents' limited login |

## 4. Settled while building step 1 (for the owner to confirm)

| # | Choice | Why |
|---|---|---|
| S-1 | **One warehouse receives a dispatch, fixed when it is planned:** its destination, or the hub serving it. A dispatch leaving from that hub, or from any warehouse in the destination country, is *onward* and goes straight to its destination. A dispatch to a country only is received by the first warehouse there that scans it | A hub changed later does not move dispatches already on their way; two warehouses never receive the same dispatch |
| S-2 | **A dispatch keeps the bundles it carried** (a link per bundle), with when each was scanned in or listed missing. A bundle received at a hub is free for an onward dispatch from there | A bundle may travel on two dispatches; what arrived on the first stays true after it goes on |
| S-3 | **Customer events follow each parcel:** an onward dispatch does not repeat *manifested*, *departed* or *arrived in the destination country*; at a hub the parcels are *in transit at* the hub; at their warehouse *arrived at* it | The customer sees each step once |
| S-4 | **What is scanned:** a bundle, a repack box or a parcel number. A parcel scanned first receives and opens its bundle | One field at the dock, as at the origin |
| S-5 | **Extra items are only noted**, never received into the dispatch. Missing lines are noted when receiving finishes; a late scan still receives the item and closes its line as *found* | R-2: staff decide |
| S-6 | **Lost** writes the exception *Missing in transit* on each parcel still going (a standard cause, always available here) and emails the customer when the company sends event emails; *settled* only closes the line. Both need a note | The customer is told only once staff confirm (R-2) |
| S-7 | **Customs is recorded on what is here:** the bundles scanned in on the dispatch and still at the warehouse (once a bundle is opened, the parcels scanned in from it), the whole dispatch or one bundle; *held* may carry a line for the customer. A held parcel cannot leave on a dispatch or become ready | R-3; nothing is said about parcels not yet arrived or gone on |
| S-8 | **Ready for pickup:** a pickup shipment's parcel at its own warehouse, scanned, not held, and cleared where that warehouse clears customs. Home-delivery parcels wait for step 3 | R-1 |
| S-9 | **The ready-for-pickup email** is a company setting in *Settings → Events*, on by default; one email per shipment, when every parcel still going is ready, with the place, its address and hours | Same pattern as the event emails (E-5) |
| S-10 | **Who sees it:** *Arrivals* opens with the right to receive or to record customs, per warehouse. Staff of the warehouse a parcel or bundle is at now (a hub) see its events and may record them | The hub works on parcels that are not its own |
| S-11 | **Dispatches that arrived before this slice** are taken as received (they can still be scanned); those still on their way are received as usual | They would otherwise wait in *On their way here* for ever |

Noted, not changed: the bundle page itself still belongs to the origin warehouse; a hub works through *Arrivals* and *Dispatches*.

## 5. Settled while building step 2 (for the owner to confirm)

| # | Choice | Why |
|---|---|---|
| S-12 | **Settings → Pickup:** the proof asked (an ID and a signature by default; a pickup code and a photo when switched on), destination charges on or off, storage at the destination on or off with its free days and fee per parcel per day (off by default) | R-4, R-5, R-10 as company settings |
| S-13 | **The ID:** its kind (national ID, passport, driving licence, other) and its last 4 letters or digits are kept, never the full number | R-5 |
| S-14 | **The pickup code:** six digits, sent by email with the ready-for-pickup email or from the counter (a new one replaces the last, at most one every two minutes); kept protected with the server's key, bound to its shipment, valid 30 days; five wrong tries stop it and a new one is sent; never written in clear to the database or the outbox | Whoever has it may collect; a copy of the database alone never gives it |
| S-15 | **Who collects:** the recipient, the person the customer named on the portal (once the shipment is booked, until it is collected; same rules as the recipient), or the customer; the same proof for each | R-6 |
| S-16 | **A destination invoice per visit**, billed at the counter: customs duties and local taxes (not taxed again), local fees and storage (taxed as charges); storage counted to the day it is billed (nothing more is owed within a day of a bill), and a later visit is billed for the days since; paid at the cashier of the destination | R-4, R-9: what is owed is billed and paid before release |
| S-17 | **Pay, then release:** every invoice of the shipment paid or on account, and the storage owed to the day billed, before the release form shows | R-9 |
| S-18 | **A release** hands over every parcel of the shipment ready at that warehouse at once; the customer sees *Collected at* the place; the shipment is *collected* once all its parcels are | One visit, one record |
| S-19 | **Storage at the destination** runs from the day a parcel is ready, with the destination's terms of that day (the origin's terms are kept apart); reminders per shipment after 7, 14 and 21 days and a final notice after 25 (as at the origin), only while the company charges storage; nothing happens by itself after the final notice | R-10 |
