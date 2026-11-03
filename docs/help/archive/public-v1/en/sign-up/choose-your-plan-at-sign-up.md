---
slug: choose-your-plan-at-sign-up
title: Choose your plan and licences
summary: Review the plan, billing period, licence count, trial and any promotion before continuing.
section: sign-up
order: 70
permissions: []
conditions: []
routes: [/en/register/plan, /fr/register/plan, /es/register/plan]
related: [choose-a-plan, licences-and-devices, pay-to-start, sign-up-by-invitation]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationPlan.razor
  - src/Public/Vixly.Public.Web/Features/Registration/OfferSummary.razor
  - src/Platform/Vixly.Platform.Application/UseCases/Registration/SelectRegistrationPlan/SelectRegistrationPlanUseCase.cs
updated: 2026-10-22
review: ok
---

Choose your subscription after confirming the owner’s email. If you selected a plan on the public catalogue, it may already be selected here; check the selection rather than assuming it is final.

## Steps
1. Under **Plan**, select the plan you want. Read its displayed price and any trial information.
2. In **Billing**, choose **Monthly** or **Annual**. Enter your device licence count in **Licences**, keeping the plan’s minimum and limit in mind.
3. If you have a promotion, enter it in **Promotion code (optional)**. A promotion must be available for your plan and registration; a visible input does not guarantee that every code is accepted.
4. Review **Start with the free trial (when the plan has one)**. Leave it selected only if you want the free trial and the plan offers one. Review the before-tax note and select **Continue**.

## What happens next
A trial or a first period with nothing due can go directly to account setup. Otherwise, the next step asks you to pay your first invoice. With an invitation, this page can show an agreed offer instead of the plan-selection form. Review its plan, licences, billing and any charges, then use **Accept this offer** only if you agree. The offer’s terms are used as shown.
