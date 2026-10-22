# Slice 5B: Insurance and claims - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-20 |
| Built on | Slice 5A; not committed here: the dev team owns git |
| Design | [Slice 5B design note](SLICE-5B-INSURANCE-AND-CLAIMS-DESIGN.md), approved by the owner on 2026-10-19 |
| Status | **Step 1 (policies, cover and liability, certificates) built, reviewed independently, fixed and verified by automated tests; waiting for owner sign-off.** Steps 2 (claims for staff) and 3 (claims on the portal, declarations, reports) follow |

| Step | Content | Status |
|---|---|---|
| 1 | Policies register with documents and reminders; cover offered names its policy; liability terms; certificates on insured shipments | Built (this record) |
| 2 | Cases and claims for staff | Next |
| 3 | Claims on the portal; the declaration to the insurer; reports | Later |

## 1. Step 1: what was built

### For staff: **Insurance** in the menu (`/staff/insurance`)

Two new permissions, company-wide: *see the insurance policies, covers and liability terms* and *manage* them. The Owner and administrators have them.

- **The company's policies.** Each one records:
  - what it covers (I-6): the **company's operations** (loss or damage the company causes while goods are in its hands) or **customers' shipments** (the cover customers buy);
  - its **kind**: the liability kinds for operations (freight forwarder's, warehouse, carrier's for its own vehicles, general); cargo for customers' shipments (open policy or per shipment); self-insured and other fit both;
  - the insurer and broker with their contacts, and the insurer's claims email and phone;
  - the policy number and its dates, and the currency;
  - its **limits** (per shipment, per conveyance, per location, per year) and its **deductible** (an amount, or a share with a minimum);
  - the risks covered (loss, damage, theft, wetting, contamination, war and strikes, general average), the modes and the routes or countries, and the exclusions;
  - the **duties to the insurer**: days to notify a claim, and whether insured shipments are declared each month or quarter;
  - notes;
  - **documents** (the policy, endorsements, certificates: PDF, PNG or JPEG up to 10 MB, checked by their first bytes, kept in the company's file store, opened as downloads);
  - an insurer and number are needed, except when the company insures itself.
- **Flags:** a policy ending within 30 days is flagged at the top of the insurance page. Expired and archived policies stay readable. Archiving a policy that stood behind a cover makes that cover self-insured until another policy is chosen.
- **Cover sold to customers:** for each transport mode, the policy it sits under (only a policy for customers' shipments, still held) or self-insured. Its price and terms stay with the prices (`/staff/charges`).
- **When the customer does not insure** (I-3), per mode, the company owes one of:
  - nothing beyond the insurance it sells;
  - an amount per lb or kg (in the company's unit), with a cap per parcel if any;
  - an amount per parcel;
  - the declared value up to a cap.

  It may add its own words for customers (kept with their line breaks). Until a company saves these terms, the figures it gave in its cover's terms (Slice 4C) are used, with their limit per shipment, so nothing is lost. Amounts per weight are always shown in the company's unit today.
- A cover can only sit under a policy that covers its transport mode.
- **Certificate:** a shipment the customer insured shows **Insurance certificate (PDF)** on its staff page.

### Insurance certificates

- When a customer insures a shipment (and accepts the terms), Vixly issues a **certificate** with its own number (`IC` + year + six digits). It names the cover of the shipment's mode and the policy that cover sits under.
- The certificate follows the shipment:
  - renewed when the customer accepts the terms again (it then names the cover and policy of that moment);
  - voided when the customer stops insuring or cancels the shipment; a cancelled shipment still opens its certificate, marked void;
  - kept as issued when the company later stops offering the cover, changes its terms or moves it under another policy: what the customer accepted stands.

  A shipment insured before this step gets its certificate the first time it is asked for. Opening a certificate never changes it.
- Staff cannot move an insured shipment to another transport mode: the customer accepted the terms of its mode, so the customer changes the service and accepts the new terms.
- The PDF shows:
  - the company, the number, the status, the insured customer (with code) and the consignee;
  - the shipment, the route and the service;
  - the insurer and policy, or "insured by the company itself";
  - the risks covered, what is not covered, how a loss is paid, the deductible and the most insured;
  - the **declared value today** (parcels received and packages still announced) and its **premium**, and when the terms were accepted.
- It stays **provisional** until the shipment leaves. Slice 5 fixes the value and the premium.

### For the customer

- On the shipment form, beside *insure*: what the company owes if the customer does not insure, for each mode the company offers the customer, in the company's words or in plain words ("we pay up to 50 USD per parcel lost or damaged"). The older single liability line in the cover's terms is gone (it said the same thing less precisely).
- On an insured shipment's page: **Insurance certificate (PDF)**, in the customer's language; only for the customer's own shipment.

### Demo day

The demo company (`bluewater-shipping`, see [DEVELOPMENT-DEMO-DATA.md](DEVELOPMENT-DEMO-DATA.md)) now has:
- an open cargo policy with Atlantic Marine, behind its air and ocean cover;
- a freight forwarder's liability policy with Gulfstream Mutual, ending within 30 days, so the flag shows;
- liability terms: $9.07 per lb up to $100 a parcel by air, $50 a parcel by sea.

Ana's insured shipment has its certificate.

## 2. Database

- `ops.InsurancePolicies` (its documents as JSON), `ops.LiabilityTerms` (one per company and mode), `ops.InsuranceCertificates` (unique number; one live certificate per shipment), `ops.InsurancePlans.PolicyId`.
- Migration `InsuranceModule`, in the global script.

## 3. Automated tests

| Test | What it proves |
|---|---|
| `Policy_KindFitsWhatItCovers_AndNeedsItsInsurer_UnlessSelfInsured` (Domain) | A cargo kind refused for operations and a liability kind for customers' shipments; insurer, dates, amounts and currency checked; self-insured needs no insurer; operations never declare shipments |
| `Policy_InForceAndEndingSoon_ArchivedIsNeither_AndKeepsItsDocuments` (Domain) | In force, ending within 30 days, ended, archived; modes covered; a document's name never a path; unknown kinds refused; at most 20 documents |
| `Liability_OwesPerBasis_UpToTheCap` (Domain) | Per lb (converted from kg) up to the cap, per parcel, declared value up to a cap, nothing; missing figures refused |
| `Certificate_IsProvisional_RenewedOrVoided_AndFinalOnce` (Domain) | The certificate's life |
| `Insurance_RecordsPolicies_PutsTheCoverUnderOne_SetsLiability_AndCertifiesInsuredShipments` (Web) | A wrong kind refused with the typed values kept; a policy saved; a text file refused, a PDF filed and downloaded as an attachment; the 30-day flag; the air cover put under the policy, an operations policy refused for it; liability refused without its amount, then saved; an insured shipment's certificate as PDF naming the policy; archiving unlinks the cover |
| `StaffWithoutTheseRoles_CannotScanCheckOrSeeParcels` (Web, extended) | Without the permission: no Insurance in the menu, the page refused, no new policy form |
| `Customer_SeesTheLiabilityTerms_InsuresAShipment_AndDownloadsItsCertificate` (Api) | The liability words on the form; an insured shipment with its certificate link and PDF; another customer gets "not found" |
| `DemoDay_FillsTheMainCompany_EveryStageShows_AndTheSecondCompanySeesNoneOfIt` (Web) | The demo day still loads through the rules, with the policies and liability terms |

`Insurance_RecordsPolicies_...` also proves that staff cannot move the insured shipment to another mode. `Customer_SeesTheLiabilityTerms_...` also proves that stopping the cover later leaves the certificate as issued, that opening it again issues no other, and that cancelling voids it while it still opens.

## 4. Independent review and fixes

An independent review of step 1 found 13 points. All are fixed or recorded:

| # | Finding | Fix |
|---|---|---|
| 1-3, 7, 13 | Opening a certificate re-synced it: it could renew or void it with the date of the shipment's last change, and a company stopping a cover, or a booked shipment, voided certificates customers had accepted | Two paths. Changing a shipment: issue when insured and none exists, renew only on a new acceptance, void only on cancel or uninsure. Opening: issue a missing one only, dated now, and mark the shipment changed so a change at the same moment conflicts instead of doubling it |
| 4 | Staff changing the service could move an insured shipment to a mode whose terms the customer never accepted; the PDF then fell back to the first terms version | Refused with its own message; the PDF uses the terms of the acceptance, else of the certificate's date, never another version |
| 5 | The cover's old limit "per shipment" was shown as "per parcel"; the cover terms still showed the old liability line beside the new one | Carried as a limit per shipment with its own wording; the old line removed |
| 6 | The certificate's premium ignored the most paid per item | Same rule as pricing (value per item) |
| 8 | An amount per lb could be labelled per kg after the company changed unit | Converted to the company's unit today |
| 9 | A cover could sit under a policy not covering its mode | Refused, on linking and on changing the policy's modes |
| 10 | Liability shown for modes the customer cannot ship by; "at most — per parcel" when no cap; line breaks lost | Only the modes offered; a wording without cap; line breaks kept |
| 11 | "Self-insured" written in English inside the domain | Removed; pages name it in the reader's language |
| 12 | The design said PDF only for policy documents | Design updated: PDF, PNG or JPEG |

### Known limits (step 1)

- The certificate's value and premium are provisional until Slice 5 fixes them when the shipment leaves.
- Reminders of a policy ending are the flag on the insurance page; email reminders come with the notifications slice.
- The declaration to the insurer and claims are steps 2 and 3.
