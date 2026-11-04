# Slice 7 step 3: Home delivery routes - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-21 |
| Built on | Slice 7 step 2, the pickup counter (package 86); not committed here: the dev team owns git |
| Design | [Slice 7 design note](SLICE-7-DESTINATION-DESIGN.md), sections 2 (R-7, R-9, R-12 to R-15, the owner's answers) and 6 (S-20 to S-28, settled while building) |
| Status | **Built, reviewed independently twice, fixed and verified by automated tests.** Step 4 (partner agents) next |

## 1. What was built

- **Settings → Delivery** (`/staff/settings/delivery`, R-12 to R-15): how drivers record their stops (a phone page, by default, or the printed run sheet entered by staff), cash on delivery (off), the tries (3), what happens after the last (waits for pickup, by default, or goes back to the sender), the proof at the door (the receiver's name always; a signature by default; a photo, the ID's last 4 characters and a delivery code switched on), the out-for-delivery email (on).
- **The delivery desk** (`/staff/delivery`): the shipments waiting for home delivery at the warehouse, with what is owed; destination charges billed before the round (duties, local taxes, local fee); a route planned for a day and a driver (who signs in, or a name for the run sheet); the routes; the parcels going back to the sender, each recorded *returned to sender* as it leaves.
- **A route** (`/staff/delivery/routes/{id}`): stops added, moved or taken off and the route cancelled while planned; the run sheet as a PDF; started (*out for delivery*, the customers emailed, the delivery code made); each stop *delivered* (who received it, the proof asked, exactly the cash owed) or *not delivered* (a reason the customer sees); back once every stop is recorded; settled once the cash is paid in at the cashier, a difference with a note.
- **My routes** (`/staff/drive`): a driver's own routes, used on a phone.
- **A failed try** steps the parcel back to the warehouse; after the last one it waits for pickup at the counter (the customer told once nothing of the shipment still waits for delivery) or is marked going back.
- **The customer** sees *Out for delivery*, *Delivered*, *Delivery attempt failed: reason* and *Delivered on* the date.
- **Rights**: *plan delivery routes* and *drive delivery routes*, per warehouse. A driver never settles.

## 2. Database

The global script adds **DeliveryRoutes** (number RT…, warehouse, day, driver, status, cash collected and handed in, the settlement and cancellation notes, request key) and **DeliveryStops** (route, shipment, order, how it went, who received it, the ID's kind and last 4 characters, the proof files' keys, whether the code matched, whether entered from the sheet, the cash and the invoices it was collected for, the reason); on **Parcels** the route it is on and its tries; on **Shipments** when it was delivered; on **TenantSettings** the delivery settings (phone page, no cash on delivery, 3 tries, wait for pickup, a signature, the email on, for every company).

Checked: the script runs on an empty database twice, and twice on a database upgraded through every earlier package to package 86 with demo data.

## 3. Independent review and fixes

| # | Finding | Fix |
|---|---|---|
| 1 | Destination charges could not be billed for home delivery (the counter bills ready parcels only) | Billed at the delivery desk before the round (S-27) |
| 2 | A parcel held by customs after planning still went out and was delivered | Every parcel is checked again as the route leaves and at the door |
| 3 | Cash on delivery could be asked twice for a shipment split over two rounds | The cash is kept against the invoices it was collected for; those are not asked again until paid in |
| 4 | Lowering the tries left parcels on no list | Still listed; the next failure applies the rule |
| 5 | Entries from the run sheet asked for the delivery code, which the paper cannot carry | Not asked from the sheet (form and server) |
| 6 | A double click showed an error for something done | The second answers done when the first saved the same result |
| 7 | No delivery code when the email could not be sent | The code is made as the route leaves |
| 8 | The last failure of one parcel announced the whole shipment ready | Only once nothing of it still waits for delivery, also when its other parcels are delivered later |
| 9 | The delivered date could be overwritten | The first date is kept |
| 10 | The receiver's name was filled in for the driver | Left empty: the driver types who received it |
| 11 | Second pass: cash already paid in at the cashier could be taken off a later bill | Fixed with 3: only the invoices collected for are set aside, and only until paid |

Noted, not changed (low): the trip back to the origin is arranged by staff (a return dispatch is a later step, S-26); billing destination charges is optional (duties may be nothing) and nothing reminds staff to bill before planning; a code that never reached the customer has no resend action on the route; after a refused entry the driver's drawn signature and photo must be taken again; the driver's page reads *today* in the company's time zone.

## 4. Automated tests

| Suite | Result |
|---|---|
| Operations (domain, application, infrastructure, web, API, customer web, architecture) | All pass: domain 250, application 93, infrastructure 65, web 103, API 31, customer web 40, customer infrastructure 1, architecture 13 349 |

`EveryForm_SentTwiceAtOnce_NeverFails` (an earlier slice's language form) failed once in a full run and passed three times alone; it is timing-sensitive, not related to this step.

New for this step:

- Domain (5): a route planned, started, back and settled (a difference needs a note); a stop delivered with the receiver's name, the ID's last 4 only and the invoices collected for, or failed with a known reason, once; the delivery settings and their defaults; a failed delivery steps the parcel back, a delivered one stays delivered, going back and returned; a parcel on one route at a time, each failure a try.
- `Delivery_PlansARoute_TheDriverRecordsAtTheDoor_TheLastFailureWaitsForPickup` (web): settings checked and saved; a driver who signs in offered and required for the phone page; planned once for a double click; the driver sees the route, someone without the right does not; the run sheet; started by the driver, the customers emailed; the signature and the receiver checked; delivered once for a double click; failed; the driver cannot settle; settled; a second failure is the last: the parcel waits at the pickup counter.
- `Delivery_FromTheRunSheet_CollectsCash_SettlesOncePaidIn_AndReturnsTheLastFailure` (web): duties billed at the desk and shown to collect; a driver named only; staff enter the sheet without a signature; exactly what is owed collected; the last failure goes back and is listed; settlement refused until the cash is paid in, a difference refused without a note; returned to sender.

Not covered by an automated test yet: a customs hold after planning; the delivery code at the door; a photo taken at the door; a second driver at the same warehouse opening another's route.

## 5. Styling

New screens are in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*: Home delivery desk, Delivery route (used on a phone; starter styles in `css/ops-delivery.css`), My routes, Delivery settings; the customer's Shipment row notes *Delivered on*.
