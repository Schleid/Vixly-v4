---
slug: company-details
title: Introducir los datos de su empresa
summary: Complete los datos de empresa, dirección de facturación, propietario y dirección de su espacio.
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

El primer formulario recoge los datos necesarios para crear la cuenta de su empresa. Revise cuidadosamente la dirección de facturación: el formulario pide confirmar que es correcta para las facturas.

## Pasos
1. En **Empresa**, introduzca **Razón social**, **Correo de la empresa** y **Teléfono de la empresa**. Use **Nombre comercial (opcional)** si la empresa opera con otro nombre.
2. En **Dirección**, complete **Dirección línea 1**, **Ciudad** y **País**. Añada **Dirección línea 2 (opcional)**, **Estado o provincia** y **Código postal** cuando corresponda. Tras revisarla, marque **Esta dirección es correcta; aparecerá en las facturas.**.
3. En **Propietario de la cuenta**, introduzca **Nombre completo**, **Correo** y **Teléfono**. El propietario debe poder abrir el correo enviado a esa dirección.
4. En **Nombre de la dirección**, use letras minúsculas, números y guiones. Su equipo y sus clientes utilizarán esta dirección. El formulario indica que no puede cambiarse después.
5. Lea **términos de servicio y la política de privacidad de Vixly**, marque la aceptación, complete la comprobación humana y seleccione **Crear la cuenta**.

## Si algo falla
Si el nombre de dirección no está disponible, elija otro. Si aparece **Confirme la dirección antes de continuar.**, revise la dirección de facturación y marque su confirmación. Distinga el correo de la empresa del correo del propietario cuando sean direcciones diferentes.
