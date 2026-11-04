---
slug: price-lists
title: Tarifs et versions
summary: Les tarifs donnent à votre entreprise les prix du transport pour un service, un trajet et éventuellement un client précis.
section: setting-up
order: 80
permissions: [ops.charges.configure]
conditions: []
routes: [/staff/charges]
related: [zones-and-routes, extras-and-item-types, publish-prices]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/PriceListPanel.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/PriceVersionFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/VersionHistory.razor
updated: 2026-10-25
review: needs-review
---

Les tarifs donnent à {brandName} les prix du transport pour un service, un trajet et éventuellement un client précis. Chaque tarif conserve des versions datées. Vérifiez la devise et les unités de l’entreprise avant toute saisie, puis choisissez le bon mode de transport.

## Étapes
1. Ouvrez **Prix** et choisissez le mode.
2. Ouvrez un tarif existant ou choisissez **Ajouter une liste de prix** puis **Service**.
3. Vérifiez l’origine et la destination. Renseignez **Code client (vide : prix standard)** uniquement pour un tarif propre à un client.
4. Saisissez les tranches, prix par article ou frais terrestres applicables. Vérifiez minimum, frais de service et regroupement.
5. Définissez **Commence le**, vérifiez **En usage**, puis choisissez **Enregistrer** pour un tarif existant ou **Ajouter une liste de prix** pour un nouveau.

## Et ensuite
Vérifiez l’historique après l’enregistrement. Une version déjà commencée est en lecture seule ; créez une nouvelle version datée pour changer les prix. Une version future peut être retirée avant son début. Confirmez la date avant d’utiliser le tarif dans un devis. Publier les prix est un choix distinct de leur enregistrement.