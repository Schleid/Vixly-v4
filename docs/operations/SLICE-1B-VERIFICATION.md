# Slice 1B: The tenant's public site - verification record

| | |
|---|---|
| Plane | Operations (Stage 3) |
| Date | 2026-10-18 |
| Built on | Slice 1A as delivered, with the dev team's Control login styling; not committed here: the dev team owns git |
| Design | [Slice 1B design note](SLICE-1B-TENANT-SITE-DESIGN.md), [Slice 1A verification](SLICE-1A-VERIFICATION.md), ADR-0012, ADR-0017, ADR-0019, ADR-0021 |
| Status | **Built and verified by automated tests; waiting for owner sign-off.** Part 1C follows (going live, staff) |

## 1. What was built

- **The tenant's public site** on `{slug}.techcrafters.ltd`:
  - `/` goes to `/en`, `/fr` or `/es`, from the visitor's language cookie or the tenant's language. Each address always shows its own language.
  - **Home:** the tenant's logo or name, language links, Track, Locations, **Customer login** and **Sign up** (to `/customer/sign-in` and `/customer/sign-up`); hero with headline, text, optional image, the tracking box and "Create my account"; services; how it works; locations; questions and answers; footer with the description, social links, "Staff login" and the copyright (customer login only in the header).
  - **`/{language}/locations`:** branches and warehouses grouped by the country of **their own address**, with flag, address, opening hours, role badges (office, receiving warehouse, pickup point, accepting deliveries) and "Get directions", never a phone number; then the contacts of that country: every call-centre number (up to 12, in the order the owner entered them), WhatsApp, email and hours.
  - **`/{language}/track`:** milestones and dates only (ADR-0021). Until Slice 3 every number answers "not found", the same answer as another tenant's number. 30 lookups per minute per address.
  - Default texts in en, fr and es carry the tenant's name, so a new tenant has a complete site before writing anything.
- **SEO, all about the tenant:** title and meta description from its texts; canonical address, `hreflang` for each language and `x-default`; Open Graph and Twitter card; favicon, touch icon and `theme-color`; JSON-LD `Organization` (logo, contact points per country, social profiles) and one `LocalBusiness` per public location; `robots.txt` and `sitemap.xml` per tenant with language alternates. Staff, setup, invitation, customer and image addresses are not indexed. A tenant being set up answers `Disallow: /` and has no public site.
- **Images:** `/site/image/{id}`, from the database (owner decision). PNG, JPEG or WebP, recognized by their content (SVG and anything else refused), 1 MB at most (favicon 256 KB, square), size limits per kind; served with their own type, `nosniff`, and a one-year cache once published. Draft images answer only to the signed-in Owner.
- **The Owner's editor** at `/staff/site` and `/staff/locations`:
  - name, colour theme (Ocean, Harbour, Sunrise, Forest, Graphite, Ruby), main and accent colours, services shown, how it works and questions on or off, social links (https only);
  - every text per language (headline, introduction, meta description, footer, six services, four steps, six questions); an empty text falls back to the default;
  - logo, logo for dark backgrounds, favicon and main image (upload and remove);
  - "Preview" per language shows the draft as visitors will see it (`noindex`, never cached); "Publish" replaces the public version;
  - branches, warehouses (with roles, accepting deliveries, own address) and contacts per country; "show on the website" per location.
  - Every change and every publish is audited (`ops.site.*`, `ops.locations.*`).
- **Rules the domain enforces:** WCAG contrast (white text on the main colour 4.5:1, accent 3:1; every preset passes); plain text only (`<` and `>` refused) with a length per text; known text keys and languages; ISO country codes; phones in international form; location codes unique per tenant.
- **Staff sign-in moved to `/staff`** (`/staff/sign-in/*`, `/staff/sign-out`). The Slice 1A pages and flows are unchanged.
- **Data:** migration `ContactPhones` replaces the contact's single phone with a list of numbers (an existing number is kept). Migration `TenantSite` adds `SiteImages`, `SiteTexts`, `Branches`, `Warehouses` and `CountryContacts`, and the draft and published columns of `TenantBrands`, each with `TenantId`. An existing brand keeps its name in both versions (checked by upgrading a Slice 1A database that had a brand).
- **Texts:** `OperationsText` grows from 94 to 536 keys in en, fr and es: 193 for the site, the editor and the new errors, and the 249 country names (copied from `PlatformText`).

## 2. Automated checks

Release builds of the three solutions: no errors, no warnings.

