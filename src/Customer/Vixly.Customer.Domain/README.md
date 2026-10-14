# Vixly.Customer.Domain

Customer-scoped read models and session concepts (`CustomerInvoice`, `CustomerShipment`, ...). Deliberately thin: it presents Operations data, it does not own it.

| | |
|---|---|
| Owner | Customer team |
| Build stage | 3 |
| May reference | Vixly.Shared.Kernel |
| Must not reference | Operations or Platform internals; persistence of Operations data. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Do not add code here until this project's stage opens.
