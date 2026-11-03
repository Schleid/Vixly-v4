# Company website fonts

The curated fonts a company may choose for its website (website themes, step 1). Served locally; pages never contact a font CDN (CSP `font-src 'self'`). The default font stays Inter from Shared.UI.

| File | Font | Source | Weights | License |
|---|---|---|---|---|
| manrope.woff2 | Manrope | npm `@fontsource-variable/manrope` 5.3.0, `files/manrope-latin-wght-normal.woff2` | 200–800 | SIL OFL 1.1, see manrope-OFL.txt |
| plus-jakarta-sans.woff2 | Plus Jakarta Sans | npm `@fontsource-variable/plus-jakarta-sans` 5.3.0, `files/plus-jakarta-sans-latin-wght-normal.woff2` | 200–800 | SIL OFL 1.1, see plus-jakarta-sans-OFL.txt |
| dm-sans.woff2 | DM Sans | npm `@fontsource-variable/dm-sans` 5.3.0, `files/dm-sans-latin-wght-normal.woff2` | 100–900 | SIL OFL 1.1, see dm-sans-OFL.txt |
| lora.woff2 | Lora | npm `@fontsource-variable/lora` 5.3.0, `files/lora-latin-wght-normal.woff2` | 400–700 | SIL OFL 1.1 (Reserved Font Name "Lora"), see lora-OFL.txt |

Downloaded 2026-10-29. Files kept without modification and renamed only; the licence files are the packages' LICENSE files. Latin subset: covers English, French, Spanish and Haitian Creole. Normal style only, `font-display: swap`; the browser downloads a font only when the page uses it.

To add a font: an OFL (or similar) variable font, latin subset, a row here, its licence file, an `@font-face` in `css/tenant-site.css`, a value in `OperationsSiteFont` and its texts EN/FR/ES.