| Test project | Slice 1A | Now |
|---|---|---|
| Operations.Domain.Tests | 29 | 52 |
| Operations.Application.Tests | 40 | 46 |
| Operations.Infrastructure.Tests (SQL Server) | 21 | 28 |
| Operations.Api.Tests | 14 | 14 |
| Operations.Web.Tests (SQL Server) | 17 | 25 |
| Customer tests | 20 | 20 |
| Platform tests | 657 | 657 |
| Public.Web, Shared.Kernel, Shared.UI | 113 | 113 |
| Architecture.Tests | 7,404 | 7,711 |

All 8,666 pass. The global SQL script runs twice on an empty server without errors.

What the new tests cover:
- **End to end over HTTP with a real database** (each tenant's Owner is invited, sets a password and enrols an authenticator app first):
  - two tenants show two sites: title, description, canonical, `hreflang`, JSON-LD, robots and sitemap of their own; neither shows the other, and no page, header, robots or sitemap contains "Vixly";
  - `/` goes to the tenant's language (`/en`, `/es`);
  - a draft headline is invisible to visitors, even with `?preview=true`; the Owner's preview shows it with `noindex` and `no-store`; after publishing, visitors see it;
  - a Fort Lauderdale warehouse of a Haitian company is listed under the United States, the Port-au-Prince office under Haiti with the three call-centre numbers, and the office's own number appears nowhere on the site;
  - the customer login link appears once, in the header;
  - a location named `<script>alert(1)</script>` is encoded on the page;
  - a wrong phone number is refused with its message;
  - an SVG upload is refused; a PNG is accepted, hidden from visitors until published, then served as `image/png` with `nosniff` and an immutable cache;
  - a light main colour and HTML in a text are refused with the reason;
  - tracking answers "not found";
  - the editor sends a visitor to `/staff`; a tenant being set up sends `/en` to `/setup` and closes `robots.txt`.
- **Use cases:** preview only for the signed-in Owner; editing, publishing and uploads need the Owner; audit of publish; image checked by content; draft images hidden from visitors; locations grouped by their own country, hidden ones left out, codes unique.
- **Domain:** every preset passes the contrast check; WCAG ratios; light colours refused; https links only; plain, limited, known texts and languages; draft and published versions; image types, sizes and favicon shape; locations keep their own country; bad country, phone and missing role refused.
- **Database:** draft and published brand, texts and image round-trip; locations and contacts invisible to another tenant; location codes unique per tenant; the image check reads PNG, JPEG and WebP and refuses SVG, GIF, HTML and empty files.

## 3. Where the build differs from the design note

- **Image addresses:** one address per image, `/site/image/{id}`, instead of one per kind and version. A new upload is a new id, so the long cache stays safe.
- **Editor pages** are server-rendered forms with antiforgery and the form guard, like the Slice 1A pages, not interactive pages. The contrast check runs when saving and gives the reason; a live check in the page is Frontend/UI work.
- **Locations** are listed by country with anchors (`#country-ht`), not as tabs.

## 4. Not covered here

- **The plan's branding entitlement (W-4):** every tenant may change colours for now. Operations does not read entitlements yet; this arrives with the plan check in 1D. "Powered by" is never shown.
- **The tracking bot check after repeated misses:** only the rate limit is in place; the check comes with Slice 3, when numbers can be found.
- **Legal links** (the tenant's terms and privacy texts) and the legal name in the footer: with the terms in 1C.
- **Languages per tenant:** every site speaks en, fr and es until tenant settings (1D) let a tenant choose.
- **Customer sign-up and sign-in pages:** the links point to `/customer/...`, which Slice 2 fills.
- **The final look** of the site and the editor: Frontend/UI. The site uses only `tenant-site.css` and two colour properties, so a redesign does not touch the tenant data.

## 5. Deployment notes

- **Edge:** `{slug}.techcrafters.ltd/robots.txt` and `/sitemap.xml` must reach Operations.Web (not a shared static file).
- **Request size:** image uploads accept at most 1 MB plus the form; no server setting is needed.
- **Database:** run the newest global SQL script (migration `TenantSite`).

## 6. Development

- Tenant sites answer on `https://{slug}.localhost:7404` (browsers send `*.localhost` to the machine, no hosts file needed); `https://localhost:7404` is `northwind-freight`. The same holds for Customer.Web.
- **Demo owners:** at start-up in Development, Operations.Web asks Platform.Api which of Platform's demo tenants are active and runs the real owner invitation for each one that has no staff yet: Owner, brand and the invitation email in the `.mail` folder, with a link to `https://{slug}.localhost:7404/invitation`. Restarts add nothing new. Switch it off with `OperationsDemoData:SeedOnStartup = false`.
- Platform.Api must be running and must know the `operations` caller (`PlatformInternal:Callers:operations`, scopes `tenant`, `portal`, `events`, with the public key; `OperationsPlatform:CallerKeyId` and `CallerPrivateKey` in Operations.Web's user secrets). Without it every tenant host answers "try again" and the demo seed logs that Platform did not answer.
