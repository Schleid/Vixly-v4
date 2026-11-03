---
slug: confirm-your-email
title: Confirmar su correo electrónico
summary: Use el enlace de confirmación, vuelva a enviarlo o corrija la dirección del propietario.
section: sign-up
order: 60
permissions: []
conditions: []
routes: [/en/register/email, /fr/register/email, /es/register/email, /register/verify]
related: [company-details, choose-your-plan-at-sign-up, sign-up-questions]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationEmail.razor
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationVerify.razor
  - src/Public/Vixly.Public.Web/Components/Pages/RegisterVerifyPage.razor
  - build/HelpCapture/capture.cjs
  - src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json
updated: 2026-10-25
review: needs-review
---

Después del formulario de empresa, Vixly le pide confirmar el correo del propietario de la cuenta. El paso del correo muestra una dirección parcialmente oculta y la fecha límite del enlace. Compruebe ambos antes de salir de la página.

## Pasos
1. Abra la bandeja de entrada de la dirección del propietario que introdujo. Busque el correo de confirmación de Vixly y abra su enlace antes de la fecha indicada.
2. En la página que se abre, seleccione **Confirmar mi correo**. Se confirma la dirección y continúa al paso del plan. Puede realizar esta confirmación en otro navegador mediante el enlace recibido.
3. Si el correo no llegó, compruebe la dirección mostrada y la carpeta de correo no deseado. Seleccione **Enviar el enlace de nuevo** en el paso del correo para recibir otro enlace.
4. Si la dirección es incorrecta, introduzca la corrección en **Nueva dirección de correo electrónico** y seleccione **Cambiar el correo**. Busque el mensaje nuevo en la dirección corregida.

## Si algo falla
**Este enlace está incompleto. Abra de nuevo el enlace del correo.** indica que debe volver a abrir el enlace completo del correo. Abrir el enlace por sí solo no confirma la dirección: use el botón. Si el enlace venció o el registro terminó, siga el mensaje de la página en vez de reutilizar el enlace antiguo. Conserve el correo más reciente hasta llegar al siguiente paso.

## En esta pantalla

1 señala el reenvío del correo; 2 marca el campo para corregir la dirección. En la segunda imagen, 1 señala el botón de confirmación al abrir el enlace recibido.

[![Pantalla anotada que indica dónde actuar: Estado, reenvío y corrección del correo](/help/media/v2/es-email.png)](/help/media/v2/es-email.png)

[![Pantalla anotada que indica dónde actuar: Botón de confirmación del correo](/help/media/v2/es-verify.png)](/help/media/v2/es-verify.png)

Pantalla ilustrativa con datos ficticios. Los precios, fechas y opciones disponibles pueden variar. Seleccione la imagen para verla a tamaño completo.
