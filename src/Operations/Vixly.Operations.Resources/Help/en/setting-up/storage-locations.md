---
slug: storage-locations
title: Storage locations and their labels
summary: Storage locations identify the shelves or areas used by your company inside one warehouse.
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
review: ok
---

Storage locations identify the shelves or areas used by {brandName} inside one warehouse. Their codes let staff refer to the same place when putting packages away or finding them later. Check the warehouse before adding a location; a code belongs to that warehouse’s list.

## Steps
1. Open the warehouse’s **Shelves** from the locations list.
2. Under **Add a shelf**, enter **Code** using the code planned for that physical shelf or area.
3. Choose the appropriate **Zone** and select **Add**.
4. Check that the new code appears in the list before using it on the warehouse floor.
5. To update an existing location, change its zone or **In use** choice and select **Save** on that row.

## What happens next
A location code cannot be changed after creation. Its zone and availability can be updated without renaming it. Keep physical shelf labels consistent with the saved code. Adding a location does not move packages into it or print a shelf label from this page; placement is a separate warehouse task.