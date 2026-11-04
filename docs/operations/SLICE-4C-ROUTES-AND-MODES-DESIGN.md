# Slice 4C: Routes, transport modes, customs and insurance - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-19 |
| Builds on | [Slice 4B](SLICE-4B-ZONES-AND-PRICE-LISTS-DESIGN.md) (price lists with bands and versions), which it corrects |
| Owner input | 2026-10-19, 13:49 to 14:25: the Prices page reviewed with the owner, answers in section 2 |
| Status | **Approved by the owner on 2026-10-19 (Q-C1 to Q-C4 as proposed); built, see the [verification record](SLICE-4C-VERIFICATION.md)** |

## 1. Outcome

- Prices are set per **route**: from the origin warehouse (where the package is dropped off) to the destination warehouse, for each service. Vixly is a SaaS product, so routes run in any direction: Miami → Les Cayes, Les Cayes → Miami, or anything else a company serves.
- The Prices page has three tabs, **Air**, **Ocean** and **Ground**, and each tab only shows what belongs to that mode.
- Each price list says whether **duties are included** or the price is **transport only**.
- **Insurance** becomes a full model of who covers what, what is excluded, what is paid and how a claim is made, not just a premium.
- The separate **Zones** page from 4B is removed. Destinations are the company's own warehouses, run by the company or by a partner.

## 2. Settled with the owner (2026-10-19)

1. **Destinations are warehouses.** A destination exists because the company has an office there or a partner runs it. Jérémie becomes a destination once the company or a partner operates there.
2. **Tabs per mode.** Air, Ocean and Ground each have their own tab. On Air, nothing about ocean is shown, and on Ocean, nothing about air.
3. **Ground is flexible.** The company prices it by weight, by a flat price, or both.
4. **Insurance follows the industry standard.** Vixly serves many companies, so the model covers the basic needs of any courier or forwarder.
5. **Customs is company policy.** Either the price includes duties and the company clears, or the price is transport only and the recipient clears.
6. **Transport only needs proof.** The package is released only after the recipient shows proof that customs is cleared. The customs receipt is scanned into the shipment's release record.
7. **Duties included is just the price.** The company's bands already include the duties when the list is marked duties included (for example, a flat amount for a weight range). Vixly computes no duties. Extras (a CONATEL fee) can still apply.
8. **No dispatch without payment.** A package cannot be dispatched until its payment is settled: paid, marked **pay at destination**, or billed to a credit customer's account. An unpaid package stops at dispatch only. Assigning to a shipment, inspection, storage and the rest go on.
9. **Claims are flexible.** The company may claim on its own cargo insurance, file a claim for an insured customer, or owe its own stated liability to an uninsured one.
10. **Partners** get limited access to Vixly. This is a feature switched on per company and designed with reception.
11. **Routes, not destinations.** Packages go Miami → Haiti and Haiti → Miami. The origin is always the drop-off, the warehouse that received the package.

## 3. What changes from 4B

| 4B | 4C |
|---|---|
| Zones page, separate from locations | Removed. Destinations are warehouses, run by the company or a partner |
| Price list: service + zone + customer | Price list: service + **origin** (or any) + **destination** (or any) + customer |
| One Prices page with every field | **Air / Ocean / Ground** tabs |
| Item types shown with air | Item types only on the Ocean tab |
| Ground priced like air | Ground: weight bands, flat per package, flat per shipment, or both |
| Insurance: a percentage or value bands | Full insurance terms per mode (section 8) |
| No customs policy | Duties included or transport only, per price list |

What 4B got right and keeps: any number of bands of three kinds, versions with start days (a started version never changes), customer prices, extras with filters, the engine choosing the most specific list.

## 4. Destinations and routes

**A destination warehouse** (the existing warehouse, side destination or both) gains:
- **operated by:** the company, or a partner (name, contact);
- **served through:** its hub, another warehouse (Jérémie through Les Cayes). The route does not change the price;
- **delivery offered:** pickup, home delivery or both;
- **home delivery area:** optional communes it covers. When it lists some, a home address must be in one of them.

