# Slice 7 step 4: Partner agents - verification record

| | |
|---|---|
| Plane | Operations and Platform (Stage 4) |
| Date | 2026-10-21 |
| Built on | Package 91 (licences under the company); not committed here: the dev team owns git |
| Design | [Slice 7 design note](SLICE-7-DESTINATION-DESIGN.md), section 2 (R-8, the owner's answers) and section 8 (S-29 to S-34, settled while building) |
| Status | **Built, reviewed independently, fixed and verified by automated tests.** Slice 7 is complete |

## 1. What was built

- **Platform:** the plan feature *Partner agents* (`partner_agents`), switched on or off in a plan's draft in Control, shown on the public plans page. Operations reads it with the branding features (kept 5 minutes); when Platform cannot answer it counts as off.
- **The warehouse** (Locations, as a destination run by a partner): *The partner takes payments here*, off by default.
- **Staff → Invite a partner agent:** name, email, language and the partner's warehouse. The staff list and the person's page show *Partner agent · warehouse*; no role can be added. Only someone who holds the partner's rights at that warehouse (and payments, where money is handled) may invite.
- **What a partner agent does:** arrivals and customs, the pickup counter and home delivery (planning and driving), at its warehouse only. The menu shows nothing else; customers, shipments, the cashier, staff and settings are refused.
- **Money:** where the partner does not handle money the counter, the delivery desk, the route and the run sheet show no amounts or invoices; billing is refused; the counter says whether something is still owed and the company settles it; cash on delivery is never used from that warehouse. Where it does, the agent takes payments at that warehouse's cashier, finding a customer only by a number of that warehouse.
- **Off:** while the plan lacks partner agents (or Platform is silent, or the warehouse is no longer run by a partner or is closed), an agent holds nothing, cannot be invited, cannot accept the invitation and cannot sign in; an open session loses every right on its next page.
- **Devices:** as any staff member, with one of the company's licences.

## 2. Database

The global script adds `Users.PartnerWarehouseId` (the warehouse, indexed) and `Warehouses.PartnerHandlesMoney` (off). Checked: on an empty database twice, and twice on a database upgraded through every earlier package to package 91 with demo data.

## 3. Independent review and fixes

| # | Finding | Fix |
|---|---|---|
| 1 | The run sheet PDF printed amounts for a partner that does not handle money | Masked as on the screen |
| 2 | Inviting a partner agent did not check that the inviter holds those rights there | Checked, as for any role |
| 3 | Accepting the invitation signed the agent in while partner access was off | Refused; the link stays usable |
| 4 | Partner agents offered as drivers while partner access was off | Offered only while open |
| 5 | A misleading comment above the partner list (it reads this company only) | Moved |

For the owner to confirm (not changed): where a partner handles money, *payments* also lets the agent bill an invoice to the customer's credit account and see that account's position; a new partner taking over a warehouse (another partner name) keeps the old agents (switching the warehouse to the company closes their access); the counter shows the sender's name and phone with the recipient's; a partner planner sees the company's drivers at that warehouse by name.

## 4. Automated tests

- `PartnerAgent_WorksOnlyAtThePartnersWarehouse_SeesNoMoney_AndOnlyWhileThePlanHasIt` (web): refused while the plan lacks it and for a company-run warehouse; invited, listed, no role added; the menu and the refused pages; receives an arrival at its warehouse; the counter with no amounts, billing refused, another warehouse not found; once the plan lacks it, nothing allowed and the sign-in refused.
- `PartnerAgent_HoldsThePartnersRights_AtItsWarehouseOnly_MoneyWhereAllowed` (application): grants ignored, its warehouse only, payments only where allowed, nothing when off, never an administrator.
- Every suite passes (architecture, Operations, Customer, Platform, Public).

## 5. Styling

New in the [UI styling inventory](UI-STYLING-INVENTORY.md): the *Invite a partner agent* panel, the partner line on the staff list and page, the money setting of a partner's warehouse, the counter's line for a partner.
