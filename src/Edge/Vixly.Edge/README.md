# Vixly.Edge

YARP reverse proxy in front of every tenant hostname: resolves hostname to tenant, routes `/customer/**` to Customer.Web and everything else to Operations.Web.

| | |
|---|---|
| Owner | Control team |
| Build stage | 4 |
| May reference | Vixly.Contracts.Platform, Vixly.Shared.Kernel |
| Must not reference | Any Domain / Application / Infrastructure project; business logic. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Do not add code here until this project's stage opens.
