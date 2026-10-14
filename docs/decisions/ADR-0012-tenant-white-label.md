# ADR-0012: Tenant white label

- Status: **Accepted** (owner decision, 2026-10-13)
- Date: 2026-10-13
- Deciders: Jude (owner)
- Affects: every tenant-facing surface (Operations, Customer, Edge, tenant landing page, tenant emails and documents); Platform Slices 2 and 3; Shared.UI; Global Standard sections 1 and 5.5

## Context

V4 is offered as a white-label product: a tenant can present Vixly to its staff and customers under its own brand and domain, with no visible Vixly mark. The Platform console and the Public site are always Vixly's.

V3 hardcodes `vixly.ltd` in 11 files and has no tenant branding beyond an unbuilt landing-page theming design (legacy `ControlPlane/docs/landing-page-theming/001–005`). That design's model is kept: code-owned presets, allowlisted tokens, draft and publish, no raw CSS. It is extended to every tenant surface.

## Decision

Rules for every plane:

- **W-1 No product name in tenant-facing text.** Resources for tenant surfaces (Operations, Customer, Edge pages, tenant emails, PDFs) never contain "Vixly". They use the `{brandName}` placeholder, filled from the tenant's brand. An L-10 build check fails on a literal "Vixly" in those resource sets. The Platform and Public resources may name Vixly.
- **W-2 Brand is Platform data.** `PlatformTenantBrand` holds the display name, logos (light and dark), favicon and app icon, theme preset and allowlisted tokens, the email sender name and reply-to address, support contacts and the legal footer. It is versioned with draft and publish. It is exposed through `Contracts.Platform` and cached by the consumer planes.
- **W-3 One theme renderer.** A `ThemeProvider` in `Shared.UI` turns a validated brand into CSS custom properties that override the `--vx-*` tokens on tenant surfaces only. No raw CSS, HTML or script from tenants. Publishing fails if a colour pair misses WCAG 2.2 AA. Vixly's blue and white stays the default preset.
- **W-4 Entitlements, not plan names.** Basic branding (logo, colours) and the **White Label** bundle are plan features. The bundle covers: no "Powered by Vixly", a custom domain, a custom email sending domain, and app title and icons. When a tenant loses the entitlement, its brand data is kept and the surfaces fall back without loss.
- **W-5 Custom domains.** Edge resolves a verified custom domain to the tenant, beside `{slug}.<base domain>` (designed in legacy `custom-domains/002`). TLS is issued at Cloudflare. A domain is used only after it is verified.
- **W-6 Identity follows the host.** Tenant staff and customer passkeys are bound to the hostname they were registered on. The TOTP issuer shown in authenticator apps is the tenant's display name. Moving a tenant to a custom domain therefore needs a guided passkey re-enrolment. Set the custom domain before the tenant's users enrol.
- **W-7 Email and documents.** Tenant emails always carry the tenant's sender name. Without a custom sending domain they go from a neutral platform sending domain, never a Vixly-named one, and only after SPF and DKIM are verified. Invoices, receipts and other customer documents carry the tenant's brand only; no Vixly mark ever appears on a tenant-to-customer document.
- **W-8 The tenant base domain is neutral.** A white-label tenant without a custom domain must not show "vixly" in its URL. The base domain stays configuration (H-1). Before tenants migrate, the owner decides whether `{slug}.vixly.ltd` remains the default or a neutral base domain is used for white-label tenants.

## Consequences

- Platform Slice 2 adds `PlatformTenantBrand` and custom-domain records; Slice 3 adds the Branding and White Label features to plans.
- Frontend/UI designs every tenant surface to work with any allowlisted brand. The blue and white direction is the default preset, not a fixed look for tenant surfaces.
- Operations and Customer read the brand through `Contracts.Platform` (cached, with a fallback), never from their own copy.
- Slice 1 (Platform staff identity) is unaffected: the console is always Vixly.
