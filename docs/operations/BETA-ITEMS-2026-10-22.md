# Beta items of 2026-10-22: design and verification

The owner's list for the staging beta (2026-10-22): "get V4 out, then continue on for the extras". Items 2 and 3 are built here; item 1 shipped in package 101 and the sign-up fixes in package 102.

| # | Item | Owner's words | Package |
|---|---|---|---|
| 1 | "All warehouses" crash | "Do what needs to be done" | 101 |
| 2 | Basic parcel inventory | "Fair enough, you can build that" | 103 |
| 3 | R-6: a device used from another country | "You can proceed with that too" | 103 |
| 4 | Staging settings | "Keep the settings of V3. Since they're already there V4 will just reuse them" | none (configuration) |
| - | Smooth sign-up for companies, customers and staff | "The first impression is always the good one" | 102 |

## 1. Empty filter fields (package 101)

- A browser sends an empty choice as `name=`. ASP.NET Core refuses an empty value for a `Guid?`, `int?` or `bool?`, with a 400, or an exception in Development.
- `OperationsBlankFields` (Hosting) now drops empty values from the query string, and from url-encoded forms, of `/staff` requests before binding. Empty means not sent.
- Not applied to:
  - webhooks and the public API, whose requests stay exactly as sent;
  - multipart uploads;
  - forms whose antiforgery token failed (those still get a 400).
- Test: `EmptyFilterFields_MeanAllWarehouses_OnEveryPage` covers 15 pages and one form.

## 2. Basic parcel inventory

The basic part of the owner's parcel inventory answers of 2026-10-22, built for the beta. The full inventory and the warehouse management add-on come after the beta (see section 5).

### On hand: `/staff/parcels/on-hand`

- **Which parcels:** every parcel physically on hand in the viewer's warehouses, meaning at its origin warehouse and not yet departed. Cancelled, returned and disposed parcels are gone. The destination side comes with full inventory.
- **Order:** oldest first.
- **Columns:** parcel, warehouse and shelf ("no shelf" when none), customer code and name, shipment, status (and "on hold"), days on hand, weight.
- **Filters:** warehouse, shelf (when one warehouse is shown), zone, without a shelf, on hold, at least so many days on hand, and a search (parcel, carrier tracking, customer code).
- **Totals:** pieces and weight over all pages, in the company's weight unit.
- **Access:** scoped like the parcels list. A warehouse grant shows that warehouse's parcels only (`ops.parcels.view`).
- **Reads:** paged, 50 a page. Two queries, the page and the totals, plus the summaries' lookups.

### Move station: `/staff/parcels/move`

- **How it works:** scan the package, then the shelf. Each scan ends with Enter and submits its own step, which suits keyboard-wedge scanners without any script.
- **Rules:** the same as the put-away on the parcel page.
  - The person needs `ops.receiving.receive` at the package's warehouse.
  - The shelf must be active and in that warehouse.
  - The package must still be at its origin.
  - Each scan carries a command key, so a scan sent twice moves once.
- **History:** every move is a `put_away` tracking event with the shelf code, which gives each package its location history (ADR-0021).
- **Found and fixed while building:** a lookup by number returned a parcel that was not tracked, so the move would have been confirmed but not saved. The use case now loads the parcel to be changed.
- Links to both pages sit in the parcels list's views.

### Test

`OnHand_ListsWhatIsInTheWarehouse_AndTheMoveStationShelvesItByScan` checks:
- oldest first, the totals, and the empty filter fields;
- step one keeps the package;
- an unknown shelf is refused and keeps the package;
- the shelf and zone filters after a move.

## 3. R-6: a device used from another country

Owner's words (2026-10-21, 18:31): "if the laptop is stolen, the user might have his email open also. I would block sign-in until company system admin unblock them. At sign in I would notify admin that a device is block because of out of country sign in and need his attention."

### Behaviour

- **At sign-in:** a licensed device used from another country than its own is blocked (`ops.DeviceBlocks`). This replaces the earlier rule, where the second step confirmed the other country.
  - The sign-in is refused at once with `ops.auth.device_blocked`.
  - No code, email or second factor lets the person through.
- **During a session:** if the device's cookie appears from another country (a trip, or a session opened without the device), the device gate blocks the device, ends the session and shows the same message (`/staff?blocked=1`).
- **While blocked:** the device signs nobody in, back home included. A session already open on it ends at its next device check.
- **Notice:** the company's administrators are emailed in the same commit as the block (outbox `ops.devices.blocked.v1`). Administrators are the Owner and the holders of the Administrator role. The email, in each administrator's language, gives:
  - the device, the person, both countries and the time;
  - the link to the device's page.
- **Unblocking:**
  - On the device's page (Company › Licences), a company administrator unblocks it with a fresh authenticator code (`ops.devices.approve`, a system-role right).
  - The licences list marks blocked devices.
  - Lifted blocks stay as history.
- **Unchanged:**
  - No country header at all (a LAN or localhost) tells nothing and blocks nothing.
  - A device without a country of its own takes its first one as before.
- **History:** `ops.auth.country_changed`, `ops.auth.device_blocked`, `ops.auth.device_blocked_refused` and `ops.devices.unblocked`.

### Known limit

If the only administrator's only device is blocked, nobody in the company can unblock it. Vixly support then uses the existing owner recovery in Control: it revokes the device and sends the Owner a new first-device link.

### Tests

- **Application:**
  - `Device_InAnotherCountry_IsBlocked_AndNoCodeLetsThePersonThrough`
  - `BlockedDevice_RefusesEveryone_UntilUnblocked`
- **Domain:** `DeviceBlock_IsLiftedOnce_AndKeepsWhereItWasUsed`
- **HTTP:**
  - `Device_UsedFromAnotherCountry_IsBlocked_UntilAnAdministratorUnblocksIt`: refused abroad, then at home; the administrator's email; unblocked from another computer with a code; signs in again.
  - `SignIn_WithoutTheDevice_ThenItsCookieFromAnotherCountry_BlocksTheDevice`

## 4. Staging settings

The owner's decision: staging reuses the settings V3 already has (mail server, provider accounts, hosting). Nothing is built for it.

The sign-up check on 2026-10-22 found two settings staging needs:
- **Control must run.** It sends the confirmation emails and issues the first invoice. When it stops, sign-up stops at "check your email" or at payment.
- **Card or Zelle must be set up in Control's payment settings.** Without either, a paid plan ends at "Online payment is not available for this invoice".

## 5. After the beta

The rest of the owner's warehouse and parcel inventory answers of 2026-10-22, kept in the project document *Warehouse management and parcel inventory: owner answers*. These items are the warehouse management add-on (owner, 2026-10-22: an add-on, like QuickBooks):

- counts of every parcel, and the exceptions queue;
- the full location structure and its capacity;
- put-away rules, pick lists, the task queue and the warehouse dashboard;
- the destination side of inventory.
