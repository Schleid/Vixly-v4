# Slice 5F: Billing - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-20 |
| Built on | Slice 5E and the estimate in view (package 76); not committed here: the dev team owns git |
| Design | [Slice 5F design note](SLICE-5F-BILLING-AND-PAYMENTS-DESIGN.md), section 4 (I-1 to I-11) |
| Status | **Step 1 (the invoice), step 2a (the cashier) and step 2b (credit accounts) built, reviewed independently, fixed and verified by automated tests.** Next: the customer screen (3) and Stripe (4) |

## 1. What was built (step 1)

- **Issued by itself** (I-1): a shipment's invoice when it becomes final (whatever made it final: the counter, the station, an inspection, the customer); an order's invoice when the clerk confirms it. Numbered with the invoice prefix (INV). One invoice per shipment and per order.
- **Kept as issued** (I-2): the company's name and tax number, the issuing warehouse, the customer (code, name, company, phone, email), the shipment or order number, route and service, the note, in the customer's language. Later price, name or setting changes never alter it.
- **Lines** (I-3): the shipment's charges as priced at final (freight per parcel, items, minimum, service fee, consolidation, extras, insurance), then its supplies with their discounts.
- **Taxes** (I-4): Settings → *Invoices and taxes*: a tax name, the company's tax number, a rate per kind (freight, insurance, supplies, charges and extras), one tax line or one per kind, and a note at the foot in English, French and Spanish. Each kind is taxed on the total of its taxed lines, rounded to the cent; government fees and supplies marked not taxed are never taxed; no rate, no tax (the invoice says *No tax charged*).
- **Corrections** (I-6): an invoice is never edited. A person with *request credits* asks for a credit note (an amount before tax per line, never more than the line still holds) or a void (only while nothing is credited or paid), with a reason. One request at a time per invoice. A different person with *approve credits* approves (numbered CN, with its PDF; taxes come back in proportion, the last credit of a kind takes what is left to the cent) or refuses with a note. *Credits to approve* lists the requests.
- **A voided order** (I-7) is cancelled and its stock comes back; a confirmed order with its invoice can no longer be cancelled from the counter.
- **Where it is seen** (I-8): *Invoices* (search by invoice, shipment or order number, customer code, name, phone or email; filter to pay, credited, voided); the invoice page with lines, taxes, totals, credit notes, PDF; the counter's final shipment and confirmed order show the invoice and its PDF; the portal's new *Invoices* page lists the customer's own invoices and approved credit notes with their PDFs.
- **Rights** (I-9): *see invoices*, *request credits*, *approve credits*, each per warehouse; Owner and Administrators hold them. A counter clerk prints the invoices of the counter's warehouse.
- **The currency** can no longer change once an invoice exists; Numbering gains the credit note prefix (CN; a company already using CN gets CRN).

## 2. Database

The global script adds **Invoices** (parties as columns, lines and taxes as JSON, totals as columns; number unique per company; one per shipment and per order), **CreditNotes** (lines and taxes as JSON; number unique once approved; one request waiting per invoice; form key unique), and **TenantSettings**: `CreditNotePrefix`, `TaxName`, `TaxNumber`, `FreightTaxPercent`, `InsuranceTaxPercent`, `SuppliesTaxPercent`, `ChargesTaxPercent`, `TaxPerKind`, `InvoiceNoteEn/Fr/Es`.

Checked: the script runs on an empty database twice, and twice on a database upgraded through every earlier package to package 76 with demo data.

## 3. Independent review and fixes

An independent review read the whole step: no high finding; the money rules, the unique invoice per shipment and order, the rights per warehouse, the requester never deciding, and the portal's isolation were confirmed. Fixed:

