---
slug: sign-up-overview
title: Registrar su empresa
summary: Complete los datos, confirme el correo, elija un plan y realice el pago si corresponde.
section: sign-up
order: 40
permissions: []
conditions: []
routes: [/en/register, /fr/register, /es/register]
related: [company-details, confirm-your-email, choose-your-plan-at-sign-up, pay-to-start, after-sign-up]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationStart.razor
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationSteps.cs
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationGate.razor
  - src/Platform/Vixly.Platform.Application/UseCases/Registration/SelectRegistrationPlan/SelectRegistrationPlanUseCase.cs
updated: 2026-10-22
review: needs-review
---

El registro crea la cuenta de Vixly de su empresa e inicia la preparación de su espacio. Tenga a mano los datos de facturación, los datos del propietario de la cuenta y una bandeja de entrada accesible. Conserve también cualquier invitación u oferta recibida.

## Pasos
1. Introduzca los datos de la empresa, su dirección y los del propietario. Elija el nombre de dirección que usarán el equipo y los clientes, revise las condiciones y seleccione **Crear la cuenta**.
2. Abra el correo de Vixly y seleccione **Confirmar mi correo** en la página de confirmación. Abrir el enlace por sí solo no completa la confirmación.
3. Elija el plan, el período de facturación y las licencias, o acepte la oferta presentada. Revise la prueba o la promoción antes de continuar.
4. Pague la primera factura si corresponde. Una prueba o un primer período sin importe pendiente puede llevar directamente a la configuración de la cuenta.
5. Siga la página de creación y el correo con el enlace para configurar el acceso de su equipo.

## Para volver más tarde
La página siguiente depende del progreso guardado. Conserve el correo de confirmación y use el enlace antes de que venza; el paso del correo muestra la fecha límite. Otro navegador no conserva necesariamente todas las elecciones anteriores. Si el registro general está cerrado, contacte con ventas o use su invitación válida. La duración depende de la confirmación, el pago y la preparación del espacio.
