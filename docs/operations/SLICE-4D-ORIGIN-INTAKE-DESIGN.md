# Slice 4D: Origin intake, pre-alerts first, undeclared parcels - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-19 |
| Builds on | Slice 3B (dock scan, ID label), 3C (inspection, holds, placement), 3E (answers, reminders, unclaimed); corrects them |
| Owner input | 2026-10-19, 17:16 to 17:38: reception reviewed with the owner, answers in section 2 |
| Status | **Approved by the owner on 2026-10-19 (Q-D1 to Q-D5 as proposed)** |

## 1. Outcome

- A customer **pre-alerts first**, on its own, then groups pre-alerts into shipments. A shipment needs no pre-alert (goods dropped off by the customer).
- A customer has **any number of open shipments**, each with its own destination, recipient, route, mode and price. A cardboard maker ships 1,000 cartons he drops off to the Dominican Republic, and Amazon tape he pre-alerted to Peru, at the same time.
- At the dock the clerk only scans. A pre-alerted package goes to **the shipment its pre-alert is in**; it is never placed by guesswork.
- Inspection prints and sticks on the **shipping label** of every parcel that is cleared, beside the ID label printed at the dock.
- Packages nobody can place go to the **undeclared parcels** area: inventoried, aged by the company's rules, and decided (return to sender, dispose of, auction) with approval.
- Customers who miss a package open a **ticket**; staff search. Customers never search the warehouse.
- Staff get an **incoming packages** list: every pre-alert, filtered by expected day or month, received, not yet received, past its expected date.

This is the origin side only. Dispatch (payment rule, bundles, manifests) is Slice 5; pass-through hubs, destination arrival and release are Slice 6.

## 2. Owner answers (2026-10-19)

| # | Answer |
|---|---|
| A-1 | Staff move every unidentified package to **undeclared parcels** and inventory it, following industry practice |
| A-2 | At the dock the carrier barcode is scanned and matched to its shipment automatically when the customer did everything right: pre-alert, then a shipment holding it |
| A-3 | The pre-alert is created first, then the shipment. **A shipment does not require a pre-alert** (a customer dropping off his own goods) |
| A-4 | Flexibility: one customer, several shipments at once, to different destinations, mixing pre-alerted and dropped-off goods |
| A-5 | Inspection prints and sticks the shipping label on every parcel that is cleared |
| A-6 | Customers cannot search for a lost package; they open a ticket with what they know and staff find it. Customers create pre-alerts and shipments; staff take it from there |
| A-7 | An incoming packages list for staff, filtered by month, day, delivered, past delivery and so on |
| A-8 | Storage fee, return to sender, disposal or auction for packages nobody claims |
| A-9 | The handling unit is what the customer hands over: 1,000 cartons stacked in 4 packs of 250 are 4 parcels, 4 ID labels, 4 shipping labels, charged as 4 parcels by the greater of actual and volumetric weight |
| A-10 | Transport damage: photograph and notify; the company decides what follows (Slice 6) |
| A-11 | Pass-through hubs only scan in and out (Slice 6) |

## 3. What changes from Slices 3B to 3E

| Today | Slice 4D |
|---|---|
| A pre-alert is created **inside** a shipment that already has its instructions | A pre-alert stands alone (customer, carrier, tracking number, store, contents, value, expected date, origin warehouse). It can be put into, moved between and taken out of the customer's open shipments until its package leaves the shipment's origin |
| A parcel without an announcement joins the customer's one shipment *without instructions* at the warehouse | Kept, renamed **waiting shipment** (the owner's rule: a parcel always belongs to a shipment, even an unidentified one). Never offered to the customer as a shipment to send; the customer moves its parcels into a real shipment |
| Received and pre-alerted but not in a shipment: (not possible) | The parcel joins the waiting shipment with hold **awaiting shipment**; the customer is told to put the pre-alert in a shipment, and the hold ends when it does |
| One ID label at the dock | Unchanged; the **shipping label** follows at inspection (section 5) |
| Unidentified parcels in a staff queue | The **undeclared parcels** area with inventory, aging and dispositions (section 6) |
| Unclaimed days (default 30), then return or dispose | Free days, a storage fee, reminders, final notice, then return, dispose or **auction** with approval (section 6) |

Existing data moves as is: pre-alerts keep their shipment; shipments without instructions become waiting shipments.

## 4. Pre-alerts and shipments on the portal

