---
slug: shipping-services
title: Shipping services
summary: Shipping services control which delivery services your company offers and how they are described.
section: setting-up
order: 70
permissions: [ops.services.view]
conditions: []
routes: [/staff/services]
related: [price-lists, company-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Settings/ShippingServicesPage.razor
updated: 2026-10-31
review: ok
---

Shipping services control which delivery services {brandName} offers and how they are described. Review the transport mode and the existing choices before making changes. The list comes with service names, but your company chooses which services to offer and the operational values shown beside them.

## Steps
1. Open **Shipping services** from the navigation and read the introductory hint.
2. Review **Offered** for each service. If editing is available, select the services your company actually provides.
3. Check **Your label (optional)** and enter the customer-facing label when you need one.
4. Check **Label code**: exactly three letters A to Z, printed large on the shipping label so the warehouse can sort parcels by eye (for example VXA for air standard, VXE for air express, VXS for ocean, VXG for ground). Two services may share a code.
5. Review **Fewest business days** and **Most business days** for the expected transit range.
6. Review the volumetric divisor where the page provides one. Container services show their own explanation instead.
7. If shown, choose **Save** and read the result.

## What happens next
Reopen the page to verify the offered services and their values. Offering a service does not automatically create its price list or dispatch schedule. Configure matching prices separately before relying on a quote. Readers without editable controls can review these choices without saving a change. A new label code shows on shipping labels printed from then on; labels already on parcels keep the old one.