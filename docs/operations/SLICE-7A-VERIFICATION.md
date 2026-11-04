# Slice 7 step 1: Destination receiving - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-21 |
| Built on | Slice 6C, operational events (package 84); not committed here: the dev team owns git |
| Design | [Slice 7 design note](SLICE-7-DESTINATION-DESIGN.md), sections 2 (R-1 to R-3, R-11, the owner's answers) and 4 (S-1 to S-11, settled while building) |
| Status | **Built, reviewed independently, fixed and verified by automated tests.** Step 2 (the pickup counter) next |

## 1. What was built

- **Arrivals** (`/staff/arrivals`): a destination warehouse sees the dispatches on their way to it (or to the hub that serves it), oldest first, and the last ones received.
- **Receiving a dispatch** (`/staff/arrivals/{id}`, R-1): each bundle is scanned off the truck (the leg under way arrives with the first one); the bundles that end here are opened and their parcels scanned (a repack box number receives its parcels). A parcel scanned first receives and opens its bundle. Repeats change nothing and say so.
- **Customs** (R-3): a *Customs is cleared here* checkbox on the warehouse. There, *with customs*, *cleared* or *held* (with a line for the customer) is recorded on the whole dispatch or one bundle, on what has arrived. A held parcel cannot leave on a dispatch or be made ready.
- **Ready for pickup**: at its own warehouse, scanned, not held, cleared where customs is cleared. The customer gets one email per shipment when every parcel still going is ready (*Settings → Events*, on by default), with the place, its address and hours.
- **Discrepancies** (R-2, `/staff/arrivals/discrepancies`): items not on the dispatch are noted as extra; finishing lists what was not scanned as missing (refused while a bundle that ends here is unopened). A late scan closes the line as found. Staff at the destination or the origin resolve a line as *lost* (an exception *Missing in transit* on the parcels, and the customer's email when the company sends them) or *settled*, with a note.
- **Onward from a hub** (R-11): the hub receives a dispatch for the warehouse it serves; the bundle goes on unopened on an onward dispatch; the customer sees *In transit at* the hub, never *manifested* or *departed* twice.
- **Rights**: *receive dispatches* and *record customs*, per warehouse.

## 2. Database

The global script adds **DispatchBundles** (the bundles each dispatch carried, when each arrived or went missing) and **ArrivalDiscrepancies**; on **Dispatches** `ReceivingWarehouseId`, `Onward` and `ReceivedUtc`; on **Bundles** `CurrentWarehouseId`, `ArrivedUtc` and `OpenedUtc`; on **Warehouses** `ClearsCustoms`; on **Shipments** `PickupNotifiedUtc`; on **TenantSettings** `PickupEmails` (on).

Data steps, once: a link row for every bundle already on a dispatch; each dispatch's receiving warehouse (the hub serving its destination unless it leaves from that hub, then *onward*); dispatches already arrived are taken as received (S-11).

Checked: the script runs on an empty database twice, and twice on a database upgraded through every earlier package to package 84 with demo data; the data steps were checked on package-84 rows (a dispatch through a hub, an onward one with its bundle).

## 3. Independent review and fixes

| # | Finding | Fix |
|---|---|---|
| 1 | A late scan at another warehouse moved a bundle and its parcels back, with no event | A dispatch is received at one warehouse fixed when planned; what arrived is kept on the dispatch's link to each bundle; a repeat changes nothing; a bundle gone on since is refused |
| 2 | Finishing at a hub after the bundle went on listed it as missing | *Arrived* is read from the dispatch's link, not from where the bundle is now |
| 3 | A dispatch from a hub in another country could be received nowhere; a hub changed in transit moved receiving | The receiving warehouse is fixed when the dispatch is planned; leaving from the destination's hub is onward |
| 4 | Customs reached parcels not arrived or gone on | Only bundles scanned in on the dispatch and still here; once a bundle is opened, only its parcels scanned in (second pass) |
| 5 | Old dispatches filled *On their way here*; the list was filtered after a cut of 200 | Filtered by the receiving warehouse in the database; dispatches arrived before this slice taken as received |
| 6 | One query per missing line when finishing, one per shipment for the email | One query for all open lines; the email decided by its handler |
| 7 | The last two parcels scanned at once sent no email | Each request writes a notice; the handler sends once, when the whole shipment is ready, and marks the shipment |
| 8 | Closed lines showed a resolve form; *lost* offered for extra items | Only open lines, for those who receive; *lost* only for missing ones |
| 9 | A dispatch whose last leg never departed was not arrived by the first scan | The latest departed leg arrives |
| 10 | Finishing and a scan at the same time could leave a stale missing line | Both change the bundle's link row: one of them waits and is retried |
| 11 | One failure path of finishing kept its changes | Discarded |
| 12 | The page repeated any text from its address as a success message | Only a number's shape is shown |
| 13 | Bundles already at their journey's end counted as free for a dispatch | Left out |
| 14 | Second pass: the data step routed older same-country dispatches through the hub, unlike new ones | Same rule as new dispatches |

The second pass confirmed the fixes. Noted, not changed (low):
- A dispatch with a later leg never departed stays *departed* after receiving; the leg is departed and arrived from the dispatch page as before.
- A parcel scanned at the very moment receiving finishes may leave its *missing* line open; staff close it as *settled*.
- If saving after the ready-for-pickup email fails (a conflict on the shipment), the email may be sent a second time on retry: a duplicate is preferred to a lost email.
- A ready-for-pickup notice is written each time a parcel becomes ready; the handler sends only the last one.
- Dispatches that arrived before this slice are taken as received without listing missing parcels (S-11).

## 4. Automated tests

| Suite | Result |
|---|---|
| Operations (domain, application, infrastructure, web, API, customer web, architecture) | All pass: domain 241, application 91, infrastructure 65, web 100, API 30, customer web 40, customer infrastructure 1, architecture 12 982 |

New for this step:

- Domain (3): a discrepancy found by itself, resolved as lost or settled with a note, once; customs held blocks and cleared releases, with the destination milestones and what the customer sees; a warehouse clears customs as its terms say.
- `Arrivals_ReceiveBundlesThenParcels_ClearCustoms_AndListWhatIsMissing` (web): listed at its destination, not found at the origin; customs refused before anything arrived; an unknown number refused; the bundle received (the leg arrives with it), twice is once; finishing refused while unopened; opened, one parcel scanned; held with a line (only the parcel scanned in), then cleared for the bundle: ready; finished with one parcel missing; *lost* needs a note, tells the customer, and cannot be resolved twice; the parcel found late arrives, is cleared and ready; one email for the shipment; someone who only fills bundles sees nothing.
- `Arrivals_AHubReceivesForTheWarehouseItServes_AndSendsTheBundleOn` (web): bound for the hub, not the warehouse; received and finished at the hub (in transit there); sent on (onward); scanned at the warehouse it ends at: arrived, opened, ready; scanned again at the hub on the first dispatch: nothing moves back; each customer event once.

Not covered by an automated test yet: a dispatch to a country only received by the first warehouse that scans it; two scans at the same instant.

## 5. Styling

New screens are in the [UI styling inventory](UI-STYLING-INVENTORY.md) as *Needs styling*: Arrivals, Receive a dispatch, Discrepancies; the Warehouse and Events settings rows note their new checkbox.
