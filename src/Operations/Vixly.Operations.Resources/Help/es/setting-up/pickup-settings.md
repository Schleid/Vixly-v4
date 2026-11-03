---
slug: pickup-settings
title: Ajustes de recogida
summary: Los ajustes definen cómo registra su empresa la entrega en el mostrador de destino y los cargos relacionados.
section: setting-up
order: 190
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/pickup]
related: [delivery-settings, event-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Pickup/PickupSettingsPage.razor
updated: 2026-10-25
review: needs-review
---

Los ajustes definen cómo registra {brandName} la entrega en el mostrador de destino y los cargos relacionados. Revise los comprobantes exigidos antes de editar. Los ajustes describen las reglas de trabajo; no entregan un paquete ni confirman por sí solos la identidad de un cliente.

## Pasos
1. Abra **Ajustes de entrega** y revise **Pruebas pedidas en el mostrador**. Lea la explicación antes de seleccionar comprobantes cuando pueda editar.
2. Revise **Facturar cargos en destino (derechos, impuestos y cargos locales)** y su indicación.
3. En **Almacenaje en destino**, compruebe si se cobran gastos de almacenamiento en destino.
4. Revise **Días gratis** y el importe mostrado. Utilice la moneda de la empresa sin suponer un importe fijo.
5. Elija **Guardar** si aparece y lea el resultado.

## Qué ocurre después
Vuelva a abrir la página y confirme las reglas antes de utilizarlas en el mostrador. Los días gratuitos y cargos de almacenamiento son distintos de los comprobantes recogidos en la entrega. Cambiar esta configuración no libera envíos, cobra saldos pendientes ni demuestra que un cliente haya recibido sus mercancías.