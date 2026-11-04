# Slice 4C: Routes, transport modes, customs and insurance - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-19 |
| Built on | Slice 4B; not committed here: the dev team owns git |
| Design | [Slice 4C design note](SLICE-4C-ROUTES-AND-MODES-DESIGN.md), approved by the owner on 2026-10-19 with Q-C1 to Q-C4 as proposed |
| Status | **Built and verified by automated tests; waiting for owner sign-off** |

## 1. What was built

### Destinations are warehouses (the Zones page is gone)

- A warehouse is a **destination** when it is a pickup point (its role) or **delivers home** from there. The warehouse form gains "As a destination":
  - **run by** the company or a **partner** (name, phone, email; the name is required for a partner);
  - **served through** a hub: another warehouse in use with the hub role, in the same country (Jérémie through Les Cayes);
  - **home delivery** and the **communes** it delivers to (empty: anywhere in its country).
- An **origin** is a warehouse in use with the receiving role, where packages are dropped off.

### Prices per route, one mode at a time (`/staff/charges?mode=air|ocean|ground`)

- **Air, Ocean, Ground tabs.** Each tab shows only its price lists, its extras (and those set for every mode) and its insurance. Item types are on Ocean only.
- A **price list** is for a service, a **route** (from an origin, or any; to a destination, or any) and a customer (or the standard prices), with its **customs** policy: duties included or transport only.
- **Engine choice:** the customer's lists from the most specific route (both ends, any origin to the destination, the origin to any destination, any route), then the standard lists the same way; only lists with a version in force on the pricing day.
- **Air and ground** are priced by weight, **ocean** by size and by item. The service decides; there is no "charged by" choice any more.
- **Ground:** any bands, a **flat price per package**, a **flat price per shipment** (its own line), or both; bands may be left out when a flat price is set.
- **Extras** apply to one mode or all, and may be limited to an origin and a destination.

### Insurance per mode, with full terms

- One plan per mode. Each version has the price (percent with a minimum, or value bands; the most insured per shipment) and the **terms**:
  - who covers: the company, or an insurer (name required, policy number, contact);
  - covered risks: loss, damage, theft;
  - exclusions: nine standard ones on or off (cash, jewelry, perishables, live animals, hazardous goods, fragile items not packed by the company, used electronics without proof, improper packing, customs seizure) and the company's own;
  - liability without insurance: per lb/kg and/or per shipment, or none;
  - payout on the declared value or the lower of the declared and invoice value; deductible (amount or percent); the most paid per item (a package worth more cannot be insured);
  - claims: days for damage and for loss, the proof required, who files;
  - the company's own text in English, French and Spanish.
- A new plan starts with the industry defaults: own cover of loss and damage, the standard exclusions, the lower of declared and invoice value, 7 days for damage and 21 for loss, invoice and photos, filed by the company.

### Customer side

