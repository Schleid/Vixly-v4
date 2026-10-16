# ADR-0014: Internal contracts, service tokens and the API error shape

- Status: **Accepted** (owner decisions D-K1 to D-K11, 2026-10-16)
- Date: 2026-10-16
- Deciders: Jude (owner)
- Affects: Platform, Operations, Edge; `Contracts.Platform`; Global Standard sections 3 (cross-plane rule 4), 4.1, 6 (S-7, S-9)

## Context

Slice 9 makes Platform reachable from Operations and Edge ([sweep](../platform/SLICE-9-CONTRACTS-SWEEP.md), [design](../platform/SLICE-9-CONTRACTS-DESIGN.md)). Three rules of the Global Standard did not fit what the owner chose:

- S-7 asks for "an internal API key header handler at minimum". A shared key cannot say which tenant user a call is for, cannot expire per call and must be copied to every caller.
- S-9 says APIs return a code, its arguments and a correlation id, "not display text". Operations and Edge show Platform's refusals to tenant users in three languages; without Platform's text each consumer would keep a copy of Platform's texts.
- Cross-plane rule 4 asks wire enums to carry explicit numeric values. Numbers tie consumers to Platform's enum order and read badly in logs and events; the owner chose string codes (D-K10).
- 4.1 names wire types `<Plane><Noun>Dto`. `Contracts.Platform` has used `...Body` for requests and `...Info` (or a plain noun) for answers since Slice 2, and Public.Web already reads them.

## Decision

1. **Service tokens (S-7).** Internal callers authenticate with short-lived ES256 tokens signed by their own private key. Platform keeps only public keys, per caller, with scopes. A token lives at most 2 minutes, is used once and names the tenant user for tenant calls (tenant, reference, name, role, second factor, browser IP and user agent). Internal routes answer only on internal hostnames, and the host's IP allow-list adds a network layer. A shared API key is no longer acceptable for Platform.
2. **Error shape (S-9).** Every Platform API error is `{ code, message, arguments, correlationId }`. `message` is Platform's own text for the code in the caller's `Accept-Language` (en, fr, es; English fallback). Consumers may show it or their own text for the code; they never parse it. Raw exception text is still never returned.
3. **Wire naming (4.1).** In `Contracts.Platform`, requests end in `Body`, answers end in `Info` or are a noun (`PlatformEventPageInfo`, `PlatformApiError`), events end in `Event`. Folders: `Common`, `Internal/<Area>`, `Events`, and the existing public folders. `PlatformApiError` moves to `Common`. Other planes keep `Dto` unless they record their own ADR.
4. **Wire codes (rule 4).** `Contracts.Platform` carries enum values as lowercase string codes (`pending_activation`, `read_only`, `lost_or_stolen`). A code never changes once published; an unknown code means "refused" to a consumer.
5. **Versioning.** The major version is in the route (`/internal/v1`) and in every event type (`.v1`). Inside a version, changes are additive only; consumers ignore unknown fields and treat unknown codes as refusals. A new major version lives beside the old one for at least 90 days and needs an ADR. Golden tests pin the shapes.

## Consequences

- Global Standard: S-7, S-9, cross-plane rule 4 and the 4.1 wire-type bullet are amended (status line updated).
- Operations and Edge each generate a P-256 key pair and give Platform the public key; rotating means adding a second key id, switching, then removing the first.
- Consumers read error text from Platform instead of keeping copies; Platform owns those texts.
- Contract tests and the internal endpoint register live in the [plane verification record](../platform/PLATFORM-PLANE-VERIFICATION.md).
