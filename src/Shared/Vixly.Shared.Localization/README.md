# Vixly.Shared.Localization

Culture and request-localization setup, the error-to-text localizer, the plural helper (CLDR categories for en, fr, es) and the `en-XA` pseudo-locale. Holds the `shared.*` error texts.

| | |
|---|---|
| Owner | Architecture / Frontend/UI |
| Build stage | 1 (first created in Slice 1) |
| May reference | Vixly.Shared.Kernel, ASP.NET Core localization |
| Must not reference | Any plane project. Never referenced by Domain or Application. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md) (section 5.5) and [ADR-0005](../../../docs/decisions/ADR-0005-multilingual-system.md). Not created as a project until its slice opens.
