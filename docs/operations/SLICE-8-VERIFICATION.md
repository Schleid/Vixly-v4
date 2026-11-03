# Slice 8: Payables and profit per shipment - verification

| | |
|---|---|
| Date | 2026-10-22 |
| Design | [Slice 8 design](SLICE-8-PAYABLES-AND-PROFIT-DESIGN.md) |

## 1. What was built

- **Bills** (`/staff/bills`, new menu item): totals owed, overdue and waiting for approval (worked out in SQL over every bill); filter by state; the list. *New bill*: from a carrier, a partner agent or a vendor, their number, category, dates, amount, note, and up to ten shares by dispatch or shipment number.
- **One bill:** its facts, shares (the rest shown as general cost), payments; *Approve* / *Send back* for an approver who did not enter or change it, refused if the bill changed since it was opened; *Record a payment made*; *Void* with a reason while nothing is paid; corrected while waiting or sent back.
- **Vendors** (`/staff/vendors`): add, rename, category, contact, in use.
- **Approval setting** on the bills page for whoever edits settings (second check): bills need someone else's approval, on by default.
- **Dispatch:** *Billed by the carriers* on the dispatch's cost panel; once every carrier on its legs has billed, that cost replaces the estimate in its shipments' shares.
- **Profit per shipment** (`/staff/profit`, new menu item, and `/staff/profit/export` as CSV): the shipments invoiced in a period, revenue, carrier cost, other costs, profit and margin, *estimate* marked, totals. The shipment's page shows the same panel to whoever may see costs.
- **Rights:** *See costs and profit*, *Enter bills and vendors*, *Approve bills and record what was paid*, in the role editor.

## 2. Database

**Vendors**; **Bills** (party, their number, category, dates, amount, currency, state, who entered and changed it, decision, reasons, paid total, payments as JSON); **BillAllocations** (bill, dispatch or shipment, amount; indexed by dispatch and shipment); on **Dispatches** `BilledTotal` and `BilledComplete`; on **TenantSettings** `BillsNeedApproval` (on for existing companies). One migration, `Payables`.

## 3. Independent review and fixes

| # | Finding | Fix |
|---|---|---|
| 1 | Any bill's share on a dispatch (a broker's) replaced the carrier's estimate | Only carriers' bills go on dispatches, and only on dispatches that carrier is on; other bills go on shipments |
| 2 | One carrier's bill replaced the whole estimate of a dispatch with several carriers | The billed cost replaces the estimate only once every carrier on its legs has billed; *estimate* stays marked until then |
| 3 | An approver could approve figures changed after they opened the bill | The decision carries the version read; a changed bill is refused |
| 4 | The two-person rule followed only the last person who changed the bill | Everyone who entered or changed it is kept; none of them may approve or send it back |
| 5 | The totals counted only the newest 500 bills | Worked out in SQL over every bill |
| 6 | The profit list kept arbitrary shipments past 2 000 and counted voided invoices | The newest are kept; voided invoices are left out |
| 7 | Missing or odd dates were accepted; a payment could be dated in the future | Dates checked (bills from 2000; payments not after today) |
| 8 | Correcting a bill whose vendor was turned off changed its vendor silently | The bill's own vendor stays a choice |
| 9 | A void's reason overwrote the rejection's | Kept apart |
| 10 | CSV cells starting with a tab or return were not guarded | Guarded |

Left as is (for the owner): the dispatch's billed cost is shown on the dispatch page to whoever manages dispatches (as the estimate already was); two people entering the same vendor number at the same instant could both save it (checked by the application, not by a unique index); bills are in the company's currency only.

## 4. Tests

All suites pass: architecture 13 989, domain 261, application 97, infrastructure 69, web 112, API 33, customer web 40, customer infrastructure 1, platform domain 308, application 236, infrastructure 63, web 43, API 21, public web 23. The global script runs twice on an empty database and on a database upgraded through every earlier package.

New: `OperationsBillDomainTests` (approved by another, sent back with a reason and corrected, shares never above the bill, paid up to its amount, void only while nothing is paid); `Dispatch_ManifestsBundles_DepartsAndArrives_AndTheParcelFollows` extended (web: a carrier's bill with a share on the dispatch, an unknown number and a repeated number refused, a decision on a changed bill refused, approved by another person, the dispatch's shipment then carries the billed cost instead of the estimate, paid up to its amount, a broker's bill refused on a dispatch and put on the shipment, the profit counting both and no longer estimated, the profit page and CSV, a void taking the broker's cost off).

## 5. Styling

New in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*: Bills, Bill, Vendors, Profit per shipment; the shipment page's costs panel; the dispatch cost panel's billed line.
