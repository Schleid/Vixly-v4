---
slug: pickup-settings
title: Pickup settings
summary: Pickup settings define how your company records handover at the destination counter and handles pickup-related charges.
section: setting-up
order: 190
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/pickup]
related: [delivery-settings, event-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Pickup/PickupSettingsPage.razor
updated: 2026-10-25
review: ok
---

Pickup settings define how {brandName} records handover at the destination counter and handles pickup-related charges. Review the company’s proof requirements before changing the form. The settings describe the company’s working rules; they do not hand over a package or confirm a customer’s identity on their own.

## Steps
1. Open **Pickup settings** and review **Proof asked at the counter**. Read the explanatory hint before selecting proof choices where editing is available.
2. Review **Bill destination charges (duties, local taxes and fees)** and the explanation beside that choice.
3. Under **Storage at the destination**, check whether destination storage charging is enabled.
4. Review **Free days** and the storage amount shown on the form. Use the company’s displayed currency rather than assuming a fixed amount.
5. Choose **Save** if available and read the result.

## What happens next
Reopen the page and check the saved rules before using them at the counter. Free days and storage charges are separate from the proof collected during handover. Changing this configuration does not itself release a shipment, collect an outstanding balance or create proof that a customer received their goods.