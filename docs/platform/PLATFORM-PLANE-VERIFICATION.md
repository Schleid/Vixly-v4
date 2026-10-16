# Platform plane - verification record (Stage 1)

| | |
|---|---|
| Plane | Platform |
| Date | 2026-10-16 |
| Scope | Slices 0 to 9 and the Slice 5 Zelle update |
| Standard | [Global Standard v1.3](../global/VIXLY-GLOBAL-STANDARD.md) with ADR-0014 |
| Status | **Built and verified; waiting for owner sign-off of slices 3 to 9 and of this record** |

## 1. Slice sign-offs

| Slice | Record | Status |
|---|---|---|
| 0 Foundation | [SLICE-0](SLICE-0-FOUNDATION.md) | Waiting for owner sign-off |
| 1 Identity | [SLICE-1](SLICE-1-VERIFICATION.md) | Signed off 2026-10-14 |
| 2 Tenants | [SLICE-2](SLICE-2-VERIFICATION.md) | Signed off 2026-10-14, one correction for later |
| 3 Subscriptions | [SLICE-3](SLICE-3-VERIFICATION.md) | Waiting for owner sign-off |
| 4 Billing | [SLICE-4](SLICE-4-VERIFICATION.md) | Waiting for owner sign-off |
| 5 Payments | [SLICE-5](SLICE-5-VERIFICATION.md) | Waiting for owner sign-off and a Stripe test-mode run |
| 5 Zelle update | [Zelle update](SLICE-5-ZELLE-UPDATE.md) | Waiting for owner sign-off |
| 6 Collections | [SLICE-6](SLICE-6-VERIFICATION.md) | Waiting for owner sign-off |
| 7 Licensing | [SLICE-7](SLICE-7-VERIFICATION.md) | Waiting for owner sign-off |
| 8 Legal | [SLICE-8](SLICE-8-VERIFICATION.md) | Waiting for owner sign-off |
| 9 Contracts | [SLICE-9](SLICE-9-VERIFICATION.md) | Waiting for owner sign-off |

## 2. Anonymous endpoint register (S-6)

Platform.Api, public host. Limits are per client IP.

| Route | Purpose | Limit |
|---|---|---|
| `GET /health/live`, `/health/ready` | Health | none |
| `GET /registration/slugs/{slug}` | Slug check | 30 per minute |
| `POST /registration` | Start registration (bot check) | 5 per hour |
| `POST /registration/verify` | Verify email | 10 per hour |
| `POST /registration/email`, `/resend`, `/plan` | Change a registration (access token) | 10 per hour |
| `GET /registration/plans` | Plan catalogue | 60 per minute |
| `GET /registration/promotions/{code}`, `POST /registration/offer` | Promotion and offer checks | 30 per minute |
| `GET /registration/status`, `/registration/invoice` | Registration status and invoice (access token) | 60 per hour |
| `POST /registration/checkout`, `/registration/zelle` | Pay the first invoice | 20 per hour |
| `GET /payment-links/{token}`, `POST /payment-links/{token}/checkout`, `/zelle` | Pay and save-card links | 60 per hour, 20 per hour |
| `GET /payments/zelle/qr` | Zelle QR image | 60 per hour |
| `POST /payments/webhook` | Card provider events (signed) | 600 per minute |
| `GET /legal/terms`, `/legal/documents/{id}/{language}` | Current Terms and PDFs | 60 per minute |
| `GET`/`POST /dev/simulated-checkout/{id}` | Development only (not mapped in Production) | none |

Console (Platform.Web) sign-in, invitation, password reset and second-factor routes are listed in [Slice 1](SLICE-1-VERIFICATION.md) with their limits; the console legal file route (`/legal/files/...`) needs a staff session.

## 3. Internal endpoint register (S-7, D-K8)

Platform.Api, internal hosts only, `/internal/v1`, ES256 service token on every call.

| Route | Scope | Role | Second factor | Limit (per minute) | Cache |
|---|---|---|---|---|---|
| `GET /portal/{slug}`, `POST /portal/batch` | portal | none | no | 3,000 per caller | 60 s |
| `POST /jobs/entitlements` | jobs | none | no | 120 per caller | 5 min |
| `GET /tenant`, `/tenant/entitlements`, `/tenant/subscription`, `/tenant/plans`, `/tenant/legal` | tenant | any | no | 600 per caller and tenant | 5 min |
| `PUT /tenant` | tenant | owner, admin | yes | 600 | no-store |
| `POST /tenant/subscription/preview` | tenant | any | no | 600 | no-store |
| `POST /tenant/subscription` | tenant | owner, admin | yes | 600 | no-store |
| `GET /tenant/invoices`, `/tenant/documents` | tenant | any | no | 600 | no-store |
| `GET /tenant/documents/{kind}/{id}` | tenant | any | no | 600 | 1 h private |
| `POST /tenant/invoices/{id}/pay-link` | tenant | owner, admin, billing | no | 600 | no-store |
| `GET /tenant/devices`, `POST /tenant/devices` | tenant | any | no | 600 | no-store |
| `POST /tenant/devices/{id}/verify`, `/challenge` | tenant | any | no | 60 per device | positive 5 min |
| `POST /tenant/devices/{id}/approve`, `/deny`, `/release`, `/withdraw-release` | tenant | owner, admin | yes | 600 | no-store |
| `POST /tenant/devices/first-device-link`, `/owner-recovery` | tenant | owner | no | 600 (and 3 links per hour per tenant) | no-store |
| `POST /tenant/legal/accept` | tenant | owner | no | 600 | no-store |
| `POST /device-links/complete` | tenant (no tenant claim) | none: the emailed token | no | 600 | no-store |
| `GET /events` | events | none (Edge: three types) | no | 240 per caller | no-store |

