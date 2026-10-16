# Public styling pass — 2026-10-16

Owner: Frontend/UI. Scope: presentation only; no backend, presenter, state, resource key or workflow changes.

## Layers

1. Shared tokens: content widths, responsive display typography, spacing, control targets and border/focus metrics.
2. Shared primitives: standalone button, card and message foundations; full-width inputs and resizable text areas. New visual defaults use low-specificity selectors so existing plane rules retain precedence. Public no longer needs Platform CSS for these controls.
3. Public layout: a separate marketing frame, balanced hero, section rhythm, responsive navigation, language selection, feature grids, numbered workflow, pricing alignment, contact form and footer. All colours reference semantic tokens; there are no plane dark-mode overrides.
4. Composition: existing SSR markup, localization keys and actions are preserved.

## Owner decisions

- Public retains its marketing layout on shared tokens.
- Console chrome gets dedicated semantic tokens in the subsequent Platform pass.
- Frontend/UI approves new tokens; architectural changes receive Control review.
- Tenant overrides are restricted to brand accents; presets control other values. No ThemeProvider or tenant configuration changes are included here.

## Verification

- Public HTTP suite: 21 passed, zero failed.
- Public CSS raw colour scan: no colour literals.
- No backend or application logic changed.
- Visual sign-off is pending: the in-app browser timed out reaching localhost and the local Chrome connection also timed out. The en/fr/es, light/dark, 375/768/1280/1600 and keyboard matrix must be reviewed before this plane is declared complete.
- Shared Inter font hosting remains an existing foundation task; this pass preserves the current fallback stack.

The existing Stage 2 delivery was uncommitted when this pass began. Its application files are deliberately excluded from this styling commit.

## Screenshot correction

Owner screenshots exposed invisible primary link-button labels. The zero-specificity `:where()` action variant lost to Public's generic `a` colour rule, producing blue text on blue fill. Primary variants now retain class specificity in normal and hover states; all existing labels and actions are preserved. Diff validation passes. Browser visual verification remains pending; no preview process was started for this correction.

## Logistics imagery

Added locally hosted, generated warehouse-scanning and morning-dispatch WebP images (approximately 288 KB combined). Decorative CSS integrates a two-column home hero and workflow banner, stacked at the tablet breakpoint. Prompts, provenance and checks are recorded in wwwroot/images/README.md. Backend and page composition are unchanged; live browser verification remains pending.
