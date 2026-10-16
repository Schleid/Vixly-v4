# Slice 9: Contracts and verification - verification record

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Date | 2026-10-16 |
| Built on | Slice 8 as delivered (not committed: the dev team owns git) |
| Design | [Design note](SLICE-9-CONTRACTS-DESIGN.md) (build notes in section 11), [sweep](SLICE-9-CONTRACTS-SWEEP.md), [ADR-0014](../decisions/ADR-0014-internal-contracts-and-error-shape.md) |
| Plane record | [Platform plane verification](PLATFORM-PLANE-VERIFICATION.md) |
| Status | **Built and verified; waiting for owner sign-off** |

## 1. Automated checks

Release build of `Vixly.Platform.slnx`: no errors, no warnings.

| Test project | Before (Slice 8) | After |
|---|---|---|
| Platform.Domain.Tests | 286 | 286 |
| Platform.Application.Tests | 210 | 216 |
| Platform.Infrastructure.Tests (with SQL Server) | 54 | 56 |
| Platform.Api.Tests | 8 | 14 |
| Platform.Web.Tests | 31 | 31 |
| Architecture.Tests | 5,698 | 6,094 |
| Shared.Kernel.Tests | 81 | 81 |

All pass.

What the new tests cover:
- **Roles and second factor:** staff and billing users cannot approve devices; an admin without a checked second factor is asked for one; only the owner asks for the first-device link and accepts the Terms.
- **Tenant isolation (S-13):** another tenant's invoice, pay link and PDF answer "not found"; the tenant always comes from the token.
- **Own account:** the owner contact cannot be changed by the tenant; billing contact and language change; the owner contact stays.
- **Subscription:** a tenant sees public plans and its own; a private plan is refused; a change is audited with the tenant actor.
- **Portal access and batches:** canonical slug, default language and legal status; more than 100 tenants refused; an unknown tenant answers no access.
- **Event feed:** wire codes (`revoked`, `lost_or_stolen`); one row per outbox message and type, also when a message is retried; the 5-second settle window; Edge sees only its three types; the cursor does not move on an empty page.
- **Database:** sequence order across commits; the unique rule per message and type; type filter and settle time in SQL; 30-day clean-up; billing document lists per tenant without loading content.
- **HTTP channel:** internal routes do not exist on the public host or for unknown paths; a valid token reaches the handler once and a replay is refused; nine broken tokens (none, garbage, unknown key, wrong key id, wrong audience, too long, expired, short id, bad signature) all answer `platform.internal.token_invalid` with `WWW-Authenticate`; scopes and the tenant claim are checked before any handler; errors carry arguments, a correlation id (also in `X-Correlation-Id`) and text in the caller's language.
- **Contract shapes:** a golden file pins every internal record, event and event type name; errors are camelCase.

`PublishedEvents` cannot be updated by the app login (global script).

## 2. End-to-end run (console, worker and API, fresh Development database)

Fresh database from the global script, first SuperUser (Jude), `--seed-demo-tenants`, card payments on. Platform.Api ran with an internal host (`localhost`) and two callers whose keys were made for the run; the test signed ES256 tokens like Operations and Edge will.

| # | Check | Result |
|---|---|---|
| 1 | Edge portal access (upper-case slug) | Found, active, full access, canonical slug, language `fr`, legal `due`; `private, max-age=60`; batch of two answers found and not found |
| 2 | Edge reads a tenant | 403 `platform.internal.scope_not_allowed` |
| 3 | Operations reads the account | Code, status, three contacts, Vixly support; `private, max-age=300` |
| 4 | Entitlements, subscription (fr), plans, legal (es) | Wire codes throughout (`max_licensed_devices`, `annual`, `full`); plan name in French; public plans only; Terms 2026-10 in Spanish |
| 5 | Device approval | Staff 403 `role_not_allowed`; admin without second factor 403 `step_up_required` with the French text; admin approves (204), device active; staff enrols a tablet and gets its secret once |
| 6 | Billing | Invoice list; receipt and invoice PDFs; PDF downloads; another tenant's PDF and invoice answer 404 |
| 7 | Subscription change | Preview +1 licence; unknown term refused; private plan refused; owner change applied; audit holds the tenant actor and role |
| 8 | Terms | Admin refused (`owner_only`); owner accepts; acceptance stores role, browser IP and user agent from the token |
| 9 | Account update | Billing contact replaced (204); owner contact refused |
| 10 | Event feed | `device.approved` and `seats_changed` after the approval; subscription `status_changed` and `seats_changed` after the change; `terms_accepted`; payloads with wire codes; the cursor after the last event returns nothing |
| 11 | Replay | The same token id twice: second call 401 |
| 12 | Evidence protection | The app login cannot update published events |
| 13 | Outbox | No failing message |

## 3. Not covered here

- Operations and Edge themselves: they do not exist yet (Stages 3 and 5). Their teams agree each call shape before finishing (C-2); the golden file is the starting point.
- The DNS name, IP allow-list and key exchange for production: deployment tasks, listed in the plane record.
- Shipping catalogue, maintenance status and tenant activity: later, with their consumers (D-K9).
