---
slug: sign-up-questions
title: Sign-up questions
summary: Find your next action when an address, email link or payment holds up sign-up.
section: sign-up
order: 110
permissions: []
conditions: []
routes: []
related: [confirm-your-email, company-details, pay-to-start, sign-up-by-invitation, contact-sales]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationEmail.razor
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationStart.razor
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationProgress.razor
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationGate.razor
updated: 2026-10-22
review: ok
---

Use these answers when you cannot continue registration. Read the message on your current page first: it helps distinguish an incomplete step from a registration that has ended.

## Company address and email
If the workspace address name is taken, choose a different name using lowercase letters, numbers and hyphens. This is the address your team and customers will use; check it before submitting.

If the confirmation email did not arrive, check the partly hidden owner address and unwanted-mail folder. Use **Send the link again** for another message. If the address is wrong, fill in **New email address** and select **Change email**. Reopen the complete email link if the confirmation page says it is incomplete.

## Payment and finishing
A cancelled checkout does not finish payment. Use **Go to payment** when shown to return to the invoice. A Zelle report waits for confirmation, so reporting it does not immediately mean the invoice is paid.

The account-created page says when your workspace is being prepared and which email will receive the setup link. It does not let you change a plan you have already chosen. Contact the team if your selected subscription needs attention.

## Sign-up is closed
Use a valid invitation sent for your owner email, or contact sales. The help articles remain available while general sign-up is closed; reading them does not create an account or reserve a workspace address.
