# ADR-0002: Resolve open decisions O-1, O-2 and O-5

- Status: **Accepted** (owner decision, 2026-10-06)
- Date: 2026-10-06
- Deciders: Jude (owner)
- Affects: Global Standard sections 4.1, 5.2, 13; Platform Infrastructure design (Stage 1)

## Context

The Global Standard lists open decisions with defaults applied until resolved. Three of them gate or shape Stage 1 (Control Plane).

## Decision

- **O-1 Operations prefix.** Entities owned by the Operations plane carry the prefix `Operations` (for example `OperationsInvoice`, table `OperationsInvoices`), extending the Platform / Customer rule to the third plane. Confirmed as written in 4.1.
- **O-2 Existing data.** V4 starts with a **fresh schema**. Table and entity names follow 4.1 (platform-prefixed, plural tables). Existing Vixly data is brought across by a separate, one-off migration tool that maps legacy rows into the V4 schema; the V4 domain model is never shaped to fit legacy tables. The migration tool lives under `build/` (or its own tool project) and is not referenced by any plane project.
- **O-5 Hardening.** Confirmed as written in 5.2: a typed authorization context for every plane, a null-check on every constructor dependency, `{ get; init; }` on every Request.

## Consequences

- Stage 1 may design the Platform Infrastructure (EF Core model, migrations) from scratch.
- A data-migration workstream is added to the build plan. It is scheduled after the Platform and Operations schemas stabilize and is a gate before any production cut-over.
- Global Standard section 13 marks O-1, O-2 and O-5 resolved by this ADR. O-3 and O-4 remain open.
