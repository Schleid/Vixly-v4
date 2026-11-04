---
slug: announce-a-package
title: Announce a package
summary: Announcing a package tells your company what you bought and which delivery to expect.
section: your-packages
order: 20
permissions: []
conditions: []
routes: [/customer/packages/new]
related: [your-mailbox-address, create-a-shipment]
sources:
  - src/Customer/Vixly.Customer.Web/Components/Portal/PreAlertFormPage.razor
  - src/Customer/Vixly.Customer.Web/Components/Portal/PackageFields.razor
  - src/Customer/Vixly.Customer.Web/Portal/CustomerPackageEndpoints.cs
updated: 2026-10-27
review: ok
signed_out: false
---

Announcing a package tells {brandName} what you bought and which delivery to expect. It does not send the package or create the instructions for its onward shipment. Have the carrier’s tracking number ready; a shop’s order number is not the same thing.

## Steps
1. Open **My packages**, then choose **Announce a package**.
2. Enter **Carrier**: the company delivering the package to the mailbox. Enter the shop where you bought the goods in **Purchase store**; this is not a destination address.
3. Copy **Tracking number** from the carrier or the shop’s dispatch notice. Use the tracking number for this package, not the order number for the whole purchase.
4. Describe the goods in **What is inside**. Enter their declared value in **Value** and the expected date if known.
5. If an existing shipment is offered, choose it only if these goods belong in it. Otherwise you can choose a shipment later.
6. Check the details and choose **Announce**.

## What happens next
Read the confirmation and check your package list. An announcement is not proof that the warehouse has received the goods. If the shop sends the order in several packages with different tracking numbers, announce each package separately. Next, create or choose the onward shipment.

## On this screen
1. The carrier and purchase store are different things.
2. Enter the carrier’s tracking number.
3. Describe the actual goods.
4. Submit after checking all fields.

[![Announce a package](/customer/help/media/v2/en-announce.png)](/customer/help/media/v2/en-announce.png)

Fictional example. Use the address and details in your own account. Select the image to enlarge it.