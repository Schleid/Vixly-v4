---
slug: change-company-status
title: Active una empresa
summary: Active una empresa después de revisar su cuenta y su estado actual.
section: companies
order: 40
permissions: [platform.tenants.activate]
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

Active una empresa cuando esté preparada para pasar a activa en Vixly. La acción aparece para empresas en borrador o pendientes de activación. Confirme la cuenta correcta antes de cambiar su estado.

## Pasos
1. Abra la empresa correcta y revise **Resumen**. Compare su identidad y su estado actual con la solicitud que está atendiendo.
2. Busque **Acceso y estado** y elija **Activar**. El panel solo ofrece cambios disponibles desde el estado actual.
3. Lea la confirmación mostrada antes de continuar. Este formulario no solicita un motivo interno para la activación.
4. Elija **Activar** en el formulario de confirmación. Espere el resultado antes de salir de la página.

## Qué ocurre después
La empresa pasa a activa cuando termina correctamente la acción. La activación cambia su estado; no demuestra que todos sus dispositivos o cuentas de personal hayan terminado su configuración.

Si la acción no está disponible, recargue la ficha y compruebe su estado actual. Utilice **Cancelar** para salir de una confirmación sin terminar sin aplicar el cambio.