# Slice 5F: Billing - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-20 |
| Built on | Slice 5E and the estimate in view (package 76); not committed here: the dev team owns git |
| Design | [Slice 5F design note](SLICE-5F-BILLING-AND-PAYMENTS-DESIGN.md), section 4 (I-1 to I-11) |
| Status | **Step 1 (the invoice) built and verified by automated tests; reviewed independently and fixed (section 3).** Step 2 (the cashier) waits for the owner's answers on credit accounts (F-10) |

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
