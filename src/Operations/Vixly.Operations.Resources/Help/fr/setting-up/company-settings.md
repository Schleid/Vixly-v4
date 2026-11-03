---
slug: company-settings
title: Paramètres de l’entreprise
summary: Les paramètres définissent comment votre entreprise enregistre les montants, les mesures et le travail quotidien.
section: setting-up
order: 20
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings]
related: [invoice-settings, payment-settings, shipping-services]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Settings/SettingsPage.razor
updated: 2026-10-25
review: needs-review
---

Les paramètres définissent comment {brandName} enregistre les montants, les mesures et le travail quotidien. Ces choix concernent plusieurs écrans : vérifiez les valeurs actuelles avant toute modification. Certains lecteurs peuvent consulter sans modifier ; utilisez les champs et le bouton d’enregistrement effectivement disponibles.

## Étapes
1. Ouvrez **Paramètres** dans la navigation.
2. Consultez **Devise de facturation**. Lisez l’avis associé : la devise est verrouillée dès que des factures existent.
3. Vérifiez **Poids**, **Dimensions** et **Fuseau horaire** selon les pratiques de l’entreprise.
4. Examinez les choix de paiement à destination, crédit, prix publics et livraison. Lisez les exigences de connexion avant de les modifier.
5. Vérifiez les préfixes dans **Numérotation**. Si la page permet la modification, choisissez **Enregistrer**.

## Et ensuite
Lisez le résultat puis rouvrez la page pour confirmer les choix enregistrés. Un paramètre enregistré ne crée pas de facture, ne réceptionne pas de colis et ne configure pas un prestataire de paiement. Utilisez les pages de facturation et de paiement pour leurs règles détaillées.