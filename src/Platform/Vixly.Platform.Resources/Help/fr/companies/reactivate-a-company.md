---
slug: reactivate-a-company
title: Réactiver une entreprise
summary: Réactiver une entreprise après examen de son compte et de son état actuel.
section: companies
order: 42
permissions: [platform.tenants.reactivate]
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

Réactivez une entreprise suspendue ou désactivée lorsque la cause de son inactivité est résolue. Consultez son historique récent et expliquez pourquoi l’accès peut être rétabli avant de confirmer l’action.

## Étapes
1. Ouvrez la bonne entreprise et consultez **Vue d'ensemble**. Comparez son identité et son état actuel avec la demande traitée.
2. Repérez **Accès et statut** et choisissez **Réactiver**. Le panneau propose uniquement les changements disponibles depuis l’état actuel.
3. Renseignez **Motif**. Rédigez une explication factuelle adaptée à l’historique interne du compte.
4. Choisissez **Réactiver** dans le formulaire de confirmation. Confirmez votre identité si cela est demandé, puis attendez le résultat avant de quitter la page.

## Et ensuite
L’entreprise devient active et une suspension ouverte est clôturée. La réactivation n’efface pas l’historique de la décision précédente. Examinez les questions d’abonnement ou de facturation dans leurs rubriques respectives.

Si l’action est indisponible, rechargez la fiche et vérifiez son état actuel. Utilisez **Annuler** pour quitter une confirmation inachevée sans appliquer le changement.