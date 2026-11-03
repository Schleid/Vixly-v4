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
updated: 2026-10-22
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