## 4. Tenant-isolation matrix (S-13)

| Data | How the tenant is fixed | Proof |
|---|---|---|
| Staff console (all slices) | Staff permissions; tenant chosen by staff | Gate and permission tests per slice |
| Account, entitlements, subscription, plans | Token `tid` only | Application tests; e2e reads |
| Invoices, pay links, PDFs | Token `tid`; another tenant's record answers "not found" | `AnotherTenantsInvoiceOrDocument_LooksMissing`; e2e 404s |
| Devices | Token `tid`; another tenant's device is "not found" | `AnotherTenantsDevice_IsNeverFound` (Slice 7) |
| Legal status and acceptance | Token `tid`; owner role | Slice 8 tests; e2e |
| Device link completion | The link token's tenant | Slice 7 link tests |
| Event feed | Not tenant-scoped (consumers filter by `tenantId`); Edge sees three types only | Feed tests |
| Payment links (public) | The link token's tenant and invoice | Slice 5 tests |

## 5. Contract tests

- `PlatformContractShapeTests` pins every `Contracts.Platform` internal record, event and event type name against `PlatformContractShapes.golden.txt` (names, types, nullability). Version 1 changes may only add lines.
- Errors are `{ code, message, arguments, correlationId }` in camelCase on every route (ADR-0014).
- Public registration, payment and legal records are pinned by their Slice 2 to 8 HTTP tests and Public.Web usage.

## 6. Security checks

| Rule | Where it stands |
|---|---|
| S-1 authorization in the gate and the use case | Staff: permission gate per use case; tenant: role and second-factor rules first in each use case |
| S-2 identity from trusted context | Staff from the session; tenant from the signed token only |
| S-4, S-5 cookies and keys | Slice 1 (host-only `__Host-` cookies, keys in the `platform` schema) |
| S-6 rate limits | Registers in sections 2 and 3 |
| S-7 internal callers | ES256 tokens, 2-minute lifetime, one use, per-caller keys and scopes, internal host only (ADR-0014) |
| S-8 CORS and hosts | Public site origin only; `AllowedHosts` restricted outside Development |
| S-9 errors | No exception text; code, arguments, Platform text, correlation id |
| S-10 headers | CSP, HSTS, nosniff, frame protection on both hosts |
| S-11 secrets | None in the repository; keys and passwords from the environment |
| S-12 health and logs | `/health/live`, `/health/ready` (schema level); no tokens or secrets logged |
| S-13 isolation | Section 4 |
| Evidence tables | Audit, history, documents, device events, Terms acceptances, published events are append-only for the app login |

## 7. Performance

- Lists are paged with a hard cap (P-1): staff lists 25 by default, internal lists at most 100 per page, feed at most 500.
- Batch reads for Operations jobs and Edge (P-3): entitlements and portal access, 100 per call.
- Short caching per D-K6 keeps Edge and Operations from calling on every request.
- Known cost: a batch reads its tenants one after another (about five queries each). Fine for 100; to tune with real Operations jobs.

## 8. Operability

- One worker loop runs the subscription, billing, payments, collections, licensing, legal and event-feed clocks; each logs what it did.
- The outbox delivers each tenant's messages in order; failures back off and stop after 10 attempts (Slice 2).
- One global SQL script builds the database, logins and grants (16 migrations to `Contracts`).
- Configuration: `PlatformInternal` hosts and caller keys; off until set.

## 9. Open items for later stages

| Item | When |
|---|---|
| Internal DNS name, IP allow-list and `AllowedHosts` entry for Platform.Api | Before Operations goes live (Stage 3) |
| Operations and Edge generate their P-256 keys and send the public keys | Stage 3 and Stage 5 |
| Agree each call shape with the Operations and Edge teams (C-2) | As they build |
| Shipping catalogue, maintenance status, tenant activity calls | With their consumers (D-K9) |
| Stripe test-mode run | Owner (Slice 5) |
| Slice 2 correction noted at sign-off | Slice 2 record, section 5 |
| Outbox: log handler failures with the message id (sweep finding) | Next Platform change |
| Replay cache is per process | Fine on shared hosting (one process); shared cache if Platform.Api ever runs on several servers |

## 10. Build and test counts, and sign-off

| | |
|---|---|
| Release build | `Vixly.Platform.slnx`: no errors, no warnings |
| Platform.Domain.Tests | 286 |
| Platform.Application.Tests | 216 |
| Platform.Infrastructure.Tests (SQL Server) | 56 |
| Platform.Api.Tests | 14 |
| Platform.Web.Tests | 31 |
| Architecture.Tests | 6,094 |
| Shared.Kernel.Tests | 81 |

Owner sign-off: ______________________ Date: __________
