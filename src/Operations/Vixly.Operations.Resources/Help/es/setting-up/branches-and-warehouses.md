---
slug: branches-and-warehouses
title: Sucursales y almacenes
summary: Las sucursales y almacenes representan los lugares donde trabaja su empresa.
section: setting-up
order: 30
permissions: [ops.branches.view, ops.warehouses.view]
conditions: []
routes: [/staff/locations, /staff/locations/branch/{branchId:guid?}, /staff/locations/warehouse/{warehouseId:guid?}]
related: [customers-mailbox, storage-locations, printers]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Editor/LocationsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Editor/BranchFormPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Editor/WarehouseFormPage.razor
updated: 2026-10-29
review: needs-review
---

Las sucursales y almacenes describen dónde trabaja {brandName}. Una **sucursal** es un país o mercado que usted atiende, por ejemplo Estados Unidos o Haití. Sus **almacenes** son los lugares reales allí: casillero, recepción, almacenamiento, puntos de retiro. Una sucursal no es «origen» ni «destino»: sus rutas y las funciones de cada almacén deciden qué va adónde.

## Pasos
1. Abra **Ubicaciones y contactos** y revise las listas de sucursales y almacenes a las que tiene acceso.
2. Para añadir un país que atiende, elija **Añadir una sucursal**: indique un código y un nombre, elija su **país** y, si el país lo exige, su **identidad legal** allí (razón social, número fiscal, menciones impresas en las facturas).
3. Deje **Oficina principal** vacía si la sucursal no tiene oficina propia. Solo una sucursal con oficina puede aparecer en su sitio.
4. Para añadir un lugar, elija **Añadir un almacén**, elija su sucursal y escriba su código, nombre y dirección. La dirección debe estar en el país de la sucursal.
5. Seleccione las funciones reales del almacén y revise la visibilidad en el sitio. Elija **Guardar** cuando la edición esté disponible.

## Qué pasa después
Las facturas emitidas en un almacén imprimen la identidad legal de su sucursal cuando está indicada, y la de la empresa en caso contrario. Crear un almacén no configura estantes, impresoras ni precios; configúrelos por separado. La visibilidad en el sitio y la actividad son elecciones distintas; revise ambas antes de dar una dirección a los clientes.
