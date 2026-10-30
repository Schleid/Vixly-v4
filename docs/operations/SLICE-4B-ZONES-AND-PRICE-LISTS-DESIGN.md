# Slice 4B: Destination zones and price lists - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-19 |
| Builds on | [Slice 4](SLICE-4-CHARGES-DESIGN.md) (rate cards, extras, insurance, quotes, customer rates); Slice 1 locations; Slice 3C customer shipments |
| Owner input | 2026-10-19, 09:55 to 10:22: the V3 Pricing Engine and Warehouse screens, and the answers recorded in section 2 |
| Status | **Approved by the owner on 2026-10-19 (Q-Z1 to Q-Z3 as proposed) and built**; see the [verification record](SLICE-4B-VERIFICATION.md). Zones keep their pickup branches in a JSON column and versions are JSON columns on their owner (section 9 planned separate tables) |

## 1. Outcome

- Each company defines its own **destination zones**, such as Les Cayes, Miragoâne and Jérémie, and prices each service **per zone**.
- A price list has **any number of bands** the company defines. Each band is a flat amount, a price per lb or kg, or a base plus a price per lb or kg above the band's start.
- Prices have **effective dates**: a new price list can be scheduled ahead, and a price list that has started never changes.
- **Hubs** are gateways: the last point before the flight or vessel, or the first point after customs. A hub receives, routes and can offer pickup. One hub serves several zones. Les Cayes, with its international airport, serves Les Cayes, Miragoâne and Jérémie.
- Each company keeps one list of **item types** (barrel, car, TV), used by its prices and chosen on the parcel.
- Vixly imposes no pricing structure. Every choice belongs to the company.

## 2. Settled with the owner (2026-10-19)

1. **Bands are the company's.** There may be 1, 2, 3 or n bands, starting wherever the company says, each flat or per unit. Vixly decides nothing ("it's not to Vixly to decide").
2. **Prices are per final destination.** The company configures the places it serves (Les Cayes, Miragoâne, Jérémie…) and what each costs. A package to Jérémie has its own price, as do Miragoâne and Les Cayes. Jérémie is a department, like a US county.
3. **The customer chooses the zone** from the company's list when creating the shipment.
4. **Pickup, home delivery or both** is each company's choice, per zone. Galeo offers pickup only.
5. **What a zone is:** the company's own named area of any size (a department, a city, a group of communes), the industry-standard "zone".
6. **The hub** is the last point before the vessel or flight, or the first point after customs. It can receive, route and offer pickup. Les Cayes, with the international airport, is the hub for Jérémie and Miragoâne.
7. **Customer special prices** are per zone too, and how they are used is company policy.

## 3. What V3 does, and what changes

| V3 | Flaw | V4 |
|---|---|---|
| Tiers per profile and "destination warehouse" | A place to deliver to and a building are the same record, so a department has to be entered as a "warehouse" | Zones (where the company delivers and what it charges) are separate from locations (buildings with an address and a role) |
| Tiers with From and To | Gaps between tiers: 10.4 falls between 0–10 and 11–20 | Bands have a start only and the billable weight is rounded up first, so every weight has exactly one band |
| Tiers FlatAmount or RatePerWeightUnit | kg and lb mixed in one calculation | Each company's unit (kg or lb) throughout; stored in kg |
| Effective date per tier | Each tier dated on its own, so a price list can be half old and half new | A whole price list has one effective date; scheduled versions; started versions are read-only |
| One role per warehouse (Dispatch Hub) | A place cannot be both a hub and a pickup point | Roles are combined: receiving, storage, hub, pickup point |
| Manager name, email and phone typed on the warehouse | Copies of staff data that go stale | The manager is a staff member, linked |
| Item types used by sea rules only | Air cannot price a TV or a barrel | One list of item types, usable by any service |

## 4. Destination zones (`/staff/zones`, `ops.charges.configure`)

A zone has:
- a **name** and a **code** (HT-JER), unique in the company;
- a **country**;
- optionally, the **cities or communes it covers**, used to check a delivery address and to suggest the zone;
- the **hub** that serves it, a destination location with the hub role (Les Cayes);
- **delivery offered**: pickup, home delivery or both;
- the **pickup branches** in the zone, when pickup is offered;
- in use or not (never deleted, so past shipments keep their meaning).

**Customer side:** the shipment form asks "Deliver to" and lists the company's zones in use for the shipment's country. The pickup branch must be in the zone, or the delivery address in a city the zone covers when the zone lists cities. The current choice of destination branch or address stays, now inside the zone.

**Existing data:** each destination branch of a company becomes a zone of the same name, served by that branch's hub. Shipments keep their branch and gain the zone.

## 5. Price lists (`/staff/charges`)

