---
slug: pay-to-start
title: Payer votre première facture
summary: Payez la première facture si nécessaire et suivez sa confirmation.
section: sign-up
order: 80
permissions: []
conditions: []
routes: [/en/register/pay, /fr/register/pay, /es/register/pay, /register/payment]
related: [pay-by-zelle, save-a-card, after-sign-up]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationPay.razor
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationProgress.razor
  - src/Public/Vixly.Public.Web/Features/Pay/PaymentPanel.razor
  - src/Public/Vixly.Public.Web/Features/Pay/ZellePanel.razor
  - build/HelpCapture/capture.cjs
  - src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json
updated: 2026-10-25
review: needs-review
---

Si votre abonnement exige un paiement initial, l’inscription ouvre la première facture. Lorsque **Nous préparons votre facture. Cette page s’actualise d’elle-même dans quelques secondes.** apparaît, laissez la page s’actualiser pendant sa préparation. Un essai ou une première période sans montant dû peut éviter cette étape.

## Étapes
1. Vérifiez le nom de l’entreprise, **Facture**, **Montant dû** et l’éventuelle **Échéance**. Utilisez le montant et la devise de cette facture.
2. Si la carte est disponible, examinez séparément **Enregistrer cette carte pour les prochaines factures** et le consentement aux paiements automatiques. Enregistrer une carte ne signifie pas autoriser les débits automatiques. Sélectionnez **Continuer vers le paiement** pour accéder au prestataire de paiement.
3. Si Zelle est proposé, effectuez le paiement depuis votre banque avec les coordonnées affichées, puis remplissez **J’ai envoyé un paiement Zelle** sur cette page.
4. Revenez à la page de progression après le paiement et lisez son état avant d’agir à nouveau.

## En cas de problème
**Le paiement a été annulé. Rien n’a été débité.** indique que le paiement a été annulé. Utilisez **Aller au paiement** si vous devez encore payer. Un paiement Zelle déclaré est vérifié avant d’être comptabilisé. Un message de confirmation peut donc apparaître avant que l’espace soit prêt. La page de retour ne prouve pas que tous les paiements sont déjà encaissés : suivez l’état actuel et l’e-mail de reçu.

## Sur cet écran

1 indique l’entreprise et la facture à vérifier ; 2 la rubrique du paiement par carte ; 3 l’action qui mène au paiement hébergé. Aucun numéro de carte n’est saisi sur cet écran.

[![Écran annoté indiquant où agir: Première facture et action de paiement](/help/media/v2/fr-first-invoice.png)](/help/media/v2/fr-first-invoice.png)

Écran illustratif avec des données fictives. Les prix, dates et choix disponibles peuvent varier. Sélectionnez l’image pour l’ouvrir en grand.
