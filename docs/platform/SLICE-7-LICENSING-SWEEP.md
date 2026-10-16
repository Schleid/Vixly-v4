# Slice 7: Licensing - V3 behaviour sweep

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Sweep done, 2026-10-16.** Decisions in section 6 are proposals for the owner; answers go into the design note |
| Source | V3 `ControlPlane` (read-only): `src/Vixly.Platform.Application/UseCases/Licensing` (14 units), `UseCases/PortalAccess` (3 units), `UseCases/Tenants/Registration` first-device units (3), `UseCases/Billing/RegisterPayment` and `GenerateRecurringInvoices` (licence generation and counting), `UseCases/Subscriptions/RequestSubscriptionPlanChange`, `Domain/Entities/Licensing`, `Domain/Enums/Licensing`, `Domain/Entities/Tenants/Registration/FirstDeviceSetupToken`, `Infrastructure/Persistence/Configurations/Licensing`, `Infrastructure/Services/SubscriptionEntitlementService`, `API/Controllers/Licensing/InternalLicensingController`, `API/Controllers/Public/PortalAccessController`, `PublicLicensingController`, `TenantRegistrationController`, console `Pages/Tenants` (licences panel, capacity expansion) and `Components/Licenses` |
| Rule | Replicate behaviour, not code. Classification as in Slices 1 to 6: Active, Duplicate, Dead, Disconnected-wanted, Moved |
| Feeds | Slice 7 design note; Slice 2 (registration moves to Pending first device and Completed, tenant activation, tenant page Licensing section); Slice 3 (`IPlatformLicenceCounter`, decrease and downgrade checks, ended subscription); Slice 6 (D-C17, access level while blocked); Slice 9 and Operations (device and portal contract, enforcement) |

## 1. How the evidence was gathered

Each licensing and portal-access use case, controller action and console component was traced to what injects or calls it. Entities, enums and EF configurations were read for states, rules and indexes. Payment, recurring billing and plan-change use cases were read where they create or count licences. The payment extract adds only the registration-token repository and one test. Neither tree has the device and licence repositories, the three portal validation services, an Infrastructure DI file, `Program.cs` (rate limits, internal API key) or any Operations code, so the exact validation chain is known only from interface comments and result codes, and "nobody calls it" means nothing in the snapshot does. V4 code under `src/Platform` was read to see what Slice 7 must add and what it can reuse. Detailed notes with file and line evidence: `v3-licensing-sweep.md`.

V3 treats a licence as a **seat for one device** of a tenant, not for a user. Licence rows with a code (`VXL-XXXX-XXXX-XXXX-XX`) are created when an invoice is paid. A device becomes usable when it is Registered and a licence is bound to it. The device's only proof of identity is a plaintext identifier that the browser keeps and sends.

## 2. Use cases (14 + 3 portal access, plus 3 first-device units outside the folder)

