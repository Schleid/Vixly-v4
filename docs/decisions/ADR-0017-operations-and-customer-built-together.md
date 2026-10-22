# ADR-0017: Operations and Customer built together; the theme marketplace belongs to Platform

- Status: **Accepted** (owner decisions, 2026-10-16)
- Date: 2026-10-16
- Deciders: Jude (owner)
- Affects: Operations, Customer, Platform and Edge planes; Global Standard section 12 (build order)

## Context

Global Standard section 12 builds one plane at a time: Platform, Public, Operations, Customer, Edge. The owner chose a big-bang weekend migration from V3 (Operations plan, O-13), so no V3 tenant moves until V4 does everything the tenant uses today. The [V3 review](../operations/V3-OPERATIONS-REVIEW.md) shows that a casillero business depends on the customer side every day: self-registration, the mailbox address, pre-alerts, shipments, invoices and Zelle payment. Operations alone can therefore never go live, and building Customer only after every Operations slice would find gaps between the two sides months late.

The Operations plan listed a "Landing-page marketplace" slice. The owner placed the theme and template marketplace in Control (Platform), reached from Operations.

## Decision

1. **Operations and Customer are built side by side.** Stage 3 covers Operations, `Contracts.Operations` and the Customer plane. Each Operations slice ships with the customer screens it serves (for example: customers and mailboxes with customer sign-up and the mailbox address; receiving with pre-alerts; shipments with tracking; finance with customer invoices and payment). A slice's gate (section 12) covers both sides, including tenant isolation for every customer-facing read. The Operations plan's separate "Customer portal" slice is removed.
2. **The theme and template marketplace is a Platform feature: Platform Slice 10.** Platform keeps the catalog, versions, previews and prices, which plans include which themes, purchases (billed as add-ons on the Platform invoice) and which themes each tenant owns. Staff publish and price themes in the console. Operations reaches it through the internal API (ADR-0014) from a "Website" screen.
3. **A tenant's site content lives in Operations**: texts, photos, services, rates and contact shown on the site, kept with the tenant's other business data. Edge shows the site on the tenant host with the owned theme and that content.
4. **The marketplace is built after the migration.** V3 tenants do not need it to move; their current landing page and branding settings migrate as plain content.
5. **Edge stays Stage 5**, and its work needed on migration weekend (the tenant host, `{slug}.`) is listed in the migration plan.

## Consequences

- Global Standard section 12 is amended: Stage 3 reads "Operations, `Contracts.Operations` and Customer, built together slice by slice (ADR-0017)"; Stage 4 is retired; Edge stays Stage 5.
- Each Operations slice design note has a "Customer side" section and its verification record covers both planes.
- Customer self-registration (and every other anonymous customer form) carries the bot check before it is sent, as the public site's contact and sign-up forms do (ADR-0019). Customer pages run on tenant hosts and custom domains, so each host is added to the Turnstile widget's allowed hostnames when the tenant is set up (owner request, 2026-10-17).
- The Customer plane's references are unchanged: it reads business data only through the Operations API.
- The Platform plane document gains Slice 10 (Marketplace) in its backlog, marked "after the migration".
- Frontend/UI's Operations workbench primitives (ADR-0016, proposed) are adopted slice by slice by both the Operations and Customer screens.
- The Operations plan's slice order no longer lists "Customer portal" or "Landing-page marketplace".
