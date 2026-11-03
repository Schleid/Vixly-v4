---
slug: company-details
title: Saisir les coordonnées de votre entreprise
summary: Complétez les coordonnées, l’adresse de facturation, le propriétaire et l’adresse de votre espace.
section: sign-up
order: 50
permissions: []
conditions: []
routes: []
related: [sign-up-overview, confirm-your-email, terms-and-privacy]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationStart.razor
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationStartBody.cs
  - src/Public/Vixly.Public.Web/Resources/PublicText.resx
  - build/HelpCapture/capture.cjs
  - src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json
updated: 2026-10-27
review: needs-review
---

Le premier formulaire recueille les informations nécessaires à la création du compte de votre entreprise. Vérifiez soigneusement l’adresse de facturation : le formulaire vous demande de confirmer qu’elle convient aux factures.

## Choisissez votre adresse Vixly permanente

> Choisissez avec soin : votre adresse Vixly est permanente et ne peut pas être modifiée après l’inscription. Vérifiez l’orthographe et l’adresse complète avant de continuer.

C’est l’adresse du site de votre entreprise, pas son adresse postale. Choisissez un nom court et familier ou des initiales reconnaissables. Une partie courte de la raison sociale convient souvent.

| Entreprise | Nom d’adresse | Exemple d’adresse |
|---|---|---|
| Harbour Demo Logistics | hdl | `hdl.vixly.ltd` |
| Harbour Demo Logistics | harbour | `harbour.vixly.ltd` |
| Bluewater Shipping | bluewater | `bluewater.vixly.ltd` |
| Andes Courier | andes | `andes.vixly.ltd` |

Ces exemples sont fictifs et les noms doivent être disponibles. Évitez les mots et chiffres inutiles. Votre équipe utilise l’espace Operations à `/staff` ; les clients utilisent leur portail à `/customer` sur cette adresse d’entreprise.

## Étapes
1. Dans **Entreprise**, renseignez **Raison sociale**, **E-mail de l’entreprise** et **Téléphone de l’entreprise**. Utilisez **Nom commercial (facultatif)** si votre entreprise exerce sous un autre nom.
2. Dans **Adresse**, remplissez **Adresse ligne 1**, **Ville** et **Pays**. Ajoutez **Adresse ligne 2 (facultatif)**, **État ou province** et **Code postal** si nécessaire. Après vérification, cochez **Cette adresse est exacte ; elle figurera sur les factures.**.
3. Dans **Propriétaire du compte**, saisissez **Nom complet**, **E-mail** et **Téléphone**. Le propriétaire doit pouvoir ouvrir l’e-mail envoyé à cette adresse.
4. Dans **Nom d’adresse**, utilisez des lettres minuscules, des chiffres et des traits d’union. Votre équipe et vos clients utiliseront cette adresse. Le formulaire précise qu’elle ne pourra pas être modifiée ensuite.
5. Lisez **conditions d’utilisation et la politique de confidentialité de Vixly**, cochez l’acceptation, effectuez la vérification humaine si elle est affichée et sélectionnez **Créer le compte**.

## En cas de problème
Si le nom d’adresse est indisponible, choisissez-en un autre. Si **Veuillez confirmer l’adresse avant de continuer.** apparaît, vérifiez l’adresse de facturation et cochez sa confirmation. Distinguez l’e-mail de l’entreprise de celui du propriétaire lorsqu’ils sont différents.

## Sur cet écran

1 indique les coordonnées de l’entreprise ; 2 l’adresse et sa confirmation ; 3 les contacts du propriétaire ; 4 le nom d’adresse ; 5 l’acceptation des conditions ; 6 le bouton de création.

[![Écran annoté indiquant où agir: Formulaire d’inscription de l’entreprise](/help/media/v2/fr-company.png)](/help/media/v2/fr-company.png)

Écran illustratif avec des données fictives. Les prix, dates et choix disponibles peuvent varier. Sélectionnez l’image pour l’ouvrir en grand.
