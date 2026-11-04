# Slice 4B: Destination zones and price lists - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-19 |
| Built on | Slice 4; not committed here: the dev team owns git |
| Design | [Slice 4B design note](SLICE-4B-ZONES-AND-PRICE-LISTS-DESIGN.md), approved by the owner on 2026-10-19 with Q-Z1 to Q-Z3 as proposed |
| Status | **Built and verified by automated tests; waiting for owner sign-off** |

## 1. What was built

### Destination zones (`/staff/zones`, `ops.charges.configure`, menu "Zones")

- Each zone has a code (`HT-JER`, unique in the company), a name, a country, optional cities or communes, the hub that serves it (a warehouse with the hub role), pickup and/or home delivery, and its pickup branches. In use or not, never deleted.
- With pickup, at least one pickup branch of the zone's country is required; without pickup, none.
- **Q-Z1:** a new destination branch gets its own zone (its code and name, pickup there), covering any address in its country until the company lists cities. Existing destination branches were converted the same way.

### Price lists (`/staff/charges`)

- One price list per service, zone (or all zones) and customer (or the standard prices), by weight or by size.
- Each **version** has its start day and its prices:
  - any number of **bands** (up to 100; "Add a band" adds a row, the form starts with the bands already set plus one empty row), each from its start, charged **flat** for the band, **per lb/kg (or ft³/m³)** on the whole billable amount, or a **base plus per unit above the start**. The first band starts at 0; each start once;
  - **item prices** from the company's item types;
  - minimum charge, service fee, consolidation fee.
- A day starts at midnight in the **company's time zone** (Settings). No day: the version starts now. A day in the past is refused.
- Saving prices on a list adds a version. A scheduled version for the same day is replaced; a version that has started never changes. A scheduled version can be removed (a second click gives the same answer).
- The page shows each list's current, scheduled and past versions (the last five, **Q-Z3**; all are kept).
- **Engine choice:** the customer's list for the zone, the customer's list for all zones, the standard list for the zone, the standard list for all zones. No version on the pricing day: no price.

### Item types, extras and insurance

- **Item types:** name, words found in a declaration, in use; unique name. Price lists price them; quotes choose them on a package.
- **Extras** gain a zone and a delivery filter (for example, a home delivery fee in Jérémie only) and versions of their amount.
- **Insurance** gains versions: new insurer terms can be scheduled.

### Locations and settings

- Warehouse roles: receiving, **storage**, hub (the gateway: "Hub (gateway)" replaces "Dispatch hub"), pickup point.
- A warehouse's **manager** is a staff member (chosen from the active staff), and it has an optional **time zone**.
- The company's **time zone** (Settings), used for price start days.
- The weight and size units are **locked once a price list exists** ("The weight and size units cannot change once prices are set.").

### Quotes and the customer side

- Staff quotes are priced to a zone and a delivery mode; the saved quote keeps its zone and prices (**Q-Z2:** booked shipments and quotes keep what they were given).
- The customer's shipment form asks **"Deliver to"** with the company's zones. The pickup branch must be in the zone, the home address in one of its cities when it lists cities, and the delivery mode offered there. Without a choice, the zone that holds the pickup branch is used.
- The calculator asks the zone, and "Delivered to my address" where a zone offers it (else pickup); the shipment estimate uses the shipment's zone and delivery mode.

### Also fixed

- A select's empty choice ("all zones", "no hub") was refused with 400 by the staff endpoints: such fields are now read as text.
- An unreadable start day now says so, instead of the bands message.
- EF warning on the site services column: a company that chose no services would have got all of them back from the database default. The column now always sends the value (no migration).
- Unused texts and the small-package error removed; the "rate card" wording is now "price list".

## 2. Database

