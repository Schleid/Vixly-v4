# ADR-0018: One shipment milestone list for every plane

- Status: **Accepted** (owner answers on delivery, receiving, payment gate and currency 2026-10-16; list approved as written 2026-10-17)
- Date: 2026-10-16
- Deciders: Jude (owner)
- Affects: Operations, Customer and Edge planes; `Contracts.Operations`; the V3 migration tool
- Amended 2026-10-17 by ADR-0021 (milestones from events) and ADR-0022 (Disposed, hold reasons, holds block)

## Context

In V3 a shipment is created as `Created` and only ever moves to `Cancelled`; `ReadyForDispatch`, `InTransit` and `Delivered` are never set, so customers see "Created" until the end ([V3 review](../operations/V3-OPERATIONS-REVIEW.md), sections 2.3 and 2.7). Real progress lives only in tracking events, and bundles, dispatches, legs, pre-alerts, receptions and invoices each keep their own status. Staff screens, customer tracking, carrier data, notifications and billing need one shared answer to "where is this package?".

Owner answers (2026-10-16):

- **Delivery:** both pickup and home delivery, chosen per tenant (and per shipment where the tenant offers both).
- **Receiving:** in casillero, scanning a package in matches the customer's pre-alert by the carrier tracking number and starts or adds to the customer's shipment.
- **Payment gate:** a tenant setting. Paid first by default; a tenant may allow Collect (pay on pickup or delivery) or customer credit.
- **Currency:** each tenant chooses its billing currency; one currency per tenant, no per-customer currency (confirmed 2026-10-17).

## Decision

1. **One ordered milestone list**, used on parcels and shipments. A shipment's milestone is the lowest among its active parcels; screens show "partial" when parcels differ.

| # | Milestone | Reached when |
|---|---|---|
| 0 | PreAlerted | The customer declares an incoming package |
| 1 | ReceivedAtOrigin | The package is scanned in at the origin warehouse (matched to a pre-alert, or unidentified) |
| 2 | Booked | The shipment and parcels are captured and charges calculated |
| 3 | AwaitingPayment | The invoice is issued and the tenant's payment gate requires payment |
| 4 | Released | Cleared to ship: paid, or allowed on Collect or credit; tracking and parcel codes assigned |
| 5 | Consolidated | Scanned into a bundle |
| 6 | Manifested | The bundle is closed and attached to a dispatch |
| 7 | DepartedOrigin | The dispatch leaves |
| 8 | InTransit | A leg has departed |
| 9 | ArrivedDestinationCountry | The last international leg arrives |
| 10 | InCustoms | Entered customs |
| 11 | CustomsCleared | Released by customs |
| 12 | ArrivedDestinationWarehouse | Scanned in at the destination warehouse |
| 13 | ReadyForPickup | Pickup shipments only |
| 14 | OutForDelivery | Home-delivery shipments only |
| 15 | Delivered | Handed over, with proof of delivery (signature or photo, name, time) |
| T | Cancelled | Terminal |
| T | ReturnedToSender | Terminal |
| T | Disposed | Terminal: destroyed or surrendered after an inspection refusal or the unclaimed period (added 2026-10-17, ADR-0022) |

2. **Order rules.** Milestones 1 and 2 may happen in either order (casillero receives first; a counter drop-off books first). From 3 on, a parcel moves forward only. A tenant whose payment gate allows Collect or credit passes from Booked straight to Released. A shipment is either pickup (13) or delivery (14), never both.
3. **Flags, not milestones:** OnHold (with a reason from a controlled list: unidentified, awaiting customer information, inspection hold for documents, permit, value or a fee, damaged, refused, other), Delayed, Exception (with a reason, including "unidentified" for an orphan receipt and "damaged"), InspectionPending, InspectionInProgress. In V4 a hold or a pending inspection blocks bundling, dispatch and handover until released (ADR-0022).
4. **Milestones come from tracking events** (ADR-0021): every event code declares the milestone or flag it sets, and the parcel's milestone is a projection written in the same transaction (D-2). The event stream is the history; the milestone is the current state.
5. **Money carries its currency.** Every amount is stored with the tenant's billing currency (ISO 4217 code); a tenant changes currency only while it has no invoice at all.
6. **Migration.** V3 shipments get their milestone from the mapping in the V3 review (section 6), computed from invoices, bundles, dispatches and tracking events, not from V3's shipment status.

## Consequences

- `Contracts.Operations` publishes the milestone names; Customer tracking and Edge show the same list with their own wording in en, fr and es.
- Receiving needs carrier-tracking matching and an "unidentified package" queue (Operations slice 3).
- Delivery and proof (Operations slice 8) needs delivery addresses, routes and proof capture for tenants that deliver.
- Charges and finance need a per-tenant currency setting from slice 1 (tenant setup).
- The status colours in ADR-0016 (received blue, staged amber, transit purple, completed green, exceptions rose) group these milestones: 0–2 received, 3–6 staged, 7–14 transit, 15 completed, flags and terminal exceptions rose. The milestone name is always shown as text too.
