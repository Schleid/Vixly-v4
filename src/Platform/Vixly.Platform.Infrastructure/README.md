# Vixly.Platform.Infrastructure

EF Core `PlatformDbContext` and migrations in the `platform` schema (D-1), repositories, Data Protection key store (in the `platform` schema), provider adapters.

| | |
|---|---|
| Owner | Control team |
| Build stage | 1 |
| May reference | Vixly.Platform.Application, Vixly.Platform.Domain, Vixly.Shared.Kernel, Contracts, Vixly.Platform.Resources, Vixly.Shared.Localization |
| Must not reference | Api, Web, any other plane. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Connection string: `ConnectionStrings:Platform`. Classes are prefixed (`PlatformInvoice`); tables are not (`platform.Invoices`).
