# Slice 5C: Front desk counter - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-20 |
| Built on | Slice 5B step 1; not committed here: the dev team owns git |
| Design | [Slice 5C design note](SLICE-5C-FRONT-DESK-COUNTER-DESIGN.md), from the owner's answers of 2026-10-20 |
| Status | **Step 1 (parcel count, final shipments, the counter) built, reviewed independently, fixed and verified by automated tests; waiting for owner sign-off.** Step 2 (billing and the cashier) and step 3 (bundles, dispatch) follow |

| Step | Content | Status |
|---|---|---|
| 1 | Parcel count; final ("booked") and locked shipments; the counter: lookup, the customer's shipments, a new shipment, parcel entry, live amount | Built (this record) |
| 2 | Invoice from the final shipment; the cashier; payment terms; correcting a final shipment with a reason | Next |
| 3 | Bundles, then dispatch | Later |

## 1. What was built

### For staff: **Front desk** in the menu (`/staff/counter`)

A new permission, *serve customers at the counter* (`ops.counter.serve`), scoped to warehouses like the other warehouse rights. The Owner and administrators have it; the demo *Front desk* role has it.

- **Find the customer:** one box for a phone number (the last 7 digits or more), an email or a customer code. One match opens the customer. No match says so. Only name, code, phone, email and status are shown.
- **The customer:** contact strip and **all their shipments** (number, route, parcels entered against the count, status, the amount once final). Shipments from other warehouses are listed but only those of the clerk's warehouses can be served.
- **New shipment** (C-6): origin (the clerk's receiving warehouse; picked when they cover several), destination, pickup or home delivery and the address, service (only the ones the company prices for this customer), **number of parcels**, recipient and phone, consolidation, refrigeration, insurance (with the terms accepted when the company sells cover). The customer needs no mailbox address at the counter's warehouse: they bring the parcels in person. The clerk never registers customers. A suspended or closed customer cannot get a new shipment or new parcels.
- **The shipment:** the parcel row (weight, length, width, height in the company's units, contents, declared value; `2.5` or `2,5`), the parcels with their label links, the **amount so far** from the company's prices (the same rating as the calculator and quotes), and a count correction:
  - on a shipment made at the counter, before its first parcel;
  - on an older shipment that has no count yet (before Slice 5C).
- **A parcel at the counter** is received at the shipment's warehouse, measured and declared in one step, with the history *received*, *checked* (source **Counter**). The inspection rules and random share apply as at the station; no photo (C-7, *to confirm*).
- **Exactly the count:** a parcel beyond the count is refused with *More boxes need a new shipment*.

### Final shipments (C-2, C-3)

A shipment becomes **final** (status *Booked*) as soon as all of these hold:

- it has instructions, a service and a count;
- its parcels (not cancelled, returned or disposed of) equal the count;
- every one is measured, described with a value, not waiting for a check or an inspection, and not on hold;
- no package the customer announced in it is still on its way;
- the company has a price for its service on that route.

Then the amount and currency and the time are fixed, and a *booked* event is written on each parcel (milestone *Booked*, shown to the customer as *Shipment final: all parcels in, amount fixed*).

It is tried after everything that can complete a shipment:

- the counter: entering a parcel;
- the dock: the station check, finishing an inspection, releasing a hold;
- the customer: creating a shipment, changing a shipment or its count, placing a package, withdrawing an announced package;
- staff: changing the service.

A final shipment is locked: no change of instructions, service or count, no cancellation, no parcel joining or leaving, no new station check of its parcels.

**More parcels than declared** (C-4): the dock still takes the box. The shipment waits, and the counter shows *More parcels than declared* until the customer raises the count.

### For customers (portal)

- Creating a shipment asks for the **number of parcels** (1 to 99, required by the server too). Changing the shipment can change the count, never below the parcels already in it.
- The shipment page shows *n of m received*, and once final the amount and *All your parcels are in: this shipment is final and its amount is fixed*.

### Shipments desk

The shipment record shows the parcels against the declared count and the final amount.

## 2. Database

Migration `20261020094001_FrontDeskCounter` on `ops.Shipments`: `ParcelCount int null`, `CreatedAtCounter bit not null default 0`, `BookedUtc`, `BookedTotal decimal(12,2)`, `BookedCurrencyCode varchar(3)`. Included in the one global script; the script was run on an empty database (twice) and over the previous script.

## 3. Independent review and fixes

A separate reviewer, who had not seen the work, read the change. No high findings. Fixed:

| Finding | Fix |
|---|---|
| Some paths that complete a shipment never booked it (portal create, place a package, withdraw a pre-alert, service change) | Booking is tried in each |
| A portal shipment could be created without a count and then never become final | The count is required on create; the counter can record a count on a shipment that has none |
| `2,5` typed at the counter gave an empty error page | The counter reads one decimal mark, dot or comma, up to 12 characters; anything else is refused by the rules |
| A very large size could overflow the unit conversion | Same: inputs are limited to 12 characters |
| The portal count was checked only against the parcels chosen, not against received pre-alerts moved in | Checked against the parcels in the shipment after placing them |
| Booking did not need a description and value | It does: no commercial invoice without them (owner) |
| The counter right printed the label of any parcel of the warehouse | Only parcels entered at a counter; other labels need *see parcels* |
| Parcels could be entered for a suspended customer | Refused |
| A double click on the last parcel could say *added* instead of *final* | It reads the stored booked event |

Found by the tests and fixed:

- A shipment declaring 1 box with 2 announced booked at the first arrival. Booking now waits for every announced package.
- A final shipment dropped out of the shipments desk's *To send* list and out of the labels to print, and an insured one could not get its missing certificate. Final shipments are treated as ready to send in all three.

## 4. Automated tests

| Test | Proves |
|---|---|
| `Shipment_CountsItsParcels_AndOnceBookedIsLocked` (domain) | Count 1 to 99, never below the parcels in; booked needs a service and a count; the amount is rounded; a final shipment refuses count, instructions and cancellation |
| `Counter_FindsByPhone_CreatesTheShipment_AndTheLastParcelMakesItFinal` (web) | Lookup by phone and code, unknown email; a new shipment of 2; a parcel without contents refused with the typed values kept; the label link; count locked after the first parcel; the second parcel (`1,5`) makes it final with an amount and 2 booked and 2 counter-received events; a third parcel refused; the customer page shows *Final* |
| `StationCheck_OfTheLastDeclaredParcel_MakesTheShipmentFinal_AndMoreThanDeclaredWaits` (web) | A portal shipment of 1 becomes final at the station check with a booked event (source scan); a shipment with more parcels than declared stays open and shows *More parcels than declared* |
| `Counter_IsOnlyForThoseServingAtTheCounter` (web) | Without the right: no customer data, no menu item |
| `Customer_CreatesShipments_AnnouncesPackages_AndSeesOnlyTheirOwn` (API, extended) | No count refused; count 0 refused; count changed to 3 and shown as *0 of 3 received* |

All suites of the three solutions pass, the architecture rules included (one type per file, BOM and CRLF, text keys and markup budget).

## 5. Styling

The three counter pages are in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*; the portal shipment pages moved from *On hold* to *Needs styling*. The shipments desk stays *On hold* (being redesigned with the owner).

## 6. Also in this delivery: the quotes list

The owner asked on 2026-10-20 for "a list of previous quotes and their status". **All quotes** (`/staff/quotes/list`, linked from the quotes page) lists every saved quote, newest first, 50 a page:

- tabs by status with their counts: all, **open** (still valid), **expired** (open but past its date; not stored, worked out when the list is read), **withdrawn**;
- search by quote number, or by the customer's name, code, email or phone (the customer list's search);
- columns: quote, created, customer, service, destination, amount (with the standard price when a preferred one was given), valid until, status;
- withdraw an open quote from the list.

The quotes page keeps the calculator and the 10 latest quotes. The same permission (*create quotes*) opens both; without it nothing of the list shows. Test: `PricesByModeAndRoute_InsuranceTerms_AndAQuoteOnARoute_WithAPreferredPrice` extended (open count, search by number, the expired tab, withdraw from the list).

A quote *accepted* or *turned into a shipment* is not a status yet: nothing links a shipment to its quote. With billing (step 2) if the owner wants it.

## 7. After the owner's first try (2026-10-20, afternoon)

The owner tried the counter and asked for:

- **A quick way to the next customer**, as in V3: the search bar is now on top of every counter page, with *Clear · next customer* (keyboard: Alt+N) back to an empty counter with the search box focused.
- **The customer must call for attention**, with their standing: a card replaces the one-line strip. It shows the name large, code, phone, email and customer since; the **standing** (in good standing, or not and why: suspended, email not confirmed, closed), announced to screen readers when not; and the figures: open shipments, final shipments to send, parcels on hold, last shipment.
- **No new shipment for a customer not in good standing:** the new-shipment form and parcel entry are hidden, and the server refuses both. Today the account's status decides; unpaid invoices past their date will count once billing exists (step 2).
- The shipment page shows its route, service, recipient, parcels against the count and status as a grid under the customer card.

Also: a known company no longer gets the *please try again* page when Platform is slow or restarting. Operations keeps the last good answer about each company for up to 24 hours and uses it while Platform cannot be reached; an unknown name is asked again after 10 seconds instead of 60, so a company created or seeded a moment ago appears at once. Tests: `OperationsTenantDirectoryTests`; the counter test checks the card, the standing, the *Clear* link and the refusal for a suspended customer.

## 8. Open with the owner

- C-3: who may correct a final shipment, and how (with billing, step 2).
- C-7: no photo at the counter.
- A customer moving a parcel out of a shipment that had more than declared does not book that shipment until it changes again.
- Archiving shipments (the clerk sees all shipments until archived).