A **price list** is for one service and one zone (or all zones), for the company or one customer. It has versions:
- each **version** has an "effective from" date and its prices;
- a version applies until the next one starts;
- a **scheduled** version (starting later) can be changed or removed; a version that has started is read-only and can only be replaced by a newer one;
- the page shows **Current** and **Scheduled** versions, and the history.

**Prices of a version:**
- **bands**: any number, each with:
  - its start (in the company's lb or kg);
  - its kind: **flat** for the band, **per unit** on the whole billable weight, or **base plus per unit** above the band's start;
  - its amount (and the base for the third kind);
- for size-based services (ocean), a price per ft³ or m³, with the same three band kinds on volume;
- **item prices**, taken from the company's item types (barrel = 85);
- the minimum charge, the service fee and the consolidation fee (as in Slice 4).

**Engine choice** for a shipment's service, zone and customer, on its pricing date:
1. the customer's price list for the zone;
2. the customer's price list for all zones;
3. the standard price list for the zone;
4. the standard price list for all zones.

No price list means the service is not offered to that zone.

**Pricing date:** the day the shipment is booked. A quote keeps its own prices until it expires. Invoices (Slice 9) use the booked prices.

**Billable weight** is unchanged from Slice 4: the larger of the weight and the volumetric weight, rounded up to the next whole lb or kg. The rounded weight finds its band.

The Slice 4 small-package fee is now simply a first flat band; existing cards convert to it.

## 6. Extras and insurance

- Extras gain a **zone** and a **delivery** filter (for example, a home delivery fee in Jérémie only), next to service and destination country.
- Extras and the insurance plan get the same **versions with effective dates** as price lists, so a new CONATEL rate or a new insurer rate can be scheduled.

## 7. Item types (`/staff/charges`, item types tab)

- The company's list: a name, optional words that point to it in a declaration (*barrel, drum*), in use or not.
- Price lists price item types from this list, never typed names.
- The counter and the inspection station choose an item type on the parcel (Slice 5). Until then, shipment estimates use weight or size, as today.

## 8. Locations, improved over V3

- A **branch** keeps its side (origin, destination or both). A **warehouse** keeps its **roles as checkboxes**: receiving, storage, hub, pickup point. "Dispatch hub" becomes "hub", the gateway described in section 2.
- **Manager**: a link to a staff member instead of typed name, email and phone.
- **Time zone** on each location, for opening hours and pickup times; opening hours already exist.
- **Codes** stay unique per company (HT-CAY).
- **Zones served** are shown on the hub, so staff see that Les Cayes serves Les Cayes, Miragoâne and Jérémie.

## 9. Data

| Class | Table | Notes |
|---|---|---|
| `OperationsZone` | `Zones` | Code, name, country, cities (JSON), hub location, delivery offered, in use; unique code per company |
| `OperationsZonePickup` | `ZonePickups` | A zone's pickup branches |
| `OperationsPriceList` | `PriceLists` (from `RateCards`) | Service, zone (null: all), customer (null: standard), basis; one per service, zone and customer |
| `OperationsPriceVersion` | `PriceVersions` | Price list, effective from, bands and item prices (JSON), minimum, service and consolidation fees |
| `OperationsItemType` | `ItemTypes` | Name, words, in use; unique name per company |
| `OperationsChargeExtra`, `OperationsInsurancePlan` | (existing) | Versions with effective dates, zone and delivery filters |
| `OperationsShipment` | (existing) | Gains `ZoneId`; the zone's code is shown |
| `OperationsWarehouse` | (existing) | Gains storage role, time zone, manager user id |

One migration; the Slice 4 cards and the shipments' destinations are converted in it.

## 10. Questions for the owner

1. **Q-Z1 Destination country without zones:** may a company deliver to a whole country without defining zones (one "all of Haiti" zone created for it)? Proposed: yes, every country served gets a default zone the company can rename or split.
2. **Q-Z2 Moving a hub:** if Jérémie later gets its own hub, should open shipments keep the old route? Proposed: yes; the route is fixed when the shipment is booked (Slice 5).
3. **Q-Z3 Price list history:** how long are old versions shown? Proposed: always kept, and the page shows the last five by default.

## 11. Done when

- A company with two services and three zones (Les Cayes, Miragoâne, Jérémie through the Les Cayes hub) sets a different price list for each, with its own bands of all three kinds, and the engine picks the right one per zone and customer.
- A scheduled price list takes effect on its date. A started one cannot be changed. A quote keeps its prices.
- The customer chooses the zone. A pickup branch outside the zone, or a delivery mode the zone does not offer, is refused.
- Item prices come from the item-type list.
- Every new text in en, fr and es; no "Vixly" on any tenant page.
