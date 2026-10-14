# Vixly.Contracts.Platform

Wire contracts of the Platform API (tenant resolution, onboarding, plans, entitlements) for Edge, Operations and Public. Types prefixed `Platform` (e.g. `PlatformTenantDto`). Wire enums carry explicit numeric values.

| | |
|---|---|
| Owner | Control team |
| Build stage | 1 |
| May reference | BCL only |
| Must not reference | Any other project; logic of any kind. |

Additive changes only; breaking change = new version + ADR.

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Stage 1: this project is created empty in Slice 0. Contracts are added by the slice that needs them (see [PLATFORM-PLANE.md](../../../docs/platform/PLATFORM-PLANE.md)).
