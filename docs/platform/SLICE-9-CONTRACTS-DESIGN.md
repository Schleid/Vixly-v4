# Slice 9: Contracts and verification - design

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Date | 2026-10-16 |
| Sweep | [SLICE-9-CONTRACTS-SWEEP.md](SLICE-9-CONTRACTS-SWEEP.md) |
| Owner decisions | 2026-10-16, section 10 |
| Status | **Built and verified; waiting for owner sign-off** ([verification](SLICE-9-VERIFICATION.md), [plane record](PLATFORM-PLANE-VERIFICATION.md)) |

## 1. Outcome

- Edge and Operations can call Platform safely: each caller signs short-lived tokens with its own private key; Platform checks every call.
- The internal calls live in Platform.Api under `/internal/v1`, answering only on an internal hostname.
- Every call the earlier slices promised for Operations and Edge (sweep calls 1 to 24) is reachable, scoped to the tenant in the token.
- Other planes read Platform's events from an ordered feed with a cursor.
- Answers use `Contracts.Platform` wire records: string codes, never domain types; errors carry code, arguments, text in the caller's language and a correlation id.
- The plane verification record closes Stage 1.

## 2. Service tokens (D-K1, D-K8)

- A token is a JWT signed with ES256 by the caller's private key (P-256). Header: `alg` ES256, `kid` the key id.
- Claims: `iss` the caller (`operations` or `edge`), `aud` `vixly-platform-internal`, `iat`, `exp` (at most 2 minutes after `iat`), `jti` (unique; a reused id is refused for 5 minutes).
- Tenant calls add: `tid` tenant id, `sub` actor reference, `name`, `role` (`owner`, `admin`, `billing`, `staff`), `stepup` (true when Operations checked a second factor in the last 5 minutes), and for legal acceptance `ip` and `ua` of the user's browser.
- Platform holds only public keys, per caller, in configuration (`PlatformInternal:Callers:{caller}:Keys:{kid}`, base64 SubjectPublicKeyInfo); two keys per caller allow rotation. Each caller has scopes: `portal`, `tenant`, `events`, `jobs`.
- Platform builds `PlatformTenantAuthorizationContext` and `PlatformInternalAuthorizationContext` only from a valid token. The tenant id never comes from the route or the body (S-2, S-13).
- Clock skew allowed: 30 seconds.

## 3. Where the calls live (D-K2)

- `/internal/v1/...` in Platform.Api, mapped only for the hosts in `PlatformInternal:Hosts` (an internal hostname, DNS only, not proxied by Cloudflare). On any other host the routes do not exist (404).
- Forwarded headers are not trusted for internal calls; the host's IP allow-list adds a network layer.
- With no hosts or no callers configured, the internal routes are off.

## 4. Role and step-up rules per call (D-K8)

| Calls | Role | Step-up |
|---|---|---|
| Reads (tenant, entitlements, subscription, plans, invoices, devices, legal status) | any | no |
| Enrol a device, create a challenge, verify a device | any | no |
| Preview a subscription change | any | no |
| Approve, deny, release, withdraw a release | owner or admin | yes |
| First-device link; owner recovery (Operations confirmed the owner by email) | owner | no |
| Change the subscription; update contacts, language or time zone | owner or admin | yes |
| Pay link | owner, admin or billing | no |
| Accept the Terms | owner | no |

A refused call answers `platform.common.role_not_allowed` or `platform.common.step_up_required` (403). The role is checked before the second factor.

## 5. The calls (D-K9)

Edge (`portal`): portal access by slug, and a batch of up to 100 slugs. The answer adds the legal reason, the canonical slug and the tenant's default language.

Operations (`tenant`): tenant summary (code, names, address, contacts, language, time zone, status, Vixly support contact); update billing and technical contacts; entitlements (with the non-payment and legal parts); current subscription; plans the tenant can move to; preview and change the subscription; invoices, credit notes and receipts (paged) and their PDFs; a pay link for an open invoice; devices (paged); the 10 device calls of Slice 7; legal status and accept.

Operations (`jobs`): entitlements for up to 100 tenants in one call.

Later, with their consumers: shipping catalogue, maintenance status, tenant activity (reverse direction, same token scheme).

## 6. Events (D-K3)

