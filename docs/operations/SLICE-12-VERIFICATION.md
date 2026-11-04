# Slice 12: handheld and driver apps - verification

| | |
|---|---|
| Date | 2026-10-23 |
| Owner answers | [Slice 12 owner answers](SLICE-12-OWNER-ANSWERS.md) (H-1 to H-11) |
| Design | [Slice 12 design](SLICE-12-HANDHELD-AND-DRIVER-DESIGN.md) |
| Packages | 111 (12A), 112 (12B, 12C and test fixes), 113 (12D, 12E); applied on the development PC and verified |

## 1. What was built

### 12A. Badge or PIN on a licensed device (H-8, H-9)
- **Sign-in screens:**
  - `/staff/sign-in/badge`: the badge field, ready for a scanner;
  - `/staff/sign-in/pin`: the people who signed in on this device in the last 30 days, then the PIN;
  - `/staff/sign-in/password`: the daily check for a person without an authenticator.
- **Starting screen:**
  - the sign-in page offers *Sign in with a badge or PIN* on a licensed device;
  - a device that used a badge or PIN opens at the badge screen next time (`__Host-vx4-quick`).
- **Once a day:** the first badge or PIN sign-in of the day (company time zone) asks the authenticator code. A person without one is asked the password, when Require MFA is No.
- **A badge or PIN session is a quick session.** It never counts as a fresh code: step-up still asks it. A PIN cannot be set from it.
- **Badges:**
  - a credit-card PDF from the person's staff page: company name, person, Code 128 of a 27-character code; only the code's hash is kept;
  - printing a new badge revokes the old one;
  - *Revoke* also ends that person's badge and PIN sessions.
- **PIN:**
  - set in My account with the current password;
  - four digits, not all the same and not a run;
  - five wrong tries lock it until a full sign-in.
- **Device rules unchanged:** a blocked device or one used from another country (R-6) refuses badges and PINs as it refuses passwords.

### 12B. The driver's route offline (H-3, H-4, H-10)
- **The phone keeps every stop first:** `ops-drive.js` stores it in IndexedDB with its photo and signature.
- **Sending:** in order, to `/staff/delivery/routes/{id}/stops/{stop}/sync` (JSON answer), now or when the signal is back. A resend counts once.
- **The pages work without a signal:** a service worker (`/staff-sw.js`, scope `/staff/`) keeps the driver's routes and route pages, and their files. Signing out clears the pages; stops not yet sent stay on the phone.
- **The stop's time is the phone's,** kept between the route's start and now.
- **A code typed without a signal:** if it is wrong, or the code is locked, when the stop arrives, the stop is kept as *Code not confirmed* for the desk instead of being refused. Sent at once, it is refused as before.
- **Partner agents** use the same pages for their warehouse's routes.

### 12C. Live location (H-5)
- **The driver's phone** sends its position every 30 seconds while the route page is open and the route is out. Only the route's driver is kept; others are told to stop.
- **Staff:** a map on the route page (Leaflet, served by the site) with the way so far and the next stop.
- **Customer:**
  - the portal's shipment page says "N stops away", with the map once theirs is the next stop;
  - the "out for delivery" notice links to `/customer/track/{token}`, a private link for that day only (hash kept on the stop).
- **Map pictures** come through `/maps/tiles/{z}/{x}/{y}.png` (rate-limited, kept a day); the provider's address and key stay on the server. Setting `OperationsMaps`; staging uses OpenStreetMap.
- **Positions are kept 7 days** (hourly housekeeping).
- **Known limit:** a browser sends the position only while the page is on screen.

### 12D. Handheld stations and the shelf count (H-7)
- **Camera button:** beside the scan fields of the station pages (inspection, bundles, arrivals, pickup, move, counts), on phones without a scanner. The camera is allowed on those pages only (Permissions-Policy).
- **Shelf count** (`/staff/parcels/shelf-counts`):
  1. scan the shelf;
  2. scan its parcels;
  3. *Finish*: found, missing, recorded elsewhere (*Move here* puts it away on this shelf) and unknown codes.
