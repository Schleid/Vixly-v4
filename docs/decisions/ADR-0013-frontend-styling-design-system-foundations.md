# ADR-0013: Frontend styling and design system foundations

- Status: **Accepted** (owner decision, 2026-10-14)
- Date: 2026-10-14
- Deciders: Jude (owner)
- Affects: Frontend/UI, `Vixly.Shared.UI`, `Vixly.Shared.UI.Gallery`, `Vixly.Platform.Web`, Global Standard sections 1, 5.3, 10

## Context

The Platform staff console works end to end (sign-in, MFA, passkeys, user management, My account, step-up), currently styled by a 195-line placeholder stylesheet. As Frontend/UI takes over the console styling and establishes the shared design system for V4, four foundational design decisions must be locked:
1. Component library (e.g. FluentUI packages) versus building our own Semantic CSS primitives.
2. Typography and brand asset hosting strategy (CDN vs self-hosted, CSP adherence).
3. Icon set licensing and distribution.
4. Viewport breakpoints and UI density rules.

## Decision

- **F-1 Own Semantic CSS & Razor primitives in `Vixly.Shared.UI`.** Rather than taking a dependency on the Microsoft.FluentUI Blazor library, V4 builds lightweight, presentational Razor primitives in `Vixly.Shared.UI` styled via semantic CSS custom properties (`--vx-*`).
  - *Static SSR Compatibility:* Sign-in and recovery flows run as static server rendering without Blazor circuits; our primitives render clean HTML without web-component polyfills or hydration scripts.
  - *White-labeling (ADR-0012):* Tenant theme overrides apply directly via CSS variables without Shadow DOM or styling isolation conflicts.
  - *Zero package bloat:* `Vixly.Shared.UI` retains zero external NuGet dependencies.
- **F-2 Self-hosted Inter font and vector assets.** Typography uses self-hosted Inter (`woff2`, Latin Extended for full French/Spanish accent support) with fallback to `Segoe UI Variable, Segoe UI, system-ui, -apple-system, sans-serif`.
  - Self-hosting satisfies Security Baseline **S-10** (strict CSP: no external font CDNs or third-party requests on sign-in pages).
  - Brand mark and favicon are self-hosted SVGs that leverage CSS tokens (`currentColor` and `--vx-color-primary`).
- **F-3 Permissive SVG icon components.** Icons are built as lightweight SVG Razor components (based on Fluent UI System Icons / Lucide) under the MIT license, using `currentColor` so icons inherit semantic token colors.
- **F-4 Responsive breakpoints and density.**
  - *Breakpoints:* Mobile (`< 640px`, stacked layout, full-width actions, touch targets $\ge 44\text{px}$), Tablet (`640px`–`1024px`), Desktop (`1024px`–`1440px`), and Wide Desktop (`> 1440px`, constrained content containers).
  - *Density:* Default **Comfortable** density for console navigation, forms, cards, and auth flows; **Compact** density (~36px row height with tabular figures) for high-volume data tables and user lists.
- **F-5 Token-only styling.** All colors, spacing, typography scales, elevation shadows, and border radii are defined through `--vx-*` tokens in light and dark modes with WCAG 2.2 AA verified contrast ratios. No hardcoded hex or pixel values exist in component markup or scoped CSS.

## Consequences

- `Vixly.Shared.UI` starts implementing Batch 1 primitives (Button, TextInput, StatusBadge, Message, Card, Table with Pager, Dialog, EmptyState).
- `Vixly.Shared.UI.Gallery` is configured as a standalone Blazor application displaying every primitive across all states, light/dark themes, and `en`/`fr`/`es`/`en-XA` locales.
- `Vixly.Platform.Web` shell frames (`MainLayout.razor` and `AuthShell.razor`) and feature components are updated to use these primitives and tokens without altering presenters or state.
