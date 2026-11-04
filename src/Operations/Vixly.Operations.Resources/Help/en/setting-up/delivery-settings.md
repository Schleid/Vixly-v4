---
slug: delivery-settings
title: Delivery settings
summary: Delivery settings define how your company records home delivery, the proof it asks for, and the choices after unsuccessful attempts.
section: setting-up
order: 200
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/delivery]
related: [pickup-settings, payment-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Delivery/DeliverySettingsPage.razor
updated: 2026-10-25
review: ok
---

Delivery settings define how {brandName} records home delivery, the proof it asks for, and the choices after unsuccessful attempts. Review the company’s working procedure before making changes. These settings prepare delivery work; they do not create a route, assign a driver or record a completed handover.

## Steps
1. Open **Delivery settings** and review **How drivers record their stops**. Read each recording method before selecting one where editing is allowed.
2. Review **Proof at the door** and its hint, then select the intended proof choices.
3. Under **Failed deliveries**, review **Tries before giving up** and what should happen after the configured attempts.
4. Review **Payment**, including **Take cash on delivery**, and read the cash-handling explanation.
5. Check **Email customers when their shipment goes out for delivery** and choose **Save** if the page provides the button.

## What happens next
Read the result and reopen the form to verify the recorded choices. A delivery attempt and a successful handover are different outcomes. Staff must still record the actual result and proof in the delivery workflow. Selecting cash collection here does not itself receive money or settle a customer’s invoice.