---
slug: choose-your-plan-at-sign-up
title: Choisir votre forfait et vos licences
summary: Vérifiez le forfait, la facturation, les licences, l’essai et la promotion avant de continuer.
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
review: needs-review
---

Choisissez votre abonnement après avoir confirmé l’adresse du propriétaire. Si vous avez choisi un forfait dans le catalogue public, il peut être présélectionné ici. Vérifiez ce choix avant de continuer.

## Étapes
1. Dans **Forfait**, sélectionnez le forfait souhaité. Lisez son prix affiché et les informations sur un éventuel essai.
2. Dans **Facturation**, choisissez **Mensuelle** ou **Annuelle**. Indiquez le nombre de licences d’appareils dans **Licences**, en tenant compte du minimum et de la limite du forfait.
3. Si vous avez une promotion, saisissez-la dans **Code promotionnel (facultatif)**. Elle doit être disponible pour votre forfait et votre inscription ; la présence du champ ne garantit pas l’acceptation de tout code.
4. Vérifiez **Commencer par l’essai gratuit (si le forfait en propose un)**. Gardez cette option seulement si vous souhaitez l’essai et si le forfait en propose un. Lisez la note sur les taxes puis sélectionnez **Continuer**.

## Ce qui se passe ensuite
Un essai ou une première période sans montant dû peut mener directement à la configuration du compte. Sinon, vous devez payer la première facture. Avec une invitation, cette page peut présenter une offre convenue à la place du formulaire. Vérifiez le forfait, les licences, la facturation et les frais, puis utilisez **Accepter cette offre** uniquement si vous acceptez les conditions affichées.
