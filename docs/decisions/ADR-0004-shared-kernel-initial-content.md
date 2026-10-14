# ADR-0004: Initial content of Vixly.Shared.Kernel

- Status: **Accepted** (owner approved Stage 1 start, 2026-10-06; content reviewed with Slice 0)
- Date: 2026-10-06
- Deciders: Jude (owner)
- Affects: Global Standard 3 (rule 6), `Vixly.Shared.Kernel`, every plane's Domain and Application layers

## Context

Rule 6 says `Shared.Kernel` holds no entities and no plane vocabulary and grows only by ADR. Slice 0 needs a small base that all three planes will otherwise duplicate, as the legacy Control and Operations planes did (each had its own `AuditableEntity`, `DomainException` and value objects).

## Decision

The Kernel starts with these types and nothing else (amended the same day by ADR-0005 to add `Error`, `KernelErrors` and `LanguageCode`):

| Type | Why it is shared |
|---|---|
| `DomainException` | One exception for invariant violations. It carries an `Error`, never text, and is translated by use cases into a failure response |
| `Error`, `KernelErrors` | A language-neutral failure: a stable code plus named, typed arguments (ADR-0005). The Kernel's own error codes start `shared.` |
| `LanguageCode` | The supported languages (en, fr, es) as a value object; English is the default and the fallback (ADR-0005) |
| `AuditableEntity` | Identity and audit timestamps. Time is passed in (from `TimeProvider`), never read from the clock, so behavior is testable. Ids are version 7 GUIDs derived from the creation time, so clustered indexes grow append-only (P-6) |
| `EmailAddress`, `PhoneNumber` | Value objects with identical rules in every plane. Immutable records; `Create` throws `DomainException`, `TryCreate` returns false |
| `PagedRequest`, `PagedResult<T>` | One default page size (25) and one hard cap (100) for every list read (P-1). Out-of-range input is clamped, never rejected |

Deliberately not in the Kernel:

- `TenantOwnedEntity`. "Tenant" is plane vocabulary. Platform owns tenants; Operations and Customer define their own tenant-scoped base when their stage opens, and may propose a shared one by ADR if the two are identical.
- Authorization contexts, gates, permission keys. Each plane has its own typed context (O-5).
- Any enum, constant or error code that names a business concept.

Changes from the legacy equivalents:

- Domain exceptions and responses carry error codes, not English messages (ADR-0005). Legacy `DomainException(string)` is gone.
- Timestamps are `DateTimeOffset` in UTC and are supplied by the caller. Legacy used `DateTime.UtcNow` inside constructors.
- Value objects have no `RestoreFromPersistence` back door. Legacy data is cleaned by the migration tool (ADR-0002) before it enters V4.
- Ids are v7 GUIDs, not random v4 GUIDs.

## Consequences

- Planes reference `Vixly.Shared.Kernel` for these types and may not re-declare them.
- Adding any further type to the Kernel needs a new ADR.
- Persistence mappings (Infrastructure, per slice) must map `Id`, `CreatedUtc` and `LastModifiedUtc` and use `datetimeoffset` columns.