- New tables `ops.Zones` (cities and pickup branches as JSON; unique code per company) and `ops.ItemTypes` (unique name per company).
- `ops.RateCards`: `ZoneId` instead of the country, `Versions` (JSON, with bands and item prices); one list per service, zone and customer (`NULL` counts as a value). `ops.ChargeExtras` and `ops.InsurancePlans`: `Versions` (JSON); extras gain `ZoneId` and `Delivery`.
- `ops.Shipments.DestinationZoneId`, `ops.Quotes.ZoneId`, `ops.Warehouses.TimeZone` and `ManagerUserId`, `ops.TenantSettings.TimeZone`.
- Migration `20261019144549_ZonesAndPriceVersions` converts the Slice 4 data in place: zones from destination branches; shipments get the zone of their branch; item types from the card item names; each card becomes a first version (the small-package fee becomes a first flat band; a country card goes to that country's zones); extras and insurance become first versions.
- In the global script `build/scripts/sql/vixly-v4-database.sql` (19 Platform, 17 Operations, 1 Customer migrations). Tested on an empty server (run twice) and over the Slice 4 script's database with prices, extras, insurance and shipments in it (run twice).

**On the development machine: run the global SQL script once before starting the sites.**

## 3. Automated tests

| Test | What it proves |
|---|---|
| `OperationsRatingEngineTests` (11, Application) | The V3 tiers (0 flat 50, 11 flat 65, 21 flat 75, 31 at 4.75/lb) with rounding up and no gaps; base plus per unit; 40 bands; bad bands refused; minimum, fees, ocean by size, item price from an item type; extras by declaration, percentage, staff-added, government fee kept, zone and delivery filters; insurance by percent and scheduled bands; list choice by zone and customer, a future list never hiding the one in force; versions scheduled, replaced, removed, a started one kept, a day in the past refused; start days at midnight in Port-au-Prince (including the end of summer time); zones and shipment instructions; preferred price |
| `Locations_AreListedUnderTheirOwnCountry...` (Application) | A new destination branch creates its zone once; storage role and time zone saved; a bad time zone and an unknown manager refused |
| `Zones_AndPriceLists_AreSet_AndAQuoteUsesThem_WithAPreferredPrice` (Web) | Zones saved, no delivery and duplicate code refused; a list with six bands of all three kinds saved whole; item type and duplicate; price lists with bands; duplicate, bad bands (also an unreadable base), unknown customer and unknown zone refused; a version scheduled, a past day and an unreadable day refused, removed twice; the quote to Jérémie with the customer's list (41.00), the same customer to Les Cayes with the standard list (47.50), a walk-in (42.50); pickup refused in a home-delivery zone; preferred price, double click, withdraw; units locked, time zone saved; someone without the permission sees no menu entry and changes nothing |
| `Customer_ChoosesTheZone_AndThePickupBranchMustBeInIt` (Api, through Customer.Web) | "Deliver to" lists the zones; a pickup branch outside the zone refused; the chosen zone saved, and the branch's zone when none is chosen |
| `Customer_EstimatesRates...` (Api) | The calculator by zone; another customer's list never used; an unknown zone refused |

All suites green: Operations (Domain 185, Application 87, Infrastructure 62, Web 77, Api 25, Customer.Web 40), Platform, Public and the architecture tests.

## 4. Independent review (2026-10-19)

A separate reviewer read the change without having written it. The band charges, version selection, start days, the choice order, extras and insurance filters, permissions, tenant isolation, form ids, antiforgery and the JSON mappings were found correct. Its findings and what was done:

| # | Finding | Fix |
|---|---|---|
| H1 | The upgrade would stop with a duplicate key when a country card was copied to several zones (the old one-card-per-country index was dropped too late) | The old index is dropped before the conversion; tested with an "all countries" card and two zones in Haiti |
| H2 | A card with a small-package fee lost its bands near the limit: the first band after the fee always took the base price | That band takes the price the old card charged at that weight (its highest band from at or below it, else the base price); tested |
| H3 | Saving a list that had a scheduled version (to take it out of use, say) started the future prices at once, because the form showed the scheduled prices | Forms show the version in force (else the first scheduled one), and saving the prices the form showed adds no version; the same for extras and insurance |
| M4 | A list starting next month hid the list in force today: "not priced" until then | The choice only counts lists with a version on the pricing day (test) |
| M5 | A country card for a country without a destination branch became the price for every zone, or was deleted | Such a country gets a zone not in use (`ZONE-JM`), which keeps its prices until the company sets it up; tested |
| M6 | The customer calculator ignored the delivery mode, so extras for pickup or home delivery were left out; staff could quote a delivery the zone does not offer | The calculator prices pickup where offered, or home delivery when the customer ticks "Delivered to my address" (refused where not offered); staff quotes refuse a delivery the zone does not offer (test) |
| M7 | Home-delivery shipments were given the first zone of their country, even one not in use | Only when the country has exactly one zone in use; otherwise the customer chooses |
| L8 | A typo in a band's base ("1O") was read as 0 | Refused (test) |
| L9 | Smaller points | Flat and base-plus lines store quantity 1 and the charge; item types not in use are refused on a piece; a hub must be in the zone's country; removing a list's only version leaves it unchanged (test); copied versions get their own ids |

Kept as they are: a list keeps up to 500 versions (ended ones included), enough for years of weekly changes; a start day falling on a daylight-saving change exactly at midnight (Havana, Santiago) can start an hour off.
