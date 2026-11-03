---
slug: close-a-company
title: Désactiver une entreprise
summary: Désactiver une entreprise après examen de son compte et de son état actuel.
section: companies
order: 43
permissions: [platform.tenants.deactivate]
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

Désactivez une entreprise lorsque son compte Vixly ne doit plus rester actif. Cette action concerne les entreprises en brouillon, en attente, actives ou suspendues. Elle se distingue d’un bannissement définitif.

## Étapes
1. Ouvrez la bonne entreprise et consultez **Vue d'ensemble**. Comparez son identité et son état actuel avec la demande traitée.
2. Repérez **Accès et statut** et choisissez **Désactiver**. Le panneau propose uniquement les changements disponibles depuis l’état actuel.
3. Renseignez **Motif**. Rédigez une explication factuelle adaptée à l’historique interne du compte.
4. Choisissez **Désactiver** dans le formulaire de confirmation. Confirmez votre identité si cela est demandé, puis attendez le résultat avant de quitter la page.

## Et ensuite
L’entreprise devient désactivée et le changement reste dans son historique. Ne présentez pas cette action comme une suppression des données. Une réactivation ultérieure constitue une décision distincte avec sa propre confirmation.

Si l’action est indisponible, rechargez la fiche et vérifiez son état actuel. Utilisez **Annuler** pour quitter une confirmation inachevée sans appliquer le changement.