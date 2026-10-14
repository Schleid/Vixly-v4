# Vixly.Shared.Kernel

Tiny plane-agnostic building blocks (guards, clock, tenant-context abstraction, small result helpers). No entities, no plane vocabulary.

| | |
|---|---|
| Owner | Architecture (changes by ADR) |
| Build stage | 1 |
| May reference | BCL only |
| Must not reference | Any plane project; entity types; persistence. |

Grows only by ADR. If it needs a plane word in its name, it does not belong here.

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Stage 1 (Slice 0) is built: see [ADR-0004](../../../docs/decisions/ADR-0004-shared-kernel-initial-content.md). Adding any type here needs a new ADR.
