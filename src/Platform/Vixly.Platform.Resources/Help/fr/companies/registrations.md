---
slug: registrations
title: Suivre les inscriptions
summary: Consultez l’avancement d’une inscription en libre-service.
section: companies
order: 50
permissions: [platform.tenants.view]
conditions: []
routes: [/tenants/registrations, /tenants/registrations/{RegistrationId:guid}]
related: [find-a-company]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Registrations/RegistrationList.razor
  - src/Platform/Vixly.Platform.Web/Features/Registrations/RegistrationSummary.razor
updated: 2026-10-23
review: needs-review
---

La liste des inscriptions montre les entreprises ayant commencé l’inscription en libre-service. Utilisez-la pour comprendre l’étape atteinte avant de demander au propriétaire de recommencer.

Une inscription et un compte d’entreprise établi sont des fiches différentes. Certaines inscriptions attendent encore la confirmation du courriel ou le paiement. Une inscription terminée peut contenir un lien vers l’entreprise créée. Lisez l’état actuel plutôt que de supposer que chaque entrée peut déjà utiliser son espace.

## Étapes
1. Ouvrez **Inscriptions** et retrouvez l’inscription concernée.
2. Ouvrez le nom de l’entreprise et consultez **Inscription**. Comparez le nom de son adresse et les coordonnées du propriétaire avec la demande.
3. Consultez les dates de **Commencée**, **Dernier lien envoyé** et **E-mail vérifié**. Elles distinguent un message envoyé d’une confirmation terminée.
4. Vérifiez **Consentement** et l’état actuel. Si un lien d’entreprise apparaît, utilisez **Ouvrir le locataire** pour consulter le compte créé.

## Et ensuite
Consulter l’inscription ne renvoie pas de courriel, n’annule pas l’inscription et ne confirme pas un paiement. Ces actions sont distinctes. Conservez le parcours existant lorsqu’il reste valide et expliquez ce qui manque à partir de l’étape enregistrée.