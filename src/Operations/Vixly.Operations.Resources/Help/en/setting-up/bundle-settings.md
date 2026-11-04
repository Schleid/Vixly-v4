---
slug: bundle-settings
title: Bundle settings
summary: Bundle settings define the choices your company uses when preparing grouped freight.
section: setting-up
order: 170
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/bundles]
related: [company-settings, shipping-services]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Bundles/BundleSettingsPage.razor
updated: 2026-10-25
review: ok
---

Bundle settings define the choices {brandName} uses when preparing grouped freight. Review the names and working method with the team that packs and dispatches goods. These are company defaults, not an instruction to load a particular shipment or close an existing bundle.

## Steps
1. Open **Bundle settings** and review the current form before editing.
2. Check **Bundle types**. Keep the entries aligned with the physical bundle types your company uses and follow the field’s displayed format.
3. Under **When a bundle reaches its target**, read the available target rules and select the intended one if editing is available.
4. Under **Consolidated shipments**, review each method and its explanatory hint before choosing the company’s method.
5. Choose **Save** when shown and check the result before leaving.

## What happens next
Reopen the page to confirm the recorded values. Changing these settings does not place packages into a bundle, repack them into boxes or dispatch freight. Those steps happen in their operational screens. If you are reviewing a read-only form, record the current choices for your work without expecting the page to save a change.