| V3 use case | Called from | Status | V4 outcome |
|---|---|---|---|
| RegisterDevice | Operations, `POST api/internal/licensing/devices/register` | Active; identifier chosen by the client; a denied device returns to Pending by registering again; no cap, no expiry | Device enrolment request from Operations with a Platform-issued credential; capped and expiring (D-L4, D-L5) |
| ListTenantDevices | Operations, `GET devices/pending` (filters Pending in memory) | Active; whole list | Paged device list for Operations and the console (D-L15) |
| ApproveDeviceRequest | Operations, `POST devices/approve` | Active; takes the first open licence; actor id from the request body; no status check | Approve with a free-seat check under concurrency control (D-L2, D-L4) |
| DenyDeviceRequest | Operations, `POST devices/deny` | Active; the reason is accepted and dropped | Deny with reason code, history (D-L4, D-L14) |
| AttributeDeviceLicense | Operations, `POST licenses/attribute` | Active; Duplicate of the bind inside Approve; lets Operations pick a licence | Gone: no licence rows to pick (D-L1) |
| ListTenantLicenses | Operations (`POST licenses`), console tenant panel | Active; whole list plus every device joined in memory | Licensing summary and paged device list (D-L15) |
| RequestDeviceLicenseRelease | Operations, `POST licenses/request-release` | Active; no reason, no notice; nobody can answer it | Release by tenant admin, with a review queue above the free allowance (D-L6) |
| ApproveLicenseReleaseRequest | Nobody (registered only) | Disconnected-wanted; disables the device and frees the licence | Staff approval of releases above the allowance (D-L6) |
| DenyDeviceLicenseRelease | Nobody (registered twice) | Disconnected-wanted | Staff refusal with reason (D-L6) |
| ReleaseDeviceLicense | Nobody | Dead; Duplicate of approve-release but leaves the device Registered | Gone; one release rule (D-L6) |
| GenerateDeviceLicenses | RegisterPayment (onboarding, trial, capacity expansion) | Active; checks the plan limit only; count-then-insert race; overwrites the subscription counter | Moved: seats are `PlatformSubscription.Licences` (Slice 3); nothing is generated (D-L1) |
| GenerateDeviceRegistrationLink | Console tenant panel; Operations `POST licenses/generate-registration-link` | Active; sign-in only, no permission or step-up; raw link returned to the caller and shown on screen | Owner device recovery link, emailed only, never shown (D-L8) |
| CompleteDeviceRegistration | Anonymous `POST api/public/licensing/complete-device-registration` | Active; no rate limit; new device registered with no approval; one commit | Recovery completion through Operations, rate-limited, one commit, owner notice (D-L8) |
| ValidatePortalAccess (Licensing folder) | Anonymous `POST api/portal/access/validate` ("legacy, kept temporarily") | Active; Duplicate | Gone; merged (D-L10) |
| PortalAccessValidationUseCase (PortalAccess folder) | Nobody, not in DI | Dead; Duplicate | Gone |
| ValidatePortalRuntime | Anonymous `POST api/portal/access/validate-runtime` (pre-login) | Active; no rate limit; distinct codes reveal tenant status | Internal `GetPlatformPortalAccess` for Edge and Operations (D-L10) |
| VerifyTenantDevice | Anonymous `POST api/portal/access/verify-device` (post-login) | Active; tenant id and plaintext identifier, no rate limit, not tied to a session | Internal `VerifyPlatformDevice` with a hashed rotating credential (D-L5, D-L10) |
| GenerateFirstDeviceSetupLink, CompleteFirstDeviceSetup, RegisterFirstDevice (Tenants) | Anonymous registration endpoints | Active; Moved to Slice 7 (D-T4); two commits; fake actor id; English emails sent after commit | First-device step of registration: link by outbox, one commit that completes the registration and activates the tenant (D-L7) |
| `EnsureCanRegisterDeviceAsync` (entitlement service) | Nobody | Dead; checks "limit above zero" only | Replaced by the seat check (D-L2) |
| Console `TenantLicensingSection` and `LicenseList` (release approve, deny, request buttons) | No page uses them | Dead (the release review screen was never wired) | Release review queue (D-L6, D-L15) |

## 3. Entities and rules found