**A route** is origin warehouse → destination warehouse.
- The **origin** is never chosen by the customer. It is the warehouse that received the package (the drop-off).
- The **customer chooses the destination** on the shipment. The destination warehouse, then pickup there or home delivery in its area.
- The **calculator** asks "from" (the company's origin warehouses, preselected when there is only one) and "to".

## 5. Price lists per route

A price list is for one **service**, one **origin** (or any), one **destination** (or any), and the standard prices or one **customer**. Versions, bands and start days work as in 4B.

**The engine picks the most specific list** in force on the pricing day:
1. the customer's list for the origin and destination;
2. the customer's list for any origin to the destination;
3. the customer's list for the origin to any destination;
4. the customer's list for any route;
5. then the same four for the standard prices.

No list means the service is not offered on that route.

Each list also says:
- **customs:** duties included (shown to the customer as "Duties included") or transport only ("Customs clearance and duties paid by the recipient");
- **in use** or not.

## 6. The three tabs

| | Air | Ocean | Ground |
|---|---|---|---|
| Charged by | weight (lb or kg, the company's unit) | size (ft³ or m³), and item types at their own price | weight, a flat price per package, a flat price per shipment, or both |
| Bands | any number, flat / per unit / base plus per unit | the same, on size | the same, on weight, plus the flat prices |
| Item types | not shown | barrel, TV, car... with their prices | not shown |
| Fees | minimum, service fee | minimum, service fee, consolidation fee | minimum, service fee |
| Extras and insurance | the air ones | the ocean ones | the ground ones |

Extras get a mode (air, ocean, ground or all) and keep their filters by service and route. A fee that applies everywhere, such as a customs paperwork fee, is set once for all modes.

## 7. Customs and release

- **Duties included:** the company clears customs at destination, and the duties are part of its price. The release needs nothing more from the recipient.
- **Transport only:** the shipment is ready for the recipient once it arrives. **Release needs proof of clearance**: staff scan or upload the customs receipt (PDF, PNG or JPEG) into the shipment's release record. Without it, release is refused.
- The release record (who released, when, to whom, ID checked, the customs proof) is built with destination arrival, after the reception redesign. 4C only stores the policy on the price list and shows it to the customer.

## 8. Insurance model

Set per mode (Air, Ocean, Ground), with versions and start days as for prices.

| Part | What the company sets |
|---|---|
| **Cover** | Own cover (the company pays claims itself) or an insurer: name, policy number, contact |
| **Covered risks** | Loss, damage, theft (each on or off) |
| **Exclusions** | An editable list. Default: cash and negotiable documents, jewelry and precious metals, perishables, live animals, hazardous goods, fragile items not packed by the company, used electronics without proof of condition, and losses from improper packing or customs seizure |
| **Liability without insurance** | What the company pays when the customer did not insure, as forwarders and airlines do: an amount per lb/kg and/or per shipment, or none |
| **Premium** | A percentage of the declared value with a minimum, or value bands (as in 4B) |
| **Payout** | Based on the declared value or the invoice value, whichever is lower; a deductible (amount or percent); the most paid per item and per shipment |
| **Claims** | The deadline after delivery (for example 7 days, or 21 for loss); the proof required (invoice, photos, damage noted at release); who files: the company for the customer, or the customer |
| **Terms** | Text shown to the customer, per language, accepted when they insure. The acceptance is recorded with the shipment |

The claims workflow itself (cases, evidence, the insurer's answer, the payout) is a later slice. This model records what it needs.

## 9. No dispatch without payment (recorded now, enforced with dispatch)

- **The rule:** a package can be dispatched only when its payment is settled.
- **Settled means:** paid; marked **pay at destination** by the company; or, for a credit customer, billed to the account (within the credit limit).
- **Unpaid stops at dispatch only.** The dispatch screen refuses an unpaid package and says why. Everything else (shipment, inspection, storage, holds) goes on.
- **At destination, release needs both:** the package is paid (a pay-at-destination package is paid there first), and, on a transport-only list, the recipient's customs clearance is proven (section 7). Until then the recipient cannot pick it up or receive it.

The rule is written into the design now and enforced when dispatch is built (Slice 5).

## 10. Data and conversion

| Class | Change |
|---|---|
| `OperationsWarehouse` | Gains: operated by (company or partner name and contact), served through (hub warehouse), delivery offered, home delivery area (communes, JSON) |
| `OperationsRateCard` | `ZoneId` becomes `OriginWarehouseId` (null: any) and `DestinationWarehouseId` (null: any); gains `Customs` (duties included or transport only); `Mode` comes from the service |
| Ground versions | Gain a flat price per package and per shipment |
| `OperationsChargeExtra` | Gains `Mode` (or all); zone filter becomes route filter |
| `OperationsInsurancePlan` | One per mode; the terms of section 8 in each version |
| `OperationsShipment`, `OperationsQuote` | `DestinationZoneId` becomes `DestinationWarehouseId`; origin from the receiving warehouse |
| `OperationsZone` | Removed after conversion |

**Conversion:**
- Each 4B zone maps to the active destination warehouse of its branch (the hub when there are several). A zone with no warehouse becomes a destination warehouse marked "partner", not in use, for the company to complete.
- Price lists, extras, shipments and quotes move to that warehouse.
- Existing insurance becomes the Air, Ocean and Ground plans with the same premium and the default exclusions.

## 11. Questions for the owner

1. **Q-C1 Shipment origin:** when a customer creates a shipment before any package arrives, the origin is not known yet. Proposed: the customer's home origin warehouse (their mailbox address) until a package arrives, and the warehouse that received it after that.
2. **Q-C2 Packages at two origins:** can one shipment hold packages received at two different origin warehouses? Proposed: no. A package received at another warehouse goes into a shipment from that warehouse.
3. **Q-C3 Insurance per mode or per route:** proposed per mode. A company that needs different terms per route can add that later.
4. **Q-C4 Customs proof override:** may an Owner or administrator release a transport-only package without the receipt, with a written reason? Proposed: yes, recorded in the audit.

## 12. Done when

- A company sets Air Miami → Les Cayes, Air Les Cayes → Miami and Ocean Miami → any destination. Each list has its own bands and customs policy, and the engine picks the right one per route and customer.
- The Air tab shows nothing about ocean, the Ocean tab nothing about air, and Ground offers weight and flat prices.
- A shipment's origin is its drop-off warehouse. The customer chooses only the destination.
- Insurance terms are set per mode and shown to the customer, who accepts them when insuring.
- Every new text is in en, fr and es, with no "Vixly" on any tenant page.
