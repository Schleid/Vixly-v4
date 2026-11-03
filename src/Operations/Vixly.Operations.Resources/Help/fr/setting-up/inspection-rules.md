---
slug: inspection-rules
title: Règles d’inspection et articles restreints
summary: La configuration définit les contrôles et indications sur les articles restreints de votre entreprise.
section: setting-up
order: 130
permissions: [ops.inspection.configure]
conditions: []
routes: [/staff/inspection/setup]
related: [shipping-services, branches-and-warehouses]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Inspection/InspectionSetupPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Inspection/RestrictedItemFields.razor
updated: 2026-10-25
review: needs-review
---

La configuration définit les contrôles et indications sur les articles restreints de {brandName}. Examinez ces choix avec les responsables de réception et de conformité. Une liste initiale sert de base à votre examen ; elle ne confirme pas la couverture de toutes les exigences des destinations et transporteurs.

## Étapes
1. Ouvrez **Règles d'inspection** et lisez l’explication initiale.
2. Consultez **Part aléatoire**. Saisissez la part choisie dans **Colis sur cent** puis **Enregistrer**.
3. Dans **Règles**, choisissez type et valeur, puis **Ajouter la règle**. Examinez les règles avant de changer leur activation.
4. Consultez **Articles restreints**. Si une liste initiale est proposée, lisez son explication avant **Partir de la liste proposée**.
5. Vérifiez nom, mots de correspondance, restriction, destinations et transporteurs avant chaque enregistrement.

## Et ensuite
Vérifiez les règles et articles obtenus. Ces paramètres orientent le travail ; ils n’inspectent aucun colis, ne résolvent aucun blocage existant et ne certifient pas qu’une marchandise peut voyager. Le personnel doit toujours enregistrer le contrôle réel et suivre les instructions de l’entreprise sur les articles restreints.