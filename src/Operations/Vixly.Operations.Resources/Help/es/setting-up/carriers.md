---
slug: carriers
title: Transportistas y sus tarifas
summary: Las fichas describen los transportistas utilizados por su empresa y sus tarifas por destino.
section: setting-up
order: 220
permissions: [ops.carriers.manage]
conditions: []
routes: [/staff/carriers, /staff/carriers/{carrierId:guid}]
related: [shipping-services, price-lists]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Dispatch/CarriersPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Dispatch/CarrierPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Dispatch/CarrierForm.razor
updated: 2026-10-25
review: needs-review
---

Las fichas describen los transportistas utilizados por {brandName} y sus tarifas por destino. Son costes del transportista, distintos de los precios cobrados a clientes. Compruebe el tipo antes de introducir importes porque los formularios aéreo y marítimo utilizan opciones diferentes.

## Pasos
1. Abra **Transportistas** y revise la lista para evitar duplicados.
2. Utilice **Nuevo transportista** para un transportista nuevo o abra un nombre existente.
3. Revise **Nombre**, **Tipo**, **Número de cuenta** y **Prefijo de la aerolínea**. Lea la indicación del prefijo y seleccione el estado activo previsto.
4. Elija **Guardar** y abra la ficha guardada.
5. En **Tarifas**, revise destino, fecha inicial e importes según el modo; elija **Añadir una tarifa** para añadir una tarifa.

## Qué ocurre después
Compruebe destino y fecha después de guardar. Una tarifa posterior sustituye la anterior desde su inicio. Las tarifas futuras pueden eliminarse antes de empezar. Guardar un transportista o coste no publica precios para clientes ni crea salidas; configure esos elementos mediante sus propios procesos de trabajo.