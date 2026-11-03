---
slug: items-and-customs-codes
title: Items and customs codes
summary: Customers declare what is inside item by item; staff add the customs code and the country of origin before bundling.
section: setting-up
order: 72
permissions: [ops.services.view, ops.inspection.check]
conditions: []
routes: [/staff/items, /staff/settings/items]
related: [shipping-services, inspection-rules]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Items/ItemCataloguePage.razor
  - src/Operations/Vixly.Operations.Web/Components/Items/ItemsReviewPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Items/ParcelItemsPanel.razor
updated: 2026-11-01
review: ok
---

Each package now lists what is inside item by item: the customer's own words, how many, the value of one, and whether it is new or used. Customers never type a customs code. {brandName} adds it, with the country of origin read on the item, so customs papers can be prepared correctly.

## Steps
1. Open **Item catalogue** in the navigation. The first time, it is filled with everyday courier items and their customs codes. Change, add or remove items to fit your company. The words under "also matches" are the words customers use, in any language.
2. When a customer's words match a catalogue item, the description, the customs code and the unit are filled in by themselves. The country of origin is never guessed.
3. Open **Items to review** to see the parcels at origin whose items still need a customs code or a country of origin, oldest first.
4. Open a parcel from the list. In its items, choose the **Description on documents** from the suggestions to fill its **Customs code**, and type the **Origin** in two letters from the "Made in…" label. Tick **Add to the catalogue** for a new everyday item.
5. Choose **Save items**.
6. When your team is ready, open **Item catalogue** and choose **Require codes and origin**.

## What happens next
From the moment the rule is switched on, a parcel received from then on cannot go into a bundle or a box, and its dispatch cannot close, until every item has a customs code and a country of origin. The message names the parcel and the item to complete. Parcels received before the rule was switched on are never stopped. The customer's declaration (words, quantity and value) is never changed by staff.
