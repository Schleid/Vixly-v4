# Vixly.Platform.Domain

Control-plane entities, value objects and domain rules: tenants, subscriptions, plans, entitlements, platform billing. Every entity is prefixed `Platform` (e.g. `PlatformInvoice`).

| | |
|---|---|
| Owner | Control team |
| Build stage | 1 |
| May reference | Vixly.Shared.Kernel |
| Must not reference | Any other project; EF Core; ASP.NET Core. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Stage 1 is open. Built so far: Slice 0 (see [SLICE-0-FOUNDATION.md](../../../docs/platform/SLICE-0-FOUNDATION.md)). Next: Slice 1, Identity and access.
