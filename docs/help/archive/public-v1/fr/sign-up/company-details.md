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
updated: 2026-10-22
review: needs-review
---

Le premier formulaire recueille les informations nécessaires à la création du compte de votre entreprise. Vérifiez soigneusement l’adresse de facturation : le formulaire vous demande de confirmer qu’elle convient aux factures.

## Étapes
1. Dans **Entreprise**, renseignez **Raison sociale**, **E-mail de l’entreprise** et **Téléphone de l’entreprise**. Utilisez **Nom commercial (facultatif)** si votre entreprise exerce sous un autre nom.
2. Dans **Adresse**, remplissez **Adresse ligne 1**, **Ville** et **Pays**. Ajoutez **Adresse ligne 2 (facultatif)**, **État ou province** et **Code postal** si nécessaire. Après vérification, cochez **Cette adresse est exacte ; elle figurera sur les factures.**.
3. Dans **Propriétaire du compte**, saisissez **Nom complet**, **E-mail** et **Téléphone**. Le propriétaire doit pouvoir ouvrir l’e-mail envoyé à cette adresse.
4. Dans **Nom d’adresse**, utilisez des lettres minuscules, des chiffres et des traits d’union. Votre équipe et vos clients utiliseront cette adresse. Le formulaire précise qu’elle ne pourra pas être modifiée ensuite.
5. Lisez **conditions d’utilisation et la politique de confidentialité de Vixly**, cochez l’acceptation, effectuez la vérification humaine et sélectionnez **Créer le compte**.

## En cas de problème
Si le nom d’adresse est indisponible, choisissez-en un autre. Si **Veuillez confirmer l’adresse avant de continuer.** apparaît, vérifiez l’adresse de facturation et cochez sa confirmation. Distinguez l’e-mail de l’entreprise de celui du propriétaire lorsqu’ils sont différents.
