---
slug: extras-and-item-types
title: Suplementos y tipos de artículos
summary: Los suplementos añaden un cargo cuando se cumplen sus criterios en el trabajo de su empresa.
section: setting-up
order: 100
permissions: [ops.charges.configure]
conditions: []
routes: []
related: [price-lists, zones-and-routes]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Charges/ExtrasPanel.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargeExtraFields.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ItemTypesPanel.razor
updated: 2026-10-25
review: needs-review
---

Los suplementos añaden un cargo cuando se cumplen sus criterios en el trabajo de {brandName}. Los tipos de artículos describen mercancías con precios marítimos propios. Revise el modo de transporte y distinga el nombre y los criterios del importe vigente en una fecha.

## Pasos
1. En los cargos, abra **Agregar un cargo adicional** o despliegue un suplemento existente.
2. Introduzca **Nombre que se muestra en la línea**, **Monto o porcentaje** y las opciones de cálculo.
3. Revise **Se aplica**, palabras, servicio, país, ruta y entrega. Seleccione la opción gubernamental solo para esos cargos.
4. Establezca **Empieza el** y guarde el suplemento.
5. Para transporte marítimo, revise **Tipos de artículos marítimos**. Introduzca nombre y palabras de coincidencia; utilice **Añadir el tipo de artículo** o **Guardar**.

## Qué ocurre después
Compruebe la entrada y los importes programados. Desactivar un suplemento o tipo conserva su registro en lugar de eliminarlo. Crear un tipo no proporciona su precio: revise también la lista marítima correspondiente. Compruebe una cotización representativa antes de utilizar las reglas modificadas para atender a los clientes.