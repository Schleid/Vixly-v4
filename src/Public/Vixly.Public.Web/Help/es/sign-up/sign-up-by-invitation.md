---
slug: sign-up-by-invitation
title: Registrarse con una invitación u oferta
summary: Revise la oferta y use la invitación destinada a su empresa y al correo del propietario.
section: sign-up
order: 100
permissions: []
conditions: []
routes: []
related: [company-details, choose-your-plan-at-sign-up, contact-sales]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationGate.razor
  - src/Public/Vixly.Public.Web/Features/Registration/OfferSummary.razor
  - src/Platform/Vixly.Platform.Application/UseCases/Registration/StartPlatformTenantRegistration/StartPlatformTenantRegistrationUseCase.cs
  - src/Platform/Vixly.Platform.Application/UseCases/Registration/SelectRegistrationPlan/SelectRegistrationPlanUseCase.cs
  - build/HelpCapture/capture.cjs
  - src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json
updated: 2026-10-25
review: needs-review
---

Una invitación comercial permite registrarse con condiciones acordadas, incluso cuando el registro general está cerrado. Use el enlace enviado para su empresa en vez de empezar de nuevo desde la página general.

## Pasos
1. Abra la invitación y revise la oferta presentada. Compruebe la empresa, el plan, la facturación, el importe, los cargos de configuración, la prueba y la fecha de validez mostrados.
2. Complete los datos de empresa y propietario con el correo asociado a la oferta. Una oferta no es un enlace de descuento general para compartir con otra empresa o dirección.
3. Confirme el correo del propietario mediante el mensaje que envía Vixly.
4. En el paso del plan, revise otra vez la oferta y seleccione **Aceptar esta oferta** si acepta sus condiciones. Complete después cualquier pago requerido.

## Si algo falla
Una oferta vencida, ya utilizada o no aplicable no permite iniciar una suscripción nueva. Contacte con ventas si hay que revisar los datos de la empresa, el correo del propietario o las condiciones acordadas. No sustituya la oferta por un código de promoción supuesto.

Sin una invitación válida, la página de registro cerrado le dirige al equipo comercial. Puede seguir consultando los planes y la ayuda. Aceptar una oferta utiliza las condiciones mostradas en ella, no las de otro plan que haya consultado antes.

## En esta pantalla

1 marca las condiciones acordadas; 2 la restricción de correo parcialmente oculta; 3 el correo del propietario; 4 el botón para enviar el formulario completo.

[![Pantalla anotada que indica dónde actuar: Condiciones de invitación y correo del propietario](/help/media/v2/es-offer.png)](/help/media/v2/es-offer.png)

Pantalla ilustrativa con datos ficticios. Los precios, fechas y opciones disponibles pueden variar. Seleccione la imagen para verla a tamaño completo.
