---
slug: branches-and-warehouses
title: Branches and warehouses
summary: Branches and warehouses describe the places where your company works.
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
review: ok
---

Branches and warehouses describe where {brandName} works. A branch is a country or market you serve, for example United States or Haiti. Its warehouses are the real places there: mailbox, reception, storage, pickup points. A branch is neither "origin" nor "destination": your routes and each warehouse's roles decide what goes where.

## Steps
1. Open **Locations and contacts** and review the branch and warehouse lists available to you.
2. To add a country you serve, choose **Add a branch**: give a code and a name, choose its country, and, if the country requires it, its legal identity there (legal name, tax number, lines printed on invoices).
3. Leave **Main office (optional)** empty if the branch has no office of its own. Only a branch with an office can show on your website.
4. To add a place, choose **Add a warehouse**, choose its branch, then enter its code, name and address. The address must be in the branch's country.
5. Select the warehouse's actual roles and review the website visibility choice. Choose **Save** when editing is available.

## What happens next
Invoices issued at a warehouse print its branch's legal identity when one is set, and the company's otherwise. Creating a warehouse does not set up shelves, printers or shipping prices; set those up separately. Website visibility and activity are separate choices; check both before telling customers to use an address.
