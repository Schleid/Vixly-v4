# Compact illustrated Control login

The initial sign-in page uses a medium blue header and a two-column workspace: a security and multi-factor authentication sketch beside a compact credential form. Existing semantic chrome and primary tokens are mixed to lighten the blue surfaces. The logo sits on the blue panel instead of above the form. Field gaps remain compact while inputs and buttons retain 44px minimum height. At 720px and below only the blue logo strip remains above the form; the decorative sketch is hidden.

The illustration is decorative (`alt=""`, hidden from assistive technology). All existing localized text, form endpoints, antiforgery fields, return URLs, password autocomplete, passkey support and submit protection remain intact. Recovery and MFA pages retain their existing presentation. No backend logic changes.

## Illustration provenance

Current asset: `src/Platform/Vixly.Platform.Web/wwwroot/img/control-login-security.webp` (960Ã—720, optimized WebP). Built-in imagegen, generation mode, resized and encoded with Pillow. The earlier workstation sketch below is retained as an unused prior asset.

Current final prompt:

Use case: stylized-concept. Asset type: editorial SECURITY illustration beside an enterprise staff login form. Primary request: a tasteful pencil-and-ink security sketch that immediately communicates protected digital access and multi-factor authentication, not a generic office workstation. Subject: an elegant three-quarter view of a laptop with a simple browser login window showing an account avatar, two input fields drawn as abstract lines and masked password dots; a prominent sculptural blue shield bearing a small keyhole stands in front of the laptop at one side, and a smartphone with an authenticator symbol and six abstract dots stands at the other side. Thin blue verification paths join the phone, shield and browser with a small checkmark at a connection point. Restrained composition with only these objects, no clutter. Style: premium architectural pencil and ink editorial drawing, delicate slate hatching and restrained watercolor washes on pale warm ivory paper. Palette: medium navy blue, cobalt, pale blue and subtle slate. Landscape 4:3, generous pale-paper margin, readable at 420px wide, sober and refined. No legible text, letters, numbers, logos, watermarks, office buildings, warehouse, parcels, delivery scene, desk lamp, plants, futuristic holograms, cyberpunk glow, cartoon eyes, stock photo or oversized floating UI cards.

Asset: `src/Platform/Vixly.Platform.Web/wwwroot/img/control-login-workspace.webp` (960Ãƒâ€”720, optimized WebP). Generated with the built-in imagegen tool in generation mode; resized and encoded with Pillow.

Final prompt:

Use case: stylized-concept. Asset type: illustration beside a compact staff login form for Vixly Control, an enterprise logistics SaaS platform administration console. Scene: elegant quiet platform administrator workstation, no people, open laptop angled three-quarter view with a refined abstract admin dashboard, rows of tenant accounts, billing summaries and small charts represented by simple lines with no readable text. Beside it a slim notebook and small brass desk lamp. In the background delicate architectural line drawing of connected office buildings suggesting multiple businesses managed from one platform, not a warehouse or delivery scene. Premium editorial pencil and ink sketch with restrained watercolor washes on warm ivory paper. Palette predominantly deep navy blue, slate linework, a little cobalt and muted warm brass. Landscape 4:3 composition, calm and professional, plenty of pale paper around the edges, no text, letters, logos, watermark, giant padlock, courier, parcel handoff, trucks, futuristic holograms or stock-photo appearance.

## Verification

Control web build passed with zero warnings and zero errors. The initial compact-layout revision passed all 42 existing Control web tests, including HTTP antiforgery, security-header and rate-limit coverage; these were not rerun for the artwork/palette revision. Actual HTTPS browser preview checked at 1280Ã—720 and 375Ã—812: all four login controls are 44px high, the desktop panel fits without scrolling, and mobile has no horizontal overflow in English, French or Spanish. Password recovery retains its original single-panel layout. Keyboard navigation was checked and explicit focus rings added to the login controls and links. No credentials were submitted. The temporary preview disables database startup work and uses an ephemeral key provider.
