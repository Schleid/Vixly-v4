# Release 2026-10-25: first production fixes

| | |
|---|---|
| Packages | 150, 151, 152 |
| Tested | Full test run 151 on the development copy: every suite green (architecture rules included); to check on staging before production |
| First install or update | Update (see docs/global/PRODUCTION-UPDATE-RUNBOOK.md) |

## 1. What changed (for the owner)
- **Control staff employee numbers:** every Control staff member gets `TC-0001`, `TC-0002` … automatically when invited. The field is gone from the invite form. Existing staff are numbered in the order they were invited (the update replaces any number typed by hand).
- **Public payment page:** while the first invoice is being prepared, the page now checks every 10 seconds for 5 minutes (it was every 3 seconds without end), then shows a "Check again" button. When Platform asks a visitor to slow down, the page says "Too many attempts" instead of "We cannot reach our service".
- **Platform API limits:** the sign-up status and the sign-up invoice each allow 300 requests an hour per visitor (status was 60, shared with the invoice). Found on production: the payment page used the whole hour in a few minutes.
- **Offer page (sign-up with an offer link):** the offer card has padding, the expiry date has its own line and no longer wraps, the email line is set apart, and the Accept button is no longer inside an empty panel.
- **Designed emails (package 151):** every email now comes with a designed version, and the plain text stays for mail programs that want it.
  - **Vixly's own emails** (Control invitations, sign-up, billing, terms, licences): the Vixly logo on a blue header, the main link as a button ("Choose my password", "Confirm my email", "Pay online" …), the link again below it to copy, and a footer with the support address and Tech Crafters' address.
  - **Company emails** (Galeo to its customers and staff): the company's name as the header, the same button and copy link, and "This email was sent by Galeo Logistics." They never show Vixly; a test checks it in all three languages.
  - The texts themselves did not change; each email's button has its own words in English, French and Spanish.
- **Company profile without approval (package 152):** on Settings → Company, the owner now saves the trading name, email, phone and address directly. Only the **legal name** still goes through a request with proof that Control approves; approving it changes the legal name only and keeps the rest of the profile as the company last saved it. Control's company page says so.
- **Phone numbers in international form (package 152):** Control, the company workspace, the customer portal and the payment page show every phone as `+1 855-548-0504`, `+509 4137-6609`, `+33 6 12 34 56 78` … What is saved does not change; call links keep the saved number.
- **Country flags (package 152):** the website's locations and the Locations pages show real flag pictures (Windows shows flag emoji as two letters, "HT").
- **Logos (package 152):** when a company uploads a logo, the blank margins around it are cut away first, so it fills the header. Logos already uploaded need uploading again once.
- **Operations roles page:** 14 permission groups showed raw keys (`ui.permissions.area.charges` …); they now read Prices, Quotes, Shipments, Counter … in English, French and Spanish. A new test fails the build if a permission or group has no text.

## 2. Database
- **Run** `build/scripts/sql/vixly-v4-database.sql` **before** copying files.
- **New migrations:** Platform `EmployeeNumbers` (a sequence, the backfill of existing staff, a unique index on the employee number).
- **Migrations after the script:** Platform 24, Operations 56, Customer 1.
- **Removes nothing:** the previous version runs on the new database (rollback is safe; it simply does not use the sequence).

## 3. Settings
No setting changes.

## 4. Order and checks
1. Back up the database, and keep the previous publish folders.
2. Run the SQL script (same file for staging and production).
3. Copy, behind `app_offline.htm`: Platform API, Control (Platform Web), Operations API, Operations Web, Public. The customer portal does not change.
4. Check `/health/ready` on each, then:
   - Control → Users: each person shows an employee number (`TC-0001` for the first SuperUser);
   - invite a test person: the invite form has no employee number field, and the person gets the next number;
   - galeo.vixly.ltd → Roles → Add a role: every group heading reads as words;
   - vixly.ltd → open an offer link: the offer card and the Accept button look right on a phone and a computer;
   - Control → invite yourself on a second address (or "Forgot your password?"): the email shows the Vixly header and a button;
   - galeo.vixly.ltd/customer → "Forgot your password?": the email shows Galeo Logistics, a button, and no Vixly;
   - galeo.vixly.ltd → Settings → Company: change the phone and save (no approval asked); the legal name form asks for a reason and proof only;
   - Control → the company's page: the phone reads `+1 …`;
   - galeo.vixly.ltd → Website: upload the logo again, publish, and check the header; the locations show flags.

## 5. Known limits and follow-ups
- The payment page text "We are preparing your invoice" waits for Platform's outbox; if the invoice never comes (for example the billing Company profile is missing), the page stops after 5 minutes and the visitor can come back later.
- French and Spanish texts added here are marked `needs-review`.
- Company emails use the company name as the header; showing the company's own logo and colour (from Website settings) is a follow-up.

## 6. Second update of the day: health details, waived settings, logo
No database change: copy the files only (all six sites use the shared health code).

- **Why a site is Degraded or Unhealthy:** `/health/ready` still answers one word to everyone. With `?key=` (or the `X-Vixly-Health-Key` header) and the site's `VixlyHealth__DetailKey` (32 characters or more), it lists each check, its status and what is wrong: the setting's name and environment name, or the error message. Values are never shown.
- **Settings left out on purpose:** `VixlySettingsCheck__Waived__0`, `__1`, … name optional settings the site runs without (for example `PlatformPayments:Stripe:SecretKey`). They no longer degrade the site and are listed as "waived on purpose". Required settings cannot be waived.
- **Website logo:** the header logo is larger (56 px, 44 px on a phone), and the footer shows the logo: the dark-background logo when one is uploaded, otherwise the main logo on a white plate.

**Settings to add (each site):** `VixlyHealth__DetailKey` (new secret, environment variable). Optional: `VixlySettingsCheck__Waived__n`.

**Checks:** open `/health/ready?key=…` on api.vixly.ltd and Control; the Degraded line names the missing setting. galeo.vixly.ltd: the logo shows in the header and the footer.

## 7. Third update: idle warning in Control, countdowns, locations side by side
No database or setting change. Copy Control (Platform Web) and Operations Web.

- **Control:** one minute before the 5-minute idle sign-out, a box says so, with a countdown and "Stay signed in". At zero the page signs out on its own and the sign-in page says why. Several open tabs share the clock. Activity sent at most once a minute, as before.
- **Operations:** the existing warning (2 minutes before the 15-minute sign-out) now shows the countdown too.
- **Website locations:** countries sit side by side, each with its locations below; one country alone keeps its cards in a row.
- The countdown only displays what the server already enforces; the server still ends the session on its own.

**Checks:** sign in to Control and leave it for 4 minutes: the box shows 1:00 counting down; "Stay signed in" closes it; leaving it to zero lands on the sign-in page with the reason. galeo.vixly.ltd/staff: the same after 13 minutes, from 2:00. galeo.vixly.ltd: Haiti and United States side by side.

## 8. Fourth update: company emails in the company's look
No database or setting change. Copy Operations API and Operations Web (and Control, which shares the email layout).

- Emails a company sends (customers and staff) use its **published** website logo, on white under a band of its main colour, and the button in that colour.
- PNG or JPEG logos up to 300 KB; a WebP or larger logo, or no published logo, keeps the plain header with the company name.
- A main colour too light for white text keeps the plain colours. Still never "Vixly".

**Check:** galeo.vixly.ltd/customer → "Forgot your password?": the email shows the Galeo logo and colour.

