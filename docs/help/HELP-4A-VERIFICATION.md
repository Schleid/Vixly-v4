# Operations help Version 1 — H4a verification

Date: 2026-10-25. Status: **all H4a guides and available setup links built; verification in progress**. Owner sign-off pending.

The owner authorized starting Operations Version 1 after Public Version 2 (ADR-0023 Amendment 1). This does not sign off Control, Customer or the later Operations parts.

## Delivered

- All 27 Getting started and Setting up guides in English, French and Spanish: 81 article files, including 21 new guides in this continuation.
- Signed-in help home, search, related articles and contextual header links in the staff shell. Company tokens remain request-local and encoded. Existing tenant, device and access gates are unchanged.
- Setup header links now open the guide for the actual route, including constrained warehouse, supply and carrier templates.
- Reader-filtered links beside the mailbox choice, route fields, extra charges and publication choice. Checklist links accompany company settings, branch, receiving warehouse and services. Existing action links continue opening their task screens.
- Website and staff checklist items keep their existing task links. Their help articles belong to H4e and will be linked when published; no link points to an unwritten guide.
- Operations Version 1 remains text-only. No screenshot or desktop capture was used for this work.

## Articles

All English guides are source-language drafts marked ok. Each French/Spanish article remains needs-review until native review. There are 54 translated article drafts requiring that review.

| Slug | Title (en) | Permissions (any) | Actual route templates |
|---|---|---|---|
| first-sign-in | Your first sign-in | [] | [] |
| signing-in-from-another-country | Signing in from another country | [] | [] |
| this-device | Using an approved device | [] | [] |
| workspace-at-a-glance | Your workspace at a glance | [] | [/staff] |
| your-account | Your password, recovery codes and language | [] | [/staff/account] |
| branches-and-warehouses | Branches and warehouses | [ops.branches.view, ops.warehouses.view] | [/staff/locations, /staff/locations/branch/{branchId:guid?}, /staff/locations/warehouse/{warehouseId:guid?}] |
| bundle-settings | Bundle settings | [ops.settings.view] | [/staff/settings/bundles] |
| carriers | Carriers and their rates | [ops.carriers.manage] | [/staff/carriers, /staff/carriers/{carrierId:guid}] |
| company-settings | Company settings | [ops.settings.view] | [/staff/settings] |
| credit-settings | Credit account settings | [ops.settings.view] | [/staff/settings/credit] |
| customers-mailbox | Choose the customers’ mailbox | [ops.warehouses.manage] | [] |
| delivery-settings | Delivery settings | [ops.settings.view] | [/staff/settings/delivery] |
| event-settings | Event causes and notifications | [ops.settings.view] | [/staff/settings/events] |
| extras-and-item-types | Extras and item types | [ops.charges.configure] | [] |
| inspection-rules | Inspection rules and restricted items | [ops.inspection.configure] | [/staff/inspection/setup] |
| insurance-setup | Insurance products and documents | [ops.insurance.view] | [/staff/insurance/] |
| invoice-settings | Invoice settings | [ops.settings.view] | [/staff/settings/invoices] |
| payment-settings | Payment methods: cards, Zelle and transfers | [ops.settings.view] | [/staff/settings/payments] |
| pickup-settings | Pickup settings | [ops.settings.view] | [/staff/settings/pickup] |
| price-lists | Price lists and versions | [ops.charges.configure] | [/staff/charges] |
| printers | Label printers and the print program | [ops.printers.manage] | [/staff/locations/warehouse/{warehouseId:guid}/printers/] |
| publish-prices | Show prices on the website and portal | [ops.charges.configure] | [] |
| setup-checklist | Set up your company step by step | [ops.settings.edit] | [] |
| shipping-services | Shipping services | [ops.services.view] | [/staff/services] |
| storage-locations | Storage locations and their labels | [ops.warehouses.manage] | [/staff/locations/warehouse/{warehouseId:guid}/storage] |
| supplies-catalogue | Supplies catalogue and prices | [ops.charges.configure] | [/staff/supplies, /staff/supplies/{supplyId:guid}] |
| zones-and-routes | Choose the route for a price | [ops.charges.configure] | [] |

## Corrections to the plan and source findings

- Route metadata uses the mapped templates, including warehouseId:guid, carrierId:guid and supplyId:guid, optional branch/warehouse identifiers and the trailing slash on grouped printer and insurance home routes. Planned {id} routes would not match the contextual header.
- The zones-and-routes guide describes the origin/destination selectors actually built into price lists and extras. There is no separate map-based pricing-zone editor on these screens.
- Settings, service and insurance readers may inspect a page without being able to edit it. Their guides make editing conditional on available controls and do not promise a save button to every reader.
- Selecting a customer mailbox replaces the previous warehouse mailbox choice; it does not redirect packages already shipped to the old address.
- The storage-location page adds codes and changes zone/availability. Its guide does not claim that it prints shelf labels or moves packages.
- The help stage changes no business behavior. Source files and exact localized resource labels are checked; these checks do not replace a live workflow walkthrough.

## Checks

- Expanded Operations help suite: **29 passed**, zero failures or skips. Covers exact labels and source paths, 27 guides per language, all ten newly represented permission groups, encoded company branding, contextual route selection, inline link hiding, unknown plan conditions and the anonymous tenant/sign-in gates.
- Actual Razor help browser renders all **81 localized articles** in the component checks. These checks verify article markup and localized resource resolution, not browser layout or database workflows.
- Architecture publication/help, project dependencies, file organization and UI rules: **15,469 passed**, zero failures or skips.
- Article length: **157–235 words** across all 81 files. All 54 French/Spanish drafts retain needs-review. No new help chrome keys were needed.
- Expanded Operations Web test build completed without warnings or errors. An earlier build overlapped addition of the new checklist-topic type and failed to see that file; the final build includes it and passes.
- Whitespace checks passed for the changed help integration files.

## Remaining before H4a sign-off

- Live demo walkthrough of every English guide, plus owner/restricted staff/partner visibility, two-company isolation and mobile layouts. VIXLY_TEST_SQL_SERVER is not configured in this shell and the documented local demo is not running; a real database workflow run is not certified by the unit tests.
- Owner supplied `https://hdl.vixly.freeddns.org` for the walkthrough. Its staff sign-in page responds 200 and identifies Harbour Demo Logistics. **GET /staff/help returns 404 with an empty body**, so this deployment currently does not expose the new help route. A deployment containing these changes and signed-in access are needed for the live walkthrough. No credentials were guessed, no settings were changed and no screenshot was captured.
- Open all help pages in each language in a browser.
- Native review of all 54 French/Spanish article drafts and help chrome before release.
- Owner sign-off of this H4a part before H4b begins, as required by HELP-PLAN and HELP-4-OPERATIONS.

H4b (receiving and counter), H4c (money), H4d (dispatch and destination) and H4e (team, website, integrations and reports) remain unwritten. Operations help as a whole is not complete.
