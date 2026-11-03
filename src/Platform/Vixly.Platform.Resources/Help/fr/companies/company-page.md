---
slug: company-page
title: Consulter la fiche d’une entreprise
summary: Comprenez la présentation de l’entreprise et ses rubriques accessibles.
section: companies
order: 20
permissions: [platform.tenants.view]
conditions: []
routes: [/tenants/{TenantId:guid}]
related: [find-a-company, edit-company-details]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantSummary.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantCompanyFacts.razor
updated: 2026-10-23
review: needs-review
---

La fiche d’une entreprise est le point de départ du soutien lié à son compte Vixly. Elle rassemble la présentation, les contacts et l’historique du compte. Les autres rubriques, comme l’abonnement, la facturation ou les licences, dépendent de vos accès.

Utilisez les liens de rubrique pour parcourir la page sans chercher dans chaque panneau. L’état de l’entreprise, celui de l’abonnement et le solde d’une facture sont des informations différentes. Consultez la bonne rubrique avant de déterminer ce qui demande une intervention.

## Étapes
1. Retrouvez l’entreprise dans **Locataires**, puis ouvrez son nom.
2. Consultez **Vue d'ensemble**. Comparez le code et l’identité de l’entreprise avec la demande traitée.
3. Consultez **Entreprise** et **Contacts** pour comprendre le profil et les contacts enregistrés.
4. Lisez **Notes internes** et **Historique** pour connaître le contexte accessible. Suivez seulement les liens utiles à votre tâche.

## Et ensuite
Consulter une fiche ne change pas l’état de l’entreprise. Chaque action disponible possède sa propre confirmation et son résultat. Distinguez les notes internes des messages destinés à l’entreprise. Si une autre rubrique est inaccessible, demandez au collègue responsable de la traiter avec son propre compte.