# Slice 6C: Operational events - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-21 |
| Built on | Slice 6B, dispatch (package 83); not committed here: the dev team owns git |
| Design | [Slice 6C design note](SLICE-6C-OPERATIONAL-EVENTS-DESIGN.md), sections 3 (E-1 to E-7, the owner's answers) and 4 (settled while building) |
| Status | **Built, reviewed independently, fixed and verified by automated tests.** Slice 8 (delivery and proof) next, from the plan |

## 1. What was built

- **The events workspace** (`/staff/events`, E-6): type or scan a number; a parcel (or its carrier's tracking number), a shipment, a bundle or a dispatch of the person's warehouses opens with its facts, its parcels and its history newest first, filtered (all, what the customer sees, recorded by hand, corrections), 50 per page.
- **Recording by hand** (E-1, E-2): a **note** (staff only), a **delay** or an **exception** with a cause and an optional line for the customer, at the time it happened; one form posted twice records one.
- **Reaching the parcels** (E-4): a delay or an exception on a shipment, bundle or dispatch is copied on each parcel inside not yet finished, linked to the source; the customer's package history shows *Delayed: Bad weather — Storm in Miami* in their language.
- **Settings → Events** (E-3, E-5): emails to customers on or off (on by default); the standard causes offered; the company's own causes in English, French and Spanish, switched off when no longer used.
- **Emails** (E-5): one per shipment reached (or per package not yet in a shipment), in the customer's language, linking to their account.
- **Corrections** (E-7): an event recorded by hand is voided with a reason; its copies on the parcels go with it and the customer stops seeing them. Copies are never voided alone.
- **Rights** (E-6): *see events* and *record events*, per warehouse.

## 2. Database

The global script adds **EventReasons** (the company's own causes; code unique per company), on **TrackingEvents** `CausedByEventId` (indexed), and on **TenantSettings** `EventEmails` (on) and `DisabledEventCauses`.

Checked: the script runs on an empty database twice, and twice on a database upgraded through every earlier package to package 83 with demo data.

## 3. Independent review and fixes

| # | Finding | Fix |
|---|---|---|
| 1 | The search took the first match, so a carrier tracking number reused at another warehouse (or a parcel's tracking number equal to a shipment's number) hid the person's own | Every match is listed, exact numbers first, then tracking numbers newest first; the first one the person's warehouses cover opens |
| 2 | A shipment without a number could not take a delay | The source is left out when there is no number |
| 3 | A package not yet in a shipment got no email | Its customer gets one about the package |
| 4 | Delivered, cancelled or disposed parcels got delays; a shipment's parcels were cut at 1,000 | Finished parcels are left alone; no cut |
| 5 | A note or delay sent parcels to the back of the holds queue | Manual events do not change a parcel's last event time |
| 6 | Two clicks at the same instant showed a conflict although the first worked | The repeat is checked again after a failed save |
| 7 | A typed line starting with `res:` would have been looked up as a text key in the email | Keys to word and typed values are kept apart |
| 8 | Voiding loaded the whole subject's parcels; the history page loaded parcels for update | Only the copies' parcels, in one query; the page reads without tracking, at most 200 |
| 9 | No filter for corrections; the same cause added twice made two | *Corrections* filter; the same cause twice is one |

Noted, not changed: a dispatch leg is not a subject of its own (S-1); notes do not reach parcels (S-2).

## 4. Automated tests

| Suite | Result |
|---|---|
| Operations (domain, application, infrastructure, web, API, customer web, architecture) | All pass: domain 238, application 91, infrastructure 65, web 98, API 30, customer web 40, customer infrastructure 1, architecture 12 850 |

New for this slice:

- Domain (3): a company's own cause (kind, words, English fallback, switched off); the standard causes switched off and the emails; the manual codes (a note internal, a delay or an exception for the customer, its optional words, the link to the source).
- `Events_RecordNotesDelaysAndExceptions_ReachTheParcels_AndAreVoidedWithTheirCopies` (web): a shipment found by its number typed in lower case, an unknown number not found; a note refused without text and kept internal; a delay refused without a cause, recorded once from one form posted twice, shown on the shipment and on its parcel *via* the shipment, with its copy linked and one email queued; a company cause (markup refused) and a standard cause switched off (no longer offered, refused); an exception with the company's cause on the parcel; a copy cannot be voided alone, a void needs a reason, the delay voided with its copy (twice: once); someone without the right sees nothing.

Not covered by an automated test yet: a person whose rights cover one warehouse only (the scope is checked by the same code as dispatches and bundles).

## 5. Styling

New screens are in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*: Events, Event history, Events settings.
