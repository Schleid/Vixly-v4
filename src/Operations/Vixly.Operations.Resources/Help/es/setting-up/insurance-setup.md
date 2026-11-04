---
slug: insurance-setup
title: Productos y documentos de seguro
summary: La página de seguros reúne pólizas, asignaciones y textos de responsabilidad de su empresa.
section: setting-up
order: 120
permissions: [ops.insurance.view]
conditions: []
routes: [/staff/insurance/]
related: [price-lists, invoice-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Insurance/InsurancePage.razor
  - src/Operations/Vixly.Operations.Web/Components/Insurance/PolicyPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/InsuranceProductsPanel.razor
updated: 2026-10-25
review: needs-review
---

La página de seguros reúne pólizas, asignaciones y textos de responsabilidad de {brandName}. Los productos para clientes y sus primas se configuran en los cargos. Revise ambos lugares antes de describir una oferta: una póliza registrada no es por sí sola un producto con precio.

## Pasos
1. Abra **Seguros** y revise **Las pólizas de la empresa**. Abra una póliza para consultar detalles y documentos.
2. Si aparecen controles de edición, utilice **Agregar una póliza** para registrar una póliza y sus datos.
3. Revise **Cobertura vendida a clientes** y la póliza elegida en **Amparada por**. Guarde cambios solo cuando la página lo permita.
4. Revise **El personal puede cambiar el seguro de un envío abierto, cuando el cliente acepta las condiciones** y los textos de responsabilidad por modo.
5. Utilice **Abrir los precios** para revisar productos y primas en los cargos si esa tarea está disponible.

## Qué ocurre después
Compruebe conjuntamente fechas, asignaciones y textos para clientes. Registrar documentos no vende un seguro ni añade una prima a un envío. Mantenga los datos correctos y consulte el producto realmente seleccionado al comprobar el resultado del seguro para el envío del cliente.