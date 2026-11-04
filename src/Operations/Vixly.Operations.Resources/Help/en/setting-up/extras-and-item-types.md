---
slug: extras-and-item-types
title: Extras and item types
summary: Extras add a charge when its selected circumstances apply to work for your company.
section: setting-up
order: 100
permissions: [ops.charges.configure]
conditions: []
routes: []
related: [price-lists, zones-and-routes]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Charges/ExtrasPanel.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargeExtraFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ItemTypesPanel.razor
updated: 2026-10-25
review: ok
---

Extras add a charge when its selected circumstances apply to work for {brandName}. Item types describe goods with their own ocean prices. Check the transport tab before editing, and distinguish a charge’s name and matching rules from the amount that takes effect on a particular date.

## Steps
1. On the charges page, open **Add an extra charge** or expand an existing extra.
2. Enter **Name shown on the line**, **Amount or percentage** and the calculation choices.
3. Review **Applies**, matching words, service, country, route and delivery restrictions. Select the government-fee choice only for that kind of charge.
4. Set **Starts on** and save the extra.
5. For ocean goods, review **Ocean item types**. Enter the item name and matching words, then use **Add the item type** or **Save**.

## What happens next
Review the saved entry and any scheduled amount. Taking an extra or item type out of use keeps its record rather than deleting it. Creating an item type does not supply its price: review the matching ocean price list separately. Test a representative quote before relying on changed rules for customer work.