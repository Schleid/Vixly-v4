# Slice 3A: Tracking core - verification record

| | |
|---|---|
| Plane | Operations (Stage 3) |
| Date | 2026-10-18 |
| Built on | Slice 2 with the owner's review (D-C6, menu); not committed here: the dev team owns git |
| Design | [Slice 3 design note](SLICE-3-RECEIVING-DESIGN.md) section 2 (3A); ADR-0018, ADR-0021 |
| Status | **Built and verified by automated tests; waiting for owner sign-off.** No screen yet: the first screens that write events arrive with 3B (receiving) |

## 1. What was built

### One history (ADR-0021)

- **`OperationsTrackingEvent`** (table `TrackingEvents`) records each step of a parcel's life:
  - **what:** the subject, a parcel for now;
  - **when:** the time it happened, with its time zone, and the time it was recorded;
  - **where:** a location code;
  - **who:** staff (user and device), system, carrier, partner or customer;
  - **how:** scan, manual, carrier feed, system or customer;
  - **why:** a code from the catalogue, with its arguments; staff may add a note.
- **Append-only:**
  - nothing in the code updates or deletes an event;
  - the global SQL script denies UPDATE and DELETE on the table to the application login, as for the audit log;
  - a wrong event is corrected by a new **void** event that names it and its reason. Staff see both; customers see only the corrected history.
- **Repeats never double:** every event carries an idempotency key (scan id or command id), unique per tenant.
  - The same key again returns the first event and adds nothing.
  - The same key for another parcel or another step is refused.

### The catalogue (ADR-0021 decision 7)

- 15 codes for Slice 3:
  - pre-alerted and received at origin;
  - customer assigned, condition recorded, photo added, put away;
  - inspection required, started and completed;
  - hold opened and released;
  - returned to sender, disposed of, cancelled;
  - event voided.
- Each code declares:
  - who may see it (staff only, or the customer too);
  - the milestone it reaches;
  - the flags it sets or clears;
  - its required arguments.
- A hold names exactly one reason from the ADR-0018 list.
- Staff wording for every code, hold reason and milestone in en, fr and es; customer wording (in the customer portal's texts) for every customer-visible code, hold reason and milestone.
- Arguments a customer may see take a value from a fixed list (inspection result, hold reason, condition, photo kind, void reason), never free text.

### Parcels carry their state (ADR-0018 decision 4)

- **`OperationsParcel`** (table `Parcels`) keeps the milestone, the flags and the open hold reasons as a projection of its events.
  - The projection is written in the same unit of work as the event (D-R6), so they never disagree.
  - Several events of one command (for example received, photographed, held) project together before the save.
- **Order rules:**
  - a parcel only moves forward;
  - a terminal milestone (cancelled, returned to sender, disposed of) is final.
- **Holds:** several reasons may be open at once. The parcel is *on hold* while any reason is open, and a hold or an unfinished inspection **blocks** it (ADR-0022).
- **Late scans:** events are replayed in the order they happened, so a scan from an offline device that arrives late lands in its right place.
- A parcel belongs to a shipment (owner, 2026-10-18). The shipment link arrives with 3B.

### Who sees what (ADR-0021 decision 5)

- **Staff:** every event, voided ones marked, with note, actor and device.
- **Customer:** customer events about their own parcel, corrected (voided events and void entries removed), without note, actor or device.
- **Public tracking:** milestones and their times only.

## 2. Automated checks

Release builds: no errors, no warnings.

| Test project | Slice 2 (after the owner review) | Slice 3A |
|---|---|---|
| Operations.Domain.Tests | 89 | 104 |
| Operations.Application.Tests | 65 | 71 |
| Operations.Infrastructure.Tests (SQL Server) | 51 | 55 |
| Operations.Api.Tests | 19 | 19 |
| Operations.Web.Tests (SQL Server) | 49 | 55 |
| Customer tests | 40 | 40 |
| Architecture.Tests | 8,857 | 8,944 |

All pass. The global SQL script (30 migrations) runs twice on an empty server, and twice over a database built with the script of the previous delivery, without errors. With the application login created by the script:
- an UPDATE or DELETE on `ops.TrackingEvents` is refused ("permission was denied");
- updating `ops.Parcels` works.

What the new tests cover:

- **Domain:**
  - the catalogue is well formed;
  - unknown codes, missing or extra arguments, bad hold reasons, bad keys and times in the future are refused (a device clock up to 5 minutes fast is accepted);
  - forward-only and terminal milestones;
  - several holds at once;
  - inspection flags;
  - a voided hold no longer counts;
  - **50 random arrival orders give the same state**.
- **Service:**
  - a repeat returns the first event and adds nothing;
  - a reused key for another step is refused;
  - the parcel follows its events, and a void;
  - a void cannot be voided or repeated;
  - an unknown parcel is refused;
  - staff, customer and public views each show only what they may.
- **Database:**
  - events of one command project together;
  - the stored state equals a replay of the stored events;
  - another tenant sees nothing;
  - one key per tenant, the same key in another tenant;
  - the script keeps the table append-only.
- **Texts:** every code, hold reason and milestone has staff wording, and every customer-visible one customer wording, in en, fr and es.

## 3. Independent review

A separate reviewer read the 3A code without having written it. Its findings were fixed, each with a test:

- **Two writers on one parcel** (a desk opening a hold while a handheld puts the parcel away) could overwrite each other's projection, leaving a held parcel unblocked. Parcels now carry a row version: the second writer's save is refused and retried, and the stored state always equals a replay. Test: two contexts race; the hold survives.
- **Two writers with the same scan reference** got a generic error instead of the first event. `RecoverRepeatAsync` now returns the stored event as a repeat after such a race (ADR-0021 decision 4). Test included.
- **Customer lines could show staff details:** a free-text inspection result, and the shelf location. Customer lines now carry only the arguments their code marks customer-safe, and no location; those arguments take values from fixed lists.
- **A parcel could point at another tenant's customer:** the link now includes the tenant (composite key).
- **A void must name its reason** (fixed list: entered in error, wrong parcel, wrong step, duplicate, other), as ADR-0021 says.
- **Public tracking** shows only the milestones the parcel really reached, so it never contradicts the current one.
- **Stricter input:** unknown actor or source kinds and a staff event without the staff member are refused; time zones and location codes are checked, not cut; times are stored in UTC.

**Noted:** repeating a command whose event was later voided returns the voided event (Created false); the caller sees the history. Each write replays the parcel's history, which is small.

## 4. Deployment notes

- **Database:** run the newest global SQL script (migration `TrackingCore`; the append-only grant is in section 5b).
- No new settings.
