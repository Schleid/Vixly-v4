---
slug: extras-and-item-types
title: Suppléments et types d’articles
summary: Les suppléments ajoutent des frais lorsque leurs critères correspondent au travail de votre entreprise.
section: setting-up
order: 100
permissions: [ops.charges.configure]
conditions: []
routes: []
related: [price-lists, zones-and-routes]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Charges/ExtrasPanel.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargeExtraFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ItemTypesPanel.razor
updated: 2026-10-25
review: needs-review
---

Les suppléments ajoutent des frais lorsque leurs critères correspondent au travail de {brandName}. Les types d’articles décrivent les marchandises ayant leurs propres prix maritimes. Vérifiez le mode avant de modifier et distinguez le nom et les critères du montant applicable à une date.

## Étapes
1. Dans les prix, ouvrez **Ajouter un supplément** ou développez un supplément existant.
2. Renseignez **Nom affiché sur la ligne**, **Montant ou pourcentage** et le mode de calcul.
3. Vérifiez **S'applique**, les mots, le service, le pays, le trajet et la livraison. Réservez le choix de frais gouvernementaux à ces frais.
4. Définissez **Commence le** et enregistrez le supplément.
5. Pour le maritime, consultez **Types d’articles maritimes**. Saisissez nom et mots de correspondance, puis **Ajouter le type d’article** ou **Enregistrer**.

## Et ensuite
Vérifiez l’entrée et les montants programmés. Désactiver un supplément ou type d’article conserve son historique au lieu de le supprimer. Créer un type ne définit pas son prix : consultez aussi le tarif maritime correspondant. Vérifiez un devis représentatif avant d’utiliser les nouvelles règles avec les clients.