# Slice 9: Contracts and verification - sweep

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Sweep done, 2026-10-16.** Decisions in section 6 are proposals for the owner; answers go into the design note |
| Source | V4: `docs/platform/PLATFORM-PLANE.md`, every Slice 0 to 8 design, sweep and verification note, Global Standard v1.3, ADR-0003, ADR-0007, ADR-0011, ADR-0012, the ADD; `src/Contracts/*`, `src/Platform/Vixly.Platform.Api`, `Platform.Application` (`Security`, `Messaging`, `UseCases/Licensing`, `Legal`, `Subscriptions`, `Tenants`), the empty `Operations`, `Customer`, `Edge` and `Public` projects. V3 `ControlPlane` (read-only): `API/Controllers` (all 11 controllers, including `InternalLicensingController`, `InternalTermsController`, `InternalSubscriptionsController`, `InternalTenantsController`, `TenantCompanyProfileController`, `TenantLandingPagesController`, `PortalAccessController`, `TenantAuthController`), `IJwtTokenService`, `UseCases/PortalAccess`, `SubscriptionEntitlementService` |
| Rule | Replicate behaviour, not code. The contract is shaped by what V4 slices promised, not by V3's routes |
| Feeds | Slice 9 design note; `Contracts.Platform` (wire records for Edge and Operations, events); `Platform.Api` (internal channel, auth, limits); plane verification record; Operations (Stage 3: Platform client, cache, enforcement); Edge (Stage 5: tenant resolution); Global Standard (S-7, S-9 and section 4 amendments if the owner agrees) |

## 1. How the evidence was gathered

Every Slice 0 to 8 design note was read for its "Contract for Operations" section and for every line that says "Slice 9 exposes them" or "through `Contracts.Platform`". The Application layer was searched for use cases whose first argument is `PlatformTenantAuthorizationContext` or `PlatformInternalAuthorizationContext`: these are the internal calls that are built and wait for a channel. `PlatformMessageTypes.cs` was read for events marked "For Operations", and the outbox dispatcher for what happens to them. Every record in `Contracts.Platform` and every endpoint in `Platform.Api` was listed with its auth and rate limit. The Operations, Customer, Edge and Public projects were opened to see what calls Platform. V3's controllers were traced for every route another plane used. The V3 snapshot lacks `Program.cs`, the internal API key middleware, the JWT service implementation, the API DTOs and all Operations code, so how the key was checked and how JWTs were signed is known only from comments and the ADD. Detailed notes with file and line evidence: `v4-contracts-notes.md`.

In short: V4 has **13 internal use cases built and unreachable**, **14 more promised with no tenant-facing use case yet** (13 into Platform, 1 from Platform to Operations), **5 events written for Operations that nobody can read**, and **no service authentication at all**. No other plane has code yet, so the shapes can still be fixed without breaking anyone.

## 2. Inventory: every internal call and event the earlier slices promised

### 2.1 Calls built in Application, waiting for Slice 9

| # | Caller | Use case | Context | Returns | Promised by |
|---|---|---|---|---|---|
| 1 | Edge, Operations | `GetPlatformPortalAccess(slug)` | Internal | Found, tenant id, tenant status, access level (Full, Read-only, Payment required, None). Unknown, never-active and closed look the same | Slice 7, D-L10 |
| 2 | Operations (Edge named too) | `VerifyPlatformDevice` | Tenant | Allowed, or a code (`device_unknown`, `device_pending`, `device_released`, `device_revoked`, `proof_invalid`, `tenant_suspended`, `tenant_closed`, `subscription_none`); access level; new secret when rotating | Slice 7, D-L5, D-L10 |
| 3 | Operations | `CreatePlatformDeviceChallenge` | Tenant | A challenge, 2 minutes, single use | Slice 7 (device key) |
| 4 | Operations | `EnrolPlatformDevice` | Tenant | Device id, number, status, secret (rotating-secret devices) | Slice 7, D-L4 |
| 5 | Operations | `ApprovePlatformDevice` | Tenant | Success or error code | Slice 7, D-L4 |
| 6 | Operations | `DenyPlatformDevice` | Tenant | Success or error code | Slice 7, D-L4 |
| 7 | Operations | `ReleasePlatformDevice` | Tenant | New status, whether it waits for Vixly review | Slice 7 |
| 8 | Operations | `WithdrawPlatformDeviceRelease` | Tenant | Success or error code | Slice 7 |
| 9 | Operations | `RequestPlatformFirstDeviceLink` | Tenant | Success; the link goes only by email | Slice 7 |
| 10 | Operations | `StartPlatformOwnerRecoveryForOwner` | Tenant | Success; the link goes only by email | Slice 7 |
| 11 | Operations (relays the tenant-host page) | `CompletePlatformDeviceLink` | Anonymous | Device enrolment (as 4) | Slice 7, D-L12, D-L16 |
| 12 | Operations | `GetPlatformTenantLegalStatus(language)` | Tenant | Status (accepted, due, overdue), deadline, read-only flag, the Terms to accept with parts, hashes and summary | Slice 8, D-Lg11 |
| 13 | Operations | `AcceptPlatformLegalTerms` | Tenant | Success or `platform.legal.version_changed` | Slice 8, D-Lg5, D-Lg11 |

