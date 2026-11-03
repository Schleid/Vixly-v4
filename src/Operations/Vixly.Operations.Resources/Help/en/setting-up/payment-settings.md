---
slug: payment-settings
title: Payment methods: cards, Zelle and transfers
summary: Payment settings describe the ways your company accepts money.
section: setting-up
order: 150
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/payments]
related: [invoice-settings, credit-settings, card-payments-stripe]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Cashier/PaymentSettingsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Cashier/CardSettingsPanel.razor
updated: 2026-10-25
review: ok
---

Payment settings describe the ways {brandName} accepts money. Review the actual company account details before enabling a method. These settings prepare payment choices; selecting a method here does not confirm that a customer has sent money or that an invoice is paid.

## Steps
1. Open **Payment methods** and review **Methods the company accepts**. Select the accepted methods only where the page allows editing.
2. Review any additional method names so staff can identify them consistently.
3. For Zelle, check the displayed recipient details, QR image and instructions in each language. Replace information only with the company’s intended payment details.
4. Use **Save** when shown and read the result.
5. Review **Cards (Stripe)** separately. If card setup controls are available, follow the connection instructions for the company’s own account and complete any requested identity confirmation.

## What happens next
Reopen the page and verify the saved methods and recipient information. Card connection, card acceptance and warehouse readers are separate choices within the card panel. Keep connection credentials private. Turning on a method does not collect money; staff must still follow the appropriate invoice or cashier workflow and check its payment result.