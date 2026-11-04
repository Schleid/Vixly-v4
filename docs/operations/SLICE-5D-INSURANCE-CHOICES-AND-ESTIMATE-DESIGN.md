# Slice 5D: Insurance choices and the cost estimate panel - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-20 |
| Builds on | Slice 4B/4C (prices, insurance plans per mode), 5B (policies, certificates), 5C (the counter) |
| Source | The owner, 2026-10-20 afternoon: "the customer says add insurance and tell me the cost now: the clerk cannot answer that"; "a company might have two insurance companies… which one do you want?" |
| Status | **Agreed with the owner on 2026-10-20 (afternoon); building.** Points marked *industry standard* follow current practice, checked when decided, as the owner asked; each company can set them its own way |

## 1. What the owner said

- The clerk must always be able to answer *"what does it cost now?"*. In V3 a side panel shows the detailed financial information.
- The panel is **always an estimate**, even with every parcel in: no taxes, no delivery fees.
- Before any parcel: the shipment's charges (service, consolidation, insurance...). With parcels: each parcel with its size and cost. It **follows every change** to the shipment.
- The same panel on the **customer portal** while the customer builds a shipment, without parcel costs (the customer does not enter parcels).
- **Always visible:** it stays on screen while the clerk scrolls.
- A company may sell insurance from **several insurers**, each with its own price and advantages. Ticking *insured* shows the list of insurance products with their price; choosing one shows what it covers.
- Limits on who may buy which product: industry standard, flexible for each company.
- Whether staff may change the insurance choice of an open shipment: **a company setting**.

## 2. Decisions

### Insurance products (replacing "one plan per mode")

| # | Decision |
|---|---|
| D-1 | **An insurance product** is what the company sells: a name ("Basic", "Full value"), the transport modes it applies to, the policy and insurer it sits under (Slice 5B), its price rule, its coverage terms (already in Slice 4C: risks, exclusions, payout, deductible, limits, claim deadlines, terms text in three languages) and a **short pitch**. A company may have **several active products per mode**, in the order it chooses. |
| D-2 | **The pitch** is generated from the terms (what is covered, the limit, the deductible, the claim deadline) and the company may rewrite it, in each language. Full terms are one click away. |
| D-3 | **Price, industry standard** (checked 2026-10-20: cargo cover on (invoice value + freight) × 1.1 for forwarders; carriers per package on declared value, first $100 included, then per $100): a percentage of the **insured value** with a **minimum premium per shipment**, or a premium per band of value. The insured value is the **sum of the parcels' declared values** of the shipment. Options per product: insured value plus the freight (*CIF*), plus an uplift (often 10%); a maximum insured value per parcel and per shipment. Per-parcel pricing is a setting (courier style). |
| D-4 | **Eligibility, flexible:** optional minimum and maximum declared value, destinations (countries or destination warehouses) and customers or customer groups; excluded contents categories when the company keeps a list. None set: open to everyone on its modes. |
| D-5 | **The shipment records the product chosen** (or none), the product version, and the **customer's acceptance** of its terms: on the portal by the customer; at the counter the customer accepts and the clerk records it (the clerk's name is kept as the one who recorded it, never as the one who accepted). The certificate names that product's policy and insurer. |
| D-8 | **Signed paper and signature pad** (owner, 2026-10-20): the counter prints one page for the customer to sign: the shipment, each parcel's contents and declared value (the customer's declaration), the insurance product with its insured value, premium and a summary of its terms, or *no insurance* with the company's liability, and a signature line. The customer may instead **sign on screen** (a signature pad, a tablet or a touch screen): the signature is kept with the shipment and printed on the page. Pads that act as a pen or touch device work in the browser; vendor drivers (Topaz, Wacom) can come later if a company needs them. |
| D-6 | **Changing the choice** on an open shipment: the customer may, on the portal. Staff may, when the company's setting allows it (*Staff may change the insurance of an open shipment*, default on); the acceptance is then recorded as given at the counter. A final shipment's insurance never changes (C-3). |
| D-7 | **Migration:** each existing plan per mode becomes one product named after its mode ("Air insurance"), with the same prices, terms and policy; insured shipments point to it. Nothing changes for a company with one product: the choice list has one line. |

### The cost estimate panel

| # | Decision |
|---|---|
| E-1 | **Always an estimate**, titled *Shipment estimate* (owner, 2026-10-20: nothing more), even with every parcel in: no taxes, no delivery fees. It is never the invoice. |
| E-2 | **Lines:** the service's charges for the shipment (minimum, consolidation, refrigeration, other extras), the **insurance** chosen with its insured value, then **each parcel** (number, weight, size, chargeable weight, cost), then the total. A *what if* block lists **each insurance product** the shipment can have with its premium and the total it would make, and *no insurance*; consolidation on or off when it changes the price. **Built for the owner to see, then keep, change or drop** (owner, 2026-10-20). |
| E-3 | **Follows every change:** at the counter, the panel is recomputed with each parcel and each change of service, options or insurance. On the new shipment form (counter and portal), it updates as fields change, with no parcel lines: the service's rates are shown (price per kg or volume, minimum) and the insurance products with their rule (e.g. 3%, minimum $10), since no value is known yet; the customer may type an expected value to see the premium. |
| E-4 | **Always visible:** a side panel beside the form, kept in view when scrolling (sticky); under the form on narrow screens. |
| E-5 | **Where:** the counter's customer page (new shipment) and shipment page; the portal's new and change shipment forms, and the portal's shipment page. Same numbers everywhere: one pricing service (the calculator, quotes, the counter and the portal use the same rules). |
| E-6 | **No script needed to work:** each change posts and redraws (static pages); a small script updates the panel as the clerk types, sending the form to a read-only estimate endpoint. Rate limited, no save. |

## 3. Steps

| Step | Content |
|---|---|
| 1 | Insurance products: model, migration, staff setup (Prices → Insurance), choice on the counter and the portal, certificate from the product |
| 1b | The page to sign and the signature pad |
| 2 | The estimate panel: counter (customer and shipment pages) and portal (new, change, shipment), with *what if* |
| 3 | Later, with billing: taxes and delivery fees, then the invoice |

## 4. Not in this slice

Taxes and delivery fees (billing), discounts and promotions, customer groups (D-4 uses single customers until groups exist).
