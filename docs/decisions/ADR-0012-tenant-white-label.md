# ADR-0012: Tenant white label

- Status: **Accepted** (owner decision, 2026-10-13); **amended 2026-10-14** (owner): white label is an Operations matter; Control is never white-labelled (see Amendment 1)
- Date: 2026-10-13
- Deciders: Jude (owner)
- Affects: every tenant-facing surface (Operations, Customer, Edge, tenant landing page, tenant emails and documents); Platform Slices 2 and 3; Shared.UI; Global Standard sections 1 and 5.5

## Context

V4's tenant product (the Operations plane and the surfaces it serves) is offered white-label: a tenant can present it to its staff and customers under its own brand and domain, with no visible Vixly mark. The Platform plane (Control) is proprietary to TechCrafters: it is never white-labelled and never offered to anyone; it runs Vixly's own business, including the finance side of white label (plans, entitlements, billing). The Public site is always Vixly's.

V3 hardcodes `vixly.ltd` in 11 files and has no tenant branding beyond an unbuilt landing-page theming design (legacy `ControlPlane/docs/landing-page-theming/001–005`). That design's model is kept: code-owned presets, allowlisted tokens, draft and publish, no raw CSS. It is extended to every tenant surface.

## Decision

Rules for every plane:

- **W-1 No product name in tenant-facing text.** Resources for tenant surfaces (Operations, Customer, Edge pages, tenant emails, PDFs) never contain "Vixly". They use the `{brandName}` placeholder, filled from the tenant's brand. An L-10 build check fails on a literal "Vixly" in those resource sets. The Platform and Public resources may name Vixly.
- **W-2 Brand is Operations data** (amended 2026-10-14). `OperationsTenantBrand` holds the display name, logos (light and dark), favicon and app icon, theme preset and allowlisted tokens, the email sender name and reply-to address, support contacts and the legal footer. It is versioned with draft and publish, edited by the tenant's own administrators in Operations, and exposed to Customer and Edge through `Contracts.Operations`. Control never stores or edits it.
- **W-3 One theme renderer.** A `ThemeProvider` in `Shared.UI` turns a validated brand into CSS custom properties that override the `--vx-*` tokens on tenant surfaces only. No raw CSS, HTML or script from tenants. Publishing fails if a colour pair misses WCAG 2.2 AA. Vixly's blue and white stays the default preset.
- **W-4 Entitlements, not plan names; Control keeps the finance side.** Basic branding (logo, colours) and the **White Label** bundle are plan features, sold, invoiced and collected by Control (Platform Slices 3 to 6) and read by Operations through `Contracts.Platform`. The bundle covers: no "Powered by Vixly", a custom domain, a custom email sending domain, and app title and icons. When a tenant loses the entitlement, its brand data is kept and the surfaces fall back without loss.
- **W-5 Custom domains.** The tenant's domain records live with the brand in Operations. Edge resolves a verified custom domain to the tenant, beside `{slug}.<base domain>` (designed in legacy `custom-domains/002`). TLS is issued at Cloudflare. A domain is used only after it is verified.
- **W-6 Identity follows the host.** Tenant staff and customer passkeys are bound to the hostname they were registered on. The TOTP issuer shown in authenticator apps is the tenant's display name. Moving a tenant to a custom domain therefore needs a guided passkey re-enrolment. Set the custom domain before the tenant's users enrol.
- **W-7 Email and documents.** Tenant emails always carry the tenant's sender name. Without a custom sending domain they go from a neutral platform sending domain, never a Vixly-named one, and only after SPF and DKIM are verified. Invoices, receipts and other customer documents carry the tenant's brand only; no Vixly mark ever appears on a tenant-to-customer document.
- **W-8 The tenant base domain is neutral.** A white-label tenant without a custom domain must not show "vixly" in its URL. The base domain stays configuration (H-1). Before tenants migrate, the owner decides whether `{slug}.vixly.ltd` remains the default or a neutral base domain is used for white-label tenants.

## Consequences

- Platform Slice 2 does **not** add brand or custom-domain records (amended 2026-10-14); they are Operations work. Platform Slice 3 adds the Branding and White Label features to plans; Slices 4 to 6 bill and collect them.
- Frontend/UI designs every tenant surface to work with any allowlisted brand. The blue and white direction is the default preset, not a fixed look for tenant surfaces. The Control console has one fixed look and is never themed by a tenant.
- Customer and Edge read the brand through `Contracts.Operations` (cached, with a fallback), never from their own copy. Operations reads the tenant's entitlements through `Contracts.Platform`.
- Slice 1 (Platform staff identity) is unaffected: the console is always Vixly.

## Amendment 1 (2026-10-14, owner)

White label applies to the Operations plane only. Control (the Platform plane) is proprietary to TechCrafters, is not offered to anyone, and manages the finance side: plans, entitlements, invoices and collections. Consequently W-2 and W-5 move brand and custom-domain data from Platform to Operations, W-4 states Control's part, and Platform Slice 2 drops `PlatformTenantBrand`.
