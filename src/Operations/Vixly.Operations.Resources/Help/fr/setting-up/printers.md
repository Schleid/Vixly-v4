---
slug: printers
title: Imprimantes d’étiquettes et programme d’impression
summary: Configurez les imprimantes d’un entrepôt pour que votre entreprise envoie les étiquettes au bon appareil.
section: setting-up
order: 60
permissions: [ops.printers.manage]
conditions: []
routes: [/staff/locations/warehouse/{warehouseId:guid}/printers/]
related: [branches-and-warehouses, storage-locations]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Parcels/PrintersPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Parcels/PrinterForm.razor
updated: 2026-10-25
review: needs-review
---

Configurez les imprimantes d’un entrepôt pour que {brandName} envoie les étiquettes au bon appareil. La définition de l’imprimante et le programme d’impression sont distincts : enregistrer une imprimante ne prouve pas que l’ordinateur exécutant le programme est connecté ou prêt.

## Étapes
1. Ouvrez **Imprimantes** à côté de l’entrepôt dans la liste des lieux.
2. Renseignez **Nom** et choisissez **Connexion**.
3. Complétez **Adresse ou nom Windows**, **Résolution (ppp)** et **Étiquettes** selon l’appareil et les étiquettes chargées. Choisissez **Ajouter**.
4. Dans **Programmes d'impression**, saisissez **Ordinateur** puis **Enregistrer** pour enregistrer le programme. Suivez les instructions et gardez sa clé de connexion privée.
5. Lorsque l’appareil et le programme sont prêts, utilisez **Imprimer une étiquette de test** pour l’imprimante enregistrée.

## Et ensuite
Vérifiez l’étiquette physique et la dernière présence du programme. Une imprimante enregistrée ne garantit pas une impression réussie. Associez correctement les supports d’identification et d’expédition. En cas d’échec, vérifiez l’entrepôt choisi, la connexion de l’imprimante et l’état du programme avant de poursuivre le travail.