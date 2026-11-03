---
slug: publish-prices
title: Show prices on the website and portal
summary: Public pricing lets your company decide which standard freight prices may be shown to visitors and customers.
section: setting-up
order: 110
permissions: [ops.charges.configure]
conditions: []
routes: []
related: [price-lists, company-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Settings/SettingsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/PriceListPanel.razor
updated: 2026-10-25
review: ok
---

Public pricing lets {brandName} decide which standard freight prices may be shown to visitors and customers. Saving a list and publishing it are separate choices. Review the service, route and effective prices before making a list public; a customer-specific agreement is not a public standard list.

## Steps
1. Open the charges page and select the intended transport mode.
2. Open the standard price list and check its service, route, current version and active status.
3. Review **Publish on the public website**. This choice is shown for standard lists, not customer-specific lists.
4. Select the publication choice and save the list using **Save**, reviewing the version fields and date on that form.
5. Check whether public pricing is enabled in company settings. If that page is read-only for you, have the person responsible for company settings review that choice.

## What happens next
Check the customer-facing prices after both settings are correct. A public list does not guarantee every shipment costs the displayed freight amount: applicable extras and shipment details still matter. Turning publication off is separate from taking a price list out of use for staff pricing.