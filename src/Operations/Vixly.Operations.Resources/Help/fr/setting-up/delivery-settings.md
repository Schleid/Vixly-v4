---
slug: delivery-settings
title: Paramètres de livraison
summary: Les paramètres définissent comment votre entreprise enregistre la livraison à domicile, les justificatifs demandés et la suite des tentatives infructueuses.
section: setting-up
order: 200
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/delivery]
related: [pickup-settings, payment-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Delivery/DeliverySettingsPage.razor
updated: 2026-10-25
review: needs-review
---

Les paramètres définissent comment {brandName} enregistre la livraison à domicile, les justificatifs demandés et la suite des tentatives infructueuses. Vérifiez la procédure de l’entreprise avant modification. Ces choix préparent le travail ; ils ne créent pas de tournée, n’affectent pas de chauffeur et n’enregistrent pas de remise.

## Étapes
1. Ouvrez **Paramètres de livraison** et consultez **Comment les livreurs enregistrent leurs arrêts**. Lisez les méthodes avant d’en choisir une si la modification est possible.
2. Vérifiez **Preuve à la porte** et son indication, puis sélectionnez les justificatifs prévus.
3. Dans **Livraisons échouées**, consultez **Tentatives avant d’abandonner** et la suite après ces tentatives.
4. Vérifiez **Paiement**, notamment **Accepter le paiement à la livraison**, et l’explication sur les espèces.
5. Consultez **Prévenir les clients par courriel quand leur envoi part en livraison** puis choisissez **Enregistrer** si disponible.

## Et ensuite
Lisez le résultat et rouvrez le formulaire pour confirmer les choix. Une tentative et une remise réussie sont des résultats différents. Le personnel doit encore enregistrer le résultat réel et ses justificatifs dans le parcours de livraison. Autoriser les espèces ici ne reçoit pas d’argent et ne règle aucune facture.