# Compact illustrated Control login

The initial sign-in page uses a navy header and a two-column workspace: an editorial platform-administration sketch beside a more compact credential form. Existing semantic chrome tokens provide the navy surfaces. Header/logo spacing, field gaps and passkey spacing are reduced while inputs and buttons retain 44px minimum height. At 720px and below the decorative illustration is hidden to prioritize the form.

The illustration is decorative (`alt=""`, hidden from assistive technology). All existing localized text, form endpoints, antiforgery fields, return URLs, password autocomplete, passkey support and submit protection remain intact. Recovery and MFA pages retain their existing presentation. No backend logic changes.

## Illustration provenance

Asset: `src/Platform/Vixly.Platform.Web/wwwroot/img/control-login-workspace.webp` (960Ã—720, optimized WebP). Generated with the built-in imagegen tool in generation mode; resized and encoded with Pillow.

Final prompt:

Use case: stylized-concept. Asset type: illustration beside a compact staff login form for Vixly Control, an enterprise logistics SaaS platform administration console. Scene: elegant quiet platform administrator workstation, no people, open laptop angled three-quarter view with a refined abstract admin dashboard, rows of tenant accounts, billing summaries and small charts represented by simple lines with no readable text. Beside it a slim notebook and small brass desk lamp. In the background delicate architectural line drawing of connected office buildings suggesting multiple businesses managed from one platform, not a warehouse or delivery scene. Premium editorial pencil and ink sketch with restrained watercolor washes on warm ivory paper. Palette predominantly deep navy blue, slate linework, a little cobalt and muted warm brass. Landscape 4:3 composition, calm and professional, plenty of pale paper around the edges, no text, letters, logos, watermark, giant padlock, courier, parcel handoff, trucks, futuristic holograms or stock-photo appearance.

## Verification

Control web build passed with zero warnings and zero errors. All 42 existing Control web tests passed, including HTTP antiforgery, security-header and rate-limit coverage. Actual HTTPS browser preview checked at 1280×720 and 375×812: all four login controls are 44px high, the desktop panel fits without scrolling, and mobile has no horizontal overflow in English, French or Spanish. Password recovery retains its original single-panel layout. Keyboard navigation was checked and explicit focus rings added to the login controls and links. No credentials were submitted. The temporary preview disables database startup work and uses an ephemeral key provider.