### 2.2 Calls promised, with no tenant-facing use case yet

| # | Caller | Call | Returns | Promised by | State today |
|---|---|---|---|---|---|
| 14 | Operations | Tenant entitlements | Subscription status, access level, features, effective limits, non-payment part (block date, amount due, pay link possible), legal part | Slices 3, 6, 8; ADR-0003; ADR-0012 W-4 | Staff context only; answer uses domain enums |
| 15 | Operations | Tenant company profile (read) | Legal name, address, contacts, default language, time zone | Slice 2 sweep | Staff context only |
| 16 | Operations | Update the tenant's contacts and settings | Success or error codes | Slice 2 owner decision (tenant keeps its profile in Operations) | Staff context only |
| 17 | Operations | Vixly support contact | Support email and phone | Slice 8 build note, D-Lg15 | Not exposed |
| 18 | Operations | Tenant code | Immutable code (used in Operations numbers) | V3 internal API; D-T3 | Not exposed |
| 19 | Operations | Current subscription | Plan, term, licences, seats, renewal date, scheduled change | Slice 3 sweep | Staff context only |
| 20 | Operations | Plans the tenant can move to | Plan list in the reader's language with prices | Slice 3 sweep | Only the public registration catalogue |
| 21 | Operations | Preview and change the subscription (plan, licences) | Price preview; change result | Slice 3 sweep, D-S4, D-S5 | Staff context only |
| 22 | Operations | The tenant's invoices, receipts and their PDFs | Paged list; file | Slice 4 design | Not exposed |
| 23 | Operations | Pay link for the owner and billing contact when payment is required | A link or checkout start | Slice 6 design, D-C4 | Not exposed; entitlements say only whether cards are available |
| 24 | Operations | The tenant's device list (paged) | Devices with status, last seen | Slice 7, D-L15 | Staff context only |
| 25 | Operations | Shipping service catalogue (sea, air) | Stable codes and names per language | Plane document, section 4 (Slice 3) | **Not built** |
| 26 | Edge, Operations | Maintenance status | Active window, message per language, end time | Slice 8, D-Lg14 | Owner: later, with Edge |
| 27 | Platform calls Operations | Tenant activity (sign-ins, daily counts) | Counts and events | Slice 2, D-T14 | Stage 3; the reverse direction, same auth problem |

The **Customer plane calls nothing**. Slice 7 (D-L10) says the customer portal follows the tenant status only; Slice 8 (D-Lg11) says customers only see links to Vixly's public Privacy Policy; Slice 6 says customer-facing features ignore a block. **Public** is already served by the anonymous endpoints of Slices 2 to 8 (section 4).

### 2.3 Events promised for other planes

| Event | Promised by | Written today? | Who needs it |
|---|---|---|---|
| `platform.legal.version_published.v1` | Slice 8, D-Lg11 | Yes | Operations (owner banner) |
| `platform.legal.terms_accepted.v1` | Slice 8, D-Lg11 | Yes | Operations (lift read-only) |
| `platform.device.approved.v1` | Slice 7, D-L14 | Yes | Operations (device screen, cache) |
| `platform.device.released.v1` | Slice 7, D-L14 | Yes | Operations (cache) |
| `platform.device.revoked.v1` | Slice 7, D-L5, D-L14 | Yes | Operations (drop cached check at once) |
| `platform.licensing.seats_changed.v1` | Slice 7, D-L14 | **No**, missing from `PlatformMessageTypes` | Operations (seat count) |
| `platform.subscription.status_changed.v1` | Slice 6, D-C4 ("Operations enforces") | Yes, but shaped for Billing | Operations (access level), Edge (cache) |
| `platform.tenant.status_changed.v1` | Slice 2 (no consumer named) | Yes | Edge and Operations (cache, suspension) |

