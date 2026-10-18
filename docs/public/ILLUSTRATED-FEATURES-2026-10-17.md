# Illustrated homepage and Features redesign — 2026-10-17

Owner: Frontend/UI. Scope: the two supplied homepage screenshots and a creative Features page redesign with motion.

## Delivery

- Homepage: six subject-specific sketches sit below the module text. A four-card desktop row wraps to two centered bottom cards. Tablet uses two columns, mobile one. The scanning-only hero photograph is replaced by a coherent end-to-end logistics sketch. The flow banner uses dispatch artwork.
- Features: a new illustrated introduction, sticky seven-section index, numbered alternating chapters, dotted connecting spine and closing sales/plans links. Warehouse management and inventory remain explicit in the existing localized copy. Seven chapter images are distinct and subject-specific.
- Motion: a finite route-line drawing, staggered route-node arrival, small image/card hover responses, and progressive scroll-linked chapter movement. No looping motion, hidden content or new JavaScript. Reduced-motion media rules remove the effects; browsers without scroll timelines receive ordinary visible content.
- Eight original sketches, optimized WebP total under 1 MB. Prompt set and filenames are in Public wwwroot/images/README.md. Illustrations are decorative; all required content stays localized HTML.
- Existing localized copy, registration/contact destinations, feature heading IDs and source arrays are preserved. New chapter anchors expose the full chapter below the sticky index. No backend, form, authorization, security, subscription or payment behavior changed.

## Verification

- Public builds with zero warnings/errors.
- Final Public regression run: 23 passed. Focused architecture UI rules: 367 passed.
- Browser: all six card images and all seven distinct chapter images load. The two bottom desktop cards are centered. Feature navigation reaches warehouse/customer sections, the sticky index remains visible, and scroll-linked animation is active in the preview browser.
- Responsive document-width checks: mobile homepage and Features at 375, French Features at 375, Spanish Features at 768; no document overflow. The narrow index scrolls within itself. Desktop checked at 1280.
- New CSS tokens and braces verified, markup size/header budgets checked, scoped diff whitespace checked. No sales request or registration was submitted. Temporary hosts are stopped after visual review.
- Review screenshots: public-sketch-hero.png, public-sketch-modules.png and public-feature-journey.png in this chat's visualization artifacts.

Delivered on the UI review branch through draft PR #1. Production requires review, merge and deployment. Only Frontend-owned marketing markup, styles, assets and this record are included; unrelated backend/application worktree changes remain excluded.
