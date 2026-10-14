# Vixly.Customer.Web

Customer portal (Blazor Server), its own process behind Edge at `{tenant-host}/customer/**`. One cookie scheme: customer, `Path=/customer`.

| | |
|---|---|
| Owner | Customer team |
| Build stage | 3 |
| May reference | Application, Infrastructure (composition root only), Vixly.Shared.UI |
| Must not reference | Operations or Platform internals; staff routes. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Do not add code here until this project's stage opens.
