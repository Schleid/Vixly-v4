---
slug: support-calls
title: Appels clients et comptes externes
summary: Laissez un centre d'appels répondre aux clients sur un envoi à la fois, sans voir leurs informations.
section: setting-up
order: 70
permissions: [ops.support.lookup, ops.staff.edit, ops.customers.manage]
conditions: []
routes: [/staff/support, /staff/support/requests]
related: [branches-and-warehouses, ticket-desk]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Support/SupportPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Staff/StaffMemberPage.razor
updated: 2026-10-29
review: needs-review
---

Un agent de support répond aux appelants sur un envoi ou un colis à la fois. Il ne voit jamais de liste de clients, d'adresse complète, de numéro de téléphone complet ni d'e-mail, et il ne modifie rien : il ouvre un ticket d'appel pour votre équipe.

## Configurer un agent
1. Invitez l'agent comme tout membre du personnel et donnez-lui le rôle **Agent de support** pour toute l'entreprise.
2. Ouvrez la personne et, sous **Compte externe**, cochez **C'est un compte externe**.
3. Choisissez la **Date de fin** du contrat, les **Jours de travail** et les heures **De** et **À** dans le fuseau horaire de votre entreprise. Sélectionnez **Enregistrer**.

Un compte externe ne peut utiliser que les **Appels clients**, seulement ses jours et heures, et jusqu'à sa date de fin. En dehors, il ne peut pas se connecter et une session ouverte cesse de fonctionner.

## Prendre un appel
1. Ouvrez **Appels clients** et saisissez le numéro de téléphone complet de l'appelant. Sélectionnez **Trouver l'appelant**. Vous ne voyez que son prénom, l'initiale de son nom, son code et s'il est en règle.
2. Demandez le numéro de suivi : celui de votre entreprise, ou celui du transporteur (Amazon, UPS, USPS...) pour un colis. Sélectionnez **Ouvrir**.
3. Il ne s'ouvre que si l'envoi ou le colis est celui de l'appelant, comme expéditeur ou destinataire. Vous voyez où il se trouve, les blocages, la destination et le montant dû.
4. Si l'appelant demande un changement, écrivez-le et sélectionnez **Ouvrir un ticket d'appel**.

Un appelant sans compte, comme un destinataire, n'obtient que le statut, avec le numéro de suivi de votre entreprise.

## Et ensuite
L'agent choisit le sujet du ticket ; le service qui traite ce sujet le voit dans **Tickets** (voir *Le bureau des tickets*). Chaque recherche par téléphone ou numéro de suivi est enregistrée dans le journal d'audit, et un agent peut en faire au plus 60 par heure.
