# Slice 5D: Insurance choices and the shipment estimate - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-20 |
| Built on | Slice 5C and its follow-ups (package 73); not committed here: the dev team owns git |
| Design | [Slice 5D design note](SLICE-5D-INSURANCE-CHOICES-AND-ESTIMATE-DESIGN.md), agreed with the owner on 2026-10-20 |
| Status | **Steps 1, 1b and 2 built, reviewed independently, fixed and verified by automated tests; waiting for the owner's look, in particular at the *what if* block (E-2).** Step 3 (taxes, delivery fees, the invoice) comes with billing |

## 1. What was built

### Insurance products (step 1, D-1 to D-7)

- **Prices → Insurance** lists the company's **products**. A product has a name ("Basic", "Full value"), a position in the list, a short **pitch** in English, French and Spanish, and everything a plan already had: its mode, its price rule (percentage with a minimum, or bands, with an optional maximum value), its coverage terms and its policy and insurer (Slice 5B). A company may have **several products per mode**.
- A new company setting on the same page: **Staff may change the insurance of an open shipment** (default on).
- **At the counter**, the new shipment form and the shipment page list the products for the service with their rule and pitch, and, once parcels are in, **the premium and the total each one makes**. The clerk ticks *the customer accepts the terms*; the shipment records the product, the time, and the clerk as **the one who recorded** the acceptance (never as the one who accepted).
- **On the portal**, the new and change shipment forms list the same products; the customer chooses one or none and accepts the terms.
- The **certificate** names the product chosen (its policy and insurer).
- **Existing data:** each plan per mode becomes one product named after its mode ("Air insurance"); insured shipments point to it. A company with one product sees one line.

### The page to sign and the signature pad (step 1b, D-8)

- **Print for signature** on the counter's shipment page: one PDF page, in the **customer's language**, with the company's name, the shipment number and barcode, the route and recipient, each parcel's contents, declared value and weight, the totals, and either the insurance product with its insured value, premium and main terms, or *no insurance* with the company's liability words. A signature line at the bottom.
- **Sign on screen:** a pad on the same page (mouse, touch screen, tablet or a signature pad that acts as a pen). The signature is kept with the shipment (a PNG of at most 200 KB, checked by its first bytes), the clerk present is recorded, it is printed on the page, and the page says *Signed on screen on ...*.

### The shipment estimate (step 2, E-1 to E-6)

- A side panel titled **Shipment estimate**, kept in view while scrolling (under the form on narrow screens), on the counter's customer page (new shipment), the counter's shipment page, and the portal's new and change shipment forms.
- **Before any parcel:** the service's rates (price per kg or volume, the minimum), the shipment's charges, and the insurance on the **value the customer expects** to declare (an optional field, never saved).
- **With parcels (counter):** each parcel with its number, size, billed size and cost, then the shipment's charges, the insurance with the insured value, and the total. The portal never shows parcel costs.
- **What if:** each insurance product with the total it would make, *no insurance*, and consolidation the other way when it changes the price. A product the value is too high for says *not possible for this value*.
- It follows every change: a small script sends the form to a read-only estimate address as the clerk types (300 ms pause); without the script, each saved change redraws the page.

## 2. Database

The global script adds:

- **InsurancePlans:** `Name`, `PitchEn`, `PitchFr`, `PitchEs`, `Position`; the unique index on (tenant, mode) becomes a plain one on (tenant, mode, position).
- **Shipments:** `InsurancePlanId` (foreign key to the product), `InsuranceRecordedByUserId`, `CustomerSignatureKey`, `CustomerSignedUtc`, `CustomerSignatureRecordedByUserId`.
- **TenantSettings:** `StaffMayChangeInsurance` (default on).
- Data: existing plans named after their mode; insured shipments linked to their mode's product.

Checked: the script runs on an empty database twice, and twice on a database upgraded through every earlier package to package 73 with demo data and insured shipments (the products are named and the shipments linked). The data step runs inside `EXEC`, found by that upgrade test: the script compiles each step before the new columns exist.

## 3. Independent review and fixes

An independent review of the whole slice found 13 points; all are fixed.

| # | Finding | Fix |
|---|---|---|
| 1 | The certificate looked up the product by mode, so with two products it could print the wrong one | It uses the product on the certificate |
| 2 | The estimate showed a product's minimum premium for a value above its maximum | The product says *not possible for this value*; no insurance line on that value |
| 3 | The page to sign failed for a shipment without a number yet | The shipment gets its number first, as its other documents |
| 4 | A signature stayed after the insurance, the parcels or the route changed, and could be replaced after booking | Any change to what was signed for (route, recipient, service, options, insurance, a new parcel) clears it; signing only while the shipment is open; the pad is hidden otherwise |
| 5 | Archiving a policy found the products to move by mode | It takes each product of the policy |
| 6 | On the portal, a product the company no longer offers left no choice ticked and dropped the insurance silently | The form says so and asks the customer to choose again |
| 7 | The page to sign did not check that the shipment leaves from the clerk's counter | It does, as every counter action |
| 8 | No rate limit on the estimate addresses | Counter: 240 per minute per address; portal: 60 per minute per address |
| 9 | The premium on the expected value was not rounded | Rounded to the cent |
| 10 | With no service chosen, any mode's product was accepted | Only the products of the mode the shipment is priced in |
| 11 | Signing again kept the earlier image in the file store | The earlier one is removed |
| 12 | A change of insurance at the counter left no trace of who removed it | Each change is in the history, with the product before and after |
| 13 | The expected value did not read "1,234.56" | Amounts read with thousands separators ("1,234.56", "1.234,56", "1 234") |

Known and left as is: a signature recorded before a parcel's value is corrected at inspection stays (the clerk prints again when needed); a signature cleared by a change keeps its image in the file store as evidence.

## 4. Automated tests

| Suite | Result |
|---|---|
| Operations (domain, application, infrastructure, web, API, customer web, architecture) | All pass: domain 201, application 91, infrastructure 65, web 89, API 29, customer web 40, customer infrastructure 1, architecture 11 157 |
| Platform | All pass (kernel 85, application 235, domain 307, infrastructure 63, web 43, API 21, UI 9) |
| Public | All pass (23) |

New or extended for this slice:

- The counter offers each product with its price, records the customer's choice and the clerk who recorded it, refuses insuring without acceptance, and follows the company setting.
- The estimate before any parcel (rates, the premium on the expected value), with parcels (each parcel, what if), and above a product's maximum (*not possible*, read with a thousands separator).
- The page to sign before any parcel and after; the signature refused when not a PNG, kept when valid, cleared when the insurance changes.
- Domain: the product and acceptance kept on the shipment; the signature holds for the same instructions, clears on a parcel, a service or an insurance change, and is refused once booked.
- The portal estimate and the product choice.

## 5. Styling

New pieces are listed in the styling inventory (estimate panel, insurance choice, signature pad): `ops-counter__layout`, `ops-counter__main`, `ops-counter__estimate` (and its `-lines`, `-parcel`, `-rate`, `-final`), `ops-counter__what-if`, `ops-counter__sign`, `ops-counter__pad`; on the portal `customer-portal__with-estimate`, `customer-portal__estimate` (and its `-lines`, `-rate`), `customer-portal__what-if`, `customer-portal__insurance`. The new product notice reuses `customer-portal__notice`.

## 6. For the owner to look at

- **What if (E-2):** built as described so the owner can see it, then keep, change or drop it.
- The portal's shipment page (after creating) does not show the estimate yet; the new and change forms do.
- Vendor signature pad drivers (Topaz, Wacom) are not included; pads that act as a pen or touch device work now.
