# Slice 5A: The shipments desk and shipment documents - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-19 |
| Builds on | Slice 4C (routes, services), 4D (shipments the customer creates, shipping labels) |
| Owner input | Approved by the owner on 2026-10-19 as "the next easy slice": built without new owner questions; every choice below follows the owner's earlier answers or common industry practice and can be changed after review |
| Status | **Built; waiting for owner review** ([verification record](SLICE-5A-VERIFICATION.md)) |

## 1. Outcome

Since Slice 4D customers create any number of shipments, but staff have no page that shows them. This slice gives staff the **shipments desk** and both sides the two standard **shipment documents**.

- Staff see every shipment of their warehouses, find one, and see everything in it in one place: instructions, customer, pre-alerts, parcels and where each one stands.
- Staff change the **service** of a shipment before it leaves (the owner's answer to Q-D1).
- A **packing list** and a **pro forma commercial invoice** per shipment, as PDF, for staff and for the customer.

No dispatch, booking, payment or invoice here: those stay in Slice 5 (dispatch payment rule, consolidation, bundles, manifests) and Slice 9 (finance).

## 2. The shipments desk (`/staff/shipments`)

- **Permissions:** two new permissions given per warehouse, as parcels are: `ops.shipments.view` (the desk and the documents) and `ops.shipments.manage` (change the service). The Owner and administrators have them; a company gives them to its roles.
- **The list:** shipments of the user's warehouses by origin, newest first, 25 per page.
  - Views: *to send* (open, with instructions), *waiting* (the waiting shipments of Slice 4D: packages not yet placed), *cancelled*, *all*; counts per view follow the other filters.
  - Filters: warehouse, service, destination country; search by shipment number, customer code or recipient name.
  - Each row: number (once numbered), customer code, destination, service, packages announced and arrived, labels printed (whether each parcel is cleared shows on the shipment page).
  - Export to CSV (up to 5,000 rows, formula-safe as the incoming list).
- **One shipment:** the instructions (destination, recipient, pickup or home delivery, options, service), the customer's code (and name for staff who may see customers), the pre-alerts not yet arrived, and each parcel with its number, weight and size, chargeable weight (the greater of actual and volumetric, A-9), milestone, holds, cleared or not, and whether its shipping label is printed. Totals: pieces, actual weight, chargeable weight, declared value.
- **Change the service:** among the services priced on the shipment's route (as the customer chooses them); only while the shipment is open and none of its parcels has left the origin. The parcels' shipping labels go back to the labels to print, as when the customer changes the shipment (Slice 4D step 2). Audited.

## 3. Shipment documents

Both are PDF, generated on request from the current data, never stored. A shipment gets its number when its first document is made, as with the shipping label.

- **Packing list:** the company, the shipment number and barcode, origin and destination, service, recipient; one line per parcel: number, contents as declared, weight, size; totals. No shipper, no values.
- **Commercial invoice (pro forma):** the shipper (the customer: company if any, name with mailbox code, and mailbox address at the origin warehouse), the consignee (the recipient and the delivery address or pickup office), one line per parcel with the contents and declared value, the total in the company's currency, a line for the reason for export (left for the shipper to fill in, since a person sending gifts and a business selling goods give different reasons), the shipper's signature, and the words *Pro forma: values declared by the shipper*. Tariff codes and customs filing data come with Slice 6 (O-3).
- **Who gets them:** staff with `ops.shipments.view` at the origin warehouse (they prepare the shipment's papers, so they see the shipper on the invoice); the customer for its own shipment on the portal (ownership checked, S-3). A waiting shipment has no documents.
- Tenant-facing text only: the company's name, never "Vixly".

## 4. Not in this slice

- Booking, dispatch and its payment rule, consolidation, bundles, manifests (Slice 5 and 6).
- The final commercial invoice with tariff codes and export filing (Slice 6, O-3).
- Charges on the shipment page: the estimate stays on the customer's rates page until invoices (Slice 9).
