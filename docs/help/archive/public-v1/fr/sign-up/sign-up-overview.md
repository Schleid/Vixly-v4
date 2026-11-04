---
slug: sign-up-overview
title: Inscrire votre entreprise
summary: Suivez la saisie des coordonnées, la confirmation, le choix du forfait et le paiement éventuel.
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
review: needs-review
---

L’inscription crée le compte Vixly de votre entreprise et lance la préparation de son espace. Préparez les coordonnées de facturation, celles du propriétaire du compte et une boîte de réception accessible. Gardez également votre éventuelle invitation ou offre.

## Étapes
1. Saisissez les coordonnées de l’entreprise, son adresse et celles du propriétaire. Choisissez le nom d’adresse utilisé par l’équipe et les clients, lisez les conditions puis sélectionnez **Créer le compte**.
2. Ouvrez l’e-mail envoyé par Vixly et sélectionnez **Confirmer mon e-mail** sur la page de confirmation. Ouvrir le lien seul ne termine pas la confirmation.
3. Choisissez le forfait, la période de facturation et les licences, ou acceptez l’offre présentée. Vérifiez l’essai ou la promotion avant de continuer.
4. Payez la première facture si nécessaire. Un essai ou une première période sans montant dû peut mener directement à la configuration du compte.
5. Suivez les indications de la page de création et l’e-mail contenant le lien de configuration de la connexion de votre équipe.

## Pour reprendre plus tard
La page suivante dépend de votre progression enregistrée. Conservez l’e-mail de confirmation et utilisez son lien avant son expiration ; l’étape e-mail affiche la date limite. Un autre navigateur ne conserve pas forcément tous vos choix. Si l’inscription générale est fermée, contactez le service commercial ou utilisez votre invitation valide. La durée dépend de la confirmation, du paiement et de la préparation de l’espace.