| V3 | Contents | V4 proposal |
|---|---|---|
| `Device` | Tenant; identifier (client- or server-generated string, plaintext, unique per tenant); display name; platform; status Pending, Registered, Disabled; first registered; last seen (never written). No row version | `PlatformDevice`: tenant, short public number, name, kind (browser, scanner, mobile app), status Pending, Active, Released, Revoked, Denied, Expired; requested, approved, ended dates; last seen (hour precision); credential hash and rotation dates; row version (D-L3, D-L5) |
| `DeviceLicense` | Tenant; code `VXL-...` (48 random bits, display only); status Open, Bound, Revoked; bound device; release-request flag with who and when; released and revoked who and when, wiped on the next bind. No row version; device binding not unique | No licence rows. A seat is in use while a device is Active or awaiting release; the count is checked against `PlatformSubscription.Licences`. Filtered unique index: one Active device per credential; row version on a per-tenant licensing row (D-L1, D-L2) |
| `DeviceRegistrationToken` | Replacement link: licence, old device, token hash (SHA-256 of 64 random bytes), expiry (30 minutes), creator, consumed (new device), revoked. Newer link revokes older. No row version | `PlatformDeviceLink` for first-device and recovery links: purpose, tenant, registration (first device) or replaced device (recovery), hash, expiry, sent to, consumed, revoked, row version (D-L7, D-L8) |
| `FirstDeviceSetupToken` | Same shape for onboarding, 24 hours, row version | Merged into `PlatformDeviceLink` |
| `TenantSubscription.RegisteredDeviceCount` | Means the requested quantity in one place, the generated licences in another; set at once even for a scheduled decrease | Gone: `PlatformSubscription.Licences` already holds the quantity (Slice 3) |
| Release request | A flag on a bound licence; no reason, no decision record | `PlatformDeviceReleaseRequest`: device, requested by (Operations user reference), reason code, status Pending, Approved, Refused, Withdrawn, decided by, row version (D-L6) |
| (none) | Governance only as nullable columns | `PlatformDeviceEvent` append-only history, same commit, plus audit (D-L14) |

V3 values fixed in code: replacement link 30 minutes; first-device link 24 hours; link token 64 random bytes, SHA-256 hex stored; licence code 12 hex digits plus a two-digit checksum; plan limit `MaxLicensedDevices`.

## 4. Behaviour found

### How licences come into being

- Onboarding: when the registration invoice is paid (or a trial is approved), RegisterPayment generates one licence row per licence bought, checks the plan's `MaxLicensedDevices` (not the tenant override) and sets the subscription counter. Capacity expansion is a prorated invoice opened by staff; its payment generates more rows.
- Recurring invoices bill the number of non-revoked licence rows, not the subscription quantity.
- No code ever revokes a licence. A requested decrease lowers the counter at once while the rows stay, so billing never goes down.

### How devices register

| Path | Who starts | Who approves | Result |
|---|---|---|---|
| First device | Owner, from an emailed setup link (24 hours) during registration | Nobody: the link is the approval | Device Registered, first open licence bound, registration Completed, **tenant activated** |
| Further device | A device signed in to Operations sends its identifier | A tenant user, through Operations | Pending, then Registered with the first open licence, or Disabled |
| Replacement | Any signed-in Vixly staff, from the tenant panel | Nobody: the link is the approval | Old device Disabled, licence moved to a new device with a server-made identifier |

The identifier is the device's secret. Operations keeps it in the browser (`vx-device-id`), sends it in plain text, and Platform stores it in plain text and lists it back.

### Release

- A tenant user asks Operations to release a bound licence. The licence is flagged. The approve and deny use cases exist but nothing calls them, so a request is never decided.
- Approving would disable the device and return the licence to Open. A second, unused release path frees the licence and leaves the device Registered.

### Portal access validation

- `validate-runtime` (before sign-in): portal slug, tenant state, subscription; returns branding for the landing page.
- `verify-device` (after sign-in): tenant id and identifier; checks device status and bound licence.
- `validate` (legacy): slug plus optional identifier, all checks at once.
- All three are anonymous with no rate limit and answer with distinct codes (`tenant_suspended`, `tenant_banned`, `device_not_registered`, `device_license_invalid`, ...). The result separates "customer access allowed" from "staff Operations access allowed".
- While a tenant is suspended for non-payment, licences stay bound; devices fail only because the tenant check fails first.

### Console screens found

| V3 page | State | V4 |
|---|---|---|
| Tenant panel, Licences | Whole list; "Generate link" and "Deny" only on the first bound licence; "Deny" does nothing; "Register device" on an open licence always fails; the raw link is shown in a text box | Tenant page Licensing section: seats bought, in use, free, pending; paged devices; actions by permission (D-L15) |
| Manage tenant licences (capacity expansion) | Quantity form, opens an expansion invoice | Gone: licence changes are Slice 3's "change subscription" (prorated) |
| `TenantLicensingSection`, `LicenseList` | Built, not used (release approve, deny, request) | Release review queue (D-L6) |

