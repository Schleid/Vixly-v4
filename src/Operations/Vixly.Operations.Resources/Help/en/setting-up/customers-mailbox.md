---
slug: customers-mailbox
title: Choose the customers’ mailbox
summary: The customer mailbox identifies the receiving address that your company gives its customers.
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
review: ok
---

The customer mailbox identifies the receiving address that {brandName} gives its customers. Choose the warehouse deliberately: the address must be suitable for deliveries sent on a customer’s behalf. Marking a place for website display is a separate choice and does not by itself make it the customer mailbox.

## Steps
1. Open the locations list and select the warehouse you want customers to use.
2. Check its branch, full street address and contact information before changing the mailbox choice.
3. Review **Roles** and the receiving role. Confirm that the warehouse is meant to accept incoming packages.
4. Review **Accepting deliveries now** and select **Customers’ mailbox** when this is the intended mailbox. Read the hint beside that choice.
5. Choose **Save** and read the result.

## What happens next
Reopen the warehouse and check the recorded choice. Only one warehouse is the customer mailbox; selecting this one removes that choice from the previous warehouse. Review the customer-facing mailbox address before distributing delivery instructions. Changing warehouse information does not redirect packages already sent to an old address. Coordinate address changes with your company’s customer support and receiving team.
