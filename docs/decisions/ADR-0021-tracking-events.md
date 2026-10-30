# ADR-0021: Tracking events, one history for staff and customers

- Status: **Accepted** (owner, 2026-10-17)
- Date: 2026-10-17
- Deciders: Jude (owner)
- Affects: Operations, Customer, Edge (public tracking), the handheld and driver apps, carrier and partner integrations; ADR-0018

## Context

The owner called tracking V3's biggest mistake. In V3 the shipment status never moves (it stays `Created`); progress lives only in `TrackingEvents`, which mixes system and staff entries, stores hold reasons as free text in the title, can point at shipments that do not exist (orphan receipts), and lets a public tracking page show payment amounts (SEC-03) ([V3 review](../operations/V3-OPERATIONS-REVIEW.md), sections 2.7 and 3). V4 needs one trustworthy history that staff, customers, notifications, carriers, partner agents and the scan apps all write to and read from.

Industry practice: GS1 EPCIS records each supply-chain event by **what** (the items), **when**, **where** (the site) and **why** (the business step); air cargo reports fixed status codes per shipment (for example received from shipper, departed, arrived, received from flight, notified for pickup, delivered).

## Decision

1. **One append-only event store.** Every scan, status change, inspection result, hold, carrier update and delivery is an `OperationsTrackingEvent`. Each event records:
   - **what:** the subject (parcel, shipment, pre-alert, receipt, bundle or dispatch) and, for a bundle or dispatch, the parcels it carries;
   - **when:** the time it happened, with its time zone, and the time it was recorded (scans from an offline handheld arrive late and are kept in their real order);
   - **where:** a location code (warehouse, branch, airport IATA code, port UN/LOCODE, or an address for delivery);
   - **who:** the actor (staff user, system, carrier, partner agent, customer) and, for staff, the device;
   - **how:** the source (scan, manual, carrier feed, system, customer);
   - **why:** a fixed event code from a catalogue in code, with typed arguments (D-4), never free text; staff may add a note;
   - **visibility:** internal, customer or public;
   - **evidence:** photos and documents attached by reference.
2. **Never edited.** A wrong event is corrected by a new event that voids it, naming the reason. Staff see both; customers see only the corrected history.
3. **Milestones come from events.** Each event code declares which milestone (ADR-0018) it reaches or which flag it sets or clears. The parcel's milestone and flags are a projection updated in the same commit as the event (D-2), so they can never disagree. Shipments take the lowest milestone of their active parcels.
4. **Repeats never double.** Every event carries an idempotency key (scan id, carrier event id, or command id). A double scan, a resent carrier update or an offline app replaying its queue never creates a second event.
5. **Who sees what** (owner, 2026-10-17):
   - **Staff:** everything, filtered by their branch and warehouse scope.
   - **Customers:** customer events about their own parcels (received, inspected, held with what to do, consolidated, departed, arrived, customs, ready for pickup, out for delivery, delivered) with photos of their own package, in their language; ownership checked on every read (S-3).
   - **Public tracking** (tracking number only): milestones and dates, never amounts, names, photos, notes or internal events.
6. **One feed out.** Notifications (email now; SMS and WhatsApp in Slice 10), customer pages, Edge public tracking and the tenant API read the same events through the outbox. Carrier feeds and partner-agent scans write through the same command, with the same rules.
7. **Codes are a catalogue.** Each code has a stable name (`ops.tracking.received_at_origin`), a default visibility, the milestone or flag it sets, required arguments and localized texts (staff and customer wording) in en, fr and es. Carrier status codes map onto it in one table per carrier.

## Consequences

- `OperationsTrackingEvent` replaces V3's `TrackingEvents`; `OperationsParcel` keeps the milestone and flags as a projection only.
- The tracking core is built in Operations Slice 3 with receiving and inspection, the first events; every later slice adds its codes to the catalogue.
- The migration tool turns V3 tracking rows, receptions, bundles and dispatches into V4 events with their real times, marking them "migrated".
- Tests: append-only (the app login has no UPDATE or DELETE on the table); idempotent replay; customer and public visibility; out-of-order offline scans; milestone projection equals a full replay.
