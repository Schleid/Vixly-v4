# Slice 5E: Shipping supplies - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-20 |
| Builds on | Slice 4B (price versions), 5C (the counter), 5D (the shipment estimate) |
| Source | The owner, 2026-10-20 evening: "customers can buy shipping supplies from the company; those purchases are part of the estimate and the final invoice" |
| Status | **Agreed with the owner on 2026-10-20; waiting for sign-off of this note before building.** Points marked *industry standard* follow current shipping-store practice (counter software such as ShipRite and PostalMate sells retail items and shipments on one ticket, scanned by barcode); each company can set them its own way |

## 1. What the owner said

- Customers buy **shipping supplies** from the company (boxes, tape, bubble wrap, bags). They are part of the **shipment estimate** and of the **invoice**.
- The front desk page stays as it is: a phone box and a Search button. Once the customer is found, **Order supplies** sits next to **New shipment**. While the clerk processes a shipment, **Add supplies** is on the shipment page.
- The clerk **scans** a supply's barcode or **looks it up**.
- **No supplies once a shipment is final.**
- The clerk **never changes a price and never gives an item free**; the clerk may give a **discount, as the company's policy allows** (a setting).
- **Full supplies inventory**, stock counting as a company setting, and a **pricing system**.
- Keep it simple: **no special prices per customer**.

## 2. Decisions

### Catalogue and prices

| # | Decision |
|---|---|
| S-1 | **A supply** has a name (in English, French and Spanish when given), an item code unique in the company, **one or more barcodes** (a supplier's code and the company's own label), a unit ("each", "roll", "pack of 10"), a category for the lists (boxes, tape, padding, bags, other), active or not, and whether it is **taxed** (supplies are usually taxed, freight often is not: billing uses this flag). |
| S-2 | **Prices** use the price versions of Slice 4B: a price with the date it starts, so a change can be planned ahead and the history is kept. Optional: another price for one warehouse, and a lower price from a quantity (e.g. 10 boxes or more). The price is **before tax**. No price per customer (owner). |
| S-3 | Prices → **Supplies** (staff with the right to manage prices): the catalogue, prices and barcodes; printing a barcode label for an item that has none. |

### Inventory (industry standard, *full* per the owner)

| # | Decision |
|---|---|
| S-4 | **Stock per warehouse.** Every change is a **movement** with who, when, the quantity, and why: received, sold, sale cancelled, adjusted (damaged, used by the company, found, count correction, each with a note), transferred out and in. The stock on hand is the sum of the movements: nothing changes it otherwise. |
| S-5 | **Receiving stock:** the warehouse, the items and quantities, the unit cost, an optional supplier and reference (a purchase invoice). The cost is kept for margin reports later. |
| S-6 | **Transfers** between the company's warehouses: sent, then received at the other end (stock is in transit between the two). |
| S-7 | **Stock counts:** a count for a warehouse (all items or some); the clerk enters what is on the shelf; on closing, each difference becomes an adjustment *count correction*. |
| S-8 | **Reorder level** per item and warehouse: below it, the item shows on a *low stock* list and on the warehouse's home page. |
| S-9 | **Company settings:** *Count supplies stock* (on: movements, counts and alerts; off: the catalogue and sales only); *When stock runs out at the counter*: refuse the sale, or allow it with a warning (default: allow with a warning, since the box is already in the customer's hands, and the stock shows below zero until corrected). |
| S-10 | **Rights:** *manage supplies* (catalogue, prices, barcodes: with prices); *manage supplies stock* (receive, adjust, transfer, count) per warehouse; selling is part of *serve customers at the counter*. |

### At the counter

| # | Decision |
|---|---|
| S-11 | **Add supplies** on the counter's shipment page while the shipment is open and not final. One box takes a **scan or text**: a scan (a USB or Bluetooth scanner types like a keyboard) adds one of that item at once, scanned again it adds another; text searches by name or item code. Each line: the item, quantity (changeable), unit price, discount, amount, remove. |
| S-12 | **The price is kept as it was when the line was added**, so a price change does not change this shipment's supplies. |
| S-13 | **No supplies on a final shipment** (owner). Since a shipment becomes final at its last parcel, the shipment page says before that parcel: *Supplies can only be added before the last parcel*. After it, the clerk uses **Order supplies**. |
| S-14 | **Order supplies** on the customer's page (next to New shipment): a supplies order for that customer, numbered (prefix set by the company), with the same scan-or-search box. Only for a customer found at the counter (no anonymous sales). It is paid with billing (Slice 5F); until then it is recorded as *to pay*. |
| S-15 | **Stock** moves when the shipment becomes final or the order is confirmed, from the warehouse of the counter; a line removed before that moves nothing. A cancelled order puts the stock back (*sale cancelled*). |
| S-16 | **In the shipment estimate:** a *Supplies* group with each item, quantity, discount and amount, counted in the total. The customer's portal shows the supplies the counter added; the customer does not add supplies on the portal. |

### Discounts by company policy

| # | Decision |
|---|---|
| S-17 | **Company settings:** whether clerks may give discounts; the largest discount a clerk may give, as a percentage per line and per order (always below 100%: no free items, owner); whether a reason is required. |
| S-18 | **Above the clerk's limit:** a person with the new right *approve discounts* (Owner and administrators by default) approves it at the counter with their own sign-in, as step-up. |
| S-19 | Every discount is kept with the line: who gave it, who approved it, the reason, and shown in the estimate and on the invoice. |
| S-20 | **Discounts apply to supplies in this slice.** Discounts on freight belong to billing (to decide there). |

## 3. Steps

| Step | Content |
|---|---|
| 1 | The catalogue, barcodes and prices (Prices → Supplies) |
| 2 | Inventory: receiving, adjustments, transfers, counts, low stock; the company settings |
| 3 | At the counter: *Add supplies* on the shipment, *Order supplies* for the customer, scan or search, in the estimate and the portal; discounts by policy with approval |
| 4 | Paying a supplies order: with the cashier in Slice 5F |

## 4. Not in this slice

- Prices per customer (owner: keep it simple).
- Supplies bought on the portal, and anonymous walk-in sales.
- Bundles of supplies (a "moving kit" of several items), purchase orders to suppliers, margin reports: later if asked.
