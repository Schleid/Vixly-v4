# ADR-0016: Operations UI preparation before the Stage 3 backend

- Status: **Proposed**; requested by the owner on 2026-10-16. This proposal is not approval to open Stage 3.
- Date: 2026-10-16
- Owners: Architecture, Operations and Frontend/UI
- Affects: Shared.UI, Shared.UI.Gallery, future Operations.Web

## Context

The owner requests a dense logistics operating experience: shipment lifecycle, barcode scans, warehouse queues, inventory, dispatch preparation, billing and multi-branch coordination. The current repository reserves Operations for Stage 3. Platform Control owns tenant administration and platform commercial functions; control-host shipment workflows would contradict the Global Standard.

## Proposed decision

1. Continue reusable presentation primitives and clearly labelled sample-only Gallery fixtures under existing C-4 authority. Gallery compositions demonstrate primitives; they are not production feature components.
2. Allow a separately reviewed Operations UI workstream after Architecture and Operations accept its consumer API and handoff shapes. Do not treat this ADR as automatic stage sign-off.
3. Keep live shipments, inventory, route building, barcode processing, financial calculations and tenant/branch authorization in Operations. No production workflow is implemented in Shared.UI or Platform.
4. Stage 3 adoption maps presenter/state outputs into approved primitive parameters. Branch selection comes from trusted authorized context; hiding rows or controls never grants access.
5. Default the new experience to light mode with an explicit persisted dark option. Status meaning remains textual as well as colour-coded: received blue, staged amber, transit purple, completed green, exceptions rose.
6. Require localized en/fr/es, keyboard and scanner/tablet reviews, 375/768/1280/1600 widths, empty/loading/error states, and tenant/warehouse isolation tests before production adoption.

## Consequences

Frontend/UI can validate density, hierarchy and component APIs without fabricating live data or expanding backend scope. Operational preview numbers, feeds, DIM values and dispatch selections are fixtures only. API-backed global search, connectivity telemetry, branch entitlements and dispatch submission remain producer/consumer integration work. Existing Control search and telemetry placeholders are not made operational by styling.

## Acceptance

Architecture and Operations review pending. The Global Standard and normal stage gates remain authoritative.
