# ADR-0015: Public site references, key storage and the public caller

- Status: **Accepted** (owner decisions D-U1 to D-U6, 2026-10-16)
- Date: 2026-10-16
- Deciders: Jude (owner)
- Affects: `Vixly.Public.Web`, Platform.Api; Global Standard sections 1 (plane table), 3 (project references), 6 (S-5, S-7)

## Context

Stage 2 builds the public site, `vixly.techcrafters.ltd` (later `vixly.ltd`) ([sweep](../public/STAGE-2-PUBLIC-SWEEP.md), [design](../public/STAGE-2-PUBLIC-DESIGN.md)). The owner chose static rendering (D-U1), a lean marketing set (D-U2), a contact form that Platform stores and emails to sales (D-U3), sign-up built but switched off until Stage 3 (D-U4), the language in the URL (D-U5), and Control builds while Frontend/UI restyles (D-U6). Three rules of the Global Standard did not fit:

- Section 3 lets `Vixly.Public.Web` reference only `Contracts.Platform` and `Shared.UI`. The site needs the same security headers, forwarded-header trust, health endpoints and culture list as every other V4 host. Copying them would let them drift.
- S-5 puts every Data Protection key ring in the plane's own database schema. Public has no database by design: it talks to Platform only over HTTP.
- Platform.Api limits and bot-checks public routes per visitor IP. Every call from the site comes from the site's server, so all visitors would share one limit, the bot check would see the wrong address and audit would record the server.

## Decision

1. **References (section 3).** `Vixly.Public.Web` may reference `Contracts.Platform`, `Shared.UI`, `Shared.Hosting` and `Shared.Localization`. It still never references a Domain, Application, Infrastructure or Resources project; its text lives in its own `Resources/PublicText` (en, fr, es).
2. **Key storage (S-5).** Public.Web persists its Data Protection keys to the folder named by `PublicSite:KeysPath`, outside the web root, under the application name `Vixly.V4.Public.Web`. The keys protect only antiforgery tokens; losing them costs a visitor one re-submitted form. The site logs a warning at start when the folder is not set outside Development. Every other deployable keeps S-5 as written.
3. **Public caller (S-7, extends ADR-0014).** Public.Web is a registered internal caller named `public` with the single scope `public`. On each call to a public Platform route it sends `X-Vixly-Caller`: an ES256 token (60 seconds, single use) whose only claim of interest is `ip`, the visitor address. Platform.Api, on public routes only, uses that address for rate limits, the bot check and audit. A missing, forged, expired or wrong-scope token changes nothing: the connection address is used. The token never authorizes anything.
4. **Plane scope (section 1).** The Public plane is the marketing site (home, features, plans, contact, legal), the pay and save-card pages that Platform emails link to, and self sign-up. It is always Vixly-branded and never white-labelled.

## Consequences

- `ProjectDependencyRulesTests` allows the two Shared references for Public.Web; `ResourceRulesTests` checks `PublicText`; `UiRulesTests` checks Public markup keys against `PublicText`.
- Hosting must give Public.Web a writable folder outside the site root and set `PublicSite:KeysPath`, plus `PublicSite:CallerKeyId` and `PublicSite:CallerPrivateKey`; Platform.Api lists the matching public key under `PlatformInternal:Callers:public`.
- Global Standard section 1 (Public row), section 3 (Public.Web row), S-5 and S-7 are amended to point here.