## 5. Defects and risks seen in V3 (fixed by design in V4)

1. The device credential is a plaintext identifier: stored in plain text, listed to Operations and the console, and accepted anonymously with a tenant id. It never rotates or expires. Anyone who sees it holds a licensed device.
2. Portal validation and recovery completion are anonymous with no rate limit. They sit outside `/api/internal`, so the internal API key does not cover them. Distinct codes reveal tenant status and whether a device exists.
3. Identifiers are chosen by the client. A denied device goes back to Pending by registering again. Pending requests have no cap and never expire.
4. Any signed-in console user can create a replacement link for any tenant, see the raw link on screen, open it and enrol a device of their own, which disables the owner's device. No permission, step-up or audit. The internal API also returns the raw link to Operations.
5. Using the link registers the new device with no approval and records the bind as done by the staff member who made the link. The owner is not told afterwards.
6. Release requests can never be decided: the approve and deny use cases and their screen are not wired. Broken console buttons ("Deny" without handler, "Register device" that always fails).
7. Two release rules: one disables the device, the other leaves it Registered.
8. The licence quantity never goes down. Licences are never revoked, billing counts licence rows, and a decrease only changes a counter with three meanings. Plan changes are checked against "active devices" while billing uses another number.
9. Races: count-then-insert when generating licences; two approvals can take the same "first open licence"; one link can be consumed twice in parallel. No row version on devices, licences or replacement tokens; one device can be bound to two licences (index not unique).
10. No tenant or subscription status check when registering, approving or recovering a device: devices can be added to a suspended or ended tenant. Generation uses the plan limit and ignores overrides.
11. First device: two commits (registration and tenant activation first, token consumption second), a hard-coded fake actor id, setup and welcome emails sent directly after the commit in English. The anonymous "resume" endpoint, given only a registration id, voids the owner's link and sends a new one on every call.
12. No history or audit. The deny reason is dropped, the person who refused a release is not stored, and a new bind wipes the release and revoke columns.
13. Unbounded lists (devices, licences joined in memory); the pending filter runs in memory; listing licences is a POST with the tenant id in the body.
14. The internal API trusts actor ids sent in request bodies and checks no permission. Any caller holding the API key can act for any tenant.
15. English-only exception texts and emails; codes exist only on portal validation.
16. Licence codes are display-only (48 random bits, never redeemed) and the generator lives in the wrong folder.
17. Leftovers: last-seen never written; unused `ReEnable`, `RenameCode`, repository methods, an empty interface and an empty response class; a Platform class in an Operations namespace; a use case registered twice; a `ping` endpoint.
18. Four portal validators (three endpoints and one unregistered class); the legacy one "kept temporarily".
19. Link tokens travel in the query string and can reach logs and referrers (short life and single use limit the harm).
20. No licensing tests; only one test that first-device setup goes through the token endpoint.

Kept from V3 because they are right: a licence is per device, not per user; links are 64 random bytes with only a SHA-256 hash stored, single use, short life, and a newer link voids the older; recovery completes in one commit; "release requested" is a request, not a device state.

## 6. Decisions

Proposals for the owner; answers go into the Slice 7 design note.

