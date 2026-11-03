---
slug: sign-up-overview
title: Sign up your company
summary: Follow company details, email confirmation, plan selection and any payment needed.
section: sign-up
order: 40
permissions: []
conditions: []
routes: [/en/register, /fr/register, /es/register]
related: [company-details, confirm-your-email, choose-your-plan-at-sign-up, pay-to-start, after-sign-up]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationStart.razor
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationSteps.cs
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationGate.razor
  - src/Platform/Vixly.Platform.Application/UseCases/Registration/SelectRegistrationPlan/SelectRegistrationPlanUseCase.cs
updated: 2026-10-22
review: ok
---

Sign-up creates your company’s Vixly account and starts the preparation of its workspace. Have your company’s billing details, the account owner’s contact details and an accessible email inbox ready. Keep any invitation or offer email available too.

## Steps
1. Enter your company, address and owner details. Choose the address name your team and customers will use, review the terms and submit **Create account**.
2. Open the email Vixly sends you and select **Confirm my email** on the confirmation page. Opening the email link alone does not finish confirmation.
3. Choose your plan, billing period and licences, or accept the offer shown. Review any trial or promotion before continuing.
4. Pay the first invoice if payment is required. A trial or a first period with nothing due can move directly to account setup.
5. Follow the account-created page and the email with the link for setting up your team’s sign-in.

## Before returning later
The next page depends on your saved progress. Keep the verification email and use the link while it remains valid; the email step displays its deadline. Do not assume that restarting on another browser will retain every earlier choice. General sign-up may be closed, in which case contact sales or use the valid invitation sent to you. Completion time depends on email confirmation, payment and workspace preparation.
