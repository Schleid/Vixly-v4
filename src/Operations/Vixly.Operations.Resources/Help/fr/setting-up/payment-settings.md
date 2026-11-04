---
slug: payment-settings
title: Moyens de paiement : cartes, Zelle et virements
summary: Les paramètres décrivent comment votre entreprise accepte l’argent.
section: setting-up
order: 150
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/payments]
related: [invoice-settings, credit-settings, card-payments-stripe]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Cashier/PaymentSettingsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Cashier/CardSettingsPanel.razor
updated: 2026-10-25
review: needs-review
---

Les paramètres décrivent comment {brandName} accepte l’argent. Vérifiez les coordonnées réelles de paiement de l’entreprise avant d’activer un moyen. Ces choix préparent les options ; ils ne confirment ni un envoi d’argent par le client ni le règlement d’une facture.

## Étapes
1. Ouvrez **Modes de paiement** et consultez **Modes acceptés par l’entreprise**. Sélectionnez les moyens acceptés uniquement si la page permet la modification.
2. Vérifiez les noms des moyens supplémentaires afin que le personnel les identifie correctement.
3. Pour Zelle, vérifiez destinataire, image QR et instructions dans chaque langue. Utilisez uniquement les coordonnées prévues par l’entreprise.
4. Choisissez **Enregistrer** si disponible et lisez le résultat.
5. Consultez séparément **Cartes (Stripe)**. Si les commandes existent, suivez les instructions pour le compte de l’entreprise et confirmez votre identité si demandé.

## Et ensuite
Rouvrez la page et vérifiez moyens et destinataire. Connexion du compte, acceptation des cartes et lecteurs d’entrepôt sont des choix distincts. Gardez les identifiants de connexion privés. Activer un moyen ne reçoit aucun paiement ; le personnel doit suivre le parcours de facturation ou d’encaissement et vérifier son résultat.