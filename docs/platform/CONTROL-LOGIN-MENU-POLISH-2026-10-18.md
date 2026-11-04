# Control login and navigation polish

The accepted navy-to-cobalt panel, transparent security illustration, left-panel logo and compact white credential form remain the foundation.

## Login

- Stronger heading hierarchy, quieter top chrome and a subtle concentric architectural motif behind the original cutout.
- Softer container depth, clear field hover/focus/invalid states, consistent control radii and a directional sign-in cue.
- One brief entrance animation and small hover transitions; reduced-motion preference disables them.
- Decorative latency telemetry is hidden rather than presented as measured network health.
- All localized strings, authentication endpoints, antiforgery, autocomplete and passkey behavior are retained. No new imagery or authentication logic.

## Menu

- Navy/cobalt brand treatment, clearer separators and disclosure chevrons, consistent icon weight and distinct active/hover states.
- The brand and account area remain visible on desktop while the navigation scrolls independently. Native disclosure groups retain keyboard behavior.
- On smaller screens the navigation remains vertically bounded with internal scrolling; existing routes and permissions remain unchanged.
- Changes live in the real Control stylesheet and login markup. No production preview route, synthetic account or authentication override is added.

## Verification

- Control web build: zero warnings/errors. Existing Control web tests: 42 passed.
- Actual HTTPS login preview checked at 1280×720 and 375×812. Mobile page fits vertically, has no horizontal overflow and retains 44px form controls.
- The menu preview renders the real MainLayout with a clearly marked synthetic display identity in an isolated temporary host, without live platform data. At desktop the sidebar and footer end at the viewport boundary while navigation scrolls internally.
- Native menu disclosures were expanded/collapsed by keyboard with visible focus. Mobile navigation remains bounded to 256px with no horizontal overflow.
- Temporary preview sources remain outside the repository and preview servers are stopped after verification. Changes are for owner/consumer review in the existing draft PR, not deployed.
