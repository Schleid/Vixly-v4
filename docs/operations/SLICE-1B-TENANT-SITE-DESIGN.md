# Slice 1B: The tenant's public site - design note

| | |
|---|---|
| Plane | Operations (Stage 3) |
| Status | **Built, 2026-10-18** ([verification record](SLICE-1B-VERIFICATION.md), which lists where the build differs from this note). Owner decisions of 2026-10-18 in section 9 |
| Depends on | Slice 0, Slice 1A; ADR-0012 (white label), ADR-0017 (site content in Operations), ADR-0021 (public tracking) |
| Examples given by the owner | galeo.vixly.ltd (V3), shippex.net, dalbagay.com, welj-ht.com |

## 1. Outcome

`{slug}.techcrafters.ltd` is the tenant's own public website, the storefront of the company for its customers:
- its logo, colours, texts and favicon;
- search engines and link previews that describe the tenant, never Vixly;
- links to customer sign-up and sign-in;
- the tenant's warehouses and branches grouped by country, and its phone numbers per serving country;
- a tracking box.

Staff sign in from a discreet footer link at `/staff`. The Owner edits the site from Operations: a draft, a preview, then publish.

What V3 got wrong and V4 fixes (galeo.vixly.ltd today):
- the page's meta author and description talk about Vixly;
- a Fort Lauderdale warehouse is listed under Haiti;
- tracking requires an account.

## 2. Addresses on the tenant host

