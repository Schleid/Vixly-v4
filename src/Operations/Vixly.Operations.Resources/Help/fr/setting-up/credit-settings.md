---
slug: credit-settings
title: Paramètres des comptes de crédit
summary: Les paramètres définissent les choix communs lorsque votre entreprise propose du crédit.
section: setting-up
order: 160
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/credit]
related: [company-settings, invoice-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Credit/CreditSettingsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Settings/SettingsPage.razor
updated: 2026-10-25
review: needs-review
---

Les paramètres définissent les choix communs lorsque {brandName} propose du crédit. Ils sont distincts de la limite et du solde impayé d’un client. Vérifiez les choix existants avant modification, notamment les délais de paiement et le comportement en cas de dépassement d’une limite.

## Étapes
1. Ouvrez **Paramètres du crédit** dans la navigation.
2. Lisez tout avis indiquant que le crédit est désactivé. Son activation générale se consulte dans les paramètres de l’entreprise.
3. Vérifiez **Conditions (jours)** pour le délai par défaut.
4. Dans **Quand une charge dépasse la limite**, lisez chaque règle et sélectionnez celle prévue si la modification est possible.
5. Vérifiez **Prévenir chaque mois les clients que leur relevé est prêt**. Choisissez **Enregistrer** si le bouton est proposé puis lisez le résultat.

## Et ensuite
Rouvrez la page pour confirmer les choix enregistrés. Ces règles n’accordent pas automatiquement du crédit à tous les clients et ne changent pas une limite individuelle. Consultez séparément le crédit du client concerné. Un formulaire en lecture seule permet la vérification sans modification ; l’absence de bouton d’enregistrement ne signifie pas un échec de consultation.