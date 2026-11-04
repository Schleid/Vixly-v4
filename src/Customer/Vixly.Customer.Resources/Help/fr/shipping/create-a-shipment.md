---
slug: create-a-shipment
title: Créer une expédition
summary: Une expédition indique à votre entreprise où envoyer vos marchandises après leur arrivée à l’adresse de réception.
section: shipping
order: 30
permissions: []
conditions: []
routes: [/customer/packages/shipments/new]
related: [your-mailbox-address, announce-a-package]
sources:
  - src/Customer/Vixly.Customer.Web/Components/Portal/ShipmentFormPage.razor
  - src/Customer/Vixly.Customer.Web/Components/Portal/ShipmentDestinationFields.razor
  - src/Customer/Vixly.Customer.Web/Portal/CustomerShipmentEndpoints.cs
updated: 2026-10-27
review: needs-review
signed_out: false
---

Une expédition indique à {brandName} où envoyer vos marchandises après leur arrivée à l’adresse de réception. Elle est distincte de l’annonce d’un colis. Annoncez les nouveaux colis sur leur page ; le formulaire d’expédition permet de sélectionner les colis disponibles sans ressaisir leur suivi.

## Étapes
1. Ouvrez **Mes colis** et choisissez **Nouvel envoi**.
2. Choisissez la destination affichée. Sélectionnez retrait ou livraison à domicile si proposés. Pour le domicile, renseignez l’adresse du destinataire, pas celle de réception des achats.
3. Vérifiez **Service** si affiché. Dans **Nombre de colis**, indiquez le nombre de colis attendus dans cette expédition. La valeur prévue aide l’estimation ; ce n’est pas un paiement.
4. Vérifiez **Destinataire** et **Téléphone du destinataire (avec l'indicatif, par exemple +509)**. Il s’agit de la personne recevant les marchandises à destination finale.
5. Examinez les options. Choisissez une assurance après avoir lu les conditions et complété l’acceptation demandée.
6. Dans **Colis à y mettre**, sélectionnez les colis annoncés ou reçus concernés, s’ils apparaissent.
7. Vérifiez destination et colis puis choisissez **Créer l'envoi**.

## Et ensuite
Ouvrez l’expédition enregistrée et vérifiez ses détails. Sa création ne signifie pas que les marchandises sont parties, arrivées ou payées. Si un colis n’apparaît pas, vérifiez son annonce et son éventuelle affectation ailleurs avant de créer une annonce supplémentaire.

## Sur cet écran
1. Choisissez destination finale et mode de remise.
2. Indiquez le nombre de colis attendus.
3. Vérifiez nom et téléphone du destinataire.
4. Examinez les options.
5. Sélectionnez les colis de cette expédition.
6. Créez après vérification des instructions.

[![Créer une expédition](/customer/help/media/v2/fr-shipment.png)](/customer/help/media/v2/fr-shipment.png)

Exemple fictif. Utilisez les coordonnées de votre propre compte. Sélectionnez l’image pour l’agrandir.