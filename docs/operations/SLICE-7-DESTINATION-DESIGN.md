# Slice 7: Destination receiving, pickup and delivery - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4); the plan's slice 8 "Delivery and proof" |
| Date | 2026-10-21 |
| Builds on | Slice 6B (dispatch), 6C (operational events), 5C (counter, signature), 5F (invoices, cash drawers, credit), 4C (destinations, hubs, partners), 3E (reminders, unclaimed) |
| Source | V3 (OperationPlane), read on 2026-10-21: the last leg's arrival writes an internal "arrived at destination warehouse" event; customs, ready for pickup, out for delivery and delivered exist only as codes typed by hand; no receiving at destination, no pickup counter, no routes, no proof of delivery, no partner login |
| Status | **Agreed with the owner on 2026-10-21** (section 2). Steps 1 to 4 built and verified ([step 1](SLICE-7A-VERIFICATION.md), [step 2](SLICE-7B-VERIFICATION.md), [step 3](SLICE-7C-VERIFICATION.md), [step 4](SLICE-7D-VERIFICATION.md)) |

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
| R-12 | **The driver's record (owner, 2026-10-21, step 3): company setting.** Drivers use a phone page (their route, proof at the door, delivered or failed as they go), or staff enter the results from the printed run sheet when the driver is back; the company chooses. |
| R-13 | **Cash on delivery: company setting, off by default.** Off: a shipment goes out only once paid. On: it may go out unpaid; the driver collects what is owed and the cash is handed in at the warehouse's cashier when the route is settled. |
| R-14 | **After failed tries: company setting.** A number of tries (3 by default), then the parcel waits for pickup at the warehouse (the customer is told), or goes back to its origin, as the company chooses. |
| R-15 | **Proof at the door: company setting.** The receiver's name and a signature by default; a photo, the ID's last 4 characters and a delivery code switched on by the company. |
| R-16 | **Freight collect (owner, 2026-10-21).** A shipment leaves the origin only when it is paid, freight collect or billed on account; an unpaid prepaid shipment waits at the origin. Freight collect never stops the movement, only the hand-over at the destination until paid. Billed on account stops neither. The sender chooses freight collect when booking, when the company allows it. |

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

## 6. Settled while building step 3 (for the owner to confirm)

