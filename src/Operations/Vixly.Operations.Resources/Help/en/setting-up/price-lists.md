---
slug: price-lists
title: Price lists and versions
summary: Price lists give your company the freight prices for a service, route and, optionally, a particular customer.
section: setting-up
order: 80
permissions: [ops.charges.configure]
conditions: []
routes: [/staff/charges]
related: [zones-and-routes, extras-and-item-types, publish-prices]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/PriceListPanel.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/PriceVersionFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/VersionHistory.razor
updated: 2026-10-25
review: ok
---

Price lists give {brandName} the freight prices for a service, route and, optionally, a particular customer. Each list keeps dated versions. Check the company’s currency and units before entering amounts, and choose the right transport tab before opening a list.

## Steps
1. Open **Prices** and choose the transport mode.
2. Open an existing list, or choose **Add a price list** and select **Service**.
3. Review the origin and destination. Enter **Customer code (empty: standard prices)** only for a customer-specific list.
4. Enter the applicable bands, item prices or ground charges. Review minimum, service and consolidation fees.
5. Set **Starts on**, review **In use**, and choose **Save** for an existing list or **Add a price list** for a new one.

## What happens next
Check the version history after saving. A version that has started is read-only; create a new dated version for later prices. A scheduled version can be removed before it starts. Confirm the effective date before using the list in a quote. Publishing prices is a separate choice from saving them.