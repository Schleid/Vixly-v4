# ADR-0003: Platform plane scope, slice order and tenant staff accounts

- Status: **Accepted** (owner decision, 2026-10-06)
- Date: 2026-10-06
- Deciders: Jude (owner)
- Affects: Platform plane document (`docs/platform/PLATFORM-PLANE.md`), Operations plane scope, `Contracts.Platform`

## Context

The Platform plane document proposed three open decisions (O-6, O-7, O-8) that shape Stage 1.

## Decision

- **O-6** Operations owns tenant staff accounts (`TenantUser`, `TenantUserAccount`, their tokens) and the tenant staff login. Platform keeps only the tenant directory and entitlements, and exposes tenant status, plan, features and limits through `Contracts.Platform`. Platform holds no per-tenant operational identities. The Operations entities take the `Operations` prefix (ADR-0002 O-1), for example `OperationsUser`, `OperationsUserAccount`.
- **O-7** Slice order after the Foundation slice: Identity and access first, then Tenants, Subscriptions, Billing, and so on, as in section 3 of the plane document.
- **O-8** The V4 entity names in section 4 of the plane document are accepted as written.

## Consequences

- `Contracts.Platform` must carry the tenant-status and entitlement queries Operations needs to authenticate a staff login for a given tenant (tenant active, plan limits, device license). The shape is agreed with the Operations team before slice 2 finishes (C-2).
- The legacy `LoginTenantUser` behavior moves to Operations scope and leaves the Platform use-case catalogue.
- Slice 2 (Tenants) no longer builds tenant-user entities; the first-device setup flow keeps only its Platform-side licensing part.
