---
slug: create-a-shipment
title: Create a shipment
summary: A shipment tells your company where to send your goods after they reach the mailbox.
section: shipping
order: 30
permissions: []
conditions: []
routes: [/customer/packages/shipments/new]
related: [your-mailbox-address, announce-a-package]
sources:
  - src/Customer/Vixly.Customer.Web/Components/Portal/ShipmentFormPage.razor
  - src/Customer/Vixly.Customer.Web/Components/Portal/ShipmentDestinationFields.razor
  - src/Customer/Vixly.Customer.Web/Portal/CustomerShipmentEndpoints.cs
updated: 2026-10-27
review: ok
signed_out: false
---

A shipment tells {brandName} where to send your goods after they reach the mailbox. It is different from announcing a package. Announce new packages on their own page first; the shipment form lets you choose available packages rather than enter their tracking details again.

## Steps
1. Open **My packages** and choose **New shipment**.
2. Choose the destination shown on the form. Select pickup or home delivery where offered. For home delivery, complete the recipient’s delivery address, not the mailbox address.
3. Review **Service** if shown. Enter **Number of parcels** for the number of packages expected in this shipment. The expected value helps the estimate; it is not a payment.
4. Check **Recipient** and **Recipient's phone (with the country code, for example +509)**. These belong to the person receiving the goods at the final destination.
5. Review the optional choices. Choose insurance only after reading its terms and completing any acceptance shown.
6. Under **Packages to put in it**, select the announced or received packages that belong in this shipment, if listed.
7. Check the destination and package selection, then choose **Create the shipment**.

## What happens next
Open the saved shipment and check its details. Creating it does not mean the goods have departed, arrived or been paid for. If a package is missing from the available list, check whether it was announced or already assigned elsewhere before creating another announcement.

## On this screen
1. Choose the final destination and delivery method.
2. Enter the expected package count.
3. Check the recipient’s name and phone.
4. Review optional choices.
5. Select the packages for this shipment.
6. Create after checking the instructions.

[![Create a shipment](/customer/help/media/v2/en-shipment.png)](/customer/help/media/v2/en-shipment.png)

Fictional example. Use the address and details in your own account. Select the image to enlarge it.