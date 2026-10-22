# Slice 5A: The shipments desk and shipment documents - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-19 |
| Built on | Slice 4D; not committed here: the dev team owns git |
| Design | [Slice 5A design note](SLICE-5A-SHIPMENTS-DESK-DESIGN.md), the "next easy slice" the owner approved on 2026-10-19 |
| Status | **Built and verified by automated tests; waiting for owner review** |

## 1. What was built

### For staff

- **Shipments** in the menu (`/staff/shipments`, new permission *see shipments and their documents*, given per warehouse; the Owner and administrators have it):
  - views *to send*, *waiting for the customer*, *cancelled* and *all*, with their counts under the other filters;
  - filters: warehouse, service, destination country; search by shipment number, customer code or recipient name;
  - each line: number (or *not numbered yet*), origin, customer code, destination and recipient, service, packages expected, received and labelled;
  - 25 per page; export to CSV (up to 5,000 lines, formula-safe).
- **One shipment** (`/staff/shipments/{id}`): the customer's code (and name for staff who may see customers), origin, destination (home delivery or pickup), recipient, service, options, status; each parcel with its contents, weight, **chargeable weight** (the greater of actual and volumetric with the service's divisor, A-9), stage, cleared or on hold, and its shipping label (printed or to print); totals; the packages still expected.
- **Change the service** (new permission *change a shipment's service before it leaves*, per warehouse): among the services the company prices for the customer, while the shipment is open, says where it goes and every parcel is at the origin. Shipping labels already printed go back to the labels to print. Audited.

### For staff and the customer

- **Packing list** and **commercial invoice (pro forma)**, as PDF, from the shipment page (staff) and the customer's shipment page on the portal, once the shipment says where it goes and has a parcel. The first document numbers the shipment.
- Contents: the company, the shipment number and barcode, the date (in the company's time zone); the consignee (recipient, office or address, phone); origin, destination and service; one line per parcel (number, contents, weight, size); totals. The invoice adds the shipper (company if any, name with mailbox code, mailbox address at the origin), each parcel's declared value, the total declared value, *Pro forma: values declared by the shipper*, a line for the reason for export and the shipper's signature.
- Parcels returned to the sender, disposed of or sold are not on the documents.
- The customer's documents are in the customer's language.
- The customer gets only its own shipment's documents (S-3); another customer's answer "not found".

## 2. Database

- One index for the desk: `ops.Shipments (TenantId, OriginWarehouseId, CreatedUtc)`. The permissions live in code and the documents are made on request, never stored.
- Migration `20261020010151_ShipmentsDesk`, in the global script (19 Platform, 23 Operations, 1 Customer migrations); tested on an empty server and over the Slice 4D database, each run twice.

## 3. Automated tests

| Test | What it proves |
|---|---|
| `Service_ChangesOnAnOpenShipmentWithInstructions_Only` (Domain) | The service changes on an open shipment with where it goes; never the waiting one or a cancelled one |
| `ShipmentsDesk_ListsAndShowsAShipment_GivesItsDocuments_AndChangesItsService` (Web) | The customer's shipment listed with its counts and found by customer code, not by another; the CSV; the shipment page with the parcel and the customer's name; both documents as PDF named after the shipment, which is then numbered; an unknown document refused; the service changed to one the company prices, another refused; the list filtered by service |
| `StaffWithoutTheseRoles_CannotScanCheckOrSeeParcels` (Web, extended) | Without the permission: no Shipments in the menu, the desk refused, the export forbidden |
| `Customer_DownloadsTheDocumentsOfTheirShipment_NeverAnothers` (Api) | No documents before a parcel is in the shipment; then both PDFs from the portal; an unknown document and another customer refused |
| `EndedParcels_AreLeftOut_AndNeverCountAsDeparted` (Application) | A parcel disposed of or returned is left off the documents and never blocks the service as "departed" |

## 4. Independent review (2026-10-19)

Tenant isolation, the customer's ownership, the permission per warehouse for viewing, changing and documents, CSV and header injection, antiforgery, the audit and the texts were found sound. Its findings and what was done:

| # | Finding | Done |
|---|---|---|
| 1 | A parcel disposed of or returned blocked the service change as "departed" and stayed on the documents | End milestones are neither departed nor on the documents |
| 2 | The packing list showed the shipper; the customer's phone was on the documents | The shipper is on the invoice only, without the phone; staff who prepare a shipment's papers see the shipper on the invoice (recorded in the design) |
| 3 | No index for the desk | Index by origin and date (migration above) |
| 4 | The chargeable weight shown is not rounded as billing rounds it | Labelled "before rounding"; billing (Slice 9) rounds per its price list |
| 5 | The staff change checks the services the company prices for the customer, not the route; insurance terms are not asked again when the mode changes | Kept as a limit: the route and the insurance terms are checked again at booking (Slice 5) |
| 6 | Two documents asked at once could fail when both numbered the shipment | The second reloads the numbered shipment and builds again |
| 7 | The list has no "cleared" count | Kept on the shipment page; the list shows labels printed (design updated) |
| 8 | "To send" would include booked shipments once booking exists | Open shipments only |
| 9 | Paging had no tie-breaker, no limit and no way back | Ordered by date then id; the page number is capped; a previous link |
| 10 | The customer's documents came in the platform's default language | In the customer's language |
| 11 | Dates in UTC | The document date in the company's time zone |
| 12 | The desk shows the service's standard name, the documents the company's label | Kept; the company's own labels come to the desk with the services page later |
| 13 | A shipment of an inactive warehouse is off the desk but opens by link | Kept: staff with the permission may still read it |
| 14 | Test gaps (another warehouse, view-only, CSV formulas) | The scope and CSV code are shared with the incoming list and parcels, whose tests cover them; noted for the next desk change |

