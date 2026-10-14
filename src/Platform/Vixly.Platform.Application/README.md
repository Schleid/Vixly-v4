# Vixly.Platform.Application

Control-plane use cases (Request / Response / UseCase), ports (interfaces), the `IPlatformAuthorizationGate`, `PlatformAuthorizationContext`.

| | |
|---|---|
| Owner | Control team |
| Build stage | 1 |
| May reference | Vixly.Platform.Domain, Vixly.Shared.Kernel |
| Must not reference | Infrastructure, Api, Web, EF Core, any other plane. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Stage 1 is open. Built so far: Slice 0, the authorization context and gate (see [SLICE-0-FOUNDATION.md](../../../docs/platform/SLICE-0-FOUNDATION.md)). Next: Slice 1, Identity and access.
