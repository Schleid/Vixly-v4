---
slug: invoice-settings
title: Invoice settings
summary: Invoice settings control the tax presentation and footer notes used by your company.
section: setting-up
order: 140
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/invoices]
related: [company-settings, payment-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Invoices/InvoiceSettingsPage.razor
updated: 2026-10-25
review: ok
---

Invoice settings control the tax presentation and footer notes used by {brandName}. Review the company’s approved wording and tax choices before editing. The page can be available for review without editable controls; only save when the form gives you that option.

## Steps
1. Open **Invoices and taxes** and review **Tax**.
2. Check **Name of the tax on invoices** and **Company tax number** against the company’s records.
3. Review each displayed tax rate and read the rate hint. A zero rate means no tax for that kind; government fees and supplies marked not taxed are excluded.
4. Under **Taxes on the invoice**, choose **One tax line** or **One tax line per kind** when editing is available.
5. Review the English, French and Spanish notes under **Note at the foot of every invoice**. Choose **Save** if shown.

## What happens next
Read the saved result and reopen the page to confirm the choices. The note is intended for invoice footers, so check all languages rather than placing every translation in one field. Invoice numbering belongs to company settings. Saving this form does not itself issue an invoice or record a payment.