---
slug: confirm-your-email
title: Confirm your email
summary: Use the emailed confirmation link, resend it or correct the owner’s address.
section: sign-up
order: 60
permissions: []
conditions: []
routes: [/en/register/email, /fr/register/email, /es/register/email, /register/verify]
related: [company-details, choose-your-plan-at-sign-up, sign-up-questions]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationEmail.razor
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationVerify.razor
  - src/Public/Vixly.Public.Web/Components/Pages/RegisterVerifyPage.razor
  - build/HelpCapture/capture.cjs
  - src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json
updated: 2026-10-25
review: ok
---

After the company form, Vixly asks you to confirm the account owner’s email. The email step shows a partly hidden address and the deadline for using the link. Check both before leaving the page.

## Steps
1. Open the inbox for the owner email you entered. Find the confirmation email from Vixly and open its link before the displayed deadline.
2. On the page that opens, select **Confirm my email**. This confirms the address and continues to the plan step. You can complete this confirmation in another browser using the emailed link.
3. If the email did not arrive, check the address shown and your unwanted-mail folder. Select **Send the link again** on the email step if you need another link.
4. If the address is wrong, enter the correction in **New email address** and select **Change email**. Look for the new message at the corrected address.

## If something goes wrong
**This link is incomplete. Open the link from the email again.** means you should reopen the complete link from the email. Opening the link alone does not confirm the address; use the confirmation button. If the link has expired or your registration has ended, follow the page’s message rather than repeatedly using the same old link. Keep the newest email until you reach the next step.

## On this screen

1 points to resending the email; 2 marks the address-correction field. In the second image, 1 points to the confirmation button reached from your email link.

[![Annotated screen showing where to act: Email status, resend and correction](/help/media/v2/en-email.png)](/help/media/v2/en-email.png)

[![Annotated screen showing where to act: Email confirmation button](/help/media/v2/en-verify.png)](/help/media/v2/en-verify.png)

Illustrative screen with fictional demo data. Prices, dates and available choices may differ. Select the image to open it at full size.
