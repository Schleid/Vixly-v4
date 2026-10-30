# Slice 3E: Customer side of holds and unclaimed packages - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 3) |
| Date | 2026-10-19 |
| Built on | Slice 3C (inspection and holds); not committed here: the dev team owns git |
| Design | [Slice 3 design note](SLICE-3-RECEIVING-DESIGN.md) sections 1 (3E), 7 and 9 (Q-R6, Q-R7, agreed as proposed); [Slice 3C design](SLICE-3C-INSPECTION-DESIGN.md) section 6 |
| Status | **Built and verified by automated tests; waiting for owner sign-off.** Paying a hold fee waits for Slice 9 |

## 1. What was built

### The customer answers a hold (`/customer/packages/{id}`)

- A package on hold for **documents**, a **permit**, the **declared value** (set by the inspector) or **damage** shows "What we need from you" and a form for each hold the customer can answer:
  - documents or permit: a file (PDF, PNG or JPEG, up to 10 MB), with an optional message;
  - declared value: the contents and value, and the invoice or receipt (optional);
  - damaged: ship it as it is, return it to the sender, or dispose of it.
- The file is checked by its first bytes (not its name or the browser's type), stored in the company's file store and fingerprinted (SHA-256). Anything else is refused with "The file must be a PDF, PNG or JPEG of up to 10 MB."
- While the answer waits, the page says "We received your answer. Our team is reviewing it." A new answer replaces the one still waiting. A declined answer shows why ("Your last answer was not enough: ...") and the form again.
- The customer only reaches their own packages: another customer of the same company gets "not found" (S-3).

### Staff review the answers (`/staff/holds`, `ops.holds.manage`)

- Each held parcel shows the waiting answer: the hold, when, the contents and value, the choice, the message and a link to the document.
- The **document is a download** with its own sandbox policy (`default-src 'none'; sandbox`, `nosniff`, `no-store`), never shown inside the staff site.
- **Accept** ends the hold:
  - a value answer corrects the parcel's declaration (contents, value);
  - *return it to the sender* makes the parcel's last milestone **returned to sender**;
  - *dispose of it* makes it **disposed**, and only the Owner or an administrator can accept it (the button is hidden for others and the request is refused).
- **Decline** needs a note; the customer gets the "action needed" email again and sees the note.
- Pressing twice gives one result (the answer's status decides).

### Reminders (hourly job)

- For packages still waiting on the customer at the warehouse: a reminder at day **7, 14 and 21**, a **final notice at day 25**. Each goes once.
- A job that did not run for a while sends only the latest step that is due (for example only the final notice), never a burst of emails.
- The job runs for each company in turn and records its run like the other jobs.

### Unclaimed packages (`/staff/holds`)

- A package still waiting on the customer, or still unidentified, after the company's **unclaimed days** (default 30; 30 to 180; Owner or administrator, on the holds page) is listed under "Unclaimed after N days".
- The Owner or an administrator decides: **return to sender** or **dispose of**, with a note. Every hold is released and the milestone recorded. A package that is not unclaimed yet is refused.

### Texts and emails

- New texts in English, French and Spanish (staff: `ops.holds.*`, `ui.holds.*`; customer: `ops.holds.*`, `ui.answers.*`).
- New emails: `parcel_reminder`, `parcel_final_notice`. No "Vixly" on any tenant page or email.

### Also fixed

- **Security headers:** a response that sets its own Content-Security-Policy (the sandboxed downloads of answer documents and parcel photos) now keeps it. Before, the site policy replaced it when the response started, so the parcel photo download of 3B never had its sandbox. The site policy (`script-src 'self'`) still applied, so nothing could run, but the sandbox is the intended second wall.

## 2. Database

- New table `ops.HoldAnswers` (one open answer per parcel and hold: filtered unique index; row version).
- `ops.Parcels.RemindersSent`, `ops.TenantSettings.UnclaimedDays` (default 30).
- Migration `20261019054858_HoldAnswersAndUnclaimed`, included in the global script `build/scripts/sql/vixly-v4-database.sql` (19 Platform, 15 Operations, 1 Customer migrations).
- Script tested: on an empty server, run twice; and over the previous script's database, run twice.

**On the development machine: run the global SQL script once before starting the sites.**

## 3. Automated tests

| Test | What it proves |
|---|---|
| `OperationsHoldDomainTests` | Kinds of answer per hold; required fields; replace, accept and decline rules; damage choices; reminder steps |
| `CustomerAnswers_AreReviewed_RemindersGoOut_AndUnclaimedPackagesAreDecided` (Web) | Reminder once at day 7, then the final notice alone at day 26; a wrong file refused; decline with note seen by the customer; an inspector cannot accept a disposal, the Owner can; accepting the value corrects the declaration; the sandboxed download; an unidentified package unclaimed at day 35 returned to sender; a package not yet unclaimed refused |
| `Customer_AnswersAHold_WithADocument` (Api, through Customer.Web) | The form on the package page; a text file refused; a PNG accepted ("reviewing"); another customer gets 404 |

All suites green: Operations (Domain 185, Application 76, Infrastructure 62, Web 76, Api 23, Customer.Web 40), Platform, Public and the architecture tests (9,987).

## 4. Independent review (2026-10-19)

A separate reviewer read the change without having written it. Tenant isolation, antiforgery, file checks, warehouse scope, the download policy and the migration were found correct. Its findings and what was done:

| # | Finding | Fix |
|---|---|---|
| H1 | The reminder job took the 500 oldest waiting packages, including those with every reminder sent: with many undecided unclaimed packages, newer ones would never be reminded | The job only takes packages with a reminder still to send |
| H2 | Accepting an old "return" or "dispose" answer (an old tab) after the hold was released by hand could end a parcel that had moved on | An answer whose hold has ended, or whose parcel has left or been decided, is refused: "This hold has already ended" (test) |
| M3 | A return or disposal the customer chose released only the damage hold | Every hold ends with the last milestone, as for an unclaimed package |
| M4 | A package whose customer had answered (waiting for staff) still counted as unclaimed and still got reminders | Neither reminded nor unclaimed while an answer waits; the answer is reviewed first |
| M5 | Replacing a waiting answer was saved in two separate steps | One transaction; a double click shows the answer already sent |
| L6 | Reminders count from receipt, not from when the hold opened | Kept: the agreed rule (Q-R6) counts from receipt. To confirm with the owner |
| L7 | A file above the form limit gave an error page | The customer sees the size rule |

Also found while testing: the reminder job sent the next step, not the latest one due, after a pause (fixed and tested), and the sandbox policy of downloads was replaced by the site policy (section 1, "Also fixed").
