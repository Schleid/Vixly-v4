---
slug: confirm-your-email
title: Confirmer votre adresse e-mail
summary: Utilisez le lien de confirmation, renvoyez-le ou corrigez l’adresse du propriétaire.
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
review: needs-review
---

Après le formulaire de l’entreprise, Vixly vous demande de confirmer l’adresse du propriétaire du compte. L’étape e-mail présente une adresse partiellement masquée et la date limite du lien. Vérifiez-les avant de quitter la page.

## Étapes
1. Ouvrez la boîte de réception de l’adresse du propriétaire saisie. Recherchez l’e-mail de confirmation Vixly et ouvrez son lien avant la date limite affichée.
2. Sur la page ouverte, sélectionnez **Confirmer mon e-mail**. L’adresse est confirmée et vous passez au choix du forfait. Vous pouvez effectuer cette confirmation dans un autre navigateur avec le lien reçu.
3. Si le message n’est pas arrivé, vérifiez l’adresse présentée et les courriers indésirables. Sélectionnez **Renvoyer le lien** à l’étape e-mail pour recevoir un autre lien.
4. Si l’adresse est incorrecte, saisissez la correction dans **Nouvelle adresse e-mail** et sélectionnez **Changer d’e-mail**. Consultez la nouvelle boîte de réception.

## En cas de problème
**Ce lien est incomplet. Rouvrez le lien de l’e-mail.** vous demande de rouvrir le lien complet de l’e-mail. Ouvrir le lien seul ne confirme pas l’adresse : utilisez le bouton. Si le lien a expiré ou si l’inscription est terminée, suivez le message de la page plutôt que de réutiliser l’ancien lien. Conservez l’e-mail le plus récent jusqu’à l’étape suivante.

## Sur cet écran

1 pointe vers le renvoi du courriel ; 2 indique le champ de correction de l’adresse. Dans la seconde image, 1 indique le bouton de confirmation atteint depuis le lien reçu.

[![Écran annoté indiquant où agir: Suivi, renvoi et correction du courriel](/help/media/v2/fr-email.png)](/help/media/v2/fr-email.png)

[![Écran annoté indiquant où agir: Bouton de confirmation du courriel](/help/media/v2/fr-verify.png)](/help/media/v2/fr-verify.png)

Écran illustratif avec des données fictives. Les prix, dates et choix disponibles peuvent varier. Sélectionnez l’image pour l’ouvrir en grand.
