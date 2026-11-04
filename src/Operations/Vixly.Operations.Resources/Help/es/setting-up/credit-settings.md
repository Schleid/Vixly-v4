---
slug: credit-settings
title: Ajustes de cuentas de crédito
summary: Los ajustes definen las opciones comunes cuando su empresa ofrece crédito.
section: setting-up
order: 160
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/credit]
related: [company-settings, invoice-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Credit/CreditSettingsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Settings/SettingsPage.razor
updated: 2026-10-25
review: needs-review
---

Los ajustes definen las opciones comunes cuando {brandName} ofrece crédito. Son distintos del límite o saldo pendiente de un cliente concreto. Revise las opciones antes de modificarlas, especialmente los plazos de pago y la regla cuando un cliente superaría un límite.

## Pasos
1. Abra **Ajustes de crédito** desde la navegación.
2. Lea cualquier aviso de crédito desactivado. La activación general se revisa en los ajustes de empresa.
3. Revise **Plazo (días)** para el plazo predeterminado.
4. En **Cuando un cargo supera el límite**, lea cada regla y seleccione la prevista si puede editar.
5. Revise **Avisar cada mes a los clientes que su estado de cuenta está listo**. Elija **Guardar** cuando aparezca el botón y lea el resultado.

## Qué ocurre después
Vuelva a abrir la página para confirmar las opciones guardadas. Estas reglas no conceden crédito automáticamente a todos los clientes ni cambian un límite individual. Revise por separado el crédito del cliente correspondiente. Un formulario de solo lectura permite consultar sin modificar; la ausencia del botón de guardado no significa que la consulta haya fallado.