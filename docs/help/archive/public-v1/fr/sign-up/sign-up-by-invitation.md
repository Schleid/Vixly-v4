---
slug: sign-up-by-invitation
title: S’inscrire avec une invitation ou une offre
summary: Vérifiez l’offre et utilisez l’invitation destinée à votre entreprise et au propriétaire.
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
review: needs-review
---

Une invitation commerciale permet de vous inscrire avec des conditions convenues, même lorsque l’inscription générale est fermée. Utilisez le lien envoyé à votre entreprise plutôt que de recommencer sur la page générale.

## Étapes
1. Ouvrez l’invitation et vérifiez l’offre présentée. Contrôlez l’entreprise, le forfait, la facturation, le montant, les frais de mise en service, l’essai et la date de validité affichés.
2. Remplissez les coordonnées de l’entreprise et du propriétaire avec l’adresse e-mail associée à l’offre. Une offre n’est pas un lien de réduction à partager avec une autre entreprise ou une autre adresse.
3. Confirmez l’adresse du propriétaire avec le message envoyé par Vixly.
4. À l’étape du forfait, relisez l’offre et sélectionnez **Accepter cette offre** si vous en acceptez les conditions. Effectuez ensuite le paiement éventuellement demandé.

## En cas de problème
Une offre expirée, déjà utilisée ou inadaptée ne permet pas de démarrer un nouvel abonnement. Contactez le service commercial si les coordonnées, l’adresse du propriétaire ou les conditions doivent être revues. Ne remplacez pas l’offre par un code promotionnel supposé.

Sans invitation valide, la page fermée vous dirige vers l’équipe commerciale. Vous pouvez toujours consulter les forfaits et l’aide. Accepter une offre utilise ses conditions affichées, et non celles d’un autre forfait consulté auparavant.
