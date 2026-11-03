---
slug: pay-an-invoice-link
title: Payer une facture depuis un e-mail
summary: Ouvrez le lien de facture Vixly, vérifiez le montant et choisissez un moyen proposé.
section: paying
order: 120
permissions: []
conditions: []
routes: [/pay/{Token}]
related: [pay-by-zelle, save-a-card, contact-sales]
sources:
  - src/Public/Vixly.Public.Web/Components/Pages/PayLinkPage.razor
  - src/Public/Vixly.Public.Web/Features/Pay/PaymentPanel.razor
  - src/Public/Vixly.Public.Web/Features/Pay/PaymentTarget.cs
  - build/HelpCapture/capture.cjs
  - src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json
updated: 2026-10-25
review: needs-review
---

Un e-mail de facture peut contenir un lien vers une page de paiement Vixly. Il concerne la facture Vixly de votre entreprise, distincte des factures réglées par vos propres clients dans votre portail à votre marque.

## Étapes
1. Ouvrez le lien de l’e-mail. Vérifiez le nom de l’entreprise, **Facture**, **Montant dû**, la devise et l’éventuelle **Échéance**.
2. Si **Payer par carte** est proposé, examinez les choix facultatifs d’enregistrement de carte et de paiement automatique. Sélectionnez **Continuer vers le paiement** pour ouvrir la page du prestataire.
3. Si **Payer avec Zelle** est proposé, payez depuis votre banque avec les coordonnées affichées puis envoyez la déclaration sur cette page.
4. Au retour, lisez le message d’état. Un paiement par carte peut être en cours de confirmation et une déclaration Zelle attend une vérification bancaire. Conservez le reçu envoyé par e-mail.

## En cas de problème
**Cette facture est réglée. Merci.** signifie que la facture est déjà payée. **Le paiement en ligne n’est pas disponible pour cette facture. Contactez la facturation Vixly.** vous dirige vers la facturation Vixly lorsqu’aucun moyen en ligne n’est disponible. Vous pouvez reprendre un paiement annulé si le règlement est encore nécessaire. Si le lien ne s’ouvre pas ou si les informations doivent être revues, contactez la facturation sans deviner une autre adresse. Les liens de langue permettent de choisir le français ou l’espagnol.

## Sur cet écran

1 indique l’entreprise et le solde de la facture ; 2 la rubrique du paiement par carte ; 3 le bouton pour continuer. Le paiement du prestataire s’ouvre après cette action.

[![Écran annoté indiquant où agir: Détails de la facture et paiement par carte](/help/media/v2/fr-invoice.png)](/help/media/v2/fr-invoice.png)

Écran illustratif avec des données fictives. Les prix, dates et choix disponibles peuvent varier. Sélectionnez l’image pour l’ouvrir en grand.
