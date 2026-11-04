---
slug: company-notes
title: Ajouter une note interne à une entreprise
summary: Laissez une note factuelle aux autres membres du personnel Vixly.
section: companies
order: 35
permissions: [platform.tenants.add_notes]
conditions: []
routes: []
related: [company-page]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantNotes.razor
  - docs/platform/SLICE-2-TENANTS-DESIGN.md
updated: 2026-10-23
review: needs-review
---

Les notes internes donnent au personnel de Vixly le contexte nécessaire pour poursuivre le travail sur un compte. Utilisez-les pour enregistrer le résultat d’une intervention, un suivi convenu ou une correction. Elles restent dans Control et ne sont pas des messages à l’entreprise.

Les notes forment un historique. Corrigez une erreur en ajoutant une nouvelle note expliquant la correction. Ne comptez pas remplacer une entrée précédente et n’y mettez pas de mots de passe, de codes de récupération ou de données de carte.

## Étapes
1. Ouvrez l’entreprise concernée et vérifiez son identité avant de saisir des informations sur le compte.
2. Repérez **Notes internes**. Lisez les dernières entrées pour apporter un contexte utile plutôt que répéter une question non résolue.
3. Renseignez **Nouvelle note**. Expliquez les faits et le travail restant. Distinguez les informations confirmées de celles qui demandent encore une vérification.
4. Choisissez **Ajouter la note**. Attendez le résultat et vérifiez la nouvelle entrée dans la liste.

## Et ensuite
La note enregistrée apparaît avec son auteur et sa date. Les collègues consultant le compte peuvent s’en servir pour poursuivre le travail. Envoyer un courriel ou modifier le compte reste une action distincte ; écrire une note n’effectue aucune de ces tâches.