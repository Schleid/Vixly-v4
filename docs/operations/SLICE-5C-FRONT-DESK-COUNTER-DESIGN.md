# Slice 5C: Front desk counter - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-20 |
| Builds on | Slices 3 (receiving, inspection), 4 (charges), 4D (shipments from the portal), 5A (shipments desk, documents), 5B (insurance) |
| Source | The owner's walk-through of V3 with Claude, 2026-10-20 (front desk, cashier, bundles); the owner asked for the build the same night |
| Status | **Step 1 built from the owner's answers of 2026-10-20.** Points marked *to confirm* are Claude's reading where the owner was not asked |

## 1. What the owner said (2026-10-20)

- A registered customer walks up to the counter with packages. If they already created a shipment on the portal, the clerk adds the parcels to it. If not, **the clerk creates the shipment** and adds the parcels. The clerk never registers customers.
- The clerk finds the customer by **phone or email**.
- The clerk records the destination, the recipient, consolidation yes or no, insurance yes or no, the service type, and **how many parcels** the customer has. Parcel entry then accepts exactly that many.
- Each parcel is **weighed and measured** at the counter, with **a description of what is shipped** and its value. Without that, no commercial invoice and no payment.
- **When the last parcel is entered, the shipment is final.** It can no longer be edited, or the commercial invoice would be wrong. The clerk can tell the customer the amount for the whole shipment.
- More boxes than declared means **a new shipment**.
- On the portal, the customer can **change the parcel count**: an online order may arrive as 4 cartons instead of 3. The warehouse clerk calls the customer before entering anything, and the customer updates the count.
- The clerk sees all of the customer's shipments until they are archived.
- Not everyone sees everything about a customer: sections and links follow each person's rights.
- The cashier finds the customer the same way and sees their invoices, each with a Pay button (with billing, section 6).

## 2. Decisions

| # | Decision |
|---|---|
| C-1 | **Every shipment has a parcel count** (1 to 99), stated by the customer on the portal or by the clerk at the counter. The customer may change it while the shipment is open, never below the parcels already in it. Staff set it when creating a shipment at the counter, and may correct it until the first parcel is entered. A portal shipment's count is the customer's statement: staff do not change it. |
| C-2 | **Final ("booked") automatically.** A shipment becomes final when all of these hold: its parcels with weight and size reach the count; none of them is on hold or waiting for a compliance inspection; no package the customer announced in it is still on its way; and it has a destination, a recipient and a service. Then its status becomes **Booked**, the **amount** (the price of its service for those parcels, from the company's prices) and the time are recorded, and a *booked* event is written (milestone Booked, ADR-0018). |
| C-3 | **A booked shipment is locked.** Its instructions, service and count cannot change; parcels cannot join or leave; the customer cannot cancel it. Corrections by a person with authority (reopen with a reason, recorded) come with billing: *to confirm with the owner*. |
| C-4 | **More parcels than declared** (a dock delivery): the parcel still joins the shipment, as the dock cannot refuse a box. The shipment is shown as *more parcels than declared* and does not become final until the customer raises the count. |
| C-5 | **The counter** (`/staff/counter`, menu *Front desk*) starts with one search box: **phone, email or customer code**. It shows the customer (name, code, phone, email, status) and **all their shipments**, with the parcels entered against the count, the status and the amount when booked. **+ New shipment** is beside them. |
| C-6 | **A new shipment at the counter** uses the same fields and rules as the portal: destination and pickup or home delivery, address, recipient and phone, the services the company prices for that customer, consolidation, refrigeration, insurance (the clerk confirms the customer accepted the terms), and the parcel count. The origin is the counter's receiving warehouse: the clerk picks it when they cover several. The customer needs no mailbox address there, as they bring the parcels in person. |
| C-7 | **A parcel at the counter:** weight, length, width and height in the company's units, a description and a declared value. It is received at that warehouse (*source: counter*), measured and declared in one step, and joins the shipment. The company's inspection rules and random share apply, as at the station. No photo is required: the customer is present and the box is checked in front of them. *To confirm with the owner.* |
| C-8 | **The amount** is shown live while parcels are entered, using the same prices as the calculator and the quotes. It is fixed when the shipment becomes final. |
| C-9 | **Rights.** A new permission, *serve customers at the counter* (`ops.counter.serve`), opens the counter for the people who hold it. It is given to the Front desk role; the Owner and administrators always have it. The counter shows contact details and shipments only; financial details come with billing, under their own permission. |

## 3. Steps

| Step | Content |
|---|---|
| 1 | Parcel count (portal and counter); finalize and lock; the counter: lookup, customer's shipments, new shipment, parcel entry, live amount, *booked* event; the shipments desk and the portal show the count and the final state |
| 2 | Billing: the invoice from the final shipment (one per shipment, numbered from it), the cashier (lookup, invoices, Pay, receipt), payment terms (prepaid, collect at destination, on account); correction of a final shipment with a reason |
| 3 | Bundles (the ship manager's bundles, scanning into them, closing with weight and size), then dispatch |

## 4. Not in this slice

- Customer registration at the counter (never: the owner).
- Billing and payments (step 2, with the cashier).
- Archiving shipments.
