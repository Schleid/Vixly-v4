---
slug: zones-and-routes
title: Choisir le trajet d’un tarif
summary: Le trajet limite les déplacements auxquels un tarif ou supplément s’applique pour votre entreprise.
section: setting-up
order: 90
permissions: [ops.charges.configure]
conditions: []
routes: []
related: [price-lists, branches-and-warehouses]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Charges/RouteFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargeExtraFields.razor
updated: 2026-10-25
review: needs-review
---

Le trajet limite les déplacements auxquels un tarif ou supplément s’applique pour {brandName}. Ces formulaires proposent des entrepôts d’origine et de destination, et non une zone dessinée sur une carte. Vérifiez les choix avant d’enregistrer : une sélection large peut étendre le prix au-delà du trajet prévu.

## Étapes
1. Ouvrez le tarif ou supplément concerné depuis la page des prix.
2. Repérez **De (lieu de dépôt)** et choisissez l’entrepôt de départ prévu.
3. Utilisez **Toute origine** uniquement si le prix doit couvrir toutes les origines correspondantes.
4. Sélectionnez **Vers** ou **Toute destination** si la règle ne doit pas viser un seul lieu.
5. Vérifiez le service, les autres restrictions et la date d’effet, puis enregistrez ce formulaire.

## Et ensuite
Rouvrez le tarif ou supplément pour confirmer son trajet. Un entrepôt précédemment choisi peut rester sélectionné même s’il n’est plus proposé ; lisez l’indication de choix conservé sans supposer que le trajet devient illimité. Ces paramètres définissent l’application des prix. Ils ne créent aucun départ, ne déplacent aucun colis et n’organisent pas les arrêts d’un chauffeur.