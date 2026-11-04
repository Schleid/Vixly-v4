---
slug: customer-reviews
title: Avis des clients : demander, publier ou masquer
summary: Laissez les clients donner leur avis sur {brandName} dans le portail client, puis choisissez les avis affichés sur votre site web.
section: setting-up
order: 109
permissions: [ops.brand.edit]
conditions: []
routes: []
related: [website-marketing, ticket-desk]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Editor/SiteTestimonialsPage.razor
  - src/Customer/Vixly.Customer.Web/Components/Portal/ReviewPage.razor
updated: 2026-11-03
review: ok
---

Les clients qui ont utilisé votre service peuvent donner leur avis sur {brandName} dans le portail client. Vous décidez, avis par avis, de ce qui apparaît sur votre site web. Rien n'est publié avant que vous le publiiez, et vous ne modifiez jamais les mots d'un client.

## Avant de commencer
Votre forfait comprend le portail client.

## Étapes
1. Ouvrez *Votre site web* et choisissez **Témoignages clients**.
2. Cochez **Demander l'avis des clients dans le portail client** et choisissez **Enregistrer**. Les clients dont un envoi a été livré ou retiré voient alors une invitation sur leur page d'accueil et une page d'avis dans le menu.
3. Choisissez **Nouveaux** pour voir les avis qui vous attendent. Chacun montre les étoiles, les mots, le nom tel que le client a choisi de l'afficher (par exemple « Marie P. ») et la date.
4. Pour afficher un avis sur votre site web, choisissez **Publier**. Un client qui n'a pas accepté d'être cité ne peut pas être publié : son avis reste un commentaire privé pour {brandName}.
5. Pour garder un avis hors du site web, choisissez une raison dans **Raison (facultatif)** puis **Masquer**. Le client ne voit jamais la raison.
6. Utilisez les flèches pour changer l'ordre des avis publiés. Jusqu'à six apparaissent sur la page d'accueil.
7. Pour une mauvaise expérience, choisissez **Ouvrir un ticket**. Le service client reçoit un ticket sur le dernier envoi terminé du client.

## Ce qui se passe ensuite
- Un client peut modifier son avis à tout moment. Il revient dans **Nouveaux** et quitte le site web jusqu'à ce que vous le publiiez à nouveau.
- Un client peut supprimer son avis. Il quitte le site web immédiatement.
- Désactiver la demande masque la page du portail. Les avis déjà envoyés restent dans votre liste.

## Bon à savoir
- Choisissez les avis honnêtement. Les règles américaines sur les avis interdisent de masquer les avis négatifs en laissant croire que ceux affichés sont tous les avis. Votre page d'accueil les présente comme « Ce que disent nos clients », sans note moyenne ni nombre d'avis : elle ne le prétend jamais.
- Indiquez une raison quand vous masquez un avis. Masquez-en un pour les mêmes raisons, quelles que soient ses étoiles : informations personnelles, propos abusifs, sans rapport avec votre service ou faux présumé.
