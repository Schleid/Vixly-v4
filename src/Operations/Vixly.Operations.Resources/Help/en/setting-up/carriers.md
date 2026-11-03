---
slug: carriers
title: Carriers and their rates
summary: Carrier records describe the transport companies used by your company and their destination rates.
section: setting-up
order: 220
permissions: [ops.carriers.manage]
conditions: []
routes: [/staff/carriers, /staff/carriers/{carrierId:guid}]
related: [shipping-services, price-lists]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Dispatch/CarriersPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Dispatch/CarrierPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Dispatch/CarrierForm.razor
updated: 2026-10-25
review: ok
---

Carrier records describe the transport companies used by {brandName} and their destination rates. These are carrier cost records, separate from the prices charged to customers. Check the carrier’s kind before entering rates because the air and ocean forms use different charging choices.

## Steps
1. Open **Carriers** and review the existing list before creating a duplicate.
2. Use **New carrier** for a new carrier, or open an existing carrier’s name.
3. Review **Name**, **Kind**, **Account number** and **Airline prefix**. Read the prefix hint and select the intended active status.
4. Choose **Save** and open the saved carrier.
5. Under **Rates**, review the destination, start date and amounts for the relevant transport kind, then choose **Add a rate** when adding a rate.

## What happens next
Check the rate’s destination and start date after saving. A later rate replaces an earlier one from its start date. Future rates can be removed before they start. Saving a carrier or cost rate does not publish customer freight prices or create a departure; configure those through their own workflows.