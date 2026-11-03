---
slug: setup-checklist
title: Configure su empresa paso a paso
summary: Utilice la lista inicial para saber qué tareas de configuración quedan pendientes.
section: setting-up
order: 10
permissions: [ops.settings.edit]
conditions: []
routes: []
related: [company-settings, branches-and-warehouses, shipping-services, workspace-at-a-glance]
sources:
  - src/Operations/Vixly.Operations.Web/Components/SignIn/WorkspacePage.razor
  - src/Operations/Vixly.Operations.Application/UseCases/Settings/GetOperationsSetupChecklist/GetOperationsSetupChecklistUseCase.cs
updated: 2026-10-25
review: needs-review
---

La lista de configuración muestra las tareas iniciales pendientes para {brandName}. Aparece en el inicio del propietario o de un administrador mientras quede trabajo de configuración. Es un resumen, no un sustituto de revisar cada pantalla.

La lista refleja la información registrada. Un elemento completado significa que se encontró su requisito inicial; no certifica que se hayan revisado todos los precios, contactos o decisiones del negocio.

## Pasos
1. Abra **Inicio** y busque **Terminar la configuración** cuando aparezca la lista.
2. Lea los elementos pendientes. Incluyen los ajustes de empresa, la primera sucursal, un almacén de recepción, los servicios, el sitio publicado y un empleado adicional.
3. Utilice el enlace de un elemento incompleto para abrir su página de configuración. Revise la información existente antes de realizar el cambio adecuado.
4. Vuelva al inicio después de guardar y compruebe la lista actualizada. El enlace de ayuda junto al título abre este resumen.

## Qué ocurre después
Los elementos completados muestran una marca. Cuando todos los elementos iniciales estén completos, la lista ya no necesita aparecer en el inicio. Puede seguir accediendo a la configuración mediante la navegación habitual. Completar la lista no recibe paquetes, cobra pagos ni aprueba dispositivos; son tareas independientes.