# Approved Operations staff login

Implements the owner-approved blue/white mockup, including the final removal of the repeated company wordmark beside the logo and the entire Current portal strip. This supersedes the photographic staff-entry section of OPERATIONS-IDENTITY-2026-10-18.md. The storefront and workbench designs remain separate.

## Presentation

- Blue welcome panel occupies 55 percent of the desktop card. The welcome names the company already supplied by the trusted sign-in endpoint.
- Three translucent tiles describe Shipments, Warehouses and Your team. No uptime, idle-time or fabricated activity figures are shown.
- The white side shows the tenant logo beside Operations portal, then the staff form. A green decorative dot accompanies the approved protected-access text; it is not a connection-status signal.
- Mobile places the form first, with the welcome panel and tiles below. Long company names wrap without truncation. Language links, input autocomplete, visible keyboard focus, reduced-motion support and accessible password visibility control are retained or provided.
- The page uses shared semantic color tokens. No raster mockup or platform logo is shipped as UI.

## Published tenant logo

The LoginBrand feature presenter calls the existing GetOperationsPublicSite use case with Preview=false, using tenant identity from the resolved request host. The component initializes its owned data scope from that same trusted context. The existing entitlement handling and image-serving endpoint remain authoritative. No Application, Domain, Infrastructure, authentication endpoint, cookie or security-policy behavior changes.

Only the published LogoImageId is used, through the existing same-origin /site/image/{id} route. An absent logo, unpublished draft, setup state, failed branding read or failed image load retains company initials. No Vixly logo is used as a fallback. The Web composition root adds only the presenter registration to the existing baseline.

## Validation and integration

- Operations build passed with zero warnings/errors. Web tests: 15 passed, 34 database-dependent tests skipped. Five new presenter tests cover published-over-draft selection, resolved-tenant context, missing branding, setup fallback and safe failure.
- Focused UI architecture checks: 452 passed. All 19 new presentation text keys are present and nonempty in English, French and Spanish. French/Spanish draft translations carry needs-review for the native-language release gate.
- Actual components reviewed in an isolated local HTTPS fixture using sample company data, including a test logo image and an initials-only case. No real tenant credentials, database writes or authentication submissions were used.
- Browser checks: desktop card fits the viewport; no horizontal overflow at 375px French mobile or 768px Spanish tablet with a long company name and error state. Inputs are 48px high, password toggle 44px and submit 50px. Verified password visibility both ways, email-to-password keyboard focus, published-image loading, antiforgery/form action and no browser console errors.
- The separate Operations backend/project baseline is still uncommitted in the shared workspace. It must be integrated before the review branch can build independently from a clean checkout. This change includes the composition-root file needed to register the UI presenter; it does not sweep the remaining baseline into the styling commit. No merge or deployment is performed.

The approval mockup is a visual reference only. Its provisional GL mark is not an official tenant asset. Production uses each tenant's published logo, with initials when unavailable.
