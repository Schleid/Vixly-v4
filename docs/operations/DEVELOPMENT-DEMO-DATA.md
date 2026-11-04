# Demo day: sample data for local testing (Development only)

A working day of two fictional companies, so you can sign in, click through, and compare what Vixly decided with what you expected. Every record below went through the same rules as real work: customers announced and created shipments on the portal, the dock scanned, the station checked, staff assigned and shelved. Only dates were moved back afterwards (a package 40 days old cannot arrive today).

## How to load it

1. Start Platform.Api, then Operations.Web, in **Development** (as for the [demo users](DEVELOPMENT-DEMO-USERS.md)).
2. On start-up Operations.Web fills the companies listed in `OperationsDemoData:ScenarioSlugs`: `bluewater-shipping` (the story below) and `andes-courier` (a second company, to check that one never sees the other).
3. On a new database, start everything, then create the first SuperUser in Control. Control adds its demo tenants as soon as that user exists, and Operations.Web picks them up within a minute: no restart needed (each waits up to an hour; `OperationsDemoData:WaitForTenantsMinutes`).
4. It runs once per company (it stops if the company already has a warehouse coded `MIA`). The log says "sample day added". If a step is refused, the log names the step and the reason. To load it again, start from a fresh database.

## Who signs in

Password for everyone: `Harbour demo 2026!`. Staff second step: the demo authenticator key in [DEVELOPMENT-DEMO-USERS.md](DEVELOPMENT-DEMO-USERS.md).

| Who | Email | Where |
|---|---|---|
| Administrator, Bluewater | `admin@bluewater-shipping.example` | `https://bluewater-shipping.localhost:7404/staff` |
| Ana Pierre (customer, French) | `ana@bluewater-shipping.example` | `https://bluewater-shipping.localhost:7404/customer/sign-in` |
| Marc Dubois, Dubois Cartons (customer, French) | `marc@bluewater-shipping.example` | same |
| Lucie Jean (customer, English) | `lucie@bluewater-shipping.example` | same |
| Jean-Baptiste Noël (customer, French) | `jb@bluewater-shipping.example` | same |
| Administrator, Andes Courier | `admin@andes-courier.example` | `https://andes-courier.localhost:7404/staff` |
| Rosa Quispe (customer, Spanish) | `rosa@andes-courier.example` | `https://andes-courier.localhost:7404/customer/sign-in` |

Customer codes (such as `C00002`) are given in order when the day is loaded; the staff customer list shows them.

## Bluewater Shipping: the company

| Setting | Value | Rule it shows |
|---|---|---|
| Units and currency | lb, in, USD; New York time | Each company chooses kg or lb (owner, Slice 4) |
| Origin | **MIA**, Doral receiving (Miami): receiving and storage | Customers' mailbox address; the dock and the station |
| Destinations | **PAP** Port-au-Prince hub (pickup and home), **CAY** Les Cayes office and **JER** Jérémie office (pickup; Les Cayes also home delivery; both through the PAP hub), **SDQ** Santo Domingo office, **LIM** Lima, run by the partner *Rímac Entregas* | Destinations and hubs (Slice 4C); a destination run by a partner (owner, 2026-10-19) |
| Shelves at MIA | A-01-1, A-01-2, B-01-1 (storage), H-01 (hold), U-01 and U-02 (**undeclared area**) | Putaway; the undeclared area (Slice 4D step 3) |
| Services | Air standard, air express, ocean consolidated | The company picks from Vixly's catalogue |
| Prices, air standard | 0 to 10 lb: **$50 flat**; 10 to 20 lb: **$65 flat**; above: **$3.25 per lb**; minimum $50. Les Cayes: $55 / $72 / $3.60; Jérémie: $58 / $75 / $3.75 | Flat bands then per lb, dearer per destination (owner, Slice 4) |
| Prices, others | Air express $6.50 per lb (min $35); ocean consolidated $1.40 per lb (min $40) | |
| Extra fee | **CONATEL fee, $10 per item**, when the declared contents mention an iPhone, iPad or Samsung Galaxy, to Haiti | Conditional extra charges by item and destination (owner) |
| Insurance cover sold | Air 3% of the declared value (min $5, up to $5,000); ocean 2% (min $10, up to $10,000); both under the open cargo policy below | The cover customers buy (Slice 4C), naming its policy (Slice 5B) |
| Shipping supplies | BOX-S small box $1.75, **BOX-M** medium box $2.50 (barcode 012345678905; 10 or more at $2.00), BOX-L large box $3.75, TAPE-48 packing tape $4.50 a roll, WRAP-12 bubble wrap $6.00 a roll, BAG-POLY poly mailer $1.25 (10 or more at $1.00); no stock yet (receive some at MIA to try the stock) | Supplies sold at the counter (Slice 5E); created only when the demo company is first seeded |
| Company policies | **Atlantic Marine OC-2026-11873**, open cargo for customers' shipments (declared monthly); **Gulfstream Mutual FFL-55-0921**, freight forwarder's liability for the company's own operations, **ending within 30 days** (flagged) | Two kinds of company insurance (I-6) |
| Not insured | By air: up to $9.07 per lb, at most $100 a parcel; by sea: $50 a parcel | The company's liability terms (I-3) |
| Storage | 10 free days, then **$0.50 a day** | Storage fee for packages nobody claims (Slice 4D step 3) |

## The customers and their packages

