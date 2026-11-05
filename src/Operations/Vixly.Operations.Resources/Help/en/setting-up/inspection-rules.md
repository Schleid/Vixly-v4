---
slug: inspection-rules
title: Inspection rules and restricted items
summary: Inspection setup defines the checks and restricted-item guidance used by your company.
section: setting-up
order: 130
permissions: [ops.inspection.configure]
conditions: []
routes: [/staff/inspection/setup]
related: [shipping-services, branches-and-warehouses]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Inspection/InspectionSetupPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Inspection/RestrictedItemFields.razor
updated: 2026-11-04
review: ok
---

Inspection setup defines the checks and restricted-item guidance used by {brandName}. Review these choices with the people responsible for receiving and compliance. A starting list is a starting point for your company’s review, not confirmation that every destination and carrier requirement has been covered.

## Steps
1. In the Settings part of the menu, open **Inspection settings** and read the explanation at the top.
2. Review **Random share**. Enter the company’s chosen share in **Parcels in a hundred** and choose **Save**.
3. Under **Rules**, choose the rule kind and value, then select **Add the rule**. Review existing rules before changing their active choice.
4. Review **Restricted items**. If a starting list is offered, use **Start from the suggested list** only after reading its explanation.
5. Review each item’s name, matching words, restriction, destination and carrier information before saving.

## What happens next
Check the resulting rules and item entries. These settings guide inspection work; they do not inspect a package, resolve an existing hold or certify that goods may travel. Staff must still record the actual package check and follow the company’s instructions for restricted goods.