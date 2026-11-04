---
slug: customers-mailbox
title: Elija la dirección de recepción de los clientes
summary: La dirección de recepción indica dónde deben entregar los clientes sus paquetes para su empresa.
section: setting-up
order: 40
permissions: [ops.warehouses.manage]
conditions: []
routes: []
related: [branches-and-warehouses, shipping-services]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Editor/WarehouseFormPage.razor
  - src/Operations/Vixly.Operations.Application/UseCases/Locations/SaveOperationsWarehouse/SaveOperationsWarehouseUseCase.cs
updated: 2026-10-25
review: needs-review
---

La dirección de recepción indica dónde deben entregar los clientes sus paquetes para {brandName}. Elija el almacén con cuidado: la dirección debe admitir entregas realizadas por cuenta de los clientes. Mostrar un lugar en el sitio es una opción distinta y no lo convierte por sí sola en dirección de recepción.

## Pasos
1. Abra la lista de ubicaciones y seleccione el almacén que utilizarán los clientes.
2. Compruebe la sucursal, la dirección completa y el contacto antes de cambiar esta opción.
3. Revise **Funciones** y la función de recepción. Confirme que el almacén debe admitir paquetes entrantes.
4. Revise **Acepta entregas ahora** y seleccione **Casillero de los clientes** para el lugar previsto. Lea la indicación correspondiente.
5. Elija **Guardar** y lea el resultado.

## Qué ocurre después
Vuelva a abrir el almacén y confirme la selección guardada. Solo un almacén es la dirección de recepción de clientes; seleccionar este retira esa opción del anterior. Revise la dirección que ven los clientes antes de distribuir instrucciones. Cambiar los datos no redirige los paquetes enviados a una dirección anterior. Coordine los cambios con la atención al cliente y el equipo de recepción de su empresa.
