# Stage 2: Public site - design

| | |
|---|---|
| Plane | Public (Stage 2) |
| Date | 2026-10-16 |
| Sweep | [STAGE-2-PUBLIC-SWEEP.md](STAGE-2-PUBLIC-SWEEP.md) |
| Owner decisions | 2026-10-16, section 9 |
| Status | **Design agreed; building** |

## 1. Outcome

- `vixly.techcrafters.ltd` (later `vixly.ltd`) is served by V4 `Vixly.Public.Web`: home, features, plans, contact, legal, and the pay and save-card pages that Platform's invoice emails link to. Everything is in English, French and Spanish.
- Pages arrive as full HTML on the first request (static server rendering). No live connection is held per visitor.
- A contact request reaches Vixly: Platform stores it, emails sales, and staff see it in the console.
- **Self sign-up is built but switched off** (`PublicSite:RegistrationOpen = false`) until Operations can take the new owner (Stage 3). While it is off, "Start" leads to contact sales, and registration, verification and offer links show a "sign-up opens soon" page.

## 2. Pages

| Route | Page | Data |
|---|---|---|
| `/` | Redirect to `/{language}/` (cookie, then `Accept-Language`, then `en`) | none |
| `/{lang}/` | Home: what Vixly does, modules, who it serves, why Vixly, call to action (V3 Home, Platform, Industries and About folded in) | text only |
| `/{lang}/features` | Features by area: shipments, parcels and scanning, dispatch, warehouses, invoices and payments, customer portal, multi-branch and tenant isolation | text only |
| `/{lang}/plans` | Plan catalogue: monthly and annual prices, limits, features, trial days | `GET /registration/plans` (cached 5 minutes) |
| `/{lang}/contact` | Contact sales: company, name, work email, phone, operation type, message, bot check | `POST /leads` (new) |
| `/{lang}/legal` | Current Terms and the documents they include, with PDF links | `GET /legal/terms` |
| `/{lang}/register/...` | Sign-up (off for now): company and owner, address check, portal address check, Terms, check your email, choose a plan, pay | Slice 2 to 5 registration calls |
| `/register/verify`, `/register/payment` | Links from emails (no language in the link) | registration calls |
| `/pay/{token}`, `/cards/{token}` | Pay an invoice (card or Zelle) or save a card, from Platform emails | Slice 5 and Zelle calls |
| `/legal/documents/{id}/{lang}` | PDF passed through from Platform.Api (same origin, cached) | `GET /legal/documents/...` |
| `/robots.txt`, `/sitemap.xml` | Search engines; sign-up, pay and card pages are `noindex` | none |
| `/health/live`, `/health/ready` | Health (ready checks that Platform.Api answers) | none |

