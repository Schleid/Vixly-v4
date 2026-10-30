# Slice 4: Charges engine - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-19 |
| Built on | Slice 3E; not committed here: the dev team owns git |
| Design | [Slice 4 design note](SLICE-4-CHARGES-DESIGN.md), from the owner's answers of 2026-10-19 01:31 |
| Status | **Built and verified by automated tests; waiting for owner sign-off.** The assumptions A-1 to A-9 (design note section 9) are to confirm |

## 1. What was built

### Prices (`/staff/charges`, new permission `ops.charges.configure`, menu "Prices")

- **Rate cards**, one per service, destination (or all) and customer (or the company's standard prices):
  - freight **by weight** (per lb or kg), with optional **weight bands** typed as `20 = 2.25`, one per line; or **by size** (per ft³ or m³);
  - **item prices** typed as `Barrel = 85`: a fixed price whatever it weighs (barrel, car, container);
  - the **small-package fee** and its weight limit (owner: "0.1 to 10");
  - the **minimum charge** per shipment, the **service fee** and the **consolidation fee**;
  - in use or not; never deleted. A duplicate card (same service, destination and customer) is refused.
- **Extras**: name; government fee passed through (shown apart, never discounted); a fixed amount or a percentage of the freight; per item, package or shipment; always, when the declaration names it (its words, matched like the restricted-items list), or only when staff add it; for a service and/or a destination, or all.
- **Insurance**: a percentage of the declared value with a minimum premium, or value bands (`100 = 3.00`); an optional highest insured value. Added only when the shipment is insured.

### The engine (one for quotes, the calculator, estimates and later invoices)

- Billable weight = the larger of the weight and the volumetric weight (the service's divisor), **rounded up to the next whole lb or kg**; 10 lb typed and stored in kg is still 10.
- Each package: its item price, else the small-package fee, else freight by weight (band price) or by size (rounded up to 0.01 ft³ or m³).
- Then the minimum adjustment (its own line), service fee, consolidation fee, extras, insurance. Each line rounded half away from zero; the total is their sum.
- Card choice: the customer's card for the country, then the customer's card for all countries, then the standard card for the country, then the standard card for all. Another customer's card never counts.

### Quotes (`/staff/quotes`, new permission `ops.quotes.create`, menu "Quotes")

- Staff give the customer (optional), destination, service (or every service offered), consolidated, insured, up to five packages (weight, size, value, item type, contents) and the quantity of adjustable extras.
- With a customer, each priced service can be **saved as a quote** (`QT26000001`), valid 1 to 90 days, with an optional **preferred total** and its reason: lower only, never below the government fees, its own line. Pressing twice saves one quote. A quote can be withdrawn.
- The customer sees their valid quotes on the portal, with "Your preferred price"; never the reason or who made it.

### Customer side (`/customer/rates`, menu "Rates")

- The **calculator**: destination (the company's destinations), weight, size, declared value, consolidated, insured. An estimate for each service with a price, with the customer's own card when there is one.
- The **shipment estimate** (link "Estimate the charges" on the shipment page): its measured packages to its destination, with the number of packages not measured yet.
- Always marked: "Estimate: the final amount is set when your packages are measured at our warehouse."

## 2. Database

- New tables `ops.RateCards`, `ops.ChargeExtras`, `ops.InsurancePlans`, `ops.Quotes` (bands, items, pieces and lines as JSON columns; row versions; unique indexes for one card per service, destination and customer, one extra name, one plan, one quote number and one form reference per company).
- Migration `ChargesEngine`, in the global script `build/scripts/sql/vixly-v4-database.sql` (19 Platform, 16 Operations, 1 Customer migrations). Tested on an empty server (run twice) and over the 3E script's database (run twice).

**On the development machine: run the global SQL script once before starting the sites.**

## 3. Automated tests

| Test | What it proves |
|---|---|
| `OperationsRatingEngineTests` (8, Application) | Air by weight with rounding, bands and volumetric weight; small-package fee, minimum, service fee; ocean by size, barrel price, consolidation; CONATEL per item from the declaration, a percentage, a manual extra, a government fee that cannot be removed; insurance by percent with a minimum, by bands, refused above them and without a plan; card choice; preferred price rules; bad prices refused |
| `Prices_AreSet_AndAQuoteUsesThem_WithAPreferredPrice` (Web) | Cards, an extra and insurance set on the page; duplicate card, bad band and unknown customer refused; the quote uses the customer's card, the CONATEL fee and insurance (41.00); a walk-in price (42.50); a preferred price below the government fee refused; 38.00 saved with its reason, once on a double click; withdrawn; a person without the permissions sees neither menu entry nor the page |
| `Customer_EstimatesRates_SeesTheirQuotes_AndOnlyTheirOwnPrices` (Api, through Customer.Web) | The calculator with the standard price (7.50) and another customer's own card (1.50) never mixed; quotes shown to their customer only, without the reason; bad weight and an unserved destination refused; a shipment without a destination has no estimate yet; another customer's shipment is not found |

All suites green: Operations (Domain 185, Application 84, Infrastructure 62, Web 77, Api 24, Customer.Web 40) and the architecture tests (10,247).

## 4. Independent review (2026-10-19)

A separate reviewer read the change without having written it. Card selection, tenant isolation, permissions, the JSON mapping, the unique index and the rounding were found correct. Its findings and what was done:

| # | Finding | Fix |
|---|---|---|
| M1 | A quantity of 0 sent for a government fee removed it from a quote | Staff give quantities only for extras added by hand or counted per item from the declaration; a government fee is never counted below the declaration (test) |
| M2 | Pricing every service ignored the extra quantities, so the saved quote could differ from the lines shown | The quantities are used for every service |
| M3 | A double click saved two quotes | The save form carries a one-time reference with a unique index (test) |
| M4 | The design note said saving emails the customer | The email waits for Slice 5 notifications; the note says so (A-7) |
| L5 | A quote could be saved as insured without an insurance line | Refused with a message; "insured" without a plan in use counts as not insurable |
| L6 | Very large but allowed values could overflow the quote columns | Package limits lowered (weight 10,000, sides 2,000) and money columns widened |
| L7 | Customer quotes were shown in the company's current currency | Each quote carries its own currency |
| L8 | A shipment estimate never uses item prices | Parcels have no item type yet; noted as A-9, for Slice 5 |
