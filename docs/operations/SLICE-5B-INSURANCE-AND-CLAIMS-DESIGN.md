# Slice 5B: Insurance and claims - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-19 |
| Replaces | The insurance "plan" of Slices 4 to 4C, which only priced cover; it moves into this module |
| Owner input | 2026-10-19, 22:07 to 22:22 (section 2) |
| Status | **Approved by the owner on 2026-10-19 (23:03)** |

## 1. Outcome

Insurance becomes its own module, **Insurance and claims**, with its own menu and permissions, rather than a few lines in prices. It covers three things, kept apart as the industry keeps them:

1. **The company's policies:** what protects the company, from which insurer, on what terms.
2. **Cover for customers:** what a customer buys when insuring a shipment, and what the company owes when the customer did not insure (its liability limits).
3. **Claims:** loss, damage or shortage, from the first notice to the payout and to what the company recovers from its insurer, carrier or partner.

Prices keep charging the premium, but read it from the cover offered (section 4) instead of owning it.

## 2. Owner answers (2026-10-19)

| # | Answer |
|---|---|
| I-1 | Vixly serves many companies and cannot assume how each is insured: follow industry practice and keep everything flexible |
| I-2 | Payout approval is a setting: an administrator sets the amount a claims handler may settle without approval |
| I-3 | Liability for shipments the customer did not insure: flexible, following industry practice |
| I-4 | Customers can open claims from the portal |
| I-5 | Claim deadlines: each company may set them, or not |
| I-6 | Two kinds of company insurance: one for the company's operations, one for customer shipments. An uninsured TV broken by the company's team at a hub falls under the company's insurance; transport damage on an insured shipment is claimed from the transporter and from the insurer of the cover the customer bought |

## 3. The company's policies (`/staff/insurance/policies`)

A register of every policy the company holds. Several at once is normal.

- **What it covers** (I-6), the first choice on every policy:
  - **company operations:** damage or loss the company causes while goods are in its hands (its warehouses, hubs, vehicles, team); kinds: freight forwarder's liability, warehouse legal liability, carrier's liability (its own trucks), general liability;
  - **customer shipments:** the cover customers buy for their shipments; kinds: cargo, open (shipments declared each period) or per shipment.