| # | Question | Proposal |
|---|---|---|
| D-L1 | What is a licence? | A seat that lets **one device** of a tenant into the tenant's back office (Operations). Not a user and not a customer. The number of seats is `PlatformSubscription.Licences` (Slice 3), which Billing already charges. No licence rows, no generation step and no `VXL-` codes: a device uses a seat while it is Active or has a release pending. Devices show a short public number (`D-000123`) instead of a code |
| D-L2 | Seats against subscription and plan | Devices using a seat may never exceed `Licences`; `Licences` is already capped by the effective `max_licensed_devices` limit and the plan minimum (2) in Slice 3. Approval with no free seat is refused (`platform.licensing.no_free_seat`) and the answer says how many are in use; the tenant adds seats through Slice 3 (prorated now). Slice 7 replaces `PendingPlatformLicenceCounter` with the real count. Concurrency: one `PlatformTenantLicensing` row per tenant (seats in use, row version) updated in every approve, release and revoke commit; a filtered unique index allows one Active device per credential. Pending devices do not use a seat |
| D-L3 | Device states | Pending (asked, not approved), Active (holds a seat), Release pending (still holds a seat), Released, Revoked (by staff, by the tenant admin for a lost device, or by the system), Denied, Expired (pending more than 7 days). Released, Revoked, Denied and Expired are final; a device that comes back enrols again as a new device. This removes V3's "denied device re-requests for ever" |
| D-L4 | How further devices register and who approves | A device opens the tenant portal, the staff member signs in (Operations), and Operations asks Platform to enrol it with name and kind. Platform creates it Pending, at most 10 pending per tenant, and returns a device id and credential (D-L5). A tenant admin approves or denies it in Operations; Operations sends the decision with its own user reference and display name (ADR-0003: Platform holds no tenant users) and Operations checks its own permission and step-up for that admin. Platform checks the tenant is Active, the subscription access level is Full and a seat is free. The owner gets an email when a device is approved ("new device added"). Vixly staff do not approve tenant devices |
| D-L5 | Device identity and runtime trust | No browser fingerprint (unstable, and personal data). On enrolment Platform issues a random credential (32 bytes from `IPlatformSecretGenerator`) and stores only its SHA-256 hash. Operations keeps it in a Secure, HttpOnly cookie on the tenant host, or in the app's secure store for scanners. Each verification may rotate it: after 24 hours Platform returns a new credential; the previous one stays valid for 5 minutes. Use of an old credential after that grace marks the device Revoked (`credential_reused`) and emails the owner. Operations may cache a positive answer for at most 5 minutes and drops it on `platform.device.revoked.v1`. Last seen is written at most once an hour. A signed key pair (WebCrypto non-extractable key) is the stronger option if the owner wants it; it needs a challenge round trip on every check |
| D-L6 | Release: who asks, who approves, limits | A tenant admin releases a device in Operations with a reason code (replaced, lost, broken, staff left, other). Within a free allowance (each rolling 30 days: 2 releases, or 25% of seats if more) the release is immediate: the device is Released, its credential stops working and the seat is free in the same commit. Above the allowance the device goes to Release pending (keeps its seat and still works) and appears in the Platform release queue; staff with `platform.licensing.approve_release` approve or refuse with a reason and step-up. A lost or stolen device can be revoked at once by the tenant admin; that counts against the allowance but never waits. The admin may withdraw a pending request. The tenant is emailed the decision |
| D-L7 | First device at registration | Registration gets the step V4 already declares: after account setup (Operations), Operations asks Platform for the first-device link; Platform moves the registration to Pending first device and emails the owner a single-use link (24 hours, hash only, newer voids older, outbox, tenant language). The page is served by Operations on the tenant host. Opening it on a device enrols that device as Active without approval, uses one seat, completes the registration and activates the tenant (lifecycle event, actor "registration") in **one commit**. The anonymous resend is limited to 3 per hour per registration and needs the registration access token (Slice 2), not just the id. Sales-led tenants activated by staff (Slice 2) get their first device the same way, from the tenant page action "send first-device link" |
| D-L8 | Owner device recovery (V3 replacement link) | For an owner who has lost every device. Staff with `platform.licensing.manage` and step-up, or the owner through Operations' "I lost my device" page after email confirmation, start it. Platform emails a single-use link to the owner contact only (60 minutes, hash only, outbox, tenant language); the link is never returned by an API or shown on screen. Using it enrols the new device as Active; if no seat is free, the device the owner names is revoked in the same commit. The owner gets "device replaced". The endpoint is rate-limited per IP and per link (5 tries) |
| D-L9 | Fewer seats, downgrade, ended subscription, blocked | Slice 3 already refuses a decrease or downgrade below active devices; the tenant releases devices first (the refusal lists them). While a decrease is scheduled, new approvals count against the lower number, so the renewal always fits. Subscription Ended, tenant Deactivated or Banned: every device is Revoked (`subscription_ended` or `tenant_closed`) by an outbox handler, one commit per tenant. Tenant Suspended (conduct): devices kept, checks fail with `tenant_suspended`. Blocked for non-payment or Pending payment: nothing is released or revoked (D-C17); the check answers with the access level. Billing never reads device counts |
| D-L10 | Portal access validation | Two internal Platform queries in `Contracts.Platform`, called by Edge and Operations over the internal channel of Slice 9, not by browsers: `GetPlatformPortalAccess(slug)` returns tenant id, tenant status and subscription access level (Full, Read-only, Payment required, None); `VerifyPlatformDevice(tenant, device id, credential)` returns allowed or a code (`device_unknown`, `device_pending`, `device_released`, `device_revoked`, `credential_invalid`, `tenant_suspended`, `tenant_closed`, `subscription_none`) plus the access level and, when rotating, a new credential. Unknown, never-active and closed tenants give the same answer to the outside. Answers are codes; text is resolved by the caller (Slice 9 rule). No public `/api/portal/access/*`. Customers are never device-checked and never blocked for payment; customer portal availability follows the tenant status only |
| D-L11 | Platform or Operations? | **Platform** owns devices, seats in use, credentials (hashes), links, release requests, the release allowance and queue, licensing history, audit, licensing emails and the console section. **Operations** owns tenant staff sign-in, the tenant admin's devices screen and its permission and step-up, keeping the credential on the device, and enforcing the access level (Read-only, Payment required) in its back office. Slice 7 builds the Platform use cases and the contract shapes; Slice 9 exposes them on the internal channel; Stage 3 calls them. Until then the first-device step is built and tested but not reachable (registration waits at account setup, as today) |
| D-L12 | Anonymous and device-facing entry points | Listed in the anonymous-endpoint register with limits (S-6): link completion (first device, recovery) 10 per hour per IP and 5 tries per link; enrolment 20 per hour per tenant and 10 pending per tenant; device verification limited per device (60 per minute) on the internal channel. One answer for unknown, expired, used or revoked links (`platform.licensing.link_invalid`) |
| D-L13 | Who may do what in the console | `platform.licensing.view` (summary, devices, history, queue); `platform.licensing.manage` (send first-device link, start owner recovery, revoke a device for security; step-up and reason); `platform.licensing.approve_release` (step-up). Billing role: view. Support role: view and manage. Administrator: all. Every use case checks its permission; actions coming from Operations are checked against the calling tenant (S-13) and never trust a tenant id or actor id the caller could not have |
| D-L14 | History and audit | `PlatformDeviceEvent` append-only: requested, approved, denied (reason code), released, release requested, release approved or refused, withdrawn, revoked (reason code), expired, replaced, credential reuse detected, link sent, link used. Codes and ids only; the Operations actor stored as reference plus display name. Written in the same commit as the change with an audit entry (D-2). Outbox messages `platform.device.approved.v1`, `.released.v1`, `.revoked.v1`, `platform.licensing.seats_changed.v1` |
| D-L15 | Lists and screens | Tenant page Licensing section (Slice 2 placeholder): seats bought, in use, free, pending, scheduled decrease; paged device list (P-1) with status filter and search by name or number; device page with history; idle devices (not seen for 60 days) marked, never released automatically. Release queue page: paged, oldest first. The Operations-facing device list is paged the same way |
| D-L16 | Emails | First-device link, owner recovery link, device replaced, new device approved (owner), release decided, devices revoked because the subscription ended. Outbox, tenant language (en, fr, es), sent to the owner contact (Slice 2). Links point to the tenant host with the token after `#`, which browsers do not send to servers, so it stays out of logs and referrers; the page posts it once |
