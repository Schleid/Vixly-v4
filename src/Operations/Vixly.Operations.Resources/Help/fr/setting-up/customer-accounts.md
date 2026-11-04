---
slug: customer-accounts
title: Suspendre, réactiver, fermer ou bannir un client
summary: Quand un client ne respecte pas vos conditions ou veut partir : ce que fait chaque action, qui peut la faire, et ce que le client apprend.
section: setting-up
order: 109
permissions: [ops.customers.manage]
conditions: []
routes: [/staff/customers]
related: [plan-modules]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Customers/CustomerStatusPanel.razor
  - src/Operations/Vixly.Operations.Domain/Customers/OperationsCustomer.cs
updated: 2026-10-31
review: ok
---

Ouvrez le client depuis **Clients**. En bas de la page, choisissez le changement et donnez une raison. La raison reste dans l'historique du client ; le client reçoit un e-mail de {brandName} sans elle.

## Les actions
- **Suspendre** : le client ne peut ni se connecter ni envoyer de nouveaux colis. Vous pouvez choisir **Réactiver automatiquement le (facultatif)** : le compte redevient actif tout seul ce jour-là. Suspendez de nouveau pour changer le jour.
- **Réactiver** : met fin à une suspension, ou rouvre un compte fermé.
- **Fermer le compte** : pour un client qui part ou demande la fermeture. Il peut être rouvert plus tard.
- **Bannir (non-respect des conditions, définitif)** : le compte ne peut jamais être rouvert, et l'e-mail et le téléphone du client ne peuvent pas ouvrir un nouveau compte chez vous.

Seuls le propriétaire et les administrateurs peuvent bannir un client ou rouvrir un compte fermé. Ceux qui gèrent les clients peuvent suspendre, réactiver et fermer.

## Ce qui arrive au travail en cours
Les colis déjà reçus et les envois déjà payés continuent et peuvent toujours être retirés. Un client qui n'est pas actif ne peut pas être facturé sur compte : il paie avant que ses marchandises soient remises.

## Bon à savoir
- La page du client liste aussi ses derniers envois, avec l'état de chacun.
- Les comptes de crédit s'ouvrent et se modifient depuis **Crédit**, pas depuis la page du client.
