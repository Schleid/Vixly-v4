# Slice 8: Payables and profit per shipment - design note

| | |
|---|---|
| Plane | Operations |
| Date | 2026-10-22 |
| Builds on | Slice 6B (carriers, dispatch cost estimate and shares), Slice 7 step 4 (partner agents), Slice 5F (invoices) |
| Plan | The finance part left from plan slice 9 ([Operations plane](OPERATIONS-PLANE.md), section 3): receivables and payables, profit per shipment |
| Status | **Agreed with the owner on 2026-10-22** (section 1); built |

## 1. The owner's answers (2026-10-22)

- **Which bills:** carriers, partner agents and any other vendor (customs broker, landlord, supplies), each with a category.
- **Process:** one person enters a bill, another approves it (a company setting can turn approval off), then the payment made by bank, cheque or otherwise is recorded. Vixly never sends money.
- **Profit:** the dispatch's estimated cost until the carrier's bill is entered, then the bill's amount, shared the same way; destination costs and partner fees added when billed.
- **Rights:** *See costs and profit*, *Enter bills*, *Approve bills*; the Owner and Administrators hold them.

## 2. Decisions

| # | Decision |
|---|---|
| B-1 | **Bills** (`/staff/bills`): from a carrier, a partner agent (a partner-operated warehouse) or another vendor (`/staff/vendors`: name, usual category, contact, in use). Their number, a category (freight, partner fee, customs, handling and delivery, supplies bought, rent, utilities, other), bill date, due date, amount in the company's currency, note. The same vendor's number twice is refused. |
| B-2 | **Shares:** a bill is put on dispatches or shipments by typing their numbers and amounts; what is left is a general cost. A **carrier's** bill goes on **dispatches** that carrier is on (it replaces their estimate); any other bill goes on **shipments** (a partner's fee, a customs broker). The shares never exceed the bill. |
| B-3 | **Approval:** a bill waits until someone who did not enter or change it approves it, or sends it back with a reason to be corrected. A decision is refused if the bill changed since the approver opened it. With approval off (Settings, second check), a bill counts as soon as it is entered. Only approved (or paid) bills count in the costs. |
| B-4 | **Paying:** the approver records each payment made (day, amount, how, reference) up to what is owed; paid in full, the bill is *paid*. A bill entered by mistake is voided with a reason while nothing is paid. The list shows what is owed, overdue and waiting. |
| B-5 | **Dispatch cost:** the carriers' approved shares on a dispatch are its *billed* cost. Once **every carrier on its legs** has billed, the billed cost replaces the estimate (or the dispatcher's correction) and is shared on its shipments by chargeable weight, as the estimate was. Until then the estimate stays. |
| B-6 | **Profit per shipment** (`/staff/profit`, a CSV too): the shipments invoiced in a period (at most a year, 2 000 shipments). Revenue: its invoices' lines after credits, without sales tax and without duties and local taxes collected for the government. Costs: its share of each dispatch, plus the bills put on it. Marked *estimate* until every dispatch it travelled on is fully billed. The shipment's page shows the same to whoever may see costs. |
| B-7 | **Rights:** *ops.costs.view*, *ops.bills.enter*, *ops.bills.approve*, company-wide; never given to partner agents. |

## 3. Not in this slice

Bills in another currency; sending bills and payments to QuickBooks (Slice 9); a payables ageing report (Slice 11).
