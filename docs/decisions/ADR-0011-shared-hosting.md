# ADR-0011: Vixly.Shared.Hosting for cross-cutting host setup

- Status: **Accepted** (owner decision, 2026-10-13)
- Date: 2026-10-13
- Deciders: Jude (owner)
- Affects: Global Standard sections 2, 3 (new project kind); every Api, Web and Worker host

## Context

S-10 (security headers), S-12 (health endpoints, structured logs, OpenTelemetry) and the Cloudflare forwarded-header rule (ADR-0006) must be implemented once and used by every host. In V3 the API had a headers middleware and the Web host had none. The repository layout has no place for hosting code: `Shared.Kernel` is BCL-only and `Shared.UI` is components-only.

## Decision

- Add `src/Shared/Vixly.Shared.Hosting`, an ASP.NET Core class library that holds:
  - `UseVixlySecurityHeaders()`: CSP, HSTS, `X-Content-Type-Options`, frame protection and Referrer-Policy. The Blazor relaxations are listed in one place as tracked exceptions.
  - `AddVixlyForwardedHeaders()`: trusts `CF-Connecting-IP` and `X-Forwarded-Proto` only from configured Cloudflare ranges.
  - `MapVixlyHealth()`: `/health/live` and `/health/ready`.
  - `AddVixlyObservability()`: structured logging and OpenTelemetry by configuration.
- It may reference ASP.NET Core and `Shared.Kernel` only. No plane project, no Contracts.
- Only `*.Api`, `*.Web`, `*.Worker`, `Vixly.Edge` and `Vixly.Public.Web` may reference it, and only from their composition root.
- The architecture tests gain this project kind.

## Consequences

- Security headers are applied the same way on every host and tested once.
- Global Standard section 2 lists `Shared.Hosting`; the dependency table in section 3 gains its row.
