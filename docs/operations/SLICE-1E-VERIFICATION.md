# Slice 1E: Settings, company, billing and history - verification record

| | |
|---|---|
| Plane | Operations (Stage 3) |
| Date | 2026-10-18 |
| Built on | Slices 1C and 1D (delivered the same day); not committed here: the dev team owns git |
| Design | [Slice 1 design note](SLICE-1-STAFF-AND-SETUP-DESIGN.md) sections 2, 3 (Subscription access levels, Tenant setup), 5 and 11; ADR-0018; owner decisions D-O2, D-O7, D-O8 and D-O12 |
| Status | **Built and verified by automated tests; waiting for owner sign-off.** Shipping services wait for an owner answer (section 6) |

## 1. What was built

### Settings (`/staff/settings`; `ops.settings.view` and `.edit`)

- **Money:**
  - The billing currency is any ISO 4217 code. It can change only while no invoice exists (ADR-0018); invoices arrive with Slice 9, so until then it can always change.
  - **Payment gate:** paid first by default. "Allow Collect" and "Allow customer credit" open it (ADR-0018).
- **Units:** kilograms or pounds, centimetres or inches, for display and entry. Values are always stored in kilograms and centimetres (D-O8).
- **Delivery modes:** pickup, home delivery, or both; at least one.
- **Security:** "every staff member must use an authenticator app" (D-O2).
  - With it on, staff without an app enrol at their next sign-in, and new staff enrol when they accept the invitation.
  - Turning it **off** needs step-up.
- **Numbering prefixes** for shipments, receiving, invoices and payments:
  - each prefix is 1 to 6 capital letters or digits, starts with a letter, and differs from the others;
  - the page shows an example number.
- Before the first save the page shows the defaults (USD, kg and cm, paid first, pickup, SH, RC, INV, PAY).
- Each save writes `ops.settings.changed` with the before and after values.

### Numbering (D-O12)

- One sequence per tenant, kind and year in `NumberSequences`, taken with **one atomic `MERGE … WITH (HOLDLOCK)`**. The first number of a year creates the row.
- **Number format:** prefix, two-digit year, six digits (for example `SH26000042`).
- `OperationsNumbering.NextAsync(kind)` is ready for Slice 3. Nothing issues numbers yet.

### Company (`/staff/company`; `ops.company.*`)

- **Legal details** from Platform (read-only), with the account code.
- **Contacts, language and time zone:** the Owner contact is shown, not editable. Billing and technical contacts, the company language and the time zone are saved to Platform (`PUT /tenant`). This needs the Owner or an Administrator, and step-up.
- **Change of legal details (B12):**
  - Owner only, with step-up.
  - Needs the new details, a reason and a proof file (PDF, PNG or JPEG, 10 MB at most), sent to Platform.
  - The page shows the latest request and its status, and the Owner can withdraw a pending one.
- Operations writes `ops.company.contacts_changed`, `ops.company.change_requested` and `ops.company.change_withdrawn`; Platform keeps its own audit too.

### Billing (`/staff/billing`; Owner and Administrators)

- The plan: name, status, term, licences used, amount, and the trial, renewal or end date.
- **Invoices:** number, dates, total, what is left to pay, and "overdue".
  - **Pay online** asks Platform for a pay link and sends the browser there.
  - Only an `https` address from Platform is followed.
- **Billing PDFs:** served through the tenant host, signed for the user, never cached.
- Other staff get "Only the Owner and Administrators see billing."

### When a payment is due (design section 3)

- **The Owner and Administrators** land on `/staff/billing` from any other staff page. They can still open My account, the Terms and the confirm page, sign in and out, and pay.
- **Other staff** go to `/staff/limited`, which says the Owner or an Administrator can pay.
- The banner on every staff page links to billing.
- Read-only access is unchanged: every change is refused, except paying and the step-up confirmation.

### History (`/staff/audit`; `ops.audit.view`)

- Every audit entry of the company, newest first, 50 per page.
- **Filters:**
  - area: sign-in, own accounts, staff, roles, devices, website, locations, settings, company, Terms, setup;
  - a date range;
  - one person: the person page now links "See this person's activity".
- Each entry shows when, who (or "System"), the action code, the reason, the IP address and the recorded values.

### Setup checklist and my language

- The `/staff` home shows the Owner and Administrators what is left:
  - currency and units;
  - first branch;
  - a receiving warehouse;
  - publish the website;
  - invite a first staff member.

  Each open step links to its page; the list disappears once all are done.
- The home page links to every section.
- **My account:** the language of my pages and emails (en, fr, es). The pages switch at once.

### Data and texts

- Migration `TenantSettings`: tables `TenantSettings` (one row per tenant) and `NumberSequences` (unique per tenant, kind and year), each with `TenantId`.
- `OperationsText` grows from 772 to 907 keys in en, fr and es. No "Vixly" in any of them.

## 2. Automated checks

Release builds of the three solutions: no errors, no warnings.

| Test project | Slice 1D | Slice 1E |
|---|---|---|
| Operations.Domain.Tests | 66 | 74 |
| Operations.Application.Tests | 58 | 63 |
| Operations.Infrastructure.Tests (SQL Server) | 37 | 41 |
| Operations.Api.Tests | 14 | 14 |
| Operations.Web.Tests (SQL Server) | 35 | 40 |
| Customer tests | 20 | 20 |
| Platform tests | 665 | 665 |
| Public.Web, Shared.Kernel, Shared.UI | 113 | 113 |
| Architecture.Tests | 8,202 | 8,444 |

All 9,474 pass. The global SQL script runs twice on an empty server, and twice over a database built with the script of the 1C and 1D delivery, without errors.

