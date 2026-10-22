# Slice 1C: Going live (onboarding completion) - verification record

| | |
|---|---|
| Plane | Operations (Stage 3), with a Platform change |
| Date | 2026-10-18 |
| Built on | Slice 1B as delivered (with the call-centre phones change); not committed here: the dev team owns git |
| Design | [Slice 1 design note](SLICE-1-STAFF-AND-SETUP-DESIGN.md) sections 1b and 3, owner decisions D-O13 and D-O14, ADR-0012, ADR-0019 |
| Status | **Built and verified by automated tests; waiting for owner sign-off.** Slice 1D (staff, roles, step-up) is built on top of it |

## 1. What was built

### Onboarding, end to end (owner, 2026-10-18)

The order is the one the owner described: registration and payment on the public site, email verification, then Platform's emails, and **the Welcome email last**.

1. Platform emails the Owner the invitation. The Owner chooses a password and enrols an authenticator app on `{slug}.techcrafters.ltd` (Slice 1A).
2. The setup page offers **"Email me the link"**: Platform sends the first-device link to the Owner's email (`/setup/first-device`). The Terms were accepted at registration, so setup no longer asks for them.
3. The link opens `/devices/first#token` on the tenant host. The Owner names the computer and chooses its kind (computer, phone, tablet, scanner, other). Operations completes the link with Platform. Platform refuses a link that belongs to another tenant.
4. Platform activates the tenant and sends the **Welcome email**: the company name, the staff address (`{slug}.../staff`) and the public site address (`{slug}...`). It is the last onboarding email (`PlatformLicensingNoticeKind.Welcome`, en, fr, es).

### Licensed devices

- **Device gate.** Once the tenant is live, every signed-in `/staff/*` page needs a licensed device.
  - The device is identified by the cookie `__Host-vx4-device` (device id plus a secret Platform rotates).
  - Sign-in, sign-out, forgot password and reset password are exempt.
  - A device waiting for approval goes to `/devices/waiting`. Any other refusal goes to `/devices/enrol?reason=…` and clears the cookie.
  - A positive answer is kept 5 minutes; a refusal is never kept.
  - A rotated secret rewrites the cookie in the same response.
- **New computer** (`/devices/enrol`): the person names it. It waits until someone approves it.
- **Devices page** (`/staff/devices`):
  - pending and active devices;
  - **Approve** or **Deny**;
  - **Release**, with a reason: lost or stolen, broken, staff left, replaced, other.
  - Approve and release need a fresh authenticator code on the same form.
  - Platform receives the role (owner, admin, staff) and whether the code was fresh.

### Forgot password (every login page)

- `/staff/forgot-password` emails a single-use link, valid 60 minutes. The answer is the same whether or not the email exists. Limit: 5 per hour per address.
- `/staff/reset-password#token` sets the new password and **ends every session** of that person.
- This is the Owner's self-service reset too (owner, 2026-10-18).

### Terms and access level

- `/staff/legal` shows the Terms status: not required, accepted, due or overdue.
  - The Owner reads the PDFs (served from the tenant host) and accepts the current version.
  - Platform's emails link to `/legal`, which redirects here.
- **Access gate.** When Platform says the company is not at Full access (read-only, or payment required), every change under `/staff` goes to `/staff/limited`, which explains why.
  - A banner on every staff page says the same.
  - Pages that only show still work.

### Owner recovery through Vixly support (D-O13)

- **Lost devices:** support starts the recovery in the Platform console. It needs a reason and an identity check, and can replace one device. Platform emails the Owner a link to `/devices/recover#token`; the Owner enrols the new computer there.
- **Lost second factor:** support files a request and **a second staff member approves it**. Platform emails the link to `/account/second-factor-reset#token`. There the Owner confirms email and password. Operations checks the link with Platform, removes the old authenticator and recovery codes, ends the Owner's sessions, and starts a new enrolment.
- **No Operations page can start either recovery.** The Operations-started recovery of Slice 7 was removed from Platform.

### Platform change (Slices 1C and D-O13)

- New: `PlatformOwnerSupportRequest`, with kind and status. The approver must differ from the requester.
- New link kind: second-factor reset.
- New use cases: `RequestPlatformSecondFactorReset`, `DecidePlatformSecondFactorReset` and `CompletePlatformSecondFactorReset`.
- New internal endpoint: `POST /internal/v1/device-links/second-factor-reset/complete`.
- Support emails go to the Owner and Billing contacts.
- Owner recovery now needs a reason and an identity check.
- The console forms were updated.
- The Welcome notice, and links to the staff page and the site, now built from `PlatformTenants:PortalUrlTemplate`.
- Migration: `OwnerSupport`.

## 2. Automated checks

Release builds of the three solutions: no errors, no warnings.

| Test project | Slice 1B | Slice 1C |
|---|---|---|
| Operations.Domain.Tests | 52 | 52 |
| Operations.Application.Tests | 46 | 48 |
| Operations.Infrastructure.Tests (SQL Server) | 28 | 34 |
| Operations.Api.Tests | 14 | 14 |
| Operations.Web.Tests (SQL Server) | 25 | 31 |
| Platform tests | 657 | 665 |
| Architecture.Tests | 7,711 | 7,901 |

What the new tests cover:

- **Over HTTP, with a real database:**
  - the Owner enrols the first computer from the emailed link, and the tenant goes live;
  - a second computer waits for approval;
  - approval with a wrong code is refused; with a fresh code it works and Platform sees the step-up;
  - a rotated secret replaces the cookie;
  - a released computer is sent back to enrolment with its reason;
  - forgot password emails a single-use link and the new password ends every session;
  - overdue Terms make the company read-only until the Owner accepts;
  - the support second-factor reset needs the password, ends in a new enrolment, and a used link says it is no longer valid.
- **Use cases:** reset requests answer the same for unknown emails; reset links are single-use and expire.
- **Signer:** the actor claims (`sub`, `role`, `stepup`, `ip`, `ua`) and the translation of Platform's answers to `ops.*` codes.
- **Platform:**
  - the support request needs a second approver;
  - the reset link works once and only for its tenant;
  - recovery needs a reason and an identity check;
  - Welcome is sent after activation.

## 3. Where the build differs from the design note

- **Proof of the device:** browsers use the rotating secret only. A device key pair (WebAuthn-style proof) is deferred. Scanners and phone apps need it more, and arrive later.
- **Payment required:** for now it is treated like read-only (every change refused, with the reason). The design shows Owners and Administrators only the billing page; that page comes with settings (Slice 1E).

## 4. Deployment notes

- **Database:** run the newest global SQL script (migrations `OwnerSupport` on Platform, `ContactPhones` on Operations).
- **Platform:** the links in the Welcome and recovery emails are `https://{slug}.` plus `PlatformTenants:TenantBaseDomain`; nothing to set in production. Development sets `PlatformTenants:PortalUrlTemplate` to `https://{slug}.localhost:7404`.
- **Staging domain:** the owner registered **`vixly.freedns.org`** for staging (2026-10-18). Nothing uses it yet. The host names under it are an open question for the owner (see the Slice 1D record, section 5).
