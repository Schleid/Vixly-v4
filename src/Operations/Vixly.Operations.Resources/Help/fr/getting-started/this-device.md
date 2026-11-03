---
slug: this-device
title: Utiliser un appareil approuvé
summary: Suivez les demandes d’enregistrement et d’approbation de votre appareil de travail.
section: getting-started
order: 40
permissions: []
conditions: []
routes: []
related: [first-sign-in, your-account, signing-in-from-another-country]
sources:
  - src/Operations/Vixly.Operations.Web/Components/SignIn/DeviceEnrolPage.razor
  - src/Operations/Vixly.Operations.Web/Components/SignIn/DeviceKindField.razor
  - src/Operations/Vixly.Operations.Web/Components/SignIn/DeviceWaitingPage.razor
  - src/Operations/Vixly.Operations.Web/Security/OperationsDeviceGate.cs
updated: 2026-10-25
review: needs-review
---

Travailler pour {brandName} peut nécessiter un appareil approuvé en plus de votre connexion personnelle. La réussite d’une vérification du mot de passe ne signifie pas qu’un nouvel ordinateur est approuvé pour le travail.

Lisez la page d’appareil affichée après la connexion. Elle indique si cet ordinateur doit être enregistré, attend une décision ou ne peut pas accéder à l’espace. Gardez l’appareil avec vous pendant les étapes demandées.

## Étapes
1. Connectez-vous avec votre propre compte sur l’ordinateur que vous souhaitez utiliser.
2. Si l’enregistrement est demandé, renseignez **Nom de cet ordinateur** avec un nom reconnaissable par votre entreprise. Sélectionnez le type d’appareil adapté dans le formulaire.
3. Choisissez **Demander l’ajout de cet ordinateur** et lisez la page suivante. Si une approbation est en attente, attendez la décision plutôt que d’enregistrer plusieurs fois le même ordinateur.
4. Revenez à l’espace lorsque la page vous permet de continuer. Utilisez **Se déconnecter** après une session sur un poste partagé.

## Et ensuite
Un ordinateur approuvé exige toujours la connexion appropriée du personnel. La déconnexion ne supprime pas son approbation. Si l’appareil est bloqué, perdu ou ne vous appartient plus, contactez la personne responsable du soutien aux appareils. Ne partagez pas un compte et n’utilisez pas la connexion d’un collègue pour contourner le message.