- `GET /internal/v1/events?after={sequence}&limit={n}` (`events` scope, at most 500).
- A published-events table holds the public events with a growing sequence number. Rows are added by an outbox handler for each public type, once per outbox message (unique index), in the order the outbox delivers them.
- The feed shows only events older than 5 seconds, so a cursor never skips an event whose commit was still running.
- Public types (all `.v1`): `platform.tenant.status_changed`, `platform.tenant.slug_corrected`, `platform.subscription.status_changed`, `platform.plan.version_published`, `platform.device.approved`, `platform.device.released`, `platform.device.revoked`, `platform.licensing.seats_changed` (new), `platform.legal.version_published`, `platform.legal.terms_accepted`. Edge sees only the tenant, slug and subscription status events.
- Payloads are `Contracts.Platform.Events` records with string codes. Events are kept 30 days.

## 7. Wire rules (D-K4, D-K7, D-K10)

- Major version in the route and event type; inside v1 only additive changes. Consumers ignore unknown fields and treat unknown codes as refused. A v2 lives beside v1 for at least 90 days, with an ADR.
- Errors: `{ code, arguments, message, correlationId }`. The message and Platform-owned texts are in the caller's `Accept-Language` (en, fr, es; English fallback). Applies to the public endpoints too.
- Names: `...Body` for requests, `...Info` or a noun for answers; folders `Common`, `Internal`, `Events`. `PlatformApiError` moves to `Common`.
- Golden JSON tests pin the shape of every internal record and event.
- ADR-0014 amends Global Standard S-7, S-9 (text in the requested language when the producer owns it) and section 4.1 (wire naming). ADR-0013 was already taken by the Frontend/UI styling decision.

## 8. Caching and limits (D-K6)

- Answers say how long a caller may keep them: portal access 60 seconds; entitlements, subscription and legal status 5 minutes (`Cache-Control: private, max-age`). Device checks: a positive answer at most 5 minutes; refusals are never cached. Callers also drop cached answers on the matching events.
- Rate limits per caller and tenant (not per IP); device checks 60 per minute per device.

## 9. Plane verification record (D-K11)

A separate `PLATFORM-PLANE-VERIFICATION.md`: slice sign-offs 0 to 9; anonymous and internal endpoint registers (route, auth, scope, role, limit, cache); tenant-isolation matrix; contract tests; security checks; performance; operability; open items for later stages; build and test counts; the owner's signature.

## 10. Owner decisions (2026-10-16)

| # | Decision |
|---|---|
| D-K1 | Signed service tokens (as proposed) |
| D-K2 | Platform.Api, internal hostname only, IP allow-list, tokens still checked (as proposed) |
| D-K3 | Pull feed with a cursor (as proposed) |
| D-K4 to D-K11 | As proposed |

## 11. Build notes (2026-10-16)

### Where things are

| Part | Code |
|---|---|
| Tenant roles | `Security/PlatformTenantActorRole`, `PlatformTenantAccess` (Require, RoleName, ParseRole); `PlatformTenantAuthorizationContext` gained Role, StepUp, Caller, ClientIp, UserAgent |
| Tenant calls 14 to 24 | `UseCases/TenantAccount/*` (13 use cases), `PlatformEntitlementsReader` (shared with the staff entitlements use case), `PlatformTenantPlanChoice` |
| Event feed | `Domain/Messaging/PlatformPublishedEvent`, `Messaging/PlatformPublicEvents` (mapping), `PlatformPublicEventHandler` (one per source type), `PlatformEventFeedClock`, `UseCases/Events/ReadPlatformEvents`; table `platform.PublishedEvents` |
| Wire records | `Contracts.Platform/Common`, `Internal/{Portal, Tenant, Entitlements, Subscription, Billing, Devices, Legal}`, `Events` |
| Channel | `Platform.Api/Internal/*` (options, token validator, authentication step, rate limits), `Endpoints/Internal/*` (routes, mapping, cache) |

### Routes (`/internal/v1`, internal hosts only)

