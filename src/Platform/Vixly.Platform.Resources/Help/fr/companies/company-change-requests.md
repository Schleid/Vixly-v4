---
slug: company-change-requests
title: Examiner les demandes de modification d’entreprise
summary: Comparez les coordonnées juridiques proposées avec la fiche actuelle et le justificatif.
section: companies
order: 60
permissions: [platform.tenants.review_changes]
conditions: []
routes: [/tenants/changes, /tenants/changes/{RequestId:guid}]
related: [company-page, edit-company-details]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantChangeView.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantChangesOverview.razor
updated: 2026-10-23
review: needs-review
---

Les demandes de modification permettent d’examiner les coordonnées juridiques proposées pour une entreprise. Comparez la demande à la fiche actuelle et au justificatif avant de prendre une décision.

Une explication de refus n’est pas une note de travail privée. Lisez le rôle du champ avant de saisir du texte et donnez une explication claire permettant à l’entreprise d’agir. N’acceptez pas une demande uniquement parce que l’orthographe proposée semble plausible.

## Étapes
1. Ouvrez **Changements d’entreprise**, choisissez l’état pertinent et appliquez le filtre. Ouvrez la demande à examiner.
2. Comparez **Informations actuelles** et **Informations demandées**. Vérifiez l’identité de l’entreprise et le justificatif joint.
3. Pour accepter, renseignez **Note (facultative)** si nécessaire et choisissez **Approuver**. Confirmez votre identité lorsque cela est demandé.
4. Pour refuser, renseignez **Motif du refus (envoyé au client)** et choisissez **Refuser**. Expliquez la correction nécessaire plutôt que de laisser un refus imprécis.

## Et ensuite
Lisez le nouvel état et vérifiez la fiche après approbation. Une demande déjà décidée ne devient pas une nouvelle demande en attente. Si l’écran indique que la fiche a changé, rechargez-la et examinez les informations actuelles avant de décider.