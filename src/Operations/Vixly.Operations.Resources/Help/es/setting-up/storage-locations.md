---
slug: storage-locations
title: Ubicaciones de almacenamiento y sus códigos
summary: Las ubicaciones identifican estantes o zonas utilizados por su empresa dentro de un almacén.
section: setting-up
order: 50
permissions: [ops.warehouses.manage]
conditions: []
routes: [/staff/locations/warehouse/{warehouseId:guid}/storage]
related: [branches-and-warehouses, printers]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Parcels/StorageLocationsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Parcels/ZoneSelect.razor
updated: 2026-10-25
review: needs-review
---

Las ubicaciones identifican estantes o zonas utilizados por {brandName} dentro de un almacén. Sus códigos permiten que el personal se refiera al mismo lugar al guardar o buscar paquetes. Compruebe el almacén antes de añadir una ubicación; cada código pertenece a su lista.

## Pasos
1. Abra **Ubicaciones** para el almacén desde la lista de ubicaciones generales.
2. En **Agregar una ubicación**, introduzca **Código** según el código previsto para el estante o zona física.
3. Elija **Zona** y seleccione **Agregar**.
4. Compruebe que el código nuevo aparece en la lista antes de utilizarlo en el almacén.
5. Para modificar una ubicación existente, cambie la zona o **En uso** y seleccione **Guardar** en esa fila.

## Qué ocurre después
El código no puede cambiarse después de crear la ubicación. Puede modificar su zona y disponibilidad sin renombrarla. Mantenga las etiquetas físicas de acuerdo con el código guardado. Añadir una ubicación no mueve paquetes ni imprime una etiqueta de estante desde esta página; la colocación es una tarea distinta.