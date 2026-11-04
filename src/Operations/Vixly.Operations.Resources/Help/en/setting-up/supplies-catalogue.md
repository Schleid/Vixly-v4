---
slug: supplies-catalogue
title: Supplies catalogue and prices
summary: The supplies catalogue holds the goods and packing materials sold by your company.
section: setting-up
order: 210
permissions: [ops.charges.configure]
conditions: []
routes: [/staff/supplies, /staff/supplies/{supplyId:guid}]
related: [invoice-settings, price-lists]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SuppliesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SupplyPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SupplyDetailsFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SupplyPriceFields.razor
updated: 2026-10-25
review: ok
---

The supplies catalogue holds the goods and packing materials sold by {brandName}. Review an existing entry before adding another, especially its code, barcodes and current price. Product details and dated prices are separate, so changing a name does not replace the price history.

## Steps
1. Open the supplies page and use **Code, name or barcode** or **Category** to find the item.
2. To add one, open **New supply**. Enter the code, category, unit and localized names; review tax and barcode choices.
3. Complete the initial price fields and select **Add the supply**.
4. To change an existing item, open its code, edit **Details and barcodes** and choose **Save**.
5. Under **A new price**, review the effective date, quantity breaks and warehouse prices. Select **Save the price** to save that price.

## What happens next
Check the saved item and its current or scheduled price. Future prices are not immediately current. Taking an item out of use is different from changing its price. Adding a catalogue entry or printing its labels does not receive physical stock or record a sale; those are separate tasks.