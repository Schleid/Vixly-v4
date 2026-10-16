# Platform styling — token layer (2026-10-16)

Owner: Frontend/UI. Presentation changes only; no presenter, state, resource key, use case, security or backend changes.

## Changes

- Fixed stylesheet order: Shared.UI theme, Shared.UI primitives, Platform layout.
- Added dedicated semantic chrome tokens, keeping the navigation rail dark in both themes as requested by the owner.
- Removed all hex/RGB colour literals from the console stylesheet. General surfaces, text, borders and elevation use existing shared tokens; print and QR surfaces use explicit invariant tokens.
- Removed duplicate dark-mode paint overrides. Theme-dependent icon visibility and native date-indicator adjustment remain presentation rules.
- Simplified glowing chrome decoration, increased secondary-label contrast, added visible keyboard focus, and established 44px minimum targets for navigation, theme, language and sign-out controls.
- Sign-in now uses semantic app surfaces instead of a fixed dark background. Primary actions use matched link/surface colours in both themes.

## Checks

- Balanced CSS braces, no undefined token references, no hex/RGB literals, correct stylesheet ordering, clean diff whitespace.
- Calculated contrast: chrome text 12.02:1, secondary text 8.76:1, active navigation text 12.62:1, active accent 7.00:1, danger 7.73:1; primary actions 6.70:1 light and 6.82:1 dark.
- Shared control conflicts reviewed: table loading/empty states and card headers retain their more-specific primitive rules; plane button variants follow the fixed load order.
- Automated test results recorded after the run below. Initial parallel builds collided on shared output files; verification rerun sequentially.
- Live visual review remains required for light/dark, en/fr/es, keyboard navigation and 375/768/1280/1600 widths. No preview host was started or left running in this pass.

## Next layer

Reusable shell/navigation, forms, toolbars, badges, KPI and auth styles still require extraction into Shared.UI with Gallery coverage. Existing literal geometry, inline SVG icons, inline layout styles and self-hosted Inter remain follow-up foundation work. This record does not declare the entire Platform styling migration complete.

Pre-existing uncommitted Stage 2 delivery files are excluded from this commit.

## Automated results

Platform Web: 32 passed, 0 failed. Architecture: 6,363 passed, 0 failed. Sequential reruns completed successfully. Static CSS checks and diff whitespace checks passed.

## Quiet workspace trial (owner feedback)

Owner rejected the background, card style and menu and approved trying a crisp workspace direction. This presentation-only trial uses an opt-in neutral workspace token, flat lightly bordered panels, restrained headings, simpler dark navigation and a narrow active marker. Cards and KPIs do not lift on hover. Public is unaffected by the new workspace tokens.

Login becomes an open, left-aligned form on a plain surface, without a floating card, decorative grid/glows, oversized logo tile or telemetry chrome. Functional forms, passkeys, language links, errors, recovery and menu composition remain unchanged. Wider recovery-code and QR flows retain additional room. Print styles remain outside the screen-only trial rules.

Static verification: balanced CSS blocks, all token references defined, no raw console colours, clean diff whitespace. No backend changes or preview processes. Live light/dark, responsive and keyboard visual review is pending owner feedback on this design direction.

## Trial withdrawn

Owner rejected the quiet workspace trial. Restored the console stylesheet and theme tokens from e40ffab. Removed the explanatory passkey helper line from sign-in markup. Scoped hidden-attribute enforcement prevents passkey feedback from showing before the existing browser flow reveals it; sign-in actions and actual error feedback are preserved. Diff whitespace checks pass. No backend or authentication logic changed.

## Enterprise workbench implementation

See [the shared implementation and verification record](../shared/ENTERPRISE-WORKBENCH-2026-10-16.md). The owner supplied a new dense enterprise brief after the rejected workspace trial. This pass adds compact tables/panels, canonical shared status styling and native collapsible navigation while preserving current Control workflows. New operational screens remain labelled Gallery fixtures pending ADR-0016 and Stage 3 integration.

## Direct Control adoption

See [Control workbench adoption](CONTROL-WORKBENCH-ADOPTION-2026-10-16.md): shared metrics now bind directly to existing Control presenters on the home page and tenant/subscription/invoice directories, with localized navigation and functional tenant search.
