---
slug: close-a-company
title: Desactive una empresa
summary: Desactive una empresa después de revisar su cuenta y su estado actual.
section: companies
order: 43
permissions: [platform.tenants.deactivate]
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

Desactive una empresa cuando su cuenta de Vixly ya no deba permanecer activa. Esta acción se ofrece para empresas en borrador, pendientes, activas o suspendidas. Es distinta de una prohibición permanente.

## Pasos
1. Abra la empresa correcta y revise **Resumen**. Compare su identidad y su estado actual con la solicitud que está atendiendo.
2. Busque **Acceso y estado** y elija **Desactivar**. El panel solo ofrece cambios disponibles desde el estado actual.
3. Complete **Motivo**. Escriba una explicación objetiva y adecuada para el historial interno de la cuenta.
4. Elija **Desactivar** en el formulario de confirmación. Confirme su identidad cuando se solicite y espere el resultado antes de salir de la página.

## Qué ocurre después
La empresa pasa a desactivada y el cambio permanece en su historial. No describa esta acción como la eliminación de sus datos. Una reactivación posterior es una decisión independiente con su propia confirmación.

Si la acción no está disponible, recargue la ficha y compruebe su estado actual. Utilice **Cancelar** para salir de una confirmación sin terminar sin aplicar el cambio.