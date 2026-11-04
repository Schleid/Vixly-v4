---
slug: suspend-a-company
title: Suspenda una empresa
summary: Suspenda una empresa después de revisar su cuenta y su estado actual.
section: companies
order: 41
permissions: [platform.tenants.suspend]
conditions: []
routes: []
related: [company-page]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantLifecycle.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantMoves.cs
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantLifecyclePresenter.cs
updated: 2026-10-23
review: needs-review
---

Suspenda una empresa activa cuando sea necesario pausar su cuenta. Elija la categoría de suspensión adecuada y escriba un motivo interno. La categoría y la explicación interna tienen finalidades distintas.

## Pasos
1. Abra la empresa correcta y revise **Resumen**. Compare su identidad y su estado actual con la solicitud que está atendiendo.
2. Busque **Acceso y estado** y elija **Suspender**. El panel solo ofrece cambios disponibles desde el estado actual.
3. Complete **Motivo**. Seleccione también **Categoría**. Escriba una explicación objetiva y adecuada para el historial interno de la cuenta.
4. Elija **Suspender** en el formulario de confirmación. Confirme su identidad cuando se solicite y espere el resultado antes de salir de la página.

## Qué ocurre después
La empresa pasa a suspendida. El aviso indica la categoría de suspensión y cómo contactar con Vixly, sin revelar el motivo interno. Compruebe el estado registrado antes de responder a la solicitud.

Si la acción no está disponible, recargue la ficha y compruebe su estado actual. Utilice **Cancelar** para salir de una confirmación sin terminar sin aplicar el cambio.