| # | Choice | Why |
|---|---|---|
| S-20 | **Settings → Delivery:** how drivers record (a phone page as they go, by default; or the printed run sheet, entered by staff afterwards), cash on delivery (off), the tries before giving up (3, from 1 to 10), what happens after the last (waits for pickup at the warehouse, by default; or goes back to its origin), the proof at the door (the receiver's name always; a signature by default; a photo, the ID's last 4 characters and a delivery code switched on), and the out-for-delivery email (on) | R-12 to R-15 as company settings |
| S-21 | **Waiting for delivery:** a home-delivery shipment's parcels at the warehouse it ends at, arrived there, not held by customs (and cleared when that warehouse clears customs), on no route; paid, unless the company takes cash on delivery; a payment waiting for confirmation or credit keeps it here. A parcel whose tries are used up (the company lowered them) is still listed: its next failure applies the rule | R-7, R-13 |
| S-22 | **A route:** a number (RT…), a warehouse, a day (yesterday to 30 days ahead), a driver who signs in (phone page) or a name only (run sheet), at most 80 stops, one stop per shipment; planned (stops added, moved, taken off; cancelled with a reason), started (*out for delivery*, the customers emailed), back once every stop is recorded, then settled | R-7, R-12 |
| S-23 | **Who may do what:** *plan delivery routes* (plan, start, enter the run sheet, settle, return parcels to the sender, bill destination charges) and *drive delivery routes* (see one's own routes, start them and record their stops on the phone page), per warehouse. A driver sees only routes given to them; settling is never the driver's | R-12, segregation of cash |
| S-24 | **At the door:** delivered with who received it and the proof asked (from the run sheet, the signature is on the paper and no photo is asked), and exactly what is owed in cash (less what was already collected on a route not settled yet, so it is never asked twice); a parcel held by customs or turned back after the route was planned cannot go out or be handed over; or not delivered with a reason (no one at home, refused, address wrong, no access, not paid, other with a note), shown to the customer | R-7, R-13, R-15 |
| S-25 | **The delivery code** is the shipment's pickup code (one code, at the door or at the counter): made as the route leaves and sent with the out-for-delivery email when the company asks for it; entries from the run sheet do not ask it (the driver cannot write it on the paper), kept protected, five wrong tries stop it | R-15; the same protection as S-14 |
| S-26 | **A failed try** steps the parcel back to *at the warehouse* and counts a try; after the last one it is *ready for pickup* there (storage and reminders as at the counter; the customer emailed once nothing of the shipment still waits for delivery) or marked *going back to the sender*, listed on the delivery desk until staff record it *returned to sender* as it leaves. The trip back itself is arranged by staff for now; a return dispatch to the origin is a later step | R-14 |
| S-27 | **Destination charges for home delivery** (duties, local taxes, local fee) are billed at the delivery desk before the round, on a destination invoice; paid at the cashier, or collected at the door with cash on delivery | R-4, R-9 |
| S-28 | **Settling a route:** the cash collected is paid into each invoice at the cashier first; then the cash handed in is counted, and a difference needs a note | R-13 |

## 7. Freight collect (owner, 2026-10-21)

Built after step 3, from R-16.

- **Settings** (the company page): *Allow senders to pay at the destination (freight collect)*. The old *allow collect* box meant something else and was never used: the update switches it off for every company, so freight collect is offered only once a company turns it on.
- **Booking:** the sender ticks *Pay at the destination* on the portal, or the clerk does at the counter; refused when the company does not offer it. It is part of the shipment's instructions (changeable while the shipment is open, fixed once booked).
- **The invoice** says *Freight collect: to pay at the destination* (PDF and cashier page).
- **Leaving the origin:** bundling accepts a freight-collect shipment unpaid (scan, ready-to-bundle list, consolidated shipments); other unpaid shipments wait, as before; billed on account already counted as released.
- **At the destination:** unchanged. The pickup counter releases once every invoice is paid; home delivery goes out once paid, or with cash on delivery the driver collects it.

Reviewed independently. Fixed: the old setting switched off; the parcel page's options show freight collect; a customer editing a freight-collect shipment after the company stopped offering it sees the choice and is told, rather than losing it silently; the cashier page names it; one rule (*may leave*) for the origin. Noted (low): the counter's other screens show the invoice as unpaid without naming freight collect.

Tested: `FreightCollect_MovesUnpaid_AndIsHandedOverOncePaidAtTheDestination` (web): refused while not offered; offered once on; booked unpaid, the invoice marked, bundled; an unpaid prepaid shipment not bundled; sent, ready at the destination, the release refused until paid.

## 8. Settled while building step 4 (for the owner to confirm)

The owner's answers (2026-10-21): a staff login with a partner role, limited to the partner's own warehouse; handling money is a company setting per partner warehouse; the partner's devices use the company's licences.

| # | Choice | Why |
|---|---|---|
| S-29 | **A partner agent is a staff member tied to one warehouse run by a partner** (*Invite a partner agent* on the staff page: name, email, language, the partner's warehouse). It holds the partner's rights there and nowhere else, and no role can be added to it | R-8: one fixed set of rights, never more by mistake |
| S-30 | **The partner's rights:** receive arrivals, record customs, release at the counter, plan and drive delivery routes, at that warehouse | R-8 |
| S-31 | **Money (company setting on the warehouse, *The partner takes payments here*, off by default).** Off: the agent sees no amounts, invoices or billing; the counter says whether something is still owed, and the company settles it; cash on delivery is never used from that warehouse. On: the agent also takes payments at the cashier of that warehouse (the cashier finds a customer only by a number of that warehouse, never by phone or email) | The owner's answer; no prices or invoices otherwise |
| S-32 | **Fails closed:** while the plan does not include *partner agents*, or Platform cannot say, or the warehouse is no longer run by a partner or is closed, an agent holds nothing, cannot be invited and cannot sign in. A plan change reaches Operations within 5 minutes | R-8 |
| S-33 | **Devices:** the agent registers its device like any staff member; it takes one of the company's licences and is approved in the company's Licences tab | The owner's answer |
| S-34 | **Drivers:** the partner's agents are offered as drivers at their warehouse | R-7 at a partner's warehouse |

