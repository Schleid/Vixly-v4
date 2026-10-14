# Vixly.Platform.Api

HTTP surface of the Control plane (JWT, rate limiting, output caching, CORS). Implements `Vixly.Contracts.Platform` for Edge, Operations and Public.

| | |
|---|---|
| Owner | Control team |
| Build stage | 1 |
| May reference | Application, Infrastructure (composition root only), Vixly.Contracts.Platform |
| Must not reference | Domain types in public responses; any other plane's internals. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Do not add code here until this project's stage opens.