| # | Finding | Fix |
|---|---|---|
| 1 | A request made before another credit was approved kept the taxes it was given then | Its taxes are worked out again on the invoice as it is when approved; a tax is never credited beyond what it still holds |
| 2 | A credit note's date was the UTC day, while invoices use the company's time zone | The approval day is kept in the company's time zone and printed |
| 3 | A shipment without a number yet sold its supplies before getting one | It is numbered as it becomes final, before its supplies and its invoice |
| 4 | The same form sent twice at the same instant showed an error though it worked | The request and the decision answer with what the first did; a second click with the other decision is refused |
| 5 | An approver without the right to see invoices could not open the invoice they decide on | The invoice opens for its approvers |
| 6 | The reason typed by staff is printed on the customer's credit note | The form says so |
| 7 | A company using both CN and CRN would get clashing prefixes | It gets CRNT |

Left as is: two approvals at the very same instant can leave a gap in the credit note numbers (the second is refused).

## 4. Automated tests

| Suite | Result |
|---|---|
| Operations (domain, application, infrastructure, web, API, customer web, architecture) | All pass: domain 212, application 91, infrastructure 65, web 93, API 30, customer web 40, customer infrastructure 1, architecture 11 750 |
| Platform and Public | Unchanged by this step |

New for this step:

- Domain: taxes per kind on taxed lines and rounding; a part credit with its tax in proportion and the last credit taking what is left; never more than a line holds; the requester never decides; a refusal changes nothing; a void takes the whole invoice and is refused once credited; the invoice settings and the credit note prefix.
- `Invoices_AreIssuedByThemselves_WithTaxesByKind_AndCorrectedOnlyWithAnotherPersonsApproval` (web): settings with 7% on supplies; a counter shipment with two tapes becomes final and has its invoice; found by customer code; the page and PDF; the currency locked; a credit request, a second one refused, the owner cannot decide their own, Finance approves (CN number, 4.82 credited); an order's invoice, cancel refused, void approved, the order cancelled and its stock back; a clerk prints the counter's invoice but has no invoices page.
- `Customer_SeesTheirOwnInvoices_AndCreditNotes_WithThePdfs` (API and portal): the customer's list and PDFs; another customer sees nothing and gets *not found*.

## 5. Styling

New screens are in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*: Invoices, Invoice, Credits to approve, Invoices and taxes, the invoice line at the counter, the portal's Invoices.

## 6. For the owner

- Set your tax rates under Settings → *Invoices and taxes* before the first shipment you want invoiced with tax; invoices already issued keep what they were issued with.
- Shipments made final before this package have no invoice.
- Paying an invoice comes with the cashier (step 2): until then an invoice shows *to pay*.

## 7. Step 2a: the cashier (C-1 to C-9)

### What was built

- **Settings → Payment methods** (C-1): cash (on by default), Zelle, cheque, bank transfer, and up to three methods the company names (MonCash, Natcash...). Zelle: the QR picture (PNG or JPEG of at most 1 MB, told by its bytes), the Zelle tag, the email or phone, the name shown, and the terms in English, French and Spanish. Zelle is offered only with a tag or an email or phone and its English terms. Changing who customers pay (the Zelle details or picture) asks for a fresh second factor, and the history keeps both the old and the new.
- **Cashier** (`/staff/cashier`, right *take payments*, per warehouse) (C-2): one box finds a customer by phone, email or code, or an invoice by its number or its shipment's or order's number (a scanner works); the customer's invoices to pay at the person's warehouses, with **Pay**.
- **Paying** (F-4, C-3): up to three tenders whose amounts add up to exactly what is owed; cash with the amount handed over and the change; cheque, transfer and Zelle with their reference; numbered PAY with a receipt PDF in the customer's language; the page after paying shows the change in large type. The same form sent twice takes one payment; two cashiers on one invoice at once: the second is refused.
- **Cash drawers** (C-4): each cashier opens their own drawer at a warehouse with a starting float (none when the company takes no cash); cash goes only into the person's open drawer; closing it, the count is entered and any difference (over or short) needs a note. The drawer's report: float, cash in, cash out, expected, counted, difference, each payment and refund. Finance (*confirm payments*) sees every drawer of its warehouses.
- **Zelle** (P-9, C-5): the cashier page shows a strong warning with the number to write in the memo, the company's terms in the customer's language, the box *the customer read and accepted the Zelle terms* (required), then the QR code, tag, email or phone, name and memo. A Zelle or transfer tender waits for finance; the invoice is *awaiting confirmation*.
- **To verify** (`/staff/cashier/verify`, right *confirm payments*) (C-6): Zelle and transfers waiting, with the confirmation number; *Received* (with the bank reference) settles them; *Not received* (a reason is required) takes them back: the invoice is owed again and the customer gets an email with a link to their invoices. The person who took the payment never confirms it.
- **Where an invoice stands** (C-7): unpaid, awaiting confirmation, paid, voided: on the invoice, the counter (with a Pay button), the shipments desk, and the portal. Bundling will use it.
- **Refunds** (F-7, C-8): after a credit note on a paid invoice, a person with *refund payments* gives the money back (cash from their open drawer, never more than it holds; or a method the company uses, with its reference), with a reason. A paid or pending invoice is never voided (C-9).
- **Rights:** *take payments*, *confirm payments*, *refund payments*, each per warehouse.

