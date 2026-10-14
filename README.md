# Vixly V4

Multi-tenant SaaS logistics platform, rebuilt from scratch. Four teams (Control, Operations, Customer, Frontend/UI) build in parallel inside one repository, under one standard.

**Start here: [`docs/global/VIXLY-GLOBAL-STANDARD.md`](docs/global/VIXLY-GLOBAL-STANDARD.md).** It is the single document every team refers to: dependency rules, naming (every entity carries its platform), file and code organization, security and performance baselines, definition of done, and the build order.

## Map

| Folder | Holds |
|---|---|
| `docs/` | Global standard, ADRs, reference material, per-plane documents |
| `src/Platform` | Control plane (`Vixly.Platform.*`) |
| `src/Operations` | Operations plane (`Vixly.Operations.*`), including the internal API |
| `src/Customer` | Customer plane (`Vixly.Customer.*`), served at `{tenant-host}/customer` |
| `src/Public` | Public site |
| `src/Edge` | Reverse proxy: tenant resolution and routing |
| `src/Contracts` | The only way planes talk to each other |
| `src/Shared` | Kernel, shared UI library and gallery |
| `src/Hybrid` | Reserved for MAUI Blazor Hybrid (V2+) |
| `tests/` | One test project per source project, plus architecture tests |
| `build/` | CI and scripts |

## Build order

Planes are built and verified one at a time: Platform, Public, Operations, Customer, Edge. Platform and Public go live first. Shared UI runs in parallel. See section 12 of the global standard (v1.3).

V4 is independent of V3: separate databases, hostnames on `techcrafters.ltd` until tenants migrate, no calls either way (Global Standard section 9, ADR-0010).

## Status

| Stage | State |
|---|---|
| 1 Platform | Slice 0 (Foundation) built and reworked for the multilingual rule (ADR-0005); v1.3 addendum (schema, connection string, D-5, gate test, security headers) in progress; then owner verification. Next: Slice 1, Identity and access. See [`docs/platform/`](docs/platform/PLATFORM-PLANE.md) |
| 2 to 5 | Not started |

Build and test: `dotnet test Vixly.Platform.slnx` (see [`docs/platform/SLICE-0-FOUNDATION.md`](docs/platform/SLICE-0-FOUNDATION.md) for what has and has not been verified yet).
