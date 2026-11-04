---
slug: reactivate-a-company
title: Reactive una empresa
summary: Reactive una empresa después de revisar su cuenta y su estado actual.
section: companies
order: 42
permissions: [platform.tenants.reactivate]
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

Reactive una empresa suspendida o desactivada cuando se haya resuelto el motivo de su inactividad. Revise el historial reciente y explique por qué puede restablecerse el acceso antes de confirmar.

## Pasos
1. Abra la empresa correcta y revise **Resumen**. Compare su identidad y su estado actual con la solicitud que está atendiendo.
2. Busque **Acceso y estado** y elija **Reactivar**. El panel solo ofrece cambios disponibles desde el estado actual.
3. Complete **Motivo**. Escriba una explicación objetiva y adecuada para el historial interno de la cuenta.
4. Elija **Reactivar** en el formulario de confirmación. Confirme su identidad cuando se solicite y espere el resultado antes de salir de la página.

## Qué ocurre después
La empresa pasa a activa y se cierra cualquier suspensión abierta. La reactivación no borra el historial de la decisión anterior. Revise las cuestiones de suscripción o facturación en sus secciones.

Si la acción no está disponible, recargue la ficha y compruebe su estado actual. Utilice **Cancelar** para salir de una confirmación sin terminar sin aplicar el cambio.