---
slug: change-company-status
title: Activer une entreprise
summary: Activer une entreprise après examen de son compte et de son état actuel.
section: companies
order: 40
permissions: [platform.tenants.activate]
conditions: []
routes: []
related: [company-page]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantLifecycle.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantMoves.cs
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantLifecyclePresenter.cs
updated: 2026-10-23
review: needs-review
---

Activez une entreprise lorsqu’elle est prête à devenir active dans Vixly. L’action est proposée pour une entreprise en brouillon ou en attente d’activation. Vérifiez le compte concerné avant de changer son état.

## Étapes
1. Ouvrez la bonne entreprise et consultez **Vue d'ensemble**. Comparez son identité et son état actuel avec la demande traitée.
2. Repérez **Accès et statut** et choisissez **Activer**. Le panneau propose uniquement les changements disponibles depuis l’état actuel.
3. Lisez la confirmation affichée avant de continuer. Ce formulaire ne demande pas de raison interne pour l’activation.
4. Choisissez **Activer** dans le formulaire de confirmation. Attendez le résultat avant de quitter la page.

## Et ensuite
L’entreprise devient active après la réussite de l’action. L’activation modifie son état ; elle ne prouve pas que chaque appareil ou compte du personnel a terminé sa configuration.

Si l’action est indisponible, rechargez la fiche et vérifiez son état actuel. Utilisez **Annuler** pour quitter une confirmation inachevée sans appliquer le changement.