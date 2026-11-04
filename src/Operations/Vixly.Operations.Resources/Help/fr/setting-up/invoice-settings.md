---
slug: invoice-settings
title: Paramètres des factures
summary: Les paramètres définissent la présentation des taxes et les notes de bas de facture de votre entreprise.
section: setting-up
order: 140
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/invoices]
related: [company-settings, payment-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Invoices/InvoiceSettingsPage.razor
updated: 2026-10-25
review: needs-review
---

Les paramètres définissent la présentation des taxes et les notes de bas de facture de {brandName}. Vérifiez les textes et choix approuvés par l’entreprise avant modification. La page peut être consultable sans champs modifiables ; enregistrez uniquement si le formulaire propose cette possibilité.

## Étapes
1. Ouvrez **Factures et taxes** et consultez **Taxe**.
2. Vérifiez **Nom de la taxe sur les factures** et **Numéro fiscal de l’entreprise** selon les documents de l’entreprise.
3. Vérifiez chaque taux et son indication. Un taux nul signifie aucune taxe pour ce type ; frais gouvernementaux et fournitures non taxées sont exclus.
4. Dans **Taxes sur la facture**, choisissez **Une seule ligne de taxe** ou **Une ligne de taxe par type** lorsque la modification est possible.
5. Vérifiez les notes anglaise, française et espagnole dans **Note en bas de chaque facture**. Choisissez **Enregistrer** si disponible.

## Et ensuite
Lisez le résultat puis rouvrez la page pour confirmer les choix. Les notes figurent au bas des factures : vérifiez chaque langue au lieu de réunir toutes les traductions dans un champ. La numérotation appartient aux paramètres de l’entreprise. Ce formulaire n’émet aucune facture et n’enregistre aucun paiement.