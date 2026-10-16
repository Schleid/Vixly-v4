# Slice 7: Licensing - design note

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Approved by the owner, 2026-10-16** (section 9). Building |
| Inputs | [Sweep](SLICE-7-LICENSING-SWEEP.md), Slice 2 design (registration, tenant status, owner contact), Slice 3 design (licence quantity, `max_licensed_devices`), Slice 6 design (D-C4, D-C17 access levels), ADR-0003 (tenant users live in Operations) |
| Depends on | Slices 2, 3 and 6 (built) |

## 1. Outcome

A licence is a seat for one device of a tenant's back office. A tenant has as many seats as its subscription's licence quantity, which Billing already charges, and never more devices in use than seats.

**Devices:**
- New devices ask to join; a tenant admin approves them in Operations when a seat is free.
- Each device proves who it is on every use, with one of two methods. A device that can hold a key signs a fresh challenge with a key that never leaves it. Any other device uses a rotating secret that Platform stores only as a hash.
- Copying a device's proof does not work for long. A reused old secret disables the device.

**Releases:** tenants free seats themselves within an allowance; past it, Vixly staff review each release. A lost or stolen device is always cut off at once.

**Registration and recovery:**
- At registration the owner gets a link to enrol the first device. Using it completes the registration and activates the tenant in one step.
- An owner who lost every device gets a recovery link by email only.

**Other effects:**
- Blocking for non-payment releases nothing.
- An ended subscription or a closed account revokes the devices.
- Customers are never device-checked.
- Platform owns the records and the checks. Operations (Stage 3) calls them through the internal channel of Slice 9.

## 2. Data (schema `platform`)

| Entity | Table | Notes |
|---|---|---|
| `PlatformTenantLicensing` | `TenantLicensing` | One row per tenant: seats in use (Active and Release pending devices), row version. Every approve, release, revoke and replace changes it in the same commit, so two hosts cannot hand out the same seat |
| `PlatformDevice` | `Devices` | Tenant, public number (`D-000123`), name, kind (PC, phone, scanner, tablet, other), proof method (key pair or rotating secret), public key (ECDSA P-256, key pair only), current and previous secret hash with times (secret only), status, requested by and approved by (Operations user reference and display name), last seen (written at most hourly), status reason, row version |
| `PlatformDeviceChallenge` | `DeviceChallenges` | Key-pair proof: a random challenge (hash only), device, expiry (2 minutes), used at; single use |
| `PlatformDeviceLink` | `DeviceLinks` | First-device and owner-recovery links: kind, tenant, registration (first device), token hash, expiry (24 hours, recovery 60 minutes), used at, tries; a newer link voids older ones |
| `PlatformDeviceEvent` | `DeviceEvents` | Append-only history: codes and ids only; Operations actors as reference plus display name |

No licence rows and no `VXL-` codes (D-L1). Seat count = `PlatformSubscription.Licences`.

## 3. Rules

### Device states (D-L3)

| State | Holds a seat | Notes |
|---|---|---|
| Pending | No | Asked to join; at most 10 pending per tenant; expires after 7 days |
| Active | Yes | Approved; passes the device check |
| Release pending | Yes | Over the allowance; waits for Vixly staff; still works |
| Released, Revoked, Denied, Expired | No | Final; a device that comes back enrols again as a new device |

A lost or stolen device revoked over the allowance is in **Revoked, seat held**: it stops working at once, but its seat comes back only when Vixly staff approve.

### Joining and approval (D-L4; owner: tenant admin approves)

- Operations enrols a device after a tenant staff member signs in: name, kind, and either a public key (key-pair method) or nothing (secret method). Platform answers with the device id and, for the secret method, the first secret.
- A tenant admin approves or denies in Operations. Operations checks its own permission and step-up and sends its user reference and display name; Platform holds no tenant users (ADR-0003).
- Approval needs: tenant Active, subscription access Full, a free seat. Otherwise `platform.licensing.no_free_seat` (with seats in use) or the access reason.
- The owner gets "new device added".
- Vixly staff do not approve devices.

### Proving the device on every use (D-L5; owner: both methods)

- **Key pair** (preferred where the device supports it, for example a browser with WebCrypto or a scanner app):
  - Operations asks Platform for a challenge (random, 2 minutes, single use).
  - The device signs it (ECDSA P-256, SHA-256) with a key that cannot be exported.
  - Platform checks the signature against the stored public key.
