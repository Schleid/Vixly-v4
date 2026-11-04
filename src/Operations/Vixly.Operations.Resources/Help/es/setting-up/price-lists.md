---
slug: price-lists
title: Listas de precios y versiones
summary: Las listas proporcionan a su empresa precios de transporte para un servicio, una ruta y, opcionalmente, un cliente concreto.
section: setting-up
order: 80
permissions: [ops.charges.configure]
conditions: []
routes: [/staff/charges]
related: [zones-and-routes, extras-and-item-types, publish-prices]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/PriceListPanel.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/PriceVersionFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/VersionHistory.razor
updated: 2026-10-25
review: needs-review
---

Las listas proporcionan a {brandName} precios de transporte para un servicio, una ruta y, opcionalmente, un cliente concreto. Cada lista conserva versiones con fecha. Compruebe la moneda y las unidades antes de introducir importes y elija el modo de transporte adecuado.

## Pasos
1. Abra **Precios** y seleccione el modo.
2. Abra una lista existente o elija **Añadir una lista de precios** y seleccione **Servicio**.
3. Revise origen y destino. Complete **Código de cliente (vacío: precios estándar)** solo para una lista específica de un cliente.
4. Introduzca tramos, precios por artículo o cargos terrestres aplicables. Revise mínimos, servicio y consolidación.
5. Establezca **Empieza el**, revise **En uso** y elija **Guardar** para una lista existente o **Añadir una lista de precios** para una nueva.

## Qué ocurre después
Compruebe el historial después de guardar. Una versión iniciada es de solo lectura; cree otra con fecha para los precios posteriores. Puede eliminar una versión programada antes de que empiece. Confirme la fecha antes de utilizar la lista en una cotización. Publicar precios es una decisión distinta de guardarlos.