---
slug: signing-in-from-another-country
title: Vous connecter depuis un autre pays
summary: Comprenez les vérifications supplémentaires et les messages de blocage d’appareil.
section: getting-started
order: 50
permissions: []
conditions: []
routes: []
related: [this-device, your-account, first-sign-in]
sources:
  - src/Operations/Vixly.Operations.Web/Components/SignIn/VerifyPage.razor
  - src/Operations/Vixly.Operations.Web/Components/SignIn/PasswordCheckPage.razor
  - src/Operations/Vixly.Operations.Web/Components/SignIn/SignInPage.razor
  - src/Operations/Vixly.Operations.Web/Security/OperationsDeviceGate.cs
updated: 2026-10-25
review: needs-review
---

Lorsque vous travaillez depuis un autre pays, {brandName} peut vous demander une nouvelle connexion ou une vérification supplémentaire. Lisez le message de la page actuelle plutôt que de supposer que votre session précédente reste valable.

Un appareil peut également être bloqué à l’entrée de l’espace. La confirmation du compte et la décision concernant l’appareil sont distinctes. Terminer l’une ne garantit pas la levée de l’autre restriction.

## Étapes
1. Revenez à la page de connexion du personnel et utilisez votre propre compte. Vérifiez le nom de l’entreprise avant toute saisie.
2. Si la page demande un code d’authentification, utilisez votre propre application et suivez les instructions. Si elle demande votre mot de passe, effectuez plutôt cette vérification.
3. Lisez le résultat. Si l’appareil est bloqué ou ne peut pas continuer, contactez le responsable du soutien aux appareils et expliquez le message affiché.
4. Continuez uniquement lorsque la page le permet. Gardez vos codes de récupération et vos liens privés pendant votre demande d’aide.

## Et ensuite
Vous pouvez retrouver l’espace après les vérifications ou avoir besoin de l’assistance de l’entreprise pour l’appareil. N’enregistrez pas plusieurs appareils de remplacement, ne partagez pas un autre compte et ne promettez pas à un client que l’accès est rétabli avant de pouvoir ouvrir votre écran de travail.