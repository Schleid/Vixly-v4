---
slug: branches-and-warehouses
title: Succursales et entrepôts
summary: Les succursales et entrepôts représentent les lieux de travail de votre entreprise.
section: setting-up
order: 30
permissions: [ops.branches.view, ops.warehouses.view]
conditions: []
routes: [/staff/locations, /staff/locations/branch/{branchId:guid?}, /staff/locations/warehouse/{warehouseId:guid?}]
related: [customers-mailbox, storage-locations, printers]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Editor/LocationsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Editor/BranchFormPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Editor/WarehouseFormPage.razor
updated: 2026-10-29
review: needs-review
---

Les succursales et entrepôts décrivent où {brandName} travaille. Une succursale est un pays ou un marché que vous desservez, par exemple États-Unis ou Haïti. Ses entrepôts sont les lieux réels sur place : boîte postale, réception, stockage, points de retrait. Une succursale n'est ni « origine » ni « destination » : vos routes et les rôles de chaque entrepôt décident de ce qui va où.

## Étapes
1. Ouvrez **Adresses et contacts** et vérifiez les listes de succursales et d'entrepôts qui vous sont accessibles.
2. Pour ajouter un pays desservi, choisissez **Ajouter une agence** : indiquez un code et un nom, choisissez son pays et, si le pays l'exige, son identité légale sur place (raison sociale, numéro fiscal, mentions imprimées sur les factures).
3. Laissez **Bureau principal (facultatif)** vide si la succursale n'a pas de bureau propre. Seule une succursale avec bureau peut apparaître sur votre site.
4. Pour ajouter un lieu, choisissez **Ajouter un entrepôt**, choisissez sa succursale, puis saisissez son code, son nom et son adresse. L'adresse doit se trouver dans le pays de la succursale.
5. Sélectionnez les rôles réels de l'entrepôt et vérifiez le choix de visibilité sur le site. Choisissez **Enregistrer** lorsque la modification est possible.

## Et ensuite
Les factures émises dans un entrepôt impriment l'identité légale de sa succursale si elle est renseignée, sinon celle de l'entreprise. Créer un entrepôt ne configure pas les étagères, les imprimantes ni les prix ; configurez-les séparément. La visibilité sur le site et l'activité sont deux choix distincts ; vérifiez les deux avant de donner une adresse aux clients.
