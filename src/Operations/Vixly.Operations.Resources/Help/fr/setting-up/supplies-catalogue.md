---
slug: supplies-catalogue
title: Catalogue et prix des fournitures
summary: Le catalogue contient les marchandises et emballages vendus par votre entreprise.
section: setting-up
order: 210
permissions: [ops.charges.configure]
conditions: []
routes: [/staff/supplies, /staff/supplies/{supplyId:guid}]
related: [invoice-settings, price-lists]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SuppliesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SupplyPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SupplyDetailsFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SupplyPriceFields.razor
updated: 2026-10-25
review: needs-review
---

Le catalogue contient les marchandises et emballages vendus par {brandName}. Recherchez une entrée avant d’en créer une autre, notamment son code, ses codes-barres et son prix actuel. Les détails et prix datés sont distincts : renommer un article ne remplace pas son historique de prix.

## Étapes
1. Ouvrez les fournitures puis utilisez **Code, nom ou code-barres** ou **Catégorie** pour trouver l’article.
2. Pour un ajout, ouvrez **Nouvelle fourniture**. Saisissez code, catégorie, unité et noms localisés ; vérifiez taxe et codes-barres.
3. Complétez le prix initial puis choisissez **Ajouter la fourniture**.
4. Pour modifier un article, ouvrez son code, corrigez **Détails et codes-barres** puis **Enregistrer**.
5. Dans **Un nouveau prix**, vérifiez date d’effet, paliers de quantité et prix par entrepôt. Choisissez **Enregistrer le prix**.

## Et ensuite
Vérifiez l’article et son prix actuel ou programmé. Un prix futur n’est pas immédiatement en vigueur. Désactiver un article est distinct de changer son prix. Créer une entrée ou imprimer ses étiquettes ne réceptionne pas de stock physique et n’enregistre pas de vente ; ce sont des tâches distinctes.