The V3 Track page is dropped (a mock; tracking belongs to the tenant's Customer portal). Maintenance notices wait for Edge (owner decision, Slice 8).

## 3. Rendering and forms (D-U1)

- Razor components with **static server rendering** and enhanced navigation. There is no interactive render mode, so there is no SignalR circuit.
- Forms are plain posts with antiforgery tokens. The server calls Platform.Api and answers with the next page (post, redirect, get).
- Script is limited to two things: the Cloudflare Turnstile widget on the contact and start forms, and an optional live portal-address check that calls a same-origin endpoint. Every form works without script except the bot check.
- Output caching: home, features and legal for 10 minutes per language; plans for 5 minutes. Pages with forms or tokens are never cached.

## 4. Language (D-U5, ADR-0005)

- Every content page lives under `/en`, `/fr` or `/es`, with `hreflang` alternates and a canonical link.
- A language switcher keeps the current page.
- The choice is kept in a `__Host-vx4-culture` cookie (1 year; not sensitive).
- Links from emails (`/pay/...`, `/register/verify`) carry no language: the page uses the cookie, then `Accept-Language`, then `en`.
- Calls to Platform.Api send `Accept-Language`, so Platform's texts (errors, plan names) come back in the page language.

## 5. Talking to Platform.Api

- A typed client is configured with `PublicSite:PlatformApiBaseUrl`.
  - Timeout: 20 seconds per call.
  - There are no retries on posts. A failure shows a localized error with the correlation id (S-9).
- **The visitor's IP.** Platform's public limits (for example, 5 sign-ups per hour) are per client IP. Seen from Platform, every Public.Web call comes from one server. So Public.Web signs a short service token (Slice 9 scheme, caller `public`, scope `public`) on every call, carrying the visitor's IP. It sends this in an `X-Vixly-Caller` header; `Authorization` stays free for the registration access token. Platform.Api uses that IP for limits, the bot check and audit, but only when the token is valid. Without the token, Platform falls back to the connection IP as today.
- **The registration access token** is kept in `__Host-vx4-registration`: HttpOnly, Secure, SameSite=Lax, path `/`, 7 days. It is only sent server to server as a bearer token (Slice 2).
- **Pay and save-card tokens** travel in the URL path, as Platform emails them. Public.Web never logs request paths or query strings at Information level.

## 6. Contact requests (D-U3, Platform addition)

- **Platform:** a new `PlatformSalesLead` record:
  - fields: company, contact name, email, phone, operation type, message, language, source page (the record also takes an optional country; the form does not ask for it);
  - stored as written in the visitor's language;
  - status New, Contacted or Closed, with a staff note.
- **`POST /leads`** (anonymous, Platform.Api):
  - Turnstile token checked by Platform;
  - 5 per hour per visitor IP;
  - message up to 2,000 characters;
  - answers the same whether or not the email is known;
  - through the outbox, emails every staff member who holds `platform.leads.manage` (once each), in that staff member's language, with the lead's language noted;
  - no email to the visitor, so the form cannot be used to send mail to others.
- **Console:** a Leads list (new, contacted, closed; search) and a lead page (details, mark contacted or closed with a note).
  - Permissions: `platform.leads.view` and `platform.leads.manage` (granted to the Administrator, Salesperson and Sales manager system roles).
  - Audited.
- Leads are deleted 24 months (730 days) after they are closed (privacy). The Platform Worker clock does this.

## 7. Hosting and security

- The project references `Contracts.Platform`, `Shared.UI`, `Shared.Hosting` and `Shared.Localization` (ADR-0015 amends Global Standard section 3).
- Its text lives in `Vixly.Public.Web/Resources/PublicText.{en,fr,es}.resx`, checked by the same architecture rules as Platform's.
- Shared.Hosting supplies security headers, health and forwarded IP (Cloudflare).
  - CSP allows Turnstile only (`challenges.cloudflare.com` script and frame), plus the checkout redirect.
- Rate limits on Public.Web's own posts are per visitor IP: contact 5 per hour, sign-up start 5 per hour, other sign-up posts 30 per hour, pay page posts 20 per hour. A refused post gets a short localized plain-text answer (429). A post without its antiforgery token is refused (400) before it is counted and never reaches Platform.
- `AllowedHosts` is restricted outside Development.
- No database. The Data Protection key ring protects only antiforgery tokens and is kept in the folder named by `PublicSite:KeysPath`, outside the web root (ADR-0015 amends S-5). A warning is logged at start when it is not set.

## 8. Look (D-U6)

Pages use Shared.UI primitives and `--vx-*` tokens (ADR-0013), with a public layout (header, language switcher, footer). Frontend/UI restyles in parallel; markup carries no literal text (UI-10).

## 9. Owner decisions (2026-10-16)

| # | Decision |
|---|---|
| D-U1 | Static rendering |
| D-U2 | Lean marketing set: home, features, plans, contact, legal; Track dropped; Platform, Industries and About folded in |
| D-U3 | Contact form sent to Vixly (Platform stores leads, emails sales, console list) |
| D-U4 | **Hold sign-up until Stage 3.** The site goes live with marketing, plans, contact, legal and the pay pages. The sign-up pages are built and tested now but switched off. Exception (2026-10-16): a company invited with a Sales offer link can sign up; Platform enforces it (`PlatformTenants:RegistrationInviteOnly`) |
| D-U5, D-U6 | As proposed (language in the URL; Control builds, Frontend/UI restyles) |
