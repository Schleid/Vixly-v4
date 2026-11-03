---
slug: edit-company-details
title: Modifier les contacts et paramètres d’une entreprise
summary: Actualisez les contacts, la langue, le fuseau horaire et le suivi du compte.
section: companies
order: 30
permissions: [platform.tenants.edit]
conditions: []
routes: []
related: [company-page, company-change-requests]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantProfileEditor.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantContactsEditor.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantAccountManagement.razor
updated: 2026-10-23
review: needs-review
---

La fiche permet de maintenir les contacts et les paramètres du compte d’une entreprise. Ces modifications sont différentes de l’approbation d’un changement de son identité juridique ou de son adresse.

Les coordonnées juridiques sont verrouillées sur cette page. Pour les modifier, examinez une demande de changement présentée par l’entreprise. Les notes internes et les paramètres fiscaux ont également leurs propres actions ; modifier un contact ne les actualise pas.

## Étapes
1. Ouvrez la bonne entreprise et repérez **Entreprise**. Choisissez **Modifier les paramètres** pour modifier la langue ou le fuseau horaire.
2. Vérifiez les valeurs et choisissez **Enregistrer**. Utilisez **Annuler** pour abandonner la modification.
3. Dans **Contacts**, choisissez **Modifier**. Modifiez le nom, le courriel ou le téléphone facultatif du bon groupe de contacts, puis enregistrez.
4. Dans **Gestion du compte**, choisissez **Modifier** pour modifier **Gestionnaire de compte** ou **Étiquettes**. Vérifiez les saisies avant l’enregistrement.

## Et ensuite
La fiche recharge ses informations après une modification réussie. Vérifiez le résultat affiché avant de passer à une autre rubrique. Utilisez des coordonnées actuelles fournies pour cette entreprise. Ne mettez pas d’explications confidentielles de soutien dans un champ de contact ou une étiquette.