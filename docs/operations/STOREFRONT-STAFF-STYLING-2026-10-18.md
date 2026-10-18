# Operations storefront and staff access styling

The tenant public site now has a freight-image hero, prominent tracking band, service-specific icons, four-step process, country-grouped locations and a clear footer with discreet staff access. Tenant-published image settings take precedence over the original fallback image. Existing company names, service visibility, translations, preview status, customer links, tracking action, locations, SEO and publishing behavior are retained. No quotes, rates, statistics, routes or operating capabilities are invented.

The staff sign-in retains the existing form and endpoints in a responsive security-themed layout with the tenant's company name. Shared semantic palette tokens are used because the current authentication view receives the company name, not published brand settings; no new branding contract or backend read was introduced. Existing recovery, MFA, session-warning and double-submit behavior remain in place. The security illustration is an independent static copy of our original unbranded artwork, not a runtime dependency on another plane.

## Inspiration and assets

Reviewed [Shippex](https://shippex.net/), [Dalbagay](https://dalbagay.com/) and [WELJ](https://www.weljht.com/) for tracking prominence, customer entry points and country-based locations. Their text, logos, images and layouts were not copied.

`src/Operations/Vixly.Operations.Web/wwwroot/images/logistics-network.webp` is an original image generated with the built-in imagegen tool in generation mode, then resized and encoded as WebP. It is generic decorative freight imagery, not a representation of a tenant's actual facilities.

Final generation prompt:

Use case: photorealistic-natural. Asset type: original fallback hero image for a white-label international freight company's public landing page. Wide landscape editorial photograph, sophisticated commercial logistics photography. Scene: a modern coastal cargo terminal in warm early morning light, a clean unbranded white medium cargo truck in foreground on the right, orderly navy and muted blue shipping containers to the left, a white cargo aircraft visible in the background near a runway, distant ocean and port cranes, believable spatial scale. Strong diagonal composition, clean professional environment, subtle human scale but no identifiable close-up people. Color palette deep marine blue, warm ivory, steel and restrained amber highlights. Natural light, fine realistic material texture, calm confident operational reliability. No logos, branding, readable text, numbers, watermarks, fake interface overlays, duplicate vehicles, surreal elements or exaggerated cinematic effects. Landscape 3:2, composition readable at 560 pixels wide. This is generic illustrative freight imagery, no claim of a specific company facility or route.

`staff-security.webp` reuses our previously generated transparent security cutout; its original generation/edit provenance is in `docs/platform/CONTROL-LOGIN-STYLING-2026-10-17.md`.

## Verification and integration

- Operations build: zero warnings/errors. Operations web tests: 10 passed, 30 database-dependent checks skipped by the existing configuration. Focused UI architecture checks: 438 passed.
- The actual Razor components were rendered in an isolated HTTPS fixture with a clearly marked tenant preview and sample locations. No database, live tenant or production authentication was used.
- Desktop and mobile layouts checked, including French and Spanish content, loaded hero image, tenant-only branding, country sections and preserved localized tracking routes. Staff controls are at least 46px high. No horizontal overflow at 375px mobile or 1280px desktop.
- These UI files bind to the Operations Slice 1 baseline already present in the shared workspace. That baseline's uncommitted backend/project files belong to the Operations team and are not swept into this styling commit. Backend baseline integration is required before the PR can build independently from a clean checkout.
- No deployment or content publication. Temporary preview hosts are stopped after review.