- The shipment form asks **"Deliver to"** (the company's destinations), then **pickup there** or **home delivery** in its area. Companies without destinations keep the branch choice of Slice 3C.
- When an insurance plan is in use, the form shows the **terms of each mode** in the customer's language and **"I accept the insurance terms"**. Insured without accepting is refused; the acceptance time is kept on the shipment (Q-C1 to Q-C4 as proposed).
- The calculator asks **from** (only when the company has several origins) and **to**.

### Recorded for later slices

- Release of a transport-only shipment needs the recipient's customs receipt (destination arrival).
- No dispatch without payment: paid, pay at destination, or billed to a credit account (dispatch, Slice 5).

## 2. Database

- `ops.Warehouses`: `DeliversHome`, `HomeArea` (JSON), `HubWarehouseId`, `OperatedBy`, `PartnerName`, `PartnerPhone`, `PartnerEmail`.
- `ops.RateCards`: `OriginWarehouseId`, `DestinationWarehouseId`, `Customs`; one list per service, route and customer (any counts as a value). Versions gain the ground flat prices (JSON).
- `ops.ChargeExtras`: `Mode`, `OriginWarehouseId`, `DestinationWarehouseId`.
- `ops.InsurancePlans`: `Mode`, one plan per company and mode; versions gain the terms (JSON).
- `ops.Shipments`: `DestinationWarehouseId`, `InsuranceAcceptedUtc`. `ops.Quotes`: `OriginWarehouseId`, `DestinationWarehouseId`.
- `ops.Zones` is dropped.
- Migration `20261019184840_RoutesAndModes` converts the 4B data: each zone goes to a warehouse (a pickup point of its pickup branches, else its branch's warehouse, else its hub). The warehouse takes what **all its zones in use** offered together: pickup, home delivery, their cities (anywhere in the country when one of them named none) and a hub; a zone not in use changes nothing. Price lists, extras, quotes and shipments follow.
- A zone that maps to no warehouse: its price lists and extras are kept, **not in use**, as lists for any destination. Check them on the Prices page and give them a route, or leave them out of use. The insurance plan becomes the air plan with the standard terms, copied for ocean and ground.
- In the global script `build/scripts/sql/vixly-v4-database.sql` (19 Platform, 18 Operations, 1 Customer migrations). Tested on an empty server (run twice) and from the Slice 4 database with prices, extras and insurance, upgraded to 4B then 4C (run twice).

**On the development machine: run the global SQL script once before starting the sites.**

## 3. Automated tests

| Test | What it proves |
|---|---|
| `OperationsRatingEngineTests` (Application) | Route choice from the most specific (Miami → Jérémie, any → Jérémie, Miami → any, any) for the customer then the standard lists; a list not yet in force never hides one; customs policy; ground flat per package and per shipment, both with bands; flat prices refused on air; an ocean extra never on air; destinations: pickup, home delivery in the area, outside refused, partner name and hub rules; insurance defaults, insurer terms per language, the most paid per item refusing a valuable package |
| `Locations_AreListedUnderTheirOwnCountry...` (Application) | Les Cayes (hub, pickup, home delivery in two communes) and Jérémie (partner, through Les Cayes) saved; a hub abroad or without the role, and a partner without a name, refused |
| `PricesByModeAndRoute_InsuranceTerms_AndAQuoteOnARoute_WithAPreferredPrice` (Web) | Jérémie set as a partner destination on the warehouse form; one tab at a time (ocean item types only on Ocean, an air extra not on Ocean); air lists per route with customs, refusals (duplicate, bands, customer, route); ground flat price; insurance with an insurer; scheduled version; quotes Miami → Jérémie (41.00), to Les Cayes (47.50), a walk-in from the only origin (42.50), home delivery refused at a pickup-only destination; preferred price; units locked; no access without permission |
| `Customer_ChoosesTheDestination_PickupOrHomeInItsArea_AndAcceptsTheInsuranceTerms` (Api) | "Deliver to" lists the destinations; the insurance terms shown (insurer, exclusions); pickup at Jérémie saved; insured refused until the terms are accepted, then the acceptance time kept |
| `Customer_EstimatesRates...` (Api) | The calculator to a destination; another customer's list never used; an unknown destination refused |
| `Ground_ByWeight_AFlatPrice...` (Application, review) | Ground with only a price per shipment: one line, no 0.00 line per package; a flat price of zero never stands in for the bands |
| `Destinations_Offer...` (Application, review) | The acceptance time is renewed with each acceptance, kept by a change that gives none, removed when no longer insured |
| `Locations_AreListed...` (Application, review) | A circle of hubs is refused (Jacmel through Les Cayes, so Les Cayes not through Jacmel) |
| Upgrade script (review) | Two zones in use on one warehouse give it both zones' communes, without duplicates; a zone not in use changes nothing; the hub never the warehouse itself |

All suites green: Operations, Platform, Public and the architecture tests.

## 4. Independent review (2026-10-19)

An agent that had not seen the work reviewed the change. Its findings and what was done:

| # | Finding | Done |
|---|---|---|
| 1 | The migration gave a warehouse the pickup and home delivery of a zone **not in use** | Only zones in use change a warehouse |
| 2 | A warehouse with **several zones** took only the first one's cities | All its zones in use together: pickup or home delivery from any, all their communes, anywhere when one named none |
| 3 | Saving an extra whose service or warehouse is **no longer listed** on the tab widened it to every service or route | The current service and warehouses stay chosen ("the current one, no longer offered here") |
| 4 | The insurance acceptance was **pre-ticked** when changing a shipment, and its time never renewed | Never pre-ticked; the time is that of the latest acceptance |
| 5 | A plan in use with **no terms in force yet** (only scheduled) still asked for an acceptance | Only plans with terms in force on the day |
| 6 | Lists of a zone mapped to no warehouse became lists for any destination | Kept not in use, written in section 2 so the company checks them |
| 7 | Removing a scheduled extra amount went back to the Air tab | It goes back to its tab |
| 8 | The hub select lost a hub that no longer qualifies; **circles of hubs** were possible | The current hub stays chosen (the save says why); a circle is refused |
| 9 | Ground with only a price per shipment showed a **0.00 line per package**; a flat price of zero passed | No line per package then; a flat price above zero is needed without bands |

All suites green after the fixes. The upgrade script tested again: empty server (twice), and from Slice 4 with prices, extras and insurance, then 4B, then several zones on one warehouse and zones not in use, upgraded to 4C (twice).