- **Kind**, within that choice, or other.
- **Insurer and broker:** names, contacts, the insurer's claims email and phone.
- **Policy:** number, start and end dates (expired policies stay readable), currency.
- **Limits:** per shipment, per conveyance (one flight, vessel or truck), per location (a warehouse), per year; the deductible (an amount, or a share of the loss with a minimum).
- **Scope:** covered risks (loss, damage, theft, wetting, contamination, war and strikes, general average), the transport modes and routes or countries covered, the warehouses covered.
- **Exclusions:** the standard list we already have (cash, jewellery, perishables, hazardous goods, improper packing, customs seizure...) plus the company's own.
- **Duties to the insurer:** the deadline to notify a claim; whether insured shipments must be **declared** to the insurer (an open policy's monthly declaration, the "bordereau") and how often.
- **Documents:** the policy and its endorsements, uploaded (PDF, or a PNG or JPEG scan, checked by their first bytes, up to 10 MB, in the company's file store).
- **Reminders:** a policy ending within 30 days is flagged on the insurance page and to administrators.

A company that self-insures (no insurer behind the cover it sells, the "own" cover we have today) records no policy, or a policy of kind *self-insured* with its own limits. Both work.

## 4. Cover for customers

### Insured shipments (bought by the customer)

- **Cover offered**, per transport mode as today: how the premium is worked out (a share of the declared value with a minimum, or a price per value band), the maximum value, the risks covered and exclusions, how a loss is paid (declared value, or the lower of declared and invoice value), the terms the customer accepts. Dated versions as today.
- Each cover offered names the **policy it sits under** (or self-insured). When a customer insures a shipment, Vixly records an **insurance certificate**: a number, the policy, the declared value, the premium, the terms version accepted and when. The certificate is a PDF the customer and staff can download.
- When the policy requires it, insured shipments appear on the **declaration to the insurer** for the period (CSV and PDF: certificate, shipment, route, mode, value, premium), marked as sent.

### Shipments the customer did not insure: the company's liability

The industry rule: the carrier or forwarder is liable only up to a limit unless the customer declared and paid for a higher value. Each company sets **its liability terms**, per transport mode:

- **None** (the company accepts no liability beyond insurance it sells), or
- **an amount per weight** (per kg or lb of the parcel lost or damaged, as the international air and sea conventions do), or
- **an amount per parcel**, or
- **the declared value up to a cap**;
- with its text for the customer (shown before the customer chooses not to insure, and in the terms).

The legal limits of each convention are not hard-coded: they change and depend on the route. The company enters the figures its lawyer or broker gives it.

## 5. Claims (`/staff/claims`, and the portal)

### Who opens a claim

- **The customer**, from the portal, on its shipment or parcel: what happened (loss, damage, shortage, delay), when it was found, what it claims, with photos, the purchase invoice and the receipt signed at delivery. One open claim per parcel and kind.
- **Staff**, for the customer (a phone call, transport damage found on arrival, A-10).
- **The company itself**, against its insurer, a carrier or a partner, to recover what it paid or lost (with or without a customer claim behind it).

### One incident, several claims (I-6)

A damaged or lost parcel is a **case**. A case holds the claims it gives rise to, each with its own party, amounts and status:

| Claim | Against | Example |
|---|---|---|
| The customer's claim | the company | Jean asks to be paid for his TV |
| Under the customer's cover | the insurer of the cover the customer bought | Jean insured: the insurer pays Jean, or pays the company back |
| Under the company's operations policy | the company's own insurer | Jean did not insure, the company's team broke the TV at the hub: the company pays Jean, then claims from its insurer |
| Recovery | the transporter or a partner | transport damage: the airline, shipping line, trucker or partner who had the parcel |

- **Where it happened and who was responsible:** our warehouse or hub, our team, the transporter, a partner, customs, or not known. Vixly suggests it from the scans and holds (the last place the parcel was seen in good condition, the first where it was found damaged); the handler confirms it.
- **Which claims Vixly proposes** follows from that: our responsibility, the operations policy; insured shipment, the customer's cover; transporter or partner, a recovery claim. The handler adds or removes claims.
- **No one is paid twice:** the case totals what the customer was paid and what each insurer, transporter or partner paid, and shows what is still owed or recovered. When an insurer pays and then pursues the transporter itself, the recovery claim is marked as the insurer's, not the company's.
- **The transporter's deadline first:** notice to a transporter is often due within days; the case shows the earliest deadline at the top.

### What each claim holds

- The parcels and shipment concerned, the kind of loss, the amount claimed, the currency.
- **Evidence:** the customer's files, plus what Vixly already knows and shows side by side: the condition, weight, size and photos at origin inspection, every scan, holds, the proof of delivery, the declared value and the certificate.
- **Cover found by Vixly:** insured (certificate, cover, policy, deductible, exclusions) or not insured (the company's liability terms). The handler can change it with a note.
- **Messages** with the customer, as on tickets (staff are never named to the customer).
- **Internal notes**, never shown to the customer.
- **Reserve:** the amount the company expects to pay, kept up to date (an industry habit for reporting).

### Steps

| Status | Meaning |
|---|---|
| Open | Received; deadlines run from the day the loss was found |
| Waiting for the customer | More information asked |
| Under review | The handler assesses cover and amount |
| Filed with the insurer | The insurer's claim number and adjuster recorded; the policy's notice deadline shown |
| Decided | Accepted in full, in part, or denied, with the reason the customer sees and the amount: claimed, deductible, depreciation or limit applied, offered |
| Settled | Paid to the customer: credit on account, refund, or another way the company records (the money itself goes through Finance, Slice 9) |
| Closed | Nothing left, including recoveries |

**Recoveries** are tracked on the same claim: from the insurer, a carrier or a partner, amount asked and amount received.

### Settings (each company; all optional, I-2 and I-5)

- **Approval threshold:** a handler may settle up to this amount; above it, someone with the approval permission (the Owner or an administrator by default) approves. No amount set: every settlement needs approval.
- **Deadlines to claim**, per kind: for example 7 days after delivery for damage, 30 days after the expected date for loss. Blank: no deadline. A late claim is accepted but flagged; the handler decides.
- **Who may open claims on the portal:** every customer, or none (staff only).

### Permissions

`ops.insurance.view` (policies and covers, read), `ops.insurance.manage` (policies, covers, liability terms, declarations), `ops.claims.handle` (claims, per warehouse), `ops.claims.approve` (settlements above the threshold).

### Reports

Premiums collected against claims paid and reserved (the loss ratio), per period, mode, route, warehouse and cause; open claims by age; recoveries.

## 6. Customer side

- Insuring a shipment shows the cover's terms; not insuring shows the company's liability terms.
- The insurance certificate on the shipment page.
- **Claims** on the portal: open one on a shipment or parcel, add files, follow the status and messages, see the decision with its reason and the amount.

## 7. Built in steps

| Step | Content |
|---|---|
| 1 | Policies register with documents and reminders; cover offered names its policy; liability terms; certificates on insured shipments; existing plans move in as covers (no data lost) |
| 2 | Cases and claims for staff: open, evidence, where it happened and who was responsible, the claims proposed (customer, customer's cover, operations policy, recovery), statuses, messages and notes, reserve, decision, settlement with the approval threshold, what each party paid, deadlines |
| 3 | Claims on the portal; the declaration to the insurer; reports |

Payments and credits are recorded on the claim; posting them to accounts waits for Finance (Slice 9).

## 8. Not in this slice

- Paying the customer through a payment provider and accounting entries (Slice 9).
- Integration with an insurer's own system (later, if a company asks; the declaration file covers the usual need).
