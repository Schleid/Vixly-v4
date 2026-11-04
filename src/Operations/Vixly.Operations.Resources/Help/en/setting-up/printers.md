---
slug: printers
title: Label printers and the print program
summary: Configure a warehouse’s label printers so your company can send labels to the right device.
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
review: ok
---

Configure a warehouse’s label printers so {brandName} can send labels to the right device. The printer definition and the print program are separate parts: saving a printer does not show that the computer running the program is connected or ready to print.

## Steps
1. Open **Printers** beside the warehouse in the locations list.
2. Enter **Name** and select the appropriate **Connection**.
3. Fill in **Address or Windows name**, **Resolution (dpi)** and **Labels** to match the actual printer and loaded labels. Select **Add**.
4. In **Print programs**, enter **Computer** and choose **Register** when registering the print program. Follow the instructions shown and keep its connection key private.
5. With the printer and program ready, use **Print a test label** for the saved printer.

## What happens next
Check the physical test label and the program’s last-seen information. A saved printer is not proof that a label printed successfully. Keep ID and shipping-label media matched to their intended printers. If printing fails, check the selected warehouse, printer connection and program status before continuing operational work.