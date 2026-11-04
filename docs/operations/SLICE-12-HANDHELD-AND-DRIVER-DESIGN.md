# Slice 12: handheld and driver apps - design

| | |
|---|---|
| Date | 2026-10-23 |
| Owner answers | [Slice 12 owner answers](SLICE-12-OWNER-ANSWERS.md) (H-1 to H-11) |
| Builds on | `/staff/scan` (3B, offline queue, camera), the driver page and stops (Slice 7 step 3), partner agents (7 step 4), print program and printers (3B, 4D), device licensing (Platform Slice 7, R-6 blocks) |

## 0. What exists and what is new

| Need | Exists | New in Slice 12 |
|---|---|---|
| Scan at the dock, offline | `/staff/scan` with queue and camera | Badge sign-in, camera on the other stations |
| Driver: route, proof, cash, hand-in | `/staff/drive`, route page, stop forms (online only) | The whole route offline, live location |
| Partner agents drive | `DeliveryDrive` in `PartnerAgent` rights | Nothing: the same app, limited to their warehouse's routes |
| Printing | Windows print program, network and installed printers | **Vixly Print** on phones: Bluetooth printers |
| Stock | On hand and move station (beta item 2) | Basic shelf count |

Five sub-slices, each delivered and verified alone: **12A** badge and PIN, **12B** the driver offline, **12C** live location, **12D** handheld stations and the shelf count, **12E** Vixly Print.

## 12A. Badge or PIN sign-in on a licensed device (H-8, H-9)

- **Where:** only on a licensed device of the company (the device cookie and the R-6 device gate). Elsewhere the badge and PIN do nothing. Every handheld and phone takes a licence (H-9).
- **The badge:** a card (CR80) printed from the person's staff page: company name (never "Vixly"), the person's name and a Code 128 of a random badge code (stored hashed). Printing a new badge revokes the old one; *Revoke badge* alone is also there.
- **The PIN:** 4 digits, set by the person in *My account* with the password. The device's sign-in screen lists the people who signed in on it in the last 30 days. Five wrong PINs lock the PIN until the next full sign-in.
- **Authenticator once a day:** the first badge or PIN sign-in of the day (company time zone) asks the authenticator code, or the password when the person has no authenticator.
- **Who can't:** a person blocked, suspended or without access; a blocked device (R-6). Step-up actions still ask the code.

## 12B. The driver's route, offline (H-3, H-4, H-10)

- The page saves the route and each stop in IndexedDB; a service worker keeps the pages, so they open without a signal.
- Each stop waits on the phone, photos and signature included, with a key so a resend counts once, and is sent in order when there is a signal. The time recorded is the time of the stop (ADR-0021).
- The phone never holds the delivery codes. A wrong code sent later turns the stop into *delivered, code not confirmed* for the desk.
- Partner agents use the same pages, for their warehouse's routes.

## 12C. Live location (H-5)

- While the route runs and the page is open, the phone sends its position every 30 seconds.
- Staff: a map on the route page. Customer: "N stops away" in the portal, the map once theirs is next; the "out for delivery" notice carries a private link for that day only.
- Positions deleted after 7 days. Map: Leaflet, tiles through the site (`OperationsMaps`). **Production needs a tile provider account.**
- Known limit: a browser sends the position only while the page is on screen.

## 12D. Handheld stations and the shelf count (H-7)

- Stations: camera button beside the scan fields; rugged handhelds type the scan like a keyboard.
- Shelf count: scan the shelf, scan its parcels, finish: **found**, **missing**, **elsewhere** (*Move here*) and **unknown**. Each count is kept. No exceptions queue (add-on).

## 12E. Vixly Print, the print helper (H-6)

- A small .NET MAUI app (Android and iOS) that only prints: it pairs as a print program, takes its printers' label jobs and sends the ZPL over Bluetooth Low Energy.
- Pairing: *Add a phone* on the Printers page shows a QR code (one-time code, 10 minutes).
- A printer kind **Bluetooth**, bound to that phone.
- Name "Vixly Print"; a white-label copy is built with its own name and app id.
- The logic lives in a plain .NET library with tests; the app itself needs the Android and iOS workloads and the store accounts.

## Built differently (2026-10-23)

| Design said | Built | Why |
|---|---|---|
| Shift session ends after 30 minutes idle | The usual 15 minutes idle and 12 hours | One session rule for every sign-in; a quick sign-in is a badge scan away |
| Sync by route: `/staff/delivery/routes/{id}/sync` | Sync by stop: `/staff/delivery/routes/{id}/stops/{stop}/sync` | Each stop is confirmed alone, so a weak signal never resends the whole route |
| Shelf count at `/staff/parcels/count` | `/staff/parcels/shelf-counts` | Counts are kept as a list with their history |
| A missing parcel gets a tracking event `count_missing` | No tracking event; the count keeps its four lists and the history records it | The customer never sees an internal count; nothing to clear when the parcel turns up |
| Map on the delivery desk too | Map on the route page | One map per route keeps the desk light |

## Done when

- A badge or PIN works only on a licensed device, asks the authenticator once a day, and a revoked badge opens nothing.
- A route recorded offline arrives complete, in order, once; a resend changes nothing.
- A customer sees only their own driver, only that day, only while the route runs.
- A shelf count's lists match the parcels' records.
- en, fr and es complete; no "Vixly" on any company or customer page (the store name of Vixly Print is the one exception).

Verification: [Slice 12 verification](SLICE-12-VERIFICATION.md).
