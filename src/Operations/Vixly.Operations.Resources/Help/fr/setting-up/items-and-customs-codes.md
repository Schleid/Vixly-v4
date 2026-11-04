---
slug: items-and-customs-codes
title: Articles et codes douaniers
summary: Les clients déclarent le contenu article par article ; le personnel ajoute le code douanier et le pays d'origine avant la mise en lot.
section: setting-up
order: 72
permissions: [ops.services.view, ops.inspection.check]
conditions: []
routes: [/staff/items, /staff/settings/items]
related: [shipping-services, inspection-rules]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Items/ItemCataloguePage.razor
  - src/Operations/Vixly.Operations.Web/Components/Items/ItemsReviewPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Items/ParcelItemsPanel.razor
updated: 2026-11-01
review: needs-review
---

Chaque colis indique désormais son contenu article par article : les mots du client, la quantité, la valeur d'une unité et l'état, neuf ou usagé. Le client ne saisit jamais de code douanier. {brandName} l'ajoute, avec le pays d'origine lu sur l'article, pour préparer correctement les documents de douane.

## Étapes
1. Ouvrez **Catalogue d'articles** dans la navigation. La première fois, il contient des articles courants et leurs codes douaniers. Modifiez, ajoutez ou supprimez des articles selon votre entreprise. Les mots de « correspond aussi à » sont ceux des clients, dans toutes les langues.
2. Quand les mots d'un client correspondent à un article du catalogue, la description, le code douanier et l'unité sont remplis automatiquement. Le pays d'origine n'est jamais deviné.
3. Ouvrez **Articles à vérifier** pour voir les colis à l'origine dont un article n'a pas encore de code douanier ou de pays d'origine, du plus ancien au plus récent.
4. Ouvrez un colis de la liste. Dans ses articles, choisissez la **Description sur les documents** parmi les suggestions pour remplir le **Code douanier**, puis saisissez l'**Origine** en deux lettres d'après l'étiquette « Made in… ». Cochez **Ajouter au catalogue** pour un nouvel article courant.
5. Choisissez **Enregistrer les articles**.
6. Lorsque votre équipe est prête, ouvrez **Catalogue d'articles** et choisissez **Exiger codes et origine**.

## Et ensuite
Dès que la règle est active, un colis reçu ensuite ne peut entrer ni dans un lot ni dans un carton, et son expédition ne peut pas être clôturée, tant que chaque article n'a pas de code douanier et de pays d'origine. Le message indique le colis et l'article à compléter. Les colis reçus avant l'activation ne sont jamais bloqués. La déclaration du client (mots, quantité et valeur) n'est jamais modifiée par le personnel.