### Ana Pierre: one shipment, four packages at four stages

Ana created **one shipment to her home in Les Cayes**, by air standard, insured (her **insurance certificate** is on the shipment page), and put four pre-alerts in it.

| Package | What happened | Where to see it | Rule |
|---|---|---|---|
| Amazon `TBA301884422190`, running shoes, $89.90 | Scanned at the dock, placed **in her shipment** (its pre-alert was there); checked intact, 4.2 lb; cleared, **shipping label printed**; shelved at A-01-1 | Staff: shipment page (label printed); Ana: shipment page, history with photos | A pre-alerted package goes to the shipment its pre-alert is in (A-2); cleared means checked, no hold, in a real shipment (section 5) |
| UPS `1Z999AA10123456701`, summer dresses | Same, but nobody printed its label yet | Staff: **Labels to print** | A cleared parcel without its label waits in the list |
| USPS `9400111899223344556677`, vitamins | Expected in 3 days | Staff: **Incoming**, view *Expected* | The incoming list (A-7) |
| FedEx `771234567890`, phone case | Expected 6 days ago, never arrived | Staff: **Incoming**, view *Past expected date* | "Past delivery" (A-7) |

Ana also opened a **ticket** (*Missing packages*): Nike says the shoes `1ZW45E670323454421` were delivered two days ago; she sent the carrier's proof. Staff see it under **Tickets**, with the undeclared UPS package `1Z999AA10123454421` as a lookalike (**the same last 8 characters**), whose label shows "A. Pierre" and the sender Nike. Staff can give it to her from the ticket. (A-6)

### Marc Dubois (Dubois Cartons): two shipments at once

| Shipment | What happened | Rule |
|---|---|---|
| To **Santo Domingo** (pickup, Rafael Méndez), ocean consolidated, **no package in it yet** | Cartons he drops off himself; the counter that receives them comes in Slice 5 | A shipment needs no pre-alert (A-3) |
| To **Lima** (home delivery, Carlos Rojas), air standard | He pre-alerted Amazon tape `TBA302775533110` and put it in this shipment. It arrived **damaged**: checked with a damage photo, 11.8 lb, put on hold (shelf H-01); Marc is asked what to do | One customer, several shipments to different countries (A-4); transport damage: photograph and notify (A-10) |

Staff also made Marc a **quote**: four packs of 250 lb (40 x 30 x 30 in) to Santo Domingo by air, with a **preferred price of $2,900** for a valued customer. (owner, Slice 4)

### Lucie Jean: two packages without a shipment

| Package | What happened | Rule |
|---|---|---|
| UPS `1Z999AA10123456702`, bed sheets | She announced it but put it in no shipment. The dock matched it to her, put it in her **waiting shipment** with the hold **awaiting shipment** | Received and announced but in no shipment: it waits for her to place it (section 3) |
| FedEx `783300112233`, a Best Buy box she never announced | Nobody could place it at the dock (**unidentified**). The clerk recorded the label: "Lucie Jean", her code, sender Best Buy. Staff then **assigned** it to her with the proof "the mailbox code on the label". It joined her waiting shipment, held **awaiting customer information** | Undeclared: owner found by the label, checked by staff (section 6) |

### Jean-Baptiste Noël: an iPhone to Jérémie

He created a shipment to the **Jérémie office** by air express and put in his pre-alert for a new iPhone ($899). It arrived and was checked; the clerk **flagged it for a compliance inspection** ("check the CONATEL declaration"), so it waits in the **inspection queue** and is not cleared. Its estimate carries the **CONATEL fee**.

## The undeclared area

| Package | State | Rule |
|---|---|---|
| UPS `1Z999AA10123454421` | Label recorded ("A. Pierre", Nike), shelf U-01; resembles Ana's ticket | Inventory of undeclared packages (A-1) |
| Amazon `TBA309911002244` | Nothing readable, shelf U-01 | |
| FedEx `789900112277` | Arrived **40 days ago**, nobody claimed it, shelf U-02. On the **Holds** page under *Unclaimed*, with its storage fee so far ($15.00: 30 days past the 10 free days at $0.50); the Owner or an administrator decides **return to sender, dispose of, or auction** | Aging, storage fee and disposition (A-8, Q-D3, Q-D4) |

The **monthly count** of the undeclared area was started this morning (**Parcels → Counts**): it lists these three, to mark found or missing.

## Today's deliveries

Four carrier deliveries were received and **closed**, each with its **warehouse receipt** (Amazon 3 packages, UPS 3, FedEx 3, and the old FedEx one). No label printer is set up in the demo, so each parcel's ID label prints from its page (PDF).

## Andes Courier: the second company

Rosa Quispe pre-alerted an instant camera `TBA401122334455`, put it in a shipment to the San Isidro office, and it arrived and was checked. Signed in at Bluewater, you will find **no trace** of Rosa, her shipment or her package; signed in at Andes Courier, none of Bluewater's. Both companies even use the warehouse code `MIA`: codes are per company.

## What you can try

- Sign in as **Ana** and put the phone case in another shipment, or open the shoes and look at the photos and history.
- Sign in as **Marc** and answer the damage hold on the tape.
- Sign in as **Lucie** and put her bed sheets in a new shipment: the hold ends and the parcel is cleared.
- As **staff**, give the undeclared UPS package to Ana from her ticket; print the dresses' label; download a shipment's packing list and pro forma invoice; mark the count; decide the unclaimed package.
