# Slice 4: Charges engine - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-19 |
| Builds on | Slice 1 (shipping services, units, currency), Slice 2 (customers), Slice 3 (parcels with weight, size and declaration; customer shipments with options); [Operations plane](OPERATIONS-PLANE.md) stage 4; V3 review section 2.8 and findings 3.4, 11 |
| Owner answers | 2026-10-19, 01:31 (Project doc `slice-4-owner-answers.md`), all eight questions |
| Status | **Built overnight 2026-10-19** from the owner's answers; see the [verification record](SLICE-4-VERIFICATION.md). The assumptions in section 9 wait for the owner's review |

## 1. Outcome

- Each company keeps **rate cards**: one per shipping service, for all destinations or one country, with the company's prices. A customer can have **its own card** (special prices).
- One **rating engine** turns a shipment's pieces (weight, size, declared value, item type, contents) into charge lines: freight, small-package fee, minimum, service fee, consolidation, extras and insurance. The same engine serves the staff quote, the customer's calculator and estimate, and later the invoice (Slice 9).
- Staff **quote** a shipment for a customer and can offer a **preferred price**; the customer sees the quote.
- Customers see a **rate calculator** and an **estimate** on their shipments, always marked as an estimate.

## 2. What V3 did, and what changes

- V3 had air tiers, sea rules by item type, "operational charges" and insurance tiers in separate resolvers; the consolidation fee applied only to a charge literally named "Consolidation" (finding 11); the air weight mixed kg and lb and ignored the divisor (finding 3.4); there was no currency.
- V4: one engine, one card per service, the company's units and currency throughout, the consolidation fee as its own field, and the billable weight from the service's own volumetric divisor.

## 3. Rate cards (`/staff/charges`, `ops.charges.configure`)

Each card has:
- the **service** (air standard, air express, ocean consolidated, ocean full container, ground) and the **destination**: all countries the company serves, or one;
- for **one customer** or the company's standard prices;
- **how freight is charged**:
  - **by weight**: a price per lb or kg, optionally in **weight bands** (from 0, from 20, from 50 ...: the band of the billable weight sets the price per unit);
  - **by size**: a price per cubic foot (inch companies) or per cubic metre (cm companies);
  - **item prices** on any card: a fixed price per item type, whatever it weighs (a barrel, a car, a 20 ft container). A piece given that item type pays its fixed price;