`platform.tenant.slug_corrected.v1` and `platform.plan.version_published.v1` are also written and would help Edge and Operations caches. The other 19 message types are Platform's own work (mail, billing, collections) and stay inside.

## 3. What V3 did for plane-to-plane calls

### Transport and routes

- Plain HTTP and JSON from `Operations.Web` to the Control API. 22 actions under `/api/internal/*`: legal (3: publish Terms, status per user, accept), licensing (8: list licences, attribute, register device, pending devices, approve, deny, request release, generate registration link), subscriptions (4: current, plans, capacity expansion, plan upgrade), tenant code (1), company profile and logo (2), landing page (4).
- 8 more actions Operations used sat **outside** `/api/internal` and were anonymous: portal access (`ping`, `validate-runtime`, `verify-device`, legacy `validate`), complete device registration, tenant staff login and `me`, maintenance status.
- No events. Operations asked per request and per user.

### Authentication

- Comments in every internal controller say "Internal API key middleware protects /api/internal/*". The middleware is not in the snapshot. It appears to be one key per environment held by Operations, with no per-caller identity and no scopes.
- Control also issued **JWTs to tenant staff** (`TenantAuthController`, `GenerateTenantUserToken(tenantId, tenantUserId, email, displayName)`), which Operations used. The ADD notes a fixed "JWT audience" bug. V4 dropped this: Operations owns staff login (O-6).

### Problems

1. One shared key opened every internal action. Anyone holding it could act for **any tenant**.
2. Tenant id and actor id came from the route, query string or body (`request.ApprovedByUserId`, `?tenantId=`). Platform trusted them (S-2 broken).
3. Legal acceptance recorded the **Operations server's** IP and user agent as the owner's.
4. Portal and device checks were anonymous, not rate-limited, and their codes revealed tenant status.
5. English sentences and `DomainException` text on the wire; a "temporary detailed exception output" left in the login endpoint.
6. One rate-limited internal action out of 22.
7. Whole lists, filtered in memory; the caller sent its own device count for plan upgrades.
8. Raw secrets in answers (the device registration link).
9. A diagnostic `ping` endpoint in production; a controller using the database context directly with no use case or permission.
10. The internal key could publish the master Terms with any URL and hash.

Kept from V3 because it is right: internal calls are server to server, never from a browser; Platform stays the source of truth for tenant status, entitlements and devices; tenant data is never sent to Customer.

## 4. Current state of `Contracts.Platform` and `Platform.Api`

### `Contracts.Platform`

- 23 records in 3 folders: `Legal` (2), `Payments` (6), `Registration` (15). All are for **Public.Web**. BCL only, no project or package references.
- **Nothing for Edge or Operations**: no entitlements, portal access, device, legal status, subscription, invoice or event types.
- Kinds on the wire are lowercase string codes (`pay_invoice`, `privacy`, `percent`), not enums.
- Names use `...Body` for requests and `...Info` or a plain noun for answers. The Global Standard (section 4) asks for `Platform...Dto`.
- `PlatformApiError(Code, Message)` is the only error shape. It sits in `Registration`, and it has **no arguments and no correlation id**.

### `Platform.Api`

- 21 endpoints plus health, all **anonymous** or protected by a bearer token that is not a service identity (registration access token, payment link token, provider webhook signature). Two simulated-checkout endpoints run in Development only.
- **No authentication** is registered; there is no JWT package in `Directory.Packages.props`. The host comment says "Internal endpoints for Edge and Operations (JWT) come in Slice 9".
- Rate limits are fixed windows **per client IP** (9 named policies).
- Errors are `{ code, message }` with the message in the `Accept-Language` culture.
- `AllowedHosts` is the public API hostname; forwarded headers trust Cloudflare ranges only. Today the API is reached through Cloudflare.
- CORS allows only the Public site, on 4 endpoints.

