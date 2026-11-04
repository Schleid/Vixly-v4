---
slug: pay-by-zelle
title: Pay by Zelle
summary: Use the recipient details shown on your invoice and report the payment for confirmation.
section: paying
order: 140
permissions: []
conditions: []
routes: []
related: [pay-an-invoice-link, pay-to-start, contact-sales]
sources:
  - src/Public/Vixly.Public.Web/Features/Pay/ZellePanel.razor
  - src/Public/Vixly.Public.Web/Features/Pay/PaymentPanel.razor
  - src/Public/Vixly.Public.Web/Resources/PublicText.resx
updated: 2026-10-22
review: ok
---

When Zelle is offered on your invoice page, you make the transfer through your bank and then tell Vixly about it. Submitting the report does not send money from your bank and does not immediately settle the invoice.

## Steps
1. Read **Recipient** and whichever email, tag or phone details the page shows. Use the current invoice’s recipient details, rather than details copied from an older payment. A QR code may also be displayed.
2. Send the invoice amount through your bank’s Zelle service. Check the amount and keep the bank’s payment confirmation.
3. Under **I sent a Zelle payment**, enter **Amount sent**, **Date sent**, **Name on the sending account** and **Zelle confirmation code**. These identify the transfer you have already sent.
4. Select **Tell Vixly**. Read the message after returning to the invoice or registration page.

## What happens next
Vixly checks the report against the payment received at the bank. The page can show the amount and date being checked while confirmation is pending. Do not send the same money again because the invoice has not changed immediately.

If a report is already waiting, the page shows its status instead of another report form. If you made a mistake in the report or need help matching a transfer, contact Vixly billing with the invoice details and the bank confirmation.
