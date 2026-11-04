---
slug: setup-checklist
title: Configurer votre entreprise étape par étape
summary: Consultez la liste de l’accueil pour connaître les tâches de configuration restantes.
section: setting-up
order: 10
permissions: [ops.settings.edit]
conditions: []
routes: []
related: [company-settings, branches-and-warehouses, shipping-services, workspace-at-a-glance]
sources:
  - src/Operations/Vixly.Operations.Web/Components/SignIn/WorkspacePage.razor
  - src/Operations/Vixly.Operations.Application/UseCases/Settings/GetOperationsSetupChecklist/GetOperationsSetupChecklistUseCase.cs
updated: 2026-10-25
review: needs-review
---

La liste de configuration indique les tâches initiales restantes pour {brandName}. Elle apparaît à l’accueil du propriétaire ou d’un administrateur lorsqu’une tâche reste à terminer. Elle donne une vue d’ensemble et ne remplace pas l’examen de chaque écran de configuration.

La liste reflète les informations enregistrées. Un élément terminé signifie que son exigence initiale a été trouvée ; il ne certifie pas que tous les prix, contacts ou choix métier ont été vérifiés.

## Étapes
1. Ouvrez **Accueil** et repérez **Terminer la mise en place** lorsque la liste apparaît.
2. Lisez les éléments en attente. Ils concernent les paramètres de l’entreprise, la première succursale, un entrepôt de réception, les services, le site publié et un membre supplémentaire du personnel.
3. Utilisez le lien d’un élément incomplet pour ouvrir sa page. Examinez les informations existantes avant d’effectuer une modification adaptée.
4. Revenez à l’accueil après l’enregistrement et vérifiez la liste actualisée. Le lien d’aide près du titre ouvre cette présentation.

## Et ensuite
Les éléments terminés portent une coche. Lorsque tous les éléments initiaux sont terminés, la liste n’a plus besoin d’apparaître à l’accueil. La configuration reste accessible par la navigation habituelle. Terminer la liste ne réceptionne pas un colis, n’encaisse pas un paiement et n’approuve pas un appareil ; ces tâches sont distinctes.