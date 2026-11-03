---
slug: zones-and-routes
title: Choose the route for a price
summary: Route choices limit where a price list or extra applies for your company.
section: setting-up
order: 90
permissions: [ops.charges.configure]
conditions: []
routes: []
related: [price-lists, branches-and-warehouses]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Charges/RouteFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargeExtraFields.razor
updated: 2026-10-25
review: ok
---

Route choices limit where a price list or extra applies for {brandName}. On these forms, you select an origin and destination warehouse rather than drawing a delivery area on a map. Read the route choices before saving: leaving a choice broad can make the price apply beyond one intended journey.

## Steps
1. Open the price list or extra you are configuring from the charges page.
2. Find **From (drop-off)** and choose the intended origin warehouse.
3. Use **Any origin** only when the price is meant for every origin covered by that choice.
4. Select **To**, or **Any destination** when the rule should not be limited to one destination.
5. Review the service, other restrictions and effective date, then save that form.

## What happens next
Reopen the saved list or extra and confirm its route. A previously selected warehouse that is no longer offered can remain selected; read the retained-choice notice instead of assuming the route became unrestricted. These settings define pricing coverage. They do not create a dispatch, move a package or assign a driver’s delivery stops.