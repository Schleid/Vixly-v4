---
slug: ticket-desk
title: Le bureau des tickets
summary: Chaque ticket va au service qui traite son sujet, quelqu'un le prend en charge, et les responsables sont avertis quand il attend trop longtemps.
section: setting-up
order: 71
permissions: [ops.parcels.view, ops.invoices.view, ops.customers.manage, ops.settings.edit]
conditions: []
routes: [/staff/tickets/]
related: [support-calls]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Tickets/TicketsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Tickets/TicketPage.razor
updated: 2026-10-29
review: needs-review
---

**Tickets** regroupe les tickets de colis manquants des clients et les tickets que les agents de support ouvrent pendant les appels. Chaque ticket a un sujet, et seul le service de ce sujet le voit.

## Qui voit quoi
- **Colis manquant ou erroné :** le personnel de l'entrepôt (qui peut y voir les colis).
- **Facturation et paiement :** la comptabilité (qui peut voir les factures ou encaisser).
- **Retrait, livraison, destinataire :** le service client, et le personnel des envois de l'agence.
- **Dommage ou réclamation :** le service client et l'assurance.
- **Autre :** le service client.

Le propriétaire et les administrateurs voient tous les tickets.

## Traiter un ticket
1. Ouvrez **Tickets**. Filtrez par état (**Nouveaux**, **Pris en charge**, **Signalés**...), sujet, entrepôt ou **Pris par moi**.
2. Ouvrez un ticket et sélectionnez **Je m'en occupe**, ou choisissez un collègue et sélectionnez **Confier**.
3. Un ticket dans le mauvais service : choisissez un autre sujet et sélectionnez **Déplacer**.
4. Répondez, ou **Ajouter une note** sur le ticket d'un appel (le client ne reçoit pas d'e-mail pour ceux-là), puis résolvez-le.

## Quand les responsables sont avertis
Sous **Quand les responsables sont avertis**, indiquez après combien d'heures personne n'a pris un ticket, et après combien de jours il n'est toujours pas résolu. Les responsables du service reçoivent un e-mail à chaque étape ; le second va aussi au propriétaire. Les tickets signalés affichent **Signalé aux responsables** et passent en premier.
