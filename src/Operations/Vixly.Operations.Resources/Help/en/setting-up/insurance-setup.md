---
slug: insurance-setup
title: Insurance products and documents
summary: The insurance page brings together the policies, cover assignments and liability wording used by your company.
section: setting-up
order: 120
permissions: [ops.insurance.view]
conditions: []
routes: [/staff/insurance/]
related: [price-lists, invoice-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Insurance/InsurancePage.razor
  - src/Operations/Vixly.Operations.Web/Components/Insurance/PolicyPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/InsuranceProductsPanel.razor
updated: 2026-10-25
review: ok
---

The insurance page brings together the policies, cover assignments and liability wording used by {brandName}. Customer-facing insurance products and their premiums are configured on the charges page. Review both places before describing a product to a customer; a policy record alone is not a priced offer.

## Steps
1. Open **Insurance** and review **The company’s policies**. Open a policy to inspect its details and documents.
2. If editing controls are available, use **Add a policy** to record a policy and its supporting information.
3. Review **Cover sold to customers** and the policy selected under **Sits under**. Save changes only where the page allows them.
4. Review **Staff may change the insurance of an open shipment, when the customer accepts the terms** and the liability wording for each mode.
5. Use **Open the prices** to review the customer products and premiums on the charges page when that task is available.

## What happens next
Check policy dates, cover assignments and customer wording together. Uploading or recording documents does not itself sell insurance or add a premium to a shipment. Keep the company’s records accurate and use the product actually selected for the customer’s shipment when checking its insurance result.