| Path | What |
|---|---|
| `/` | Redirects to `/{language}`, using the visitor's language cookie or the tenant's default |
| `/en`, `/fr`, `/es` | The home page in that language (only the tenant's enabled languages) |
| `/{language}/locations` | All locations, grouped by country, with a country picker |
| `/{language}/track?number=` | Public tracking (section 5) |
| `/robots.txt`, `/sitemap.xml` | Per tenant, with language alternates; no indexing of `/staff`, `/setup`, `/invitation` or `/customer` sign-in pages |
| `/site/image/{id}` | The tenant's published images, cached for a year (a new upload is a new id); draft images only for the signed-in Owner, never cached |
| `/staff` | Staff sign-in, moved from `/` (Slice 1A pages and flows unchanged); `/staff/sign-in/*` for the steps |
| `/customer`, `/customer/sign-up`, `/customer/sign-in` | Customer.Web (Slice 2 fills them; until then the portal placeholder) |

A tenant still being set up, and a suspended tenant, show no public site and are not indexed. A tenant in read-only or payment-required access keeps its public site.

## 3. Page content (home)

1. **Header:** the tenant logo (linking home), language links, "Track", "Locations", **Sign up** and **Customer login** buttons.
2. **Hero:** the tenant's headline and short text (per language), an optional hero image, the tracking box and a "Create my account" call.
3. **Services:** the services the tenant chooses to show (air, ocean, consolidation, U.S. mailbox address, door-to-door, pickup), each with its own short text. The labels come from the tenant's shipping services (Slice 1D), editable per language.
4. **How it works:** three or four steps the tenant may edit or hide.
5. **Locations:** countries as tabs with flags; under each country, the branches and warehouses that are in that country, each with address, opening hours, role badges and a "Get directions" link, but **no phone number** (owner, 2026-10-18: customers never call a warehouse directly) (receiving warehouse, pickup point, office; "accepting deliveries" when it receives). A location is shown under the country of its own address, never under another.
6. **Contact per country:** the call centre's phone numbers, several per country when the tenant uses several lines that all reach the same call centre (up to 12, as Shippex shows for Haiti), with WhatsApp, email and hours.
7. **FAQ:** optional questions and answers per language.
8. **Footer:**
   - the tenant logo and short description;
   - contact and social links;
   - legal links (the tenant's terms and privacy texts);
   - "Staff login" (customer login is only in the header, owner, 2026-10-18);
   - copyright with the tenant's legal name.

   No "Powered by Vixly" unless the plan's branding entitlement requires it (W-4). The link would then point to vixly.ltd, outside the tenant's SEO.

## 4. SEO and sharing (all about the tenant)

- `<title>` and meta description from the tenant's texts per language. Canonical URL and `hreflang` alternates for each enabled language, plus `x-default`.
- Open Graph and Twitter card: the tenant name, description, logo or hero image, and the page URL.
- JSON-LD:
  - an `Organization` with name, legal name, logo, URL, contact points per country and social profiles;
  - one `LocalBusiness` (or `PostalAddress` under a `Place`) per published location.
- Favicon, apple-touch icon and `theme-color` from the tenant brand.
- No meta author, generator or comment that names Vixly. A test fails any "Vixly" in the HTML, headers, robots or sitemap of a tenant site.

## 5. Public tracking (owner decision, 2026-10-18)

- The visitor types a tracking number and sees its milestones and dates only: never amounts, names, addresses, photos, notes or internal events (ADR-0021).
- A number that does not exist and one that belongs to another tenant give the same "not found" answer.
- Limits: 30 lookups per minute per address and a bot check after repeated misses. Answers can be cached for 60 seconds.
- The data arrives with Slice 3 (tracking core). Until then the box is shown and answers "not found" for every number, so the page does not change when tracking starts.

## 6. Appearance the tenant controls

- **Logo** (light and dark backgrounds), **favicon** and an optional **hero image**:
  - PNG, JPEG or WebP only; SVG is refused because it can carry script;
  - type checked by the file content, at most 1 MB each, dimension limits;
  - served with their own content type and `nosniff`.
- **Colours:**
  - a preset (for example Ocean, Harbour, Sunrise, Forest, Graphite, Ruby) and a primary and an accent colour;
  - every combination is checked for WCAG 2.2 AA contrast (4.5:1 for text, 3:1 for buttons), and a failing combination is refused with the reason;
  - colours become CSS custom properties on the page.
- **Texts per language:**
  - headline, short text and meta description;
  - services, steps, FAQ and footer description;
  - each with a maximum length;
  - plain text only: no HTML or Markdown.
- **Sections:** show or hide services, how-it-works, FAQ and social links. The order is fixed in this slice; templates and layouts come from the marketplace in Platform Slice 10 (ADR-0017).
- **No custom CSS, script or HTML** in this slice: a tenant cannot break the page or attack its visitors.
- **What a plan allows:**
  - the plan's branding entitlement (W-4) decides custom colours and hiding "Powered by";
  - a plan without it gets the preset colours only.

## 7. Data (schema `ops`; every table carries `TenantId`)

| Class | Table | Notes |
|---|---|---|
| `OperationsTenantBrand` (extended) | `TenantBrands` | Display name, preset, primary and accent colours, logo, favicon and hero image ids, social links (https only), legal name shown, published version and time; one row for the draft and one for the published version |
| `OperationsSiteText` | `SiteTexts` | Brand version, language, key (headline, intro, meta description, services, steps, FAQ), value with a length limit |
| `OperationsSiteImage` | `SiteImages` | Kind (logo light or dark, favicon, hero), content type, bytes (at most 1 MB), width, height, SHA-256; never changed, a new upload is a new row |
| `OperationsBranch` | `Branches` | As the Slice 1 design: code, name, country, city, address, phones, time zone, status, side; plus opening hours and "show on the public site" |
| `OperationsWarehouse` | `Warehouses` | As the Slice 1 design: branch, code, name, address (with its own country), roles (receiving, dispatch hub, pickup point), "accepting deliveries", opening hours, "show on the public site" |
| `OperationsCountryContact` | `CountryContacts` | Serving country, label, phones (call), WhatsApp number, email, hours, order; shown in "Contact per country" and in JSON-LD |

Images live in the database (`SiteImages`) for now. Shared hosting has no object storage yet (B3, O-16), and the images are few and small. Customer and parcel photos (Slice 3) still need the storage decision.

## 8. Screens in Operations (Owner in this slice; roles arrive in 1C)

- **Site and brand:** logo, favicon, hero image, colours with a live contrast check, texts per language, sections; "Preview" opens the draft as visitors would see it, in each language; "Publish" replaces the public version (step-up when 1C brings it, audited).
- **Locations:** branches and warehouses (create, edit, deactivate, show on the site).
- **Contacts per country.**
- These are the first Operations workbench screens. They are interactive Blazor pages under `/staff/...`, built with the shared workbench primitives (ADR-0016) and the double-click guard (ADR-0019), and only for a signed-in Owner.

## 9. Owner decisions (2026-10-18)

- The landing page is the tenant's public site, as galeo.vixly.ltd: the tenant's logo and SEO, never Vixly's.
- It links to customer sign-up and sign-in and to staff login, and shows warehouse locations and phone numbers per serving country.
- The tenant changes its appearance (colours and so on).
- **Public tracking:** milestones and dates without signing in.
- **Staff login:** `{slug}.techcrafters.ltd/staff`, linked from the footer.
- **Order:** this site first, before going live (terms, first device) and staff management.

- **Images in the database for now** (section 7), confirmed by the owner on 2026-10-18.
- **Phones:** a country contact holds several numbers that all reach the call centre; branches and warehouses show no phone on the site, their number stays internal (owner, 2026-10-18).
- **Footer:** no customer login link; the header has it (owner, 2026-10-18).

## 10. Slice 1 parts after this decision

| Part | Contents |
|---|---|
| 1A (built) | Staff sign-in, owner invitation, sessions |
| **1B** (this) | Tenant public site, brand, locations, contacts per country, site editor; staff sign-in moves to `/staff` |
| 1C | Terms and first device (the tenant goes live), device checks, Administrator and staff, roles with scope, password reset, passkeys, step-up, access levels |
| 1D | Settings (currency, units, payment gate, delivery modes), shipping services, numbering, setup checklist, audit log screen |

## 11. Done when (in addition to the slice gate)

- Two tenants on two hosts show two different sites: logo, colours, texts, SEO, JSON-LD, sitemap. Neither contains "Vixly" (HTTP tests).
- Each location appears under its own country only (test with a U.S. warehouse of a Haitian company).
- A colour pair below AA is refused; an SVG or an oversized image is refused; text is encoded (XSS test with script in every field).
- Draft changes are invisible until published; preview shows them to the signed-in Owner only.
- Public tracking never shows more than milestones and dates, and answers the same for unknown and other-tenant numbers.
- `/staff` runs the Slice 1A sign-in unchanged; `/setup` and `/invitation` keep working while the tenant is set up.
- en, fr and es complete; the page passes an accessibility check (landmarks, headings, contrast, keyboard).
