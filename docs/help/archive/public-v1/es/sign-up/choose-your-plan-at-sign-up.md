---
slug: choose-your-plan-at-sign-up
title: Elegir su plan y sus licencias
summary: Revise el plan, la facturación, las licencias, la prueba y la promoción antes de continuar.
section: sign-up
order: 70
permissions: []
conditions: []
routes: [/en/register/plan, /fr/register/plan, /es/register/plan]
related: [choose-a-plan, licences-and-devices, pay-to-start, sign-up-by-invitation]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationPlan.razor
  - src/Public/Vixly.Public.Web/Features/Registration/OfferSummary.razor
  - src/Platform/Vixly.Platform.Application/UseCases/Registration/SelectRegistrationPlan/SelectRegistrationPlanUseCase.cs
updated: 2026-10-22
review: needs-review
---

Elija su suscripción después de confirmar el correo del propietario. Si seleccionó un plan en el catálogo público, puede aparecer seleccionado aquí. Revise la elección antes de continuar.

## Pasos
1. En **Plan**, seleccione el plan deseado. Lea el precio mostrado y la información sobre cualquier prueba.
2. En **Facturación**, elija **Mensual** o **Anual**. Introduzca la cantidad de licencias de dispositivos en **Licencias**, considerando el mínimo y el límite del plan.
3. Si tiene una promoción, introdúzcala en **Código promocional (opcional)**. Debe estar disponible para su plan y registro; que el campo sea visible no garantiza la aceptación de cualquier código.
4. Revise **Empezar con la prueba gratis (si el plan la tiene)**. Mantenga la opción solo si desea la prueba y el plan la ofrece. Lea la nota sobre impuestos y seleccione **Continuar**.

## Qué ocurre después
Una prueba o un primer período sin importe pendiente puede llevar directamente a configurar la cuenta. De lo contrario, deberá pagar la primera factura. Si tiene una invitación, esta página puede mostrar una oferta acordada en lugar del formulario. Revise el plan, las licencias, la facturación y los cargos; use **Aceptar esta oferta** solo si acepta las condiciones mostradas.