### Database

**Payments** (tenders as JSON, `Pending` as a column for the list to verify; number and form key unique), **CashDrawers** (one open drawer per person and warehouse), **Refunds**; **Invoices** gains `PendingTotal` and `RefundedTotal`; **TenantSettings** gains the methods and the Zelle details (cash on for existing companies).

### Independent review and fixes

| # | Finding | Fix |
|---|---|---|
| 1 | A cashier holding both rights could confirm their own Zelle | The one who took a payment never confirms it |
| 2 | The Zelle payee could change without a second factor, and the history lost the old details | A fresh second factor for any change of the Zelle details or picture; both sides kept, with the picture's fingerprint |
| 3 | Zelle could not be turned off while clearing its details in one save | The details are checked against the new choice |
| 4 | The totals did not reconcile once paid, pending or refunded | Paid, awaiting confirmation and given back lines; owed never below zero; *to give back* on its own line (screen and PDF) |
| 5 | A Zelle still waiting showed *Settled* in the list | The state comes from what is paid and pending |
| 6 | A new QR picture stayed in the store when the save failed | It is removed |
| 7 | Refunds offered every method and could take more cash than the drawer held | Only the company's methods; never more cash than the drawer holds |
| 8 | The cashier page could miss the person's open drawer at a busy site | It reads the person's open drawers directly |
| 9 | Four tenders in the rules, three on the form | Three |
| 10 | A drawer could open when the company takes no cash | Refused |

### Tests

All suites pass: domain 217, application 91, infrastructure 65, web 94, API 30, customer web 40, customer infrastructure 1, architecture 11 965. The global script runs twice on an empty database and twice on a database upgraded through every package to 79 with demo data.

