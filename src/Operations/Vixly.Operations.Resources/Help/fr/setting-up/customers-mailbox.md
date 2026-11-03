---
slug: customers-mailbox
title: Choisir l’adresse de réception des clients
summary: L’adresse de réception indique où les clients doivent faire livrer leurs colis pour votre entreprise.
section: setting-up
order: 40
permissions: [ops.warehouses.manage]
conditions: []
routes: []
related: [branches-and-warehouses, shipping-services]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Editor/WarehouseFormPage.razor
  - src/Operations/Vixly.Operations.Application/UseCases/Locations/SaveOperationsWarehouse/SaveOperationsWarehouseUseCase.cs
updated: 2026-10-25
review: needs-review
---

L’adresse de réception indique où les clients doivent faire livrer leurs colis pour {brandName}. Choisissez l’entrepôt avec soin : son adresse doit convenir aux livraisons effectuées pour les clients. L’affichage sur le site est un choix distinct et ne transforme pas automatiquement ce lieu en adresse de réception.

## Étapes
1. Ouvrez la liste des lieux et sélectionnez l’entrepôt destiné aux clients.
2. Vérifiez sa succursale, son adresse complète et ses coordonnées avant de changer ce choix.
3. Consultez **Rôles** et la fonction de réception. Confirmez que le lieu doit recevoir des colis entrants.
4. Vérifiez **Accepte les livraisons actuellement** et sélectionnez **Boîte postale des clients** pour le lieu prévu. Lisez l’indication associée.
5. Choisissez **Enregistrer** et lisez le résultat.

## Et ensuite
Rouvrez l’entrepôt et confirmez le choix enregistré. Un seul entrepôt sert d’adresse de réception des clients ; choisir celui-ci retire ce rôle au précédent. Examinez l’adresse présentée aux clients avant de diffuser des instructions de livraison. Modifier un entrepôt ne redirige pas les colis déjà envoyés à une ancienne adresse. Coordonnez les changements avec le soutien aux clients et l’équipe de réception de votre entreprise.