What the new tests cover:

- **Over HTTP, with a real database:**
  - **Settings and checklist:** the checklist asks for the currency and units; equal prefixes are refused with the reason; HTG, pounds and inches are saved and shown; the checklist ticks the step; the history shows `ops.settings.changed` with the Owner's name under "settings" and not under "roles".
  - **Company:** contacts, language and time zone reach Platform as the Owner with step-up; a change request with a PDF proof reaches Platform with the file name and an upper-case country; the page then shows "Waiting for support" and the withdraw button.
  - **Billing:** the invoice is listed; "Pay online" sends the browser to the pay link; the PDF is served; a website editor sees only the refusal.
  - **Payment due:** the Owner is sent from the staff list to billing and can still pay; a website editor is sent to the limited page, which says who can pay.
- **Use cases:**
  - with the staff-wide rule on, staff without an app must enrol at sign-in;
  - saving needs `ops.settings.edit` and is audited;
  - turning the rule off after 10 minutes needs step-up, while other changes do not;
  - the checklist is for the Owner and follows the saved settings.
- **Domain:**
  - defaults;
  - ISO currency, locked once an invoice exists;
  - known units and at least one delivery mode;
  - prefix shape, upper case, and all different.
- **Database:**
  - settings round-trip and stay in their tenant;
  - **25 numbers taken at the same time are 1 to 25 with no repeat**;
  - numbers restart per kind, year and tenant;
  - the history filters by person, area and dates, newest first, paged, within the tenant.

## 3. Independent security review (2026-10-18)

A separate reviewer read the 1D and 1E code without having written it. Its findings were fixed before delivery, each with a test:

- **Scoped grants gave company powers.** A role given for one warehouse counted for staff, roles, settings, company, history and devices everywhere. Now only a grant **for the whole company** counts for those. Branch and warehouse grants count for branches and warehouses only. Test: a warehouse lead with staff and role permissions is refused all three.
- **Ownership transfer could leave no Owner** if its second save failed. Both saves now run in one database transaction.
- **Inviting gave a role without step-up** and skipped the scope checks of a grant. Inviting now needs step-up, a real branch or warehouse, and the Administrator role only for the whole company.
- **Changing the billing currency** now needs step-up (design section 3). Test added.
- **POSTs with no form field skipped the antiforgery check.** For example: new recovery codes, sign out everywhere, pay, withdraw. ASP.NET Core rejects a bad token only when an endpoint reads the form. Every POST under `/staff` now checks it (`OperationsFormGuard`). Test: posting without the token answers 400.
- **Reactivating a role** now needs every permission the role holds, for the whole company.
- **Removing a role** checks that the grant belongs to the person whose page it was removed from.

**Not changed** (low risk, noted):

- The first listing of roles creates the Administrator role, even on a read-only day.
- A proof file over 10 MB is read before it is refused.

## 4. Where the build differs from the design note

- **Shipping services** (`ShippingServiceSetups`) are not built. They need Vixly's shipping-service catalogue in `Contracts.Platform`, which does not exist yet (section 6).
- **Numbering year** is the UTC year, not the tenant's time zone. A number taken in the first hours of 1 January (UTC−5) carries the new year.
- **Staff management by scope:** the design lets a branch or warehouse manager manage the staff of their place. For now, staff, roles, settings, company, history and devices need a company-wide grant (section 3). Managing staff by place needs a rule for which staff belong to a place; it can come with Slice 2 or 3.
- **Turning on the staff-wide second factor** applies at each person's next sign-in. A session already open continues until it ends (at most 12 hours).
- **The history** shows action codes (`ops.staff.status_changed`), not sentences. Frontend/UI can map them to text later.
- **Theme on My account:** not built; it is a Frontend/UI choice.

## 5. Deployment notes

- **Database:** run the newest global SQL script (migration `TenantSettings`). The application login gets the new tables through the existing schema grant.
- No new settings.

## 6. Questions for the owner

1. **Shipping services:** what is Vixly's catalogue of shipping services? For example: air express, air standard, ocean consolidated, ocean full container, ground. Platform will publish it in `Contracts.Platform`. Each tenant then picks which ones it offers and sets the label, business days and volumetric divisor (D-O8).
2. **Passkeys:** a slice of their own after this one (proposed), or before?

## 7. Follow-up after the owner's answers (2026-10-18)

- **Branding by plan (W-4), answered yes:**
  - Without `portal_theme_customization`, the public site and the preview show the default colours (Ocean).
  - Without `homepage_content_customization`, they show the default texts, no main image, every service and the default sections.
  - The company name, logos, favicon, social links, locations and contacts always show.
  - Saved choices are kept and appear again when the plan includes the feature.
  - The website editor says which part the plan does not include.
  - Operations reads the features from Platform's entitlements and keeps them 5 minutes per tenant. If Platform cannot answer, the site shows the tenant's own choices.
  - Test: one tenant moves through no features, theme only, then all.
- **Staging on `vixly.freedns.org`, answered yes (wildcard supported):**
  - Every host has an `appsettings.Staging.json`, loaded with `ASPNETCORE_ENVIRONMENT=Staging`.
  - Host names: `vixly.freedns.org` (public site), `control.` (console), `api.` (API, which also serves the internal calls), `ops-internal.` (Operations internal API), and `{slug}.` (tenants).
  - Secrets (connection strings, caller keys, mail, payments, bot check) stay outside the files, as in production.
  - `ops-internal` and `vixly-ops-internal` are now reserved slugs in Platform's code floor, because the internal host shares the tenant wildcard. The Global Standard H-2 and the hostname table record it.
