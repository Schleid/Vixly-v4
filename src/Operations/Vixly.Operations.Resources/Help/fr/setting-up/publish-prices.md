---
slug: publish-prices
title: Afficher les prix sur le site et le portail
summary: Les prix publics permettent à votre entreprise de choisir les tarifs standards visibles par les visiteurs et clients.
section: setting-up
order: 110
permissions: [ops.charges.configure]
conditions: []
routes: []
related: [price-lists, company-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Settings/SettingsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/PriceListPanel.razor
updated: 2026-10-25
review: needs-review
---

Les prix publics permettent à {brandName} de choisir les tarifs standards visibles par les visiteurs et clients. Enregistrer et publier un tarif sont deux choix distincts. Vérifiez service, trajet et prix en vigueur avant publication ; un accord propre à un client n’est pas un tarif standard public.

## Étapes
1. Ouvrez les prix et sélectionnez le mode de transport voulu.
2. Ouvrez le tarif standard et vérifiez service, trajet, version actuelle et état actif.
3. Consultez **Publier sur le site public**. Ce choix concerne les tarifs standards, pas ceux d’un client particulier.
4. Sélectionnez la publication et enregistrez avec **Enregistrer**, après vérification des champs de version et de date.
5. Vérifiez l’activation des prix publics dans les paramètres de l’entreprise. Si cette page est en lecture seule, faites vérifier le choix par la personne chargée des paramètres.

## Et ensuite
Consultez les prix présentés aux clients une fois les deux choix corrects. Un tarif public ne garantit pas le montant total de chaque expédition : suppléments et caractéristiques restent applicables. Retirer la publication est distinct de désactiver le tarif utilisé par le personnel.