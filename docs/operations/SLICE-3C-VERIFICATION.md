# Slice 3C: Inspection and holds - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 3) |
| Date | 2026-10-18 |
| Built on | Slice 3B; not committed here: the dev team owns git |
| Design | [Slice 3C design note](SLICE-3C-INSPECTION-DESIGN.md) section 1a (owner's answer to Q-C1); ADR-0018, ADR-0019, ADR-0021, ADR-0022 |
| Status | **Built and verified by automated tests; waiting for owner sign-off.** Step 1 (the customer's shipment) 2026-10-18; step 2 (restricted items, rules, queue, outcomes, holds) 2026-10-19 |

## 1. Step 1: the customer creates the shipment

### On the portal (`/customer/packages`)

- **New shipment** (`/customer/packages/shipments/new`):
  - the mailbox warehouse it ships from, asked only when the company has several;
  - where it goes: pickup at one of the company's destination branches, or delivery to an address in a country the company serves, as the company's delivery settings offer;
  - the recipient's name and phone, the customer by default; the phone with its country code;
  - consolidate, refrigerated, insured;
  - the first package: carrier, tracking number, store, contents, value.
- **The shipment's page:** where it goes, who receives it, the options, its announced packages and the packages that arrived. More packages are announced in it.
- **Change where it goes** while the shipment is open. A shipment without instructions (packages that came in unannounced) shows **Tell us where it goes**.
- **Cancel** while none of its packages arrived; its announcements are cancelled with it.
- `/customer/packages/new` leads to the new-shipment form; the old stand-alone announcement page is gone.
- A customer sees only their own shipments, packages and photos (S-3); another customer's shipment answers "not found".

### At the dock

- A package matched to an announcement joins **the shipment it was announced in**, when that shipment is open at this warehouse and is the same customer's.
- Otherwise (no announcement, an announcement from before 2026-10-18 without a shipment, a cancelled shipment, another warehouse) it waits in the customer's **one shipment without instructions** at that warehouse, on hold *awaiting customer information*. The customer gives that shipment its instructions, or announces the package by its tracking number in a shipment from that warehouse; the package moves there and the hold is released.
- The same rule applies when staff assign an unidentified parcel to a customer.

### For staff

- The parcel page and the inspection station show the shipment's instructions (where it goes, recipient, phone, options), read-only: only the customer writes them (D-C6).
- No mailbox address of the customer is shown to staff (owner, 2026-10-18).

## 2. Review fixes (independent review of step 1)

| Finding | Fix |
|---|---|
| H1: an announcement that could not be used (no shipment, or a closed one) was still matched, leaving the package stuck | Matched only when its shipment has instructions and is open; an older announcement of the number gives way to a new one instead of blocking it as a duplicate |
| H2: announcing a package that arrived at another warehouse | Refused with "This package arrived at another of our warehouses"; the customer announces it in a shipment from there |
| M1: a package announced while the shipment is being cancelled | Announcing changes the shipment's row, so the two clash and one is retried or refused |
| M2: assigning an unidentified parcel ignored its announcement's shipment | Assign uses the same placement as the scan |
| M3: announcing in a shipment without instructions gave a "customer not found" message | Its own message: tell us where the shipment goes first |
| L1: a parcel already past receiving could move to another shipment | Refused once it is past *received at origin* |

## 2a. Double click on the customer sign-in and sign-up (owner report, 2026-10-18)

The owner saw the app "crash" after a double click on the customer **Sign in** and **Sign up** buttons on the developer computer.

- **Cause.** On the developer computer the staff app passes `/customer/**` on to the customer portal (the development proxy; Edge does this in production). The storefront's *Sign in* and *Create account* buttons are links, and a double click on a link sends two requests, the browser dropping the first. The proxy let that dropped request end in an unhandled `TaskCanceledException` from the app's own code, which is where the Visual Studio debugger stops. With the portal not running it answered with the developer error page.
- **Fix.** The proxy ends a dropped request quietly (499, logged at Debug), and answers 502 with a plain "the customer portal did not answer, start it" when the portal is not running.
- **Checked.** A real Chromium against the portal on a real port, in Development: double click, five fast clicks and Enter twice on sign-in, double click on sign-up and on a wrong password. Each sent one form and showed the right page; the browser guard (`vixly-forms.js`) works. New `OperationsDevelopmentProxyTests` (real sockets) failed before the fix with the escaped `TaskCanceledException` and pass after it.
- Production does not use this proxy.

## 2a-bis. Email addresses with accents (owner report, 2026-10-19)

- **Problem.** Staff accounts such as `sofía@…` could be saved but never typed: a browser's email field accepts plain letters only.
- **New input.** New addresses are now plain letters only, as browsers and most mail services require. An international domain is entered in its `xn--` form.
- **Saved addresses.** They still load exactly as saved (`EmailAddress.FromStored`), so a stricter rule never stops the apps from reading older records.
- **Correcting the data.** `fix-accented-emails.sql` removes the accents from every email column of the Control and Operations schemas, for the people who must sign in.

## 2b. Step 2: inspection and holds (2026-10-19)

### Company setup (`/staff/inspection/setup`, `ops.inspection.configure`, company-wide)

- **Restricted-items list.** Each entry has:
  - a name (one per company);
  - the words that point to it in a declaration, in any language;
  - prohibited or restricted;
  - where it applies: destinations and carriers, empty for all;
  - what is needed.
- **Starting list (D-R5).** **Start from the suggested list** copies eight entries in the language of the person copying: lithium batteries, aerosols, flammable liquids, medicine, firearms and parts, currency, perishable food, and items needing a permit. Their words cover English, French and Spanish declarations. The copy belongs to the company and is a starting point, not legal advice.
- **Rules:**
  - the declaration names an item of the list;
  - the declared value is at or above an amount;
  - a carrier;
  - a destination country.

  Rules and entries are taken out of use, never deleted.
- **Random share.** Parcels in a hundred, 5 by default; 0 turns it off.
- **Reading declarations.** They are read word by word, without accents and with plurals, so "Médicaments" finds "medicament" and "power bank" needs both words in a row. The inspector confirms what is really inside (Q-C2).

### At the station (`ops.inspection.check`)

- After the check, a parcel goes to compliance inspection when:
  - the clerk flags it, with a reason;
  - a rule matches, using the destination of the customer's shipment;
  - the random share picks it, the first time only.
- The reason is recorded and the station says "set it aside".
- A parcel declared or given a destination after its check is checked against the rules again then.
- A parcel already chosen or inspected cannot be flagged again: the station says so.

### The queue and the inspection (`/staff/inspection/queue`, `ops.inspection.perform`, per warehouse)

- **The queue.** Oldest first, with why each parcel was chosen. Scanning an ID label opens that parcel's inspection.
- **The inspection page:**
  - shows the declaration, the shipment's instructions, and the listed items that apply to this destination and carrier, with what is needed; the items the declaration names are marked;
  - **Start** marks the inspection in progress;
  - the outcome form records the listed items found, notes, photos of the contents and the outcome.
- **Outcomes:**
  - **Passed.**
  - **Passed with notes.** Notes are required.
  - **Held for the customer.** Requires documents, a permit, a value or a fee, plus what the customer must do and the fee. The customer is emailed and sees "What we need from you" on the package page.
  - **Repacked.**
  - **Refused, return to sender.**
  - **Refused, dispose of.** Requires the day the customer was called and what was said (Q-C3).
- The parcel page shows its inspection.

### Holds (`/staff/holds`, `ops.holds.manage`, per warehouse)

- **The list.** Every open hold, by reason, with what is awaited.
- **Releasing.** A release needs a note, which goes into the history with who released it. Damaged, the inspection holds and Other are released here. The other reasons end through their own step:
  - Unidentified, by assigning the customer;
  - awaiting information, by the customer's answer;
  - refused for return, by **Returned to sender**, the parcel's last milestone.
- **Disposals awaiting approval.** The Owner or an administrator approves or declines. It cannot be the person who opened the box or asked for the disposal (the second person, Q-C3). Approved: the parcel is *Disposed*. Declined: it goes back to the inspector.
- **The menu** shows Inspection queue, Holds and Inspection rules only to the roles that allow them.

### Double clicks (ADR-0019)

Every new form sent twice at the same moment shows its result and records it once:
- setup: copying the list, saving an entry, adding a rule, saving the share;
- the inspection: start and outcome;
- holds: a release and a disposal decision.

Unique indexes back this: one entry per name, one rule per kind and value, one open inspection per parcel.

### Review of step 2

| Finding | Fix |
|---|---|
| The inspector who opened the box could approve the disposal another inspector asked for | Neither the one who started nor the one who finished may approve |
| A rule on the destination missed a parcel whose shipment was created in the same request | The new shipment is passed to the re-check |
| A parcel refused for return stayed on hold for good | **Returned to sender** ends the hold and sets the last milestone |
| A call recorded "today" was refused east of UTC | The day may be up to one day ahead of UTC |
| A clerk's flag on a parcel already inspected was dropped silently | Refused with a message |
| A scan in the queue found only the first page | The scan looks for that parcel's inspection |
| A carrier rule differing only in letter case gave a misleading error | Compared without case |
| The holds page showed a request whose hold had ended | Shown only while that hold is open |

### Not in this step

- The customer answering on the portal (uploading a document, paying a fee), reminders, and unclaimed packages: 3E.
- Fees from a fee list: Slice 4. Paid with the shipment's invoice: Slice 9.
- Holds blocking consolidation, dispatch and handover: Slices 5 to 8. They are already recorded in the projection.

## 3. Data

- `Shipments`: destination branch or delivery address, destination country, recipient name and phone, options; a customer has at most one shipment without instructions per warehouse (filtered unique index).
- `PreAlerts.ShipmentId`, with its index and foreign key.
- Migration `CustomerShipments` (step 1).
- Step 2: `RestrictedItems`, `InspectionRules` and `Inspections` (a row version; one open inspection per parcel), and `TenantSettings.InspectionPercent` (default 5). Migration `InspectionAndHolds`.
- The global script `build/scripts/sql/vixly-v4-database.sql` (19 Platform, 14 Operations, 1 Customer migrations) was run twice on an empty database, over the previous script with data, and checked for the application logins.

## 4. Tests

| Suite | Result |
|---|---|
| Operations domain | 182 passed |
| Operations application | 76 passed |
| Operations infrastructure (SQL Server) | 62 passed |
| Operations web (SQL Server and real sockets) | 75 passed |
| Operations API with the customer portal (SQL Server) | 22 passed |
| Customer web / infrastructure | 40 / 1 passed |
| Architecture | 9,883 passed |

New or changed:
- `OperationsShipmentDomainTests`: instructions, pickup and home delivery, recipient phone, cancel, announcing only with instructions, never into another customer's shipment.
- `Customer_CreatesShipments_AnnouncesPackages_AndSeesOnlyTheirOwn`: the whole portal flow, the package that arrived first, cancel refused once a package arrived, another customer sees nothing.
- `OlderAnnouncements_GiveWay_AndAPackageAtAnotherWarehouse_IsRefused`.
- `EveryCustomerForm_SentTwiceAtOnce_NeverFails`: the shipment forms sent twice at the same moment.
- Step 2: `OperationsInspectionDomainTests`, plus `OperationsInspectionHttpTests` on SQL Server:
  - rules, the flag and the share send parcels to the queue;
  - held, released, passed and returned to sender;
  - disposal with the call and a second person's approval;
  - a package declared after its check;
  - every inspection form sent twice at once.
- The dock tests: placement in the announced shipment; the staff instructions panel.

## 5. Not in this step

- Prices for the options (Slice 4); booking with a service and number (Slice 5).
- The consolidation waiting limit and the upload of the purchase invoice (3E and Slice 6).
