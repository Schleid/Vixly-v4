---
slug: announce-a-package
title: Annoncer un colis
summary: Annoncer un colis indique à votre entreprise ce que vous avez acheté et quelle livraison attendre.
section: your-packages
order: 20
permissions: []
conditions: []
routes: [/customer/packages/new]
related: [your-mailbox-address, create-a-shipment]
sources:
  - src/Customer/Vixly.Customer.Web/Components/Portal/PreAlertFormPage.razor
  - src/Customer/Vixly.Customer.Web/Components/Portal/PackageFields.razor
  - src/Customer/Vixly.Customer.Web/Portal/CustomerPackageEndpoints.cs
updated: 2026-10-27
review: needs-review
signed_out: false
---

Annoncer un colis indique à {brandName} ce que vous avez acheté et quelle livraison attendre. Cela n’envoie pas le colis et ne crée pas ses instructions d’expédition. Préparez le numéro de suivi du transporteur ; le numéro de commande de la boutique est différent.

## Étapes
1. Ouvrez **Mes colis**, puis choisissez **Annoncer un colis**.
2. Renseignez **Transporteur** : le transporteur livrant à l’adresse de réception. Dans **Boutique d’achat**, indiquez la boutique d’achat, pas une adresse de destination.
3. Copiez **Numéro de suivi** depuis le transporteur ou l’avis d’envoi de la boutique. Utilisez le suivi de ce colis, pas le numéro de commande de tout l’achat.
4. Décrivez les marchandises dans **Contenu**. Renseignez **Valeur** et la date prévue si vous la connaissez.
5. Si une expédition existante est proposée, choisissez-la uniquement si ces marchandises lui appartiennent. Vous pouvez aussi choisir plus tard.
6. Vérifiez les informations puis choisissez **Annoncer**.

## Et ensuite
Lisez la confirmation et vérifiez la liste. Une annonce ne prouve pas que l’entrepôt a reçu les marchandises. Si la boutique répartit une commande en plusieurs colis avec des suivis différents, annoncez chaque colis séparément. Créez ou choisissez ensuite l’expédition vers la destination finale.

## Sur cet écran
1. Le transporteur et la boutique d’achat sont distincts.
2. Saisissez le numéro de suivi du transporteur.
3. Décrivez les marchandises réelles.
4. Envoyez après avoir vérifié tous les champs.

[![Annoncer un colis](/customer/help/media/v2/fr-announce.png)](/customer/help/media/v2/fr-announce.png)

Exemple fictif. Utilisez les coordonnées de votre propre compte. Sélectionnez l’image pour l’agrandir.