- **Rules of a count:** one open count per shelf, 500 scans at most; counts are kept as history.
- **Changed from the design:** a missing parcel gets no tracking event; the count itself is the record.

### 12E. Vixly Print (H-6)
- **Printers page:** *Add a phone* makes a one-time code (10 minutes, once), shown as a QR code.
- **Joining:** the app posts the code to `/agent/v1/pair` and receives its print program key. It saves the printer next to it with `/agent/v1/printers`.
- **Phone printers:**
  - a printer kind *Bluetooth*, bound to its phone;
  - only that phone takes its labels, and a computer's print program never does;
  - the desk sees them read-only and chooses them like any printer.
- **The app** (`src/Operations/Vixly.PrintHelper`, .NET MAUI, Android and iOS):
  - scan or type the code, find the printers nearby, choose one, print every 3 seconds while open;
  - Bluetooth Low Energy with Zebra's Link-OS service.
- **Name:** "Vixly Print" by default. A white-label copy is built with `-p:PrintHelperAppName` and `-p:PrintHelperAppId`. The Printers page takes the name from `OperationsPrintHelper:AppName`, so no page text says "Vixly".
- **Logic and build:**
  - the logic is in `Vixly.PrintHelper.Core`, built and tested with the rest;
  - **the MAUI app itself was not compiled here** (no Android or iOS workloads). See its README.

## 2. Database

Three migrations, in the global script:

| Migration | Changes |
|---|---|
| `QuickSignIn` | Tables `StaffBadges`, `StaffPins`, `DevicePeople`; `UserSessions.IsQuick` |
| `DriverApps` | `DeliveryStops.CodeUnconfirmed`, `DeliveryStops.TrackTokenHash` (unique when set); table `DriverPositions` |
| `HandheldsAndPrintHelper` | Tables `ShelfCounts` and `PrintPairings`; `PrintAgents.IsPhone`; `LabelPrinters.AgentId` |

The script counts 21 Platform, 56 Operations and 1 Customer migrations.

## 3. Tests

| Suite | Result |
|---|---|
| Architecture | 15,178 passed |
| Operations domain / application / infrastructure | 302 / 126 / 72 passed |
| Operations web (SQL Server) | 127 passed |
| Operations API / Customer web | 34 / 45 passed |
| Print helper logic | 3 passed |
| Platform, Public, Shared | all passed |

New for this slice:
- **Application:** `OperationsQuickSignInTests`:
  - the authenticator once a day, then the badge alone, as a quick session;
  - licensed device only; a revoked badge refused;
  - another country blocks the device;
  - five wrong PINs lock it until a full sign-in;
  - the daily password;
  - PIN and badge code rules.
- **HTTP:**
  - `Badge_SignsInOnTheLicensedDevice_WithTheCodeOnceADay`;
  - `Driver_SendsStopsFromThePhone_LaterWithoutASignal_TheCodeIsMarkedNotRefused`: also the private link, positions and the map;
  - `ShelfCount_FindsWhatIsMissingAndMisplaced_AndAPhonePrintsOnItsOwnPrinterOnly`.
- **Print helper:** the QR link, a round that prints and reports (old labels and other kinds skipped), a stopped phone.

Fixed on the way: seven Operations web tests broken by the frontend team's markup changes of 2026-10-22 and 23 (exact HTML in the tests); the pages behave as before.

## 4. To publish
1. **Database:** run the global SQL script.
2. **Sites:** publish Operations Web, Operations API and the customer portal.
3. **Maps:**
   - production needs a map provider account, set in `OperationsMaps:TileUrl` and `Attribution` on both Operations pools;
   - staging uses OpenStreetMap through the site.
4. **Vixly Print:** build and publish the app (workloads and store accounts).

## 5. Open
- Owner sign-off.
- The frontend team styles the new screens (rows added to the styling inventory).
- Background location needs a native driver app (later, if wanted).
