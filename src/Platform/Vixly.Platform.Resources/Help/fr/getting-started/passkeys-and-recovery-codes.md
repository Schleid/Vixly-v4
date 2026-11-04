---
slug: passkeys-and-recovery-codes
title: Passkeys et codes de récupération
summary: Gérez vos passkeys et conservez vos nouveaux codes de récupération en sécurité.
section: getting-started
order: 30
permissions: []
conditions: []
routes: []
related: [sign-in-and-second-factor]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Account/MySecondFactors.razor
  - src/Platform/Vixly.Platform.Web/Features/Account/MySecondFactorsPresenter.cs
updated: 2026-10-23
review: needs-review
---

Une passkey permet d’utiliser le mode de connexion proposé par votre appareil ou votre clé de sécurité. Les codes de récupération permettent de retrouver l’accès lorsque votre second facteur habituel est indisponible. Ces moyens appartiennent à votre propre compte.

Préparez un emplacement sûr avant de remplacer les codes. Ne les envoyez jamais dans un message de soutien et ne les laissez pas à la vue d’autres personnes. Un collègue vous aidant à retrouver l’accès ne doit pas vous demander de révéler un code.

## Étapes
1. Ouvrez **Mon compte**, puis repérez **Seconds facteurs**. Vérifiez si votre application d’authentification est activée et combien de codes restent inutilisés.
2. Pour ajouter une passkey, renseignez **Nom de la nouvelle clé d'accès (par exemple, Ordinateur du bureau)** et choisissez **Ajouter une clé d'accès**. Suivez les instructions du navigateur ou de l’appareil. Donnez un nom reconnaissable.
3. Pour supprimer une passkey, choisissez **Supprimer** à côté de son nom. Confirmez votre identité si nécessaire.
4. Choisissez **Obtenir de nouveaux codes de récupération** pour remplacer vos codes. Conservez les nouveaux codes en sécurité avant de choisir **Je les ai enregistrés**.

## Et ensuite
Les nouveaux codes remplacent l’ensemble précédent. Gardez donc le nouvel ensemble plutôt qu’une ancienne copie. Si le navigateur ne prend pas les passkeys en charge, lisez le message affiché et utilisez vos autres moyens de connexion.