### Other planes

`Operations` (6 projects), `Customer` (5), `Edge`, `Public.Web` and `Contracts.Operations` hold a README each and nothing else; their test folders hold `.gitkeep`. The READMEs say: Operations.Infrastructure will hold the "Platform API client"; Operations.Api is "service-to-service auth only"; Customer reads only `Contracts.Operations`; Edge may reference only `Contracts.Platform` and `Shared.Kernel`.

## 5. Gaps and risks

1. **No service authentication yet.** `PlatformInternalAuthorizationContext` is an empty singleton and `PlatformTenantAuthorizationContext` is built by nobody. Until Slice 9, the 13 use cases cannot be reached safely.
2. **Internal routes would be public by default.** Platform.Api answers on a Cloudflare-fronted public hostname. S-7 says internal APIs are never routed publicly.
3. **The tenant context is thin.** It holds tenant id, actor reference and actor name. It does not hold the role, the permission or the step-up result Operations claims to have checked; `ActorRole` for legal acceptance travels in the body. Platform cannot see which caller made a call, and Edge and Operations look the same.
4. **Tenant isolation rests on the token.** Platform can only be as sure of the tenant as it is of the caller. The tenant id must come from the signed caller token, never from the body or route. Today there is one cross-tenant test (devices) and none for legal or at the HTTP layer.
5. **13 promised calls into Platform have no tenant-facing use case** (entitlements, profile, contacts, support contact, tenant code, subscription, plans, change, invoices, pay link, devices list, shipping catalogue, maintenance). The shipping catalogue was promised for Slice 3 and is not built.
6. **Answers use domain types.** Entitlements, portal access, device check, legal status and event payloads use domain enums. Each needs a wire record with stable codes.
7. **Events stay in Platform's outbox.** A message with no handler is marked processed at once. There is no ordered sequence number to use as a cursor, no retention, and payloads carry numeric domain enums. `seats_changed` is missing. The dispatcher does not log handler failures, and one failing message blocks later ones for the same tenant.
8. **Language on the wire.** The plane rule wants "codes and resolved text"; S-9 says "code, arguments and correlation id, not display text". The current error drops arguments and has no correlation id. The language comes only from `Accept-Language`, so Operations must forward the user's language. Legal files are served by Platform.Api with relative URLs.
9. **No versioning.** The contract README says "breaking change = new version + ADR", but there is no route version, no deprecation window and no contract test.
10. **Caching and rate limits are not designed.** Only D-L5 says anything (a positive device answer at most 5 minutes, dropped on `device.revoked`). Edge would call portal access on every request. A cached device answer cannot hand out a rotated secret. Per-IP limits put every internal call from one server in one bucket; D-L12 wants 60 per minute per device.
11. **Portal access answer is short.** No legal reason (Slice 8 design promised it), no canonical slug for redirects, no default language. The slug lookup ignores the slug state and hold date; a slug held after a correction may still resolve. To test.
12. **No batch reads** (P-3). Operations jobs that need entitlements for many tenants would loop.
13. **Records and doc drift.** The Platform Worker (ADR-0007) is not built; jobs still run in Platform.Web. Slices 0 and 3 to 8 wait for owner sign-off. There is no single anonymous-endpoint register; the Api README stops at Slice 3b. Project READMEs give stage numbers that differ from the Global Standard (Edge 4 instead of 5, and so on).
14. **Edge topology is open** (O-4). It decides whether Edge can reach a private Platform host.

## 6. Decisions

Proposals for the owner; answers go into the Slice 9 design note. Mechanics that follow from V4 rules already in place (use-case gate, paging, error codes with en, fr and es text, audit, outbox, cancellation and timeouts) are written into the recommendations and are not separate questions.

