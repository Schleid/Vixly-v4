---
slug: carriers
title: Transporteurs et leurs tarifs
summary: Les fiches décrivent les transporteurs utilisés par votre entreprise et leurs tarifs par destination.
section: setting-up
order: 220
permissions: [ops.carriers.manage]
conditions: []
routes: [/staff/carriers, /staff/carriers/{carrierId:guid}]
related: [shipping-services, price-lists]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Dispatch/CarriersPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Dispatch/CarrierPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Dispatch/CarrierForm.razor
updated: 2026-10-25
review: needs-review
---

Les fiches décrivent les transporteurs utilisés par {brandName} et leurs tarifs par destination. Ce sont des coûts de transporteur, distincts des prix facturés aux clients. Vérifiez le type avant de saisir des montants : les formulaires aérien et maritime proposent des calculs différents.

## Étapes
1. Ouvrez **Transporteurs** et consultez la liste pour éviter un doublon.
2. Utilisez **Nouveau transporteur** pour un nouveau transporteur ou ouvrez un nom existant.
3. Vérifiez **Nom**, **Type**, **Numéro de compte** et **Préfixe de la compagnie**. Lisez l’indication du préfixe puis choisissez le statut actif prévu.
4. Choisissez **Enregistrer** et ouvrez la fiche enregistrée.
5. Dans **Tarifs**, vérifiez destination, date de début et montants selon le mode, puis **Ajouter un tarif** pour ajouter un tarif.

## Et ensuite
Vérifiez destination et date après l’enregistrement. Un tarif ultérieur remplace le précédent à partir de son début. Un tarif futur peut être retiré avant cette date. Enregistrer un transporteur ou coût ne publie pas de prix clients et ne crée pas de départ ; configurez ces éléments dans leurs parcours respectifs.