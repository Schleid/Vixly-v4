# Slice 6C: Operational events - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-21 |
| Builds on | ADR-0021 (tracking events), Slice 3C (holds), Slice 6A (bundles), Slice 6B (dispatch) |
| Source | V3's Operational Events (OperationPlane: `OperationalEventsWorkspace`, `OperationalEventEntryForm`, `CreateOperationalEventUseCase`, `GetOperationalEventTimelineUseCase`, `OperationalEventSubjectResolver`, `TrackingEventBlockerEvaluator`), read on 2026-10-21 |
| Status | **Agreed with the owner on 2026-10-21** (section 3). **Built; waiting for owner review** ([verification record](SLICE-6C-VERIFICATION.md)) |

## 1. What V3 does

- **A workspace** (`/operation-control/operational-events`): staff pick a subject type (shipment, parcel, bundle, dispatch, dispatch leg), type its reference code and see its history. Read-only; staff of a warehouse see subjects leaving from or arriving at it, owners and a role named "Regional Manager" see all.
- **Manual entry**: subject and reference code, an event code (operational note, delayed, exception, hold placed or released, inspection started, passed or failed, ready for dispatch, in transit, delivered), a title, a customer description, internal notes, and a "visible to the customer" box.
- **Blockers**: holds placed and released by manual events are counted per title; a net positive count blocks.
- Only shipments and parcels resolve in V3; bundle, dispatch and leg lookups throw "not implemented". Free text reaches the customer as typed. Events are never cascaded to the parcels of a bundle or dispatch.

## 2. What V4 already has

- Every workflow writes its own tracking events (ADR-0021) with a code, a visibility, the actor and source, voids with a reason, and the projection replay.
- Holds are a real domain (Slice 3C), not counted from event titles.
- The parcel page shows its staff history; the customer sees the customer-visible part.
- No central workspace, no manual entry, nothing on shipments, bundles or dispatches as one timeline.

## 3. Decisions (owner, 2026-10-21)

| # | Decision |
|---|---|
| E-1 | **Manual kinds: notes, delays and exceptions only.** Milestones (in transit, delivered...) come only from the real workflows (scan, dispatch, delivery), so a timeline cannot be faked or contradict itself. Holds stay in the holds workflow. |
| E-2 | **Customer text: a preset reason plus an optional short line.** The customer sees the reason translated (EN, FR, ES) and the optional line. Internal notes are never shown to the customer. |
| E-3 | **Reasons: a standard list plus the company's own.** Every company has the translated standard list (weather, customs inspection, flight rebooked, missed connection, carrier delay, damaged, address problem...), can switch standard ones off and add its own with EN, FR and ES wording. |
| E-4 | **Cascade:** an event on a dispatch, leg, bundle or shipment reaches every parcel inside it, on the parcel's timeline and the customer's tracking, linked back to the one source event. |
| E-5 | **Notify:** customer-visible delays and exceptions email the customer; a company setting, on by default; one email per shipment per event, in the customer's language. |
| E-6 | **Access:** new permissions `ops.events.view` and `ops.events.add`, given per warehouse; staff see subjects leaving from or arriving at their warehouses; owners and administrators see all. |
| E-7 | **Corrections:** never edited or deleted; voided with a reason (ADR-0021). A void on the source event voids its cascaded copies, and the customer stops seeing them. |

## 4. Settled while building (Claude, industry practice; can be changed after review)

| # | Point |
|---|---|
| S-1 | **Subjects**: a parcel (by its number or its carrier's tracking number), a shipment, a bundle or a dispatch; a dispatch leg is named in the event's words, not a subject. Staff of the warehouse a subject leaves from or goes to find it; a number reused at another warehouse never hides theirs. |
| S-2 | **A delay or an exception is always shown to the customer** (cause in their language and the optional line, at most 140 characters); anything for staff only is a note. A note stays on its subject and does not reach the parcels. |
| S-3 | **Standard causes**: delays (bad weather, flight rebooked, missed connection, carrier delay, no space on the planned departure, customs inspection, waiting for documents, public holiday); exceptions (damaged, address problem, recipient unreachable, held by customs, documents missing, refused, security check). A company's own causes (up to 40) need English; French and Spanish fall back to it. An event keeps the words it was written with. |
| S-4 | **Reaching the parcels**: every parcel inside not yet delivered, cancelled, returned or disposed of; each copy names its source (the shipment's, bundle's or dispatch's number) for staff. |
| S-5 | **Emails**: one per shipment reached, or per package not yet in a shipment; none for notes. |
| S-6 | **Time**: when it happened, where the company works; never in the future, at most 30 days back. A note, delay or exception does not change a parcel's place in the holds queue. |
| S-7 | **The workspace**: the subject, its parcels, the record form, its history newest first (all, what the customer sees, recorded by hand, corrections), 50 per page. |