## 9. Fifth update (2026-10-26): customer home address, shorter package forms
**Database: yes.** Run `build/scripts/sql/vixly-v4-database.sql` first (new Operations migration `CustomerHomeAddress`: six empty `Home_*` columns and `HomeAddressUtc` on `ops.Customers`; nothing removed, so the previous version still runs on it). Then copy Operations API, Operations Web and the customer portal.

- **Home address:** after signing in, a customer with no address is asked for it once (street, more, city, region, postal code, country); street, city and country are required. They change it later on Profile. Staff see it on the customer's page.
- **V3 customers:** the migration tool copies their V3 address onto V4 customers that have none, matched by email, never overwriting:
  1. Dry run: `dotnet run --project build/tools/V3Migration -- --tenant 01A17160-EE7F-765E-9647-DA058D9E8677 --addresses-only`
  2. Read the report (`v3-migration-<time>.txt`): each customer copied, skipped (no V3 address) or a problem (unreadable country).
  3. Same command with `--apply`.
  Customers left without an address are asked at their next sign-in.
- **Announce a package:** no Store field.
- **New shipment:** no "A new package in it" section; packages are announced on their own page.

**Checks:** sign in as a test customer with no address: the address page shows; save it; the home page opens. Profile shows the address. Staff → the customer's page shows it.

## 10. Sixth update (2026-10-26): payments added one by one, the card last for the rest
**Database: yes.** Run `build/scripts/sql/vixly-v4-database.sql` first (new Operations migration `SplitCardPayments`: `OtherTenders` and `ZelleTermsAccepted` on `ops.CardCharges`; nothing removed). Then copy Operations Web (and Operations API, which shares the application code).

- **Cashier → Pay:** one row, *Add a payment* (method, amount, cash handed over or reference). Each added payment goes into the list and comes off *Left to pay*; the row offers what is left for the next one. Any line can be removed. *Take the payment* records them all as one payment with one receipt, once nothing is left (the last one may stay in the row without *Add*). Cash once per payment; the Zelle box shows only when Zelle is used.
- **Card, always last:** *Charge the rest by card* (payment page or a reader) charges only what is left. The lines added before it wait on the card charge and are recorded with the card, as one payment, when Stripe approves it; declined or abandoned, nothing is recorded. The page says to keep the customer's cash until then.
- **One card per invoice** (on the page): two different cards cannot be used on the same invoice.
- **Zelle with a card:** the card part is paid at once; the Zelle part waits for finance as before. *Not received* takes back only the Zelle part.
- Up to six lines per payment (was three).

**Checks:** an invoice: add cash 1.00 (handed over 5.00), then *Charge the rest by card* on the page; the card page says "Plus 1.00 … recorded when the card is approved"; pay with a test card; the payment shows Cash 1.00 (change 4.00) and Card for the rest, and the drawer has the 1.00. Another invoice: cash, then MonCash for the rest, *Take the payment*.

## 11. Seventh update (2026-10-27): freight collect at the cashier and the counter; no "New shipment" on the customer page
No database or setting change. Copy Operations Web and Operations API.

- **Cashier → an invoice:** a box *Where the customer pays*: *Pay at the destination* (with an optional reason), or *Pay here instead* to undo. Any person who takes payments, when the company allows freight collect (Settings). The invoice and its shipment switch together; the amounts don't change.
  - Only on a shipment's invoice with nothing paid, waiting for confirmation or on account; never with supplies; back to *paid here* only until a parcel has left.
  - After a switch the page says to reprint the waybill. Recorded in the audit with who, when and the reason.
- **Counter → an open shipment:** the same box, for people who take payments, while the shipment is still open.
- **The sender can still pay anytime on the portal**, collect or not.
- **Customers → a customer:** the *+ New shipment* button is gone; shipments are made at the counter by the people allowed to.
- **Portal → Announce a package:** the store field is back, named *Purchase store* (the shop the customer bought from), with examples; also copy the customer portal.

**Checks:** allow freight collect in Settings; open an unpaid shipment invoice at the cashier; *Pay at the destination*; the invoice says freight collect and the shipment can be bundled unpaid; *Pay here instead* puts it back. A customer's page has no *New shipment* button.

## 12. Eighth update (2026-10-27): ID labels scanned at the inspection station; a better phone camera
No database or setting change. Copy Operations Web and Operations API.

