# ADR-0022: Receiving and inspection

- Status: **Accepted** (owner, 2026-10-17; counter procedure corrected by the owner the same day: accept at the counter, inspect afterwards)
- Date: 2026-10-17
- Deciders: Jude (owner)
- Affects: Operations (Slice 3), Customer (pre-alerts, notices, holds), the handheld app (Slice 12); ADR-0018, ADR-0021

## Context

Packages reach a tenant in two ways: shipped by a merchant to the tenant's receiving warehouse under the customer's mailbox (suite) code, and dropped off by the customer at a branch counter. V3 matches receipts only against its own tracking number, ignores pre-alerts, records unknown packages against shipments that do not exist, has no counter intake, and has no inspection at all ([V3 review](../operations/V3-OPERATIONS-REVIEW.md), sections 2.1, 2.2 and 4). The owner asked for both intakes at industry standard and for an inspection feature for legal compliance and defects.

## Decision

### Every intake ends in a warehouse receipt

`OperationsWarehouseReceipt` (number `WR-…` from the tenant's sequence) records who received what, where and when: packages, carrier and tracking number, weight and dimensions (scale and dimensioner ready), photos (label and package), condition, inspection result, storage location, and the customer it belongs to. It is the document the customer and staff see for "your package is here".

### Intake 1: from a merchant (casillero)

1. Scan the carrier tracking number.
2. Identify the customer: first by an open pre-alert with that tracking number, then by the suite or casillero code read from the label (typed, later read automatically), then by name.
3. Weigh, measure and photograph; record condition (below).
4. Matched with a pre-alert: the receipt starts or joins the customer's shipment (ADR-0018). Matched without a pre-alert: the customer is told and asked for the contents and value.
5. Not matched: the package goes to the **unidentified** queue with its photos; staff assign it later.
6. Print the internal label and put the package away at a location.

### Intake 2: customer drop-off at a counter

The counter **accepts** cargo and never inspects it in front of the customer, which would make the counter a bottleneck (owner, 2026-10-17). In outline:

1. Find or create the customer; record the declared contents and value.
2. Weigh and label; give the customer a receipt.
3. The package moves to the inspection area and follows the same condition check and compliance rules as a merchant package (below).

The detailed drop-off procedure (declaration, photos at the counter, when charges and payment happen) is settled with the owner when Slice 3 is designed.

### Inspection: two checks

- **Condition check on every package** at the inspection area: intact, damaged, wet, opened or re-taped, contents missing; a photo is required. A damaged package is **put on hold until the customer is reached** (hold reason "damaged"); the customer gets the photos and the record is kept for claims.
- **Compliance inspection** (box opened and checked), chosen by **rules plus a random share** (owner, 2026-10-17):
  - always when a tenant rule matches: item category, destination country, declared value, or carrier restrictions (for example lithium batteries, aerosols, liquids, medicine, firearms and parts, currency, food, items needing a permit at destination);
  - plus a random percentage of the rest, set by each tenant;
  - plus any package a staff member flags.
- **What is checked:** the contents against the declaration, against the tenant's prohibited and restricted list for the destination and carrier, dangerous goods, and the documents or permits needed. Photos are attached.
- **Where:** inspection happens in the warehouse's inspection area after acceptance, never at the counter; each warehouse sees its inspection queue.
- **Outcomes:** passed; passed with notes; **held** for the customer (documents, permit, value, or a fee) with what to do; repacked; refused and returned to sender; refused and disposed of (with the customer's consent or as the tenant's terms allow). Each outcome is a tracking event (ADR-0021).
- **Blocking:** an open hold or a pending inspection blocks consolidation, dispatch and handover (ADR-0018 holds now block).
- **Rules belong to the tenant** (as charge rules, O-11): each tenant keeps its own restricted-item list and inspection rules; Vixly may ship a starting list that tenants copy and adjust.

### Unidentified and unclaimed packages

Each tenant sets how many days a package waits (default 30). The customer, when known, gets reminders and a final notice; then the package is returned to sender or disposed of as the tenant's terms say, with a tracking event and staff approval (owner, 2026-10-17).

## Consequences

- Operations Slice 3 becomes **Receiving, inspection and tracking**: both intakes, the warehouse receipt, the inspection queue with condition and compliance checks, holds, the unidentified queue, locations and putaway, and the tracking core (ADR-0021). Charges and payment for drop-offs come with Slices 4 and 9; the drop-off procedure is settled in the Slice 3 design.
- ADR-0018 gains the terminal milestone **Disposed** and the hold reasons: unidentified, awaiting customer information, inspection hold (documents, permit, value, fee), damaged, refused.
- New data: `OperationsWarehouseReceipt`, `OperationsInspection`, `OperationsInspectionRule`, `OperationsRestrictedItem`, `OperationsDropOffDeclaration`, `OperationsPackagePhoto`, `OperationsStorageLocation`.
- Photos and signed declarations need file storage outside the database; Slice 3 decides where (host folder or object storage) with the hosting check (B3, O-16).
- Customer side of Slice 3: pre-alerts, "received" notices with photos, holds with what to do (upload a document, confirm the value, pay a fee, decide about a damaged package).
