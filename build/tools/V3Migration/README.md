# V3 → V4 migration tool

Copies **one company's people and open work** from V3 into its V4 company (owner decision, 2026-10-23). It is a one-off tool: it reads V3 and never writes to it, and no V4 site references it (ADR-0002 O-2, Global Standard M-2).

## What it copies

| V3 | V4 | Notes |
|---|---|---|
| Staff with an active account | Staff, **same password** | Matched by email: a person already in V4 (the owner) is skipped. V3 administrators (and the V3 owner when V4's owner is someone else) get the **Administrator** role; the others get no role, and the report lists them for you. Administrators set up the authenticator at their first sign-in. |
| Customers | Customers, active and verified, **same portal password** | Matched by email. The V3 code is kept without its dash (`CUST-0001` → `CUST0001`). Phone numbers become international (`+1…`, `+509…`); one it cannot read is listed and not copied. Language: English (change it in V4 if needed). |
| Open shipments (draft or created) | Counter shipments, **V3 number kept** | Pickup at the destination warehouse with the same code, same service (VXA → air standard, VXE → air express, VXS → ocean, VXG → ground), parcel count, recipient and consolidation. |
| Their parcels | Parcels, **V3 number kept** | Received and measured at the origin counter on V3's date (lb/in converted to kg/cm), with the same tracking as the V4 counter. |
| An issued or collect invoice of a complete shipment | The shipment **booked at V3's amount** and its V4 invoice with V3's lines | Collect stays "pay at the destination". A payment V3 recorded is listed for you to record at the cashier. |
| A draft invoice | Not copied | It was never issued; the shipment is priced with V4's prices when its last parcel is entered. |

**Not copied**, because it is set up fresh in V4:
- prices, services, extras, insurance, supplies;
- the company's settings, plan, subscription and Terms acceptance.

V3's history of finished work stays in V3's database, kept as a backup.

## Before running
1. **The company:** registered and activated in V4 Control, with the same slug. The owner has signed in to Operations once.
2. **The settings:** in V4 Operations, the owner saves Settings (currency, units).
3. **The warehouses:** created with **the same codes as V3** (`US-FLL`, `HT-CAY`). The destination is a pickup point.
4. **Services and prices:** set up, at least the services V3's open shipments use.
5. **V3 frozen:** no new work in V3 from this moment. Take a backup.

## Running it (on your PC, from the V4 folder)
Connection strings go in environment variables only, in that terminal:

```powershell
$env:V3_DB = "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True"   # V3 (or a restored copy)
$env:V4_DB = "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True"   # V4 production
dotnet run --project build/tools/V3Migration -- --tenant <V4 company id>
```

- **Dry run:** without `--apply` it checks everything inside a transaction and **rolls it back**. Read the report (also saved as `v3-migration-<time>.txt`).
- **Apply:** when the report is right, run it again with `--apply`. Any error rolls everything back.
- **Running twice is safe:** everything already copied is skipped.
- **Options:**
  - `--v3-tenant <id>` when V3 holds several companies;
  - `--units kg` when V3 measured in kg and cm (default: lb and in).
- **The V4 company id** is on the company's page in Control.

Close the terminal afterwards, so the connection strings are gone.

## The report
- **One line per row:** each person, customer, shipment, parcel and invoice: copied, skipped (already in V4) or PROBLEM.
- **Totals and money:** V3's invoiced money against V4's, which must say **(equal)**.
- **"For the owner, in V4":** what to do by hand (roles, draft invoices, payments to record).
- **Exit code:** 0 when clean, 2 when there were problems, 1 when it stopped and saved nothing.

## Rehearsed
- **On:** a copy of V3 production (2026-10-23 backup) into an empty V4 database with the company, settings and warehouses set up.
- **Copied:** 4 staff (2 administrators), 8 customers, 2 open shipments, 1 parcel with its tracking, 1 collect invoice. Invoiced money was equal: 301.42 in both.
- **Running again** copied nothing (14 skipped).
