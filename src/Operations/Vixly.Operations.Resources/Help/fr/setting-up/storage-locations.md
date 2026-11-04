---
slug: storage-locations
title: Emplacements de stockage et leurs codes
summary: Les emplacements identifient les étagères ou zones utilisées par votre entreprise dans un entrepôt.
section: setting-up
order: 50
permissions: [ops.warehouses.manage]
conditions: []
routes: [/staff/locations/warehouse/{warehouseId:guid}/storage]
related: [branches-and-warehouses, printers]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Parcels/StorageLocationsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Parcels/ZoneSelect.razor
updated: 2026-10-25
review: needs-review
---

Les emplacements identifient les étagères ou zones utilisées par {brandName} dans un entrepôt. Leurs codes permettent au personnel de désigner le même lieu lors du rangement et de la recherche. Vérifiez l’entrepôt avant un ajout : le code appartient à sa liste d’emplacements.

## Étapes
1. Ouvrez **Emplacements** pour l’entrepôt depuis la liste des lieux.
2. Dans **Ajouter un emplacement**, saisissez **Code** selon le code prévu pour l’étagère ou la zone physique.
3. Choisissez **Zone** puis **Ajouter**.
4. Vérifiez que le nouveau code apparaît dans la liste avant de l’utiliser dans l’entrepôt.
5. Pour un emplacement existant, changez sa zone ou **Utilisé**, puis choisissez **Enregistrer** sur sa ligne.

## Et ensuite
Le code d’un emplacement ne peut plus être modifié après sa création. Sa zone et sa disponibilité peuvent changer sans le renommer. Faites correspondre les étiquettes physiques au code enregistré. Ajouter un emplacement n’y déplace aucun colis et n’imprime pas d’étiquette d’étagère depuis cette page ; le rangement est une tâche distincte.