New: the payment rules in the domain (in full, cash and change into the person's drawer, Zelle received and not received, drawer differences, refunds within limits, no void once paid), and `Cashier_TakesPaymentsInFull_ZelleWaitsForFinance_AndDrawersBalance` (web): the payment settings with the QR picture; cash refused without a drawer; a drawer opened; cash with change, sent twice taken once, the receipt; the desk says paid; a credit note approved by finance and given back from the drawer; an order paid by Zelle, the terms required, the payer unable to confirm it, finance marks it not received (an email is queued), paid again with MonCash; the drawer closed one dollar short with its note.

## 8. Step 2b: credit accounts (K-1 to K-9)

### What was built

- **Credit accounts** (`/staff/credit`, the right *manage credit accounts*, formerly *allow customer credit*): open an account for a customer by code with a limit and terms (empty: the company's); the list with each account's state, limit, owed, available and overdue; customers marked for credit before accounts existed, to open. Opening and changing ask for the second check. Hold and close need a reason; close only once nothing is owed or waiting. The old credit toggle on the customer record is replaced by a link to the account.
- **Settings → Credit:** terms (30 days), above the limit *refuse* (default) or *ask the credit department*, the monthly statement notice (on).
- **Bill the account** at the cashier: the whole amount owed, within what is available, due after the terms; above it, refused or waiting. A charged invoice reads *on account*, releases its shipment for bundling, and stays owed until paid at the cashier (paid in full by Zelle, it stays released while finance checks).
- **Charges to approve:** approve, or refuse with a note; never by the person who asked; both with the second check. A charge paid meanwhile leaves the queue.
- **The account page:** state and reason, limit and terms, owed, awaiting, available, overdue and the five ages (current, 1-30, 31-60, 61-90, over 90 days), the open items with their due dates, the statement PDF in the customer's language.
- **Monthly notice:** from the 1st, each account with something owed gets one email a month that its statement is ready, linking to the portal; an account opened during a month gets its first notice the next month.
- **Portal:** the invoices page shows *Your credit account* (state, limit and terms, owed, available, overdue, open items) and the statement PDF; the state chips *on your account* and *awaiting approval*.

### Database

**CreditAccounts** (one per customer; limit, terms, state, reason, month of the last notice); on **Invoices** `OnAccount`, `DueOn`, who asked and who decided (indexed by account state and customer); on **TenantSettings** `CreditTermsDays` (30), `CreditOverLimit` (refuse), `CreditStatements` (on). Checked on an empty database twice and on a database upgraded through package 81 twice.

### Independent review and fixes

No high finding left after the fixes; warehouse scope of billing, the credit right and the second check, portal isolation, the money arithmetic, double submits and the migration defaults were confirmed. Fixed:

| # | Finding | Fix |
|---|---|---|
| 1 | Open items were read with a global cap, so long-standing customers or many accounts could show wrong owed and available figures and pass the limit | Only invoices still owed are read, worked out in SQL, with no cap |
| 2 | A charged invoice paid in full by Zelle was held again until finance confirmed | It stays released (*on account*) while the Zelle payment is checked |
| 3 | Only the newest 500 accounts were ever considered for the monthly notice | The accounts due are read in batches until all are marked |
| 4 | Refusing a charge had no note, and decisions skipped the second check | A note is required to refuse and kept in the audit; deciding asks for the second check |
| 5 | A charge paid at the cashier while waiting stayed in the queue | Such charges leave the queue and cannot be approved |
| 6 | The old credit toggle could still be posted; customers marked for credit had no account | The toggle is removed; those customers are listed for the credit department |
| 7 | A notice could be sent again for an earlier month; a new account got one at once | Months only move forward; a new account's first notice is the next month |
| 8 | The portal's overdue mark used the UTC day | It uses the company's day, like the totals |

Left as is: two cashiers billing the same account at the same instant can together pass the limit (each is checked alone); the credit department sees it on the account.

### Tests

All suites pass: domain 229, application 91, infrastructure 65, web 96, API 30, customer web 40, customer infrastructure 1, architecture 12 412.

New: the account rules (limit, terms, hold and close), the charge (released, still owed, paid later, paid by Zelle), above the limit decided by another person or refused, paid while waiting, the position and its ages, the credit settings (domain); `Credit_TheCashierBillsTheAccount_AboveTheLimitTheCreditDepartmentDecides` (web: settings, an account opened twice is one, billed and released with its due date, the second above the limit waits, the requester cannot decide, a refusal needs a note, another person approves, the statement, paid at the cashier, a hold needs a reason, a close needs nothing owed, the billing panel on hold, the monthly notice once a month); the portal test now shows the customer's credit account and statement, never to another customer.

### Styling

New screens in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*: Credit accounts, Credit account, Charges to approve, Credit settings; the cashier invoice, the customer record and the portal invoices noted.

## 9. Step 4: Stripe cards (S-1 to S-8)

### What was built

- **Settings → Payments → Cards** (`#cards`): the company's Stripe keys (secret or restricted, publishable, webhook signing secret), checked with Stripe, kept protected and shown only by their last characters; the webhook address to add in Stripe; *Accept cards*; readers named per warehouse; *Disconnect*. Connecting and disconnecting ask for the second check.
- **At the cashier:** *Card: payment page (QR code)* and *Card on <reader>*; the card payment page (`/staff/cashier/card/{id}`) with the QR code or the reader's state, refreshing itself until Stripe says paid; *Cancel*.
- **On the portal:** *Pay by card* on an invoice owed, Stripe's page, back to the portal's card page that says paid once Stripe confirms.
- **The webhook** (`POST /hooks/stripe` on the company's own address): signature checked with that company's secret, stale events refused, rate limited per host, 64 KB at most; a payment recorded once, as a card tender with Stripe's reference.
- **Refunds by card** through Stripe, pending until Stripe confirms, with *Finish the refund* when Stripe did not answer.

### Database

**CardProviders** (one per company: protected keys, key hint, live or test, account name), **CardReaders** (warehouse, label, Stripe id), **CardCharges** (invoice, channel, amount, Stripe page and payment, live or test, state; one started charge per invoice by a filtered unique index); **Refunds** gain `Pending`. One migration, `CardPayments`.

### Independent review and fixes

Two passes. Fixed:

| # | Finding | Fix |
|---|---|---|
| 1 | A cancelled charge could still be paid on Stripe's page and never be recorded | Cancel expires the page or cancels the payment at Stripe first; already paid there, the payment is recorded; a late payment on a cancelled charge is still recorded |
| 2 | A card refund made at Stripe could be lost if the save after it failed, or made twice | Kept as *pending* before Stripe is asked, Stripe's idempotency key is the refund; refused, it is removed; unanswered or not saved, *Finish the refund* asks Stripe again with the same key |
| 3 | The webhook answered 200 when recording failed, so Stripe stopped retrying | It answers 400 and Stripe retries |
| 4 | Disconnecting or changing the account left open charges payable on an account no longer read | Open charges are closed at Stripe first, or the change is refused (*a card payment could not be closed*) |
| 5 | Test and live payments were not told apart | Each charge keeps its mode; a payment from the other mode is audited and never applied |
| 6 | Cancel could close a charge locally while Stripe was still processing it | Closed only when Stripe confirms it paid or over |
| 7 | Whole-unit currencies (JPY and others) could be charged or refunded with cents | Refused |
| 8 | A partner agent could refund, see or cancel card payments where the partner does not handle money | Refused |
| 9 | Stripe refunds were looked up without tracking, so a pending one could not be settled | Tracked |

Left as is (for the owner): a payment arriving for an invoice already settled another way is kept as *paid, not applied* and refunded in Stripe's dashboard; a charge started in the same instant as a disconnect could stay open on the old account; the restricted key's permissions and the webhook secret are not tested with Stripe when pasted (a wrong secret shows as payments not confirmed); the return address is the company's own host.

### Tests

All suites pass: domain 256, application 97, infrastructure 69, web 111, API 33, customer web 40, customer infrastructure 1, architecture 13 693, platform domain 308, application 236, infrastructure 63, web 43, API 21, public web 23.

New: `OperationsStripeGatewayTests` (signature and age, amounts in the smallest unit, the idempotency key, refusals and outages told without the key); `Cards_ConnectStripe_PayOnThePageOrAReader_OnlyOnStripesWord` (web: keys refused then kept and never shown, a reader, the QR page, the webhook refused unsigned or wrongly signed and recorded once, a reader payment found by asking Stripe, a card refund with Stripe silent then finished once, a page paid before its cancel recorded as paid, disconnecting closes an open page, cards turned off); `Customer_PaysTheirInvoiceByCard_OnStripesPage_NeverAnothersInvoice` (API); the pending refund rules (domain).

### Styling

New in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*: the card payment page (staff) and the portal card page; *Changed*: the payment settings (card panel) and the cashier invoice (card panel, pending refund).

## 10. Step 3: the customer screen (T-1 to T-7)

### What was built

- **Customer screens** (`/staff/screens`, linked from the cashier): add a screen to a warehouse with a name and get its one-use code (10 characters, 15 minutes); the list with each screen's state (waiting for its code, paired and last seen) and what it shows; *New code* (the old tablet stops at once); *Remove*.
- **The screen** (`/screen` on the company's own address): the code typed once, then a secret in a host-only cookie for 400 days; no sign-in, no licence. It shows welcome, the invoice with *Pay by card* and *Pay with Zelle* (never cash), the Zelle warning and terms, then the Zelle details and QR picture, Stripe's page as a QR code and a link, the declaration to sign, and thank you, in the customer's language. It reloads itself when what it shows changes.
- **The clerk's side:** *Send to pay* on the cashier invoice and *Send to sign* on the counter shipment, one per screen of that warehouse, with what the screen shows now and *Clear · next customer*; the Zelle acceptance shown while the screen is on that invoice.
- **At the cashier:** a Zelle payment needs no tick from the clerk when the customer accepted the same terms on a screen for that invoice in the last 4 hours.
- **The signature** on the screen is the shipment's own (Slice 5D): on the declaration PDF, with the clerk who sent it as the one present.

### Database

**CustomerScreens** (warehouse, name, secret and pairing code as hashes with filtered unique indexes, code expiry, what it shows, who sent it, when last seen) and **ScreenRecords** (the customer's Zelle acceptances: invoice, screen, clerk, language, a hash of the terms read). One migration, `CustomerScreens`.

### Independent review and fixes

No high finding left. Fixed:

| # | Finding | Fix |
|---|---|---|
| 1 | A declaration changed after it was sent could be signed from the page shown before | The screen's version includes the shipment; a signature from an older page is refused and the screen reloads |
| 2 | *Pay by card* on the screen cancelled a card-reader or portal payment already started | Refused while one is started; only a payment page for the same amount is shown again |
| 3 | A Zelle acceptance counted for that invoice forever, whatever the terms became | It counts for 4 hours and only for the terms the customer read |
| 4 | Waiting for a card, every poll asked Stripe, and its save was tied to the screen's own | Stripe is asked when the page opens and about once a minute; the screen is saved apart |
| 5 | The last customer's invoice or declaration stayed on the screen for the next one | Paid: thank you, then clear after a minute; anything sent clears after 20 minutes |
| 6 | Paying on one's own phone from the QR code ended on the pairing form | A thank-you page |
| 7 | An invoice with nothing owed or voided could be sent | Refused |
| 8 | Some refusals showed no message, or in the browser's language | Allowed and written in the customer's language; *es-MX* and the like read as *es* |
| 9 | The signature size differed between the address and the rule | One limit (200 KB) |

Left as is (for the owner): rights are checked when the clerk sends, not again while the customer acts (bounded by the 20 minutes); after *Back* from the card, Stripe's page stays payable from the customer's phone (a payment then is recorded, or refunded if the invoice was paid another way); the guidance tells the company to pair the tablet in a browser where no one is signed in to the staff site.

### Tests

All suites pass: domain 259, application 97, infrastructure 69, web 112, API 33, customer web 40, customer infrastructure 1, architecture 13 823, platform domain 308, application 236, infrastructure 63, web 43, API 21, public web 23.

New: the screen rules (pairing once with an expiring code, a new code stops the old screen, steps only from where they are offered, terms before the Zelle details, tidying itself) (domain); `CustomerScreen_PairedByCode_PaysAndSigns_OnlyWhatTheClerkSent` (web: the code shown once and kept as a hash, a wrong code and a used code refused, an unpaired browser gets nothing, the invoice sent and shown with card and Zelle and never cash, the version, an old choice refused, the QR picture only after the terms, the cashier's Zelle payment counting the customer's acceptance, the screen saying paid, a second invoice paid by card on Stripe's page with the screen's return address, the thank-you on a phone, the declaration signed, a signature from a page shown before a change refused, clear and remove).

### Styling

New in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*: Customer screens (staff), the screen send panel, and the customer screen pages (pairing, welcome, invoice, Zelle terms, Zelle, card, declaration, thank you).