- **Parcels moved from V3 are found by scan again.** Their numbers keep V3's dashes, and the barcode on their ID label holds them; the scan was cleaned to letters and digits before the search, so it never matched. The search now tries the exact number, then the number without dashes, spaces, slashes, dots or underscores. This fixes the inspection queue, the station pages (move, shelf count, repack, bundle) and arrivals. Parcels made in V4 were not affected.
- **A clearer message:** a parcel that is found but has no inspection waiting (already finished, or let go at the station) now says so, instead of "This inspection was not found."
- **Phone camera, on the station pages and the dock scan (one shared reader):**
  - asks the back camera for full HD (it was the browser's default, often 640×480, too small for an ID label) and continuous focus where the phone allows it;
  - asks the phone's own reader only for the barcode types it supports (an unsupported type made it fail silently), otherwise the bundled reader with "try harder";
  - checks about six times a second;
  - a *Light* button when the phone has a torch;
  - a message when the camera cannot open (permission refused, or a browser that cannot read barcodes), instead of nothing.

**Checks:** on a phone, *Inspection → queue*: *Camera*, point at a migrated parcel's ID label (a number with dashes) that is waiting for inspection; its inspection opens. Scan a parcel that is not waiting: the page says it was found but is not waiting. Deny the camera permission: a message says to allow it. Phones need the page on https.

## 13. Ninth update (2026-10-27): inactivity sign-out set by the security administrator
**Database: yes.** Run `build/scripts/sql/vixly-v4-database.sql` first. It adds two migrations, both named `IdleSettings`, and removes nothing:
- Operations: `StaffIdleMinutes`, `CustomerIdleMinutes` and `IdleWarnMinutes` on `ops.TenantSettings` (15, 60 and 2 for existing companies);
- Control: `IdleMinutes` and `IdleWarnMinutes` on `platform.SecuritySettings` (5 and 1).

Every company keeps the times it had until someone changes them. Then copy Operations Web, Operations API, the customer portal and Control.

- **Operations → Settings → Security → Sign-out after inactivity:**
  - **Staff:** 5 to 60 minutes, 15 by default.
  - **Customers on the portal:** 15 to 240 minutes, 60 by default.
  - **Warn before:** 1 to 5 minutes, 2 by default.
  - The same people who change *Require MFA* change these, with a fresh confirmation. The change is recorded in the history.
- **Customer portal:** customers now get the same warning with a countdown and *Stay signed in*. After the sign-out, the sign-in page says why.
- **Control → Users → Sign-in security:** the security administrator (the right to end sessions) sets *Sign out after* (5 to 30 minutes, 5 by default) and *Warn before* (1 to 5, 1 by default), with a fresh confirmation. The change is recorded in the history.
- **When a change applies:** at once on the server where it was made. Elsewhere it applies within a minute, from each person's next page. The server enforces the time; the page only warns.
- The activity lists (staff sign-ins in Operations, sessions in Control) use the set time to show when an unattended session ended.

**Checks:**
1. In Operations Settings, set staff to 20, customers to 90 and warn to 3, then save.
2. Open any staff page. The page source has `data-idle-warn="1020" data-idle-end="1200"`.
3. On the portal, sign in as a test customer. The page has `data-idle-end="5400"` (it can take up to two minutes to show).
4. In Control → Users, set 10 and 2 minutes. Stay idle: the warning shows at 8 minutes and the sign-out comes at 10.

## 14. Tenth update (2026-10-27): customers pay on the portal by Zelle, bank transfer or card
**Database: yes.** Run `build/scripts/sql/vixly-v4-database.sql` first. It adds the Operations migration `PortalPayments`, which puts six columns on `ops.TenantSettings` and removes nothing:
- `PortalZelle`, `PortalBankTransfer`, `PortalCard`;
- `BankTransferInstructionsEn`, `BankTransferInstructionsFr`, `BankTransferInstructionsEs`.

Companies that took cards keep them on the portal. Then copy Operations Web, Operations API and the customer portal.

- **Settings → Payment methods → Offer on the customer portal:** one box each for Zelle, bank transfer and card. A method shows on the portal only when its box is ticked and it is set up:
  - **Zelle:** accepted, with its tag or email/phone, the name shown and the terms;
  - **Bank transfer:** accepted, with its instructions (English required; French and Spanish optional);
  - **Card:** accepted, with the Stripe account connected.

  Cash, cheque and the company's own methods (MonCash…) are never offered there. Changing the bank instructions needs a fresh confirmation, like Zelle's payee.
- **Portal → Invoices:** an unpaid invoice gets a **Pay** button when at least one method is offered. With several, the customer chooses; with one, it opens directly (card alone goes straight to Stripe's page). The customer always pays the whole amount left.
- **Zelle:** the QR picture, tag, email or phone, name, the amount and the invoice number to write in the memo. The customer accepts the terms, gives the Zelle confirmation number and presses *I have sent the payment*.
- **Bank transfer:** the company's instructions, the amount and the invoice number. The customer gives the transfer reference.
- **After sending:** the invoice shows *waiting for confirmation*. The payment appears in **Payments to verify**, marked *Sent by the customer on the portal*. Finance marks it received or not received, as for the cashier's Zelle. Sending twice records one payment.
- A customer can never see or pay another customer's invoice.

**Checks:**
1. In Payment methods, accept Zelle (tag, name, terms) and bank transfer, write the instructions, tick both portal boxes and save (with a fresh confirmation).
2. On the portal, as a test customer with an unpaid invoice, press **Pay**, choose Zelle, accept the terms, enter a reference and send. The invoice shows waiting for confirmation.
3. In Payments to verify, the payment is marked as sent by the customer. Mark it received: the invoice is paid.

## 15. Eleventh update (2026-10-27): all the labels of a shipment at once
**Database: not for this change.** The global script `build/scripts/sql/vixly-v4-database.sql` is regenerated and now also carries the portal team's Operations migration `CustomerWelcomeTour`; run it before copying their customer portal and Operations API. For this change, copy Operations Web.

- **Counter shipment page and staff shipment page:** a new bar, *Labels of this shipment*:
  - **Print all ID labels (3):** one PDF in a new tab, one label per page, ready to print in one go;
  - **Print all shipping labels (3):** the parcels cleared for shipping, one per page; each is then marked printed. Staff who don't see the warehouse's parcels don't get this button;
  - **Send all to printer:** shown when the origin warehouse has a label printer for that kind of label (ID or shipping). All the labels go to it at once. Shipping labels still waiting for inspection or a hold are left out, and the message says how many.
- **One label:** each parcel line keeps its own *ID label* link, and on the staff shipment page a *Shipping label* button when the parcel is cleared, to reprint just that one.
- **Unchanged:** warehouse reception (one ID label per parcel as it arrives), the inspection station and *Labels to print*.

**Checks:**
1. At the counter, create a shipment with 3 parcels. Press **Print all ID labels (3)**: one PDF with 3 pages opens.
2. On the shipment, press **Print all shipping labels**: the cleared parcels print, one per page, and show *Printed*.
3. With a label printer in the warehouse, choose it and press **Send all to printer**: the message says how many labels were sent.

**Also fixed in this update (portal team's work merged on 2026-10-27):**
- **Insurance → Add a policy** showed "Something went wrong": the new tabbed page read the policy's name before there was a policy. It opens again.
- The `CustomerWelcomeTour` migration now runs inside the global SQL script, which stopped with "Invalid column name 'WelcomeTourDismissedUtc'". The script was run twice on an empty database without errors.

## 16. Twelfth update (2026-10-28): tracking numbers, the document logo, quote PDFs
**Database: yes.** Run `build/scripts/sql/vixly-v4-database.sql` first. It adds the Operations migration `TrackingAndDocumentLogo` and removes nothing:
- on `ops.Shipments`: `TrackingNumber` (unique) and `TrackingNumberUtc`;
- on `ops.TenantSettings`: `TrackingPrefix` (default VX), `DocumentLogoImageId`, `LabelLogoImageId`, `DocumentLogoUtc` and `DocumentLogoByUserId`.

Then copy Operations Web, Operations API, the customer portal and Control (Platform Web).

**Tracking numbers**
- **Format:** `VX 482 917 36 054`, two letters and 11 digits. The digits are random, never in sequence, and the last one is a check digit, so a mistyped number is refused instead of showing another shipment.
- **When a shipment gets one:** when its invoice is paid in full, charged to the customer's credit account, or the shipment is collect. A Zelle or bank transfer waiting for finance gets none until finance marks it received.
- **Where it shows:**
  - the counter and staff shipment pages ("Given once the shipment is paid" until then);
  - the customer portal's shipment page, with a *Track* link;
  - the invoice and receipt PDFs;
  - the customer's emails about the shipment (ready for pickup, out for delivery, delays);
  - the shipping label, as its main barcode.
- **Shipments already paid, on account or collect** get their number when Operations Web starts after the update (the hourly clean-up round), and any missed later.
- **Tracking on the company website** (`/en/track`) now works with the tracking number. It shows only the steps and their dates, never names, addresses or amounts.
- **Settings → Numbering → Tracking numbers (2 letters):** VX by default. A white-label company can use its own letters. Numbers already given keep theirs.

**Document logo (Company page)**
- **Company → Document logo:** upload a PNG (at most 1 MB and 2000 × 2000 pixels; Owner and Administrators, with a fresh confirmation). Once saved, the page shows:
  - the logo as it prints, and its black and white copy for labels;
  - its size, and who saved it when;
  - *Sample page (PDF)* and *Sample shipping label (PDF)*.

  *Replace logo* and *Remove logo* are there too.
- **It prints at the top of** invoices, receipts, statements, the declaration, the insurance certificate, the packing list and commercial invoice, house waybills, manifests, the shipper's letter, the run sheet and quotes. Shipping labels print the black and white copy. Without a logo, the company name prints as before.
- **Control → Billing settings → Company profile:** the saved logo is now shown, with *Replace the logo* and *Remove the logo*. The logo section always shows; before the profile is saved it says to save the profile first.

**Quote PDF**
- **Quotes and Quotes → all quotes:** each quote has *Print (PDF)*, and *Email to customer* while it is open. The PDF is in the customer's language, with the logo, the route, the pieces, the priced lines, the total and *Valid until*. The email goes to the customer's address with the PDF attached. A withdrawn or past quote is not emailed.
- *Valid until* is the quote's own number of days, 14 by default, set on the quote form as before.

**Also in this update**
- The global SQL script carries the portal team's `CustomerWelcomeTour` migration (section 15).

**Checks:**
1. Take a payment for a shipment at the cashier: the counter page shows its tracking number; the receipt PDF prints it.
2. On the company website, open Track and type the number with spaces or small letters: the steps show. Change one digit: "No shipment matches this number".
3. Company → Document logo: upload a PNG; the page shows it with its label copy; open both samples.
4. Print a shipping label: the logo is at the top and the tracking number is the large barcode.
5. Quotes: open *Print (PDF)* on a quote, then *Email to customer*; the customer receives the PDF.
6. Control → Billing settings: the logo shows under Company profile.

## 17. Thirteenth update (2026-10-28): shipping labels only after payment
**Database: no.** Copy Operations Web.

- **A shipping label now prints only once the shipment has its tracking number**, that is when it is paid, charged to a customer account or marked collect. A shipment waiting for payment (including a Zelle or bank transfer not yet confirmed) gets no shipping label, so it cannot be labelled and moved on by mistake.
- **Counter and staff shipment pages:** until then, the *Labels of this shipment* bar shows only the ID labels, with the line *Shipping labels print once the shipment is paid.*. Each parcel line says *After payment* instead of the *Shipping label* button.
- **Inspection station:** unchanged. Parcels are still inspected when they arrive, paid or not. A checked parcel of an unpaid shipment prints no shipping label; its label prints after payment, from the shipment or from *Labels to print*.
- **Labels to print** lists only shipments that have their tracking number.
- **Label printers (ZPL):** the shipping label sent to a thermal printer now carries the company's black and white logo (from Company → Document logo) and the tracking number as the large barcode, with the number written under it. Without a logo, the company name prints as before.
- A link or button used before payment answers *Not paid yet: the shipping label prints once the shipment is paid, charged to an account or collect.*

**Checks:**
1. At the counter, create a shipment and don't take payment: the bar shows only the ID labels and the payment line.
2. Take the payment: *Print all shipping labels* appears; the label's large barcode is the tracking number.
3. With a label printer and a saved document logo, press *Send all to printer*: the printed label shows the logo at the top.

## 18. Fourteenth update (2026-10-29): the maintenance page at the edge (Cloudflare Worker)
**Database: no.** Copy Control, Operations Web, the customer portal and the public site. Then set up the Worker in the Cloudflare dashboard with `build/edge/maintenance-worker/README.md`; until then nothing changes for visitors.

- **The Cloudflare Worker** (`build/edge/maintenance-worker/worker.js`), installed by pasting it in the Cloudflare dashboard:
  - **during a window published in Control,** it shows the maintenance page without contacting the servers, with the company's logo and name (never "Vixly") in the visitor's language. IT's pass link still opens the site;
  - **when a server fails** (502, 503, 504 or a Cloudflare 52x error, including during an update copy), it shows the same page with "not available right now". Program calls get a short JSON answer;
  - everything else goes to the servers untouched. If the Worker fails, the sites keep working ("fail open"). Control is never closed.
- **Control** sends the open windows to the Worker after every publish, cancel and *End now*, and every hour. **Control → System → Jobs** shows the hourly send as *Maintenance sent to Cloudflare*.
- **Operations Web** sends the company's name, main colour, language and website logo (100 KB or less) to the Worker when the website is published, and every night at 06:00 UTC.
- **New settings, only once the Worker is set up:**
  - Control: `Edge:MaintenanceUrl` and the secret `Edge__MaintenanceSecret`;
  - Operations Web: `Edge:BrandUrl` and the secret `Edge__BrandSecret`.

  Empty, nothing is sent; staging stays as it is.
- **Fixed: the pass link of a window closing only the staff workspaces.** It was refused on the sites. Open it under `/staff`: `{company}.vixly.ltd/staff/maintenance-pass/{code}`. Control now shows both forms after publishing.

**Checks:** follow step 6 of the setup sheet:
1. the sites work as before;
2. your sign-in history shows your real country;
3. the Jobs page shows the hourly send;
4. publishing the website sends the brand;
5. a 10-minute test window shows the branded page, the pass link opens it, and *End now* reopens it.

## 19. Fifteenth update (2026-10-29): branches as countries, company-only tasks, standard roles (package A)
**Database: yes, together with the copy.** The script removes the old *side* column, so the Operations Web and Operations API now running stop working once it runs. Do it in one go: put `app_offline.htm` on Operations Web and Operations API, run the whole global script `build/scripts/sql/vixly-v4-database.sql` (migration `BranchesAsCountries`), copy both, then remove `app_offline.htm`. The customer portal, Control and the public site do not change. Design: project document *Branches, warehouses and staff roles* (2026-10-29).

- **A branch is now a country or market you serve** (for example United States, Haiti); its warehouses are the real places.
  - **Locations → branch:** choose its **country**, and optionally its **legal identity** there (legal name, tax number, lines the country requires on invoices). The address becomes **Main office (optional)**. The *origin / destination / both* choice is gone: your routes and each warehouse's roles decide what goes where.
  - **Each branch's country is filled from its address** by the script. Nothing else changes in your branches; rename them yourself (owner, 2026-10-29), for example Fort Lauderdale → *United States* and Les Cayes → *Haiti*.
  - **A warehouse must be in its branch's country**, checked when it is saved.
  - **Invoices** issued at a warehouse print its branch's legal identity when one is set; otherwise the company's, as before.
  - A branch without an office does not show on your website.
- **Company-only tasks.** Staff, roles, devices, company settings, branches and warehouses, services, charges, carriers, inspection rules, the website and the audit log now work **only in a role given for the whole company**. Given for a branch or a warehouse, they do nothing. The role form marks them *company level only*.
- **Device approval is an ordinary permission:** *Approve new devices* and *Release devices* can be in any company role (for example a Security administrator), given for the whole company. The Owner and Administrators keep them.
- **Standard roles:** the first time someone opens Roles or People, every company gets nine ready roles: Security administrator, Finance, Customer service, Branch manager, Warehouse manager, Counter clerk, Cashier, Warehouse worker, Driver. They are named in the company's language and fully editable; your own roles are untouched, and a name you already use gets " 2".

**Checks:**
1. Run the script; then *Locations* shows each branch with its country flag.
2. Open a branch: country, legal identity and *Main office (optional)*; no origin/destination.
3. Try to add a US warehouse to the Haiti branch: it is refused.
4. Open *Roles*: the nine standard roles are there.
5. Open a role: staff, roles and devices permissions say *company level only*.

## 20. Sixteenth update (2026-10-29): find one customer, the customer list for the company only (package B)
**Database: yes.** Run the whole global script `build/scripts/sql/vixly-v4-database.sql` (migration `CustomerLookup`), then copy Operations Web and Operations API. The script only adds a permission to roles; the sites keep working while it runs, but do not edit a role between the script and the copy. The customer portal, Control and the public site do not change. Design: *Branches, warehouses and staff roles*, R-4.

- **New permission *Find a customer*.** Staff who have it, and not the customer list, see **Customers → Find a customer**: they type the customer's full phone number, email or customer code and get that one customer, never the list. One exact match opens the customer's page.
- **The customer list is now company level only.** *See the customer list* works only in a role given for the whole company; given for a branch or a warehouse it does nothing.
- **Every lookup is recorded** in the audit log (area *Lookups*), with the customer found but never what was typed.
- **Limit:** someone without the customer list can do at most **120 lookups an hour** (the design said 60; 120 leaves room for a busy counter). After that they are told to wait.
- **The front desk and the cashier** use the same lookup, so they are recorded and limited the same way.
- **Your roles:** every role that had *See the customer list* also gets *Find a customer*, so nobody loses anything. Standard roles made from now on: Counter clerk, Cashier, Branch manager and Warehouse manager get *Find a customer*; Finance and Customer service keep the list. If your standard roles were already made, untick *See the customer list* on those four if you want them limited.

**Checks:**
1. Run the script, copy both sites.
2. Give a test person a role with only *Find a customer*, for a branch. Their *Customers* opens *Find a customer*, with no list.
3. Search a name: nothing. Search an exact email: the customer's page opens.
4. As the owner, open *Audit*, area *Lookups*: the lookup is there, without the email.

## 21. Seventeenth update (2026-10-29): support agents, outside accounts, call tickets (package C)
**Database: yes.** Run the whole global script `build/scripts/sql/vixly-v4-database.sql` (migration `SupportAgents`: a new table for call tickets and five new columns on staff accounts, all empty), then copy Operations Web and Operations API. The sites keep working while the script runs. The customer portal, Control and the public site do not change. Design: *Branches, warehouses and staff roles*, section 9.

- **Support agent role.** The first time someone opens Roles or People, every company gets a tenth standard role, *Support agent*, with one permission: *Answer callers about one shipment*. Give it for the whole company.
- **Outside accounts.** On a staff member's page, *Outside account*: tick *This is an outside account*, then choose the end date, the working days and the times (in the company's time zone). An outside account can only use *Support calls*, whatever its roles say; outside its days and hours, or after the end date, it cannot sign in and its open session stops working. The Owner, an Administrator or a partner's agent can never be one.
- **Support calls** (`/staff/support`), for the agent:
  1. The caller's full phone number: only their first name and last initial, customer code, and *Good standing* or *Please refer to billing*. Never an amount.
  2. A tracking number: the company's, or the carrier's for a package. It opens only if the shipment or package is the caller's (they sent it, or their phone is the recipient's). Then: the milestones, holds, destination, amount due or paid, the customer's masked phone and email, and city. Other packages of the shipment only as a count.
  3. Without a matching phone: the status only, by the company's tracking number. A carrier's number needs the phone.
  4. A call ticket for staff, with what the caller asks. The agent changes nothing else. The page cannot be printed.
- **Call tickets** (`/staff/support/requests`), for staff with *Manage customers* for the whole company (Customer service): open ones oldest first, closed ones; close with a note. The customer never sees them.
- **Recorded and limited:** every phone and tracking number lookup is in the audit log (area *Support calls*), never the phone typed; at most **60 lookups an hour** per agent. Call tickets are in the area *Call tickets*.
- **Fix to package B:** the audit log's area list now has *Customer lookups* (it worked by link only).

**Checks:**
1. Run the script, copy both sites, open *Roles*: *Support agent* is there.
2. Invite a test agent with *Support agent* for the whole company; on their page, make it an outside account for today.
3. As the agent: *Support calls*, a customer's phone, then one of their tracking numbers: the shipment opens with masked contact. Another customer's number: nothing.
4. Open a call ticket; as the owner, *Call tickets* shows it; close it.
5. Change the agent's days to tomorrow only: the agent's *Support calls* no longer opens.

## 22. Eighteenth update (2026-10-29): a Support chapter in the menu
**Database: no.** Copy Operations Web and Operations API (after section 21's script).

- **Support** is a new chapter of the staff menu, just above *Technical*, with *Support calls* (agents), *Call tickets* (customer service) and *Package tickets* (the customers' missing-package tickets, which before had no menu entry).
- **The workspace** shows *Open call tickets* to customer service; the counter *Open tickets* is now called *Open package tickets*.

## 23. Nineteenth update (2026-10-29): one ticket desk, departments, escalation
**Database: yes.** Run the whole global script `build/scripts/sql/vixly-v4-database.sql` (migration `TicketDesk`), then copy Operations Web and Operations API. The script moves the call tickets of section 21 into the tickets table and removes their old table, so put `app_offline.htm` on Operations Web and Operations API while it runs. The customer portal, Control and the public site do not change. Design: project document *One ticket desk* (2026-10-29).

- **One desk:** *Support → Tickets* holds the customers' missing-package tickets and the tickets support agents open during calls. *Call tickets* is gone (its address leads to the desk).
- **Topics decide the department.** Missing or wrong package: the warehouse's staff. Billing and payment: Finance (invoices or payments). Pickup, delivery, recipient: customer service and the branch's shipment staff. Damage or claim: customer service and insurance. Other: customer service. Only that department sees the ticket; the Owner and Administrators see all.
- **The agent picks the topic** when opening a ticket from a call.
- **Taken by:** *I'm on it*, *Give* to a colleague of the department, *Release it*; answering also takes it. *Move* sends it to another department (not a customer's missing-package ticket).
- **Filters:** state (open, new, taken, answered, escalated, resolved, all), topic, warehouse, *Taken by me*.
- **A support call's ticket stays internal:** staff add notes; the customer is never emailed about it.
- **Escalation:** nobody took a ticket after **4 hours**: one email to the department's managers (those with the department's rights for the whole company; if none, the Administrators; if none, the Owner). Still not resolved after **3 days**: one email to them and the Owner. Set both under *When managers are told* on the desk (settings rights). A job runs every 15 minutes (*ops.tickets.escalations*); escalated tickets show *Escalated* and come first.
- **Workspace counters:** *Open package tickets* (warehouse), *Open billing tickets* (Finance), *Open customer service tickets* (customer service).
- **Audit:** areas *Tickets* and *Support agents* added to the audit list.

**Checks:**
1. Script, copy, then *Support → Tickets* opens the desk.
2. As an agent, open a call ticket with topic *Billing and payment*; a person with only the parcels rights does not see it, a person with invoices rights does.
3. *I'm on it*, then *Release it*; *Move* to *Other*.
4. With a ticket nobody took for more than the hours set, the next run of the job sends the managers one email.

## 24. Twentieth update (2026-10-29): website themes, fonts, air and sea photos (website themes, step 1)
**Database: yes.** Run the whole global script `build/scripts/sql/vixly-v4-database.sql` (migration `WebsiteThemes`: eight new columns on the website settings, all with defaults; nothing moves), then copy Operations Web and Operations API. The customer portal, Control and the public site do not change. Design: project document *Company websites: themes, tiers and the guided builder* (2026-10-29).

- **Three themes** for a company's website: *Classic* (today's page, every company's default), *Horizon* (dark, bold opening with a route drawing, key figures, dark and light sections in turn) and *Air & Sea* (bright, a large photo, air and ocean side by side, locations near the top). Every theme shows the company's own content; tracking, sign-in, account creation, languages and the footer links are always there.
- **Fonts:** Standard, Manrope, Plus Jakarta Sans, DM Sans and Lora (serif titles), served by the site itself (no font service).
- **Air and sea photos:** two new image slots on the *Website* page for the Air & Sea theme; original illustrations in the company's colours until they are uploaded.
- **Preview side by side:** the *Website* page has *Preview: Classic / Horizon / Air & Sea*; each opens the draft in that theme without saving. Only the Owner's preview can do this.
- **Plans:** choosing a theme or font and uploading the air and sea photos need the plan feature `portal_theme_customization` (the one that already gives own colours). Without it the choices are locked in the editor; a website already published keeps its theme, font and photos (nothing breaks on a downgrade).
- **Key figures** (Horizon): services, countries and locations counted from the company's own setup; a zero is not shown.
- **Fix:** the *Website* page no longer shows the document and label logo cards (they belong to the *Company* page and could not be uploaded there).
- **Help:** new article *Your website: theme, font and photos* (EN/FR/ES).
- **The global script now ends with a status table:** the server and database it worked in (check both: an SSMS query tab can be connected to another server than Object Explorer), and for each schema the newest migration in the file and in the database, with *UP TO DATE* or *NOT UPDATED*. If the file column is older than the migration named in these notes, SSMS ran an old copy of the file: close the tab and open the file again.

**Checks:**
1. Script, copy, then *Website*: the *Theme and font* box and the three *Preview* buttons are there.
2. *Preview: Horizon* and *Preview: Air & Sea* open in a new tab with your content; the public site still shows Classic.
3. Choose *Air & Sea* and *Manrope*, save, publish: the public home page changes; check it on a phone width.
4. Upload an air cargo photo: it replaces the illustration in the air panel after publishing.
5. On a company whose plan lacks theme customisation: the box is locked, the previews still open, the published site stays as it was.

## 25. Twenty-first update (2026-10-30): shipments per month in the plans (shipment cap)
**Database: yes.** Run the whole global script `build/scripts/sql/vixly-v4-database.sql` (migrations `ShipmentCap` in Platform and in Operations: a price column on plan versions, a new table `platform.TenantShipmentUsage`, three columns on `ops.TenantSettings`; nothing moves). Then copy Control (Platform.Web), Platform.Api, the public site, Operations Web, Operations API and the customer portal. Design: project document *Shipments per month* (2026-10-29).

- **Control, plans:** a plan version gains **Shipments per month** (0 = no cap) and **Price per extra shipment**, in the draft form and the versions table. Plans already published have no cap until a new version sets one. TechCrafters administrators decide the numbers.
- **Public plans page:** a capped plan shows "Up to N shipments a month, then P each".
- **Control, tenant page:** the limits table shows the cap ("No cap" when 0) and the shipments counted for the latest month; the limit override works for the cap too.
- **Counting (Operations):** every shipment created in the company's calendar month, in its time zone: counter, portal and receiving. Cancelled shipments do not count. Every hour Operations sends each company's count to Platform (job `ops.plans.shipment_cap`); in the first 3 days of a month it also sends last month as final.
- **Charging:** when a month is final, Platform adds a one-time charge "Extra shipments YYYY-MM: N x P" to the subscription (shown in its history, audit `platform.subscriptions.shipments_charged`), billed on the next invoice. Each month is charged once. Under the cap, or without a cap, nothing is charged.
- **Warnings:** at 80%, 100%, 125% and 150% of the cap, one email to the Owner per level and month, and a notice on the workspace for those who manage the settings.
- **The stop:** above 150% of the cap, new shipments at the counter and in the portal are refused until the company upgrades or the month ends. The first time a company goes above 150% is its **grace month** (once per company): nothing stops, extra shipments are still charged. Receiving, payments, dispatch and releases never stop. In the portal the customer is only told new requests are paused.
- **Platform not reachable:** nothing is refused.
- **Help:** new article *Your plan's shipments each month* (EN/FR/ES).

**Checks:**
1. Script, copy, then in Control open a plan, start a draft: the two new fields are there; publish a test plan with a small cap (for example 3) and a price.
2. Put a test company on that plan. Create 4 shipments at its counter: the workspace notice appears for the Owner, the Owner gets the 80% and 100% emails within the hour.
3. Keep going past 150% (5 with a cap of 3): this first time it is the grace month and nothing stops; the Owner gets the grace email.
4. In Control, the tenant page shows the shipments counted for the month.

## 26. Twenty-second update (2026-10-30): plan modules (Vixly Lite) and the Subscriptions usage column
**Database: yes.** Run the whole global script `build/scripts/sql/vixly-v4-database.sql` (migration `PlanModules` in Platform: it adds the seven new plan features to every plan version already in the database, so every existing plan keeps everything it had; nothing else changes). Then copy Control (Platform.Web), Platform.Api, the public site, Operations Web, Operations API and the customer portal. Design: project document *Vixly Lite* (2026-10-30).

- **Control, plans:** seven new plan features, one per module a plan may leave out: **Customer portal**, **Insurance**, **Credit accounts**, **Supplies**, **Home delivery**, **Support desk**, **Bills and profit**. A plan version without one of them leaves that module out. Existing plan versions get all seven from the script. A new plan (for example Lite) is made in Control by a TechCrafters administrator: leave the seven features unticked, set the shipments per month, the licences and the warehouses, and the prices. Nothing about Lite is in the code.
- **Control, Subscriptions:** a new column *Shipments* shows each company's shipments counted for the latest month.
- **Public plans page:** the new features show in each plan's list.
- **Operations, a module left out:** its pages leave the menu and the workspace task links, and answer *Not found*; its permissions are withheld from every role (the roles keep them, so they come back with a larger plan).
  - *Customer portal:* the company website shows no sign-in or account buttons (the hero offers tracking instead); the portal answers that the account is unavailable; staff enter customers themselves from **Customers > New customer** (name, email, phone, language; the email is required and unique).
  - *Insurance:* no insurance products at the counter; an insured shipment is refused.
  - *Credit accounts:* no bill-my-account at the cashier.
  - *Supplies:* no supplies at the counter (on a shipment or as an order).
  - *Home delivery:* pickup only, whatever the company settings say.
- **Platform not reachable:** every module stays on.
- **Help:** new article *What your plan includes, and entering customers* (EN/FR/ES).

**Checks:**
1. Script, copy, then in Control open each live plan: the seven new features are ticked.
2. In Control, Subscriptions: the *Shipments* column is there.
3. Make a test plan without the seven features and put a test company on it. Within 5 minutes its workspace has no Insurance, Credit, Supplies, Delivery, Support, Tickets or Bills in the menu; **Customers** has *New customer*; its website has no sign-in buttons.
4. Put the test company back on its plan: everything returns within 5 minutes.

## 27. Twenty-third update (2026-10-30): side menu keeps its place; plan features on the public plans page
**Database: no.** Copy Control (Platform.Web), Platform.Api, the public site and Operations Web.

- **Side menu (workspace and Control):** a section the user closes stays closed, and the menu keeps its scroll after each click.
- **Public plans page:** the seven plan modules (customer portal, insurance, credit accounts, supplies, home delivery, support desk, bills and profit) show under their own names. Before, once the section 26 script had run, each showed as "Data export".

**Checks:**
1. Close a menu section, click another page: it stays closed. Scroll the menu down, click an item: the menu stays where it was.
2. vixly.ltd/plans: no repeated "Data export" line; each plan lists the modules it includes.

## 28. Twenty-fourth update (2026-10-30): the Air & Sea home page becomes a full sales page
**Database: yes.** Run the whole global script `build/scripts/sql/vixly-v4-database.sql` (migration `LandingMarketing` in Operations: three columns on the website settings, draft and published, and a new table `ops.SiteTestimonials`; nothing moves). Then copy Operations Web. Help: new article *Your home page: figures, rates and customers' words* (EN/FR/ES).

- **New Air & Sea layout:** a full-width hero with the company's main image under a brand-colour band, the headline, a small line above it, up to three promises, the two buttons and the tracking box over its lower edge. Then key figures, how it works, the air and ocean stories, other services, rates with an estimate, testimonials, why us, locations, questions and the closing call to action. Classic and Horizon do not change.
- **Key figures, real only:** packages delivered (counted from shipments picked up or delivered, rounded down, shown from 500, never typed in), delivery times per service (the company's words), pickup points, and one figure of the company's own.
- **Rates with an estimate:** when the company publishes prices, the home page shows its published lists (first bands) and an estimate for one package by service, pickup point and weight, from the same pricing engine as the counter; always labelled as an estimate (no customs, taxes or insurance). Works without script.
- **Customer testimonials:** *Your website > Customer testimonials*: staff record the customer's own words, name as agreed, place, rating and source, and must confirm the customer agreed to be quoted (recorded with who and when). Up to six shown, in order; show, hide or remove. No testimonial, no section.
- **New texts** in the website editor, in three languages: small line above the headline, promises, delivery times, own figure, air and ocean stories (title, text, up to four promises), why us (up to four cards), closing title and text. Three new switches under Sections: packages delivered, rates, testimonials.

**Checks:**
1. Script, copy, then in a company's workspace open *Your website*: the new text groups, the three switches and *Customer testimonials* are there.
2. Preview Air & Sea: the hero is full width; sections without content do not show.
3. With prices published, the estimate answers for a weight to a pickup point; with a service not published it says there is no published price.

## 29. Twenty-fifth update (2026-10-31): suspend, reactivate, close or ban a customer; the customer's shipments
**Database: yes.** Run the whole global script `build/scripts/sql/vixly-v4-database.sql` (migration `CustomerBans` in Operations: one column `SuspendedUntil` on `ops.Customers`). Then copy Operations Web, the Operations API and the customer portal. Help: new article *Suspend, reactivate, close or ban a customer* (EN/FR/ES).

- **Fix:** the customer page never showed its status panel (a value was not passed to the page), so no one could suspend or close a customer. It shows now for those who manage customers.
- **Four actions with a reason each:** *Suspend* (optionally until a day: the account is active again by itself that day, checked every hour), *Reactivate* (also reopens a closed account), *Close the account* (can be reopened), *Ban* (breaks the terms; permanent). Ban and reopening a closed account: the Owner and administrators only.
- **A ban also blocks** the customer's email and phone: a new portal sign-up with them gets the usual screen and no account; staff entry (Vixly Lite) and a phone change in the portal are refused.
- **The customer is told by email** every time, in their language, without the internal reason.
- **Work in progress continues;** a customer who is not active cannot be billed on credit (pays before release). Portal sign-in and password reset are refused for banned customers, as for suspended and closed ones.
- **Customer page:** new *Shipments* list (latest 20: number, date, service, route, parcels, status, each linked); the credit panel is gone (credit accounts only from *Credit*).

**Checks:**
1. Script, copy. Open a customer as the Owner: the status panel is at the bottom, with *Ban*; the *Shipments* list is there; no credit button.
2. Suspend a test customer until tomorrow: the page says until when, and the customer gets the email.
3. Ban a test customer: no action is offered any more; a portal sign-up with the same phone creates no account.

## 30. Twenty-sixth update (2026-10-31): the new shipping label with the service code
**Database: yes.** Run the whole global script `build/scripts/sql/vixly-v4-database.sql` (migration `ShippingLabelCodes` in Operations: one column `LabelCode` on `ops.ShippingServiceSetups`, empty for existing companies; empty means the default code). Then copy Operations Web and the Operations API (no change on the print helper). Help: *Shipping services* updated (EN/FR/ES).

- **Service code on the label:** each service has a three-letter label code, printed large at the top right of the 4 x 6 label so the warehouse sorts by eye. Defaults: Air standard VXA, Air express VXE, Ocean consolidated and Ocean full container VXS, Ground VXG. *Settings > Shipping services* gains a *Label code* column: exactly 3 letters A to Z, stored in capitals (also checked by the database); two services may share one.
- **A box per mode:** air solid black, ocean striped, ground outlined; express adds an *EXPRESS* band across the top.
- **New layout (Zebra ZPL and PDF alike):** logo, ship date and origin warehouse; route and service; recipient in capitals, city with a *PICKUP* (black) or *HOME* (outlined) tag on the right; the tracking barcode across the row with its number centred; weight, *INSURED* / *CONSOL* when they apply, piece n / N; the parcel barcode with its number; the shipment number at the foot, in print only (no barcode). No customer number (the customer is often the sender) and no contents (a thief would read them). The 2 x 1 parcel ID label does not change.

**Checks:**
1. Script, copy. *Settings > Shipping services*: the *Label code* column shows VXA, VXE, VXS, VXS, VXG; typing "VX" or "VX1" is refused.
2. *Company > Document logo > Sample label*: the new layout.
3. Print a shipping label on a Zebra at the dock: the code box and the barcodes print and scan.

## 31. Twenty-seventh update (2026-11-01): items in parcels, customs codes and the item catalogue
**Database: yes.** Run the whole global script `build/scripts/sql/vixly-v4-database.sql` (migration `ParcelItems` in Operations: new tables `ops.ParcelItems` and `ops.ItemCatalogue`, two columns on the company settings; nothing moves). Then copy Operations Web, the Operations API and the customer portal. Help: new article *Items and customs codes* (EN/FR/ES).

- **Customers declare item by item:** *Announce a package* (portal) asks for each item in the customer's own words, how many, the value of one, new or used. The package's contents and value are made from the items. Customers are never asked for a customs code.
- **At the counter:** the parcel row keeps contents and value as the first item; *More items in this box* takes up to four more.
- **Item catalogue** (*Item catalogue*, back office): filled once with about 150 everyday courier items and their 6-digit customs codes, with the words customers use (English, French, Spanish, Creole). The company changes it freely; it is never filled again once the company removed items. When a customer's words match, the description, code and unit are filled in by themselves; the country of origin is never guessed.
- **Items to review** (warehouse operations): the parcels at origin with an item missing its customs code or origin. On the parcel page, the new *Items* panel: description on documents (catalogue suggestions), customs code, origin (from the "Made in…" label), unit, *Add to the catalogue*. The customer's words, quantity and value are never changed by staff.
- **The rule before bundling, off until the company switches it on** (*Item catalogue* > *Require codes and origin*): from that moment, a new parcel cannot go into a bundle or a box, and its dispatch cannot close, until every item has a code and an origin. Parcels received before are never stopped, so nothing in progress is blocked.
- A customer's corrected declaration (hold answer) replaces the parcel's items with one item to classify again.
- No document changes yet: the commercial invoice, the export line and the battery question come next.

**Checks:**
1. Script, copy. Open *Item catalogue*: about 150 items are there.
2. Announce a package on the portal with two items: the package shows "2 x …, 1 x …" and the sum of the values.
3. Open a parcel with items: the *Items* panel shows the code suggested for a known item; enter the origin, save; the parcel leaves *Items to review*.
4. Only when the team is ready: *Require codes and origin*. A new parcel without its origin is refused at bundling with the item named.

## 32. Twenty-eighth update (2026-11-01): the Operations icon at staff sign-in; "Get started" on the plans page
**Database: no.** Copy Operations Web and the public site. No script.

- **Staff sign-in:** the company's website logo (squeezed into a square, or initials when none was published) is replaced by one fixed **Operations icon**, a parcel with a route arrow, the same for every company. It also replaces the initials in the top bar, marks the other sign-in pages (forgot password, verify, recovery, device) and is the browser tab icon of the staff workspace, the scan station and the screens. It never shows the platform's name.
- **The company is named in the subtitle:** "Sign in to the Galeo Logistics workspace." (FR/ES too), so staff see which company they sign in to.
- **Public plans page:** the plan button says **Get started** (Commencer, Empezar) instead of *Choose this plan*; the help article *Choose a plan* follows.

**Checks:**
1. Open a company's `/staff`: the parcel icon in the top bar and beside *Operations portal*, the company in the subtitle, the icon on the browser tab.
2. vixly.ltd/plans: each plan's button says *Get started*.

## 33. Twenty-ninth update (2026-11-02): Vixly on the TechCrafters Stripe account; sales reported to TechOS
**Database: yes.** Run the global script `build/scripts/sql/vixly-v4-database.sql` first (migration `SalesFeedAndCheckout`), then copy **Platform API** and **Control**.

- **Payment methods:** checkouts no longer send `payment_method_types` (refused by API 2026-09-30.endive). Stripe chooses the methods, narrowed per payment: sign-up and card saving take cards only (Apple Pay and Google Pay come with cards); pay links and invoices in USD also take a **US bank account (ACH)**; renewals charge the saved card only.
- **Bank payments are delayed:** a checkout paid by bank account is *processing*; the invoice is paid only when the bank settles, and the payment reads *Bank account (ACH)*. A failed bank payment fails the charge and the invoice stays unpaid.
- **Checkout look and statement:** Control › Payments settings has a new card, *Checkout page and bank statement*: name, logo, icon, colours (empty = Stripe Dashboard branding), the statement prefix (the account's shortened descriptor, for the wording only) and suffix (**VIXLY** set by the script). Card receipts and the new renewal reminder say the charge appears as e.g. *TECHCRAFT\* VIXLY*. Stripe allows 2 to 10 characters for the shortened descriptor, so *TECHCRAFTERS* (12) cannot be the prefix.
- **Renewal reminder:** 7 days before an annual renewal charged to a saved card (automatic charging on), the billing contact gets one email naming the card. New job *Renewal reminders* (hourly clock).
- **Sales feed to TechOS (contract version 1):** every invoice issued, payment received or reversed, void, credit and subscription change, plus daily totals, is sent to TechOS as a signed event, every minute, oldest first. The first run sends the whole history once (Galeo: its subscription, INV-2026-000001 and its payment). New page **Control › Sales feed** (`/sales-feed`): status, pending, rejected events with TechOS's error and *Send again*, *Try again now*. Staff with *payments.configure* get an email when events are rejected, when TechOS refuses the feed (400/401) and after 24 hours without an answer. Without the TechOS address or secret the feed is off; events still wait.

**Environment variables (Jude, SmarterASP; never in web.config):**

| Variable | Platform API | Control |
|---|---|---|
| `PlatformPayments__Stripe__SecretKey` (TechCrafters key, test first) | yes | yes |
| `PlatformPayments__Stripe__WebhookSecret` (Vixly's own endpoint's `whsec_…`; Control needs it too, or it reports the keys as not configured) | yes | yes |
| `PlatformSales__TechOs__Url` (staging `https://techcrafters.dyndns.ws/api/sales-sources/vixly/events`, production `https://techcrafters.ltd/api/sales-sources/vixly/events`) | | yes |
| `PlatformSales__TechOs__Secret` (the same value TechOS holds) | | yes |

**Stripe webhook endpoint** `https://api.vixly.ltd/payments/webhook` (staging `https://api.vixly.freeddns.org/payments/webhook`), events: `checkout.session.completed`, `checkout.session.expired`, `checkout.session.async_payment_succeeded`, `checkout.session.async_payment_failed`, `setup_intent.succeeded`, `payment_intent.succeeded`, `payment_intent.payment_failed`, `payment_intent.requires_action`, `refund.created`, `refund.updated`, `charge.dispute.created`, `charge.dispute.closed`.

**Checks:**
1. Control › Payments settings: publishable key and USD saved, *Check connection* succeeds; the new card shows the wording *TECHCRAFT\* VIXLY* once the prefix TECHCRAFT (the shortened descriptor set in Stripe) is entered.
2. Pay a test invoice from its pay link: the checkout shows Vixly's name, logo and colours, and offers card and US bank account. Pay $1 by card: paid, receipt mentions the statement wording.
3. Pay another $1 by test bank account: the invoice stays unpaid while processing, then is paid as *Bank account (ACH)*.
4. Registration checkout offers no bank account.
5. Control › Sales feed: *Sending: On*, pending goes to 0 within a minute, *Last answer from TechOS* set; Jobs shows *Sales feed to TechOS*; TechOS › Vixly sales shows Galeo's invoice and payment.

## 34. Thirtieth update (2026-11-02): sign-up card only; the company's time zone from the start
**Database: no.** Copy **Platform API**, **Control**, the **public site** and **Operations Web**. No script, no new setting.

- **Sign-up is card only:** the registration checkout no longer shows Stripe Link (the green *link* button and its *Bank* option). Card, Apple Pay and Google Pay stay. Pay links and invoices keep Link and the US bank account as before.
- **Time zone at sign-up:** a new company no longer starts on UTC. The sign-up form sends the browser's time zone; when it cannot (or sends something unknown), Platform takes the main time zone of the company's state or province, then of its country (Florida: America/New_York). The owner can still change it.
- **Operations follows the company:** the first time a company's workspace is used, Operations copies the company's time zone into its own settings (prices from midnight, reports, monthly cap) when they have none. A zone already chosen in Operations settings is kept; UTC is not copied. The audit shows *ops.settings.time_zone_from_company*.

**Checks:**
1. Sign up on the public site: the Stripe page shows Card (and Apple Pay or Google Pay on a device that has them), no *link* button, no *Bank* option.
2. Control › the new tenant: *Time zone* is your browser's (for example America/New_York), not UTC.
3. Open that company's workspace once, then Settings: the time zone is the company's.

## 35. Thirty-first update (2026-11-03): the sales feed says which setting stops it
**Database: no.** Copy **Control** (including its `appsettings.json`). No script.

- **Control › Sales feed:** when the feed is off, the status now names the setting: `PlatformSales__TechOs__Url` not set, not an https address, or only the site (it must be TechOS's full event address); `PlatformSales__TechOs__Secret` not set; or `PlatformSales:TechOs:Source` missing because the server's `appsettings.json` predates the feed.
- **Control's settings:** the sales feed runs in Control only. Control needs `PlatformSales__TechOs__Url` = `https://techcrafters.ltd/api/sales-sources/vixly/events` (the full address, not `https://techcrafters.ltd`), `PlatformSales__TechOs__Secret`, and the `PlatformSales` section of the new `appsettings.json` (`Source: vixly`). The Platform API does not use them.

- **`invoice.issued` always carries the service period:** a first-period invoice is issued before payment starts the subscription's period, so it now carries the period it will cover from its issue date, in the term it was priced for (TechOS, 2026-11-03). Only a one-time charges invoice (*Bill pending charges now*) has no period: `periodStart` and `periodEnd` are null.
- **Correct and send:** a rejected event gets a second button beside *Send again*. *Correct and send* writes the event again from Vixly's records as it would be written today, under a new id and the next number; the rejected one stays on record as replaced and is left out of the daily totals. TechOS keys invoices by number, so nothing counts twice.

**Checks:**
1. Control › Sales feed: *Sending: On*; *Waiting to be sent* drops to 0 within a minute; *Received by TechOS* counts up.
2. Rejected event #2 (`invoice.issued`, INV-2026-000001): click **Correct and send**. It leaves the rejected list; a new event is sent at the next run with `periodStart` 2026-10-25 and `periodEnd` 2027-10-25.