- **Pre-alerts** (`/customer/packages`): announce a package (carrier, tracking number, store, contents, value, expected date, the warehouse it is shipped to). The list shows each one as *expected*, *received* (with the parcel) or *in a shipment*.
- **Shipments** (`/customer/shipments`): create one with where it goes (Slice 4C destinations), the recipient, the **service** (air, ocean or ground, from those priced on the route) and options; then add pre-alerts and received parcels from the same origin warehouse to it, or none.
- **Dropped-off goods:** a shipment can declare the pieces the customer brings to the counter (what is inside and how many packs). The counter receives them in Slice 5; each pack is a parcel (A-9).
- Several shipments at once. A pre-alert or a parcel is in one shipment at a time and moves only while the shipment is open and the parcel is still at its origin.
- A shipment cannot be sent with nothing in it; its price comes from its route and service (Slice 4C) once its parcels are measured.

## 5. Inspection and the shipping label

- The inspection station checks the parcel as today (condition, photos, weight and size, compliance draw).
- **Cleared** means: checked, no open hold, no compliance inspection pending, and in a real shipment (not the waiting one). Cleared at the station: the **shipping label** prints at once on the station printer and is stuck on.
- Cleared later (a hold answered, the customer puts it in a shipment): the parcel enters the warehouse's **labels to print** list; the clerk prints it when picking the parcel. The parcel page can print it again.
- The shipping label (4 x 6 inch, ZPL and PDF like the ID label): shipment number and barcode, piece *n of N*, origin and destination warehouse codes (MIA → JER), service, recipient name and city, pickup or home delivery, weight. No price and no customer contact beyond the recipient's name and city.

## 6. Undeclared parcels (industry practice, A-1 and A-8)

- **Arrival:** a package the dock cannot place prints its ID label as *UNIDENTIFIED* (as today) and is moved to the **undeclared area**, a storage zone of the warehouse.
- **Inventory:** the clerk records what the label shows: sender, the name or suite code written on it, and photos of the label and the package. The shelf is recorded at putaway.
- **Two outcomes:**
  - *owner found* (suite code or name on the label, checked by staff): the parcel is **assigned** to the customer, who is asked to declare it and put it in a shipment (hold *awaiting customer information*, as today);
  - *owner not found*: the parcel stays undeclared and ages.
- **Aging, per company** (settings, Owner or administrator):
  - free days (default 30);
  - a storage fee per day after that (recorded on the parcel; charged when the parcel is shipped or paid with the dispatch payment in Slice 5);
  - reminders to a known customer at the company's days, and a final notice (as 3E);
  - after the final day: **return to sender**, **dispose of** or **auction**, decided by the Owner or an administrator with a note, and recorded as a milestone.
- **Count:** a monthly count of the undeclared area lists what should be on each shelf; the clerk marks found or missing; a missing parcel needs a manager's note.
- Every step is a tracking event (ADR-0021).

## 7. Tickets: a missing package (A-6)

- The customer opens a ticket from the portal: carrier, tracking number, store and order number, delivery date, the carrier's proof of delivery (photo or PDF), what is inside.
- Staff see open tickets at the warehouse, with the undeclared parcels and pre-alerts that resemble it (same carrier, tracking number end, date). Staff link the parcel (the assignment of section 6) or answer with a note.
- The customer sees the ticket's status and answers; nothing in the warehouse is shown to the customer before staff link it.
- This is the first ticket kind; others (billing, damage) can follow later on the same model.

## 8. Incoming packages (A-7)

`/staff/incoming` (permission `ops.parcels.view`, scoped to the user's warehouses):

- every pre-alert of the warehouse with customer code, carrier, tracking number, store, expected date, status and shipment;
- filters: expected today, this week, a day or month; received; not yet received; **past its expected date**; carrier; customer;
- counts per filter, and export to CSV.

"Delivered" here means received by the warehouse. Carrier tracking feeds (UPS, FedEx and others) can fill "delivered by the carrier" later.

## 9. Questions for the owner

| # | Question | Proposal |
|---|---|---|
| Q-D1 | Does the customer choose the **service** (air, ocean, ground) when creating the shipment? | Yes: the shipping label and the price need it; staff can change it before dispatch |
| Q-D2 | The shipping label size | 4 x 6 inch, the industry standard for parcels |
| Q-D3 | The storage fee | A daily amount per parcel after the free days, set by the company; none by default |
| Q-D4 | Auction | Recorded as a decision with a note (who bought, for how much is optional); Vixly does not run the auction |
| Q-D5 | Reminders count from receipt (as 3E) | Kept |

## 10. Not in this slice

- Counter drop-off (Slice 5), with the packs of section 4.
- Dispatch payment rule, consolidation, bundles, manifests (Slice 5).
- Pass-through scans, destination arrival, transport damage, release with the customs receipt (Slice 6).
- Partner access (later).
