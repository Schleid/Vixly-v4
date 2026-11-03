---
slug: suspend-a-company
title: Suspendre une entreprise
summary: Suspendre une entreprise après examen de son compte et de son état actuel.
section: companies
order: 41
permissions: [platform.tenants.suspend]
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

Suspendez une entreprise active lorsque son compte doit être interrompu. Choisissez la catégorie de suspension adaptée et indiquez une raison interne. La catégorie et l’explication interne ont des rôles différents.

## Étapes
1. Ouvrez la bonne entreprise et consultez **Vue d'ensemble**. Comparez son identité et son état actuel avec la demande traitée.
2. Repérez **Accès et statut** et choisissez **Suspendre**. Le panneau propose uniquement les changements disponibles depuis l’état actuel.
3. Renseignez **Motif**. Choisissez aussi **Catégorie**. Rédigez une explication factuelle adaptée à l’historique interne du compte.
4. Choisissez **Suspendre** dans le formulaire de confirmation. Confirmez votre identité si cela est demandé, puis attendez le résultat avant de quitter la page.

## Et ensuite
L’entreprise devient suspendue. Son avis indique la catégorie de suspension et comment contacter Vixly, sans révéler la raison interne. Vérifiez l’état enregistré avant de répondre à la demande de soutien.

Si l’action est indisponible, rechargez la fiche et vérifiez son état actuel. Utilisez **Annuler** pour quitter une confirmation inachevée sans appliquer le changement.