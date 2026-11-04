---
slug: supplies-catalogue
title: Catálogo y precios de suministros
summary: El catálogo contiene los productos y materiales de embalaje vendidos por su empresa.
section: setting-up
order: 210
permissions: [ops.charges.configure]
conditions: []
routes: [/staff/supplies, /staff/supplies/{supplyId:guid}]
related: [invoice-settings, price-lists]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SuppliesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SupplyPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SupplyDetailsFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Supplies/SupplyPriceFields.razor
updated: 2026-10-25
review: needs-review
---

El catálogo contiene los productos y materiales de embalaje vendidos por {brandName}. Revise las entradas antes de añadir otra, especialmente código, códigos de barras y precio actual. Los datos y precios con fecha son distintos; cambiar un nombre no sustituye el historial.

## Pasos
1. Abra los suministros y utilice **Código, nombre o código de barras** o **Categoría** para encontrar el artículo.
2. Para añadirlo, abra **Nuevo suministro**. Introduzca código, categoría, unidad y nombres localizados; revise impuestos y códigos de barras.
3. Complete el precio inicial y seleccione **Agregar el suministro**.
4. Para modificar un artículo, abra su código, edite **Detalles y códigos de barras** y elija **Guardar**.
5. En **Un precio nuevo**, revise fecha, descuentos por cantidad y precios por almacén. Seleccione **Guardar el precio**.

## Qué ocurre después
Compruebe el artículo y su precio actual o programado. Un precio futuro no entra en vigor inmediatamente. Desactivar un artículo es distinto de cambiar su precio. Crear una entrada o imprimir etiquetas no recibe existencias físicas ni registra ventas; esas son tareas separadas del mantenimiento del catálogo.