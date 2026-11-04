---
slug: zones-and-routes
title: Elija la ruta de un precio
summary: La ruta limita los trayectos a los que se aplica una lista o suplemento de su empresa.
section: setting-up
order: 90
permissions: [ops.charges.configure]
conditions: []
routes: []
related: [price-lists, branches-and-warehouses]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Charges/RouteFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargeExtraFields.razor
updated: 2026-10-25
review: needs-review
---

La ruta limita los trayectos a los que se aplica una lista o suplemento de {brandName}. Estos formularios seleccionan almacenes de origen y destino; no dibujan zonas de entrega en un mapa. Revise las opciones antes de guardar: una selección amplia puede aplicar el precio fuera del trayecto previsto.

## Pasos
1. Abra la lista o suplemento desde la página de cargos.
2. Busque **Desde (lugar de entrega)** y elija el almacén de salida previsto.
3. Utilice **Cualquier origen** únicamente cuando el precio deba cubrir todos los orígenes correspondientes.
4. Seleccione **Hasta** o **Cualquier destino** cuando la regla no deba limitarse a un destino.
5. Revise el servicio, las demás restricciones y la fecha de vigencia; después guarde ese formulario.

## Qué ocurre después
Vuelva a abrir la lista o suplemento para confirmar su ruta. Un almacén seleccionado anteriormente puede conservarse aunque ya no figure entre las opciones; lea el aviso sin suponer que la ruta se vuelve ilimitada. Estos ajustes determinan dónde se aplica un precio. No crean salidas, mueven paquetes ni asignan paradas a un conductor.