| # | Question | Options | Proposal |
|---|---|---|---|
| D-K1 | How do Edge and Operations prove who they are to Platform? | A: shared API key header per caller (S-7 minimum). B: **signed service tokens**: each caller holds its own private key and signs a short-lived JWT (client credentials with a signed assertion, no token server). C: OAuth client credentials with a token endpoint on Platform. D: mutual TLS (client certificates) | **B (Recommended).** Each caller (`edge`, `operations`) has its own key pair (ES256). The private key is a secret of the caller (environment variable today, Google Secret Manager later, S-11); Platform holds only the public keys and the allowed scopes per caller in configuration. A token lives 2 minutes, carries issuer, audience `vixly-platform-internal`, a unique id (replays refused) and, for tenant calls, the tenant id, the actor reference, name and role, and whether step-up was done. Platform builds `PlatformTenantAuthorizationContext` and `PlatformInternalAuthorizationContext` (with the caller name) only from a valid token, never from the body or route (S-2, S-13). Two active keys per caller allow rotation without downtime. A is too weak (one leaked string acts for every tenant, as in V3). C adds a token service to run and secure. D is hard on IIS behind Cloudflare, where TLS ends at Cloudflare, and needs certificate-store work the production host does not allow |
| D-K2 | Where do internal endpoints live? | A: in Platform.Api under `/internal` on the public hostname, protected by tokens only. B: a separate internal deployable. C: the same Platform.Api, but `/internal` answers only on a separate internal hostname that Cloudflare does not route, with an IP allow-list | **C (Recommended).** One deployable, one code base. `/internal/v1/...` is mapped with a host filter for an internal hostname (for example `vixly-api-internal.techcrafters.ltd`, DNS only, not proxied) and the host's IP allow-list for the Edge and Operations servers. On the public hostname `/internal` answers 404. The token check (D-K1) still applies on every call, so a network mistake does not open anything. Forwarded headers are not trusted on the internal host. B only if the internal surface grows large or needs separate scaling |
| D-K3 | How do other planes receive Platform events? | A: **pull feed over HTTP** with a cursor. B: webhook push from Platform to each plane. C: a shared message broker (Service Bus, RabbitMQ) | **A (Recommended).** `GET /internal/v1/events?after={cursor}&limit=200` returns published events in order. A separate published-events table gets a row in the same commit as the change (D-2), with a growing sequence number as the cursor, the event type with its version, the tenant id and a wire payload from `Contracts.Platform` (string codes, no domain enums). Only types listed as public are published; the other outbox messages stay internal. The consumer keeps its own cursor and polls every 15 to 30 seconds; events are kept 30 days. Edge sees only the types its scope allows. Add the missing `platform.licensing.seats_changed.v1`. B needs Operations to be up, a retry queue per subscriber and signed callbacks. C is new infrastructure that shared hosting does not provide; the feed can later be bridged to a broker without changing the contract |
| D-K4 | Contract versioning policy | A: no versions, additive only. B: **major version in the route and event type, additive inside a version**. C: version per endpoint in a header | **B (Recommended).** Routes are `/internal/v1/...`; events are `...v1`. Inside v1 only additive changes: new optional fields, new endpoints, new codes. Consumers must ignore unknown fields and treat unknown codes as "refused". A breaking change is a v2 beside v1, with an ADR (C-1, C-2) and at least 90 days of overlap. Contract tests keep a golden JSON sample for every record, so a shape change fails the build. Wire kinds are lowercase string codes (as the existing contracts do); C# enums, where used, carry explicit numbers (rule 4) |
| D-K5 | Does the Customer plane call Platform? | A: never; it reads tenant facts from Edge and Operations. B: a small read-only set (tenant status, maintenance) | **A (Recommended).** Customer reads only `Contracts.Operations` (Global Standard rule 2, Customer.Infrastructure README). Customer availability follows the tenant status (D-L10), which Edge already knows from portal access and passes on; maintenance comes the same way. Customers are never device-checked or blocked for payment. Fewer callers means fewer keys to manage |
| D-K6 | Caching of entitlements, portal access, device checks and legal status | A: no caching. B: **short time limits plus event-driven drops**. C: long caches, events only | **B (Recommended).** Portal access in Edge: 60 seconds, positive and negative answers, dropped on `tenant.status_changed`, `tenant.slug_corrected` and `subscription.status_changed`. Entitlements, subscription and legal status in Operations: 5 minutes, dropped on `subscription.*`, `plan.version_published` and `legal.*` events for that tenant. Device check: a positive answer at most 5 minutes (D-L5), never a refusal, dropped on `device.revoked` or `device.released`; the first check of a session always goes to Platform so a rotated secret is handed out. Platform answers carry `Cache-Control: private, max-age` to say the same. Rate limits per caller and per tenant, not per IP; device checks 60 per minute per device (D-L12). Add batch reads (entitlements and portal access for up to 100 tenants or slugs) for background jobs (P-3) |
| D-K7 | Language on the wire | A: codes and arguments only; each caller renders text (S-9 as written). B: **codes and arguments always, plus text resolved by Platform in the requested language**. C: text only | **B (Recommended), with an S-9 amendment.** Operations cannot render Platform's error and label texts (they live in Platform.Resources), so Platform resolves them. Every error is `{ code, arguments, message, correlationId }`; the message is in the language the caller asks for. Platform-owned content (plan names, feature labels, legal summaries, maintenance message) comes in that language too, with the language actually used in the answer and English as the fallback. The caller sends the end user's language in `Accept-Language` (the user's choice, then the tenant default, ADR-0005). Statuses and reasons stay codes, so Operations can also use its own words. Legal files are linked by absolute URL to the public Platform.Api file endpoint (published files are public), so Operations does not proxy them. Amend S-9 to say "code, arguments, correlation id, and text in the requested language when the producer owns the text" |
| D-K8 | What tenant context must Operations send, and how much does Platform trust it? | A: tenant id and actor only (today). B: tenant id, actor reference, name, **role and step-up fact** inside the signed token, checked against a rule per call | **B (Recommended).** The token for a tenant call carries tenant id, actor reference, display name, role (`owner`, `admin`, `billing`, `staff`) and step-up done (true or false). Each internal use case states the role and step-up it needs: accept Terms needs `owner`; approve, deny, release and owner links need `admin` or `owner` with step-up; reads need any role. Platform refuses a call that does not meet the rule and records the actor in history and audit. The browser's IP and user agent travel as signed claims for legal acceptance, not in the body |
| D-K9 | Which promised calls are in Slice 9, and which wait? | A: everything in section 2. B: **what Operations needs to open (Stage 3) and what Edge needs to resolve tenants**; the rest when the consumer is designed | **B (Recommended).** In Slice 9: calls 1 to 24 of section 2 (the 13 built ones; entitlements, profile, contacts and settings, support contact, tenant code, subscription, plans, preview and change, invoices with PDFs, pay link, device list), the event feed, and portal access extended with the legal reason, canonical slug and default language. Later: shipping catalogue (with Operations shipment setup), maintenance status (with Edge, owner decision), tenant activity (Stage 3, reverse direction using the same token scheme). Each call shape is agreed with the Operations team before it is finished (C-2) |
| D-K10 | Wire naming in `Contracts.Platform` | A: rename to `Platform...Dto` (Standard section 4). B: keep `...Body` for requests and `...Info` or a noun for answers, and amend the Standard | **B (Recommended).** The 23 existing records already follow one clear pattern and no consumer exists yet to break. New internal records follow it in folders `Internal/Tenants`, `Internal/Licensing`, `Internal/Legal`, `Internal/Billing`, `Events`; `PlatformApiError` moves to a `Common` folder now, while nobody depends on it. Amend Global Standard section 4 to describe the pattern |
| D-K11 | What must the plane verification record contain? | A: the slice gate list only. B: **a full plane record** | **B (Recommended).** (1) Sign-off of every slice record, 0 to 9 (0 and 3 to 8 are still open). (2) One anonymous-endpoint register: every route, its auth and its rate limit. (3) One internal-endpoint register: route, caller, scope, role and step-up rule, rate limit, cache rule. (4) Tenant-isolation matrix: every tenant-context call tested at Application and HTTP level with a token for tenant A against data of tenant B (S-13). (5) Contract tests: golden JSON for every record and event; no English sentence in any answer (en, fr and es complete). (6) Security checks: no secret in the repository, headers, CORS, `AllowedHosts`, internal host not reachable through Cloudflare, token replay and expiry refused, key rotation tried. (7) Performance: every list paged, indexes for the new queries, event feed under load. (8) Operability: health on every host, outbox and feed failures logged and alerted, Worker status (ADR-0007; today jobs still run in Platform.Web, to be closed or recorded as an accepted exception). (9) Open items carried to later stages: Public.Web pages, shipping catalogue, maintenance, invitation flow (D-T8), tenant activity, Stripe test-mode run. (10) Build and test counts, and the owner's signature |
