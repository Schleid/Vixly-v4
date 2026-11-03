# Vixly.Public.Web

Public site (Stage 2): home, features, plans, contact, legal, the pay and save-card pages that Platform emails link to, and self sign-up (built, switched off until Stage 3). Static server rendering; talks to Platform only through `Vixly.Contracts.Platform` over HTTP.

| | |
|---|---|
| Owner | Control team; markup restyled by Frontend/UI |
| Build stage | 2 |
| May reference | Vixly.Contracts.Platform, Vixly.Shared.UI, Vixly.Shared.Hosting, Vixly.Shared.Localization (ADR-0015), Vixly.Shared.Help (ADR-0023) |
| Must not reference | Any Domain / Application / Infrastructure project |
| Design | [docs/public/STAGE-2-PUBLIC-DESIGN.md](../../../docs/public/STAGE-2-PUBLIC-DESIGN.md) |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md).

In-app help (ADR-0023) lives at `/en/help`, `/fr/help` and `/es/help`.
Embedded Markdown belongs to `Help/<lang>/<section>/`; labels remain in PublicText resources.
The checked catalogue is loaded at startup. Public help has no reader permissions or
conditions and remains anonymous when general sign-up is closed. See the H1 verification
record in `docs/help/HELP-1-VERIFICATION.md` for content and review status.

Version 2 adds localized annotated page captures under `wwwroot/help/media/v2/` (ADR-0023 Amendment 1). The isolated `build/HelpCapture/` tool renders actual Public components with fictional API responses and captures web-page content only. The original text-only files are preserved under `docs/help/archive/public-v1/`. See `docs/help/HELP-1-V2-VERIFICATION.md` for capture provenance and verification.
