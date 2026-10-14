# Vixly.Operations.Api

Internal API that serves customer-scoped data to the Customer plane. Implements `Vixly.Contracts.Operations`. Service-to-service auth only; never routed publicly by Edge.

| | |
|---|---|
| Owner | Operations team |
| Build stage | 2 |
| May reference | Application, Infrastructure (composition root only), Vixly.Contracts.Operations |
| Must not reference | Domain types in responses; browser-facing auth. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Do not add code here until this project's stage opens.
