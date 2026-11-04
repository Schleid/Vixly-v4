---
slug: shipping-services
title: Services d’expédition
summary: Les services déterminent les prestations proposées par votre entreprise et leur présentation.
section: setting-up
order: 70
permissions: [ops.services.view]
conditions: []
routes: [/staff/services]
related: [price-lists, company-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Settings/ShippingServicesPage.razor
updated: 2026-10-31
review: needs-review
---

Les services déterminent les prestations proposées par {brandName} et leur présentation. Vérifiez le mode de transport et les choix existants avant toute modification. La liste fournit des noms de services ; votre entreprise choisit ceux qu’elle propose et les valeurs opérationnelles associées.

## Étapes
1. Ouvrez **Services d’expédition** dans la navigation et lisez l’indication initiale.
2. Vérifiez **Proposé** pour chaque service. Si la modification est possible, sélectionnez les prestations réellement proposées.
3. Vérifiez **Votre libellé (facultatif)** et renseignez le libellé destiné aux clients si nécessaire.
4. Vérifiez **Code étiquette** : exactement trois lettres de A à Z, imprimées en grand sur l’étiquette d’expédition pour que l’entrepôt trie les colis d’un coup d’œil (par exemple VXA pour l’aérien standard, VXE pour l’aérien express, VXS pour le maritime, VXG pour le terrestre). Deux services peuvent partager un code.
5. Consultez **Jours ouvrés minimum** et **Jours ouvrés maximum** pour la plage de délais prévue.
6. Vérifiez le diviseur volumétrique lorsque la page en propose un. Les services par conteneur affichent leur propre explication.
7. Si le bouton apparaît, choisissez **Enregistrer** et lisez le résultat.

## Et ensuite
Rouvrez la page pour confirmer les services et leurs valeurs. Proposer un service ne crée pas automatiquement son tarif ni son calendrier de départ. Configurez les prix correspondants avant d’utiliser un devis. Sans champs modifiables, vous pouvez consulter les choix sans enregistrer de changement. Un nouveau code étiquette apparaît sur les étiquettes imprimées ensuite ; celles déjà posées sur les colis gardent l’ancien.