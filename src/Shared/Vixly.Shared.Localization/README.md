# Vixly.Shared.Localization

Culture and request-localization setup, the error-to-text localizer, the plural helper (CLDR categories for en, fr, es) and the `en-XA` pseudo-locale. Holds the `shared.*` error texts.

`VixlyEmail` (2026-10-25) turns a plain-text email into the designed HTML version every plane sends with it: a header with the sender's name and logo, the main link as a button with the link to copy below, and a footer. Each plane's mail sender chooses the brand (Platform: Vixly; Operations: the company, never Vixly) and its own texts (`email.layout.*`, and `email.<x>.button` for each email's button).

| | |
|---|---|
| Owner | Architecture / Frontend/UI |
| Build stage | 1 (first created in Slice 1) |
| May reference | Vixly.Shared.Kernel, ASP.NET Core localization |
| Must not reference | Any plane project. Never referenced by Domain or Application. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md) (section 5.5) and [ADR-0005](../../../docs/decisions/ADR-0005-multilingual-system.md). Not created as a project until its slice opens.
