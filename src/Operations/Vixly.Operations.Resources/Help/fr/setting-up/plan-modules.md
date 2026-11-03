---
slug: plan-modules
title: Ce que comprend votre forfait, et saisir les clients
summary: Certains forfaits laissent de côté des parties de l'espace, comme le portail client ou l'assurance. Ce qui change, et comment saisir un client.
section: setting-up
order: 107
permissions: [ops.customers.manage]
conditions: []
routes: [/staff/customers/new]
related: [plan-shipments, company-settings]
sources:
  - src/Operations/Vixly.Operations.Application/Plans/OperationsPlanModules.cs
  - src/Operations/Vixly.Operations.Web/Components/Customers/CustomerNewPage.razor
updated: 2026-10-30
review: ok
---

Le forfait de votre entreprise décide des parties de l'espace de travail que vous utilisez. Quand une partie n'est pas dans votre forfait, ses pages ne sont pas dans le menu et le comptoir ne la propose pas.

## Parties qu'un forfait peut laisser de côté
- **Portail client :** les clients ne peuvent ni créer de compte, ni se connecter, ni demander d'envoi en ligne. Votre site n'affiche aucun bouton de connexion ou de compte ; il garde la page de suivi.
- **Assurance :** aucun produit d'assurance au comptoir, aucun envoi assuré.
- **Comptes de crédit :** pas de facturation sur compte à la caisse.
- **Fournitures :** pas de fournitures vendues au comptoir.
- **Livraison à domicile :** les envois sont retirés en agence.
- **Service d'assistance et tickets.**
- **Factures fournisseurs et bénéfice.**

## Saisir un client vous-même
Sans portail client, les clients ne s'inscrivent pas eux-mêmes. Le personnel qui gère les clients les ajoute :
1. Ouvrez **Clients** et choisissez **Nouveau client**.
2. Saisissez le prénom et le nom, le nom de l'entreprise s'il y en a un, l'e-mail, le téléphone et la langue.
3. Choisissez **Créer le client**. Le client reçoit un numéro client et il est actif tout de suite.

## Bon à savoir
- L'e-mail est obligatoire et ne doit pas appartenir à un autre client : les avis et les reçus y sont envoyés.
- Quand votre forfait comprend le portail client, les clients s'inscrivent eux-mêmes et **Nouveau client** n'est pas affiché.
- Votre forfait est affiché dans **Facturation**. Pour utiliser une partie que votre forfait laisse de côté, demandez un forfait plus complet.
