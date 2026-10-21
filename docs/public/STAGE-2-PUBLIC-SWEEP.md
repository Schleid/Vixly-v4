# Stage 2: Public site - sweep

| | |
|---|---|
| Plane | Public (Stage 2) |
| Date | 2026-10-16 |
| Sources | V3 `PublicPortal/Vixly.Public.Web` (read-only); V4 `src/Public` (README only); Platform.Api public endpoints (Slices 2 to 8); Global Standard; ADR-0005, ADR-0010, ADR-0012, ADR-0013 |
| Status | **Sweep done; owner decisions needed (section 6)** |

## 1. What V3 has

V3 `Vixly.Public.Web` is Blazor Server, every page interactive (a SignalR circuit per visitor), FluentUI, English only.

| Area | V3 pages | Notes |
|---|---|---|
| Marketing | Home (430 lines), Features, Platform, Industries, About, Contact | Hard-coded English text. Contact's "demo request" opens the visitor's mail app (`mailto:sales@techcrafters.ltd`); nothing reaches Vixly unless they send it |
| Catalogue | Plans | Cached in memory, warmed at start |
| Tracking | Track | A mock preview with no backend; real tracking is tenant-bound (Customer plane) |
| Registration | Register (company, owner, review), verify email, select plan, pay, invoice, activate account (owner password), register first device, complete | 12 steps in one workspace; scoped `OnboardingState` held the progress |
| Other | Maintenance (redirect while Platform says so), Error, NotFound | Maintenance calls Platform on each request with a short cache |

Calls go to `api/public/tenants/*` on the V3 Platform API (19 calls).

## 2. Problems found in V3

1. **No content without a live circuit.** Every page is interactive Blazor Server: crawlers and slow phones get little until the circuit connects (ADD Public finding; SEO and Core Web Vitals risk). Each visitor holds server memory on shared hosting.
2. **Domain-wide cookie.** `OnboardingCookies:SharedDomain = .vixly.dyndns.ws` breaks S-4 (host-only cookies).
3. **No protection of its own.** `AllowedHosts: *`, no rate limit, no bot check, no security headers (S-6, S-8, S-10).
4. **English only**, text in markup (L-1, UI-10).
5. **Plane boundaries crossed.** Owner account activation and first-device registration happen on the public site. In V4, tenant staff accounts belong to Operations (ADR-0003) and the first device is enrolled from the tenant host (Slice 7).
6. **Progress in server memory.** A refresh or a second tab loses the step; the backend already knows the step.
7. **Mock tracking page** suggests a feature that does not exist there.
8. **Silent error swallowing** in the maintenance middleware (`catch (HttpRequestException) { }`, D-6).

## 3. What V4 Platform already offers Public.Web

All anonymous, on `vixly-api.techcrafters.ltd`, CORS for the public site, errors `{ code, message, arguments, correlationId }` in the caller's language.

| Step | Platform.Api | Built in |
|---|---|---|
| Plans catalogue | `GET /registration/plans` | Slice 3 |
| Offer link preview | `POST /registration/offer` | Slice 3 |
| Promotion check | `GET /registration/promotions/{code}` | Slice 3 |
| Current Terms and PDFs | `GET /legal/terms`, `GET /legal/documents/{id}/{language}` | Slice 8 |
| Slug check | `GET /registration/slugs/{slug}` | Slice 2 |
| Start (Turnstile token) | `POST /registration` → access token | Slice 2 |
| Verify email | `POST /registration/verify` (token from the email link `/register/verify?token=`) | Slice 2 |
| Correct email, resend | `POST /registration/email`, `/resend` | Slice 2 |
| Status (next step) | `GET /registration/status` | Slice 2 |
| Choose plan (trial or pay) | `POST /registration/plan` | Slice 3 |
| Registration invoice, card checkout, Zelle report | `GET /registration/invoice`, `POST /registration/checkout`, `/registration/zelle`; return page `/register/payment?status=` | Slices 5 and Zelle |
| Pay and save-card links from emails | `GET /payment-links/{token}`, `POST .../checkout`, `.../zelle`; pages `/pay/{token}`, `/cards/{token}` | Slice 5 and Zelle |
| Zelle QR | `GET /payments/zelle/qr` | Zelle |

Links Platform already puts in emails point to Public.Web: `/register/verify?token=`, `/register?offer=`, `/register/payment`, `/pay/{token}`, `/cards/{token}`. They carry no language, so Public.Web must pick one (ADR-0005 order).

After a trial is chosen or the first invoice is paid, the registration waits at **account setup**, which Operations owns (Stage 3). Nothing in V4 sends an account-setup link yet.

## 4. What V4 has for Public today

- `src/Public/Vixly.Public.Web`: README only (it says "Build stage 5"; the Global Standard says Stage 2).
- `tests/Public/Vixly.Public.Web.Tests`: `.gitkeep`.
- `Vixly.Shared.UI` (Frontend/UI, ADR-0013): token-styled primitives (button, card, text input, message, dialog, data table, pager, badges, empty state), presenter base, self-hosted Inter, light and dark themes; built to work with static server rendering.

## 5. Gaps against the Global Standard

| Rule | Gap |
|---|---|
| Section 3 table | `Public.Web` may reference only `Contracts.Platform` and `Shared.UI`; it also needs `Shared.Hosting` (headers, health, forwarded IP) and `Shared.Localization` (cultures, error text). Needs an ADR |
| Section 2 | "Public and Edge are single projects; each holds its own resources": text lives in `Public.Web` itself |
| L-4, ADR-0005 | Language in the URL (`/en`, `/fr`, `/es`) with `hreflang` |
| S-4 | The registration access token in a host-only `__Host-` cookie, HttpOnly, 7 days (Slice 2 design) |
| S-6 | Public.Web's own form posts need rate limits; the bot check widget must be on the start form |
| S-10 | CSP must allow the Turnstile script and frame only |
| P-5 | Output caching for read-mostly pages (home, plans, legal) |

## 6. Decisions for the owner

| # | Question | Options | Recommendation |
|---|---|---|---|
| D-U1 | How pages render | A: interactive Blazor Server everywhere (V3). B: **static server rendering** with enhanced navigation and plain form posts; a little script only for the bot check and the live slug check | **B.** Full HTML on first load (SEO, fast phones), no circuit per visitor on shared hosting, works with Shared.UI as built. Forms post to the server, which calls Platform.Api |
| D-U2 | Which pages | A: registration and payment pages only. B: **those plus a lean marketing set**: home, features, plans, contact, legal. C: port every V3 page | **B.** Drop Track (mock; tracking is tenant-bound) and fold Platform, Industries and About into Home and Features. All text in en, fr, es |
| D-U3 | Contact / demo request | A: mailto (V3). B: **form sent to Vixly**: Platform stores the lead and emails sales; staff see it in the console. C: contact details only | **B.** A lead is not lost when the visitor has no mail app; needs one anonymous Platform endpoint with the bot check and a small console list |
| D-U4 | Where self sign-up ends before Operations exists | A: **"workspace being prepared" page**; the owner gets an email to set up the account when Operations opens (Stage 3). B: Public.Web creates the owner account (V3) | **A.** Tenant accounts belong to Operations (ADR-0003); the status page shows the step and the console lets staff see who is waiting |
| D-U5 | Language | **URL prefix** `/en`, `/fr`, `/es`; unprefixed links (from emails) go to the visitor's language (cookie, then `Accept-Language`, then `en`) | As ADR-0005 |
| D-U6 | Look | Control team builds working pages on Shared.UI primitives and tokens; **Frontend/UI restyles in parallel** (as for the console) | As before |
