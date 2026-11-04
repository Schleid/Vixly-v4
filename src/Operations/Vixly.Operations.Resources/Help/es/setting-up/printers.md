---
slug: printers
title: Impresoras de etiquetas y programa de impresión
summary: Configure las impresoras de un almacén para que su empresa envíe las etiquetas al dispositivo adecuado.
section: setting-up
order: 60
permissions: [ops.printers.manage]
conditions: []
routes: [/staff/locations/warehouse/{warehouseId:guid}/printers/]
related: [branches-and-warehouses, storage-locations]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Parcels/PrintersPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Parcels/PrinterForm.razor
updated: 2026-10-25
review: needs-review
---

Configure las impresoras de un almacén para que {brandName} envíe las etiquetas al dispositivo adecuado. La definición de la impresora y el programa de impresión son partes distintas: guardar una impresora no demuestra que el equipo del programa esté conectado o preparado.

## Pasos
1. Abra **Impresoras** junto al almacén en la lista de ubicaciones.
2. Introduzca **Nombre** y seleccione **Conexión**.
3. Complete **Dirección o nombre en Windows**, **Resolución (ppp)** y **Etiquetas** según la impresora y las etiquetas cargadas. Seleccione **Agregar**.
4. En **Programas de impresión**, introduzca **Computadora** y elija **Registrar** para registrar el programa. Siga las instrucciones y mantenga privada su clave de conexión.
5. Cuando el dispositivo y el programa estén listos, utilice **Imprimir una etiqueta de prueba** para la impresora guardada.

## Qué ocurre después
Compruebe la etiqueta física y la última conexión del programa. Guardar una impresora no garantiza que haya impreso correctamente. Asocie los soportes de identificación y envío con sus impresoras correspondientes. Si falla, compruebe el almacén seleccionado, la conexión y el estado del programa antes de continuar.