---
slug: pickup-settings
title: Paramètres du retrait
summary: Les paramètres définissent comment votre entreprise enregistre la remise au comptoir de destination et les frais associés.
section: setting-up
order: 190
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/pickup]
related: [delivery-settings, event-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Pickup/PickupSettingsPage.razor
updated: 2026-10-25
review: needs-review
---

Les paramètres définissent comment {brandName} enregistre la remise au comptoir de destination et les frais associés. Vérifiez les justificatifs exigés avant modification. Les paramètres décrivent les règles de travail ; ils ne remettent pas un colis et ne confirment pas seuls l’identité d’un client.

## Étapes
1. Ouvrez **Paramètres du retrait** et consultez **Preuves demandées au comptoir**. Lisez l’indication avant de choisir les justificatifs si la modification est possible.
2. Vérifiez **Facturer des frais à destination (droits, taxes et frais locaux)** et son explication.
3. Dans **Entreposage à destination**, vérifiez si les frais de stockage à destination sont activés.
4. Consultez **Jours gratuits** et le montant affiché. Utilisez la devise de l’entreprise sans supposer un montant fixe.
5. Choisissez **Enregistrer** si disponible puis lisez le résultat.

## Et ensuite
Rouvrez la page et confirmez les règles avant de les appliquer au comptoir. Les jours gratuits et les frais de stockage sont distincts des justificatifs recueillis à la remise. Modifier cette configuration ne libère aucune expédition, ne règle aucun solde et ne prouve pas qu’un client a reçu ses marchandises.