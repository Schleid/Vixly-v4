---
slug: card-payments-stripe
title: Connecter votre compte Stripe
summary: Les paiements par carte vont sur le propre compte Stripe de votre entreprise ; vous le connectez avec une clé restreinte.
section: setting-up
order: 151
permissions: [ops.settings.view]
conditions: []
routes: []
related: [payment-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Cashier/CardSettingsPanel.razor
  - src/Operations/Vixly.Operations.Infrastructure/Payments/OperationsStripeGateway.cs
updated: 2026-11-03
review: needs-review
---

Les paiements par carte, sur la page de paiement et sur les lecteurs, sont encaissés par le propre compte Stripe de votre entreprise et versés sur celui-ci. {brandName} ne détient jamais l'argent. Vous connectez le compte une fois, avec trois valeurs fournies par Stripe. Commencez en mode test de Stripe, essayez un paiement, puis refaites la même chose avec le compte réel.

## Étapes
1. Dans Stripe, ouvrez **Développeurs**, **Clés API**, puis **Créer une clé restreinte**. Choisissez l'intégration à votre propre usage et les **autorisations personnalisées**.
2. Mettez **Checkout Sessions**, **PaymentIntents** et **Charges and Refunds** en **écriture (Write)**. Si vous utilisez des lecteurs de cartes au comptoir, mettez aussi **Terminal Readers** en écriture. Laissez tout le reste à **Aucun (None)**, puis créez la clé et copiez-la (elle commence par rk_).
3. Toujours dans Stripe, ouvrez **Développeurs**, **Webhooks**, et ajoutez un point de terminaison à l'adresse indiquée sur le panneau **Cartes (Stripe)**, pour les événements qui y sont listés. Copiez son **secret de signature** (il commence par whsec_).
4. Copiez la **clé publiable** (pk_) dans **Développeurs**, **Clés API**, dans le même mode que la clé restreinte.
5. Dans **Modes de paiement**, **Cartes (Stripe)**, collez la clé restreinte, la clé publiable et le secret de signature, puis enregistrez.

## Et ensuite
Les clés sont vérifiées auprès de Stripe avant d'être conservées. Une clé restreinte à laquelle il manque l'une des autorisations ci-dessus est refusée, avec l'autorisation à ajouter. Une fois enregistrées, les clés ne sont plus jamais affichées (seuls leurs derniers caractères) et sont conservées chiffrées. Cochez les cartes dans les moyens acceptés par l'entreprise, faites un petit paiement de test, remboursez-le, et vérifiez dans Stripe que les envois du webhook ont réussi. Une clé secrète complète (sk_) fonctionne aussi mais donne bien plus d'accès que nécessaire : préférez la clé restreinte. Pour changer de clés, enregistrez les nouvelles par-dessus les anciennes ; pour arrêter, utilisez **Déconnecter Stripe**.
