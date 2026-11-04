---
slug: credit-settings
title: Credit account settings
summary: Credit settings define the company-wide choices used when your company offers account credit.
section: setting-up
order: 160
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/credit]
related: [company-settings, invoice-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Credit/CreditSettingsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Settings/SettingsPage.razor
updated: 2026-10-25
review: ok
---

Credit settings define the company-wide choices used when {brandName} offers account credit. They are different from an individual customer’s credit limit or unpaid balance. Review the existing choices before changing them, especially the payment terms and what happens when a customer would exceed a limit.

## Steps
1. Open **Credit settings** from the navigation.
2. Read any notice that credit is off. The company’s general credit choice is reviewed in company settings.
3. Review **Terms (days)** for the default payment period.
4. Under **When a charge goes above the limit**, read each available rule and select the intended one when editing is allowed.
5. Review **Tell customers each month that their statement is ready**. Choose **Save** if the form provides that button, then read the result.

## What happens next
Reopen the page to confirm the stored choices. Saving these rules does not give every customer a credit account or change a particular customer’s limit. Review customer-specific credit separately. Readers with a read-only form can inspect the rules without changing them; the lack of a save button does not mean their review failed.