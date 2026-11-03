---
slug: plan-shipments
title: Your plan's shipments each month
summary: Some plans include a number of shipments a month. See where you stand, what extra shipments cost, and when new shipments stop.
section: setting-up
order: 106
permissions: [ops.settings.edit]
conditions: []
routes: [/staff/billing]
related: [company-settings]
sources:
  - src/Operations/Vixly.Operations.Application/Plans/OperationsShipmentCap.cs
  - src/Operations/Vixly.Operations.Web/Components/SignIn/WorkspacePage.razor
updated: 2026-10-30
review: ok
---

Some plans include a number of shipments each calendar month, counted in your company's time zone. Every shipment counts: those created at the counter, by customers in the portal, and for packages that arrive at your warehouse. Cancelled shipments do not count.

## What happens through the month
1. **At 80%** of your plan's shipments, the workspace shows a notice and the Owner gets an email.
2. **Above 100%**, work goes on. Each extra shipment is charged at your plan's price per extra shipment, on your next invoice.
3. **At 125%**, a last warning: new shipments stop at 150% unless you change your plan.
4. **Above 150%**, new shipments at the counter and in the portal stop until you change your plan or the month ends. Receiving, payments, dispatch and releases keep working, and packages that arrive still get their shipment.

## The grace month
The first time your company goes above 150%, new shipments are not stopped that month. Extra shipments are still charged. This happens only once.

## Good to know
- Only the people who manage the company's settings see the notice in the workspace.
- Customers who try to request a shipment in the portal while new shipments are stopped are asked to contact you; they are not told about your plan.
- A new month starts the count again.
- Your plan is shown on **Billing**. To raise your limit, ask for a larger plan.
