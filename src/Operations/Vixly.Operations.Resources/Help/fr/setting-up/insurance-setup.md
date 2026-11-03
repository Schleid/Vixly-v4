---
slug: insurance-setup
title: Produits et documents d’assurance
summary: La page d’assurance rassemble les polices, rattachements et textes de responsabilité utilisés par votre entreprise.
section: setting-up
order: 120
permissions: [ops.insurance.view]
conditions: []
routes: [/staff/insurance/]
related: [price-lists, invoice-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Insurance/InsurancePage.razor
  - src/Operations/Vixly.Operations.Web/Components/Insurance/PolicyPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/InsuranceProductsPanel.razor
updated: 2026-10-25
review: needs-review
---

La page d’assurance rassemble les polices, rattachements et textes de responsabilité utilisés par {brandName}. Les produits proposés aux clients et leurs primes se configurent dans les prix. Vérifiez les deux espaces avant de présenter une offre : une police enregistrée n’est pas un produit tarifé.

## Étapes
1. Ouvrez **Assurance** et consultez **Les polices de l’entreprise**. Ouvrez une police pour lire ses détails et documents.
2. Si les commandes existent, utilisez **Ajouter une police** pour enregistrer une police et ses informations.
3. Consultez **Couverture vendue aux clients** et la police choisie dans **S’appuie sur**. Enregistrez uniquement lorsque la page le permet.
4. Vérifiez **Le personnel peut changer l’assurance d’un envoi ouvert, quand le client accepte les conditions** et les textes de responsabilité par mode.
5. Utilisez **Ouvrir les prix** pour consulter les produits et primes dans les prix si cette tâche est disponible.

## Et ensuite
Vérifiez ensemble les dates des polices, rattachements et textes clients. Enregistrer des documents ne vend pas d’assurance et n’ajoute pas une prime à une expédition. Maintenez des informations exactes et consultez le produit réellement sélectionné pour vérifier le résultat d’assurance de l’expédition du client.