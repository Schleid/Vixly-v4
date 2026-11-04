---
slug: plan-shipments
title: Les expéditions de votre forfait chaque mois
summary: Certains forfaits comprennent des expéditions par mois. Où vous en êtes, le prix des expéditions en plus et quand les nouvelles s'arrêtent.
section: setting-up
order: 106
permissions: [ops.settings.edit]
conditions: []
routes: [/staff/billing]
related: [company-settings]
sources:
  - src/Operations/Vixly.Operations.Application/Plans/OperationsShipmentCap.cs
  - src/Operations/Vixly.Operations.Web/Components/SignIn/WorkspacePage.razor
updated: 2026-10-30
review: ok
---

Certains forfaits comprennent un nombre d'expéditions par mois civil, comptées dans le fuseau horaire de {brandName}. Toutes les expéditions comptent : celles créées au comptoir, par les clients dans le portail, et pour les colis qui arrivent à votre entrepôt. Les expéditions annulées ne comptent pas.

## Ce qui se passe au cours du mois
1. *À 80 %* des expéditions de votre forfait, l'espace de travail affiche un avis et le propriétaire reçoit un courriel.
2. *Au-delà de 100 %*, le travail continue. Chaque expédition supplémentaire est facturée au prix par expédition supplémentaire de votre forfait, sur votre prochaine facture.
3. *À 125 %*, un dernier avertissement : les nouvelles expéditions s'arrêtent à 150 %, sauf si vous changez de forfait.
4. *Au-delà de 150 %*, les nouvelles expéditions au comptoir et dans le portail s'arrêtent jusqu'à un changement de forfait ou la fin du mois. La réception, les paiements, les expéditions groupées et les remises continuent, et les colis qui arrivent reçoivent toujours leur expédition.

## Le mois de grâce
La première fois que votre entreprise dépasse 150 %, les nouvelles expéditions ne sont pas arrêtées ce mois-là. Les expéditions supplémentaires restent facturées. Cela n'arrive qu'une seule fois.

## Bon à savoir
- Seules les personnes qui gèrent les paramètres de l'entreprise voient l'avis dans l'espace de travail.
- Les clients qui essaient de demander une expédition dans le portail pendant l'arrêt sont invités à vous contacter ; votre forfait ne leur est pas mentionné.
- Un nouveau mois remet le compte à zéro.
- Votre forfait figure dans **Facturation**. Pour augmenter votre limite, demandez un forfait supérieur.
