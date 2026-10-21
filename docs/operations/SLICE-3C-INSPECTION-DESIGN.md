# Slice 3C: Inspection and holds - design note

| | |
|---|---|
| Plane | Operations (Stage 3) |
| Date | 2026-10-18 |
| Builds on | 3B (inspection station, random draw, holds opened at the dock and the station); ADR-0018, ADR-0021, ADR-0022; [Slice 3 design](SLICE-3-RECEIVING-DESIGN.md) section 5 |
| Status | **Owner answers 2026-10-18: all four settled.** Step 1, the customer's shipment (section 1a), and step 2, inspection and holds (sections 3 to 6), built 2026-10-19; see the [verification record](SLICE-3C-VERIFICATION.md) |

## 1. Outcome

- Every company keeps its own **restricted-items list** and **inspection rules**. The first check at the station sends a parcel to compliance inspection when a rule matches, when the random draw picks it, or when the clerk flags it.
- Each warehouse has an **inspection queue**, oldest first. The inspector opens the box, compares it with the declaration and the list, takes photos, and records an **outcome**.
- Staff see and manage **holds** in one place. The customer side of holds (answering on the portal, reminders, unclaimed packages) is 3E.

## 1a. Step 1: the customer creates the shipment (owner, 2026-10-18)

The owner's answer to Q-C1: the customer creates the shipment when announcing the package, saying where it goes, who receives it and the options. The warehouse only adds the parcels it measures under that shipment. This is the "instructions up front" model of Caribbean and Latin American couriers, extended so the customer builds the whole shipment; it is built before the rest of 3C because inspection rules need the destination.

- **On the portal** (`/customer/packages/shipments/new`): the mailbox warehouse it ships from (when the customer has several); pickup at a destination branch or delivery to an address in a country the company serves, as the company's settings offer; the recipient's name and phone (the customer by default); consolidate, refrigerated, insured; and the first package. More packages are announced in it; the instructions change while it is open; it is cancelled while none of its packages arrived, with its announcements.
- **At the dock**: a package matched to an announcement joins the shipment it was announced in, when that shipment is open at this warehouse. Otherwise (no announcement, an older one without a shipment, another warehouse) it waits in the customer's one shipment without instructions at the warehouse, on hold *awaiting customer information*. The customer gives that shipment its instructions and declares the package by its tracking number; the hold is then released.
- **Staff** see the instructions on the parcel page and at the inspection station; only the customer writes them (D-C6).
- **Inspection rules** use the shipment's destination country from the first check (Q-C1 answered).
- Prices for the options come with Slice 4; booking (service, number) with Slice 5. The consolidation waiting limit and the upload of the purchase invoice are noted for 3E and Slice 6.

## 2. What 3B already does

- The inspection station records the condition, photos, weight and size.
- It draws about one parcel in twenty for compliance inspection (random only, fixed at 5 percent).
- A parcel can be put on hold as *damaged*, *unidentified* or *awaiting customer information*; holds block nothing yet, because nothing ships before Slice 5.

## 3. Restricted items and rules (settings, `ops.inspection.configure`)

**Restricted-items list.** Each entry has:
- a name (for example *Lithium batteries*);
- the words that point to it in a declaration (*battery, batteries, power bank, lithium*);
- prohibited or restricted;
- where it applies: all destinations or chosen countries; all carriers or chosen ones;
- what is needed when it is restricted (a permit, a document, special packing).

Vixly ships a starting list (lithium batteries, aerosols, flammable liquids, medicine, firearms and parts, currency, perishable food, items needing a permit; D-R5). A company copies it on first use and owns its copy.

**Inspection rules.** Each rule sends matching parcels to compliance inspection:
- a declaration that names an item of the list (by its words);
- a declared value at or above an amount;
- a carrier;
- a destination country (see Q-C1).

**Random share.** A company setting, default 5 percent, 0 to 100.

## 4. Selection at the first check

At the inspection station, after the condition check:
1. The rules are checked against the declaration, value, carrier and destination. Each match is recorded: *inspection required (rule: Lithium batteries)*.
2. Otherwise the random draw runs with the company's share.
3. The clerk may also **flag** a parcel ("looks suspicious") with a reason.

The station says "set it aside for inspection" and the parcel joins the warehouse's queue.

## 5. The inspection queue and the inspection (`ops.inspection.perform`)

- **`/staff/inspection/queue`**: parcels waiting for inspection at the warehouses the person covers, oldest first, with why each was chosen. Scanning an ID label opens it.
- **Inspecting**:
  - start (the parcel shows *inspection in progress*);
  - compare the contents with the declaration, item by item if needed;
  - mark any listed item found; the page shows what the list says is needed for it;
  - photos of the contents;
  - notes.
- **Outcomes**, each a tracking event (ADR-0021):
  - **passed**;
  - **passed with notes**;
  - **held**: the customer must send a document, a permit, a corrected value, or pay a fee. The inspector writes what to do; the customer sees it (portal in 3E, email now);
  - **repacked**;
  - **refused, return to sender**;
  - **refused, dispose of**: needs a second person's approval (Q-C3).
- A refused parcel stays on hold until it leaves; *Disposed* is its last milestone (ADR-0018).

## 6. Holds (`/staff/holds`, `ops.holds.manage`)

- All open holds at the warehouses the person covers, by reason, oldest first, with the parcel, the customer and what is awaited.
- **Release** a hold with a note (for example "customer sent the invoice by email"); the history records who released it.
- Holds already block consolidation, dispatch and handover in the projection (ADR-0018); Slices 5 to 8 check it.

## 7. Questions for the owner

*Answered 2026-10-18: Q-C1 the customer creates the shipment with its destination (section 1a); Q-C2 agreed; Q-C3 the company calls the customer, then follows its own policy; Q-C4 agreed, to be reviewed in use.*

1. **Q-C1 Destination at receipt.** Settled (owner): the customer creates the shipment with its destination when announcing the package (section 1a), so the destination is known from the first check. A package that arrived without instructions is checked again when the customer gives them.
2. **Q-C2 Matching declarations.** Settled (owner): the list's words flag the parcel for inspection, and the inspector confirms what is really inside.
3. **Q-C3 Disposal.** Settled (owner): the company calls the customer first, then acts according to its own policy. So *refused, dispose of* records that the customer was called (when, by whom, what was said) and needs the approval of the Owner or an administrator; the customer is also told by email.
4. **Q-C4 Hold fees.** Settled (owner, "let's see how it turns out"): until Slice 4 the inspector types the amount; it is paid with the shipment's invoice (Slice 9) and moves to the company's fee list in Slice 4.

## 8. Data

| Class | Table | Notes |
|---|---|---|
| `OperationsRestrictedItem` | `RestrictedItems` | Per tenant: name, words, prohibited or restricted, destinations, carriers, what is needed, in use |
| `OperationsInspectionRule` | `InspectionRules` | Per tenant: kind (listed item, value, carrier, destination), its value, in use |
| `OperationsInspection` | `Inspections` | Parcel, why chosen, started and finished by and when, items found, outcome, notes, fee and what the customer must do |
| `OperationsTenantSettings` | (existing) | Random share |

## 9. Done when

- A rule match, the random draw and a clerk's flag each send a parcel to the queue, with the reason recorded.
- Each outcome is a tracking event; held and refused parcels show on the holds page; disposal needs the second approval.
- A warehouse-scoped inspector sees and changes only that warehouse's parcels.
- Every new text in en, fr and es; no "Vixly" on any tenant page.
