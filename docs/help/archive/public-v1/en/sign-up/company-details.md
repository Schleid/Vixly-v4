---
slug: company-details
title: Enter your company details
summary: Complete the company, billing address, owner and workspace address sections before submitting.
section: sign-up
order: 50
permissions: []
conditions: []
routes: []
related: [sign-up-overview, confirm-your-email, terms-and-privacy]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationStart.razor
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationStartBody.cs
  - src/Public/Vixly.Public.Web/Resources/PublicText.resx
updated: 2026-10-22
review: ok
---

The first sign-up form collects the details used to create your company’s account. Check the billing address carefully before submitting: the form asks you to confirm that it is correct for invoices.

## Steps
1. Under **Company**, enter **Legal name**, **Company email** and **Company phone**. Use **Trading name (optional)** if your company trades under another name.
2. Under **Address**, fill in **Address line 1**, **City** and **Country**. Add **Address line 2 (optional)**, **State or province** and **Postal code** where applicable. Select **This address is correct; it will appear on invoices.** after reviewing the address.
3. Under **Account owner**, enter **Full name**, **Email** and **Phone**. Make sure the owner can open the email sent to this address.
4. In **Address name**, choose lowercase letters, numbers and hyphens. This becomes the address used by your team and customers, and the form says it cannot be changed later.
5. Read **Terms of Service and Privacy Policy**, select the acceptance checkbox, complete the human check and select **Create account**.

## If something goes wrong
If the address name is unavailable, choose another. If you see **Please confirm the address before continuing.**, review the billing address and select its confirmation checkbox. Keep the form’s company email and owner email distinct when they are different addresses.