- **Rotating secret** (every other device):
  - The device keeps a random 32-byte secret in a Secure, HttpOnly cookie or the app's secure store; Platform stores only its SHA-256.
  - After 24 hours the check returns a new secret; the old one still works for 5 minutes.
  - Using an old secret after that marks the device **Revoked** (`credential_reused`) and emails the owner.
- **The check answers** allowed, or a code: `device_unknown`, `device_pending`, `device_released`, `device_revoked`, `proof_invalid`, `tenant_suspended`, `tenant_closed`, `subscription_none`. With it comes the subscription access level (Full, Read-only, Payment required) and, when rotating, the new secret.
- **Caching:** Operations may cache a positive answer for at most 5 minutes and drops it on "device revoked".

### Releases (D-L6; owner: allowance, then Vixly review)

- **Who and why:** a tenant admin releases a device in Operations with a reason (replaced, lost or stolen, broken, staff left, other).
- **Within the allowance** (2 releases in any 30 days, or 25% of seats if that is more): the release is immediate. The device is Released, its proof stops working, and the seat is free, all in one commit.
- **Over the allowance:** the device goes to Release pending (it keeps its seat and still works) and appears in the Vixly release queue. Staff with `platform.licensing.approve_release` approve or refuse, with a reason and step-up; the tenant is emailed the decision.
- **Lost or stolen** always stops the device at once. Over the allowance it waits in the queue as "Revoked, seat held" until staff decide.
- The admin may withdraw a pending release.

### First device (D-L7)

- After account setup, Operations asks Platform for the first-device link. Platform moves the registration to Pending first device and emails the owner a single-use link (24 hours, hash only, newer voids older).
- **Using the link, in one commit:**
  - The device enrols (either proof method) as Active, without approval, using one seat.
  - The registration completes, and the tenant is activated (lifecycle event, actor "registration").
- **Resend:** at most 3 per hour, and needs the registration access token.
- **Tenants created by staff** get the link from the tenant page action "Send first-device link".

### Owner recovery (D-L8)

- **Who starts it:** staff with `platform.licensing.manage` and step-up, or the owner through Operations after email confirmation.
- **The link:** emailed to the owner contact only (60 minutes, 5 tries). It is never shown on screen or returned by an API.
- **Using it:** the device enrols as Active. If no seat is free, the device the owner names is revoked in the same commit. The owner gets "device replaced".

### Seats and the subscription (D-L2, D-L9)

- Approval with no free seat is refused. The tenant adds seats through Slice 3.
- A licence decrease or downgrade below the devices in use is refused (Slice 3 now counts real devices). While a decrease is scheduled, approvals count against the lower number.
- **Ended subscription, account Deactivated or Banned:** every device is Revoked (`subscription_ended`, `tenant_closed`), one commit per tenant.
- **Account Suspended:** devices are kept; checks answer `tenant_suspended`.
- **Blocked for non-payment or Pending payment:** nothing is released or revoked (D-C17); the check carries the access level.
- **Customers** are never device-checked.

### Portal access (D-L10)

`GetPlatformPortalAccess(slug)` answers tenant id, tenant status and access level. Unknown, never-active and closed tenants all get the same answer. It and the device check are internal queries for Edge and Operations (exposed in Slice 9), not public endpoints.

## 4. Permissions (D-L13)

| Permission | Step-up | Use |
|---|---|---|
| `platform.licensing.view` | | Tenant Licensing section, devices, history, release queue |
| `platform.licensing.manage` | Yes | Send first-device link, start owner recovery, revoke a device for security |
| `platform.licensing.approve_release` | Yes | Decide releases over the allowance |

Billing clerk: view. Security administrator: view, manage. Administrator: all.

## 5. Use cases

**Console:** tenant licensing summary; list devices (paged, status filter, search); get device with history; release queue; decide a release; revoke a device; send the first-device link; start owner recovery.

**For Operations** (internal; Slice 9 exposes them):
- enrol device; approve; deny; release; withdraw release
- create challenge; verify device
- get portal access; request the first-device link
- complete a device link
- start owner recovery

**Handlers:** subscription ended, account closed; expiry of pending devices and links.

## 6. Contract for Operations

- **The device check is the only proof.** Operations never trusts a device id alone.
- **Keeping the proof:** Operations keeps the secret in a Secure, HttpOnly cookie on the tenant host, or the private key as a non-extractable WebCrypto key or in the scanner's secure store.
- **Who acts:** Operations sends the acting tenant user's reference and display name, and its own permission and step-up result, for approvals and releases.
- **Links:** they carry the token after `#`, so it never reaches server logs; the page posts it once.

