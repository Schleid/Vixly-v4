---
slug: superuser-actions
title: Responsabilités du SuperUser
summary: Comprenez les décisions exceptionnelles réservées au SuperUser.
section: team-and-security
order: 90
permissions: [superuser]
conditions: []
routes: []
related: [company-page, sign-in-and-second-factor]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantLifecycle.razor
  - src/Platform/Vixly.Platform.Web/Features/Users/UserInviteForm.razor
  - src/Platform/Vixly.Platform.Web/Features/Users/SignInSecurityPanel.razor
  - src/Platform/Vixly.Platform.Application/Security/PlatformPermissions.cs
updated: 2026-10-23
review: needs-review
---

Le SuperUser assume des responsabilités exceptionnelles dans Vixly Control. L’attribution d’un rôle ne transforme pas un membre ordinaire du personnel en SuperUser. Utilisez votre propre compte et gardez une raison claire pour les décisions qui touchent les accès.

Le bannissement d’une entreprise est définitif. Il se distingue de la suspension ou de la désactivation, qui possèdent leur propre aide. Ne l’utilisez pas pour une interruption temporaire.

## Étapes
1. Avant de bannir une entreprise, ouvrez sa fiche et vérifiez son identité, son historique et la raison de la décision. Repérez **Accès et statut**.
2. Choisissez **Bannir**, renseignez **Motif** et lisez la confirmation. Choisissez de nouveau l’action et confirmez votre identité si nécessaire.
3. Lors d’une invitation, vérifiez **Niveau d'autorité** avant l’envoi. Accordez uniquement le niveau prévu ; le choix d’un rôle constitue une responsabilité distincte.
4. Examinez les changements de politique de connexion dans leur panneau de paramètres. Lisez l’explication et confirmez votre identité avant de les appliquer.

## Et ensuite
Une entreprise bannie ne peut pas redevenir active par la réactivation ordinaire. La raison interne appartient à l’historique du compte. Le niveau choisi lors d’une invitation concerne le compte invité ; il ne modifie pas un autre compte existant.