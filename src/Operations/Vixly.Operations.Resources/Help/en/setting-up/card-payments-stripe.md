---
slug: card-payments-stripe
title: Connect your Stripe account
summary: Card payments go to your company's own Stripe account; you connect it with a restricted key.
section: setting-up
order: 151
permissions: [ops.settings.view]
conditions: []
routes: []
related: [payment-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Cashier/CardSettingsPanel.razor
  - src/Operations/Vixly.Operations.Infrastructure/Payments/OperationsStripeGateway.cs
updated: 2026-11-03
review: ok
---

Card payments, on the payment page and on card readers, are taken by your company's own Stripe account and paid into it. {brandName} never holds the money. You connect the account once, with three values from Stripe. Start in Stripe's test mode, try a payment, then do the same with the live account.

## Steps
1. In Stripe, open *Developers*, **API keys**, then *Create restricted key*. Choose *Building your own integration* and *Custom permissions*.
2. Set *Checkout Sessions*, *PaymentIntents* and *Charges and Refunds* to *Write*. If you use card readers at the counter, also set *Terminal Readers* to *Write*. Leave everything else at **None**, then create the key and copy it (it starts with rk_).
3. Still in Stripe, open *Developers*, **Webhooks**, and add an endpoint at the address shown on the **Cards (Stripe)** panel, for the events listed there. Copy its **Signing secret** (it starts with whsec_).
4. Copy the **Publishable key (pk_…)** (pk_) from *Developers*, **API keys**, in the same mode as the restricted key.
5. In **Payment methods**, **Cards (Stripe)**, paste the restricted key, the publishable key and the signing secret, then save.

## What happens next
The keys are checked with Stripe before anything is kept. A restricted key that is missing one of the permissions above is refused, with the permission to add. Once saved, the keys are never shown again (only their last characters) and are kept encrypted. Tick cards in **Methods the company accepts**, take a small test payment, refund it, and check in Stripe that the webhook deliveries succeeded. A full secret key (sk_) also works but gives far more access than card payments need: prefer the restricted key. To change keys, save the new ones over the old; to stop, use **Disconnect Stripe**.