## 7. Console screens

- **Tenant page, Licensing section:** seats bought, in use, free, pending, any scheduled decrease; paged device list with status filter and search; actions "Send first-device link" and "Start owner recovery".
- **Device page:** state, proof method, last seen, history; action "Revoke".
- **Release queue** (Commercial menu): paged, oldest first; approve or refuse with a reason.

## 8. Done when (in addition to the slice gate)

- Devices holding a seat never exceed the subscription's licences, even with two hosts racing.
- A device passes the check only with a valid signature or current secret; a reused old secret revokes it.
- Releases within the allowance are immediate; over it they wait for Vixly staff, and lost or stolen devices stop at once without freeing the seat early.
- The first-device link completes the registration and activates the tenant in one commit; links are single use, hash only, and never shown or returned by an API.
- Blocking for non-payment releases nothing; an ended subscription revokes everything; customers are never device-checked.
- History and audit in the same commit; emails through the outbox in en, fr and es; every list paged.

## 9. Owner decisions

Settled by the owner on 2026-10-16:

- **New devices:** a tenant admin approves them in Operations when a seat is free.
- **Device proof:** both methods: a device key pair where the device supports it, otherwise a rotating secret.
- **Releases:** immediate within the allowance; over the quota, each release waits for Vixly staff review.
- **Everything else:** as proposed in the [sweep](SLICE-7-LICENSING-SWEEP.md), section 6.

## 10. Build notes

Decisions taken while building; none changes an owner decision.

- **Seat limit.** A device can be approved only while the seats in use are below both the licences bought and any lower licence count already scheduled for the next period. So a scheduled decrease is never overtaken by new approvals.
- **One seat row per tenant** (`TenantLicensing`) with a row version. Two approvals racing for the last seat: one commits, the other gets "No licence is free". Device numbers (`D-000001`) are unique per tenant.
- **The subscription's "x active of y licences"** now counts devices holding a seat. The Slice 3 placeholder counter (`PendingPlatformLicenceCounter`) is removed.
- **Release allowance.** Counted over devices the tenant ended itself (released or reported lost or stolen) in the last 30 days: 2, or 25 % of the licences when that is more. A release within the allowance frees the seat at once; past it, it goes to Vixly's review queue and the seat stays taken until staff decide.
- **Lost or stolen** always stops the device at once. Within the allowance the seat is freed; past it, the device is revoked but keeps its seat until staff approve the release.
- **Rotating secret.** The first check after 24 hours returns a new secret; the old one still works for 5 minutes. Using the old secret after that revokes the device (reason "Old secret used again"), frees its seat and emails the owner.
- **Device key.** A challenge lives 2 minutes and works once; signatures are ECDSA P-256 over SHA-256 (IEEE P1363 format). The key is checked when the device is added.
- **Waiting requests** expire after 7 days, checked by the clock every minute. At most 10 requests can wait per tenant.
- **Last seen** is written at most once an hour per device, so checks stay read-only most of the time.
- **Owner links** (first device, recovery) are created when the email is sent; only their hash is stored. A new link voids older ones of the same kind. A link allows 5 tries; at most 3 links per tenant per hour.
- **Recovery when every seat is taken:** the owner names a device to replace. That device is revoked ("Replaced during owner recovery") and the owner gets an email about it.
- **First-device link** completes the registration and activates the tenant in one commit. The activation event has no staff actor; its audit action is `platform.tenants.activated_by_registration`.
- **Staff "Send the first-device link again"** shows only when no device holds a seat (a new tenant, or one whose devices were all ended).
- **Internal check answers** (for Slice 9): allowed, or refused with `device_unknown`, `device_pending`, `device_released`, `device_revoked`, `proof_invalid`, `tenant_suspended`, `tenant_closed` or `subscription_none`; plus the subscription access level and, when rotating, the new secret. Customers are never checked.
- **Use-case contexts.** The Operations calls take `PlatformTenantAuthorizationContext` (tenant, user reference and name) and the internal check takes `PlatformInternalAuthorizationContext`, following the rule that every use case starts with an authorization context.
- **Device history** (`DeviceEvents`) is append-only in the global script (DENY UPDATE, DELETE to the app login).
- **Demo data** adds three devices per active demo tenant: two approved, one waiting, and one release under review on the first tenant. Demo secrets are random and discarded.