| Scope | Route | Use case |
|---|---|---|
| portal | `GET /portal/{slug}`, `POST /portal/batch` | GetPlatformPortalAccess |
| jobs | `POST /jobs/entitlements` | GetPlatformEntitlementsBatch |
| tenant | `GET` and `PUT /tenant` | GetTenantAccount, UpdateTenantAccount |
| tenant | `GET /tenant/entitlements`, `/tenant/subscription`, `/tenant/plans` | GetTenantEntitlements, GetTenantSubscription, ListTenantPlanOptions |
| tenant | `POST /tenant/subscription/preview`, `POST /tenant/subscription` | PreviewTenantSubscriptionChange, ChangeTenantSubscription |
| tenant | `GET /tenant/invoices`, `POST /tenant/invoices/{id}/pay-link` | ListTenantInvoices, CreateTenantPayLink |
| tenant | `GET /tenant/documents`, `GET /tenant/documents/{kind}/{id}` | ListTenantBillingDocuments, GetTenantBillingDocument |
| tenant | `GET /tenant/legal`, `POST /tenant/legal/accept` | GetPlatformTenantLegalStatus, AcceptPlatformLegalTerms |
| tenant | `GET` and `POST /tenant/devices`; `POST /tenant/devices/{id}/approve`, `/deny`, `/release`, `/withdraw-release`; `POST /tenant/devices/first-device-link`, `/owner-recovery` | the Slice 7 device use cases, ListTenantDevices |
| tenant (device limit) | `POST /tenant/devices/{id}/verify`, `/challenge` | VerifyPlatformDevice, CreatePlatformDeviceChallenge |
| tenant, no tenant claim | `POST /device-links/complete` | CompletePlatformDeviceLink (the emailed token proves the tenant) |
| events | `GET /events?after=&limit=` | ReadPlatformEvents |

### Configuration

```json
"PlatformInternal": {
  "Hosts": [ "<internal hostname>" ],
  "Callers": {
    "operations": { "Scopes": [ "tenant", "events", "jobs" ], "Keys": { "<key id>": "<base64 SubjectPublicKeyInfo>" } },
    "edge": { "Scopes": [ "portal", "events" ], "Keys": { "<key id>": "<base64 SubjectPublicKeyInfo>" } }
  }
}
```

- Off by default (empty hosts and callers in `appsettings.json`): no internal route exists.
- The internal hostname must also be in `AllowedHosts`.
- A key that does not load stops Platform.Api at start, rather than refusing every call later.
- Public keys are not secrets; private keys stay with Operations and Edge.

### Decisions made while building

- **Error codes:** the existing `platform.common.step_up_required` is reused; `platform.common.role_not_allowed` is new. Channel refusals are `platform.internal.token_invalid` (401, one code for every token fault), `platform.internal.scope_not_allowed` and `platform.internal.tenant_required` (403).
- **Internal status codes:** 401 token, 403 role, scope, step-up and owner-only, 404 for every `..._not_found`, 409 concurrency, 429 link throttles, 400 otherwise. Public routes keep their Slice 2 to 8 statuses; only the body gained `arguments` and `correlationId`.
- **Owner contact:** a tenant changes its billing and technical contacts; the owner contact stays with Vixly staff (identity and recovery) and is refused with `platform.tenant_account.owner_contact_locked`.
- **Plans a tenant may choose:** public plans on sale and its current plan; anything else is `platform.tenant_account.plan_not_offered`. Changes are audited with the tenant actor (reference, name, role).
- **Device link completion** has no tenant claim: the emailed token proves the tenant and the user may not be signed in yet.
- **Events:** one outbox message gives one or two public events: device events also give `seats_changed` (cause `device`); subscription started, changed and renewed give `subscription.status_changed` and `seats_changed` (cause `subscription`); ended and status changes give `subscription.status_changed`. The subscription event carries the history code in `change`, so consumers can tell a status move from a plan change.
- **Feed order:** rows of one commit may get their sequence numbers in any order between themselves; later commits always get later numbers. The 5-second settle window covers commits still running.
- **Portal access:** only permanent slugs resolve (a slug still held by a registration does not); an old slug answers the current one in `canonicalSlug`.
- **Caching:** portal 60 seconds; account, entitlements, subscription, plans and legal 5 minutes; a positive device check without a new secret 5 minutes; PDFs 1 hour (private); everything else `no-store`.
- **Rate limits:** portal 3,000, tenant 600, jobs 120, events 240 per minute per caller and tenant; device checks 60 per minute per device.
- **Published events** are append-only for the app login except the 30-day clean-up (`DENY UPDATE` in the global script).