- **small-package fee** (owner): a flat fee for a piece whose billable weight is at most a limit (for example 10 lb);
- **minimum charge** for the shipment on this service (owner): freight below it is raised to it, as its own line;
- **service fee**: a flat amount per shipment (V3's "service charge");
- **consolidation fee** (owner): a flat amount per consolidated shipment;
- in use or not.

The engine picks, for a shipment's service and destination: the customer's card for that country, else the customer's card for all countries, else the standard card for that country, else the standard card for all countries. No card: the service has no price there, and it is not offered in the calculator.

**Billable weight** of a piece = the larger of its weight and its volumetric weight (length x width x height / the service's divisor), in the company's unit, **rounded up to the next whole lb or kg** (owner). Size is rounded up to the next 0.01 cubic foot or metre.

## 4. Extras (`/staff/charges`)

A fixed amount, "with some flexibility" (owner). Each extra has:
- a name shown on the line (for example *CONATEL fee*), and whether it is a **government fee passed through** (O-10: shown apart, never discounted);
- an **amount**, fixed or a percentage of the freight;
- **per** item, per piece or per shipment;
- **when** it applies: always; when the declaration names it (its words, the same matching as the restricted-items list: *iphone, iphones*); or only when staff add it;
- limited to a service and/or a destination country, or for all;
- in use or not.

Per item: the quantity is the number the staff or the customer give (one by default when the declaration names it). Staff can change the quantity or the amount on a quote.

## 5. Insurance (`/staff/charges`)

Configured "the way the insurer charges" (owner). One insurance plan per company, one of:
- **a percentage of the declared value**, with a minimum premium (for example 3 %, minimum 5.00);
- **value bands**: a flat premium for each band of declared value (up to 100: 3.00; up to 500: 10.00; ...); above the last band it is not insured;
- optionally a **maximum insured value** per shipment.

Insurance is charged only when the shipment is insured (the customer's option, Slice 3C).

## 6. The engine

Input: tenant settings (units, currency), the service and its divisor, the destination, the customer, the pieces (weight, length, width, height, declared value, item type, contents), consolidated, insured, extras added by staff.

Lines, in order: freight per piece (or the small-package fee, or the item price), the minimum adjustment, the service fee, the consolidation fee, extras, insurance. Amounts in the company's currency, rounded half away from zero to two decimals per line; the total is the sum of the lines.

The engine is pure (domain code, no database): the use cases load the cards and settings and call it, so the quote, the calculator, the estimate and later the invoice give the same result for the same input.

## 7. Quotes and preferred prices (`/staff/quotes`, `ops.quotes.create`)

- Staff choose the customer, the service, the destination, the pieces (or a shipment's measured parcels), consolidated and insured. The engine shows the lines.
- **Preferred price**: staff set a lower total, with a reason; it is its own line, and never below the government fees. The quote keeps the standard total and the preferred one. Staff can also give the quantity of an extra added by hand or counted per item from the declaration; a government fee is never counted below the declaration.
- A quote is valid for a number of days (default 14, at most 90) and gets a number (`QT26000001`). Saving it shows it on the portal under *Rates*; the email to the customer comes with the Slice 5 notifications (A-7).
- Shipments (Slice 5) and invoices (Slice 9) will use an open quote of the customer when it matches; until then a quote is informative.
- Every quote records who made it and the reason for a preferred price (audit).

## 8. Customer side

- **`/customer/rates`**: the calculator. The customer chooses the destination, gives the weight and size (in the company's units), the declared value, insured and consolidated, and sees an **estimate** for each service with a price, with the customer's own card when it has one. Marked "Estimate: the final amount is set when your packages are measured."
- **Shipment page**: once its packages are measured, an estimate for each service offered, with the same note.
- **Quotes**: the customer's quotes, with validity.
- Nothing shows another customer's prices.

## 9. Assumptions to confirm (owner)

1. **A-1** Weight bands are optional; a card without bands has one price per unit.
2. **A-2** "Small package 0.1 to 10": the flat fee replaces the per-weight freight for a piece whose billable weight is at most the limit, set per card (10 by default when the fee is set).
3. **A-3** The minimum charge applies to the freight of the whole shipment on that service (not per piece), as its own line "minimum charge adjustment".
4. **A-4** Item prices are on the card; a piece without an item type is charged by weight or size.
5. **A-5** Size rounds up to 0.01 cubic foot or metre.
6. **A-6** One insurance plan per company, for all services.
7. **A-7** Quotes are shown on the portal; the email and using a quote on the shipment wait for Slice 5.
9. **A-9** A shipment estimate prices its measured packages by weight or size: parcels have no item type yet (a barrel, a car), so item prices count in quotes and the calculator only, until Slice 5 records the item type at the counter or the station.
8. **A-8** Taxes (sales tax, VAT) stay out of Slice 4; government fees are extras passed through.

## 10. Data

| Class | Table | Notes |
|---|---|---|
| `OperationsRateCard` | `RateCards` | Service, country (null: all), customer (null: standard), basis, price per unit, weight bands and item prices (JSON), small-package limit and fee, minimum, service fee, consolidation fee, in use; one card per service, country and customer |
| `OperationsChargeExtra` | `ChargeExtras` | Name, government fee, amount, fixed or percent, per, when, words, service, country, in use |
| `OperationsInsurancePlan` | `InsurancePlans` | One per company: method, percent, minimum, bands (JSON), maximum value |
| `OperationsQuote` | `Quotes` | Number, customer, service, country, pieces and lines (JSON), standard and preferred totals, reason, valid until, made by |

## 11. Done when

- The engine gives the documented result for: air by weight with bands and rounding, the small-package fee, the minimum, ocean by size, an item price (barrel), consolidation, extras (fixed per item from the declaration words, percent of freight, government fee), insurance by percent with a minimum and by bands, a customer card over the standard card.
- Only `ops.charges.configure` changes prices; only `ops.quotes.create` makes quotes; a customer sees only their own estimate and quotes.
- Every new text in en, fr and es; no "Vixly" on any tenant page.
