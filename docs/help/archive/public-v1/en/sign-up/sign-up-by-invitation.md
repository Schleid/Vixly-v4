---
slug: sign-up-by-invitation
title: Sign up with an invitation or offer
summary: Review a sales offer and use the invitation sent for your company and owner email.
section: sign-up
order: 100
permissions: []
conditions: []
routes: []
related: [company-details, choose-your-plan-at-sign-up, contact-sales]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationGate.razor
  - src/Public/Vixly.Public.Web/Features/Registration/OfferSummary.razor
  - src/Platform/Vixly.Platform.Application/UseCases/Registration/StartPlatformTenantRegistration/StartPlatformTenantRegistrationUseCase.cs
  - src/Platform/Vixly.Platform.Application/UseCases/Registration/SelectRegistrationPlan/SelectRegistrationPlanUseCase.cs
updated: 2026-10-22
review: ok
---

A sales invitation can let you sign up with agreed terms, including while general online sign-up is closed. Use the offer link sent for your company, rather than starting again from the general registration page.

## Steps
1. Open your invitation and review the offer shown on the sign-up page. Check the company, plan, billing period, amount, setup charges, trial and validity date that are displayed.
2. Complete the company and owner form using the owner email associated with the offer. An offer is not a general discount link to share with another company or email address.
3. Confirm the owner email using the message Vixly sends you.
4. At the plan step, review the offer again and select **Accept this offer** if you agree to its terms. Then complete any payment required.

## If something goes wrong
An expired, already-used or unsuitable offer cannot be used to start a new subscription. Contact sales if the company details, intended owner email or agreed terms need attention. Do not replace the offer with a guessed promotion code.

Without a valid invitation, a closed sign-up page directs you to the sales team. You can still browse the public plans and help while deciding what your operation needs. Accepting an offer uses the terms shown in that offer, rather than a different plan you happened to view earlier.
