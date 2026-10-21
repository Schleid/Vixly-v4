# Stage 2: Public site - verification record

| | |
|---|---|
| Plane | Public, with Platform additions (Stage 2) |
| Date | 2026-10-16 |
| Built on | Platform Slices 0 to 9 as committed by the dev team (`3f5ae26`); not committed here: the dev team owns git |
| Design | [Design note](STAGE-2-PUBLIC-DESIGN.md), [sweep](STAGE-2-PUBLIC-SWEEP.md), [ADR-0015](../decisions/ADR-0015-public-site-references-keys-and-caller.md) |
| Status | **Built and verified; waiting for owner sign-off.** Sign-up is built and switched off (D-U4) |

## 1. What was built

- **`Vixly.Public.Web`** (new, `Vixly.Public.slnx`): static server rendering with no Blazor script (D-U1). Pages: home, features, plans, contact, legal (D-U2) in `/en`, `/fr` and `/es` (D-U5); the pay and save-card pages that Platform emails link to; the sign-up steps behind `PublicSite:RegistrationOpen` (off in production, on in Development); not-found and error pages; `robots.txt` and `sitemap.xml` with language alternates. 270 text keys in `PublicText` (en, fr, es); marketing copy follows today's vixly.ltd.
- **Platform, contact requests (D-U3):** `PlatformSalesLead` (table `platform.SalesLeads`, migration `Leads`), `POST /leads` (anonymous, bot check, 5 per hour per visitor), console Leads list and lead page, permissions `platform.leads.view` and `platform.leads.manage` (Administrator, Salesperson, Sales manager), two audit actions, an outbox email to every holder of `platform.leads.manage` in their language, and deletion 730 days after closing (worker clock).
- **Platform, visitor address:** Platform.Api accepts an `X-Vixly-Caller` token from Public.Web (caller `public`, scope `public`) and uses its `ip` for limits, the bot check and audit on public routes (ADR-0015).
- **Shared.Hosting:** `UseVixlySecurityHeaders` takes a site-specific CSP (Public allows Turnstile and the card checkout only).
- **Rules and documents:** architecture tests cover Public (references, `PublicText`, markup keys, page and markup budgets); ADR-0015; Global Standard section 1, section 3, S-5 and S-7 amended; the global SQL script now has 17 migrations.

## 2. Automated checks

Release builds of `Vixly.Platform.slnx` and `Vixly.Public.slnx`: no errors, no warnings.

| Test project | Before (Slice 9) | After |
|---|---|---|
| Platform.Domain.Tests | 286 | 297 |
| Platform.Application.Tests | 216 | 224 |
| Platform.Infrastructure.Tests (with SQL Server) | 56 | 59 |
| Platform.Api.Tests | 14 | 17 |
| Platform.Web.Tests | 31 | 32 |
| Public.Web.Tests (new) | - | 21 |
| Architecture.Tests | 6,094 | 6,379 |
| Shared.Kernel.Tests | 81 | 81 |

All pass.

What the new tests cover:
- **Lead rules:** trimmed, bounded values; one code per bad input (company, name, email, phone, country, kind, message length, control characters); status changes record the staff member; a repeat is no change.
- **Lead use cases:** a failed bot check stores nothing; the visitor address, audit entry and one outbox message on success; `leads.view` to list and `leads.manage` to change; the email goes once to each `leads.manage` holder and to no one else; only closed leads past 730 days are deleted.
- **Database:** a lead round-trips; the list filters by status and search, newest first, and names who handled it; retention deletes only closed leads past the cut-off.
- **Visitor address (Platform.Api):** limits follow the address in a valid public token; a wrong-scope, forged or broken token counts against the connection address.
- **Public site (in memory, real pipeline, stand-in Platform):** the root sends `fr`, `es-MX` and `de` visitors to `/fr/`, `/es/` and `/en/`; pages render in their language with no Blazor script; the CSP allows only Turnstile and the checkout; every cookie is a Secure, HttpOnly `__Host-` cookie; Platform down still renders with the unavailable text; sign-up while closed shows "opens soon" and calls nothing; the contact post reaches Platform with a signed token whose signature and `ip` check out; a post without the antiforgery token is refused and never reaches Platform; the sixth contact post in an hour gets a short localized 429; pay links show the invoice or "paid"; robots and sitemap; unknown pages answer 404.

## 3. End-to-end run (fresh database, API, console and public site)

Fresh `VixlyV4` from the global script; one French-speaking SuperUser; the console's Development demo data (plans, subscriptions). Platform.Api ran with the `public` caller key made for the run; Public.Web ran over HTTPS on a different address from the API, with its own key folder.

| # | Check | Result |
|---|---|---|
| 1 | `/` with `Accept-Language: es` | 302 to `/es/` |
| 2 | Home, features, plans, legal, contact, sign-up, robots, sitemap, health | All 200; unknown page 404 |
| 3 | `/fr/plans` | Platform's three plans, names in French, prices as `12,00 USD par licence et par mois` |
| 4 | `/en/legal` | Terms 2026-10, included documents, download link |
| 5 | Pay link from an email (`/pay/x?lang=fr`) | Language set by cookie, redirected without `lang`; Platform's refusal shown in French with a reference |
| 6 | Contact form (fr) | 302 to the thank-you; lead stored with the **visitor** address (Platform saw only the site's address); audit `platform.leads.received` with the visitor address; outbox processed; the SuperUser got the email in French with the console link |
| 7 | Sign-up (Development, sign-up on) | Start, email link, confirm, plan (a too-low licence count refused with Platform's text), done; consent address is the visitor's; tenant created |
| 8 | Headers | CSP, `X-Frame-Options: DENY`, Secure `__Host-` cookies |
| 9 | Logs | No warnings or failures in the three hosts |

Found and fixed during the run: the antiforgery cookie lacked `Secure`, so browsers would drop the `__Host-` cookie and every form would fail (now always Secure, with a test); the public host had no clock service registered (found by the first test run); a rate-limited post showed the not-found page (now a short localized text); the sign-up link deadline now says UTC.

## 4. Not covered here

- Turnstile with real keys and card checkout with real Stripe keys: deployment configuration; both are covered by the stand-in tests and earlier Platform slices.
- The paid sign-up path to the pay step: built and tested with a stand-in; it opens with sign-up in Stage 3 (D-U4).
- The console Leads screens with a signed-in user: the route is protected (test) and the presenters follow the Slice patterns; the owner checks them in the console.
- Frontend/UI restyling of the public pages (D-U6): markup uses Shared.UI primitives and `--vx-*` tokens only.

## 5. Deployment notes

- Public.Web settings: `PublicSite:PlatformApiBaseUrl`, `CanonicalBaseUrl`, `TurnstileSiteKey`, `CallerKeyId`, `CallerPrivateKey` (secret), `CheckoutOrigins`, `KeysPath` (writable folder outside the site root), `SalesEmail`; `RegistrationOpen` stays `false` until Stage 3.
- Platform.Api: add the matching public key under `PlatformInternal:Callers:public:Keys` with scope `public`.
- Run the newest global SQL script (adds `platform.SalesLeads`).
