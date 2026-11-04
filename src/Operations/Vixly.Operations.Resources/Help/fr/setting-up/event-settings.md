---
slug: event-settings
title: Causes d’événements et notifications
summary: Les paramètres fournissent à votre entreprise des causes cohérentes pour les retards et incidents ainsi que des choix de notification.
section: setting-up
order: 180
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/events]
related: [company-settings, pickup-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Events/EventSettingsPage.razor
updated: 2026-10-25
review: needs-review
---

Les paramètres fournissent à {brandName} des causes cohérentes pour les retards et incidents ainsi que des choix de notification. Vérifiez les textes destinés aux clients avant modification. Configurer une cause la rend disponible pour le travail futur ; cela n’enregistre aucun événement sur une expédition.

## Étapes
1. Ouvrez **Paramètres des événements** et consultez les notifications actuelles.
2. Vérifiez **Prévenir les clients par e-mail des retards et incidents (un e-mail par envoi)** et **Envoyer un e-mail aux clients quand un envoi est prêt à être retiré** si la modification est disponible.
3. Examinez les causes standards de chaque type. Conservez celles que le personnel doit utiliser.
4. Dans **Causes propres à l’entreprise**, vérifiez les causes personnalisées et leur activation.
5. Si **Ajouter une cause** est disponible, choisissez le type d’événement, complétez les textes localisés puis ajoutez la cause. Utilisez **Enregistrer** pour un formulaire existant.

## Et ensuite
Rouvrez la page et confirmez causes et notifications. Vérifiez tous les champs de langue afin que les clients reçoivent un texte compréhensible. Désactiver une cause n’enregistre aucun événement opérationnel. Le personnel doit encore choisir le motif approprié lorsqu’il enregistre un retard ou incident réel concernant une expédition.