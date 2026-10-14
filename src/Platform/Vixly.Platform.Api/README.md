# Vixly.Platform.Api

HTTP surface of the Control plane (JWT, rate limiting, output caching, CORS). Implements `Vixly.Contracts.Platform` for Edge, Operations and Public.

| | |
|---|---|
| Owner | Control team |
| Build stage | 1 |
| May reference | Application, Infrastructure (composition root only), Vixly.Contracts.Platform |
| Must not reference | Domain types in public responses; any other plane's internals. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md).

**Opened in Slice 2** (see `docs/platform/SLICE-2-TENANTS-DESIGN.md`, section 6). It serves the anonymous registration endpoints that Public.Web calls:

| Endpoint | Rate limit (per client IP) |
|---|---|
| `GET /registration/slugs/{slug}` | 30 per minute |
| `POST /registration` | 5 per hour, plus the Turnstile bot check |
| `POST /registration/verify` | 10 per hour |
| `POST /registration/email`, `POST /registration/resend` | 10 per hour, and 3 links per hour per registration |
| `GET /registration/status` | 60 per hour |

The access token travels as `Authorization: Bearer <token>`. Errors are `{ code, message }`, with the message in the caller's `Accept-Language`. Request bodies are capped at 16 KB. Internal endpoints for Edge and Operations (JWT, `Contracts.Platform`) arrive in Slice 9.

Local run: `https://localhost:7402`. In Development the bot check is off; elsewhere set `PlatformBotCheck:SecretKey` with user secrets or the host, never in the repository.
