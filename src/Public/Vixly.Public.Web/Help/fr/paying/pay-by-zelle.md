---
slug: pay-by-zelle
title: Payer avec Zelle
summary: Utilisez le destinataire affiché et déclarez le paiement pour sa confirmation.
section: paying
order: 140
permissions: []
conditions: []
routes: []
related: [pay-an-invoice-link, pay-to-start, contact-sales]
sources:
  - src/Public/Vixly.Public.Web/Features/Pay/ZellePanel.razor
  - src/Public/Vixly.Public.Web/Features/Pay/PaymentPanel.razor
  - src/Public/Vixly.Public.Web/Resources/PublicText.resx
  - build/HelpCapture/capture.cjs
  - src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json
updated: 2026-10-25
review: needs-review
---

Lorsque Zelle est proposé sur la facture, effectuez le transfert depuis votre banque puis informez Vixly. L’envoi de la déclaration ne transfère pas d’argent depuis votre banque et ne solde pas immédiatement la facture.

## Étapes
1. Lisez **Bénéficiaire** et les coordonnées e-mail, identifiant ou téléphone affichées. Utilisez les coordonnées de la facture actuelle, pas celles copiées d’un ancien paiement. Un code QR peut aussi être présenté.
2. Envoyez le montant de la facture avec le service Zelle de votre banque. Vérifiez le montant et conservez la confirmation bancaire.
3. Dans **J’ai envoyé un paiement Zelle**, remplissez **Montant envoyé**, **Date d’envoi**, **Nom du compte émetteur** et **Code de confirmation Zelle**. Ces informations identifient le transfert déjà envoyé.
4. Sélectionnez **Prévenir Vixly**. Lisez le message au retour à la facture ou à l’inscription.

## Ce qui se passe ensuite
Vixly rapproche la déclaration du paiement reçu à la banque. La page peut montrer le montant et la date en cours de vérification. Ne renvoyez pas la même somme simplement parce que la facture n’a pas changé immédiatement.

Lorsqu’une déclaration attend déjà, la page présente son état au lieu d’un autre formulaire. Si la déclaration est incorrecte ou si le rapprochement nécessite de l’aide, contactez la facturation Vixly avec les références de facture et la confirmation bancaire.

## Sur cet écran

1 indique le destinataire ; 2 le montant envoyé ; 3 la date ; 4 l’expéditeur ; 5 le code de confirmation bancaire ; 6 le bouton de déclaration. Le personnel doit encore confirmer le paiement.

[![Écran annoté indiquant où agir: Destinataire Zelle et déclaration de paiement](/help/media/v2/fr-zelle.png)](/help/media/v2/fr-zelle.png)

Écran illustratif avec des données fictives. Les prix, dates et choix disponibles peuvent varier. Sélectionnez l’image pour l’ouvrir en grand.
