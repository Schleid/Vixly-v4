# Vixly.Customer.Infrastructure

Operations internal-API client (implements `IOperationsGateway`) and the Customer process's own Data Protection key ring (shared database).

| | |
|---|---|
| Owner | Customer team |
| Build stage | 3 |
| May reference | Application, Domain, Vixly.Contracts.Operations |
| Must not reference | Operations database or `DbContext`; Web. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Do not add code here until this project's stage opens.
