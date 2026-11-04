# Slice 5E: Shipping supplies - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-20 |
| Built on | Slice 5D (package 74); not committed here: the dev team owns git |
| Design | [Slice 5E design note](SLICE-5E-SUPPLIES-DESIGN.md), approved by the owner on 2026-10-20 |
| Status | **Steps 1 to 3 built, reviewed independently, fixed and verified by automated tests; waiting for the owner's look.** Step 4 (paying a supplies order) comes with the cashier in Slice 5F |

## 1. What was built

### Step 1: the catalogue and prices (S-1 to S-3)

**Supplies** in the menu (`/staff/supplies`, for the people who set prices):

- **A supply:** item code (letters, digits, `- . /`, kept in capitals), name in English, French and Spanish, unit ("roll", "pack of 10"; empty means each), category (boxes, tape, padding, bags, other), taxed or not, in use or not.
- **Barcodes:** up to five per supply (a supplier's and the company's own). One code per item in the whole company: no barcode may be another supply's item code or barcode. The item code is also scanned like a barcode.
- **Prices** are versions, as freight prices are (Slice 4B):
  - a unit price before tax;
  - optional lower prices from a quantity (each row a larger quantity and a lower price);
  - optional prices per warehouse, which replace the company price there, quantity prices included;
  - the day it starts; a planned price can be removed before it starts.
- **Shelf labels:** a Letter sheet of 30 labels (2⅝ × 1 in) with the name, the price today and the item code as a barcode.
- No price per customer (owner: keep it simple).

### Step 2: the stock (S-4 to S-10)

**Supplies stock** in the menu (`/staff/supplies/stock`, right *keep the supplies stock*, given per warehouse):

- **Stock per warehouse.** Every change is a recorded movement with who, when, how many and why. A movement is never changed or removed. The stock on hand moves only with movements.
- **Receive supplies:** up to six lines per form with their unit cost, the supplier and the invoice or delivery note.
- **Adjust:** more or fewer, with a reason: damaged, used by the company, found, or correction and other (these two need a note).
- **Send to another warehouse:** the stock leaves at once; the transfer shows *in transit* at both ends; the receiving warehouse marks it received and the stock arrives as sent.
- **Count:** for every supply or one category. The clerk enters what is on the shelf, saves as they go, then closes. Each difference becomes a *count correction*, measured against the stock at closing (count when the counter is quiet). A supply left empty is not touched; a count can be cancelled.
- **Reorder level** per supply and warehouse. At or below it, the supply is *low* on the stock page and on the staff home page of the people who keep that warehouse's stock.
- **Each supply's movements** at a warehouse (the last 200).
- **Company settings** (on the Supplies page):
  - *Count the supplies stock*, on by default: off means the catalogue and sales only;
  - *When an item has no stock left at the counter*: sell it with a warning (default; the stock shows below zero until corrected) or refuse the sale.

### Step 3: at the counter (S-11 to S-20)

- **Add supplies** on the counter's shipment page while the shipment is open:
  - one box takes a scan or text: a barcode or item code adds one at once, scanned again adds another;
  - a name matching several supplies lists them to pick from;
  - each line keeps the price version it was sold under, so a later price change never alters it, and quantity prices still apply as the quantity changes;
  - the page says supplies can only be added before the last parcel. Once the shipment is final the panel is read only, and adding is refused.
- **Order supplies** on the counter's customer page, next to New shipment:
  - for a customer in good standing; one open order per customer at a counter;
  - the same scan box;
  - **Confirm** gives it its number (prefix SO, a company setting under Settings → Numbering) and moves its stock; it is then *to pay* at the cashier (Slice 5F);
  - **Cancel** puts back the stock of a confirmed order.
  - The customer page lists the latest orders.
- **Stock:** a shipment's supplies are sold, and their stock moves, when the shipment becomes final; they are cancelled with the shipment. Selling more than the stock warns the clerk, or is refused, as the company chose.
- **Discounts by company policy** (Supplies → settings):
  - whether clerks may discount; the largest discount per line and per order without approval (10% each by default); whether a reason is required (yes by default);
  - **above the limit**, a person with the new right *approve supplies discounts* (per warehouse; owner and administrators by default) approves on the clerk's screen with their email and the code from their authenticator app. A person who holds the right approves their own;
  - **prices never change at the counter and no item is free:** a discount is always below 100%;
  - every discount keeps who gave it, who approved it and why. Refused approvals are recorded in the history.
- **In the shipment estimate:** a line per supply with its quantity and discount, counted in the total and in every *what if* total; on the counter and on the customer's portal (the customer sees the supplies the counter added and cannot add any).
- No freight discounts at the counter (owner, F-9): they come only through special quotes.

## 2. Database

The global script adds:

- **Supplies** and **SupplyBarcodes** (item code and barcodes unique per company; prices as JSON versions);
- **SupplyMovements** (append only), **SupplyStocks** (one row per supply and warehouse, with a row version), **SupplyTransfers**, **SupplyCounts**;
- **SupplySales** (a shipment's supplies or an order; lines as JSON; an order number unique per company);
- **TenantSettings:** `CountSupplyStock` (on), `SupplyStockOut` (allow with a warning), `SupplyOrderPrefix` (SO), `SupplyDiscountsAllowed` (on), `SupplyDiscountLinePercent` and `SupplyDiscountOrderPercent` (10), `SupplyDiscountReasonRequired` (on).

Checked: the script runs on an empty database twice, and twice on a database upgraded through every earlier package to package 74 with demo data. The only data step (a company already using SO as a prefix gets SPO) runs inside `EXEC`.

## 3. Independent review and fixes

An independent review read the whole slice: no high finding; the permissions, the warehouse scopes, the forms and the migrations were confirmed. Fixed:

| # | Finding | Fix |
|---|---|---|
| 1 | A discount given within the limits could exceed the order limit after a quantity change or a line removed | Every add, quantity change and removal checks the order's discounts again; beyond the limits it asks for an approval |
| 2 | Typing a name with a space ("bubble wrap") at the counter showed the error page | The text is searched by name when it cannot be a barcode |
| 3 | An order of supplies always went to the clerk's first counter | The clerk picks the counter's warehouse when they serve at several |
| 4 | An approver's code could be tried again and again | A wrong code counts as a failed sign-in of the approver; a locked account approves nothing |
| 5 | A clerk holding the approve right approved their own discount with an open session alone | They type their own email and code too, like any approver |
| 6 | A refused approval still saved the new quantity | A refused approval changes nothing; only the failure is recorded |
| 7 | Two first scans at once could open two supply lists on one shipment, or a list could open as the shipment became final | One live list per shipment (unique index); each change touches the shipment so a booking at the same moment clashes |
| 8 | The insurance choices' totals left out the supplies | They count them, as the estimate does |
| 9 | A count could correct against a stock read a moment earlier | It reads the stock row it corrects |
| 10 | The settings history did not show the discount policy before a change | It does |
| 11 | The new SO prefix could clash with a prefix a company already uses | Such a company gets SPO |
| 12 | The counter's customer page listed orders from every warehouse | Only those of the clerk's counters |

Left as they are, for billing (Slice 5F):

- Cancelling a confirmed order needs no reason or approval yet. Once orders are paid, a cancellation will go through a credit note.
- Sending supplies to another warehouse does not warn when it takes the stock below zero.
- A form sent twice at the very same instant can be refused with "someone changed this at the same time" rather than ignored.
- The design note's S-18 said "with their own sign-in": the approver types their email and authenticator code on the clerk's screen, which is the same second factor without a new session. Catalogue rights are the prices right (S-10); a warehouse price replaces quantity prices there (S-2).

## 4. Automated tests

| Suite | Result |
|---|---|
| Operations (domain, application, infrastructure, web, API, customer web, architecture) | All pass: domain 205, application 91, infrastructure 65, web 92, API 29, customer web 40, customer infrastructure 1, architecture 11 536 |
| Platform | All pass (kernel 85, application 235, domain 307, infrastructure 63, web 43, API 21, UI 9) |
| Public | All pass (23) |

New for this slice:

- Domain: codes in capitals and scanned either way; prices with quantity and warehouse prices and a later day; stock moved only by movements; transfers and counts; a sale's lines, discounts below 100%, confirm and cancel.
- `Supplies_WithBarcodesAndPrices_OneCodePerItem_AndShelfLabels` (web): created once when sent twice; one code per item; quantity prices must go down; a planned price removed; shelf labels; no access without the right.
- `SuppliesStock_ReceivedAdjustedTransferredAndCounted_ByMovementsOnly` (web): received once per form; adjusted with a reason; low stock on the stock page and the staff home; a transfer from Miami to Les Cayes; a count that corrects; the movements page; counting turned off refuses the actions; no access without the right.
- `Counter_SellsSupplies_OnTheShipmentUntilFinal_AndOnTheirOwn_WithDiscountsByPolicy` (web): scan by barcode and code, once per form; choosing among names; no stock adds with a warning; the estimate counts the supplies; discounts within the limit, above it refused, a wrong approval refused, the owner's approval with their code accepted, 100% refused; the last parcel sells the supplies and closes the panel; an order confirmed with its SO number and its stock, then cancelled with its stock back.

## 5. Styling

The new pages and parts are in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*.

## 6. For the owner

- Your demo company gets the six sample supplies at the next start (none in stock: receive some at MIA to try the stock and the counter).
- Paying an order of supplies comes with the cashier (Slice 5F); until then a confirmed order shows *to pay*.
- A collect shipment cannot have supplies (F-2); this applies once collect is built with billing.
