---
slug: save-a-card
title: Enregistrer une carte pour les paiements automatiques
summary: Vérifiez l’enregistrement de carte et le consentement aux débits sur le lien de facturation.
section: paying
order: 130
permissions: []
conditions: []
routes: [/cards/{Token}]
related: [pay-an-invoice-link, pay-to-start, contact-sales]
sources:
  - src/Public/Vixly.Public.Web/Components/Pages/SaveCardPage.razor
  - src/Public/Vixly.Public.Web/Features/Pay/PaymentPanel.razor
  - src/Public/Vixly.Public.Web/Resources/PublicText.resx
updated: 2026-10-22
review: needs-review
---

La facturation Vixly peut envoyer un lien pour enregistrer une carte. La page précise qu’aucun montant n’est débité maintenant et ouvre la page du prestataire pour ajouter la carte. Enregistrement et débit automatique sont deux choix distincts.

## Étapes
1. Ouvrez le lien de l’e-mail et vérifiez le nom de l’entreprise. Assurez-vous qu’il s’agit du compte que vous souhaitez modifier.
2. Lisez les informations sous **Enregistrer une carte**. Si une case de consentement aux débits automatiques apparaît, lisez-la avant de la sélectionner. Elle concerne les futures factures Vixly, pas seulement l’enregistrement de la carte.
3. Sélectionnez **Continuer** et remplissez la page du prestataire de paiement. Les coordonnées de carte sont saisies sur cette page, pas dans le formulaire public Vixly.
4. Revenez à la page de résultat et lisez son message. **Merci. Votre carte est en cours d’enregistrement.** indique que la confirmation de l’enregistrement est en cours.

## Ce qui se passe ensuite
Si vous avez accepté les débits automatiques, le consentement affiché précise qu’ils concernent les factures Vixly à leur échéance et peuvent être arrêtés en contactant la facturation. Un paiement de facture peut également proposer **Enregistrer cette carte pour les prochaines factures** ; examinez ce choix séparément des débits automatiques.

**Ce lien a déjà servi à enregistrer une carte.** signifie que ce lien a déjà été utilisé. Contactez la facturation pour modifier la carte plutôt que d’envoyer plusieurs fois l’ancien lien.
